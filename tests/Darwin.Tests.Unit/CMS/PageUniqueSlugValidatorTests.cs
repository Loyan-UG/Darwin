using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Validators;
using Darwin.Domain.Entities.CMS;
using FluentAssertions;
using Xunit;

namespace Darwin.Tests.Unit.CMS
{
    /// <summary>
    /// Unit tests for the page slug unique validators.  These tests exercise the
    /// <see cref="PageCreateUniqueSlugValidator"/> and <see cref="PageEditUniqueSlugValidator"/>
    /// to ensure that slugs remain unique per culture.  The tests cover both the
    /// creation scenario—where a page with a duplicate slug must be rejected—and
    /// the edit scenario, where the slug may remain unchanged for the same page or
    /// be changed to a new unique value.  Conflicts with other pages should be
    /// detected and cause validation to fail.
    /// </summary>
    public sealed class PageUniqueSlugValidatorTests
    {
        /// <summary>
        /// Verifies that when creating a page, attempting to reuse a slug that
        /// already exists for the same culture results in a validation
        /// failure.  The test seeds a translation with culture "de-DE" and
        /// slug "about-us", then validates a new page with an identical
        /// translation.  The expected result is that validation fails (IsValid == false).
        /// </summary>
        [Fact]
        public async Task Create_Should_Fail_When_Duplicate_Slug_Per_Culture()
        {
            // Arrange: set up a new in-memory context and seed an existing page
            // translation.  We only need to populate the fields used by the
            // validator (Culture and Slug).  Other properties such as Title
            // are optional for this test.
            var ctx = TestDbFactory.Create();
            await ctx.Set<PageTranslation>().AddAsync(new PageTranslation
            {
                Id = Guid.NewGuid(),
                PageId = Guid.NewGuid(),
                Culture = "de-DE",
                Slug = "about-us",
                Title = "Existing"
            });
            await ctx.SaveChangesAsync();

            // Create a new DTO with a duplicate slug for the same culture
            var dto = new PageCreateDto
            {
                Translations =
                {
                    new PageTranslationDto
                    {
                        Culture = "de-DE",
                        Slug = "about-us",
                        Title = "New"
                    }
                }
            };

            var sut = new PageCreateUniqueSlugValidator(ctx);

            // Act
            var result = await sut.ValidateAsync(dto, CancellationToken.None);

            // Assert: validation should fail due to duplicate slug
            result.IsValid.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that creating a page with a slug that does not yet exist in the
        /// same culture succeeds.  We seed a translation for culture "de-DE" with
        /// slug "about-us" and then attempt to create another page with slug
        /// "company".  Because the slug is unique, the validator should accept
        /// the DTO and IsValid should be true.
        /// </summary>
        [Fact]
        public async Task Create_Should_Pass_When_Slug_Unique_Per_Culture()
        {
            var ctx = TestDbFactory.Create();
            // Seed a page with slug "about-us" in culture "de-DE"
            await ctx.Set<PageTranslation>().AddAsync(new PageTranslation
            {
                Id = Guid.NewGuid(),
                PageId = Guid.NewGuid(),
                Culture = "de-DE",
                Slug = "about-us",
                Title = "About Us"
            });
            await ctx.SaveChangesAsync();

            // Create a new page DTO with a different slug in the same culture
            var dto = new PageCreateDto
            {
                Translations =
                {
                    new PageTranslationDto
                    {
                        Culture = "de-DE",
                        Slug = "company",
                        Title = "Company"
                    }
                }
            };
            var sut = new PageCreateUniqueSlugValidator(ctx);
            var result = await sut.ValidateAsync(dto, CancellationToken.None);
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Ensures that editing a page does not fail when the slug is
        /// unchanged for the same page.  We seed a translation for a page
        /// with culture "en-US" and slug "contact".  When validating an
        /// edit DTO with the same page identifier and unchanged slug, the
        /// validator should allow it (IsValid == true).
        /// </summary>
        [Fact]
        public async Task Edit_Should_Pass_When_Slug_Unchanged_For_Same_Page()
        {
            var ctx = TestDbFactory.Create();
            var pageId = Guid.NewGuid();
            await ctx.Set<PageTranslation>().AddAsync(new PageTranslation
            {
                Id = Guid.NewGuid(),
                PageId = pageId,
                Culture = "en-US",
                Slug = "contact",
                Title = "Contact"
            });
            await ctx.SaveChangesAsync();

            var dto = new PageEditDto
            {
                Id = pageId,
                Translations =
                {
                    new PageTranslationDto
                    {
                        Culture = "en-US",
                        Slug = "contact",
                        Title = "Updated Contact"
                    }
                }
            };
            var sut = new PageEditUniqueSlugValidator(ctx);
            var result = await sut.ValidateAsync(dto, CancellationToken.None);
            result.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Tests that editing a page fails when changing the slug to one that
        /// conflicts with another page's translation in the same culture.
        /// This seeds two pages with distinct identifiers and initial slugs,
        /// then attempts to change the second page's slug to match the
        /// first.  The validator should catch the conflict and return
        /// invalid.
        /// </summary>
        [Fact]
        public async Task Edit_Should_Fail_When_Slug_Duplicated_By_Other_Page()
        {
            var ctx = TestDbFactory.Create();
            var pageId1 = Guid.NewGuid();
            var pageId2 = Guid.NewGuid();

            // Seed two pages with distinct slugs
            await ctx.Set<PageTranslation>().AddRangeAsync(
                new PageTranslation
                {
                    Id = Guid.NewGuid(),
                    PageId = pageId1,
                    Culture = "en-US",
                    Slug = "about",
                    Title = "About"
                },
                new PageTranslation
                {
                    Id = Guid.NewGuid(),
                    PageId = pageId2,
                    Culture = "en-US",
                    Slug = "faq",
                    Title = "FAQ"
                }
            );
            await ctx.SaveChangesAsync();

            // Attempt to change the second page's slug to "about", creating a conflict
            var dto = new PageEditDto
            {
                Id = pageId2,
                Translations =
                {
                    new PageTranslationDto
                    {
                        Culture = "en-US",
                        Slug = "about",
                        Title = "FAQ Updated"
                    }
                }
            };
            var sut = new PageEditUniqueSlugValidator(ctx);
            var result = await sut.ValidateAsync(dto, CancellationToken.None);
            result.IsValid.Should().BeFalse();
        }

        /// <summary>
        /// Ensures that editing a page succeeds when the slug is changed to a
        /// new unique value.  We seed two pages with slugs "about" and
        /// "faq".  The test then changes the second page's slug to
        /// "contact", which does not conflict with any existing slug.  The
        /// validator should accept this change.
        /// </summary>
        [Fact]
        public async Task Edit_Should_Pass_When_Slug_Changed_And_Unique()
        {
            var ctx = TestDbFactory.Create();
            var pageId1 = Guid.NewGuid();
            var pageId2 = Guid.NewGuid();
            await ctx.Set<PageTranslation>().AddRangeAsync(
                new PageTranslation
                {
                    Id = Guid.NewGuid(),
                    PageId = pageId1,
                    Culture = "en-US",
                    Slug = "about",
                    Title = "About"
                },
                new PageTranslation
                {
                    Id = Guid.NewGuid(),
                    PageId = pageId2,
                    Culture = "en-US",
                    Slug = "faq",
                    Title = "FAQ"
                }
            );
            await ctx.SaveChangesAsync();

            // Change second page's slug to a unique value
            var dto = new PageEditDto
            {
                Id = pageId2,
                Translations =
                {
                    new PageTranslationDto
                    {
                        Culture = "en-US",
                        Slug = "contact",
                        Title = "FAQ to Contact"
                    }
                }
            };
            var sut = new PageEditUniqueSlugValidator(ctx);
            var result = await sut.ValidateAsync(dto, CancellationToken.None);
            result.IsValid.Should().BeTrue();
        }
    }
}