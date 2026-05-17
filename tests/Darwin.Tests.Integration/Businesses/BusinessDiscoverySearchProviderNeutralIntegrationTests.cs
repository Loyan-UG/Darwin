using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Businesses;

public sealed class BusinessDiscoverySearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public BusinessDiscoverySearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetBusinessesForDiscovery_Should_HandleEscapedSubstringAndCaseVariants_OnNameSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchName = $"business_%_probe[{marker}]";
        var unrelatedName = $"businessXprobe[{marker.Substring(0, 6)}]";

        db.Set<Business>().AddRange(
            new Business
            {
                Name = exactMatchName,
                IsActive = true,
                OperationalStatus = BusinessOperationalStatus.Approved
            },
            new Business
            {
                Name = unrelatedName,
                IsActive = true,
                OperationalStatus = BusinessOperationalStatus.Approved
            });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetBusinessesForDiscoveryHandler>();

        var lowerCaseResult = await handler.HandleAsync(new BusinessDiscoveryRequestDto
        {
            Page = 1,
            PageSize = 20,
            Query = exactMatchName,
            Culture = "en-US"
        }, TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(new BusinessDiscoveryRequestDto
        {
            Page = 1,
            PageSize = 20,
            Query = exactMatchName.ToUpperInvariant(),
            Culture = "en-US"
        }, TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Name == exactMatchName);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Name == exactMatchName);
    }
}

