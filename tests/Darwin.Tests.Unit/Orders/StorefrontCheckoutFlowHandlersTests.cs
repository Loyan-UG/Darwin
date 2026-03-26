using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Queries;
using Darwin.Application.Shipping.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Orders;
using Darwin.Domain.Entities.Pricing;
using Darwin.Domain.Entities.Shipping;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

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
        var pickupMethodId = Guid.NewGuid();

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
                Id = pickupMethodId,
                Name = "Store Pickup",
                Carrier = "Darwin",
                Service = "Pickup",
                CountriesCsv = "DE",
                Currency = "EUR",
                Rates =
                [
                    new ShippingRate
                    {
                        Id = Guid.NewGuid(),
                        ShippingMethodId = pickupMethodId,
                        MaxShipmentMass = 5000,
                        PriceMinor = 0,
                        SortOrder = 1
                    }
                ]
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db), new RateShipmentHandler(db));

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
        result.SelectedShippingMethodId.Should().Be(pickupMethodId);
        result.SelectedShippingTotalMinor.Should().Be(0);
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
                    Provider = "DarwinCheckout",
                    ProviderTransactionRef = "chk_existing",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateStorefrontPaymentIntentHandler(db);

        var result = await handler.HandleAsync(new CreateStorefrontPaymentIntentDto
        {
            OrderId = orderId,
            OrderNumber = "D-20260326-00001",
            Provider = "DarwinCheckout"
        }, TestContext.Current.CancellationToken);

        result.PaymentId.Should().Be(paymentId);
        result.ProviderReference.Should().Be("chk_existing");
    }

    [Fact]
    public async Task CompleteStorefrontPayment_Should_MarkPaymentCaptured_AndOrderPaid()
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
                    Provider = "DarwinCheckout",
                    ProviderTransactionRef = "chk_pending",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db);

        var result = await handler.HandleAsync(new CompleteStorefrontPaymentDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            OrderNumber = "D-20260326-00003",
            ProviderReference = "psp_txn_1001",
            Outcome = StorefrontPaymentOutcome.Succeeded
        }, TestContext.Current.CancellationToken);

        result.OrderStatus.Should().Be(OrderStatus.Paid);
        result.PaymentStatus.Should().Be(PaymentStatus.Captured);
        result.PaidAtUtc.Should().NotBeNull();

        var persistedPayment = await db.Set<Payment>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        persistedPayment.Status.Should().Be(PaymentStatus.Captured);
        persistedPayment.ProviderTransactionRef.Should().Be("psp_txn_1001");

        var persistedOrder = await db.Set<Order>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);
        persistedOrder.Status.Should().Be(OrderStatus.Paid);
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
                    Provider = "DarwinCheckout",
                    ProviderTransactionRef = "chk_pending_cancel",
                    AmountMinor = 2590,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CompleteStorefrontPaymentHandler(db);

        var result = await handler.HandleAsync(new CompleteStorefrontPaymentDto
        {
            OrderId = orderId,
            PaymentId = paymentId,
            OrderNumber = "D-20260326-00004",
            Outcome = StorefrontPaymentOutcome.Cancelled,
            FailureReason = "Customer closed the checkout window."
        }, TestContext.Current.CancellationToken);

        result.OrderStatus.Should().Be(OrderStatus.Created);
        result.PaymentStatus.Should().Be(PaymentStatus.Voided);
        result.PaidAtUtc.Should().BeNull();

        var persistedPayment = await db.Set<Payment>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == paymentId, TestContext.Current.CancellationToken);
        persistedPayment.Status.Should().Be(PaymentStatus.Voided);
        persistedPayment.FailureReason.Should().Be("Customer closed the checkout window.");
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

        var handler = new GetStorefrontOrderConfirmationHandler(db);

        var result = await handler.HandleAsync(new GetStorefrontOrderConfirmationDto
        {
            OrderId = orderId,
            OrderNumber = "D-20260326-00002"
        }, TestContext.Current.CancellationToken);

        result.Should().BeNull();
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

            modelBuilder.Entity<Payment>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.Currency).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
