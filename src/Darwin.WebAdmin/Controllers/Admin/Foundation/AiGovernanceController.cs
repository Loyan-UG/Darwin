using System.Security.Claims;
using Darwin.Application.Foundation;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.WebAdmin.Controllers.Admin.Foundation;

public sealed class AiGovernanceController : AdminBaseController
{
    private readonly GetAiGovernanceReviewPageHandler _getReviewPage;
    private readonly GetAiRecommendationDetailHandler _getRecommendation;
    private readonly GetAiActionDraftDetailHandler _getActionDraft;
    private readonly GetInternalFollowUpTasksPageHandler _getFollowUpTasks;
    private readonly GetInternalFollowUpTaskDetailHandler _getFollowUpTask;
    private readonly AiGovernanceService _governance;
    private readonly AiActionHandoffService _handoff;
    private readonly InternalFollowUpTaskService _followUpTaskService;

    public AiGovernanceController(
        GetAiGovernanceReviewPageHandler getReviewPage,
        GetAiRecommendationDetailHandler getRecommendation,
        GetAiActionDraftDetailHandler getActionDraft,
        GetInternalFollowUpTasksPageHandler getFollowUpTasks,
        GetInternalFollowUpTaskDetailHandler getFollowUpTask,
        AiGovernanceService governance,
        AiActionHandoffService handoff,
        InternalFollowUpTaskService followUpTaskService)
    {
        _getReviewPage = getReviewPage ?? throw new ArgumentNullException(nameof(getReviewPage));
        _getRecommendation = getRecommendation ?? throw new ArgumentNullException(nameof(getRecommendation));
        _getActionDraft = getActionDraft ?? throw new ArgumentNullException(nameof(getActionDraft));
        _getFollowUpTasks = getFollowUpTasks ?? throw new ArgumentNullException(nameof(getFollowUpTasks));
        _getFollowUpTask = getFollowUpTask ?? throw new ArgumentNullException(nameof(getFollowUpTask));
        _governance = governance ?? throw new ArgumentNullException(nameof(governance));
        _handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
        _followUpTaskService = followUpTaskService ?? throw new ArgumentNullException(nameof(followUpTaskService));
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? businessId, string? q, AiRecommendationStatus? recommendationStatus, AiActionDraftStatus? actionDraftStatus, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var vm = await _getReviewPage.HandleAsync(new AiGovernanceReviewQuery(businessId, q, recommendationStatus, actionDraftStatus, page, pageSize), ct).ConfigureAwait(false);
        return View("~/Views/AiGovernance/Index.cshtml", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Recommendation(Guid id, CancellationToken ct = default)
    {
        var vm = await _getRecommendation.HandleAsync(id, ct).ConfigureAwait(false);
        return vm is null ? NotFound() : View("~/Views/AiGovernance/Recommendation.cshtml", vm);
    }

    [HttpGet]
    public async Task<IActionResult> ActionDraft(Guid id, CancellationToken ct = default)
    {
        var vm = await _getActionDraft.HandleAsync(id, ct).ConfigureAwait(false);
        return vm is null ? NotFound() : View("~/Views/AiGovernance/ActionDraft.cshtml", vm);
    }

    [HttpGet]
    public async Task<IActionResult> FollowUpTasks(Guid? businessId, string? q, InternalFollowUpTaskStatus? status, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var vm = await _getFollowUpTasks.HandleAsync(new InternalFollowUpTasksQuery(businessId, q, status, page, pageSize), ct).ConfigureAwait(false);
        return View("~/Views/AiGovernance/FollowUpTasks.cshtml", vm);
    }

    [HttpGet]
    public async Task<IActionResult> FollowUpTask(Guid id, CancellationToken ct = default)
    {
        var vm = await _getFollowUpTask.HandleAsync(id, ct).ConfigureAwait(false);
        return vm is null ? NotFound() : View("~/Views/AiGovernance/FollowUpTask.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptRecommendation(Guid id, string rowVersion, string reason, CancellationToken ct)
        => await ReviewRecommendationAsync(id, rowVersion, reason, AiRecommendationStatus.Accepted, ct).ConfigureAwait(false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissRecommendation(Guid id, string rowVersion, string reason, CancellationToken ct)
        => await ReviewRecommendationAsync(id, rowVersion, reason, AiRecommendationStatus.Dismissed, ct).ConfigureAwait(false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpireRecommendation(Guid id, string rowVersion, string reason, CancellationToken ct)
        => await ReviewRecommendationAsync(id, rowVersion, reason, AiRecommendationStatus.Expired, ct).ConfigureAwait(false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitActionDraft(Guid id, string rowVersion, CancellationToken ct)
    {
        var actorId = ResolveCurrentUserId();
        if (actorId == Guid.Empty)
        {
            SetErrorMessage("AiGovernanceActorRequired");
            return RedirectToAction(nameof(ActionDraft), new { id });
        }

        var result = await _governance.SubmitActionDraftForApprovalAsync(id, actorId, DecodeBase64RowVersion(rowVersion), ct).ConfigureAwait(false);
        SetResultMessage(result.Succeeded, result.Error);
        return RedirectToAction(nameof(ActionDraft), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteFollowUpTask(Guid id, string rowVersion, string reason, CancellationToken ct)
    {
        var actorId = ResolveCurrentUserId();
        if (actorId == Guid.Empty)
        {
            SetErrorMessage("AiGovernanceActorRequired");
            return RedirectToAction(nameof(FollowUpTask), new { id });
        }

        var result = await _followUpTaskService.CompleteAsync(new InternalFollowUpTaskLifecycleCommand(
            id,
            DecodeBase64RowVersion(rowVersion),
            actorId,
            reason), ct).ConfigureAwait(false);
        SetResultMessage(result.Succeeded, result.Error);
        return RedirectToAction(nameof(FollowUpTask), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelFollowUpTask(Guid id, string rowVersion, string reason, CancellationToken ct)
    {
        var actorId = ResolveCurrentUserId();
        if (actorId == Guid.Empty)
        {
            SetErrorMessage("AiGovernanceActorRequired");
            return RedirectToAction(nameof(FollowUpTask), new { id });
        }

        var result = await _followUpTaskService.CancelAsync(new InternalFollowUpTaskLifecycleCommand(
            id,
            DecodeBase64RowVersion(rowVersion),
            actorId,
            reason), ct).ConfigureAwait(false);
        SetResultMessage(result.Succeeded, result.Error);
        return RedirectToAction(nameof(FollowUpTask), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveActionDraft(Guid id, string rowVersion, string reason, CancellationToken ct)
        => await DecideActionDraftAsync(id, rowVersion, reason, approve: true, ct).ConfigureAwait(false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectActionDraft(Guid id, string rowVersion, string reason, CancellationToken ct)
        => await DecideActionDraftAsync(id, rowVersion, reason, approve: false, ct).ConfigureAwait(false);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteActionDraft(Guid id, string rowVersion, string reason, CancellationToken ct)
    {
        var actorId = ResolveCurrentUserId();
        if (actorId == Guid.Empty)
        {
            SetErrorMessage("AiGovernanceActorRequired");
            return RedirectToAction(nameof(ActionDraft), new { id });
        }

        var result = await _handoff.ExecuteApprovedDraftAsync(new ExecuteAiActionDraftCommand(
            id,
            actorId,
            DecodeBase64RowVersion(rowVersion),
            reason), ct).ConfigureAwait(false);
        SetResultMessage(result.Succeeded, result.Error);
        return RedirectToAction(nameof(ActionDraft), new { id });
    }

    private async Task<IActionResult> ReviewRecommendationAsync(Guid id, string rowVersion, string reason, AiRecommendationStatus status, CancellationToken ct)
    {
        var actorId = ResolveCurrentUserId();
        if (actorId == Guid.Empty)
        {
            SetErrorMessage("AiGovernanceActorRequired");
            return RedirectToAction(nameof(Recommendation), new { id });
        }

        var result = await _governance.ReviewRecommendationAsync(id, status, actorId, reason, DecodeBase64RowVersion(rowVersion), ct).ConfigureAwait(false);
        SetResultMessage(result.Succeeded, result.Error);
        return RedirectToAction(nameof(Recommendation), new { id });
    }

    private async Task<IActionResult> DecideActionDraftAsync(Guid id, string rowVersion, string reason, bool approve, CancellationToken ct)
    {
        var actorId = ResolveCurrentUserId();
        if (actorId == Guid.Empty)
        {
            SetErrorMessage("AiGovernanceActorRequired");
            return RedirectToAction(nameof(ActionDraft), new { id });
        }

        var version = DecodeBase64RowVersion(rowVersion);
        var result = approve
            ? await _governance.ApproveActionDraftAsync(id, actorId, reason, version, ct).ConfigureAwait(false)
            : await _governance.RejectActionDraftAsync(id, actorId, reason, version, ct).ConfigureAwait(false);
        SetResultMessage(result.Succeeded, result.Error);
        return RedirectToAction(nameof(ActionDraft), new { id });
    }

    private Guid ResolveCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub")
                    ?? User.FindFirstValue("user_id");
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }

    private void SetResultMessage(bool succeeded, string? error)
    {
        if (succeeded)
        {
            SetSuccessMessage("Saved");
        }
        else
        {
            TempData["Error"] = string.IsNullOrWhiteSpace(error) ? T("OperationFailed") : error;
        }
    }
}
