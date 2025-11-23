using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessMember"/>.
    /// </summary>
    public sealed class BusinessMemberConfiguration : IEntityTypeConfiguration<BusinessMember>
    {
        public void Configure(EntityTypeBuilder<BusinessMember> b)
        {
            b.ToTable("BusinessMembers", schema: "Businesses");

            b.HasKey(x => x.Id);

            b.Property(x => x.BusinessId).IsRequired();
            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.Role).IsRequired();
            b.Property(x => x.IsActive).IsRequired();

            b.HasIndex(x => x.BusinessId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.BusinessId, x.UserId }).IsUnique();
        }
    }
}
