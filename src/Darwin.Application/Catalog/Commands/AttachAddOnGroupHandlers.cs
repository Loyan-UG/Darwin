using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Replaces the set of products attached to an add-on group (upsert semantics).
    /// This handler enforces:
    /// - Group existence and concurrency (RowVersion).
    /// - ProductIds validity (existing, not soft-deleted).
    /// - Efficient set-diff to add new links and soft-delete removed ones.
    /// </summary>
    public sealed class AttachAddOnGroupToProductsHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddOnGroupAttachToProductsDto> _validator;

        public AttachAddOnGroupToProductsHandler(IAppDbContext db, IValidator<AddOnGroupAttachToProductsDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        /// <summary>
        /// Performs a replace operation:
        /// Existing active links for the group are compared to the requested set.
        /// New ones are inserted; missing ones are soft-deleted.
        /// </summary>
        public async Task<Result> HandleAsync(AddOnGroupAttachToProductsDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            // Load the group with RowVersion for optimistic concurrency check.
            var group = await _db.Set<AddOnGroup>()
                .Where(g => g.Id == dto.AddOnGroupId && !g.IsDeleted)
                .Select(g => new { g.Id, g.RowVersion })
                .FirstOrDefaultAsync(ct);

            if (group is null)
                return Result.Fail("Add-on group not found.");

            // Manual RowVersion check, because IAppDbContext does not expose Entry(..).
            if (!dto.RowVersion.SequenceEqual(group.RowVersion))
                return Result.Fail("The add-on group was modified by another operation. Please reload and retry.");

            // Validate product ids exist and are not soft-deleted.
            var validProductIds = await _db.Set<Product>()
                .AsNoTracking()
                .Where(p => dto.ProductIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (validProductIds.Count != dto.ProductIds.Distinct().Count())
                return Result.Fail("One or more products were not found or are deleted.");

            // Current active links for the group.
            var existing = await _db.Set<AddOnGroupProduct>()
                .Where(x => x.AddOnGroupId == group.Id && !x.IsDeleted)
                .Select(x => new { x.Id, x.ProductId })
                .ToListAsync(ct);

            var existingSet = existing.Select(e => e.ProductId).ToHashSet();
            var requestedSet = validProductIds.ToHashSet();

            // To add: requested - existing
            var toAdd = requestedSet.Except(existingSet).ToList();

            // To remove (soft delete): existing - requested
            var toRemove = existing.Where(e => !requestedSet.Contains(e.ProductId)).Select(e => e.Id).ToList();

            if (toAdd.Count > 0)
            {
                foreach (var pid in toAdd)
                {
                    // Join entity has public setters per Domain dump; soft delete handled globally.
                    _db.Set<AddOnGroupProduct>().Add(new AddOnGroupProduct
                    {
                        AddOnGroupId = group.Id,
                        ProductId = pid
                    });
                }
            }

            if (toRemove.Count > 0)
            {
                var removeRows = await _db.Set<AddOnGroupProduct>()
                    .Where(x => toRemove.Contains(x.Id))
                    .ToListAsync(ct);

                // Soft delete (IsDeleted) is mapped by BaseEntity; auditing occurs in DbContext.
                foreach (var r in removeRows)
                    r.IsDeleted = true;
            }

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }


    /// <summary>
    /// Attaches an add-on group to categories.
    /// </summary>
    public sealed class AttachAddOnGroupToCategoriesHandler
    {
        private readonly IAppDbContext _db;
        public AttachAddOnGroupToCategoriesHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid groupId, IEnumerable<Guid> categoryIds, CancellationToken ct = default)
        {
            var groupExists = await _db.Set<AddOnGroup>().AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!groupExists) throw new InvalidOperationException("Add-on group not found.");

            var existing = await _db.Set<AddOnGroupCategory>()
                .Where(x => x.AddOnGroupId == groupId)
                .ToListAsync(ct);

            _db.Set<AddOnGroupCategory>().RemoveRange(existing);

            var toAdd = categoryIds.Distinct().Select(cid => new AddOnGroupCategory
            {
                AddOnGroupId = groupId,
                CategoryId = cid
            });

            _db.Set<AddOnGroupCategory>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Attaches an add-on group to brands.
    /// </summary>
    public sealed class AttachAddOnGroupToBrandsHandler
    {
        private readonly IAppDbContext _db;
        public AttachAddOnGroupToBrandsHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid groupId, IEnumerable<Guid> brandIds, CancellationToken ct = default)
        {
            var groupExists = await _db.Set<AddOnGroup>().AnyAsync(g => g.Id == groupId && !g.IsDeleted, ct);
            if (!groupExists) throw new InvalidOperationException("Add-on group not found.");

            var existing = await _db.Set<AddOnGroupBrand>()
                .Where(x => x.AddOnGroupId == groupId)
                .ToListAsync(ct);

            _db.Set<AddOnGroupBrand>().RemoveRange(existing);

            var toAdd = brandIds.Distinct().Select(bid => new AddOnGroupBrand
            {
                AddOnGroupId = groupId,
                BrandId = bid
            });

            _db.Set<AddOnGroupBrand>().AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
        }
    }
}
