using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Profile
{
    public interface IProfileService
    {
        Task<CustomerProfile?> GetMeAsync(CancellationToken ct);
        Task<Result> UpdateMeAsync(CustomerProfile profile, CancellationToken ct);
    }
}
