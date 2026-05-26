using System;
using System.Globalization;
using System.Net;
using System.Text;
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
        private const string DefaultBrandName = "Loyan";
        private const string DefaultSupportEmail = "support@loyan.de";
        private const string DefaultFrontOfficeBaseUrl = "https://web.loyan.de";
        private const string DefaultLogoUrl = "https://web.loyan.de/images/DarwinJustLogo.png";

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
                return ResolveLocalizedOrFallback(localizer[resourceKey], resourceKey);
            }

            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var targetCulture = CultureInfo.GetCultureInfo(effectiveCulture);
                CultureInfo.CurrentCulture = targetCulture;
                CultureInfo.CurrentUICulture = targetCulture;
                return ResolveLocalizedOrFallback(localizer[resourceKey], resourceKey);
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

        public static string BuildAccountActionUrl(string path, IReadOnlyDictionary<string, string?> query)
        {
            var normalizedPath = string.IsNullOrWhiteSpace(path) ? "/account" : path.Trim();
            if (!normalizedPath.StartsWith("/", StringComparison.Ordinal))
            {
                normalizedPath = "/" + normalizedPath;
            }

            var builder = new StringBuilder(DefaultFrontOfficeBaseUrl.TrimEnd('/'));
            builder.Append(normalizedPath);
            var separator = '?';
            foreach (var item in query)
            {
                if (string.IsNullOrWhiteSpace(item.Value))
                {
                    continue;
                }

                builder.Append(separator);
                builder.Append(Uri.EscapeDataString(item.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(item.Value));
                separator = '&';
            }

            return builder.ToString();
        }

        public static string BuildActionButtonHtml(string url, string label)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            var safeUrl = HtmlAttributeEncode(url.Trim());
            var safeLabel = HtmlEncode(string.IsNullOrWhiteSpace(label) ? "Open" : label.Trim());
            return $"""
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:24px 0 18px 0;">
                  <tr>
                    <td style="border-radius:8px;background:#295135;">
                      <a href="{safeUrl}" style="display:inline-block;padding:14px 22px;border-radius:8px;background:#295135;color:#ffffff;font-family:Arial,Helvetica,sans-serif;font-size:15px;font-weight:700;line-height:20px;text-decoration:none;">{safeLabel}</a>
                    </td>
                  </tr>
                </table>
                """;
        }

        public static string BuildManualLinkHtml(string url, string intro)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            var safeUrl = HtmlEncode(url.Trim());
            var safeHref = HtmlAttributeEncode(url.Trim());
            var safeIntro = HtmlEncode(string.IsNullOrWhiteSpace(intro)
                ? "If the button does not work, copy this link into your browser:"
                : intro.Trim());
            return $"""
                <p style="margin:18px 0 8px 0;color:#5d6b61;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;">{safeIntro}</p>
                <p style="margin:0 0 18px 0;word-break:break-all;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;">
                  <a href="{safeHref}" style="color:#295135;text-decoration:underline;">{safeUrl}</a>
                </p>
                """;
        }

        public static string WrapTransactionalEmail(
            string title,
            string preheader,
            string bodyHtml,
            string? supportEmail = null)
        {
            var safeTitle = HtmlEncode(string.IsNullOrWhiteSpace(title) ? DefaultBrandName : title.Trim());
            var safePreheader = HtmlEncode(preheader ?? string.Empty);
            var safeSupportEmail = HtmlEncode(string.IsNullOrWhiteSpace(supportEmail) ? DefaultSupportEmail : supportEmail.Trim());
            var safeSupportHref = HtmlAttributeEncode("mailto:" + (string.IsNullOrWhiteSpace(supportEmail) ? DefaultSupportEmail : supportEmail.Trim()));
            return $"""
                <!doctype html>
                <html>
                  <head>
                    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1">
                    <meta name="color-scheme" content="light">
                    <title>{safeTitle}</title>
                  </head>
                  <body style="margin:0;padding:0;background:#f4f7f1;">
                    <span style="display:none!important;visibility:hidden;opacity:0;color:transparent;height:0;width:0;overflow:hidden;mso-hide:all;">{safePreheader}</span>
                    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#f4f7f1;margin:0;padding:0;">
                      <tr>
                        <td align="center" style="padding:32px 16px;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:640px;background:#ffffff;border:1px solid #dfe8d9;border-radius:14px;overflow:hidden;">
                            <tr>
                              <td style="padding:28px 32px 18px 32px;background:#ffffff;border-bottom:1px solid #edf2e8;">
                                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">
                                  <tr>
                                    <td width="56" valign="middle">
                                      <img src="{DefaultLogoUrl}" width="48" height="48" alt="Loyan" style="display:block;border:0;border-radius:12px;">
                                    </td>
                                    <td valign="middle" style="padding-left:14px;">
                                      <div style="font-family:Arial,Helvetica,sans-serif;font-size:20px;font-weight:700;line-height:24px;color:#1f3327;">Loyan</div>
                                      <div style="font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;color:#6d776f;">Commerce and loyalty operations</div>
                                    </td>
                                  </tr>
                                </table>
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:30px 32px 12px 32px;">
                                {bodyHtml}
                              </td>
                            </tr>
                            <tr>
                              <td style="padding:20px 32px 30px 32px;">
                                <p style="margin:0;color:#748077;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;">Need help? Reply to this email or contact <a href="{safeSupportHref}" style="color:#295135;text-decoration:underline;">{safeSupportEmail}</a>.</p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>
                """;
        }

        public static string HtmlEncode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string HtmlAttributeEncode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string ResolveLocalizedOrFallback(LocalizedString localized, string resourceKey)
        {
            if (!localized.ResourceNotFound &&
                !string.Equals(localized.Value, resourceKey, StringComparison.Ordinal))
            {
                return localized.Value;
            }

            return ResolveBuiltInFallback(resourceKey);
        }

        private static string ResolveBuiltInFallback(string resourceKey) =>
            resourceKey switch
            {
                "BusinessInvitationSubjectTemplateDefault" => "You're invited to join {business_name}",
                "BusinessInvitationIntroHtmlDefault" => "<p style=\"margin:0 0 16px 0;color:#36483d;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;\">You have been invited to join <strong>{business_name}</strong> as <strong>{role}</strong>.</p>",
                "BusinessInvitationAcceptanceLinkHtml" => "{acceptance_button_html}{manual_link_html}",
                "BusinessInvitationBodyTemplateDefault" => "<h1 style=\"margin:0 0 14px 0;color:#1f3327;font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:34px;\">Join {business_name}</h1><p style=\"margin:0 0 16px 0;color:#36483d;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;\">Hello,</p>{invitation_intro_html}{acceptance_link_html}<p style=\"margin:16px 0 8px 0;color:#5d6b61;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;\">Invitation token:</p><p style=\"margin:0 0 18px 0;padding:12px 14px;border-radius:8px;background:#f3f6ef;color:#1f3327;font-family:Consolas,Monaco,monospace;font-size:13px;line-height:18px;word-break:break-all;\">{token}</p><p style=\"margin:0;color:#5d6b61;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;\">This invitation expires at <strong>{expires_at_utc}</strong>. Contact your administrator if you need assistance.</p>",
                "AccountActivationSubjectTemplateDefault" => "Confirm your Loyan email address",
                "AccountActivationBodyTemplateDefault" => "<h1 style=\"margin:0 0 14px 0;color:#1f3327;font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:34px;\">Confirm your email</h1><p style=\"margin:0 0 16px 0;color:#36483d;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;\">Please confirm the Loyan account email for <strong>{email}</strong>.</p><p style=\"margin:0 0 18px 0;color:#36483d;font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:24px;\">Use the button below to activate the account. This link expires at <strong>{expires_at_utc}</strong>.</p>{activation_link_html}{manual_link_html}<p style=\"margin:16px 0 8px 0;color:#5d6b61;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;\">Activation token:</p><p style=\"margin:0 0 18px 0;padding:12px 14px;border-radius:8px;background:#f3f6ef;color:#1f3327;font-family:Consolas,Monaco,monospace;font-size:13px;line-height:18px;word-break:break-all;\">{token}</p>",
                "PasswordResetSubjectTemplateDefault" => "Reset your Loyan password",
                "PasswordResetBodyTemplateDefault" => "<h1 style=\"margin:0 0 14px 0;color:#1f3327;font-family:Arial,Helvetica,sans-serif;font-size:26px;line-height:34px;\">Reset your password</h1><p style=\"margin:0 0 16px 0;color:#36483d;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:25px;\">We received a request to reset the password for <strong>{email}</strong>.</p><p style=\"margin:0 0 18px 0;color:#36483d;font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:24px;\">Use the button below to choose a new password. This link expires at <strong>{expires_at_utc}</strong>.</p>{reset_link_html}{manual_link_html}<p style=\"margin:16px 0 8px 0;color:#5d6b61;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;\">Reset token:</p><p style=\"margin:0 0 18px 0;padding:12px 14px;border-radius:8px;background:#f3f6ef;color:#1f3327;font-family:Consolas,Monaco,monospace;font-size:13px;line-height:18px;word-break:break-all;\">{token}</p><p style=\"margin:0;color:#5d6b61;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;\">If you did not request this password reset, you can ignore this email. Your password will stay unchanged.</p>",
                "PhoneVerificationSmsTemplateDefault" => "Your Loyan verification code is {token}. It expires at {expires_at_utc} UTC.",
                "PhoneVerificationWhatsAppTemplateDefault" => "Confirm your Loyan mobile number with code {token}. It expires at {expires_at_utc} UTC.",
                "RecipientOverrideNoticeHtml" => "<div style=\"margin:0 0 18px 0;padding:12px 14px;border-radius:8px;background:#fff6df;color:#6b4d12;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;\"><strong>Test delivery notice:</strong> original recipient was {original_recipient}.</div>",
                _ => resourceKey
            };

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
