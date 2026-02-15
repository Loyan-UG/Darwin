using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Darwin.WebApi.Auth
{
    /// <summary>
    /// Represents a single permission requirement used in policy-based authorization.
    /// Permission keys must match values stored in the database and evaluated via
    /// <see cref="IPermissionService"/>.
    /// </summary>
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionRequirement"/> class.
        /// </summary>
        /// <param name="permissionKey">The permission key to be checked for the current user.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="permissionKey"/> is null, empty, or whitespace.
        /// </exception>
        public PermissionRequirement(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                throw new ArgumentException(
                    "Permission key must not be null or whitespace.",
                    nameof(permissionKey));
            }

            PermissionKey = permissionKey;
        }

        /// <summary>
        /// Gets the logical permission key that must be granted to satisfy this requirement.
        /// </summary>
        public string PermissionKey { get; }
    }

    /// <summary>
    /// Produces authorization policies on demand for names that start with <c>"perm:"</c>.
    /// For example, policy <c>"perm:AccessMemberArea"</c> becomes a single
    /// <see cref="PermissionRequirement"/> with that key.
    ///
    /// Policies that do not match the prefix are delegated to the default policy provider.
    /// </summary>
    public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private const string PolicyPrefix = "perm:";
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionPolicyProvider"/> class.
        /// </summary>
        /// <param name="options">The authorization options used by the default provider.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="options"/> is <c>null</c>.
        /// </exception>
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _fallback = new DefaultAuthorizationPolicyProvider(options);
        }

        /// <inheritdoc />
#if NET9_0_OR_GREATER
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return _fallback.GetFallbackPolicyAsync();
        }
#endif

        /// <inheritdoc />
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return _fallback.GetDefaultPolicyAsync();
        }

        /// <inheritdoc />
        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if (string.IsNullOrWhiteSpace(policyName) ||
                !policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
            {
                // Non-permission or invalid policy names are delegated to the default provider.
                return _fallback.GetPolicyAsync(policyName);
            }

            var permissionKey = policyName.Substring(PolicyPrefix.Length);

            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                // Invalid dynamic policy name, fall back to the default provider to avoid surprises.
                return _fallback.GetPolicyAsync(policyName);
            }

            var requirement = new PermissionRequirement(permissionKey);

            var builder = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .AddRequirements(requirement);

            var policy = builder.Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }

    /// <summary>
    /// Authorization handler that evaluates <see cref="PermissionRequirement"/> instances
    /// by delegating to the application-layer <see cref="IPermissionService"/>.
    /// </summary>
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PermissionAuthorizationHandler"/> class.
        /// </summary>
        /// <param name="permissions">
        /// The permission service that resolves effective permissions for a given user.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="permissions"/> is <c>null</c>.
        /// </exception>
        public PermissionAuthorizationHandler(IPermissionService permissions)
        {
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        }


        /// <inheritdoc />
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (requirement is null)
            {
                throw new ArgumentNullException(nameof(requirement));
            }

            // Extract the user id from the standard "sub" / NameIdentifier claim.
            var subjectClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (subjectClaim is null || string.IsNullOrWhiteSpace(subjectClaim.Value))
            {
                return;
            }

            if (!Guid.TryParse(subjectClaim.Value, out var userId))
            {
                // Unexpected subject format; do not throw, simply fail the requirement.
                return;
            }

            // There is no ambient CancellationToken in authorization handlers; using None is intentional.
            var ct = CancellationToken.None;

            // Global admin override: FullAdminAccess short-circuits all permission checks.
            if (await _permissions.HasAsync(userId, "FullAdminAccess", ct).ConfigureAwait(false))
            {
                context.Succeed(requirement);
                return;
            }

            // Required permission key.
            if (await _permissions.HasAsync(userId, requirement.PermissionKey, ct).ConfigureAwait(false))
            {
                context.Succeed(requirement);
            }
        }
    }

    /// <summary>
    /// Attribute used on controllers or actions to express a required permission in a concise way.
    /// Internally this maps to a dynamic policy with the name <c>"perm:{permissionKey}"</c>,
    /// which is resolved by <see cref="PermissionPolicyProvider"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class HasPermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HasPermissionAttribute"/> class.
        /// </summary>
        /// <param name="permissionKey">
        /// The logical permission key that must be granted to the authenticated user.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="permissionKey"/> is null, empty, or whitespace.
        /// </exception>
        public HasPermissionAttribute(string permissionKey)
        {
            if (string.IsNullOrWhiteSpace(permissionKey))
            {
                throw new ArgumentException(
                    "Permission key must not be null or whitespace.",
                    nameof(permissionKey));
            }

            Policy = $"perm:{permissionKey}";
        }
    }
}
