using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Security
{
    public sealed class AuthAntiBotCheck
    {
        public string? ChallengeToken { get; init; }
        public string? HoneypotValue { get; init; }
        public string? ClientIpAddress { get; init; }
        public string? UserAgent { get; init; }
    }

    public sealed class AuthAntiBotVerificationResult
    {
        public bool Succeeded { get; init; }
        public string? FailureReason { get; init; }

        public static AuthAntiBotVerificationResult Success() => new() { Succeeded = true };
        public static AuthAntiBotVerificationResult Fail(string reason) => new() { Succeeded = false, FailureReason = reason };
    }

    public interface IAuthAntiBotVerifier
    {
        Task<AuthAntiBotVerificationResult> VerifyAsync(AuthAntiBotCheck check, CancellationToken ct = default);
    }
}
