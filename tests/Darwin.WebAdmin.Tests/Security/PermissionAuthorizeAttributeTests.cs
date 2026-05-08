using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Darwin.Application.Identity.Services;
using Darwin.WebAdmin.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.WebAdmin.Tests.Security;

public sealed class PermissionAuthorizeAttributeTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenPermissionKeyIsMissing()
    {
        Action act = () => new PermissionAuthorizeAttribute(" ");

        act.Should().Throw<ArgumentException>().WithParameterName("permissionKey");
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_AllowWhenAllowAnonymousFilterPresent()
    {
        var context = CreateContext(null, null, [new AllowAnonymousFilter()]);

        var attribute = new PermissionAuthorizeAttribute("SomePermission");
        await attribute.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Challenge_WhenUserIsNotAuthenticated()
    {
        var context = CreateContext(null, null);

        var attribute = new PermissionAuthorizeAttribute("SomePermission");
        await attribute.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<ChallengeResult>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Forbid_WhenUserIdClaimMissingOrInvalid()
    {
        var contextMissing = CreateContext(BuildPrincipal(), null);
        var attribute = new PermissionAuthorizeAttribute("SomePermission");
        await attribute.OnAuthorizationAsync(contextMissing);
        contextMissing.Result.Should().BeOfType<ForbidResult>();

        var contextInvalid = CreateContext(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "not-a-guid") }, "Test")), null);
        await attribute.OnAuthorizationAsync(contextInvalid);
        contextInvalid.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_InternalError_WhenPermissionServiceMissing()
    {
        var context = CreateContext(BuildPrincipal(Guid.NewGuid()), null);
        var attribute = new PermissionAuthorizeAttribute("SomePermission");

        await attribute.OnAuthorizationAsync(context);

        var result = context.Result.Should().BeOfType<StatusCodeResult>().Subject;
        result.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Succeed_ForFullAdmin()
    {
        var service = new FakePermissionService();
        var userId = service.UserId;
        service.SetPermission(userId, "FullAdminAccess", true);
        var context = CreateContext(BuildPrincipal(userId), service);
        var attribute = new PermissionAuthorizeAttribute("AnyPermission");

        await attribute.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Deny_WhenRequiredPermissionMissing()
    {
        var service = new FakePermissionService();
        var userId = service.UserId;
        var context = CreateContext(BuildPrincipal(userId), service);
        var attribute = new PermissionAuthorizeAttribute("MissingPermission");

        await attribute.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Allow_WhenRequiredPermissionGranted()
    {
        var service = new FakePermissionService();
        service.SetPermission(service.UserId, "Catalog.View", true);
        var context = CreateContext(BuildPrincipal(service.UserId), service);
        var attribute = new PermissionAuthorizeAttribute("Catalog.View");

        await attribute.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_Should_Throw_WhenContextIsNull()
    {
        var attribute = new PermissionAuthorizeAttribute("AnyPermission");

        Func<System.Threading.Tasks.Task> act = () => attribute.OnAuthorizationAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
    }

    private static AuthorizationFilterContext CreateContext(ClaimsPrincipal? principal, object? service, List<IFilterMetadata>? filters = null)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        if (service is IPermissionService permissionService)
        {
            var services = new ServiceCollection();
            services.AddSingleton(permissionService);
            httpContext.RequestServices = services.BuildServiceProvider();
        }

        var identity = principal?.Identity as ClaimsIdentity;
        if (principal is not null && identity is not null)
        {
            httpContext.User = principal;
        }

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            filters?.Cast<IFilterMetadata>().ToList() ?? new List<IFilterMetadata>());
    }

    private static ClaimsPrincipal BuildPrincipal(Guid? userId = null)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private sealed class FakePermissionService : IPermissionService
    {
        public Guid UserId { get; } = Guid.NewGuid();
        private readonly Dictionary<(Guid, string), bool> _rights = new();

        public Task<bool> HasAsync(Guid userId, string permissionKey, System.Threading.CancellationToken ct = default)
        {
            _ = userId;
            _ = ct;
            return Task.FromResult(_rights.TryGetValue((userId, permissionKey), out var allowed) && allowed);
        }

        public Task<HashSet<string>> GetAllAsync(Guid userId, System.Threading.CancellationToken ct = default)
        {
            _ = userId;
            _ = ct;
            return Task.FromResult(new HashSet<string>(_rights.Keys
                .Where(x => x.Item1 == userId && _rights.TryGetValue(x, out var allowed) && allowed)
                .Select(x => x.Item2)));
        }

        public void SetPermission(Guid userId, string permission, bool granted)
        {
            _rights[(userId, permission)] = granted;
        }
    }
}
