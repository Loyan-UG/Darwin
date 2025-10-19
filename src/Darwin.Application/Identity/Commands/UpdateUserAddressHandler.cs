using System.Collections;
using System.Linq;
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
    /// Updates an existing address with optimistic concurrency.
    /// Also maintains the uniqueness of default billing/shipping per user.
    /// </summary>
    public sealed class UpdateUserAddressHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddressEditDto> _validator;

        public UpdateUserAddressHandler(IAppDbContext db, IValidator<AddressEditDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<Result> HandleAsync(AddressEditDto dto, CancellationToken ct = default)
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

            // Update fields
            address.FullName = dto.FullName;
            address.Company = dto.Company;
            address.Street1 = dto.Street1;
            address.Street2 = dto.Street2;
            address.PostalCode = dto.PostalCode;
            address.City = dto.City;
            address.State = dto.State;
            address.CountryCode = dto.CountryCode;
            address.PhoneE164 = dto.PhoneE164;

            // Handle default flags uniqueness per user
            if (address.UserId is not null)
            {
                if (dto.IsDefaultBilling && !address.IsDefaultBilling)
                {
                    var others = _db.Set<Address>().Where(a => a.UserId == address.UserId && !a.IsDeleted);
                    await others.Where(a => a.IsDefaultBilling).ForEachAsync(a => a.IsDefaultBilling = false, ct);
                    address.IsDefaultBilling = true;
                }
                else if (!dto.IsDefaultBilling && address.IsDefaultBilling)
                {
                    address.IsDefaultBilling = false;
                }

                if (dto.IsDefaultShipping && !address.IsDefaultShipping)
                {
                    var others = _db.Set<Address>().Where(a => a.UserId == address.UserId && !a.IsDeleted);
                    await others.Where(a => a.IsDefaultShipping).ForEachAsync(a => a.IsDefaultShipping = false, ct);
                    address.IsDefaultShipping = true;
                }
                else if (!dto.IsDefaultShipping && address.IsDefaultShipping)
                {
                    address.IsDefaultShipping = false;
                }
            }
            else
            {
                // No owner: ensure default flags are not set
                address.IsDefaultBilling = false;
                address.IsDefaultShipping = false;
            }

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
