using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Darwin.Application.Identity.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Darwin.Web.Security
{
    /// <summary>
    /// Minimal permission-based authorization filter that leverages the Application query
    /// to check whether the current user has the required permission key.
    /// This does not replace ASP.NET Core policy provider; it keeps the Web layer thin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class PermissionAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _permissionKey;

        /// <summary>
        /// Declares that accessing the resource requires the given permission key.
        /// </summary>
        public PermissionAuthorizeAttribute(string permissionKey)
        {
            _permissionKey = permissionKey ?? throw new ArgumentNullException(nameof(permissionKey));
        }

        /// <inheritdoc />
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(idClaim, out var userId))
            {
                context.Result = new ForbidResult();
                return;
            }

            var checker = context.HttpContext.RequestServices.GetService(typeof(UserHasPermissionHandler)) as UserHasPermissionHandler;
            if (checker is null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            var res = await checker.HandleAsync(userId, _permissionKey);
            if (!res.Succeeded || !res.Value)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
