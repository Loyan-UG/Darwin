using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Settings.Queries;
using Darwin.Shared.Results;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebAdmin.Controllers.Admin.Catalog
{
    /// <summary>
    /// Admin controller for managing Brands.
    /// Uses paging on Index, form pages for Create/Edit, and a shared delete confirmation modal.
    /// All messages surface via TempData and the shared Alerts partial.
    /// </summary>
    public sealed class BrandsController : AdminBaseController
    {
        private readonly GetBrandsPageHandler _getPage;
        private readonly GetBrandOpsSummaryHandler _getBrandOpsSummary;
        private readonly GetBrandForEditHandler _getForEdit;
        private readonly CreateBrandHandler _create;
        private readonly UpdateBrandHandler _update;
        private readonly SoftDeleteBrandHandler _softDelete;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly GetCulturesHandler _getCultures;

        public BrandsController(
            GetBrandsPageHandler getPage,
            GetBrandOpsSummaryHandler getBrandOpsSummary,
            GetBrandForEditHandler getForEdit,
            CreateBrandHandler create,
            UpdateBrandHandler update,
            SoftDeleteBrandHandler softDelete,
            ISiteSettingCache siteSettingCache,
            GetCulturesHandler getCultures)
        {
            _getPage = getPage;
            _getBrandOpsSummary = getBrandOpsSummary;
            _getForEdit = getForEdit;
            _create = create;
            _update = update;
            _softDelete = softDelete;
            _siteSettingCache = siteSettingCache;
            _getCultures = getCultures;
        }

        /// <summary>
        /// Paged list of brands with a simple query (by name/slug if supported at query side).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20,
            string? query = null, string? filter = null, CancellationToken ct = default)
        {
            var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;
            var (items, total) = await _getPage.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);
            var summary = await _getBrandOpsSummary.HandleAsync(ct);

            var vm = new BrandsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter ?? string.Empty,
                Summary = new BrandOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    UnpublishedCount = summary.UnpublishedCount,
                    MissingSlugCount = summary.MissingSlugCount,
                    MissingLogoCount = summary.MissingLogoCount
                },
                Playbooks = BuildBrandPlaybooks(),
                Items = items.Select(b => new BrandListItemVm
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    ModifiedAtUtc = b.ModifiedAtUtc,
                    RowVersion = b.RowVersion
                }).ToList()
            };

            return RenderIndexWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct = default)
        {
            var vm = new BrandEditVm();
            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderBrandEditor(vm, isCreate: true);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await EnsureTranslationsAsync(vm, ct);
                return RenderBrandEditor(vm, isCreate: true);
            }

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
            {
                AddModelErrorMessage("BrandCreateFailed");
                await EnsureTranslationsAsync(vm, ct);
                return RenderBrandEditor(vm, isCreate: true);
            }

            var dto = new BrandCreateDto
            {
                Slug = string.IsNullOrWhiteSpace(vm.Slug) ? null : vm.Slug.Trim(),
                LogoMediaId = vm.LogoMediaId,
                Translations = translations.Select(t => new BrandTranslationDto
                {
                    Id = t.Id,
                    Culture = t.Culture,
                    Name = t.Name,
                    DescriptionHtml = t.DescriptionHtml
                }).ToList()
            };

            try
            {
                await _create.HandleAsync(dto, ct);
                SetSuccessMessage("BrandCreated");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BrandCreateFailed");
                await EnsureTranslationsAsync(vm, ct);
                return RenderBrandEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetErrorMessage("BrandNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new BrandEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Slug = dto.Slug,
                LogoMediaId = dto.LogoMediaId,
                Translations = dto.Translations.Select(t => new BrandTranslationVm
                {
                    Id = t.Id,
                    Culture = t.Culture,
                    Name = t.Name,
                    DescriptionHtml = t.DescriptionHtml
                }).ToList()
            };

            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderBrandEditor(vm, isCreate: false);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await EnsureTranslationsAsync(vm, ct);
                return RenderBrandEditor(vm, isCreate: false);
            }

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
            {
                AddModelErrorMessage("BrandUpdateFailed");
                await EnsureTranslationsAsync(vm, ct);
                return RenderBrandEditor(vm, isCreate: false);
            }

            var dto = new BrandEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Slug = string.IsNullOrWhiteSpace(vm.Slug) ? null : vm.Slug.Trim(),
                LogoMediaId = vm.LogoMediaId,
                Translations = translations.Select(t => new BrandTranslationDto
                {
                    Id = t.Id,
                    Culture = t.Culture,
                    Name = t.Name,
                    DescriptionHtml = t.DescriptionHtml
                }).ToList()
            };

            try
            {
                await _update.HandleAsync(dto, ct);
                SetSuccessMessage("BrandUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BrandConcurrencyConflict");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BrandUpdateFailed");
                await EnsureTranslationsAsync(vm, ct);
                return RenderBrandEditor(vm, isCreate: false);
            }
        }

        /// <summary>
        /// Soft delete using the shared confirmation modal (Id + RowVersion).
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            var dto = new BrandDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };
            Result result = await _softDelete.HandleAsync(dto, ct);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? T("BrandDeleted") : T("BrandDeleteFailed");

            return RedirectOrHtmx(nameof(Index), new { });
        }

        private IActionResult RenderBrandEditor(BrandEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Brands/_BrandEditorShell.cshtml", vm);
            }
            return isCreate
                ? View("Create", vm)
                : View("Edit", vm);
        }

        private IActionResult RenderIndexWorkspace(BrandsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Brands/Index.cshtml", vm);
            }

            return View("Index", vm);
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

        private async Task EnsureTranslationsAsync(BrandEditVm vm, CancellationToken ct)
        {
            var (defaultCulture, cultures) = await _getCultures.HandleAsync(ct).ConfigureAwait(false);
            var orderedCultures = cultures
                .Prepend(defaultCulture)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (orderedCultures.Length == 0)
            {
                orderedCultures = [defaultCulture];
            }

            foreach (var culture in orderedCultures)
            {
                if (vm.Translations.All(x => !string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
                {
                    vm.Translations.Add(new BrandTranslationVm { Culture = culture });
                }
            }
        }

        private static List<BrandTranslationVm> FilterCompleteTranslations(IEnumerable<BrandTranslationVm> translations)
        {
            return translations
                .Where(static t =>
                    !string.IsNullOrWhiteSpace(t.Culture) &&
                    !string.IsNullOrWhiteSpace(t.Name))
                .ToList();
        }

        private OperationalPlaybookVm[] BuildBrandPlaybooks()
        {
            return
            [
                new OperationalPlaybookVm
                {
                    QueueLabel = T("Unpublished"),
                    WhyItMatters = T("BrandPlaybookUnpublishedScope"),
                    OperatorAction = T("BrandPlaybookUnpublishedAction")
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = T("MissingSlug"),
                    WhyItMatters = T("BrandPlaybookMissingSlugScope"),
                    OperatorAction = T("BrandPlaybookMissingSlugAction")
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = T("MissingLogo"),
                    WhyItMatters = T("BrandPlaybookMissingLogoScope"),
                    OperatorAction = T("BrandPlaybookMissingLogoAction")
                }
            ];
        }
    }
}

