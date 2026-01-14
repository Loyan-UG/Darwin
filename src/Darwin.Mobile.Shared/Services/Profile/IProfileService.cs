using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;

namespace Darwin.Mobile.Shared.Services.Profile
{
    public interface IProfileService
    {
        Task<CustomerProfile?> GetMeAsync(CancellationToken ct);
        Task<bool> UpdateMeAsync(CustomerProfile profile, CancellationToken ct);
    }
}