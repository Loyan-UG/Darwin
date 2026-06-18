using Darwin.Domain.Entities.Foundation;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class FoundationPrimitivesModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void FoundationPrimitives_Should_MapToFoundationSchema(string provider)
    {
        using var context = CreateContext(provider);

        GetEntity(context, typeof(CustomFieldDefinition)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(CustomFieldValue)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(Activity)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(Note)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(DocumentRecord)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(NumberSequence)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(BusinessEvent)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(AuditTrail)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(FeatureArea)).GetSchema().Should().Be("Foundation");
        GetEntity(context, typeof(BusinessFeatureOverride)).GetSchema().Should().Be("Foundation");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void CustomFields_Should_HaveStableColumnsAndIndexes(string provider)
    {
        using var context = CreateContext(provider);
        var definition = GetEntity(context, typeof(CustomFieldDefinition));
        var value = GetEntity(context, typeof(CustomFieldValue));

        definition.FindProperty(nameof(CustomFieldDefinition.TargetEntityType))!.GetMaxLength().Should().Be(128);
        definition.FindProperty(nameof(CustomFieldDefinition.Key))!.GetMaxLength().Should().Be(128);
        definition.FindProperty(nameof(CustomFieldDefinition.Label))!.GetMaxLength().Should().Be(200);
        definition.FindProperty(nameof(CustomFieldDefinition.DataType))!.GetMaxLength().Should().Be(32);
        definition.FindProperty(nameof(CustomFieldDefinition.Visibility))!.GetMaxLength().Should().Be(32);
        var scopedDefinitionIndex = definition.GetIndexes().Single(x => x.GetDatabaseName() == "UX_CustomFieldDefinitions_Business_Target_Key");
        scopedDefinitionIndex.IsUnique.Should().BeTrue();
        scopedDefinitionIndex.GetFilter().Should().Contain("BusinessId");
        var globalDefinitionIndex = definition.GetIndexes().Single(x => x.GetDatabaseName() == "UX_CustomFieldDefinitions_Global_Target_Key");
        globalDefinitionIndex.IsUnique.Should().BeTrue();
        globalDefinitionIndex.GetFilter().Should().Contain("BusinessId");

        value.FindProperty(nameof(CustomFieldValue.EntityType))!.GetMaxLength().Should().Be(128);
        value.FindProperty(nameof(CustomFieldValue.StringValue))!.GetMaxLength().Should().Be(4000);
        value.FindProperty(nameof(CustomFieldValue.NumberValue))!.GetPrecision().Should().Be(18);
        value.FindProperty(nameof(CustomFieldValue.NumberValue))!.GetScale().Should().Be(4);
        value.GetIndexes().Single(x => x.GetDatabaseName() == "UX_CustomFieldValues_Definition_Entity")
            .IsUnique.Should().BeTrue();
        value.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CustomFieldValues_EntityType_EntityId")
            .Properties.Select(x => x.Name).Should().Equal(
                nameof(CustomFieldValue.EntityType),
                nameof(CustomFieldValue.EntityId));
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void TimelineAndDocumentRecords_Should_HaveLookupIndexesAndRequiredMetadata(string provider)
    {
        using var context = CreateContext(provider);
        var activity = GetEntity(context, typeof(Activity));
        var note = GetEntity(context, typeof(Note));
        var document = GetEntity(context, typeof(DocumentRecord));

        activity.FindProperty(nameof(Activity.EntityType))!.GetMaxLength().Should().Be(128);
        activity.FindProperty(nameof(Activity.ActivityType))!.GetMaxLength().Should().Be(128);
        activity.FindProperty(nameof(Activity.Title))!.GetMaxLength().Should().Be(300);
        activity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Activities_EntityType_EntityId_OccurredAtUtc");

        note.FindProperty(nameof(Note.Body))!.GetMaxLength().Should().Be(4000);
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Notes_EntityType_EntityId_CreatedAtUtc");

        document.FindProperty(nameof(DocumentRecord.FileName))!.GetMaxLength().Should().Be(260);
        document.FindProperty(nameof(DocumentRecord.ContentType))!.GetMaxLength().Should().Be(128);
        document.FindProperty(nameof(DocumentRecord.StorageKey))!.GetMaxLength().Should().Be(1024);
        document.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DocumentRecords_EntityType_EntityId");
        document.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DocumentRecords_MediaAssetId");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void NumberSequence_Should_HaveStableColumnsAndNullableScopeIndexes(string provider)
    {
        using var context = CreateContext(provider);
        var sequence = GetEntity(context, typeof(NumberSequence));

        sequence.FindProperty(nameof(NumberSequence.DocumentType))!.GetMaxLength().Should().Be(32);
        sequence.FindProperty(nameof(NumberSequence.ScopeKey))!.GetMaxLength().Should().Be(128);
        sequence.FindProperty(nameof(NumberSequence.PrefixPattern))!.GetMaxLength().Should().Be(200);
        sequence.FindProperty(nameof(NumberSequence.ResetPolicy))!.GetMaxLength().Should().Be(32);
        sequence.FindProperty(nameof(NumberSequence.CurrentPeriodKey))!.GetMaxLength().Should().Be(32);
        sequence.FindProperty(nameof(NumberSequence.Description))!.GetMaxLength().Should().Be(1024);
        sequence.FindProperty(nameof(NumberSequence.MetadataJson))!.GetMaxLength().Should().Be(4000);

        var scopedIndex = sequence.GetIndexes().Single(x => x.GetDatabaseName() == "UX_NumberSequences_Business_DocumentType_Scope");
        scopedIndex.IsUnique.Should().BeTrue();
        scopedIndex.GetFilter().Should().Contain("BusinessId");
        var globalIndex = sequence.GetIndexes().Single(x => x.GetDatabaseName() == "UX_NumberSequences_Global_DocumentType_Scope");
        globalIndex.IsUnique.Should().BeTrue();
        globalIndex.GetFilter().Should().Contain("BusinessId");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void BusinessEventsAndAuditTrails_Should_HaveStableColumnsAndIndexes(string provider)
    {
        using var context = CreateContext(provider);
        var businessEvent = GetEntity(context, typeof(BusinessEvent));
        var auditTrail = GetEntity(context, typeof(AuditTrail));

        businessEvent.FindProperty(nameof(BusinessEvent.EntityType))!.GetMaxLength().Should().Be(128);
        businessEvent.FindProperty(nameof(BusinessEvent.EventType))!.GetMaxLength().Should().Be(128);
        businessEvent.FindProperty(nameof(BusinessEvent.EventKey))!.GetMaxLength().Should().Be(256);
        businessEvent.FindProperty(nameof(BusinessEvent.Source))!.GetMaxLength().Should().Be(32);
        businessEvent.FindProperty(nameof(BusinessEvent.Severity))!.GetMaxLength().Should().Be(32);
        businessEvent.FindProperty(nameof(BusinessEvent.Visibility))!.GetMaxLength().Should().Be(32);
        businessEvent.FindProperty(nameof(BusinessEvent.Title))!.GetMaxLength().Should().Be(300);
        businessEvent.FindProperty(nameof(BusinessEvent.PayloadJson))!.GetMaxLength().Should().Be(4000);
        businessEvent.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BusinessEvents_EntityType_EntityId_OccurredAtUtc");
        businessEvent.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BusinessEvents_BusinessId_OccurredAtUtc");
        businessEvent.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BusinessEvents_CorrelationId");
        var eventKeyIndex = businessEvent.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BusinessEvents_EventKey");
        eventKeyIndex.IsUnique.Should().BeTrue();
        eventKeyIndex.GetFilter().Should().Contain("EventKey");

        auditTrail.FindProperty(nameof(AuditTrail.EntityType))!.GetMaxLength().Should().Be(128);
        auditTrail.FindProperty(nameof(AuditTrail.Action))!.GetMaxLength().Should().Be(32);
        auditTrail.FindProperty(nameof(AuditTrail.Reason))!.GetMaxLength().Should().Be(2000);
        auditTrail.FindProperty(nameof(AuditTrail.CorrelationId))!.GetMaxLength().Should().Be(128);
        auditTrail.FindProperty(nameof(AuditTrail.ChangeSetJson))!.GetMaxLength().Should().Be(4000);
        auditTrail.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AuditTrails_EntityType_EntityId_OccurredAtUtc");
        auditTrail.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AuditTrails_BusinessId_OccurredAtUtc");
        auditTrail.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AuditTrails_BusinessEventId");
        auditTrail.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AuditTrails_CorrelationId");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void FeatureAreas_Should_HaveStableColumnsAndIndexes(string provider)
    {
        using var context = CreateContext(provider);
        var area = GetEntity(context, typeof(FeatureArea));
        var featureOverride = GetEntity(context, typeof(BusinessFeatureOverride));

        area.FindProperty(nameof(FeatureArea.Code))!.GetMaxLength().Should().Be(128);
        area.FindProperty(nameof(FeatureArea.Name))!.GetMaxLength().Should().Be(200);
        area.FindProperty(nameof(FeatureArea.Description))!.GetMaxLength().Should().Be(1024);
        area.FindProperty(nameof(FeatureArea.Category))!.GetMaxLength().Should().Be(32);
        area.FindProperty(nameof(FeatureArea.VisibilityScope))!.GetMaxLength().Should().Be(32);
        area.FindProperty(nameof(FeatureArea.RequiredPermissionKey))!.GetMaxLength().Should().Be(128);
        area.FindProperty(nameof(FeatureArea.MetadataJson))!.GetMaxLength().Should().Be(4000);
        var codeIndex = area.GetIndexes().Single(x => x.GetDatabaseName() == "UX_FeatureAreas_Code");
        codeIndex.IsUnique.Should().BeTrue();
        codeIndex.GetFilter().Should().Contain("IsActive");
        area.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FeatureAreas_Category_SortOrder");

        featureOverride.FindProperty(nameof(BusinessFeatureOverride.Reason))!.GetMaxLength().Should().Be(1024);
        featureOverride.FindProperty(nameof(BusinessFeatureOverride.MetadataJson))!.GetMaxLength().Should().Be(4000);
        var overrideIndex = featureOverride.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BusinessFeatureOverrides_Business_Feature");
        overrideIndex.IsUnique.Should().BeTrue();
        overrideIndex.GetFilter().Should().Contain("IsDeleted");
        featureOverride.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BusinessFeatureOverrides_Business_IsEnabled");
    }

    [Fact]
    public void PostgreSqlModel_Should_MapFoundationJsonColumnsToJsonb()
    {
        using var context = CreateContext("PostgreSql");

        GetEntity(context, typeof(CustomFieldDefinition))
            .FindProperty(nameof(CustomFieldDefinition.ValidationJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(CustomFieldDefinition))
            .FindProperty(nameof(CustomFieldDefinition.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(CustomFieldValue))
            .FindProperty(nameof(CustomFieldValue.JsonValue))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(Activity))
            .FindProperty(nameof(Activity.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(Note))
            .FindProperty(nameof(Note.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(DocumentRecord))
            .FindProperty(nameof(DocumentRecord.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(NumberSequence))
            .FindProperty(nameof(NumberSequence.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(BusinessEvent))
            .FindProperty(nameof(BusinessEvent.PayloadJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(BusinessEvent))
            .FindProperty(nameof(BusinessEvent.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(AuditTrail))
            .FindProperty(nameof(AuditTrail.ChangeSetJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(AuditTrail))
            .FindProperty(nameof(AuditTrail.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(FeatureArea))
            .FindProperty(nameof(FeatureArea.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(BusinessFeatureOverride))
            .FindProperty(nameof(BusinessFeatureOverride.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
    }

    private static IEntityType GetEntity(DarwinDbContext context, Type type)
        => context.Model.FindEntityType(type)!;

    private static DarwinDbContext CreateContext(string provider)
    {
        var builder = new DbContextOptionsBuilder<DarwinDbContext>();
        if (provider == "PostgreSql")
        {
            builder.UseNpgsql(DummyPostgreSqlConnectionString);
        }
        else
        {
            builder.UseSqlServer(DummySqlServerConnectionString);
        }

        return new DarwinDbContext(builder.Options);
    }
}
