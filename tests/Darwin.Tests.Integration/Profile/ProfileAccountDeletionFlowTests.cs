using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Profile;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Tests.Common.TestInfrastructure;
using Darwin.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.Tests.Integration.Profile;

/// <summary>
/// Provides integration coverage for the authenticated consumer account deletion request flow.
/// </summary>
public sealed class ProfileAccountDeletionFlowTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    /// Initializes the shared integration-test fixture.
    /// </summary>
    /// <param name="factory">Shared web application factory.</param>
    public ProfileAccountDeletionFlowTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    /// Verifies that an authenticated current user can request deletion, becomes inactive/anonymized,
    /// cannot login again with the original credentials, and still preserves related rows.
    /// </summary>
    [Fact]
    public async Task RequestAccountDeletion_Should_DeactivateAndAnonymizeCurrentUser_WhilePreservingRelatedRows()
    {
        using var client = CreateHttpsClient();
        var credentials = CreateUniqueCredentials();

        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(
            client,
            "Delete",
            "Candidate",
            credentials.Email,
            credentials.Password,
            TestContext.Current.CancellationToken);

        var token = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            client,
            credentials.Email,
            credentials.Password,
            credentials.DeviceId,
            TestContext.Current.CancellationToken);

        await SeedRelatedRowsAsync(token.UserId, TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var deleteResponse = await client.PostAsJsonAsync(
            "/api/v1/profile/me/deletion-request",
            new RequestAccountDeletionRequest(true),
            cancellationToken: TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AssertUserStateAsync(token.UserId, credentials.Email, TestContext.Current.CancellationToken);

        using var loginAfterDeletionResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new PasswordLoginRequest
            {
                Email = credentials.Email,
                Password = credentials.Password,
                DeviceId = credentials.DeviceId
            },
            cancellationToken: TestContext.Current.CancellationToken);

        loginAfterDeletionResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var refreshAfterDeletionResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenRequest
            {
                RefreshToken = token.RefreshToken,
                DeviceId = credentials.DeviceId
            },
            cancellationToken: TestContext.Current.CancellationToken);

        refreshAfterDeletionResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Verifies that the deletion endpoint rejects callers that do not provide explicit irreversible confirmation.
    /// </summary>
    [Fact]
    public async Task RequestAccountDeletion_Should_RejectRequest_WhenExplicitConfirmationIsMissing()
    {
        using var client = CreateHttpsClient();
        var credentials = CreateUniqueCredentials();

        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(
            client,
            "Delete",
            "Candidate",
            credentials.Email,
            credentials.Password,
            TestContext.Current.CancellationToken);

        var token = await IdentityFlowTestHelper.LoginExpectSuccessAsync(
            client,
            credentials.Email,
            credentials.Password,
            credentials.DeviceId,
            TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        using var response = await client.PostAsJsonAsync(
            "/api/v1/profile/me/deletion-request",
            new RequestAccountDeletionRequest(false),
            cancellationToken: TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);
        problem.Should().NotBeNull();
        problem!.Title.Should().NotBeNullOrWhiteSpace();
        problem.Title!.Should().ContainEquivalentOf("confirmation");
    }

    /// <summary>
    /// Seeds directly-related rows so the test can verify referential integrity after anonymization.
    /// </summary>
    private async Task SeedRelatedRowsAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var businessId = await db.Set<Business>()
            .Where(x => !x.IsDeleted)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        db.Set<Address>().Add(new Address
        {
            UserId = userId,
            FullName = "Delete Candidate",
            Company = "Darwin",
            Street1 = "Example Street 1",
            PostalCode = "12345",
            City = "Berlin",
            CountryCode = "DE",
            PhoneE164 = "+491701234567",
            IsDefaultBilling = true,
            IsDefaultShipping = true
        });

        db.Set<BusinessFavorite>().Add(new BusinessFavorite(userId, businessId));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asserts that the deleted account remained in the database but had direct personal fields anonymized.
    /// </summary>
    private async Task AssertUserStateAsync(Guid userId, string originalEmail, CancellationToken cancellationToken)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DarwinDbContext>();

        var user = await db.Set<User>()
            .SingleAsync(x => x.Id == userId, cancellationToken)
            .ConfigureAwait(false);

        user.IsDeleted.Should().BeFalse();
        user.IsActive.Should().BeFalse();
        user.Email.Should().NotBe(originalEmail);
        user.Email.Should().Contain($"deleted-user-{userId:N}");
        user.UserName.Should().Be(user.Email);
        user.FirstName.Should().Be("Deleted");
        user.LastName.Should().Be("User");
        user.PhoneE164.Should().BeNull();
        user.Company.Should().BeNull();
        user.VatId.Should().BeNull();
        user.MarketingConsent.Should().BeFalse();
        user.ChannelsOptInJson.Should().Be("{}");

        var address = await db.Set<Address>()
            .SingleAsync(x => x.UserId == userId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        address.FullName.Should().Be("Deleted User");
        address.Street1.Should().Be("Deleted");
        address.City.Should().Be("Deleted");
        address.PostalCode.Should().Be("00000");
        address.PhoneE164.Should().BeNull();

        var favorite = await db.Set<BusinessFavorite>()
            .SingleAsync(x => x.UserId == userId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        favorite.UserId.Should().Be(userId);

        var activeRefreshTokens = await db.Set<UserToken>()
            .CountAsync(x => x.UserId == userId && !x.IsDeleted && x.UsedAtUtc == null, cancellationToken)
            .ConfigureAwait(false);

        activeRefreshTokens.Should().Be(0);

        var activeDevices = await db.Set<UserDevice>()
            .CountAsync(x => x.UserId == userId && !x.IsDeleted && x.IsActive, cancellationToken)
            .ConfigureAwait(false);

        activeDevices.Should().Be(0);
    }

    /// <summary>
    /// Creates unique credentials for each test run.
    /// </summary>
    private static TestCredentials CreateUniqueCredentials()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new TestCredentials(
            Email: $"deletion-{suffix}@example.test",
            Password: "P@ssw0rd!Aa1",
            DeviceId: $"deletion-device-{suffix}");
    }

    /// <summary>
    /// Encapsulates per-test credentials used by the deletion flow suite.
    /// </summary>
    private sealed record TestCredentials(string Email, string Password, string DeviceId);
}
