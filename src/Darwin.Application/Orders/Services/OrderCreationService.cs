using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Foundation;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Services;

public sealed class OrderCreationService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly NumberSequenceService? _numberSequenceService;

    public OrderCreationService(IAppDbContext db, IClock clock, NumberSequenceService? numberSequenceService = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _numberSequenceService = numberSequenceService;
    }

    public async Task<Order> CreateAsync(OrderCreationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Order requires at least one line.");
        }

        var currency = NormalizeCurrency(request.Currency);
        var subtotalNet = request.Lines.Sum(x => x.TotalNetMinor);
        var taxTotal = request.Lines.Sum(x => x.LineTaxMinor);
        var shippingTotal = Math.Max(0, request.ShippingTotalMinor);
        var discountTotal = Math.Max(0, request.DiscountTotalMinor);
        var grandTotal = Math.Max(0, subtotalNet + taxTotal + shippingTotal - discountTotal);

        var order = new Order
        {
            OrderNumber = await NextOrderNumberAsync(request.FallbackMode, ct).ConfigureAwait(false),
            UserId = NormalizeGuid(request.UserId),
            BusinessId = NormalizeGuid(request.BusinessId),
            CustomerId = NormalizeGuid(request.CustomerId),
            Currency = currency,
            PricesIncludeTax = request.PricesIncludeTax,
            SalesChannel = request.SalesChannel,
            OrderedAtUtc = _clock.UtcNow,
            SubtotalNetMinor = subtotalNet,
            TaxTotalMinor = taxTotal,
            ShippingTotalMinor = shippingTotal,
            DiscountTotalMinor = discountTotal,
            GrandTotalGrossMinor = grandTotal,
            ShippingMethodId = NormalizeGuid(request.ShippingMethodId),
            ShippingMethodName = NormalizeOptional(request.ShippingMethodName),
            ShippingCarrier = NormalizeOptional(request.ShippingCarrier),
            ShippingService = NormalizeOptional(request.ShippingService),
            Status = OrderStatus.Created,
            BillingAddressJson = NormalizeJsonSnapshot(request.BillingAddressJson),
            ShippingAddressJson = NormalizeJsonSnapshot(request.ShippingAddressJson),
            InternalNotes = NormalizeOptional(request.InternalNotes),
            Lines = request.Lines
                .Select((line, index) => new OrderLine
                {
                    VariantId = NormalizeGuid(line.VariantId),
                    WarehouseId = NormalizeGuid(line.WarehouseId),
                    Name = Required(line.Name, "Order line name"),
                    Sku = NormalizeOptional(line.Sku) ?? string.Empty,
                    Quantity = line.Quantity,
                    UnitPriceNetMinor = line.UnitPriceNetMinor,
                    VatRate = line.VatRate,
                    UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                    LineTaxMinor = line.LineTaxMinor,
                    LineGrossMinor = line.LineGrossMinor,
                    AddOnValueIdsJson = NormalizeJsonArray(line.AddOnValueIdsJson),
                    AddOnPriceDeltaMinor = line.AddOnPriceDeltaMinor
                })
                .ToList()
        };

        _db.Set<Order>().Add(order);
        return order;
    }

    private async Task<string> NextOrderNumberAsync(OrderNumberFallbackMode fallbackMode, CancellationToken ct)
    {
        if (_numberSequenceService is not null)
        {
            var reserved = await _numberSequenceService.ReserveNextAsync(
                new NumberSequenceRequest(null, NumberSequenceDocumentType.Order, NumberSequenceService.GlobalScopeKey),
                ct)
                .ConfigureAwait(false);
            if (reserved.Succeeded && !string.IsNullOrWhiteSpace(reserved.Value))
            {
                return reserved.Value;
            }
        }

        var nowUtc = _clock.UtcNow;
        if (fallbackMode == OrderNumberFallbackMode.OpaqueSuffix)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
                var candidate = $"D-{nowUtc:yyyyMMdd}-{suffix}";
                var exists = await _db.Set<Order>()
                    .AsNoTracking()
                    .AnyAsync(x => !x.IsDeleted && x.OrderNumber == candidate, ct)
                    .ConfigureAwait(false);

                if (!exists)
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Could not reserve a fallback order number.");
        }

        var count = await _db.Set<Order>()
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted, ct)
            .ConfigureAwait(false);

        for (var attempt = 1; attempt <= 20; attempt++)
        {
            var candidate = $"D-{nowUtc:yyyyMMdd}-{count + attempt:00000}";
            var exists = await _db.Set<Order>()
                .AsNoTracking()
                .AnyAsync(x => !x.IsDeleted && x.OrderNumber == candidate, ct)
                .ConfigureAwait(false);

            if (!exists)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not reserve a fallback order number.");
    }

    private static string NormalizeCurrency(string value)
    {
        var normalized = Required(value, "Currency").ToUpperInvariant();
        if (normalized.Length != 3)
        {
            throw new InvalidOperationException("Currency must be a three-letter ISO code.");
        }

        return normalized;
    }

    private static string Required(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is required.");
        }

        return value.Trim();
    }

    private static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value.Value : null;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeJsonSnapshot(string? value)
        => string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();

    private static string NormalizeJsonArray(string? value)
        => string.IsNullOrWhiteSpace(value) ? "[]" : value.Trim();
}

public sealed class OrderCreationRequest
{
    public Guid? UserId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? CustomerId { get; init; }
    public string Currency { get; init; } = string.Empty;
    public bool PricesIncludeTax { get; init; }
    public SalesChannel SalesChannel { get; init; } = SalesChannel.Admin;
    public string BillingAddressJson { get; init; } = "{}";
    public string ShippingAddressJson { get; init; } = "{}";
    public Guid? ShippingMethodId { get; init; }
    public string? ShippingMethodName { get; init; }
    public string? ShippingCarrier { get; init; }
    public string? ShippingService { get; init; }
    public long ShippingTotalMinor { get; init; }
    public long DiscountTotalMinor { get; init; }
    public string? InternalNotes { get; init; }
    public OrderNumberFallbackMode FallbackMode { get; init; } = OrderNumberFallbackMode.Sequential;
    public IReadOnlyList<OrderCreationLineRequest> Lines { get; init; } = Array.Empty<OrderCreationLineRequest>();
}

public enum OrderNumberFallbackMode
{
    Sequential = 0,
    OpaqueSuffix = 1
}

public sealed class OrderCreationLineRequest
{
    public Guid? VariantId { get; init; }
    public Guid? WarehouseId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public long UnitPriceNetMinor { get; init; }
    public decimal VatRate { get; init; }
    public long UnitPriceGrossMinor { get; init; }
    public long LineTaxMinor { get; init; }
    public long LineGrossMinor { get; init; }
    public string AddOnValueIdsJson { get; init; } = "[]";
    public long AddOnPriceDeltaMinor { get; init; }
    public long TotalNetMinor => UnitPriceNetMinor * Quantity;
}
