using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Queries
{
    /// <summary>
    /// Loads a user for admin editing together with the user's addresses.
    /// </summary>
    public sealed class GetUserWithAddressesForEditHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public GetUserWithAddressesForEditHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        /// <summary>
        /// Loads user edit fields and related addresses in one call.
        /// </summary>
        public async Task<Result<UserWithAddressesEditDto>> HandleAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _db.Set<User>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);

            if (user is null)
                return Result<UserWithAddressesEditDto>.Fail(_localizer["UserNotFound"]);

            var addresses = await _db.Set<Address>()
                .AsNoTracking()
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .OrderByDescending(a => a.IsDefaultShipping)
                .ThenByDescending(a => a.IsDefaultBilling)
                .ThenBy(a => a.City)
                .Select(a => new AddressListItemDto
                {
                    Id = a.Id,
                    RowVersion = a.RowVersion,
                    FullName = a.FullName,
                    Company = a.Company,
                    Street1 = a.Street1,
                    Street2 = a.Street2,
                    PostalCode = a.PostalCode,
                    City = a.City,
                    State = a.State,
                    CountryCode = a.CountryCode,
                    PhoneE164 = a.PhoneE164,
                    IsDefaultBilling = a.IsDefaultBilling,
                    IsDefaultShipping = a.IsDefaultShipping
                })
                .ToArrayAsync(ct);

            var dto = new UserWithAddressesEditDto
            {
                Id = user.Id,
                RowVersion = user.RowVersion,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEndUtc = user.LockoutEndUtc,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Locale = user.Locale,
                Timezone = user.Timezone,
                Currency = user.Currency,
                PhoneE164 = user.PhoneE164,
                IsActive = user.IsActive,
                Addresses = addresses
            };

            return Result<UserWithAddressesEditDto>.Ok(dto);
        }
    }
}
