using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Services;
using Microsoft.AspNetCore.Http;

namespace Darwin.Web.Areas.Admin.Infrastructure
{
    /// <summary>
    /// Small helper usable from Razor views to conditionally render UI chunks based on permissions.
    /// The helper runs read-only checks via IPermissionService.HasAsync using the current principal.
    /// </summary>
    public sealed class PermissionRazorHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPermissionService _permissions;

        /// <summary>
        /// Creates a new helper bound to the current HTTP context and permission service.
        /// </summary>
        public PermissionRazorHelper(IHttpContextAccessor httpContextAccessor, IPermissionService permissions)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        }

        /// <summary>
        /// Returns true if the current user has the given permission key.
        /// Falls back to false when the user is anonymous or the id claim is missing.
        /// </summary>
        public async Task<bool> HasAsync(string permissionKey, CancellationToken ct = default)
        {
            var http = _httpContextAccessor.HttpContext;
            var user = http?.User;
            if (user?.Identity is null || !user.Identity.IsAuthenticated)
                return false;

            var idValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(idValue, out var userId))
                return false;

            // FullAdminAccess bypass
            if (await _permissions.HasAsync(userId, "FullAdminAccess", ct)) return true;

            return await _permissions.HasAsync(userId, permissionKey, ct);
        }
    }
}
