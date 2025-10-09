using Darwin.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Base controller for the Admin area. Access requires the "AccessAdminPanel" permission.
    /// The authorization pipeline also grants access to users who have "FullAdminAccess".
    /// Additional controller-level requirements can be added on top of this base type if needed.
    /// </summary>
    [Area("Admin")]
    [HasPermission("AccessAdminPanel")]
    public abstract class AdminBaseController : Controller
    {
    }
}
