using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Identity
{
    /// <summary>
    /// EF Core configuration for <see cref="UserToken"/>.
    /// Stores arbitrary tokens (email confirmation, recovery codes, etc.) with optional expiry and usage time.
    /// Enforces uniqueness per (UserId, Purpose) if your flows keep one active token per purpose.
    /// </summary>
    public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            builder.ToTable("UserTokens", schema: "Identity");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId).IsRequired();

            builder.Property(x => x.Purpose)
                   .IsRequired()
                   .HasMaxLength(100);

            // The token Value can be long (hashed/opaque). Keep it required.
            builder.Property(x => x.Value)
                   .IsRequired()
                   .HasMaxLength(4000);

            builder.Property(x => x.ExpiresAtUtc);
            builder.Property(x => x.UsedAtUtc);

            builder.HasOne<Darwin.Domain.Entities.Identity.User>()
                   .WithMany(u => u.Tokens)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // If you want only one active token per purpose per user, keep this unique index.
            builder.HasIndex(x => new { x.UserId, x.Purpose }).IsUnique();

            // Useful lookups
            builder.HasIndex(x => x.ExpiresAtUtc);
        }
    }
}
