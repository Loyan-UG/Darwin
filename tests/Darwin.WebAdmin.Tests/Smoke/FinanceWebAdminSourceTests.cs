using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class FinanceWebAdminSourceTests
{
    [Fact]
    public void Finance_Workspace_Should_Be_Navigable_From_Sidebar()
    {
        var layout = ReadWebAdminFile("Views", "Shared", "_Layout.cshtml");

        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"Index\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"Receivables\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"Postings\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"AccountMappings\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"Exports\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"SupplierInvoices\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"SupplierPayments\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"SupplierAdvances\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"BankAccounts\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"BankStatements\"");
        layout.Should().Contain("asp-controller=\"Finance\" asp-action=\"BankReconciliation\"");
    }

    [Fact]
    public void Finance_Controller_Should_Expose_Only_Owned_Internal_Mutations()
    {
        var controller = ReadWebAdminFile("Controllers", "Admin", "Finance", "FinanceController.cs");

        controller.Should().Contain("public sealed class FinanceController");
        controller.Should().Contain("GetFinanceOverviewHandler");
        controller.Should().Contain("GetFinanceReceivablesPageHandler");
        controller.Should().Contain("GetFinancePostingsPageHandler");
        controller.Should().Contain("GetFinanceAccountMappingsPageHandler");
        controller.Should().Contain("UpsertFinanceAccountMappingHandler");
        controller.Should().Contain("GetFinanceExportsPageHandler");
        controller.Should().Contain("CreateFinanceExportBatchHandler");
        controller.Should().Contain("GenerateFinanceExportPackageHandler");
        controller.Should().Contain("DownloadFinanceExportPackageHandler");
        controller.Should().Contain("PushFinanceExportPackageHandler");
        controller.Should().Contain("GetSupplierInvoicesPageHandler");
        controller.Should().Contain("GetSupplierInvoiceDetailHandler");
        controller.Should().Contain("CreateSupplierInvoiceHandler");
        controller.Should().Contain("UpdateSupplierInvoiceHandler");
        controller.Should().Contain("UpdateSupplierInvoiceLifecycleHandler");
        controller.Should().Contain("PostSupplierInvoiceHandler");
        controller.Should().Contain("GetSupplierPaymentsPageHandler");
        controller.Should().Contain("GetSupplierPaymentDetailHandler");
        controller.Should().Contain("CreateSupplierPaymentHandler");
        controller.Should().Contain("UpdateSupplierPaymentHandler");
        controller.Should().Contain("PostSupplierPaymentHandler");
        controller.Should().Contain("CancelSupplierPaymentHandler");
        controller.Should().Contain("ReverseSupplierPaymentHandler");
        controller.Should().Contain("SettleSupplierPaymentFromBankReconciliationHandler");
        controller.Should().Contain("CreateSupplierPaymentBankCorrectionHandler");
        controller.Should().Contain("PostSupplierPaymentBankCorrectionHandler");
        controller.Should().Contain("CancelSupplierPaymentBankCorrectionHandler");
        controller.Should().Contain("GetSupplierAdvancesPageHandler");
        controller.Should().Contain("GetSupplierAdvanceDetailHandler");
        controller.Should().Contain("CreateSupplierAdvanceHandler");
        controller.Should().Contain("UpdateSupplierAdvanceHandler");
        controller.Should().Contain("PostSupplierAdvanceHandler");
        controller.Should().Contain("CancelSupplierAdvanceHandler");
        controller.Should().Contain("ReverseSupplierAdvanceHandler");
        controller.Should().Contain("ApplySupplierAdvanceHandler");
        controller.Should().Contain("ReverseSupplierAdvanceApplicationHandler");
        controller.Should().Contain("GetBankAccountsPageHandler");
        controller.Should().Contain("CreateBankAccountHandler");
        controller.Should().Contain("UpdateBankAccountHandler");
        controller.Should().Contain("ArchiveBankAccountHandler");
        controller.Should().Contain("GetBankStatementsPageHandler");
        controller.Should().Contain("CreateBankStatementImportHandler");
        controller.Should().Contain("CancelBankStatementImportHandler");
        controller.Should().Contain("GetBankReconciliationPageHandler");
        controller.Should().Contain("CreateBankReconciliationMatchHandler");
        controller.Should().Contain("UpdateBankReconciliationMatchHandler");
        controller.Should().Contain("MarkBankReconciliationMatchedHandler");
        controller.Should().Contain("CancelBankReconciliationMatchHandler");
        controller.Should().Contain("[HttpGet]");
        controller.Should().Contain("[HttpPost]");
        controller.Should().Contain("UpsertAccountMapping");
        controller.Should().Contain("CreateExportBatch");
        controller.Should().Contain("GenerateExportPackage");
        controller.Should().Contain("DownloadExportPackage");
        controller.Should().Contain("PushExportPackage");
        controller.Should().Contain("SupplierInvoices");
        controller.Should().Contain("CreateSupplierInvoice");
        controller.Should().Contain("EditSupplierInvoice");
        controller.Should().Contain("UpdateSupplierInvoiceLifecycle");
        controller.Should().Contain("PostSupplierInvoice");
        controller.Should().Contain("SupplierPayments");
        controller.Should().Contain("CreateSupplierPayment");
        controller.Should().Contain("EditSupplierPayment");
        controller.Should().Contain("PostSupplierPayment");
        controller.Should().Contain("CancelSupplierPayment");
        controller.Should().Contain("ReverseSupplierPayment");
        controller.Should().Contain("SettleSupplierPaymentFromBankReconciliation");
        controller.Should().Contain("CreateSupplierPaymentBankCorrection");
        controller.Should().Contain("PostSupplierPaymentBankCorrection");
        controller.Should().Contain("CancelSupplierPaymentBankCorrection");
        controller.Should().Contain("SupplierAdvances");
        controller.Should().Contain("CreateSupplierAdvance");
        controller.Should().Contain("EditSupplierAdvance");
        controller.Should().Contain("PostSupplierAdvance");
        controller.Should().Contain("CancelSupplierAdvance");
        controller.Should().Contain("ReverseSupplierAdvance");
        controller.Should().Contain("ApplySupplierAdvance");
        controller.Should().Contain("ReverseSupplierAdvanceApplication");
        controller.Should().Contain("BankAccounts");
        controller.Should().Contain("CreateBankAccount");
        controller.Should().Contain("EditBankAccount");
        controller.Should().Contain("ArchiveBankAccount");
        controller.Should().Contain("BankStatements");
        controller.Should().Contain("CreateBankStatement");
        controller.Should().Contain("CancelBankStatement");
        controller.Should().Contain("BankReconciliation");
        controller.Should().Contain("CreateBankReconciliation");
        controller.Should().Contain("EditBankReconciliation");
        controller.Should().Contain("MarkBankReconciliationMatched");
        controller.Should().Contain("CancelBankReconciliation");
        controller.Should().NotContain("CreateJournalEntry");
        controller.Should().NotContain("UpdateJournalEntry");
        controller.Should().NotContain("CreateCreditNote");
        controller.Should().NotContain("IssueCreditNote");
        controller.Should().NotContain("CreateCustomerInvoice");
        controller.Should().NotContain("DownloadInvoiceArchive");
        controller.Should().NotContain("CreateRefund");
        controller.Should().NotContain("CreatePayment(");
        controller.Should().NotContain("BankCredential");
        controller.Should().NotContain("DirectBankSettlement");
        controller.Should().NotContain("ReturnedTransferAutomation");
        controller.Should().NotContain("TreasuryLedger");
        controller.Should().NotContain("SupplierOverpayment");
    }

    [Fact]
    public void Finance_Views_Should_Link_To_Authoritative_Billing_Workspaces_Without_Mutations()
    {
        var allFinanceViews = string.Join(
            Environment.NewLine,
            Directory.GetFiles(Path.Combine(FindWebAdminRoot(), "Views", "Finance"), "*.cshtml")
                .OrderBy(x => x)
                .Select(File.ReadAllText));

        allFinanceViews.Should().Contain("asp-controller=\"Billing\" asp-action=\"FinancialAccounts\"");
        allFinanceViews.Should().Contain("asp-controller=\"Billing\" asp-action=\"JournalEntries\"");
        allFinanceViews.Should().Contain("asp-action=\"Receivables\"");
        allFinanceViews.Should().Contain("asp-action=\"Postings\"");
        allFinanceViews.Should().Contain("asp-action=\"AccountMappings\"");
        allFinanceViews.Should().Contain("asp-action=\"Exports\"");
        allFinanceViews.Should().Contain("asp-action=\"UpsertAccountMapping\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateExportBatch\"");
        allFinanceViews.Should().Contain("asp-action=\"GenerateExportPackage\"");
        allFinanceViews.Should().Contain("asp-action=\"PushExportPackage\"");
        allFinanceViews.Should().Contain("asp-action=\"SupplierInvoices\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateSupplierInvoice\"");
        allFinanceViews.Should().Contain("asp-action=\"EditSupplierInvoice\"");
        allFinanceViews.Should().Contain("asp-action=\"UpdateSupplierInvoiceLifecycle\"");
        allFinanceViews.Should().Contain("asp-action=\"PostSupplierInvoice\"");
        allFinanceViews.Should().Contain("PostPayable");
        allFinanceViews.Should().Contain("asp-action=\"SupplierPayments\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateSupplierPayment\"");
        allFinanceViews.Should().Contain("asp-action=\"EditSupplierPayment\"");
        allFinanceViews.Should().Contain("asp-action=\"PostSupplierPayment\"");
        allFinanceViews.Should().Contain("asp-action=\"CancelSupplierPayment\"");
        allFinanceViews.Should().Contain("asp-action=\"ReverseSupplierPayment\"");
        allFinanceViews.Should().Contain("asp-action=\"SettleSupplierPaymentFromBankReconciliation\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateSupplierPaymentBankCorrection\"");
        allFinanceViews.Should().Contain("asp-action=\"PostSupplierPaymentBankCorrection\"");
        allFinanceViews.Should().Contain("asp-action=\"CancelSupplierPaymentBankCorrection\"");
        allFinanceViews.Should().Contain("asp-action=\"SupplierAdvances\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateSupplierAdvance\"");
        allFinanceViews.Should().Contain("asp-action=\"EditSupplierAdvance\"");
        allFinanceViews.Should().Contain("asp-action=\"PostSupplierAdvance\"");
        allFinanceViews.Should().Contain("asp-action=\"CancelSupplierAdvance\"");
        allFinanceViews.Should().Contain("asp-action=\"ReverseSupplierAdvance\"");
        allFinanceViews.Should().Contain("asp-action=\"ApplySupplierAdvance\"");
        allFinanceViews.Should().Contain("asp-action=\"ReverseSupplierAdvanceApplication\"");
        allFinanceViews.Should().Contain("PostSupplierPayment");
        allFinanceViews.Should().Contain("ReversePayment");
        allFinanceViews.Should().Contain("SettleFromBankReconciliation");
        allFinanceViews.Should().Contain("CreateBankCorrection");
        allFinanceViews.Should().Contain("PostCorrection");
        allFinanceViews.Should().Contain("asp-action=\"BankAccounts\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateBankAccount\"");
        allFinanceViews.Should().Contain("asp-action=\"EditBankAccount\"");
        allFinanceViews.Should().Contain("asp-action=\"ArchiveBankAccount\"");
        allFinanceViews.Should().Contain("asp-action=\"BankStatements\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateBankStatement\"");
        allFinanceViews.Should().Contain("asp-action=\"CancelBankStatement\"");
        allFinanceViews.Should().Contain("asp-action=\"BankReconciliation\"");
        allFinanceViews.Should().Contain("asp-action=\"CreateBankReconciliation\"");
        allFinanceViews.Should().Contain("asp-action=\"EditBankReconciliation\"");
        allFinanceViews.Should().Contain("asp-action=\"MarkBankReconciliationMatched\"");
        allFinanceViews.Should().Contain("asp-action=\"CancelBankReconciliation\"");
        allFinanceViews.Should().Contain("asp-action=\"Postings\"");
        allFinanceViews.Should().Contain("DownloadExportPackage");
        allFinanceViews.Should().Contain("ConnectorReadinessMessage");
        allFinanceViews.Should().Contain("FinanceExportPushBlocked");
        allFinanceViews.Should().Contain("@Html.AntiForgeryToken()");
        allFinanceViews.Should().Contain("RowVersion");
        allFinanceViews.Should().NotContain("hx-post=");
        allFinanceViews.Should().NotContain("ConnectorCredential");
        allFinanceViews.Should().NotContain("RegenerateExportPackage");
        allFinanceViews.Should().NotContain("CreateJournalEntry");
        allFinanceViews.Should().NotContain("CreatePayable");
        allFinanceViews.Should().NotContain("CreateCustomerInvoice");
        allFinanceViews.Should().NotContain("DownloadInvoiceArchive");
        allFinanceViews.Should().NotContain("CreatePayment(");
        allFinanceViews.Should().NotContain("CreateRefund");
        allFinanceViews.Should().NotContain("AddPayment");
        allFinanceViews.Should().NotContain("AddShipment");
        allFinanceViews.Should().NotContain("CreateCreditNote");
        allFinanceViews.Should().NotContain("IssueCreditNote");
        allFinanceViews.Should().NotContain("ConnectorPush");
        allFinanceViews.Should().NotContain("FinanceInvoice");
        allFinanceViews.Should().NotContain("SalesInvoice");
        allFinanceViews.Should().NotContain("BankCredential");
        allFinanceViews.Should().NotContain("DirectBankSettlement");
        allFinanceViews.Should().NotContain("BankApi");
        allFinanceViews.Should().NotContain("BankTransferFailure");
        allFinanceViews.Should().NotContain("TreasuryLedger");
        allFinanceViews.Should().NotContain("ReturnedPayment");
        allFinanceViews.Should().NotContain("ReturnedTransferAutomation");
        allFinanceViews.Should().NotContain("SupplierOverpayment");
    }

    [Fact]
    public void WebAdmin_Composition_Should_Register_Finance_Reporting_Dependencies()
    {
        var composition = ReadWebAdminFile("Extensions", "DependencyInjection.cs");

        composition.Should().Contain("services.AddScoped<GetFinanceOverviewHandler>();");
        composition.Should().Contain("services.AddScoped<GetFinanceReceivablesPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetFinancePostingsPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetFinanceAccountMappingsPageHandler>();");
        composition.Should().Contain("services.AddScoped<UpsertFinanceAccountMappingHandler>();");
        composition.Should().Contain("services.AddScoped<GetFinanceExportsPageHandler>();");
        composition.Should().Contain("services.AddScoped<CreateFinanceExportBatchHandler>();");
        composition.Should().Contain("services.AddScoped<GenerateFinanceExportPackageHandler>();");
        composition.Should().Contain("services.AddScoped<DownloadFinanceExportPackageHandler>();");
        composition.Should().Contain("services.AddScoped<PushFinanceExportPackageHandler>();");
        composition.Should().Contain("services.AddScoped<SupplierInvoiceWorkflowPolicy>();");
        composition.Should().Contain("services.AddScoped<GetSupplierInvoicesPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetSupplierInvoiceDetailHandler>();");
        composition.Should().Contain("services.AddScoped<CreateSupplierInvoiceHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateSupplierInvoiceHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateSupplierInvoiceLifecycleHandler>();");
        composition.Should().Contain("services.AddScoped<PostSupplierInvoiceHandler>();");
        composition.Should().Contain("services.AddScoped<SupplierPaymentWorkflowPolicy>();");
        composition.Should().Contain("services.AddScoped<GetSupplierPaymentsPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetSupplierPaymentDetailHandler>();");
        composition.Should().Contain("services.AddScoped<GetSupplierPaymentDraftHandler>();");
        composition.Should().Contain("services.AddScoped<CreateSupplierPaymentHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateSupplierPaymentHandler>();");
        composition.Should().Contain("services.AddScoped<PostSupplierPaymentHandler>();");
        composition.Should().Contain("services.AddScoped<CancelSupplierPaymentHandler>();");
        composition.Should().Contain("services.AddScoped<ReverseSupplierPaymentHandler>();");
        composition.Should().Contain("services.AddScoped<SettleSupplierPaymentFromBankReconciliationHandler>();");
        composition.Should().Contain("services.AddScoped<CreateSupplierPaymentBankCorrectionHandler>();");
        composition.Should().Contain("services.AddScoped<PostSupplierPaymentBankCorrectionHandler>();");
        composition.Should().Contain("services.AddScoped<CancelSupplierPaymentBankCorrectionHandler>();");
        composition.Should().Contain("services.AddScoped<SupplierAdvanceWorkflowPolicy>();");
        composition.Should().Contain("services.AddScoped<GetSupplierAdvancesPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetSupplierAdvanceDetailHandler>();");
        composition.Should().Contain("services.AddScoped<GetSupplierAdvanceDraftHandler>();");
        composition.Should().Contain("services.AddScoped<CreateSupplierAdvanceHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateSupplierAdvanceHandler>();");
        composition.Should().Contain("services.AddScoped<PostSupplierAdvanceHandler>();");
        composition.Should().Contain("services.AddScoped<CancelSupplierAdvanceHandler>();");
        composition.Should().Contain("services.AddScoped<ReverseSupplierAdvanceHandler>();");
        composition.Should().Contain("services.AddScoped<ApplySupplierAdvanceHandler>();");
        composition.Should().Contain("services.AddScoped<ReverseSupplierAdvanceApplicationHandler>();");
        composition.Should().Contain("services.AddScoped<GetBankAccountsPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetBankAccountForEditHandler>();");
        composition.Should().Contain("services.AddScoped<CreateBankAccountHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateBankAccountHandler>();");
        composition.Should().Contain("services.AddScoped<ArchiveBankAccountHandler>();");
        composition.Should().Contain("services.AddScoped<GetBankStatementsPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetBankStatementImportDetailHandler>();");
        composition.Should().Contain("services.AddScoped<CreateBankStatementImportHandler>();");
        composition.Should().Contain("services.AddScoped<CancelBankStatementImportHandler>();");
        composition.Should().Contain("services.AddScoped<GetBankReconciliationPageHandler>();");
        composition.Should().Contain("services.AddScoped<GetBankReconciliationDetailHandler>();");
        composition.Should().Contain("services.AddScoped<GetBankReconciliationDraftHandler>();");
        composition.Should().Contain("services.AddScoped<CreateBankReconciliationMatchHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateBankReconciliationMatchHandler>();");
        composition.Should().Contain("services.AddScoped<MarkBankReconciliationMatchedHandler>();");
        composition.Should().Contain("services.AddScoped<CancelBankReconciliationMatchHandler>();");
        composition.Should().Contain("services.AddFinanceExportFileDeliveryAdapterIfConfigured(config);");
        composition.Should().NotContain("NoNetwork");
        composition.Should().NotContain("TestFinanceExportConnectorAdapter");
        composition.Should().NotContain("services.AddSingleton<IFinanceExportConnectorAdapter");
    }

    [Fact]
    public void Production_Source_Should_Not_Register_Test_Finance_Export_Adapters()
    {
        var srcRoot = Path.GetFullPath(Path.Combine(FindWebAdminRoot(), ".."));
        var productionSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
                .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x)
                .Select(File.ReadAllText));

        productionSource.Should().Contain("FinanceExportFileDeliveryAdapter");
        productionSource.Should().Contain("AddFinanceExportFileDeliveryAdapterIfConfigured");
        productionSource.Should().NotContain("NoNetworkFinanceExport");
        productionSource.Should().NotContain("TestFinanceExportConnectorAdapter");
        productionSource.Should().NotContain("RecordingConnectorAdapter");
    }

    private static string ReadWebAdminFile(params string[] segments)
    {
        return File.ReadAllText(Path.Combine(new[] { FindWebAdminRoot() }.Concat(segments).ToArray()));
    }

    private static string FindWebAdminRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Darwin.WebAdmin");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Darwin.WebAdmin.");
    }
}
