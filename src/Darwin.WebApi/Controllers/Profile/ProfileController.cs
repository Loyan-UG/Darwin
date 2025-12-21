using System;
using System.Threading;
using System.Threading.Tasks;
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
    /// Contract-first: returns and accepts Darwin.Contracts DTOs.
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
        public ProfileController(
            GetCurrentUserProfileHandler getCurrentUserProfileHandler,
            UpdateCurrentUserHandler updateCurrentUserHandler)
        {
            _getCurrentUserProfileHandler = getCurrentUserProfileHandler ?? throw new ArgumentNullException(nameof(getCurrentUserProfileHandler));
            _updateCurrentUserHandler = updateCurrentUserHandler ?? throw new ArgumentNullException(nameof(updateCurrentUserHandler));
        }

        /// <summary>
        /// Returns the current user's profile in an edit-ready shape (includes RowVersion).
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(CustomerProfile), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var result = await _getCurrentUserProfileHandler.HandleAsync(ct);

            if (!result.Succeeded)
                return ProblemFromResult(result);

            if (result.Value is null)
                return NotFoundProblem("Profile not found.");

            var contract = new CustomerProfile
            {
                Email = result.Value.Email,
                FirstName = result.Value.FirstName,
                LastName = result.Value.LastName,
                PhoneE164 = result.Value.PhoneE164,
                RowVersion = result.Value.RowVersion ?? Array.Empty<byte>()
            };

            return Ok(contract);
        }

        /// <summary>
        /// Updates the current user's profile using optimistic concurrency (RowVersion).
        /// </summary>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateMe([FromBody] CustomerProfile? request, CancellationToken ct)
        {
            if (request is null)
                return BadRequestProblem("Request body is required.");

            var dto = new UserProfileEditDto
            {
                Email = request.Email ?? string.Empty,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                PhoneE164 = request.PhoneE164,
                RowVersion = request.RowVersion ?? Array.Empty<byte>()
            };

            var result = await _updateCurrentUserHandler.HandleAsync(dto, ct);

            if (!result.Succeeded)
                return ProblemFromResult(result);

            return NoContent();
        }
    }
}
