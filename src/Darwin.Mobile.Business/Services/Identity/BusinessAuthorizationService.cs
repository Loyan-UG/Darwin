using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;
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
    public string RoleDisplayName { get; init; } = AppResources.AuthorizationRoleUnknown;
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

        var jwt = JwtClaimReader.TryReadToken(accessToken);
        if (jwt is null)
        {
            return Result<BusinessAuthorizationSnapshot>.Fail(MobileErrorMessages.InvalidSession());
        }

        var scopes = JwtClaimReader.ReadScopeSet(jwt);

        var hasBusinessAccess = scopes.Contains("AccessLoyaltyBusiness", StringComparer.OrdinalIgnoreCase);
        var isFullAdmin = scopes.Contains("FullAdminAccess", StringComparer.OrdinalIgnoreCase);

        var hasAnyScope = scopes.Count > 0;
        var useLegacyCompatibility = !hasAnyScope;

        var snapshot = new BusinessAuthorizationSnapshot
        {
            RoleDisplayName = isFullAdmin
                ? AppResources.AuthorizationRoleAdministrator
                : hasBusinessAccess
                    ? AppResources.AuthorizationRoleBusinessOperator
                    : useLegacyCompatibility
                        ? AppResources.AuthorizationRoleBusinessOperatorLegacy
                        : AppResources.AuthorizationRoleRestricted,

            // Compatibility mode: for old tokens without scope claims, keep existing app behavior.
            CanEditRewards = useLegacyCompatibility || isFullAdmin || hasBusinessAccess,
            CanConfirmRedemption = useLegacyCompatibility || isFullAdmin || hasBusinessAccess,
            CanConfirmAccrual = useLegacyCompatibility || isFullAdmin || hasBusinessAccess,
            IsLegacyTokenWithoutScopes = useLegacyCompatibility
        };

        return Result<BusinessAuthorizationSnapshot>.Ok(snapshot);
    }
}
