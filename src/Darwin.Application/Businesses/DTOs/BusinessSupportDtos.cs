using System;

namespace Darwin.Application.Businesses.DTOs
{
    /// <summary>
    /// Aggregated support and onboarding counts used by WebAdmin dashboards and support queues.
    /// </summary>
    public sealed class BusinessSupportSummaryDto
    {
        public int AttentionBusinessCount { get; set; }
        public int PendingApprovalBusinessCount { get; set; }
        public int SuspendedBusinessCount { get; set; }
        public int ApprovedInactiveBusinessCount { get; set; }
        public int MissingOwnerBusinessCount { get; set; }
        public int MissingPrimaryLocationBusinessCount { get; set; }
        public int MissingContactEmailBusinessCount { get; set; }
        public int MissingLegalNameBusinessCount { get; set; }
        public int PendingInvitationCount { get; set; }
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
        public int FailedInvitationCount { get; set; }
        public int FailedActivationCount { get; set; }
        public int FailedPasswordResetCount { get; set; }
        public int FailedAdminTestCount { get; set; }
        public int SelectedBusinessPendingInvitationCount { get; set; }
        public int SelectedBusinessOpenInvitationCount { get; set; }
        public int SelectedBusinessPendingActivationCount { get; set; }
        public int SelectedBusinessLockedMemberCount { get; set; }
        public int SelectedBusinessFailedInvitationCount { get; set; }
        public int SelectedBusinessFailedActivationCount { get; set; }
        public int SelectedBusinessFailedPasswordResetCount { get; set; }
        public int SelectedBusinessFailedAdminTestCount { get; set; }
    }
}
