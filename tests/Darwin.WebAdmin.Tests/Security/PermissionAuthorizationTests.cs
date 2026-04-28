using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Darwin.Application.Identity.Services;
using Darwin.WebAdmin.Auth;
using Darwin.WebAdmin.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Darwin.WebAdmin.Tests.Security;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public void PermissionRequirement_Should_Throw_WhenPermissionKeyMissing()
    {
        Action act = () => new PermissionRequirement(" ");

        act.Should().Throw<ArgumentNullException>().WithParameterName("permissionKey");
    }

    [Fact]
    public void PermissionRequirement_Should_SetPermissionKey()
    {
        var requirement = new PermissionRequirement("Catalog.View");

        requirement.PermissionKey.Should().Be("Catalog.View");
    }

    [Fact]
    public void HasPermissionAttribute_Should_ProducePermissionPolicyName()
    {
        var attribute = new HasPermissionAttribute("Catalog.View");

        attribute.Policy.Should().Be("perm:Catalog.View");
    }

    [Fact]
    public async Task GetPolicyAsync_Should_CreatePolicy_ForPermissionPrefix()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("perm:Catalog.View");
        policy.Should().NotBeNull();
        var requirement = policy!.Requirements.Should().ContainSingle().Subject;

        requirement.Should().BeOfType<PermissionRequirement>();
        ((PermissionRequirement)requirement).PermissionKey.Should().Be("Catalog.View");
    }

    [Fact]
    public async Task GetPolicyAsync_Should_Fallback_WhenNotPermissionPolicy()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("SomethingElse");

        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_Should_NotCreatePolicy_WhenPolicyNameIsWhitespace()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("   ");

        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_Should_HandleCaseInsensitivePermissionPrefix()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("PERM:Catalog.View");
        policy.Should().NotBeNull();
        var requirement = policy!.Requirements.Should().ContainSingle().Subject;

        requirement.Should().BeOfType<PermissionRequirement>();
        ((PermissionRequirement)requirement).PermissionKey.Should().Be("Catalog.View");
    }

    [Fact]
    public async Task GetDefaultPolicyAsync_Should_DelegateToFallback()
    {
        var options = new AuthorizationOptions();
        var fallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        options.DefaultPolicy = fallbackPolicy;
        var provider = new PermissionPolicyProvider(Options.Create(options));

        var policy = await provider.GetDefaultPolicyAsync();

        policy.Should().Be(fallbackPolicy);
    }

    [Fact]
    public async Task GetFallbackPolicyAsync_Should_DelegateToFallback_WhenNotSet()
    {
        var options = new AuthorizationOptions();
        options.FallbackPolicy = null;
        var provider = new PermissionPolicyProvider(Options.Create(options));

        var policy = await provider.GetFallbackPolicyAsync();

        policy.Should().BeNull();
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Should_GrantFullAdminWithoutRequiredPermission()
    {
        var userId = Guid.NewGuid();
        var service = new FakePermissionService();
        service.SetPermission(userId, "FullAdminAccess", true);
        var handler = new PermissionAuthorizationHandler(service);
        var requirement = new PermissionRequirement("Catalog.View");
        var context = CreateAuthorizationContext(userId, requirement);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        service.Calls.Select(c => c.permission).Should().Contain(new[] { "FullAdminAccess" });
        service.Calls.Should().NotContain(c => c.permission == "Catalog.View");
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Should_Grant_WhenRequiredPermissionGranted()
    {
        var userId = Guid.NewGuid();
        var service = new FakePermissionService();
        service.SetPermission(userId, "Catalog.View", true);
        var handler = new PermissionAuthorizationHandler(service);
        var requirement = new PermissionRequirement("Catalog.View");
        var context = CreateAuthorizationContext(userId, requirement);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        service.Calls.Should().Contain(c => c.permission == "FullAdminAccess");
        service.Calls.Should().Contain(c => c.permission == "Catalog.View");
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Should_NotGrant_WhenRequiredPermissionMissing()
    {
        var userId = Guid.NewGuid();
        var service = new FakePermissionService();
        var handler = new PermissionAuthorizationHandler(service);
        var requirement = new PermissionRequirement("Catalog.View");
        var context = CreateAuthorizationContext(userId, requirement);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        service.Calls.Should().Contain(c => c.permission == "FullAdminAccess");
        service.Calls.Should().Contain(c => c.permission == "Catalog.View");
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Should_NotGrant_WhenUserIdInvalid()
    {
        var service = new FakePermissionService();
        var handler = new PermissionAuthorizationHandler(service);
        var requirement = new PermissionRequirement("Catalog.View");
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "TestAuth");
        var context = new AuthorizationHandlerContext(new[] { requirement }, new ClaimsPrincipal(identity), resource: null);

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
        service.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task PermissionRazorHelper_Should_Reject_WhenNoContextOrUnauthenticated()
    {
        var helperWithNullContext = new PermissionRazorHelper(new HttpContextAccessor(), new FakePermissionService());
        var canWithoutContext = await helperWithNullContext.HasAsync("Catalog.View");

        canWithoutContext.Should().BeFalse();

        var access = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>()))
            }
        };
        var helper = new PermissionRazorHelper(access, new FakePermissionService());

        var can = await helper.HasAsync("Catalog.View");

        can.Should().BeFalse();
    }

    [Fact]
    public async Task PermissionRazorHelper_Should_Reject_WhenUserIdIsInvalid()
    {
        var access = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "invalid") }, "TestAuth"))
            }
        };
        var helper = new PermissionRazorHelper(access, new FakePermissionService());

        var can = await helper.HasAsync("Catalog.View");

        can.Should().BeFalse();
    }

    [Fact]
    public async Task PermissionRazorHelper_Should_Allow_WhenFullAdmin()
    {
        var userId = Guid.NewGuid();
        var service = new FakePermissionService();
        service.SetPermission(userId, "FullAdminAccess", true);
        var access = new HttpContextAccessor { HttpContext = CreateHttpContext(userId, isAuthenticated: true) };
        var helper = new PermissionRazorHelper(access, service);

        var can = await helper.HasAsync("Catalog.View");

        can.Should().BeTrue();
        service.Calls.Select(c => c.permission).Should().Contain(new[] { "FullAdminAccess" });
        service.Calls.Should().NotContain(c => c.permission == "Catalog.View");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PermissionRazorHelper_Should_RespectGrantedPermission(bool grantPermission)
    {
        var userId = Guid.NewGuid();
        var service = new FakePermissionService();
        service.SetPermission(userId, "Catalog.View", grantPermission);
        var access = new HttpContextAccessor { HttpContext = CreateHttpContext(userId, isAuthenticated: true) };
        var helper = new PermissionRazorHelper(access, service);

        var can = await helper.HasAsync("Catalog.View");

        can.Should().Be(grantPermission);
        service.Calls.Should().Contain(c => c.permission == "FullAdminAccess");
        service.Calls.Should().Contain(c => c.permission == "Catalog.View");
    }

    [Fact]
    public void PermissionRazorHelper_Should_Throw_WhenDependenciesAreMissing()
    {
        var actAccessor = () => new PermissionRazorHelper(null!, new FakePermissionService());

        actAccessor.Should().Throw<ArgumentNullException>().WithParameterName("httpContextAccessor");

        var act = () => new PermissionRazorHelper(new HttpContextAccessor(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("permissions");
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(Guid userId, params PermissionRequirement[] requirements)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new AuthorizationHandlerContext(requirements, principal, resource: null);
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId, bool isAuthenticated)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
            isAuthenticated ? "TestAuth" : null);
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private sealed class FakePermissionService : IPermissionService
    {
        private readonly Dictionary<(Guid UserId, string Permission), bool> _grants = new();

        public List<(Guid userId, string permission, bool granted)> Calls { get; } = new();

        public Task<HashSet<string>> GetAllAsync(Guid userId, System.Threading.CancellationToken ct = default)
        {
            return Task.FromResult(_grants.Where(x => x.Key.UserId == userId && x.Value)
                .Select(x => x.Key.Permission)
                .ToHashSet());
        }

        public Task<bool> HasAsync(Guid userId, string permission, System.Threading.CancellationToken ct = default)
        {
            Calls.Add((userId, permission, _grants.TryGetValue((userId, permission), out var grant) && grant));
            return Task.FromResult(_grants.TryGetValue((userId, permission), out var g) && g);
        }

        public void SetPermission(Guid userId, string permission, bool granted)
        {
            _grants[(userId, permission)] = granted;
        }
    }
}
