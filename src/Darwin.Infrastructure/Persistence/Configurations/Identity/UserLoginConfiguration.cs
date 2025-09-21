using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// External logins (Google/Microsoft/etc). Unique per (Provider, ProviderKey).
    /// </summary>
    public sealed class UserLoginConfiguration : IEntityTypeConfiguration<UserLogin>
    {
        public void Configure(EntityTypeBuilder<UserLogin> builder)
        {
            builder.ToTable("UserLogins");

            builder.HasKey(l => l.Id);
            builder.Property(l => l.RowVersion).IsRowVersion();

            builder.Property(l => l.LoginProvider).HasMaxLength(64).IsRequired();
            builder.Property(l => l.ProviderKey).HasMaxLength(256).IsRequired();
            builder.Property(l => l.ProviderDisplayName).HasMaxLength(256);

            builder.HasIndex(l => new { l.LoginProvider, l.ProviderKey })
                   .IsUnique()
                   .HasDatabaseName("UX_UserLogin_Provider_Key");

            builder.HasOne<User>()
                   .WithMany(u => u.Logins)
                   .HasForeignKey(l => l.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
