using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core mapping for <see cref="UserEngagementSnapshot"/>.
    /// This entity is a denormalized projection with intended uniqueness: one row per user.
    /// </summary>
    public sealed class UserEngagementSnapshotConfiguration : IEntityTypeConfiguration<UserEngagementSnapshot>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<UserEngagementSnapshot> builder)
        {
            builder.ToTable("UserEngagementSnapshots", schema: "Identity");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.LastActivityAtUtc);
            builder.Property(x => x.LastLoginAtUtc);
            builder.Property(x => x.LastLoyaltyActivityAtUtc);
            builder.Property(x => x.LastOrderAtUtc);

            builder.Property(x => x.EventCount)
                .IsRequired();

            builder.Property(x => x.EngagementScore30d)
                .IsRequired();

            builder.Property(x => x.CalculatedAtUtc)
                .IsRequired();

            builder.Property(x => x.SnapshotJson)
                .IsRequired()
                .HasMaxLength(8000);

            // Relationship to User is configured without relying on User-side navigation names.
            // The domain suggests "one row per user", so this mapping enforces a one-to-one relationship.
            builder.HasOne<User>()
                .WithOne()
                .HasForeignKey<UserEngagementSnapshot>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Enforce the intended uniqueness: one snapshot row per user.
            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasDatabaseName("UX_UserEngagementSnapshots_UserId");

            builder.HasIndex(x => x.CalculatedAtUtc)
                .HasDatabaseName("IX_UserEngagementSnapshots_CalculatedAtUtc");

            builder.HasIndex(x => x.LastActivityAtUtc)
                .HasDatabaseName("IX_UserEngagementSnapshots_LastActivityAtUtc");
        }
    }
}
