using Darwin.Domain.Entities.HumanResources;
using Darwin.Domain.Enums;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class PayrollPaymentReversalCoreTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void PayrollPaymentReversal_Should_Map_Additive_Reversal_Fields(string provider)
    {
        using var context = CreateContext(provider);
        var payment = GetEntity(context, typeof(PayrollPayment));

        payment.GetSchema().Should().Be("HumanResources");
        payment.GetTableName().Should().Be("PayrollPayments");
        payment.FindProperty(nameof(PayrollPayment.ReversalJournalEntryId))!.IsNullable.Should().BeTrue();
        payment.FindProperty(nameof(PayrollPayment.ReversedAtUtc))!.IsNullable.Should().BeTrue();
        payment.FindProperty(nameof(PayrollPayment.ReversalReason))!.GetMaxLength().Should().Be(1000);
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPayments_ReversalJournalEntryId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPayments_ReversedAtUtc");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void PayrollPaymentBankSettlement_Should_Map_Additive_Settlement_Fields(string provider)
    {
        using var context = CreateContext(provider);
        var payment = GetEntity(context, typeof(PayrollPayment));

        payment.GetSchema().Should().Be("HumanResources");
        payment.GetTableName().Should().Be("PayrollPayments");
        payment.FindProperty(nameof(PayrollPayment.BankSettledAtUtc))!.IsNullable.Should().BeTrue();
        payment.FindProperty(nameof(PayrollPayment.BankSettlementJournalEntryId))!.IsNullable.Should().BeTrue();
        payment.FindProperty(nameof(PayrollPayment.BankSettlementReconciliationMatchId))!.IsNullable.Should().BeTrue();
        payment.FindProperty(nameof(PayrollPayment.BankSettlementNotes))!.GetMaxLength().Should().Be(1000);
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPayments_BankSettledAtUtc");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPayments_BankSettlementJournalEntryId");
        payment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPayments_BankSettlementReconciliationMatchId");
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void PayrollPaymentBankCorrection_Should_Map_To_HumanResources_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var correction = GetEntity(context, typeof(PayrollPaymentBankCorrection));

        correction.GetSchema().Should().Be("HumanResources");
        correction.GetTableName().Should().Be("PayrollPaymentBankCorrections");
        correction.FindProperty(nameof(PayrollPaymentBankCorrection.CorrectionType))!.GetMaxLength().Should().Be(64);
        correction.FindProperty(nameof(PayrollPaymentBankCorrection.Status))!.GetMaxLength().Should().Be(64);
        correction.FindProperty(nameof(PayrollPaymentBankCorrection.Currency))!.GetMaxLength().Should().Be(3);
        correction.FindProperty(nameof(PayrollPaymentBankCorrection.Reason))!.GetMaxLength().Should().Be(1000);
        correction.FindProperty(nameof(PayrollPaymentBankCorrection.InternalNotes))!.GetMaxLength().Should().Be(4000);
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "UX_PayrollPaymentBankCorrections_Payment_Type_Reconciliation_Active").IsUnique.Should().BeTrue();
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPaymentBankCorrections_PayrollPaymentId");
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPaymentBankCorrections_BankReconciliationMatchId");
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPaymentBankCorrections_BankStatementLineId");
        correction.GetIndexes().Single(x => x.GetDatabaseName() == "IX_PayrollPaymentBankCorrections_CorrectionJournalEntryId");
        if (provider == "PostgreSql")
        {
            correction.FindProperty(nameof(PayrollPaymentBankCorrection.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }
    }

    [Fact]
    public void PayrollPaymentReversal_Migrations_Should_Add_Only_Reversal_Fields()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*PayrollPaymentReversalCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("PayrollPayments");
            migration.Should().Contain("ReversalJournalEntryId");
            migration.Should().Contain("ReversalReason");
            migration.Should().Contain("ReversedAtUtc");
            migration.Should().Contain("IX_PayrollPayments_ReversalJournalEntryId");
            migration.Should().Contain("IX_PayrollPayments_ReversedAtUtc");
            migration.Should().NotContain("CreateTable");
            migration.Should().NotContain("BankAccount");
            migration.Should().NotContain("BankReconciliation");
            migration.Should().NotContain("PayrollProvider");
            migration.Should().NotContain("FinanceExport");
            migration.Should().NotContain("name: \"Payments\"");
            migration.Should().NotContain("name: \"Refunds\"");
            migration.Should().NotContain("SupplierPayments");
        }
    }

    [Fact]
    public void PayrollPaymentBankSettlement_Migrations_Should_Add_Only_Settlement_Fields()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*PayrollPaymentBankSettlementCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("PayrollPayments");
            migration.Should().Contain("BankSettledAtUtc");
            migration.Should().Contain("BankSettlementJournalEntryId");
            migration.Should().Contain("BankSettlementReconciliationMatchId");
            migration.Should().Contain("BankSettlementNotes");
            migration.Should().Contain("IX_PayrollPayments_BankSettledAtUtc");
            migration.Should().Contain("IX_PayrollPayments_BankSettlementJournalEntryId");
            migration.Should().Contain("IX_PayrollPayments_BankSettlementReconciliationMatchId");
            migration.Should().NotContain("CreateTable");
            migration.Should().NotContain("BankCredentials");
            migration.Should().NotContain("BankApi");
            migration.Should().NotContain("DirectBankSettlement");
            migration.Should().NotContain("name: \"Payments\"");
            migration.Should().NotContain("name: \"Refunds\"");
            migration.Should().NotContain("SupplierPayments");
            migration.Should().NotContain("FinanceExport");
        }
    }

    [Fact]
    public void PayrollPaymentBankCorrection_Migrations_Should_Create_Only_Correction_Table()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*PayrollReturnedTransferCorrectionCore.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("PayrollPaymentBankCorrections");
            migration.Should().Contain("UX_PayrollPaymentBankCorrections_Payment_Type_Reconciliation_Active");
            migration.Should().Contain("PayrollPaymentBankCorrection");
            migration.Should().Contain("HumanResources");
            migration.Should().NotContain("BankCredentials");
            migration.Should().NotContain("BankApi");
            migration.Should().NotContain("DirectBankSettlement");
            migration.Should().NotContain("name: \"Payments\"");
            migration.Should().NotContain("name: \"Refunds\"");
            migration.Should().NotContain("SupplierPayments");
            migration.Should().NotContain("SupplierAdvance");
            migration.Should().NotContain("FinanceExport");
            migration.Should().NotContain("Mobile");
        }
    }

    [Fact]
    public void PayrollPaymentReversal_Enums_Should_Expose_Reversal_State_And_PostingKind()
    {
        Enum.GetNames<PayrollPaymentStatus>().Should().Contain(nameof(PayrollPaymentStatus.Reversed));
        Enum.GetNames<JournalEntryPostingKind>().Should().Contain(nameof(JournalEntryPostingKind.Reversal));
        Enum.GetNames<JournalEntryPostingKind>().Should().Contain(nameof(JournalEntryPostingKind.PayrollPaymentBankSettled));
        Enum.GetNames<JournalEntryPostingKind>().Should().Contain(nameof(JournalEntryPostingKind.PayrollPaymentBankCorrection));
        Enum.GetNames<PayrollPaymentBankCorrectionType>().Should().Contain(nameof(PayrollPaymentBankCorrectionType.ReturnedTransfer));
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
