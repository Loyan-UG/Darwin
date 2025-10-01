using Darwin.Domain.Entities.Identity;
using Darwin.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for <see cref="UserTwoFactorSecret"/>.
    /// Applies at-rest encryption for <see cref="UserTwoFactorSecret.SecretBase32"/>
    /// via a ValueConverter provided by <see cref="SecretProtectionConverterFactory"/>.
    /// </summary>
    public sealed class UserTwoFactorSecretConfiguration : IEntityTypeConfiguration<UserTwoFactorSecret>
    {
        /// <summary>
        /// Configures table name, keys, indexes, lengths, and the encryption converter.
        /// </summary>
        public void Configure(EntityTypeBuilder<UserTwoFactorSecret> b)
        {
            b.ToTable("UserTwoFactorSecrets");

            b.HasKey(x => x.Id);

            b.Property(x => x.UserId).IsRequired();

            // Encrypt/decrypt at-rest
            b.Property(x => x.SecretBase32)
             .HasConversion(SecretProtectionConverterFactory.CreateStringProtector())
             .HasMaxLength(512) // ample for protected payload
             .IsRequired();

            b.Property(x => x.Issuer).HasMaxLength(200).IsRequired();
            b.Property(x => x.Label).HasMaxLength(320).IsRequired(); // email-length safe

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.UserId, x.ActivatedAtUtc });

            // Optional: uncomment to enforce single secret per user
            // b.HasIndex(x => x.UserId).IsUnique();
        }
    }
}
