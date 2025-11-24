using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Commands
{
    /// <summary>
    /// Hard-deletes a <see cref="BusinessMedia"/> row.
    /// Intended for logic-managed / join-like data.
    /// </summary>
    public sealed class DeleteBusinessMediaHandler
    {
        private readonly IAppDbContext _db;
        public DeleteBusinessMediaHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<BusinessMedia>()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null) return;

            _db.Set<BusinessMedia>().Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
