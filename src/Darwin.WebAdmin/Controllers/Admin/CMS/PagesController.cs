using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Application.Settings.Queries;
using Darwin.WebAdmin.ViewModels.CMS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.CMS
{
    /// <summary>
    ///     Admin controller for CMS Pages covering full content lifecycle:
    ///     draft/publish windows, per-culture translations, and rich text content with sanitization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Features:
    ///         <list type="bullet">
    ///             <item>Quill v2 editor for content; server-side sanitization before persistence.</item>
    ///             <item>Unique slug per culture; index-level enforcement in the database.</item>
    ///             <item>Publish window fields (start/end) interpreted in UTC for unambiguous scheduling.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         UX:
    ///         Offers culture selection sourced from <c>SiteSetting</c>; displays friendly validation summaries and alerts.
    ///     </para>
    /// </remarks>
    public sealed class PagesController : AdminBaseController
    {
        private readonly CreatePageHandler _create;
        private readonly UpdatePageHandler _update;
        private readonly GetPagesPageHandler _list;
        private readonly GetPageForEditHandler _get;
        private readonly GetCulturesHandler _getCultures;
        private readonly SoftDeletePageHandler _softDeletePage;

        public PagesController(
            CreatePageHandler create,
            UpdatePageHandler update,
            GetPagesPageHandler list,
            GetPageForEditHandler get,
            GetCulturesHandler getCultures,
            SoftDeletePageHandler softDeletePage)
        {
            _create = create;
            _update = update;
            _list = list;
            _get = get;
            _getCultures = getCultures;
            _softDeletePage = softDeletePage;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var (items, total) = await _list.HandleAsync(page, pageSize, "de-DE", query, ct);
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Query = query ?? string.Empty;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadCulturesAsync(ct).ConfigureAwait(false);
            return View(new PageCreateVm());
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(PageCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), "At least one translation is required.");

            if (!ModelState.IsValid)
            {
                await LoadCulturesAsync(ct).ConfigureAwait(false);
                EnsureTranslations(vm);
                return RenderCreateEditor(vm);
            }

            var dto = new PageCreateDto
            {
                Status = vm.Status,
                PublishStartUtc = vm.PublishStartUtc,
                PublishEndUtc = vm.PublishEndUtc,
                Translations = vm.Translations.Select(t => new PageTranslationDto
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            try
            {
                await _create.HandleAsync(dto, ct);
                TempData["Success"] = "Page created successfully.";
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await LoadCulturesAsync(ct).ConfigureAwait(false);
                EnsureTranslations(vm);
                return RenderCreateEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var dto = await _get.HandleAsync(id, ct);
            if (dto == null) return NotFound();

            var vm = new PageEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Status = dto.Status,
                PublishStartUtc = dto.PublishStartUtc,
                PublishEndUtc = dto.PublishEndUtc,
                Translations = dto.Translations.Select(t => new PageTranslationVm
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            await LoadCulturesAsync(ct).ConfigureAwait(false);

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(PageEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();

            if (!ModelState.IsValid)
            {
                await LoadCulturesAsync(ct).ConfigureAwait(false);
                EnsureTranslations(vm);
                return RenderEditEditor(vm);
            }

            var dto = new PageEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Status = vm.Status,
                PublishStartUtc = vm.PublishStartUtc,
                PublishEndUtc = vm.PublishEndUtc,
                Translations = vm.Translations.Select(t => new PageTranslationDto
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            try
            {
                await _update.HandleAsync(dto, ct);
                TempData["Success"] = "Page updated successfully.";
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Concurrency conflict: the record was modified by another user.");
                await LoadCulturesAsync(ct).ConfigureAwait(false);
                EnsureTranslations(vm);
                return RenderEditEditor(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await LoadCulturesAsync(ct).ConfigureAwait(false);
                EnsureTranslations(vm);
                return RenderEditEditor(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _softDeletePage.HandleAsync(id, rowVersion, ct);
                TempData["Success"] = "Page deleted.";
            }
            catch
            {
                TempData["Error"] = "Failed to delete the page.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCulturesAsync(CancellationToken ct)
        {
            var (_, cultures) = await _getCultures.HandleAsync(ct).ConfigureAwait(false);
            ViewBag.Cultures = cultures;
        }

        private IActionResult RenderCreateEditor(PageCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Pages/_PageCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderEditEditor(PageEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Pages/_PageEditEditorShell.cshtml", vm);
            }

            return View("Edit", vm);
        }

        private IActionResult RedirectOrHtmx(string actionName, object routeValues)
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(actionName, routeValues) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(actionName, routeValues);
        }

        private bool IsHtmxRequest()
        {
            return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureTranslations(PageEditorVm vm)
        {
            if (vm.Translations.Count == 0)
            {
                vm.Translations.Add(new PageTranslationVm());
            }
        }
    }
}
