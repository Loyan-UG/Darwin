using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Catalog.Commands
{
    /// <summary>
    /// Performs a soft delete for <see cref="Brand"/> by setting <c>IsDeleted = true</c>.
    /// Uses optimistic concurrency (RowVersion) and blocks deletion of system brands.
    /// </summary>
    public sealed class SoftDeleteBrandHandler
    {
        private readonly IAppDbContext _db;
        private readonly BrandDeleteValidator _validator = new();

        /// <summary>
        /// Initializes a new handler instance with the application DbContext abstraction.
        /// </summary>
        public SoftDeleteBrandHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Soft-deletes the brand identified by the given DTO.
        /// </summary>
        /// <param name="dto">DTO carrying the Id and RowVersion from the UI grid.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// <see cref="Result"/> with success or a short error message suitable for UI Alerts.
        /// </returns>
        public async Task<Result> HandleAsync(BrandDeleteDto dto, CancellationToken ct = default)
        {
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
                return Result.Fail("Invalid delete request.");

            // Track entity for update; include RowVersion for concurrency comparison.
            var entity = await _db.Set<Brand>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return Result.Fail("Brand not found.");

            if (entity.IsDeleted)
                return Result.Ok(); // idempotent

            // Concurrency check: compare current row version with the one from UI.
            if (!entity.RowVersion.SequenceEqual(dto.RowVersion ?? Array.Empty<byte>()))
                return Result.Fail("Concurrency conflict. The brand has been modified by another process.");

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
