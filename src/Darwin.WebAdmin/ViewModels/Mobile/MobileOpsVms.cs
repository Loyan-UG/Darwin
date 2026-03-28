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
        public string DefaultCulture { get; set; } = string.Empty;
        public string? TimeZone { get; set; }
        public int AttentionBusinessCount { get; set; }
        public int PendingApprovalBusinessCount { get; set; }
        public int OpenInvitationCount { get; set; }
        public int PendingActivationMemberCount { get; set; }
        public int LockedMemberCount { get; set; }
        public int BusinessesRequiringEmailSetupCount { get; set; }
        public bool EmailTransportConfigured { get; set; }
        public bool SmsTransportConfigured { get; set; }
        public bool WhatsAppTransportConfigured { get; set; }
        public bool AdminAlertRoutingConfigured { get; set; }
    }
}
