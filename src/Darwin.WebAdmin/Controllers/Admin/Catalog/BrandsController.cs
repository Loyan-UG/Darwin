using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Shared.Results;
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
    /// Admin controller for managing Brands.
    /// Uses paging on Index, form pages for Create/Edit, and a shared delete confirmation modal.
    /// All messages surface via TempData and the shared Alerts partial.
    /// </summary>
    public sealed class BrandsController : AdminBaseController
    {
        private readonly GetBrandsPageHandler _getPage;
        private readonly GetBrandOpsSummaryHandler _getBrandOpsSummary;
        private readonly GetBrandForEditHandler _getForEdit;
        private readonly CreateBrandHandler _create;
        private readonly UpdateBrandHandler _update;
        private readonly SoftDeleteBrandHandler _softDelete;

        public BrandsController(
            GetBrandsPageHandler getPage,
            GetBrandOpsSummaryHandler getBrandOpsSummary,
            GetBrandForEditHandler getForEdit,
            CreateBrandHandler create,
            UpdateBrandHandler update,
            SoftDeleteBrandHandler softDelete)
        {
            _getPage = getPage;
            _getBrandOpsSummary = getBrandOpsSummary;
            _getForEdit = getForEdit;
            _create = create;
            _update = update;
            _softDelete = softDelete;
        }

        /// <summary>
        /// Paged list of brands with a simple query (by name/slug if supported at query side).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20,
            string? query = null, string? filter = null, CancellationToken ct = default)
        {
            var (items, total) = await _getPage.HandleAsync(page, pageSize, "de-DE", query, filter, ct);
            var summary = await _getBrandOpsSummary.HandleAsync(ct);

            var vm = new BrandsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter ?? string.Empty,
                Summary = new BrandOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    UnpublishedCount = summary.UnpublishedCount,
                    MissingSlugCount = summary.MissingSlugCount,
                    MissingLogoCount = summary.MissingLogoCount
                },
                Playbooks = BuildBrandPlaybooks(),
                Items = items.Select(b => new BrandListItemVm
                {
                    Id = b.Id,
                    Name = b.Name,
                    Slug = b.Slug,
                    ModifiedAtUtc = b.ModifiedAtUtc,
                    RowVersion = b.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create() => View(new BrandEditVm
        {
            Translations = { new BrandTranslationVm { Culture = "de-DE" } }
        });

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureTranslations(vm);
                return RenderBrandEditor(vm, isCreate: true);
            }

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
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureTranslations(vm);
                return RenderBrandEditor(vm, isCreate: true);
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
            if (!ModelState.IsValid)
            {
                EnsureTranslations(vm);
                return RenderBrandEditor(vm, isCreate: false);
            }

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
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. The brand was modified by another process. Please reload and try again.";
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureTranslations(vm);
                return RenderBrandEditor(vm, isCreate: false);
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

        private IActionResult RenderBrandEditor(BrandEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            return isCreate
                ? View("Create", vm)
                : View("Edit", vm);
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

        private static void EnsureTranslations(BrandEditVm vm)
        {
            if (vm.Translations.Count == 0)
            {
                vm.Translations.Add(new BrandTranslationVm { Culture = "de-DE" });
            }
        }

        private static OperationalPlaybookVm[] BuildBrandPlaybooks()
        {
            return
            [
                new OperationalPlaybookVm
                {
                    QueueLabel = "Unpublished",
                    WhyItMatters = "Unpublished brands stay hidden from brand-driven navigation and merchandising flows.",
                    OperatorAction = "Review whether the brand should remain internal-only, then publish it once storefront exposure is intended."
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = "Missing Slug",
                    WhyItMatters = "Brands without slugs are harder to use in SEO-aware landing pages and direct support handoffs.",
                    OperatorAction = "Open the brand editor and add a stable slug before marketing or content teams link to the brand publicly."
                },
                new OperationalPlaybookVm
                {
                    QueueLabel = "Missing Logo",
                    WhyItMatters = "Brands without logos create a weak or inconsistent catalog presentation across admin and storefront surfaces.",
                    OperatorAction = "Review the brand asset setup and attach a media-library logo if the business expects visual brand presentation."
                }
            ];
        }
    }
}
