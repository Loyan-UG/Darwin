using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// Arbitrary tokens for password reset, email confirm, etc. (if you store them here).
    /// </summary>
    public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            builder.ToTable("UserTokens");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.RowVersion).IsRowVersion();

            builder.Property(t => t.LoginProvider).HasMaxLength(64).IsRequired();
            builder.Property(t => t.Name).HasMaxLength(128).IsRequired();
            builder.Property(t => t.Value).HasMaxLength(2000);

            builder.HasIndex(t => new { t.UserId, t.LoginProvider, t.Name })
                   .IsUnique()
                   .HasDatabaseName("UX_UserToken_User_Provider_Name");

            builder.HasOne<User>()
                   .WithMany(u => u.Tokens)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
