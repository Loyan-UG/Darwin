using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class ChannelDispatchOperationConfiguration : IEntityTypeConfiguration<ChannelDispatchOperation>
{
    public void Configure(EntityTypeBuilder<ChannelDispatchOperation> builder)
    {
        builder.ToTable("ChannelDispatchOperations", schema: "Integration");

        builder.Property(x => x.Channel).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecipientAddress).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IntendedRecipientAddress).HasMaxLength(256);
        builder.Property(x => x.MessageText).IsRequired();
        builder.Property(x => x.FlowKey).HasMaxLength(128);
        builder.Property(x => x.TemplateKey).HasMaxLength(128);
        builder.Property(x => x.CorrelationKey).HasMaxLength(128);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1024);

        builder.HasIndex(x => new { x.Channel, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => x.CorrelationKey);
        builder.HasIndex(x => x.IntendedRecipientAddress);
    }
}
