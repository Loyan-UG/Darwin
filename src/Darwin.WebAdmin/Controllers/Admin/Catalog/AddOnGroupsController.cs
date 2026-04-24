using Darwin.Application.Catalog.Commands;
// Application layer
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebAdmin.Controllers.Admin.Catalog
{
    /// <summary>
    /// Admin controller to manage Add-on Groups and their attachments across
    /// Variant ? Product ? Category ? Brand ? Global resolution levels.
    /// </summary>
    public sealed class AddOnGroupsController : AdminBaseController
    {
        // Groups
        private readonly GetAddOnGroupsPageHandler _getPage;
        private readonly GetAddOnGroupOpsSummaryHandler _getSummary;
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
        private readonly ISiteSettingCache _siteSettingCache;

        public AddOnGroupsController(
            GetAddOnGroupsPageHandler getPage,
            GetAddOnGroupOpsSummaryHandler getSummary,
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
            GetAddOnGroupAttachedBrandIdsHandler getAttachedBrands,
            ISiteSettingCache siteSettingCache
            )
        {
            _getPage = getPage;
            _getSummary = getSummary;
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
            _siteSettingCache = siteSettingCache;
        }

        // ---------------- List ----------------

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            AddOnGroupQueueFilter filter = AddOnGroupQueueFilter.All,
            CancellationToken ct = default)
        {
            // Application paging contract mirrors Brands (items,total)
            var (items, total) = await _getPage.HandleAsync(page, pageSize, query, filter, ct);
            var summary = await _getSummary.HandleAsync(query, ct);
            var vm = new AddOnGroupsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                FilterItems = BuildFilterItems(filter),
                Summary = new AddOnGroupOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    InactiveCount = summary.InactiveCount,
                    GlobalCount = summary.GlobalCount,
                    UnattachedCount = summary.UnattachedCount,
                    VariantLinkedCount = summary.VariantLinkedCount
                },
                Playbooks = BuildPlaybooks(),
                Items = items.Select(x => new AddOnGroupListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    Currency = x.Currency,
                    IsGlobal = x.IsGlobal,
                    IsActive = x.IsActive,
                    OptionsCount = x.OptionsCount,
                    AttachmentCount = x.AttachmentCount,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };
            return RenderIndexWorkspace(vm);
        }

        // ---------------- Create ----------------

        [HttpGet]
        public IActionResult Create()
        {
            var defaultCurrency = _siteSettingCache.GetAsync().GetAwaiter().GetResult().DefaultCurrency;

            // A simple example for user convenience (Option + a Value)
            return RenderCreateEditor(new AddOnGroupCreateVm
            {
                Currency = defaultCurrency,
                Options = { new AddOnOptionVm { Label = "Option", Values = { new AddOnOptionValueVm { Label = "Value" } } } }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddOnGroupCreateVm vm, CancellationToken ct = default)
        {
            vm.Options ??= new();
            foreach (var o in vm.Options) o.Values ??= new();
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct)).DefaultCurrency;

            if (!ModelState.IsValid) return RenderCreateEditor(vm);

            var dto = new AddOnGroupCreateDto
            {
                Name = vm.Name?.Trim() ?? string.Empty,
                Currency = string.IsNullOrWhiteSpace(vm.Currency)
                    ? defaultCurrency
                    : vm.Currency.Trim().ToUpperInvariant(),
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
                SetSuccessMessage("AddOnGroupCreated");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                return RenderCreateEditor(vm);
            }
            catch (Exception)
            {
                AddModelErrorMessage("AddOnGroupCreateFailed");
                return RenderCreateEditor(vm);
            }
        }

        // ---------------- Edit ----------------

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct); // returns DTO directly
            if (dto is null)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
            return RenderEditEditor(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddOnGroupEditVm vm, CancellationToken ct = default)
        {
            vm.Options ??= new();
            foreach (var o in vm.Options) o.Values ??= new();
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct)).DefaultCurrency;

            if (!ModelState.IsValid) return RenderEditEditor(vm);

            var dto = new AddOnGroupEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name?.Trim() ?? string.Empty,
                Currency = string.IsNullOrWhiteSpace(vm.Currency)
                    ? defaultCurrency
                    : vm.Currency.Trim().ToUpperInvariant(),
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
                SetSuccessMessage("AddOnGroupUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, T("AddOnGroupConcurrencyConflict"));
                return RenderEditEditor(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors) ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                return RenderEditEditor(vm);
            }
            catch (Exception)
            {
                AddModelErrorMessage("AddOnGroupUpdateFailed");
                return RenderEditEditor(vm);
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
                    SetWarningMessage("AddOnGroupDeleteFailed");
                else
                    SetSuccessMessage("AddOnGroupDeleted");
            }
            catch (Exception)
            {
                SetErrorMessage("AddOnGroupDeleteFailed");
            }
            return RedirectOrHtmx(nameof(Index), new { });
        }

        // ---------------- Attach: Products ----------------

        /// <summary>
        /// GET: show selectable products with pre-checked items that are already attached to the add-on group.
        /// Mirrors the paging/filtering UI but marks Selected based on current attachments.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AttachToProducts(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var group = await _getForEdit.HandleAsync(id, ct);
            if (group == null)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var defaultCulture = (await _siteSettingCache.GetAsync(ct)).DefaultCulture;
            var (items, total) = await _getProductsPage.HandleAsync(page, pageSize, defaultCulture, query, filter: null, ct); // culture-based list

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
                Query = query ?? string.Empty,
                Items = items.Select(p => new SelectableItemVm
                {
                    Id = p.Id,
                    Display = p.DefaultName ?? p.Id.ToString(),
                    Selected = attachedSet.Contains(p.Id)
                }).ToList()
            };
            return RenderAttachToProducts(vm);
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
                SetErrorMessage("AddOnGroupAttachProductsFailed");
                return RedirectOrHtmx(nameof(AttachToProducts), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query });
            }

            SetSuccessMessage("AddOnGroupAttachedToProducts");
            return RedirectOrHtmx(nameof(Index), new { });
        }

        // ---------------- Attach: Categories ----------------

        [HttpGet]
        public async Task<IActionResult> AttachToCategories(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var group = await _getForEdit.HandleAsync(id, ct);
            if (group == null)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var defaultCulture = (await _siteSettingCache.GetAsync(ct)).DefaultCulture;
            var (items, total) = await _getCategoriesPage.HandleAsync(page, pageSize, defaultCulture, query, filter: null, ct);

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
                Query = query ?? string.Empty,
                Items = items.Select(c => new SelectableItemVm
                {
                    Id = c.Id,
                    Display = string.IsNullOrWhiteSpace(c.Name) ? c.Id.ToString() : c.Name,
                    Selected = attachedSet.Contains(c.Id)
                }).ToList()
            };
            return RenderAttachToCategories(vm);
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

                SetSuccessMessage("AddOnGroupAttachedToCategories");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (Exception)
            {
                SetErrorMessage("AddOnGroupAttachCategoriesFailed");
                return RedirectOrHtmx(nameof(AttachToCategories), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query });
            }
        }

        // ---------------- Attach: Brands ----------------

        [HttpGet]
        public async Task<IActionResult> AttachToBrands(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var group = await _getForEdit.HandleAsync(id, ct);
            if (group == null)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var defaultCulture = (await _siteSettingCache.GetAsync(ct)).DefaultCulture;
            var (items, total) = await _getBrandsPage.HandleAsync(page, pageSize, defaultCulture, query, filter: null, ct);

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
                Query = query ?? string.Empty,
                Items = items.Select(b => new SelectableItemVm
                {
                    Id = b.Id,
                    Display = string.IsNullOrWhiteSpace(b.Name) ? b.Id.ToString() : b.Name,
                    Selected = attachedSet.Contains(b.Id)
                }).ToList()
            };
            return RenderAttachToBrands(vm);
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

                SetSuccessMessage("AddOnGroupAttachedToBrands");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (Exception)
            {
                SetErrorMessage("AddOnGroupAttachBrandsFailed");
                return RedirectOrHtmx(nameof(AttachToBrands), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query });
            }
        }

        // ---------------- Attach: Variants (override level) ----------------

        /// <summary>
        /// GET: show selectable variants with pre-checked rows that are already attached to the add-on group.
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
            if (dto is null)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            
            var culture = (await _siteSettingCache.GetAsync(ct)).DefaultCulture;

            var (items, total) = await _getVariantsPage.HandleAsync(page, pageSize, q, culture, ct);

            // Load currently attached variant ids (non-deleted links)
            var attached = await _getAttachedVariants.HandleAsync(id, ct);
            var attachedSet = attached.ToHashSet();

            var vm = new AddOnGroupAttachToVariantsVm
            {
                AddOnGroupId = dto.Id,
                RowVersion = dto.RowVersion,
                AddOnGroupName = dto.Name,
                Query = q ?? string.Empty,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
                    .Select(v => new SelectableVariantItemVm
                    {
                        Id = v.Id,
                        Sku = v.Sku,
                        ProductName = string.IsNullOrWhiteSpace(v.ProductName) ? T("ProductUnnamed") : v.ProductName,
                        Gtin = v.Gtin,
                        Currency = v.Currency,
                        BasePriceNetMinor = v.BasePriceNetMinor,
                        StockOnHand = v.StockOnHand,
                        IsDigital = v.IsDigital,
                        Selected = attachedSet.Contains(v.Id)
                    }).ToList(),
                SelectedVariantIds = attached.ToList()
            };

            return RenderAttachToVariants(vm);
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
                SetErrorMessage("AddOnGroupAttachVariantsFailed");
                return RedirectOrHtmx(nameof(AttachToVariants), new { id = vm.AddOnGroupId, q = vm.Query, page = vm.Page, pageSize = vm.PageSize });
            }

            SetSuccessMessage("AddOnGroupAttachedToVariants");
            return RedirectOrHtmx(nameof(Index), new { });
        }

        private IActionResult RenderCreateEditor(AddOnGroupCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/AddOnGroups/_AddOnGroupCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderEditEditor(AddOnGroupEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/AddOnGroups/_AddOnGroupEditEditorShell.cshtml", vm);
            }

            return View("Edit", vm);
        }

        private IActionResult RenderIndexWorkspace(AddOnGroupsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/AddOnGroups/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderAttachToProducts(AddOnGroupAttachToProductsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/AddOnGroups/AttachToProducts.cshtml", vm);
            }

            return View("AttachToProducts", vm);
        }

        private IActionResult RenderAttachToCategories(AddOnGroupAttachToCategoriesVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/AddOnGroups/AttachToCategories.cshtml", vm);
            }

            return View("AttachToCategories", vm);
        }

        private IActionResult RenderAttachToBrands(AddOnGroupAttachToBrandsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/AddOnGroups/AttachToBrands.cshtml", vm);
            }

            return View("AttachToBrands", vm);
        }

        private IActionResult RenderAttachToVariants(AddOnGroupAttachToVariantsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/AddOnGroups/AttachToVariants.cshtml", vm);
            }

            return View("AttachToVariants", vm);
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

        private IEnumerable<SelectListItem> BuildFilterItems(AddOnGroupQueueFilter selected)
        {
            yield return new SelectListItem(T("AddOnAllGroups"), AddOnGroupQueueFilter.All.ToString(), selected == AddOnGroupQueueFilter.All);
            yield return new SelectListItem(T("Inactive"), AddOnGroupQueueFilter.Inactive.ToString(), selected == AddOnGroupQueueFilter.Inactive);
            yield return new SelectListItem(T("Global"), AddOnGroupQueueFilter.Global.ToString(), selected == AddOnGroupQueueFilter.Global);
            yield return new SelectListItem(T("Unattached"), AddOnGroupQueueFilter.Unattached.ToString(), selected == AddOnGroupQueueFilter.Unattached);
            yield return new SelectListItem(T("AddOnVariantProductLinked"), AddOnGroupQueueFilter.VariantLinked.ToString(), selected == AddOnGroupQueueFilter.VariantLinked);
        }

        private List<AddOnGroupPlaybookVm> BuildPlaybooks()
        {
            return
            [
                new()
                {
                    QueueLabel = T("AddOnPlaybookUnattachedTitle"),
                    WhyItMatters = T("AddOnPlaybookUnattachedScope"),
                    OperatorAction = T("AddOnPlaybookUnattachedAction")
                },
                new()
                {
                    QueueLabel = T("AddOnPlaybookInactiveTitle"),
                    WhyItMatters = T("AddOnPlaybookInactiveScopeLive"),
                    OperatorAction = T("AddOnPlaybookInactiveActionLive")
                },
                new()
                {
                    QueueLabel = T("AddOnPlaybookGlobalTitle"),
                    WhyItMatters = T("AddOnPlaybookGlobalScope"),
                    OperatorAction = T("AddOnPlaybookGlobalAction")
                }
            ];
        }
    }
}






