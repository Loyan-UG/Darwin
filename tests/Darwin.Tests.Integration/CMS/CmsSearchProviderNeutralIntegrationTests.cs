using Darwin.Application.CMS.Queries;
using Darwin.Domain.Entities.CMS;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.CMS;

public sealed class CmsSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public CmsSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetPagesPage_Should_HandleEscapedSubstringAndCaseVariants_OnSlugSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchSlug = $"cms_%_page[{marker}]";
        var unrelatedSlug = $"cms-page[{marker.Substring(0, 6)}]";

        db.Set<Page>().AddRange(
            new Page
            {
                Translations =
                {
                    new PageTranslation
                    {
                        Culture = "en-US",
                        Title = "Exact CMS page",
                        Slug = exactMatchSlug,
                        ContentHtml = "<p>Exact</p>"
                    }
                }
            },
            new Page
            {
                Translations =
                {
                    new PageTranslation
                    {
                        Culture = "en-US",
                        Title = "Other CMS page",
                        Slug = unrelatedSlug,
                        ContentHtml = "<p>Other</p>"
                    }
                }
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetPagesPageHandler>();

        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            culture: "en-US",
            query: exactMatchSlug,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            culture: "en-US",
            query: exactMatchSlug.ToUpperInvariant(),
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Slug == exactMatchSlug);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Slug == exactMatchSlug);
    }
}

