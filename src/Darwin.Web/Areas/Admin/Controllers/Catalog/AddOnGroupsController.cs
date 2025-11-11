using Darwin.Application.Catalog.Commands;
// Application layer
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Web.Areas.Admin.Controllers;
using Darwin.Web.Areas.Admin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Catalog
{
    /// <summary>
    /// Admin controller to manage Add-on Groups and their attachments across
    /// Variant → Product → Category → Brand → Global resolution levels.
    /// </summary>
    [Area("Admin")]
    public sealed class AddOnGroupsController : AdminBaseController
    {
        // Groups
        private readonly GetAddOnGroupsPageHandler _getPage;
        private readonly GetAddOnGroupForEditHandler _getForEdit;
        private readonly CreateAddOnGroupHandler _create;
        private readonly UpdateAddOnGroupHandler _update;
        private readonly SoftDeleteAddOnGroupHandler _softDelete;

        // Attachments
        private readonly AttachAddOnGroupToVariantsHandler _attachVariants;
        private readonly AttachAddOnGroupToProductsHandler _attachProducts;
        private readonly AttachAddOnGroupToCategoriesHandler _attachCategories;
        private readonly AttachAddOnGroupToBrandsHandler _attachBrands;

        // Lookups for attach pages
        private readonly GetProductsPageHandler _getProductsPage;
        private readonly GetCategoriesPageHandler _getCategoriesPage;
        private readonly GetBrandsPageHandler _getBrandsPage;

        private readonly GetVariantsPageHandler _getVariantsPage;

        private readonly GetAddOnGroupAttachedProductIdsHandler _getAttachedProducts;
        private readonly GetAddOnGroupAttachedVariantIdsHandler _getAttachedVariants;
        private readonly GetAddOnGroupAttachedCategoryIdsHandler _getAttachedCategories;
        private readonly GetAddOnGroupAttachedBrandIdsHandler _getAttachedBrands;

        public AddOnGroupsController(
            GetAddOnGroupsPageHandler getPage,
            GetAddOnGroupForEditHandler getForEdit,
            CreateAddOnGroupHandler create,
            UpdateAddOnGroupHandler update,
            SoftDeleteAddOnGroupHandler softDelete,
            AttachAddOnGroupToVariantsHandler attachVariants,
            AttachAddOnGroupToProductsHandler attachProducts,
            AttachAddOnGroupToCategoriesHandler attachCategories,
            AttachAddOnGroupToBrandsHandler attachBrands,
            GetProductsPageHandler getProductsPage,
            GetCategoriesPageHandler getCategoriesPage,
            GetBrandsPageHandler getBrandsPage,
            GetVariantsPageHandler getVariantsPage,
            GetAddOnGroupAttachedProductIdsHandler getAttachedProducts,
            GetAddOnGroupAttachedVariantIdsHandler getAttachedVariants,
            GetAddOnGroupAttachedCategoryIdsHandler getAttachedCategories,
            GetAddOnGroupAttachedBrandIdsHandler getAttachedBrands
            )
        {
            _getPage = getPage;
            _getForEdit = getForEdit;
            _create = create;
            _update = update;
            _softDelete = softDelete;

            _attachVariants = attachVariants;
            _attachProducts = attachProducts;
            _attachCategories = attachCategories;
            _attachBrands = attachBrands;

            _getProductsPage = getProductsPage;
            _getCategoriesPage = getCategoriesPage;
            _getBrandsPage = getBrandsPage;
            _getVariantsPage = getVariantsPage;

            _getAttachedProducts = getAttachedProducts;
            _getAttachedVariants = getAttachedVariants;
            _getAttachedCategories = getAttachedCategories;
            _getAttachedBrands = getAttachedBrands;
        }

        // ---------------- List ----------------

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            // Application paging contract mirrors Brands (items,total)
            var (items, total) = await _getPage.HandleAsync(page, pageSize, query, ct);
            var vm = new AddOnGroupsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Items = items.Select(x => new AddOnGroupListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    Currency = x.Currency,
                    IsGlobal = x.IsGlobal,
                    IsActive = x.IsActive,
                    OptionsCount = x.OptionsCount,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };
            return View(vm);
        }

        // ---------------- Create ----------------

        [HttpGet]
        public IActionResult Create()
        {
            // A simple example for user convenience (Option + a Value)
            return View(new AddOnGroupCreateVm
            {
                Options = { new AddOnOptionVm { Label = "Option", Values = { new AddOnOptionValueVm { Label = "Value" } } } }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddOnGroupCreateVm vm, CancellationToken ct = default)
        {
            vm.Options ??= new();
            foreach (var o in vm.Options) o.Values ??= new();

            if (!ModelState.IsValid) return View(vm);

            var dto = new AddOnGroupCreateDto
            {
                Name = vm.Name?.Trim() ?? string.Empty,
                Currency = vm.Currency?.Trim().ToUpperInvariant() ?? "EUR",
                IsGlobal = vm.IsGlobal,
                SelectionMode = vm.SelectionMode,
                MinSelections = vm.MinSelections,
                MaxSelections = vm.MaxSelections,
                IsActive = vm.IsActive,
                Options = vm.Options.Select(o => new AddOnOptionDto
                {
                    Label = o.Label?.Trim() ?? string.Empty,
                    SortOrder = o.SortOrder,
                    Values = o.Values.Select(v => new AddOnOptionValueDto
                    {
                        Label = v.Label?.Trim() ?? string.Empty,
                        PriceDeltaMinor = v.PriceDeltaMinor,
                        Hint = string.IsNullOrWhiteSpace(v.Hint) ? null : v.Hint.Trim(),
                        SortOrder = v.SortOrder,
                        IsActive = v.IsActive
                    }).ToList()
                }).ToList()
            };

            try
            {
                await _create.HandleAsync(dto, ct); // Application: Task (no Result)
                TempData["Success"] = "Add-on group created.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(vm);
            }
        }

        // ---------------- Edit ----------------

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct); // returns DTO directly
            var vm = new AddOnGroupEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion ?? Array.Empty<byte>(),
                Name = dto.Name,
                Currency = dto.Currency,
                IsGlobal = dto.IsGlobal,
                SelectionMode = dto.SelectionMode,
                MinSelections = dto.MinSelections,
                MaxSelections = dto.MaxSelections,
                IsActive = dto.IsActive,
                Options = dto.Options.Select(o => new AddOnOptionVm
                {
                    Label = o.Label,
                    SortOrder = o.SortOrder,
                    Values = o.Values.Select(v => new AddOnOptionValueVm
                    {
                        Label = v.Label,
                        PriceDeltaMinor = v.PriceDeltaMinor,
                        Hint = v.Hint,
                        SortOrder = v.SortOrder,
                        IsActive = v.IsActive
                    }).ToList()
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddOnGroupEditVm vm, CancellationToken ct = default)
        {
            vm.Options ??= new();
            foreach (var o in vm.Options) o.Values ??= new();

            if (!ModelState.IsValid) return View(vm);

            var dto = new AddOnGroupEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name?.Trim() ?? string.Empty,
                Currency = vm.Currency?.Trim().ToUpperInvariant() ?? "EUR",
                IsGlobal = vm.IsGlobal,
                SelectionMode = vm.SelectionMode,
                MinSelections = vm.MinSelections,
                MaxSelections = vm.MaxSelections,
                IsActive = vm.IsActive,
                Options = vm.Options.Select(o => new AddOnOptionDto
                {
                    Label = o.Label?.Trim() ?? string.Empty,
                    SortOrder = o.SortOrder,
                    Values = o.Values.Select(v => new AddOnOptionValueDto
                    {
                        Label = v.Label?.Trim() ?? string.Empty,
                        PriceDeltaMinor = v.PriceDeltaMinor,
                        Hint = string.IsNullOrWhiteSpace(v.Hint) ? null : v.Hint.Trim(),
                        SortOrder = v.SortOrder,
                        IsActive = v.IsActive
                    }).ToList()
                }).ToList()
            };

            try
            {
                await _update.HandleAsync(dto, ct); // Application: Task (no Result)
                TempData["Success"] = "Add-on group updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Concurrency conflict: the record was modified by another user.");
                return View(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(vm);
            }
        }

        // ---------------- Delete (soft) ----------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                var dto = new AddOnGroupDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };
                var result = await _softDelete.HandleAsync(dto, ct);
                if (!result.Succeeded)
                    TempData["Warning"] = result.Error ?? "Failed to delete add-on group.";
                else
                    TempData["Success"] = "Add-on group deleted.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete add-on group.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Attach: Products ----------------

        /// <summary>
        /// GET: show selectable products with pre-checked items that are already attached to the add-on group.
        /// Mirrors the paging/filtering UI but marks Selected based on current attachments.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AttachToProducts(Guid id, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var group = await _getForEdit.HandleAsync(id, ct);
            if (group == null)
                return NotFound();

            var (items, total) = await _getProductsPage.HandleAsync(page, pageSize, "de-DE", ct); // culture-based list

            // Load attached ids
            var attached = await _getAttachedProducts.HandleAsync(id, ct);
            var attachedSet = attached.ToHashSet();

            var vm = new AddOnGroupAttachToProductsVm
            {
                AddOnGroupId = group.Id,
                AddOnGroupName = group.Name,
                RowVersion = group.RowVersion ?? Array.Empty<byte>(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(p => new SelectableItemVm
                {
                    Id = p.Id,
                    Display = p.DefaultName ?? p.Id.ToString(),
                    Selected = attachedSet.Contains(p.Id)
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachToProducts(AddOnGroupAttachToProductsVm vm, CancellationToken ct = default)
        {
            var dto = new AddOnGroupAttachToProductsDto
            {
                AddOnGroupId = vm.AddOnGroupId,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                ProductIds = (vm.SelectedProductIds ?? new List<Guid>()).ToArray()
            };

            var result = await _attachProducts.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error ?? "Failed to attach to products.";
                return RedirectToAction(nameof(AttachToProducts), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize });
            }

            TempData["Success"] = "Attached to products.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Attach: Categories ----------------

        [HttpGet]
        public async Task<IActionResult> AttachToCategories(Guid id, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var group = await _getForEdit.HandleAsync(id, ct);
            if (group == null)
                return NotFound();

            var (items, total) = await _getCategoriesPage.HandleAsync(page, pageSize, "de-DE", ct);

            var attached = await _getAttachedCategories.HandleAsync(id, ct);
            var attachedSet = attached.ToHashSet();

            var vm = new AddOnGroupAttachToCategoriesVm
            {
                AddOnGroupId = group.Id,
                AddOnGroupName = group.Name,
                RowVersion = group.RowVersion ?? Array.Empty<byte>(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(c => new SelectableItemVm
                {
                    Id = c.Id,
                    Display = string.IsNullOrWhiteSpace(c.Name) ? c.Id.ToString() : c.Name,
                    Selected = attachedSet.Contains(c.Id)
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachToCategories(AddOnGroupAttachToCategoriesVm vm, CancellationToken ct = default)
        {
            try
            {
                await _attachCategories.HandleAsync(
                    vm.AddOnGroupId,
                    (vm.SelectedCategoryIds ?? new List<Guid>()).ToArray(),
                    ct);

                TempData["Success"] = "Attached to categories.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(AttachToCategories), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize });
            }
        }

        // ---------------- Attach: Brands ----------------

        [HttpGet]
        public async Task<IActionResult> AttachToBrands(Guid id, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var group = await _getForEdit.HandleAsync(id, ct);
            if (group == null)
                return NotFound();

            var (items, total) = await _getBrandsPage.HandleAsync(page, pageSize, "de-DE", ct);

            var attached = await _getAttachedBrands.HandleAsync(id, ct);
            var attachedSet = attached.ToHashSet();

            var vm = new AddOnGroupAttachToBrandsVm
            {
                AddOnGroupId = group.Id,
                AddOnGroupName = group.Name,
                RowVersion = group.RowVersion ?? Array.Empty<byte>(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(b => new SelectableItemVm
                {
                    Id = b.Id,
                    Display = string.IsNullOrWhiteSpace(b.Name) ? b.Id.ToString() : b.Name,
                    Selected = attachedSet.Contains(b.Id)
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachToBrands(AddOnGroupAttachToBrandsVm vm, CancellationToken ct = default)
        {
            try
            {
                await _attachBrands.HandleAsync(
                    vm.AddOnGroupId,
                    (vm.SelectedBrandIds ?? new List<Guid>()).ToArray(),
                    ct);

                TempData["Success"] = "Attached to brands.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(AttachToBrands), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize });
            }
        }

        // ---------------- Attach: Variants (override level) ----------------

        /// <summary>
        /// GET placeholder: UI can provide variants from the product/search page.
        /// This action only gives the group information for the header + concurrency.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AttachToVariants(
            Guid id,
            string? q,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct);
            if (dto is null) return NotFound();
            
            // Determine culture (fallback to "de-DE" if not resolved from site/user)
            var culture = CultureInfo.CurrentUICulture?.Name ?? "de-DE";

            var (items, total) = await _getVariantsPage.HandleAsync(page, pageSize, q, culture, ct);

            // Load currently attached variant ids (non-deleted links)
            var attached = await _getAttachedVariants.HandleAsync(id, ct);
            var attachedSet = attached.ToHashSet();

            var vm = new AddOnGroupAttachToVariantsVm
            {
                AddOnGroupId = dto.Id,
                RowVersion = dto.RowVersion,
                AddOnGroupName = dto.Name,
                Query = q,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
                    .Select(v => new SelectableItemVm
                    {
                        Id = v.Id,
                        Display = $"{v.Sku} — {v.ProductName ?? "(no name)"}",
                        Selected = attachedSet.Contains(v.Id) /* isSelected: handled client-side via hidden inputs to persist across pages */
                    }).ToList(),
                SelectedVariantIds = attached.ToList()
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachToVariants(AddOnGroupAttachToVariantsVm vm, CancellationToken ct = default)
        {
            var dto = new AddOnGroupAttachToVariantsDto
            {
                AddOnGroupId = vm.AddOnGroupId,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                VariantIds = (vm.SelectedVariantIds ?? new List<Guid>()).ToArray()
            };

            var result = await _attachVariants.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error ?? "Failed to attach to variants.";
                return RedirectToAction(nameof(AttachToVariants), new { id = vm.AddOnGroupId });
            }

            TempData["Success"] = "Attached to variants.";
            return RedirectToAction(nameof(Index));
        }
    }
}
