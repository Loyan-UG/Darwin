using Darwin.Web.Auth;
using Darwin.Web.Security;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Member.Controllers
{
    /// <summary>
    /// Base controller for the Member area. Access requires the "AccessMemberArea" permission.
    /// The permission model ensures that "FullAdminAccess" overrides and grants access everywhere.
    /// </summary>
    [Area("Member")]
    [PermissionAuthorize("AccessMemberArea")]
    public abstract class MemberBaseController : Controller
    {
    }
}
