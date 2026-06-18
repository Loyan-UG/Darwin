using Darwin.Domain.Entities.Billing;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class FinancePostingFoundationModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void JournalEntryPostingFields_Should_MapToExistingBillingJournalEntriesTable(string provider)
    {
        using var context = CreateContext(provider);
        var entry = GetEntity(context, typeof(JournalEntry));

        entry.GetSchema().Should().Be("Billing");
        entry.GetTableName().Should().Be("JournalEntries");
        entry.FindProperty(nameof(JournalEntry.PostingStatus))!.GetMaxLength().Should().Be(32);
        entry.FindProperty(nameof(JournalEntry.PostingKind))!.GetMaxLength().Should().Be(32);
        entry.FindProperty(nameof(JournalEntry.PostingKey))!.GetMaxLength().Should().Be(256);
        entry.FindProperty(nameof(JournalEntry.SourceEntityType))!.GetMaxLength().Should().Be(128);
        entry.FindProperty(nameof(JournalEntry.SourceDocumentNumber))!.GetMaxLength().Should().Be(128);
        entry.FindProperty(nameof(JournalEntry.PostingReason))!.GetMaxLength().Should().Be(1000);
        entry.FindProperty(nameof(JournalEntry.MetadataJson))!.GetMaxLength().Should().Be(4000);
        entry.FindProperty(nameof(JournalEntry.PostingStatus))!.IsNullable.Should().BeFalse();
        entry.FindProperty(nameof(JournalEntry.PostingKind))!.IsNullable.Should().BeFalse();
        entry.FindProperty(nameof(JournalEntry.MetadataJson))!.IsNullable.Should().BeFalse();

        entry.GetIndexes().Single(x => x.GetDatabaseName() == "IX_JournalEntries_PostingStatus");
        entry.GetIndexes().Single(x => x.GetDatabaseName() == "IX_JournalEntries_PostingKind");
        entry.GetIndexes().Single(x => x.GetDatabaseName() == "IX_JournalEntries_SourceEntity");
        var postingKeyIndex = entry.GetIndexes().Single(x => x.GetDatabaseName() == "UX_JournalEntries_PostingKey");
        postingKeyIndex.IsUnique.Should().BeTrue();
        postingKeyIndex.GetFilter().Should().Contain("PostingKey");
        postingKeyIndex.GetFilter().Should().Contain("IsDeleted");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void FinancePostingAccountMappings_Should_MapBusinessScopedRoleMappings(string provider)
    {
        using var context = CreateContext(provider);
        var mapping = GetEntity(context, typeof(FinancePostingAccountMapping));

        mapping.GetSchema().Should().Be("Billing");
        mapping.GetTableName().Should().Be("FinancePostingAccountMappings");
        mapping.FindProperty(nameof(FinancePostingAccountMapping.BusinessId))!.IsNullable.Should().BeFalse();
        mapping.FindProperty(nameof(FinancePostingAccountMapping.FinancialAccountId))!.IsNullable.Should().BeFalse();
        mapping.FindProperty(nameof(FinancePostingAccountMapping.Role))!.GetMaxLength().Should().Be(64);
        mapping.FindProperty(nameof(FinancePostingAccountMapping.Role))!.GetProviderClrType().Should().Be(typeof(string));
        mapping.FindProperty(nameof(FinancePostingAccountMapping.IsActive))!.IsNullable.Should().BeFalse();
        mapping.FindProperty(nameof(FinancePostingAccountMapping.Description))!.GetMaxLength().Should().Be(500);
        mapping.FindProperty(nameof(FinancePostingAccountMapping.MetadataJson))!.GetMaxLength().Should().Be(4000);
        mapping.FindProperty(nameof(FinancePostingAccountMapping.MetadataJson))!.IsNullable.Should().BeFalse();

        mapping.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinancePostingAccountMappings_BusinessId");
        mapping.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinancePostingAccountMappings_Role");
        mapping.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinancePostingAccountMappings_FinancialAccountId");
        var activeRoleIndex = mapping.GetIndexes().Single(x => x.GetDatabaseName() == "UX_FinancePostingAccountMappings_Business_Role_Active");
        activeRoleIndex.IsUnique.Should().BeTrue();
        activeRoleIndex.GetFilter().Should().Contain("IsActive");
        activeRoleIndex.GetFilter().Should().Contain("IsDeleted");
    }

    [Fact]
    public void PostgreSqlModel_Should_MapJournalEntryMetadataToJsonb()
    {
        using var context = CreateContext("PostgreSql");

        GetEntity(context, typeof(JournalEntry))
            .FindProperty(nameof(JournalEntry.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(FinancePostingAccountMapping))
            .FindProperty(nameof(FinancePostingAccountMapping.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void FinanceExportBatches_Should_MapBusinessScopedExportIdentity(string provider)
    {
        using var context = CreateContext(provider);
        var batch = GetEntity(context, typeof(FinanceExportBatch));
        var attempt = GetEntity(context, typeof(FinanceExportAttempt));

        batch.GetSchema().Should().Be("Billing");
        batch.GetTableName().Should().Be("FinanceExportBatches");
        batch.FindProperty(nameof(FinanceExportBatch.BusinessId))!.IsNullable.Should().BeFalse();
        batch.FindProperty(nameof(FinanceExportBatch.ExternalSystemId))!.IsNullable.Should().BeFalse();
        batch.FindProperty(nameof(FinanceExportBatch.ExportKey))!.GetMaxLength().Should().Be(256);
        batch.FindProperty(nameof(FinanceExportBatch.PostingStatusMode))!.GetProviderClrType().Should().Be(typeof(string));
        batch.FindProperty(nameof(FinanceExportBatch.PostingStatusMode))!.GetMaxLength().Should().Be(64);
        batch.FindProperty(nameof(FinanceExportBatch.Status))!.GetProviderClrType().Should().Be(typeof(string));
        batch.FindProperty(nameof(FinanceExportBatch.Status))!.GetMaxLength().Should().Be(64);
        batch.FindProperty(nameof(FinanceExportBatch.PackageHashSha256))!.GetMaxLength().Should().Be(128);
        batch.FindProperty(nameof(FinanceExportBatch.PackageContentType))!.GetMaxLength().Should().Be(128);
        batch.FindProperty(nameof(FinanceExportBatch.PackageFileName))!.GetMaxLength().Should().Be(260);
        batch.FindProperty(nameof(FinanceExportBatch.ErrorSummary))!.GetMaxLength().Should().Be(1000);
        batch.FindProperty(nameof(FinanceExportBatch.MetadataJson))!.GetMaxLength().Should().Be(4000);

        batch.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinanceExportBatches_BusinessId");
        batch.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinanceExportBatches_ExternalSystemId");
        batch.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinanceExportBatches_Status");
        batch.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinanceExportBatches_BusinessId_PeriodStartUtc_PeriodEndUtc");
        var uniqueBatchIndex = batch.GetIndexes().Single(x => x.GetDatabaseName() == "UX_FinanceExportBatches_Business_Target_Key_Active");
        uniqueBatchIndex.IsUnique.Should().BeTrue();
        uniqueBatchIndex.GetFilter().Should().Contain("IsDeleted");

        attempt.GetSchema().Should().Be("Billing");
        attempt.GetTableName().Should().Be("FinanceExportAttempts");
        attempt.FindProperty(nameof(FinanceExportAttempt.Status))!.GetProviderClrType().Should().Be(typeof(string));
        attempt.FindProperty(nameof(FinanceExportAttempt.Status))!.GetMaxLength().Should().Be(64);
        attempt.FindProperty(nameof(FinanceExportAttempt.PackageHashSha256))!.GetMaxLength().Should().Be(128);
        attempt.FindProperty(nameof(FinanceExportAttempt.ErrorSummary))!.GetMaxLength().Should().Be(1000);
        attempt.FindProperty(nameof(FinanceExportAttempt.MetadataJson))!.GetMaxLength().Should().Be(4000);
        attempt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinanceExportAttempts_FinanceExportBatchId");
        attempt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_FinanceExportAttempts_Status");
        var uniqueAttemptIndex = attempt.GetIndexes().Single(x => x.GetDatabaseName() == "UX_FinanceExportAttempts_Batch_AttemptNumber_Active");
        uniqueAttemptIndex.IsUnique.Should().BeTrue();
        uniqueAttemptIndex.GetFilter().Should().Contain("IsDeleted");
    }

    [Fact]
    public void PostgreSqlModel_Should_MapFinanceExportMetadataToJsonb()
    {
        using var context = CreateContext("PostgreSql");

        GetEntity(context, typeof(FinanceExportBatch))
            .FindProperty(nameof(FinanceExportBatch.MetadataJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(FinanceExportAttempt))
            .FindProperty(nameof(FinanceExportAttempt.MetadataJson))!
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
