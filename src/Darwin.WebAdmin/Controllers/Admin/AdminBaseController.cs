using Darwin.WebAdmin.Auth;
using Darwin.WebAdmin.Localization;
using Darwin.WebAdmin.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Darwin.WebAdmin.Controllers.Admin
{
    /// <summary>
    /// Base controller for the Admin area. Access requires the "AccessAdminPanel" permission.
    /// The authorization pipeline also grants access to users who have "FullAdminAccess".
    /// Additional controller-level requirements can be added on top of this base type if needed.
    /// </summary>
    [PermissionAuthorize("AccessAdminPanel")]
    public abstract class AdminBaseController : Controller
    {
        protected string T(string key)
        {
            return HttpContext.RequestServices.GetRequiredService<IAdminTextLocalizer>().T(key);
        }

        protected void SetSuccessMessage(string key)
        {
            TempData["Success"] = T(key);
        }

        protected void SetErrorMessage(string key)
        {
            TempData["Error"] = T(key);
        }

        protected void SetWarningMessage(string key)
        {
            TempData["Warning"] = T(key);
        }
    }
}
