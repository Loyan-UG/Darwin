using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Services
{
    /// <summary>
    /// Validates add-on selections and computes price deltas for a variant using the current Domain model:
    /// - Constraints (SelectionMode, MinSelections, MaxSelections, IsActive) live on AddOnGroup.
    /// - Values carry PriceDeltaMinor and IsActive on AddOnOptionValue.
    /// - Group assignment is via AddOnGroupVariant / AddOnGroupProduct / AddOnGroupCategory / AddOnGroupBrand, plus IsGlobal.
    /// 
    /// Resolution precedence for applicable groups:
    ///   Variant → Product → Category (PrimaryCategoryId) → Brand → Global (IsGlobal = true).
    /// </summary>
    public sealed class AddOnPricingService : IAddOnPricingService
    {
        private readonly IAppDbContext _db;

        public AddOnPricingService(IAppDbContext db) => _db = db;

        /// <inheritdoc />
        /// <inheritdoc />
        public async Task ValidateSelectionsForVariantAsync(Guid variantId, IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct)
        {
            // Load minimal variant link (Id + ProductId) and ensure not deleted.
            var variant = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => v.Id == variantId && !v.IsDeleted)
                .Select(v => new { v.Id, v.ProductId })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Variant not found.");

            // Load minimal product link (PrimaryCategoryId + BrandId).
            var product = await _db.Set<Product>()
                .AsNoTracking()
                .Where(p => p.Id == variant.ProductId && !p.IsDeleted)
                .Select(p => new { p.Id, p.PrimaryCategoryId, p.BrandId })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Owning product not found.");

            // Resolve all applicable group IDs (precedence: Variant → Product → Category → Brand → Global)
            var groupIds = await ResolveApplicableGroupIdsAsync(
                variantId: variant.Id,
                productId: product.Id,
                primaryCategoryId: product.PrimaryCategoryId,
                brandId: product.BrandId,
                ct: ct);

            if (groupIds.Count == 0)
            {
                if (selectedValueIds != null && selectedValueIds.Count > 0)
                    throw new InvalidOperationException("No add-ons are applicable to this product, but selections were provided.");
                return;
            }

            // Load all active groups with constraints and their options/values.
            var groups = await _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Include(g => g.Options)
                    .ThenInclude(o => o.Values)
                .Where(g => groupIds.Contains(g.Id) && !g.IsDeleted && g.IsActive)
                .ToListAsync(ct);

            // Map all valid value IDs by group
            var validValuesByGroup = groups
                .SelectMany(g => g.Options)
                .SelectMany(o => o.Values.Select(v => new { g = o.AddOnGroupId, v.Id }))
                .GroupBy(x => x.g)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToHashSet());

            // 1. Membership & Active checks
            var allValidValues = validValuesByGroup.SelectMany(x => x.Value).ToHashSet();
            if (selectedValueIds.Any(id => !allValidValues.Contains(id)))
                throw new InvalidOperationException("One or more selected add-on values do not belong to applicable groups.");

            // 2. Group-level constraints
            var selectedValuesGrouped = selectedValueIds
                .Select(id => new { id, g = validValuesByGroup.FirstOrDefault(kvp => kvp.Value.Contains(id)).Key })
                .GroupBy(x => x.g)
                .ToDictionary(g => g.Key, g => g.Select(x => x.id).ToList());

            foreach (var group in groups)
            {
                selectedValuesGrouped.TryGetValue(group.Id, out var selected);
                var count = selected?.Count ?? 0;
                var min = group.MinSelections;
                var max = group.MaxSelections ?? int.MaxValue;

                // MinSelections check
                if (count < min)
                    throw new InvalidOperationException($"Add-on group '{group.Name}' requires at least {min} selection(s).");

                // MaxSelections check
                if (count > max)
                    throw new InvalidOperationException($"Add-on group '{group.Name}' allows at most {max} selection(s).");

                // SelectionMode check
                if (group.SelectionMode == AddOnSelectionMode.Single && count > 1)
                    throw new InvalidOperationException($"Add-on group '{group.Name}' allows only one selection (single-choice).");
            }

            // 3. Duplicate checks (rare at DTO level, but safe)
            if (selectedValueIds.Count != selectedValueIds.Distinct().Count())
                throw new InvalidOperationException("Duplicate add-on selections are not allowed.");

        }




        /// <inheritdoc />
        public async Task<long> SumPriceDeltasAsync(IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct)
        {
            if (selectedValueIds == null || selectedValueIds.Count == 0) return 0;

            var sum = await _db.Set<AddOnOptionValue>()
                .AsNoTracking()
                .Where(v => selectedValueIds.Contains(v.Id) && !v.IsDeleted && v.IsActive)
                .SumAsync(v => (long?)v.PriceDeltaMinor, ct) ?? 0;

            return sum;
        }

        /// <summary>
        /// Resolves all applicable add-on group IDs by precedence:
        /// Variant → Product → Category → Brand → Global.
        /// Returns a distinct, precedence-ordered list of group IDs.
        /// </summary>
        private async Task<List<Guid>> ResolveApplicableGroupIdsAsync(
            Guid variantId,
            Guid productId,
            Guid? primaryCategoryId,
            Guid? brandId,
            CancellationToken ct)
        {
            // 1) Variant-level group attachments
            var variantGroupIds = await _db.Set<AddOnGroupVariant>()
                .AsNoTracking()
                .Where(j => j.VariantId == variantId && !j.IsDeleted)
                .Select(j => j.AddOnGroupId)
                .ToListAsync(ct);

            // 2) Product-level group attachments
            var productGroupIds = await _db.Set<AddOnGroupProduct>()
                .AsNoTracking()
                .Where(j => j.ProductId == productId && !j.IsDeleted)
                .Select(j => j.AddOnGroupId)
                .ToListAsync(ct);

            // 3) Category-level group attachments (primary category)
            var categoryGroupIds = primaryCategoryId.HasValue
                ? await _db.Set<AddOnGroupCategory>()
                    .AsNoTracking()
                    .Where(j => j.CategoryId == primaryCategoryId.Value && !j.IsDeleted)
                    .Select(j => j.AddOnGroupId)
                    .ToListAsync(ct)
                : new List<Guid>();

            // 4) Brand-level group attachments
            var brandGroupIds = brandId.HasValue
                ? await _db.Set<AddOnGroupBrand>()
                    .AsNoTracking()
                    .Where(j => j.BrandId == brandId.Value && !j.IsDeleted)
                    .Select(j => j.AddOnGroupId)
                    .ToListAsync(ct)
                : new List<Guid>();

            // 5) Global groups (active only)
            var globalGroupIds = await _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => g.IsGlobal && !g.IsDeleted && g.IsActive)
                .Select(g => g.Id)
                .ToListAsync(ct);

            // Merge precedence with stable distinct semantics (first occurrence wins).
            var orderedDistinct = variantGroupIds
                .Concat(productGroupIds)
                .Concat(categoryGroupIds)
                .Concat(brandGroupIds)
                .Concat(globalGroupIds)
                .Distinct()
                .ToList();

            return orderedDistinct;
        }
    }
}
