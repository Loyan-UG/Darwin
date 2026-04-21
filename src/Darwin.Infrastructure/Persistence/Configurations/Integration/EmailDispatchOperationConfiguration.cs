using Darwin.Domain.Entities.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Integration;

public sealed class EmailDispatchOperationConfiguration : IEntityTypeConfiguration<EmailDispatchOperation>
{
    public void Configure(EntityTypeBuilder<EmailDispatchOperation> builder)
    {
        builder.ToTable("EmailDispatchOperations", schema: "Integration");

        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RecipientEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.IntendedRecipientEmail).HasMaxLength(320);
        builder.Property(x => x.Subject).HasMaxLength(512).IsRequired();
        builder.Property(x => x.HtmlBody).IsRequired();
        builder.Property(x => x.FlowKey).HasMaxLength(64);
        builder.Property(x => x.TemplateKey).HasMaxLength(128);
        builder.Property(x => x.CorrelationKey).HasMaxLength(128);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(1024);

        builder.HasIndex(x => new { x.Provider, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.RecipientEmail, x.FlowKey, x.BusinessId, x.CreatedAtUtc });
    }
}
