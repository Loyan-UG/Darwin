using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class CrmFoundationWebAdminSourceTests
{
    [Fact]
    public void CrmFoundationPanel_Should_Be_RenderedFromCustomerLeadAndOpportunityEditors()
    {
        ReadWebAdminFile("Views", "Crm", "_CustomerEditorShell.cshtml")
            .Should().Contain("~/Views/Crm/_CrmFoundationPanel.cshtml");
        ReadWebAdminFile("Views", "Crm", "_LeadEditorShell.cshtml")
            .Should().Contain("~/Views/Crm/_CrmFoundationPanel.cshtml");
        ReadWebAdminFile("Views", "Crm", "_OpportunityEditorShell.cshtml")
            .Should().Contain("~/Views/Crm/_CrmFoundationPanel.cshtml");
    }

    [Fact]
    public void CrmFoundationNotePost_Should_KeepAntiForgeryAndPartialRefreshContract()
    {
        var controller = ReadWebAdminFile("Controllers", "Admin", "CRM", "CrmController.cs");
        var partial = ReadWebAdminFile("Views", "Crm", "_CrmFoundationPanel.cshtml");

        controller.Should().Contain("public async Task<IActionResult> AddCrmFoundationNote");
        controller.Should().Contain("[ValidateAntiForgeryToken]");
        controller.Should().Contain("[Bind(Prefix = \"NewNote\")]");
        controller.Should().Contain("PartialView(\"~/Views/Crm/_CrmFoundationPanel.cshtml\", panel)");
        partial.Should().Contain("asp-action=\"AddCrmFoundationNote\"");
        partial.Should().Contain("@Html.AntiForgeryToken()");
        partial.Should().Contain("hx-post=\"@Url.Action(\"AddCrmFoundationNote\", \"Crm\")\"");
        partial.Should().Contain("hx-target=\"#@Model.AddNoteTargetId\"");
    }

    private static string ReadWebAdminFile(params string[] segments)
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(new[] { root, "src", "Darwin.WebAdmin" }.Concat(segments).ToArray());
        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (File.Exists(Path.Combine(current, "Darwin.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Darwin repository root.");
    }
}
