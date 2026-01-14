using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Shared.Api;

namespace Darwin.Mobile.Shared.Services.Profile
{
    public sealed class ProfileService : IProfileService
    {
        private readonly IApiClient _api;
        public ProfileService(IApiClient api) => _api = api ?? throw new ArgumentNullException(nameof(api));

        public Task<CustomerProfile?> GetMeAsync(CancellationToken ct)
            => _api.GetAsync<CustomerProfile>(ApiRoutes.Profile.GetMe, ct);

        public async Task<bool> UpdateMeAsync(CustomerProfile profile, CancellationToken ct)
        {
            if (profile is null) throw new ArgumentNullException(nameof(profile));
            var res = await _api.PostResultAsync<CustomerProfile, object?>(
                ApiRoutes.Profile.UpdateMe,
                profile,
                ct).ConfigureAwait(false);
            return res.Succeeded;
        }
    }
}