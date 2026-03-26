using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Profile
{
    /// <summary>
    /// Provides profile-related operations for the current authenticated mobile user.
    /// </summary>
    public interface IProfileService
    {
        /// <summary>
        /// Loads the current user's editable profile payload.
        /// </summary>
        Task<CustomerProfile?> GetMeAsync(CancellationToken ct);

        /// <summary>
        /// Updates the current user's editable profile fields.
        /// </summary>
        Task<Result> UpdateMeAsync(CustomerProfile profile, CancellationToken ct);

        /// <summary>
        /// Loads the current user's privacy and communication preferences.
        /// </summary>
        Task<MemberPreferences?> GetPreferencesAsync(CancellationToken ct);

        /// <summary>
        /// Updates the current user's privacy and communication preferences.
        /// </summary>
        Task<Result> UpdatePreferencesAsync(UpdateMemberPreferencesRequest preferences, CancellationToken ct);

        /// <summary>
        /// Loads the current user's reusable address book.
        /// </summary>
        Task<IReadOnlyList<MemberAddress>> GetAddressesAsync(CancellationToken ct);

        /// <summary>
        /// Creates a reusable address for the current user.
        /// </summary>
        Task<Result<MemberAddress>> CreateAddressAsync(CreateMemberAddressRequest request, CancellationToken ct);

        /// <summary>
        /// Updates an address owned by the current user.
        /// </summary>
        Task<Result<MemberAddress>> UpdateAddressAsync(Guid addressId, UpdateMemberAddressRequest request, CancellationToken ct);

        /// <summary>
        /// Deletes an address owned by the current user.
        /// </summary>
        Task<Result> DeleteAddressAsync(Guid addressId, DeleteMemberAddressRequest request, CancellationToken ct);

        /// <summary>
        /// Sets default billing and shipping flags on an address owned by the current user.
        /// </summary>
        Task<Result<MemberAddress>> SetDefaultAddressAsync(Guid addressId, SetMemberDefaultAddressRequest request, CancellationToken ct);

        /// <summary>
        /// Loads the CRM customer profile linked to the current identity account, when one exists.
        /// </summary>
        Task<LinkedCustomerProfile?> GetLinkedCustomerAsync(CancellationToken ct);

        /// <summary>
        /// Loads richer CRM customer context linked to the current identity account.
        /// </summary>
        Task<MemberCustomerContext?> GetLinkedCustomerContextAsync(CancellationToken ct);

        /// <summary>
        /// Requests irreversible account deactivation and anonymization for the current authenticated user.
        /// </summary>
        Task<Result> RequestAccountDeletionAsync(RequestAccountDeletionRequest request, CancellationToken ct);
    }
}
