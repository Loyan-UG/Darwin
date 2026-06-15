using Darwin.Domain.Entities.Sales;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class ReturnOrderCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void ReturnOrder_Should_Map_To_Sales_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var order = GetEntity(context, typeof(ReturnOrder));
        var line = GetEntity(context, typeof(ReturnOrderLine));
        var refundLink = GetEntity(context, typeof(ReturnOrderRefundLink));

        order.GetSchema().Should().Be("Sales");
        order.GetTableName().Should().Be("ReturnOrders");
        order.FindProperty(nameof(ReturnOrder.ReturnOrderNumber))!.GetMaxLength().Should().Be(50);
        order.FindProperty(nameof(ReturnOrder.Status))!.GetMaxLength().Should().Be(32);
        order.FindProperty(nameof(ReturnOrder.Currency))!.GetMaxLength().Should().Be(3);
        order.FindProperty(nameof(ReturnOrder.CustomerSnapshotJson))!.GetMaxLength().Should().Be(16000);
        order.FindProperty(nameof(ReturnOrder.ShippingAddressJson))!.GetMaxLength().Should().Be(16000);
        order.FindProperty(nameof(ReturnOrder.InternalNotes))!.GetMaxLength().Should().Be(2000);
        order.FindProperty(nameof(ReturnOrder.MetadataJson))!.GetMaxLength().Should().Be(16000);

        if (provider == "PostgreSql")
        {
            order.FindProperty(nameof(ReturnOrder.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        var numberIndex = order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_ReturnOrderNumber");
        numberIndex.IsUnique.Should().BeTrue();
        numberIndex.GetFilter().Should().Contain("ReturnOrderNumber");
        numberIndex.GetFilter().Should().Contain("IsDeleted");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_OrderId");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_ShipmentId");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_InvoiceId");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_Status");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_ApprovedAtUtc");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_ReceivedAtUtc");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_InspectedAtUtc");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrders_RefundedAtUtc");

        line.GetSchema().Should().Be("Sales");
        line.GetTableName().Should().Be("ReturnOrderLines");
        line.FindProperty(nameof(ReturnOrderLine.ProductVariantId))!.IsNullable.Should().BeTrue();
        line.FindProperty(nameof(ReturnOrderLine.Name))!.GetMaxLength().Should().Be(250);
        line.FindProperty(nameof(ReturnOrderLine.Disposition))!.GetMaxLength().Should().Be(32);
        line.FindProperty(nameof(ReturnOrderLine.TaxRate))!.GetPrecision().Should().Be(18);
        line.FindProperty(nameof(ReturnOrderLine.TaxRate))!.GetScale().Should().Be(4);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrderLines_ReturnOrder_SortOrder")
            .Properties.Select(x => x.Name).Should().Equal(nameof(ReturnOrderLine.ReturnOrderId), nameof(ReturnOrderLine.SortOrder));

        refundLink.GetTableName().Should().Be("ReturnOrderRefundLinks");
        refundLink.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ReturnOrderRefundLinks_ReturnOrder_Refund").IsUnique.Should().BeTrue();
    }

    [Fact]
    public void ReturnOrder_Migrations_Should_Create_Only_ReturnOrder_Tables()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*ReturnOrderCoreModel.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("ReturnOrders");
            migration.Should().Contain("ReturnOrderLines");
            migration.Should().Contain("ReturnOrderRefundLinks");
            migration.Should().Contain("IX_ReturnOrders_ReturnOrderNumber");
            migration.Should().NotContain("SalesOrders");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
            migration.Should().NotContain("CreditNotes");
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
