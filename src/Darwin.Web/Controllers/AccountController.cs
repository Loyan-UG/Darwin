using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Auth.Commands;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Controllers
{
    /// <summary>
    /// Handles authentication flows (email/password, TOTP 2FA, and WebAuthn) plus self-service registration.
    /// Cookie issuance happens here. Post-login navigation is decided by effective permissions.
    /// </summary>
    [AllowAnonymous]
    public sealed class AccountController : Controller
    {
        private readonly SignInHandler _signIn;
        private readonly RegisterUserHandler _register;
        private readonly VerifyTotpForLoginHandler _verifyTotp;
        private readonly BeginLoginHandler _webauthnBegin;
        private readonly FinishLoginHandler _webauthnFinish;
        private readonly GetSecurityStampHandler _getSecurityStamp;
        private readonly GetRoleIdByKeyHandler _getRoleIdByKey;
        private readonly IPermissionService _permissions;

        /// <summary>
        /// Initializes the controller with required Application services.
        /// </summary>
        public AccountController(
            SignInHandler signIn,
            RegisterUserHandler register,
            VerifyTotpForLoginHandler verifyTotp,
            BeginLoginHandler webauthnBegin,
            FinishLoginHandler webauthnFinish,
            GetSecurityStampHandler getSecurityStamp,
            GetRoleIdByKeyHandler getRoleIdByKey,
            IPermissionService permissions)
        {
            _signIn = signIn;
            _register = register;
            _verifyTotp = verifyTotp;
            _webauthnBegin = webauthnBegin;
            _webauthnFinish = webauthnFinish;
            _getSecurityStamp = getSecurityStamp;
            _getRoleIdByKey = getRoleIdByKey;
            _permissions = permissions;
        }

        /// <summary>Renders the login page.</summary>
        [HttpGet("/account/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Processes email/password login. If 2FA is required, redirects to the TOTP step.
        /// On success, issues the auth cookie and navigates based on effective permissions.
        /// </summary>
        [HttpPost("/account/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginPost(
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] bool rememberMe = false,
            [FromForm] string? returnUrl = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Login");
            }

            var dto = new SignInDto { Email = email.Trim(), Password = password, RememberMe = rememberMe };
            var result = await _signIn.HandleAsync(dto, ct);

            if (!result.Succeeded)
            {
                if (result.RequiresTwoFactor && result.UserId.HasValue)
                {
                    TempData["2fa_user"] = result.UserId.Value.ToString();
                    TempData["remember"] = rememberMe ? "1" : "0";
                    TempData["return"] = returnUrl ?? string.Empty;
                    return RedirectToAction(nameof(LoginTwoFactor));
                }

                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(result.FailureReason)
                    ? "Invalid credentials."
                    : result.FailureReason!);

                ViewData["ReturnUrl"] = returnUrl;
                return View("Login");
            }

            if (!result.UserId.HasValue || string.IsNullOrWhiteSpace(result.SecurityStamp))
            {
                ModelState.AddModelError(string.Empty, "Unexpected login result.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Login");
            }

            await IssueCookieAsync(result.UserId.Value, result.SecurityStamp!, rememberMe, ct);
            var dest = await DeterminePostLoginRedirectAsync(result.UserId.Value, returnUrl, ct);
            return Redirect(dest);
        }

        /// <summary>Renders the TOTP verification form during a 2FA login.</summary>
        [HttpGet("/account/login-2fa")]
        public IActionResult LoginTwoFactor()
        {
            if (!TempData.TryGetValue("2fa_user", out var idObj) || idObj is null)
                return RedirectToAction(nameof(Login));

            ViewData["RememberMe"] = TempData.TryGetValue("remember", out var r) && (string?)r == "1";
            ViewData["ReturnUrl"] = (string?)TempData["return"] ?? string.Empty;
            ViewData["TwoFaUserId"] = idObj.ToString();
            return View();
        }

        /// <summary>
        /// Verifies the TOTP code and completes the sign-in by issuing the auth cookie.
        /// </summary>
        [HttpPost("/account/login-2fa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginTwoFactorPost(
            [FromForm] string userId,
            [FromForm] int code,
            [FromForm] bool rememberMe = false,
            [FromForm] string? returnUrl = null,
            CancellationToken ct = default)
        {
            if (!Guid.TryParse(userId, out var uid))
            {
                ModelState.AddModelError(string.Empty, "Invalid 2FA flow.");
                return View("LoginTwoFactor");
            }

            var verify = await _verifyTotp.HandleAsync(new TotpVerifyDto { UserId = uid, Code = code }, ct);
            if (!verify.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(verify.Error) ? "Invalid code." : verify.Error!);
                return View("LoginTwoFactor");
            }

            var stampRes = await _getSecurityStamp.HandleAsync(uid, ct);
            if (!stampRes.Succeeded || string.IsNullOrWhiteSpace(stampRes.Value))
            {
                ModelState.AddModelError(string.Empty, "Unable to complete sign-in.");
                return View("LoginTwoFactor");
            }

            await IssueCookieAsync(uid, stampRes.Value!, rememberMe, ct);
            var dest = await DeterminePostLoginRedirectAsync(uid, returnUrl, ct);
            return Redirect(dest);
        }

        /// <summary>Begins a WebAuthn (passkey) login ceremony.</summary>
        [HttpPost("/account/webauthn/begin-login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WebAuthnBeginLogin([FromForm] Guid userId, CancellationToken ct = default)
        {
            var res = await _webauthnBegin.HandleAsync(new WebAuthnBeginLoginDto { UserId = userId }, ct);
            if (!res.Succeeded || res.Value is null)
                return BadRequest(new { error = string.IsNullOrWhiteSpace(res.Error) ? "Failed to begin passkey login." : res.Error });

            return Json(new { challengeTokenId = res.Value.ChallengeTokenId, options = res.Value.OptionsJson });
        }

        /// <summary>
        /// Finishes WebAuthn login. On success, retrieves stamp and issues the auth cookie.
        /// </summary>
        [HttpPost("/account/webauthn/finish-login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WebAuthnFinishLogin(
            [FromForm] Guid userId,
            [FromForm] Guid challengeTokenId,
            [FromForm] string clientResponseJson,
            [FromForm] bool rememberMe = false,
            [FromForm] string? returnUrl = null,
            CancellationToken ct = default)
        {
            var res = await _webauthnFinish.HandleAsync(new WebAuthnFinishLoginDto
            {
                ChallengeTokenId = challengeTokenId,
                ClientResponseJson = clientResponseJson
            }, ct);

            if (!res.Succeeded)
                return BadRequest(new { error = string.IsNullOrWhiteSpace(res.Error) ? "Passkey login failed." : res.Error });

            var stampRes = await _getSecurityStamp.HandleAsync(userId, ct);
            if (!stampRes.Succeeded || string.IsNullOrWhiteSpace(stampRes.Value))
                return BadRequest(new { error = "Unable to complete sign-in." });

            await IssueCookieAsync(userId, stampRes.Value!, rememberMe, ct);

            var dest = await DeterminePostLoginRedirectAsync(userId, returnUrl, ct);
            return Json(new { redirect = dest });
        }

        /// <summary>Renders the end-user registration page.</summary>
        [HttpGet("/account/register")]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Creates a new user, assigns default "Members" role if present, then signs the user in.
        /// </summary>
        [HttpPost("/account/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPost(
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string locale = "de-DE",
            [FromForm] string timezone = "Europe/Berlin",
            [FromForm] string currency = "EUR",
            [FromForm] string? returnUrl = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Register");
            }

            var create = new UserCreateDto
            {
                Email = email.Trim(),
                Password = password,
                Locale = string.IsNullOrWhiteSpace(locale) ? "de-DE" : locale,
                Timezone = string.IsNullOrWhiteSpace(timezone) ? "Europe/Berlin" : timezone,
                Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency
            };

            // Resolve default role "Members" by key if present.
            Guid? defaultRoleId = null;
            var roleRes = await _getRoleIdByKey.HandleAsync("Members", ct);
            if (roleRes.Succeeded) defaultRoleId = roleRes.Value;

            var result = await _register.HandleAsync(create, defaultRoleId, ct);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(result.Error) ? "Registration failed." : result.Error!);
                ViewData["ReturnUrl"] = returnUrl;
                return View("Register");
            }

            // Sign-in after registration (best-effort).
            var sign = await _signIn.HandleAsync(new SignInDto { Email = email.Trim(), Password = password, RememberMe = true }, ct);
            if (sign.Succeeded && sign.UserId.HasValue && !string.IsNullOrWhiteSpace(sign.SecurityStamp))
            {
                await IssueCookieAsync(sign.UserId.Value, sign.SecurityStamp!, true, ct);
                var dest = await DeterminePostLoginRedirectAsync(sign.UserId.Value, returnUrl, ct);
                return Redirect(dest);
            }

            return Redirect(SafeReturnUrl(returnUrl));
        }

        /// <summary>Signs the current user out and redirects to home.</summary>
        [Authorize]
        [HttpPost("/account/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(CancellationToken ct = default)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("~/");
        }

        /// <summary>
        /// Issues the authentication cookie with minimal stable claims.
        /// </summary>
        private async Task IssueCookieAsync(Guid userId, string securityStamp, bool persistent, CancellationToken ct)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("sstamp", securityStamp)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var props = new AuthenticationProperties
            {
                IsPersistent = persistent,
                AllowRefresh = true,
                ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(30) : (DateTimeOffset?)null
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);
        }

        /// <summary>
        /// Computes the safest post-login destination. If the provided returnUrl is a safe relative URL, it wins.
        /// Otherwise, use permission-based defaults: Admin area for "AccessAdminPanel",
        /// Member area for "AccessMemberArea", or site root as the final fallback.
        /// </summary>
        private async Task<string> DeterminePostLoginRedirectAsync(Guid userId, string? returnUrl, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Uri.TryCreate(returnUrl, UriKind.Relative, out _))
            {
                return returnUrl!;
            }

            if (await _permissions.HasAsync(userId, "AccessAdminPanel", ct))
            {
                var adminUrl = Url.Action("Index", "Home", new { area = "Admin" });
                if (!string.IsNullOrWhiteSpace(adminUrl)) return adminUrl!;
            }

            if (await _permissions.HasAsync(userId, "AccessMemberArea", ct))
            {
                var memberUrl = Url.Action("Index", "Home", new { area = "Member" });
                if (!string.IsNullOrWhiteSpace(memberUrl)) return memberUrl!;
            }

            return "~/";
        }

        /// <summary>
        /// Normalizes an arbitrary return URL to a safe relative path. Used only in legacy flows.
        /// </summary>
        private static string SafeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl)) return "~/";
            return Uri.TryCreate(returnUrl, UriKind.Relative, out _) ? returnUrl : "~/";
        }
    }
}
