using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
	/// <summary>
	/// Loyalty DbSets separated into a dedicated partial file to keep the main context readable.
	/// </summary>
	public sealed partial class DarwinDbContext
	{
        public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
        public DbSet<LoyaltyPointsTransaction> LoyaltyPointsTransactions => Set<LoyaltyPointsTransaction>();
        public DbSet<LoyaltyProgram> LoyaltyPrograms => Set<LoyaltyProgram>();
        public DbSet<LoyaltyRewardTier> LoyaltyRewardTiers => Set<LoyaltyRewardTier>();
        public DbSet<LoyaltyRewardRedemption> LoyaltyRewardRedemptions => Set<LoyaltyRewardRedemption>();
        public DbSet<QrCodeToken> QrCodeTokens => Set<QrCodeToken>();
        public DbSet<ScanSession> ScanSessions => Set<ScanSession>();
    }
}
