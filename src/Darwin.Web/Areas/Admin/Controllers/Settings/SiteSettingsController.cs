using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Settings.Commands;
using Darwin.Application.Settings.DTOs;
using Darwin.Application.Settings.Queries;
using Darwin.Web.Areas.Admin.ViewModels.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Web.Areas.Admin.Controllers.Settings
{
    [Area("Admin")]
    public sealed class SiteSettingsController : Controller
    {
        private readonly GetSiteSettingHandler _get;
        private readonly UpdateSiteSettingHandler _update;

        public SiteSettingsController(GetSiteSettingHandler get, UpdateSiteSettingHandler update)
        {
            _get = get;
            _update = update;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(CancellationToken ct)
        {
            var dto = await _get.HandleAsync(ct);
            if (dto == null)
                return NotFound();

            var vm = new SiteSettingVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Title = dto.Title,
                DefaultCulture = dto.DefaultCulture,
                SupportedCulturesCsv = dto.SupportedCulturesCsv
            };
            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(SiteSettingVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var dto = new UpdateSiteSettingDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                Title = vm.Title,
                DefaultCulture = vm.DefaultCulture,
                SupportedCulturesCsv = vm.SupportedCulturesCsv
            };

            try
            {
                await _update.HandleAsync(dto, ct);
                TempData["Success"] = "Settings have been updated.";
                return RedirectToAction(nameof(Edit));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Concurrency conflict: the settings were modified by another user.");
                return View(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                return View(vm);
            }
        }
    }
}
