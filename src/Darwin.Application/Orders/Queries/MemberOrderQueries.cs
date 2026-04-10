using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries;

/// <summary>
/// Returns a paged list of orders owned by the current authenticated member.
/// </summary>
public sealed class GetMyOrdersPageHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyOrdersPageHandler"/> class.
    /// </summary>
    public GetMyOrdersPageHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// Returns the current member order history page.
    /// </summary>
    public async Task<(List<MemberOrderSummaryDto> Items, int Total)> HandleAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var userId = _currentUser.GetCurrentUserId();
        var baseQuery = _db.Set<Order>()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var total = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MemberOrderSummaryDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                Currency = x.Currency,
                GrandTotalGrossMinor = x.GrandTotalGrossMinor,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}

/// <summary>
/// Returns a detailed order view owned by the current authenticated member.
/// </summary>
public sealed class GetMyOrderForViewHandler
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyOrderForViewHandler"/> class.
    /// </summary>
    public GetMyOrderForViewHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>
    /// Returns the member-owned order detail or <c>null</c> when the order is not accessible.
    /// </summary>
    public async Task<MemberOrderDetailDto?> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var userId = _currentUser.GetCurrentUserId();

        var order = await _db.Set<Order>()
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(x => new MemberOrderDetailDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                Currency = x.Currency,
                PricesIncludeTax = x.PricesIncludeTax,
                SubtotalNetMinor = x.SubtotalNetMinor,
                TaxTotalMinor = x.TaxTotalMinor,
                ShippingTotalMinor = x.ShippingTotalMinor,
                ShippingMethodId = x.ShippingMethodId,
                ShippingMethodName = x.ShippingMethodName,
                ShippingCarrier = x.ShippingCarrier,
                ShippingService = x.ShippingService,
                DiscountTotalMinor = x.DiscountTotalMinor,
                GrandTotalGrossMinor = x.GrandTotalGrossMinor,
                Status = x.Status,
                BillingAddressJson = x.BillingAddressJson,
                ShippingAddressJson = x.ShippingAddressJson,
                CreatedAtUtc = x.CreatedAtUtc,
                Lines = x.Lines.Select(line => new MemberOrderLineDto
                {
                    Id = line.Id,
                    VariantId = line.VariantId,
                    Name = line.Name,
                    Sku = line.Sku,
                    Quantity = line.Quantity,
                    UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                    LineGrossMinor = line.LineGrossMinor
                }).ToList(),
                Payments = x.Payments
                    .OrderByDescending(payment => payment.CreatedAtUtc)
                    .Select(payment => new MemberOrderPaymentDto
                {
                    Id = payment.Id,
                    CreatedAtUtc = payment.CreatedAtUtc,
                    Provider = payment.Provider,
                    ProviderReference = payment.ProviderTransactionRef,
                    AmountMinor = payment.AmountMinor,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    PaidAtUtc = payment.PaidAtUtc
                }).ToList(),
                Shipments = x.Shipments.Select(shipment => new MemberOrderShipmentDto
                {
                    Id = shipment.Id,
                    Carrier = shipment.Carrier,
                    Service = shipment.Service,
                    TrackingNumber = shipment.TrackingNumber,
                    Status = shipment.Status,
                    ShippedAtUtc = shipment.ShippedAtUtc,
                    DeliveredAtUtc = shipment.DeliveredAtUtc
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (order is null)
        {
            return null;
        }

        order.Invoices = await _db.Set<Invoice>()
            .AsNoTracking()
            .Where(x => x.OrderId == order.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new MemberOrderInvoiceDto
            {
                Id = x.Id,
                Currency = x.Currency,
                TotalGrossMinor = x.TotalGrossMinor,
                Status = x.Status,
                DueDateUtc = x.DueDateUtc,
                PaidAtUtc = x.PaidAtUtc
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return order;
    }
}
