using System.Net;
using System.Net.Http.Json;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Identity;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Common.TestInfrastructure;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Businesses;

/// <summary>
///     Provides hosted API smoke coverage for onboarding contracts that Darwin.Web and mobile clients rely on.
/// </summary>
public sealed class BusinessOnboardingApiSmokeTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    ///     Initializes the shared hosted API test fixture.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public BusinessOnboardingApiSmokeTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    ///     Verifies that an account created through public registration cannot obtain an API token
    ///     before the email-confirmation flow is completed.
    /// </summary>
    [Fact]
    public async Task ApiPasswordLogin_ShouldRejectRegisteredUser_WhenEmailIsNotConfirmed()
    {
        using var client = CreateHttpsClient();
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"unconfirmed-{suffix}@example.test";
        const string password = "P@ssw0rd!Aa1";

        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(
            client,
            "Unconfirmed",
            "Member",
            email,
            password,
            TestContext.Current.CancellationToken);

        await AssertUserEmailConfirmedAsync(email, expected: false, TestContext.Current.CancellationToken);

        using var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new PasswordLoginRequest
            {
                Email = email,
                Password = password,
                DeviceId = $"device-{suffix}"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await loginResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    ///     Verifies that public business discovery and detail endpoints only expose businesses that
    ///     are both active and operator-approved.
    /// </summary>
    [Fact]
    public async Task PublicBusinessEndpoints_ShouldExposeOnlyApprovedActiveBusinesses()
    {
        using var client = CreateHttpsClient();
        var seed = await SeedBusinessVisibilityCasesAsync(TestContext.Current.CancellationToken);

        using var listResponse = await client.PostAsJsonAsync(
            "/api/v1/public/businesses/list",
            new BusinessListRequest
            {
                Query = seed.QueryPrefix,
                Page = 1,
                PageSize = 20,
                Culture = "en-US"
            },
            cancellationToken: TestContext.Current.CancellationToken);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<PagedResponse<BusinessSummary>>(
            cancellationToken: TestContext.Current.CancellationToken);
        list.Should().NotBeNull();
        list!.Items.Should().ContainSingle(item => item.Id == seed.ApprovedActiveBusinessId);
        list.Items.Should().NotContain(item => seed.HiddenBusinessIds.Contains(item.Id));

        using var approvedDetailResponse = await client.GetAsync(
            $"/api/v1/public/businesses/{seed.ApprovedActiveBusinessId:D}?culture=en-US",
            TestContext.Current.CancellationToken);
        approvedDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await approvedDetailResponse.Content.ReadFromJsonAsync<BusinessDetail>(
            cancellationToken: TestContext.Current.CancellationToken);
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(seed.ApprovedActiveBusinessId);

        foreach (var hiddenBusinessId in seed.HiddenBusinessIds)
        {
            using var hiddenDetailResponse = await client.GetAsync(
                $"/api/v1/public/businesses/{hiddenBusinessId:D}?culture=en-US",
                TestContext.Current.CancellationToken);
            hiddenDetailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    private async Task AssertUserEmailConfirmedAsync(string email, bool expected, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();
        var normalizedEmail = email.Trim().ToUpperInvariant();

        var confirmed = await db.Set<Darwin.Domain.Entities.Identity.User>()
            .Where(user => user.NormalizedEmail == normalizedEmail && !user.IsDeleted)
            .Select(user => user.EmailConfirmed)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        confirmed.Should().Be(expected);
    }

    private async Task<BusinessVisibilitySeed> SeedBusinessVisibilityCasesAsync(CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var queryPrefix = $"Public Visibility Smoke {suffix}";

        var approvedActive = CreateBusiness($"{queryPrefix} Approved", isActive: true, BusinessOperationalStatus.Approved);
        var pendingActive = CreateBusiness($"{queryPrefix} Pending", isActive: true, BusinessOperationalStatus.PendingApproval);
        var approvedInactive = CreateBusiness($"{queryPrefix} Inactive", isActive: false, BusinessOperationalStatus.Approved);
        var suspendedActive = CreateBusiness($"{queryPrefix} Suspended", isActive: true, BusinessOperationalStatus.Suspended);

        db.Set<Business>().AddRange(approvedActive, pendingActive, approvedInactive, suspendedActive);
        db.Set<Darwin.Domain.Entities.Businesses.BusinessLocation>().Add(new Darwin.Domain.Entities.Businesses.BusinessLocation
        {
            BusinessId = approvedActive.Id,
            Name = $"{queryPrefix} Main",
            AddressLine1 = "Smoke Street 1",
            City = "Berlin",
            CountryCode = "DE",
            PostalCode = "10115",
            IsPrimary = true
        });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new BusinessVisibilitySeed(
            queryPrefix,
            approvedActive.Id,
            new HashSet<Guid> { pendingActive.Id, approvedInactive.Id, suspendedActive.Id });
    }

    private static Business CreateBusiness(string name, bool isActive, BusinessOperationalStatus status)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            LegalName = $"{name} GmbH",
            ContactEmail = $"{name.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant()}@example.test",
            Category = BusinessCategoryKind.Services,
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "Europe/Berlin",
            IsActive = isActive,
            OperationalStatus = status,
            ApprovedAtUtc = status == BusinessOperationalStatus.Approved ? DateTime.UtcNow : null,
            SuspendedAtUtc = status == BusinessOperationalStatus.Suspended ? DateTime.UtcNow : null
        };

    private sealed record BusinessVisibilitySeed(
        string QueryPrefix,
        Guid ApprovedActiveBusinessId,
        IReadOnlySet<Guid> HiddenBusinessIds);
}
