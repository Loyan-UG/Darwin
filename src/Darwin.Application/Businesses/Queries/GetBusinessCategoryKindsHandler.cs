using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.DTOs;
using Darwin.Domain.Enums;

namespace Darwin.Application.Businesses.Queries
{
    /// <summary>
    /// Returns all <see cref="BusinessCategoryKind"/> values for discovery/filter UIs (mobile/web).
    /// This is a pure in-memory query and does not require database access.
    /// </summary>
    public sealed class GetBusinessCategoryKindsHandler
    {
        /// <summary>
        /// Returns a stable list of categories ordered by enum numeric value.
        /// </summary>
        public Task<BusinessCategoryKindsResponseDto> HandleAsync(CancellationToken ct = default)
        {
            // No I/O here; keep async surface consistent for WebApi and future extension.
            ct.ThrowIfCancellationRequested();

            var values = Enum.GetValues<BusinessCategoryKind>()
                .OrderBy(x => (short)x)
                .Select(x => new BusinessCategoryKindItemDto
                {
                    Kind = x,
                    Value = (short)x,
                    DisplayName = GetFallbackDisplayName(x)
                })
                .ToList();

            BusinessCategoryKindsResponseDto result = new()
            {
                Items = values
            };

            return Task.FromResult(result);
        }

        /// <summary>
        /// Provides a deterministic English label for <see cref="BusinessCategoryKind"/>.
        /// This is intentionally kept as a fallback; localization should be handled in WebApi/UI layers.
        /// </summary>
        private static string GetFallbackDisplayName(BusinessCategoryKind kind) => kind switch
        {
            BusinessCategoryKind.Unknown => "Unknown",
            BusinessCategoryKind.Cafe => "Cafe",
            BusinessCategoryKind.Restaurant => "Restaurant",
            BusinessCategoryKind.Bakery => "Bakery",
            BusinessCategoryKind.Supermarket => "Supermarket",
            BusinessCategoryKind.SalonSpa => "Salon & Spa",
            BusinessCategoryKind.Fitness => "Fitness",
            BusinessCategoryKind.OtherRetail => "Other Retail",
            BusinessCategoryKind.Services => "Services",
            _ => kind.ToString()
        };
    }
}
