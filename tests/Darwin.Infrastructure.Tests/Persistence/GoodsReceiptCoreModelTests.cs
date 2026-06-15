using Darwin.Domain.Entities.Inventory;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class GoodsReceiptCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void GoodsReceipt_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var receipt = GetEntity(context, typeof(GoodsReceipt));
        var line = GetEntity(context, typeof(GoodsReceiptLine));

        receipt.GetSchema().Should().Be("Inventory");
        receipt.GetTableName().Should().Be("GoodsReceipts");
        receipt.FindProperty(nameof(GoodsReceipt.GoodsReceiptNumber))!.GetMaxLength().Should().Be(100);
        receipt.FindProperty(nameof(GoodsReceipt.InternalNotes))!.GetMaxLength().Should().Be(4000);
        receipt.FindProperty(nameof(GoodsReceipt.MetadataJson))!.GetMaxLength().Should().Be(8000);
        receipt.FindProperty(nameof(GoodsReceipt.Status))!.IsNullable.Should().BeFalse();

        if (provider == "PostgreSql")
        {
            receipt.FindProperty(nameof(GoodsReceipt.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        var numberIndex = receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_BusinessId_GoodsReceiptNumber");
        numberIndex.IsUnique.Should().BeTrue();
        numberIndex.GetFilter().Should().Contain("GoodsReceiptNumber");
        numberIndex.GetFilter().Should().Contain("IsDeleted");
        receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_BusinessId");
        receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_SupplierId");
        receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_PurchaseOrderId");
        receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_WarehouseId");
        receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_Status");
        receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_ReceivedAtUtc");
        receipt.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceipts_PostedAtUtc");

        line.GetSchema().Should().Be("Inventory");
        line.GetTableName().Should().Be("GoodsReceiptLines");
        line.FindProperty(nameof(GoodsReceiptLine.ProductVariantId))!.IsNullable.Should().BeFalse();
        line.FindProperty(nameof(GoodsReceiptLine.SupplierSku))!.GetMaxLength().Should().Be(100);
        line.FindProperty(nameof(GoodsReceiptLine.Description))!.GetMaxLength().Should().Be(1000);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLines_GoodsReceiptId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLines_PurchaseOrderLineId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLines_ProductVariantId");
    }

    [Fact]
    public void GoodsReceipt_Migrations_Should_Create_Only_GoodsReceipt_Tables()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*GoodsReceiptCoreModel.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("GoodsReceipts");
            migration.Should().Contain("GoodsReceiptLines");
            migration.Should().Contain("IX_GoodsReceipts_BusinessId_GoodsReceiptNumber");
            migration.Should().NotContain("SupplierInvoices");
            migration.Should().NotContain("Payables");
            migration.Should().NotContain("InventoryLedgers");
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
