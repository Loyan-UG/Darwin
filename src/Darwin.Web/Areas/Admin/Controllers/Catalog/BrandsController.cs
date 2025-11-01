using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Shared.Results;
using Darwin.Web.Areas.Admin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Catalog
{
    /// <summary>
    /// Admin controller for managing Brands.
    /// Uses paging on Index, form pages for Create/Edit, and a shared delete confirmation modal.
    /// All messages surface via TempData and the shared Alerts partial.
    /// </summary>
    [Area("Admin")]
    public sealed class BrandsController : AdminBaseController
    {
        private readonly GetBrandsPageHandler _getPage;
        private readonly GetBrandForEditHandler _getForEdit;
        private readonly CreateBrandHandler _create;
        private readonly UpdateBrandHandler _update;
        private readonly SoftDeleteBrandHandler _softDelete;

        public BrandsController(
            GetBrandsPageHandler getPage,
            GetBrandForEditHandler getForEdit,
            CreateBrandHandler create,
            UpdateBrandHandler update,
            SoftDeleteBrandHandler softDelete)
        {
            _getPage = getPage;
            _getForEdit = getForEdit;
            _create = create;
            _update = update;
            _softDelete = softDelete;
        }

        /// <summary>
        /// Paged list of brands with a simple query (by name/slug if supported at query side).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var (items, total) = await _getPage.HandleAsync(page, pageSize, "de-DE", ct);

            var vm = new BrandsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Items = items.Select(b => new BrandListItemVm
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    ModifiedAtUtc = b.ModifiedAtUtc,
                    RowVersion = b.RowVersion
                }).ToList(),
                // Standard page-size choices for the pager
                PageSizeItems =
                    [
                        new SelectListItem("10",  "10",  pageSize == 10),
                        new SelectListItem("20",  "20",  pageSize == 20),
                        new SelectListItem("50",  "50",  pageSize == 50),
                        new SelectListItem("100", "100", pageSize == 100),
                    ]
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create() => View(new BrandEditVm
        {
            // Start with one translation row for convenience
            Translations = { new BrandTranslationVm { Culture = "de-DE" } }
        });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new BrandCreateDto
            {
                Slug = string.IsNullOrWhiteSpace(vm.Slug) ? null : vm.Slug.Trim(),
                LogoMediaId = vm.LogoMediaId,
                Translations = vm.Translations.Select(t => new BrandTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    DescriptionHtml = t.DescriptionHtml
                }).ToList()
            };

            try
            {
                await _create.HandleAsync(dto, ct);
                TempData["Success"] = "Brand created.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Application throws FluentValidation or returns friendly messages upstream.
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                TempData["Error"] = "Brand not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new BrandEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Slug = dto.Slug,
                LogoMediaId = dto.LogoMediaId,
                Translations = dto.Translations.Select(t => new BrandTranslationVm
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    DescriptionHtml = t.DescriptionHtml
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BrandEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new BrandEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Slug = string.IsNullOrWhiteSpace(vm.Slug) ? null : vm.Slug.Trim(),
                LogoMediaId = vm.LogoMediaId,
                Translations = vm.Translations.Select(t => new BrandTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    DescriptionHtml = t.DescriptionHtml
                }).ToList()
            };

            try
            {
                await _update.HandleAsync(dto, ct);
                TempData["Success"] = "Brand updated.";
                return RedirectToAction(nameof(Index), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. The brand was modified by another process. Please reload and try again.";
                return RedirectToAction(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
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
                result.Succeeded ? "Brand deleted." : (result.Error ?? "Failed to delete brand.");

            return RedirectToAction(nameof(Index));
        }
    }
}
