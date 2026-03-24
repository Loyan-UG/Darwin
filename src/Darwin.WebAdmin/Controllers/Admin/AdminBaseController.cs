using Darwin.WebAdmin.Auth;
using Darwin.WebAdmin.Security;
using Microsoft.AspNetCore.Mvc;

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
    }
}
