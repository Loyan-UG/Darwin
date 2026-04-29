using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration
{
    public sealed class ChannelDispatchAuditConfiguration : IEntityTypeConfiguration<ChannelDispatchAudit>
    {
        public void Configure(EntityTypeBuilder<ChannelDispatchAudit> builder)
        {
            builder.ToTable("ChannelDispatchAudits", schema: "Integration");

            builder.Property(x => x.Channel)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.FlowKey)
                .HasMaxLength(64);

            builder.Property(x => x.TemplateKey)
                .HasMaxLength(128);

            builder.Property(x => x.CorrelationKey)
                .HasMaxLength(128);

            builder.Property(x => x.RecipientAddress)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(x => x.IntendedRecipientAddress)
                .HasMaxLength(64);

            builder.Property(x => x.MessagePreview)
                .IsRequired()
                .HasMaxLength(240);

            builder.Property(x => x.ProviderMessageId)
                .HasMaxLength(256);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.FailureMessage)
                .HasMaxLength(2000);

            builder.HasIndex(x => x.AttemptedAtUtc);
            builder.HasIndex(x => x.Channel);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.FlowKey);
            builder.HasIndex(x => x.CorrelationKey)
                .HasDatabaseName("IX_ChannelDispatchAudits_CorrelationKey");
            builder.HasIndex(x => new { x.Channel, x.CorrelationKey })
                .IsUnique()
                .HasDatabaseName("UX_ChannelDispatchAudits_ActiveChannelCorrelation")
                .HasFilter("[CorrelationKey] IS NOT NULL AND [IsDeleted] = 0 AND [Status] IN (N'Pending', N'Sent')");
            builder.HasIndex(x => x.BusinessId);
            builder.HasIndex(x => x.RecipientAddress);
            builder.HasIndex(x => x.IntendedRecipientAddress);
        }
    }
}
