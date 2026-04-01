using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Application.CMS.Validators;
using Darwin.Application;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Darwin.Tests.Unit.CMS
{
    public sealed class MenuValidationAndPublicQueryTests
    {
        private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
        {
            public LocalizedString this[string name] => new(name, name, false);
            public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments), false);
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();
            public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/catalog")]
        [InlineData("https://example.com/help")]
        public void MenuValidator_Should_Accept_Rooted_Or_HttpUrls(string url)
        {
            var dto = new MenuItemDto
            {
                Url = url,
                SortOrder = 0,
                Translations = new List<MenuItemTranslationDto>
                {
                    new() { Culture = "en-US", Label = "Link" }
                }
            };

            var sut = new MenuItemDtoValidator(new TestStringLocalizer());
            var result = sut.Validate(dto);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("catalog")]
        [InlineData("//broken")]
        [InlineData("javascript:alert(1)")]
        public void MenuValidator_Should_Reject_Invalid_PublicNavigation_Urls(string url)
        {
            var dto = new MenuItemDto
            {
                Url = url,
                SortOrder = 0,
                Translations = new List<MenuItemTranslationDto>
                {
                    new() { Culture = "en-US", Label = "Link" }
                }
            };

            var sut = new MenuItemDtoValidator(new TestStringLocalizer());
            var result = sut.Validate(dto);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task PublicMenuQuery_Should_Return_MainNavigation_Using_Exact_Name_And_Active_Items()
        {
            var ctx = TestDbFactory.Create();
            var menuId = Guid.NewGuid();
            var activeItemId = Guid.NewGuid();

            await ctx.Set<Menu>().AddAsync(new Menu
            {
                Id = menuId,
                Name = "main-navigation",
                Culture = "en-US",
                Items = new List<MenuItem>
                {
                    new()
                    {
                        Id = activeItemId,
                        MenuId = menuId,
                        Url = "/catalog",
                        Title = "Catalog",
                        SortOrder = 1,
                        IsActive = true,
                        Translations = new List<MenuItemTranslation>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                MenuItemId = activeItemId,
                                Culture = "en-US",
                                Label = "Catalog"
                            }
                        }
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MenuId = menuId,
                        Url = "/hidden",
                        Title = "Hidden",
                        SortOrder = 2,
                        IsActive = false,
                        Translations = new List<MenuItemTranslation>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                Culture = "en-US",
                                Label = "Hidden"
                            }
                        }
                    }
                }
            }, TestContext.Current.CancellationToken);
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

            var sut = new GetPublicMenuByNameHandler(ctx);
            var result = await sut.HandleAsync("main-navigation", "en-US", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!.Name.Should().Be("main-navigation");
            result.Items.Should().ContainSingle();
            result.Items[0].Url.Should().Be("/catalog");
            result.Items[0].Label.Should().Be("Catalog");
        }

        [Fact]
        public async Task PublishedPageQuery_Should_Return_Only_Published_Page_By_Slug()
        {
            var ctx = TestDbFactory.Create();
            var publishedId = Guid.NewGuid();

            await ctx.Set<Page>().AddRangeAsync(
                new[]
                {
                    new Page
                    {
                        Id = publishedId,
                        Title = "Ueber uns",
                        Slug = "ueber-uns",
                        ContentHtml = "<h1>Ueber uns</h1>",
                        MetaTitle = "Ueber uns",
                        MetaDescription = "Ueber uns - Informationen & Details.",
                        IsPublished = true,
                        Status = PageStatus.Published,
                        PublishStartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        Translations = new List<PageTranslation>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                PageId = publishedId,
                                Culture = "de-DE",
                                Title = "Ueber uns",
                                Slug = "ueber-uns",
                                ContentHtml = "<h1>Ueber uns</h1>",
                                MetaTitle = "Ueber uns",
                                MetaDescription = "Ueber uns - Informationen & Details."
                            }
                        }
                    },
                    new Page
                    {
                        Id = Guid.NewGuid(),
                        Title = "FAQ",
                        Slug = "faq",
                        ContentHtml = "<h1>FAQ</h1>",
                        IsPublished = false,
                        Status = PageStatus.Draft,
                        Translations = new List<PageTranslation>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                Culture = "de-DE",
                                Title = "FAQ",
                                Slug = "faq",
                                ContentHtml = "<h1>FAQ</h1>"
                            }
                        }
                    }
                },
                TestContext.Current.CancellationToken);
            await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

            var sut = new GetPublishedPageBySlugHandler(ctx);
            var result = await sut.HandleAsync("ueber-uns", "de-DE", TestContext.Current.CancellationToken);
            var draftResult = await sut.HandleAsync("faq", "de-DE", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!.Slug.Should().Be("ueber-uns");
            draftResult.Should().BeNull();
        }
    }
}
