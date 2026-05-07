namespace Darwin.Infrastructure.Media
{
    /// <summary>
    /// Configuration for shared media storage and public media URLs.
    /// </summary>
    public sealed class MediaStorageOptions
    {
        public const string SectionName = "MediaStorage";

        public string? RootPath { get; set; }

        public string RequestPath { get; set; } = "/uploads";

        public string? PublicBaseUrl { get; set; }

        public string[] LegacyRootPaths { get; set; } = [];
    }
}
