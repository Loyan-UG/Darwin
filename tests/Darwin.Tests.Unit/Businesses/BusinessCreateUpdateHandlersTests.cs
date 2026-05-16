using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Settings;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Covers business create and edit rules that protect the operational approval workflow.
/// </summary>
public sealed class BusinessCreateUpdateHandlersTests
{
    [Fact]
    public async Task CreateBusiness_Should_DefaultPendingApprovalBusinesses_ToInactive()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Title = "Darwin Market",
            LogoUrl = "https://cdn.darwin.test/logo.svg",
            ContactEmail = "ops@darwin.test",
            SmtpFromAddress = "mailer@darwin.test",
            SmtpFromDisplayName = "Darwin Ops"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateBusinessHandler(db, new BusinessCreateDtoValidator(localizer));

        var id = await handler.HandleAsync(new BusinessCreateDto
        {
            Name = "Baeckerei Morgenstern",
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            CustomerEmailNotificationsEnabled = true,
            CustomerMarketingEmailsEnabled = true,
            OperationalAlertEmailsEnabled = true,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.PendingApproval);
        persisted.IsActive.Should().BeFalse();
        persisted.DefaultTimeZoneId.Should().Be("Europe/Berlin");
        persisted.CustomerEmailNotificationsEnabled.Should().BeTrue();
        persisted.CustomerMarketingEmailsEnabled.Should().BeTrue();
        persisted.OperationalAlertEmailsEnabled.Should().BeTrue();
        persisted.ContactEmail.Should().Be("ops@darwin.test");
        persisted.BrandDisplayName.Should().Be("Darwin Market");
        persisted.BrandLogoUrl.Should().Be("https://cdn.darwin.test/logo.svg");
        persisted.SupportEmail.Should().Be("ops@darwin.test");
        persisted.CommunicationSenderName.Should().Be("Darwin Ops");
        persisted.CommunicationReplyToEmail.Should().Be("ops@darwin.test");
    }

    [Fact]
    public async Task CreateBusiness_Should_PreferExplicitCommunicationAndBrandingValues_OverSiteSettingFallbacks()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        db.Set<SiteSetting>().Add(new SiteSetting
        {
            Title = "Darwin Market",
            LogoUrl = "https://cdn.darwin.test/logo.svg",
            ContactEmail = "ops@darwin.test",
            SmtpFromAddress = "mailer@darwin.test",
            SmtpFromDisplayName = "Darwin Ops"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBusinessHandler(db, new BusinessCreateDtoValidator(localizer));

        var id = await handler.HandleAsync(new BusinessCreateDto
        {
            Name = "Baeckerei Morgenstern",
            ContactEmail = "kontakt@morgenstern.de",
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            BrandDisplayName = "Morgenstern",
            BrandLogoUrl = "https://morgenstern.test/logo.png",
            SupportEmail = "support@morgenstern.de",
            CommunicationSenderName = "Morgenstern Team",
            CommunicationReplyToEmail = "reply@morgenstern.de"
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        persisted.ContactEmail.Should().Be("kontakt@morgenstern.de");
        persisted.BrandDisplayName.Should().Be("Morgenstern");
        persisted.BrandLogoUrl.Should().Be("https://morgenstern.test/logo.png");
        persisted.SupportEmail.Should().Be("support@morgenstern.de");
        persisted.CommunicationSenderName.Should().Be("Morgenstern Team");
        persisted.CommunicationReplyToEmail.Should().Be("reply@morgenstern.de");
    }

    [Fact]
    public async Task UpdateBusiness_Should_NotActivatePendingApprovalBusiness()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var entity = CreateBusiness(BusinessOperationalStatus.PendingApproval, isActive: false);

        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator(localizer), localizer);

        await handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion,
            Name = entity.Name,
            LegalName = entity.LegalName,
            Category = entity.Category,
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            DefaultTimeZoneId = entity.DefaultTimeZoneId,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.PendingApproval);
        persisted.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBusiness_Should_AllowApprovedBusiness_ToRemainActive()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var entity = CreateBusiness(BusinessOperationalStatus.Approved, isActive: true);
        entity.ApprovedAtUtc = new DateTime(2030, 1, 5, 8, 0, 0, DateTimeKind.Utc);

        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator(localizer), localizer);

        await handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion,
            Name = "Baeckerei Morgenstern Innenstadt",
            LegalName = entity.LegalName,
            Category = entity.Category,
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            DefaultTimeZoneId = "Europe/Vienna",
            SupportEmail = "support@morgenstern.de",
            CustomerEmailNotificationsEnabled = true,
            CustomerMarketingEmailsEnabled = true,
            OperationalAlertEmailsEnabled = false,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        var persisted = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        persisted.OperationalStatus.Should().Be(BusinessOperationalStatus.Approved);
        persisted.IsActive.Should().BeTrue();
        persisted.Name.Should().Be("Baeckerei Morgenstern Innenstadt");
        persisted.DefaultTimeZoneId.Should().Be("Europe/Vienna");
        persisted.SupportEmail.Should().Be("support@morgenstern.de");
        persisted.CustomerEmailNotificationsEnabled.Should().BeTrue();
        persisted.CustomerMarketingEmailsEnabled.Should().BeTrue();
        persisted.OperationalAlertEmailsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBusiness_Should_Throw_WhenNotFound()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator(localizer), localizer);

        var act = () => handler.HandleAsync(new BusinessEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1],
            Name = "Ghost",
            DefaultCurrency = "EUR",
            DefaultCulture = "en-US",
            DefaultTimeZoneId = "UTC"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent business must raise not-found");
    }

    [Fact]
    public async Task UpdateBusiness_Should_Throw_WhenRowVersionIsEmpty()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var entity = CreateBusiness(BusinessOperationalStatus.Approved, isActive: true);
        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator(localizer), localizer);

        var act = () => handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = [],    // empty → concurrency guard should reject this
            Name = entity.Name,
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            DefaultTimeZoneId = entity.DefaultTimeZoneId
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty RowVersion must be rejected");
    }

    [Fact]
    public async Task UpdateBusiness_Should_Throw_WhenRowVersionIsStale()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var entity = CreateBusiness(BusinessOperationalStatus.Approved, isActive: true);
        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator(localizer), localizer);

        var act = () => handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = [9, 9, 9],    // stale
            Name = entity.Name,
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            DefaultTimeZoneId = entity.DefaultTimeZoneId
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("stale RowVersion must be rejected as a concurrency conflict");
    }

    [Fact]
    public async Task UpdateBusiness_Should_TrimStringFields_WhenWhitespaceProvided()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var entity = CreateBusiness(BusinessOperationalStatus.Approved, isActive: true);
        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator(localizer), localizer);

        await handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion,
            Name = "  Baeckerei Morgenstern  ",
            LegalName = "  Morgenstern GmbH  ",
            SupportEmail = "  support@test.de  ",
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            DefaultTimeZoneId = entity.DefaultTimeZoneId,
            IsActive = true
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        updated.Name.Should().Be("Baeckerei Morgenstern", "name should be trimmed");
        updated.LegalName.Should().Be("Morgenstern GmbH", "legal name should be trimmed");
        updated.SupportEmail.Should().Be("support@test.de", "support email should be trimmed");
    }

    [Fact]
    public async Task UpdateBusiness_Should_NullifyOptionalFields_WhenWhitespaceOnlyProvided()
    {
        await using var db = BusinessCreateUpdateTestDbContext.Create();
        var localizer = new TestStringLocalizer();
        var entity = CreateBusiness(BusinessOperationalStatus.Approved, isActive: true);
        db.Set<Business>().Add(entity);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessHandler(db, new BusinessEditDtoValidator(localizer), localizer);

        await handler.HandleAsync(new BusinessEditDto
        {
            Id = entity.Id,
            RowVersion = entity.RowVersion,
            Name = entity.Name,
            LegalName = "   ",        // whitespace-only → should become null
            TaxId = "   ",
            WebsiteUrl = "   ",
            DefaultCurrency = entity.DefaultCurrency,
            DefaultCulture = entity.DefaultCulture,
            DefaultTimeZoneId = entity.DefaultTimeZoneId
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Business>().AsNoTracking().SingleAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);
        updated.LegalName.Should().BeNull("whitespace-only legal name must be nullified");
        updated.TaxId.Should().BeNull("whitespace-only tax id must be nullified");
        updated.WebsiteUrl.Should().BeNull("whitespace-only website url must be nullified");
    }

    private static Business CreateBusiness(BusinessOperationalStatus status, bool isActive)
    {
        return new Business
        {
            Name = "Baeckerei Morgenstern",
            LegalName = "Morgenstern GmbH",
            Category = BusinessCategoryKind.Bakery,
            DefaultCurrency = "EUR",
            DefaultCulture = "de-DE",
            DefaultTimeZoneId = "Europe/Berlin",
            OperationalStatus = status,
            IsActive = isActive,
            RowVersion = [1, 2, 3]
        };
    }

    private sealed class TestStringLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class BusinessCreateUpdateTestDbContext : DbContext, IAppDbContext
    {
        private BusinessCreateUpdateTestDbContext(DbContextOptions<BusinessCreateUpdateTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessCreateUpdateTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BusinessCreateUpdateTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_create_update_tests_{Guid.NewGuid()}")
                .Options;

            return new BusinessCreateUpdateTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Business>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.DefaultTimeZoneId).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<SiteSetting>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Title).IsRequired();
                builder.Property(x => x.ContactEmail).IsRequired();
                builder.Property(x => x.DefaultCulture).IsRequired();
                builder.Property(x => x.DefaultCurrency).IsRequired();
                builder.Property(x => x.TimeZone).IsRequired();
                builder.Property(x => x.HomeSlug).IsRequired();
                builder.Property(x => x.SupportedCulturesCsv).IsRequired();
                builder.Property(x => x.DefaultCountry).IsRequired();
                builder.Property(x => x.DateFormat).IsRequired();
                builder.Property(x => x.TimeFormat).IsRequired();
                builder.Property(x => x.MeasurementSystem).IsRequired();
                builder.Property(x => x.DisplayWeightUnit).IsRequired();
                builder.Property(x => x.DisplayLengthUnit).IsRequired();
                builder.Property(x => x.WebAuthnRelyingPartyId).IsRequired();
                builder.Property(x => x.WebAuthnRelyingPartyName).IsRequired();
                builder.Property(x => x.WebAuthnAllowedOriginsCsv).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
