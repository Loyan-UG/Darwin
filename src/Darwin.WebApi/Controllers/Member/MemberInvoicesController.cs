using System.Text;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Domain.Enums;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Member;

/// <summary>
/// Member-scoped invoice history endpoints for the front-office member portal and consumer apps.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/member/invoices")]
public sealed class MemberInvoicesController : ApiControllerBase
{
    private readonly GetMyInvoicesPageHandler _getMyInvoicesPageHandler;
    private readonly GetMyInvoiceDetailHandler _getMyInvoiceDetailHandler;
    private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;
    private readonly StorefrontCheckoutUrlBuilder _checkoutUrlBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberInvoicesController"/> class.
    /// </summary>
    public MemberInvoicesController(
        GetMyInvoicesPageHandler getMyInvoicesPageHandler,
        GetMyInvoiceDetailHandler getMyInvoiceDetailHandler,
        CreateStorefrontPaymentIntentHandler createStorefrontPaymentIntentHandler,
        StorefrontCheckoutUrlBuilder checkoutUrlBuilder)
    {
        _getMyInvoicesPageHandler = getMyInvoicesPageHandler ?? throw new ArgumentNullException(nameof(getMyInvoicesPageHandler));
        _getMyInvoiceDetailHandler = getMyInvoiceDetailHandler ?? throw new ArgumentNullException(nameof(getMyInvoiceDetailHandler));
        _createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));
        _checkoutUrlBuilder = checkoutUrlBuilder ?? throw new ArgumentNullException(nameof(checkoutUrlBuilder));
    }

    /// <summary>
    /// Returns a paged invoice history for the current authenticated member.
    /// </summary>
    [HttpGet]
    [HttpGet("/api/v1/invoices")]
    [ProducesResponseType(typeof(PagedResponse<MemberInvoiceSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyInvoicesAsync([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page.GetValueOrDefault(1);
        if (normalizedPage <= 0)
        {
            return BadRequestProblem("Page must be a positive integer.");
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(20);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem("PageSize must be between 1 and 200.");
        }

        var (items, total) = await _getMyInvoicesPageHandler
            .HandleAsync(normalizedPage, normalizedPageSize, ct)
            .ConfigureAwait(false);

        return Ok(new PagedResponse<MemberInvoiceSummary>
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
    /// Returns the detail of a single invoice owned by the current authenticated member.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HttpGet("/api/v1/invoices/{id:guid}")]
    [ProducesResponseType(typeof(MemberInvoiceDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyInvoiceAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem("Id must not be empty.");
        }

        var dto = await _getMyInvoiceDetailHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem("Invoice not found.");
        }

        return Ok(MapDetail(dto));
    }

    /// <summary>
    /// Creates or reuses a storefront payment intent for a member-owned invoice that is linked to an order.
    /// </summary>
    [HttpPost("{id:guid}/payment-intent")]
    [HttpPost("/api/v1/invoices/{id:guid}/payment-intent")]
    [ProducesResponseType(typeof(CreateStorefrontPaymentIntentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentIntentAsync(Guid id, [FromBody] CreateStorefrontPaymentIntentRequest? request, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem("Id must not be empty.");
        }

        var dto = await _getMyInvoiceDetailHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem("Invoice not found.");
        }

        if (!dto.OrderId.HasValue)
        {
            return BadRequestProblem("Invoice is not linked to an order and cannot open a storefront payment flow.");
        }

        if (!CanRetryPayment(dto))
        {
            return BadRequestProblem("Invoice cannot accept a new payment attempt.");
        }

        try
        {
            var result = await _createStorefrontPaymentIntentHandler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = dto.OrderId.Value,
                UserId = GetCurrentUserId(),
                OrderNumber = dto.OrderNumber,
                Provider = string.IsNullOrWhiteSpace(request?.Provider) ? "DarwinCheckout" : request.Provider.Trim()
            }, ct).ConfigureAwait(false);

            var returnUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.OrderId.Value, dto.OrderNumber, cancelled: false);
            var cancelUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.OrderId.Value, dto.OrderNumber, cancelled: true);
            var checkoutUrl = _checkoutUrlBuilder.BuildGatewayUrl(result, returnUrl, cancelUrl);

            return Ok(new CreateStorefrontPaymentIntentResponse
            {
                OrderId = result.OrderId,
                PaymentId = result.PaymentId,
                Provider = result.Provider,
                ProviderReference = result.ProviderReference,
                AmountMinor = result.AmountMinor,
                Currency = result.Currency,
                Status = result.Status.ToString(),
                CheckoutUrl = checkoutUrl,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                ExpiresAtUtc = result.ExpiresAtUtc
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestProblem("Payment intent could not be created.", ex.Message);
        }
    }

    /// <summary>
    /// Downloads a member-friendly plain-text document for an owned invoice.
    /// </summary>
    [HttpGet("{id:guid}/document")]
    [HttpGet("/api/v1/invoices/{id:guid}/document")]
    [Produces("text/plain")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocumentAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem("Id must not be empty.");
        }

        var dto = await _getMyInvoiceDetailHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem("Invoice not found.");
        }

        var fileName = $"invoice-{SanitizeFileToken(dto.OrderNumber ?? dto.Id.ToString("D"))}.txt";
        var bytes = Encoding.UTF8.GetBytes(RenderInvoiceDocument(dto));
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    private static MemberInvoiceSummary MapSummary(MemberInvoiceSummaryDto dto)
        => new()
        {
            Id = dto.Id,
            BusinessId = dto.BusinessId,
            BusinessName = dto.BusinessName,
            OrderId = dto.OrderId,
            OrderNumber = dto.OrderNumber,
            Currency = dto.Currency,
            TotalGrossMinor = dto.TotalGrossMinor,
            RefundedAmountMinor = dto.RefundedAmountMinor,
            SettledAmountMinor = dto.SettledAmountMinor,
            BalanceMinor = dto.BalanceMinor,
            Status = dto.Status.ToString(),
            DueDateUtc = dto.DueDateUtc,
            PaidAtUtc = dto.PaidAtUtc,
            CreatedAtUtc = dto.CreatedAtUtc
        };

    private static MemberInvoiceDetail MapDetail(MemberInvoiceDetailDto dto)
        => new()
        {
            Id = dto.Id,
            BusinessId = dto.BusinessId,
            BusinessName = dto.BusinessName,
            OrderId = dto.OrderId,
            OrderNumber = dto.OrderNumber,
            Currency = dto.Currency,
            TotalGrossMinor = dto.TotalGrossMinor,
            RefundedAmountMinor = dto.RefundedAmountMinor,
            SettledAmountMinor = dto.SettledAmountMinor,
            BalanceMinor = dto.BalanceMinor,
            Status = dto.Status.ToString(),
            DueDateUtc = dto.DueDateUtc,
            PaidAtUtc = dto.PaidAtUtc,
            CreatedAtUtc = dto.CreatedAtUtc,
            TotalNetMinor = dto.TotalNetMinor,
            TotalTaxMinor = dto.TotalTaxMinor,
            PaymentSummary = dto.PaymentSummary,
            Lines = dto.Lines.Select(line => new MemberInvoiceLine
            {
                Id = line.Id,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPriceNetMinor = line.UnitPriceNetMinor,
                TaxRate = line.TaxRate,
                TotalNetMinor = line.TotalNetMinor,
                TotalGrossMinor = line.TotalGrossMinor
            }).ToList(),
            Actions = BuildActions(dto)
        };

    private static MemberInvoiceActions BuildActions(MemberInvoiceDetailDto dto)
    {
        var canRetryPayment = CanRetryPayment(dto);
        return new MemberInvoiceActions
        {
            CanRetryPayment = canRetryPayment,
            PaymentIntentPath = canRetryPayment ? GetPaymentIntentPath(dto.Id) : null,
            OrderPath = dto.OrderId.HasValue ? GetOrderPath(dto.OrderId.Value) : null,
            DocumentPath = GetDocumentPath(dto.Id)
        };
    }

    private static bool CanRetryPayment(MemberInvoiceDetailDto dto)
        => dto.OrderId.HasValue &&
           dto.Status is not InvoiceStatus.Cancelled &&
           dto.BalanceMinor > 0;

    private static string GetPaymentIntentPath(Guid id) => $"/api/v1/member/invoices/{id:D}/payment-intent";

    private static string GetOrderPath(Guid id) => $"/api/v1/member/orders/{id:D}";

    private static string GetDocumentPath(Guid id) => $"/api/v1/member/invoices/{id:D}/document";

    private static string SanitizeFileToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "invoice";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim().Select(ch => invalidChars.Contains(ch) ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "invoice" : sanitized;
    }

    private static string RenderInvoiceDocument(MemberInvoiceDetailDto dto)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"InvoiceId: {dto.Id:D}");
        builder.AppendLine($"Status: {dto.Status}");
        builder.AppendLine($"CreatedAtUtc: {dto.CreatedAtUtc:O}");
        builder.AppendLine($"BusinessId: {(dto.BusinessId.HasValue ? dto.BusinessId.Value.ToString("D") : "N/A")}");
        builder.AppendLine($"BusinessName: {dto.BusinessName ?? "N/A"}");
        builder.AppendLine($"OrderId: {(dto.OrderId.HasValue ? dto.OrderId.Value.ToString("D") : "N/A")}");
        builder.AppendLine($"OrderNumber: {dto.OrderNumber ?? "N/A"}");
        builder.AppendLine($"Currency: {dto.Currency}");
        builder.AppendLine($"TotalNetMinor: {dto.TotalNetMinor}");
        builder.AppendLine($"TotalTaxMinor: {dto.TotalTaxMinor}");
        builder.AppendLine($"TotalGrossMinor: {dto.TotalGrossMinor}");
        builder.AppendLine($"SettledAmountMinor: {dto.SettledAmountMinor}");
        builder.AppendLine($"RefundedAmountMinor: {dto.RefundedAmountMinor}");
        builder.AppendLine($"BalanceMinor: {dto.BalanceMinor}");
        builder.AppendLine($"DueDateUtc: {dto.DueDateUtc:O}");
        builder.AppendLine($"PaidAtUtc: {dto.PaidAtUtc:O}");
        builder.AppendLine($"PaymentSummary: {dto.PaymentSummary}");
        builder.AppendLine();
        builder.AppendLine("Lines:");
        foreach (var line in dto.Lines)
        {
            builder.AppendLine($"- {line.Description} | Qty: {line.Quantity} | UnitNetMinor: {line.UnitPriceNetMinor} | TaxRate: {line.TaxRate} | TotalNetMinor: {line.TotalNetMinor} | TotalGrossMinor: {line.TotalGrossMinor}");
        }

        return builder.ToString();
    }
}
