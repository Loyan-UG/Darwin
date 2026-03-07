using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Services;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Business.Services.Identity;

/// <summary>
/// Resolves client-side authorization capabilities from JWT claims for the Business app.
/// </summary>
/// <remarks>
/// This service provides a local, UI-facing guard layer only.
/// Server-side authorization remains the source of truth.
/// </remarks>
public interface IBusinessAuthorizationService
{
    /// <summary>
    /// Returns role/capability snapshot for the currently signed-in operator.
    /// </summary>
    Task<Result<BusinessAuthorizationSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Immutable authorization snapshot used by ViewModels to enable/disable sensitive actions.
/// </summary>
public sealed class BusinessAuthorizationSnapshot
{
    public string RoleDisplayName { get; init; } = "Unknown";
    public bool CanEditRewards { get; init; }
    public bool CanConfirmRedemption { get; init; }
    public bool CanConfirmAccrual { get; init; }
    public bool IsLegacyTokenWithoutScopes { get; init; }
}

/// <summary>
/// JWT-claim-based implementation of <see cref="IBusinessAuthorizationService"/>.
/// </summary>
public sealed class BusinessAuthorizationService : IBusinessAuthorizationService
{
    private readonly ITokenStore _tokenStore;

    public BusinessAuthorizationService(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    public async Task<Result<BusinessAuthorizationSnapshot>> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var (accessToken, _) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Result<BusinessAuthorizationSnapshot>.Fail("No active access token found.");
        }

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var scopes = ReadScopeSet(jwt.Claims);

            var hasBusinessAccess = scopes.Contains("AccessLoyaltyBusiness", StringComparer.OrdinalIgnoreCase);
            var isFullAdmin = scopes.Contains("FullAdminAccess", StringComparer.OrdinalIgnoreCase);

            var hasAnyScope = scopes.Count > 0;
            var useLegacyCompatibility = !hasAnyScope;

            var snapshot = new BusinessAuthorizationSnapshot
            {
                RoleDisplayName = isFullAdmin
                    ? "Administrator"
                    : hasBusinessAccess
                        ? "Business Operator"
                        : useLegacyCompatibility
                            ? "Business Operator (Legacy token)"
                            : "Restricted",

                // Compatibility mode: for old tokens without scope claims, keep existing app behavior.
                CanEditRewards = useLegacyCompatibility || isFullAdmin || hasBusinessAccess,
                CanConfirmRedemption = useLegacyCompatibility || isFullAdmin || hasBusinessAccess,
                CanConfirmAccrual = useLegacyCompatibility || isFullAdmin || hasBusinessAccess,
                IsLegacyTokenWithoutScopes = useLegacyCompatibility
            };

            return Result<BusinessAuthorizationSnapshot>.Ok(snapshot);
        }
        catch (Exception ex)
        {
            return Result<BusinessAuthorizationSnapshot>.Fail($"Failed to parse access token claims: {ex.Message}");
        }
    }

    private static HashSet<string> ReadScopeSet(IEnumerable<System.Security.Claims.Claim> claims)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in claims)
        {
            if (!string.Equals(claim.Type, "scope", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(claim.Type, "scp", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(claim.Type, "permissions", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var token in claim.Value.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                set.Add(token.Trim());
            }
        }

        return set;
    }
}
