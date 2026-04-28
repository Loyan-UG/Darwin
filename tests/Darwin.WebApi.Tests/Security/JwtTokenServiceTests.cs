using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Security.Jwt;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebApi.Tests.Security;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDbContextIsMissing()
    {
        Action act = () => new JwtTokenService(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("db");
    }

    [Fact]
    public void IssueTokens_Should_Throw_WhenSiteSettingIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var service = new JwtTokenService(db);

        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IssueTokens_Should_Throw_WhenJwtIsDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtEnabled = false, JwtSigningKey = "secret", JwtIssuer = "issuer", JwtAudience = "audience" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT is disabled by SiteSetting (JwtEnabled = false).");
    }

    [Fact]
    public void IssueTokens_Should_RequireDeviceId_WhenDeviceBindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Device binding is required (JwtRequireDeviceBinding = true) but no device id was supplied.");
    }

    [Fact]
    public void IssueTokens_Should_RequireNonWhiteSpaceDeviceId_WhenDeviceBindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", "   ");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Device binding is required (JwtRequireDeviceBinding = true) but no device id was supplied.");
    }

    [Fact]
    public void IssueTokens_Should_UseMinimumTokenLifetimeWhenConfiguredValuesAreTooSmall()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtAccessTokenMinutes = -3,
            JwtRefreshTokenDays = 0
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var start = DateTime.UtcNow;

        var (_, accessExpiresAtUtc, _, refreshExpiresAtUtc) = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        accessExpiresAtUtc.Should().BeCloseTo(start.AddMinutes(5), TimeSpan.FromSeconds(5));
        refreshExpiresAtUtc.Should().BeCloseTo(start.AddDays(1), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IssueTokens_Should_IncludeRequiredClaims_SubAndEmail()
    {
        var userId = Guid.NewGuid();
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Subject.Should().Be(userId.ToString());
        token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("user@example.com");
    }

    [Fact]
    public void IssueTokens_Should_IncludeJti_AndBeDifferentPerToken()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var first = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);
        var second = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var firstToken = new JwtSecurityTokenHandler().ReadJwtToken(first.accessToken);
        var secondToken = new JwtSecurityTokenHandler().ReadJwtToken(second.accessToken);

        var firstJti = firstToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = secondToken.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        firstJti.Should().NotBeNullOrWhiteSpace();
        secondJti.Should().NotBeNullOrWhiteSpace();
        firstJti.Should().NotBe(secondJti);
    }

    [Fact]
    public void IssueTokens_Should_UseConfiguredPositiveTokenLifetime()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtAccessTokenMinutes = 20,
            JwtRefreshTokenDays = 3
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var start = DateTime.UtcNow;
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        issued.expiresAtUtc.Should().BeCloseTo(start.AddMinutes(20), TimeSpan.FromSeconds(6));
        issued.refreshExpiresAtUtc.Should().BeCloseTo(start.AddDays(3), TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void IssueTokens_Should_RespectIssuerAndAudience_WhenConfigured()
    {
        var userId = Guid.NewGuid();
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtIssuer = "test-issuer",
            JwtAudience = "test-audience"
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Issuer.Should().Be("test-issuer");
        token.Audiences.Should().Contain("test-audience");
    }

    [Fact]
    public void IssueTokens_Should_PersistRefreshToken_WithJwtRefreshPurpose_WhenDeviceBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var siteSetting = new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRefreshTokenDays = 2,
            JwtRequireDeviceBinding = false
        };
        db.Set<SiteSetting>().Add(siteSetting);
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "ignored-device");

        issued.refreshToken.Should().NotBeNullOrWhiteSpace();
        var row = db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh");
        row.Value.Should().Be(issued.refreshToken);
        row.UsedAtUtc.Should().BeNull();
        row.ExpiresAtUtc.Should().Be(issued.refreshExpiresAtUtc);
    }

    [Fact]
    public void IssueTokens_Should_PersistDeviceBoundRefreshPurpose_WhenDeviceBindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "device-1");

        db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh:device-1")
            .Value.Should().Be(issued.refreshToken);
    }

    [Fact]
    public void IssueTokens_Should_IgnoreDeviceId_WhenDeviceBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = false
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.IssueTokens(userId, "user@example.com", "   ");

        var tokenRow = db.Set<UserToken>().Where(x => x.UserId == userId)
            .Single(x => x.Purpose == "JwtRefresh");

        tokenRow.Purpose.Should().Be("JwtRefresh");
    }

    [Fact]
    public void IssueTokens_Should_EmitScopeClaim_WhenScopeFlagIsEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtEmitScopes = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null, new[] { "orders.read", "orders.write" });

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        var claim = token.Claims.FirstOrDefault(c => c.Type == "scope");
        claim.Should().NotBeNull();
        claim!.Value.Should().Be("orders.read,orders.write");
    }

    [Fact]
    public void IssueTokens_Should_NotEmitScopeClaim_WhenScopeFlagIsDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtEmitScopes = false
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null, new[] { "orders.read", "orders.write" });

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Should().NotContain(c => c.Type == "scope");
    }

    [Fact]
    public void IssueTokens_Should_NotEmitScopeClaim_WhenScopesAreNullEvenIfScopeFlagEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtEmitScopes = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null, null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Should().NotContain(c => c.Type == "scope");
    }

    [Fact]
    public void IssueTokens_Should_AllowEmptyIssuerAndAudience()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtIssuer = string.Empty,
            JwtAudience = string.Empty
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Issuer.Should().BeEmpty();
        token.Audiences.Should().BeEmpty();
    }

    [Fact]
    public void IssueTokens_Should_EmbedBusinessId_WhenMemberIsActive()
    {
        var userId = Guid.NewGuid();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Active Business" });
        db.Set<BusinessMember>().Add(new BusinessMember { UserId = userId, BusinessId = businessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims
            .Where(c => c.Type == "business_id")
            .Single()
            .Value.Should().Be(businessId.ToString("D"));
    }

    [Fact]
    public void IssueTokens_Should_EmbedPreferredBusinessId_WhenRequestedAndUserMemberIsActive()
    {
        var userId = Guid.NewGuid();
        var preferredBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ignoredBusinessId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = preferredBusinessId, Name = "Preferred Business" },
            new Business { Id = ignoredBusinessId, Name = "Other Business" });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = preferredBusinessId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = ignoredBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: preferredBusinessId);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims
            .Where(c => c.Type == "business_id")
            .Single()
            .Value.Should().Be(preferredBusinessId.ToString("D"));
    }

    [Fact]
    public void IssueTokens_Should_OverwriteExistingRefreshTokenForSamePurpose()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "old-refresh", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        db.Set<UserToken>().Where(x => x.UserId == userId).Should().HaveCount(1);
        db.Set<UserToken>().Single(x => x.UserId == userId).Value.Should().Be(issued.refreshToken);
        db.Set<UserToken>().Single(x => x.UserId == userId).UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_InvalidateAllRefreshTokens_WhenSingleDeviceOnlyEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtSingleDeviceOnly = true,
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "old-1", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:device-x", "old-2", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.IssueTokens(userId, "user@example.com", null);

        db.Set<UserToken>().Count(x => x.UserId == userId).Should().Be(2);
        db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh:device-x").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_InvalidateAllExistingRefreshTokens_WhenSingleDeviceOnlyEnabledAndDeviceBindingOn()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtSingleDeviceOnly = true,
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "old-1", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "old-2", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.IssueTokens(userId, "user@example.com", "device-phone");

        db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh:phone").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh:device-phone").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenValidAndBindingNotRequired()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var tokenValue = "refresh-token";
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", tokenValue, DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken(tokenValue, null);

        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenTokenIsBlank()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken(" ", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenDeviceIsMissing_AndBindingRequired()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(Guid.NewGuid(), "JwtRefresh:device-1", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenDeviceDoesNotMatchBinding()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:device-1", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "different-device");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenExpired()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddSeconds(-10)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenNoExpiryIsSet()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", null));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenDeviceBindingDisabledButStoredTokenIsDeviceBound()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenDeviceBindingEnabledAndTokenIsGenericPurpose()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "device-1");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_IgnoreDeviceId_WhenDeviceBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "any-device");

        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenPurposeIsNotRefresh()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtAccess", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_ForDifferentValueCasing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("Refresh-Token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_Throw_WhenSiteSettingIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var service = new JwtTokenService(db);

        Action act = () => service.ValidateRefreshToken("refresh-token", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RevokeRefreshToken_Should_MarkUsed_ForValue_WhenDeviceBindingNotRequired()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:tablet", "another-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Value == "another-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenIsBlank()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(Guid.NewGuid(), "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("   ", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenNotFound()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(Guid.NewGuid(), "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("missing-token", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeBoundPurpose_WhenDeviceBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_MarkOnlyUserRefreshTokens_WhenSiteSettingMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "token-user", DateTime.UtcNow.AddDays(1)),
            new UserToken(otherUserId, "JwtRefresh", "token-other", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(1);
        db.Set<UserToken>().Single(x => x.Value == "token-user").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Value == "token-other").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotOverwriteUsedAt_WhenAlreadyUsed()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var usedAt = DateTime.UtcNow.AddMinutes(-5);
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });

        var userId = Guid.NewGuid();
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().Be(usedAt);
    }

    [Fact]
    public void RevokeRefreshToken_Should_MarkUsed_ForBoundPurpose_WhenDeviceBindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:tablet", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", "phone");

        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:tablet").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_MarkAllActiveTokens_AndReturnCount()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token-1", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "refresh-token-2", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "other-purpose", "other-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(2);
        db.Set<UserToken>().Where(x => x.Purpose.StartsWith("JwtRefresh")).Should().AllSatisfy(x => x.UsedAtUtc.Should().NotBeNull());
        db.Set<UserToken>().Single(x => x.Purpose == "other-purpose").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_ReturnAllRefreshRows_AndNotOverwriteUsedTokens()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var alreadyUsed = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token-1", DateTime.UtcNow.AddDays(1))
            {
                UsedAtUtc = alreadyUsed
            },
            new UserToken(userId, "JwtRefresh:phone", "refresh-token-2", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(2);
        db.Set<UserToken>().Single(x => x.Value == "refresh-token-1").UsedAtUtc.Should().Be(alreadyUsed);
        db.Set<UserToken>().Single(x => x.Value == "refresh-token-2").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_NotRevokeNonRefreshTokens_WhenUserHasNoRefreshRows()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "OtherPurpose", "token-1", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtAccess", "token-2", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(0);
        db.Set<UserToken>().Where(x => x.UserId == userId).Should().AllSatisfy(x => x.UsedAtUtc.Should().BeNull());
    }

    [Fact]
    public void RevokeAllForUser_Should_ReturnZero_WhenNoRefreshRowsExist()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(0);
    }

    [Fact]
    public void RevokeAllForUser_Should_NotAffectOtherUsers()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token-1", DateTime.UtcNow.AddDays(1)),
            new UserToken(otherUserId, "JwtRefresh", "refresh-token-2", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(1);
        db.Set<UserToken>().Single(x => x.Value == "refresh-token-2").UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.Value == "refresh-token-1").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void IssueTokens_Should_Throw_WhenJwtSigningKeyIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtEnabled = true, JwtSigningKey = "   " });
        db.SaveChanges();

        var service = new JwtTokenService(db);

        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT signing key (SiteSetting.JwtSigningKey) is not configured.");
    }

    [Fact]
    public void IssueTokens_Should_Work_WhenSigningKeyIsValidBase64()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "dGVzdC1iYXNlNjQtc2VjcmV0",
            JwtEmitScopes = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null, new[] { "read" });

        issued.accessToken.Should().NotBeNullOrWhiteSpace();
        issued.refreshToken.Should().NotBeNullOrWhiteSpace();
        issued.refreshToken.Length.Should().Be(64);
    }

    [Fact]
    public void IssueTokens_Should_Work_WhenSigningKeyIsNotBase64_UsingUtf8Fallback()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "not-base64-key" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        issued.accessToken.Should().NotBeNullOrWhiteSpace();
        issued.refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenDeviceBindingRequiredAndDeviceMatches()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:device-1", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "device-1");

        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenTokenAlreadyUsed()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var tokenRow = new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddMinutes(5))
        {
            UsedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(tokenRow);
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_NotEmitBusinessClaim_WhenNoActiveBusinessFound()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().Add(new Business { Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"), Name = "Deleted business", IsDeleted = true });
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            UserId = Guid.NewGuid(),
            BusinessId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            IsActive = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims.Should().NotContain(c => c.Type == "business_id");
    }

    [Fact]
    public void IssueTokens_Should_FallBackToAnyActiveBusiness_WhenPreferredBusinessMissingOrInactive()
    {
        var userId = Guid.NewGuid();
        var preferred = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var fallback = Guid.Parse("22222222-2222-2222-2222-222222222222");

        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = preferred, Name = "Preferred", IsDeleted = false, IsActive = true },
            new Business { Id = fallback, Name = "Fallback", IsDeleted = false, IsActive = true });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = preferred, IsActive = true, IsDeleted = true },
            new BusinessMember { UserId = userId, BusinessId = fallback, IsActive = true, IsDeleted = false });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: preferred);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims.Single(c => c.Type == "business_id").Value.Should().Be(fallback.ToString("D"));
    }

    [Fact]
    public void IssueTokens_Should_ChooseLowestBusinessId_WhenMultipleMembershipsAreAvailable()
    {
        var userId = Guid.NewGuid();
        var lowBusinessId = Guid.Parse("11111111-1111-1111-1111-000000000001");
        var highBusinessId = Guid.Parse("11111111-1111-1111-1111-ffffffffffff");

        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = lowBusinessId, Name = "Low", IsActive = true },
            new Business { Id = highBusinessId, Name = "High", IsActive = true });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = highBusinessId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = lowBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims.Single(c => c.Type == "business_id").Value.Should().Be(lowBusinessId.ToString("D"));
    }

    [Fact]
    public void RevokeRefreshToken_Should_MarkUsed_WhenBindingEnabledAndDeviceMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:tablet", "other-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Value == "other-token").UsedAtUtc.Should().BeNull();
    }
    
    [Fact]
    public void IssueTokens_Should_EmitNumericIatClaim()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        var iatValue = token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Iat).Value;
        var iat = long.Parse(iatValue);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        iat.Should().BeInRange(now - 10, now + 5);
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotRevoke_WhenBindingEnabledAndWrongDeviceProvided()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", "tablet");

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeAllMatches_WhenBindingEnabledAndDeviceMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh:phone", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:tablet", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Count(x => x.Value == "shared-token" && x.UsedAtUtc != null).Should().Be(2);
    }

    [Fact]
    public void IssueTokens_Should_ExcludeBusinessClaim_WhenPreferredBusinessInactiveAndNoOtherActiveBusiness()
    {
        var userId = Guid.NewGuid();
        var inactivePreferredBusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().Add(new Business { Id = inactivePreferredBusinessId, Name = "Inactive", IsActive = false });
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            UserId = userId,
            BusinessId = inactivePreferredBusinessId,
            IsActive = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: inactivePreferredBusinessId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Should().NotContain(c => c.Type == "business_id");
    }

    [Fact]
    public void RevokeAllForUser_Should_ReturnCount_ButNotOverwriteUsedRows()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var firstUsed = DateTime.UtcNow.AddMinutes(-10);
        var secondUsed = DateTime.UtcNow.AddMinutes(-5);
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token-1", DateTime.UtcNow.AddDays(1)) { UsedAtUtc = firstUsed },
            new UserToken(userId, "JwtRefresh:device-x", "refresh-token-2", DateTime.UtcNow.AddDays(1)) { UsedAtUtc = secondUsed },
            new UserToken(userId, "JwtAccess", "access-token", DateTime.UtcNow.AddDays(1)) { UsedAtUtc = null }
        );
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(2);
        var first = db.Set<UserToken>().Single(x => x.Value == "refresh-token-1");
        var second = db.Set<UserToken>().Single(x => x.Value == "refresh-token-2");
        first.UsedAtUtc.Should().Be(firstUsed);
        second.UsedAtUtc.Should().Be(secondUsed);
    }

    [Fact]
    public void IssueTokens_Should_UseMinimumLifetime_WhenConfigurationIsZero()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtAccessTokenMinutes = 0,
            JwtRefreshTokenDays = 0
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var start = DateTime.UtcNow;

        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        issued.expiresAtUtc.Should().BeCloseTo(start.AddMinutes(5), TimeSpan.FromSeconds(6));
        issued.refreshExpiresAtUtc.Should().BeCloseTo(start.AddDays(1), TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void IssueTokens_Should_NotEmitBusinessClaim_WhenMemberIsInactive()
    {
        var userId = Guid.NewGuid();
        var businessId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().Add(new Business { Id = businessId, Name = "Active Business", IsActive = true });
        db.Set<BusinessMember>().Add(new BusinessMember
        {
            UserId = userId,
            BusinessId = businessId,
            IsActive = false
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims.Should().NotContain(c => c.Type == "business_id");
    }

    [Fact]
    public void IssueTokens_Should_Throw_WhenJwtSigningKeyIsNull()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = null, JwtEnabled = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT signing key (SiteSetting.JwtSigningKey) is not configured.");
    }

    [Fact]
    public void RevokeAllForUser_Should_NotChangeAlreadyUsedRows()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var used = DateTime.UtcNow.AddMinutes(-2);
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "used-token", DateTime.UtcNow.AddDays(1)) { UsedAtUtc = used },
            new UserToken(userId, "JwtRefresh:device", "used-device-token", DateTime.UtcNow.AddDays(1)) { UsedAtUtc = used });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(2);
        db.Set<UserToken>().Single(x => x.Value == "used-token").UsedAtUtc.Should().Be(used);
        db.Set<UserToken>().Single(x => x.Value == "used-device-token").UsedAtUtc.Should().Be(used);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenExpiryIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var tokenValue = "refresh-token";
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", tokenValue, null));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken(tokenValue, null);

        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenBindingDisabledAndPurposeIsDeviceBound()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:device-1", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotChangeUsedAt_WhenAlreadyUsed_BindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var usedAt = DateTime.UtcNow.AddMinutes(-8);
        var tokenValue = "already-used-token";
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", tokenValue, DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken(tokenValue, null);

        db.Set<UserToken>().Single(x => x.Value == tokenValue).UsedAtUtc.Should().Be(usedAt);
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotChangeUsedAt_WhenAlreadyUsed_BindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var usedAt = DateTime.UtcNow.AddMinutes(-8);
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:device-1", "already-used-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("already-used-token", "device-1");

        db.Set<UserToken>().Single(x => x.Value == "already-used-token").UsedAtUtc.Should().Be(usedAt);
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotRevokeNonRefreshTokens_WhenTokenValueMatches()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtAccess", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Single(x => x.Value == "shared-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_EmitScopeClaim_WhenScopesIsEmptyCollectionAndScopeEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtEmitScopes = true
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null, Array.Empty<string>());

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        var scopeClaim = token.Claims.SingleOrDefault(c => c.Type == "scope");
        scopeClaim.Should().NotBeNull();
        scopeClaim!.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenDeviceIdIsWhitespace_BindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:device-1", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "   ");

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenIsBlank()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "existing-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken(" ", null);

        db.Set<UserToken>().Single(x => x.Value == "existing-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_OverwriteExistingDeviceBoundRefreshToken_ForSamePurpose()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        var oldToken = "old-token";
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", oldToken, DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "phone");

        var rows = db.Set<UserToken>().Where(x => x.UserId == userId && x.Purpose == "JwtRefresh:phone").ToList();
        rows.Should().HaveCount(1);
        rows[0].Value.Should().Be(issued.refreshToken);
        rows[0].UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_OverwriteExistingGenericRefreshRow_WhenBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "generic-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "phone");

        db.Set<UserToken>().Where(x => x.UserId == userId).Should().HaveCount(1);
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh").Value.Should().Be(issued.refreshToken);
    }

    [Fact]
    public void IssueTokens_Should_AddNewRefreshRow_WhenPurposeDiffersForSameUser_AndBindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "generic-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "phone");

        db.Set<UserToken>().Count(x => x.UserId == userId).Should().Be(2);
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh").Value.Should().Be("generic-token");
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh" && x.Value == "generic-token").Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone" && x.Value == issued.refreshToken).Should().NotBeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeAllMatchingRefreshPurposes_WhenBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtAccess", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Where(x => x.Purpose.StartsWith("JwtRefresh") && x.Value == "shared-token")
            .AllSatisfy(x => x.UsedAtUtc.Should().NotBeNull());
        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess" && x.Value == "shared-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_IgnoreDeviceId_WhenBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var tokenValue = "refresh-token";
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", tokenValue, DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken(tokenValue, "device-ignored");

        result.Should().Be(userId);
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeGenericRefreshRow_WhenBindingDisabled_IgnoringDeviceId()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", "phone");

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenBindingEnabledAndDeviceProvidedButGenericPurpose()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", "phone");

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_EmitEmptyIssuerAndAudience_WhenIssuerAudienceNull()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtIssuer = null,
            JwtAudience = null
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Issuer.Should().BeEmpty();
        token.Audiences.Should().BeEmpty();
    }

    [Fact]
    public void IssueTokens_Should_NotRevokeNonRefreshRows_WhenSingleDeviceOnlyIsEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var accessValue = "access-token";
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtSingleDeviceOnly = true,
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtAccess", accessValue, DateTime.UtcNow.AddDays(1)));
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "old-refresh", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.IssueTokens(userId, "user@example.com", null);

        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess" && x.Value == accessValue).UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_RevokesRefreshRowsEvenWhenExpiryIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-no-expiry", null),
            new UserToken(userId, "JwtRefresh:phone", "device-no-expiry", null));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(2);
        db.Set<UserToken>().Where(x => x.Purpose.StartsWith("JwtRefresh"))
            .AllSatisfy(x => x.UsedAtUtc.Should().NotBeNull());
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenBindingEnabledAndNoExpiry()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:device-1", "refresh-token", null));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "device-1");

        result.Should().Be(userId);
    }

    [Fact]
    public void IssueTokens_Should_OverwriteGenericRefresh_WhenSingleDeviceOnlyDisabled_AndPreserveDeviceBoundRefreshRows()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = false,
            JwtSingleDeviceOnly = false
        });
        var oldDeviceBound = "old-device-bound";
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "old-generic", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", oldDeviceBound, DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "phone");

        db.Set<UserToken>().Count(x => x.UserId == userId).Should().Be(2);
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh").Value.Should().Be(issued.refreshToken);
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone").Value.Should().Be(oldDeviceBound);
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeAllRefreshRows_WhenBindingEnabledButDeviceIdIsWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh:phone", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:tablet", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtAccess", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", "   ");

        db.Set<UserToken>().Where(x => x.Purpose.StartsWith("JwtRefresh") && x.Value == "shared-token")
            .AllSatisfy(x => x.UsedAtUtc.Should().NotBeNull());
        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess" && x.Value == "shared-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_IgnoreDeviceId_WhenBindingEnabledAndDeviceIdWhitespace_FallbackToWildcardPurpose()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddMinutes(5)),
            new UserToken(userId, "JwtRefresh:tablet", "refresh-token", DateTime.UtcNow.AddMinutes(5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "   ");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_Throw_WhenSiteSettingIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.ValidateRefreshToken("refresh-token", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RevokeRefreshToken_Should_Throw_WhenSiteSettingIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.RevokeRefreshToken("refresh-token", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IssueTokens_Should_OverwriteUsedRefreshRowAndResetUsedAtUtc()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var usedAt = DateTime.UtcNow.AddMinutes(-20);
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "old-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        var row = db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh");
        row.Value.Should().Be(issued.refreshToken);
        row.UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotRevokeJwtAccess_WhenBindingEnabledAndDeviceIdMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtAccess", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Where(x => x.Purpose.StartsWith("JwtRefresh") && x.Value == "shared-token")
            .AllSatisfy(x => x.UsedAtUtc.Should().NotBeNull());
        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess" && x.Value == "shared-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenBindingDisabledAndJwtRefreshPurposeRowsExpired()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddMinutes(-1)),
            new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddMinutes(-1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_NotAffectAccessTokenRows_WhenIssuingNewRefreshForUsedUser()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtSingleDeviceOnly = true,
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtAccess", "access-before", DateTime.UtcNow.AddDays(1)));
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-before", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.IssueTokens(userId, "user@example.com", null);

        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess" && x.Value == "access-before").UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenTokenIsNull()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken(null!, null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenTokenContainsOnlyWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("   ", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenIsNull()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken(null!, null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenContainsOnlyWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("   ", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_OverwriteUsedDeviceBoundRefreshRow_AndResetUsedAtUtc()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var usedAt = DateTime.UtcNow.AddMinutes(-9);
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "old-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "phone");

        var row = db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh:phone");
        row.Value.Should().Be(issued.refreshToken);
        row.UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_Throw_WhenJwtIsDisabledEvenIfSigningKeyMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtEnabled = false,
            JwtSigningKey = null
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT is disabled by SiteSetting (JwtEnabled = false).");
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenTokenHasLeadingOrTrailingWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken(" refresh-token ", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenHasLeadingOrTrailingWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken(" refresh-token ", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_RevokeExpiredRefreshRows()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-expired", DateTime.UtcNow.AddDays(-1)),
            new UserToken(userId, "JwtRefresh:phone", "refresh-expired-device", DateTime.UtcNow.AddMinutes(-30)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(2);
        db.Set<UserToken>().All(x => x.UsedAtUtc != null).Should().BeTrue();
    }

    [Fact]
    public void IssueTokens_Should_Throw_WhenJwtSigningKeyIsWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "   " });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("JWT signing key (SiteSetting.JwtSigningKey) is not configured.");
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenOneMatchingRowIsUsed_AndAnotherIsUnused()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddHours(1))
            {
                UsedAtUtc = DateTime.UtcNow.AddHours(-1)
            },
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().Be(userId);
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotRevoke_WhenTokenCaseDiffers()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("REFRESH-TOKEN", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_FallBackToActiveBusiness_WhenPreferredBusinessIdIsEmptyGuid()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var activeBusinessA = new Business { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), IsActive = true };
        var activeBusinessB = new Business { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), IsActive = true };

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtEmitScopes = false });
        db.Set<Business>().AddRange(activeBusinessA, activeBusinessB);
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = activeBusinessB.Id, IsActive = true, IsDeleted = false },
            new BusinessMember { UserId = userId, BusinessId = activeBusinessA.Id, IsActive = true, IsDeleted = false });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: Guid.Empty);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims.Single(c => c.Type == "business_id").Value.Should().Be(activeBusinessA.Id.ToString("D"));
    }

    [Fact]
    public void IssueTokens_Should_GenerateDifferentRefreshToken_PerCall()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);

        var first = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);
        var second = service.IssueTokens(Guid.NewGuid(), "other@example.com", null);

        first.refreshToken.Should().NotBe(second.refreshToken);
    }

    [Fact]
    public void IssueTokens_Should_JoinMultipleScopes_WithComma()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtEmitScopes = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null, scopes: new[] { "read", "write", "admin" });
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims.First(c => c.Type == "scope").Value.Should().Be("read,write,admin");
    }

    [Fact]
    public void RevokeRefreshToken_Should_BeIdempotent_WhenCalledTwice()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", null);

        var revokedAt = db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc;
        service.RevokeRefreshToken("refresh-token", null);
        var revokedAtAgain = db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc;

        revokedAt.Should().NotBeNull();
        revokedAtAgain.Should().Be(revokedAt);
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotRevokeNonRefreshRows_WhenValueCollidesAcrossPurposes()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "ApiKey", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtAccess", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh" && x.Value == "shared-token").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "ApiKey" && x.Value == "shared-token").UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess" && x.Value == "shared-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_PreferBoundPurpose_AndIgnoreGeneric_WhenBindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "phone");

        result.Should().Be(userId);
    }

    [Fact]
    public void RevokeAllForUser_Should_BeIdempotent_WhenCalledTwice()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "first", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "second", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var first = service.RevokeAllForUser(userId);
        var before = db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh" && x.Value == "first").UsedAtUtc;
        var beforeDeviceBound = db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone" && x.Value == "second").UsedAtUtc;

        var second = service.RevokeAllForUser(userId);
        var after = db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh" && x.Value == "first").UsedAtUtc;
        var afterDeviceBound = db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone" && x.Value == "second").UsedAtUtc;

        first.Should().Be(2);
        second.Should().Be(2);
        after.Should().Be(before);
        afterDeviceBound.Should().Be(beforeDeviceBound);
    }

    [Fact]
    public void RevokeAllForUser_Should_NotRevokeAlreadyUsedAndShouldPreserveUsedTimestamp()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var usedAt = DateTime.UtcNow.AddMinutes(-5);
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        var usedRow = new UserToken(userId, "JwtRefresh", "used-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        };
        var unusedRow = new UserToken(userId, "JwtRefresh:phone", "unused-token", DateTime.UtcNow.AddDays(1));
        db.Set<UserToken>().AddRange(usedRow, unusedRow);
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeAllForUser(userId);

        db.Set<UserToken>().Single(x => x.Value == "used-token" && x.Purpose == "JwtRefresh").UsedAtUtc.Should().Be(usedAt);
        db.Set<UserToken>().Single(x => x.Value == "unused-token" && x.Purpose == "JwtRefresh:phone").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenTokenIsWhitespace_AndSiteSettingMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var service = new JwtTokenService(db);

        var result = service.ValidateRefreshToken("  ", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenIsWhitespace_AndSiteSettingMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var service = new JwtTokenService(db);

        Action act = () => service.RevokeRefreshToken("   ", null);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenBindingDisabledAndGenericTokenExistsWithDeviceBoundSibling()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddHours(2)),
            new UserToken(userId, "JwtRefresh:tablet", "refresh-token", DateTime.UtcNow.AddHours(2)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().Be(userId);
    }

    [Fact]
    public void IssueTokens_Should_InvalidateAllRefreshRows_WhenSingleDeviceOnlyEnabledAndDeviceBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = false,
            JwtSingleDeviceOnly = true
        });
        var genericOld = "old-generic";
        var boundOld = "old-device";
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", genericOld, DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", boundOld, DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "phone");

        var genericRow = db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh");
        var boundRow = db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone");
        genericRow.Value.Should().Be(issued.refreshToken);
        genericRow.UsedAtUtc.Should().BeNull();
        boundRow.UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_NotReturnOnlyActiveRows_ButAllRefreshRows()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var pastUsed = DateTime.UtcNow.AddDays(-10);
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        var expiredUsed = new UserToken(userId, "JwtRefresh", "used-expired", DateTime.UtcNow.AddDays(-1))
        {
            UsedAtUtc = pastUsed
        };
        var activeRefresh = new UserToken(userId, "JwtRefresh:phone", "unused", DateTime.UtcNow.AddDays(1));
        var expiredUnused = new UserToken(userId, "JwtRefresh:tablet", "unused-tablet", DateTime.UtcNow.AddDays(-1));
        db.Set<UserToken>().AddRange(expiredUsed, activeRefresh, expiredUnused);
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(3);
        db.Set<UserToken>().Single(x => x.Value == "used-expired").UsedAtUtc.Should().Be(pastUsed);
        db.Set<UserToken>().Single(x => x.Value == "unused").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Value == "unused-tablet").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void IssueTokens_Should_EmitCommaSeparatedScope_WhenScopeItemsContainEmptyEntries()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtEmitScopes = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(
            Guid.NewGuid(),
            "user@example.com",
            null,
            scopes: new[] { "read", "", "write", null!, "admin" });
        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);

        token.Claims.Single(c => c.Type == "scope").Value.Should().Be("read,,write,,admin");
    }

    [Fact]
    public void IssueTokens_Should_GenerateUniqueAccessToken_OnEachCall()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);

        var first = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);
        var second = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        first.accessToken.Should().NotBe(second.accessToken);
    }

    [Fact]
    public void IssueTokens_Should_PreferPreferredBusiness_WhenPreferredBusinessIsActive()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var preferredId = Guid.Parse("11111111-1111-1111-1111-aaaaaaaaaaaa");
        var alternateId = Guid.Parse("11111111-1111-1111-1111-bbbbbbbbbbbb");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = preferredId, IsActive = true },
            new Business { Id = alternateId, IsActive = true });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = preferredId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = alternateId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: preferredId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Single(c => c.Type == "business_id").Value.Should().Be(preferredId.ToString("D"));
    }

    [Fact]
    public void ValidateRefreshToken_Should_IgnoreJwtRefreshPrefixOnly_WhenPurposeIsNotExact()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefreshable", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_DoNothing_WhenTokenNotFound_AndDeviceIdWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "real-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("missing-token", "   ");

        db.Set<UserToken>().Single(x => x.Value == "real-token" && x.Purpose == "JwtRefresh:phone")
            .UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_DecodeJwtSigningKeyFromBase64()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        // "secret" in base64.
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "c2VjcmV0" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Should().NotBeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenRefreshTokenIsExpired()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "expired-token", DateTime.UtcNow.AddMinutes(-5)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("expired-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenRefreshTokenIsAlreadyUsed()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var usedAt = DateTime.UtcNow.AddMinutes(-1);
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "used-token", DateTime.UtcNow.AddHours(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("used-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_Throw_WhenSiteSettingIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<UserToken>().Add(new UserToken(Guid.NewGuid(), "JwtRefresh", "some-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);

        Action act = () => service.ValidateRefreshToken("some-token", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RevokeRefreshToken_Should_Throw_WhenSiteSettingIsMissing()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<UserToken>().Add(new UserToken(Guid.NewGuid(), "JwtRefresh", "some-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);

        Action act = () => service.RevokeRefreshToken("some-token", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IssueTokens_Should_NotEmitBusinessId_WhenNoActiveBusinessMembership()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Should().NotContain(c => c.Type == "business_id");
    }

    [Fact]
    public void IssueTokens_Should_ChooseLowestActiveBusinessId_WhenNoPreferredBusiness()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var lowestBusinessId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var mediumBusinessId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var highestBusinessId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = highestBusinessId },
            new Business { Id = lowestBusinessId },
            new Business { Id = mediumBusinessId });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = highestBusinessId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = mediumBusinessId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = lowestBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.FirstOrDefault(c => c.Type == "business_id")!.Value.Should().Be(lowestBusinessId.ToString("D"));
    }

    [Fact]
    public void IssueTokens_Should_FallBackToActiveBusiness_WhenPreferredBusinessIsInactive()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var inactivePreferredBusinessId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var activeFallbackBusinessId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = inactivePreferredBusinessId, IsActive = false },
            new Business { Id = activeFallbackBusinessId, IsActive = true });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = inactivePreferredBusinessId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = activeFallbackBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(
            userId,
            "user@example.com",
            null,
            preferredBusinessId: inactivePreferredBusinessId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.FirstOrDefault(c => c.Type == "business_id")!.Value.Should().Be(activeFallbackBusinessId.ToString("D"));
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeDeviceBoundRefreshToken_WhenDeviceBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        var deviceBound = "bound-token";
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone-1", deviceBound, DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken(deviceBound, null);

        db.Set<UserToken>().Single(x => x.Value == deviceBound).UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void IssueTokens_Should_IncludeIatClaim_AsUnixTimestamp()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issuedAt = DateTime.UtcNow;

        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        var iat = token.Claims.SingleOrDefault(c => c.Type == JwtRegisteredClaimNames.Iat);
        iat.Should().NotBeNull();

        long iatValue = 0;
        long.TryParse(iat!.Value, out iatValue).Should().BeTrue();
        var iatDate = DateTimeOffset.FromUnixTimeSeconds(iatValue).UtcDateTime;
        iatDate.Should().BeCloseTo(issuedAt, TimeSpan.FromSeconds(8));
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenBindingEnabledAndDeviceIdIsWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("token", "   ");

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotRevoke_WhenBindingEnabledAndDeviceIdIsWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("token", "   ");

        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone" && x.Value == "token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_IgnoreDeletedBusinessMemberships_WhenSelectingBusiness()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var validBusinessId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var deletedBusinessId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var deletedMembershipBusinessId = Guid.Parse("88888888-8888-8888-8888-888888888888");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = validBusinessId, IsActive = true },
            new Business { Id = deletedBusinessId, IsActive = true },
            new Business { Id = deletedMembershipBusinessId, IsActive = true });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = deletedBusinessId, IsActive = true, IsDeleted = true },
            new BusinessMember { UserId = userId, BusinessId = deletedMembershipBusinessId, IsActive = true, IsDeleted = false },
            new BusinessMember { UserId = userId, BusinessId = validBusinessId, IsActive = true, IsDeleted = false });
        // make the active membership point to deleted business so it must be ignored despite active flag.
        db.Set<Business>().Single(x => x.Id == deletedMembershipBusinessId).IsActive = false;
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Single(c => c.Type == "business_id").Value.Should().Be(validBusinessId.ToString("D"));
    }

    [Fact]
    public void IssueTokens_Should_SelectPreferredBusiness_WhenPreferredIsActive_AndOtherBusinessInactive()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var preferredBusinessId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var inactiveBusinessId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var otherActiveBusinessId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = preferredBusinessId, IsActive = true },
            new Business { Id = inactiveBusinessId, IsActive = false },
            new Business { Id = otherActiveBusinessId, IsActive = true });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = preferredBusinessId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = inactiveBusinessId, IsActive = true },
            new BusinessMember { UserId = userId, BusinessId = otherActiveBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: preferredBusinessId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Single(c => c.Type == "business_id").Value.Should().Be(preferredBusinessId.ToString("D"));
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeExactPurpose_WhenBindingEnabledAndDeviceIdMatches()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh:phone", "token", DateTime.UtcNow.AddHours(1)),
            new UserToken(userId, "JwtRefresh:tablet", "token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("token", "phone");

        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone" && x.Value == "token").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:tablet" && x.Value == "token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenTokenHasOuterWhitespace()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken(" refresh-token ", null);

        result.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_OverwriteExistingDeviceBoundRefreshToken_ForSameDevice()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var deviceId = "phone";
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRequireDeviceBinding = true
        });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "old-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", deviceId);

        db.Set<UserToken>().Where(x => x.UserId == userId && x.Purpose == $"JwtRefresh:{deviceId}").Should().HaveCount(1);
        db.Set<UserToken>().Single(x => x.Purpose == $"JwtRefresh:{deviceId}")
            .Value.Should().Be(issued.refreshToken);
    }

    [Fact]
    public void IssueTokens_Should_PersistRefreshTokenWithConfiguredExpiration_RoundedToDays()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var now = DateTime.UtcNow;
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtRefreshTokenDays = 7
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        issued.refreshExpiresAtUtc.Should().BeCloseTo(now.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IssueTokens_Should_NotEmitBusinessId_WhenPreferredBusinessIsInactiveAndNoActiveFallback()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var preferredBusinessId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().Add(new Business { Id = preferredBusinessId, IsActive = false });
        db.Set<BusinessMember>().Add(new BusinessMember { UserId = userId, BusinessId = preferredBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: preferredBusinessId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Should().NotContain(c => c.Type == "business_id");
    }

    [Fact]
    public void ValidateRefreshToken_Should_SelectExpiredRefreshOverUsedRefresh_AndReturnNull()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(-1)),
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1))
            {
                UsedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_IgnoreNonRefreshRows()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtAccess", "access-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "Otp", "otp-token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(1);
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess").UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "Otp").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_HandleUserWithNoTokens()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(0);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenBindingEnabledAndOnlyGenericRefreshTokenExists()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("token", "phone");

        result.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_ResetUsedAtUtc_WhenOverwritingExistingRefreshRow()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var usedAt = DateTime.UtcNow.AddMinutes(-10);
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "old-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        var row = db.Set<UserToken>().Single(x => x.UserId == userId && x.Purpose == "JwtRefresh");
        row.Value.Should().Be(issued.refreshToken);
        row.UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_NotAffectOtherUsersTokens()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userA, "JwtRefresh", "a-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userB, "JwtRefresh", "b-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userA);

        revoked.Should().Be(1);
        db.Set<UserToken>().Single(x => x.UserId == userA && x.Purpose == "JwtRefresh").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.UserId == userB && x.Purpose == "JwtRefresh").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_Throw_WhenEmailIsNull()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.IssueTokens(Guid.NewGuid(), null!, null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeOnlyOneRefreshRow_WhenDuplicateValueAcrossRefreshRowsAndBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "dup-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:device", "dup-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("dup-token", null);

        db.Set<UserToken>().Count(x => x.Value == "dup-token" && x.UsedAtUtc != null).Should().Be(1);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenGenericRefreshIsUsedAndDeviceBoundSiblingIsValid()
{
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "token", DateTime.UtcNow.AddHours(1))
            {
                UsedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            },
            new UserToken(userId, "JwtRefresh:phone", "token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_HandlePreferredBusinessGuidWhenUserHasNoMembership()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var preferredBusinessId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().Add(new Business { Id = preferredBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: preferredBusinessId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Should().NotContain(c => c.Type == "business_id");
    }

    [Fact]
    public void RevokeAllForUser_Should_NotModifyRows_WhenOnlyNonMatchingPurposeRowsExist()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "Otp", "otp-token", DateTime.UtcNow.AddHours(1)),
            new UserToken(userId, "JwtAccess", "access-token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(0);
        db.Set<UserToken>().Single(x => x.Purpose == "Otp").UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_NotAddBusinessId_WhenBusinessIsDeleted()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var businessId = Guid.Parse("efefefef-efef-efef-efef-efefefefefef");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().Add(new Business { Id = businessId, IsActive = true, IsDeleted = true });
        db.Set<BusinessMember>().Add(new BusinessMember { UserId = userId, BusinessId = businessId, IsActive = true, IsDeleted = false });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Should().NotContain(c => c.Type == "business_id");
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnUserId_WhenExpirationIsNull()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1))
        {
            ExpiresAtUtc = null
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().Be(userId);
    }

    [Fact]
    public void ValidateRefreshToken_Should_BeCaseSensitiveForValue()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "AbCdEf", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("abcdef", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokePrefixLikeJwtRefreshablePurpose_WhenBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefreshable", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token" && x.Purpose == "JwtRefreshable").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void IssueTokens_Should_KeepRefreshTokenLengthAt64HexChars()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        issued.refreshToken.Should().HaveLength(64);
        issued.refreshToken.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Fact]
    public void RevokeAllForUser_Should_NotChangeUsedTimestamp_WhenAlreadyUsed()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var usedAt = DateTime.UtcNow.AddMinutes(-20);
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "old-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeAllForUser(userId);

        db.Set<UserToken>().Single(x => x.Value == "old-token").UsedAtUtc.Should().Be(usedAt);
    }

    [Fact]
    public void IssueTokens_Should_FallbackToUtf8ForNonBase64SigningKey()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "not-a-valid-base64-key-with-#chars!" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(Guid.NewGuid(), "user@example.com", null);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Should().NotBeNull();
    }

    [Fact]
    public void IssueTokens_Should_CreateGenericRefreshToken_WhenDeviceBindingDisabledAndBoundTokenExists()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "legacy-device-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "phone");

        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone" && x.Value == "legacy-device-token").UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh" && x.Value == issued.refreshToken).Should().NotBeNull();
    }

    [Fact]
    public void RevokeAllForUser_Should_RevokeRowsWithNullExpiration()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "no-expire-token", DateTime.UtcNow.AddDays(1))
        {
            ExpiresAtUtc = null
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(1);
        db.Set<UserToken>().Single(x => x.Value == "no-expire-token").UsedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void IssueTokens_Should_RejectInactivePreferredBusinessMembership()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var preferredBusinessId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var fallbackBusinessId = Guid.Parse("87654321-4321-4321-4321-210987654321");

        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<Business>().AddRange(
            new Business { Id = preferredBusinessId, IsActive = true },
            new Business { Id = fallbackBusinessId, IsActive = true });
        db.Set<BusinessMember>().AddRange(
            new BusinessMember { UserId = userId, BusinessId = preferredBusinessId, IsActive = false },
            new BusinessMember { UserId = userId, BusinessId = fallbackBusinessId, IsActive = true });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null, preferredBusinessId: preferredBusinessId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.accessToken);
        token.Claims.Single(c => c.Type == "business_id").Value.Should().Be(fallbackBusinessId.ToString("D"));
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenRefreshTokenMatchesGenericButUsedAtIsSet()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddHours(2))
        {
            UsedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_IgnoreMissingDeviceId_WhenBindingEnabledAndValueBelongsToDifferentDevice()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        var userId = Guid.NewGuid();
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:tablet", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:tablet" && x.Value == "shared-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeValueAcrossUsers_WhenTokenCollidesAcrossUsersAndBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userA, "JwtRefresh", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userB, "JwtRefresh", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Single(x => x.UserId == userA).UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.UserId == userB).UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenBindingEnabledAndDeviceIdDoesNotMatch()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh:phone", "refresh-token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", "tablet");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_NotReturnUser_WhenTokenMatchesNonRefreshPurpose()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtAccess", "refresh-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("refresh-token", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_PreserveUsedTimestamp_WhenCalledTwiceAfterUsed()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        var usedAt = DateTime.UtcNow.AddMinutes(-15);
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1))
        {
            UsedAtUtc = usedAt
        });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("refresh-token", null);

        db.Set<UserToken>().Single(x => x.Value == "refresh-token").UsedAtUtc.Should().Be(usedAt);
    }

    [Fact]
    public void IssueTokens_Should_IgnoreWhitespaceDeviceId_WhenBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", "   ");

        db.Set<UserToken>().Single(x => x.UserId == userId).Purpose.Should().Be("JwtRefresh");
        db.Set<UserToken>().Single(x => x.UserId == userId).Value.Should().Be(issued.refreshToken);
    }

    [Fact]
    public void RevokeAllForUser_Should_OnlyTargetRefreshPurposeRows_WithSameUser()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "refresh-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "bound-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtAccess", "access-token", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var revoked = service.RevokeAllForUser(userId);

        revoked.Should().Be(2);
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtAccess").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_RevokeOnlyOneMatchingRefreshRow_WhenBindingDisabledAndValueSharedAcrossRefreshPurposes()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "shared-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtAccess", "shared-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("shared-token", null);

        db.Set<UserToken>().Count(x => x.Value == "shared-token" && x.Purpose.StartsWith("JwtRefresh") && x.UsedAtUtc != null).Should().Be(1);
    }

    [Fact]
    public void ValidateRefreshToken_Should_ReturnNull_WhenRefreshRowsForSameTokenAreAllExpiredOrUsed()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "duplicate", DateTime.UtcNow.AddDays(-1)),
            new UserToken(userId, "JwtRefresh", "duplicate", DateTime.UtcNow.AddDays(1))
            {
                UsedAtUtc = DateTime.UtcNow.AddMinutes(-1)
            });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var result = service.ValidateRefreshToken("duplicate", null);

        result.Should().BeNull();
    }

    [Fact]
    public void RevokeRefreshToken_Should_NotThrow_WhenNoMatchingRefreshRowExists()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = false });
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "known-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        Action act = () => service.RevokeRefreshToken("unknown-token", null);

        act.Should().NotThrow();
        db.Set<UserToken>().Single(x => x.Value == "known-token").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void ValidateRefreshToken_Should_Throw_WhenSiteSettingIsMissingAndTokenIsNotBlank()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<UserToken>().Add(new UserToken(userId, "JwtRefresh", "token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);

        Action act = () => service.ValidateRefreshToken("token", null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RevokeRefreshToken_Should_SelectExactDevicePurpose_WhenBindingEnabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtRequireDeviceBinding = true });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh:phone", "token-phone", DateTime.UtcNow.AddHours(1)),
            new UserToken(userId, "JwtRefresh:tablet", "token-phone", DateTime.UtcNow.AddHours(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.RevokeRefreshToken("token-phone", "phone");

        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone" && x.Value == "token-phone").UsedAtUtc.Should().NotBeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:tablet" && x.Value == "token-phone").UsedAtUtc.Should().BeNull();
    }

    [Fact]
    public void IssueTokens_Should_KeepJwtRefreshPurpose_WhenSingleDeviceOnlyDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret", JwtSingleDeviceOnly = false, JwtRequireDeviceBinding = false });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var issued = service.IssueTokens(userId, "user@example.com", null);

        db.Set<UserToken>().Single(x => x.UserId == userId && x.Value == issued.refreshToken).Purpose.Should().Be("JwtRefresh");
    }

    [Fact]
    public void IssueTokens_Should_GenerateDifferentRefreshTokens_ForConsecutiveCalls()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting { JwtSigningKey = "secret" });
        db.SaveChanges();

        var service = new JwtTokenService(db);
        var first = service.IssueTokens(userId, "user@example.com", null);
        var second = service.IssueTokens(userId, "user@example.com", null);

        first.refreshToken.Should().NotBe(second.refreshToken);
    }

    [Fact]
    public void IssueTokens_Should_RevokeEverythingWithSingleDeviceOnly_ThenRefreshRowCountShouldEqualOne_WhenBindingDisabled()
    {
        using var db = JwtTokenServiceTestDbContext.Create();
        var userId = Guid.NewGuid();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            JwtSigningKey = "secret",
            JwtSingleDeviceOnly = true,
            JwtRequireDeviceBinding = false
        });
        db.Set<UserToken>().AddRange(
            new UserToken(userId, "JwtRefresh", "old-token", DateTime.UtcNow.AddDays(1)),
            new UserToken(userId, "JwtRefresh:phone", "old-device-token", DateTime.UtcNow.AddDays(1)));
        db.SaveChanges();

        var service = new JwtTokenService(db);
        service.IssueTokens(userId, "user@example.com", null);

        db.Set<UserToken>().Count(x => x.UserId == userId).Should().Be(2);
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh").UsedAtUtc.Should().BeNull();
        db.Set<UserToken>().Single(x => x.Purpose == "JwtRefresh:phone").UsedAtUtc.Should().NotBeNull();
    }

}

file sealed class JwtTokenServiceTestDbContext : DbContext, IAppDbContext
{
    private JwtTokenServiceTestDbContext(DbContextOptions<JwtTokenServiceTestDbContext> options)
        : base(options)
    {
    }

    public new DbSet<T> Set<T>() where T : class => base.Set<T>();

    public static JwtTokenServiceTestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<JwtTokenServiceTestDbContext>()
            .UseInMemoryDatabase($"darwin_jwt_token_service_tests_{Guid.NewGuid()}")
            .Options;
        return new JwtTokenServiceTestDbContext(options);
    }
}
