using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class WarehouseLocationWebAdminSourceTests
{
    [Fact]
    public void WarehouseLocationViews_Should_Render_Internal_Forms_Without_Stock_Task_Or_Finance_Mutations()
    {
        var root = RepositoryRoot();
        var listView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "Locations.cshtml"));
        var editorView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "LocationEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("asp-action=\"Locations\"");
        controller.Should().Contain("CreateLocation");
        controller.Should().Contain("EditLocation");
        controller.Should().Contain("ArchiveLocation");
        editorView.Should().Contain("@Html.AntiForgeryToken()");
        editorView.Should().Contain("asp-for=\"RowVersion\"");

        var combined = listView + editorView;
        combined.Should().NotContain("AdjustStock");
        combined.Should().NotContain("ReserveStock");
        combined.Should().NotContain("ReleaseReservation");
        combined.Should().NotContain("WarehouseTask");
        combined.Should().NotContain("SupplierInvoice");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("CreateInvoice");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("AddRefund");
    }

    [Fact]
    public void WarehouseLabelTemplateViews_Should_Render_PrintDownload_Readiness_Without_Printer_Or_Stock_Mutations()
    {
        var root = RepositoryRoot();
        var listView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "LabelTemplates.cshtml"));
        var editorView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "LabelTemplateEditor.cshtml"));
        var printView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "PrintLocationLabels.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("asp-action=\"LabelTemplates\"");
        controller.Should().Contain("CreateLabelTemplate");
        controller.Should().Contain("EditLabelTemplate");
        controller.Should().Contain("ArchiveLabelTemplate");
        controller.Should().Contain("PrintLocationLabels");
        controller.Should().Contain("DownloadLocationLabels");
        editorView.Should().Contain("@Html.AntiForgeryToken()");
        editorView.Should().Contain("asp-for=\"RowVersion\"");
        printView.Should().Contain("window.print()");
        printView.Should().Contain("DownloadLocationLabels");

        var combined = listView + editorView + printView;
        combined.Should().NotContain("PrinterCredential");
        combined.Should().NotContain("BankCredential");
        combined.Should().NotContain("WarehouseTask");
        combined.Should().NotContain("AdjustStock");
        combined.Should().NotContain("ReserveStock");
        combined.Should().NotContain("SupplierInvoice");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("CreateInvoice");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("AddRefund");
    }

    [Fact]
    public void WarehouseTaskViews_Should_Render_Internal_Review_Surface_Without_Stock_Finance_Or_Pwa_Mutations()
    {
        var root = RepositoryRoot();
        var listView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "WarehouseTasks.cshtml"));
        var editorView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "_WarehouseTaskEditorShell.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("asp-action=\"WarehouseTasks\"");
        controller.Should().Contain("WarehouseTasks");
        controller.Should().Contain("CreateWarehouseTask");
        controller.Should().Contain("EditWarehouseTask");
        controller.Should().Contain("UpdateWarehouseTaskLifecycle");
        editorView.Should().Contain("@Html.AntiForgeryToken()");
        editorView.Should().Contain("asp-for=\"RowVersion\"");
        listView.Should().Contain("WarehouseTasksTitle");
        listView.Should().Contain("NewWarehouseTask");
        listView.Should().Contain("CreatePickingTaskFromOrder");
        listView.Should().Contain("Shortage");
        controller.Should().Contain("CreatePickingTaskFromOrder");
        editorView.Should().Contain("showShortage");
        editorView.Should().Contain("WarehouseTaskType.Picking");
        editorView.Should().Contain("ShortQuantity");
        editorView.Should().Contain("ShortReason");

        var combined = listView + editorView;
        combined.Should().NotContain("AdjustInventory");
        combined.Should().NotContain("InventoryTransaction");
        combined.Should().NotContain("SupplierInvoice");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("CreateShipment");
        combined.Should().NotContain("CreateInvoice");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("AddRefund");
        combined.Should().NotContain("Pwa");
        combined.Should().NotContain("Mobile");
    }

    [Fact]
    public void GoodsReceiptTaskActions_Should_Create_ReceivingAndPutawayTasks_Without_StockOrFinance_Mutations()
    {
        var root = RepositoryRoot();
        var detailView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "EditGoodsReceipt.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryController.cs"));

        detailView.Should().Contain("CreateReceivingTaskFromGoodsReceipt");
        detailView.Should().Contain("CreatePutawayTaskFromGoodsReceipt");
        detailView.Should().Contain("@Html.AntiForgeryToken()");
        controller.Should().Contain("CreateReceivingTaskFromGoodsReceipt");
        controller.Should().Contain("CreatePutawayTaskFromGoodsReceipt");

        detailView.Should().NotContain("CreateSupplierInvoice");
        detailView.Should().NotContain("PostSupplierInvoice");
        detailView.Should().NotContain("CreateSupplierPayment");
        detailView.Should().NotContain("AdjustStock");
        detailView.Should().NotContain("ReserveStock");
        detailView.Should().NotContain("FinanceExport");
        detailView.Should().NotContain("CreateInvoice");
        detailView.Should().NotContain("AddPayment");
        detailView.Should().NotContain("AddRefund");
        detailView.Should().NotContain("Mobile");
    }

    [Fact]
    public void StockCountViews_Should_Render_Internal_Workflow_Without_Parallel_Stock_Finance_Or_Mobile_Mutations()
    {
        var root = RepositoryRoot();
        var listView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "StockCounts.cshtml"));
        var editorView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "_StockCountEditorShell.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("asp-action=\"StockCounts\"");
        controller.Should().Contain("StockCounts");
        controller.Should().Contain("CreateStockCount");
        controller.Should().Contain("EditStockCount");
        controller.Should().Contain("UpdateStockCountLifecycle");
        editorView.Should().Contain("@Html.AntiForgeryToken()");
        editorView.Should().Contain("asp-for=\"RowVersion\"");
        listView.Should().Contain("StockCountsTitle");
        listView.Should().Contain("NewStockCount");
        editorView.Should().Contain("StockCountSessionStatus.Prepared");
        editorView.Should().Contain("StockCountSessionStatus.Posted");

        var combined = listView + editorView;
        combined.Should().NotContain("AdjustStock");
        combined.Should().NotContain("ReserveStock");
        combined.Should().NotContain("ReleaseReservation");
        combined.Should().NotContain("SupplierInvoice");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("CreateInvoice");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("AddRefund");
        combined.Should().NotContain("Pwa");
        combined.Should().NotContain("Mobile");
    }

    [Fact]
    public void LotSerialHandlingUnitViews_Should_Render_Internal_Traceability_Without_Stock_Finance_Or_Mobile_Mutations()
    {
        var root = RepositoryRoot();
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "InventoryTraceability");
        var policyList = File.ReadAllText(Path.Combine(viewsRoot, "ProductTrackingPolicies.cshtml"));
        var policyEditor = File.ReadAllText(Path.Combine(viewsRoot, "ProductTrackingPolicyEditor.cshtml"));
        var lots = File.ReadAllText(Path.Combine(viewsRoot, "Lots.cshtml"));
        var lotEditor = File.ReadAllText(Path.Combine(viewsRoot, "LotEditor.cshtml"));
        var serials = File.ReadAllText(Path.Combine(viewsRoot, "SerialUnits.cshtml"));
        var serialEditor = File.ReadAllText(Path.Combine(viewsRoot, "SerialUnitEditor.cshtml"));
        var handlingUnits = File.ReadAllText(Path.Combine(viewsRoot, "HandlingUnits.cshtml"));
        var handlingUnitEditor = File.ReadAllText(Path.Combine(viewsRoot, "HandlingUnitEditor.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryTraceabilityController.cs"));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));

        layout.Should().Contain("asp-controller=\"InventoryTraceability\"");
        layout.Should().Contain("asp-action=\"ProductTrackingPolicies\"");
        layout.Should().Contain("asp-action=\"Lots\"");
        layout.Should().Contain("asp-action=\"SerialUnits\"");
        layout.Should().Contain("asp-action=\"HandlingUnits\"");
        controller.Should().Contain("CreateProductTrackingPolicy");
        controller.Should().Contain("CreateLot");
        controller.Should().Contain("CreateSerialUnit");
        controller.Should().Contain("CreateHandlingUnit");
        policyEditor.Should().Contain("@Html.AntiForgeryToken()");
        lotEditor.Should().Contain("@Html.AntiForgeryToken()");
        serialEditor.Should().Contain("@Html.AntiForgeryToken()");
        handlingUnitEditor.Should().Contain("@Html.AntiForgeryToken()");
        policyEditor.Should().Contain("asp-for=\"RowVersion\"");
        lotEditor.Should().Contain("asp-for=\"RowVersion\"");
        serialEditor.Should().Contain("asp-for=\"RowVersion\"");
        handlingUnitEditor.Should().Contain("asp-for=\"RowVersion\"");
        handlingUnitEditor.Should().Contain("HandlingUnitContents");

        var combined = policyList + policyEditor + lots + lotEditor + serials + serialEditor + handlingUnits + handlingUnitEditor + controller;
        combined.Should().NotContain("AdjustStock");
        combined.Should().NotContain("ReserveStock");
        combined.Should().NotContain("ReleaseReservation");
        combined.Should().NotContain("PostGoodsReceipt");
        combined.Should().NotContain("SupplierInvoice");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("FinanceExport");
        combined.Should().NotContain("CreateInvoice");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("AddRefund");
        combined.Should().NotContain("Pwa");
        combined.Should().NotContain("Mobile");
        combined.Should().NotContain("Public");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
