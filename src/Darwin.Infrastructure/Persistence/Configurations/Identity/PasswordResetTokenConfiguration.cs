using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// One-time password reset tokens with expiry; unique per (UserId, Token) while active.
    /// </summary>
    public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("PasswordResetTokens");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.RowVersion).IsRowVersion();

            builder.Property(p => p.Token).HasMaxLength(128).IsRequired();
            builder.Property(p => p.ExpiresAtUtc).IsRequired();

            builder.HasIndex(p => new { p.UserId, p.Token })
                   .IsUnique()
                   .HasDatabaseName("UX_ResetToken_User_Token");

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
