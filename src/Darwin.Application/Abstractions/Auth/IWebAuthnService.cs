using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Abstraction over WebAuthn ceremonies. The service returns options as JSON
    /// so the Web layer can pass them directly to the browser API and store them temporarily.
    /// For verification, the service requires the original options JSON (not only the challenge)
    /// to comply with fido2-net-lib v4 requirements.
    /// </summary>
    public interface IWebAuthnService
    {
        /// <summary>
        /// Begins registration by producing a CredentialCreateOptions JSON for the browser.
        /// </summary>
        Task<(string OptionsJson, byte[] Challenge)> BeginRegistrationAsync(
            Guid userId,
            string userName,
            string displayName,
            CancellationToken ct = default);

        /// <summary>
        /// Finishes registration by verifying attestation with the original options JSON.
        /// Returns credential material to be persisted by the caller (Application handler).
        /// </summary>
        Task<(bool Ok, byte[] CredentialId, byte[] PublicKey, Guid? Aaguid, string CredType, string? AttestationFmt, uint SignCount, bool IsSynced, string? Error)>
            FinishRegistrationAsync(
                string clientResponseJson,
                string optionsJson,
                CancellationToken ct = default);

        /// <summary>
        /// Begins login by producing an AssertionOptions JSON for the browser.
        /// Caller can restrict credentials via allowedCredentialIds (optional).
        /// </summary>
        Task<(string OptionsJson, byte[] Challenge)> BeginLoginAsync(
            Guid userId,
            IReadOnlyCollection<byte[]> allowedCredentialIds,
            CancellationToken ct = default);

        /// <summary>
        /// Finishes login by verifying assertion with the original options JSON.
        /// Returns the credential id and the updated sign counter.
        /// </summary>
        Task<(bool Ok, byte[] CredentialId, uint NewSignCount, string? Error)>
            FinishLoginAsync(
                string clientResponseJson,
                string optionsJson,
                CancellationToken ct = default);
    }
}
