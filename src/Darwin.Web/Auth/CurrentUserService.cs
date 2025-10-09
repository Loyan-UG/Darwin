using System;
using System.Security.Claims;
using Darwin.Application.Abstractions.Auth;
using Darwin.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace Darwin.Web.Auth
{
    /// <summary>
    /// Current user provider backed by HttpContext. Falls back to SystemUserId.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _http;

        public CurrentUserService(IHttpContextAccessor http) => _http = http;

        public Guid GetCurrentUserId()
        {
            var user = _http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                // Try common claim types
                var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? user.FindFirstValue("sub")
                         ?? user.FindFirstValue("uid");

                if (Guid.TryParse(id, out var guid))
                    return guid;
            }
            return WellKnownIds.AdministratorUserId;
        }
    }
}
