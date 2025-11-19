using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Contracts.Businesses
{
    /// <summary>
    /// Detailed business profile including locations and loyalty overview.
    /// </summary>
    public sealed class BusinessDetail
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public string Category { get; init; } = default!;
        public string? Description { get; init; }
        /// <summary>Multiple photos or branding images.</summary>
        public IReadOnlyList<string> ImageUrls { get; init; } = Array.Empty<string>();

        /// <summary>Structured opening hours string (client may parse).</summary>
        public string? OpeningHours { get; init; }

        /// <summary>Phone number in E.164 if present.</summary>
        public string? PhoneE164 { get; init; }
        public string DefaultCurrency { get; init; } = "EUR";
        public string DefaultCulture { get; init; } = "de-DE";
        public string? WebsiteUrl { get; init; }
        public string? ContactEmail { get; init; }
        public string? ContactPhoneE164 { get; init; }
        public IReadOnlyList<BusinessLocation> Locations { get; init; } = Array.Empty<BusinessLocation>();
        public Loyalty.LoyaltyProgramSummary? LoyaltyProgram { get; init; }
    }
}
