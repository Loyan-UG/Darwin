using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Settings.Queries;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebAdmin.Controllers.Admin.Catalog
{
    /// <summary>
    ///     Admin controller for category management with support for hierarchical relationships,
    ///     per-culture translations, and SEO-friendly slugs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Responsibilities:
    ///         <list type="bullet">
    ///             <item>List categories for browsing and editing.</item>
    ///             <item>Create/edit with validation for required translations and unique slugs per culture.</item>
    ///             <item>Provide lookup data for parent category selection.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Notes:
    ///         Keep forms small and responsive; for large trees consider async lookups or virtualized lists in the future.
    ///     </para>
    /// </remarks>
    public sealed class CategoriesController : AdminBaseController
    {
        private readonly CreateCategoryHandler _create;
        private readonly UpdateCategoryHandler _update;
        private readonly GetCategoriesPageHandler _list;
        private readonly GetCategoryOpsSummaryHandler _getCategoryOpsSummary;
        private readonly GetCategoryForEditHandler _getForEdit;
        private readonly GetCatalogLookupsHandler _getLookups;
        private readonly GetCulturesHandler _getCultures;
        private readonly SoftDeleteCategoryHandler _softDelete;
        private readonly ISiteSettingCache _siteSettingCache;


        public CategoriesController(
            CreateCategoryHandler create,
            UpdateCategoryHandler update,
            GetCategoriesPageHandler list,
            GetCategoryOpsSummaryHandler getCategoryOpsSummary,
            GetCategoryForEditHandler getForEdit,
            GetCatalogLookupsHandler getLookups,
            GetCulturesHandler getCultures,
            SoftDeleteCategoryHandler softDelete,
            ISiteSettingCache siteSettingCache)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _getCategoryOpsSummary = getCategoryOpsSummary ?? throw new ArgumentNullException(nameof(getCategoryOpsSummary));
            _getForEdit = getForEdit ?? throw new ArgumentNullException(nameof(getForEdit));
            _getLookups = getLookups ?? throw new ArgumentNullException(nameof(getLookups));
            _getCultures = getCultures ?? throw new ArgumentNullException(nameof(getCultures));
            _softDelete = softDelete ?? throw new ArgumentNullException(nameof(softDelete));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)
        {
            var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;
            var (items, total) = await _list.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);
            var summary = await _getCategoryOpsSummary.HandleAsync(ct);

            var vm = new CategoriesIndexVm
            {
                Items = items,
                Query = query ?? string.Empty,
                Filter = filter ?? string.Empty,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Summary = new CategoryOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    InactiveCount = summary.InactiveCount,
                    UnpublishedCount = summary.UnpublishedCount,
                    RootCount = summary.RootCount,
                    ChildCount = summary.ChildCount
                },
                Playbooks = BuildCategoryPlaybooks()
            };

            return RenderIndexWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var vm = new CategoryCreateVm();
            vm.Translations ??= new();
            await EnsureCreateTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderCreateEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CategoryCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), T("CategoryAtLeastOneTranslationRequired"));

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), T("CategoryAtLeastOneTranslationRequired"));

            if (!ModelState.IsValid)
            {
                await EnsureCreateTranslationsAsync(vm, ct);
                return RenderCreateEditor(vm);
            }

            var dto = new CategoryCreateDto
            {
                ParentId = vm.ParentId,
                SortOrder = vm.SortOrder,
                IsActive = vm.IsActive,
                Translations = translations.Select(t => new CategoryTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription
                }).ToList()
            };

            try
            {
                await _create.HandleAsync(dto, ct);
                SetSuccessMessage("CategoryCreated");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await EnsureCreateTranslationsAsync(vm, ct);
                return RenderCreateEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("CategoryNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getForEdit.HandleAsync(id, ct);
            if (dto == null)
            {
                SetErrorMessage("CategoryNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new CategoryEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                ParentId = dto.ParentId,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                Translations = dto.Translations.Select(t => new CategoryTranslationVm
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription
                }).ToList()
            };

            await EnsureEditTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(CategoryEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();

            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("CategoryNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await EnsureEditTranslationsAsync(vm, ct);
                return RenderEditEditor(vm);
            }

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.Translations), T("CategoryAtLeastOneTranslationRequired"));
                await EnsureEditTranslationsAsync(vm, ct);
                return RenderEditEditor(vm);
            }

            var dto = new CategoryEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                ParentId = vm.ParentId,
                SortOrder = vm.SortOrder,
                IsActive = vm.IsActive,
                Translations = translations.Select(t => new CategoryTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription
                }).ToList()
            };

            try
            {
                await _update.HandleAsync(dto, ct);
                SetSuccessMessage("CategoryUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, T("CategoryConcurrencyConflict"));
                await EnsureEditTranslationsAsync(vm, ct);
                return RenderEditEditor(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await EnsureEditTranslationsAsync(vm, ct);
                return RenderEditEditor(vm);
            }
        }

        /// <summary>
        /// Performs a soft delete of a category and redirects back to the list.
        /// This action expects a confirmation via the shared modal and uses TempData
        /// to display the outcome message. The underlying handler performs a soft
        /// delete (IsDeleted=true) to preserve auditability and prevent hard data loss.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("CategoryDeleteFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _softDelete.HandleAsync(id, DecodeBase64RowVersion(rowVersion), ct);
            if (result.Succeeded)
            {
                SetSuccessMessage("CategoryDeleted");
            }
            else
            {
                SetErrorMessage("CategoryDeleteFailed");
            }

            return RedirectOrHtmx(nameof(Index), new { });
        }


        private async Task PopulateCategoryLookupsAsync(CategoryCreateVm vm, CancellationToken ct)
        {
            var (options, cultures) = await BuildCategoryEditorLookupsAsync(ct).ConfigureAwait(false);
            vm.ParentCategoryOptions = options;
            vm.Cultures = cultures;
        }

        private async Task PopulateCategoryLookupsAsync(CategoryEditVm vm, CancellationToken ct)
        {
            var (options, cultures) = await BuildCategoryEditorLookupsAsync(ct).ConfigureAwait(false);
            vm.ParentCategoryOptions = options;
            vm.Cultures = cultures;
        }

        private async Task<(List<SelectListItem> ParentCategoryOptions, IReadOnlyList<string> Cultures)> BuildCategoryEditorLookupsAsync(CancellationToken ct)
        {
            var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;
            var lookups = await _getLookups.HandleAsync(defaultCulture, ct);

            var parentCategoryOptions = lookups.Categories.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name
            }).ToList();

            var (_, cultures) = await _getCultures.HandleAsync(ct).ConfigureAwait(false);
            return (parentCategoryOptions, cultures);
        }

        private IActionResult RenderIndexWorkspace(CategoriesIndexVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Categories/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderCreateEditor(CategoryCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Categories/_CategoryCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderEditEditor(CategoryEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Categories/_CategoryEditEditorShell.cshtml", vm);
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

        private async Task EnsureCreateTranslationsAsync(CategoryCreateVm vm, CancellationToken ct)
        {
            await PopulateCategoryLookupsAsync(vm, ct).ConfigureAwait(false);
            vm.MultilingualEnabled = await IsMultilingualEnabledAsync(ct).ConfigureAwait(false);
            await EnsureTranslationsAsync(vm.Translations, vm.MultilingualEnabled, ct).ConfigureAwait(false);
        }

        private async Task EnsureEditTranslationsAsync(CategoryEditVm vm, CancellationToken ct)
        {
            await PopulateCategoryLookupsAsync(vm, ct).ConfigureAwait(false);
            vm.MultilingualEnabled = await IsMultilingualEnabledAsync(ct).ConfigureAwait(false);
            await EnsureTranslationsAsync(vm.Translations, vm.MultilingualEnabled, ct).ConfigureAwait(false);
        }

        private async Task EnsureTranslationsAsync(List<CategoryTranslationVm> translations, bool multilingualEnabled, CancellationToken ct)
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

            if (!multilingualEnabled)
            {
                orderedCultures = [defaultCulture];
                translations.RemoveAll(x => !string.Equals(x.Culture, defaultCulture, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var culture in orderedCultures)
            {
                if (translations.All(x => !string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
                {
                    translations.Add(new CategoryTranslationVm { Culture = culture });
                }
            }
        }

        private async Task<bool> IsMultilingualEnabledAsync(CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            return CountCultures(settings.SupportedCulturesCsv) > 1;
        }

        private static int CountCultures(string? supportedCulturesCsv)
            => (supportedCulturesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

        private static List<CategoryTranslationVm> FilterCompleteTranslations(IEnumerable<CategoryTranslationVm> translations)
        {
            return translations
                .Where(static t =>
                    !string.IsNullOrWhiteSpace(t.Culture) &&
                    !string.IsNullOrWhiteSpace(t.Name) &&
                    !string.IsNullOrWhiteSpace(t.Slug))
                .ToList();
        }

        private OperationalPlaybookVm[] BuildCategoryPlaybooks()
        {
            return
            [
                new OperationalPlaybookVm
                {
                    QueueLabel = T("Inactive"),
                    WhyItMatters = T("CategoryPlaybookInactiveScope"),
                    OperatorAction = T("CategoryPlaybookInactiveAction")
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = T("Unpublished"),
                    WhyItMatters = T("CategoryPlaybookUnpublishedScope"),
                    OperatorAction = T("CategoryPlaybookUnpublishedAction")
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = T("ChildCategories"),
                    WhyItMatters = T("CategoryPlaybookChildScope"),
                    OperatorAction = T("CategoryPlaybookChildAction")
                }
            ];
        }
    }
}


