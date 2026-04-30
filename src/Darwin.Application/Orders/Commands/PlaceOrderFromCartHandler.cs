using System.Globalization;
using System.Text.Json;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Application.Orders.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Orders.Commands;

/// <summary>
/// Creates an order snapshot from a cart, capturing address snapshots and authoritative financial totals.
/// </summary>
public sealed class PlaceOrderFromCartHandler
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;
    private readonly ComputeCartSummaryHandler _computeCartSummaryHandler;
    private readonly Darwin.Application.Orders.Queries.CreateStorefrontCheckoutIntentHandler _createStorefrontCheckoutIntentHandler;
    private readonly IStringLocalizer<ValidationResource> _localizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceOrderFromCartHandler"/> class.
    /// </summary>
    public PlaceOrderFromCartHandler(
        IAppDbContext db,
        IClock clock,
        ComputeCartSummaryHandler computeCartSummaryHandler,
        Darwin.Application.Orders.Queries.CreateStorefrontCheckoutIntentHandler createStorefrontCheckoutIntentHandler,
        IStringLocalizer<ValidationResource> localizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _computeCartSummaryHandler = computeCartSummaryHandler ?? throw new ArgumentNullException(nameof(computeCartSummaryHandler));
        _createStorefrontCheckoutIntentHandler = createStorefrontCheckoutIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontCheckoutIntentHandler));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Creates an order from the specified cart and finalizes the cart to prevent duplicate checkout submissions.
    /// </summary>
    public async Task<PlaceOrderFromCartResultDto> HandleAsync(PlaceOrderFromCartDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.CartId == Guid.Empty)
        {
            throw new InvalidOperationException(_localizer["CartIdRequired"]);
        }

        if (dto.ShippingTotalMinor < 0)
        {
            throw new InvalidOperationException(_localizer["ShippingTotalMinorMustBeZeroOrPositive"]);
        }

        var cart = await _db.Set<Cart>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == dto.CartId && !x.IsDeleted, ct)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(_localizer["CartNotFound"]);

        if (cart.UserId.HasValue && dto.UserId != cart.UserId)
        {
            throw new InvalidOperationException(_localizer["CartDoesNotBelongToCurrentUser"]);
        }

        var activeItems = cart.Items.Where(x => !x.IsDeleted && x.Quantity > 0).ToList();
        if (activeItems.Count == 0)
        {
            throw new InvalidOperationException(_localizer["CartIsEmpty"]);
        }

        var summary = await _computeCartSummaryHandler.HandleAsync(cart.Id, ct).ConfigureAwait(false);
        var summaryLinesByKey = summary.Items.ToDictionary(
            x => BuildSummaryKey(x.VariantId, x.SelectedAddOnValueIdsJson),
            x => x,
            StringComparer.Ordinal);

        var billingAddressJson = await ResolveAddressJsonAsync(dto.UserId, dto.BillingAddressId, dto.BillingAddress, "billing", ct).ConfigureAwait(false);

        var checkoutIntent = await _createStorefrontCheckoutIntentHandler.HandleAsync(new CreateStorefrontCheckoutIntentDto
        {
            CartId = dto.CartId,
            UserId = dto.UserId,
            ShippingAddressId = dto.ShippingAddressId,
            ShippingAddress = dto.ShippingAddress,
            SelectedShippingMethodId = dto.SelectedShippingMethodId
        }, ct).ConfigureAwait(false);

        if (checkoutIntent.RequiresShipping)
        {
            if (checkoutIntent.SelectedShippingMethodId is null)
            {
                throw new InvalidOperationException(_localizer["ShippingMethodRequiredForCurrentCheckout"]);
            }

            if (dto.ShippingTotalMinor != checkoutIntent.SelectedShippingTotalMinor)
            {
                throw new InvalidOperationException(_localizer["ShippingTotalDoesNotMatchAuthoritativeCheckoutIntent"]);
            }
        }
        else if (dto.ShippingTotalMinor != 0)
        {
            throw new InvalidOperationException(_localizer["ShippingTotalMustBeZeroWhenCheckoutDoesNotRequireShipping"]);
        }

        var selectedShippingOption = checkoutIntent.SelectedShippingMethodId.HasValue
            ? checkoutIntent.ShippingOptions.FirstOrDefault(x => x.MethodId == checkoutIntent.SelectedShippingMethodId.Value)
            : null;
        var shippingAddressJson = await ResolveAddressJsonAsync(dto.UserId, dto.ShippingAddressId, dto.ShippingAddress, "shipping", ct).ConfigureAwait(false);

        var variantIds = activeItems.Select(x => x.VariantId).Distinct().ToList();
        var variants = await _db.Set<ProductVariant>()
            .AsNoTracking()
            .Where(x => variantIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id, ct)
            .ConfigureAwait(false);

        var productIds = variants.Values.Select(x => x.ProductId).Distinct().ToList();
        var translations = await _db.Set<ProductTranslation>()
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && x.Culture == dto.Culture && !x.IsDeleted)
            .ToDictionaryAsync(x => x.ProductId, ct)
            .ConfigureAwait(false);

        long subtotalNetMinor = 0;
        long taxTotalMinor = 0;
        var orderLines = new List<OrderLine>(activeItems.Count);

        foreach (var cartItem in activeItems)
        {
            if (!variants.TryGetValue(cartItem.VariantId, out var variant))
            {
                throw new InvalidOperationException(_localizer["VariantNotFound"]);
            }

            if (!summaryLinesByKey.TryGetValue(BuildSummaryKey(cartItem.VariantId, cartItem.SelectedAddOnValueIdsJson), out var summaryLine))
            {
                throw new InvalidOperationException(_localizer["CartSummaryMissingRequiredLineForCheckout"]);
            }

            var name = translations.TryGetValue(variant.ProductId, out var translation)
                ? translation.Name
                : variant.Sku;

            var unitNetWithAddOns = summaryLine.UnitPriceNetMinor;
            var lineNet = summaryLine.LineNetMinor;
            var lineTax = summaryLine.LineVatMinor;
            var unitGross = unitNetWithAddOns + (long)Math.Round(unitNetWithAddOns * (double)summaryLine.VatRate, MidpointRounding.AwayFromZero);
            var lineGross = summaryLine.LineGrossMinor;

            subtotalNetMinor += lineNet;
            taxTotalMinor += lineTax;

            orderLines.Add(new OrderLine
            {
                VariantId = cartItem.VariantId,
                WarehouseId = null,
                Name = name,
                Sku = variant.Sku,
                Quantity = summaryLine.Quantity,
                UnitPriceNetMinor = unitNetWithAddOns,
                VatRate = summaryLine.VatRate,
                UnitPriceGrossMinor = unitGross,
                LineTaxMinor = lineTax,
                LineGrossMinor = lineGross,
                AddOnValueIdsJson = summaryLine.SelectedAddOnValueIdsJson,
                AddOnPriceDeltaMinor = summaryLine.AddOnPriceDeltaMinor
            });
        }

        var rawGrossMinor = subtotalNetMinor + taxTotalMinor;
        var discountTotalMinor = Math.Max(0L, rawGrossMinor - summary.GrandTotalGrossMinor);
        var grandTotalGrossMinor = rawGrossMinor + dto.ShippingTotalMinor - discountTotalMinor;

        var order = new Order
        {
            OrderNumber = await NextOrderNumberAsync(ct).ConfigureAwait(false),
            UserId = dto.UserId ?? cart.UserId,
            Currency = summary.Currency,
            PricesIncludeTax = false,
            SubtotalNetMinor = subtotalNetMinor,
            TaxTotalMinor = taxTotalMinor,
            ShippingTotalMinor = dto.ShippingTotalMinor,
            DiscountTotalMinor = discountTotalMinor,
            GrandTotalGrossMinor = Math.Max(0L, grandTotalGrossMinor),
            ShippingMethodId = checkoutIntent.SelectedShippingMethodId,
            ShippingMethodName = selectedShippingOption?.Name,
            ShippingCarrier = selectedShippingOption?.Carrier,
            ShippingService = selectedShippingOption?.Service,
            Status = OrderStatus.Created,
            BillingAddressJson = billingAddressJson,
            ShippingAddressJson = shippingAddressJson,
            Lines = orderLines
        };

        _db.Set<Order>().Add(order);

        cart.IsDeleted = true;
        foreach (var cartItem in activeItems)
        {
            cartItem.IsDeleted = true;
        }

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(_localizer["CartAlreadyCheckedOut"]);
        }

        return new PlaceOrderFromCartResultDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Currency = order.Currency,
            GrandTotalGrossMinor = order.GrandTotalGrossMinor,
            Status = order.Status
        };
    }

    private async Task<string> ResolveAddressJsonAsync(
        Guid? userId,
        Guid? addressId,
        CheckoutAddressDto? inlineAddress,
        string role,
        CancellationToken ct)
    {
        if (addressId.HasValue)
        {
            if (!userId.HasValue || userId == Guid.Empty)
            {
                throw new InvalidOperationException(_localizer["SignedInUserRequiredToUseSavedAddress", role]);
            }

            var address = await _db.Set<Address>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == addressId.Value && x.UserId == userId.Value && !x.IsDeleted, ct)
                .ConfigureAwait(false);

            if (address is null)
            {
                throw new InvalidOperationException(_localizer["SavedAddressNotFound", role]);
            }

            return JsonSerializer.Serialize(new CheckoutAddressDto
            {
                FullName = address.FullName,
                Company = address.Company,
                Street1 = address.Street1,
                Street2 = address.Street2,
                PostalCode = address.PostalCode,
                City = address.City,
                State = address.State,
                CountryCode = address.CountryCode,
                PhoneE164 = address.PhoneE164
            });
        }

        if (inlineAddress is null)
        {
            throw new InvalidOperationException(_localizer["AddressRequired", role]);
        }

        ValidateInlineAddress(inlineAddress, role);
        return JsonSerializer.Serialize(inlineAddress);
    }

    private void ValidateInlineAddress(CheckoutAddressDto address, string role)
    {
        if (string.IsNullOrWhiteSpace(address.FullName) ||
            string.IsNullOrWhiteSpace(address.Street1) ||
            string.IsNullOrWhiteSpace(address.PostalCode) ||
            string.IsNullOrWhiteSpace(address.City) ||
            string.IsNullOrWhiteSpace(address.CountryCode))
        {
            throw new InvalidOperationException(_localizer["AddressIncomplete", role]);
        }
    }

    private async Task<string> NextOrderNumberAsync(CancellationToken ct)
    {
        var nowUtc = _clock.UtcNow;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..6].ToUpperInvariant();
            var candidate = $"D-{nowUtc:yyyyMMdd-HHmmssfff}-{suffix}";
            var exists = await _db.Set<Order>()
                .AsNoTracking()
                .AnyAsync(x => !x.IsDeleted && x.OrderNumber == candidate, ct)
                .ConfigureAwait(false);

            if (!exists)
            {
                return candidate;
            }
        }

        return $"D-{nowUtc:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}"[..50].ToUpperInvariant();
    }

    private static string BuildSummaryKey(Guid variantId, string? selectedAddOnValueIdsJson)
    {
        return string.Concat(
            variantId.ToString("N", CultureInfo.InvariantCulture),
            "|",
            selectedAddOnValueIdsJson ?? "[]");
    }
}
