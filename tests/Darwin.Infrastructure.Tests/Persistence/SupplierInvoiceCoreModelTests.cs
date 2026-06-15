using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class SupplierInvoiceCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void SupplierInvoice_Should_Map_To_Billing_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var invoice = GetEntity(context, typeof(SupplierInvoice));
        var line = GetEntity(context, typeof(SupplierInvoiceLine));

        invoice.GetSchema().Should().Be("Billing");
        invoice.GetTableName().Should().Be("SupplierInvoices");
        invoice.FindProperty(nameof(SupplierInvoice.SupplierInvoiceNumber))!.GetMaxLength().Should().Be(128);
        invoice.FindProperty(nameof(SupplierInvoice.InternalInvoiceNumber))!.GetMaxLength().Should().Be(128);
        invoice.FindProperty(nameof(SupplierInvoice.Status))!.GetMaxLength().Should().Be(64);
        invoice.FindProperty(nameof(SupplierInvoice.Currency))!.GetMaxLength().Should().Be(3);
        invoice.FindProperty(nameof(SupplierInvoice.PostedAtUtc))!.IsNullable.Should().BeTrue();
        invoice.FindProperty(nameof(SupplierInvoice.PostingJournalEntryId))!.IsNullable.Should().BeTrue();
        invoice.GetIndexes().Single(x => x.GetDatabaseName() == "UX_SupplierInvoices_Business_Supplier_Number_Active").IsUnique.Should().BeTrue();
        invoice.GetIndexes().Single(x => x.GetDatabaseName() == "UX_SupplierInvoices_Business_InternalNumber_Active").IsUnique.Should().BeTrue();
        invoice.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierInvoices_PostedAtUtc");
        invoice.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierInvoices_PostingJournalEntryId");

        if (provider == "PostgreSql")
        {
            invoice.FindProperty(nameof(SupplierInvoice.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        line.GetSchema().Should().Be("Billing");
        line.GetTableName().Should().Be("SupplierInvoiceLines");
        line.FindProperty(nameof(SupplierInvoiceLine.MatchStatus))!.GetMaxLength().Should().Be(64);
        line.FindProperty(nameof(SupplierInvoiceLine.TaxRate))!.GetPrecision().Should().Be(9);
        line.FindProperty(nameof(SupplierInvoiceLine.TaxRate))!.GetScale().Should().Be(4);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierInvoiceLines_SupplierInvoiceId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierInvoiceLines_PurchaseOrderLineId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierInvoiceLines_GoodsReceiptLineId");
    }

    [Fact]
    public void SupplierInvoice_Migrations_Should_Create_Only_CoreSupplierInvoiceObjects()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*SupplierInvoiceCoreModel.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("SupplierInvoices");
            migration.Should().Contain("SupplierInvoiceLines");
            migration.Should().Contain("UX_SupplierInvoices_Business_Supplier_Number_Active");
            migration.Should().NotContain("Payables");
            migration.Should().NotContain("SupplierPayments");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
        }

        File.ReadAllText(migrationFiles.Single(x => x.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))).Should().Contain("type: \"jsonb\"");
    }

    [Fact]
    public void NumberSequenceDocumentType_Should_Include_SupplierInvoice()
        => Enum.GetNames<NumberSequenceDocumentType>().Should().Contain(nameof(NumberSequenceDocumentType.SupplierInvoice));

    [Fact]
    public void SupplierInvoicePosting_Migrations_Should_AddOnlyPostingColumnsAndIndexes()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*SupplierInvoiceMatchingAndPosting.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("PostedAtUtc");
            migration.Should().Contain("PostingJournalEntryId");
            migration.Should().Contain("IX_SupplierInvoices_PostedAtUtc");
            migration.Should().Contain("IX_SupplierInvoices_PostingJournalEntryId");
            migration.Should().NotContain("CreateTable");
            migration.Should().NotContain("Payables");
            migration.Should().NotContain("SupplierPayments");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
        }
    }

    [Fact]
    public void SupplierInvoicePosting_Enums_Should_IncludePostingRolesAndKind()
    {
        Enum.GetNames<SupplierInvoiceStatus>().Should().Contain(nameof(SupplierInvoiceStatus.Posted));
        Enum.GetNames<JournalEntryPostingKind>().Should().Contain(nameof(JournalEntryPostingKind.SupplierInvoicePosted));
        var roles = Enum.GetNames<FinancePostingAccountRole>();
        roles.Should().Contain(nameof(FinancePostingAccountRole.AccountsPayable));
        roles.Should().Contain(nameof(FinancePostingAccountRole.PurchaseExpense));
        roles.Should().Contain(nameof(FinancePostingAccountRole.InventoryClearing));
        roles.Should().Contain(nameof(FinancePostingAccountRole.TaxReceivable));
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void SupplierPayment_Should_Map_To_Billing_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var payment = GetEntity(context, typeof(SupplierPayment));
        var allocation = GetEntity(context, typeof(SupplierPaymentAllocation));

        payment.GetSchema().Should().Be("Billing");
        payment.GetTableName().Should().Be("SupplierPayments");
        payment.FindProperty(nameof(SupplierPayment.PaymentNumber))!.GetMaxLength().Should().Be(128);
        payment.FindProperty(nameof(SupplierPayment.Status))!.GetMaxLength().Should().Be(64);
        payment.FindProperty(nameof(SupplierPayment.PaymentMethod))!.GetMaxLength().Should().Be(64);
        payment.FindProperty(nameof(SupplierPayment.Currency))!.GetMaxLength().Should().Be(3);
        payment.FindProperty(nameof(SupplierPayment.Reference))!.GetMaxLength().Should().Be(256);
        payment.FindProperty(nameof(SupplierPayment.ReversalReason))!.GetMaxLength().Should().Be(1000);
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "UX_SupplierPayments_Business_Number_Active").IsUnique.Should().BeTrue();
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_PostingJournalEntryId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_ReversalJournalEntryId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_ReversedAtUtc");

        if (provider == "PostgreSql")
        {
            payment.FindProperty(nameof(SupplierPayment.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        allocation.GetSchema().Should().Be("Billing");
        allocation.GetTableName().Should().Be("SupplierPaymentAllocations");
        allocation.FindProperty(nameof(SupplierPaymentAllocation.Memo))!.GetMaxLength().Should().Be(1000);
        allocation.GetIndexes().Single(x => x.GetDatabaseName() == "UX_SupplierPaymentAllocations_Payment_Invoice_Active").IsUnique.Should().BeTrue();
        allocation.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPaymentAllocations_SupplierInvoiceId");
    }

    [Fact]
    public void SupplierPayment_Migrations_Should_Create_OnlySupplierPaymentObjects()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*SupplierPaymentCoreModel.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("SupplierPayments");
            migration.Should().Contain("SupplierPaymentAllocations");
            migration.Should().Contain("UX_SupplierPayments_Business_Number_Active");
            migration.Should().Contain("UX_SupplierPaymentAllocations_Payment_Invoice_Active");
            migration.Should().NotContain("CreateTable(\r\n                name: \"Payments\"");
            migration.Should().NotContain("Refunds");
            migration.Should().NotContain("Payables");
            migration.Should().NotContain("SalesInvoices");
            migration.Should().NotContain("FinanceInvoices");
        }

        File.ReadAllText(migrationFiles.Single(x => x.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))).Should().Contain("type: \"jsonb\"");
    }

    [Fact]
    public void SupplierPayment_Enums_Should_IncludePaymentKindAndSequence()
    {
        Enum.GetNames<SupplierPaymentStatus>().Should().Contain(nameof(SupplierPaymentStatus.Posted));
        Enum.GetNames<SupplierPaymentStatus>().Should().Contain(nameof(SupplierPaymentStatus.Reversed));
        Enum.GetNames<SupplierPaymentMethod>().Should().Contain(nameof(SupplierPaymentMethod.BankTransfer));
        Enum.GetNames<JournalEntryPostingKind>().Should().Contain(nameof(JournalEntryPostingKind.SupplierPaymentPosted));
        Enum.GetNames<JournalEntryPostingKind>().Should().Contain(nameof(JournalEntryPostingKind.Reversal));
        Enum.GetNames<NumberSequenceDocumentType>().Should().Contain(nameof(NumberSequenceDocumentType.SupplierPayment));
    }

    [Fact]
    public void SupplierPaymentReversal_Migrations_Should_AddOnlyReversalFields()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*SupplierPaymentReversalCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("ReversalJournalEntryId");
            migration.Should().Contain("ReversalReason");
            migration.Should().Contain("ReversedAtUtc");
            migration.Should().Contain("IX_SupplierPayments_ReversalJournalEntryId");
            migration.Should().Contain("IX_SupplierPayments_ReversedAtUtc");
            migration.Should().NotContain("CreateTable");
            migration.Should().NotContain("BankAccount");
            migration.Should().NotContain("TreasuryLedger");
            migration.Should().NotContain("SupplierAdvance");
            migration.Should().NotContain("SupplierCredit");
            migration.Should().NotContain("SupplierPaymentRefund");
            migration.Should().NotContain("table: \"Payments\"");
            migration.Should().NotContain("Refunds");
            migration.Should().NotContain("FinanceExport");
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
