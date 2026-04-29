namespace Darwin.Application.CMS.Media
{
    internal static class MediaAssetRoleConventions
    {
        public const string EditorAssetRole = "EditorAsset";
        public const string LibraryAssetRole = "LibraryAsset";

        public static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return null;
            }

            var normalized = role.Trim();
            return normalized.ToUpperInvariant() switch
            {
                "EDITORASSET" => EditorAssetRole,
                "LIBRARYASSET" => LibraryAssetRole,
                _ => normalized
            };
        }
    }
}
