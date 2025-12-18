using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common.DTOs;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Loads a public business detail view for consumer/mobile usage.
    /// Includes locations, gallery images and the currently active loyalty program (if any).
    /// </summary>
    public sealed class GetBusinessPublicDetailHandler
    {
        private readonly IAppDbContext _db;

        public GetBusinessPublicDetailHandler(IAppDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        /// <summary>
        /// Returns a public business detail model by business id.
        /// If the business does not exist or is not active, returns null.
        /// </summary>
        public async Task<BusinessPublicDetailDto?> HandleAsync(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                return null;
            }

            // Load the business core (discovery only shows active businesses).
            var business = await _db.Set<Business>()
                .AsNoTracking()
                .Where(b => b.Id == businessId && b.IsActive)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.ShortDescription,
                    b.WebsiteUrl,
                    b.ContactEmail,
                    b.ContactPhoneE164,
                    b.Category,
                    b.IsActive,
                    b.DefaultCurrency,
                    b.DefaultCulture
                })
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (business == null)
            {
                return null;
            }

            // Locations (primary first).
            var locations = await _db.Set<BusinessLocation>()
                .AsNoTracking()
                .Where(l => l.BusinessId == businessId)
                .OrderByDescending(l => l.IsPrimary)
                .ThenBy(l => l.Name)
                .Select(l => new BusinessPublicLocationDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    AddressLine1 = l.AddressLine1,
                    AddressLine2 = l.AddressLine2,
                    City = l.City,
                    Region = l.Region,
                    CountryCode = l.CountryCode,
                    PostalCode = l.PostalCode,
                    Coordinate = l.Coordinate == null
                        ? null
                        : new GeoCoordinateDto
                        {
                            Latitude = l.Coordinate.Latitude,
                            Longitude = l.Coordinate.Longitude,
                            AltitudeMeters = l.Coordinate.AltitudeMeters
                        },
                    IsPrimary = l.IsPrimary,
                    OpeningHoursJson = l.OpeningHoursJson
                })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Media: primary first, then ordered gallery.
            var media = await _db.Set<BusinessMedia>()
                .AsNoTracking()
                .Where(m => m.BusinessId == businessId)
                .OrderByDescending(m => m.IsPrimary)
                .ThenBy(m => m.SortOrder)
                .Select(m => new { m.Url, m.IsPrimary })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var primaryImageUrl = media.FirstOrDefault(x => x.IsPrimary)?.Url;
            var gallery = media.Select(x => x.Url).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

            // Loyalty program (public): only active program is returned.
            var program = await _db.Set<LoyaltyProgram>()
                .AsNoTracking()
                .Where(p => p.BusinessId == businessId && p.IsActive)
                .Select(p => new LoyaltyProgramPublicDto
                {
                    Id = p.Id,
                    BusinessId = p.BusinessId,
                    Name = p.Name,
                    IsActive = p.IsActive,
                    RewardTiers = _db.Set<LoyaltyRewardTier>()
                        .AsNoTracking()
                        .Where(t => t.LoyaltyProgramId == p.Id)
                        .OrderBy(t => t.PointsRequired)
                        .Select(t => new LoyaltyRewardTierPublicDto
                        {
                            Id = t.Id,
                            PointsRequired = t.PointsRequired,
                            RewardType = t.RewardType,
                            RewardValue = t.RewardValue,
                            Description = t.Description,
                            AllowSelfRedemption = t.AllowSelfRedemption
                        })
                        .ToList()
                })
                .SingleOrDefaultAsync(ct)
                .ConfigureAwait(false);

            return new BusinessPublicDetailDto
            {
                Id = business.Id,
                Name = business.Name,
                ShortDescription = business.ShortDescription,
                WebsiteUrl = business.WebsiteUrl,
                ContactEmail = business.ContactEmail,
                ContactPhoneE164 = business.ContactPhoneE164,
                Category = business.Category,
                IsActive = business.IsActive,
                DefaultCurrency = business.DefaultCurrency,
                DefaultCulture = business.DefaultCulture,
                PrimaryImageUrl = primaryImageUrl,
                GalleryImageUrls = gallery,
                Locations = locations,
                LoyaltyProgram = program
            };
        }
    }
}
