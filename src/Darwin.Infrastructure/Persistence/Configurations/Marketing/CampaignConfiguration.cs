using Darwin.Domain.Entities.Marketing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Marketing
{
    /// <summary>
    /// EF Core mapping for <see cref="Campaign"/>.
    /// </summary>
    public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<Campaign> builder)
        {
            builder.ToTable("Campaigns", schema: "Marketing");

            builder.HasKey(x => x.Id);

            // Optional scope
            builder.Property(x => x.BusinessId);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Subtitle)
                .HasMaxLength(512);

            builder.Property(x => x.Body)
                .HasMaxLength(4000);

            builder.Property(x => x.MediaUrl)
                .HasMaxLength(1024);

            builder.Property(x => x.LandingUrl)
                .HasMaxLength(1024);

            builder.Property(x => x.Channels)
                .IsRequired();

            builder.Property(x => x.StartsAtUtc);
            builder.Property(x => x.EndsAtUtc);

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.TargetingJson)
                .IsRequired()
                .HasMaxLength(8000);

            builder.Property(x => x.PayloadJson)
                .IsRequired()
                .HasMaxLength(8000);

            // Indexes for feed selection and backoffice filters.
            builder.HasIndex(x => x.BusinessId)
                .HasDatabaseName("IX_Campaigns_BusinessId");

            builder.HasIndex(x => x.IsActive)
                .HasDatabaseName("IX_Campaigns_IsActive");

            builder.HasIndex(x => x.StartsAtUtc)
                .HasDatabaseName("IX_Campaigns_StartsAtUtc");

            builder.HasIndex(x => x.EndsAtUtc)
                .HasDatabaseName("IX_Campaigns_EndsAtUtc");
        }
    }
}
