using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Darwin.Application.Abstractions.Services;
using Darwin.WebAdmin.Localization;

namespace Darwin.WebAdmin.Controllers
{
    public sealed class CultureController : Controller
    {
        private readonly IClock _clock;

        public CultureController(IClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetCulture(string culture, string? returnUrl = null)
        {
            var normalizedCulture = AdminCultureCatalog.NormalizeUiCulture(culture);

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(normalizedCulture)),
                new CookieOptions
                {
                    Expires = new DateTimeOffset(_clock.UtcNow, TimeSpan.Zero).AddYears(1),
                    IsEssential = true,
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
