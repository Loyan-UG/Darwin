using Darwin.Contracts.Profile;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Security;
using Darwin.Shared.Results;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Services.Profile;

/// <summary>
/// Profile service for the current authenticated user.
/// Responsibilities:
/// - Load the current user's profile payloads with short-lived local caching.
/// - Keep profile reads scoped to the authenticated user identity to avoid cache bleed between accounts.
/// - Invalidate profile caches immediately after write operations so optimistic concurrency remains correct.
/// </summary>
public sealed class ProfileService : IProfileService
{
    private static readonly TimeSpan ProfileCacheTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ProfileFallbackMaxAge = TimeSpan.FromMinutes(10);

    private readonly IApiClient _api;
    private readonly IMobileCacheService _cache;
    private readonly ITokenStore _tokenStore;

    public ProfileService(IApiClient api, IMobileCacheService cache, ITokenStore tokenStore)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
    }

    /// <summary>
    /// Retrieves the current user's profile using the canonical member-profile endpoint.
    /// </summary>
    public async Task<CustomerProfile?> GetMeAsync(CancellationToken ct)
    {
        var cacheKey = await GetScopedCacheKeyAsync("profile.me", ct).ConfigureAwait(false);
        var cached = await _cache.GetFreshAsync<CustomerProfile>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.GetAsync<CustomerProfile>(ApiRoutes.Profile.GetMe, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ProfileCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<CustomerProfile>(cacheKey, ProfileFallbackMaxAge, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Updates the current user's profile using the canonical member-profile endpoint.
    /// The server returns 204 No Content on success; this method maps that to a functional result.
    /// </summary>
    public async Task<Result> UpdateMeAsync(CustomerProfile profile, CancellationToken ct)
    {
        if (profile is null) throw new ArgumentNullException(nameof(profile));

        var result = await _api.PutNoContentAsync(ApiRoutes.Profile.UpdateMe, profile, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await RemoveProfileReadCachesAsync(ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Requests a phone verification code using the canonical member-profile endpoint.
    /// </summary>
    public async Task<Result> RequestPhoneVerificationAsync(RequestPhoneVerificationRequest request, CancellationToken ct)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        return await _api.PostNoContentAsync(ApiRoutes.Profile.RequestPhoneVerification, request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Confirms a phone verification code using the canonical member-profile endpoint.
    /// </summary>
    public async Task<Result> ConfirmPhoneVerificationAsync(ConfirmPhoneVerificationRequest request, CancellationToken ct)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var result = await _api.PostNoContentAsync(ApiRoutes.Profile.ConfirmPhoneVerification, request, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await RemoveProfileReadCachesAsync(ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Retrieves the current user's privacy and communication preferences.
    /// </summary>
    public async Task<MemberPreferences?> GetPreferencesAsync(CancellationToken ct)
    {
        var cacheKey = await GetScopedCacheKeyAsync("profile.preferences", ct).ConfigureAwait(false);
        var cached = await _cache.GetFreshAsync<MemberPreferences>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.GetAsync<MemberPreferences>(ApiRoutes.Profile.GetPreferences, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ProfileCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<MemberPreferences>(cacheKey, ProfileFallbackMaxAge, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the current user's reusable address book.
    /// </summary>
    public async Task<IReadOnlyList<MemberAddress>> GetAddressesAsync(CancellationToken ct)
    {
        var cacheKey = await GetScopedCacheKeyAsync("profile.addresses", ct).ConfigureAwait(false);
        var cached = await _cache.GetFreshAsync<IReadOnlyList<MemberAddress>>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.GetAsync<IReadOnlyList<MemberAddress>>(ApiRoutes.Profile.GetAddresses, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ProfileCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<IReadOnlyList<MemberAddress>>(cacheKey, ProfileFallbackMaxAge, ct).ConfigureAwait(false)
            ?? Array.Empty<MemberAddress>();
    }

    /// <summary>
    /// Updates the current user's privacy and communication preferences using optimistic concurrency.
    /// </summary>
    public async Task<Result> UpdatePreferencesAsync(UpdateMemberPreferencesRequest preferences, CancellationToken ct)
    {
        if (preferences is null) throw new ArgumentNullException(nameof(preferences));

        var result = await _api.PutNoContentAsync(ApiRoutes.Profile.UpdatePreferences, preferences, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await RemovePreferencesCacheAsync(ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Creates a reusable address for the current user.
    /// </summary>
    public async Task<Result<MemberAddress>> CreateAddressAsync(CreateMemberAddressRequest request, CancellationToken ct)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var result = await _api.PostResultAsync<CreateMemberAddressRequest, MemberAddress>(ApiRoutes.Profile.CreateAddress, request, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await RemoveAddressReadCachesAsync(ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Updates an owned address using optimistic concurrency.
    /// </summary>
    public async Task<Result<MemberAddress>> UpdateAddressAsync(Guid addressId, UpdateMemberAddressRequest request, CancellationToken ct)
    {
        if (addressId == Guid.Empty) return Result<MemberAddress>.Fail("AddressId is required.");
        if (request is null) throw new ArgumentNullException(nameof(request));

        var result = await _api.PutResultAsync<UpdateMemberAddressRequest, MemberAddress>(ApiRoutes.Profile.UpdateAddress(addressId), request, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await RemoveAddressReadCachesAsync(ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Deletes an owned address using optimistic concurrency.
    /// </summary>
    public async Task<Result> DeleteAddressAsync(Guid addressId, DeleteMemberAddressRequest request, CancellationToken ct)
    {
        if (addressId == Guid.Empty) return Result.Fail("AddressId is required.");
        if (request is null) throw new ArgumentNullException(nameof(request));

        var result = await _api.PostNoContentAsync(ApiRoutes.Profile.DeleteAddress(addressId), request, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await RemoveAddressReadCachesAsync(ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Sets default billing and shipping flags for an owned address.
    /// </summary>
    public async Task<Result<MemberAddress>> SetDefaultAddressAsync(Guid addressId, SetMemberDefaultAddressRequest request, CancellationToken ct)
    {
        if (addressId == Guid.Empty) return Result<MemberAddress>.Fail("AddressId is required.");
        if (request is null) throw new ArgumentNullException(nameof(request));

        var result = await _api.PostResultAsync<SetMemberDefaultAddressRequest, MemberAddress>(ApiRoutes.Profile.SetDefaultAddress(addressId), request, ct).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await RemoveAddressReadCachesAsync(ct).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Retrieves the CRM customer profile linked to the current identity account.
    /// </summary>
    public async Task<LinkedCustomerProfile?> GetLinkedCustomerAsync(CancellationToken ct)
    {
        var cacheKey = await GetScopedCacheKeyAsync("profile.customer", ct).ConfigureAwait(false);
        var cached = await _cache.GetFreshAsync<LinkedCustomerProfile>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.GetAsync<LinkedCustomerProfile>(ApiRoutes.Profile.GetLinkedCustomer, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ProfileCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<LinkedCustomerProfile>(cacheKey, ProfileFallbackMaxAge, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves richer CRM customer context linked to the current identity account.
    /// </summary>
    public async Task<MemberCustomerContext?> GetLinkedCustomerContextAsync(CancellationToken ct)
    {
        var cacheKey = await GetScopedCacheKeyAsync("profile.customer-context", ct).ConfigureAwait(false);
        var cached = await _cache.GetFreshAsync<MemberCustomerContext>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var response = await _api.GetAsync<MemberCustomerContext>(ApiRoutes.Profile.GetLinkedCustomerContext, ct).ConfigureAwait(false);
        if (response is not null)
        {
            await _cache.SetAsync(cacheKey, response, ProfileCacheTtl, ct).ConfigureAwait(false);
            return response;
        }

        return await _cache.GetUsableAsync<MemberCustomerContext>(cacheKey, ProfileFallbackMaxAge, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Requests deactivation and anonymization of the current authenticated user account.
    /// </summary>
    public async Task<Result> RequestAccountDeletionAsync(RequestAccountDeletionRequest request, CancellationToken ct)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        return await _api.PostNoContentAsync(ApiRoutes.Profile.RequestAccountDeletion, request, ct).ConfigureAwait(false);
    }

    private async Task RemoveProfileReadCachesAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync("profile.me", ct).ConfigureAwait(false), ct).ConfigureAwait(false);
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync("profile.customer", ct).ConfigureAwait(false), ct).ConfigureAwait(false);
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync("profile.customer-context", ct).ConfigureAwait(false), ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes address and profile-summary caches after address-book writes so profile cards cannot show stale defaults.
    /// </summary>
    private async Task RemoveAddressReadCachesAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync("profile.addresses", ct).ConfigureAwait(false), ct).ConfigureAwait(false);
        await RemoveProfileReadCachesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Removes preference and linked customer context caches after preference writes.
    /// </summary>
    private async Task RemovePreferencesCacheAsync(CancellationToken ct)
    {
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync("profile.preferences", ct).ConfigureAwait(false), ct).ConfigureAwait(false);
        await _cache.RemoveAsync(await GetScopedCacheKeyAsync("profile.customer-context", ct).ConfigureAwait(false), ct).ConfigureAwait(false);
    }

    private async Task<string> GetScopedCacheKeyAsync(string suffix, CancellationToken ct)
    {
        var (accessToken, _) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
        var subject = JwtClaimReader.GetSubject(accessToken);
        return string.IsNullOrWhiteSpace(subject)
            ? $"{suffix}:{BuildFallbackScope(accessToken)}"
            : $"{suffix}:{subject}";
    }

    /// <summary>
    /// Builds a non-readable cache scope when the JWT subject cannot be parsed.
    /// This prevents authenticated profile cache entries from falling back to a shared unscoped key.
    /// </summary>
    private static string BuildFallbackScope(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return "anonymous";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
