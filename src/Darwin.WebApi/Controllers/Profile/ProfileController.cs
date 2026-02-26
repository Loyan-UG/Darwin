using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Contracts.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers.Profile
{
    /// <summary>
    /// Provides endpoints for reading and updating the current authenticated user's profile.
    /// Contract-first: accepts and returns Darwin.Contracts.Profile DTOs only.
    /// </summary>
    [ApiController]
    [Route("api/v1/profile")]
    [Authorize(Policy = "perm:AccessMemberArea")]
    public sealed class ProfileController : ApiControllerBase
    {
        private readonly GetCurrentUserProfileHandler _getCurrentUserProfileHandler;
        private readonly UpdateCurrentUserHandler _updateCurrentUserHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileController"/> class.
        /// </summary>
        /// <param name="getCurrentUserProfileHandler">Application query handler for loading the current user's profile.</param>
        /// <param name="updateCurrentUserHandler">Application command handler for updating the current user's profile.</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
        public ProfileController(
            GetCurrentUserProfileHandler getCurrentUserProfileHandler,
            UpdateCurrentUserHandler updateCurrentUserHandler)
        {
            _getCurrentUserProfileHandler =
                getCurrentUserProfileHandler ?? throw new ArgumentNullException(nameof(getCurrentUserProfileHandler));

            _updateCurrentUserHandler =
                updateCurrentUserHandler ?? throw new ArgumentNullException(nameof(updateCurrentUserHandler));
        }

        /// <summary>
        /// Returns the current user's profile in an edit-ready shape (includes optimistic concurrency token).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Current user's profile.</returns>
        [HttpGet("me")]
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
                return NotFoundProblem("Profile not found.");

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
        /// Updates the current user's profile using optimistic concurrency (RowVersion).
        /// </summary>
        /// <param name="request">Contract DTO containing updated profile fields.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>No content on success.</returns>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(Darwin.Contracts.Common.ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateMe([FromBody] CustomerProfile? request, CancellationToken ct)
        {
            if (request is null)
                return BadRequestProblem("Request body is required.");

            // API-level validation: keep it minimal but deterministic.
            // Application validators will do the deeper checks.
            if (request.Id == Guid.Empty)
                return BadRequestProblem("Id must not be empty.");

            if (request.RowVersion is null || request.RowVersion.Length == 0)
                return BadRequestProblem("RowVersion must be provided for optimistic concurrency.");

            if (string.IsNullOrWhiteSpace(request.Locale))
                return BadRequestProblem("Locale must be provided.");

            if (string.IsNullOrWhiteSpace(request.Timezone))
                return BadRequestProblem("Timezone must be provided.");

            if (string.IsNullOrWhiteSpace(request.Currency))
                return BadRequestProblem("Currency must be provided.");

            // API-level minimal guards: keep null-safety, but leave real validation to Application validators.
            // Avoid passing nulls into Application DTO and keep strings deterministic.
            var dto = new UserProfileEditDto
            {
                // IMPORTANT:
                // UpdateCurrentUserHandler enforces currentUserId == dto.Id.
                // If we do not map Id here, default Guid.Empty causes update to fail.
                Id = request.Id,

                Email = request.Email ?? string.Empty,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                PhoneE164 = request.PhoneE164,

                Locale = request.Locale,
                Timezone = request.Timezone,
                Currency = request.Currency,

                // Ensure non-null token to avoid null deref and to keep concurrency semantics explicit.
                RowVersion = request.RowVersion ?? Array.Empty<byte>()
            };

            var result = await _updateCurrentUserHandler.HandleAsync(dto, ct);

            if (!result.Succeeded)
                return ProblemFromResult(result);

            return NoContent();
        }
    }
}
