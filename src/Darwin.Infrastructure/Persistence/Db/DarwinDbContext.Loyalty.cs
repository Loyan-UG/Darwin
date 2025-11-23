using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
	/// <summary>
	/// Loyalty DbSets separated into a dedicated partial file to keep the main context readable.
	/// </summary>
	public sealed partial class DarwinDbContext
	{
		/// <summary>
		/// Reward programs owned by businesses.
		/// </summary>
		public DbSet<LoyaltyProgram> LoyaltyPrograms => Set<LoyaltyProgram>();

		/// <summary>
		/// Reward tiers under programs.
		/// </summary>
		public DbSet<LoyaltyRewardTier> LoyaltyRewardTiers => Set<LoyaltyRewardTier>();

		/// <summary>
		/// Loyalty accounts per business-consumer pair.
		/// </summary>
		public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();

		/// <summary>
		/// Point transactions (accrual/redemption).
		/// </summary>
		public DbSet<LoyaltyPointTransaction> LoyaltyPointTransactions => Set<LoyaltyPointTransaction>();

		/// <summary>
		/// Short-lived QR tokens issued to consumers.
		/// </summary>
		public DbSet<QrCodeToken> QrCodeTokens => Set<QrCodeToken>();

		/// <summary>
		/// Active scan sessions between business and consumer.
		/// </summary>
		public DbSet<ScanSession> ScanSessions => Set<ScanSession>();
	}
}
