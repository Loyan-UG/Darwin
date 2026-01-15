using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Shared.Api;

namespace Darwin.Mobile.Shared.Services.Profile
{
    /// <summary>
    /// Profile service for the current authenticated user.
    /// Responsibilities:
    /// - Load the current user's profile (edit-ready shape including concurrency token).
    /// - Update the profile using optimistic concurrency (RowVersion) via HTTP PUT.
    /// </summary>
    public sealed class ProfileService : IProfileService
    {
        private readonly IApiClient _api;

        public ProfileService(IApiClient api) => _api = api ?? throw new ArgumentNullException(nameof(api));

        /// <summary>
        /// Retrieves the current user's profile using GET /api/v1/profile/me.
        /// </summary>
        public Task<CustomerProfile?> GetMeAsync(CancellationToken ct)
            => _api.GetAsync<CustomerProfile>(ApiRoutes.Profile.GetMe, ct);

        /// <summary>
        /// Updates the current user's profile using PUT /api/v1/profile/me.
        /// The server returns 204 No Content on success; this method maps that to true.
        /// </summary>
        public async Task<bool> UpdateMeAsync(CustomerProfile profile, CancellationToken ct)
        {
            if (profile is null) throw new ArgumentNullException(nameof(profile));

            var res = await _api.PutNoContentAsync(ApiRoutes.Profile.UpdateMe, profile, ct).ConfigureAwait(false);
            return res.Succeeded;
        }
    }
}