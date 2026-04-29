using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration
{
    /// <summary>
    /// EF configuration for phase-1 email dispatch audit entries.
    /// </summary>
    public sealed class EmailDispatchAuditConfiguration : IEntityTypeConfiguration<EmailDispatchAudit>
    {
        public void Configure(EntityTypeBuilder<EmailDispatchAudit> builder)
        {
            builder.ToTable("EmailDispatchAudits", schema: "Integration");

            builder.Property(x => x.Provider)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.FlowKey)
                .HasMaxLength(64);

            builder.Property(x => x.TemplateKey)
                .HasMaxLength(128);

            builder.Property(x => x.CorrelationKey)
                .HasMaxLength(128);

            builder.Property(x => x.RecipientEmail)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(x => x.IntendedRecipientEmail)
                .HasMaxLength(320);

            builder.Property(x => x.Subject)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.ProviderMessageId)
                .HasMaxLength(256);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.FailureMessage)
                .HasMaxLength(2000);

            builder.HasIndex(x => x.AttemptedAtUtc);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.RecipientEmail);
            builder.HasIndex(x => x.IntendedRecipientEmail);
            builder.HasIndex(x => x.FlowKey);
            builder.HasIndex(new[] { nameof(EmailDispatchAudit.CorrelationKey) }, "IX_EmailDispatchAudits_CorrelationKey_Model")
                .HasDatabaseName("IX_EmailDispatchAudits_CorrelationKey");
            builder.HasIndex(new[] { nameof(EmailDispatchAudit.CorrelationKey) }, "UX_EmailDispatchAudits_ActiveCorrelation_Model")
                .IsUnique()
                .HasDatabaseName("UX_EmailDispatchAudits_ActiveCorrelation")
                .HasFilter("[CorrelationKey] IS NOT NULL AND [IsDeleted] = 0 AND [Status] IN (N'Pending', N'Sent')");
            builder.HasIndex(x => x.BusinessId);
        }
    }
}
