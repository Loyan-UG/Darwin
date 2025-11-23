using Darwin.Domain.Entities.Loyalty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Loyalty
{
    /// <summary>
    /// EF Core configurations for Loyalty module entities.
    /// Uses schema "Loyalty" and enforces key constraints, indexes, and relationships.
    /// Global soft-delete filter and RowVersion mapping are applied via Conventions.
    /// </summary>
    public sealed class LoyaltyProgramConfiguration :
        IEntityTypeConfiguration<LoyaltyProgram>,
        IEntityTypeConfiguration<LoyaltyRewardTier>,
        IEntityTypeConfiguration<LoyaltyAccount>,
        IEntityTypeConfiguration<LoyaltyPointsTransaction>,
        IEntityTypeConfiguration<QrCodeToken>,
        IEntityTypeConfiguration<ScanSession>
    {
        public void Configure(EntityTypeBuilder<LoyaltyProgram> builder)
        {
            builder.ToTable("LoyaltyPrograms", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BusinessUserId).IsRequired();
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(2000);
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.StartsAtUtc);
            builder.Property(x => x.EndsAtUtc);

            builder.HasIndex(x => new { x.BusinessUserId, x.IsActive });
        }

        public void Configure(EntityTypeBuilder<LoyaltyRewardTier> builder)
        {
            builder.ToTable("LoyaltyRewardTiers", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProgramId).IsRequired();
            builder.Property(x => x.BusinessUserId).IsRequired();
            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.RequiredPoints).IsRequired();
            builder.Property(x => x.SortOrder).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();

            builder.HasIndex(x => new { x.ProgramId, x.SortOrder }).IsUnique();
            builder.HasIndex(x => x.BusinessUserId);

            builder.HasOne<LoyaltyProgram>()
                .WithMany()
                .HasForeignKey(x => x.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
        {
            builder.ToTable("LoyaltyAccounts", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BusinessUserId).IsRequired();
            builder.Property(x => x.ConsumerUserId).IsRequired();
            builder.Property(x => x.PointsBalance).IsRequired();
            builder.Property(x => x.TotalPointsEarned).IsRequired();
            builder.Property(x => x.TotalPointsRedeemed).IsRequired();
            builder.Property(x => x.LastActivityAtUtc).IsRequired();

            builder.HasIndex(x => new { x.BusinessUserId, x.ConsumerUserId })
                .IsUnique();
        }

        public void Configure(EntityTypeBuilder<LoyaltyPointsTransaction> builder)
        {
            builder.ToTable("LoyaltyPointTransactions", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.LoyaltyAccountId).IsRequired();
            builder.Property(x => x.BusinessUserId).IsRequired();
            builder.Property(x => x.ConsumerUserId).IsRequired();
            builder.Property(x => x.PointsDelta).IsRequired();
            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.Note).HasMaxLength(500);
            builder.Property(x => x.OccurredAtUtc).IsRequired();
            builder.Property(x => x.ScanSessionId);
            builder.Property(x => x.RewardTierId);

            builder.HasIndex(x => new { x.LoyaltyAccountId, x.OccurredAtUtc });

            builder.HasOne<LoyaltyAccount>()
                .WithMany()
                .HasForeignKey(x => x.LoyaltyAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<QrCodeToken> builder)
        {
            builder.ToTable("QrCodeTokens", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            builder.Property(x => x.IssuedAtUtc).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();
            builder.Property(x => x.IsConsumed).IsRequired();
            builder.Property(x => x.ConsumedAtUtc);
            builder.Property(x => x.Nonce).IsRequired().HasMaxLength(64);

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
        }

        public void Configure(EntityTypeBuilder<ScanSession> builder)
        {
            builder.ToTable("ScanSessions", schema: "Loyalty");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BusinessUserId).IsRequired();
            builder.Property(x => x.ConsumerUserId).IsRequired();
            builder.Property(x => x.StartedAtUtc).IsRequired();
            builder.Property(x => x.ExpiresAtUtc).IsRequired();
            builder.Property(x => x.IsClosed).IsRequired();
            builder.Property(x => x.ClosedAtUtc);

            builder.HasIndex(x => new { x.BusinessUserId, x.ExpiresAtUtc });
        }
    }
}
