using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Security;

namespace Darwin.WebAdmin.Services.Security
{
    public interface IAuthAntiBotChallengeService : IAuthAntiBotVerifier
    {
        string CreateChallengeToken();
    }
}
