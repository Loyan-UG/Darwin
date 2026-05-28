using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.Domain.Entities.CRM;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.CRM;

public sealed class CRMSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public CRMSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetCustomersPage_Should_HandleEscapedSubstringAndCaseVariants_OnFirstNameSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchName = $"customer_%_probe[{marker}]_name";
        var unrelatedName = $"customerXprobe[{marker.Substring(0, 6)}]_name";

        var exactMatchCustomer = new Customer { FirstName = exactMatchName, LastName = "Probe" };
        var unrelatedCustomer = new Customer { FirstName = unrelatedName, LastName = "Other" };

        db.Set<Customer>().AddRange(exactMatchCustomer, unrelatedCustomer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetCustomersPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"customer_%_probe[{marker}]",
            filter: CustomerQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"CUSTOMER_%_PROBE[{marker}]",
            filter: CustomerQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchCustomer.Id);
        lowerCaseResult.Items.Should().NotContain(x => x.Id == unrelatedCustomer.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchCustomer.Id);
        upperCaseResult.Items.Should().NotContain(x => x.Id == unrelatedCustomer.Id);
    }

    [Fact]
    public async Task GetLeadsPage_Should_HandleEscapedSubstringAndCaseVariants_OnEmailSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchEmail = $"lead_%_probe[{marker}]@example.test";
        var unrelatedEmail = $"leadprobe[{marker.Substring(0, 6)}]@example.test";

        var exactMatchLead = new Lead { FirstName = "Alpha", LastName = "Lead", Email = exactMatchEmail };
        var unrelatedLead = new Lead { FirstName = "Beta", LastName = "Lead", Email = unrelatedEmail };

        db.Set<Lead>().AddRange(exactMatchLead, unrelatedLead);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetLeadsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"lead_%_probe[{marker}]",
            filter: LeadQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"LEAD_%_PROBE[{marker}]",
            filter: LeadQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchLead.Id);
        lowerCaseResult.Items.Should().NotContain(x => x.Id == unrelatedLead.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchLead.Id);
        upperCaseResult.Items.Should().NotContain(x => x.Id == unrelatedLead.Id);
    }

    [Fact]
    public async Task GetOpportunitiesPage_Should_HandleEscapedSubstringAndCaseVariants_OnTitleSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchTitle = $"deal_%_probe[{marker}]";
        var unrelatedTitle = $"dealprobe[{marker.Substring(0, 6)}]";

        var customer = new Customer { FirstName = "Customer", LastName = "One" };
        db.Set<Customer>().Add(customer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exactMatchOpportunity = new Opportunity { CustomerId = customer.Id, Title = exactMatchTitle };
        var unrelatedOpportunity = new Opportunity { CustomerId = customer.Id, Title = unrelatedTitle };

        db.Set<Opportunity>().AddRange(exactMatchOpportunity, unrelatedOpportunity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetOpportunitiesPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"deal_%_probe[{marker}]",
            filter: OpportunityQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"DEAL_%_PROBE[{marker}]",
            filter: OpportunityQueueFilter.All,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchOpportunity.Id);
        lowerCaseResult.Items.Should().NotContain(x => x.Id == unrelatedOpportunity.Id);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Id == exactMatchOpportunity.Id);
        upperCaseResult.Items.Should().NotContain(x => x.Id == unrelatedOpportunity.Id);
    }
}
