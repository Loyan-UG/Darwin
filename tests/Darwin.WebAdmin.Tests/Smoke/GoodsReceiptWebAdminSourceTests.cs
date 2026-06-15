using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class GoodsReceiptWebAdminSourceTests
{
    [Fact]
    public void GoodsReceiptViews_Should_Keep_Form_Guards_And_Avoid_Finance_Mutations()
    {
        var root = FindRepositoryRoot();
        var listView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "GoodsReceipts.cshtml"));
        var detailView = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "EditGoodsReceipt.cshtml"));
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryController.cs"));

        listView.Should().Contain("@Html.AntiForgeryToken()");
        detailView.Should().Contain("@Html.AntiForgeryToken()");
        detailView.Should().Contain("name=\"rowVersion\"");
        detailView.Should().Contain("if (isDraft)");
        detailView.Should().Contain("asp-for=\"Lines[i].ReceivedQuantity\"");
        detailView.Should().Contain("if (isReceived)");
        detailView.Should().Contain("asp-for=\"Lines[i].AcceptedQuantity\"");
        detailView.Should().Contain("asp-for=\"Lines[i].RejectedQuantity\"");
        detailView.Should().Contain("asp-for=\"Lines[i].DamagedQuantity\"");

        controller.Should().Contain("UpdateGoodsReceiptLifecycle");
        controller.Should().Contain("DecodeBase64RowVersion(rowVersion)");

        var combined = string.Concat(listView, detailView, controller);
        combined.Should().NotContain("SupplierInvoice");
        combined.Should().NotContain("Payable");
        combined.Should().NotContain("CreatePayment");
        combined.Should().NotContain("CreateRefund");
        combined.Should().NotContain("CreateInvoice");
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
