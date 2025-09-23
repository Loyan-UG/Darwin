using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for <see cref="UserTwoFactorSecret"/>.
    /// Holds per-user TOTP secrets and metadata. Secrets should be encrypted at-rest at Infrastructure level.
    /// </summary>
    public sealed class UserTwoFactorSecretConfiguration : IEntityTypeConfiguration<UserTwoFactorSecret>
    {
        public void Configure(EntityTypeBuilder<UserTwoFactorSecret> builder)
        {
            builder.ToTable("UserTwoFactorSecrets");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId).IsRequired();

            builder.Property(x => x.SecretBase32)
                   .IsRequired()
                   .HasMaxLength(256); // Base32-encoded; length depends on policy

            builder.Property(x => x.Issuer)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Label)
                   .IsRequired()
                   .HasMaxLength(256);

            builder.Property(x => x.ActivatedAtUtc);

            builder.HasOne(x => x.User)
                   .WithMany(u => u.TwoFactorSecrets)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // A user may rotate secrets; if you want at most one active secret per user,
            // enforce it at application level or introduce an IsActive flag + unique filtered index.
            builder.HasIndex(x => x.UserId);
        }
    }
}
