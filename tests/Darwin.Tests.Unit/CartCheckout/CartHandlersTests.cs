using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.Services;
using Darwin.Application.CartCheckout.Commands;
using Darwin.Application.CartCheckout.DTOs;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CartCheckout;

/// <summary>
/// Covers cart mutation handler behavior for coupon application,
/// item removal, and quantity updates.
/// </summary>
public sealed class CartHandlersTests
{
    // ─── ApplyCouponHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task AddOrIncreaseCartItem_Should_IncreaseExistingLine_WhenSameVariantAndAddOnsAlreadyExist()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();

        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });
        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            TaxCategoryId = taxCategoryId,
            Sku = "SKU-111",
            Currency = "EUR",
            BasePriceNetMinor = 2999
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-add-1", Currency = "EUR" });
        db.Set<CartItem>().Add(new CartItem
        {
            CartId = cartId,
            VariantId = variantId,
            Quantity = 1,
            UnitPriceNetMinor = 2999,
            VatRate = 0.19m,
            SelectedAddOnValueIdsJson = "[]"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AddOrIncreaseCartItemHandler(db, new TestAddOnPricingService(), new TestStringLocalizer());

        await handler.HandleAsync(new CartAddItemDto
        {
            AnonymousId = "anon-add-1",
            VariantId = variantId,
            Quantity = 2,
            Currency = "EUR"
        }, TestContext.Current.CancellationToken);

        var items = await db.Set<CartItem>()
            .Where(x => x.CartId == cartId && !x.IsDeleted)
            .ToListAsync(TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
        items[0].Quantity.Should().Be(3);
    }

    [Fact]
    public async Task AddOrIncreaseCartItem_Should_CreateCart_WhenAnonymousCartDoesNotExist()
    {
        await using var db = CartTestDbContext.Create();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();

        db.Set<TaxCategory>().Add(new TaxCategory
        {
            Id = taxCategoryId,
            Name = "Standard",
            VatRate = 0.19m
        });
        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            TaxCategoryId = taxCategoryId,
            Sku = "SKU-222",
            Currency = "EUR",
            BasePriceNetMinor = 1999
        });
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AddOrIncreaseCartItemHandler(db, new TestAddOnPricingService(), new TestStringLocalizer());

        var cartId = await handler.HandleAsync(new CartAddItemDto
        {
            AnonymousId = "anon-add-2",
            VariantId = variantId,
            Quantity = 1,
            Currency = "EUR"
        }, TestContext.Current.CancellationToken);

        var cart = await db.Set<Cart>().SingleAsync(x => x.Id == cartId, TestContext.Current.CancellationToken);
        var item = await db.Set<CartItem>().SingleAsync(x => x.CartId == cartId && x.VariantId == variantId, TestContext.Current.CancellationToken);

        cart.AnonymousId.Should().Be("anon-add-2");
        item.Quantity.Should().Be(1);
        item.UnitPriceNetMinor.Should().Be(1999);
    }

    [Fact]
    public async Task ApplyCoupon_Should_SetCouponCode_WhenPromoIsValid()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-1", Currency = "EUR" });
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
        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            TaxCategoryId = taxCategoryId,
            Sku = "SKU-COUPON-1",
            Currency = "EUR",
            BasePriceNetMinor = 3000
        });
        db.Set<CartItem>().Add(new CartItem
        {
            CartId = cartId,
            VariantId = variantId,
            Quantity = 1,
            UnitPriceNetMinor = 3000,
            VatRate = 0.19m,
            SelectedAddOnValueIdsJson = "[]"
        });
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Summer Sale",
            Code = "SUMMER10",
            Type = Darwin.Domain.Enums.PromotionType.Percentage,
            Percent = 10m,
            IsActive = true,
            StartsAtUtc = null,
            EndsAtUtc = null,
            Currency = "EUR"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyCouponHandler(db, new TestStringLocalizer());

        await handler.HandleAsync(new CartApplyCouponDto { CartId = cartId, CouponCode = "SUMMER10" },
            TestContext.Current.CancellationToken);

        var cart = await db.Set<Cart>().SingleAsync(c => c.Id == cartId, TestContext.Current.CancellationToken);
        cart.CouponCode.Should().Be("SUMMER10");
    }

    [Fact]
    public async Task ApplyCoupon_Should_ClearCouponCode_WhenCouponCodeIsEmpty()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-2", Currency = "EUR", CouponCode = "OLD_CODE" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyCouponHandler(db, new TestStringLocalizer());

        await handler.HandleAsync(new CartApplyCouponDto { CartId = cartId, CouponCode = null },
            TestContext.Current.CancellationToken);

        var cart = await db.Set<Cart>().SingleAsync(c => c.Id == cartId, TestContext.Current.CancellationToken);
        cart.CouponCode.Should().BeNull();
    }

    [Fact]
    public async Task ApplyCoupon_Should_Throw_WhenCouponCodeIsInvalidOrInactive()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-3", Currency = "EUR" });
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Expired Promo",
            Code = "EXPIRED",
            IsActive = false,
            Currency = "EUR"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyCouponHandler(db, new TestStringLocalizer());

        var act = () => handler.HandleAsync(
            new CartApplyCouponDto { CartId = cartId, CouponCode = "EXPIRED" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CouponIsInvalidOrInactive");
    }

    [Fact]
    public async Task ApplyCoupon_Should_Throw_WhenPromoIsOutsideTimeWindow()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-4", Currency = "EUR" });
        db.Set<Promotion>().Add(new Promotion
        {
            Id = Guid.NewGuid(),
            Name = "Future Promo",
            Code = "FUTURE20",
            IsActive = true,
            StartsAtUtc = DateTime.UtcNow.AddDays(10),
            EndsAtUtc = null,
            Currency = "EUR"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ApplyCouponHandler(db, new TestStringLocalizer());

        var act = () => handler.HandleAsync(
            new CartApplyCouponDto { CartId = cartId, CouponCode = "FUTURE20" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CouponIsInvalidOrInactive");
    }

    [Fact]
    public async Task ApplyCoupon_Should_Throw_WhenCartNotFound()
    {
        await using var db = CartTestDbContext.Create();

        var handler = new ApplyCouponHandler(db, new TestStringLocalizer());

        var act = () => handler.HandleAsync(
            new CartApplyCouponDto { CartId = Guid.NewGuid(), CouponCode = "ANY" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CartNotFound");
    }

    // ─── RemoveCartItemHandler ───────────────────────────────────────────────

    [Fact]
    public async Task RemoveCartItem_Should_SoftDeleteLine_WhenItemExists()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-5", Currency = "EUR" });
        db.Set<CartItem>().Add(new CartItem
        {
            Id = itemId,
            CartId = cartId,
            VariantId = variantId,
            Quantity = 2,
            UnitPriceNetMinor = 1000,
            VatRate = 0.19m,
            SelectedAddOnValueIdsJson = "[]"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RemoveCartItemHandler(db);

        await handler.HandleAsync(new CartRemoveItemDto { CartId = cartId, VariantId = variantId },
            TestContext.Current.CancellationToken);

        var item = await db.Set<CartItem>().SingleAsync(i => i.Id == itemId, TestContext.Current.CancellationToken);
        item.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveCartItem_Should_BeIdempotent_WhenItemDoesNotExist()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-6", Currency = "EUR" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RemoveCartItemHandler(db);

        // Should not throw even when no line is found
        var act = () => handler.HandleAsync(
            new CartRemoveItemDto { CartId = cartId, VariantId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveCartItem_Should_OnlyRemoveMatchingAddOnConfiguration()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var itemAId = Guid.NewGuid();
        var itemBId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-7", Currency = "EUR" });
        db.Set<CartItem>().AddRange(
            new CartItem
            {
                Id = itemAId,
                CartId = cartId,
                VariantId = variantId,
                Quantity = 1,
                UnitPriceNetMinor = 1000,
                VatRate = 0.19m,
                SelectedAddOnValueIdsJson = "[\"addon-a\"]"
            },
            new CartItem
            {
                Id = itemBId,
                CartId = cartId,
                VariantId = variantId,
                Quantity = 1,
                UnitPriceNetMinor = 1200,
                VatRate = 0.19m,
                SelectedAddOnValueIdsJson = "[\"addon-b\"]"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RemoveCartItemHandler(db);

        await handler.HandleAsync(
            new CartRemoveItemDto
            {
                CartId = cartId,
                VariantId = variantId,
                SelectedAddOnValueIdsJson = "[\"addon-a\"]"
            },
            TestContext.Current.CancellationToken);

        var itemA = await db.Set<CartItem>().SingleAsync(i => i.Id == itemAId, TestContext.Current.CancellationToken);
        var itemB = await db.Set<CartItem>().SingleAsync(i => i.Id == itemBId, TestContext.Current.CancellationToken);

        itemA.IsDeleted.Should().BeTrue();
        itemB.IsDeleted.Should().BeFalse();
    }

    // ─── UpdateCartItemQuantityHandler ───────────────────────────────────────

    [Fact]
    public async Task UpdateCartItemQuantity_Should_UpdateQuantity_WhenPositive()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-8", Currency = "EUR" });
        db.Set<CartItem>().Add(new CartItem
        {
            Id = itemId,
            CartId = cartId,
            VariantId = variantId,
            Quantity = 1,
            UnitPriceNetMinor = 500,
            VatRate = 0.19m,
            SelectedAddOnValueIdsJson = "[]"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCartItemQuantityHandler(db, new TestStringLocalizer());

        await handler.HandleAsync(
            new CartUpdateQtyDto { CartId = cartId, VariantId = variantId, Quantity = 5 },
            TestContext.Current.CancellationToken);

        var item = await db.Set<CartItem>().SingleAsync(i => i.Id == itemId, TestContext.Current.CancellationToken);
        item.Quantity.Should().Be(5);
        item.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCartItemQuantity_Should_SoftDeleteLine_WhenQuantityIsZero()
    {
        await using var db = CartTestDbContext.Create();
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        db.Set<Cart>().Add(new Cart { Id = cartId, AnonymousId = "anon-9", Currency = "EUR" });
        db.Set<CartItem>().Add(new CartItem
        {
            Id = itemId,
            CartId = cartId,
            VariantId = variantId,
            Quantity = 3,
            UnitPriceNetMinor = 800,
            VatRate = 0.07m,
            SelectedAddOnValueIdsJson = "[]"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateCartItemQuantityHandler(db, new TestStringLocalizer());

        await handler.HandleAsync(
            new CartUpdateQtyDto { CartId = cartId, VariantId = variantId, Quantity = 0 },
            TestContext.Current.CancellationToken);

        var item = await db.Set<CartItem>().SingleAsync(i => i.Id == itemId, TestContext.Current.CancellationToken);
        item.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCartItemQuantity_Should_Throw_WhenLineNotFound()
    {
        await using var db = CartTestDbContext.Create();

        var handler = new UpdateCartItemQuantityHandler(db, new TestStringLocalizer());

        var act = () => handler.HandleAsync(
            new CartUpdateQtyDto { CartId = Guid.NewGuid(), VariantId = Guid.NewGuid(), Quantity = 2 },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CartLineNotFound");
    }

    // ─── Shared DbContext + helpers ──────────────────────────────────────────

    private sealed class CartTestDbContext : DbContext, IAppDbContext
    {
        private CartTestDbContext(DbContextOptions<CartTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CartTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CartTestDbContext>()
                .UseInMemoryDatabase($"darwin_cart_tests_{Guid.NewGuid()}")
                .Options;
            return new CartTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cart>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<CartItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.SelectedAddOnValueIdsJson).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ProductVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Sku).HasMaxLength(128).IsRequired();
                b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<Promotion>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<TaxCategory>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }

    private sealed class TestAddOnPricingService : IAddOnPricingService
    {
        public Task ValidateSelectionsForVariantAsync(Guid variantId, IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<long> SumPriceDeltasAsync(IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct) =>
            Task.FromResult(0L);
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
