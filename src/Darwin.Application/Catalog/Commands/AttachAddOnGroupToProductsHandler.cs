using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Performs a hard-replace of product links for an add-on group.
    /// This implementation aligns with the Brand attach behavior:
    /// - Validates group existence and optimistic concurrency (RowVersion).
    /// - Validates all requested Product IDs exist (and are not soft-deleted).
    /// - Physically deletes all existing links for the group (including soft-deleted rows)
    ///   and then inserts the requested set, preventing unique index collisions.
    /// </summary>
    public sealed class AttachAddOnGroupToProductsHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddOnGroupAttachToProductsDto> _validator;

        /// <summary>
        /// Initializes a new instance of the handler.
        /// </summary>
        public AttachAddOnGroupToProductsHandler(
            IAppDbContext db,
            IValidator<AddOnGroupAttachToProductsDto> validator)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Replaces the set of attached products for the specified add-on group (hard delete + insert).
        /// </summary>
        public async Task<Result> HandleAsync(AddOnGroupAttachToProductsDto dto, CancellationToken ct = default)
        {
            // 1) Basic validation
            await _validator.ValidateAndThrowAsync(dto, ct);

            // 2) Load group (existence + concurrency check)
            var group = await _db.Set<AddOnGroup>()
                .Where(g => g.Id == dto.AddOnGroupId && !g.IsDeleted)
                .Select(g => new { g.Id, g.RowVersion })
                .FirstOrDefaultAsync(ct);

            if (group is null)
                return Result.Fail("Add-on group not found.");

            if (dto.RowVersion is null || !dto.RowVersion.SequenceEqual(group.RowVersion))
                return Result.Fail("The add-on group was modified by another operation. Please reload and retry.");

            // 3) Normalize and validate requested products
            var requested = (dto.ProductIds ?? Array.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            if (requested.Length == 0)
            {
                // Hard-replace to empty set: just remove all existing links.
                var existingNone = await _db.Set<AddOnGroupProduct>()
                    .IgnoreQueryFilters()
                    .Where(x => x.AddOnGroupId == group.Id)
                    .ToListAsync(ct);

                if (existingNone.Count > 0)
                {
                    _db.Set<AddOnGroupProduct>().RemoveRange(existingNone);
                    await _db.SaveChangesAsync(ct);
                }
                return Result.Ok();
            }

            var validProductIds = await _db.Set<Product>()
                .AsNoTracking()
                .Where(p => requested.Contains(p.Id) && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync(ct);

            if (validProductIds.Count != requested.Length)
                return Result.Fail("One or more products were not found or are deleted.");

            // 4) Hard delete ALL existing links for this group (active + soft-deleted)
            var existing = await _db.Set<AddOnGroupProduct>()
                .IgnoreQueryFilters()
                .Where(x => x.AddOnGroupId == group.Id)
                .ToListAsync(ct);

            if (existing.Count > 0)
                _db.Set<AddOnGroupProduct>().RemoveRange(existing);

            // 5) Insert the requested set
            var toAdd = validProductIds.Select(pid => new AddOnGroupProduct
            {
                AddOnGroupId = group.Id,
                ProductId = pid
            });

            _db.Set<AddOnGroupProduct>().AddRange(toAdd);

            // 6) Commit
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
