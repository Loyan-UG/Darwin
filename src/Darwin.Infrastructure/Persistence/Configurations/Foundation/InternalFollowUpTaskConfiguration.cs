using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class InternalFollowUpTaskConfiguration : IEntityTypeConfiguration<InternalFollowUpTask>
{
    public void Configure(EntityTypeBuilder<InternalFollowUpTask> builder)
    {
        builder.ToTable("InternalFollowUpTasks", schema: "Foundation");

        builder.Property(x => x.FeatureAreaCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TargetEntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CompletionNotes).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.BusinessId, x.Status, x.DueAtUtc })
            .HasDatabaseName("IX_InternalFollowUpTasks_Business_Status_DueAtUtc");

        builder.HasIndex(x => new { x.TargetEntityType, x.TargetEntityId })
            .HasDatabaseName("IX_InternalFollowUpTasks_Target");

        builder.HasIndex(x => x.AssignedToUserId)
            .HasDatabaseName("IX_InternalFollowUpTasks_AssignedToUserId");

        builder.HasIndex(x => x.SourceAiActionDraftId)
            .HasDatabaseName("IX_InternalFollowUpTasks_SourceAiActionDraftId");
    }
}
