using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Identity;
using Darwin.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Identity.Commands
{
    /// <summary>
    /// Sets a specific address as default billing or shipping for the given user, ensuring uniqueness.
    /// </summary>
    public sealed class SetDefaultUserAddressHandler
    {
        private readonly IAppDbContext _db;

        public SetDefaultUserAddressHandler(IAppDbContext db) => _db = db;

        /// <summary>
        /// Sets default address for the user.
        /// </summary>
        /// <param name="userId">Owner user id.</param>
        /// <param name="addressId">Address id to set as default.</param>
        /// <param name="asBilling">When true, sets default billing.</param>
        /// <param name="asShipping">When true, sets default shipping.</param>
        public async Task<Result> HandleAsync(Guid userId, Guid addressId, bool asBilling, bool asShipping, CancellationToken ct = default)
        {
            if (userId == Guid.Empty) return Result.Fail("User id is required.");
            if (addressId == Guid.Empty) return Result.Fail("Address id is required.");
            if (!asBilling && !asShipping) return Result.Fail("Nothing to set.");

            var address = await _db.Set<Address>().FirstOrDefaultAsync(a => a.Id == addressId && !a.IsDeleted, ct);
            if (address is null || address.UserId != userId)
                return Result.Fail("Address not found or not owned by user.");

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
