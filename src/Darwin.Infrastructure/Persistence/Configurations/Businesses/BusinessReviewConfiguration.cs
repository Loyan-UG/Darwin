using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessReview"/>.
    /// Enforces: one active review per (UserId, BusinessId), plus practical indexes for discovery lists.
    /// </summary>
    public sealed class BusinessReviewConfiguration : IEntityTypeConfiguration<BusinessReview>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessReview> builder)
        {
            builder.ToTable("BusinessReviews", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.BusinessId).IsRequired();
            builder.Property(x => x.UserId).IsRequired();

            builder.Property(x => x.Rating).IsRequired();

            builder.Property(x => x.Comment)
                .HasMaxLength(2000);

            builder.Property(x => x.HiddenReason)
                .HasMaxLength(500);

            builder.Property(x => x.IsHidden).IsRequired();

            // Soft-delete-aware uniqueness (matches domain guidelines).
            builder.HasIndex(x => new { x.UserId, x.BusinessId })
                .IsUnique()
                .HasDatabaseName("UX_BusinessReviews_User_Business");

            // Useful for discovery lists: fetch reviews for a business while respecting moderation & soft delete.
            builder.HasIndex(x => new { x.BusinessId, x.IsHidden, x.IsDeleted })
                .HasDatabaseName("IX_BusinessReviews_Business_Visibility");
        }
    }
}
