using System;

namespace Darwin.Application.Settings.DTOs
{
    /// <summary>
    /// Read model for site settings.
    /// </summary>
    public sealed class SiteSettingDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string Title { get; set; } = string.Empty;

        public string DefaultCulture { get; set; } = "de-DE";
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US";

        // Add other existing fields you already have in SiteSettings entity:
        // public string? LogoUrl { get; set; }
        // public string? ContactEmail { get; set; }
        // etc.
    }

    /// <summary>
    /// Update command model for site settings.
    /// </summary>
    public sealed class UpdateSiteSettingDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string Title { get; set; } = string.Empty;

        public string DefaultCulture { get; set; } = "de-DE";
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US";

        // Mirror any additional fields you want editable.
    }
}
