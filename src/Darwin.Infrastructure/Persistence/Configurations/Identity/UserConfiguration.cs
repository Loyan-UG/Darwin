using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for User entity:
    /// - Unique indexes on normalized identifiers
    /// - Reasonable max lengths for columns used in indexes
    /// - Concurrency token mapping (RowVersion)
    /// </summary>
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            // Keys & concurrency
            builder.HasKey(u => u.Id);
            builder.Property(u => u.RowVersion).IsRowVersion();

            // Indexes/uniques
            builder.Property(u => u.UserName).HasMaxLength(256);
            builder.Property(u => u.NormalizedUserName).HasMaxLength(256);
            builder.Property(u => u.Email).HasMaxLength(256);
            builder.Property(u => u.NormalizedEmail).HasMaxLength(256);

            builder.HasIndex(u => u.NormalizedUserName).IsUnique().HasDatabaseName("UX_User_NormalizedUserName");
            builder.HasIndex(u => u.NormalizedEmail).HasDatabaseName("IX_User_NormalizedEmail");

            // Flags
            builder.Property(u => u.IsSystem).HasDefaultValue(false);
            builder.Property(u => u.IsActive).HasDefaultValue(true);

            // Strings with sensible sizes
            builder.Property(u => u.PasswordHash).HasMaxLength(512);
            builder.Property(u => u.SecurityStamp).HasMaxLength(128);
            builder.Property(u => u.PhoneE164).HasMaxLength(32);
            builder.Property(u => u.Locale).HasMaxLength(16);
            builder.Property(u => u.Currency).HasMaxLength(3);
            builder.Property(u => u.Timezone).HasMaxLength(64);

            // JSON-ish blobs
            builder.Property(u => u.ChannelsOptInJson).HasMaxLength(4000);
            builder.Property(u => u.FirstTouchUtmJson).HasMaxLength(4000);
            builder.Property(u => u.LastTouchUtmJson).HasMaxLength(4000);
            builder.Property(u => u.ExternalIdsJson).HasMaxLength(4000);

            // Navigations kept optional; relationship FKs are configured in join configurations
        }
    }
}
