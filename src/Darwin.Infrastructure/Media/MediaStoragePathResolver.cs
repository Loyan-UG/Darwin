namespace Darwin.Infrastructure.Media
{
    public static class MediaStoragePathResolver
    {
        public static string ResolveRootPath(string contentRootPath, MediaStorageOptions options)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
            {
                throw new ArgumentException("Content root path is required.", nameof(contentRootPath));
            }

            ArgumentNullException.ThrowIfNull(options);

            return ResolvePath(contentRootPath, options.RootPath, Path.Combine("..", "..", "_shared_media", "uploads"));
        }

        public static IReadOnlyList<string> ResolveLegacyRootPaths(string contentRootPath, MediaStorageOptions options)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
            {
                throw new ArgumentException("Content root path is required.", nameof(contentRootPath));
            }

            ArgumentNullException.ThrowIfNull(options);

            var defaultSharedRoot = ResolvePath(contentRootPath, null, Path.Combine("..", "..", "_shared_media", "uploads"));

            return options.LegacyRootPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => ResolvePath(contentRootPath, path, null))
                .Append(defaultSharedRoot)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string NormalizeRequestPath(string? requestPath)
        {
            var normalized = string.IsNullOrWhiteSpace(requestPath)
                ? "/uploads"
                : requestPath.Trim();

            normalized = "/" + normalized.Trim('/');
            return normalized == "/" ? "/uploads" : normalized;
        }

        public static string BuildPublicUrl(MediaStorageOptions options, string fileName)
        {
            ArgumentNullException.ThrowIfNull(options);

            if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
            {
                throw new ArgumentException("A media file name without path separators is required.", nameof(fileName));
            }

            if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
            {
                return $"{options.PublicBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(fileName)}";
            }

            return $"{NormalizeRequestPath(options.RequestPath)}/{Uri.EscapeDataString(fileName)}";
        }

        private static string ResolvePath(string contentRootPath, string? configuredPath, string? fallbackRelativePath)
        {
            var path = string.IsNullOrWhiteSpace(configuredPath)
                ? fallbackRelativePath
                : configuredPath.Trim();

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A media storage path is required.", nameof(configuredPath));
            }

            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(contentRootPath, path));
        }
    }
}
