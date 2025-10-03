using Darwin.Domain.Entities.SEO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.SEO
{
    public sealed class RedirectRuleConfiguration : IEntityTypeConfiguration<RedirectRule>
    {
        public void Configure(EntityTypeBuilder<RedirectRule> b)
        {
            b.ToTable("RedirectRules", schema: "SEO");
            b.Property(x => x.FromPath).IsRequired().HasMaxLength(400);
            b.Property(x => x.To).IsRequired().HasMaxLength(400);

            b.HasIndex(x => x.FromPath).IsUnique().HasFilter("[IsDeleted] = 0");
        }
    }
}
