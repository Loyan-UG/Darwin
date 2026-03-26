using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.CMS.Media.Queries
{
    /// <summary>
    /// Returns a single media asset edit projection for the Admin portal.
    /// </summary>
    public sealed class GetMediaAssetForEditHandler
    {
        private readonly IAppDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMediaAssetForEditHandler"/> class.
        /// </summary>
        public GetMediaAssetForEditHandler(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Loads a non-deleted media asset for editing.
        /// </summary>
        public Task<MediaAssetEditItemDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return _db.Set<MediaAsset>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(x => new MediaAssetEditItemDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    Url = x.Url,
                    Alt = x.Alt,
                    Title = x.Title,
                    OriginalFileName = x.OriginalFileName,
                    SizeBytes = x.SizeBytes,
                    Width = x.Width,
                    Height = x.Height,
                    Role = x.Role,
                    ModifiedAtUtc = x.ModifiedAtUtc ?? x.CreatedAtUtc
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
