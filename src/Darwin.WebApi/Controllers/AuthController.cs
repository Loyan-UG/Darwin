using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Contracts.Identity;
using Darwin.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebApi.Controllers
{
    /// <summary>
    /// Provides authentication endpoints for mobile and external clients.
    /// Exposes login, token refresh, and logout operations.
    /// Inputs and outputs use Darwin.Contracts models; handlers use Application DTOs.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly LoginWithPasswordHandler _loginWithPassword;
        private readonly RefreshTokenHandler _refresh;
        private readonly RevokeRefreshTokensHandler _revoke;

        /// <summary>
        /// Initializes a new instance of the authentication controller.
        /// </summary>
        /// <param name="loginWithPassword">Handler that performs email/password authentication.</param>
        /// <param name="refresh">Handler that exchanges a refresh token for a new token pair.</param>
        /// <param name="revoke">Handler that revokes refresh tokens.</param>
        public AuthController(
            LoginWithPasswordHandler loginWithPassword,
            RefreshTokenHandler refresh,
            RevokeRefreshTokensHandler revoke)
        {
            _loginWithPassword = loginWithPassword ?? throw new ArgumentNullException(nameof(loginWithPassword));
            _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
            _revoke = revoke ?? throw new ArgumentNullException(nameof(revoke));
        }

        /// <summary>
        /// Authenticates a user with email and password, returning a JWT access token and refresh token.
        /// </summary>
        /// <param name="request">Password login payload coming from the client.</param>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAsync(
            [FromBody] PasswordLoginRequest request,
            CancellationToken ct)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Map Contracts → Application DTO
            var dto = new PasswordLoginRequestDto
            {
                Email = request.Email ?? string.Empty,
                PasswordPlain = request.Password ?? string.Empty,
                DeviceId = request.DeviceId
            };

            var rateKey = BuildRateKey(request.Email);

            var result = await _loginWithPassword.HandleAsync(dto, rateKey, ct);

            if (result.Succeeded && result.Value is not null)
            {
                var tokenResponse = MapToTokenResponse(result.Value);
                return Ok(tokenResponse);
            }

            return ProblemFromResult(result);
        }

        /// <summary>
        /// Issues a new token pair using a valid refresh token.
        /// </summary>
        /// <param name="request">Refresh token payload.</param>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshAsync(
            [FromBody] RefreshTokenRequest request,
            CancellationToken ct)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var dto = new RefreshRequestDto
            {
                RefreshToken = request.RefreshToken ?? string.Empty,
                DeviceId = request.DeviceId
            };

            var result = await _refresh.HandleAsync(dto, ct);

            if (result.Succeeded && result.Value is not null)
            {
                var tokenResponse = MapToTokenResponse(result.Value);
                return Ok(tokenResponse);
            }

            return ProblemFromResult(result);
        }

        /// <summary>
        /// Revokes the provided refresh token (logout for this device).
        /// </summary>
        /// <param name="request">Logout request containing the refresh token.</param>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> LogoutAsync(
            [FromBody] LogoutRequest request,
            CancellationToken ct)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var dto = new RevokeRefreshRequestDto
            {
                RefreshToken = request.RefreshToken,
                UserId = null,
                DeviceId = null
            };

            var result = await _revoke.HandleAsync(dto, ct);

            if (result.Succeeded)
            {
                return Ok();
            }

            return ProblemFromResult(result);
        }

        /// <summary>
        /// Revokes all refresh tokens for the current user (global logout).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAllAsync(CancellationToken ct)
        {
            var userId = GetUserIdFromClaims(HttpContext.User);
            if (userId is null)
            {
                // Access token is invalid or missing required claims.
                var problem = new Darwin.Contracts.Common.ProblemDetails
                {
                    Status = 401,
                    Title = "Unauthorized",
                    Detail = "User identifier could not be resolved from the access token.",
                    Instance = HttpContext.Request?.Path.Value
                };

                return StatusCode(problem.Status, problem);
            }

            var dto = new RevokeRefreshRequestDto
            {
                UserId = userId,
                RefreshToken = null,
                DeviceId = null
            };

            var result = await _revoke.HandleAsync(dto, ct);

            if (result.Succeeded)
            {
                return Ok();
            }

            return ProblemFromResult(result);
        }

        /// <summary>
        /// Builds a simple rate limit key from email and remote IP address.
        /// This mirrors the logic inside the login handler expectations.
        /// </summary>
        /// <param name="email">User email address.</param>
        /// <returns>Composite rate key.</returns>
        private string BuildRateKey(string? email)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToUpperInvariant();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"{normalizedEmail}|{ip}";
        }

        /// <summary>
        /// Maps an authentication result DTO (Application layer) into the public TokenResponse contract.
        /// </summary>
        /// <param name="dto">Authentication result from the Application layer.</param>
        /// <returns>Token response contract for clients.</returns>
        private static TokenResponse MapToTokenResponse(AuthResultDto dto)
        {
            return new TokenResponse
            {
                AccessToken = dto.AccessToken,
                AccessTokenExpiresAtUtc = dto.AccessTokenExpiresAtUtc,
                RefreshToken = dto.RefreshToken,
                RefreshTokenExpiresAtUtc = dto.RefreshTokenExpiresAtUtc,
                UserId = dto.UserId,
                Email = dto.Email
            };
        }

        /// <summary>
        /// Extracts the user identifier from the current principal using common claim types.
        /// </summary>
        /// <param name="user">The current claims principal.</param>
        /// <returns>User identifier if present and valid, otherwise null.</returns>
        private static Guid? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var id =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("sub") ??
                user.FindFirstValue("uid");

            if (Guid.TryParse(id, out var guid))
            {
                return guid;
            }

            return null;
        }

        /// <summary>
        /// Converts a failed Result into a ProblemDetails response for API clients.
        /// </summary>
        /// <typeparam name="T">Wrapped value type.</typeparam>
        /// <param name="result">Result returned from the Application layer.</param>
        /// <returns>HTTP response with RFC-7807 style problem details.</returns>
        private IActionResult ProblemFromResult<T>(Result<T> result)
        {
            var status = 400;

            var problem = new Darwin.Contracts.Common.ProblemDetails
            {
                Status = status,
                Title = "Request failed",
                Detail = result.Error ?? "The operation could not be completed.",
                Instance = HttpContext.Request?.Path.Value
            };

            return StatusCode(problem.Status, problem);
        }
    }
}
