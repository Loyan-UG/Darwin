using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries; // <-- GetCatalogLookupsHandler
using Darwin.Application.Settings.Queries;
using Darwin.Web.Areas.Admin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Catalog
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
    [Area("Admin")]
    public sealed class CategoriesController : AdminBaseController
    {
        private readonly CreateCategoryHandler _create;
        private readonly UpdateCategoryHandler _update;
        private readonly GetCategoriesPageHandler _list;
        private readonly GetCategoryForEditHandler _getForEdit;
        private readonly GetCatalogLookupsHandler _getLookups;
        private readonly GetCulturesHandler _getCultures;

        public CategoriesController(
            CreateCategoryHandler create,
            UpdateCategoryHandler update,
            GetCategoriesPageHandler list,
            GetCategoryForEditHandler getForEdit,
            GetCatalogLookupsHandler getLookups,
            GetCulturesHandler getCultures)
        {
            _create = create;
            _update = update;
            _list = list;
            _getForEdit = getForEdit;
            _getLookups = getLookups;
            _getCultures = getCultures;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _list.HandleAsync(page, pageSize, "de-DE", ct);
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadLookupsAsync(ct);
            var vm = new CategoryCreateVm();
            vm.Translations ??= new();
            if (vm.Translations.Count == 0) vm.Translations.Add(new CategoryTranslationVm { Culture = "de-DE" });
            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CategoryCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), "At least one translation is required.");

            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                if (vm.Translations.Count == 0) vm.Translations.Add(new CategoryTranslationVm { Culture = "de-DE" });
                return View(vm);
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
                TempData["Success"] = "Category created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await LoadLookupsAsync(ct);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var dto = await _getForEdit.HandleAsync(id, ct);
            if (dto == null) return NotFound();

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
            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(CategoryEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();

            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                return View(vm);
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
                TempData["Success"] = "Category updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Concurrency conflict: the record was modified by another user.");
                await LoadLookupsAsync(ct);
                return View(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await LoadLookupsAsync(ct);
                return View(vm);
            }
        }

        private async Task LoadLookupsAsync(CancellationToken ct)
        {
            var lookups = await _getLookups.HandleAsync("de-DE", ct);
            ViewBag.Categories = lookups.Categories;

            var (_, cultures) = await _getCultures.HandleAsync(ct);
            ViewBag.Cultures = cultures;
        }
    }
}
