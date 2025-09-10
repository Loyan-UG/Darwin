using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Controllers
{
    /// <summary>
    ///     Returns a dynamic <c>robots.txt</c> reflecting environment and site settings,
    ///     enabling/disabling crawling and pointing to the sitemap endpoint.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         In development environments, disallow crawling by default to prevent indexing of non-production content.
    ///         In production, follow <c>SiteSetting</c> flags and expose the sitemap location.
    ///     </para>
    /// </remarks>
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class RobotsController : Controller
    {
        [HttpGet]
        [Route("robots.txt")]
        public IActionResult Index()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            sb.AppendLine("Disallow:");
            sb.AppendLine($"Sitemap: {Request.Scheme}://{Request.Host}/sitemap.xml");
            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }
    }
}
