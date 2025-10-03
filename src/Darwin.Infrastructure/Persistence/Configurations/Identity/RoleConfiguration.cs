using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for Role:
    /// - Unique Name + NormalizedName
    /// - Concurrency token mapping
    /// </summary>
    public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles", schema: "Identity");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.RowVersion).IsRowVersion();

            builder.Property(r => r.Key).HasMaxLength(128).IsRequired();
            builder.Property(r => r.NormalizedName).HasMaxLength(128).IsRequired();

            builder.HasIndex(r => r.Key).IsUnique().HasDatabaseName("UX_Role_Name");
            builder.HasIndex(r => r.NormalizedName).IsUnique().HasDatabaseName("UX_Role_NormalizedName");

            builder.Property(r => r.IsSystem).HasDefaultValue(false);
            builder.Property(r => r.Description).HasMaxLength(1024);
        }
    }
}
