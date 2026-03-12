using Darwin.Contracts.Common;
using Darwin.Contracts.Profile;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Darwin.Tests.Common.TestInfrastructure;
using Darwin.Tests.Integration.TestInfrastructure;

namespace Darwin.Tests.Integration.Profile;

/// <summary>
///     Provides authorized integration coverage for profile read/update endpoints,
///     including optimistic concurrency behavior based on RowVersion.
/// </summary>
public sealed class ProfileEndpointAuthorizedConcurrencyTests : DeterministicIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    /// <summary>
    ///     Initializes the test fixture with a host configured for Testing environment.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public ProfileEndpointAuthorizedConcurrencyTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    /// <summary>
    ///     Verifies that an authorized caller can fetch the current profile and receive
    ///     the optimistic concurrency token required for update operations.
    /// </summary>
    [Fact]
    public async Task GetMe_Should_ReturnCurrentProfile_WithRowVersion_WhenAuthorized()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "ProfileTester", credentials.Email, credentials.Password);
        var token = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, credentials.DeviceId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        // Act
        using var response = await client.GetAsync("/api/v1/profile/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<CustomerProfile>();
        profile.Should().NotBeNull();
        profile!.Id.Should().NotBe(Guid.Empty);
        profile.Email.Should().Be(credentials.Email);
        profile.RowVersion.Should().NotBeNull();
        profile.RowVersion!.Length.Should().BeGreaterThan(0);
    }

    /// <summary>
    ///     Verifies that authorized update succeeds when request includes the exact Id and
    ///     fresh RowVersion received from the previous profile read.
    /// </summary>
    [Fact]
    public async Task UpdateMe_Should_ReturnNoContent_WhenIdAndRowVersionAreValid()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "ProfileTester", credentials.Email, credentials.Password);
        var token = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, credentials.DeviceId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var currentProfile = await GetCurrentProfileAsync(client);
        var updateRequest = new CustomerProfile
        {
            Id = currentProfile.Id,
            Email = currentProfile.Email,
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            PhoneE164 = "+491701234567",
            Locale = "de-DE",
            Timezone = "Europe/Berlin",
            Currency = "EUR",
            RowVersion = currentProfile.RowVersion
        };

        // Act
        using var updateResponse = await client.PutAsJsonAsync("/api/v1/profile/me", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Fetch profile again and assert persisted fields changed and row-version rotated.
        var updatedProfile = await GetCurrentProfileAsync(client);
        updatedProfile.FirstName.Should().Be("UpdatedFirst");
        updatedProfile.LastName.Should().Be("UpdatedLast");
        updatedProfile.PhoneE164.Should().Be("+491701234567");
        updatedProfile.Locale.Should().Be("de-DE");
        updatedProfile.Timezone.Should().Be("Europe/Berlin");
        updatedProfile.Currency.Should().Be("EUR");
        updatedProfile.RowVersion.Should().NotBeNull();
        updatedProfile.RowVersion!.Should().NotEqual(currentProfile.RowVersion!);
    }

    /// <summary>
    ///     Verifies that update fails with a deterministic problem payload when caller uses
    ///     a stale RowVersion token after a previous successful mutation.
    /// </summary>
    [Fact]
    public async Task UpdateMe_Should_ReturnBadRequestProblem_WhenRowVersionIsStale()
    {
        // Arrange
        using var client = CreateHttpsClient();
        var credentials = CreateUniqueCredentials();
        await IdentityFlowTestHelper.RegisterExpectSuccessAsync(client, "Integration", "ProfileTester", credentials.Email, credentials.Password);
        var token = await IdentityFlowTestHelper.LoginExpectSuccessAsync(client, credentials.Email, credentials.Password, credentials.DeviceId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var initialProfile = await GetCurrentProfileAsync(client);

        // First update consumes the initial row-version and rotates it.
        using var firstUpdateResponse = await client.PutAsJsonAsync("/api/v1/profile/me", new CustomerProfile
        {
            Id = initialProfile.Id,
            Email = initialProfile.Email,
            FirstName = "FirstMutation",
            LastName = initialProfile.LastName,
            PhoneE164 = initialProfile.PhoneE164,
            Locale = initialProfile.Locale,
            Timezone = initialProfile.Timezone,
            Currency = initialProfile.Currency,
            RowVersion = initialProfile.RowVersion
        });

        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act: submit a second update with the now-stale original row-version.
        using var staleUpdateResponse = await client.PutAsJsonAsync("/api/v1/profile/me", new CustomerProfile
        {
            Id = initialProfile.Id,
            Email = initialProfile.Email,
            FirstName = "SecondMutationShouldFail",
            LastName = initialProfile.LastName,
            PhoneE164 = initialProfile.PhoneE164,
            Locale = initialProfile.Locale,
            Timezone = initialProfile.Timezone,
            Currency = initialProfile.Currency,
            RowVersion = initialProfile.RowVersion
        });

        // Assert
        staleUpdateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await staleUpdateResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Detail.Should().NotBeNullOrWhiteSpace();
        problem.Detail.Should().Contain("Concurrency", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Reads the current profile and validates a successful response.
    /// </summary>
    /// <param name="client">Authenticated HTTP client bound to the test host.</param>
    /// <returns>Current customer profile payload.</returns>
    private static async Task<CustomerProfile> GetCurrentProfileAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/profile/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<CustomerProfile>();
        profile.Should().NotBeNull();
        profile!.Id.Should().NotBe(Guid.Empty);
        profile.RowVersion.Should().NotBeNull();
        profile.RowVersion!.Length.Should().BeGreaterThan(0);

        return profile;
    }

    /// <summary>
    ///     Creates unique credentials to keep test data isolated and deterministic.
    /// </summary>
    /// <returns>Unique credential tuple for each test run.</returns>
    private static TestCredentials CreateUniqueCredentials()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new TestCredentials(
            Email: $"profile-{suffix}@example.test",
            Password: "P@ssw0rd!Aa1",
            DeviceId: $"profile-device-{suffix}");
    }

    /// <summary>
    ///     Represents credentials used by profile integration tests.
    /// </summary>
    /// <param name="Email">User email address.</param>
    /// <param name="Password">User password satisfying policy requirements.</param>
    /// <param name="DeviceId">Device identifier for login/refresh binding scenarios.</param>
    private sealed record TestCredentials(string Email, string Password, string DeviceId);
}
