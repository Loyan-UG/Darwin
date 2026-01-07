using System;
using System.Collections.Generic;
using System.Linq;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Loyalty;

namespace Darwin.WebApi.Mappers
{
    /// <summary>
    /// Central mapping helpers for converting Application DTOs into Darwin.Contracts models
    /// for business discovery and detail endpoints.
    /// </summary>
    /// <remarks>
    /// Why this exists:
    /// - WebApi is contract-first: everything returned to clients must be Darwin.Contracts.
    /// - Application DTOs are internal read models and may evolve independently.
    /// - Keeping mapping logic in controllers quickly becomes unmaintainable and error-prone
    ///   (e.g., mismatched property names like Id vs LoyaltyAccountId, enum vs string).
    /// - By centralizing mappings, we enforce consistent null handling and stable contract output.
    /// </remarks>
    public static class BusinessContractsMapper
    {
        /// <summary>
        /// Maps discovery list items (Application) to consumer contract business summaries.
        /// </summary>
        /// <param name="dto">Application discovery list item DTO.</param>
        public static BusinessSummary ToContract(BusinessDiscoveryListItemDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new BusinessSummary
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                ShortDescription = dto.ShortDescription,
                LogoUrl = dto.PrimaryImageUrl,
                Category = dto.Category.ToString(),
                Location = dto.Coordinate is null ? null : new GeoCoordinateModel
                {
                    Latitude = dto.Coordinate.Latitude,
                    Longitude = dto.Coordinate.Longitude,
                    AltitudeMeters = dto.Coordinate.AltitudeMeters
                },
                City = dto.City,
                IsOpenNow = dto.IsOpenNow,
                IsActive = dto.IsActive,

                // Application provides DistanceKm; API contract standardizes on meters.
                DistanceMeters = dto.DistanceKm.HasValue
                    ? (int?)Math.Round(dto.DistanceKm.Value * 1000.0, MidpointRounding.AwayFromZero)
                    : null,

                // Ratings are intentionally not provided by the current handler.
                Rating = null,
                RatingCount = null
            };
        }

        /// <summary>
        /// Maps a public business detail (Application) to the consumer-facing contract.
        /// </summary>
        public static BusinessDetail ToContract(BusinessPublicDetailDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var primaryLocation = dto.Locations?.FirstOrDefault(l => l.IsPrimary) ?? dto.Locations?.FirstOrDefault();

            return new BusinessDetail
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                Category = dto.Category.ToString(),

                // Prefer explicit short description; do not fabricate long description.
                ShortDescription = dto.ShortDescription,
                Description = null,

                PrimaryImageUrl = dto.PrimaryImageUrl,
                GalleryImageUrls = dto.GalleryImageUrls,

                // Legacy combined list preserved for backward compatibility.
                ImageUrls = BuildImageUrls(dto.PrimaryImageUrl, dto.GalleryImageUrls),

                City = primaryLocation?.City,
                Coordinate = primaryLocation?.Coordinate is null ? null : new GeoCoordinateModel
                {
                    Latitude = primaryLocation.Coordinate.Latitude,
                    Longitude = primaryLocation.Coordinate.Longitude,
                    AltitudeMeters = primaryLocation.Coordinate.AltitudeMeters
                },

                // Opening hours are not standardized yet. Keep null for contract stability.
                OpeningHours = null,

                // For backward compatibility: keep PhoneE164 populated, but also keep ContactPhoneE164 below.
                PhoneE164 = dto.ContactPhoneE164,

                DefaultCurrency = string.IsNullOrWhiteSpace(dto.DefaultCurrency) ? "EUR" : dto.DefaultCurrency,
                DefaultCulture = string.IsNullOrWhiteSpace(dto.DefaultCulture) ? "de-DE" : dto.DefaultCulture,
                WebsiteUrl = dto.WebsiteUrl,
                ContactEmail = dto.ContactEmail,
                ContactPhoneE164 = dto.ContactPhoneE164,

                Locations = (dto.Locations ?? new List<BusinessPublicLocationDto>())
                    .Select(ToContract)
                    .ToList(),

                // Contract currently has both LoyaltyProgram and LoyaltyProgramPublic; public is preferred.
                LoyaltyProgram = null,
                LoyaltyProgramPublic = dto.LoyaltyProgram is null ? null : ToContract(dto.LoyaltyProgram)
            };
        }

        /// <summary>
        /// Maps a business+my-account combined Application DTO to its contract equivalent.
        /// </summary>
        public static BusinessDetailWithMyAccount ToContract(BusinessPublicDetailWithMyAccountDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(dto.Business);

            return new BusinessDetailWithMyAccount
            {
                Business = ToContract(dto.Business),
                HasAccount = dto.HasAccount,
                MyAccount = dto.MyAccount is null ? null : ToContract(dto.MyAccount)
            };
        }

        /// <summary>
        /// Maps a location entry from the Application public business detail DTO to the contract model.
        /// </summary>
        public static BusinessLocation ToContract(BusinessPublicLocationDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new BusinessLocation
            {
                BusinessLocationId = dto.Id,
                Name = dto.Name ?? string.Empty,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                Region = dto.Region,
                CountryCode = dto.CountryCode,
                PostalCode = dto.PostalCode,
                Coordinate = dto.Coordinate is null ? null : new GeoCoordinateModel
                {
                    Latitude = dto.Coordinate.Latitude,
                    Longitude = dto.Coordinate.Longitude,
                    AltitudeMeters = dto.Coordinate.AltitudeMeters
                },
                IsPrimary = dto.IsPrimary,
                OpeningHoursJson = dto.OpeningHoursJson
            };
        }

        /// <summary>
        /// Maps the public loyalty program DTO (Application) to the public contract model.
        /// </summary>
        public static LoyaltyProgramPublic ToContract(LoyaltyProgramPublicDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new LoyaltyProgramPublic
            {
                Id = dto.Id,
                BusinessId = dto.BusinessId,
                Name = dto.Name ?? string.Empty,
                IsActive = dto.IsActive,
                RewardTiers = (dto.RewardTiers ?? new List<LoyaltyRewardTierPublicDto>())
                    .Select(t => new LoyaltyRewardTierPublic
                    {
                        Id = t.Id,
                        PointsRequired = t.PointsRequired,
                        RewardType = t.RewardType.ToString(),
                        RewardValue = t.RewardValue,
                        Description = t.Description,
                        AllowSelfRedemption = t.AllowSelfRedemption
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Maps the current user's loyalty account summary (Application) to the stable contract model.
        /// </summary>
        /// <remarks>
        /// Key differences handled here:
        /// - Application uses Id, contract uses LoyaltyAccountId.
        /// - Application uses LoyaltyAccountStatus enum, contract uses string.
        /// - Contract guarantees non-null BusinessName and Status strings.
        /// </remarks>
        public static LoyaltyAccountSummary ToContract(LoyaltyAccountSummaryDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new LoyaltyAccountSummary
            {
                LoyaltyAccountId = dto.Id,
                BusinessId = dto.BusinessId,
                BusinessName = dto.BusinessName ?? string.Empty,
                Status = dto.Status.ToString(),
                PointsBalance = dto.PointsBalance,
                LifetimePoints = dto.LifetimePoints,
                LastAccrualAtUtc = dto.LastAccrualAtUtc,

                // Not provided by Application yet (future enhancement).
                NextRewardTitle = null
            };
        }

        /// <summary>
        /// Builds a legacy combined image url list for backward compatibility.
        /// New clients should prefer PrimaryImageUrl + GalleryImageUrls.
        /// </summary>
        private static IReadOnlyList<string> BuildImageUrls(string? primaryImageUrl, List<string>? galleryImageUrls)
        {
            var list = new List<string>(capacity: 1 + (galleryImageUrls?.Count ?? 0));

            if (!string.IsNullOrWhiteSpace(primaryImageUrl))
            {
                list.Add(primaryImageUrl.Trim());
            }

            if (galleryImageUrls is { Count: > 0 })
            {
                foreach (var url in galleryImageUrls)
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        list.Add(url.Trim());
                    }
                }
            }

            return list;
        }
    }
}