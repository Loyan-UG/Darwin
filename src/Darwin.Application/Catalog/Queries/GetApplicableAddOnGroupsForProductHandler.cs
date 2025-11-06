using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Queries
{
    /// <summary>
    /// Resolves the set of add-on groups applicable to a given product (and optionally a specific variant),
    /// following the precedence documented in the Domain/Application:
    /// Variant → Product -> Category (PrimaryCategoryId) -> Brand -> Global (IsGlobal = true).
    /// Only active groups and active values are returned.
    /// </summary>
    /// <remarks>
    /// This query is designed for Admin UI and Storefront composition:
    /// - Admin: preview which groups will be shown for a product.
    /// - Storefront: build the configuration UI for add-on selections.
    /// To keep reads efficient, the query projects directly into DTOs using EF-friendly Select().
    /// </remarks>
    public sealed class GetApplicableAddOnGroupsForProductHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes the handler with an <see cref="IAppDbContext"/> abstraction for EF Core.
        /// </summary>
        public GetApplicableAddOnGroupsForProductHandler(IAppDbContext db)
            => _db = db;

        /// <summary>
        /// Resolves applicable groups for the specified product (and optional variant).
        /// The variantId is reserved for future per-variant overrides; current model resolves at product/category/brand/global.
        /// </summary>
        /// <param name="productId">The target product identifier.</param>
        /// <param name="variantId">
        /// Optional variant identifier. Not required for current resolution but kept for forward-compatibility
        /// when variant-level attachments or overrides are introduced.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of <see cref="ApplicableAddOnGroupDto"/> ordered by group name then option/value sort orders.</returns>
        /// <exception cref="InvalidOperationException">Thrown if product does not exist or is soft-deleted.</exception>
        public async Task<IReadOnlyList<ApplicableAddOnGroupDto>> HandleAsync(
            Guid productId, Guid? variantId = null, CancellationToken ct = default)
        {
            // Load existence + joins for product-level precedence.
            var prod = await _db.Set<Product>()
                .AsNoTracking()
                .Where(p => p.Id == productId && !p.IsDeleted)
                .Select(p => new { p.Id, p.PrimaryCategoryId, p.BrandId })
                .FirstOrDefaultAsync(ct);

            if (prod is null)
                throw new InvalidOperationException("Product not found.");

            // 1) Variant-level (highest precedence) if provided
            var variantGroupIds = new List<Guid>();
            if (variantId.HasValue)
            {
                variantGroupIds = await _db.Set<AddOnGroupVariant>()
                    .AsNoTracking()
                    .Where(j => j.VariantId == variantId.Value && !j.IsDeleted)
                    .Select(j => j.AddOnGroupId)
                    .ToListAsync(ct);
            }

            // 2) Product-level
            var productGroupIds = await _db.Set<AddOnGroupProduct>()
                .AsNoTracking()
                .Where(j => j.ProductId == prod.Id && !j.IsDeleted)
                .Select(j => j.AddOnGroupId)
                .ToListAsync(ct);

            // 3) Category-level (primary)
            var categoryGroupIds = prod.PrimaryCategoryId.HasValue
                ? await _db.Set<AddOnGroupCategory>()
                    .AsNoTracking()
                    .Where(j => j.CategoryId == prod.PrimaryCategoryId.Value && !j.IsDeleted)
                    .Select(j => j.AddOnGroupId)
                    .ToListAsync(ct)
                : new List<Guid>();

            // 4) Brand-level
            var brandGroupIds = prod.BrandId.HasValue
                ? await _db.Set<AddOnGroupBrand>()
                    .AsNoTracking()
                    .Where(j => j.BrandId == prod.BrandId.Value && !j.IsDeleted)
                    .Select(j => j.AddOnGroupId)
                    .ToListAsync(ct)
                : new List<Guid>();

            // 5) Global groups
            var globalGroupIds = await _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => g.IsGlobal && !g.IsDeleted && g.IsActive)
                .Select(g => g.Id)
                .ToListAsync(ct);

            // Precedence: Variant → Product → Category → Brand → Global (distinct, keep first occurrence).
            var orderedDistinctGroupIds = variantGroupIds
                .Concat(productGroupIds)
                .Concat(categoryGroupIds)
                .Concat(brandGroupIds)
                .Concat(globalGroupIds)
                .Distinct()
                .ToList();

            if (orderedDistinctGroupIds.Count == 0)
                return Array.Empty<ApplicableAddOnGroupDto>();

            // Fetch groups + options + active values and project to DTOs.
            var groups = await _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => orderedDistinctGroupIds.Contains(g.Id) && !g.IsDeleted && g.IsActive)
                .Select(g => new ApplicableAddOnGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Currency = g.Currency,
                    SelectionMode = g.SelectionMode,
                    MinSelections = g.MinSelections,
                    MaxSelections = g.MaxSelections,
                    IsActive = g.IsActive,
                    Options = g.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new ApplicableAddOnOptionDto
                        {
                            Id = o.Id,
                            Label = o.Label,
                            SortOrder = o.SortOrder,
                            Values = o.Values
                                .Where(v => v.IsActive)
                                .OrderBy(v => v.SortOrder)
                                .Select(v => new ApplicableAddOnOptionValueDto
                                {
                                    Id = v.Id,
                                    Label = v.Label,
                                    PriceDeltaMinor = v.PriceDeltaMinor,
                                    Hint = v.Hint,
                                    SortOrder = v.SortOrder
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .ToListAsync(ct);

            // Preserve explicit precedence order; apply secondary Name ordering for stability.
            var orderIndex = orderedDistinctGroupIds
                .Select((id, idx) => new { id, idx })
                .ToDictionary(x => x.id, x => x.idx);

            var ordered = groups
                .OrderBy(g => orderIndex.TryGetValue(g.Id, out var idx) ? idx : int.MaxValue)
                .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return ordered;
        }
    }
}
