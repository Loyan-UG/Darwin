using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Contracts.Identity;
using Darwin.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;


namespace Darwin.WebApi.Controllers
{
    /// <summary>
    /// Provides authentication endpoints for mobile and external clients.
    /// Exposes login, token refresh, and logout operations.
    /// Inputs and outputs use Darwin.Contracts models; handlers use Application DTOs.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ApiControllerBase
    {
        private readonly LoginWithPasswordHandler _loginWithPassword;
        private readonly RefreshTokenHandler _refresh;
        private readonly RevokeRefreshTokensHandler _revoke;
        private readonly RegisterUserHandler _registerUser;
        private readonly ChangePasswordHandler _changePassword;
        private readonly RequestPasswordResetHandler _requestPasswordReset;
        private readonly ResetPasswordHandler _resetPassword;
        private readonly GetRoleIdByKeyHandler _getRoleIdByKey;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the authentication controller.
        /// </summary>
        /// <param name="loginWithPassword">Handler that performs email/password authentication.</param>
        /// <param name="refresh">Handler that exchanges a refresh token for a new token pair.</param>
        /// <param name="revoke">Handler that revokes refresh tokens.</param>
        /// <param name="logger">Logger used to audit authentication-related events.</param>
        public AuthController(
            LoginWithPasswordHandler loginWithPassword,
            RefreshTokenHandler refresh,
            RevokeRefreshTokensHandler revoke,
            RegisterUserHandler registerUser,
            ChangePasswordHandler changePassword,
            RequestPasswordResetHandler requestPasswordReset,
            ResetPasswordHandler resetPassword,
            GetRoleIdByKeyHandler getRoleIdByKey,
            ILogger<AuthController> logger)
        {
            _loginWithPassword = loginWithPassword ?? throw new ArgumentNullException(nameof(loginWithPassword));
            _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
            _revoke = revoke ?? throw new ArgumentNullException(nameof(revoke));
            _registerUser = registerUser ?? throw new ArgumentNullException(nameof(registerUser));
            _changePassword = changePassword ?? throw new ArgumentNullException(nameof(changePassword));
            _requestPasswordReset = requestPasswordReset ?? throw new ArgumentNullException(nameof(requestPasswordReset));
            _resetPassword = resetPassword ?? throw new ArgumentNullException(nameof(resetPassword));
            _getRoleIdByKey = getRoleIdByKey ?? throw new ArgumentNullException(nameof(getRoleIdByKey));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        /// <summary>
        /// Authenticates a user with email and password, returning a JWT access token and refresh token.
        /// </summary>
        /// <param name="request">Password login payload coming from the client.</param>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
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
                var authResult = result.Value;

                _logger.LogInformation(
                    "Login succeeded for user. UserId={UserId}, Email={Email}, Ip={Ip}",
                    authResult.UserId,
                    authResult.Email,
                    GetClientIp());

                var tokenResponse = MapToTokenResponse(authResult);
                return Ok(tokenResponse);
            }

            _logger.LogWarning(
                "Login failed. Email={Email}, Ip={Ip}, Error={Error}",
                dto.Email,
                GetClientIp(),
                result.Error ?? "Unknown error");

            return ProblemFromResult(result);
        }



        /// <summary>
        /// Issues a new token pair using a valid refresh token.
        /// </summary>
        /// <param name="request">Refresh token payload.</param>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-refresh")]
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
            var tokenSuffix = GetRefreshTokenSuffix(dto.RefreshToken);

            if (result.Succeeded && result.Value is not null)
            {
                var authResult = result.Value;

                _logger.LogInformation(
                    "Token refresh succeeded. UserId={UserId}, Email={Email}, Ip={Ip}, TokenSuffix={TokenSuffix}",
                    authResult.UserId,
                    authResult.Email,
                    GetClientIp(),
                    tokenSuffix);

                var tokenResponse = MapToTokenResponse(authResult);
                return Ok(tokenResponse);
            }

            _logger.LogWarning(
                "Token refresh failed. Ip={Ip}, TokenSuffix={TokenSuffix}, Error={Error}",
                GetClientIp(),
                tokenSuffix,
                result.Error ?? "Unknown error");

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

            var userId = GetUserIdFromClaims(HttpContext.User);
            var tokenSuffix = GetRefreshTokenSuffix(dto.RefreshToken);

            if (result.Succeeded)
            {
                _logger.LogInformation(
                    "Logout succeeded. UserId={UserId}, Ip={Ip}, TokenSuffix={TokenSuffix}",
                    FormatUserId(userId),
                    GetClientIp(),
                    tokenSuffix);

                return Ok();
            }

            _logger.LogWarning(
                "Logout failed. UserId={UserId}, Ip={Ip}, TokenSuffix={TokenSuffix}, Error={Error}",
                FormatUserId(userId),
                GetClientIp(),
                tokenSuffix,
                result.Error ?? "Unknown error");

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
                var problem = new Darwin.Contracts.Common.ProblemDetails
                {
                    Status = 401,
                    Title = "Unauthorized",
                    Detail = "User identifier could not be resolved from the access token.",
                    Instance = HttpContext.Request?.Path.Value
                };

                _logger.LogWarning(
                    "Logout-all rejected because user id could not be resolved. Ip={Ip}",
                    GetClientIp());

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
                _logger.LogInformation(
                    "Logout-all succeeded. UserId={UserId}, Ip={Ip}",
                    FormatUserId(userId),
                    GetClientIp());

                return Ok();
            }

            _logger.LogWarning(
                "Logout-all failed. UserId={UserId}, Ip={Ip}, Error={Error}",
                FormatUserId(userId),
                GetClientIp(),
                result.Error ?? "Unknown error");

            return ProblemFromResult(result);
        }


        /// <summary>
        /// Registers a new consumer account. Only available for consumer apps; business accounts are provisioned separately.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterAsync(
            [FromBody] RegisterRequest request,
            CancellationToken ct)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var dto = new UserCreateDto
            {
                Email = request.Email ?? string.Empty,
                Password = request.Password ?? string.Empty,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                PhoneE164 = null,
                //Locale = null,
                //Timezone = null,
                //Currency = null,
                IsActive = true,
                IsSystem = false
            };

            Guid? defaultRoleId = null;
            try
            {
                var roleResult = await _getRoleIdByKey.HandleAsync("Members", ct).ConfigureAwait(false);
                if (roleResult.Succeeded)
                {
                    defaultRoleId = roleResult.Value;
                }
                else
                {
                    _logger.LogWarning(
                        "Default role not found for key 'Members'. New users will be created without a default role. Error={Error}",
                        roleResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving default role for registration.");
            }

            var result = await _registerUser.HandleAsync(dto, defaultRoleId, ct).ConfigureAwait(false);
            if (result.Succeeded && result.Value != Guid.Empty)
            {
                var response = new RegisterResponse
                {
                    DisplayName = $"{dto.FirstName} {dto.LastName}".Trim(),
                    ConfirmationEmailSent = false
                };
                return Ok(response);
            }

            return ProblemFromResult(result);
        }


        /// <summary>
        /// Changes the current user's password. Requires authentication.
        /// </summary>
        [HttpPost("password/change")]
        [Authorize]
        public async Task<IActionResult> ChangePasswordAsync(
            [FromBody] ChangePasswordRequest request,
            CancellationToken ct)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var userId = GetUserIdFromClaims(HttpContext.User);
            if (userId is null)
            {
                var problem = new ProblemDetails
                {
                    Status = 401,
                    Title = "Unauthorized",
                    Detail = "User identifier could not be resolved from the access token.",
                    Instance = HttpContext.Request?.Path.Value
                };
                _logger.LogWarning(
                    "Change password rejected because user id could not be resolved. Ip={Ip}",
                    GetClientIp());
                return StatusCode(problem.Status ?? 400, problem);
            }

            var dto = new UserChangePasswordDto
            {
                Id = userId.Value,
                CurrentPassword = request.CurrentPassword ?? string.Empty,
                NewPassword = request.NewPassword ?? string.Empty
            };

            var result = await _changePassword.HandleAsync(dto, ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                return Ok();
            }

            return ProblemFromResult(result);
        }


        /// <summary>
        /// Initiates a password reset by generating a token and sending notification (email/SMS). Always returns 200/OK to prevent user enumeration.
        /// </summary>
        [HttpPost("password/request-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordResetAsync(
            [FromBody] RequestPasswordResetRequest request,
            CancellationToken ct)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var dto = new RequestPasswordResetDto
            {
                Email = request.Email ?? string.Empty
            };

            try
            {
                await _requestPasswordReset.HandleAsync(dto, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset request for email {Email}", dto.Email);
            }
            return Ok();
        }



        /// <summary>
        /// Completes a password reset using the provided email, token, and new password.
        /// </summary>
        [HttpPost("password/reset")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordAsync(
            [FromBody] ResetPasswordRequest request,
            CancellationToken ct)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var dto = new ResetPasswordDto
            {
                Email = request.Email ?? string.Empty,
                Token = request.Token ?? string.Empty,
                NewPassword = request.NewPassword ?? string.Empty
            };

            var result = await _resetPassword.HandleAsync(dto, ct).ConfigureAwait(false);
            if (result.Succeeded)
            {
                return Ok();
            }
            return ProblemFromResult(result);
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

            return StatusCode(problem.Status != 0 ? problem.Status : 400, problem);
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
        /// Resolves the remote IP address of the current HTTP request in a null-safe way.
        /// </summary>
        /// <returns>Remote IP address string, or "unknown" when not available.</returns>
        private string GetClientIp()
        {
            var context = HttpContext;
            var remoteIp = context?.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrWhiteSpace(remoteIp) ? "unknown" : remoteIp;
        }

        /// <summary>
        /// Formats the given user identifier as a string for logging purposes.
        /// </summary>
        /// <param name="userId">User identifier value.</param>
        /// <returns>String representation or "unknown" when not set.</returns>
        private static string FormatUserId(Guid? userId)
        {
            return userId.HasValue ? userId.Value.ToString() : "unknown";
        }

        /// <summary>
        /// Produces a safe suffix representation of a refresh token for logging and correlation.
        /// The full token is never written to logs.
        /// </summary>
        /// <param name="token">Original refresh token value.</param>
        /// <returns>Suffix of the token or "null" when the input is not provided.</returns>
        private static string GetRefreshTokenSuffix(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "null";
            }

            var trimmed = token.Trim();
            if (trimmed.Length <= 6)
            {
                return trimmed;
            }

            return trimmed[^6..];
        }

    }
}
