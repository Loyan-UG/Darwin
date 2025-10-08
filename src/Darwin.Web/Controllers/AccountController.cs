using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Auth.Commands;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Shared.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Controllers
{
    /// <summary>
    /// Handles user authentication flows: classic email/password sign-in,
    /// two-factor verification (TOTP), and WebAuthn (passkey) login.
    /// Also exposes self-service registration for regular customers.
    /// Cookie issuance is performed here (Web layer) using the data returned
    /// by Application handlers (UserId, SecurityStamp).
    /// </summary>
    [AllowAnonymous]
    public sealed class AccountController : Controller
    {
        private readonly SignInHandler _signIn;
        private readonly RegisterUserHandler _register;
        private readonly VerifyTotpForLoginHandler _verifyTotp;
        private readonly BeginLoginHandler _webauthnBegin;
        private readonly FinishLoginHandler _webauthnFinish;

        /// <summary>
        /// Creates a new instance of the controller.
        /// </summary>
        public AccountController(
            SignInHandler signIn,
            RegisterUserHandler register,
            VerifyTotpForLoginHandler verifyTotp,
            BeginLoginHandler webauthnBegin,
            FinishLoginHandler webauthnFinish)
        {
            _signIn = signIn;
            _register = register;
            _verifyTotp = verifyTotp;
            _webauthnBegin = webauthnBegin;
            _webauthnFinish = webauthnFinish;
        }

        /// <summary>
        /// Renders the login page.
        /// </summary>
        [HttpGet("/account/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Processes email/password login. If user requires 2FA, redirects to TOTP verification screen.
        /// Otherwise issues the auth cookie and redirects to returnUrl or home.
        /// </summary>
        [HttpPost("/account/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginPost([FromForm] string email, [FromForm] string password, [FromForm] bool rememberMe = false, [FromForm] string? returnUrl = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Login");
            }

            SignInDto dto = new SignInDto { Email = email.Trim(), Password = password, RememberMe = rememberMe };
            var result = await _signIn.HandleAsync(dto, ct); // Properties defined in your Application DTOs. :contentReference[oaicite:5]{index=5}

            if (!result.Succeeded)
            {
                if (result.RequiresTwoFactor && result.UserId.HasValue)
                {
                    // Persist the 2FA pending userId in TempData (one-time). We avoid storing secrets here.
                    TempData["2fa_user"] = result.UserId.Value.ToString();
                    TempData["remember"] = rememberMe ? "1" : "0";
                    TempData["return"] = returnUrl ?? string.Empty;
                    return RedirectToAction(nameof(LoginTwoFactor));
                }

                ModelState.AddModelError(string.Empty, result.FailureReason ?? "Invalid credentials.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Login");
            }

            // Successful sign-in: issue auth cookie
            if (!result.UserId.HasValue || string.IsNullOrWhiteSpace(result.SecurityStamp))
            {
                ModelState.AddModelError(string.Empty, "Unexpected login result.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Login");
            }

            await IssueCookieAsync(result.UserId.Value, result.SecurityStamp!, rememberMe, ct);
            return Redirect(SafeReturnUrl(returnUrl));
        }

        /// <summary>
        /// Renders the TOTP verification form during a 2FA-required login.
        /// </summary>
        [HttpGet("/account/login-2fa")]
        public IActionResult LoginTwoFactor()
        {
            if (!TempData.TryGetValue("2fa_user", out var idObj) || idObj is null) return RedirectToAction(nameof(Login));
            ViewData["RememberMe"] = TempData.TryGetValue("remember", out var r) && (string?)r == "1";
            ViewData["ReturnUrl"] = (string?)TempData["return"] ?? string.Empty;

            // Hold values again for POST (TempData is single-read). We'll re-store them in ViewData->hidden fields.
            ViewData["TwoFaUserId"] = idObj.ToString();
            return View();
        }

        /// <summary>
        /// Verifies the TOTP code and completes the sign-in by issuing the auth cookie.
        /// </summary>
        [HttpPost("/account/login-2fa")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginTwoFactorPost([FromForm] string userId, [FromForm] string code, [FromForm] bool rememberMe = false, [FromForm] string? returnUrl = null, CancellationToken ct = default)
        {
            if (!Guid.TryParse(userId, out var uid))
            {
                ModelState.AddModelError(string.Empty, "Invalid 2FA flow.");
                return View("LoginTwoFactor");
            }
            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(string.Empty, "Code is required.");
                return View("LoginTwoFactor");
            }

            var verify = await _verifyTotp.HandleAsync(new TotpVerifyDto { UserId = uid, Code = code }, ct); // :contentReference[oaicite:6]{index=6}
            if (!verify.Succeeded)
            {
                ModelState.AddModelError(string.Empty, verify.Error ?? "Invalid code.");
                return View("LoginTwoFactor");
            }

            // After successful 2FA, we need a fresh security stamp + cookie.
            // In your SignIn pipeline, stamp is part of SignInResult; for 2FA we can fetch it by a minimal SignInResultDto-like call
            // or, if Verify handler returns it, use that. For now we require a fresh sign-in result:
            var reSign = await _signIn.HandleAsync(new SignInDto { Email = string.Empty, Password = string.Empty, RememberMe = rememberMe, UserIdOverride = uid }, ct);
            if (!reSign.Succeeded || !reSign.UserId.HasValue || string.IsNullOrWhiteSpace(reSign.SecurityStamp))
            {
                ModelState.AddModelError(string.Empty, "Sign-in failed after 2FA.");
                return View("LoginTwoFactor");
            }

            await IssueCookieAsync(reSign.UserId.Value, reSign.SecurityStamp!, rememberMe, ct);
            return Redirect(SafeReturnUrl(returnUrl));
        }

        /// <summary>
        /// Starts a WebAuthn (passkey) login ceremony. Returns the browser options JSON.
        /// </summary>
        [HttpPost("/account/webauthn/begin-login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WebAuthnBeginLogin([FromForm] Guid userId, CancellationToken ct = default)
        {
            var res = await _webauthnBegin.HandleAsync(new WebAuthnBeginLoginDto { UserId = userId }, ct);
            if (!res.Succeeded) return BadRequest(new { error = res.Error ?? "Failed to begin passkey login." });
            return Json(new { challengeTokenId = res.Value!.ChallengeTokenId, options = res.Value.OptionsJson });
        }

        /// <summary>
        /// Finishes a WebAuthn (passkey) login. On success, issues auth cookie.
        /// </summary>
        [HttpPost("/account/webauthn/finish-login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WebAuthnFinishLogin([FromForm] Guid challengeTokenId, [FromForm] string assertionResponseJson, [FromForm] bool rememberMe = false, [FromForm] string? returnUrl = null, CancellationToken ct = default)
        {
            var res = await _webauthnFinish.HandleAsync(new WebAuthnFinishLoginDto
            {
                ChallengeTokenId = challengeTokenId,
                AssertionResponseJson = assertionResponseJson
            }, ct);

            if (!res.Succeeded)
                return BadRequest(new { error = res.Error ?? "Passkey login failed." });

            if (!res.Value!.UserId.HasValue || string.IsNullOrWhiteSpace(res.Value.SecurityStamp))
                return BadRequest(new { error = "Invalid login result." });

            await IssueCookieAsync(res.Value.UserId!.Value, res.Value.SecurityStamp!, rememberMe, ct);
            return Json(new { redirect = SafeReturnUrl(returnUrl) });
        }

        /// <summary>
        /// Renders the registration page for regular customers.
        /// </summary>
        [HttpGet("/account/register")]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Creates a new user using RegisterUserHandler and signs them in on success.
        /// </summary>
        [HttpPost("/account/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterPost([FromForm] string email, [FromForm] string password, [FromForm] string locale = "de-DE", [FromForm] string timezone = "Europe/Berlin", [FromForm] string currency = "EUR", [FromForm] string? returnUrl = null, CancellationToken ct = default)
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

            var result = await _register.HandleAsync(create, defaultRoleId: null, ct);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("Register");
            }

            // After registration, sign in
            var sign = await _signIn.HandleAsync(new SignInDto { Email = email.Trim(), Password = password, RememberMe = true }, ct);
            if (sign.Succeeded && sign.UserId.HasValue && !string.IsNullOrWhiteSpace(sign.SecurityStamp))
            {
                await IssueCookieAsync(sign.UserId.Value, sign.SecurityStamp!, true, ct);
            }

            return Redirect(SafeReturnUrl(returnUrl));
        }

        /// <summary>
        /// Signs the current user out by removing the authentication cookie.
        /// </summary>
        [Authorize]
        [HttpPost("/account/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(CancellationToken ct = default)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("~/");
        }

        /// <summary>
        /// Helper to issue the authentication cookie with standard claims.
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
        /// Normalizes a return URL to avoid open-redirects. Defaults to home.
        /// </summary>
        private static string SafeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl)) return "~/";
            if (Uri.TryCreate(returnUrl, UriKind.Relative, out _)) return returnUrl;
            return "~/";
        }
    }
}
