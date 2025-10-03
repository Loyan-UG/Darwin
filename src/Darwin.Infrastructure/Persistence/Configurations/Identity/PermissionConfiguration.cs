using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for Permission:
    /// - Unique Key
    /// </summary>
    public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions", schema: "Identity");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.RowVersion).IsRowVersion();

            builder.Property(p => p.Key).HasMaxLength(128).IsRequired();
            builder.Property(p => p.DisplayName).HasMaxLength(256).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(2000);
            builder.Property(p => p.IsSystem).HasDefaultValue(false);

            builder.HasIndex(p => p.Key).IsUnique().HasDatabaseName("UX_Permission_Key");
        }
    }
}
