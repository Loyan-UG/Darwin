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
        controller.Should().NotContain("CreateJournalEntry");
        controller.Should().NotContain("UpdateJournalEntry");
        controller.Should().NotContain("CreateCreditNote");
        controller.Should().NotContain("IssueCreditNote");
        controller.Should().NotContain("CreateCustomerInvoice");
        controller.Should().NotContain("DownloadInvoiceArchive");
        controller.Should().NotContain("CreateRefund");
        controller.Should().NotContain("CreatePayment");
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
        allFinanceViews.Should().NotContain("SupplierPayment");
        allFinanceViews.Should().NotContain("CreateCustomerInvoice");
        allFinanceViews.Should().NotContain("DownloadInvoiceArchive");
        allFinanceViews.Should().NotContain("CreatePayment");
        allFinanceViews.Should().NotContain("CreateRefund");
        allFinanceViews.Should().NotContain("AddPayment");
        allFinanceViews.Should().NotContain("AddShipment");
        allFinanceViews.Should().NotContain("CreateCreditNote");
        allFinanceViews.Should().NotContain("IssueCreditNote");
        allFinanceViews.Should().NotContain("ConnectorPush");
        allFinanceViews.Should().NotContain("FinanceInvoice");
        allFinanceViews.Should().NotContain("SalesInvoice");
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
