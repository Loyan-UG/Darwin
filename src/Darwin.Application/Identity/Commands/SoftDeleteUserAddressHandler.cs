using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Soft-deletes an address. If the address was default billing/shipping, clears the flags.
    /// </summary>
    public sealed class SoftDeleteUserAddressHandler
    {
        private readonly IAppDbContext _db;
        private readonly IValidator<AddressDeleteDto> _validator;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SoftDeleteUserAddressHandler(IAppDbContext db, IValidator<AddressDeleteDto> validator, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _validator = validator;
            _localizer = localizer;
        }

        public async Task<Result> HandleAsync(AddressDeleteDto dto, CancellationToken ct = default)
        {
            var validation = await _validator.ValidateAsync(dto, ct);
            if (!validation.IsValid)
                return Result.Fail(dto.RowVersion is null || dto.RowVersion.Length == 0
                    ? _localizer["RowVersionRequired"]
                    : _localizer["InvalidDeleteRequest"]);

            var address = await _db.Set<Address>().FirstOrDefaultAsync(a => a.Id == dto.Id && !a.IsDeleted, ct);
            if (address is null)
                return Result.Fail(_localizer["AddressNotFound"]);

            var currentVersion = address.RowVersion ?? Array.Empty<byte>();
            if (!currentVersion.SequenceEqual(dto.RowVersion))
                return Result.Fail(_localizer["ConcurrencyConflict"]);

            address.IsDefaultBilling = false;
            address.IsDefaultShipping = false;
            address.IsDeleted = true;

            try
            {
                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail(_localizer["ConcurrencyConflict"]);
            }

            return Result.Ok();
        }
    }
}
