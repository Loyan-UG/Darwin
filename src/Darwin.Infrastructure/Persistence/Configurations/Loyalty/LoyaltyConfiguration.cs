using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Loyalty
{
    /// <summary>
    /// EF Core configuration for Loyalty module entities.
    /// Keeps constraints, indexes, and relations aligned with domain invariants.
    /// </summary>
    public sealed class LoyaltyConfiguration :
        IEntityTypeConfiguration<LoyaltyAccount>,
        IEntityTypeConfiguration<LoyaltyPointsTransaction>,
        IEntityTypeConfiguration<LoyaltyProgram>,
        IEntityTypeConfiguration<LoyaltyRewardTier>,
        IEntityTypeConfiguration<LoyaltyRewardRedemption>,
        IEntityTypeConfiguration<QrCodeToken>,
        IEntityTypeConfiguration<ScanSession>
    {
        public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
        {
            builder.ToTable("LoyaltyAccounts", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.PointsBalance).IsRequired().HasDefaultValue(0);
            builder.Property(x => x.LifetimePoints).IsRequired().HasDefaultValue(0);

            builder.HasIndex(x => new { x.BusinessId, x.UserId })
                   .IsUnique()
                   .HasDatabaseName("UX_LoyaltyAccounts_Business_User");

            builder.HasMany(x => x.Transactions)
                   .WithOne()
                   .HasForeignKey(t => t.LoyaltyAccountId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Redemptions)
                   .WithOne()
                   .HasForeignKey(r => r.LoyaltyAccountId)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<LoyaltyPointsTransaction> builder)
        {
            builder.ToTable("LoyaltyPointsTransactions", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.PointsDelta).IsRequired();

            builder.Property(x => x.Reference).HasMaxLength(200);
            builder.Property(x => x.Notes).HasMaxLength(1000);

            builder.HasIndex(x => x.LoyaltyAccountId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BusinessLocationId);
            builder.HasIndex(x => x.RewardRedemptionId);
        }

        public void Configure(EntityTypeBuilder<LoyaltyProgram> builder)
        {
            builder.ToTable("LoyaltyPrograms", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.AccrualMode).IsRequired();
            builder.Property(x => x.PointsPerCurrencyUnit).HasPrecision(18, 4);

            builder.Property(x => x.RulesJson).HasMaxLength(4000);

            builder.HasIndex(x => x.BusinessId)
                   .IsUnique()
                   .HasDatabaseName("UX_LoyaltyPrograms_Business");

            builder.HasIndex(x => x.IsActive);

            builder.HasMany(x => x.RewardTiers)
                   .WithOne()
                   .HasForeignKey(t => t.LoyaltyProgramId)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<LoyaltyRewardTier> builder)
        {
            builder.ToTable("LoyaltyRewardTiers", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PointsRequired).IsRequired();
            builder.Property(x => x.RewardType).IsRequired();
            builder.Property(x => x.RewardValue).HasPrecision(18, 4);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.MetadataJson).HasMaxLength(4000);

            builder.HasIndex(x => new { x.LoyaltyProgramId, x.PointsRequired })
                   .IsUnique()
                   .HasDatabaseName("UX_LoyaltyRewardTiers_Program_Points");
        }

        public void Configure(EntityTypeBuilder<LoyaltyRewardRedemption> builder)
        {
            builder.ToTable("LoyaltyRewardRedemptions", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PointsSpent).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.MetadataJson).HasMaxLength(4000);

            builder.HasIndex(x => x.LoyaltyAccountId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.LoyaltyRewardTierId);
            builder.HasIndex(x => x.BusinessLocationId);
        }

        public void Configure(EntityTypeBuilder<QrCodeToken> builder)
        {
            builder.ToTable("QrCodeTokens", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Token)
                   .IsRequired()
                   .HasMaxLength(512);

            builder.Property(x => x.Purpose).IsRequired();

            builder.HasIndex(x => x.Token)
                   .IsUnique()
                   .HasDatabaseName("UX_QrCodeTokens_Token");

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.LoyaltyAccountId);
            builder.HasIndex(x => x.ExpiresAtUtc);
        }

        public void Configure(EntityTypeBuilder<ScanSession> builder)
        {
            builder.ToTable("ScanSessions", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Outcome)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.FailureReason).HasMaxLength(500);

            builder.HasIndex(x => x.QrCodeTokenId);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BusinessLocationId);
            builder.HasIndex(x => x.ResultingTransactionId);
        }
    }
}
