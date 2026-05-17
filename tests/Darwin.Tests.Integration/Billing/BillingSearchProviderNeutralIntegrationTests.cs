using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Billing;

public sealed class BillingSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public BillingSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetBillingPlansAdminPage_Should_HandleEscapedSubstringAndCaseVariants_OnCodeSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchCode = $"plan_%_probe[{marker}]";
        var unrelatedCode = $"planXprobe[{marker.Substring(0, 6)}]";

        db.Set<BillingPlan>().AddRange(
            new BillingPlan
            {
                Code = exactMatchCode,
                Name = "Exact Billing Plan",
                PriceMinor = 10_00,
                IsActive = true,
                FeaturesJson = "{}"
            },
            new BillingPlan
            {
                Code = unrelatedCode,
                Name = "Other Billing Plan",
                PriceMinor = 20_00,
                IsActive = true,
                FeaturesJson = "{}"
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetBillingPlansAdminPageHandler>();

        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: exactMatchCode,
            filter: BillingPlanQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: exactMatchCode.ToUpperInvariant(),
            filter: BillingPlanQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Code == exactMatchCode);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Code == exactMatchCode);
    }
}

