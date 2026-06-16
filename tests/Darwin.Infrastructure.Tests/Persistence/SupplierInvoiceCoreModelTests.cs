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
        payment.FindProperty(nameof(SupplierPayment.BankSettlementNotes))!.GetMaxLength().Should().Be(1000);
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "UX_SupplierPayments_Business_Number_Active").IsUnique.Should().BeTrue();
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_PostingJournalEntryId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_ReversalJournalEntryId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_ReversedAtUtc");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_BankSettlementJournalEntryId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_BankSettlementReconciliationMatchId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPayments_BankSettledAtUtc");

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
        Enum.GetNames<JournalEntryPostingKind>().Should().Contain(nameof(JournalEntryPostingKind.SupplierPaymentBankSettled));
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

    [Fact]
    public void SupplierPaymentBankSettlement_Migrations_Should_AddOnlySettlementFields()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*SupplierPaymentBankSettlementCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("BankSettledAtUtc");
            migration.Should().Contain("BankSettlementJournalEntryId");
            migration.Should().Contain("BankSettlementReconciliationMatchId");
            migration.Should().Contain("BankSettlementNotes");
            migration.Should().Contain("IX_SupplierPayments_BankSettlementJournalEntryId");
            migration.Should().Contain("IX_SupplierPayments_BankSettlementReconciliationMatchId");
            migration.Should().Contain("IX_SupplierPayments_BankSettledAtUtc");
            migration.Should().NotContain("CreateTable");
            migration.Should().NotContain("BankSettlements");
            migration.Should().NotContain("TreasuryLedger");
            migration.Should().NotContain("SupplierAdvance");
            migration.Should().NotContain("SupplierCredit");
            migration.Should().NotContain("BankCredential");
            migration.Should().NotContain("BankApi");
            migration.Should().NotContain("table: \"Payments\"");
            migration.Should().NotContain("Refunds");
            migration.Should().NotContain("FinanceExport");
        }
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void SupplierPaymentBankCorrection_Should_Map_To_Billing_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var correction = GetEntity(context, typeof(SupplierPaymentBankCorrection));

        correction.GetSchema().Should().Be("Billing");
        correction.GetTableName().Should().Be("SupplierPaymentBankCorrections");
        correction.FindProperty(nameof(SupplierPaymentBankCorrection.CorrectionType))!.GetMaxLength().Should().Be(64);
        correction.FindProperty(nameof(SupplierPaymentBankCorrection.Status))!.GetMaxLength().Should().Be(64);
        correction.FindProperty(nameof(SupplierPaymentBankCorrection.Currency))!.GetMaxLength().Should().Be(3);
        correction.FindProperty(nameof(SupplierPaymentBankCorrection.Reason))!.GetMaxLength().Should().Be(1000);
        correction.FindProperty(nameof(SupplierPaymentBankCorrection.InternalNotes))!.GetMaxLength().Should().Be(4000);
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "UX_SupplierPaymentBankCorrections_Payment_Type_Reconciliation_Active").IsUnique.Should().BeTrue();
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPaymentBankCorrections_SupplierPaymentId");
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPaymentBankCorrections_BankReconciliationMatchId");
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPaymentBankCorrections_BankStatementLineId");
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierPaymentBankCorrections_CorrectionJournalEntryId");

        if (provider == "PostgreSql")
        {
            correction.FindProperty(nameof(SupplierPaymentBankCorrection.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }
    }

    [Fact]
    public void ReturnedTransferDuplicatePaymentCorrection_Migrations_Should_AddOnlyCorrectionObjects()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*ReturnedTransferDuplicatePaymentCorrectionCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("SupplierPaymentBankCorrections");
            migration.Should().Contain("UX_SupplierPaymentBankCorrections_Payment_Type_Reconciliation_Active");
            migration.Should().Contain("SupplierPaymentBankCorrection");
            migration.Should().NotContain("BankSettlement\"");
            migration.Should().NotContain("TreasuryLedger");
            migration.Should().NotContain("SupplierAdvance");
            migration.Should().NotContain("SupplierCredit");
            migration.Should().NotContain("BankCredential");
            migration.Should().NotContain("BankApi");
            migration.Should().NotContain("table: \"Payments\"");
            migration.Should().NotContain("Refunds");
            migration.Should().NotContain("FinanceExport");
        }

        File.ReadAllText(migrationFiles.Single(x => x.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))).Should().Contain("type: \"jsonb\"");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void BankTreasury_Should_Map_To_Billing_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var account = GetEntity(context, typeof(BankAccount));
        var import = GetEntity(context, typeof(BankStatementImport));
        var line = GetEntity(context, typeof(BankStatementLine));

        account.GetSchema().Should().Be("Billing");
        account.GetTableName().Should().Be("BankAccounts");
        account.FindProperty(nameof(BankAccount.Code))!.GetMaxLength().Should().Be(64);
        account.FindProperty(nameof(BankAccount.DisplayName))!.GetMaxLength().Should().Be(200);
        account.FindProperty(nameof(BankAccount.BankName))!.GetMaxLength().Should().Be(200);
        account.FindProperty(nameof(BankAccount.Currency))!.GetMaxLength().Should().Be(3);
        account.FindProperty(nameof(BankAccount.MaskedAccountIdentifier))!.GetMaxLength().Should().Be(128);
        account.FindProperty(nameof(BankAccount.Status))!.GetMaxLength().Should().Be(64);
        account.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BankAccounts_Business_Code_Active").IsUnique.Should().BeTrue();
        account.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BankAccounts_FinancialAccountId");

        import.GetSchema().Should().Be("Billing");
        import.GetTableName().Should().Be("BankStatementImports");
        import.FindProperty(nameof(BankStatementImport.StatementReference))!.GetMaxLength().Should().Be(128);
        import.FindProperty(nameof(BankStatementImport.Status))!.GetMaxLength().Should().Be(64);
        import.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BankStatementImports_Account_Reference_Active").IsUnique.Should().BeTrue();
        import.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BankStatementImports_BusinessId_PeriodStartUtc_PeriodEndUtc");

        line.GetSchema().Should().Be("Billing");
        line.GetTableName().Should().Be("BankStatementLines");
        line.FindProperty(nameof(BankStatementLine.Direction))!.GetMaxLength().Should().Be(64);
        line.FindProperty(nameof(BankStatementLine.Currency))!.GetMaxLength().Should().Be(3);
        line.FindProperty(nameof(BankStatementLine.CounterpartyName))!.GetMaxLength().Should().Be(256);
        line.FindProperty(nameof(BankStatementLine.CounterpartyReference))!.GetMaxLength().Should().Be(256);
        line.FindProperty(nameof(BankStatementLine.RemittanceInformation))!.GetMaxLength().Should().Be(1000);
        line.FindProperty(nameof(BankStatementLine.NormalizedIdentityKey))!.GetMaxLength().Should().Be(256);
        line.FindProperty(nameof(BankStatementLine.ReviewStatus))!.GetMaxLength().Should().Be(64);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BankStatementLines_Account_Identity_Active").IsUnique.Should().BeTrue();

        if (provider == "PostgreSql")
        {
            account.FindProperty(nameof(BankAccount.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            import.FindProperty(nameof(BankStatementImport.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            line.FindProperty(nameof(BankStatementLine.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }
    }

    [Fact]
    public void BankTreasury_Migrations_Should_Create_OnlyFoundationBankEvidenceObjects()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*BankTreasuryFoundationCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("BankAccounts");
            migration.Should().Contain("BankStatementImports");
            migration.Should().Contain("BankStatementLines");
            migration.Should().Contain("UX_BankAccounts_Business_Code_Active");
            migration.Should().Contain("UX_BankStatementImports_Account_Reference_Active");
            migration.Should().Contain("UX_BankStatementLines_Account_Identity_Active");
            migration.Should().NotContain("BankReconciliation");
            migration.Should().NotContain("BankCredential");
            migration.Should().NotContain("DirectBankSettlement");
            migration.Should().NotContain("TreasuryLedger");
            migration.Should().NotContain("SupplierAdvance");
            migration.Should().NotContain("SupplierCredit");
            migration.Should().NotContain("CreateTable(\r\n                name: \"Payments\"");
            migration.Should().NotContain("Refunds");
            migration.Should().NotContain("FinanceExport");
        }

        File.ReadAllText(migrationFiles.Single(x => x.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))).Should().Contain("type: \"jsonb\"");
    }

    [Fact]
    public void BankTreasury_Enums_Should_IncludeFoundationStatuses()
    {
        Enum.GetNames<BankAccountStatus>().Should().Contain([nameof(BankAccountStatus.Active), nameof(BankAccountStatus.Archived)]);
        Enum.GetNames<BankStatementImportStatus>().Should().Contain([nameof(BankStatementImportStatus.Imported), nameof(BankStatementImportStatus.Cancelled)]);
        Enum.GetNames<BankStatementLineDirection>().Should().Contain([nameof(BankStatementLineDirection.Debit), nameof(BankStatementLineDirection.Credit)]);
        Enum.GetNames<BankStatementLineReviewStatus>().Should().Contain(nameof(BankStatementLineReviewStatus.Unreviewed));
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void BankReconciliation_Should_Map_To_Billing_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var match = GetEntity(context, typeof(BankReconciliationMatch));
        var line = GetEntity(context, typeof(BankReconciliationMatchLine));

        match.GetSchema().Should().Be("Billing");
        match.GetTableName().Should().Be("BankReconciliationMatches");
        match.FindProperty(nameof(BankReconciliationMatch.MatchNumber))!.GetMaxLength().Should().Be(128);
        match.FindProperty(nameof(BankReconciliationMatch.Status))!.GetMaxLength().Should().Be(64);
        match.FindProperty(nameof(BankReconciliationMatch.Currency))!.GetMaxLength().Should().Be(3);
        match.FindProperty(nameof(BankReconciliationMatch.ReviewNotes))!.GetMaxLength().Should().Be(4000);
        match.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BankReconciliationMatches_Business_Number_Active").IsUnique.Should().BeTrue();
        match.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BankReconciliationMatches_BankAccountId");

        line.GetSchema().Should().Be("Billing");
        line.GetTableName().Should().Be("BankReconciliationMatchLines");
        line.FindProperty(nameof(BankReconciliationMatchLine.SourceType))!.GetMaxLength().Should().Be(64);
        line.FindProperty(nameof(BankReconciliationMatchLine.SourceEntityType))!.GetMaxLength().Should().Be(128);
        line.FindProperty(nameof(BankReconciliationMatchLine.Direction))!.GetMaxLength().Should().Be(64);
        line.FindProperty(nameof(BankReconciliationMatchLine.Memo))!.GetMaxLength().Should().Be(1000);
        line.GetIndexes().Single(x => x.GetDatabaseName() == "UX_BankReconciliationMatchLines_StatementLine_Active").IsUnique.Should().BeTrue();
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_BankReconciliationMatchLines_SourceEntity");

        if (provider == "PostgreSql")
        {
            match.FindProperty(nameof(BankReconciliationMatch.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }
    }

    [Fact]
    public void BankReconciliation_Migrations_Should_Create_OnlyReconciliationObjects()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*BankReconciliationCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("BankReconciliationMatches");
            migration.Should().Contain("BankReconciliationMatchLines");
            migration.Should().Contain("UX_BankReconciliationMatches_Business_Number_Active");
            migration.Should().Contain("UX_BankReconciliationMatchLines_StatementLine_Active");
            migration.Should().Contain("IX_BankReconciliationMatchLines_SourceEntity");
            migration.Should().NotContain("BankSettlement");
            migration.Should().NotContain("BankCredential");
            migration.Should().NotContain("DirectBankSettlement");
            migration.Should().NotContain("ReturnedTransfer");
            migration.Should().NotContain("TreasuryLedger");
            migration.Should().NotContain("SupplierAdvance");
            migration.Should().NotContain("SupplierCredit");
            migration.Should().NotContain("CreateTable(\r\n                name: \"Payments\"");
            migration.Should().NotContain("CreateTable(\r\n                name: \"Refunds\"");
            migration.Should().NotContain("FinanceExport");
        }

        File.ReadAllText(migrationFiles.Single(x => x.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))).Should().Contain("type: \"jsonb\"");
    }

    [Fact]
    public void BankReconciliation_Enums_Should_IncludeEvidenceStatuses()
    {
        Enum.GetNames<BankReconciliationMatchStatus>().Should().Contain([nameof(BankReconciliationMatchStatus.Draft), nameof(BankReconciliationMatchStatus.Matched), nameof(BankReconciliationMatchStatus.Cancelled)]);
        Enum.GetNames<BankReconciliationSourceType>().Should().Contain([nameof(BankReconciliationSourceType.JournalEntry), nameof(BankReconciliationSourceType.SupplierPayment), nameof(BankReconciliationSourceType.CustomerPayment), nameof(BankReconciliationSourceType.Refund)]);
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
