using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Web.Areas.Admin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Catalog
{
    [Area("Admin")]
    public sealed class CategoriesController : Controller
    {
        private readonly CreateCategoryHandler _createCategory;
        private readonly UpdateCategoryHandler _updateCategory;
        private readonly GetCategoriesPageHandler _getCategoriesPage;
        private readonly GetCategoryForEditHandler _getCategoryForEdit;
        private readonly GetCatalogLookupsHandler _getLookups;

        public CategoriesController(
            CreateCategoryHandler createCategory,
            UpdateCategoryHandler updateCategory,
            GetCategoriesPageHandler getCategoriesPage,
            GetCategoryForEditHandler getCategoryForEdit,
            GetCatalogLookupsHandler getLookups)
        {
            _createCategory = createCategory;
            _updateCategory = updateCategory;
            _getCategoriesPage = getCategoriesPage;
            _getCategoryForEdit = getCategoryForEdit;
            _getLookups = getLookups;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _getCategoriesPage.HandleAsync(page, pageSize, "de-DE", ct);
            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadLookupsAsync(ct);
            var vm = new CategoryCreateVm
            {
                Translations = { new CategoryTranslationVm { Culture = "de-DE" } }
            };
            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(CategoryCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                return View(vm);
            }

            var dto = new CategoryCreateDto
            {
                ParentId = vm.ParentId,
                IsActive = vm.IsActive,
                SortOrder = vm.SortOrder,
                Translations = vm.Translations.Select(t => new CategoryTranslationDto
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
                var id = await _createCategory.HandleAsync(dto, ct);
                TempData["Success"] = "Category created successfully.";
                return RedirectToAction(nameof(Edit), new { id });
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
            var dto = await _getCategoryForEdit.HandleAsync(id, ct);
            if (dto == null) return NotFound();

            var vm = new CategoryEditVm
            {
                Id = dto.Id,
                ParentId = dto.ParentId,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                RowVersion = dto.RowVersion,
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

            await LoadLookupsAsync(ct);
            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(CategoryEditVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                return View(vm);
            }

            var dto = new CategoryEditDto
            {
                Id = vm.Id,
                ParentId = vm.ParentId,
                IsActive = vm.IsActive,
                SortOrder = vm.SortOrder,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Translations = vm.Translations.Select(t => new CategoryTranslationDto
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
                await _updateCategory.HandleAsync(dto, ct);
                TempData["Success"] = "Category updated successfully.";
                return RedirectToAction(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Concurrency conflict: the record was modified by another user. Please reload and try again.");
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
            ViewBag.Brands = lookups.Brands;        // can be used later in product forms
            ViewBag.TaxCategories = lookups.TaxCategories;
            ViewBag.Categories = lookups.Categories;
        }
    }
}
