using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Settings;
using Darwin.WebApi.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Darwin.WebApi.Tests.Security;

public sealed class JwtSigningParametersProviderTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenScopeFactoryIsMissing()
    {
        Action act = () => new JwtSigningParametersProvider(
            null!,
            new Mock<ILogger<JwtSigningParametersProvider>>().Object,
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("scopeFactory");
    }

    [Fact]
    public void Ctor_Should_Throw_WhenLoggerIsMissing()
    {
        using var rootServices = JwtSecurityTestHarness.CreateServices();
        var scopeFactory = rootServices.GetRequiredService<IServiceScopeFactory>();

        Action act = () => new JwtSigningParametersProvider(
            scopeFactory,
            null!,
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Ctor_Should_Throw_WhenLocalizerIsMissing()
    {
        using var rootServices = JwtSecurityTestHarness.CreateServices();
        var scopeFactory = rootServices.GetRequiredService<IServiceScopeFactory>();

        Action act = () => new JwtSigningParametersProvider(
            scopeFactory,
            new Mock<ILogger<JwtSigningParametersProvider>>().Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validationLocalizer");
    }

    [Fact]
    public void GetParameters_Should_Throw_WhenSiteSettingMissing()
    {
        var provider = JwtSecurityTestHarness.CreateProvider(out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            Action act = () => provider.GetParameters();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JwtValidationSiteSettingsMissing");
            scopeFactory.CreateScopeCallCount.Should().Be(1);
        }
    }

    [Fact]
    public void GetParameters_Should_Throw_WhenJwtIsDisabled()
    {
        var siteSetting = new SiteSetting { JwtEnabled = false, JwtSigningKey = "SigningKey" };
        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            Action act = () => provider.GetParameters();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JwtValidationDisabled");
            scopeFactory.CreateScopeCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void GetParameters_Should_Throw_WhenSigningKeyMissing(string? signingKey)
    {
        var siteSetting = new SiteSetting { JwtSigningKey = signingKey };
        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            Action act = () => provider.GetParameters();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("JwtSigningKeyMissingInSiteSettings");
            scopeFactory.CreateScopeCallCount.Should().Be(1);
        }
    }

    [Fact]
    public void GetParameters_Should_UseDefaults_WhenIssuerAudienceMissing_AndClampNegativeClockSkewToZero()
    {
        var siteSetting = new SiteSetting
        {
            JwtSigningKey = "SigningKey",
            JwtIssuer = null,
            JwtAudience = null,
            JwtClockSkewSeconds = -30
        };

        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            var parameters = provider.GetParameters();

            parameters.Issuer.Should().Be("Darwin");
            parameters.Audience.Should().Be("Darwin.PublicApi");
            parameters.ClockSkew.Should().Be(TimeSpan.Zero);
            parameters.SigningKeys.Should().NotBeEmpty();
            parameters.SigningKeys.Count.Should().Be(1);
            scopeFactory.CreateScopeCallCount.Should().Be(1);
        }
    }

    [Fact]
    public void GetParameters_Should_IncludePreviousKey_WhenConfigured()
    {
        var siteSetting = new SiteSetting
        {
            JwtSigningKey = "CurrentKey",
            JwtPreviousSigningKey = "PreviousKey"
        };

        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            var parameters = provider.GetParameters();

            parameters.SigningKeys.Count.Should().Be(2);
            parameters.SigningKeys.Select(k => Encoding.UTF8.GetString(((SymmetricSecurityKey)k).Key))
                .Should().ContainInOrder("CurrentKey", "PreviousKey");
            scopeFactory.CreateScopeCallCount.Should().Be(1);
        }
    }

    [Theory]
    [InlineData("raw-key-content", "raw-key-content")]
    [InlineData("UmF3QmFzZTY0", "RawBase64")]
    public void GetParameters_Should_ParseSigningKeysAsBase64OrUtf8(string key, string expectedKeyText)
    {
        var siteSetting = new SiteSetting { JwtSigningKey = key };
        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out _, out var rootServices);
        using (rootServices)
        {
            var parameters = provider.GetParameters();

            parameters.SigningKeys.Count.Should().Be(1);
            Encoding.UTF8.GetString(((SymmetricSecurityKey)parameters.SigningKeys[0]).Key)
                .Should().Be(expectedKeyText);
        }
    }

    [Fact]
    public void GetParameters_Should_CacheResult_ForAtLeastOneMinuteWindow()
    {
        var siteSetting = new SiteSetting { JwtSigningKey = "OriginalKey", JwtIssuer = "original-issuer" };
        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            var first = provider.GetParameters();

            JwtSecurityTestHarness.UpdateSiteSetting(rootServices, x => x.JwtIssuer = "changed-issuer");

            var second = provider.GetParameters();

            ReferenceEquals(first, second).Should().BeTrue();
            second.Issuer.Should().Be("original-issuer");
            scopeFactory.CreateScopeCallCount.Should().Be(1);
        }
    }
}

public sealed class JwtBearerOptionsSetupTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenProviderIsMissing()
    {
        Action act = () => new JwtBearerOptionsSetup(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("provider");
    }

    [Fact]
    public void Configure_WithDefaultJwtBearerScheme_Should_SetTokenValidationParameters()
    {
        var siteSetting = new SiteSetting
        {
            JwtIssuer = "issuer-from-db",
            JwtAudience = "audience-from-db",
            JwtSigningKey = "current-signer",
            JwtPreviousSigningKey = "previous-signer",
            JwtClockSkewSeconds = 120
        };

        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out _, out var rootServices);
        using (rootServices)
        {
            var setup = new JwtBearerOptionsSetup(provider);
            var options = new JwtBearerOptions();
            var cached = provider.GetParameters();

            setup.Configure(JwtBearerDefaults.AuthenticationScheme, options);

            options.TokenValidationParameters.Should().NotBeNull();
            options.TokenValidationParameters.ValidateIssuer.Should().BeTrue();
            options.TokenValidationParameters.ValidIssuer.Should().Be("issuer-from-db");
            options.TokenValidationParameters.ValidateAudience.Should().BeTrue();
            options.TokenValidationParameters.ValidAudience.Should().Be("audience-from-db");
            options.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
            options.TokenValidationParameters.RequireExpirationTime.Should().BeTrue();
            options.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
            options.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.FromSeconds(120));
            options.TokenValidationParameters.IssuerSigningKeyResolver.Should().NotBeNull();
            options.TokenValidationParameters.IssuerSigningKeyResolver!(null!, null!, null!, null!)
                .ToList()
                .Should().BeEquivalentTo(cached.SigningKeys);
        }
    }

    [Fact]
    public void Configure_WithNonJwtBearerScheme_Should_NotConfigureOptions()
    {
        var siteSetting = new SiteSetting { JwtSigningKey = "SigningKey" };
        var provider = JwtSecurityTestHarness.CreateProvider(siteSetting, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            var setup = new JwtBearerOptionsSetup(provider);
            var expectedParameters = new TokenValidationParameters();
            var options = new JwtBearerOptions
            {
                TokenValidationParameters = expectedParameters
            };

            setup.Configure("not-the-jwt-scheme", options);

            options.TokenValidationParameters.Should().BeSameAs(expectedParameters);
            scopeFactory.CreateScopeCallCount.Should().Be(0);
        }
    }

    [Fact]
    public void Configure_WithNullSchemeName_Should_NotConfigureOptions()
    {
        var provider = JwtSecurityTestHarness.CreateProvider(new SiteSetting { JwtSigningKey = "SigningKey" }, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            var setup = new JwtBearerOptionsSetup(provider);
            var expectedParameters = new TokenValidationParameters();
            var options = new JwtBearerOptions { TokenValidationParameters = expectedParameters };

            setup.Configure((string?)null, options);

            options.TokenValidationParameters.Should().BeSameAs(expectedParameters);
            scopeFactory.CreateScopeCallCount.Should().Be(0);
        }
    }

    [Fact]
    public void Configure_WithDefaultSchemeName_Should_NotConfigureOptions()
    {
        var provider = JwtSecurityTestHarness.CreateProvider(new SiteSetting { JwtSigningKey = "SigningKey" }, out var scopeFactory, out var rootServices);
        using (rootServices)
        {
            var setup = new JwtBearerOptionsSetup(provider);
            var options = new JwtBearerOptions { };

            setup.Configure(options);

            options.TokenValidationParameters.Should().BeNull();
            scopeFactory.CreateScopeCallCount.Should().Be(0);
        }
    }
}

file static class JwtSecurityTestHarness
{
    public static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddDbContext<JwtSigningParametersTestDbContext>(options => options.UseInMemoryDatabase($"JwtSecurityTests-{Guid.NewGuid()}"));
        services.AddScoped<IAppDbContext, JwtSigningParametersTestDbContext>();
        return services.BuildServiceProvider();
    }

    public static JwtSigningParametersProvider CreateProvider(
        SiteSetting? siteSetting,
        out CountingServiceScopeFactory scopeFactory,
        out ServiceProvider rootServices)
    {
        rootServices = CreateServices();
        using var scope = rootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JwtSigningParametersTestDbContext>();
        if (siteSetting is not null)
        {
            db.Set<SiteSetting>().Add(siteSetting);
            db.SaveChanges();
        }

        scopeFactory = new CountingServiceScopeFactory(rootServices.GetRequiredService<IServiceScopeFactory>());
        return new JwtSigningParametersProvider(
            scopeFactory,
            new Mock<ILogger<JwtSigningParametersProvider>>().Object,
            new TestValidationLocalizer());
    }

    public static void UpdateSiteSetting(ServiceProvider rootServices, Action<SiteSetting> update)
    {
        using var scope = rootServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JwtSigningParametersTestDbContext>();
        var setting = db.Set<SiteSetting>().Single();
        update(setting);
        db.SaveChanges();
    }

    public static JwtSigningParametersProvider CreateProvider(out CountingServiceScopeFactory scopeFactory, out ServiceProvider rootServices)
        => CreateProvider(null, out scopeFactory, out rootServices);
}

file sealed class CountingServiceScopeFactory : IServiceScopeFactory
{
    private readonly IServiceScopeFactory _inner;

    public int CreateScopeCallCount { get; private set; }

    public CountingServiceScopeFactory(IServiceScopeFactory inner) => _inner = inner;

    public IServiceScope CreateScope()
    {
        CreateScopeCallCount++;
        return _inner.CreateScope();
    }
}

file sealed class TestValidationLocalizer : IStringLocalizer<ValidationResource>
{
    public LocalizedString this[string name] => new(name, name);
    public LocalizedString this[string name, params object[] arguments] => new(name, name);
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    public IStringLocalizer WithCulture(CultureInfo culture) => this;
}

file sealed class JwtSigningParametersTestDbContext : DbContext, IAppDbContext
{
    public JwtSigningParametersTestDbContext(DbContextOptions<JwtSigningParametersTestDbContext> options)
        : base(options)
    {
    }

    public new DbSet<T> Set<T>() where T : class => base.Set<T>();
}
