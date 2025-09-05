using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CMS
{
    public sealed class MenuConfiguration : IEntityTypeConfiguration<Menu>
    {
        public void Configure(EntityTypeBuilder<Menu> b)
        {
            b.ToTable("Menus");
            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.HasMany(m => m.Items).WithOne().HasForeignKey(i => i.MenuId).OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> b)
        {
            b.ToTable("MenuItems");
            b.Property(x => x.Label).IsRequired().HasMaxLength(200);
            b.Property(x => x.Url).IsRequired().HasMaxLength(400);
            b.HasIndex(x => new { x.MenuId, x.ParentId, x.SortOrder }).IsUnique().HasFilter("[IsDeleted] = 0");
        }
    }
}
