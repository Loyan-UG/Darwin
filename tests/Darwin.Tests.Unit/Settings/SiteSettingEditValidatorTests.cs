using System;
using Darwin.Application;
using Darwin.Application.Settings.DTOs;
using Darwin.Application.Settings.Validators;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Settings;

/// <summary>
/// Unit tests for <see cref="SiteSettingEditValidator"/>.
/// Covers the key validation groups: identity, basic info, JWT, mobile,
/// localization, measurement, VAT, shipping, soft-delete, and
/// communication channels.
/// </summary>
public sealed class SiteSettingEditValidatorTests
{
    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        return mock.Object;
    }

    /// <summary>Creates a minimal DTO that satisfies all validator rules.</summary>
    private static SiteSettingDto CreateValidDto() => new()
    {
        Id = Guid.NewGuid(),
        RowVersion = new byte[] { 1 },
        Title = "Darwin Shop",
        JwtIssuer = "Darwin",
        JwtAudience = "Darwin.PublicApi",
        JwtAccessTokenMinutes = 15,
        JwtRefreshTokenDays = 30,
        JwtSigningKey = new string('k', 32),
        JwtClockSkewSeconds = 60,
        MobileQrTokenRefreshSeconds = 30,
        MobileMaxOutboxItems = 100,
        DefaultVatRatePercent = 19m,
        ShipmentAttentionDelayHours = 24,
        ShipmentTrackingGraceHours = 12,
        SoftDeleteRetentionDays = 90,
        SoftDeleteCleanupBatchSize = 500,
        DefaultCulture = "de-DE",
        SupportedCulturesCsv = "de-DE,en-US",
        DefaultCurrency = "EUR",
        MeasurementSystem = "Metric",
        WebAuthnRelyingPartyId = "localhost",
        WebAuthnRelyingPartyName = "Darwin",
        WebAuthnAllowedOriginsCsv = "https://localhost:5001",
    };

    // ─── Identity ─────────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Pass_For_Minimal_Valid_Dto()
    {
        var dto = CreateValidDto();

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a fully valid settings DTO should pass all rules");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Id_Is_Empty()
    {
        var dto = CreateValidDto();
        dto.Id = Guid.Empty;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Id must not be empty");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Id));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_RowVersion_Is_Null()
    {
        var dto = CreateValidDto();
        dto.RowVersion = null!;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("RowVersion must not be null");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.RowVersion));
    }

    // ─── Basic site info ──────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_Title_Is_Empty()
    {
        var dto = CreateValidDto();
        dto.Title = "";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Title is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.Title));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Title_Too_Long()
    {
        var dto = CreateValidDto();
        dto.Title = new string('T', 201);

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Title must not exceed 200 characters");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_ContactEmail_Invalid()
    {
        var dto = CreateValidDto();
        dto.ContactEmail = "not-an-email";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ContactEmail must be a valid e-mail address when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ContactEmail));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_ContactEmail_Is_Null()
    {
        var dto = CreateValidDto();
        dto.ContactEmail = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("null ContactEmail is allowed");
    }

    // ─── JWT ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtIssuer_Empty()
    {
        var dto = CreateValidDto();
        dto.JwtIssuer = "";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtIssuer is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.JwtIssuer));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtAccessTokenMinutes_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.JwtAccessTokenMinutes = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtAccessTokenMinutes must be >= 1");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.JwtAccessTokenMinutes));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtAccessTokenMinutes_Exceeds_Max()
    {
        var dto = CreateValidDto();
        dto.JwtAccessTokenMinutes = 1441;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtAccessTokenMinutes must be <= 1440 (24 hours)");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtRefreshTokenDays_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.JwtRefreshTokenDays = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtRefreshTokenDays must be >= 1");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.JwtRefreshTokenDays));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtSigningKey_Too_Short()
    {
        var dto = CreateValidDto();
        dto.JwtSigningKey = new string('k', 31);

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtSigningKey must be at least 32 characters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.JwtSigningKey));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtSigningKey_Empty()
    {
        var dto = CreateValidDto();
        dto.JwtSigningKey = "";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtSigningKey is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.JwtSigningKey));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtClockSkewSeconds_Negative()
    {
        var dto = CreateValidDto();
        dto.JwtClockSkewSeconds = -1;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtClockSkewSeconds must be >= 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.JwtClockSkewSeconds));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_JwtPreviousSigningKey_Too_Short()
    {
        var dto = CreateValidDto();
        dto.JwtPreviousSigningKey = "short";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("JwtPreviousSigningKey must be >= 32 characters when set");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.JwtPreviousSigningKey));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_JwtPreviousSigningKey_Is_Null()
    {
        var dto = CreateValidDto();
        dto.JwtPreviousSigningKey = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("JwtPreviousSigningKey is optional");
    }

    // ─── Mobile bootstrap ─────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_MobileQrTokenRefreshSeconds_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.MobileQrTokenRefreshSeconds = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("MobileQrTokenRefreshSeconds must be > 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.MobileQrTokenRefreshSeconds));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_MobileMaxOutboxItems_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.MobileMaxOutboxItems = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("MobileMaxOutboxItems must be > 0");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.MobileMaxOutboxItems));
    }

    // ─── Mobile URLs ──────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_ImpressumUrl_Is_Http()
    {
        var dto = CreateValidDto();
        dto.ImpressumUrl = "http://example.com/impressum";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ImpressumUrl must use HTTPS");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ImpressumUrl));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_ImpressumUrl_Is_Https()
    {
        var dto = CreateValidDto();
        dto.ImpressumUrl = "https://example.com/impressum";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid HTTPS impressum URL is allowed");
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_ImpressumUrl_Is_Null()
    {
        var dto = CreateValidDto();
        dto.ImpressumUrl = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("ImpressumUrl is optional");
    }

    // ─── Localization ─────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_DefaultCulture_Invalid()
    {
        var dto = CreateValidDto();
        dto.DefaultCulture = "zz-INVALID";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DefaultCulture must be a recognized .NET culture");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultCulture));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_DefaultCulture_Empty()
    {
        var dto = CreateValidDto();
        dto.DefaultCulture = "";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DefaultCulture is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultCulture));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_SupportedCulturesCsv_Contains_Invalid_Culture()
    {
        var dto = CreateValidDto();
        dto.SupportedCulturesCsv = "de-DE,not-a-culture";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("all cultures in SupportedCulturesCsv must be valid");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SupportedCulturesCsv));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_DefaultCurrency_Wrong_Format()
    {
        var dto = CreateValidDto();
        dto.DefaultCurrency = "eur"; // lowercase, not matching ^[A-Z]{3}$

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DefaultCurrency must be 3 uppercase letters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultCurrency));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_DefaultCurrency_Wrong_Length()
    {
        var dto = CreateValidDto();
        dto.DefaultCurrency = "EURO";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DefaultCurrency must be exactly 3 characters");
    }

    // ─── Measurement ─────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_MeasurementSystem_Invalid()
    {
        var dto = CreateValidDto();
        dto.MeasurementSystem = "Unknown";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("MeasurementSystem must be 'Metric' or 'Imperial'");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.MeasurementSystem));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_MeasurementSystem_Is_Imperial()
    {
        var dto = CreateValidDto();
        dto.MeasurementSystem = "Imperial";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("'Imperial' is a valid measurement system value");
    }

    // ─── VAT / Invoicing ──────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_DefaultVatRatePercent_Exceeds_100()
    {
        var dto = CreateValidDto();
        dto.DefaultVatRatePercent = 101m;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DefaultVatRatePercent must be <= 100");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DefaultVatRatePercent));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_DefaultVatRatePercent_Negative()
    {
        var dto = CreateValidDto();
        dto.DefaultVatRatePercent = -1m;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DefaultVatRatePercent must be >= 0");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Stripe_Is_Enabled_Without_Required_Secrets()
    {
        var dto = CreateValidDto();
        dto.StripeEnabled = true;
        dto.StripePublishableKey = null;
        dto.StripeSecretKey = null;
        dto.StripeWebhookSecret = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("Stripe cannot be enabled without publishable, secret, and webhook signing keys");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.StripePublishableKey));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.StripeSecretKey));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.StripeWebhookSecret));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_Stripe_Is_Enabled_With_Required_Secrets()
    {
        var dto = CreateValidDto();
        dto.StripeEnabled = true;
        dto.StripePublishableKey = "stripe-publishable-configured";
        dto.StripeSecretKey = "stripe-secret-configured";
        dto.StripeWebhookSecret = "stripe-webhook-configured";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("Stripe is complete enough for test-mode smoke when all required keys are configured");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_InvoiceIssuerCountry_Not_Two_Uppercase_Letters()
    {
        var dto = CreateValidDto();
        dto.InvoiceIssuerCountry = "de"; // lowercase

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("InvoiceIssuerCountry must be two uppercase letters");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.InvoiceIssuerCountry));
    }

    // ─── Shipping ─────────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_ShipmentAttentionDelayHours_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.ShipmentAttentionDelayHours = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("ShipmentAttentionDelayHours must be >= 1");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.ShipmentAttentionDelayHours));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_DhlApiBaseUrl_Is_Http()
    {
        var dto = CreateValidDto();
        dto.DhlApiBaseUrl = "http://api.dhl.com";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DhlApiBaseUrl must use HTTPS");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlApiBaseUrl));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Dhl_Is_Enabled_Without_Required_Provider_Settings()
    {
        var dto = CreateValidDto();
        dto.DhlEnabled = true;
        dto.DhlApiBaseUrl = null;
        dto.DhlApiKey = null;
        dto.DhlApiSecret = null;
        dto.DhlAccountNumber = null;
        dto.DhlShipperName = null;
        dto.DhlShipperEmail = null;
        dto.DhlShipperStreet = null;
        dto.DhlShipperPostalCode = null;
        dto.DhlShipperCity = null;
        dto.DhlShipperCountry = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DHL cannot be enabled without account, credential, and shipper identity settings");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlApiBaseUrl));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlApiKey));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlApiSecret));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlAccountNumber));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlShipperName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlShipperEmail));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlShipperStreet));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlShipperPostalCode));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlShipperCity));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlShipperCountry));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_Dhl_Is_Enabled_With_Required_Provider_Settings()
    {
        var dto = CreateValidDto();
        dto.DhlEnabled = true;
        dto.DhlApiBaseUrl = "https://api-sandbox.dhl.example";
        dto.DhlApiKey = "dhl-key";
        dto.DhlApiSecret = "dhl-secret";
        dto.DhlAccountNumber = "1234567890";
        dto.DhlShipperName = "Darwin GmbH";
        dto.DhlShipperEmail = "shipping@example.test";
        dto.DhlShipperStreet = "Main Street 1";
        dto.DhlShipperPostalCode = "10115";
        dto.DhlShipperCity = "Berlin";
        dto.DhlShipperCountry = "DE";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("DHL is complete enough for live smoke when account, credential, and shipper identity settings are configured");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Sms_Is_Enabled_Without_Required_Twilio_Settings()
    {
        var dto = CreateValidDto();
        dto.SmsEnabled = true;
        dto.SmsProvider = null;
        dto.SmsFromPhoneE164 = null;
        dto.SmsApiKey = null;
        dto.SmsApiSecret = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SMS cannot be enabled without a supported provider and Twilio credentials");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmsProvider));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmsFromPhoneE164));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmsApiKey));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmsApiSecret));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Sms_Is_Enabled_With_Unsupported_Provider()
    {
        var dto = CreateValidDto();
        dto.SmsEnabled = true;
        dto.SmsProvider = "OtherProvider";
        dto.SmsFromPhoneE164 = "+4915700000000";
        dto.SmsApiKey = "sms-key";
        dto.SmsApiSecret = "sms-secret";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("only Twilio is currently implemented for provider-backed SMS");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmsProvider));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_Sms_Is_Enabled_With_Twilio_Settings()
    {
        var dto = CreateValidDto();
        dto.SmsEnabled = true;
        dto.SmsProvider = "Twilio";
        dto.SmsFromPhoneE164 = "+4915700000000";
        dto.SmsApiKey = "sms-key";
        dto.SmsApiSecret = "sms-secret";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("SMS is complete enough for provider-backed dispatch when Twilio settings are configured");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_WhatsApp_Is_Enabled_Without_Required_Meta_Settings()
    {
        var dto = CreateValidDto();
        dto.WhatsAppEnabled = true;
        dto.WhatsAppBusinessPhoneId = null;
        dto.WhatsAppAccessToken = null;
        dto.WhatsAppFromPhoneE164 = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("WhatsApp cannot be enabled without Meta phone id, access token, and sender phone");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.WhatsAppBusinessPhoneId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.WhatsAppAccessToken));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.WhatsAppFromPhoneE164));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_WhatsApp_Is_Enabled_With_Meta_Settings()
    {
        var dto = CreateValidDto();
        dto.WhatsAppEnabled = true;
        dto.WhatsAppBusinessPhoneId = "wa-phone";
        dto.WhatsAppAccessToken = "wa-token";
        dto.WhatsAppFromPhoneE164 = "+4915700000002";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("WhatsApp is complete enough for provider-backed dispatch when Meta settings are configured");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_DhlShipperCountry_Not_Two_Uppercase_Letters()
    {
        var dto = CreateValidDto();
        dto.DhlShipperCountry = "germany";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("DhlShipperCountry must match ^[A-Z]{2}$");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.DhlShipperCountry));
    }

    // ─── Soft-delete retention ────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_SoftDeleteRetentionDays_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.SoftDeleteRetentionDays = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SoftDeleteRetentionDays must be >= 1");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SoftDeleteRetentionDays));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_SoftDeleteCleanupBatchSize_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.SoftDeleteCleanupBatchSize = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SoftDeleteCleanupBatchSize must be >= 1");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SoftDeleteCleanupBatchSize));
    }

    // ─── WebAuthn ─────────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_WebAuthnRelyingPartyId_Empty()
    {
        var dto = CreateValidDto();
        dto.WebAuthnRelyingPartyId = "";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("WebAuthnRelyingPartyId is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.WebAuthnRelyingPartyId));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_WebAuthnAllowedOriginsCsv_Empty()
    {
        var dto = CreateValidDto();
        dto.WebAuthnAllowedOriginsCsv = "";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("WebAuthnAllowedOriginsCsv is required");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.WebAuthnAllowedOriginsCsv));
    }

    // ─── SMTP ─────────────────────────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Fail_When_SmtpPort_Is_Zero()
    {
        var dto = CreateValidDto();
        dto.SmtpPort = 0;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SmtpPort must be > 0 when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmtpPort));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_SmtpPort_Exceeds_65535()
    {
        var dto = CreateValidDto();
        dto.SmtpPort = 65536;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SmtpPort must be <= 65535");
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_SmtpPort_Is_Null()
    {
        var dto = CreateValidDto();
        dto.SmtpPort = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("SmtpPort is optional");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_SmtpFromAddress_Invalid()
    {
        var dto = CreateValidDto();
        dto.SmtpFromAddress = "not-a-valid-email";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SmtpFromAddress must be a valid email when provided");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmtpFromAddress));
    }

    // ─── Phone verification preferred channel ─────────────────────────────────

    [Theory]
    [InlineData("Sms")]
    [InlineData("WhatsApp")]
    [InlineData("sms")]
    [InlineData("whatsapp")]
    [InlineData(null)]
    [InlineData("")]
    public void SiteSetting_Should_Pass_When_PhoneVerificationPreferredChannel_Valid(string? value)
    {
        var dto = CreateValidDto();
        dto.PhoneVerificationPreferredChannel = value;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue($"'{value}' is an accepted channel value");
    }

    [Theory]
    [InlineData("Email")]
    [InlineData("Push")]
    [InlineData("Signal")]
    public void SiteSetting_Should_Fail_When_PhoneVerificationPreferredChannel_Invalid(string value)
    {
        var dto = CreateValidDto();
        dto.PhoneVerificationPreferredChannel = value;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse($"'{value}' is not an accepted channel");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.PhoneVerificationPreferredChannel));
    }

    // ─── AdminTextOverridesJson ───────────────────────────────────────────────

    [Fact]
    public void SiteSetting_Should_Pass_When_AdminTextOverridesJson_Is_Valid()
    {
        var dto = CreateValidDto();
        dto.AdminTextOverridesJson = """{"section":{"key":"value"}}""";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("a valid JSON object with nested string values should pass");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_AdminTextOverridesJson_Is_Invalid_Json()
    {
        var dto = CreateValidDto();
        dto.AdminTextOverridesJson = "not-json";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("invalid JSON must be rejected");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.AdminTextOverridesJson));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_AdminTextOverridesJson_Is_Null()
    {
        var dto = CreateValidDto();
        dto.AdminTextOverridesJson = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("null AdminTextOverridesJson is allowed");
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Smtp_Is_Enabled_Without_Required_Settings()
    {
        var dto = CreateValidDto();
        dto.SmtpEnabled = true;
        dto.SmtpHost = null;
        dto.SmtpPort = null;
        dto.SmtpFromAddress = null;
        dto.SmtpFromDisplayName = null;

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SMTP cannot be enabled without host, port, from address, and display name");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmtpHost));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmtpPort));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmtpFromAddress));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmtpFromDisplayName));
    }

    [Fact]
    public void SiteSetting_Should_Fail_When_Smtp_Username_Is_Configured_Without_Password()
    {
        var dto = CreateValidDto();
        dto.SmtpEnabled = true;
        dto.SmtpHost = "smtp.example.test";
        dto.SmtpPort = 587;
        dto.SmtpUsername = "smtp-user";
        dto.SmtpPassword = null;
        dto.SmtpFromAddress = "noreply@example.test";
        dto.SmtpFromDisplayName = "Darwin";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeFalse("SMTP password is required when username authentication is configured");
        result.Errors.Should().Contain(e => e.PropertyName == nameof(dto.SmtpPassword));
    }

    [Fact]
    public void SiteSetting_Should_Pass_When_Smtp_Is_Enabled_With_Required_Settings()
    {
        var dto = CreateValidDto();
        dto.SmtpEnabled = true;
        dto.SmtpHost = "smtp.example.test";
        dto.SmtpPort = 587;
        dto.SmtpFromAddress = "noreply@example.test";
        dto.SmtpFromDisplayName = "Darwin";

        var result = new SiteSettingEditValidator(CreateLocalizer()).Validate(dto);

        result.IsValid.Should().BeTrue("SMTP is complete enough for dispatch when relay and sender settings are configured");
    }
}
