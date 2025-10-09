using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Landing endpoint for the Admin area. The <c>Index</c> view will later evolve
    /// into the main dashboard (KPIs, quick links, system health).
    /// </summary>
    [Area("Admin")]
    public sealed class HomeController : AdminBaseController
    {
        /// <summary>
        /// Renders the Admin dashboard placeholder.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            // NOTE: Layout for the Admin area pulls shared alerts and navigation.
            return View();
        }
    }
}
