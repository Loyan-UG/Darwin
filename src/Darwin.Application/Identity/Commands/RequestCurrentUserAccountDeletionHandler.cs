using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Auth;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Deactivates and anonymizes the currently authenticated user without physically deleting the user row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This workflow is intended for consumer self-service account deletion requests. It preserves referential integrity
    /// for related business, loyalty, audit, and history records by keeping the primary user row intact.
    /// </para>
    /// <para>
    /// Security behavior:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Resolves the target account exclusively from <see cref="ICurrentUserService"/>.</description></item>
    /// <item><description>Revokes outstanding token-based sessions and disables registered devices.</description></item>
    /// <item><description>Rotates the security stamp and clears direct personal profile fields.</description></item>
    /// <item><description>Replaces login identifiers with a unique non-personal placeholder so future login with the old credentials is impossible.</description></item>
    /// </list>
    /// </remarks>
    public sealed class RequestCurrentUserAccountDeletionHandler
    {
        private const string DeletedDisplayName = "Deleted User";
        private const string DeletedFirstName = "Deleted";
        private const string DeletedLastName = "User";
        private const string DeletedStreet = "Deleted";
        private const string DeletedPostalCode = "00000";
        private const string DeletedCity = "Deleted";
        private const string DeletedCountryCode = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault;

        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISecurityStampService _securityStampService;
        private readonly IClock _clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCurrentUserAccountDeletionHandler"/> class.
        /// </summary>
        /// <param name="db">Application persistence abstraction.</param>
        /// <param name="currentUserService">Resolves the authenticated caller.</param>
        /// <param name="securityStampService">Generates a fresh security stamp for the deactivated account.</param>
        /// <param name="clock">Provides the current UTC timestamp.</param>
        public RequestCurrentUserAccountDeletionHandler(
            IAppDbContext db,
            ICurrentUserService currentUserService,
            ISecurityStampService securityStampService,
            IClock clock)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _securityStampService = securityStampService ?? throw new ArgumentNullException(nameof(securityStampService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Executes the current-user account deletion request.
        /// </summary>
        /// <param name="confirmIrreversibleDeletion">
        /// Indicates whether the caller explicitly acknowledged the irreversible anonymization/deactivation action.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A successful result when the account was deactivated and anonymized.</returns>
        public async Task<Result> HandleAsync(bool confirmIrreversibleDeletion, CancellationToken ct = default)
        {
            if (!confirmIrreversibleDeletion)
            {
                return Result.Fail("Explicit deletion confirmation is required.");
            }

            var currentUserId = _currentUserService.GetCurrentUserId();
            if (currentUserId == Guid.Empty)
            {
                return Result.Fail("User is not authenticated.");
            }

            var user = await _db.Set<User>()
                .FirstOrDefaultAsync(x => x.Id == currentUserId && !x.IsDeleted && x.IsActive, ct)
                .ConfigureAwait(false);

            if (user is null)
            {
                return Result.Fail("Active user account not found.");
            }

            if (user.IsSystem)
            {
                return Result.Fail("System users cannot request account deletion.");
            }

            var nowUtc = _clock.UtcNow;
            var anonymizedEmail = BuildDeletedEmail(user.Id);

            ApplyUserAnonymization(user, anonymizedEmail);

            await InvalidateUserTokensAsync(user.Id, nowUtc, ct).ConfigureAwait(false);
            await DisableUserDevicesAsync(user.Id, nowUtc, ct).ConfigureAwait(false);
            await AnonymizeUserAddressesAsync(user.Id, ct).ConfigureAwait(false);
            await DisableExternalAndSecondFactorCredentialsAsync(user.Id, ct).ConfigureAwait(false);

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return Result.Ok();
        }

        /// <summary>
        /// Applies direct-account anonymization and deactivation to the user aggregate.
        /// </summary>
        private void ApplyUserAnonymization(User user, string anonymizedEmail)
        {
            user.IsActive = false;
            user.Email = anonymizedEmail;
            user.NormalizedEmail = anonymizedEmail.ToUpperInvariant();
            user.UserName = anonymizedEmail;
            user.NormalizedUserName = anonymizedEmail.ToUpperInvariant();
            user.EmailConfirmed = false;
            user.PasswordHash = string.Empty;
            user.SecurityStamp = _securityStampService.NewStamp();

            user.PhoneE164 = null;
            user.PhoneNumberConfirmed = false;
            user.TwoFactorEnabled = false;
            user.LockoutEndUtc = null;
            user.AccessFailedCount = 0;

            user.FirstName = DeletedFirstName;
            user.LastName = DeletedLastName;
            user.Company = null;
            user.VatId = null;
            user.DefaultBillingAddressId = null;
            user.DefaultShippingAddressId = null;

            user.MarketingConsent = false;
            user.ChannelsOptInJson = "{}";
            user.AnonymousId = null;
            user.Tags = null;
        }

        /// <summary>
        /// Marks all token-like credentials for the user as unusable.
        /// </summary>
        private async Task InvalidateUserTokensAsync(Guid userId, DateTime nowUtc, CancellationToken ct)
        {
            var userTokens = await _db.Set<UserToken>()
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var token in userTokens)
            {
                if (token.UsedAtUtc is null)
                {
                    token.MarkUsed(nowUtc);
                }
            }

            var passwordResetTokens = await _db.Set<PasswordResetToken>()
                .Where(x => x.UserId == userId && !x.IsDeleted && x.UsedAtUtc == null)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var token in passwordResetTokens)
            {
                token.MarkUsed(nowUtc);
            }
        }

        /// <summary>
        /// Deactivates all push/device registrations that belong to the user.
        /// </summary>
        private async Task DisableUserDevicesAsync(Guid userId, DateTime nowUtc, CancellationToken ct)
        {
            var devices = await _db.Set<UserDevice>()
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var device in devices)
            {
                device.IsActive = false;
                device.NotificationsEnabled = false;
                device.PushToken = null;
                device.PushTokenUpdatedAtUtc = nowUtc;
            }
        }

        /// <summary>
        /// Scrubs reusable address-book records while preserving their rows and foreign-key references.
        /// </summary>
        private async Task AnonymizeUserAddressesAsync(Guid userId, CancellationToken ct)
        {
            var addresses = await _db.Set<Address>()
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var address in addresses)
            {
                address.FullName = DeletedDisplayName;
                address.Company = null;
                address.Street1 = DeletedStreet;
                address.Street2 = null;
                address.PostalCode = DeletedPostalCode;
                address.City = DeletedCity;
                address.State = null;
                address.CountryCode = DeletedCountryCode;
                address.PhoneE164 = null;
                address.IsDefaultBilling = false;
                address.IsDefaultShipping = false;
            }
        }

        /// <summary>
        /// Disables external-login and strong-authentication credentials that would otherwise survive deactivation.
        /// </summary>
        private async Task DisableExternalAndSecondFactorCredentialsAsync(Guid userId, CancellationToken ct)
        {
            var logins = await _db.Set<UserLogin>()
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var login in logins)
            {
                login.DisplayName = DeletedDisplayName;
                login.IsDeleted = true;
            }

            var totpSecrets = await _db.Set<UserTwoFactorSecret>()
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var secret in totpSecrets)
            {
                secret.IsDeleted = true;
            }

            var webAuthnCredentials = await _db.Set<UserWebAuthnCredential>()
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var credential in webAuthnCredentials)
            {
                credential.IsDeleted = true;
            }
        }

        /// <summary>
        /// Builds a unique non-personal replacement email for the anonymized account.
        /// </summary>
        private static string BuildDeletedEmail(Guid userId)
            => $"deleted-user-{userId:N}@deleted.local";
    }
}
