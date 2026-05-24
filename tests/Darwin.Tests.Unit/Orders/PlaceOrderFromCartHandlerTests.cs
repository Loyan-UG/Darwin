using System.Collections.Concurrent;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
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
using Microsoft.Extensions.Localization;

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
            IsActive = true,
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

        var localizer = new TestStringLocalizer();
        var checkoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);
        var handler = new PlaceOrderFromCartHandler(db, new ComputeCartSummaryHandler(db, localizer), checkoutIntentHandler, localizer);

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
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
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
                    MaxShipmentMass = 5000,
                    PriceMinor = 490,
                    SortOrder = 1
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new TestStringLocalizer();
        var checkoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);
        var handler = new PlaceOrderFromCartHandler(db, new ComputeCartSummaryHandler(db, localizer), checkoutIntentHandler, localizer);

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
        order.ShippingMethodName.Should().Be("DHL Standard");
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

        var localizer = new TestStringLocalizer();
        var checkoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(db, new ComputeCartSummaryHandler(db, localizer), new RateShipmentHandler(db), localizer);
        var handler = new PlaceOrderFromCartHandler(db, new ComputeCartSummaryHandler(db, localizer), checkoutIntentHandler, localizer);

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
            .WithMessage("SavedAddressNotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_SucceedOnce_And_BlockConcurrentCheckoutAttempts_WithPostSaveCheckoutConflict()
    {
        var databaseName = $"darwin_place_order_checkout_race_{Guid.NewGuid():N}";

        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var shippingMethodId = Guid.NewGuid();

        await using (var seedDb = PlaceOrderRaceTestDbContext.Create(databaseName))
        {
            seedDb.Set<ProductVariant>().Add(new ProductVariant
            {
                Id = variantId,
                ProductId = productId,
                Sku = "SKU-1001",
                BasePriceNetMinor = 1000,
                Currency = "EUR",
                TaxCategoryId = taxCategoryId
            });
            seedDb.Set<Product>().Add(new Product
            {
                Id = productId,
                IsActive = true,
                IsVisible = true
            });

            seedDb.Set<TaxCategory>().Add(new TaxCategory
            {
                Id = taxCategoryId,
                Name = "Standard",
                VatRate = 0.19m
            });

            seedDb.Set<Cart>().Add(new Cart
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

            seedDb.Set<ShippingMethod>().Add(new ShippingMethod
            {
                Id = shippingMethodId,
                Name = "DHL Paket",
                Carrier = "DHL",
                Service = "Paket",
                IsActive = true,
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

            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var leaderDb = PlaceOrderRaceTestDbContext.Create(databaseName, PlaceOrderRaceMode.Leader);
        await using var followerDb = PlaceOrderRaceTestDbContext.Create(databaseName, PlaceOrderRaceMode.Follower);

        var localizer = new TestStringLocalizer();

        var leaderCheckoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(
            leaderDb,
            new ComputeCartSummaryHandler(leaderDb, localizer),
            new RateShipmentHandler(leaderDb),
            localizer);

        var followerCheckoutIntentHandler = new CreateStorefrontCheckoutIntentHandler(
            followerDb,
            new ComputeCartSummaryHandler(followerDb, localizer),
            new RateShipmentHandler(followerDb),
            localizer);

        var leaderHandler = new PlaceOrderFromCartHandler(
            leaderDb,
            new ComputeCartSummaryHandler(leaderDb, localizer),
            leaderCheckoutIntentHandler,
            localizer);

        var followerHandler = new PlaceOrderFromCartHandler(
            followerDb,
            new ComputeCartSummaryHandler(followerDb, localizer),
            followerCheckoutIntentHandler,
            localizer);

        var dto = new PlaceOrderFromCartDto
        {
            CartId = cartId,
            SelectedShippingMethodId = shippingMethodId,
            ShippingTotalMinor = 590,
            BillingAddress = new CheckoutAddressDto
            {
                FullName = "Concurrent User",
                Street1 = "Waldstrasse 1",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            },
            ShippingAddress = new CheckoutAddressDto
            {
                FullName = "Concurrent User",
                Street1 = "Waldstrasse 1",
                PostalCode = "10115",
                City = "Berlin",
                CountryCode = "DE"
            }
        };

        var leaderResultTask = HandleOrderSafely(leaderHandler, dto);
        var followerResultTask = HandleOrderSafely(followerHandler, dto);

        await Task.WhenAll(leaderResultTask, followerResultTask);

        var leaderResult = await leaderResultTask;
        var followerResult = await followerResultTask;

        var successResult = new[] { leaderResult, followerResult }.Single(r => r.IsSuccess);
        var failure = new[] { leaderResult, followerResult }.Single(r => !r.IsSuccess);

        var successCount = new[] { leaderResult, followerResult }.Count(r => r.IsSuccess);
        successCount.Should().Be(1);
        failure.ErrorMessage.Should().Be("CartAlreadyCheckedOut");
        successResult.OrderNumber.Should().NotBeNullOrWhiteSpace();
        successResult.OrderNumber.Should().MatchRegex("^D-\\d{8}-[A-Z0-9]{6}$");

        await using var verifyDb = PlaceOrderRaceTestDbContext.Create(databaseName);
        var orderCount = await verifyDb.Set<Order>().CountAsync(TestContext.Current.CancellationToken);
        orderCount.Should().Be(1);

        var order = await verifyDb.Set<Order>()
            .SingleAsync(x => x.OrderNumber == successResult.OrderNumber, TestContext.Current.CancellationToken);

        var cart = await verifyDb.Set<Cart>().SingleAsync(x => x.Id == cartId, TestContext.Current.CancellationToken);
        cart.IsDeleted.Should().BeTrue();
        order.OrderNumber.Should().Be(successResult.OrderNumber);
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

            modelBuilder.Entity<Product>(builder =>
            {
                builder.HasKey(x => x.Id);
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

    private static async Task<(bool IsSuccess, string? ErrorMessage, string? OrderNumber)> HandleOrderSafely(
        PlaceOrderFromCartHandler handler,
        PlaceOrderFromCartDto dto)
    {
        try
        {
            var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);
            return (IsSuccess: true, ErrorMessage: null, OrderNumber: result.OrderNumber);
        }
        catch (InvalidOperationException ex)
        {
            return (IsSuccess: false, ErrorMessage: ex.Message, OrderNumber: null);
        }
    }

    private enum PlaceOrderRaceMode
    {
        Leader,
        Follower,
        Standard
    }

    private sealed class PlaceOrderRaceTestDbContext : DbContext, IAppDbContext
    {
        private readonly PlaceOrderRaceMode _raceMode;
        private readonly string _databaseName;
        private readonly int? _leaderDelayMs;
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> RaceSaveSignals = new();

        private PlaceOrderRaceTestDbContext(
            DbContextOptions<PlaceOrderRaceTestDbContext> options,
            PlaceOrderRaceMode raceMode = PlaceOrderRaceMode.Standard,
            string? databaseName = null,
            int? leaderDelayMs = null)
            : base(options)
        {
            _raceMode = raceMode;
            _databaseName = databaseName ?? Guid.NewGuid().ToString("N");
            _leaderDelayMs = leaderDelayMs;
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static PlaceOrderRaceTestDbContext Create(string databaseName, PlaceOrderRaceMode raceMode = PlaceOrderRaceMode.Standard, int? leaderDelayMs = null)
        {
            var options = new DbContextOptionsBuilder<PlaceOrderRaceTestDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new PlaceOrderRaceTestDbContext(options, raceMode, databaseName, leaderDelayMs);
        }

        public static PlaceOrderRaceTestDbContext Create(string databaseName) => Create(databaseName, PlaceOrderRaceMode.Standard);

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            var signal = RaceSaveSignals.GetOrAdd(_databaseName, static _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

            if (_raceMode == PlaceOrderRaceMode.Follower)
            {
                await signal.Task.ConfigureAwait(false);
                throw new DbUpdateConcurrencyException("CartAlreadyCheckedOut");
            }

            if (_leaderDelayMs.HasValue)
            {
                await Task.Delay(_leaderDelayMs.Value, ct).ConfigureAwait(false);
            }

            var result = await base.SaveChangesAsync(ct).ConfigureAwait(false);

            if (_raceMode == PlaceOrderRaceMode.Leader)
            {
                signal.TrySetResult(true);
            }

            return result;
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

            modelBuilder.Entity<Product>(builder =>
            {
                builder.HasKey(x => x.Id);
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

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
