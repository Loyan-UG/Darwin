using Darwin.Contracts.Identity;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Darwin.Tests.Common.TestInfrastructure;

/// <summary>
///     Provides reusable helpers for identity-related integration test flows
///     such as register and login.
/// </summary>
public static class IdentityFlowTestHelper
{
    /// <summary>
    ///     Registers a user and asserts the endpoint returns HTTP 200.
    /// </summary>
    /// <param name="client">HTTP client targeting the integration test host.</param>
    /// <param name="firstName">First name used for registration.</param>
    /// <param name="lastName">Last name used for registration.</param>
    /// <param name="email">Unique email address used for registration.</param>
    /// <param name="password">Password satisfying registration policy.</param>
    public static async Task RegisterExpectSuccessAsync(HttpClient client, string firstName, string lastName, string email, string password)
    {
        using var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Password = password
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Performs login and returns validated token response.
    /// </summary>
    /// <param name="client">HTTP client targeting the integration test host.</param>
    /// <param name="email">Email used for login.</param>
    /// <param name="password">Password used for login.</param>
    /// <param name="deviceId">Device id for refresh-token binding scenarios.</param>
    /// <returns>Validated token response payload.</returns>
    public static async Task<TokenResponse> LoginExpectSuccessAsync(HttpClient client, string email, string password, string? deviceId)
    {
        using var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new PasswordLoginRequest
        {
            Email = email,
            Password = password,
            DeviceId = deviceId
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        token.Should().NotBeNull();
        token!.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.RefreshToken.Should().NotBeNullOrWhiteSpace();
        return token;
    }
}
