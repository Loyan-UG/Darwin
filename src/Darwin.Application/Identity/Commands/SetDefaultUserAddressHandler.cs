using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Sets a specific address as default billing or shipping for the given user, ensuring uniqueness.
    /// </summary>
    public sealed class SetDefaultUserAddressHandler
    {
        private readonly IAppDbContext _db;
        private readonly IStringLocalizer<ValidationResource> _localizer;

        public SetDefaultUserAddressHandler(IAppDbContext db, IStringLocalizer<ValidationResource> localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        /// <summary>
        /// Sets default address for the user.
        /// </summary>
        /// <param name="userId">Owner user id.</param>
        /// <param name="addressId">Address id to set as default.</param>
        /// <param name="asBilling">When true, sets default billing.</param>
        /// <param name="asShipping">When true, sets default shipping.</param>
        public async Task<Result> HandleAsync(Guid userId, Guid addressId, bool asBilling, bool asShipping, CancellationToken ct = default)
        {
            if (userId == Guid.Empty) return Result.Fail(_localizer["UserIdRequired"]);
            if (addressId == Guid.Empty) return Result.Fail(_localizer["AddressIdRequired"]);
            if (!asBilling && !asShipping) return Result.Fail(_localizer["NothingToSet"]);

            var address = await _db.Set<Address>().FirstOrDefaultAsync(a => a.Id == addressId && !a.IsDeleted, ct);
            if (address is null || address.UserId != userId)
                return Result.Fail(_localizer["AddressNotOwnedByUser"]);

            if (asBilling)
            {
                var others = _db.Set<Address>().Where(a => a.UserId == userId && !a.IsDeleted);
                await others.Where(a => a.IsDefaultBilling).ForEachAsync(a => a.IsDefaultBilling = false, ct);
                address.IsDefaultBilling = true;
            }

            if (asShipping)
            {
                var others = _db.Set<Address>().Where(a => a.UserId == userId && !a.IsDeleted);
                await others.Where(a => a.IsDefaultShipping).ForEachAsync(a => a.IsDefaultShipping = false, ct);
                address.IsDefaultShipping = true;
            }

            await _db.SaveChangesAsync(ct);
            return Result.Ok();
        }
    }
}
