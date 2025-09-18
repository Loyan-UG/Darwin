using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Services
{
    /// <summary>
    /// Validates add-on selections and computes price deltas for a variant using the current Domain model:
    /// - Constraints (SelectionMode, MinSelections, MaxSelections, IsActive) live on AddOnGroup.
    /// - Values carry PriceDeltaMinor and IsActive on AddOnOptionValue.
    /// - Group assignment is via AddOnGroupProduct / AddOnGroupCategory / AddOnGroupBrand, plus IsGlobal.
    /// 
    /// Resolution precedence for applicable groups:
    ///   Product -> Category (PrimaryCategoryId) -> Brand -> Global (IsGlobal = true).
    /// </summary>
    public sealed class AddOnPricingService : IAddOnPricingService
    {
        private readonly IAppDbContext _db;

        public AddOnPricingService(IAppDbContext db) => _db = db;

        public async Task ValidateSelectionsForVariantAsync(Guid variantId, IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct)
        {
            var variant = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => v.Id == variantId && !v.IsDeleted)
                .Select(v => new { v.Id, v.ProductId })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Variant not found.");

            var product = await _db.Set<Product>()
                .AsNoTracking()
                .Where(p => p.Id == variant.ProductId && !p.IsDeleted)
                .Select(p => new { p.Id, p.PrimaryCategoryId, p.BrandId })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Owning product not found.");

            var groupIds = await ResolveApplicableGroupIdsAsync(product.Id, product.PrimaryCategoryId, product.BrandId, ct);
            if (groupIds.Count == 0)
            {
                if (selectedValueIds != null && selectedValueIds.Count > 0)
                    throw new InvalidOperationException("No add-ons are applicable to this product, but selections were provided.");
                return;
            }

            var optionsByGroup = await _db.Set<AddOnOption>()
                .AsNoTracking()
                .Where(o => groupIds.Contains(o.AddOnGroupId))
                .Select(o => new
                {
                    o.AddOnGroupId,
                    OptionId = o.Id,
                    ValueIds = _db.Set<AddOnOptionValue>()
                        .Where(v => v.AddOnOptionId == o.Id && !v.IsDeleted && v.IsActive)
                        .Select(v => v.Id)
                })
                .ToListAsync(ct);

            var groups = await _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => groupIds.Contains(g.Id) && !g.IsDeleted && g.IsActive)
                .Select(g => new
                {
                    g.Id,
                    g.SelectionMode,
                    g.MinSelections,
                    g.MaxSelections
                })
                .ToListAsync(ct);

            var allowedValueIds = new HashSet<Guid>(optionsByGroup.SelectMany(x => x.ValueIds));

            if (selectedValueIds != null && selectedValueIds.Count > 0)
            {
                foreach (var id in selectedValueIds)
                    if (!allowedValueIds.Contains(id))
                        throw new InvalidOperationException("Selected add-on value is not applicable to this product.");
            }

            var optionToGroup = optionsByGroup
                .GroupBy(x => x.AddOnGroupId)
                .SelectMany(g => g.Select(x => new { x.OptionId, GroupId = g.Key }))
                .ToDictionary(k => k.OptionId, v => v.GroupId);

            var valueToOption = await _db.Set<AddOnOptionValue>()
                .AsNoTracking()
                .Where(v => selectedValueIds.Contains(v.Id))
                .Select(v => new { v.Id, v.AddOnOptionId })
                .ToListAsync(ct);

            var selectedCountPerGroup = new Dictionary<Guid, int>();
            foreach (var sel in valueToOption)
            {
                if (!optionToGroup.TryGetValue(sel.AddOnOptionId, out var gid)) continue;
                if (!selectedCountPerGroup.ContainsKey(gid)) selectedCountPerGroup[gid] = 0;
                selectedCountPerGroup[gid]++;
            }

            foreach (var g in groups)
            {
                selectedCountPerGroup.TryGetValue(g.Id, out var count);

                var effectiveMax = g.SelectionMode == AddOnSelectionMode.Single
                    ? 1
                    : (g.MaxSelections ?? int.MaxValue);

                var min = Math.Max(0, g.MinSelections);

                if (count < min)
                    throw new InvalidOperationException("Selection does not meet minimum required choices for an option group.");

                if (count > effectiveMax)
                    throw new InvalidOperationException("Selection exceeds maximum allowed choices for an option group.");
            }
        }

        public async Task<long> SumPriceDeltasAsync(IReadOnlyCollection<Guid> selectedValueIds, CancellationToken ct)
        {
            if (selectedValueIds == null || selectedValueIds.Count == 0) return 0;

            var deltas = await _db.Set<AddOnOptionValue>()
                .AsNoTracking()
                .Where(v => selectedValueIds.Contains(v.Id) && !v.IsDeleted && v.IsActive)
                .Select(v => v.PriceDeltaMinor)
                .ToListAsync(ct);

            long sum = 0;
            foreach (var d in deltas) sum += d;
            return sum;
        }

        private async Task<List<Guid>> ResolveApplicableGroupIdsAsync(Guid productId, Guid? primaryCategoryId, Guid? brandId, CancellationToken ct)
        {
            var productGroupIds = await _db.Set<AddOnGroupProduct>()
                .AsNoTracking()
                .Where(x => x.ProductId == productId && !x.IsDeleted)
                .Select(x => x.AddOnGroupId)
                .Distinct()
                .ToListAsync(ct);
            if (productGroupIds.Count > 0) return productGroupIds;

            if (primaryCategoryId.HasValue)
            {
                var categoryGroupIds = await _db.Set<AddOnGroupCategory>()
                    .AsNoTracking()
                    .Where(x => x.CategoryId == primaryCategoryId.Value && !x.IsDeleted)
                    .Select(x => x.AddOnGroupId)
                    .Distinct()
                    .ToListAsync(ct);
                if (categoryGroupIds.Count > 0) return categoryGroupIds;
            }

            if (brandId.HasValue)
            {
                var brandGroupIds = await _db.Set<AddOnGroupBrand>()
                    .AsNoTracking()
                    .Where(x => x.BrandId == brandId.Value && !x.IsDeleted)
                    .Select(x => x.AddOnGroupId)
                    .Distinct()
                    .ToListAsync(ct);
                if (brandGroupIds.Count > 0) return brandGroupIds;
            }

            var globalGroupIds = await _db.Set<AddOnGroup>()
                .AsNoTracking()
                .Where(g => g.IsGlobal && g.IsActive && !g.IsDeleted)
                .Select(g => g.Id)
                .Distinct()
                .ToListAsync(ct);

            return globalGroupIds;
        }
    }
}
