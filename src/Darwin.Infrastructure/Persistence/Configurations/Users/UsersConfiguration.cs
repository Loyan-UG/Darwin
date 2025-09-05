using Darwin.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Users
{
    public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> b)
        {
            b.ToTable("UserProfiles");
            b.Property(x => x.Locale).HasMaxLength(10);
            b.Property(x => x.Currency).HasMaxLength(3);
            b.Property(x => x.Timezone).HasMaxLength(100);
        }
    }

    public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
    {
        public void Configure(EntityTypeBuilder<Address> b)
        {
            b.ToTable("Addresses");
            b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            b.Property(x => x.PostalCode).IsRequired().HasMaxLength(20);
            b.Property(x => x.City).IsRequired().HasMaxLength(200);
            b.Property(x => x.CountryCode).IsRequired().HasMaxLength(2);
        }
    }
}
