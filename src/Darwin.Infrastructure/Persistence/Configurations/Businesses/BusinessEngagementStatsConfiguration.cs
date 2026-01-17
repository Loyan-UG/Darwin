using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessEngagementStats"/>.
    /// This is a system-maintained snapshot table; business logic should not use soft-delete for it.
    /// </summary>
    public sealed class BusinessEngagementStatsConfiguration : IEntityTypeConfiguration<BusinessEngagementStats>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessEngagementStats> builder)
        {
            builder.ToTable("BusinessEngagementStats", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.BusinessId).IsRequired();
            builder.Property(x => x.RatingCount).IsRequired();
            builder.Property(x => x.RatingSum).IsRequired();
            builder.Property(x => x.LikeCount).IsRequired();
            builder.Property(x => x.FavoriteCount).IsRequired();

            builder.Property(x => x.LastCalculatedAtUtc);

            // Enforce one row per business (NO IsDeleted filter; this is not user-controlled).
            builder.HasIndex(x => x.BusinessId)
                .IsUnique()
                .HasDatabaseName("UX_BusinessEngagementStats_BusinessId");

            // Relationship configured here to avoid overload mismatch in BusinessConfiguration.
            // 1:1 relationship (one stats row per business).
            builder.HasOne(x => x.Business)
                .WithOne(b => b.EngagementStats)
                .HasForeignKey<BusinessEngagementStats>(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
