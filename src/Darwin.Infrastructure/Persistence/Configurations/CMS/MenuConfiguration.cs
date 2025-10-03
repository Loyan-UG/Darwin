using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.CMS
{
    /// <summary>
    ///     EF Core configuration for <see cref="Menu"/>, <see cref="MenuItem"/>, and <see cref="MenuItemTranslation"/>.
    ///     Defines keys, relationships (including self-reference for item hierarchy), indexes, and column constraints.
    /// </summary>
    public sealed class MenuConfiguration :
        IEntityTypeConfiguration<Menu>,
        IEntityTypeConfiguration<MenuItem>,
        IEntityTypeConfiguration<MenuItemTranslation>
    {
        public void Configure(EntityTypeBuilder<Menu> builder)
        {
            builder.ToTable("Menus", schema: "CMS");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Name)
                   .IsRequired()
                   .HasMaxLength(128);

            builder.HasMany(m => m.Items)
                   .WithOne()
                   .HasForeignKey(i => i.MenuId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => m.Name).IsUnique(); // internal uniqueness to avoid duplicates like "Main"
        }

        public void Configure(EntityTypeBuilder<MenuItem> builder)
        {
            builder.ToTable("MenuItems", schema: "CMS");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Url)
                   .IsRequired()
                   .HasMaxLength(1024);

            builder.Property(i => i.SortOrder)
                   .HasDefaultValue(0);

            // Self-referencing optional parent for tree structure
            builder.HasOne<MenuItem>()
                   .WithMany()
                   .HasForeignKey(i => i.ParentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Useful indexes for rendering and editing
            builder.HasIndex(i => new { i.MenuId, i.ParentId, i.SortOrder }); // common list pattern
            builder.HasIndex(i => i.IsActive);
            builder.HasIndex(i => i.MenuId);

            // Translations: 1..N
            builder.HasMany(i => i.Translations)
                   .WithOne()
                   .HasForeignKey(t => t.MenuItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        }

        public void Configure(EntityTypeBuilder<MenuItemTranslation> builder)
        {
            builder.ToTable("MenuItemTranslations", schema: "CMS");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Culture)
                   .IsRequired()
                   .HasMaxLength(16);

            builder.Property(t => t.Label)
                   .IsRequired()
                   .HasMaxLength(256);

            // Each MenuItem can have at most one translation per culture
            builder.HasIndex(t => new { t.MenuItemId, t.Culture }).IsUnique();
        }
    }
}
