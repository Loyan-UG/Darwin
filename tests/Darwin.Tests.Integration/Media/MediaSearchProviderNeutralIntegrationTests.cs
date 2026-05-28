using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Queries;
using Darwin.Domain.Entities.CMS;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Media;

public sealed class MediaSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public MediaSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_HandleEscapedSubstringAndCaseVariants_OnOriginalFileNameSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchFile = $"asset_%_probe[{marker}].jpg";
        var unrelatedFile = $"assetXprobe[{marker.Substring(0, 6)}].jpg";

        db.Set<MediaAsset>().AddRange(
            new MediaAsset
            {
                Url = $"https://cdn.example.test/{exactMatchFile}",
                Alt = "exact match",
                OriginalFileName = exactMatchFile
            },
            new MediaAsset
            {
                Url = $"https://cdn.example.test/{unrelatedFile}",
                Alt = "other",
                OriginalFileName = unrelatedFile
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetMediaAssetsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"asset_%_probe[{marker}].jpg",
            filter: MediaAssetQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"ASSET_%_PROBE[{marker}].JPG",
            filter: MediaAssetQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.OriginalFileName == exactMatchFile);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.OriginalFileName == exactMatchFile);
    }
}
