using Darwin.Contracts.Common;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Profile;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Darwin.Tests.Integration.Profile;

/// <summary>
///     Provides authorized integration coverage for profile read/update endpoints,
///     including optimistic concurrency behavior based on RowVersion.
/// </summary>
public sealed class ProfileEndpointAuthorizedConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>
    ///     Initializes the test fixture with a host configured for Testing environment.
    /// </summary>
    /// <param name="factory">Shared WebApplicationFactory instance.</param>
    public ProfileEndpointAuthorizedConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
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
        await RegisterUserAsync(client, credentials);
        var token = await LoginExpectSuccessAsync(client, credentials);
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
        await RegisterUserAsync(client, credentials);
        var token = await LoginExpectSuccessAsync(client, credentials);
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
        await RegisterUserAsync(client, credentials);
        var token = await LoginExpectSuccessAsync(client, credentials);
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
    ///     Registers a new user account for isolation between integration tests.
    /// </summary>
    /// <param name="client">HTTP client used against the test host.</param>
    /// <param name="credentials">Unique credentials for the registration flow.</param>
    private static async Task RegisterUserAsync(HttpClient client, TestCredentials credentials)
    {
        using var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            FirstName = "Integration",
            LastName = "ProfileTester",
            Email = credentials.Email,
            Password = credentials.Password
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Performs login with known credentials and returns a validated token response.
    /// </summary>
    /// <param name="client">HTTP client used against the test host.</param>
    /// <param name="credentials">Credentials used for authentication.</param>
    /// <returns>Token response required for authorized profile calls.</returns>
    private static async Task<TokenResponse> LoginExpectSuccessAsync(HttpClient client, TestCredentials credentials)
    {
        using var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new PasswordLoginRequest
        {
            Email = credentials.Email,
            Password = credentials.Password,
            DeviceId = credentials.DeviceId
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        return token;
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
    ///     Creates an HTTPS test client to avoid redirect side-effects in status assertions.
    /// </summary>
    /// <returns>Configured HttpClient instance.</returns>
    private HttpClient CreateHttpsClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    /// <summary>
    ///     Represents credentials used by profile integration tests.
    /// </summary>
    /// <param name="Email">User email address.</param>
    /// <param name="Password">User password satisfying policy requirements.</param>
    /// <param name="DeviceId">Device identifier for login/refresh binding scenarios.</param>
    private sealed record TestCredentials(string Email, string Password, string DeviceId);
}
