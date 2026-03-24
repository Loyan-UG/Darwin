using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebAdmin.Controllers.Admin
{
    /// <summary>
    /// Backward-compatible controller to honor existing navigation that points to
    /// 'Dashboard'. Redirects to <see cref="HomeController.Index"/>.
    /// </summary>
    public sealed class DashboardController : AdminBaseController
    {
        /// <summary>
        /// Redirects to the back-office home route to keep navigation consistent.
        /// </summary>
        [HttpGet("/admin")]
        [HttpGet("/dashboard")]
        public IActionResult Index()
        {
            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }
    }
}
