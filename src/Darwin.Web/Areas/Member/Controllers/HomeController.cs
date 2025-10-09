using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Member.Controllers
{
    /// <summary>
    /// Default entry point for the Member area.
    /// Provides a minimal landing page that can be extended with profile/orders/etc.
    /// </summary>
    public sealed class HomeController : MemberBaseController
    {
        /// <summary>
        /// Renders the default member dashboard.
        /// </summary>
        [HttpGet]
        public IActionResult Index() => View();
    }
}
