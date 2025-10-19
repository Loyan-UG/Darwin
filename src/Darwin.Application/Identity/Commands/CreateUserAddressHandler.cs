using System;
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
    /// Creates a new address for a user. Optionally sets it as default billing/shipping.
    /// Ensures that only one default billing/shipping exists per user.
    /// </summary>
    public sealed class CreateUserAddressHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddressCreateDto> _validator;

        public CreateUserAddressHandler(IAppDbContext db, IValidator<AddressCreateDto> validator)
        {
            _db = db;
            _validator = validator;
        }

        public async Task<Result<Guid>> HandleAsync(AddressCreateDto dto, CancellationToken ct = default)
        {
            await _validator.ValidateAndThrowAsync(dto, ct);

            var userExists = await _db.Set<User>().AnyAsync(u => u.Id == dto.UserId && !u.IsDeleted, ct);
            if (!userExists)
                return Result<Guid>.Fail("User not found.");

            var address = new Address
            {
                UserId = dto.UserId,
                FullName = dto.FullName,
                Company = dto.Company,
                Street1 = dto.Street1,
                Street2 = dto.Street2,
                PostalCode = dto.PostalCode,
                City = dto.City,
                State = dto.State,
                CountryCode = dto.CountryCode,
                PhoneE164 = dto.PhoneE164,
                IsDefaultBilling = dto.IsDefaultBilling,
                IsDefaultShipping = dto.IsDefaultShipping
            };

            _db.Set<Address>().Add(address);

            // Enforce single default flags per user
            if (dto.IsDefaultBilling)
            {
                var others = _db.Set<Address>()
                    .Where(a => a.UserId == dto.UserId && !a.IsDeleted);
                await others.Where(a => a.IsDefaultBilling).ForEachAsync(a => a.IsDefaultBilling = false, ct);
                address.IsDefaultBilling = true;
            }
            if (dto.IsDefaultShipping)
            {
                var others = _db.Set<Address>()
                    .Where(a => a.UserId == dto.UserId && !a.IsDeleted);
                await others.Where(a => a.IsDefaultShipping).ForEachAsync(a => a.IsDefaultShipping = false, ct);
                address.IsDefaultShipping = true;
            }

            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Ok(address.Id);
        }
    }
}
