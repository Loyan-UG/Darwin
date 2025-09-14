using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Tests.Unit.Common;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.Catalog
{
    /// <summary>
    /// Unit tests for the product unique slug validators.  These tests
    /// verify that the <see cref="ProductCreateUniqueSlugValidator"/> and
    /// <see cref="ProductEditUniqueSlugValidator"/> correctly enforce
    /// uniqueness of (culture, slug) combinations across the database.  They
    /// use an in‑memory <see cref="IAppDbContext"/> provided by
    /// <see cref="TestDbFactory"/> and do not depend on any external
    /// infrastructure.
    /// </summary>
    public sealed class ProductUniqueSlugValidatorTests
    {
        [Fact]
        public async Task Create_Should_Fail_When_Duplicate_Slug_Per_Culture()
        {
            // Arrange
            var ctx = TestDbFactory.Create();
            // Seed an existing translation with the same culture/slug
            await ctx.Set<ProductTranslation>().AddAsync(new ProductTranslation
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Culture = "de-DE",
                Slug = "test-product",
                Name = "Existing"
            });
            await ctx.SaveChangesAsync();

            var dto = new ProductCreateDto
            {
                Translations =
                {
                    new ProductTranslationDto { Culture = "de-DE", Slug = "test-product", Name = "New" }
                },
                Variants =
                {
                    new ProductVariantCreateDto
                    {
                        Sku = "SKU1",
                        Currency = "EUR",
                        BasePriceNetMinor = 100,
                        TaxCategoryId = Guid.NewGuid()
                    }
                }
            };
            var validator = new ProductCreateUniqueSlugValidator(ctx);

            // Act
            var result = await validator.ValidateAsync(dto, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task Edit_Should_Fail_When_Duplicate_Slug_Per_Culture_For_Other_Product()
        {
            // Arrange
            var ctx = TestDbFactory.Create();
            var existingProductId = Guid.NewGuid();
            // Existing translation belongs to a different product but has same culture/slug
            await ctx.Set<ProductTranslation>().AddAsync(new ProductTranslation
            {
                Id = Guid.NewGuid(),
                ProductId = existingProductId,
                Culture = "en-US",
                Slug = "sample",
                Name = "Existing"
            });
            await ctx.SaveChangesAsync();

            var dto = new ProductEditDto
            {
                Id = Guid.NewGuid(),
                RowVersion = Array.Empty<byte>(),
                Translations =
                {
                    new ProductTranslationDto { Culture = "en-US", Slug = "sample", Name = "Conflicting" }
                },
                Variants =
                {
                    new ProductVariantCreateDto
                    {
                        Sku = "SKU2",
                        Currency = "EUR",
                        BasePriceNetMinor = 100,
                        TaxCategoryId = Guid.NewGuid()
                    }
                }
            };
            var validator = new ProductEditUniqueSlugValidator(ctx);

            // Act
            var result = await validator.ValidateAsync(dto, CancellationToken.None);

            // Assert
            result.IsValid.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that creating a product with a unique slug in the same
        /// culture succeeds.  The test seeds a translation for culture
        /// "en-US" and slug "old", then attempts to create a new
        /// product with slug "new".  Because the slug is unique, the
        /// validator should accept the create DTO.
        /// </summary>
        [Fact]
        public async Task Create_Should_Pass_When_Slug_Unique_Per_Culture()
        {
            // Arrange
            var ctx = TestDbFactory.Create();
            await ctx.Set<ProductTranslation>().AddAsync(new ProductTranslation
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Culture = "en-US",
                Slug = "old",
                Name = "Existing"
            });
            await ctx.SaveChangesAsync();

            var dto = new ProductCreateDto
            {
                Translations =
                {
                    new ProductTranslationDto { Culture = "en-US", Slug = "new", Name = "New" }
                },
                Variants =
                {
                    new ProductVariantCreateDto
                    {
                        Sku = "SKU_NEW",
                        Currency = "EUR",
                        BasePriceNetMinor = 100,
                        TaxCategoryId = Guid.NewGuid()
                    }
                }
            };
            var validator = new ProductCreateUniqueSlugValidator(ctx);
            // Act
            var result = await validator.ValidateAsync(dto, CancellationToken.None);
            // Assert
            result.IsValid.Should().BeTrue("unique (culture, slug) combinations should pass validation");
        }

        /// <summary>
        /// Ensures that editing a product does not fail when the slug
        /// remains unchanged for the same product.  The test seeds a
        /// product translation with slug "same" and verifies that an
        /// edit DTO with the same slug and product identifier passes
        /// validation.
        /// </summary>
        [Fact]
        public async Task Edit_Should_Pass_When_Slug_Unchanged_For_Same_Product()
        {
            // Arrange
            var ctx = TestDbFactory.Create();
            var productId = Guid.NewGuid();
            await ctx.Set<ProductTranslation>().AddAsync(new ProductTranslation
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Culture = "en-US",
                Slug = "same",
                Name = "Original"
            });
            await ctx.SaveChangesAsync();

            var dto = new ProductEditDto
            {
                Id = productId,
                RowVersion = Array.Empty<byte>(),
                Translations =
                {
                    new ProductTranslationDto { Culture = "en-US", Slug = "same", Name = "Updated" }
                },
                Variants =
                {
                    new ProductVariantCreateDto
                    {
                        Sku = "SKU_SAME",
                        Currency = "EUR",
                        BasePriceNetMinor = 100,
                        TaxCategoryId = Guid.NewGuid()
                    }
                }
            };
            var validator = new ProductEditUniqueSlugValidator(ctx);
            // Act
            var result = await validator.ValidateAsync(dto, CancellationToken.None);
            // Assert
            result.IsValid.Should().BeTrue("editing a product without changing its slug should pass validation");
        }
    }
}