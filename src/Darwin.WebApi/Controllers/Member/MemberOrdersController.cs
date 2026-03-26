using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Contracts.Common;
using Darwin.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberOrdersController"/> class.
    /// </summary>
    public MemberOrdersController(
        GetMyOrdersPageHandler getMyOrdersPageHandler,
        GetMyOrderForViewHandler getMyOrderForViewHandler)
    {
        _getMyOrdersPageHandler = getMyOrdersPageHandler ?? throw new ArgumentNullException(nameof(getMyOrdersPageHandler));
        _getMyOrderForViewHandler = getMyOrderForViewHandler ?? throw new ArgumentNullException(nameof(getMyOrderForViewHandler));
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
            return BadRequestProblem("Page must be a positive integer.");
        }

        var normalizedPageSize = pageSize.GetValueOrDefault(20);
        if (normalizedPageSize <= 0 || normalizedPageSize > 200)
        {
            return BadRequestProblem("PageSize must be between 1 and 200.");
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
    public async Task<IActionResult> GetMyOrderAsync(Guid id, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return BadRequestProblem("Id must not be empty.");
        }

        var dto = await _getMyOrderForViewHandler.HandleAsync(id, ct).ConfigureAwait(false);
        if (dto is null)
        {
            return NotFoundProblem("Order not found.");
        }

        return Ok(MapDetail(dto));
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
                Provider = payment.Provider,
                ProviderReference = payment.ProviderReference,
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
            }).ToList()
        };
}
