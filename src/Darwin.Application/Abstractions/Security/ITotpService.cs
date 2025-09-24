using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Security
{
    /// <summary>Service for generating/verifying TOTP codes.</summary>
    public interface ITotpService
    {
        /// <summary>Generates a Base32 secret for TOTP and returns it.</summary>
        Task<string> NewSecretBase32Async(CancellationToken ct = default);

        /// <summary>Verifies a user-entered TOTP code against a Base32 secret.</summary>
        bool VerifyCode(string secretBase32, string code);
    }
}
