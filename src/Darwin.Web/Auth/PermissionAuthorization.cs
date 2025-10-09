using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Darwin.Web.Auth
{
    /// <summary>
    /// Encapsulates a single permission requirement. Permission keys must match values stored in the database.
    /// This attribute is used indirectly via the dynamic policy provider, not directly.
    /// </summary>
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new requirement using the given permission key. The key must be a stable identifier.
        /// </summary>
        /// <param name="permissionKey">Stable permission key, e.g. "FullAdminAccess".</param>
        /// <exception cref="ArgumentNullException">Thrown when the key is null or whitespace.</exception>
        public PermissionRequirement(string permissionKey)
        {
            PermissionKey = !string.IsNullOrWhiteSpace(permissionKey)
                ? permissionKey
                : throw new ArgumentNullException(nameof(permissionKey));
        }

        /// <summary>
        /// The immutable key of the permission to be evaluated.
        /// </summary>
        public string PermissionKey { get; }
    }

    /// <summary>
    /// Produces authorization policies on demand for names that start with "perm:".
    /// For example, policy "perm:AccessMemberArea" becomes a single PermissionRequirement with that key.
    /// </summary>
    public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        /// <summary>
        /// Creates the provider and keeps a reference to the default policy provider as a fallback.
        /// </summary>
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        /// <inheritdoc />
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

        /// <inheritdoc />
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

        /// <inheritdoc />
        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (!string.IsNullOrWhiteSpace(policyName) &&
                policyName.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
            {
                var key = policyName.Substring("perm:".Length);
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(key))
                    .Build();
                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return _fallback.GetPolicyAsync(policyName);
        }
    }

    /// <summary>
    /// Evaluates permission requirements using the Application layer's <see cref="IPermissionService"/>.
    /// If the user has "FullAdminAccess", all requirements succeed automatically.
    /// </summary>
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissions;

        /// <summary>
        /// Initializes a new handler that delegates checks to <see cref="IPermissionService"/>.
        /// </summary>
        public PermissionAuthorizationHandler(IPermissionService permissions)
        {
            _permissions = permissions;
        }

        /// <inheritdoc />
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var sub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(sub, out var userId))
                return;

            // Global admin override.
            if (await _permissions.HasAsync(userId, "FullAdminAccess", CancellationToken.None))
            {
                context.Succeed(requirement);
                return;
            }

            // Required permission.
            if (await _permissions.HasAsync(userId, requirement.PermissionKey, CancellationToken.None))
            {
                context.Succeed(requirement);
            }
        }
    }

    /// <summary>
    /// Convenience attribute to require a specific permission by key.
    /// The corresponding policy is generated dynamically by <see cref="PermissionPolicyProvider"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class HasPermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Creates a dynamic policy name of the form "perm:{permissionKey}".
        /// </summary>
        public HasPermissionAttribute(string permissionKey)
        {
            Policy = $"perm:{permissionKey}";
        }
    }
}
