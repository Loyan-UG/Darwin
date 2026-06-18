using Darwin.Domain.Entities.Foundation;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class AiGovernanceFoundationCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void AiGovernanceModels_Should_Map_To_Foundation_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);

        var policy = GetEntity(context, typeof(AiSensitiveFieldPolicy));
        policy.GetSchema().Should().Be("Foundation");
        policy.GetTableName().Should().Be("AiSensitiveFieldPolicies");
        policy.FindProperty(nameof(AiSensitiveFieldPolicy.EntityType))!.GetMaxLength().Should().Be(128);
        policy.FindProperty(nameof(AiSensitiveFieldPolicy.FieldPath))!.GetMaxLength().Should().Be(256);
        policy.FindProperty(nameof(AiSensitiveFieldPolicy.PurposeKey))!.GetMaxLength().Should().Be(128);
        policy.FindProperty(nameof(AiSensitiveFieldPolicy.DataCategory))!.GetMaxLength().Should().Be(64);
        policy.FindProperty(nameof(AiSensitiveFieldPolicy.SensitivityLevel))!.GetMaxLength().Should().Be(32);
        policy.FindProperty(nameof(AiSensitiveFieldPolicy.Decision))!.GetMaxLength().Should().Be(32);
        policy.GetIndexes().Single(x => x.GetDatabaseName() == "UX_AiSensitiveFieldPolicies_Scope_Field_Purpose").IsUnique.Should().BeTrue();
        policy.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AiSensitiveFieldPolicies_Field_Active");

        var recommendation = GetEntity(context, typeof(AiRecommendation));
        recommendation.GetSchema().Should().Be("Foundation");
        recommendation.GetTableName().Should().Be("AiRecommendations");
        recommendation.FindProperty(nameof(AiRecommendation.Title))!.GetMaxLength().Should().Be(300);
        recommendation.FindProperty(nameof(AiRecommendation.Summary))!.GetMaxLength().Should().Be(2000);
        recommendation.FindProperty(nameof(AiRecommendation.Rationale))!.GetMaxLength().Should().Be(4000);
        recommendation.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AiRecommendations_Business_Status_CreatedAtUtc");
        recommendation.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AiRecommendations_SourceEntity");

        var draft = GetEntity(context, typeof(AiActionDraft));
        draft.GetSchema().Should().Be("Foundation");
        draft.GetTableName().Should().Be("AiActionDrafts");
        draft.FindProperty(nameof(AiActionDraft.CommandPayloadJson))!.GetMaxLength().Should().Be(8000);
        draft.FindProperty(nameof(AiActionDraft.RiskLevel))!.GetMaxLength().Should().Be(32);
        draft.FindProperty(nameof(AiActionDraft.Status))!.GetMaxLength().Should().Be(32);
        draft.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AiActionDrafts_Business_Status_CreatedAtUtc");
        draft.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AiActionDrafts_RecommendationId");
        draft.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AiActionDrafts_TargetEntity");

        var approval = GetEntity(context, typeof(AiActionApproval));
        approval.GetSchema().Should().Be("Foundation");
        approval.GetTableName().Should().Be("AiActionApprovals");
        approval.FindProperty(nameof(AiActionApproval.Decision))!.GetMaxLength().Should().Be(32);
        approval.FindProperty(nameof(AiActionApproval.Reason))!.GetMaxLength().Should().Be(1024);
        approval.GetIndexes().Single(x => x.GetDatabaseName() == "IX_AiActionApprovals_Draft_DecidedAtUtc");
    }

    [Fact]
    public void AiGovernanceFoundationCore_Migrations_Should_Add_Only_AiGovernance_Foundation_Tables()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*AiGovernanceFoundationCore.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("AiSensitiveFieldPolicies");
            migration.Should().Contain("AiRecommendations");
            migration.Should().Contain("AiActionDrafts");
            migration.Should().Contain("AiActionApprovals");
            migration.Should().Contain("UX_AiSensitiveFieldPolicies_Scope_Field_Purpose");
            migration.Should().NotContain("Prompt");
            migration.Should().NotContain("Completion");
            migration.Should().NotContain("ModelProvider");
            migration.Should().NotContain("OpenAI");
            migration.Should().NotContain("Payment");
            migration.Should().NotContain("JournalEntry");
            migration.Should().NotContain("Invoice");
            migration.Should().NotContain("PayrollRun");
            migration.Should().NotContain("BankAccount");
        }
    }

    private static IEntityType GetEntity(DarwinDbContext context, Type type)
        => context.Model.FindEntityType(type) ?? throw new InvalidOperationException($"Entity {type.Name} not found.");

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

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
