using Darwin.Application.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Darwin.Web.Security
{
    /// <summary>
    /// Declarative, permission-based authorization attribute for MVC controllers/actions.
    /// Execution model:
    /// 1) Allows anonymous if an AllowAnonymous filter is present in the pipeline.
    /// 2) Requires an authenticated principal with a valid NameIdentifier (Guid).
    /// 3) Short-circuits if the current principal has "FullAdminAccess".
    /// 4) Otherwise checks whether the principal has the required permission key.
    /// Notes:
    /// - Uses IPermissionService.HasAsync to evaluate effective permissions built from roles/grants.
    /// - Keeps the web layer thin; does not require a custom policy provider.
    /// - Designed to be used at controller or action level.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class PermissionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private const string FullAdminAccess = "FullAdminAccess";
        private readonly string _requiredPermissionKey;

        /// <summary>
        /// Initializes the attribute with the required permission key.
        /// </summary>
        /// <param name="permissionKey">A stable, case-sensitive permission key (e.g., "AccessAdminPanel").</param>
        public PermissionAuthorizeAttribute(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
                throw new ArgumentException("Permission key must be provided.", nameof(permissionKey));

            _requiredPermissionKey = permissionKey;
        }

        /// <summary>
        /// Performs the authorization check using IPermissionService.
        /// </summary>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            // 1) Respect [AllowAnonymous]
            foreach (var filter in context.Filters)
            {
                if (filter is IAllowAnonymousFilter)
                    return;
            }

            var http = context.HttpContext;
            var user = http.User;

            // 2) Must be authenticated
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
            {
                context.Result = new ChallengeResult();
                return;
            }

            // Extract user id as Guid
            var idValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(idValue, out var userId))
            {
                // No safe way to authorize an unknown principal
                context.Result = new ForbidResult();
                return;
            }

            // Resolve the permission service
            var permissions = (IPermissionService?)http.RequestServices.GetService(typeof(IPermissionService));
            if (permissions is null)
            {
                // Misconfiguration: IPermissionService must be registered by Infrastructure.
                context.Result = new StatusCodeResult(500);
                return;
            }

            // 3) FullAdminAccess bypass
            if (await permissions.HasAsync(userId, FullAdminAccess, http.RequestAborted))
                return;

            // 4) Required permission
            var allowed = await permissions.HasAsync(userId, _requiredPermissionKey, http.RequestAborted);
            if (!allowed)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
