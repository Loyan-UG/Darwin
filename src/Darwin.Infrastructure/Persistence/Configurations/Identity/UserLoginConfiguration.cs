using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for <see cref="UserLogin"/>.
    /// Maps external login bindings (e.g., Google, Microsoft) to the User aggregate.
    /// Ensures uniqueness per (UserId, Provider) and (Provider, ProviderKey) to avoid duplicates.
    /// </summary>
    public sealed class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
    {
        public void Configure(EntityTypeBuilder<UserLogin> builder)
        {
            builder.ToTable("UserLogins");

            // Primary key (inherited from BaseEntity) is Id (GUID).
            builder.HasKey(x => x.Id);

            // Required fields and reasonable max lengths
            builder.Property(x => x.Provider)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.ProviderKey)
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(x => x.DisplayName)
                   .HasMaxLength(256);

            // Foreign key to User
            builder.Property(x => x.UserId).IsRequired();

            builder.HasOne(x => x.User)
                   .WithMany(u => u.Logins)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Uniqueness:
            // A given provider should be linked once per user,
            // and ProviderKey should be globally unique per provider.
            builder.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
            builder.HasIndex(x => new { x.Provider, x.ProviderKey }).IsUnique();

            // Optional: quick lookup by User
            builder.HasIndex(x => x.UserId);
        }
    }
}
