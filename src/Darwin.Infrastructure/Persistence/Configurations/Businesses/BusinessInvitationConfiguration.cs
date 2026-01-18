using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core mapping for <see cref="BusinessInvitation"/>.
    /// </summary>
    public sealed class BusinessInvitationConfiguration : IEntityTypeConfiguration<BusinessInvitation>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessInvitation> builder)
        {
            builder.ToTable("BusinessInvitations", schema: "Businesses");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.BusinessId)
                .IsRequired();

            builder.Property(x => x.InvitedByUserId)
                .IsRequired();

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(x => x.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(x => x.Role)
                .IsRequired();

            builder.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.Note)
                .HasMaxLength(2000);

            // Optional timestamps/ids
            builder.Property(x => x.AcceptedAtUtc);
            builder.Property(x => x.AcceptedByUserId);
            builder.Property(x => x.RevokedAtUtc);

            // Token is an opaque invitation token; it must be unique to avoid collisions.
            builder.HasIndex(x => x.Token)
                .IsUnique()
                .HasDatabaseName("UX_BusinessInvitations_Token");

            // Fast lookup for "invite by email within a business" scenarios.
            builder.HasIndex(x => new { x.BusinessId, x.NormalizedEmail })
                .HasDatabaseName("IX_BusinessInvitations_Business_NormalizedEmail");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_BusinessInvitations_Status");

            builder.HasIndex(x => x.ExpiresAtUtc)
                .HasDatabaseName("IX_BusinessInvitations_ExpiresAtUtc");

            // Relationship is intentionally configured without depending on Business navigation properties.
            builder.HasOne<Darwin.Domain.Entities.Businesses.Business>()
                .WithMany()
                .HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
