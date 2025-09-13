using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Darwin.Tests.Unit.Catalog
{
    public sealed class ProductUniqueSlugValidatorTests
    {
        [Fact]
        public async Task Create_Should_Fail_When_Duplicate_Slug_Per_Culture()
        {
            var ctx = TestDbFactory.Create();

            // seed an existing translation
            await ctx.Set<ProductTranslation>().AddAsync(new ProductTranslation
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Culture = "de-DE",
                Slug = "foo",
                Name = "Existing"
            });
            await ctx.SaveChangesAsync();

            var dto = new ProductCreateDto
            {
                Translations =
                {
                    new ProductTranslationDto { Culture = "de-DE", Slug = "foo", Name = "New" }
                },
                Variants =
                {
                    new ProductVariantCreateDto
                    {
                        Sku = "S1", Currency = "EUR", BasePriceNetMinor = 100, TaxCategoryId = Guid.NewGuid()
                    }
                }
            };

            var sut = new ProductCreateUniqueSlugValidator(ctx);
            var result = await sut.ValidateAsync(dto, CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }
    }
}
