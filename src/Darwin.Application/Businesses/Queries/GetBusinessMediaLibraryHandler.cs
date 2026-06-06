using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries;

public sealed class GetBusinessMediaLibraryHandler
{
    private readonly IAppDbContext _db;

    public GetBusinessMediaLibraryHandler(IAppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<BusinessMediaLibraryDto?> HandleAsync(Guid businessId, CancellationToken ct = default)
    {
        if (businessId == Guid.Empty)
        {
            return null;
        }

        var business = await _db.Set<Business>()
            .AsNoTracking()
            .Where(x => x.Id == businessId && !x.IsDeleted)
            .Select(x => new { x.Id, x.BrandLogoUrl })
            .SingleOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (business is null)
        {
            return null;
        }

        var gallery = await _db.Set<BusinessMedia>()
            .AsNoTracking()
            .Where(x => x.BusinessId == businessId && !x.IsDeleted)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(x => new BusinessMediaItemDto
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                BusinessLocationId = x.BusinessLocationId,
                Url = x.Url,
                Caption = x.Caption,
                SortOrder = x.SortOrder,
                IsPrimary = x.IsPrimary,
                RowVersion = x.RowVersion ?? Array.Empty<byte>()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new BusinessMediaLibraryDto
        {
            BusinessId = business.Id,
            ProfileImageUrl = business.BrandLogoUrl,
            Gallery = gallery
        };
    }
}
