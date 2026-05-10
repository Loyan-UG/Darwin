using System.Text;
using Darwin.Application;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Contracts.Common;
using Darwin.Contracts.Orders;
using Darwin.Domain.Enums;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Member;

/// <summary>
/// Member-scoped order history endpoints for the front-office member portal and consumer apps.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/member/orders")]
public sealed class MemberOrdersController : ApiControllerBase
{
    private readonly GetMyOrdersPageHandler _getMyOrdersPageHandler;
    private readonly GetMyOrderForViewHandler _getMyOrderForViewHandler;
    private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;
    private readonly StorefrontCheckoutUrlBuilder _checkoutUrlBuilder;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberOrdersController"/> class.
    /// </summary>
    public MemberOrdersController(
        GetMyOrdersPageHandler getMyOrdersPageHandler,
        GetMyOrderForViewHandler getMyOrderForViewHandler,
        CreateStorefrontPaymentIntentHandler createStorefrontPaymentIntentHandler,
        StorefrontCheckoutUrlBuilder checkoutUrlBuilder,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getMyOrdersPageHandler = getMyOrdersPageHandler ?? throw new ArgumentNullException(nameof(getMyOrdersPageHandler));
        _getMyOrderForViewHandler = getMyOrderForViewHandler ?? throw new ArgumentNullException(nameof(getMyOrderForViewHandler));
        _createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));
        _checkoutUrlBuilder = checkoutUrlBuilder ?? throw new ArgumentNullException(nameof(checkoutUrlBuilder));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns a paged order history for the current authenticated member.
    /// </summary>
    [HttpGet]
    [HttpGet("/api/v1/orders")]
    [ProducesResponseType(typeof(PagedResponse<MemberOrderSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyOrdersAsync([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem(_validationLocalizer["PageMustBePositiveInteger"]);
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(20);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem(_validationLocalizer["PageSizeMustBeBetween1And200"]);
        }

        var (items, total) = await _getMyOrdersPageHandler
            .HandleAsync(normalizedPage, normalizedPageSize, ct)
            .ConfigureAwait(false);

        return Ok(new PagedResponse<MemberOrderSummary>
        {
            Total = total,
            Items = items.Select(MapSummary).ToList(),
            Request = new PagedRequest
            {
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                Search = null
            }
        });
    }

    /// <summary>
    /// Returns the detail of a single order owned by the current authenticated member.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HttpGet("/api/v1/orders/{id:guid}")]
    [ProducesResponseType(typeof(MemberOrderDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyOrderAsync(Guid id, [FromQuery] string? culture = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var normalizedCulture = BusinessControllerConventions.NormalizeNullable(culture);
        var dto = await _getMyOrderForViewHandler.HandleAsync(id, normalizedCulture, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["OrderNotFound"]);
        }

        return Ok(MapDetail(dto));
    }

    /// <summary>
    /// Creates or reuses a storefront payment intent for a member-owned order.
    /// </summary>
    [HttpPost("{id:guid}/payment-intent")]
    [HttpPost("/api/v1/orders/{id:guid}/payment-intent")]
    [ProducesResponseType(typeof(CreateStorefrontPaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentIntentAsync(Guid id, [FromQuery] string? culture = null, [FromBody] CreateStorefrontPaymentIntentRequest? request = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var normalizedCulture = BusinessControllerConventions.NormalizeNullable(culture);
        var dto = await _getMyOrderForViewHandler.HandleAsync(id, normalizedCulture, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["OrderNotFound"]);
        }

        if (!CanRetryPayment(dto))
        {
            return BadRequestProblem(_validationLocalizer["OrderCannotAcceptNewPaymentAttempt"]);
        }

        try
        {
            var returnUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.Id, dto.OrderNumber, cancelled: false);
            var cancelUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.Id, dto.OrderNumber, cancelled: true);
            var result = await _createStorefrontPaymentIntentHandler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = dto.Id,
                UserId = GetCurrentUserId(),
                OrderNumber = dto.OrderNumber,
                Provider = string.IsNullOrWhiteSpace(request?.Provider) ? "Stripe" : request.Provider.Trim(),
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl
            }, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(result.CheckoutUrl))
            {
                throw new InvalidOperationException(_validationLocalizer["StorefrontStripeCheckoutNotConfigured"]);
            }

            return Ok(new CreateStorefrontPaymentIntentResponse
            {
                OrderId = result.OrderId,
                PaymentId = result.PaymentId,
                Provider = result.Provider,
                ProviderReference = result.ProviderReference,
                ProviderPaymentIntentReference = result.ProviderPaymentIntentReference,
                ProviderCheckoutSessionReference = result.ProviderCheckoutSessionReference,
                AmountMinor = result.AmountMinor,
                Currency = result.Currency,
                Status = result.Status.ToString(),
                CheckoutUrl = result.CheckoutUrl,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                ExpiresAtUtc = result.ExpiresAtUtc
            });
        }
        catch (InvalidOperationException)
        {
            return BadRequestProblem(_validationLocalizer["PaymentIntentCreationFailed"]);
        }
    }

    /// <summary>
    /// Downloads a member-friendly plain-text document for an owned order.
    /// </summary>
    [HttpGet("{id:guid}/document")]
    [HttpGet("/api/v1/orders/{id:guid}/document")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocumentAsync(Guid id, [FromQuery] string? culture = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var normalizedCulture = BusinessControllerConventions.NormalizeNullable(culture);
        var dto = await _getMyOrderForViewHandler.HandleAsync(id, normalizedCulture, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["OrderNotFound"]);
        }

        var fileName = $"order-{SanitizeFileToken(dto.OrderNumber)}.txt";
        var bytes = Encoding.UTF8.GetBytes(RenderOrderDocument(dto, normalizedCulture));
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    private static MemberOrderSummary MapSummary(MemberOrderSummaryDto dto)
        => new()
        {
            Id = dto.Id,
            OrderNumber = dto.OrderNumber,
            Currency = dto.Currency,
            GrandTotalGrossMinor = dto.GrandTotalGrossMinor,
            Status = dto.Status.ToString(),
            CreatedAtUtc = dto.CreatedAtUtc
        };

    private static MemberOrderDetail MapDetail(MemberOrderDetailDto dto)
        => new()
        {
            Id = dto.Id,
            OrderNumber = dto.OrderNumber,
            Currency = dto.Currency,
            PricesIncludeTax = dto.PricesIncludeTax,
            SubtotalNetMinor = dto.SubtotalNetMinor,
            TaxTotalMinor = dto.TaxTotalMinor,
            ShippingTotalMinor = dto.ShippingTotalMinor,
            ShippingMethodId = dto.ShippingMethodId,
            ShippingMethodName = dto.ShippingMethodName,
            ShippingCarrier = dto.ShippingCarrier,
            ShippingService = dto.ShippingService,
            DiscountTotalMinor = dto.DiscountTotalMinor,
            GrandTotalGrossMinor = dto.GrandTotalGrossMinor,
            Status = dto.Status.ToString(),
            BillingAddressJson = dto.BillingAddressJson,
            ShippingAddressJson = dto.ShippingAddressJson,
            CreatedAtUtc = dto.CreatedAtUtc,
            Lines = dto.Lines.Select(line => new MemberOrderLine
            {
                Id = line.Id,
                VariantId = line.VariantId,
                Name = line.Name,
                Sku = line.Sku,
                Quantity = line.Quantity,
                UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                LineGrossMinor = line.LineGrossMinor
            }).ToList(),
            Payments = dto.Payments.Select(payment => new MemberOrderPayment
            {
                Id = payment.Id,
                CreatedAtUtc = payment.CreatedAtUtc,
                Provider = payment.Provider,
                ProviderReference = payment.ProviderReference,
                ProviderPaymentIntentReference = payment.ProviderPaymentIntentReference,
                ProviderCheckoutSessionReference = payment.ProviderCheckoutSessionReference,
                AmountMinor = payment.AmountMinor,
                Currency = payment.Currency,
                Status = payment.Status.ToString(),
                PaidAtUtc = payment.PaidAtUtc
            }).ToList(),
            Shipments = dto.Shipments.Select(shipment => new MemberOrderShipment
            {
                Id = shipment.Id,
                Carrier = shipment.Carrier,
                Service = shipment.Service,
                TrackingNumber = shipment.TrackingNumber,
                TrackingUrl = shipment.TrackingUrl,
                Status = shipment.Status.ToString(),
                ShippedAtUtc = shipment.ShippedAtUtc,
                DeliveredAtUtc = shipment.DeliveredAtUtc
            }).ToList(),
            Invoices = dto.Invoices.Select(invoice => new MemberOrderInvoice
            {
                Id = invoice.Id,
                Currency = invoice.Currency,
                TotalGrossMinor = invoice.TotalGrossMinor,
                Status = invoice.Status.ToString(),
                DueDateUtc = invoice.DueDateUtc,
                PaidAtUtc = invoice.PaidAtUtc
            }).ToList(),
            Actions = BuildActions(dto)
        };

    private static MemberOrderActions BuildActions(MemberOrderDetailDto dto)
    {
        var canRetryPayment = CanRetryPayment(dto);
        return new MemberOrderActions
        {
            CanRetryPayment = canRetryPayment,
            PaymentIntentPath = canRetryPayment ? GetPaymentIntentPath(dto.Id) : null,
            ConfirmationPath = GetConfirmationPath(dto.Id),
            DocumentPath = GetDocumentPath(dto.Id)
        };
    }

    private static bool CanRetryPayment(MemberOrderDetailDto dto)
        => dto.Status is not OrderStatus.Cancelled and not OrderStatus.Refunded &&
           dto.GrandTotalGrossMinor > 0 &&
           dto.Payments.All(payment => payment.Status is not PaymentStatus.Captured and not PaymentStatus.Completed);

    private static string GetPaymentIntentPath(Guid id) => $"/api/v1/member/orders/{id:D}/payment-intent";

    private static string GetConfirmationPath(Guid id) => $"/api/v1/public/checkout/orders/{id:D}/confirmation";

    private static string GetDocumentPath(Guid id) => $"/api/v1/member/orders/{id:D}/document";

    private static string SanitizeFileToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "order";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim().Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "order" : sanitized;
    }

    private static string RenderOrderDocument(MemberOrderDetailDto dto, string? culture)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Order: {dto.OrderNumber}");
        builder.AppendLine($"Status: {Darwin.Application.Orders.MemberOrderPresentationResolver.ResolveOrderStatus(dto.Status, culture)}");
        builder.AppendLine($"CreatedAtUtc: {dto.CreatedAtUtc:O}");
        builder.AppendLine($"Currency: {dto.Currency}");
        builder.AppendLine($"SubtotalNetMinor: {dto.SubtotalNetMinor}");
        builder.AppendLine($"TaxTotalMinor: {dto.TaxTotalMinor}");
        builder.AppendLine($"ShippingTotalMinor: {dto.ShippingTotalMinor}");
        builder.AppendLine($"DiscountTotalMinor: {dto.DiscountTotalMinor}");
        builder.AppendLine($"GrandTotalGrossMinor: {dto.GrandTotalGrossMinor}");
        builder.AppendLine($"ShippingMethod: {dto.ShippingMethodName ?? dto.ShippingMethodId?.ToString("D") ?? "N/A"}");
        builder.AppendLine($"ShippingCarrier: {dto.ShippingCarrier ?? "N/A"}");
        builder.AppendLine($"ShippingService: {dto.ShippingService ?? "N/A"}");
        builder.AppendLine();
        builder.AppendLine("BillingAddressJson:");
        builder.AppendLine(dto.BillingAddressJson);
        builder.AppendLine();
        builder.AppendLine("ShippingAddressJson:");
        builder.AppendLine(dto.ShippingAddressJson);
        builder.AppendLine();
        builder.AppendLine("Lines:");
        foreach (var line in dto.Lines)
        {
            builder.AppendLine($"- {line.Name} | SKU: {line.Sku} | Qty: {line.Quantity} | UnitGrossMinor: {line.UnitPriceGrossMinor} | LineGrossMinor: {line.LineGrossMinor}");
        }

        builder.AppendLine();
        builder.AppendLine("Payments:");
        foreach (var payment in dto.Payments)
        {
            builder.AppendLine($"- {payment.Provider} | {Darwin.Application.Orders.MemberOrderPresentationResolver.ResolvePaymentStatus(payment.Status, culture)} | {payment.Currency} {payment.AmountMinor} | CreatedAtUtc: {payment.CreatedAtUtc:O} | Ref: {payment.ProviderReference ?? "N/A"} | PaidAtUtc: {payment.PaidAtUtc:O}");
        }

        builder.AppendLine();
        builder.AppendLine("Shipments:");
        foreach (var shipment in dto.Shipments)
        {
            builder.AppendLine($"- {shipment.Carrier} | {shipment.Service} | {Darwin.Application.Orders.MemberOrderPresentationResolver.ResolveShipmentStatus(shipment.Status, culture)} | Tracking: {shipment.TrackingNumber ?? "N/A"} | TrackingUrl: {shipment.TrackingUrl ?? "N/A"} | ShippedAtUtc: {shipment.ShippedAtUtc:O} | DeliveredAtUtc: {shipment.DeliveredAtUtc:O}");
        }

        builder.AppendLine();
        builder.AppendLine("Invoices:");
        foreach (var invoice in dto.Invoices)
        {
            builder.AppendLine($"- {invoice.Id:D} | {Darwin.Application.Orders.MemberOrderPresentationResolver.ResolveInvoiceStatus(invoice.Status, culture)} | {invoice.Currency} {invoice.TotalGrossMinor} | DueDateUtc: {invoice.DueDateUtc:O} | PaidAtUtc: {invoice.PaidAtUtc:O}");
        }

        return builder.ToString();
    }
}
