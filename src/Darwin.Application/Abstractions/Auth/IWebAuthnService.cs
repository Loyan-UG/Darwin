using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Auth
{
    /// <summary>
    /// Abstraction over WebAuthn (fido2-net-lib in Infrastructure) to keep Application decoupled.
    /// Produces JSON options for the browser and parses/verifies the client responses.
    /// </summary>
    public interface IWebAuthnService
    {
        /// <summary>
        /// Begins registration: returns PublicKeyCredentialCreationOptions JSON and the raw challenge to be persisted.
        /// </summary>
        Task<(string OptionsJson, byte[] Challenge)> BeginRegistrationAsync(Guid userId, string userName, string displayName, CancellationToken ct);

        /// <summary>
        /// Completes registration: verifies attestation and returns credential material for persistence.
        /// </summary>
        Task<(bool Ok, byte[] CredentialId, byte[] PublicKey, Guid? Aaguid, string CredType, string? AttestationFmt, uint SignCount, bool IsSynced, string? Error)>
            FinishRegistrationAsync(string clientResponseJson, byte[] expectedChallenge, CancellationToken ct);

        /// <summary>
        /// Begins login: returns PublicKeyCredentialRequestOptions JSON and raw challenge.
        /// </summary>
        Task<(string OptionsJson, byte[] Challenge)> BeginLoginAsync(Guid userId, IReadOnlyCollection<byte[]> allowedCredentialIds, CancellationToken ct);

        /// <summary>
        /// Completes login: verifies assertion and returns the credentialId/signcount to update.
        /// </summary>
        Task<(bool Ok, byte[] CredentialId, uint NewSignCount, string? Error)>
            FinishLoginAsync(string clientResponseJson, byte[] expectedChallenge, CancellationToken ct);
    }
}
