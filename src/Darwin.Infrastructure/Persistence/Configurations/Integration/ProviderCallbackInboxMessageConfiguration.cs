using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class ProviderCallbackInboxMessageConfiguration : IEntityTypeConfiguration<ProviderCallbackInboxMessage>
{
    public void Configure(EntityTypeBuilder<ProviderCallbackInboxMessage> builder)
    {
        builder.ToTable("ProviderCallbackInboxMessages", schema: "Integration");

        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CallbackType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(256);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1024);

        builder.HasIndex(x => new { x.Provider, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.IdempotencyKey);
    }
}
