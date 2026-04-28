using System;
using Darwin.Application;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Contracts.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Profile
{
    /// <summary>
    /// Provides endpoints for reading and updating the current authenticated user's profile.
    /// Contract-first: accepts and returns Darwin.Contracts.Profile DTOs only.
    /// </summary>
    [ApiController]
    [Route("api/v1/member/profile")]
    [Authorize]
    //[Authorize(Policy = "perm:AccessMemberArea")]
    public sealed class ProfileController : ApiControllerBase
    {
        private readonly GetCurrentUserProfileHandler _getCurrentUserProfileHandler;
        private readonly GetCurrentUserPreferencesHandler _getCurrentUserPreferencesHandler;
        private readonly UpdateCurrentUserHandler _updateCurrentUserHandler;
        private readonly UpdateCurrentUserPreferencesHandler _updateCurrentUserPreferencesHandler;
        private readonly RequestCurrentUserAccountDeletionHandler _requestCurrentUserAccountDeletionHandler;
        private readonly RequestPhoneVerificationHandler _requestPhoneVerificationHandler;
        private readonly ConfirmPhoneVerificationHandler _confirmPhoneVerificationHandler;
        private readonly IStringLocalizer<ValidationResource> _validationLocalizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileController"/> class.
        /// </summary>
        /// <param name="getCurrentUserProfileHandler">Application query handler for loading the current user's profile.</param>
        /// <param name="updateCurrentUserHandler">Application command handler for updating the current user's profile.</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
        public ProfileController(
            GetCurrentUserProfileHandler getCurrentUserProfileHandler,
            GetCurrentUserPreferencesHandler getCurrentUserPreferencesHandler,
            UpdateCurrentUserHandler updateCurrentUserHandler,
            UpdateCurrentUserPreferencesHandler updateCurrentUserPreferencesHandler,
            RequestCurrentUserAccountDeletionHandler requestCurrentUserAccountDeletionHandler,
            RequestPhoneVerificationHandler requestPhoneVerificationHandler,
            ConfirmPhoneVerificationHandler confirmPhoneVerificationHandler,
            IStringLocalizer<ValidationResource> validationLocalizer)
        {
            _getCurrentUserProfileHandler =
                getCurrentUserProfileHandler ?? throw new ArgumentNullException(nameof(getCurrentUserProfileHandler));

            _getCurrentUserPreferencesHandler =
                getCurrentUserPreferencesHandler ?? throw new ArgumentNullException(nameof(getCurrentUserPreferencesHandler));

            _updateCurrentUserHandler =
                updateCurrentUserHandler ?? throw new ArgumentNullException(nameof(updateCurrentUserHandler));

            _updateCurrentUserPreferencesHandler =
                updateCurrentUserPreferencesHandler ?? throw new ArgumentNullException(nameof(updateCurrentUserPreferencesHandler));

            _requestCurrentUserAccountDeletionHandler =
                requestCurrentUserAccountDeletionHandler ?? throw new ArgumentNullException(nameof(requestCurrentUserAccountDeletionHandler));

            _requestPhoneVerificationHandler =
                requestPhoneVerificationHandler ?? throw new ArgumentNullException(nameof(requestPhoneVerificationHandler));

            _confirmPhoneVerificationHandler =
                confirmPhoneVerificationHandler ?? throw new ArgumentNullException(nameof(confirmPhoneVerificationHandler));

            _validationLocalizer =
                validationLocalizer ?? throw new ArgumentNullException(nameof(validationLocalizer));
        }

        /// <summary>
        /// Returns the current user's profile in an edit-ready shape (includes optimistic concurrency token).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Current user's profile.</returns>
        [HttpGet("me")]
        [HttpGet("/api/v1/profile/me")]
        [ProducesResponseType(typeof(CustomerProfile), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var result = await _getCurrentUserProfileHandler.HandleAsync(ct);

            if (!result.Succeeded)
                return ProblemFromResult(result);

            // Defensive: even though Result<UserProfileEditDto> is non-nullable, we still guard against null.
            if (result.Value is null)
                return NotFoundProblem(_validationLocalizer["ProfileNotFound"]);

            var value = result.Value;

            var contract = new CustomerProfile
            {
                // IMPORTANT:
                // Id must be returned so clients can round-trip it in PUT /profile/me.
                // Missing this field forces clients to send Guid.Empty and update fails by contract validation.
                Id = value.Id,

                // Email is returned for display, but may be immutable for update depending on Application rules.
                Email = value.Email,

                FirstName = value.FirstName,
                LastName = value.LastName,
                PhoneE164 = value.PhoneE164,
                PhoneNumberConfirmed = value.PhoneNumberConfirmed,

                // These are explicitly mentioned as editable in UpdateCurrentUserHandler summary.
                Locale = value.Locale,
                Timezone = value.Timezone,
                Currency = value.Currency,

                // Always return a non-null token to keep client-side concurrency flows consistent.
                RowVersion = value.RowVersion ?? Array.Empty<byte>()
            };

            return Ok(contract);
        }

        /// <summary>
        /// Returns the current user's privacy and communication preferences.
        /// </summary>
        [HttpGet("preferences")]
        [HttpGet("/api/v1/profile/me/preferences")]
        [ProducesResponseType(typeof(MemberPreferences), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPreferencesAsync(CancellationToken ct)
        {
            var result = await _getCurrentUserPreferencesHandler.HandleAsync(ct).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                return ProblemFromResult(result);
            }

            if (result.Value is null)
            {
                return NotFoundProblem(_validationLocalizer["PreferencesNotFound"]);
            }

            return Ok(new MemberPreferences
            {
                RowVersion = result.Value.RowVersion ?? Array.Empty<byte>(),
                MarketingConsent = result.Value.MarketingConsent,
                AllowEmailMarketing = result.Value.AllowEmailMarketing,
                AllowSmsMarketing = result.Value.AllowSmsMarketing,
                AllowWhatsAppMarketing = result.Value.AllowWhatsAppMarketing,
                AllowPromotionalPushNotifications = result.Value.AllowPromotionalPushNotifications,
                AllowOptionalAnalyticsTracking = result.Value.AllowOptionalAnalyticsTracking,
                AcceptsTermsAtUtc = result.Value.AcceptsTermsAtUtc
            });
        }

        /// <summary>
        /// Updates the current user's profile using optimistic concurrency (RowVersion).
        /// </summary>
        /// <param name="request">Contract DTO containing updated profile fields.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("me")]
        [HttpPut("/api/v1/profile/me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMe([FromBody] CustomerProfile? request, CancellationToken ct)
        {
            if (request is null)
                return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);

            // API-level validation: keep it minimal but deterministic.
            // Application validators will do the deeper checks.
            if (request.Id == Guid.Empty)
                return BadRequestProblem(_validationLocalizer["IdentifierMustNotBeEmpty"]);

            if (request.RowVersion is null || request.RowVersion.Length == 0)
                return BadRequestProblem(_validationLocalizer["RowVersionRequiredForOptimisticConcurrency"]);

            var normalizedLocale = NormalizeText(request.Locale);
            if (normalizedLocale is null)
                return BadRequestProblem(_validationLocalizer["LocaleRequired"]);

            var normalizedTimezone = NormalizeText(request.Timezone);
            if (normalizedTimezone is null)
                return BadRequestProblem(_validationLocalizer["TimezoneRequired"]);

            var normalizedCurrency = NormalizeText(request.Currency)?.ToUpperInvariant();
            if (normalizedCurrency is null)
                return BadRequestProblem(_validationLocalizer["CurrencyRequired"]);

            // API-level minimal guards: keep null-safety, but leave real validation to Application validators.
            // Avoid passing nulls into Application DTO and keep strings deterministic.
            var dto = new UserProfileEditDto
            {
                // IMPORTANT:
                // UpdateCurrentUserHandler enforces currentUserId == dto.Id.
                // If we do not map Id here, default Guid.Empty causes update to fail.
                Id = request.Id,

                Email = NormalizeText(request.Email) ?? string.Empty,
                FirstName = NormalizeText(request.FirstName) ?? string.Empty,
                LastName = NormalizeText(request.LastName) ?? string.Empty,
                PhoneE164 = NormalizeText(request.PhoneE164),

                Locale = normalizedLocale,
                Timezone = normalizedTimezone,
                Currency = normalizedCurrency,

                // Ensure non-null token to avoid null deref and to keep concurrency semantics explicit.
                RowVersion = request.RowVersion ?? Array.Empty<byte>()
            };

            var result = await _updateCurrentUserHandler.HandleAsync(dto, ct);

            if (!result.Succeeded)
                return ProblemFromResult(result);

            return NoContent();
        }

        [HttpPost("me/phone/request-verification")]
        [HttpPost("/api/v1/profile/me/phone/request-verification")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestPhoneVerificationAsync([FromBody] RequestPhoneVerificationRequest? request, CancellationToken ct)
        {
            var channelValue = request?.Channel?.Trim();
            PhoneVerificationChannel? channel = string.IsNullOrWhiteSpace(channelValue)
                ? null
                : string.Equals(channelValue, "WhatsApp", StringComparison.OrdinalIgnoreCase)
                ? PhoneVerificationChannel.WhatsApp
                : PhoneVerificationChannel.Sms;

            var result = await _requestPhoneVerificationHandler.HandleAsync(
                new RequestPhoneVerificationDto { Channel = channel },
                ct).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                return ProblemFromResult(result);
            }

            return NoContent();
        }

        [HttpPost("me/phone/confirm")]
        [HttpPost("/api/v1/profile/me/phone/confirm")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmPhoneVerificationAsync([FromBody] ConfirmPhoneVerificationRequest? request, CancellationToken ct)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequestProblem(_validationLocalizer["VerificationCodeRequired"]);
            }

            var result = await _confirmPhoneVerificationHandler.HandleAsync(
                new ConfirmPhoneVerificationDto { Code = request.Code.Trim() },
                ct).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                return ProblemFromResult(result);
            }

            return NoContent();
        }

        private static string? NormalizeText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>
        /// Updates the current user's privacy and communication preferences using optimistic concurrency.
        /// </summary>
        [HttpPut("preferences")]
        [HttpPut("/api/v1/profile/me/preferences")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePreferencesAsync([FromBody] UpdateMemberPreferencesRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
            }

            if (request.RowVersion is null || request.RowVersion.Length == 0)
            {
                return BadRequestProblem(_validationLocalizer["RowVersionRequiredForOptimisticConcurrency"]);
            }

            var result = await _updateCurrentUserPreferencesHandler.HandleAsync(new UpdateMemberPreferencesDto
            {
                RowVersion = request.RowVersion,
                MarketingConsent = request.MarketingConsent,
                AllowEmailMarketing = request.AllowEmailMarketing,
                AllowSmsMarketing = request.AllowSmsMarketing,
                AllowWhatsAppMarketing = request.AllowWhatsAppMarketing,
                AllowPromotionalPushNotifications = request.AllowPromotionalPushNotifications,
                AllowOptionalAnalyticsTracking = request.AllowOptionalAnalyticsTracking
            }, ct).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                return ProblemFromResult(result);
            }

            return NoContent();
        }

        /// <summary>
        /// Requests irreversible deactivation and anonymization of the current authenticated consumer account.
        /// </summary>
        /// <param name="request">Confirmation payload for the deletion request.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpPost("me/deletion-request")]
        [HttpPost("/api/v1/profile/me/deletion-request")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RequestAccountDeletionAsync([FromBody] RequestAccountDeletionRequest? request, CancellationToken ct)
        {
            if (request is null)
            {
                return BadRequestProblem(_validationLocalizer["RequestPayloadRequired"]);
            }

            if (!request.ConfirmIrreversibleDeletion)
            {
                return BadRequestProblem(_validationLocalizer["ExplicitDeletionConfirmationRequired"]);
            }

            var result = await _requestCurrentUserAccountDeletionHandler
                .HandleAsync(request.ConfirmIrreversibleDeletion, ct)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                return ProblemFromResult(result);
            }

            return NoContent();
        }
    }
}
