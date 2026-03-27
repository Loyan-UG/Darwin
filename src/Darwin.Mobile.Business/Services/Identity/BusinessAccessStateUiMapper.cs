using System.Globalization;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Business.Resources;

namespace Darwin.Mobile.Business.Services.Identity;

/// <summary>
/// Maps business access-state contracts to localized UI-friendly labels and messages.
/// </summary>
internal static class BusinessAccessStateUiMapper
{
    /// <summary>
    /// Returns a localized operational status label.
    /// </summary>
    public static string GetOperationalStatusLabel(BusinessAccessStateResponse state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (IsStatus(state, "Suspended"))
        {
            return AppResources.HomeBusinessStatusSuspended;
        }

        if (IsStatus(state, "PendingApproval"))
        {
            return AppResources.HomeBusinessStatusPendingApproval;
        }

        if (IsStatus(state, "Approved") && state.IsActive)
        {
            return AppResources.HomeBusinessStatusApproved;
        }

        if (IsStatus(state, "Approved"))
        {
            return AppResources.HomeBusinessStatusApprovedInactive;
        }

        return string.IsNullOrWhiteSpace(state.OperationalStatus)
            ? AppResources.HomeUnavailableValue
            : state.OperationalStatus;
    }

    /// <summary>
    /// Returns a localized message that explains why live operations are blocked or allowed.
    /// </summary>
    public static string GetOperationalStatusMessage(BusinessAccessStateResponse state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (IsStatus(state, "PendingApproval"))
        {
            return AppResources.BusinessPendingApprovalMessage;
        }

        if (IsStatus(state, "Suspended"))
        {
            return string.IsNullOrWhiteSpace(state.SuspensionReason)
                ? AppResources.BusinessSuspendedMessage
                : string.Format(
                    CultureInfo.CurrentCulture,
                    AppResources.BusinessSuspendedMessageWithReasonFormat,
                    state.SuspensionReason);
        }

        if (!state.IsActive)
        {
            return AppResources.BusinessInactiveMessage;
        }

        return state.IsOperationsAllowed
            ? AppResources.HomeOperationsAllowedMessage
            : AppResources.BusinessOperationsBlockedGeneric;
    }

    /// <summary>
    /// Builds a compact onboarding checklist summary for setup-friendly screens.
    /// </summary>
    public static string BuildSetupChecklistSummary(BusinessAccessStateResponse state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return string.Join(
            Environment.NewLine,
            FormatChecklistItem(state.HasActiveOwner, AppResources.HomeChecklistActiveOwner),
            FormatChecklistItem(state.HasPrimaryLocation, AppResources.HomeChecklistPrimaryLocation),
            FormatChecklistItem(state.HasContactEmail, AppResources.HomeChecklistContactEmail),
            FormatChecklistItem(state.HasLegalName, AppResources.HomeChecklistLegalName));
    }

    private static bool IsStatus(BusinessAccessStateResponse state, string expected)
        => string.Equals(state.OperationalStatus, expected, StringComparison.OrdinalIgnoreCase);

    private static string FormatChecklistItem(bool completed, string label)
        => $"{(completed ? "[x]" : "[ ]")} {label}";
}
