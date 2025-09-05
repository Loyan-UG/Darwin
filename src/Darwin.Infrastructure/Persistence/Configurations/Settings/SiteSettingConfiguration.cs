using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Settings
{
    public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
    {
        public void Configure(EntityTypeBuilder<SiteSetting> b)
        {
            b.ToTable("SiteSettings");
            b.Property(x => x.DefaultCulture).IsRequired().HasMaxLength(10);
            b.Property(x => x.DefaultCountry).IsRequired().HasMaxLength(2);
            b.Property(x => x.DefaultCurrency).IsRequired().HasMaxLength(3);

            // Keep one active row if desired (optional): not enforced now to allow versioning later.
        }
    }
}
