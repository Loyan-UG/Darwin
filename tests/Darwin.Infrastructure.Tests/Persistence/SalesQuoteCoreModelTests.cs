using Darwin.Domain.Entities.Sales;
using Darwin.Domain.Entities.Orders;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class SalesQuoteCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void SalesQuote_Should_Map_To_Sales_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);

        var quote = GetEntity(context, typeof(SalesQuote));
        var line = GetEntity(context, typeof(SalesQuoteLine));

        quote.GetSchema().Should().Be("Sales");
        quote.GetTableName().Should().Be("SalesQuotes");
        quote.FindProperty(nameof(SalesQuote.QuoteNumber))!.GetMaxLength().Should().Be(50);
        quote.FindProperty(nameof(SalesQuote.Title))!.GetMaxLength().Should().Be(250);
        quote.FindProperty(nameof(SalesQuote.Status))!.GetMaxLength().Should().Be(32);
        quote.FindProperty(nameof(SalesQuote.Currency))!.GetMaxLength().Should().Be(3);
        quote.FindProperty(nameof(SalesQuote.InternalNotes))!.GetMaxLength().Should().Be(2000);
        quote.FindProperty(nameof(SalesQuote.CustomerSnapshotJson))!.GetMaxLength().Should().Be(16000);
        quote.FindProperty(nameof(SalesQuote.BillingAddressJson))!.GetMaxLength().Should().Be(16000);
        quote.FindProperty(nameof(SalesQuote.ShippingAddressJson))!.GetMaxLength().Should().Be(16000);
        quote.FindProperty(nameof(SalesQuote.Status))!.GetColumnType().Should().NotBeNullOrWhiteSpace();
        quote.FindProperty(nameof(SalesQuote.Status))!.GetColumnType()!.Should().ContainAny("nvarchar", "character varying");

        var quoteNumberIndex = quote.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SalesQuotes_QuoteNumber");
        quoteNumberIndex.IsUnique.Should().BeTrue();
        quoteNumberIndex.GetFilter().Should().Contain("QuoteNumber");
        quoteNumberIndex.GetFilter().Should().Contain("IsDeleted");
        quote.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SalesQuotes_Status");
        quote.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SalesQuotes_ValidUntilUtc");
        quote.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SalesQuotes_CreatedAtUtc");
        quote.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SalesQuotes_ConvertedOrderId");

        line.GetSchema().Should().Be("Sales");
        line.GetTableName().Should().Be("SalesQuoteLines");
        line.FindProperty(nameof(SalesQuoteLine.Name))!.GetMaxLength().Should().Be(250);
        line.FindProperty(nameof(SalesQuoteLine.Sku))!.GetMaxLength().Should().Be(100);
        line.FindProperty(nameof(SalesQuoteLine.Description))!.GetMaxLength().Should().Be(1000);
        line.FindProperty(nameof(SalesQuoteLine.TaxRate))!.GetPrecision().Should().Be(18);
        line.FindProperty(nameof(SalesQuoteLine.TaxRate))!.GetScale().Should().Be(4);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SalesQuoteLines_Quote_SortOrder")
            .Properties.Select(x => x.Name).Should().Equal(
                nameof(SalesQuoteLine.SalesQuoteId),
                nameof(SalesQuoteLine.SortOrder));
    }

    [Fact]
    public void SalesQuote_Migrations_Should_Create_Only_Quote_Tables()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*SalesQuoteCoreModel.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("SalesQuotes");
            migration.Should().Contain("SalesQuoteLines");
            migration.Should().Contain("IX_SalesQuotes_QuoteNumber");
            migration.Should().NotContain("SalesOrders");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
        }
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void OrderLine_VariantId_Should_Be_Nullable_For_Quote_Converted_NonCatalog_Lines(string provider)
    {
        using var context = CreateContext(provider);

        var orderLine = GetEntity(context, typeof(OrderLine));

        orderLine.FindProperty(nameof(OrderLine.VariantId))!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void SalesQuoteOrderConversion_Migrations_Should_Only_Make_OrderLine_VariantId_Nullable()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*SalesQuoteOrderConversion.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("AlterColumn<Guid>");
            migration.Should().Contain("VariantId");
            migration.Should().Contain("OrderLines");
            migration.Should().Contain("nullable: true");
            migration.Should().NotContain("CreateTable");
            migration.Should().NotContain("SalesOrders");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
        }
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
