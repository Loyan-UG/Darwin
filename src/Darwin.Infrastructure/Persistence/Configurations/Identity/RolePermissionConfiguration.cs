using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for RolePermission join table.
    /// Composite uniqueness on (RoleId, PermissionId).
    /// </summary>
    public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");

            builder.HasKey(rp => rp.Id);
            builder.Property(rp => rp.RowVersion).IsRowVersion();

            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                   .IsUnique()
                   .HasDatabaseName("UX_RolePermission_Role_Permission");

            builder.HasOne<Role>()
                   .WithMany(r => r.RolePermissions)
                   .HasForeignKey(rp => rp.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Permission>()
                   .WithMany()
                   .HasForeignKey(rp => rp.PermissionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
