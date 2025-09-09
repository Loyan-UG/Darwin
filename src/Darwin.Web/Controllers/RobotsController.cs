using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Controllers
{
    /// <summary>
    /// Dynamic robots.txt. Basic allow-all with Sitemap link.
    /// </summary>
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
