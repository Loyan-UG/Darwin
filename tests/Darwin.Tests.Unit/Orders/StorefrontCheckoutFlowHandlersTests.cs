using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Payments;
using Darwin.Application;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Application.Shipping.Queries;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.Shipping;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Covers storefront checkout intent, payment-intent, and confirmation flow handlers.
/// </summary>
public sealed class StorefrontCheckoutFlowHandlersTests
{
    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_ReturnShippingOptions_AndSelectCheapest()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var dhlMethodId = Guid.NewGuid();
        var expressMethodId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SKU-4001",
            BasePriceNetMinor = 2000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId,
            PackageWeight = 750
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });

        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });

        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 2,
                    UnitPriceNetMinor = 2000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        db.Set<ShippingMethod>().AddRange(
            new ShippingMethod
            {
                Id = dhlMethodId,
                Name = "DHL Standard",
                Carrier = "DHL",
                Service = "Standard",
                IsActive = true,
                CountriesCsv = "DE",
                Currency = "EUR",
                Rates =
                [
                    new ShippingRate
                    {
                        Id = Guid.NewGuid(),
                        ShippingMethodId = dhlMethodId,
                        MaxShipmentMass = 5000,
                        PriceMinor = 590,
                        SortOrder = 1
                    }
                ]
            },
            new ShippingMethod
            {
                Id = expressMethodId,
                Name = "DHL Express",
                Carrier = "DHL",
                Service = "Express",
                IsActive = true,
                CountriesCsv = "DE",
                Currency = "EUR",
                Rates =
                [
                    new ShippingRate
                    {
                        Id = Guid.NewGuid(),
                        ShippingMethodId = expressMethodId,
                        MaxShipmentMass = 5000,
                        PriceMinor = 990,
                        SortOrder = 1
                    }
                ]
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var result = await handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
        {
            CartId = cartId,
            ShippingAddress = new CheckoutAddressDto
            {
                FullName = "Mia Wagner",
                Street1 = "Leipziger Platz 1",
                PostalCode = "10117",
                City = "Berlin",
                CountryCode = "DE"
            }
        }, TestContext.Current.CancellationToken);

        result.RequiresShipping.Should().BeTrue();
        result.ShipmentMass.Should().Be(1500);
        result.ShippingCountryCode.Should().Be("DE");
        result.ShippingOptions.Should().HaveCount(2);
        result.SelectedShippingMethodId.Should().Be(dhlMethodId);
        result.SelectedShippingTotalMinor.Should().Be(590);
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenCartIdMissing()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto(), TestContext.Current.CancellationToken));

        ex.Message.Should().Be("CartIdRequired");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenCartNotFound()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = Guid.NewGuid()
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("CartNotFound");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenCartBelongsToDifferentUser()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            UserId = ownerId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                UserId = Guid.NewGuid(),
                ShippingAddress = new CheckoutAddressDto
                {
                    FullName = "Mia",
                    Street1 = "X",
                    PostalCode = "00000",
                    City = "Berlin",
                    CountryCode = "DE"
                }
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("CartDoesNotBelongToCurrentUser");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenCartIsEmpty()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                ShippingAddress = new CheckoutAddressDto
                {
                    FullName = "Mia",
                    Street1 = "X",
                    PostalCode = "00000",
                    City = "Berlin",
                    CountryCode = "DE"
                }
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("CartIsEmpty");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenCartVariantsUnavailable()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var missingVariantId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = missingVariantId,
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                ShippingAddress = new CheckoutAddressDto
                {
                    FullName = "Mia",
                    Street1 = "X",
                    PostalCode = "00000",
                    City = "Berlin",
                    CountryCode = "DE"
                }
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("CartVariantsNoLongerAvailable");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_ReturnForDigitalOnlyItemsWithoutShipping()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "DIGI-100",
            BasePriceNetMinor = 500,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId,
            IsDigital = true,
            PackageWeight = 0
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Digital",
            VatRate = 0.19m
        });
        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 2,
                    UnitPriceNetMinor = 500,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var result = await handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
        {
            CartId = cartId
        }, TestContext.Current.CancellationToken);

        result.RequiresShipping.Should().BeFalse();
        result.ShippingCountryCode.Should().BeNull();
        result.ShippingOptions.Should().BeEmpty();
        result.SelectedShippingMethodId.Should().BeNull();
        result.SelectedShippingTotalMinor.Should().Be(0);
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenShippingCountryCodeMissing()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SHIP-100",
            BasePriceNetMinor = 1000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId,
            PackageWeight = 100
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });
        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                ShippingAddress = new CheckoutAddressDto
                {
                    FullName = "Mia",
                    Street1 = "X",
                    PostalCode = "10115",
                    City = "Berlin",
                    CountryCode = "   "
                }
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("ShippingAddressWithCountryCodeRequired");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenSavedAddressUsedWithoutSignedInUser()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var shippingAddressId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SHIP-200",
            BasePriceNetMinor = 1000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId,
            PackageWeight = 100
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });
        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                ShippingAddressId = shippingAddressId
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("SignedInUserRequiredToUseSavedShippingAddress");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenSavedAddressDoesNotExist()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var shippingAddressId = Guid.NewGuid();

        db.Set<Address>().Add(new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = "Owner",
            Street1 = "Main 1",
            PostalCode = "10115",
            City = "Berlin",
            CountryCode = "DE"
        });
        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SHIP-300",
            BasePriceNetMinor = 1000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId,
            PackageWeight = 100
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });
        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            UserId = userId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                UserId = userId,
                ShippingAddressId = shippingAddressId
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("SavedShippingAddressNotFound");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenNoShippingOptions()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SHIP-400",
            BasePriceNetMinor = 1000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId,
            PackageWeight = 100
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });
        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                ShippingAddress = new CheckoutAddressDto
                {
                    FullName = "Mia",
                    Street1 = "X",
                    PostalCode = "10117",
                    City = "Berlin",
                    CountryCode = "DE"
                }
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("NoShippingOptionsAvailableForCurrentCheckout");
    }

    [Fact]
    public async Task CreateStorefrontCheckoutIntent_Should_Throw_WhenSelectedShippingMethodInvalid()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var shippingMethodId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SHIP-500",
            BasePriceNetMinor = 1000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId,
            PackageWeight = 100
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });
        db.Set<ShippingMethod>().Add(new ShippingMethod
        {
            Id = shippingMethodId,
            Name = "DHL Standard",
            Carrier = "DHL",
            Service = "Standard",
            IsActive = true,
            CountriesCsv = "DE",
            Currency = "EUR",
            Rates =
            [
                new ShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShippingMethodId = shippingMethodId,
                    MaxShipmentMass = 2000,
                    PriceMinor = 490,
                    SortOrder = 1
                }
            ]
        });
        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            Items =
            [
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 1,
                    UnitPriceNetMinor = 1000,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontCheckoutIntentDto
            {
                CartId = cartId,
                SelectedShippingMethodId = Guid.NewGuid(),
                ShippingAddress = new CheckoutAddressDto
                {
                    FullName = "Mia",
                    Street1 = "X",
                    PostalCode = "10117",
                    City = "Berlin",
                    CountryCode = "DE"
                }
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("SelectedShippingMethodInvalidForCurrentCheckout");
    }

    [Fact]
    public async Task CreateStorefrontPaymentIntent_Should_ReuseExistingPendingPayment_ForAnonymousOrder()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20260326-00001",
            Currency = "EUR",
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_existing",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontPaymentIntentHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new CreateStorefrontPaymentIntentDto
        {
            OrderId = orderId,
            OrderNumber = "D-20260326-00001",
            Provider = "Stripe"
        }, TestContext.Current.CancellationToken);

        result.PaymentId.Should().Be(paymentId);
        result.ProviderReference.Should().Be("chk_existing");
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_KeepStripePaymentPending_UntilVerifiedWebhookCaptures()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20260326-00003",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "cs_pending",
                    ProviderCheckoutSessionRef = "cs_pending",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new CompleteStorefrontPaymentDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            OrderNumber = "D-20260326-00003",
            ProviderReference = "cs_pending",
            Outcome = StorefrontPaymentOutcome.Succeeded
        }, TestContext.Current.CancellationToken);

        result.OrderStatus.Should().Be(OrderStatus.Created);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.PaidAtUtc.Should().BeNull();

        var persistedPayment = await db.Set<Payment>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        persistedPayment.Status.Should().Be(PaymentStatus.Pending);
        persistedPayment.ProviderTransactionRef.Should().Be("cs_pending");

        var persistedOrder = await db.Set<Order>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        persistedOrder.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_VoidPayment_WhenShopperCancels()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20260326-00004",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_pending_cancel",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new CompleteStorefrontPaymentDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            OrderNumber = "D-20260326-00004",
            Outcome = StorefrontPaymentOutcome.Cancelled,
            FailureReason = "Customer closed the checkout window."
        }, TestContext.Current.CancellationToken);

        result.OrderStatus.Should().Be(OrderStatus.Created);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.PaidAtUtc.Should().BeNull();

        var persistedPayment = await db.Set<Payment>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        persistedPayment.Status.Should().Be(PaymentStatus.Pending);
        persistedPayment.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_NotRequireProviderReferences_WhenNotProvided_AndWaitForWebhook()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var fixedClock = new DateTime(2030, 4, 30, 12, 0, 0, DateTimeKind.Utc);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20260326-00005",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_pending",
                    ProviderPaymentIntentRef = "pi_expected",
                    ProviderCheckoutSessionRef = "cs_expected",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(
            db,
            new TestStringLocalizer(),
            new FakeClock(fixedClock));

        var result = await handler.HandleAsync(new CompleteStorefrontPaymentDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            OrderNumber = "D-20260326-00005",
            Outcome = StorefrontPaymentOutcome.Succeeded
        }, TestContext.Current.CancellationToken);

        result.OrderStatus.Should().Be(OrderStatus.Created);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.PaidAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_KeepStripePaymentPending_WhenReturnOutcomeFailed()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20260326-00006",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_pending_failed",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new CompleteStorefrontPaymentDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            OrderNumber = "D-20260326-00006",
            Outcome = StorefrontPaymentOutcome.Failed,
            FailureReason = "Card was declined."
        }, TestContext.Current.CancellationToken);

        result.OrderStatus.Should().Be(OrderStatus.Created);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.PaidAtUtc.Should().BeNull();

        var persistedPayment = await db.Set<Payment>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        persistedPayment.Status.Should().Be(PaymentStatus.Pending);
        persistedPayment.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_NotUpdate_WhenPaymentAlreadyFinalized()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var fixedClock = new DateTime(2030, 4, 30, 8, 0, 0, DateTimeKind.Utc);
        var originalPaidAtUtc = fixedClock.AddHours(-1);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00001",
            Currency = "EUR",
            Status = OrderStatus.Paid,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_captured",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Captured,
                    PaidAtUtc = originalPaidAtUtc
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(
            db,
            new TestStringLocalizer(),
            new FakeClock(fixedClock));

        var result = await handler.HandleAsync(new CompleteStorefrontPaymentDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            OrderNumber = "D-20300430-00001",
            Outcome = StorefrontPaymentOutcome.Failed,
            ProviderReference = "  new_provider_ref  ",
            FailureReason = "should not be persisted"
        }, TestContext.Current.CancellationToken);

        result.OrderStatus.Should().Be(OrderStatus.Paid);
        result.PaymentStatus.Should().Be(PaymentStatus.Captured);
        result.PaidAtUtc.Should().Be(originalPaidAtUtc);

        var persistedPayment = await db.Set<Payment>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        persistedPayment.Status.Should().Be(PaymentStatus.Captured);
        persistedPayment.PaidAtUtc.Should().Be(originalPaidAtUtc);
        persistedPayment.ProviderTransactionRef.Should().Be("chk_captured");
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_Throw_WhenProviderReferenceDoesNotMatch()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00002",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_pending",
                    ProviderPaymentIntentRef = "pi_expected",
                    ProviderCheckoutSessionRef = "cs_expected",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CompleteStorefrontPaymentDto
            {
                OrderId = orderId,
                PaymentId = paymentId,
                OrderNumber = "D-20300430-00002",
                Outcome = StorefrontPaymentOutcome.Succeeded,
                ProviderPaymentIntentReference = "  pi_unexpected  "
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("StorefrontPaymentProviderReferenceMismatch");
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_Throw_WhenOutcomeUnsupported()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00003",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_pending",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CompleteStorefrontPaymentDto
            {
                OrderId = orderId,
                PaymentId = paymentId,
                OrderNumber = "D-20300430-00003",
                Outcome = (StorefrontPaymentOutcome)999
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("UnsupportedStorefrontPaymentOutcome");
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_Throw_WhenOrderNotFound()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CompleteStorefrontPaymentDto
            {
                OrderId = Guid.NewGuid(),
                PaymentId = Guid.NewGuid(),
                OrderNumber = "MISS-00000",
                Outcome = StorefrontPaymentOutcome.Succeeded
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("OrderNotFound");
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_Throw_WhenPaymentNotFoundForOrder()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var foreignPaymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00017",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_other",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CompleteStorefrontPaymentDto
            {
                OrderId = orderId,
                PaymentId = foreignPaymentId,
                OrderNumber = "D-20300430-00017",
                Outcome = StorefrontPaymentOutcome.Succeeded
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("PaymentNotFoundForOrder");
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_Throw_WhenProviderCheckoutSessionReferenceDoesNotMatch()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00018",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 2590,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_pending",
                    ProviderPaymentIntentRef = "pi_expected",
                    ProviderCheckoutSessionRef = "cs_expected",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CompleteStorefrontPaymentDto
            {
                OrderId = orderId,
                PaymentId = paymentId,
                OrderNumber = "D-20300430-00018",
                Outcome = StorefrontPaymentOutcome.Succeeded,
                ProviderCheckoutSessionReference = "  cs_wrong  ",
                ProviderPaymentIntentReference = "pi_expected"
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("StorefrontPaymentProviderReferenceMismatch");
    }

    [Fact]
    public async Task GetStorefrontOrderConfirmation_Should_ReturnNull_ForAnonymousAccess_ToMemberOwnedOrder()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20260326-00002",
            UserId = Guid.NewGuid(),
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStorefrontOrderConfirmationHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new GetStorefrontOrderConfirmationDto
        {
            OrderId = orderId,
            OrderNumber = "D-20260326-00002"
        }, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateStorefrontPaymentIntent_Should_DefaultToStripe_WhenProviderIsBlank()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var existingOldPaymentId = Guid.NewGuid();
        var existingNewPaymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00010",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 1200,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = existingOldPaymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_old",
                    AmountMinor = 1200,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending,
                    CreatedAtUtc = new DateTime(2029, 1, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new Payment
                {
                    Id = existingNewPaymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_new",
                    AmountMinor = 1200,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending,
                    CreatedAtUtc = new DateTime(2029, 1, 1, 11, 0, 0, DateTimeKind.Utc)
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontPaymentIntentHandler(db, new TestStringLocalizer());
        var fixedClock = new DateTime(2030, 4, 30, 11, 0, 0, DateTimeKind.Utc);

        handler = new CreateStorefrontPaymentIntentHandler(
            db,
            new TestStringLocalizer(),
            new FakeClock(fixedClock));

        var result = await handler.HandleAsync(new CreateStorefrontPaymentIntentDto
        {
            OrderId = orderId,
            OrderNumber = "D-20300430-00010",
            Provider = "   "
        }, TestContext.Current.CancellationToken);

        result.PaymentId.Should().Be(existingNewPaymentId);
        result.ProviderReference.Should().Be("chk_new");
        result.Status.Should().Be(PaymentStatus.Pending);
        result.ExpiresAtUtc.Should().Be(fixedClock.AddMinutes(15));
    }

    [Fact]
    public async Task CreateStorefrontPaymentIntent_Should_Throw_WhenProviderIsUnsupported()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00011",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 1200,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontPaymentIntentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = orderId,
                OrderNumber = "D-20300430-00011",
                Provider = "PayPal"
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("StorefrontPaymentProviderNotSupported");
    }

    [Fact]
    public async Task CreateStorefrontPaymentIntent_Should_CreateStripeCheckoutSession_WhenNoPendingMatch()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var fixedClock = new DateTime(2030, 4, 30, 12, 0, 0, DateTimeKind.Utc);

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00012",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 3300,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = paymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_completed",
                    ProviderPaymentIntentRef = "pi_old",
                    ProviderCheckoutSessionRef = "cs_old",
                    AmountMinor = 3300,
                    Currency = "EUR",
                    Status = PaymentStatus.Failed
                }
            ]
        });
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Title = "Darwin",
            ContactEmail = "ops@example.test",
            StripeEnabled = true,
            StripeSecretKey = "sk_test_unit"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontPaymentIntentHandler(
            db,
            new TestStringLocalizer(),
            new FakeClock(fixedClock),
            new FakeStorefrontPaymentSessionClient());

        var result = await handler.HandleAsync(new CreateStorefrontPaymentIntentDto
        {
            OrderId = orderId,
            OrderNumber = "D-20300430-00012",
            Provider = "Stripe",
            ReturnUrl = "https://shop.example/return",
            CancelUrl = "https://shop.example/cancel"
        }, TestContext.Current.CancellationToken);

        result.PaymentId.Should().NotBe(paymentId);
        result.Provider.Should().Be("Stripe");
        result.ProviderReference.Should().Be("cs_unit_session");
        result.ProviderPaymentIntentReference.Should().Be("pi_unit_intent");
        result.ProviderCheckoutSessionReference.Should().Be("cs_unit_session");
        result.CheckoutUrl.Should().Be("https://checkout.stripe.test/session/cs_unit_session");
        result.ExpiresAtUtc.Should().Be(fixedClock.AddMinutes(15));
    }

    [Fact]
    public async Task CreateStorefrontPaymentIntent_Should_ReuseAuthorizedPayment_WhenExists()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();
        var authorizedId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00014",
            Currency = "EUR",
            Status = OrderStatus.Created,
            GrandTotalGrossMinor = 4000,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = pendingId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_pending",
                    AmountMinor = 4000,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending,
                    CreatedAtUtc = new DateTime(2029, 1, 1, 9, 0, 0, DateTimeKind.Utc)
                },
                new Payment
                {
                    Id = authorizedId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_authorized",
                    AmountMinor = 4000,
                    Currency = "EUR",
                    Status = PaymentStatus.Authorized,
                    CreatedAtUtc = new DateTime(2029, 1, 1, 10, 0, 0, DateTimeKind.Utc)
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontPaymentIntentHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new CreateStorefrontPaymentIntentDto
        {
            OrderId = orderId,
            OrderNumber = "D-20300430-00014",
            Provider = "Stripe"
        }, TestContext.Current.CancellationToken);

        result.PaymentId.Should().Be(authorizedId);
        result.ProviderReference.Should().Be("chk_authorized");
    }

    [Fact]
    public async Task CreateStorefrontPaymentIntent_Should_Throw_WhenOrderCancelledOrRefunded()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var settledPaymentId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00015",
            Currency = "EUR",
            Status = OrderStatus.Refunded,
            GrandTotalGrossMinor = 4000,
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}",
            Payments =
            [
                new Payment
                {
                    Id = settledPaymentId,
                    OrderId = orderId,
                    Provider = "Stripe",
                    ProviderTransactionRef = "chk_settled",
                    AmountMinor = 4000,
                    Currency = "EUR",
                    Status = PaymentStatus.Captured
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontPaymentIntentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = orderId,
                OrderNumber = "D-20300430-00015",
                Provider = "Stripe"
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("PaymentCannotBeInitiatedForCancelledOrRefundedOrder");
    }

    [Fact]
    public async Task CreateStorefrontPaymentIntent_Should_Throw_WhenOrderNotFound()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var handler = new CreateStorefrontPaymentIntentHandler(db, new TestStringLocalizer());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new CreateStorefrontPaymentIntentDto
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "MISS-00000",
                Provider = "Stripe"
            }, TestContext.Current.CancellationToken));

        ex.Message.Should().Be("OrderNotFound");
    }

    [Fact]
    public async Task GetStorefrontOrderConfirmation_Should_ReturnNull_ForWrongMemberOrderAccess()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00016",
            UserId = ownerId,
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStorefrontOrderConfirmationHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new GetStorefrontOrderConfirmationDto
        {
            OrderId = orderId,
            OrderNumber = "D-20300430-00016",
            UserId = Guid.NewGuid()
        }, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStorefrontOrderConfirmation_Should_ReturnOrder_ForAnonymousOrder()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00019",
            UserId = null,
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStorefrontOrderConfirmationHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new GetStorefrontOrderConfirmationDto
        {
            OrderId = orderId,
            OrderNumber = "D-20300430-00019"
        }, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.OrderNumber.Should().Be("D-20300430-00019");
    }

    [Fact]
    public async Task GetStorefrontOrderConfirmation_Should_ReturnOrder_ForCorrectMember()
    {
        await using var db = StorefrontCheckoutFlowTestDbContext.Create();
        var orderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        db.Set<Order>().Add(new Order
        {
            Id = orderId,
            OrderNumber = "D-20300430-00013",
            UserId = memberId,
            Currency = "EUR",
            BillingAddressJson = "{}",
            ShippingAddressJson = "{}"
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetStorefrontOrderConfirmationHandler(db, new TestStringLocalizer());

        var result = await handler.HandleAsync(new GetStorefrontOrderConfirmationDto
        {
            OrderId = orderId,
            OrderNumber = "D-20300430-00013",
            UserId = memberId
        }, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.OrderId.Should().Be(orderId);
        result.OrderNumber.Should().Be("D-20300430-00013");
    }

    private sealed class StorefrontCheckoutFlowTestDbContext : DbContext, IAppDbContext
    {
        private StorefrontCheckoutFlowTestDbContext(DbContextOptions<StorefrontCheckoutFlowTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static StorefrontCheckoutFlowTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<StorefrontCheckoutFlowTestDbContext>()
                .UseInMemoryDatabase($"darwin_storefront_checkout_flow_tests_{Guid.NewGuid()}")
                .Options;
            return new StorefrontCheckoutFlowTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cart>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CartId);
            });

            modelBuilder.Entity<CartItem>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.SelectedAddOnValueIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ProductVariant>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Sku).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Product>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<TaxCategory>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ShippingMethod>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Carrier).IsRequired();
                builder.Property(x => x.Service).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.Rates).WithOne().HasForeignKey(x => x.ShippingMethodId);
            });

            modelBuilder.Entity<ShippingRate>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.BillingAddressJson).IsRequired();
                builder.Property(x => x.ShippingAddressJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
                builder.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.OrderId);
                builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
            });

            modelBuilder.Entity<OrderLine>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Sku).IsRequired();
                builder.Property(x => x.AddOnValueIdsJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Address>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.FullName).IsRequired();
                builder.Property(x => x.Street1).IsRequired();
                builder.Property(x => x.PostalCode).IsRequired();
                builder.Property(x => x.City).IsRequired();
                builder.Property(x => x.CountryCode).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Title).IsRequired();
                builder.Property(x => x.ContactEmail).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.SupportedCulturesCsv).IsRequired();
                builder.Property(x => x.DefaultCountry).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.TimeZone).IsRequired();
                builder.Property(x => x.DateFormat).IsRequired();
                builder.Property(x => x.TimeFormat).IsRequired();
                builder.Property(x => x.MeasurementSystem).IsRequired();
                builder.Property(x => x.DisplayWeightUnit).IsRequired();
                builder.Property(x => x.DisplayLengthUnit).IsRequired();
                builder.Property(x => x.WebAuthnRelyingPartyId).IsRequired();
                builder.Property(x => x.WebAuthnRelyingPartyName).IsRequired();
                builder.Property(x => x.WebAuthnAllowedOriginsCsv).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class FakeClock : Darwin.Application.Abstractions.Services.IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed class FakeStorefrontPaymentSessionClient : IStorefrontPaymentSessionClient
    {
        public Task<StorefrontPaymentSessionResult> CreateSessionAsync(StorefrontPaymentSessionRequest request, CancellationToken ct = default)
        {
            request.SecretKey.Should().Be("sk_test_unit");
            request.ReturnUrl.Should().Be("https://shop.example/return");
            request.CancelUrl.Should().Be("https://shop.example/cancel");

            return Task.FromResult(new StorefrontPaymentSessionResult
            {
                ProviderReference = "cs_unit_session",
                ProviderCheckoutSessionReference = "cs_unit_session",
                ProviderPaymentIntentReference = "pi_unit_intent",
                CheckoutUrl = "https://checkout.stripe.test/session/cs_unit_session"
            });
        }
    }
}
