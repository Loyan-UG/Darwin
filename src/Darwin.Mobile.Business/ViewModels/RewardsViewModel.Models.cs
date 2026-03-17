using System;
using System.Globalization;
using System.Text.Json;
using Darwin.Contracts.Loyalty;
using Darwin.Mobile.Business.Resources;

namespace Darwin.Mobile.Business.ViewModels;

public sealed class RewardTierEditorItem
{
    public Guid RewardTierId { get; init; }
    public int PointsRequired { get; init; }
    public string RewardType { get; init; } = string.Empty;
    public decimal? RewardValue { get; init; }
    public string? Description { get; init; }
    public bool AllowSelfRedemption { get; init; }
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();

    public static RewardTierEditorItem FromContract(BusinessRewardTierConfigItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new RewardTierEditorItem
        {
            RewardTierId = item.RewardTierId,
            PointsRequired = item.PointsRequired,
            RewardType = item.RewardType,
            RewardValue = item.RewardValue,
            Description = item.Description,
            AllowSelfRedemption = item.AllowSelfRedemption,
            RowVersion = item.RowVersion ?? Array.Empty<byte>()
        };
    }
}


/// <summary>
/// Represents a selectable channel combination in campaign editor UI.
/// </summary>
public sealed class CampaignChannelOption
{
    public CampaignChannelOption(short value, string label)
    {
        Value = value;
        Label = label;
    }

    public short Value { get; }
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Represents a selectable lifecycle-state filter option for campaign list.
/// </summary>
public sealed class CampaignStateFilterOption
{
    public CampaignStateFilterOption(string stateKey, string label)
    {
        StateKey = stateKey;
        Label = label;
    }

    /// <summary>
    /// Contract state key; empty means "all states".
    /// </summary>
    public string StateKey { get; }

    /// <summary>
    /// Localized display label.
    /// </summary>
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Represents a selectable audience-kind filter option for campaign list.
/// </summary>
public sealed class CampaignAudienceFilterOption
{
    public CampaignAudienceFilterOption(string audienceKindKey, string label)
    {
        AudienceKindKey = audienceKindKey;
        Label = label;
    }

    /// <summary>
    /// Contract audience key; empty means "all audiences".
    /// </summary>
    public string AudienceKindKey { get; }

    /// <summary>
    /// Localized display label.
    /// </summary>
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Supported sort modes for campaign list projection in mobile business UI.
/// </summary>
public enum CampaignSortMode
{
    StartDateDesc = 0,
    StartDateAsc = 1,
    TitleAsc = 2,
    TitleDesc = 3
}

/// <summary>
/// Represents a selectable campaign sort option.
/// </summary>
public sealed class CampaignSortOption
{
    public CampaignSortOption(CampaignSortMode mode, string label)
    {
        Mode = mode;
        Label = label;
    }

    /// <summary>
    /// Internal sort mode.
    /// </summary>
    public CampaignSortMode Mode { get; }

    /// <summary>
    /// Localized display label.
    /// </summary>
    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>
/// Lightweight campaign item used by business rewards screen.
/// </summary>
public sealed class BusinessCampaignEditorItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string CampaignState { get; init; } = PromotionCampaignState.Draft;
    public bool IsActive { get; init; }
    public DateTime? StartsAtUtc { get; init; }
    public DateTime? EndsAtUtc { get; init; }
    public string? Body { get; init; }
    public short Channels { get; init; }
    public string TargetingJson { get; init; } = "{}";
    public string PayloadJson { get; init; } = "{}";
    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
    public string AudienceKindKey { get; init; } = PromotionAudienceKind.JoinedMembers;

    /// <summary>
    /// Gets a compact, localized audience summary derived from <see cref="TargetingJson"/>.
    /// This summary helps business operators quickly verify campaign segmentation directly in
    /// the list view without opening each campaign editor.
    /// </summary>
    public string AudienceSummary => BuildAudienceSummary(TargetingJson);

    public string ActivationButtonText => IsActive ? AppResources.RewardsCampaignDeactivateButton : AppResources.RewardsCampaignActivateButton;

    public static BusinessCampaignEditorItem FromContract(BusinessCampaignItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var audienceKindKey = ResolveAudienceKindKey(item.TargetingJson);

        return new BusinessCampaignEditorItem
        {
            Id = item.Id,
            Name = item.Name,
            Title = item.Title,
            CampaignState = item.CampaignState,
            IsActive = item.IsActive,
            StartsAtUtc = item.StartsAtUtc,
            EndsAtUtc = item.EndsAtUtc,
            Body = item.Body,
            Channels = item.Channels,
            TargetingJson = item.TargetingJson ?? "{}",
            PayloadJson = item.PayloadJson ?? "{}",
            RowVersion = item.RowVersion ?? Array.Empty<byte>(),
            AudienceKindKey = audienceKindKey
        };
    }

    /// <summary>
    /// Builds a concise audience/eligibility caption by parsing campaign targeting JSON.
    /// </summary>
    /// <param name="targetingJson">Raw targeting JSON as stored in campaign payload.</param>
    /// <returns>A localized one-line summary suitable for list display.</returns>
    private static string BuildAudienceSummary(string? targetingJson)
    {
        if (string.IsNullOrWhiteSpace(targetingJson))
        {
            return AppResources.RewardsCampaignAudienceSummaryDefault;
        }

        try
        {
            using var document = JsonDocument.Parse(targetingJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return AppResources.RewardsCampaignAudienceSummaryDefault;
            }

            var root = document.RootElement;
            var audienceKind = ResolveAudienceKind(root);
            var minPoints = TryGetInt(root, "minPoints");
            var maxPoints = TryGetInt(root, "maxPoints");
            var tierKey = TryGetString(root, "tierKey");

            // Support targeting payloads that store rule details under an array-based structure.
            if (root.TryGetProperty("eligibilityRules", out var rules) &&
                rules.ValueKind == JsonValueKind.Array &&
                rules.GetArrayLength() > 0)
            {
                var firstRule = rules[0];
                if (firstRule.ValueKind == JsonValueKind.Object)
                {
                    audienceKind ??= ResolveAudienceKind(firstRule);
                    minPoints ??= TryGetInt(firstRule, "minPoints");
                    maxPoints ??= TryGetInt(firstRule, "maxPoints");
                    tierKey ??= TryGetString(firstRule, "tierKey");
                }
            }

            var audienceLabel = ResolveAudienceLabel(audienceKind);
            var eligibilityLabel = BuildEligibilityLabel(minPoints, maxPoints, tierKey);

            if (string.IsNullOrWhiteSpace(eligibilityLabel))
            {
                return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignAudienceSummaryFormat, audienceLabel);
            }

            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignAudienceSummaryWithEligibilityFormat, audienceLabel, eligibilityLabel);
        }
        catch (JsonException)
        {
            return AppResources.RewardsCampaignAudienceSummaryDefault;
        }
    }

    /// <summary>
    /// Resolves canonical audience kind key from targeting JSON for filtering scenarios.
    /// </summary>
    private static string ResolveAudienceKindKey(string? targetingJson)
    {
        if (string.IsNullOrWhiteSpace(targetingJson))
        {
            return PromotionAudienceKind.JoinedMembers;
        }

        try
        {
            using var document = JsonDocument.Parse(targetingJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return PromotionAudienceKind.JoinedMembers;
            }

            return ResolveAudienceKind(document.RootElement) ?? PromotionAudienceKind.JoinedMembers;
        }
        catch (JsonException)
        {
            return PromotionAudienceKind.JoinedMembers;
        }
    }

    /// <summary>
    /// Reads audience kind from root object or the first eligibility rule object.
    /// </summary>
    private static string? ResolveAudienceKind(JsonElement root)
    {
        var direct = TryGetString(root, "audienceKind") ?? TryGetString(root, "kind");
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        if (root.TryGetProperty("eligibilityRules", out var rules) &&
            rules.ValueKind == JsonValueKind.Array &&
            rules.GetArrayLength() > 0)
        {
            var firstRule = rules[0];
            if (firstRule.ValueKind == JsonValueKind.Object)
            {
                return TryGetString(firstRule, "audienceKind") ?? TryGetString(firstRule, "kind");
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves raw audience kind into a user-facing localized label.
    /// </summary>
    private static string ResolveAudienceLabel(string? audienceKind)
    {
        if (string.Equals(audienceKind, PromotionAudienceKind.TierSegment, StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RewardsCampaignAudienceTierSegment;
        }

        if (string.Equals(audienceKind, PromotionAudienceKind.PointsThreshold, StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RewardsCampaignAudiencePointsThreshold;
        }

        if (string.Equals(audienceKind, PromotionAudienceKind.DateWindow, StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RewardsCampaignAudienceDateWindow;
        }

        return AppResources.RewardsCampaignAudienceJoinedMembers;
    }

    /// <summary>
    /// Produces a compact eligibility clause from optional rule fields.
    /// </summary>
    private static string? BuildEligibilityLabel(int? minPoints, int? maxPoints, string? tierKey)
    {
        if (!string.IsNullOrWhiteSpace(tierKey))
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityTierFormat, tierKey);
        }

        if (minPoints.HasValue && maxPoints.HasValue)
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityRangeFormat, minPoints.Value, maxPoints.Value);
        }

        if (minPoints.HasValue)
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityMinFormat, minPoints.Value);
        }

        if (maxPoints.HasValue)
        {
            return string.Format(CultureInfo.CurrentCulture, AppResources.RewardsCampaignEligibilityMaxFormat, maxPoints.Value);
        }

        return null;
    }

    /// <summary>
    /// Reads an optional string property from a JSON object element.
    /// </summary>
    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }

    /// <summary>
    /// Reads an optional integer property from a JSON object element.
    /// Supports both numeric and string-encoded integer values for compatibility.
    /// </summary>
    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
        {
            return numericValue;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        return null;
    }
}
