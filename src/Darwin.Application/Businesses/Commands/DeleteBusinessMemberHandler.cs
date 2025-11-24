using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Hard-deletes a <see cref="BusinessMember"/> link.
    /// </summary>
    public sealed class DeleteBusinessMemberHandler
    {
        private readonly IAppDbContext _db;
        public DeleteBusinessMemberHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<BusinessMember>()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null) return;

            _db.Set<BusinessMember>().Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
