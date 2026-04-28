using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;

namespace Darwin.WebAdmin.Services.Seo
{
    public sealed class CanonicalUrlService : ICanonicalUrlService
    {
        private readonly IHttpContextAccessor _http;

        public CanonicalUrlService(IHttpContextAccessor http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public string Page(string culture, string slug) => BuildRelative(culture, "page", slug);
        public string Category(string culture, string slug) => BuildRelative(culture, "c", slug);
        public string Product(string culture, string slug) => BuildRelative(culture, "p", slug);

        public string Absolute(string relative)
        {
            relative = NormalizePathSegment(relative);
            if (string.IsNullOrWhiteSpace(relative))
            {
                relative = "/";
            }
            else if (!relative.StartsWith('/'))
            {
                relative = "/" + relative;
            }

            var req = _http.HttpContext?.Request;
            if (req == null) return relative;
            var uri = UriHelper.BuildAbsolute(req.Scheme, req.Host, req.PathBase, relative);
            return uri;
        }

        private static string BuildRelative(string culture, string routeSegment, string slug)
        {
            var normalizedCulture = NormalizePathSegment(culture);
            var normalizedSlug = NormalizePathSegment(slug);
            return string.IsNullOrWhiteSpace(normalizedCulture)
                ? $"/{routeSegment}/{normalizedSlug}"
                : $"/{normalizedCulture}/{routeSegment}/{normalizedSlug}";
        }

        private static string NormalizePathSegment(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Trim('/');
        }
    }
}
