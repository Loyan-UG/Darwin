using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Meta.Queries;
using Darwin.Domain.Entities.Settings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Settings;

/// <summary>
/// Unit tests for <see cref="GetAppBootstrapHandler"/>.
/// Covers all failure branches (missing settings, disabled JWT, missing audience,
/// invalid QR refresh, invalid outbox) and the happy-path success case.
/// </summary>
public sealed class MetaQueryHandlerTests
{
    // ─── Shared helpers ──────────────────────────────────────────────────────

    private static IStringLocalizer<Darwin.Application.ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<Darwin.Application.ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    /// <summary>Builds a fully valid <see cref="SiteSetting"/> row.</summary>
    private static SiteSetting BuildValidSetting() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Darwin",
        JwtEnabled = true,
        JwtAudience = "Darwin.PublicApi",
        MobileQrTokenRefreshSeconds = 30,
        MobileMaxOutboxItems = 200,
        DefaultCulture = "de-DE",
        SupportedCulturesCsv = "de-DE,en-US",
        DefaultCurrency = "EUR",
        RowVersion = new byte[] { 1 }
    };

    // ─── Failure branches ────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_Settings_Row_Exists()
    {
        await using var db = BootstrapTestDbContext.Create();
        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("there is no SiteSetting row in the database");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Settings_Row_Is_Soft_Deleted()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.IsDeleted = true;
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("soft-deleted settings rows must not be used");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_JwtEnabled_Is_False()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.JwtEnabled = false;
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("JWT must be enabled for mobile bootstrap to succeed");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_JwtAudience_Is_Null()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.JwtAudience = null;
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("a null JwtAudience is not a valid bootstrap configuration");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_JwtAudience_Is_Whitespace()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.JwtAudience = "   ";
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("whitespace-only JwtAudience must be rejected");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_MobileQrTokenRefreshSeconds_Is_Zero()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.MobileQrTokenRefreshSeconds = 0;
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("zero QR token refresh seconds is not a valid bootstrap configuration");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_MobileQrTokenRefreshSeconds_Is_Negative()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.MobileQrTokenRefreshSeconds = -5;
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("negative QR token refresh seconds is not a valid bootstrap configuration");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_MobileMaxOutboxItems_Is_Zero()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.MobileMaxOutboxItems = 0;
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("zero MobileMaxOutboxItems is not a valid bootstrap configuration");
    }

    // ─── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_Should_Succeed_With_Valid_Settings()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.JwtAudience = "Darwin.PublicApi";
        setting.MobileQrTokenRefreshSeconds = 45;
        setting.MobileMaxOutboxItems = 100;
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.JwtAudience.Should().Be("Darwin.PublicApi");
        result.Value.QrTokenRefreshSeconds.Should().Be(45);
        result.Value.MaxOutboxItems.Should().Be(100);
    }

    [Fact]
    public async Task HandleAsync_Should_Trim_JwtAudience_In_Response()
    {
        await using var db = BootstrapTestDbContext.Create();
        var setting = BuildValidSetting();
        setting.JwtAudience = "  Darwin.PublicApi  ";
        db.Set<SiteSetting>().Add(setting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAppBootstrapHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.JwtAudience.Should().Be("Darwin.PublicApi",
            "the handler must trim the JwtAudience before returning it");
    }

    // ─── In-memory DbContext ──────────────────────────────────────────────────

    private sealed class BootstrapTestDbContext : DbContext, IAppDbContext
    {
        private BootstrapTestDbContext(DbContextOptions<BootstrapTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BootstrapTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BootstrapTestDbContext>()
                .UseInMemoryDatabase($"darwin_bootstrap_{Guid.NewGuid()}")
                .Options;
            return new BootstrapTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SiteSetting>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.DefaultCulture);
                b.Property(x => x.SupportedCulturesCsv);
                b.Property(x => x.DefaultCurrency).HasMaxLength(3);
                b.Property(x => x.JwtAudience).HasMaxLength(200);
            });
        }
    }
}
