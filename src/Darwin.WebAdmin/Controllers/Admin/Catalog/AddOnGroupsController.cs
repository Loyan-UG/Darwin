using Darwin.Application.Catalog.Commands;
// Application layer
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Application.Settings.Queries;
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
        private readonly GetCulturesHandler _getCultures;

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
            ISiteSettingCache siteSettingCache,
            GetCulturesHandler getCultures
            )
        {
            _getPage = getPage ?? throw new ArgumentNullException(nameof(getPage));
            _getSummary = getSummary ?? throw new ArgumentNullException(nameof(getSummary));
            _getForEdit = getForEdit ?? throw new ArgumentNullException(nameof(getForEdit));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _softDelete = softDelete ?? throw new ArgumentNullException(nameof(softDelete));

            _attachVariants = attachVariants ?? throw new ArgumentNullException(nameof(attachVariants));
            _attachProducts = attachProducts ?? throw new ArgumentNullException(nameof(attachProducts));
            _attachCategories = attachCategories ?? throw new ArgumentNullException(nameof(attachCategories));
            _attachBrands = attachBrands ?? throw new ArgumentNullException(nameof(attachBrands));

            _getProductsPage = getProductsPage ?? throw new ArgumentNullException(nameof(getProductsPage));
            _getCategoriesPage = getCategoriesPage ?? throw new ArgumentNullException(nameof(getCategoriesPage));
            _getBrandsPage = getBrandsPage ?? throw new ArgumentNullException(nameof(getBrandsPage));
            _getVariantsPage = getVariantsPage ?? throw new ArgumentNullException(nameof(getVariantsPage));

            _getAttachedProducts = getAttachedProducts ?? throw new ArgumentNullException(nameof(getAttachedProducts));
            _getAttachedVariants = getAttachedVariants ?? throw new ArgumentNullException(nameof(getAttachedVariants));
            _getAttachedCategories = getAttachedCategories ?? throw new ArgumentNullException(nameof(getAttachedCategories));
            _getAttachedBrands = getAttachedBrands ?? throw new ArgumentNullException(nameof(getAttachedBrands));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
            _getCultures = getCultures ?? throw new ArgumentNullException(nameof(getCultures));
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
        public async Task<IActionResult> Create(CancellationToken ct = default)
        {
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCurrency;

            // A simple example for user convenience (Option + a Value)
            var vm = new AddOnGroupCreateVm
            {
                Currency = defaultCurrency,
                Options = { new AddOnOptionVm { Label = "Option", Values = { new AddOnOptionValueVm { Label = "Value" } } } }
            };
            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderCreateEditor(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddOnGroupCreateVm vm, CancellationToken ct = default)
        {
            vm.Options ??= new();
            foreach (var o in vm.Options) o.Values ??= new();
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct)).DefaultCurrency;
            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);

            if (!ModelState.IsValid) return RenderCreateEditor(vm);

            var groupTranslations = FilterCompleteGroupTranslations(vm.Translations);
            var optionDtos = BuildOptionDtos(vm.Options);
            if (groupTranslations.Count > 0)
            {
                vm.Name = groupTranslations[0].Name;
            }

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
                Translations = groupTranslations,
                Options = optionDtos
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
            if (id == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                Translations = dto.Translations.Select(t => new AddOnGroupTranslationVm
                {
                    Culture = t.Culture,
                    Name = t.Name
                }).ToList(),
                Options = dto.Options.Select(o => new AddOnOptionVm
                {
                    Id = o.Id,
                    Label = o.Label,
                    SortOrder = o.SortOrder,
                    Translations = o.Translations.Select(t => new AddOnOptionTranslationVm
                    {
                        Culture = t.Culture,
                        Label = t.Label
                    }).ToList(),
                    Values = o.Values.Select(v => new AddOnOptionValueVm
                    {
                        Id = v.Id,
                        Label = v.Label,
                        PriceDeltaMinor = v.PriceDeltaMinor,
                        Hint = v.Hint,
                        SortOrder = v.SortOrder,
                        IsActive = v.IsActive,
                        Translations = v.Translations.Select(t => new AddOnOptionValueTranslationVm
                        {
                            Culture = t.Culture,
                            Label = t.Label
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditEditor(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddOnGroupEditVm vm, CancellationToken ct = default)
        {
            vm.Options ??= new();
            foreach (var o in vm.Options) o.Values ??= new();
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct)).DefaultCurrency;
            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);

            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid) return RenderEditEditor(vm);

            var groupTranslations = FilterCompleteGroupTranslations(vm.Translations);
            var optionDtos = BuildOptionDtos(vm.Options);
            if (groupTranslations.Count > 0)
            {
                vm.Name = groupTranslations[0].Name;
            }

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
                Translations = groupTranslations,
                Options = optionDtos
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
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupDeleteFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                var dto = new AddOnGroupDeleteDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) };
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
            if (id == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                }).ToList(),
                SelectedProductIds = attached.ToList()
            };
            return RenderAttachToProducts(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachToProducts(AddOnGroupAttachToProductsVm vm, CancellationToken ct = default)
        {
            if (vm.AddOnGroupId == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupAttachProductsFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
            if (id == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                }).ToList(),
                SelectedCategoryIds = attached.ToList()
            };
            return RenderAttachToCategories(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachToCategories(AddOnGroupAttachToCategoriesVm vm, CancellationToken ct = default)
        {
            if (vm.AddOnGroupId == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupAttachCategoriesFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                var result = await _attachCategories.HandleAsync(
                    new AddOnGroupAttachToCategoriesDto
                    {
                        AddOnGroupId = vm.AddOnGroupId,
                        RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                        CategoryIds = (vm.SelectedCategoryIds ?? new List<Guid>()).ToArray()
                    },
                    ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    SetErrorMessage("AddOnGroupAttachCategoriesFailed");
                    return RedirectOrHtmx(nameof(AttachToCategories), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query });
                }

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
            if (id == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                }).ToList(),
                SelectedBrandIds = attached.ToList()
            };
            return RenderAttachToBrands(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AttachToBrands(AddOnGroupAttachToBrandsVm vm, CancellationToken ct = default)
        {
            if (vm.AddOnGroupId == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupAttachBrandsFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                var result = await _attachBrands.HandleAsync(
                    new AddOnGroupAttachToBrandsDto
                    {
                        AddOnGroupId = vm.AddOnGroupId,
                        RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                        BrandIds = (vm.SelectedBrandIds ?? new List<Guid>()).ToArray()
                    },
                    ct).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    SetErrorMessage("AddOnGroupAttachBrandsFailed");
                    return RedirectOrHtmx(nameof(AttachToBrands), new { id = vm.AddOnGroupId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query });
                }

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
            if (id == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
            if (vm.AddOnGroupId == Guid.Empty)
            {
                SetErrorMessage("AddOnGroupAttachVariantsFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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

        private async Task EnsureTranslationsAsync(AddOnGroupEditorVm vm, CancellationToken ct)
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
                    vm.Translations.Add(new AddOnGroupTranslationVm
                    {
                        Culture = culture,
                        Name = string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase) ? vm.Name : string.Empty
                    });
                }

                foreach (var option in vm.Options)
                {
                    option.Translations ??= new();
                    if (option.Translations.All(x => !string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
                    {
                        option.Translations.Add(new AddOnOptionTranslationVm
                        {
                            Culture = culture,
                            Label = string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase) ? option.Label : string.Empty
                        });
                    }

                    option.Values ??= new();
                    foreach (var value in option.Values)
                    {
                        value.Translations ??= new();
                        if (value.Translations.All(x => !string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
                        {
                            value.Translations.Add(new AddOnOptionValueTranslationVm
                            {
                                Culture = culture,
                                Label = string.Equals(culture, defaultCulture, StringComparison.OrdinalIgnoreCase) ? value.Label : string.Empty
                            });
                        }
                    }
                }
            }
        }

        private static List<AddOnGroupTranslationDto> FilterCompleteGroupTranslations(IEnumerable<AddOnGroupTranslationVm> translations)
        {
            return translations
                .Where(static t => !string.IsNullOrWhiteSpace(t.Culture) && !string.IsNullOrWhiteSpace(t.Name))
                .Select(static t => new AddOnGroupTranslationDto
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim()
                })
                .ToList();
        }

        private static List<AddOnOptionDto> BuildOptionDtos(IEnumerable<AddOnOptionVm> options)
        {
            return options.Select(o =>
            {
                var optionTranslations = o.Translations
                    .Where(static t => !string.IsNullOrWhiteSpace(t.Culture) && !string.IsNullOrWhiteSpace(t.Label))
                    .Select(static t => new AddOnOptionTranslationDto { Culture = t.Culture.Trim(), Label = t.Label.Trim() })
                    .ToList();
                var optionLabel = optionTranslations.FirstOrDefault()?.Label ?? o.Label?.Trim() ?? string.Empty;

                return new AddOnOptionDto
                {
                    Id = o.Id,
                    Label = optionLabel,
                    SortOrder = o.SortOrder,
                    Translations = optionTranslations,
                    Values = o.Values.Select(v =>
                    {
                        var valueTranslations = v.Translations
                            .Where(static t => !string.IsNullOrWhiteSpace(t.Culture) && !string.IsNullOrWhiteSpace(t.Label))
                            .Select(static t => new AddOnOptionValueTranslationDto { Culture = t.Culture.Trim(), Label = t.Label.Trim() })
                            .ToList();
                        var valueLabel = valueTranslations.FirstOrDefault()?.Label ?? v.Label?.Trim() ?? string.Empty;

                        return new AddOnOptionValueDto
                        {
                            Id = v.Id,
                            Label = valueLabel,
                            PriceDeltaMinor = v.PriceDeltaMinor,
                            Hint = string.IsNullOrWhiteSpace(v.Hint) ? null : v.Hint.Trim(),
                            SortOrder = v.SortOrder,
                            IsActive = v.IsActive,
                            Translations = valueTranslations
                        };
                    }).ToList()
                };
            }).ToList();
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






