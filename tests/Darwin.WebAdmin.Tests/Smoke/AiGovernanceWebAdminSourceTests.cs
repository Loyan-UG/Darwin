using FluentAssertions;

namespace Darwin.WebAdmin.Tests.Smoke;

public sealed class AiGovernanceWebAdminSourceTests
{
    [Fact]
    public void AiGovernanceWorkspace_Should_Render_Internal_ReviewQueues_WithoutProviderPromptOrAutonomousExecution()
    {
        var root = RepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Controllers", "Admin", "Foundation", "AiGovernanceController.cs"));
        var viewsRoot = Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "AiGovernance");
        var views = string.Join(Environment.NewLine, Directory.GetFiles(viewsRoot, "*.cshtml").Select(File.ReadAllText));
        var layout = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Views", "Shared", "_Layout.cshtml"));
        var service = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Foundation", "AiGovernanceService.cs"));
        var queries = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Foundation", "AiGovernanceQueries.cs"));
        var providerFoundation = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Foundation", "AiProviderAdapterFoundationService.cs"));
        var handoffFoundation = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Foundation", "AiActionHandoffService.cs"));
        var timelineExecutor = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Foundation", "AiTimelineActionDraftExecutor.cs"));
        var followUpExecutor = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Foundation", "AiInternalFollowUpTaskActionDraftExecutor.cs"));
        var followUpService = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Foundation", "InternalFollowUpTaskService.cs"));
        var appComposition = File.ReadAllText(Path.Combine(root, "src", "Darwin.Application", "Extensions", "ServiceCollectionExtensions.Application.cs"));
        var webComposition = File.ReadAllText(Path.Combine(root, "src", "Darwin.WebAdmin", "Extensions", "DependencyInjection.cs"));

        layout.Should().Contain("asp-controller=\"AiGovernance\"");
        layout.Should().Contain("asp-action=\"Index\"");
        controller.Should().Contain("AcceptRecommendation");
        controller.Should().Contain("DismissRecommendation");
        controller.Should().Contain("SubmitActionDraft");
        controller.Should().Contain("ApproveActionDraft");
        controller.Should().Contain("RejectActionDraft");
        controller.Should().Contain("[ValidateAntiForgeryToken]");
        views.Should().Contain("@Html.AntiForgeryToken()");
        views.Should().Contain("rowVersion");
        views.Should().Contain("Recommendations");
        views.Should().Contain("ActionDrafts");
        service.Should().Contain("ReviewRecommendationAsync");
        service.Should().Contain("MatchesRowVersion");
        queries.Should().Contain("AsNoTracking");
        providerFoundation.Should().Contain("IAiProviderAdapter");
        providerFoundation.Should().Contain("AiScopedContextProjectionService");
        providerFoundation.Should().Contain("AiGovernanceService");
        providerFoundation.Should().Contain("No ready AI provider adapter is configured.");
        handoffFoundation.Should().Contain("IAiActionDraftExecutor");
        handoffFoundation.Should().Contain("Only approved AI action drafts can be handed off for execution.");
        handoffFoundation.Should().Contain("High-risk AI action drafts require a module-specific execution policy.");
        timelineExecutor.Should().Contain("AiTimelineActionDraftExecutor");
        timelineExecutor.Should().Contain("Note");
        timelineExecutor.Should().Contain("Activity");
        followUpExecutor.Should().Contain("AiInternalFollowUpTaskActionDraftExecutor");
        followUpExecutor.Should().Contain("AiModuleReviewTaskActionDraftExecutor");
        followUpExecutor.Should().Contain("createmodulereviewtask");
        followUpExecutor.Should().Contain("InternalFollowUpTask");
        followUpService.Should().Contain("InternalFollowUpTaskService");
        controller.Should().Contain("FollowUpTasks");
        controller.Should().Contain("CompleteFollowUpTask");
        controller.Should().Contain("CancelFollowUpTask");
        views.Should().Contain("FollowUpTasks");
        views.Should().Contain("CompleteFollowUpTask");
        views.Should().Contain("CancelFollowUpTask");
        appComposition.Should().Contain("AiProviderAdapterFoundationService");
        appComposition.Should().Contain("AiActionHandoffService");
        appComposition.Should().Contain("AiTimelineActionDraftExecutor");
        appComposition.Should().Contain("AiInternalFollowUpTaskActionDraftExecutor");
        appComposition.Should().Contain("AiModuleReviewTaskActionDraftExecutor");
        appComposition.Should().Contain("InternalFollowUpTaskService");
        appComposition.Should().NotContain("AddScoped<IAiProviderAdapter");
        appComposition.Should().Contain("AddScoped<IAiActionDraftExecutor, AiTimelineActionDraftExecutor>");
        appComposition.Should().Contain("AddScoped<IAiActionDraftExecutor, AiInternalFollowUpTaskActionDraftExecutor>");
        appComposition.Should().Contain("AddScoped<IAiActionDraftExecutor, AiModuleReviewTaskActionDraftExecutor>");
        appComposition.Should().NotContain("NoNetwork");
        appComposition.Should().NotContain("TestAiProviderAdapter");
        webComposition.Should().Contain("services.AddScoped<GetInternalFollowUpTasksPageHandler>();");
        webComposition.Should().Contain("services.AddScoped<GetInternalFollowUpTaskDetailHandler>();");

        var combined = controller + views;
        combined.Should().NotContain("[Route(\"api");
        combined.Should().NotContain("OpenAI");
        combined.Should().NotContain("ChatCompletion");
        combined.Should().NotContain("Prompt");
        combined.Should().NotContain("ApiKey");
        combined.Should().NotContain("Credential");
        combined.Should().NotContain("AccessToken");
        combined.Should().NotContain("RefreshToken");
        combined.Should().NotContain("PrivateKey");
        combined.Should().NotContain("ProviderPayload");
        combined.Should().NotContain("CreateInvoice");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("AddRefund");
        combined.Should().NotContain("SupplierPayment");
        combined.Should().NotContain("PayrollProvider");
        combined.Should().NotContain("BankApi");
        combined.Should().NotContain("JournalEntries");
        combined.Should().NotContain("MobileApi");
        combined.Should().NotContain("MemberCommerce");

        providerFoundation.Should().NotContain("OpenAI");
        providerFoundation.Should().NotContain("ChatCompletion");
        providerFoundation.Should().NotContain("ApiKey");
        providerFoundation.Should().NotContain("AccessToken");
        providerFoundation.Should().NotContain("RefreshToken");
        providerFoundation.Should().NotContain("PrivateKey");
        providerFoundation.Should().NotContain("ProviderPayload");
        providerFoundation.Should().NotContain("ExecuteActionDraft");
        timelineExecutor.Should().NotContain("Payment");
        timelineExecutor.Should().NotContain("Refund");
        timelineExecutor.Should().NotContain("JournalEntry");
        timelineExecutor.Should().NotContain("Stock");
        timelineExecutor.Should().NotContain("InvoiceArchive");
        followUpExecutor.Should().NotContain("Payment");
        followUpExecutor.Should().NotContain("Refund");
        followUpExecutor.Should().NotContain("JournalEntry");
        followUpExecutor.Should().NotContain("Stock");
        followUpExecutor.Should().NotContain("InvoiceArchive");
        followUpService.Should().NotContain("CreateInvoice");
        followUpService.Should().NotContain("AddPayment");
        followUpService.Should().NotContain("AddRefund");
        followUpService.Should().NotContain("JournalEntry");
        followUpService.Should().NotContain("Stock");
        handoffFoundation.Should().NotContain("CreateInvoice");
        handoffFoundation.Should().NotContain("AddPayment");
        handoffFoundation.Should().NotContain("AddRefund");
        handoffFoundation.Should().NotContain("SupplierPayment");
        handoffFoundation.Should().NotContain("PayrollProvider");
        handoffFoundation.Should().NotContain("BankApi");
        handoffFoundation.Should().NotContain("JournalEntries");
        combined.Should().Contain("ExecuteActionDraft");
        combined.Should().NotContain("CreateInvoice");
        combined.Should().NotContain("AddPayment");
        combined.Should().NotContain("AddRefund");
        combined.Should().NotContain("BankApi");
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
