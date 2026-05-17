using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Businesses;

public sealed class BusinessCommunicationSearchProviderNeutralIntegrationTests
    : DeterministicIntegrationTestBase,
      IClassFixture<WebApplicationFactory<Program>>
{
    public BusinessCommunicationSearchProviderNeutralIntegrationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetBusinessCommunicationSetupPage_Should_HandleEscapedSubstringAndCaseVariants_OnNameSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchName = $"comm_%_probe[{marker}]";
        var unrelatedName = $"commXprobe[{marker.Substring(0, 6)}]";

        db.Set<Business>().AddRange(
            new Business
            {
                Name = exactMatchName,
                OperationalStatus = BusinessOperationalStatus.Approved,
                IsActive = true,
                CustomerEmailNotificationsEnabled = true,
                CustomerMarketingEmailsEnabled = true,
                OperationalAlertEmailsEnabled = true
            },
            new Business
            {
                Name = unrelatedName,
                OperationalStatus = BusinessOperationalStatus.Approved,
                IsActive = true,
                CustomerEmailNotificationsEnabled = false,
                CustomerMarketingEmailsEnabled = false,
                OperationalAlertEmailsEnabled = false
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetBusinessCommunicationSetupPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"comm_%_probe[{marker}]",
            setupOnly: true,
            filter: BusinessCommunicationSetupFilter.NeedsSetup,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"COMM_%_PROBE[{marker}]",
            setupOnly: true,
            filter: BusinessCommunicationSetupFilter.NeedsSetup,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Name == exactMatchName);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Name == exactMatchName);
    }

    [Fact]
    public async Task GetEmailDispatchAuditsPage_Should_HandleEscapedSubstringAndCaseVariants_OnRecipientEmailSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchRecipient = $"ops_%_probe[{marker}]@example.test";
        var unrelatedRecipient = $"opsXprobe[{marker.Substring(0, 6)}]@example.test";

        db.Set<EmailDispatchAudit>().AddRange(
            new EmailDispatchAudit
            {
                RecipientEmail = exactMatchRecipient,
                IntendedRecipientEmail = exactMatchRecipient,
                Subject = "Exact dispatch",
                FlowKey = "OrderPlaced",
                Status = "Pending",
                AttemptedAtUtc = DateTime.UtcNow
            },
            new EmailDispatchAudit
            {
                RecipientEmail = unrelatedRecipient,
                IntendedRecipientEmail = unrelatedRecipient,
                Subject = "Other dispatch",
                FlowKey = "OrderPlaced",
                Status = "Pending",
                AttemptedAtUtc = DateTime.UtcNow
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetEmailDispatchAuditsPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"ops_%_probe[{marker}]@example.test",
            recipientEmail: null,
            status: null,
            flowKey: null,
            stalePendingOnly: false,
            businessLinkedFailuresOnly: false,
            repeatedFailuresOnly: false,
            priorSuccessOnly: false,
            retryReadyOnly: false,
            retryBlockedOnly: false,
            highChainVolumeOnly: false,
            chainFollowUpOnly: false,
            chainResolvedOnly: false,
            businessId: null,
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            query: $"OPS_%_PROBE[{marker}]@EXAMPLE.TEST",
            recipientEmail: null,
            status: null,
            flowKey: null,
            stalePendingOnly: false,
            businessLinkedFailuresOnly: false,
            repeatedFailuresOnly: false,
            priorSuccessOnly: false,
            retryReadyOnly: false,
            retryBlockedOnly: false,
            highChainVolumeOnly: false,
            chainFollowUpOnly: false,
            chainResolvedOnly: false,
            businessId: null,
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.RecipientEmail == exactMatchRecipient);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.RecipientEmail == exactMatchRecipient);
    }

    [Fact]
    public async Task GetProviderCallbackInboxPage_Should_HandleEscapedSubstringAndCaseVariants_OnProviderSearch()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var marker = Guid.NewGuid().ToString("N");
        var exactMatchProvider = $"provider_%_probe[{marker}]";
        var unrelatedProvider = $"providerXprobe[{marker.Substring(0, 6)}]";

        db.Set<ProviderCallbackInboxMessage>().AddRange(
            new ProviderCallbackInboxMessage
            {
                Provider = exactMatchProvider,
                CallbackType = "event",
                PayloadJson = "{}"
            },
            new ProviderCallbackInboxMessage
            {
                Provider = unrelatedProvider,
                CallbackType = "event",
                PayloadJson = "{}"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = scope.ServiceProvider.GetRequiredService<GetProviderCallbackInboxPageHandler>();
        var lowerCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            filter: new ProviderCallbackInboxFilterDto { Query = $"provider_%_probe[{marker}]" },
            ct: TestContext.Current.CancellationToken);

        var upperCaseResult = await handler.HandleAsync(
            page: 1,
            pageSize: 20,
            filter: new ProviderCallbackInboxFilterDto { Query = $"PROVIDER_%_PROBE[{marker}]" },
            ct: TestContext.Current.CancellationToken);

        lowerCaseResult.Total.Should().Be(1);
        lowerCaseResult.Items.Should().ContainSingle(x => x.Provider == exactMatchProvider);

        upperCaseResult.Total.Should().Be(1);
        upperCaseResult.Items.Should().ContainSingle(x => x.Provider == exactMatchProvider);
    }
}
