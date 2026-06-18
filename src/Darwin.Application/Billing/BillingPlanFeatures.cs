using System.Text.Json;
using System.Text.Json.Nodes;
using Darwin.Application.Billing.DTOs;

namespace Darwin.Application.Billing;

public sealed class BillingPlanFeaturesSnapshot
{
    public int MaxStaff { get; set; } = 3;
    public int MaxRewardTiers { get; set; } = 5;
    public int MonthlyPushCampaigns { get; set; }
    public bool CampaignsInApp { get; set; } = true;
    public bool CampaignsPush { get; set; }
    public bool AdvancedTargeting { get; set; }
    public bool Exports { get; set; }
    public bool Sla { get; set; }
}

public static class BillingPlanFeaturesJson
{
    public const int CurrentVersion = 1;

    public static BillingPlanFeaturesSnapshot Parse(string? featuresJson)
    {
        var snapshot = new BillingPlanFeaturesSnapshot();

        JsonObject? root = null;
        try
        {
            root = JsonNode.Parse(string.IsNullOrWhiteSpace(featuresJson) ? "{}" : featuresJson) as JsonObject;
        }
        catch (JsonException)
        {
            root = null;
        }

        if (root is null)
        {
            return snapshot;
        }

        if (TryGetObject(root, "limits", out var limits))
        {
            snapshot.MaxStaff = GetInt(limits, "maxStaff", snapshot.MaxStaff);
            snapshot.MaxRewardTiers = GetInt(limits, "maxRewardTiers", snapshot.MaxRewardTiers);
            snapshot.MonthlyPushCampaigns = GetInt(limits, "monthlyPushCampaigns", snapshot.MonthlyPushCampaigns);
        }
        else
        {
            snapshot.MaxStaff = GetInt(root, "maxStaff", snapshot.MaxStaff);
        }

        if (TryGetObject(root, "features", out var features))
        {
            snapshot.CampaignsInApp = GetBool(features, "campaignsInApp", snapshot.CampaignsInApp);
            snapshot.CampaignsPush = GetBool(features, "campaignsPush", snapshot.CampaignsPush);
            snapshot.AdvancedTargeting = GetBool(features, "advancedTargeting", snapshot.AdvancedTargeting);
            snapshot.Exports = GetBool(features, "exports", snapshot.Exports);
            snapshot.Sla = GetBool(features, "sla", snapshot.Sla);
        }
        else
        {
            snapshot.Exports = GetBool(root, "exports", snapshot.Exports);
            snapshot.Sla = GetBool(root, "sla", snapshot.Sla);
        }

        if (!snapshot.CampaignsPush)
        {
            snapshot.MonthlyPushCampaigns = 0;
        }

        return snapshot;
    }

    public static string Build(BillingPlanCreateDto dto)
    {
        var root = CreateRoot(
            dto.MaxStaff,
            dto.MaxRewardTiers,
            dto.MonthlyPushCampaigns,
            dto.CampaignsInApp,
            dto.CampaignsPush,
            dto.AdvancedTargeting,
            dto.Exports,
            dto.Sla);

        PreserveLocalizedMetadata(dto.FeaturesJson, root);
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static string Build(
        int maxStaff,
        int maxRewardTiers,
        int monthlyPushCampaigns,
        bool campaignsInApp,
        bool campaignsPush,
        bool advancedTargeting,
        bool exports,
        bool sla,
        string? existingJson = null)
    {
        var root = CreateRoot(maxStaff, maxRewardTiers, monthlyPushCampaigns, campaignsInApp, campaignsPush, advancedTargeting, exports, sla);
        PreserveLocalizedMetadata(existingJson, root);
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static void ApplyToDto(BillingPlanCreateDto dto, string? featuresJson)
    {
        var snapshot = Parse(featuresJson);
        dto.FeaturesJson = string.IsNullOrWhiteSpace(featuresJson) ? "{}" : featuresJson.Trim();
        dto.MaxStaff = snapshot.MaxStaff;
        dto.MaxRewardTiers = snapshot.MaxRewardTiers;
        dto.MonthlyPushCampaigns = snapshot.MonthlyPushCampaigns;
        dto.CampaignsInApp = snapshot.CampaignsInApp;
        dto.CampaignsPush = snapshot.CampaignsPush;
        dto.AdvancedTargeting = snapshot.AdvancedTargeting;
        dto.Exports = snapshot.Exports;
        dto.Sla = snapshot.Sla;
    }

    private static JsonObject CreateRoot(
        int maxStaff,
        int maxRewardTiers,
        int monthlyPushCampaigns,
        bool campaignsInApp,
        bool campaignsPush,
        bool advancedTargeting,
        bool exports,
        bool sla)
    {
        return new JsonObject
        {
            ["version"] = CurrentVersion,
            ["limits"] = new JsonObject
            {
                ["maxStaff"] = maxStaff,
                ["maxRewardTiers"] = maxRewardTiers,
                ["monthlyPushCampaigns"] = campaignsPush ? monthlyPushCampaigns : 0
            },
            ["features"] = new JsonObject
            {
                ["campaignsInApp"] = campaignsInApp,
                ["campaignsPush"] = campaignsPush,
                ["advancedTargeting"] = advancedTargeting,
                ["exports"] = exports,
                ["sla"] = sla
            }
        };
    }

    private static void PreserveLocalizedMetadata(string? existingJson, JsonObject root)
    {
        if (string.IsNullOrWhiteSpace(existingJson))
        {
            return;
        }

        try
        {
            var existing = JsonNode.Parse(existingJson) as JsonObject;
            if (existing?["localized"] is JsonObject localized)
            {
                root["localized"] = localized.DeepClone();
            }
        }
        catch (JsonException)
        {
            // Invalid legacy metadata should not prevent the typed feature contract from being generated.
        }
    }

    private static bool TryGetObject(JsonObject root, string propertyName, out JsonObject value)
    {
        value = root[propertyName] as JsonObject ?? new JsonObject();
        return root[propertyName] is JsonObject;
    }

    private static int GetInt(JsonObject root, string propertyName, int fallback)
    {
        return root[propertyName]?.GetValueKind() == JsonValueKind.Number &&
               root[propertyName]!.GetValue<int>() >= 0
            ? root[propertyName]!.GetValue<int>()
            : fallback;
    }

    private static bool GetBool(JsonObject root, string propertyName, bool fallback)
    {
        return root[propertyName]?.GetValueKind() == JsonValueKind.True ||
               root[propertyName]?.GetValueKind() == JsonValueKind.False
            ? root[propertyName]!.GetValue<bool>()
            : fallback;
    }
}
