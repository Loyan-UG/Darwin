using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Media.Commands
{
    /// <summary>
    /// Performs a logical delete (soft delete) on a media asset to keep references intact.
    /// </summary>
    public sealed class SoftDeleteMediaAssetHandler
    {
        private readonly IAppDbContext _db;
        public SoftDeleteMediaAssetHandler(IAppDbContext db) => _db = db;

        public async Task HandleAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await _db.Set<MediaAsset>().FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
            if (entity == null) return;

            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
