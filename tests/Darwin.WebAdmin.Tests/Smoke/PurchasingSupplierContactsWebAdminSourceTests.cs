using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class PurchasingSupplierContactsWebAdminSourceTests
{
    [Fact]
    public void SupplierWorkspace_Should_ExposeContactsAndDocumentMetadataWithoutFinancialShortcuts()
    {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var view = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Inventory", "_SupplierEditorShell.cshtml"));
        var composition = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Extensions", "DependencyInjection.cs"));

        controller.Should().Contain("CreateSupplierContact");
        controller.Should().Contain("EditSupplierContact");
        controller.Should().Contain("ArchiveSupplierContact");
        controller.Should().Contain("RegisterSupplierDocument");
        composition.Should().Contain("services.AddScoped<CreateSupplierContactHandler>();");
        composition.Should().Contain("services.AddScoped<UpdateSupplierContactHandler>();");
        composition.Should().Contain("services.AddScoped<ArchiveSupplierContactHandler>();");
        composition.Should().Contain("services.AddScoped<RegisterSupplierDocumentHandler>();");

        view.Should().Contain("@T.T(\"SupplierContacts\")");
        view.Should().Contain("@T.T(\"SupplierDocuments\")");
        view.Should().Contain("asp-action=\"CreateSupplierContact\"");
        view.Should().Contain("asp-action=\"RegisterSupplierDocument\"");
        view.Should().Contain("@Html.AntiForgeryToken()");

        var combined = controller + view;
        combined.Should().NotContain("CreateSupplierInvoice");
        combined.Should().NotContain("CreateSupplierPayment");
        combined.Should().NotContain("PostSupplierPayment");
        combined.Should().NotContain("JournalEntry");
        combined.Should().NotContain("UploadSupplierDocument");
        combined.Should().NotContain("DownloadSupplierDocument");
        combined.Should().NotContain("Mobile");
        combined.Should().NotContain("Storefront");
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
