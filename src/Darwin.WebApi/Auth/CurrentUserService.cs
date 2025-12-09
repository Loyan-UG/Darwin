using System;
using System.Security.Claims;
using Darwin.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;

namespace Darwin.WebApi.Auth
{
    /// <summary>
    /// Provides the current user identifier for the WebApi layer by inspecting
    /// the <see cref="HttpContext"/> and its authenticated principal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation is tailored for JWT-based APIs. It expects that
    /// authentication has already been enforced by ASP.NET Core and that a
    /// valid <c>sub</c> or equivalent identifier claim is present on the
    /// current principal.
    /// </para>
    /// <para>
    /// When no authenticated user is available or the identifier cannot be
    /// parsed as a <see cref="Guid"/>, an <see cref="InvalidOperationException"/>
    /// is thrown instead of falling back to a system account. This ensures
    /// that misconfigured endpoints or authentication issues surface early.
    /// </para>
    /// </remarks>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserService"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">
        /// The accessor used to obtain the current <see cref="HttpContext"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="httpContextAccessor"/> is <c>null</c>.
        /// </exception>
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor
                                   ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Gets the identifier of the current authenticated user.
        /// </summary>
        /// <returns>
        /// The user identifier parsed from the current principal's claims.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is no authenticated user or when the identifier
        /// claim cannot be parsed as a <see cref="Guid"/>.
        /// </exception>
        public Guid GetCurrentUserId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                // Try common claim types used by JWT/OpenID Connect identity providers.
                var id =
                    user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub")
                    ?? user.FindFirstValue("uid");

                if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out var parsed))
                {
                    return parsed;
                }
            }

            throw new InvalidOperationException(
                "No authenticated user id is available in the current HTTP context.");
        }
    }
}
