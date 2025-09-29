using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Auth.WebAuthn
{
    /// <summary>
    /// WebAuthn adapter backed by fido2-net-lib v4.
    /// Creates options (creation/assertion) from SiteSettings RP config and verifies
    /// client responses. Persistence of issued options and resulting credentials is
    /// done by callers (Application handlers).
    /// </summary>
    public sealed class Fido2WebAuthnService : IWebAuthnService
    {
        private readonly IAppDbContext _db;
        private readonly IRelyingPartyFromSiteSettingsProvider _rp;

        public Fido2WebAuthnService(IAppDbContext db, IRelyingPartyFromSiteSettingsProvider rp)
        {
            _db = db;
            _rp = rp;
        }

        /// <inheritdoc />
        public async Task<(string OptionsJson, byte[] Challenge)> BeginRegistrationAsync(
            Guid userId, string userName, string displayName, CancellationToken ct = default)
        {
            var (rpId, rpName, origins, requireUserVerification) = await _rp.GetAsync(ct);

            var fido = new Fido2(new Fido2Configuration
            {
                ServerDomain = rpId,
                ServerName = rpName,
                Origins = new HashSet<string>(origins)
            });

            var user = new Fido2User
            {
                Id = userId.ToByteArray(),
                Name = userName,
                DisplayName = displayName
            };

            // Exclude user's existing credentials from creation ceremony
            var exclude = await _db.Set<UserWebAuthnCredential>()
                .AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
                .ToListAsync(ct);

            var uv = requireUserVerification ? UserVerificationRequirement.Required : UserVerificationRequirement.Preferred;

            var creationOptions = fido.RequestNewCredential(new RequestNewCredentialParams
            {
                User = user,
                ExcludeCredentials = exclude,
                AuthenticatorSelection = new AuthenticatorSelection
                {
                    ResidentKey = ResidentKeyRequirement.Discouraged,
                    UserVerification = uv
                },
                AttestationPreference = AttestationConveyancePreference.None,
                Extensions = new AuthenticationExtensionsClientInputs
                {
                    CredProps = true
                }
            });

            var json = creationOptions.ToJson();
            return (json, creationOptions.Challenge);
        }

        /// <inheritdoc />
        public async Task<(bool Ok, byte[] CredentialId, byte[] PublicKey, Guid? Aaguid, string CredType, string? AttestationFmt, uint SignCount, bool IsSynced, string? Error)>
            FinishRegistrationAsync(string clientResponseJson, string optionsJson, CancellationToken ct = default)
        {
            var (rpId, rpName, origins, _) = await _rp.GetAsync(ct);

            var fido = new Fido2(new Fido2Configuration
            {
                ServerDomain = rpId,
                ServerName = rpName,
                Origins = new HashSet<string>(origins)
            });

            var attestation = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(clientResponseJson);
            var originalOptions = JsonSerializer.Deserialize<CredentialCreateOptions>(optionsJson);

            if (attestation is null || originalOptions is null)
                return (false, Array.Empty<byte>(), Array.Empty<byte>(), null, "public-key", null, 0, false, "Invalid JSON payload(s).");

            async Task<bool> IsUniqueAsync(IsCredentialIdUniqueToUserParams p, CancellationToken _)
            {
                var exists = await _db.Set<UserWebAuthnCredential>()
                    .AnyAsync(c => c.CredentialId == p.CredentialId && !c.IsDeleted, ct);
                return !exists;
            }

            try
            {
                var reg = await fido.MakeNewCredentialAsync(new MakeNewCredentialParams
                {
                    AttestationResponse = attestation,
                    OriginalOptions = originalOptions,
                    IsCredentialIdUniqueToUserCallback = IsUniqueAsync
                });

                var credentialIdBytes = reg.Id.ToArray();
                var pubKey = reg.PublicKey;
                var aaguid = reg.AaGuid;
                var signCount = reg.SignCount; // <-- v4 property name
                var attFmt = reg.AttestationFormat;

                var isSynced = false; // heuristic (not determined here)

                return (true, credentialIdBytes, pubKey, aaguid, "public-key", attFmt, signCount, isSynced, null);
            }
            catch (Fido2VerificationException ex)
            {
                return (false, Array.Empty<byte>(), Array.Empty<byte>(), null, "public-key", null, 0, false, ex.Message);
            }
            catch (Exception ex)
            {
                return (false, Array.Empty<byte>(), Array.Empty<byte>(), null, "public-key", null, 0, false, "Unexpected error: " + ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task<(string OptionsJson, byte[] Challenge)> BeginLoginAsync(
            Guid userId, IReadOnlyCollection<byte[]> allowedCredentialIds, CancellationToken ct = default)
        {
            var (rpId, rpName, origins, requireUserVerification) = await _rp.GetAsync(ct);

            var fido = new Fido2(new Fido2Configuration
            {
                ServerDomain = rpId,
                ServerName = rpName,
                Origins = new HashSet<string>(origins)
            });

            var descriptors = (allowedCredentialIds ?? Array.Empty<byte[]>())
                .Select(id => new PublicKeyCredentialDescriptor(id))
                .ToList();

            var uv = requireUserVerification ? UserVerificationRequirement.Required : UserVerificationRequirement.Preferred;

            var assertionOptions = fido.GetAssertionOptions(new GetAssertionOptionsParams
            {
                AllowedCredentials = descriptors,
                UserVerification = uv,
                Extensions = new AuthenticationExtensionsClientInputs()
            });

            var json = assertionOptions.ToJson();
            return (json, assertionOptions.Challenge);
        }

        /// <inheritdoc />
        public async Task<(bool Ok, byte[] CredentialId, uint NewSignCount, string? Error)>
            FinishLoginAsync(string clientResponseJson, string optionsJson, CancellationToken ct = default)
        {
            var (rpId, rpName, origins, _) = await _rp.GetAsync(ct);

            var fido = new Fido2(new Fido2Configuration
            {
                ServerDomain = rpId,
                ServerName = rpName,
                Origins = new HashSet<string>(origins)
            });

            var assertion = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(clientResponseJson);
            var originalOptions = JsonSerializer.Deserialize<AssertionOptions>(optionsJson);

            if (assertion is null || originalOptions is null)
                return (false, Array.Empty<byte>(), 0, "Invalid JSON payload(s).");

            try
            {
                // Base64Url decode credential id from client
                var credentialId = Fido2NetLib.Decode(assertion.Id);

                var stored = await _db.Set<UserWebAuthnCredential>()
                    .FirstOrDefaultAsync(c => c.CredentialId == credentialId && !c.IsDeleted, ct);

                if (stored is null)
                    return (false, Array.Empty<byte>(), 0, "Credential not found.");

                // If userHandle is provided by authenticator, confirm ownership
                var userHandleBytes = stored.UserHandle; // may be null

                async Task<bool> IsOwner(IsUserHandleOwnerOfCredentialIdParams p, CancellationToken _)
                {
                    if (p.UserHandle is null || p.UserHandle.Length == 0) return true;
                    if (userHandleBytes is null || userHandleBytes.Length == 0) return false;
                    return p.UserHandle.AsSpan().SequenceEqual(userHandleBytes);
                }

                var verify = await fido.MakeAssertionAsync(new MakeAssertionParams
                {
                    AssertionResponse = assertion,
                    OriginalOptions = originalOptions,
                    StoredPublicKey = stored.PublicKey,
                    StoredSignatureCounter = stored.SignatureCounter,
                    IsUserHandleOwnerOfCredentialIdCallback = IsOwner
                });

                // v4 exposes SignCount and CredentialId (Base64Url)
                return (true, verify.CredentialId.ToArray(), verify.SignCount, null);
            }
            catch (Fido2VerificationException ex)
            {
                return (false, Array.Empty<byte>(), 0, ex.Message);
            }
            catch (Exception ex)
            {
                return (false, Array.Empty<byte>(), 0, "Unexpected error: " + ex.Message);
            }
        }
    }
}
