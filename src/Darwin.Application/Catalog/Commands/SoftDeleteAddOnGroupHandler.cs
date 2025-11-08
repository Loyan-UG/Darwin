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
    /// Performs a soft delete for <see cref="AddOnGroup"/> by setting <c>IsDeleted = true</c>.
    /// Uses optimistic concurrency (RowVersion) to prevent unintended overwrites from stale grids.
    /// The operation is idempotent: attempting to delete an already-deleted row succeeds.
    /// </summary>
    public sealed class SoftDeleteAddOnGroupHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddOnGroupDeleteDto> _validator;

        /// <summary>
        /// Initializes a new handler instance with the application DbContext abstraction and validator.
        /// </summary>
        public SoftDeleteAddOnGroupHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _validator = new AddOnGroupDeleteValidator();
        }

        /// <summary>
        /// Marks an add-on group as deleted after concurrency checks. 
        /// Does not hard-delete related options/values or assignments; global query filters hide them.
        /// </summary>
        /// <param name="dto">Delete request (Id + RowVersion) coming from the Admin grid.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result indicating success or a friendly error suitable for UI Alerts.</returns>
        public async Task<Result> HandleAsync(AddOnGroupDeleteDto dto, CancellationToken ct = default)
        {
            // 1) Basic input validation (Id + RowVersion)
            var vr = _validator.Validate(dto);
            if (!vr.IsValid)
                return Result.Fail("Invalid delete request.");

            // 2) Load entity with tracking to update IsDeleted and compare RowVersion
            var entity = await _db.Set<AddOnGroup>()
                .FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

            if (entity is null)
                return Result.Fail("Add-on group not found.");

            // 3) Idempotency: deleting twice is OK
            if (entity.IsDeleted)
                return Result.Ok();

            // 4) Concurrency guard: RowVersion must match the current value
            var currentVersion = entity.RowVersion ?? Array.Empty<byte>();
            var requestVersion = dto.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(requestVersion))
                return Result.Fail("Concurrency conflict. The item was modified by another process.");

            // 5) Soft delete the aggregate root. 
            //    EF global query filter will hide this and any dependent rows still referencing it.
            entity.IsDeleted = true;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
