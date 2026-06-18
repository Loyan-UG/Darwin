using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Orders;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class SalesCoreAdditiveFieldsModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void SalesCoreFields_Should_MapTo_Current_Order_And_Invoice_Tables(string provider)
    {
        using var context = CreateContext(provider);

        var order = GetEntity(context, typeof(Order));
        var invoice = GetEntity(context, typeof(Invoice));
        var invoiceLine = GetEntity(context, typeof(InvoiceLine));

        order.GetSchema().Should().Be("Orders");
        order.GetTableName().Should().Be("Orders");
        order.FindProperty(nameof(Order.BusinessId)).Should().NotBeNull();
        order.FindProperty(nameof(Order.CustomerId)).Should().NotBeNull();
        order.FindProperty(nameof(Order.SalesChannel))!.IsNullable.Should().BeFalse();
        order.FindProperty(nameof(Order.OrderedAtUtc))!.IsNullable.Should().BeFalse();
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Orders_BusinessId");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Orders_CustomerId");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Orders_OrderedAtUtc");
        order.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Orders_SalesChannel");

        invoice.GetSchema().Should().Be("CRM");
        invoice.GetTableName().Should().Be("Invoices");
        invoice.FindProperty(nameof(Invoice.InvoiceNumber))!.GetMaxLength().Should().Be(50);
        invoice.FindProperty(nameof(Invoice.InvoiceNumber))!.IsNullable.Should().BeTrue();
        var invoiceNumberIndex = invoice.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Invoices_InvoiceNumber");
        invoiceNumberIndex.IsUnique.Should().BeTrue();
        invoiceNumberIndex.GetFilter().Should().Contain("InvoiceNumber");
        invoiceNumberIndex.GetFilter().Should().Contain("IsDeleted");

        invoiceLine.GetSchema().Should().Be("CRM");
        invoiceLine.GetTableName().Should().Be("InvoiceLines");
        invoiceLine.FindProperty(nameof(InvoiceLine.TotalTaxMinor))!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void SalesCoreMigrations_Should_Backfill_OrderedAt_And_TotalTax()
    {
        var root = FindRepositoryRoot();
        var postgreSqlMigration = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Darwin.Infrastructure.PostgreSql",
            "Migrations",
            "20260611205815_SalesCoreAdditiveFields.cs"));
        var sqlServerMigration = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Darwin.Infrastructure.SqlServer",
            "Migrations",
            "20260611205839_SalesCoreAdditiveFields.cs"));

        postgreSqlMigration.Should().Contain("COALESCE(\"CreatedAtUtc\", NOW())");
        postgreSqlMigration.Should().Contain("GREATEST(0, \"TotalGrossMinor\" - \"TotalNetMinor\")");
        sqlServerMigration.Should().Contain("COALESCE([CreatedAtUtc], SYSUTCDATETIME())");
        sqlServerMigration.Should().Contain("[TotalGrossMinor] - [TotalNetMinor]");
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
