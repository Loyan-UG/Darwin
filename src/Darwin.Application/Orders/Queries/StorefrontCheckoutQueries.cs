using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Shipping.DTOs;
using Darwin.Application.Shipping.Queries;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Orders.Queries;

/// <summary>
/// Builds a storefront checkout-intent preview from the authoritative cart and shipping model.
/// </summary>
public sealed class CreateStorefrontCheckoutIntentHandler
{
    private readonly IAppDbContext _db;
    private readonly ComputeCartSummaryHandler _computeCartSummaryHandler;
    private readonly RateShipmentHandler _rateShipmentHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateStorefrontCheckoutIntentHandler"/> class.
    /// </summary>
    public CreateStorefrontCheckoutIntentHandler(
        IAppDbContext db,
        ComputeCartSummaryHandler computeCartSummaryHandler,
        RateShipmentHandler rateShipmentHandler)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _computeCartSummaryHandler = computeCartSummaryHandler ?? throw new ArgumentNullException(nameof(computeCartSummaryHandler));
        _rateShipmentHandler = rateShipmentHandler ?? throw new ArgumentNullException(nameof(rateShipmentHandler));
    }

    /// <summary>
    /// Returns the storefront checkout preview for the specified cart.
    /// </summary>
    public async Task<StorefrontCheckoutIntentResultDto> HandleAsync(CreateStorefrontCheckoutIntentDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.CartId == Guid.Empty)
        {
            throw new InvalidOperationException("CartId is required.");
        }

        var cart = await _db.Set<Cart>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == dto.CartId && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cart not found.");

        if (cart.UserId.HasValue && cart.UserId != dto.UserId)
        {
            throw new InvalidOperationException("Cart does not belong to the current user.");
        }

        var activeItems = cart.Items.Where(x => !x.IsDeleted && x.Quantity > 0).ToList();
        if (activeItems.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty.");
        }

        var summary = await _computeCartSummaryHandler.HandleAsync(dto.CartId, ct).ConfigureAwait(false);

        var variantIds = activeItems.Select(x => x.VariantId).Distinct().ToList();
        var variants = await _db.Set<ProductVariant>()
            .AsNoTracking()
            .Where(x => variantIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.PackageWeight,
                x.IsDigital
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (variants.Count != variantIds.Count)
        {
            throw new InvalidOperationException("One or more cart variants are no longer available.");
        }

        var variantById = variants.ToDictionary(x => x.Id);
        var requiresShipping = activeItems.Any(x => !variantById[x.VariantId].IsDigital);
        var shipmentMass = activeItems
            .Where(x => !variantById[x.VariantId].IsDigital)
            .Sum(x => (variantById[x.VariantId].PackageWeight ?? 0) * x.Quantity);

        var result = new StorefrontCheckoutIntentResultDto
        {
            CartId = summary.CartId,
            Currency = summary.Currency,
            SubtotalNetMinor = summary.SubtotalNetMinor,
            VatTotalMinor = summary.VatTotalMinor,
            GrandTotalGrossMinor = summary.GrandTotalGrossMinor,
            ShipmentMass = shipmentMass,
            RequiresShipping = requiresShipping
        };

        if (!requiresShipping)
        {
            return result;
        }

        var shippingCountryCode = await ResolveShippingCountryCodeAsync(dto.UserId, dto.ShippingAddressId, dto.ShippingAddress, ct).ConfigureAwait(false);
        result.ShippingCountryCode = shippingCountryCode;

        var shippingOptions = await _rateShipmentHandler.HandleAsync(new RateShipmentInputDto
        {
            Country = shippingCountryCode,
            SubtotalNetMinor = summary.SubtotalNetMinor,
            ShipmentMass = shipmentMass,
            Currency = summary.Currency
        }, summary.Currency, ct).ConfigureAwait(false);

        result.ShippingOptions = shippingOptions.Select(MapOption).ToList();
        if (result.ShippingOptions.Count == 0)
        {
            throw new InvalidOperationException("No shipping options are available for the current checkout.");
        }

        var selected = dto.SelectedShippingMethodId.HasValue
            ? result.ShippingOptions.FirstOrDefault(x => x.MethodId == dto.SelectedShippingMethodId.Value)
            : result.ShippingOptions.OrderBy(x => x.PriceMinor).FirstOrDefault();

        if (dto.SelectedShippingMethodId.HasValue && selected is null)
        {
            throw new InvalidOperationException("Selected shipping method is not valid for the current checkout.");
        }

        selected ??= result.ShippingOptions.OrderBy(x => x.PriceMinor).First();
        result.SelectedShippingMethodId = selected.MethodId;
        result.SelectedShippingTotalMinor = selected.PriceMinor;

        return result;
    }

    private async Task<string> ResolveShippingCountryCodeAsync(
        Guid? userId,
        Guid? shippingAddressId,
        CheckoutAddressDto? shippingAddress,
        CancellationToken ct)
    {
        if (shippingAddressId.HasValue)
        {
            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("A signed-in user is required to use a saved shipping address.");
            }

            var address = await _db.Set<Address>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == shippingAddressId.Value && x.UserId == userId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (address is null)
            {
                throw new InvalidOperationException("Saved shipping address not found.");
            }

            return address.CountryCode;
        }

        if (shippingAddress is null || string.IsNullOrWhiteSpace(shippingAddress.CountryCode))
        {
            throw new InvalidOperationException("A shipping address with a country code is required.");
        }

        return shippingAddress.CountryCode.Trim().ToUpperInvariant();
    }

    private static StorefrontShippingOptionDto MapOption(ShippingOptionDto dto)
        => new()
        {
            MethodId = dto.MethodId,
            Name = dto.Name,
            PriceMinor = dto.PriceMinor,
            Currency = dto.Currency,
            Carrier = dto.Carrier,
            Service = dto.Service
        };
}

/// <summary>
/// Returns a storefront order confirmation view while enforcing ownership rules for member and anonymous orders.
/// </summary>
public sealed class GetStorefrontOrderConfirmationHandler
{
    private readonly IAppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetStorefrontOrderConfirmationHandler"/> class.
    /// </summary>
    public GetStorefrontOrderConfirmationHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Returns the storefront confirmation projection or <c>null</c> when the order is not accessible.
    /// </summary>
    public async Task<StorefrontOrderConfirmationDto?> HandleAsync(GetStorefrontOrderConfirmationDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException("OrderId is required.");
        }

        var order = await _db.Set<Order>()
            .AsNoTracking()
            .Where(x => x.Id == dto.OrderId)
            .Select(x => new
            {
                x.Id,
                x.OrderNumber,
                x.UserId,
                x.Currency,
                x.SubtotalNetMinor,
                x.TaxTotalMinor,
                x.ShippingTotalMinor,
                x.ShippingMethodId,
                x.ShippingMethodName,
                x.ShippingCarrier,
                x.ShippingService,
                x.DiscountTotalMinor,
                x.GrandTotalGrossMinor,
                x.Status,
                x.BillingAddressJson,
                x.ShippingAddressJson,
                x.CreatedAtUtc,
                Lines = x.Lines.Select(line => new StorefrontOrderConfirmationLineDto
                {
                    Id = line.Id,
                    VariantId = line.VariantId,
                    Name = line.Name,
                    Sku = line.Sku,
                    Quantity = line.Quantity,
                    UnitPriceGrossMinor = line.UnitPriceGrossMinor,
                    LineGrossMinor = line.LineGrossMinor
                }).ToList(),
                Payments = x.Payments.Select(payment => new StorefrontOrderConfirmationPaymentDto
                {
                    Id = payment.Id,
                    Provider = payment.Provider,
                    ProviderReference = payment.ProviderTransactionRef,
                    AmountMinor = payment.AmountMinor,
                    Currency = payment.Currency,
                    Status = payment.Status,
                    PaidAtUtc = payment.PaidAtUtc
                }).ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (order is null || !CanAccessOrder(order.UserId, order.OrderNumber, dto.UserId, dto.OrderNumber))
        {
            return null;
        }

        return new StorefrontOrderConfirmationDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Currency = order.Currency,
            SubtotalNetMinor = order.SubtotalNetMinor,
            TaxTotalMinor = order.TaxTotalMinor,
            ShippingTotalMinor = order.ShippingTotalMinor,
            ShippingMethodId = order.ShippingMethodId,
            ShippingMethodName = order.ShippingMethodName,
            ShippingCarrier = order.ShippingCarrier,
            ShippingService = order.ShippingService,
            DiscountTotalMinor = order.DiscountTotalMinor,
            GrandTotalGrossMinor = order.GrandTotalGrossMinor,
            Status = order.Status,
            BillingAddressJson = order.BillingAddressJson,
            ShippingAddressJson = order.ShippingAddressJson,
            CreatedAtUtc = order.CreatedAtUtc,
            Lines = order.Lines,
            Payments = order.Payments
        };
    }

    internal static bool CanAccessOrder(Guid? orderUserId, string orderNumber, Guid? currentUserId, string? suppliedOrderNumber)
    {
        if (orderUserId.HasValue)
        {
            return currentUserId.HasValue && currentUserId.Value == orderUserId.Value;
        }

        return !string.IsNullOrWhiteSpace(suppliedOrderNumber) &&
               string.Equals(orderNumber, suppliedOrderNumber.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
