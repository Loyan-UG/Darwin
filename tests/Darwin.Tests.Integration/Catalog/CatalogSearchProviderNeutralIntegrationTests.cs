using Darwin.Application.Catalog.Queries;
using Darwin.Domain.Entities.Catalog;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Catalog;

public sealed class CatalogSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public CatalogSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetVariantsPage_Should_HandleEscapedSubstringAndCaseVariants_OnSkuSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchSku = $"catalog_%_probe[{marker}]_SKU";
        var unrelatedSku = $"catalogXprobe[{marker.Substring(0, 6)}]_SKU";
        var product = new Product();

        db.Set<Product>().Add(product);
        db.Set<ProductVariant>().AddRange(
            new ProductVariant
            {
                ProductId = product.Id,
                Sku = exactMatchSku,
                Currency = "EUR",
                BasePriceNetMinor = 1000,
                TaxCategoryId = Guid.NewGuid()
            },
            new ProductVariant
            {
                ProductId = product.Id,
                Sku = unrelatedSku,
                Currency = "EUR",
                BasePriceNetMinor = 1200,
                TaxCategoryId = Guid.NewGuid()
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetVariantsPageHandler>();

        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: exactMatchSku,
            culture: "en-US",
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: exactMatchSku.ToUpperInvariant(),
            culture: "en-US",
            ct: TestContext.Current.CancellationToken);

        var exactMatch = db.Set<ProductVariant>().Single(v => v.Sku == exactMatchSku);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatch.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatch.Id);
    }

    [Fact]
    public async Task GetAddOnGroupsPage_Should_HandleEscapedSubstringAndCaseVariants_OnNameSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchName = $"addon_%_group[{marker}]_probe";
        var unrelatedName = $"addonXgroup[{marker.Substring(0, 6)}]_probe";

        var exactMatchGroup = new AddOnGroup { Name = exactMatchName, Currency = "EUR" };
        var unrelatedGroup = new AddOnGroup { Name = unrelatedName, Currency = "EUR" };

        db.Set<AddOnGroup>().AddRange(exactMatchGroup, unrelatedGroup);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetAddOnGroupsPageHandler>();

        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            q: exactMatchName,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            q: exactMatchName.ToUpperInvariant(),
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchGroup.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchGroup.Id);
    }
}
