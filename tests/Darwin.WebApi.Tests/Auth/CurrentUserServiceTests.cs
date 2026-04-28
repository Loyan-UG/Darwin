using System;
using System.Security.Claims;
using Darwin.Application.Abstractions.Auth;
using Darwin.WebApi.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Darwin.WebApi.Tests.Auth;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenAccessorIsMissing()
    {
        Action act = () => new CurrentUserService(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpContextAccessor");
    }

    [Fact]
    public void GetCurrentUserId_Should_ReturnGuid_FromNameIdentifierClaim()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    },
                    authenticationType: "TestAuth"))
        };

        var accessor = new HttpContextAccessor { HttpContext = context };
        var service = new CurrentUserService(accessor);

        var actual = service.GetCurrentUserId();

        actual.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_Should_FallbackToSubClaim_WhenNameIdentifierMissing()
    {
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim("sub", userId.ToString())
                    },
                    authenticationType: "TestAuth"))
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = context });

        var actual = service.GetCurrentUserId();

        actual.Should().Be(userId);
    }

    [Fact]
    public void GetCurrentUserId_Should_Throw_WhenNoHttpContext()
    {
        var service = new CurrentUserService(new HttpContextAccessor());

        Func<Guid> act = () => service.GetCurrentUserId();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No authenticated user id is available in the current HTTP context.");
    }

    [Fact]
    public void GetCurrentUserId_Should_Throw_WhenClaimCannotBeParsed()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
                    },
                    authenticationType: "TestAuth"))
        };
        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = context });

        Func<Guid> act = () => service.GetCurrentUserId();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("No authenticated user id is available in the current HTTP context.");
    }
}
