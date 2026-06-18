using Darwin.Domain.Entities.Sales;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class DeliveryNoteCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void DeliveryNote_Should_Map_To_Sales_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);

        var note = GetEntity(context, typeof(DeliveryNote));
        var line = GetEntity(context, typeof(DeliveryNoteLine));

        note.GetSchema().Should().Be("Sales");
        note.GetTableName().Should().Be("DeliveryNotes");
        note.FindProperty(nameof(DeliveryNote.DeliveryNoteNumber))!.GetMaxLength().Should().Be(50);
        note.FindProperty(nameof(DeliveryNote.Status))!.GetMaxLength().Should().Be(32);
        note.FindProperty(nameof(DeliveryNote.Currency))!.GetMaxLength().Should().Be(3);
        note.FindProperty(nameof(DeliveryNote.Carrier))!.GetMaxLength().Should().Be(100);
        note.FindProperty(nameof(DeliveryNote.Service))!.GetMaxLength().Should().Be(100);
        note.FindProperty(nameof(DeliveryNote.TrackingNumber))!.GetMaxLength().Should().Be(120);
        note.FindProperty(nameof(DeliveryNote.ProviderShipmentReference))!.GetMaxLength().Should().Be(160);
        note.FindProperty(nameof(DeliveryNote.ShippingAddressJson))!.GetMaxLength().Should().Be(16000);
        note.FindProperty(nameof(DeliveryNote.InternalNotes))!.GetMaxLength().Should().Be(2000);
        note.FindProperty(nameof(DeliveryNote.MetadataJson))!.GetMaxLength().Should().Be(16000);
        note.FindProperty(nameof(DeliveryNote.Status))!.GetColumnType().Should().ContainAny("nvarchar", "character varying");

        if (provider == "PostgreSql")
        {
            note.FindProperty(nameof(DeliveryNote.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        var numberIndex = note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_DeliveryNoteNumber");
        numberIndex.IsUnique.Should().BeTrue();
        numberIndex.GetFilter().Should().Contain("DeliveryNoteNumber");
        numberIndex.GetFilter().Should().Contain("IsDeleted");
        var shipmentIndex = note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_ShipmentId");
        shipmentIndex.IsUnique.Should().BeTrue();
        shipmentIndex.GetFilter().Should().Contain("IsDeleted");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_OrderId");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_BusinessId");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_CustomerId");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_Status");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_IssuedAtUtc");
        note.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNotes_CreatedAtUtc");

        line.GetSchema().Should().Be("Sales");
        line.GetTableName().Should().Be("DeliveryNoteLines");
        line.FindProperty(nameof(DeliveryNoteLine.ProductVariantId))!.IsNullable.Should().BeTrue();
        line.FindProperty(nameof(DeliveryNoteLine.Name))!.GetMaxLength().Should().Be(250);
        line.FindProperty(nameof(DeliveryNoteLine.Sku))!.GetMaxLength().Should().Be(100);
        line.FindProperty(nameof(DeliveryNoteLine.Description))!.GetMaxLength().Should().Be(1000);
        line.FindProperty(nameof(DeliveryNoteLine.TaxRate))!.GetPrecision().Should().Be(18);
        line.FindProperty(nameof(DeliveryNoteLine.TaxRate))!.GetScale().Should().Be(4);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_DeliveryNoteLines_Note_SortOrder")
            .Properties.Select(x => x.Name).Should().Equal(
                nameof(DeliveryNoteLine.DeliveryNoteId),
                nameof(DeliveryNoteLine.SortOrder));
    }

    [Fact]
    public void DeliveryNote_Migrations_Should_Create_Only_DeliveryNote_Tables()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*DeliveryNoteCoreModel.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("DeliveryNotes");
            migration.Should().Contain("DeliveryNoteLines");
            migration.Should().Contain("IX_DeliveryNotes_DeliveryNoteNumber");
            migration.Should().Contain("IX_DeliveryNotes_ShipmentId");
            migration.Should().NotContain("SalesOrders");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
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
