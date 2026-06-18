using Darwin.Domain.Entities.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Darwin.Infrastructure.Persistence.Configurations.Foundation;

public sealed class AiSensitiveFieldPolicyConfiguration : IEntityTypeConfiguration<AiSensitiveFieldPolicy>
{
    public void Configure(EntityTypeBuilder<AiSensitiveFieldPolicy> builder)
    {
        builder.ToTable("AiSensitiveFieldPolicies", schema: "Foundation");

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FieldPath).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PurposeKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DataCategory).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.SensitivityLevel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.RedactionRule).HasMaxLength(512);
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.BusinessId, x.EntityType, x.FieldPath, x.PurposeKey })
            .IsUnique()
            .HasDatabaseName("UX_AiSensitiveFieldPolicies_Scope_Field_Purpose")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.EntityType, x.FieldPath, x.IsActive })
            .HasDatabaseName("IX_AiSensitiveFieldPolicies_Field_Active");
    }
}

public sealed class AiRecommendationConfiguration : IEntityTypeConfiguration<AiRecommendation>
{
    public void Configure(EntityTypeBuilder<AiRecommendation> builder)
    {
        builder.ToTable("AiRecommendations", schema: "Foundation");

        builder.Property(x => x.FeatureAreaCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RecommendationType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SourceEntityType).HasMaxLength(128);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Rationale).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReviewReason).HasMaxLength(1024);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.BusinessId, x.Status, x.CreatedAtUtc })
            .HasDatabaseName("IX_AiRecommendations_Business_Status_CreatedAtUtc");

        builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId })
            .HasDatabaseName("IX_AiRecommendations_SourceEntity");
    }
}

public sealed class AiActionDraftConfiguration : IEntityTypeConfiguration<AiActionDraft>
{
    public void Configure(EntityTypeBuilder<AiActionDraft> builder)
    {
        builder.ToTable("AiActionDrafts", schema: "Foundation");

        builder.Property(x => x.FeatureAreaCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TargetEntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CommandType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CommandPayloadJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReviewReason).HasMaxLength(1024);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.BusinessId, x.Status, x.CreatedAtUtc })
            .HasDatabaseName("IX_AiActionDrafts_Business_Status_CreatedAtUtc");

        builder.HasIndex(x => x.RecommendationId)
            .HasDatabaseName("IX_AiActionDrafts_RecommendationId");

        builder.HasIndex(x => new { x.TargetEntityType, x.TargetEntityId })
            .HasDatabaseName("IX_AiActionDrafts_TargetEntity");
    }
}

public sealed class AiActionApprovalConfiguration : IEntityTypeConfiguration<AiActionApproval>
{
    public void Configure(EntityTypeBuilder<AiActionApproval> builder)
    {
        builder.ToTable("AiActionApprovals", schema: "Foundation");

        builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.MetadataJson).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => new { x.AiActionDraftId, x.DecidedAtUtc })
            .HasDatabaseName("IX_AiActionApprovals_Draft_DecidedAtUtc");
    }
}
