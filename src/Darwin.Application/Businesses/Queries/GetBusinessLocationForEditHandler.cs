using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Loads one business location for editing.
    /// </summary>
    public sealed class GetBusinessLocationForEditHandler
    {
        private readonly IAppDbContext _db;
        public GetBusinessLocationForEditHandler(IAppDbContext db) => _db = db;

        public async Task<BusinessLocationEditDto?> HandleAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new BusinessLocationEditDto
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    AddressLine1 = x.AddressLine1,
                    AddressLine2 = x.AddressLine2,
                    City = x.City,
                    Region = x.Region,
                    CountryCode = x.CountryCode,
                    PostalCode = x.PostalCode,
                    Coordinate = x.Coordinate == null
                        ? null
                        : new Darwin.Application.Common.DTOs.GeoCoordinateDto
                        {
                            Latitude = x.Coordinate.Latitude,
                            Longitude = x.Coordinate.Longitude,
                            AltitudeMeters = x.Coordinate.AltitudeMeters
                        },
                    IsPrimary = x.IsPrimary,
                    OpeningHoursJson = x.OpeningHoursJson,
                    InternalNote = x.InternalNote
                })
                .FirstOrDefaultAsync(ct);
        }
    }
}
