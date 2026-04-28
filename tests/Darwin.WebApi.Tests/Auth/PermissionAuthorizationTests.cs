using System;
using System.Linq;
using System.Security.Claims;
using Darwin.Application.Identity.Services;
using Darwin.WebApi.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Moq;

namespace Darwin.WebApi.Tests.Auth;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public void PermissionRequirement_Should_Throw_WhenPermissionKeyIsMissing()
    {
        Action act = () => new PermissionRequirement("");

        act.Should().Throw<ArgumentException>().WithParameterName("permissionKey");
    }

    [Fact]
    public void PermissionPolicyProvider_Should_Throw_WhenOptionsAreMissing()
    {
        Action act = () => new PermissionPolicyProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void HasPermissionAttribute_Should_SetPolicy_WithPermissionPrefix()
    {
        var attribute = new HasPermissionAttribute("Catalog.View");

        attribute.Policy.Should().Be("perm:Catalog.View");
    }

    [Fact]
    public void HasPermissionAttribute_Should_Throw_WhenPermissionKeyIsMissing()
    {
        Action act = () => new HasPermissionAttribute(" ");

        act.Should().Throw<ArgumentException>().WithParameterName("permissionKey");
    }

    [Fact]
    public async Task GetPolicyAsync_Should_BuildPermissionPolicy_ForPrefixedPolicy()
    {
        var provider = new PermissionPolicyProvider(new TestAuthOptions());

        var policy = await provider.GetPolicyAsync("perm:Catalog.View");

        policy.Should().NotBeNull();
        policy!.AuthenticationSchemes.Should().Contain(JwtBearerDefaults.AuthenticationScheme);
        policy.Requirements.OfType<PermissionRequirement>()
            .Single().PermissionKey.Should().Be("Catalog.View");
    }

    [Fact]
    public async Task GetPolicyAsync_Should_DelegateToFallback_ForUnknownPolicyName()
    {
        var options = new TestAuthOptions();
        var provider = new PermissionPolicyProvider(options);
        var actual = await provider.GetPolicyAsync("some-known-policy");

        actual.Should().BeNull();
    }

    [Fact]
    public void PermissionAuthorizationHandler_Should_Throw_WhenPermissionServiceIsMissing()
    {
        Action act = () => new PermissionAuthorizationHandler(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("permissions");
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_WhenUserHasFullAdminAccess()
    {
        var userId = Guid.NewGuid();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(x => x.HasAsync(userId, "FullAdminAccess", default))
            .ReturnsAsync(true);

        var handler = new PermissionAuthorizationHandler(permissionService.Object);
        var context = BuildContext(userId, "Catalog.View");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        permissionService.Verify(x => x.HasAsync(userId, "FullAdminAccess", default), Times.Once);
        permissionService.Verify(x => x.HasAsync(userId, "Catalog.View", default), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_WhenUserHasRequiredPermission()
    {
        var userId = Guid.NewGuid();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(x => x.HasAsync(userId, "FullAdminAccess", default))
            .ReturnsAsync(false);
        permissionService.Setup(x => x.HasAsync(userId, "Catalog.View", default))
            .ReturnsAsync(true);

        var handler = new PermissionAuthorizationHandler(permissionService.Object);
        var context = BuildContext(userId, "Catalog.View");

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        permissionService.Verify(x => x.HasAsync(userId, "FullAdminAccess", default), Times.Once);
        permissionService.Verify(x => x.HasAsync(userId, "Catalog.View", default), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_NotSucceed_WhenSubjectClaimMissing()
    {
        var permissionService = new Mock<IPermissionService>();
        var handler = new PermissionAuthorizationHandler(permissionService.Object);
        var context = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement("Catalog.View") },
            new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", Guid.NewGuid().ToString()) },
                authenticationType: "TestAuth")),
            null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        permissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_Should_NotSucceed_WhenSubjectClaimIsInvalidGuid()
    {
        var permissionService = new Mock<IPermissionService>();
        var handler = new PermissionAuthorizationHandler(permissionService.Object);
        var context = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement("Catalog.View") },
            new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") },
                authenticationType: "TestAuth")),
            null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        permissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public void HandleAsync_Should_Throw_WhenContextIsNull()
    {
        var permissionService = new Mock<IPermissionService>();
        var handler = new PermissionAuthorizationHandler(permissionService.Object);

        Func<Task> act = () => handler.HandleAsync(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    private static AuthorizationHandlerContext BuildContext(Guid userId, string permission)
    {
        return new AuthorizationHandlerContext(
            new[] { new PermissionRequirement(permission) },
            new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                authenticationType: "TestAuth")),
            null);
    }

    private sealed class TestAuthOptions : IOptions<AuthorizationOptions>
    {
        public AuthorizationOptions Value { get; } = new();
    }
}
