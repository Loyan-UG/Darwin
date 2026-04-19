using System;
using System.Collections.Generic;
using System.Linq;
using Darwin.Application.Settings.DTOs;

namespace Darwin.WebAdmin.Localization
{
    /// <summary>
    /// Central catalog for the current WebAdmin culture contract.
    /// Keeps the initial platform languages explicit and avoids scattering
    /// the same hard-coded UI culture defaults across controllers, settings, and view models.
    /// </summary>
    public static class AdminCultureCatalog
    {
        public const string German = SiteSettingDto.DefaultCultureDefault;
        public const string English = "en-US";
        public const string DefaultCulture = German;
        public const string SupportedCulturesCsvDefault = SiteSettingDto.SupportedCulturesCsvDefault;

        private static readonly IReadOnlyList<(string Culture, string Label)> _languageOptions =
            new List<(string Culture, string Label)>
            {
                (German, "Deutsch"),
                (English, "English")
            }.AsReadOnly();

        public static IReadOnlyList<(string Culture, string Label)> LanguageOptions => _languageOptions;

        public static string NormalizeUiCulture(string? culture)
        {
            if (string.Equals(culture, "de", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(culture, German, StringComparison.OrdinalIgnoreCase))
            {
                return German;
            }

            if (string.Equals(culture, "en", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(culture, English, StringComparison.OrdinalIgnoreCase))
            {
                return English;
            }

            return DefaultCulture;
        }

        public static List<string> NormalizeSupportedCultureNames(string? supportedCulturesCsv)
        {
            var cultureNames = (supportedCulturesCsv ?? SupportedCulturesCsvDefault)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeUiCulture)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cultureNames.Count == 0)
            {
                cultureNames.Add(DefaultCulture);
                cultureNames.Add(English);
                return cultureNames;
            }

            if (!cultureNames.Contains(DefaultCulture, StringComparer.OrdinalIgnoreCase))
            {
                cultureNames.Insert(0, DefaultCulture);
            }

            if (!cultureNames.Contains(English, StringComparer.OrdinalIgnoreCase))
            {
                cultureNames.Add(English);
            }

            return cultureNames;
        }
    }
}
