using System;
using System.Collections.Generic;
using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CartCheckout.Queries;
using Darwin.Domain.Entities.CartCheckout;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.CartCheckout;

/// <summary>
/// Unit tests for localized add-on labels in cart summaries.
/// </summary>
public sealed class ComputeCartSummaryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Ignore_SoftDeleted_AddOnValue_Translations_WhenSelectingCulture()
    {
        await using var db = CartSummaryTestDbContext.Create();

        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var taxCategoryId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var valueId = Guid.NewGuid();

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
            Sku = "SKU-ADDON-1",
            Currency = "EUR",
            BasePriceNetMinor = 1200,
            TaxCategoryId = taxCategoryId
        });

        db.Set<Product>().Add(new Product
        {
            Id = productId,
            IsActive = true,
            IsVisible = true
        });

        db.Set<Cart>().Add(new Cart
        {
            Id = cartId,
            Currency = "EUR",
            AnonymousId = "anon-addon-summary",
            Items =
            [
                new CartItem
                {
                    CartId = cartId,
                    VariantId = variantId,
                    Quantity = 1,
                    UnitPriceNetMinor = 1200,
                    VatRate = 0.19m,
                    SelectedAddOnValueIdsJson = JsonSerializer.Serialize([valueId])
                }
            ]
        });

        db.Set<AddOnOption>().Add(new AddOnOption
        {
            Id = optionId,
            Label = "Gift Wrap",
            Translations =
            [
                new AddOnOptionTranslation
                {
                    Culture = "de-DE",
                    Label = "Verpackung",
                    IsDeleted = true
                }
            ]
        });

        db.Set<AddOnOptionValue>().Add(new AddOnOptionValue
        {
            Id = valueId,
            AddOnOptionId = optionId,
            Label = "Classic",
            PriceDeltaMinor = 150,
            IsActive = true,
            Translations =
            [
                new AddOnOptionValueTranslation
                {
                    Culture = "de-DE",
                    Label = "Löschen",
                    IsDeleted = true
                },
                new AddOnOptionValueTranslation
                {
                    Culture = "en-US",
                    Label = "Classic"
                }
            ]
        });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ComputeCartSummaryHandler(db, new TestStringLocalizer());
        var summary = await handler.HandleAsync(cartId, "de-DE", TestContext.Current.CancellationToken);

        var firstItem = summary.Items.Should().ContainSingle().Subject;
        var selected = firstItem.SelectedAddOns.Should().ContainSingle().Subject;

        selected.ValueId.Should().Be(valueId);
        selected.ValueLabel.Should().Be("Classic");
    }

    private sealed class CartSummaryTestDbContext : DbContext, IAppDbContext
    {
        private CartSummaryTestDbContext(DbContextOptions<CartSummaryTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CartSummaryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CartSummaryTestDbContext>()
                .UseInMemoryDatabase($"darwin_cart_summary_tests_{Guid.NewGuid()}")
                .Options;
            return new CartSummaryTestDbContext(options);
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
                builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
                builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
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

            modelBuilder.Entity<AddOnOption>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Label).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<AddOnOptionValue>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Label).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<AddOnOptionTranslation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Culture).IsRequired();
                builder.Property(x => x.Label).IsRequired();
            });

            modelBuilder.Entity<AddOnOptionValueTranslation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Culture).IsRequired();
                builder.Property(x => x.Label).IsRequired();
            });
        }
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
