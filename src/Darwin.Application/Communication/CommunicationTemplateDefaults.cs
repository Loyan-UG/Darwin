using System;
using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Communication
{
    public static class CommunicationTemplateDefaults
    {
        public const string LegacyBusinessInvitationSubjectTemplate = "Invitation to join {business_name} on Darwin";
        public const string LegacyBusinessInvitationBodyTemplate = "<p>Hello,</p><p>{invitation_intro_html}</p>{acceptance_link_html}<p>Your invitation token is:</p><p><code>{token}</code></p><p>This invitation expires at <strong>{expires_at_utc}</strong>.</p><p>Use this token in the Darwin business onboarding flow or contact your administrator if you need assistance.</p>";
        public const string LegacyAccountActivationSubjectTemplate = "Confirm your Darwin account email";
        public const string LegacyAccountActivationBodyTemplate = "<p>Hello,</p><p>Use the following token to confirm the Darwin account email for <strong>{email}</strong>:</p><p><code>{token}</code></p><p>This token expires at <strong>{expires_at_utc}</strong>.</p>";
        public const string LegacyPasswordResetSubjectTemplate = "Reset your Darwin account password";
        public const string LegacyPasswordResetBodyTemplate = "<p>Hello,</p><p>Use the following token to reset the Darwin account password for <strong>{email}</strong>:</p><p><code>{token}</code></p><p>This token expires at <strong>{expires_at_utc}</strong>.</p>";
        public const string LegacyPhoneVerificationSmsTemplate = "Your Darwin verification code is {token}. It expires at {expires_at_utc} UTC.";
        public const string LegacyPhoneVerificationWhatsAppTemplate = "Confirm your Darwin mobile number with code {token}. It expires at {expires_at_utc} UTC.";

        public static string ResolveTemplate(
            IStringLocalizer<CommunicationResource> localizer,
            string? culture,
            string? configuredTemplate,
            string legacySeedTemplate,
            string resourceKey)
        {
            if (!ShouldUseLocalizedDefault(configuredTemplate, legacySeedTemplate))
            {
                return configuredTemplate!;
            }

            return ResolveText(localizer, culture, resourceKey);
        }

        public static string ResolveText(
            IStringLocalizer<CommunicationResource> localizer,
            string? culture,
            string resourceKey)
        {
            var effectiveCulture = NormalizeCulture(culture);
            if (string.IsNullOrWhiteSpace(effectiveCulture))
            {
                return localizer[resourceKey].Value;
            }

            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var targetCulture = CultureInfo.GetCultureInfo(effectiveCulture);
                CultureInfo.CurrentCulture = targetCulture;
                CultureInfo.CurrentUICulture = targetCulture;
                return localizer[resourceKey].Value;
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        public static string? NormalizeCulture(string? culture, string? fallbackCulture = null)
        {
            foreach (var candidate in new[] { culture, fallbackCulture })
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                try
                {
                    return CultureInfo.GetCultureInfo(candidate.Trim()).Name;
                }
                catch (CultureNotFoundException)
                {
                }
            }

            return null;
        }

        private static bool ShouldUseLocalizedDefault(string? configuredTemplate, string legacySeedTemplate)
        {
            if (string.IsNullOrWhiteSpace(configuredTemplate))
            {
                return true;
            }

            return string.Equals(configuredTemplate.Trim(), legacySeedTemplate, StringComparison.Ordinal);
        }
    }
}
