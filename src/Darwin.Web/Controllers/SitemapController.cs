using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Web.Controllers
{
    /// <summary>
    /// Dynamic sitemap.xml including CMS Pages, Categories and Products.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class SitemapController : Controller
    {
        private readonly IAppDbContext _db;

        public SitemapController(IAppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ResponseCache(Duration = 300)]
        [Route("sitemap.xml")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            // Only include published items and within publish window. IsDeleted ignored.
            var now = DateTime.UtcNow;

            var pages = await _db.Set<Page>()
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Status == Domain.Enums.PageStatus.Published &&
                            (p.PublishStartUtc == null || p.PublishStartUtc <= now) &&
                            (p.PublishEndUtc == null || p.PublishEndUtc >= now))
                .SelectMany(p => p.Translations)
                .ToListAsync(ct);

            var cats = await _db.Set<Category>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.IsActive)
                .SelectMany(c => c.Translations)
                .ToListAsync(ct);

            var prods = await _db.Set<Product>()
                .AsNoTracking()
                .Where(pr => !pr.IsDeleted && pr.IsVisible)
                .SelectMany(pr => pr.Translations)
                .ToListAsync(ct);

            // Build XML
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urlset = new XElement(ns + "urlset");

            // Helper to add URL
            void AddUrl(string loc, DateTime? lastModUtc)
            {
                var url = new XElement(ns + "url",
                    new XElement(ns + "loc", loc));
                if (lastModUtc.HasValue)
                    url.Add(new XElement(ns + "lastmod", lastModUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)));
                urlset.Add(url);
            }

            // Culture-aware routes: /{culture}/page/{slug}, /{culture}/c/{slug}, /{culture}/p/{slug}
            foreach (var t in pages)
            {
                var loc = $"{Request.Scheme}://{Request.Host}/{t.Culture}/page/{t.Slug}";
                AddUrl(loc, t.ModifiedAtUtc ?? t.CreatedAtUtc);
            }
            foreach (var t in cats)
            {
                var loc = $"{Request.Scheme}://{Request.Host}/{t.Culture}/c/{t.Slug}";
                AddUrl(loc, t.ModifiedAtUtc ?? t.CreatedAtUtc);
            }
            foreach (var t in prods)
            {
                var loc = $"{Request.Scheme}://{Request.Host}/{t.Culture}/p/{t.Slug}";
                AddUrl(loc, t.ModifiedAtUtc ?? t.CreatedAtUtc);
            }

            var doc = new XDocument(urlset);
            var xml = doc.ToString(SaveOptions.DisableFormatting);

            return Content(xml, "application/xml", Encoding.UTF8);
        }
    }
}
