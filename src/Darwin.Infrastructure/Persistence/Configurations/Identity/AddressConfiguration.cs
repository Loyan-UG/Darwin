using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for <see cref="Address"/> in the Identity aggregate.
    /// Defines column lengths, indexes and relationships.
    /// </summary>
    public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        /// <summary>
        /// Configures the Address entity: required fields, max lengths, indexes for common lookups,
        /// and relationship to <c>User</c>.
        /// </summary>
        public void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("Addresses", schema: "Identity");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FullName).HasMaxLength(200);
            builder.Property(x => x.Company).HasMaxLength(200);
            builder.Property(x => x.Street1).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Street2).HasMaxLength(300);
            builder.Property(x => x.PostalCode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.City).HasMaxLength(150).IsRequired();
            builder.Property(x => x.State).HasMaxLength(150);
            builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            builder.Property(x => x.PhoneE164).HasMaxLength(20);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => new { x.UserId, x.IsDefaultBilling });
            builder.HasIndex(x => new { x.UserId, x.IsDefaultShipping });

            builder
                .HasOne<User>()                // no navigation on principal
                 .WithMany()                    // no collection on User side
                 .HasForeignKey(x => x.UserId)  // FK is Address.UserId (nullable)
                 .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
