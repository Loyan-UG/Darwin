using Darwin.Application.Shipping.Queries;
using Darwin.Domain.Entities.Shipping;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Shipping;

public sealed class ShippingSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public ShippingSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetShippingMethodsPage_Should_HandleEscapedSubstringAndCaseVariants_OnCarrierSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchCarrier = $"dhl_%_probe[{marker}]";
        var unrelatedCarrier = $"dhlXprobe[{marker.Substring(0, 6)}]";

        db.Set<ShippingMethod>().AddRange(
            new ShippingMethod
            {
                Name = "Exact Shipping Method",
                Carrier = exactMatchCarrier,
                Service = "Ground",
                Currency = "EUR",
                IsActive = true
            },
            new ShippingMethod
            {
                Name = "Unrelated Shipping Method",
                Carrier = unrelatedCarrier,
                Service = "Express",
                Currency = "EUR",
                IsActive = true
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetShippingMethodsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"dhl_%_probe[{marker}]",
            filter: Darwin.Domain.Enums.ShippingMethodQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"DHL_%_PROBE[{marker}]",
            filter: Darwin.Domain.Enums.ShippingMethodQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Carrier == exactMatchCarrier);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Carrier == exactMatchCarrier);
    }
}
