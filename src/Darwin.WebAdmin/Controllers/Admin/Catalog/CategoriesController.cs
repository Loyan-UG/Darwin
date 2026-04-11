using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Settings.Queries;
using Darwin.WebAdmin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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


        public CategoriesController(
            CreateCategoryHandler create,
            UpdateCategoryHandler update,
            GetCategoriesPageHandler list,
            GetCategoryOpsSummaryHandler getCategoryOpsSummary,
            GetCategoryForEditHandler getForEdit,
            GetCatalogLookupsHandler getLookups,
            GetCulturesHandler getCultures,
            SoftDeleteCategoryHandler softDelete)
        {
            _create = create;
            _update = update;
            _list = list;
            _getCategoryOpsSummary = getCategoryOpsSummary;
            _getForEdit = getForEdit;
            _getLookups = getLookups;
            _getCultures = getCultures;
            _softDelete = softDelete;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)
        {
            var (items, total) = await _list.HandleAsync(page, pageSize, "de-DE", query, filter, ct);
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
            await LoadLookupsAsync(ct);
            var vm = new CategoryCreateVm();
            vm.Translations ??= new();
            if (vm.Translations.Count == 0) vm.Translations.Add(new CategoryTranslationVm { Culture = "de-DE" });
            return RenderCreateEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CategoryCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), T("CategoryAtLeastOneTranslationRequired"));

            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                EnsureCreateTranslations(vm);
                return RenderCreateEditor(vm);
            }

            var dto = new CategoryCreateDto
            {
                ParentId = vm.ParentId,
                SortOrder = vm.SortOrder,
                IsActive = vm.IsActive,
                Translations = vm.Translations.Select(t => new CategoryTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description
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

                await LoadLookupsAsync(ct);
                EnsureCreateTranslations(vm);
                return RenderCreateEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
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
                    Description = t.Description
                }).ToList()
            };

            await LoadLookupsAsync(ct);
            return RenderEditEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(CategoryEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();

            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                EnsureEditTranslations(vm);
                return RenderEditEditor(vm);
            }

            var dto = new CategoryEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                ParentId = vm.ParentId,
                SortOrder = vm.SortOrder,
                IsActive = vm.IsActive,
                Translations = vm.Translations.Select(t => new CategoryTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    Description = t.Description
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
                await LoadLookupsAsync(ct);
                EnsureEditTranslations(vm);
                return RenderEditEditor(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await LoadLookupsAsync(ct);
                EnsureEditTranslations(vm);
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
        public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)
        {
            try
            {
                await _softDelete.HandleAsync(id, ct);
                SetSuccessMessage("CategoryDeleted");
            }
            catch (Exception)
            {
                SetErrorMessage("CategoryDeleteFailed");
            }

            return RedirectOrHtmx(nameof(Index), new { });
        }


        private async Task LoadLookupsAsync(CancellationToken ct)
        {
            var lookups = await _getLookups.HandleAsync("de-DE", ct);
            ViewBag.Categories = lookups.Categories;

            var (_, cultures) = await _getCultures.HandleAsync(ct);
            ViewBag.Cultures = cultures;
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

        private static void EnsureCreateTranslations(CategoryCreateVm vm)
        {
            if (vm.Translations.Count == 0)
            {
                vm.Translations.Add(new CategoryTranslationVm { Culture = "de-DE" });
            }
        }

        private static void EnsureEditTranslations(CategoryEditVm vm)
        {
            if (vm.Translations.Count == 0)
            {
                vm.Translations.Add(new CategoryTranslationVm { Culture = "de-DE" });
            }
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


