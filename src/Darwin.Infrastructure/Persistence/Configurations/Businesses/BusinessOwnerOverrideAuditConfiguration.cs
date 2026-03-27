using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Businesses
{
    /// <summary>
    /// EF Core configuration for <see cref="BusinessOwnerOverrideAudit"/>.
    /// </summary>
    public sealed class BusinessOwnerOverrideAuditConfiguration : IEntityTypeConfiguration<BusinessOwnerOverrideAudit>
    {
        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<BusinessOwnerOverrideAudit> builder)
        {
            builder.ToTable("BusinessOwnerOverrideAudits", schema: "Businesses");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.Property(x => x.BusinessId).IsRequired();
            builder.Property(x => x.BusinessMemberId).IsRequired();
            builder.Property(x => x.AffectedUserId).IsRequired();
            builder.Property(x => x.ActionKind).IsRequired();
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
            builder.Property(x => x.ActorDisplayName).HasMaxLength(200);

            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.BusinessMemberId);
            builder.HasIndex(x => x.AffectedUserId);
            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
}
