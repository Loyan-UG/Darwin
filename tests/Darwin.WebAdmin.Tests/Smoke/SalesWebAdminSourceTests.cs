using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class SalesWebAdminSourceTests
{
    [Fact]
    public void Sales_Workspace_Should_Be_Navigable_From_Sidebar()
    {
        var layout = ReadWebAdminFile("Views", "Shared", "_Layout.cshtml");

        layout.Should().Contain("asp-controller=\"Sales\" asp-action=\"Index\"");
        layout.Should().Contain("asp-controller=\"Sales\" asp-action=\"Quotes\"");
        layout.Should().Contain("asp-controller=\"Sales\" asp-action=\"DeliveryNotes\"");
        layout.Should().Contain("asp-controller=\"Sales\" asp-action=\"ReturnOrders\"");
        layout.Should().Contain("asp-controller=\"Sales\" asp-action=\"CreditNotes\"");
        layout.Should().Contain("asp-controller=\"Sales\" asp-action=\"Orders\"");
        layout.Should().Contain("asp-controller=\"Sales\" asp-action=\"Invoices\"");
    }

    [Fact]
    public void Sales_Controller_Should_Expose_Only_Document_Mutations_Without_Operational_Duplicates()
    {
        var controller = ReadWebAdminFile("Controllers", "Admin", "Sales", "SalesController.cs");

        controller.Should().Contain("public sealed class SalesController");
        controller.Should().Contain("[HttpGet]");
        controller.Should().Contain("CreateQuote");
        controller.Should().Contain("SendQuote");
        controller.Should().Contain("AcceptQuote");
        controller.Should().Contain("ConvertQuote");
        controller.Should().Contain("CreateOrderFromQuote");
        controller.Should().Contain("CreateDeliveryNoteFromShipment");
        controller.Should().Contain("PrepareDeliveryNote");
        controller.Should().Contain("IssueDeliveryNote");
        controller.Should().Contain("MarkDeliveryNoteShipped");
        controller.Should().Contain("MarkDeliveryNoteDelivered");
        controller.Should().Contain("CancelDeliveryNote");
        controller.Should().Contain("CreateReturnOrder");
        controller.Should().Contain("ApproveReturnOrder");
        controller.Should().Contain("ReceiveReturnOrder");
        controller.Should().Contain("InspectReturnOrder");
        controller.Should().Contain("MarkReturnOrderRefundReady");
        controller.Should().Contain("LinkReturnOrderRefund");
        controller.Should().Contain("CloseReturnOrder");
        controller.Should().Contain("CancelReturnOrder");
        controller.Should().Contain("CreateCreditNote");
        controller.Should().Contain("IssueCreditNote");
        controller.Should().Contain("VoidCreditNote");
        controller.Should().Contain("CancelCreditNote");
        controller.Should().Contain("DownloadCreditNoteSourceModel");
        controller.Should().NotContain("public async Task<IActionResult> CreateOrder(");
        controller.Should().NotContain("CreateInvoice(");
        controller.Should().NotContain("AddPayment");
        controller.Should().NotContain("AddShipment");
        controller.Should().NotContain("CreateRefund");
    }

    [Fact]
    public void Sales_Views_Should_Link_To_Operational_Workspaces_Without_New_Mutation_Forms()
    {
        var orderDetail = ReadWebAdminFile("Views", "Sales", "Order.cshtml");
        var invoiceDetail = ReadWebAdminFile("Views", "Sales", "Invoice.cshtml");
        var quoteDetail = ReadWebAdminFile("Views", "Sales", "Quote.cshtml");
        var deliveryNoteDetail = ReadWebAdminFile("Views", "Sales", "DeliveryNote.cshtml");
        var deliveryNotes = ReadWebAdminFile("Views", "Sales", "DeliveryNotes.cshtml");
        var returnOrderDetail = ReadWebAdminFile("Views", "Sales", "ReturnOrder.cshtml");
        var returnOrders = ReadWebAdminFile("Views", "Sales", "ReturnOrders.cshtml");
        var creditNoteDetail = ReadWebAdminFile("Views", "Sales", "CreditNote.cshtml");
        var creditNotes = ReadWebAdminFile("Views", "Sales", "CreditNotes.cshtml");
        var allSalesViews = string.Join(
            Environment.NewLine,
            Directory.GetFiles(Path.Combine(FindWebAdminRoot(), "Views", "Sales"), "*.cshtml")
                .OrderBy(x => x)
                .Select(File.ReadAllText));

        orderDetail.Should().Contain("asp-controller=\"Orders\" asp-action=\"Details\"");
        orderDetail.Should().Contain("asp-controller=\"Orders\" asp-action=\"CreateInvoice\"");
        invoiceDetail.Should().Contain("asp-controller=\"Crm\" asp-action=\"EditInvoice\"");
        quoteDetail.Should().Contain("asp-action=\"SendQuote\"");
        quoteDetail.Should().Contain("asp-action=\"CreateOrderFromQuote\"");
        quoteDetail.Should().Contain("asp-action=\"ConvertQuote\"");
        deliveryNoteDetail.Should().Contain("asp-controller=\"Orders\"");
        deliveryNoteDetail.Should().Contain("asp-action=\"PrepareDeliveryNote\"");
        deliveryNoteDetail.Should().Contain("asp-action=\"IssueDeliveryNote\"");
        deliveryNoteDetail.Should().Contain("asp-action=\"MarkDeliveryNoteShipped\"");
        deliveryNoteDetail.Should().Contain("asp-action=\"MarkDeliveryNoteDelivered\"");
        deliveryNoteDetail.Should().Contain("asp-action=\"CancelDeliveryNote\"");
        deliveryNotes.Should().Contain("asp-action=\"CreateDeliveryNoteFromShipment\"");
        deliveryNotes.Should().Contain("name=\"ShipmentId\"");
        deliveryNotes.Should().NotContain("asp-for=\"Quantity\"");
        deliveryNotes.Should().NotContain("name=\"Quantity\"");
        returnOrderDetail.Should().Contain("asp-action=\"ApproveReturnOrder\"");
        returnOrderDetail.Should().Contain("asp-action=\"ReceiveReturnOrder\"");
        returnOrderDetail.Should().Contain("asp-action=\"InspectReturnOrder\"");
        returnOrderDetail.Should().Contain("asp-action=\"MarkReturnOrderRefundReady\"");
        returnOrderDetail.Should().Contain("asp-action=\"LinkReturnOrderRefund\"");
        returnOrderDetail.Should().Contain("asp-controller=\"Orders\" asp-action=\"AddRefund\"");
        returnOrderDetail.Should().Contain("doc.LinkedRefundGrossMinor");
        returnOrderDetail.Should().Contain("doc.RemainingRefundGrossMinor");
        returnOrderDetail.Should().Contain("doc.Status == ReturnOrderStatus.RefundReady");
        returnOrderDetail.Should().NotContain("doc.Status == ReturnOrderStatus.RefundReady || doc.Status == ReturnOrderStatus.Refunded");
        returnOrders.Should().Contain("asp-action=\"CreateReturnOrder\"");
        returnOrders.Should().Contain("name=\"Lines[0].RequestedQuantity\"");
        returnOrders.Should().Contain("item.LinkedRefundGrossMinor");
        returnOrders.Should().Contain("item.RemainingRefundGrossMinor");
        returnOrderDetail.Should().Contain("name=\"Lines[@i].Quantity\"");
        returnOrderDetail.Should().Contain("name=\"Lines[@i].AcceptedQuantity\"");
        returnOrderDetail.Should().Contain("name=\"Lines[@i].RejectedQuantity\"");
        returnOrderDetail.Should().Contain("name=\"Lines[@i].ScrappedQuantity\"");
        returnOrderDetail.Should().Contain("name=\"Lines[@i].RestockQuantity\"");
        returnOrderDetail.Should().Contain("name=\"RefundId\"");
        returnOrderDetail.Should().NotContain("name=\"AvailableQuantity\"");
        returnOrderDetail.Should().NotContain("name=\"PaymentId\"");
        returnOrderDetail.Should().NotContain("name=\"InvoiceId\"");
        returnOrderDetail.Should().NotContain("asp-action=\"CreateRefund\"");
        returnOrderDetail.Should().NotContain("asp-action=\"CreateInvoice\"");
        returnOrderDetail.Should().NotContain("asp-action=\"AddPayment\"");
        returnOrderDetail.Should().NotContain("asp-action=\"AddShipment\"");
        creditNotes.Should().Contain("asp-action=\"CreateCreditNote\"");
        creditNotes.Should().Contain("name=\"InvoiceId\"");
        creditNotes.Should().Contain("name=\"Lines[0].InvoiceLineId\"");
        creditNotes.Should().Contain("name=\"Lines[0].CreditedQuantity\"");
        creditNoteDetail.Should().Contain("asp-action=\"IssueCreditNote\"");
        creditNoteDetail.Should().Contain("asp-action=\"VoidCreditNote\"");
        creditNoteDetail.Should().Contain("asp-action=\"CancelCreditNote\"");
        creditNoteDetail.Should().Contain("asp-action=\"DownloadCreditNoteSourceModel\"");
        creditNoteDetail.Should().Contain("doc.HasSourceModel");
        creditNoteDetail.Should().Contain("asp-controller=\"Sales\" asp-action=\"Invoice\"");
        creditNoteDetail.Should().NotContain("asp-action=\"CreateRefund\"");
        creditNoteDetail.Should().NotContain("asp-action=\"AddPayment\"");
        creditNoteDetail.Should().NotContain("asp-action=\"AddShipment\"");
        creditNoteDetail.Should().NotContain("asp-action=\"CreateInvoice\"");
        creditNoteDetail.Should().NotContain("NegativeInvoice");
        allSalesViews.Should().NotContain("asp-controller=\"Orders\" asp-action=\"Create\"");
        allSalesViews.Should().NotContain("asp-action=\"AddPayment\"");
        allSalesViews.Should().NotContain("asp-action=\"AddShipment\"");
        allSalesViews.Should().NotContain("asp-action=\"CreateRefund\"");
        allSalesViews.Should().NotContain("FinanceInvoice");
        allSalesViews.Should().NotContain("class=\"SalesInvoice\"");
        allSalesViews.Should().NotContain("name=\"SalesInvoiceId\"");
    }

    [Fact]
    public void WebAdmin_Composition_Should_Register_Business_Media_Dependencies_For_Broad_Smoke()
    {
        var composition = ReadWebAdminFile("Extensions", "DependencyInjection.cs");
        var businessesController = ReadWebAdminFile("Controllers", "Admin", "Businesses", "BusinessesController.cs");

        businessesController.Should().Contain("GetBusinessMediaLibraryHandler getBusinessMediaLibrary");
        businessesController.Should().Contain("UpdateBusinessProfileImageHandler updateBusinessProfileImage");
        businessesController.Should().Contain("CreateBusinessMediaHandler createBusinessMedia");
        businessesController.Should().Contain("UpdateBusinessMediaHandler updateBusinessMedia");
        businessesController.Should().Contain("DeleteBusinessMediaHandler deleteBusinessMedia");

        composition.Should().Contain("services.AddScoped<GetBusinessMediaLibraryHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateBusinessProfileImageHandler>();");
        composition.Should().Contain("services.AddScoped<CreateBusinessMediaHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateBusinessMediaHandler>();");
        composition.Should().Contain("services.AddScoped<DeleteBusinessMediaHandler>();");
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

        throw new InvalidOperationException("Could not find Darwin.WebAdmin root.");
    }
}
