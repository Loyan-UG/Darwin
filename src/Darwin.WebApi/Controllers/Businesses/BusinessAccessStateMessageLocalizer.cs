using Darwin.Application;
using Darwin.Application.Businesses.DTOs;
using Microsoft.Extensions.Localization;

namespace Darwin.WebApi.Controllers.Businesses;

internal static class BusinessAccessStateMessageLocalizer
{
    public static string? LocalizeBlockingReason(
        BusinessAccessStateDto state,
        IStringLocalizer<ValidationResource> localizer)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(localizer);

        return state.PrimaryBlockingCode switch
        {
            "membership_inactive" => localizer["BusinessMembershipInactiveAccess"],
            "user_inactive" => localizer["BusinessUserInactiveAccess"],
            "email_confirmation_required" => localizer["BusinessEmailConfirmationRequiredAccess"],
            "user_locked" => localizer["BusinessUserLockedAccess"],
            "business_pending_approval" => localizer["BusinessPendingApprovalAccess"],
            "business_suspended" when !string.IsNullOrWhiteSpace(state.SuspensionReason) => state.SuspensionReason,
            "business_suspended" => localizer["BusinessSuspendedAccess"],
            "business_inactive" => localizer["BusinessInactiveAccess"],
            "setup_incomplete" => localizer["BusinessSetupIncompleteAccess"],
            _ => null
        };
    }
}
