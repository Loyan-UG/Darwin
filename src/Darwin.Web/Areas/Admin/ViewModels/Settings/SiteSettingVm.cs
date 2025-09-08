using System;

namespace Darwin.Web.Areas.Admin.ViewModels.Settings
{
    public sealed class SiteSettingVm
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public string Title { get; set; } = string.Empty;

        public string DefaultCulture { get; set; } = "de-DE";
        public string SupportedCulturesCsv { get; set; } = "de-DE,en-US";
    }
}
