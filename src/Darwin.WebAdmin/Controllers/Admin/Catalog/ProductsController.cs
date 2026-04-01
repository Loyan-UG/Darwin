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
    ///     Admin controller for managing products (list, create, edit) including translations and variants.
    ///     Integrates with Application handlers for input validation, mapping, and persistence,
    ///     and supplies lookups (brands, categories, tax categories, currencies) to views.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         UX:
    ///         <list type="bullet">
    ///             <item>Uses Bootstrap for layout and field-level help tooltips.</item>
    ///             <item>Employs Quill v2 for rich text editing of product descriptions per translation.</item>
    ///             <item>Enforces server-side validation via FluentValidation with friendly error display.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Concurrency:
    ///         Edit actions include the <c>RowVersion</c> token to detect concurrent updates, surfacing a helpful error on conflict.
    ///     </para>
    ///     <para>
    ///         Security:
    ///         CSRF protection via anti-forgery tokens; input model binding restricted to view models (no over-posting).
    ///     </para>
    /// </remarks>
    public sealed class ProductsController : AdminBaseController
    {
        private readonly CreateProductHandler _createProduct;
        private readonly UpdateProductHandler _updateProduct;
        private readonly GetProductsPageHandler _getProductsPage;
        private readonly GetProductOpsSummaryHandler _getProductOpsSummary;
        private readonly GetProductForEditHandler _getProductForEdit;
        private readonly GetCatalogLookupsHandler _getLookups;
        private readonly GetCulturesHandler _getCultures;
        private readonly SoftDeleteProductHandler _softDeleteProduct;

        public ProductsController(
            CreateProductHandler createProduct,
            UpdateProductHandler updateProduct,
            GetProductsPageHandler getProductsPage,
            GetProductOpsSummaryHandler getProductOpsSummary,
            GetProductForEditHandler getProductForEdit,
            GetCatalogLookupsHandler getLookups,
            GetCulturesHandler getCultures,
            SoftDeleteProductHandler softDeleteProduct)
        {
            _createProduct = createProduct;
            _updateProduct = updateProduct;
            _getProductsPage = getProductsPage;
            _getProductOpsSummary = getProductOpsSummary;
            _getProductForEdit = getProductForEdit;
            _getLookups = getLookups;
            _getCultures = getCultures;
            _softDeleteProduct = softDeleteProduct;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)
        {
            var (items, total) = await _getProductsPage.HandleAsync(page, pageSize, "de-DE", query, filter, ct);
            var summary = await _getProductOpsSummary.HandleAsync(ct);

            var vm = new ProductsIndexVm
            {
                Items = items,
                Query = query ?? string.Empty,
                Filter = filter ?? string.Empty,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Summary = new ProductOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    InactiveCount = summary.InactiveCount,
                    HiddenCount = summary.HiddenCount,
                    SingleVariantCount = summary.SingleVariantCount,
                    ScheduledCount = summary.ScheduledCount
                },
                Playbooks = BuildProductPlaybooks()
            };

            return RenderIndexWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            await LoadLookupsAsync(ct);
            var vm = new ProductCreateVm();
            vm.Translations ??= new();
            if (vm.Translations.Count == 0) vm.Translations.Add(new ProductTranslationVm { Culture = "de-DE" });

            vm.Variants ??= new();
            if (vm.Variants.Count == 0) vm.Variants.Add(new ProductVariantCreateVm { Currency = "EUR" });

            return RenderCreateEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            vm.Variants ??= new();

            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), "At least one translation is required.");
            if (vm.Variants.Count == 0)
                ModelState.AddModelError(nameof(vm.Variants), "At least one variant is required.");

            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                EnsureProductDefaults(vm);
                return RenderCreateEditor(vm);
            }

            var dto = new ProductCreateDto
            {
                BrandId = vm.BrandId,
                PrimaryCategoryId = vm.PrimaryCategoryId,
                Kind = vm.Kind,
                Translations = vm.Translations.Select(t => new ProductTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    ShortDescription = t.ShortDescription,
                    FullDescriptionHtml = t.FullDescriptionHtml,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    SearchKeywords = t.SearchKeywords
                }).ToList(),
                Variants = vm.Variants.Select(v => new ProductVariantCreateDto
                {
                    Sku = v.Sku,
                    Gtin = v.Gtin,
                    ManufacturerPartNumber = v.ManufacturerPartNumber,
                    BasePriceNetMinor = v.BasePriceNetMinor,
                    CompareAtPriceNetMinor = v.CompareAtPriceNetMinor,
                    Currency = v.Currency,
                    TaxCategoryId = v.TaxCategoryId,
                    StockOnHand = v.StockOnHand,
                    StockReserved = v.StockReserved,
                    ReorderPoint = v.ReorderPoint,
                    BackorderAllowed = v.BackorderAllowed,
                    MinOrderQty = v.MinOrderQty,
                    MaxOrderQty = v.MaxOrderQty,
                    StepOrderQty = v.StepOrderQty,
                    PackageWeight = v.PackageWeight,
                    PackageLength = v.PackageLength,
                    PackageWidth = v.PackageWidth,
                    PackageHeight = v.PackageHeight,
                    IsDigital = v.IsDigital
                }).ToList()
            };

            try
            {
                await _createProduct.HandleAsync(dto, ct);
                SetSuccessMessage("ProductCreated");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await LoadLookupsAsync(ct);
                EnsureProductDefaults(vm);
                return RenderCreateEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var dto = await _getProductForEdit.HandleAsync(id, ct);
            if (dto == null)
            {
                SetErrorMessage("ProductNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new ProductEditVm
            {
                Id = dto.Id,
                BrandId = dto.BrandId,
                PrimaryCategoryId = dto.PrimaryCategoryId,
                Kind = dto.Kind,
                RowVersion = dto.RowVersion,
                Translations = dto.Translations?.Select(t => new ProductTranslationVm
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    ShortDescription = t.ShortDescription,
                    FullDescriptionHtml = t.FullDescriptionHtml,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    SearchKeywords = t.SearchKeywords
                }).ToList() ?? new(),
                Variants = dto.Variants?.Select(v => new ProductVariantCreateVm
                {
                    Sku = v.Sku,
                    Gtin = v.Gtin,
                    ManufacturerPartNumber = v.ManufacturerPartNumber,
                    BasePriceNetMinor = v.BasePriceNetMinor,
                    CompareAtPriceNetMinor = v.CompareAtPriceNetMinor,
                    Currency = v.Currency,
                    TaxCategoryId = v.TaxCategoryId,
                    StockOnHand = v.StockOnHand,
                    StockReserved = v.StockReserved,
                    ReorderPoint = v.ReorderPoint,
                    BackorderAllowed = v.BackorderAllowed,
                    MinOrderQty = v.MinOrderQty,
                    MaxOrderQty = v.MaxOrderQty,
                    StepOrderQty = v.StepOrderQty,
                    PackageWeight = v.PackageWeight,
                    PackageLength = v.PackageLength,
                    PackageWidth = v.PackageWidth,
                    PackageHeight = v.PackageHeight,
                    IsDigital = v.IsDigital
                }).ToList() ?? new()
            };

            await LoadLookupsAsync(ct);
            return RenderEditEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(ProductEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            vm.Variants ??= new();

            if (!ModelState.IsValid)
            {
                await LoadLookupsAsync(ct);
                EnsureProductDefaults(vm);
                return RenderEditEditor(vm);
            }

            var dto = new ProductEditDto
            {
                Id = vm.Id,
                BrandId = vm.BrandId,
                PrimaryCategoryId = vm.PrimaryCategoryId,
                Kind = vm.Kind,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Translations = vm.Translations.Select(t => new ProductTranslationDto
                {
                    Culture = t.Culture,
                    Name = t.Name,
                    Slug = t.Slug,
                    ShortDescription = t.ShortDescription,
                    FullDescriptionHtml = t.FullDescriptionHtml,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    SearchKeywords = t.SearchKeywords
                }).ToList(),
                Variants = vm.Variants.Select(v => new ProductVariantCreateDto
                {
                    Sku = v.Sku,
                    Gtin = v.Gtin,
                    ManufacturerPartNumber = v.ManufacturerPartNumber,
                    BasePriceNetMinor = v.BasePriceNetMinor,
                    CompareAtPriceNetMinor = v.CompareAtPriceNetMinor,
                    Currency = v.Currency,
                    TaxCategoryId = v.TaxCategoryId,
                    StockOnHand = v.StockOnHand,
                    StockReserved = v.StockReserved,
                    ReorderPoint = v.ReorderPoint,
                    BackorderAllowed = v.BackorderAllowed,
                    MinOrderQty = v.MinOrderQty,
                    MaxOrderQty = v.MaxOrderQty,
                    StepOrderQty = v.StepOrderQty,
                    PackageWeight = v.PackageWeight,
                    PackageLength = v.PackageLength,
                    PackageWidth = v.PackageWidth,
                    PackageHeight = v.PackageHeight,
                    IsDigital = v.IsDigital
                }).ToList()
            };

            try
            {
                await _updateProduct.HandleAsync(dto, ct);
                SetSuccessMessage("ProductUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, T("ProductConcurrencyConflict"));
                await LoadLookupsAsync(ct);
                EnsureProductDefaults(vm);
                return RenderEditEditor(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await LoadLookupsAsync(ct);
                EnsureProductDefaults(vm);
                return RenderEditEditor(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)
        {
            try { await _softDeleteProduct.HandleAsync(id, ct); SetSuccessMessage("ProductDeleted"); }
            catch { SetErrorMessage("ProductDeleteFailed"); }
            return RedirectOrHtmx(nameof(Index), new { });
        }

        private async Task LoadLookupsAsync(CancellationToken ct)
        {
            var lookups = await _getLookups.HandleAsync("de-DE", ct);
            ViewBag.Brands = lookups.Brands;
            ViewBag.Categories = lookups.Categories;
            ViewBag.TaxCategories = lookups.TaxCategories;

            var (_, cultures) = await _getCultures.HandleAsync(ct);
            ViewBag.Cultures = cultures;

            ViewBag.Currencies = new[] { "EUR", "USD", "GBP" }; // TODO: will move to SiteSetting/table later
        }

        private IActionResult RenderIndexWorkspace(ProductsIndexVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Products/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderCreateEditor(ProductCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Products/_ProductCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderEditEditor(ProductEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Products/_ProductEditEditorShell.cshtml", vm);
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

        private static void EnsureProductDefaults(ProductEditorVm vm)
        {
            if (vm.Translations.Count == 0)
            {
                vm.Translations.Add(new ProductTranslationVm { Culture = "de-DE" });
            }

            if (vm.Variants.Count == 0)
            {
                vm.Variants.Add(new ProductVariantCreateVm { Currency = "EUR" });
            }
        }

        private static OperationalPlaybookVm[] BuildProductPlaybooks()
        {
            return
            [
                new OperationalPlaybookVm
                {
                    QueueLabel = "Inactive",
                    WhyItMatters = "Inactive products stay out of live selling flows even when content and inventory already exist.",
                    OperatorAction = "Review pricing, legal content, and stock readiness, then reactivate from the product editor when the item is ready."
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = "Hidden",
                    WhyItMatters = "Hidden products often indicate merchandising holds, launch timing, or accidental visibility loss.",
                    OperatorAction = "Confirm whether the product should remain hidden and restore visibility if the business expects it to appear."
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = "Scheduled",
                    WhyItMatters = "Publish windows create timing-sensitive catalog behavior around launches, campaigns, and seasonal stock.",
                    OperatorAction = "Verify the publish window still matches campaign timing and that inventory and content are aligned before the window opens."
                }
            ];
        }
    }
}
