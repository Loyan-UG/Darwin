using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// Stores TOTP/2FA shared secrets (per app). Unique per (UserId, Provider).
    /// </summary>
    public sealed class UserTwoFactorSecretConfiguration : IEntityTypeConfiguration<UserTwoFactorSecret>
    {
        public void Configure(EntityTypeBuilder<UserTwoFactorSecret> builder)
        {
            builder.ToTable("UserTwoFactorSecrets");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.RowVersion).IsRowVersion();

            builder.Property(s => s.Provider).HasMaxLength(64).IsRequired();
            builder.Property(s => s.Secret).HasMaxLength(512).IsRequired();

            builder.HasIndex(s => new { s.UserId, s.Provider })
                   .IsUnique()
                   .HasDatabaseName("UX_User2FA_User_Provider");

            builder.HasOne<User>()
                   .WithMany(u => u.TwoFactorSecrets)
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
