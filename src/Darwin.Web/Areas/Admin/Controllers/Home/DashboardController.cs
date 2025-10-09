using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Backward-compatible controller to honor existing navigation that points to
    /// 'Dashboard'. Redirects to <see cref="HomeController.Index"/>.
    /// </summary>
    [Area("Admin")]
    public sealed class DashboardController : AdminBaseController
    {
        /// <summary>
        /// Redirects to Admin/Home/Index to keep navigation consistent.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(actionName: "Index", controllerName: "Home", routeValues: new { area = "Admin" });
        }
    }
}
