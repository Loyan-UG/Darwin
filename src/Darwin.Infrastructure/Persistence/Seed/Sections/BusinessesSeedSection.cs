using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds business-related data for discovery and mobile flows:
    /// - Businesses (10+)
    /// - Locations (10+ with German cities)
    /// - Members (10+)
    /// - Media (10+)
    /// - Engagement stats (10+)
    /// - Reviews / Likes / Favorites (10+ each)
    /// - Invitations (10+)
    /// - Staff QR codes (10+)
    /// </summary>
    public sealed class BusinessesSeedSection
    {
        private readonly ILogger<BusinessesSeedSection> _logger;

        public BusinessesSeedSection(ILogger<BusinessesSeedSection> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Entry point invoked by <see cref="DataSeeder"/>.
        /// Idempotent by table checks.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Businesses (core + discovery data) ...");

            var users = await db.Set<User>()
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.Email)
                .ToListAsync(ct);

            if (users.Count == 0)
            {
                _logger.LogWarning("Skipping business seeding because no users exist yet.");
                return;
            }

            var businesses = await EnsureBusinessesAsync(db, ct);

            if (!await db.Set<BusinessLocation>().AnyAsync(ct))
                await SeedLocationsAsync(db, businesses, ct);

            if (!await db.Set<BusinessMember>().AnyAsync(ct))
                await SeedMembersAsync(db, businesses, users, ct);

            if (!await db.Set<BusinessMedia>().AnyAsync(ct))
                await SeedMediaAsync(db, businesses, ct);

            if (!await db.Set<BusinessEngagementStats>().AnyAsync(ct))
                await SeedEngagementStatsAsync(db, businesses, ct);

            if (!await db.Set<BusinessReview>().AnyAsync(ct))
                await SeedReviewsAsync(db, businesses, users, ct);

            if (!await db.Set<BusinessFavorite>().AnyAsync(ct))
                await SeedFavoritesAsync(db, businesses, users, ct);

            if (!await db.Set<BusinessLike>().AnyAsync(ct))
                await SeedLikesAsync(db, businesses, users, ct);

            if (!await db.Set<BusinessInvitation>().AnyAsync(ct))
                await SeedInvitationsAsync(db, businesses, users, ct);

            if (!await db.Set<BusinessStaffQrCode>().AnyAsync(ct))
                await SeedStaffQrCodesAsync(db, ct);

            _logger.LogInformation("Businesses seeding done.");
        }

        private sealed record BusinessSeed(
            string Name,
            string LegalName,
            string TaxId,
            BusinessCategoryKind Category,
            string City,
            string Street,
            string Postal,
            double Lat,
            double Lon,
            string Email,
            string Phone,
            string Website,
            string ShortDescription,
            string SlugKey);

        /// <summary>
        /// Ensures at least 10 businesses exist and returns the current list.
        /// </summary>
        private static async Task<List<Business>> EnsureBusinessesAsync(DarwinDbContext db, CancellationToken ct)
        {
            var existing = await db.Set<Business>().ToListAsync(ct);
            if (existing.Count > 0) return existing;

            var seeds = new[]
            {
                new BusinessSeed("Café Aurora", "Aurora GmbH", "DE123456789", BusinessCategoryKind.Cafe,
                    "Berlin", "Invalidenstraße 117", "10115", 52.5286, 13.3849, "info@cafe-aurora.de", "+49 30 1234567",
                    "https://cafe-aurora.de", "Helles Café mit Spezialitätenkaffee und Frühstück.", "cafe-aurora"),
                new BusinessSeed("Bäckerei König", "König Backwaren KG", "DE223456789", BusinessCategoryKind.Bakery,
                    "München", "Marienplatz 8", "80331", 48.1374, 11.5755, "kontakt@baeckerei-koenig.de", "+49 89 2233445",
                    "https://baeckerei-koenig.de", "Traditionelle Backwaren nach bayerischem Handwerk.", "baeckerei-koenig"),
                new BusinessSeed("RheinEssen", "RheinEssen Restaurant GmbH", "DE323456789", BusinessCategoryKind.Restaurant,
                    "Köln", "Hohenzollernring 22", "50672", 50.9375, 6.9431, "reservierung@rheinessen.de", "+49 221 998877",
                    "https://rheinessen.de", "Rheinische Küche mit regionalen Zutaten.", "rheinessen"),
                new BusinessSeed("NordFit Club", "NordFit Fitness GmbH", "DE423456789", BusinessCategoryKind.Fitness,
                    "Hamburg", "Spitalerstraße 3", "20095", 53.5509, 9.9965, "info@nordfit.de", "+49 40 889900",
                    "https://nordfit.de", "Modernes Fitnessstudio mit persönlichem Coaching.", "nordfit"),
                new BusinessSeed("MainMarkt", "MainMarkt Supermarkt GmbH", "DE523456789", BusinessCategoryKind.Supermarket,
                    "Frankfurt am Main", "Zeil 105", "60313", 50.1136, 8.6797, "service@mainmarkt.de", "+49 69 445566",
                    "https://mainmarkt.de", "Frischeprodukte und regionale Spezialitäten.", "mainmarkt"),
                new BusinessSeed("SchönZeit Spa", "SchönZeit Wellness GmbH", "DE623456789", BusinessCategoryKind.SalonSpa,
                    "Stuttgart", "Königstraße 12", "70173", 48.7770, 9.1799, "kontakt@schoenzeit.de", "+49 711 778899",
                    "https://schoenzeit.de", "Entspannung, Massagen und Beauty-Services.", "schoenzeit"),
                new BusinessSeed("DorfLaden Süd", "DorfLaden Süd e.K.", "DE723456789", BusinessCategoryKind.OtherRetail,
                    "Düsseldorf", "Schadowstraße 55", "40212", 51.2277, 6.7735, "hallo@dorfladen.de", "+49 211 556677",
                    "https://dorfladen.de", "Feinkost und regionale Produkte aus NRW.", "dorfladen"),
                new BusinessSeed("Leipzig Service Hub", "Leipzig Service Hub UG", "DE823456789", BusinessCategoryKind.Services,
                    "Leipzig", "Grimmaische Straße 14", "04109", 51.3397, 12.3731, "team@lshub.de", "+49 341 112233",
                    "https://lshub.de", "Servicepunkt für Abholung, Reparatur und Beratung.", "lshub"),
                new BusinessSeed("ElbeBistro", "ElbeBistro GmbH", "DE923456789", BusinessCategoryKind.Restaurant,
                    "Dresden", "Altmarkt 10", "01067", 51.0504, 13.7373, "service@elbebistro.de", "+49 351 224466",
                    "https://elbebistro.de", "Bistro mit Tagesgerichten und Elbeblick.", "elbebistro"),
                new BusinessSeed("FrankenKaffee", "FrankenKaffee GmbH", "DE103456789", BusinessCategoryKind.Cafe,
                    "Nürnberg", "Königstraße 41", "90402", 49.4521, 11.0767, "kontakt@frankenkaffee.de", "+49 911 556644",
                    "https://frankenkaffee.de", "Kaffeehaus mit regionalen Röstungen.", "frankenkaffee"),
            };

            foreach (var s in seeds)
            {
                db.Add(new Business
                {
                    Name = s.Name,
                    LegalName = s.LegalName,
                    TaxId = s.TaxId,
                    Category = s.Category,
                    DefaultCurrency = "EUR",
                    DefaultCulture = "de-DE",
                    IsActive = true,
                    ContactEmail = s.Email,
                    ContactPhoneE164 = s.Phone,
                    WebsiteUrl = s.Website,
                    ShortDescription = s.ShortDescription
                });
            }

            await db.SaveChangesAsync(ct);
            return await db.Set<Business>().OrderBy(b => b.Name).ToListAsync(ct);
        }

        private static async Task SeedLocationsAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, CancellationToken ct)
        {
            var seeds = GetBusinessSeeds();
            var locations = new List<BusinessLocation>();

            for (var i = 0; i < businesses.Count && i < seeds.Length; i++)
            {
                var s = seeds[i];
                locations.Add(new BusinessLocation
                {
                    BusinessId = businesses[i].Id,
                    Name = $"{s.City} Hauptfiliale",
                    AddressLine1 = s.Street,
                    City = s.City,
                    Region = "DE",
                    CountryCode = "DE",
                    PostalCode = s.Postal,
                    Coordinate = new GeoCoordinate(s.Lat, s.Lon),
                    IsPrimary = true,
                    OpeningHoursJson = "{\"Mon\":[{\"Open\":\"08:00\",\"Close\":\"18:00\"}],\"Sat\":[{\"Open\":\"09:00\",\"Close\":\"14:00\"}]}",
                    InternalNote = "Seeded primary location for mobile discovery tests."
                });
            }

            db.AddRange(locations);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedMembersAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, IReadOnlyList<User> users, CancellationToken ct)
        {
            var members = new List<BusinessMember>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var user = users[i % users.Count];
                var role = i == 0 ? BusinessMemberRole.Owner : (i % 3 == 0 ? BusinessMemberRole.Manager : BusinessMemberRole.Staff);

                members.Add(new BusinessMember
                {
                    BusinessId = businesses[i].Id,
                    UserId = user.Id,
                    Role = role,
                    IsActive = true
                });
            }

            db.AddRange(members);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedMediaAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, CancellationToken ct)
        {
            var seeds = GetBusinessSeeds();
            var media = new List<BusinessMedia>();

            for (var i = 0; i < businesses.Count && i < seeds.Length; i++)
            {
                var s = seeds[i];
                media.Add(new BusinessMedia
                {
                    BusinessId = businesses[i].Id,
                    Url = $"/media/business/{s.SlugKey}/logo.jpg",
                    Caption = $"{s.Name} Logo",
                    SortOrder = 0,
                    IsPrimary = true
                });
            }

            db.AddRange(media);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedEngagementStatsAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, CancellationToken ct)
        {
            var stats = new List<BusinessEngagementStats>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var ratingCount = 6 + i;
                var ratingSum = ratingCount * (3 + (i % 3));

                var row = new BusinessEngagementStats
                {
                    BusinessId = businesses[i].Id
                };
                row.SetSnapshot(ratingCount, ratingSum, likeCount: 4 + i, favoriteCount: 2 + i, utcNow: DateTime.UtcNow);

                stats.Add(row);
            }

            db.AddRange(stats);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedReviewsAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, IReadOnlyList<User> users, CancellationToken ct)
        {
            var reviews = new List<BusinessReview>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var user = users[i % users.Count];
                var rating = (byte)(3 + (i % 3));
                var comment = $"Sehr freundlicher Service in {businesses[i].Name}.";

                reviews.Add(new BusinessReview(user.Id, businesses[i].Id, rating, comment));
            }

            db.AddRange(reviews);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedFavoritesAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, IReadOnlyList<User> users, CancellationToken ct)
        {
            var favorites = new List<BusinessFavorite>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var user = users[(i + 1) % users.Count];
                favorites.Add(new BusinessFavorite(user.Id, businesses[i].Id));
            }

            db.AddRange(favorites);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedLikesAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, IReadOnlyList<User> users, CancellationToken ct)
        {
            var likes = new List<BusinessLike>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var user = users[(i + 2) % users.Count];
                likes.Add(new BusinessLike(user.Id, businesses[i].Id));
            }

            db.AddRange(likes);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedInvitationsAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, IReadOnlyList<User> users, CancellationToken ct)
        {
            var invitations = new List<BusinessInvitation>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var inviter = users[i % users.Count];
                var email = $"invite{i + 1}@darwin-business.de";

                invitations.Add(new BusinessInvitation
                {
                    BusinessId = businesses[i].Id,
                    InvitedByUserId = inviter.Id,
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    Role = i % 2 == 0 ? BusinessMemberRole.Manager : BusinessMemberRole.Staff,
                    Token = $"INV-{Guid.NewGuid():N}",
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                    Status = BusinessInvitationStatus.Pending,
                    Note = "Seeded invitation for onboarding tests."
                });
            }

            db.AddRange(invitations);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedStaffQrCodesAsync(DarwinDbContext db, CancellationToken ct)
        {
            var members = await db.Set<BusinessMember>()
                .OrderBy(m => m.BusinessId)
                .ToListAsync(ct);

            if (members.Count == 0) return;

            var tokens = new List<BusinessStaffQrCode>();

            for (var i = 0; i < members.Count && i < 10; i++)
            {
                tokens.Add(new BusinessStaffQrCode
                {
                    BusinessId = members[i].BusinessId,
                    BusinessMemberId = members[i].Id,
                    Purpose = i % 2 == 0 ? BusinessStaffQrPurpose.StaffSignIn : BusinessStaffQrPurpose.TerminalPairing,
                    Token = $"BIZ-STAFF-{Guid.NewGuid():N}",
                    IssuedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                    IssuedDeviceId = $"terminal-{i:D2}"
                });
            }

            db.AddRange(tokens);
            await db.SaveChangesAsync(ct);
        }

        private static BusinessSeed[] GetBusinessSeeds() => new[]
        {
            new BusinessSeed("Café Aurora", "Aurora GmbH", "DE123456789", BusinessCategoryKind.Cafe,
                "Berlin", "Invalidenstraße 117", "10115", 52.5286, 13.3849, "info@cafe-aurora.de", "+49 30 1234567",
                "https://cafe-aurora.de", "Helles Café mit Spezialitätenkaffee und Frühstück.", "cafe-aurora"),
            new BusinessSeed("Bäckerei König", "König Backwaren KG", "DE223456789", BusinessCategoryKind.Bakery,
                "München", "Marienplatz 8", "80331", 48.1374, 11.5755, "kontakt@baeckerei-koenig.de", "+49 89 2233445",
                "https://baeckerei-koenig.de", "Traditionelle Backwaren nach bayerischem Handwerk.", "baeckerei-koenig"),
            new BusinessSeed("RheinEssen", "RheinEssen Restaurant GmbH", "DE323456789", BusinessCategoryKind.Restaurant,
                "Köln", "Hohenzollernring 22", "50672", 50.9375, 6.9431, "reservierung@rheinessen.de", "+49 221 998877",
                "https://rheinessen.de", "Rheinische Küche mit regionalen Zutaten.", "rheinessen"),
            new BusinessSeed("NordFit Club", "NordFit Fitness GmbH", "DE423456789", BusinessCategoryKind.Fitness,
                "Hamburg", "Spitalerstraße 3", "20095", 53.5509, 9.9965, "info@nordfit.de", "+49 40 889900",
                "https://nordfit.de", "Modernes Fitnessstudio mit persönlichem Coaching.", "nordfit"),
            new BusinessSeed("MainMarkt", "MainMarkt Supermarkt GmbH", "DE523456789", BusinessCategoryKind.Supermarket,
                "Frankfurt am Main", "Zeil 105", "60313", 50.1136, 8.6797, "service@mainmarkt.de", "+49 69 445566",
                "https://mainmarkt.de", "Frischeprodukte und regionale Spezialitäten.", "mainmarkt"),
            new BusinessSeed("SchönZeit Spa", "SchönZeit Wellness GmbH", "DE623456789", BusinessCategoryKind.SalonSpa,
                "Stuttgart", "Königstraße 12", "70173", 48.7770, 9.1799, "kontakt@schoenzeit.de", "+49 711 778899",
                "https://schoenzeit.de", "Entspannung, Massagen und Beauty-Services.", "schoenzeit"),
            new BusinessSeed("DorfLaden Süd", "DorfLaden Süd e.K.", "DE723456789", BusinessCategoryKind.OtherRetail,
                "Düsseldorf", "Schadowstraße 55", "40212", 51.2277, 6.7735, "hallo@dorfladen.de", "+49 211 556677",
                "https://dorfladen.de", "Feinkost und regionale Produkte aus NRW.", "dorfladen"),
            new BusinessSeed("Leipzig Service Hub", "Leipzig Service Hub UG", "DE823456789", BusinessCategoryKind.Services,
                "Leipzig", "Grimmaische Straße 14", "04109", 51.3397, 12.3731, "team@lshub.de", "+49 341 112233",
                "https://lshub.de", "Servicepunkt für Abholung, Reparatur und Beratung.", "lshub"),
            new BusinessSeed("ElbeBistro", "ElbeBistro GmbH", "DE923456789", BusinessCategoryKind.Restaurant,
                "Dresden", "Altmarkt 10", "01067", 51.0504, 13.7373, "service@elbebistro.de", "+49 351 224466",
                "https://elbebistro.de", "Bistro mit Tagesgerichten und Elbeblick.", "elbebistro"),
            new BusinessSeed("FrankenKaffee", "FrankenKaffee GmbH", "DE103456789", BusinessCategoryKind.Cafe,
                "Nürnberg", "Königstraße 41", "90402", 49.4521, 11.0767, "kontakt@frankenkaffee.de", "+49 911 556644",
                "https://frankenkaffee.de", "Kaffeehaus mit regionalen Röstungen.", "frankenkaffee"),
        };
    }
}