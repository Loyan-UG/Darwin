using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Darwin.Web.Services.Seo
{
    public sealed class CanonicalUrlService : ICanonicalUrlService
    {
        private readonly IHttpContextAccessor _http;

        public CanonicalUrlService(IHttpContextAccessor http)
        {
            _http = http;
        }

        public string Page(string culture, string slug) => $"/{culture}/page/{slug}";
        public string Category(string culture, string slug) => $"/{culture}/c/{slug}";
        public string Product(string culture, string slug) => $"/{culture}/p/{slug}";

        public string Absolute(string relative)
        {
            var req = _http.HttpContext?.Request;
            if (req == null) return relative;
            var uri = UriHelper.BuildAbsolute(req.Scheme, req.Host, req.PathBase, relative);
            return uri;
        }
    }
}
