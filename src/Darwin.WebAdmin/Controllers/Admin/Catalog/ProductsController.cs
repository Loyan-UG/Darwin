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
        private readonly ISiteSettingCache _siteSettingCache;

        public ProductsController(
            CreateProductHandler createProduct,
            UpdateProductHandler updateProduct,
            GetProductsPageHandler getProductsPage,
            GetProductOpsSummaryHandler getProductOpsSummary,
            GetProductForEditHandler getProductForEdit,
            GetCatalogLookupsHandler getLookups,
            GetCulturesHandler getCultures,
            SoftDeleteProductHandler softDeleteProduct,
            ISiteSettingCache siteSettingCache)
        {
            _createProduct = createProduct ?? throw new ArgumentNullException(nameof(createProduct));
            _updateProduct = updateProduct ?? throw new ArgumentNullException(nameof(updateProduct));
            _getProductsPage = getProductsPage ?? throw new ArgumentNullException(nameof(getProductsPage));
            _getProductOpsSummary = getProductOpsSummary ?? throw new ArgumentNullException(nameof(getProductOpsSummary));
            _getProductForEdit = getProductForEdit ?? throw new ArgumentNullException(nameof(getProductForEdit));
            _getLookups = getLookups ?? throw new ArgumentNullException(nameof(getLookups));
            _getCultures = getCultures ?? throw new ArgumentNullException(nameof(getCultures));
            _softDeleteProduct = softDeleteProduct ?? throw new ArgumentNullException(nameof(softDeleteProduct));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)
        {
            var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;
            var (items, total) = await _getProductsPage.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);
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
            var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var defaultCurrency = siteSettings.DefaultCurrency;
            var vm = new ProductCreateVm();
            await PopulateProductLookupsAsync(vm, siteSettings.DefaultCulture, defaultCurrency, ct).ConfigureAwait(false);
            vm.Translations ??= new();
            await EnsureProductTranslationsAsync(vm, ct).ConfigureAwait(false);

            vm.Variants ??= new();
            EnsureVariantDefaults(vm, defaultCurrency);

            return RenderCreateEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(ProductCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            vm.Variants ??= new();

            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), T("ProductAtLeastOneTranslationRequired"));
            if (vm.Variants.Count == 0)
                ModelState.AddModelError(nameof(vm.Variants), T("ProductAtLeastOneVariantRequired"));

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), T("ProductAtLeastOneTranslationRequired"));

            if (!ModelState.IsValid)
            {
                await EnsureProductDefaultsAsync(vm, ct).ConfigureAwait(false);
                return RenderCreateEditor(vm);
            }

            var dto = new ProductCreateDto
            {
                BrandId = vm.BrandId,
                PrimaryCategoryId = vm.PrimaryCategoryId,
                Kind = vm.Kind,
                Translations = translations.Select(t => new ProductTranslationDto
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

                await EnsureProductDefaultsAsync(vm, ct).ConfigureAwait(false);
                return RenderCreateEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("ProductNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                    Id = v.Id,
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

            await PopulateProductLookupsAsync(vm, ct).ConfigureAwait(false);
            await EnsureProductTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderEditEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(ProductEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            vm.Variants ??= new();

            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("ProductNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await EnsureProductDefaultsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.Translations), T("ProductAtLeastOneTranslationRequired"));
                await EnsureProductDefaultsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }

            var dto = new ProductEditDto
            {
                Id = vm.Id,
                BrandId = vm.BrandId,
                PrimaryCategoryId = vm.PrimaryCategoryId,
                Kind = vm.Kind,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Translations = translations.Select(t => new ProductTranslationDto
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
                    Id = v.Id,
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
                await EnsureProductDefaultsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await EnsureProductDefaultsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("ProductDeleteFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _softDeleteProduct.HandleAsync(id, DecodeBase64RowVersion(rowVersion), ct);
            if (result.Succeeded)
                SetSuccessMessage("ProductDeleted");
            else
                SetErrorMessage("ProductDeleteFailed");

            return RedirectOrHtmx(nameof(Index), new { });
        }

        private async Task PopulateProductLookupsAsync(ProductEditorVm vm, CancellationToken ct)
        {
            var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            await PopulateProductLookupsAsync(vm, siteSettings.DefaultCulture, siteSettings.DefaultCurrency, ct).ConfigureAwait(false);
        }

        private async Task PopulateProductLookupsAsync(ProductEditorVm vm, string defaultCulture, string defaultCurrency, CancellationToken ct)
        {
            var lookups = await _getLookups.HandleAsync(defaultCulture, ct);
            vm.BrandOptions = BuildLookupItems(lookups.Brands, includeEmpty: true);
            vm.CategoryOptions = BuildLookupItems(lookups.Categories, includeEmpty: true);
            vm.TaxCategoryOptions = BuildLookupItems(lookups.TaxCategories, includeEmpty: false);

            var (_, cultures) = await _getCultures.HandleAsync(ct).ConfigureAwait(false);
            vm.Cultures = cultures;

            vm.Currencies = BuildCurrencyOptions(defaultCurrency);
        }

        private static List<SelectListItem> BuildLookupItems(IEnumerable<LookupItem> items, bool includeEmpty)
        {
            var result = new List<SelectListItem>();
            if (includeEmpty)
            {
                result.Add(new SelectListItem { Value = string.Empty, Text = string.Empty });
            }

            result.AddRange(items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Name
            }));

            return result;
        }

        private static IReadOnlyList<string> BuildCurrencyOptions(string defaultCurrency)
        {
            var items = new List<string>();

            if (!string.IsNullOrWhiteSpace(defaultCurrency))
            {
                items.Add(defaultCurrency.Trim().ToUpperInvariant());
            }

            foreach (var currency in new[] { "USD", "GBP", Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault })
            {
                if (!items.Contains(currency, StringComparer.OrdinalIgnoreCase))
                {
                    items.Add(currency);
                }
            }

            return items;
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

        private async Task EnsureProductDefaultsAsync(ProductEditorVm vm, CancellationToken ct)
        {
            var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);

            await PopulateProductLookupsAsync(vm, siteSettings.DefaultCulture, siteSettings.DefaultCurrency, ct).ConfigureAwait(false);
            await EnsureProductTranslationsAsync(vm, ct).ConfigureAwait(false);

            if (vm.Variants.Count == 0)
            {
                vm.Variants.Add(new ProductVariantCreateVm { Currency = siteSettings.DefaultCurrency });
            }

            EnsureVariantDefaults(vm, siteSettings.DefaultCurrency);
        }

        private static void EnsureVariantDefaults(ProductEditorVm vm, string defaultCurrency)
        {
            var currency = string.IsNullOrWhiteSpace(defaultCurrency)
                ? Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault
                : defaultCurrency.Trim().ToUpperInvariant();

            foreach (var variant in vm.Variants.Where(static x => string.IsNullOrWhiteSpace(x.Currency)))
            {
                variant.Currency = currency;
            }
        }

        private async Task EnsureProductTranslationsAsync(ProductEditorVm vm, CancellationToken ct)
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

            vm.Cultures = orderedCultures;
            vm.MultilingualEnabled = orderedCultures.Length > 1;
            vm.Translations = vm.Translations
                .Where(t => orderedCultures.Any(c => string.Equals(c, t.Culture, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(t => t.Culture, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            foreach (var culture in orderedCultures)
            {
                if (vm.Translations.All(x => !string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
                {
                    vm.Translations.Add(new ProductTranslationVm { Culture = culture });
                }
            }
        }

        private static List<ProductTranslationVm> FilterCompleteTranslations(IEnumerable<ProductTranslationVm> translations)
        {
            return translations
                .Where(static t =>
                    !string.IsNullOrWhiteSpace(t.Culture) &&
                    !string.IsNullOrWhiteSpace(t.Name) &&
                    !string.IsNullOrWhiteSpace(t.Slug))
                .ToList();
        }

        private OperationalPlaybookVm[] BuildProductPlaybooks()
        {
            return
            [
                new OperationalPlaybookVm
                {
                    QueueLabel = T("Inactive"),
                    WhyItMatters = T("ProductPlaybookInactiveScope"),
                    OperatorAction = T("ProductPlaybookInactiveAction")
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = T("Hidden"),
                    WhyItMatters = T("ProductPlaybookHiddenScope"),
                    OperatorAction = T("ProductPlaybookHiddenAction")
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = T("Scheduled"),
                    WhyItMatters = T("ProductPlaybookScheduledScope"),
                    OperatorAction = T("ProductPlaybookScheduledAction")
                }
            ];
        }
    }
}
