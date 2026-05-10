using System.Text;
using Darwin.Application;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Domain.Enums;
using Darwin.WebApi.Controllers.Businesses;
using Darwin.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

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
    private readonly GetInvoiceArchiveDocumentHandler _getInvoiceArchiveDocumentHandler;
    private readonly GetInvoiceStructuredDataExportHandler _getInvoiceStructuredDataExportHandler;
    private readonly GetInvoiceStructuredXmlExportHandler _getInvoiceStructuredXmlExportHandler;
    private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;
    private readonly StorefrontCheckoutUrlBuilder _checkoutUrlBuilder;
    private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberInvoicesController"/> class.
    /// </summary>
    public MemberInvoicesController(
        GetMyInvoicesPageHandler getMyInvoicesPageHandler,
        GetMyInvoiceDetailHandler getMyInvoiceDetailHandler,
        GetInvoiceArchiveDocumentHandler getInvoiceArchiveDocumentHandler,
        GetInvoiceStructuredDataExportHandler getInvoiceStructuredDataExportHandler,
        GetInvoiceStructuredXmlExportHandler getInvoiceStructuredXmlExportHandler,
        CreateStorefrontPaymentIntentHandler createStorefrontPaymentIntentHandler,
        StorefrontCheckoutUrlBuilder checkoutUrlBuilder,
        IStringLocalizer<ValidationResource> validationLocalizer)
    {
        _getMyInvoicesPageHandler = getMyInvoicesPageHandler ?? throw new ArgumentNullException(nameof(getMyInvoicesPageHandler));
        _getMyInvoiceDetailHandler = getMyInvoiceDetailHandler ?? throw new ArgumentNullException(nameof(getMyInvoiceDetailHandler));
        _getInvoiceArchiveDocumentHandler = getInvoiceArchiveDocumentHandler ?? throw new ArgumentNullException(nameof(getInvoiceArchiveDocumentHandler));
        _getInvoiceStructuredDataExportHandler = getInvoiceStructuredDataExportHandler ?? throw new ArgumentNullException(nameof(getInvoiceStructuredDataExportHandler));
        _getInvoiceStructuredXmlExportHandler = getInvoiceStructuredXmlExportHandler ?? throw new ArgumentNullException(nameof(getInvoiceStructuredXmlExportHandler));
        _createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));
        _checkoutUrlBuilder = checkoutUrlBuilder ?? throw new ArgumentNullException(nameof(checkoutUrlBuilder));
        _validationLocalizer = validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
    }

    /// <summary>
    /// Returns a paged invoice history for the current authenticated member.
    /// </summary>
    [HttpGet]
    [HttpGet("/api/v1/invoices")]
    [ProducesResponseType(typeof(PagedResponse<MemberInvoiceSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMyInvoicesAsync([FromQuery] int? page, [FromQuery] int? pageSize, [FromQuery] string? culture = null, CancellationToken ct = default)
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

        var normalizedCulture = BusinessControllerConventions.NormalizeNullable(culture);
        var (items, total) = await _getMyInvoicesPageHandler
            .HandleAsync(normalizedPage, normalizedPageSize, normalizedCulture, ct)
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
    public async Task<IActionResult> GetMyInvoiceAsync(Guid id, [FromQuery] string? culture = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var normalizedCulture = BusinessControllerConventions.NormalizeNullable(culture);
        var dto = await _getMyInvoiceDetailHandler.HandleAsync(id, normalizedCulture, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["InvoiceNotFound"]);
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
    public async Task<IActionResult> CreatePaymentIntentAsync(Guid id, [FromQuery] string? culture = null, [FromBody] CreateStorefrontPaymentIntentRequest? request = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);
        }

        var normalizedCulture = BusinessControllerConventions.NormalizeNullable(culture);
        var dto = await _getMyInvoiceDetailHandler.HandleAsync(id, normalizedCulture, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["InvoiceNotFound"]);
        }

        if (!dto.OrderId.HasValue)
        {
            return BadRequestProblem(_validationLocalizer["InvoiceNotLinkedToOrderPaymentFlow"]);
        }

        if (!CanRetryPayment(dto))
        {
            return BadRequestProblem(_validationLocalizer["InvoiceCannotAcceptNewPaymentAttempt"]);
        }

        try
        {
            var returnUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.OrderId.Value, dto.OrderNumber, cancelled: false);
            var cancelUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.OrderId.Value, dto.OrderNumber, cancelled: true);
            var result = await _createStorefrontPaymentIntentHandler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = dto.OrderId.Value,
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
    /// Downloads a member-friendly plain-text document for an owned invoice.
    /// </summary>
    [HttpGet("{id:guid}/document")]
    [HttpGet("/api/v1/invoices/{id:guid}/document")]
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
        var dto = await _getMyInvoiceDetailHandler.HandleAsync(id, normalizedCulture, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem(_validationLocalizer["InvoiceNotFound"]);
        }

        var fileName = $"invoice-{SanitizeFileToken(dto.OrderNumber ?? dto.Id.ToString("D"))}.txt";
        var bytes = Encoding.UTF8.GetBytes(RenderInvoiceDocument(dto));
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    /// <summary>
    /// Downloads the printable issued-invoice archive document for an owned invoice.
    /// </summary>
    [HttpGet("{id:guid}/archive-document")]
    [HttpGet("/api/v1/invoices/{id:guid}/archive-document")]
    [Produces("text/html")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadArchiveDocumentAsync(Guid id, [FromQuery] string? culture = null, CancellationToken ct = default)
    {
        var ownedInvoice = await GetOwnedInvoiceOrNullAsync(id, culture, ct).ConfigureAwait(false);
        if (ownedInvoice is null)
        {
            return id == Guid.Empty
                ? BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"])
                : NotFoundProblem(_validationLocalizer["InvoiceNotFound"]);
        }

        var document = await _getInvoiceArchiveDocumentHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (document is null)
        {
            return NotFoundProblem(_validationLocalizer["InvoiceArchiveUnavailable"]);
        }

        return File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(document.Html)).ToArray(),
            "text/html; charset=utf-8",
            document.FileName);
    }

    /// <summary>
    /// Downloads the structured source-model JSON export for an owned issued invoice.
    /// </summary>
    [HttpGet("{id:guid}/structured-data")]
    [HttpGet("/api/v1/invoices/{id:guid}/structured-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadStructuredDataAsync(Guid id, [FromQuery] string? culture = null, CancellationToken ct = default)
    {
        var ownedInvoice = await GetOwnedInvoiceOrNullAsync(id, culture, ct).ConfigureAwait(false);
        if (ownedInvoice is null)
        {
            return id == Guid.Empty
                ? BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"])
                : NotFoundProblem(_validationLocalizer["InvoiceNotFound"]);
        }

        var export = await _getInvoiceStructuredDataExportHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (export is null)
        {
            return NotFoundProblem(_validationLocalizer["InvoiceArchiveUnavailable"]);
        }

        return File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(export.Json)).ToArray(),
            "application/json",
            export.FileName);
    }

    /// <summary>
    /// Downloads the structured source-model XML export for an owned issued invoice.
    /// </summary>
    [HttpGet("{id:guid}/structured-xml")]
    [HttpGet("/api/v1/invoices/{id:guid}/structured-xml")]
    [Produces("application/xml")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadStructuredXmlAsync(Guid id, [FromQuery] string? culture = null, CancellationToken ct = default)
    {
        var ownedInvoice = await GetOwnedInvoiceOrNullAsync(id, culture, ct).ConfigureAwait(false);
        if (ownedInvoice is null)
        {
            return id == Guid.Empty
                ? BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"])
                : NotFoundProblem(_validationLocalizer["InvoiceNotFound"]);
        }

        var export = await _getInvoiceStructuredXmlExportHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (export is null)
        {
            return NotFoundProblem(_validationLocalizer["InvoiceArchiveUnavailable"]);
        }

        return File(
            Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(export.Xml)).ToArray(),
            "application/xml",
            export.FileName);
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
            DocumentPath = GetDocumentPath(dto.Id),
            ArchiveDocumentPath = GetArchiveDocumentPath(dto.Id),
            StructuredDataPath = GetStructuredDataPath(dto.Id),
            StructuredXmlPath = GetStructuredXmlPath(dto.Id)
        };
    }

    private async Task<MemberInvoiceDetailDto?> GetOwnedInvoiceOrNullAsync(Guid id, string? culture, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        var normalizedCulture = BusinessControllerConventions.NormalizeNullable(culture);
        return await _getMyInvoiceDetailHandler.HandleAsync(id, normalizedCulture, ct).ConfigureAwait(false);
    }

    private static bool CanRetryPayment(MemberInvoiceDetailDto dto)
        => dto.OrderId.HasValue &&
           dto.Status is not InvoiceStatus.Cancelled &&
           dto.BalanceMinor > 0;

    private static string GetPaymentIntentPath(Guid id) => $"/api/v1/member/invoices/{id:D}/payment-intent";

    private static string GetOrderPath(Guid id) => $"/api/v1/member/orders/{id:D}";

    private static string GetDocumentPath(Guid id) => $"/api/v1/member/invoices/{id:D}/document";

    private static string GetArchiveDocumentPath(Guid id) => $"/api/v1/member/invoices/{id:D}/archive-document";

    private static string GetStructuredDataPath(Guid id) => $"/api/v1/member/invoices/{id:D}/structured-data";

    private static string GetStructuredXmlPath(Guid id) => $"/api/v1/member/invoices/{id:D}/structured-xml";

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
