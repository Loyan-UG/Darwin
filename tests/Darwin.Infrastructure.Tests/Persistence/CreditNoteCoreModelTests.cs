using Darwin.Domain.Entities.Sales;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class CreditNoteCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void CreditNote_Should_Map_To_Sales_Schema_With_Source_And_Posting_Metadata(string provider)
    {
        using var context = CreateContext(provider);
        var note = GetEntity(context, typeof(CreditNote));
        var line = GetEntity(context, typeof(CreditNoteLine));

        note.GetSchema().Should().Be("Sales");
        note.GetTableName().Should().Be("CreditNotes");
        note.FindProperty(nameof(CreditNote.CreditNoteNumber))!.GetMaxLength().Should().Be(50);
        note.FindProperty(nameof(CreditNote.Status))!.GetMaxLength().Should().Be(32);
        note.FindProperty(nameof(CreditNote.Reason))!.GetMaxLength().Should().Be(48);
        note.FindProperty(nameof(CreditNote.Currency))!.GetMaxLength().Should().Be(3);
        note.FindProperty(nameof(CreditNote.SourceModelJson))!.GetMaxLength().Should().Be(32000);
        note.FindProperty(nameof(CreditNote.SourceModelHashSha256))!.GetMaxLength().Should().Be(64);
        note.FindProperty(nameof(CreditNote.MetadataJson))!.GetMaxLength().Should().Be(16000);

        if (provider == "PostgreSql")
        {
            note.FindProperty(nameof(CreditNote.SourceModelJson))!.GetColumnType().Should().Be("jsonb");
            note.FindProperty(nameof(CreditNote.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            line.FindProperty(nameof(CreditNoteLine.SourceLineJson))!.GetColumnType().Should().Be("jsonb");
        }

        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNotes_CreditNoteNumber").IsUnique.Should().BeTrue();
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNotes_InvoiceId");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNotes_ReturnOrderId");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNotes_RefundId");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNotes_Status");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNotes_IssuedAtUtc");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNotes_ArchiveRetainUntilUtc");

        line.GetSchema().Should().Be("Sales");
        line.GetTableName().Should().Be("CreditNoteLines");
        line.FindProperty(nameof(CreditNoteLine.Description))!.GetMaxLength().Should().Be(400);
        line.FindProperty(nameof(CreditNoteLine.TaxRate))!.GetPrecision().Should().Be(18);
        line.FindProperty(nameof(CreditNoteLine.TaxRate))!.GetScale().Should().Be(4);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CreditNoteLines_CreditNote_SortOrder")
            .Properties.Select(x => x.Name).Should().Equal(nameof(CreditNoteLine.CreditNoteId), nameof(CreditNoteLine.SortOrder));
    }

    [Fact]
    public void CreditNote_Migrations_Should_Create_Only_CreditNote_Tables()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*CreditNoteCoreModel.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("CreditNotes");
            migration.Should().Contain("CreditNoteLines");
            migration.Should().Contain("IX_CreditNotes_CreditNoteNumber");
            migration.Should().NotContain("SalesOrders");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
            migration.Should().NotContain("NegativeInvoice");
        }

        var postgreSqlMigration = migrationFiles.Single(x => x.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase));
        File.ReadAllText(postgreSqlMigration).Should().Contain("type: \"jsonb\"");
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Darwin repository root.");
    }
}
