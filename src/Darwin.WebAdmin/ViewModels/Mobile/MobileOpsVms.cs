using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Mobile
{
    public sealed class MobileOperationsVm
    {
        public bool JwtEnabled { get; set; }
        public bool JwtSingleDeviceOnly { get; set; }
        public bool JwtRequireDeviceBinding { get; set; }
        public int JwtAccessTokenMinutes { get; set; }
        public int JwtRefreshTokenDays { get; set; }
        public int MobileQrTokenRefreshSeconds { get; set; }
        public int MobileMaxOutboxItems { get; set; }
        public bool BusinessManagementWebsiteConfigured { get; set; }
        public bool ImpressumConfigured { get; set; }
        public bool PrivacyPolicyConfigured { get; set; }
        public bool BusinessTermsConfigured { get; set; }
        public bool AccountDeletionConfigured { get; set; }
        public string DefaultCulture { get; set; } = string.Empty;
        public string? TimeZone { get; set; }
        public int AttentionBusinessCount { get; set; }
        public int PendingApprovalBusinessCount { get; set; }
        public int PendingInvitationCount { get; set; }
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
        public int BusinessesRequiringEmailSetupCount { get; set; }
        public bool EmailTransportConfigured { get; set; }
        public bool SmsTransportConfigured { get; set; }
        public bool WhatsAppTransportConfigured { get; set; }
        public bool AdminAlertRoutingConfigured { get; set; }
        public int TotalActiveDevices { get; set; }
        public int BusinessMemberDevicesCount { get; set; }
        public int StaleDevicesCount { get; set; }
        public int DevicesMissingPushTokenCount { get; set; }
        public int NotificationsDisabledCount { get; set; }
        public int AndroidDevicesCount { get; set; }
        public int IosDevicesCount { get; set; }
        public List<MobileAppVersionSnapshotVm> RecentVersions { get; set; } = new();
        public string Query { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public MobilePlatform? PlatformFilter { get; set; }
        public string StateFilter { get; set; } = string.Empty;
        public List<SelectListItem> PlatformItems { get; set; } = new();
        public List<SelectListItem> StateItems { get; set; } = new();
        public List<MobileOpsPlaybookVm> Playbooks { get; set; } = new();
        public List<MobileDeviceOpsListItemVm> Devices { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Total { get; set; }
    }

    public sealed class MobileOpsPlaybookVm
    {
        public string Title { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
        public string QueueActionLabel { get; set; } = string.Empty;
        public string QueueActionUrl { get; set; } = string.Empty;
        public string FollowUpLabel { get; set; } = string.Empty;
        public string FollowUpUrl { get; set; } = string.Empty;
    }

    public sealed class MobileAppVersionSnapshotVm
    {
        public MobilePlatform Platform { get; set; }
        public string AppVersion { get; set; } = string.Empty;
        public int DeviceCount { get; set; }
        public DateTime? LastSeenAtUtc { get; set; }
    }

    public sealed class MobileDeviceOpsListItemVm
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserDisplayName { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public MobilePlatform Platform { get; set; }
        public string? AppVersion { get; set; }
        public string? DeviceModel { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool HasPushToken { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastSeenAtUtc { get; set; }
        public int BusinessMembershipCount { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
