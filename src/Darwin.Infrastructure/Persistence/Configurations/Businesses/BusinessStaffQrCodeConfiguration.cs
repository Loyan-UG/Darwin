using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core mapping for <see cref="BusinessStaffQrCode"/>.
    /// This entity stores short-lived staff QR tokens used for privileged business flows.
    /// </summary>
    public sealed class BusinessStaffQrCodeConfiguration : IEntityTypeConfiguration<BusinessStaffQrCode>
    {
        /// <summary>
        /// Configures table mapping, constraints, and indexes for <see cref="BusinessStaffQrCode"/>.
        /// </summary>
        /// <param name="builder">The EF Core entity builder.</param>
        public void Configure(EntityTypeBuilder<BusinessStaffQrCode> builder)
        {
            builder.ToTable("BusinessStaffQrCodes", schema: "Businesses");

            builder.HasKey(x => x.Id);

            // Required scalar fields
            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.BusinessMemberId)
                .IsRequired();

            builder.Property(x => x.Purpose)
                .IsRequired();

            builder.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.IssuedAtUtc)
                .IsRequired();

            builder.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            // Optional fields
            builder.Property(x => x.ConsumedAtUtc);
            builder.Property(x => x.RevokedAtUtc);

            builder.Property(x => x.IssuedDeviceId)
                .HasMaxLength(128);

            builder.Property(x => x.ConsumedDeviceId)
                .HasMaxLength(128);

            // Indexes:
            // - Token is expected to be unique to prevent collisions/replay ambiguity.
            // - ExpiresAtUtc is commonly queried for cleanup/validity checks.
            // - BusinessId/BusinessMemberId help operational queries and audits.
            builder.HasIndex(x => x.Token)
                .IsUnique();

            builder.HasIndex(x => x.ExpiresAtUtc);
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BusinessMemberId);

            // Relationship to Business is intentionally not configured here to avoid
            // coupling to optional navigations. The FK column exists (BusinessId),
            // and Business deletion semantics can be refined when hard-delete rules are finalized.
        }
    }
}
