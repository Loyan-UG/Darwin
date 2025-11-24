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
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessMember> builder)
        {
            builder.ToTable("BusinessMembers", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.BusinessId).IsRequired();
            builder.Property(x => x.UserId).IsRequired();
            builder.Property(x => x.Role).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();

            // A user should not have multiple memberships for the same business.
            builder.HasIndex(x => new { x.BusinessId, x.UserId }).IsUnique();

            builder.HasIndex(x => x.UserId);
        }

    }
}
