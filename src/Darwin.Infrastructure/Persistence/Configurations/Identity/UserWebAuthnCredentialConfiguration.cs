using Darwin.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configuration.Identity
{
    /// <summary>
    /// EF Core configuration for <see cref="UserWebAuthnCredential"/>.
    /// Sets column types and indexes for efficient lookups.
    /// </summary>
    public sealed class UserWebAuthnCredentialConfiguration : IEntityTypeConfiguration<UserWebAuthnCredential>
    {
        public void Configure(EntityTypeBuilder<UserWebAuthnCredential> b)
        {
            b.ToTable("UserWebAuthnCredentials");

            b.HasKey(x => x.Id);

            b.Property(x => x.UserId).IsRequired();

            b.Property(x => x.CredentialId)
                .IsRequired();

            b.Property(x => x.PublicKey)
                .IsRequired();

            b.Property(x => x.AaGuid)
                .HasColumnType("uniqueidentifier"); // nullable

            b.Property(x => x.CredentialType)
                .HasMaxLength(50)
                .IsRequired();

            b.Property(x => x.AttestationFormat)
                .HasMaxLength(50);

            b.Property(x => x.SignatureCounter)
                .IsRequired();

            // Unique per user + credential id
            b.HasIndex(x => new { x.UserId, x.CredentialId }).IsUnique();

            // Fast lookup by CredentialId during login
            b.HasIndex(x => x.CredentialId);

            // Soft-delete/global filters handled elsewhere (if enabled)
        }
    }
}
