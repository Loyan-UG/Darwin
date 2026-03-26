using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Application.Shipping.Queries;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.Shipping;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Orders;

/// <summary>
/// Verifies storefront cart checkout behavior so order snapshots, totals, and cart finalization stay aligned.
/// </summary>
public sealed class PlaceOrderFromCartHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_CreateOrder_FromCart_AndFinalizeCart()
    {
        await using var db = PlaceOrderFromCartTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var addOnOptionId = Guid.NewGuid();
        var addOnValueId = Guid.NewGuid();
        var shippingMethodId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SKU-1001",
            BasePriceNetMinor = 1000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId
        });

        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });

        db.Set<AddOnOptionValue>().Add(new AddOnOptionValue
        {
            Id = addOnValueId,
            AddOnOptionId = addOnOptionId,
            Label = "Premium Box",
            PriceDeltaMinor = 200,
            IsActive = true
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
                    UnitPriceNetMinor = 1200,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = $"[\"{addOnValueId}\"]",
                    AddOnPriceDeltaMinor = 200
                }
            ]
        });

        db.Set<ShippingMethod>().Add(new ShippingMethod
        {
            Id = shippingMethodId,
            Name = "DHL Paket",
            Carrier = "DHL",
            Service = "Paket",
            CountriesCsv = "DE",
            Currency = "EUR",
            Rates =
            [
                new ShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShippingMethodId = shippingMethodId,
                    MaxShipmentMass = 5000,
                    PriceMinor = 590,
                    SortOrder = 1
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var checkoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db), new RateShipmentHandler(db));
        var handler = new PlaceOrderFromCartHandler(db, new ComputeCartSummaryHandler(db), checkoutIntentHandler);

        var result = await handler.HandleAsync(new PlaceOrderFromCartDto
        {
            CartId = cartId,
            SelectedShippingMethodId = shippingMethodId,
            ShippingTotalMinor = 590,
            BillingAddress = new CheckoutAddressDto
            {
                FullName = "Max Mustermann",
                Street1 = "Musterstrasse 1",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            },
            ShippingAddress = new CheckoutAddressDto
            {
                FullName = "Max Mustermann",
                Street1 = "Musterstrasse 1",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            }
        }, TestContext.Current.CancellationToken);

        var order = await db.Set<Order>()
            .Include(x => x.Lines)
            .SingleAsync(x => x.Id == result.OrderId, TestContext.Current.CancellationToken);

        var cart = await db.Set<Cart>()
            .Include(x => x.Items)
            .SingleAsync(x => x.Id == cartId, TestContext.Current.CancellationToken);

        result.OrderNumber.Should().NotBeNullOrWhiteSpace();
        result.Currency.Should().Be("EUR");
        result.Status.Should().Be(OrderStatus.Created);
        result.GrandTotalGrossMinor.Should().Be(3446);

        order.SubtotalNetMinor.Should().Be(2400);
        order.TaxTotalMinor.Should().Be(456);
        order.ShippingTotalMinor.Should().Be(590);
        order.DiscountTotalMinor.Should().Be(0);
        order.GrandTotalGrossMinor.Should().Be(3446);
        order.ShippingMethodId.Should().Be(shippingMethodId);
        order.ShippingMethodName.Should().Be("DHL Paket");
        order.ShippingCarrier.Should().Be("DHL");
        order.ShippingService.Should().Be("Paket");
        order.BillingAddressJson.Should().Contain("Max Mustermann");
        order.ShippingAddressJson.Should().Contain("Musterstrasse 1");
        order.Lines.Should().ContainSingle();
        order.Lines[0].AddOnPriceDeltaMinor.Should().Be(200);
        order.Lines[0].UnitPriceNetMinor.Should().Be(1200);
        order.Lines[0].UnitPriceGrossMinor.Should().Be(1428);
        order.Lines[0].LineGrossMinor.Should().Be(2856);

        cart.IsDeleted.Should().BeTrue();
        cart.Items.Should().OnlyContain(x => x.IsDeleted);
    }

    [Fact]
    public async Task HandleAsync_Should_UseSavedMemberAddresses_WhenAddressIdsAreProvided()
    {
        await using var db = PlaceOrderFromCartTestDbContext.Create();
        var userId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var billingAddressId = Guid.NewGuid();
        var shippingAddressId = Guid.NewGuid();
        var shippingMethodId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SKU-2001",
            BasePriceNetMinor = 2500,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId
        });

        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Reduced",
            VatRate = 0.07m
        });

        db.Set<Address>().AddRange(
            new Address
            {
                Id = billingAddressId,
                UserId = userId,
                FullName = "Anna Schmidt",
                Street1 = "Friedrichstrasse 12",
                PostalCode = "10117",
                City = "Berlin",
                CountryCode = "DE"
            },
            new Address
            {
                Id = shippingAddressId,
                UserId = userId,
                FullName = "Anna Schmidt",
                Street1 = "Unter den Linden 5",
                PostalCode = "10117",
                City = "Berlin",
                CountryCode = "DE"
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
                    UnitPriceNetMinor = 2500,
                    VatRate = 0.07m,
                    SelectedAddOnValueIdsJson = "[]"
                }
            ]
        });

        db.Set<ShippingMethod>().Add(new ShippingMethod
        {
            Id = shippingMethodId,
            Name = "Hermes Standard",
            Carrier = "Hermes",
            Service = "Standard",
            CountriesCsv = "DE",
            Currency = "EUR",
            Rates =
            [
                new ShippingRate
                {
                    Id = Guid.NewGuid(),
                    ShippingMethodId = shippingMethodId,
                    MaxShipmentMass = 5000,
                    PriceMinor = 490,
                    SortOrder = 1
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var checkoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db), new RateShipmentHandler(db));
        var handler = new PlaceOrderFromCartHandler(db, new ComputeCartSummaryHandler(db), checkoutIntentHandler);

        var result = await handler.HandleAsync(new PlaceOrderFromCartDto
        {
            CartId = cartId,
            UserId = userId,
            BillingAddressId = billingAddressId,
            ShippingAddressId = shippingAddressId,
            SelectedShippingMethodId = shippingMethodId,
            ShippingTotalMinor = 490
        }, TestContext.Current.CancellationToken);

        var order = await db.Set<Order>()
            .SingleAsync(x => x.Id == result.OrderId, TestContext.Current.CancellationToken);

        order.BillingAddressJson.Should().Contain("Friedrichstrasse 12");
        order.ShippingAddressJson.Should().Contain("Unter den Linden 5");
        order.GrandTotalGrossMinor.Should().Be(3165);
        order.ShippingMethodName.Should().Be("Hermes Standard");
    }

    [Fact]
    public async Task HandleAsync_Should_RejectSavedAddress_WhenItDoesNotBelongToCurrentUser()
    {
        await using var db = PlaceOrderFromCartTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var addressId = Guid.NewGuid();

        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Sku = "SKU-3001",
            BasePriceNetMinor = 1000,
            Currency = "EUR",
            TaxCategoryId = taxCategoryId
        });

        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });

        db.Set<Address>().Add(new Address
        {
            Id = addressId,
            UserId = otherUserId,
            FullName = "Lukas Meier",
            Street1 = "Hamburger Allee 3",
            PostalCode = "60486",
            City = "Frankfurt am Main",
            CountryCode = "DE"
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

        var checkoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db), new RateShipmentHandler(db));
        var handler = new PlaceOrderFromCartHandler(db, new ComputeCartSummaryHandler(db), checkoutIntentHandler);

        var action = () => handler.HandleAsync(new PlaceOrderFromCartDto
        {
            CartId = cartId,
            UserId = userId,
            BillingAddressId = addressId,
            ShippingAddress = new CheckoutAddressDto
            {
                FullName = "Anna Schmidt",
                Street1 = "Teststrasse 7",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            },
            ShippingTotalMinor = 0
        }, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Saved billing address not found.");
    }

    private sealed class PlaceOrderFromCartTestDbContext : DbContext, IAppDbContext
    {
        private PlaceOrderFromCartTestDbContext(DbContextOptions<PlaceOrderFromCartTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PlaceOrderFromCartTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<PlaceOrderFromCartTestDbContext>()
                .UseInMemoryDatabase($"darwin_place_order_tests_{Guid.NewGuid()}")
                .Options;
            return new PlaceOrderFromCartTestDbContext(options);
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

            modelBuilder.Entity<Order>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.OrderNumber).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.BillingAddressJson).IsRequired();
                builder.Property(x => x.ShippingAddressJson).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
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

            modelBuilder.Entity<ProductVariant>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Sku).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ProductTranslation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Culture).IsRequired();
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.Slug).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<TaxCategory>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<AddOnOptionValue>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Label).IsRequired();
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
        }
    }
}
