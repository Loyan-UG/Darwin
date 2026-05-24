using Darwin.Mobile.Shared.Common;
using FluentAssertions;

namespace Darwin.Mobile.Shared.Tests.Api;

/// <summary>
/// Covers launch-critical validation for mobile API endpoint configuration.
/// </summary>
public sealed class ApiOptionsValidationTests
{
    /// <summary>
    /// Verifies the mobile API client cannot start without a configured base URL.
    /// </summary>
    [Fact]
    public void ValidateForMobileClient_Should_Fail_WhenBaseUrlIsMissing()
    {
        var options = new ApiOptions { BaseUrl = "" };

        var act = () => options.ValidateForMobileClient(requireHttps: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ApiOptions:BaseUrl is required for mobile API clients.");
    }

    /// <summary>
    /// Verifies relative base URLs are rejected because they make request composition ambiguous.
    /// </summary>
    [Fact]
    public void ValidateForMobileClient_Should_Fail_WhenBaseUrlIsRelative()
    {
        var options = new ApiOptions { BaseUrl = "api/v1" };

        var act = () => options.ValidateForMobileClient(requireHttps: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ApiOptions:BaseUrl must be an absolute URL.");
    }

    /// <summary>
    /// Verifies Release/production validation requires HTTPS for the configured API endpoint.
    /// </summary>
    [Fact]
    public void ValidateForMobileClient_Should_Fail_WhenHttpsIsRequiredAndBaseUrlUsesHttp()
    {
        var options = new ApiOptions { BaseUrl = "http://localhost:5136" };

        var act = () => options.ValidateForMobileClient(requireHttps: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("ApiOptions:BaseUrl must use HTTPS for Release/production mobile clients.");
    }

    /// <summary>
    /// Verifies HTTPS API endpoints pass production validation.
    /// </summary>
    [Fact]
    public void ValidateForMobileClient_Should_Pass_WhenBaseUrlUsesHttps()
    {
        var options = new ApiOptions { BaseUrl = "https://api.example.test/" };

        var act = () => options.ValidateForMobileClient(requireHttps: true);

        act.Should().NotThrow();
    }
}
