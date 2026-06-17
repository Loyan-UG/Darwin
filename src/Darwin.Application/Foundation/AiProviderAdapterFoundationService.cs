using System.Text.Json;
using Darwin.Shared.Results;

namespace Darwin.Application.Foundation;

public interface IAiProviderAdapter
{
    string AdapterCode { get; }
    bool IsReady { get; }
    Task<Result<AiProviderAdapterResponse>> GenerateAsync(AiProviderAdapterRequest request, CancellationToken ct = default);
}

public sealed class AiProviderAdapterFoundationService
{
    private readonly AiScopedContextProjectionService _contextProjection;
    private readonly AiGovernanceService _governance;
    private readonly IReadOnlyList<IAiProviderAdapter> _adapters;

    public AiProviderAdapterFoundationService(
        AiScopedContextProjectionService contextProjection,
        AiGovernanceService governance,
        IEnumerable<IAiProviderAdapter> adapters)
    {
        _contextProjection = contextProjection ?? throw new ArgumentNullException(nameof(contextProjection));
        _governance = governance ?? throw new ArgumentNullException(nameof(governance));
        _adapters = adapters?.ToArray() ?? [];
    }

    public async Task<Result<AiProviderGenerationResultDto>> GenerateRecommendationAsync(
        AiProviderGenerationCommand command,
        CancellationToken ct = default)
    {
        var purposeKey = FoundationInputNormalizer.Key(command.PurposeKey);
        var featureAreaCode = FoundationInputNormalizer.Key(command.FeatureAreaCode);
        var recommendationType = FoundationInputNormalizer.Key(command.RecommendationType);
        var requestSummary = FoundationInputNormalizer.Required(command.RequestSummary);
        var adapterCode = FoundationInputNormalizer.Key(command.AdapterCode);

        if (purposeKey is null || featureAreaCode is null || recommendationType is null || requestSummary is null)
        {
            return Result<AiProviderGenerationResultDto>.Fail("AI provider generation requires purpose, feature area, recommendation type, and request summary.");
        }

        if (FoundationInputNormalizer.LooksSensitive(requestSummary))
        {
            return Result<AiProviderGenerationResultDto>.Fail("AI provider request summary must not contain sensitive data.");
        }

        var adapter = ResolveAdapter(adapterCode);
        if (adapter is null)
        {
            return Result<AiProviderGenerationResultDto>.Fail("No ready AI provider adapter is configured.");
        }

        var contextResult = await _contextProjection.BuildAsync(
            new AiScopedContextProjectionRequest(command.BusinessId, purposeKey, command.ModuleKeys),
            ct).ConfigureAwait(false);
        if (!contextResult.Succeeded || contextResult.Value is null)
        {
            return Result<AiProviderGenerationResultDto>.Fail(contextResult.Error ?? "AI scoped context could not be generated.");
        }

        var request = new AiProviderAdapterRequest(
            command.BusinessId,
            purposeKey,
            requestSummary,
            contextResult.Value,
            featureAreaCode,
            recommendationType);

        var providerResult = await adapter.GenerateAsync(request, ct).ConfigureAwait(false);
        if (!providerResult.Succeeded || providerResult.Value is null)
        {
            return Result<AiProviderGenerationResultDto>.Fail(SafeProviderError(providerResult.Error));
        }

        var validation = ValidateProviderResponse(providerResult.Value);
        if (!validation.Succeeded)
        {
            return Result<AiProviderGenerationResultDto>.Fail(validation.Error ?? "AI provider response was invalid.");
        }

        var metadataJson = BuildMetadataJson(adapter.AdapterCode, providerResult.Value, contextResult.Value);
        var recommendation = await _governance.CreateRecommendationAsync(new CreateAiRecommendationCommand(
            command.BusinessId,
            featureAreaCode,
            recommendationType,
            providerResult.Value.Recommendation.Title,
            providerResult.Value.Recommendation.Summary,
            providerResult.Value.Recommendation.Rationale,
            providerResult.Value.Recommendation.ConfidenceScore,
            MetadataJson: metadataJson,
            ActorUserId: command.ActorUserId), ct).ConfigureAwait(false);
        if (!recommendation.Succeeded || recommendation.Value == Guid.Empty)
        {
            return Result<AiProviderGenerationResultDto>.Fail(recommendation.Error ?? "AI recommendation could not be created.");
        }

        Guid? draftId = null;
        if (providerResult.Value.ActionDraft is not null)
        {
            var draft = providerResult.Value.ActionDraft;
            var draftResult = await _governance.CreateActionDraftAsync(new CreateAiActionDraftCommand(
                command.BusinessId,
                recommendation.Value,
                featureAreaCode,
                draft.TargetEntityType,
                draft.TargetEntityId,
                draft.CommandType,
                draft.CommandPayloadJson,
                draft.Summary,
                draft.RiskLevel,
                MetadataJson: metadataJson,
                ActorUserId: command.ActorUserId), ct).ConfigureAwait(false);
            if (!draftResult.Succeeded)
            {
                return Result<AiProviderGenerationResultDto>.Fail(draftResult.Error ?? "AI action draft could not be created.");
            }

            draftId = draftResult.Value;
        }

        return Result<AiProviderGenerationResultDto>.Ok(new AiProviderGenerationResultDto
        {
            AdapterCode = adapter.AdapterCode,
            ModelCode = FoundationInputNormalizer.Optional(providerResult.Value.ModelCode),
            RecommendationId = recommendation.Value,
            ActionDraftId = draftId,
            ModuleKeys = contextResult.Value.Modules.Select(x => x.ModuleKey).ToList(),
            InputTokenCount = providerResult.Value.InputTokenCount,
            OutputTokenCount = providerResult.Value.OutputTokenCount,
            SafeSummary = FoundationInputNormalizer.Optional(providerResult.Value.SafeSummary)
        });
    }

    private IAiProviderAdapter? ResolveAdapter(string? adapterCode)
        => _adapters.FirstOrDefault(x =>
            x.IsReady &&
            (adapterCode is null || string.Equals(FoundationInputNormalizer.Key(x.AdapterCode), adapterCode, StringComparison.OrdinalIgnoreCase)));

    private static Result ValidateProviderResponse(AiProviderAdapterResponse response)
    {
        if (FoundationInputNormalizer.LooksSensitive(response.ProviderRequestId) ||
            FoundationInputNormalizer.LooksSensitive(response.ModelCode) ||
            FoundationInputNormalizer.LooksSensitive(response.SafeSummary) ||
            FoundationInputNormalizer.LooksSensitive(response.Recommendation.Title) ||
            FoundationInputNormalizer.LooksSensitive(response.Recommendation.Summary) ||
            FoundationInputNormalizer.LooksSensitive(response.Recommendation.Rationale))
        {
            return Result.Fail("AI provider response must not contain sensitive data.");
        }

        if (response.Recommendation.ConfidenceScore is < 0 or > 100)
        {
            return Result.Fail("AI provider confidence must be between 0 and 100.");
        }

        if (FoundationInputNormalizer.Required(response.Recommendation.Title) is null ||
            FoundationInputNormalizer.Required(response.Recommendation.Summary) is null ||
            FoundationInputNormalizer.Required(response.Recommendation.Rationale) is null)
        {
            return Result.Fail("AI provider recommendation requires title, summary, and rationale.");
        }

        if (response.ActionDraft is not null &&
            (FoundationInputNormalizer.LooksSensitive(response.ActionDraft.Summary) ||
             FoundationInputNormalizer.LooksSensitive(response.ActionDraft.CommandPayloadJson) ||
             FoundationInputNormalizer.Required(response.ActionDraft.TargetEntityType) is null ||
             FoundationInputNormalizer.Required(response.ActionDraft.CommandType) is null))
        {
            return Result.Fail("AI provider action draft response is invalid or sensitive.");
        }

        return Result.Ok();
    }

    private static string SafeProviderError(string? error)
        => FoundationInputNormalizer.LooksSensitive(error)
            ? "AI provider failed with a sensitive error that was not stored."
            : FoundationInputNormalizer.Optional(error) ?? "AI provider generation failed.";

    private static string BuildMetadataJson(string adapterCode, AiProviderAdapterResponse response, AiScopedContextProjectionDto context)
    {
        var metadata = new
        {
            adapterCode,
            modelCode = FoundationInputNormalizer.Optional(response.ModelCode),
            providerRequestId = FoundationInputNormalizer.Optional(response.ProviderRequestId),
            contextPurposeKey = context.PurposeKey,
            contextGeneratedAtUtc = context.GeneratedAtUtc,
            moduleKeys = context.Modules.Select(x => x.ModuleKey).ToArray(),
            inputUnits = response.InputTokenCount,
            outputUnits = response.OutputTokenCount,
            safeSummary = FoundationInputNormalizer.Optional(response.SafeSummary)
        };

        return JsonSerializer.Serialize(metadata);
    }
}

public sealed record AiProviderGenerationCommand(
    Guid? BusinessId,
    string? PurposeKey,
    string? RequestSummary,
    string? FeatureAreaCode,
    string? RecommendationType,
    IReadOnlyCollection<string>? ModuleKeys = null,
    string? AdapterCode = null,
    Guid? ActorUserId = null);

public sealed record AiProviderAdapterRequest(
    Guid? BusinessId,
    string PurposeKey,
    string RequestSummary,
    AiScopedContextProjectionDto ScopedContext,
    string FeatureAreaCode,
    string RecommendationType);

public sealed record AiProviderAdapterResponse(
    AiProviderRecommendationResponse Recommendation,
    AiProviderActionDraftResponse? ActionDraft = null,
    string? ModelCode = null,
    string? ProviderRequestId = null,
    int? InputTokenCount = null,
    int? OutputTokenCount = null,
    string? SafeSummary = null);

public sealed record AiProviderRecommendationResponse(
    string Title,
    string Summary,
    string Rationale,
    int ConfidenceScore);

public sealed record AiProviderActionDraftResponse(
    string TargetEntityType,
    Guid? TargetEntityId,
    string CommandType,
    string CommandPayloadJson,
    string Summary,
    Darwin.Domain.Enums.AiActionRiskLevel RiskLevel);

public sealed class AiProviderGenerationResultDto
{
    public string AdapterCode { get; set; } = string.Empty;
    public string? ModelCode { get; set; }
    public Guid RecommendationId { get; set; }
    public Guid? ActionDraftId { get; set; }
    public List<string> ModuleKeys { get; set; } = new();
    public int? InputTokenCount { get; set; }
    public int? OutputTokenCount { get; set; }
    public string? SafeSummary { get; set; }
}
