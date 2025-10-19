using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Validators;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Soft-deletes an address. If the address was default billing/shipping, clears the flags.
    /// </summary>
    public sealed class SoftDeleteUserAddressHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddressDeleteDto> _validator;

        public SoftDeleteUserAddressHandler(IAppDbContext db, IValidator<AddressDeleteDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<Result> HandleAsync(AddressDeleteDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var address = await _db.Set<Address>().FirstOrDefaultAsync(a => a.Id == dto.Id && !a.IsDeleted, ct);
            if (address is null)
                return Result.Fail("Address not found.");

            // Concurrency check
            if (address.RowVersion is not null && dto.RowVersion is not null && address.RowVersion.Length > 0)
            {
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(address.RowVersion, dto.RowVersion))
                    return Result.Fail("Concurrency conflict.");
            }

            // Clear defaults if any
            address.IsDefaultBilling = false;
            address.IsDefaultShipping = false;

            // Soft delete
            address.IsDeleted = true;

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
