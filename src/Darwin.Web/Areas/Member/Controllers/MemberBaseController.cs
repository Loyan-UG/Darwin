using Darwin.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Member.Controllers
{
    /// <summary>
    /// Base controller for the Member area. Access requires the "AccessMemberArea" permission.
    /// The permission model ensures that "FullAdminAccess" overrides and grants access everywhere.
    /// </summary>
    [Area("Member")]
    [HasPermission("AccessMemberArea")]
    public abstract class MemberBaseController : Controller
    {
    }
}
