using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceSourceTests
{
    [Fact]
    public void WebApiStartup_Should_KeepRateLimiterAheadOfAuthentication_AndAuthorization()
    {
        var source = ReadWebApiFile(Path.Combine("Extensions", "Startup.cs"));

        source.Should().Contain("app.UseRateLimiter();");
        source.Should().Contain("app.UseAuthentication();");
        source.Should().Contain("app.UseAuthorization();");
        source.IndexOf("app.UseRateLimiter();", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("app.UseAuthentication();", StringComparison.Ordinal));
        source.IndexOf("app.UseAuthentication();", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("app.UseAuthorization();", StringComparison.Ordinal));
    }

    [Fact]
    public void WebApiDependencyInjection_Should_KeepAuthRateLimitPoliciesConfigured()
    {
        var source = ReadWebApiFile(Path.Combine("Extensions", "DependencyInjection.cs"));

        source.Should().Contain("services.AddRateLimiter");
        source.Should().Contain("options.AddPolicy(\"auth-login\"");
        source.Should().Contain("PermitLimit = 5");
        source.Should().Contain("Window = TimeSpan.FromSeconds(30)");
        source.Should().Contain("options.AddPolicy(\"auth-refresh\"");
        source.Should().Contain("PermitLimit = 20");
        source.Should().Contain("Window = TimeSpan.FromSeconds(60)");
        source.Should().Contain("StatusCodes.Status429TooManyRequests");
    }

    [Fact]
    public void AdminBaseController_Should_RequireAdminPanelPermissionByDefault()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        source.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
    }

    [Fact]
    public void BusinessesController_Should_KeepSensitiveLifecycleActionsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("[HttpPost, ValidateAntiForgeryToken]");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.FullAdminAccess)]");
        source.Should().Contain("public async Task<IActionResult> Delete(");
        source.Should().Contain("public async Task<IActionResult> Approve(");
        source.Should().Contain("public async Task<IActionResult> Suspend(");
        source.Should().Contain("public async Task<IActionResult> Reactivate(");
        source.Should().Contain("public async Task<IActionResult> CreateMember(");
    }

    private static string ReadWebApiFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.WebApi", relativePath));

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }

    private static string ReadWebAdminFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Darwin.WebAdmin", relativePath));

        File.Exists(path).Should().BeTrue($"source should exist at {path}");
        return File.ReadAllText(path);
    }
}
