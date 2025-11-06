using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Replaces the set of variants attached to an add-on group (upsert semantics).
    /// Enforces:
    /// - Group existence + RowVersion match (manual check as IAppDbContext has no Entry(..)).
    /// - VariantIds validity (existing, not soft-deleted).
    /// - Efficient set-diff: add missing, soft-delete removed.
    /// </summary>
    public sealed class AttachAddOnGroupToVariantsHandler
    {
        private readonly IAppDbContext _db;
        private readonly AddOnGroupAttachToVariantsValidator _validator = new();

        public AttachAddOnGroupToVariantsHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Performs a replace operation for variant links of the given group.
        /// </summary>
        public async Task<Result> HandleAsync(AddOnGroupAttachToVariantsDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid) return Result.Fail("Invalid payload.");

            // 1) Group must exist + concurrency guard.
            var group = await _db.Set<AddOnGroup>()
                .Where(g => g.Id == dto.AddOnGroupId && !g.IsDeleted)
                .Select(g => new { g.Id, g.RowVersion })
                .FirstOrDefaultAsync(ct);

            if (group is null)
                return Result.Fail("Add-on group not found.");

            if (!dto.RowVersion.SequenceEqual(group.RowVersion))
                return Result.Fail("Concurrency conflict: the add-on group was modified. Please reload and retry.");

            // 2) Validate variant ids exist (and not soft-deleted).
            var validVariantIds = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => dto.VariantIds.Contains(v.Id) && !v.IsDeleted)
                .Select(v => v.Id)
                .ToListAsync(ct);

            if (validVariantIds.Count != dto.VariantIds.Distinct().Count())
                return Result.Fail("One or more variants were not found or are deleted.");

            // 3) Load current active links for set-diff.
            var existing = await _db.Set<AddOnGroupVariant>()
                .Where(x => x.AddOnGroupId == group.Id && !x.IsDeleted)
                .Select(x => new { x.Id, x.VariantId })
                .ToListAsync(ct);

            var existingSet = existing.Select(e => e.VariantId).ToHashSet();
            var requestedSet = validVariantIds.ToHashSet();

            var toAdd = requestedSet.Except(existingSet).ToList();
            var toRemove = existing.Where(e => !requestedSet.Contains(e.VariantId)).Select(e => e.Id).ToList();

            if (toAdd.Count > 0)
            {
                foreach (var vid in toAdd)
                {
                    _db.Set<AddOnGroupVariant>().Add(new AddOnGroupVariant
                    {
                        AddOnGroupId = group.Id,
                        VariantId = vid
                    });
                }
            }

            if (toRemove.Count > 0)
            {
                var removeRows = await _db.Set<AddOnGroupVariant>()
                    .Where(x => toRemove.Contains(x.Id))
                    .ToListAsync(ct);

                // Soft-delete via BaseEntity mapping; auditing handled in DbContext.
                foreach (var r in removeRows) r.IsDeleted = true;
            }

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
