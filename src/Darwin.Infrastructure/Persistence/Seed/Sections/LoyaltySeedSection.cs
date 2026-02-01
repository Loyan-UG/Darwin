using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Loyalty;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds loyalty data to support mobile QR flows and business scanning:
    /// - Programs (10+)
    /// - Reward tiers (10+)
    /// - Accounts (10+)
    /// - Points transactions (10+)
    /// - Reward redemptions (10+)
    /// - QR tokens (10+)
    /// - Scan sessions (10+)
    /// </summary>
    public sealed class LoyaltySeedSection
    {
        private readonly ILogger<LoyaltySeedSection> _logger;

        public LoyaltySeedSection(ILogger<LoyaltySeedSection> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Entry point invoked by <see cref="DataSeeder"/>.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            _logger.LogInformation("Seeding Loyalty (programs/accounts/scan sessions) ...");

            var businesses = await db.Set<Business>().OrderBy(b => b.Name).ToListAsync(ct);
            var users = await db.Set<User>().Where(u => !u.IsDeleted).OrderBy(u => u.Email).ToListAsync(ct);

            if (businesses.Count == 0 || users.Count == 0)
            {
                _logger.LogWarning("Skipping loyalty seeding because businesses or users are missing.");
                return;
            }

            if (!await db.Set<LoyaltyProgram>().AnyAsync(ct))
                await SeedProgramsAsync(db, businesses, ct);

            if (!await db.Set<LoyaltyRewardTier>().AnyAsync(ct))
                await SeedRewardTiersAsync(db, ct);

            if (!await db.Set<LoyaltyAccount>().AnyAsync(ct))
                await SeedAccountsAsync(db, businesses, users, ct);

            if (!await db.Set<LoyaltyRewardRedemption>().AnyAsync(ct))
                await SeedRedemptionsAsync(db, ct);

            if (!await db.Set<LoyaltyPointsTransaction>().AnyAsync(ct))
                await SeedTransactionsAsync(db, ct);

            if (!await db.Set<QrCodeToken>().AnyAsync(ct))
                await SeedQrTokensAsync(db, users, ct);

            if (!await db.Set<ScanSession>().AnyAsync(ct))
                await SeedScanSessionsAsync(db, ct);

            _logger.LogInformation("Loyalty seeding done.");
        }

        private static async Task SeedProgramsAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, CancellationToken ct)
        {
            var programs = new List<LoyaltyProgram>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var mode = i % 2 == 0 ? LoyaltyAccrualMode.PerVisit : LoyaltyAccrualMode.AmountBased;

                programs.Add(new LoyaltyProgram
                {
                    BusinessId = businesses[i].Id,
                    Name = $"{businesses[i].Name} Treueprogramm",
                    AccrualMode = mode,
                    PointsPerCurrencyUnit = mode == LoyaltyAccrualMode.AmountBased ? 1.5m : null,
                    IsActive = true,
                    RulesJson = "{\"version\":1,\"note\":\"Seeded loyalty rules\"}"
                });
            }

            db.AddRange(programs);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedRewardTiersAsync(DarwinDbContext db, CancellationToken ct)
        {
            var programs = await db.Set<LoyaltyProgram>().OrderBy(p => p.Name).ToListAsync(ct);
            var tiers = new List<LoyaltyRewardTier>();

            foreach (var program in programs.Take(10))
            {
                tiers.Add(new LoyaltyRewardTier
                {
                    LoyaltyProgramId = program.Id,
                    PointsRequired = 3,
                    RewardType = LoyaltyRewardType.FreeItem,
                    RewardValue = null,
                    Description = "Kostenloser Kaffee",
                    AllowSelfRedemption = false,
                    MetadataJson = "{\"sku\":\"COFFEE-01\"}"
                });

                tiers.Add(new LoyaltyRewardTier
                {
                    LoyaltyProgramId = program.Id,
                    PointsRequired = 5,
                    RewardType = LoyaltyRewardType.PercentDiscount,
                    RewardValue = 10,
                    Description = "10% Rabatt auf den nächsten Einkauf",
                    AllowSelfRedemption = true,
                    MetadataJson = "{\"maxPercent\":10}"
                });

                tiers.Add(new LoyaltyRewardTier
                {
                    LoyaltyProgramId = program.Id,
                    PointsRequired = 8,
                    RewardType = LoyaltyRewardType.AmountDiscount,
                    RewardValue = 2.5m,
                    Description = "2,50 € Rabatt",
                    AllowSelfRedemption = false,
                    MetadataJson = "{\"amount\":\"2.50\"}"
                });
            }

            db.AddRange(tiers);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedAccountsAsync(DarwinDbContext db, IReadOnlyList<Business> businesses, IReadOnlyList<User> users, CancellationToken ct)
        {
            var accounts = new List<LoyaltyAccount>();

            for (var i = 0; i < businesses.Count && i < 10; i++)
            {
                var user = users[i % users.Count];

                accounts.Add(new LoyaltyAccount
                {
                    BusinessId = businesses[i].Id,
                    UserId = user.Id,
                    Status = LoyaltyAccountStatus.Active,
                    PointsBalance = 5 + i,
                    LifetimePoints = 20 + (i * 3),
                    LastAccrualAtUtc = DateTime.UtcNow.AddDays(-i)
                });
            }

            db.AddRange(accounts);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedRedemptionsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var accounts = await db.Set<LoyaltyAccount>().OrderBy(a => a.BusinessId).ToListAsync(ct);
            var tiers = await db.Set<LoyaltyRewardTier>().OrderBy(t => t.PointsRequired).ToListAsync(ct);
            var locations = await db.Set<BusinessLocation>().ToListAsync(ct);

            var redemptions = new List<LoyaltyRewardRedemption>();

            for (var i = 0; i < accounts.Count && i < 10; i++)
            {
                var tier = tiers[i % tiers.Count];
                var location = locations.FirstOrDefault(l => l.BusinessId == accounts[i].BusinessId);

                redemptions.Add(new LoyaltyRewardRedemption
                {
                    LoyaltyAccountId = accounts[i].Id,
                    BusinessId = accounts[i].BusinessId,
                    LoyaltyRewardTierId = tier.Id,
                    PointsSpent = tier.PointsRequired,
                    Status = i % 2 == 0 ? LoyaltyRedemptionStatus.Confirmed : LoyaltyRedemptionStatus.Pending,
                    BusinessLocationId = location?.Id,
                    MetadataJson = "{\"source\":\"seed\"}"
                });
            }

            db.AddRange(redemptions);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedTransactionsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var accounts = await db.Set<LoyaltyAccount>().OrderBy(a => a.BusinessId).ToListAsync(ct);
            var locations = await db.Set<BusinessLocation>().ToListAsync(ct);

            var tx = new List<LoyaltyPointsTransaction>();

            for (var i = 0; i < accounts.Count && i < 10; i++)
            {
                var location = locations.FirstOrDefault(l => l.BusinessId == accounts[i].BusinessId);

                tx.Add(new LoyaltyPointsTransaction
                {
                    LoyaltyAccountId = accounts[i].Id,
                    BusinessId = accounts[i].BusinessId,
                    Type = LoyaltyPointsTransactionType.Accrual,
                    PointsDelta = 1 + (i % 3),
                    BusinessLocationId = location?.Id,
                    Reference = $"VISIT-{i + 1:D3}",
                    Notes = "Seeded accrual for mobile testing."
                });
            }

            db.AddRange(tx);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedQrTokensAsync(DarwinDbContext db, IReadOnlyList<User> users, CancellationToken ct)
        {
            var accounts = await db.Set<LoyaltyAccount>().OrderBy(a => a.BusinessId).ToListAsync(ct);
            var tokens = new List<QrCodeToken>();

            for (var i = 0; i < accounts.Count && i < 10; i++)
            {
                var user = users[i % users.Count];

                tokens.Add(new QrCodeToken
                {
                    UserId = user.Id,
                    LoyaltyAccountId = accounts[i].Id,
                    Token = $"QR-{Guid.NewGuid():N}",
                    Purpose = i % 2 == 0 ? QrTokenPurpose.Accrual : QrTokenPurpose.Redemption,
                    IssuedAtUtc = DateTime.UtcNow.AddSeconds(-30),
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
                    IssuedDeviceId = $"device-{i:D2}"
                });
            }

            db.AddRange(tokens);
            await db.SaveChangesAsync(ct);
        }

        private static async Task SeedScanSessionsAsync(DarwinDbContext db, CancellationToken ct)
        {
            var accounts = await db.Set<LoyaltyAccount>().OrderBy(a => a.BusinessId).ToListAsync(ct);
            var tokens = await db.Set<QrCodeToken>().OrderBy(t => t.IssuedAtUtc).ToListAsync(ct);
            var tiers = await db.Set<LoyaltyRewardTier>().OrderBy(t => t.PointsRequired).ToListAsync(ct);
            var locations = await db.Set<BusinessLocation>().ToListAsync(ct);

            var sessions = new List<ScanSession>();

            for (var i = 0; i < tokens.Count && i < 10; i++)
            {
                var account = accounts[i % accounts.Count];
                var location = locations.FirstOrDefault(l => l.BusinessId == account.BusinessId);
                var tier = tiers[i % tiers.Count];
                var mode = tokens[i].Purpose == QrTokenPurpose.Redemption ? LoyaltyScanMode.Redemption : LoyaltyScanMode.Accrual;

                var selectedRewards = mode == LoyaltyScanMode.Redemption
                    ? $"[{{\"tierId\":\"{tier.Id}\",\"requiredPoints\":{tier.PointsRequired},\"quantity\":1}}]"
                    : null;

                sessions.Add(new ScanSession
                {
                    QrCodeTokenId = tokens[i].Id,
                    LoyaltyAccountId = account.Id,
                    BusinessId = account.BusinessId,
                    BusinessLocationId = location?.Id,
                    Mode = mode,
                    Status = i % 2 == 0 ? LoyaltyScanStatus.Completed : LoyaltyScanStatus.Pending,
                    SelectedRewardsJson = selectedRewards,
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(2),
                    CreatedByDeviceId = tokens[i].IssuedDeviceId,
                    Outcome = i % 2 == 0 ? "Accepted" : "Pending",
                    FailureReason = null,
                    ResultingTransactionId = null
                });
            }

            db.AddRange(sessions);
            await db.SaveChangesAsync(ct);
        }
    }
}