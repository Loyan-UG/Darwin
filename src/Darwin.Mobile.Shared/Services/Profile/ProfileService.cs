using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

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
        /// Retrieves the current user's profile using the canonical member-profile endpoint.
        /// </summary>
        public Task<CustomerProfile?> GetMeAsync(CancellationToken ct)
            => _api.GetAsync<CustomerProfile>(ApiRoutes.Profile.GetMe, ct);

        /// <summary>
        /// Updates the current user's profile using the canonical member-profile endpoint.
        /// The server returns 204 No Content on success; this method maps that to true.
        /// </summary>
        public async Task<Result> UpdateMeAsync(CustomerProfile profile, CancellationToken ct)
        {
            if (profile is null) throw new ArgumentNullException(nameof(profile));

            return await _api.PutNoContentAsync(ApiRoutes.Profile.UpdateMe, profile, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the current user's privacy and communication preferences.
        /// </summary>
        public Task<MemberPreferences?> GetPreferencesAsync(CancellationToken ct)
            => _api.GetAsync<MemberPreferences>(ApiRoutes.Profile.GetPreferences, ct);

        /// <summary>
        /// Retrieves the current user's reusable address book.
        /// </summary>
        public async Task<IReadOnlyList<MemberAddress>> GetAddressesAsync(CancellationToken ct)
            => await _api.GetAsync<IReadOnlyList<MemberAddress>>(ApiRoutes.Profile.GetAddresses, ct).ConfigureAwait(false)
               ?? Array.Empty<MemberAddress>();

        /// <summary>
        /// Updates the current user's privacy and communication preferences using optimistic concurrency.
        /// </summary>
        public async Task<Result> UpdatePreferencesAsync(UpdateMemberPreferencesRequest preferences, CancellationToken ct)
        {
            if (preferences is null) throw new ArgumentNullException(nameof(preferences));

            return await _api.PutNoContentAsync(ApiRoutes.Profile.UpdatePreferences, preferences, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a reusable address for the current user.
        /// </summary>
        public async Task<Result<MemberAddress>> CreateAddressAsync(CreateMemberAddressRequest request, CancellationToken ct)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            return await _api.PostResultAsync<CreateMemberAddressRequest, MemberAddress>(ApiRoutes.Profile.CreateAddress, request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an owned address using optimistic concurrency.
        /// </summary>
        public async Task<Result<MemberAddress>> UpdateAddressAsync(Guid addressId, UpdateMemberAddressRequest request, CancellationToken ct)
        {
            if (addressId == Guid.Empty) return Result<MemberAddress>.Fail("AddressId is required.");
            if (request is null) throw new ArgumentNullException(nameof(request));

            return await _api.PutResultAsync<UpdateMemberAddressRequest, MemberAddress>(ApiRoutes.Profile.UpdateAddress(addressId), request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an owned address using optimistic concurrency.
        /// </summary>
        public async Task<Result> DeleteAddressAsync(Guid addressId, DeleteMemberAddressRequest request, CancellationToken ct)
        {
            if (addressId == Guid.Empty) return Result.Fail("AddressId is required.");
            if (request is null) throw new ArgumentNullException(nameof(request));

            return await _api.PostNoContentAsync(ApiRoutes.Profile.DeleteAddress(addressId), request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets default billing and shipping flags for an owned address.
        /// </summary>
        public async Task<Result<MemberAddress>> SetDefaultAddressAsync(Guid addressId, SetMemberDefaultAddressRequest request, CancellationToken ct)
        {
            if (addressId == Guid.Empty) return Result<MemberAddress>.Fail("AddressId is required.");
            if (request is null) throw new ArgumentNullException(nameof(request));

            return await _api.PostResultAsync<SetMemberDefaultAddressRequest, MemberAddress>(ApiRoutes.Profile.SetDefaultAddress(addressId), request, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the CRM customer profile linked to the current identity account.
        /// </summary>
        public Task<LinkedCustomerProfile?> GetLinkedCustomerAsync(CancellationToken ct)
            => _api.GetAsync<LinkedCustomerProfile>(ApiRoutes.Profile.GetLinkedCustomer, ct);

        /// <summary>
        /// Retrieves richer CRM customer context linked to the current identity account.
        /// </summary>
        public Task<MemberCustomerContext?> GetLinkedCustomerContextAsync(CancellationToken ct)
            => _api.GetAsync<MemberCustomerContext>(ApiRoutes.Profile.GetLinkedCustomerContext, ct);

        /// <summary>
        /// Requests deactivation and anonymization of the current authenticated user account.
        /// </summary>
        /// <param name="request">Deletion confirmation payload.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A functional result indicating whether the server accepted the deletion request.</returns>
        public async Task<Result> RequestAccountDeletionAsync(RequestAccountDeletionRequest request, CancellationToken ct)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            return await _api.PostNoContentAsync(ApiRoutes.Profile.RequestAccountDeletion, request, ct).ConfigureAwait(false);
        }
    }
}
