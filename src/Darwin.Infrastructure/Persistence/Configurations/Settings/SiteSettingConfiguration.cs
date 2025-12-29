using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Settings
{
    /// <summary>
    /// EF Core configuration for <see cref="SiteSetting"/>.
    /// Maps all properties with sensible lengths and requiredness.
    /// Intended for a single-row table (but not strictly enforced to allow versioning/multi-tenant later).
    /// </summary>
    public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
    {
        public void Configure(EntityTypeBuilder<SiteSetting> builder)
        {
            builder.ToTable("SiteSettings", schema: "Settings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            // --- General ---
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.LogoUrl).HasMaxLength(500);
            builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(256);
            builder.Property(x => x.HomeSlug).IsRequired().HasMaxLength(200);

            builder.Property(x => x.DefaultCulture).IsRequired().HasMaxLength(10);
            builder.Property(x => x.SupportedCulturesCsv).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.DefaultCountry).IsRequired().HasMaxLength(2);
            builder.Property(x => x.DefaultCurrency).IsRequired().HasMaxLength(3);

            builder.Property(x => x.TimeZone).IsRequired().HasMaxLength(100);
            builder.Property(x => x.DateFormat).IsRequired().HasMaxLength(50);
            builder.Property(x => x.TimeFormat).IsRequired().HasMaxLength(50);

            // --- Display & Units ---
            builder.Property(x => x.MeasurementSystem).IsRequired().HasMaxLength(20);          // "Metric"/"Imperial"
            builder.Property(x => x.DisplayWeightUnit).IsRequired().HasMaxLength(10);          // "kg","lb"
            builder.Property(x => x.DisplayLengthUnit).IsRequired().HasMaxLength(10);          // "cm","in"
            builder.Property(x => x.MeasurementSettingsJson).HasMaxLength(2000);
            builder.Property(x => x.NumberFormattingOverridesJson).HasMaxLength(2000);

            // --- SEO ---
            builder.Property(x => x.SeoTitleTemplate).HasMaxLength(150);
            builder.Property(x => x.SeoMetaDescriptionTemplate).HasMaxLength(200);
            builder.Property(x => x.OpenGraphDefaultsJson).HasMaxLength(2000);
            builder.Property(x => x.EnableCanonical).IsRequired();
            builder.Property(x => x.HreflangEnabled).IsRequired();

            // --- Analytics ---
            builder.Property(x => x.GoogleAnalyticsId).HasMaxLength(50);
            builder.Property(x => x.GoogleTagManagerId).HasMaxLength(50);
            builder.Property(x => x.GoogleSearchConsoleVerification).HasMaxLength(200);

            // --- Feature flags ---
            builder.Property(x => x.FeatureFlagsJson).HasMaxLength(2000);

            // --- WhatsApp ---
            builder.Property(x => x.WhatsAppEnabled).IsRequired();
            builder.Property(x => x.WhatsAppBusinessPhoneId).HasMaxLength(50);
            builder.Property(x => x.WhatsAppAccessToken).HasMaxLength(200);
            builder.Property(x => x.WhatsAppFromPhoneE164).HasMaxLength(32);
            builder.Property(x => x.WhatsAppAdminRecipientsCsv).HasMaxLength(1000);

            // --- WebAuthn ---
            builder.Property(x => x.WebAuthnRelyingPartyId).IsRequired().HasMaxLength(255);
            builder.Property(x => x.WebAuthnRelyingPartyName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.WebAuthnAllowedOriginsCsv).IsRequired().HasMaxLength(2000);
            builder.Property(x => x.WebAuthnRequireUserVerification).IsRequired();

            // --- Mobile app bootstrap (single source of truth for WebApi "bootstrap") ---
            builder.Property(x => x.MobileQrTokenRefreshSeconds).IsRequired();
            builder.Property(x => x.MobileMaxOutboxItems).IsRequired();

            // --- Email (SMTP) ---
            builder.Property(x => x.SmtpEnabled).IsRequired();
            builder.Property(x => x.SmtpHost).HasMaxLength(255);
            builder.Property(x => x.SmtpPort);
            builder.Property(x => x.SmtpEnableSsl).IsRequired();
            builder.Property(x => x.SmtpUsername).HasMaxLength(256);
            builder.Property(x => x.SmtpPassword).HasMaxLength(512);          // consider at-rest protection later
            builder.Property(x => x.SmtpFromAddress).HasMaxLength(256);
            builder.Property(x => x.SmtpFromDisplayName).HasMaxLength(200);

            // --- SMS ---
            builder.Property(x => x.SmsEnabled).IsRequired();
            builder.Property(x => x.SmsProvider).HasMaxLength(100);
            builder.Property(x => x.SmsFromPhoneE164).HasMaxLength(32);
            builder.Property(x => x.SmsApiKey).HasMaxLength(256);
            builder.Property(x => x.SmsApiSecret).HasMaxLength(512);
            builder.Property(x => x.SmsExtraSettingsJson).HasMaxLength(2000);

            // --- Admin notification defaults ---
            builder.Property(x => x.AdminAlertEmailsCsv).HasMaxLength(2000);
            builder.Property(x => x.AdminAlertSmsRecipientsCsv).HasMaxLength(1000);

            // Helpful lookups
            builder.HasIndex(x => x.ContactEmail);
            builder.HasIndex(x => x.DefaultCulture);
            builder.HasIndex(x => x.DefaultCurrency);

            builder.HasIndex(x => x.Id).IsUnique(); // keep flexibility for versioning/multi-tenant

            // Optional: enforce a single active row by unique filtered index (disabled by default)

            // Enforce single active row (SQL Server filtered unique index).
            //builder.HasIndex(x => x.IsActive)
            //       .IsUnique()
            //       .HasDatabaseName("UX_SiteSetting_IsActive_Singleton")
            //       .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
        }
    }
}
