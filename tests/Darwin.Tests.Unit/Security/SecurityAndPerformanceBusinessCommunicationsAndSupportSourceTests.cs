using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceBusinessCommunicationsAndSupportSourceTests : SecurityAndPerformanceSourceTestBase
{

    [Fact]
    public void BusinessCommunicationsController_Should_KeepTestSendPostsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        source.Should().Contain("public sealed class BusinessCommunicationsController : AdminBaseController");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]");
        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> RetryEmailAudit(");
        source.Should().Contain("public async Task<IActionResult> SendTestEmail(");
        source.Should().Contain("public async Task<IActionResult> SendTestSms(");
        source.Should().Contain("public async Task<IActionResult> SendTestWhatsApp(");
        source.Should().Contain("public async Task<IActionResult> Index(");
        source.Should().Contain("public async Task<IActionResult> Details(");
        source.Should().Contain("public async Task<IActionResult> EmailAudits(");
        source.Should().Contain("public async Task<IActionResult> ChannelAudits(");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedResendPolicyContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private List<CommunicationResendPolicyVm> BuildResendPolicies()");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyInvitationFlow\")");
        controllerSource.Should().Contain("CurrentSafeAction = T(\"CommunicationResendPolicyInvitationSafeAction\")");
        controllerSource.Should().Contain("GenericRetryStatus = T(\"CommunicationResendPolicyInvitationRetryStatus\")");
        controllerSource.Should().Contain("OperatorEntryPoint = T(\"CommunicationResendPolicyInvitationEntryPoint\")");
        controllerSource.Should().Contain("EscalationRule = T(\"CommunicationResendPolicyInvitationEscalation\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyActivationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyPasswordResetFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyPhoneVerificationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyAdminTestFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationResendPolicyAdminAlertsFlow\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenUsers\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"BusinessCommunicationOpenPolicyAction\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAuditLog\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAlertSettings\")");

        indexViewSource.Should().Contain("@T.T(\"CommunicationResendPolicyTitle\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationCurrentSafeActionColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationGenericRetryStatusColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationOperatorEntryPointColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationEscalationRuleColumn\")");
        indexViewSource.Should().Contain("@item.CurrentSafeAction");
        indexViewSource.Should().Contain("@item.OperatorEntryPoint");
        indexViewSource.Should().Contain("@item.EscalationRule");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationResendPolicySnapshotTitle\")");
        detailsViewSource.Should().Contain("@item.CurrentSafeAction");
        detailsViewSource.Should().Contain("@item.GenericRetryStatus");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedBuiltInFlowContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<BuiltInCommunicationFlowVm> BuildBuiltInFlows()");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowInvitationName\")");
        controllerSource.Should().Contain("Trigger = T(\"CommunicationBuiltInFlowInvitationTrigger\")");
        controllerSource.Should().Contain("DeliveryPath = T(\"CommunicationBuiltInFlowInvitationDeliveryPath\")");
        controllerSource.Should().Contain("CurrentImplementationStatus = T(\"CommunicationBuiltInFlowInvitationStatus\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationBuiltInFlowInvitationNextStep\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowActivationName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowPasswordResetName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowPhoneVerificationName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowAdminTestName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowAdminAlertsName\")");
        controllerSource.Should().Contain("Name = T(\"CommunicationBuiltInFlowTestTargetsName\")");
        controllerSource.Should().Contain("private string DescribeBuiltInFlowChannel(string? channelGroup)");
        controllerSource.Should().Contain("\"Email\" => DescribeCommunicationChannel(\"Email\")");
        controllerSource.Should().Contain("\"SmsWhatsApp\" => T(\"CommunicationBuiltInFlowSmsWhatsAppChannel\")");
        controllerSource.Should().Contain("\"EmailSmsWhatsApp\" => T(\"CommunicationBuiltInFlowEmailSmsWhatsAppChannel\")");
        controllerSource.Should().Contain("\"EmailSmsWhatsAppCompact\" => T(\"CommunicationBuiltInFlowEmailSmsWhatsAppCompactChannel\")");
        controllerSource.Should().Contain("_ => string.IsNullOrWhiteSpace(channelGroup) ? T(\"CommonUnclassified\") : T(channelGroup)");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"Email\")");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"SmsWhatsApp\")");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"EmailSmsWhatsApp\")");
        controllerSource.Should().Contain("Channel = DescribeBuiltInFlowChannel(\"EmailSmsWhatsAppCompact\")");

        indexViewSource.Should().Contain("@T.T(\"Trigger\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationDeliveryPathColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CurrentStatus\")");
        indexViewSource.Should().Contain("@T.T(\"NextStep\")");
        indexViewSource.Should().Contain("@flow.Trigger");
        indexViewSource.Should().Contain("@flow.DeliveryPath");
        indexViewSource.Should().Contain("@flow.CurrentImplementationStatus");
        indexViewSource.Should().Contain("@flow.NextStep");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedCapabilityAndChannelOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<CommunicationCapabilityCoverageVm> BuildCapabilityCoverage()");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityTemplateEngine\")");
        controllerSource.Should().Contain("CurrentState = T(\"CommunicationCapabilityTemplateEngineState\")");
        controllerSource.Should().Contain("OperatorVisibility = T(\"CommunicationCapabilityTemplateEngineVisibility\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationCapabilityTemplateEngineNextStep\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityDeliveryLogging\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityRetryWorkflow\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityBusinessPolicyVisibility\")");
        controllerSource.Should().Contain("Capability = T(\"CommunicationCapabilityChannelTestTargets\")");

        controllerSource.Should().Contain("private List<CommunicationChannelOpsVm> BuildChannelOperations(SiteSettingDto settings)");
        controllerSource.Should().Contain("Channel = DescribeCommunicationChannel(\"Email\")");
        controllerSource.Should().Contain("CurrentState = emailReady ? T(\"CommunicationChannelOpsEmailReadyState\") : T(\"CommunicationChannelOpsNotReadyState\")");
        controllerSource.Should().Contain("LiveFlows = T(\"CommunicationChannelOpsEmailLiveFlows\")");
        controllerSource.Should().Contain("? T(\"CommunicationChannelOpsEmailReadyActions\")");
        controllerSource.Should().Contain(": T(\"CommunicationChannelOpsEmailNotReadyActions\")");
        controllerSource.Should().Contain("RiskBoundary = T(\"CommunicationChannelOpsEmailRiskBoundary\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationChannelOpsEmailNextStep\")");
        controllerSource.Should().Contain("Channel = \"SMS\"");
        controllerSource.Should().Contain("CurrentState = smsReady ? T(\"CommunicationChannelOpsProviderReadyState\") : T(\"CommunicationChannelOpsNotReadyState\")");
        controllerSource.Should().Contain("LiveFlows = T(\"CommunicationChannelOpsSmsLiveFlows\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsSmsFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("T(\"CommunicationChannelOpsSmsStrictRiskBoundary\")");
        controllerSource.Should().Contain("Channel = \"WhatsApp\"");
        controllerSource.Should().Contain("LiveFlows = T(\"CommunicationChannelOpsWhatsAppLiveFlows\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsWhatsAppFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("T(\"CommunicationChannelOpsWhatsAppStrictRiskBoundary\")");

        indexViewSource.Should().Contain("@item.Capability");
        indexViewSource.Should().Contain("@item.CurrentState");
        indexViewSource.Should().Contain("@item.OperatorVisibility");
        indexViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        indexViewSource.Should().Contain("@item.LiveFlows");
        indexViewSource.Should().Contain("@item.SafeOperatorActions");
        indexViewSource.Should().Contain("@item.RiskBoundary");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedTemplateInventoryContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<CommunicationTemplateInventoryVm> BuildTemplateInventory(SiteSettingDto settings)");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryInvitationFlow\")");
        controllerSource.Should().Contain("TemplateSurface = T(\"CommunicationTemplateInventoryInvitationSurface\")");
        controllerSource.Should().Contain("SubjectSource = T(\"CommunicationTemplateInventoryInvitationSubjectSource\")");
        controllerSource.Should().Contain("BodySource = T(\"CommunicationTemplateInventoryInvitationBodySource\")");
        controllerSource.Should().Contain("CurrentSubjectTemplate = SummarizeTemplate(settings.BusinessInvitationEmailSubjectTemplate, T(\"CommunicationTemplateInventoryInvitationSubjectFallback\"))");
        controllerSource.Should().Contain("CurrentBodyTemplate = SummarizeTemplate(settings.BusinessInvitationEmailBodyTemplate, T(\"CommunicationTemplateInventoryInvitationBodyFallback\"))");
        controllerSource.Should().Contain("OperatorControl = T(\"CommunicationTemplateInventoryInvitationOperatorControl\")");
        controllerSource.Should().Contain("NextStep = T(\"CommunicationTemplateInventoryInvitationNextStep\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryActivationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        controllerSource.Should().Contain("FlowName = T(\"CommunicationTemplateInventoryAdminAlertsFlow\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"OpenUsers\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"BusinessCommunicationOpenPolicyAction\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAuditLog\")");
        controllerSource.Should().Contain("OperatorActionLabel = T(\"CommunicationResendPolicyOpenAlertSettings\")");

        indexViewSource.Should().Contain("@T.T(\"CommunicationTemplateInventoryTitle\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationTemplateSurfaceColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationSubjectSourceColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationBodySourceColumn\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationOperatorControlColumn\")");
        indexViewSource.Should().Contain("@item.FlowName");
        indexViewSource.Should().Contain("@item.TemplateSurface");
        indexViewSource.Should().Contain("@item.SubjectSource");
        indexViewSource.Should().Contain("@item.BodySource");
        indexViewSource.Should().Contain("@item.OperatorControl");
        indexViewSource.Should().Contain("@item.NextStep");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedChannelFamilyContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        controllerSource.Should().Contain("private List<ChannelMessageFamilyVm> BuildChannelTemplateFamilies(SiteSettingDto settings, string? flowKey)");
        controllerSource.Should().Contain("FamilyKey = \"PhoneVerification\"");
        controllerSource.Should().Contain("FamilyKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("ChannelValue = \"SMS\"");
        controllerSource.Should().Contain("ChannelValue = \"WhatsApp\"");
        controllerSource.Should().Contain("FamilyName = T(\"CommunicationChannelFamilyPhoneVerificationName\")");
        controllerSource.Should().Contain("CurrentTemplate = SummarizeTemplate(settings.PhoneVerificationSmsTemplate, T(\"CommunicationTemplateInventoryPhoneVerificationSmsFallback\"))");
        controllerSource.Should().Contain("PolicyNote = string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelFamilyPhoneVerificationPolicyNote\")");
        controllerSource.Should().Contain("SafeUsageNote = T(\"CommunicationChannelFamilyPhoneVerificationSafeUsage\")");
        controllerSource.Should().Contain("RolloutBoundary = T(\"CommunicationChannelFamilyPhoneVerificationRolloutBoundary\")");
        controllerSource.Should().Contain("FamilyName = T(\"CommunicationChannelFamilyAdminTestName\")");
        controllerSource.Should().Contain("TargetSurface = settings.CommunicationTestSmsRecipientE164 ?? T(\"CommunicationChannelFamilyReservedTestTargetMissing\")");
        controllerSource.Should().Contain("PolicyNote = T(\"CommunicationChannelFamilyAdminTestPolicyNote\")");
        controllerSource.Should().Contain("SafeUsageNote = T(\"CommunicationChannelFamilyAdminTestSmsSafeUsage\")");
        controllerSource.Should().Contain("SafeUsageNote = T(\"CommunicationChannelFamilyAdminTestWhatsAppSafeUsage\")");
        controllerSource.Should().Contain("ActionLabel = T(\"CommunicationChannelFamilyOpenTestTargetsAction\")");
        controllerSource.Should().Contain("return string.Equals(preferredChannel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");

        indexViewSource.Should().Contain("@family.FamilyName");
        indexViewSource.Should().Contain("@family.PolicyNote");
        indexViewSource.Should().Contain("@family.SafeUsageNote");
        indexViewSource.Should().Contain("@family.RolloutBoundary");
        indexViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        indexViewSource.Should().Contain("ChannelAuditsPhoneVerificationByChannelUrl(family.ChannelValue)");
        indexViewSource.Should().Contain("ChannelAuditsAdminTestByChannelUrl(family.ChannelValue)");
        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
    }


    [Fact]
    public void BusinessCommunicationsChannelFamilyRoutes_Should_KeepCanonicalChannelValueContractsWired()
    {
        var viewModelSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        viewModelSource.Should().Contain("public string ChannelValue { get; set; } = string.Empty;");
        viewModelSource.Should().Contain("public string FamilyKey { get; set; } = string.Empty;");

        controllerSource.Should().Contain("FamilyKey = \"PhoneVerification\"");
        controllerSource.Should().Contain("FamilyKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("ChannelValue = \"SMS\"");
        controllerSource.Should().Contain("ChannelValue = \"WhatsApp\"");
        controllerSource.Should().Contain("Channel = \"SMS\"");
        controllerSource.Should().Contain("Channel = \"WhatsApp\"");

        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        indexViewSource.Should().Contain("ChannelAuditsPhoneVerificationByChannelUrl(family.ChannelValue)");
        indexViewSource.Should().Contain("ChannelAuditsAdminTestByChannelUrl(family.ChannelValue)");
        indexViewSource.Should().NotContain("asp-route-channel=\"@family.Channel\"");
        indexViewSource.Should().NotContain("channel = family.Channel })");

        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        detailsViewSource.Should().Contain("channel: family.ChannelValue");
        detailsViewSource.Should().NotContain("asp-route-channel=\"@family.Channel\"");
        detailsViewSource.Should().NotContain("channel = family.Channel })");

        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel: family.ChannelValue");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("asp-route-channel=\"@family.Channel\"");
        channelAuditsViewSource.Should().NotContain("channel = family.Channel, businessId = Model.BusinessId");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedDetailsReadinessContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private List<string> BuildActiveFlowNames(BusinessCommunicationProfileDto profile)");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowInvitation\"))");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowActivation\"))");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowPasswordReset\"))");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowAdminAlerts\"))");

        controllerSource.Should().Contain("private List<string> BuildReadinessIssues(");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSupportEmail\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSenderIdentity\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSmtp\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingAdminRouting\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssuePendingApproval\"))");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueInactive\"))");

        controllerSource.Should().Contain("private List<string> BuildRecommendedActions(");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBusinessDefaults\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionOpenSmtp\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionConfigureAdminRouting\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewMembers\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewInvitations\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewLockedMembers\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBeforeApproval\"))");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionNoImmediateAction\"))");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationReadinessIssuesTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationRecommendedNextActionsTitle\")");
        detailsViewSource.Should().Contain("@foreach (var issue in Model.ReadinessIssues)");
        detailsViewSource.Should().Contain("@foreach (var action in Model.RecommendedActions)");
        detailsViewSource.Should().Contain("@if (Model.ReadinessIssues.Count == 0)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationNoReadinessIssues\")");
    }


    [Fact]
    public void BusinessCommunicationsFamilyChannelPayloads_Should_RemainCanonical()
    {
        var viewModelSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));
        var familyBuilderStart = controllerSource.IndexOf("private List<ChannelMessageFamilyVm> BuildChannelTemplateFamilies", StringComparison.Ordinal);

        familyBuilderStart.Should().BeGreaterThanOrEqualTo(0);
        var familyBuilderSource = controllerSource[familyBuilderStart..];

        viewModelSource.Should().Contain("public string Channel { get; set; } = string.Empty;");
        viewModelSource.Should().Contain("public string ChannelValue { get; set; } = string.Empty;");

        familyBuilderSource.Should().Contain("Channel = \"SMS\"");
        familyBuilderSource.Should().Contain("Channel = \"WhatsApp\"");
        familyBuilderSource.Should().Contain("ChannelValue = \"SMS\"");
        familyBuilderSource.Should().Contain("ChannelValue = \"WhatsApp\"");
        familyBuilderSource.Should().NotContain("Channel = T(\"BusinessCommunicationSmsShort\")");
        familyBuilderSource.Should().NotContain("Channel = T(\"BusinessCommunicationWhatsAppShort\")");

        indexViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        detailsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
    }


    [Fact]
    public void BusinessCommunicationsPreferredVerificationChannelLabels_Should_RemainHelperBacked()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private string DescribePreferredPhoneVerificationChannel(string? preferredChannel)");
        controllerSource.Should().Contain("string.Equals(preferredChannel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        controllerSource.Should().Contain("? DescribeCommunicationChannel(\"WhatsApp\")");
        controllerSource.Should().Contain(": DescribeCommunicationChannel(\"SMS\")");

        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsSmsFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsWhatsAppFallbackRiskBoundary\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel))");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelFamilyPhoneVerificationPolicyNote\"), DescribePreferredPhoneVerificationChannel(settings.PhoneVerificationPreferredChannel), settings.PhoneVerificationAllowFallback ? T(\"Enabled\") : T(\"Disabled\"))");

        controllerSource.Should().NotContain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsSmsFallbackRiskBoundary\"), settings.PhoneVerificationPreferredChannel ?? T(\"BusinessCommunicationSmsShort\"))");
        controllerSource.Should().NotContain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationChannelOpsWhatsAppFallbackRiskBoundary\"), settings.PhoneVerificationPreferredChannel ?? T(\"BusinessCommunicationSmsShort\"))");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedAuditGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private string BuildAuditRecommendedAction(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationAuditRecommendedActionNoImmediateAction\")");
        controllerSource.Should().Contain("? T(\"CommunicationAuditRecommendedActionInvitationBusiness\")");
        controllerSource.Should().Contain(": T(\"CommunicationAuditRecommendedActionInvitationGeneric\")");
        controllerSource.Should().Contain("? T(\"CommunicationAuditRecommendedActionActivationBusiness\")");
        controllerSource.Should().Contain(": T(\"CommunicationAuditRecommendedActionActivationGeneric\")");
        controllerSource.Should().Contain("? T(\"CommunicationAuditRecommendedActionPasswordResetBusiness\")");
        controllerSource.Should().Contain(": T(\"CommunicationAuditRecommendedActionPasswordResetGeneric\")");
        controllerSource.Should().Contain("return T(\"CommunicationAuditRecommendedActionGeneric\")");
        controllerSource.Should().Contain("private string BuildEmailAuditChainStatusMix(string? statusMix)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.Mixed => T(\"CommunicationChainStatusMixed\")");
        controllerSource.Should().Contain("private string? BuildEmailAuditRetryBlockedReason(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedUnsupported\")");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedClosed\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedRateLimited\"), item.RecentAttemptCount24h)");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedCooldownUntil\"), item.RetryAvailableAtUtc.Value)");

        controllerSource.Should().Contain("private List<CommunicationFlowPlaybookVm> BuildAuditPlaybooks()");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookInvitationTitle\")");
        controllerSource.Should().Contain("ScopeNote = T(\"CommunicationAuditPlaybookInvitationScope\")");
        controllerSource.Should().Contain("AllowedAction = T(\"CommunicationAuditPlaybookInvitationAllowedAction\")");
        controllerSource.Should().Contain("EscalationRule = T(\"CommunicationAuditPlaybookInvitationEscalation\")");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookActivationTitle\")");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookPasswordResetTitle\")");
        controllerSource.Should().Contain("Title = T(\"CommunicationAuditPlaybookAdminTestTitle\")");

        emailAuditsViewSource.Should().Contain("@playbook.ScopeNote");
        emailAuditsViewSource.Should().Contain("@playbook.AllowedAction");
        emailAuditsViewSource.Should().Contain("@playbook.EscalationRule");
        detailsViewSource.Should().Contain("@item.RecommendedAction");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedFilterContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildFilterItems(BusinessCommunicationSetupFilter selectedFilter)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterNeedsSetup\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterMissingSupportEmail\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterMissingSenderIdentity\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterTransactionalEnabled\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterMarketingEnabled\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterOperationalAlertsEnabled\")");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationSetupFilterAllBusinesses\")");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildAuditStatusItems(string? selectedStatus)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationAuditStatusAll\")");
        controllerSource.Should().Contain("private string DescribeDeliveryStatus(string? status)");
        controllerSource.Should().Contain("\"Sent\" => T(\"Sent\")");
        controllerSource.Should().Contain("\"Failed\" => T(\"Failed\")");
        controllerSource.Should().Contain("\"Pending\" => T(\"Pending\")");
        controllerSource.Should().Contain("_ => string.IsNullOrWhiteSpace(status) ? T(\"CommonUnclassified\") : T(status)");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Sent\"), \"Sent\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Failed\"), \"Failed\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Pending\"), \"Pending\"");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildAuditFlowItems(string? selectedFlowKey)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationAuditFlowAll\")");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildChannelItems(string? selectedChannel)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationChannelAll\")");
        controllerSource.Should().Contain("new SelectListItem(DescribeCommunicationChannel(\"SMS\"), \"SMS\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeCommunicationChannel(\"WhatsApp\"), \"WhatsApp\"");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildChannelProviderItems(IEnumerable<string> providers, string? selectedProvider)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationProviderAll\")");
        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildChannelFlowItems(string? selectedFlowKey)");
        controllerSource.Should().Contain("new SelectListItem(T(\"CommunicationChannelFlowAll\")");

        emailAuditsViewSource.Should().Contain("asp-items=\"Model.StatusItems\"");
        emailAuditsViewSource.Should().Contain("asp-items=\"Model.FlowItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.ProviderItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.ChannelItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.FlowItems\"");
        channelAuditsViewSource.Should().Contain("asp-items=\"Model.StatusItems\"");
    }


    [Fact]
    public void BusinessCommunicationsStatusAndChannelShortcutHelpers_Should_RemainSourceBacked()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private string DescribeDeliveryStatus(string? status)");
        controllerSource.Should().Contain("\"Sent\" => T(\"Sent\")");
        controllerSource.Should().Contain("\"Failed\" => T(\"Failed\")");
        controllerSource.Should().Contain("\"Pending\" => T(\"Pending\")");
        controllerSource.Should().Contain("_ => string.IsNullOrWhiteSpace(status) ? T(\"CommonUnclassified\") : T(status)");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Sent\"), \"Sent\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Failed\"), \"Failed\"");
        controllerSource.Should().Contain("new SelectListItem(DescribeDeliveryStatus(\"Pending\"), \"Pending\"");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Sent\"), \"Sent\"");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Failed\"), \"Failed\"");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Pending\"), \"Pending\"");

        indexViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        indexViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"SMS\")</a>");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"WhatsApp\")</a>");

        detailsViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        detailsViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        detailsViewSource.Should().NotContain("@T.T(\"SMS\") @(Model.SmsTransportConfigured ? T.T(\"CommonReadyBadge\") : T.T(\"CommonMissingBadge\"))");
        detailsViewSource.Should().NotContain("@T.T(\"WhatsApp\") @(Model.WhatsAppTransportConfigured ? T.T(\"CommonReadyBadge\") : T.T(\"CommonMissingBadge\"))");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepIndexWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Index(");
        controllerSource.Should().Contain("var summary = await _getSummary.HandleAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (items, total) = await _getSetupPage.HandleAsync(page, pageSize, query, setupOnly, filter, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (emailAudits, _, _) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("pageSize: 10,");
        controllerSource.Should().Contain("var (channelAudits, channelAuditSummary) = await _getChannelDispatchActivity");
        controllerSource.Should().Contain("var vm = new BusinessCommunicationOpsVm");
        controllerSource.Should().Contain("Transport = new BusinessCommunicationOpsTransportVm");
        controllerSource.Should().Contain("Summary = new BusinessCommunicationOpsSummaryPanelVm");
        controllerSource.Should().Contain("BuiltInFlows = BuildBuiltInFlows(),");
        controllerSource.Should().Contain("TemplateInventory = BuildTemplateInventory(settings),");
        controllerSource.Should().Contain("CapabilityCoverage = BuildCapabilityCoverage(),");
        controllerSource.Should().Contain("ChannelOperations = BuildChannelOperations(settings),");
        controllerSource.Should().Contain("ChannelTemplateFamilies = BuildChannelTemplateFamilies(settings, null),");
        controllerSource.Should().Contain("ResendPolicies = BuildResendPolicies(),");
        controllerSource.Should().Contain("ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("RecentEmailAudits = emailAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("TemplateKey = x.TemplateKey,");
        controllerSource.Should().Contain("CorrelationKey = x.CorrelationKey,");
        controllerSource.Should().Contain("IntendedRecipientEmail = x.IntendedRecipientEmail,");
        controllerSource.Should().Contain("ProviderMessageId = x.ProviderMessageId,");
        controllerSource.Should().Contain("RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("IntendedRecipientAddress = x.IntendedRecipientAddress,");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessCommunicationSetupListItemVm");
        controllerSource.Should().Contain("return RenderCommunicationsWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderCommunicationsWorkspace(BusinessCommunicationOpsVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/Index.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"Index\", vm);");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepDetailsWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Details(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var profile = await _getProfile.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessCommunicationProfileNotFound\");");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (recentAudits, _, _) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("pageSize: 5,");
        controllerSource.Should().Contain("var vm = new BusinessCommunicationProfileVm");
        controllerSource.Should().Contain("ActiveFlowNames = BuildActiveFlowNames(profile),");
        controllerSource.Should().Contain("TemplateInventory = BuildTemplateInventory(settings),");
        controllerSource.Should().Contain("ChannelOperations = BuildChannelOperations(settings),");
        controllerSource.Should().Contain("ChannelTemplateFamilies = BuildChannelTemplateFamilies(settings, null),");
        controllerSource.Should().Contain("ResendPolicies = BuildResendPolicies(),");
        controllerSource.Should().Contain("ReadinessIssues = BuildReadinessIssues(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecommendedActions = BuildRecommendedActions(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecentEmailAudits = recentAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("IntendedRecipientEmail = x.IntendedRecipientEmail,");
        controllerSource.Should().Contain("var (channelAudits, channelAuditSummary) = await _getChannelDispatchActivity");
        controllerSource.Should().Contain("vm.ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("vm.RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("TemplateKey = x.TemplateKey,");
        controllerSource.Should().Contain("CorrelationKey = x.CorrelationKey,");
        controllerSource.Should().Contain("return RenderCommunicationProfileWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderCommunicationProfileWorkspace(BusinessCommunicationProfileVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/Details.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"Details\", vm);");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepEmailAuditsWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> EmailAudits(");
        controllerSource.Should().Contain("var (items, total, chainSummary) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("var summary = await _getEmailDispatchAuditsPage.GetSummaryAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var vm = new EmailDispatchAuditsListVm");
        controllerSource.Should().Contain("CanSendTestEmail = await CanSendTestEmailAsync(ct).ConfigureAwait(false),");
        controllerSource.Should().Contain("Summary = new EmailDispatchAuditSummaryVm");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new EmailDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("StatusMix = BuildEmailAuditChainStatusMix(chainSummary.StatusMix),");
        controllerSource.Should().Contain("PageSizeItems = BuildPageSizeItems(pageSize),");
        controllerSource.Should().Contain("StatusItems = BuildAuditStatusItems(status),");
        controllerSource.Should().Contain("FlowItems = BuildAuditFlowItems(flowKey),");
        controllerSource.Should().Contain("Playbooks = BuildAuditPlaybooks(),");
        controllerSource.Should().Contain("Items = items.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("RecipientEmail = x.RecipientEmail,");
        controllerSource.Should().Contain("IntendedRecipientEmail = x.IntendedRecipientEmail,");
        controllerSource.Should().Contain("TemplateKey = x.TemplateKey,");
        controllerSource.Should().Contain("CorrelationKey = x.CorrelationKey,");
        controllerSource.Should().Contain("ProviderMessageId = x.ProviderMessageId,");
        controllerSource.Should().Contain("RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),");
        controllerSource.Should().Contain("ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),");
        controllerSource.Should().Contain("RecommendedAction = BuildAuditRecommendedAction(x)");
        controllerSource.Should().Contain("return RenderEmailAuditsWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderEmailAuditsWorkspace(EmailDispatchAuditsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/EmailAudits.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"EmailAudits\", vm);");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelAuditsWorkspaceCompositionAndRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> ChannelAudits(");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var filter = new ChannelDispatchAuditFilterDto");
        controllerSource.Should().Contain("var (items, total, summary, chainSummary, providerSummary) = await _getChannelDispatchActivity");
        controllerSource.Should().Contain("HandlePageAsync(page, pageSize, filter, ct)");
        controllerSource.Should().Contain("var vm = new ChannelDispatchAuditsListVm");
        controllerSource.Should().Contain("Summary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new ChannelDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelChainRecommendedAction(chainSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelChainEscalationHint(chainSummary.EscalationHint),");
        controllerSource.Should().Contain("ProviderSummary = providerSummary == null ? null : new ChannelDispatchProviderSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelProviderRecommendedAction(providerSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelProviderEscalationHint(providerSummary.EscalationHint)");
        controllerSource.Should().Contain("TemplateFamilies = BuildChannelTemplateFamilies(settings, filter.FlowKey),");
        controllerSource.Should().Contain("Items = items.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("TemplateKey = x.TemplateKey,");
        controllerSource.Should().Contain("CorrelationKey = x.CorrelationKey,");
        controllerSource.Should().Contain("IntendedRecipientAddress = x.IntendedRecipientAddress,");
        controllerSource.Should().Contain("ProviderMessageId = x.ProviderMessageId,");
        controllerSource.Should().Contain("ActionPolicyState = BuildChannelAuditActionPolicyState(x.ActionPolicyState),");
        controllerSource.Should().Contain("ActionBlockedReason = BuildChannelAuditActionBlockedReason(x),");
        controllerSource.Should().Contain("EscalationReason = BuildChannelAuditEscalationReason(x),");
        controllerSource.Should().Contain("ProviderItems = BuildChannelProviderItems(items.Select(x => x.Provider), provider),");
        controllerSource.Should().Contain("ChannelItems = BuildChannelItems(channel),");
        controllerSource.Should().Contain("FlowItems = BuildChannelFlowItems(flowKey),");
        controllerSource.Should().Contain("StatusItems = BuildAuditStatusItems(status)");
        controllerSource.Should().Contain("return RenderChannelAuditsWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderChannelAuditsWorkspace(ChannelDispatchAuditsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/BusinessCommunications/ChannelAudits.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"ChannelAudits\", vm);");
    }

    [Fact]
    public void ChannelAuditsWorkspace_Should_KeepIdentityLifecycleRemediationTenantScoped()
    {
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        channelAuditsViewSource.Should().Contain("var memberSupportFilter = string.Equals(item.FlowKey, \"PasswordReset\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("? Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked");
        channelAuditsViewSource.Should().Contain(": Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation");
        channelAuditsViewSource.Should().Contain("if (item.BusinessId.HasValue)");
        channelAuditsViewSource.Should().Contain("string BusinessMembersUrl(Guid? businessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter? filter) => Url.Action(\"Members\", \"Businesses\", new { businessId, filter }) ?? string.Empty;");
        channelAuditsViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.BusinessId, memberSupportFilter)\"");
        channelAuditsViewSource.Should().Contain("string UsersQueueUrl(Darwin.Application.Identity.DTOs.UserQueueFilter filter, string? query = null) => Url.Action(\"Index\", \"Users\", new { filter, query }) ?? string.Empty;");
        channelAuditsViewSource.Should().Contain("hx-get=\"@UsersQueueUrl(userQueueFilter)\"");
        channelAuditsViewSource.Should().NotContain("hx-get=\"@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Unconfirmed)\"");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepIndexWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("Summary = new BusinessCommunicationOpsSummaryPanelVm");
        controllerSource.Should().Contain("TransactionalEmailBusinessesCount = summary.BusinessesWithCustomerEmailNotificationsEnabledCount,");
        controllerSource.Should().Contain("MarketingEmailBusinessesCount = summary.BusinessesWithMarketingEmailsEnabledCount,");
        controllerSource.Should().Contain("OperationalAlertBusinessesCount = summary.BusinessesWithOperationalAlertEmailsEnabledCount,");
        controllerSource.Should().Contain("MissingSupportEmailCount = summary.BusinessesMissingSupportEmailCount,");
        controllerSource.Should().Contain("MissingSenderIdentityCount = summary.BusinessesMissingSenderIdentityCount,");
        controllerSource.Should().Contain("BusinessesRequiringEmailSetupCount = summary.BusinessesRequiringEmailSetupCount");
        controllerSource.Should().Contain("ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("RecentEmailAudits = emailAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),");
        controllerSource.Should().Contain("ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),");
        controllerSource.Should().Contain("RecommendedAction = BuildAuditRecommendedAction(x)");
        controllerSource.Should().Contain("RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessCommunicationSetupListItemVm");
        controllerSource.Should().Contain("CommunicationReplyToEmail = x.CommunicationReplyToEmail,");
        controllerSource.Should().Contain("OperationalAlertEmailsEnabled = x.OperationalAlertEmailsEnabled,");
        controllerSource.Should().Contain("MissingSenderIdentity = x.MissingSenderIdentity");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepDetailsWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("var vm = new BusinessCommunicationProfileVm");
        controllerSource.Should().Contain("Id = profile.Id,");
        controllerSource.Should().Contain("Name = profile.Name,");
        controllerSource.Should().Contain("LegalName = profile.LegalName,");
        controllerSource.Should().Contain("OperationalStatus = profile.OperationalStatus,");
        controllerSource.Should().Contain("PendingInvitationCount = profile.PendingInvitationCount,");
        controllerSource.Should().Contain("OpenInvitationCount = profile.OpenInvitationCount,");
        controllerSource.Should().Contain("PendingActivationMemberCount = profile.PendingActivationMemberCount,");
        controllerSource.Should().Contain("LockedMemberCount = profile.LockedMemberCount,");
        controllerSource.Should().Contain("ActiveFlowNames = BuildActiveFlowNames(profile),");
        controllerSource.Should().Contain("ReadinessIssues = BuildReadinessIssues(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecommendedActions = BuildRecommendedActions(profile, emailTransportConfigured, adminAlertRoutingConfigured),");
        controllerSource.Should().Contain("RecentEmailAudits = recentAudits.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("vm.ChannelAuditSummary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("vm.RecentChannelAudits = channelAudits.Select(x => new ChannelDispatchAuditListItemVm");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepEmailAuditsWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("Summary = new EmailDispatchAuditSummaryVm");
        controllerSource.Should().Contain("TotalCount = summary.TotalCount,");
        controllerSource.Should().Contain("FailedCount = summary.FailedCount,");
        controllerSource.Should().Contain("SentCount = summary.SentCount,");
        controllerSource.Should().Contain("PendingCount = summary.PendingCount,");
        controllerSource.Should().Contain("StalePendingCount = summary.StalePendingCount,");
        controllerSource.Should().Contain("BusinessLinkedFailureCount = summary.BusinessLinkedFailureCount,");
        controllerSource.Should().Contain("FailedInvitationCount = summary.FailedInvitationCount,");
        controllerSource.Should().Contain("FailedActivationCount = summary.FailedActivationCount,");
        controllerSource.Should().Contain("FailedPasswordResetCount = summary.FailedPasswordResetCount,");
        controllerSource.Should().Contain("FailedAdminTestCount = summary.FailedAdminTestCount,");
        controllerSource.Should().Contain("RetryReadyCount = summary.RetryReadyCount,");
        controllerSource.Should().Contain("RetryBlockedCount = summary.RetryBlockedCount,");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new EmailDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("StatusMix = BuildEmailAuditChainStatusMix(chainSummary.StatusMix),");
        controllerSource.Should().Contain("RecentHistory = chainSummary.RecentHistory.Select(x => new EmailDispatchAuditChainHistoryItemVm");
        controllerSource.Should().Contain("Items = items.Select(x => new EmailDispatchAuditListItemVm");
        controllerSource.Should().Contain("RetryBlockedReason = BuildEmailAuditRetryBlockedReason(x),");
        controllerSource.Should().Contain("ChainStatusMix = BuildEmailAuditChainStatusMix(x.ChainStatusMix),");
        controllerSource.Should().Contain("RecommendedAction = BuildAuditRecommendedAction(x)");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelAuditsWorkspaceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("var filter = new ChannelDispatchAuditFilterDto");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("RecipientAddress = recipientAddress ?? string.Empty,");
        controllerSource.Should().Contain("Provider = provider ?? string.Empty,");
        controllerSource.Should().Contain("Channel = channel ?? string.Empty,");
        controllerSource.Should().Contain("FlowKey = flowKey ?? string.Empty,");
        controllerSource.Should().Contain("Status = status ?? string.Empty,");
        controllerSource.Should().Contain("FailedOnly = failedOnly,");
        controllerSource.Should().Contain("ActionBlockedOnly = actionBlockedOnly,");
        controllerSource.Should().Contain("ChainResolvedOnly = chainResolvedOnly");
        controllerSource.Should().Contain("Summary = new ChannelDispatchAuditSummaryVm");
        controllerSource.Should().Contain("TotalCount = summary.TotalCount,");
        controllerSource.Should().Contain("FailedCount = summary.FailedCount,");
        controllerSource.Should().Contain("PendingCount = summary.PendingCount,");
        controllerSource.Should().Contain("SmsCount = summary.SmsCount,");
        controllerSource.Should().Contain("WhatsAppCount = summary.WhatsAppCount,");
        controllerSource.Should().Contain("PhoneVerificationCount = summary.PhoneVerificationCount,");
        controllerSource.Should().Contain("AdminTestCount = summary.AdminTestCount,");
        controllerSource.Should().Contain("ProviderReviewCount = summary.ProviderReviewCount,");
        controllerSource.Should().Contain("ProviderRecoveredCount = summary.ProviderRecoveredCount");
        controllerSource.Should().Contain("ChainSummary = chainSummary == null ? null : new ChannelDispatchAuditChainSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelChainRecommendedAction(chainSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelChainEscalationHint(chainSummary.EscalationHint),");
        controllerSource.Should().Contain("ProviderSummary = providerSummary == null ? null : new ChannelDispatchProviderSummaryVm");
        controllerSource.Should().Contain("RecommendedAction = BuildChannelProviderRecommendedAction(providerSummary.RecommendedAction),");
        controllerSource.Should().Contain("EscalationHint = BuildChannelProviderEscalationHint(providerSummary.EscalationHint)");
        controllerSource.Should().Contain("Items = items.Select(x => new ChannelDispatchAuditListItemVm");
        controllerSource.Should().Contain("ActionPolicyState = BuildChannelAuditActionPolicyState(x.ActionPolicyState),");
        controllerSource.Should().Contain("ActionBlockedReason = BuildChannelAuditActionBlockedReason(x),");
        controllerSource.Should().Contain("EscalationReason = BuildChannelAuditEscalationReason(x),");
        controllerSource.Should().Contain("ProviderRecentAttemptCount24h = x.ProviderRecentAttemptCount24h,");
        controllerSource.Should().Contain("ProviderFailureCount24h = x.ProviderFailureCount24h,");
        controllerSource.Should().Contain("ProviderPressureState = x.ProviderPressureState,");
        controllerSource.Should().Contain("ProviderRecoveryState = x.ProviderRecoveryState,");
        controllerSource.Should().Contain("ProviderLastSuccessfulAttemptAtUtc = x.ProviderLastSuccessfulAttemptAtUtc");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepRetryEmailAuditPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> RetryEmailAudit(");
        controllerSource.Should().Contain("var result = await _retryEmailDispatchAudit");
        controllerSource.Should().Contain(".HandleAsync(new RetryEmailDispatchAuditDto { AuditId = id }, ct)");
        controllerSource.Should().Contain("SetSuccessMessage(\"EmailFlowRetriedSuccessfully\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationEmailRetryFailedFallback\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(");
        controllerSource.Should().Contain("nameof(EmailAudits),");
        controllerSource.Should().Contain("chainFollowUpOnly,");
        controllerSource.Should().Contain("chainResolvedOnly,");
        controllerSource.Should().Contain("businessId");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepSendTestEmailPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendTestEmail(CancellationToken ct = default)");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var emailTransportConfigured = IsEmailTransportConfigured(settings);");
        controllerSource.Should().Contain("SetErrorMessage(\"EmailTransportNotReadyForCommunicationTest\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationTestInboxNotConfigured\")");
        controllerSource.Should().Contain("BuildCommunicationTestPlaceholders(");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"Email\"),");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(emailTransportConfigured)");
        controllerSource.Should().Contain("_db.Set<EmailDispatchOperation>().Add(new EmailDispatchOperation");
        controllerSource.Should().Contain("FlowKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("TemplateKey = \"AdminCommunicationTestEmail\"");
        controllerSource.Should().Contain("TempData[\"Success\"] = string.Format(T(\"CommunicationTestEmailQueuedMessage\"), settings.CommunicationTestInboxEmail);");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepSendTestSmsPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendTestSms(");
        controllerSource.Should().Contain("var smsTransportConfigured = settings.SmsEnabled &&");
        controllerSource.Should().Contain("SetErrorMessage(\"SmsTransportNotReadyForCommunicationTest\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationTestSmsRecipientNotConfigured\")");
        controllerSource.Should().Contain("var smsCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(");
        controllerSource.Should().Contain("\"SMS\",");
        controllerSource.Should().Contain("TempData[\"Error\"] = string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestSmsCooldownMessage\"), smsCooldownUntilUtc.Value);");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"SMS\"),");
        controllerSource.Should().Contain("_db.Set<ChannelDispatchOperation>().Add(new ChannelDispatchOperation");
        controllerSource.Should().Contain("Channel = \"SMS\",");
        controllerSource.Should().Contain("TemplateKey = \"AdminCommunicationTestSms\",");
        controllerSource.Should().Contain("FlowKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("await _db.SaveChangesAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("TempData[\"Success\"] = string.Format(T(\"CommunicationTestSmsQueuedMessage\"), settings.CommunicationTestSmsRecipientE164);");
        controllerSource.Should().Contain("return RedirectToChannelAuditsOrIndex(");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepSendTestWhatsAppPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendTestWhatsApp(");
        controllerSource.Should().Contain("var whatsAppTransportConfigured = settings.WhatsAppEnabled &&");
        controllerSource.Should().Contain("SetErrorMessage(\"WhatsAppTransportNotReadyForCommunicationTest\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationTestWhatsAppRecipientNotConfigured\")");
        controllerSource.Should().Contain("var whatsAppCooldownUntilUtc = await GetChannelTestCooldownUntilUtcAsync(");
        controllerSource.Should().Contain("\"WhatsApp\",");
        controllerSource.Should().Contain("TempData[\"Error\"] = string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestWhatsAppCooldownMessage\"), whatsAppCooldownUntilUtc.Value);");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"WhatsApp\"),");
        controllerSource.Should().Contain("_db.Set<ChannelDispatchOperation>().Add(new ChannelDispatchOperation");
        controllerSource.Should().Contain("Channel = \"WhatsApp\",");
        controllerSource.Should().Contain("TemplateKey = \"AdminCommunicationTestWhatsApp\",");
        controllerSource.Should().Contain("FlowKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("await _db.SaveChangesAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("TempData[\"Success\"] = string.Format(T(\"CommunicationTestWhatsAppQueuedMessage\"), settings.CommunicationTestWhatsAppRecipientE164);");
        controllerSource.Should().Contain("return RedirectToChannelAuditsOrIndex(");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelTestRedirectHelperWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private IActionResult RedirectToChannelAuditsOrIndex(");
        controllerSource.Should().Contain("if (!returnToChannelAudits)");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("return RedirectOrHtmx(");
        controllerSource.Should().Contain("nameof(ChannelAudits)");
        controllerSource.Should().Contain("recipientAddress,");
        controllerSource.Should().Contain("provider,");
        controllerSource.Should().Contain("channel,");
        controllerSource.Should().Contain("flowKey,");
        controllerSource.Should().Contain("status,");
        controllerSource.Should().Contain("chainFollowUpOnly,");
        controllerSource.Should().Contain("chainResolvedOnly,");
        controllerSource.Should().Contain("businessId");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepChannelTestCooldownHelperWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private async Task<DateTime?> GetChannelTestCooldownUntilUtcAsync(");
        controllerSource.Should().Contain("var latestAttemptAtUtc = await _db.Set<ChannelDispatchAudit>()");
        controllerSource.Should().Contain(".AsNoTracking()");
        controllerSource.Should().Contain("x.FlowKey == \"AdminCommunicationTest\" &&");
        controllerSource.Should().Contain("x.Channel == channel &&");
        controllerSource.Should().Contain("x.RecipientAddress == recipientAddress)");
        controllerSource.Should().Contain(".OrderByDescending(x => x.AttemptedAtUtc)");
        controllerSource.Should().Contain(".Select(x => (DateTime?)x.AttemptedAtUtc)");
        controllerSource.Should().Contain(".FirstOrDefaultAsync(ct)");
        controllerSource.Should().Contain("if (!latestAttemptAtUtc.HasValue)");
        controllerSource.Should().Contain("var cooldownUntilUtc = latestAttemptAtUtc.Value.AddMinutes(5);");
        controllerSource.Should().Contain("return cooldownUntilUtc > nowUtc ? cooldownUntilUtc : null;");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepTestEmailReadinessHelperWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private async Task<bool> CanSendTestEmailAsync(CancellationToken ct)");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("return IsEmailTransportConfigured(settings) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.CommunicationTestInboxEmail);");
        controllerSource.Should().Contain("CanSendTestEmail = await CanSendTestEmailAsync(ct).ConfigureAwait(false),");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepTemplatePlaceholderHelpersWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private static Dictionary<string, string?> BuildCommunicationTestPlaceholders(");
        controllerSource.Should().Contain("[\"channel\"] = channel,");
        controllerSource.Should().Contain("[\"requested_by\"] = requestedBy,");
        controllerSource.Should().Contain("[\"attempted_at_utc\"] = attemptedAtUtc.ToString(\"yyyy-MM-dd HH:mm:ss\"),");
        controllerSource.Should().Contain("[\"test_target\"] = testTarget,");
        controllerSource.Should().Contain("[\"transport_state\"] = transportState");
        controllerSource.Should().Contain("private string DescribeCommunicationTransportState(bool isReady)");
        controllerSource.Should().Contain("return isReady ? T(\"Ready\") : T(\"CommunicationTransportStateNotReady\");");
        controllerSource.Should().Contain("private static string RenderTemplate(string? template, string fallback, IReadOnlyDictionary<string, string?> placeholders)");
        controllerSource.Should().Contain("var output = string.IsNullOrWhiteSpace(template) ? fallback : template;");
        controllerSource.Should().Contain("foreach (var pair in placeholders)");
        controllerSource.Should().Contain("output = output.Replace(\"{\" + pair.Key + \"}\", pair.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);");
        controllerSource.Should().Contain("return output;");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedTestTransportContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("User?.Identity?.Name ?? T(\"CommunicationChannelFamilyOperatorPlaceholder\")");
        controllerSource.Should().Contain("private string DescribeCommunicationChannel(string? channel)");
        controllerSource.Should().Contain("\"Email\" => T(\"CommunicationBuiltInFlowEmailChannel\")");
        controllerSource.Should().Contain("\"WhatsApp\" => T(\"BusinessCommunicationWhatsAppShort\")");
        controllerSource.Should().Contain("_ => T(\"BusinessCommunicationSmsShort\")");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"Email\")");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"SMS\")");
        controllerSource.Should().Contain("channel: DescribeCommunicationChannel(\"WhatsApp\")");
        controllerSource.Should().Contain("BuildCommunicationTestPlaceholders(DescribeCommunicationChannel(\"SMS\"), T(\"CommunicationChannelFamilyOperatorPlaceholder\")");
        controllerSource.Should().Contain("BuildCommunicationTestPlaceholders(DescribeCommunicationChannel(\"WhatsApp\"), T(\"CommunicationChannelFamilyOperatorPlaceholder\")");
        controllerSource.Should().NotContain("channel: T(\"BusinessCommunicationSmsShort\")");
        controllerSource.Should().NotContain("channel: T(\"BusinessCommunicationWhatsAppShort\")");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(emailTransportConfigured)");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(smsTransportConfigured)");
        controllerSource.Should().Contain("transportState: DescribeCommunicationTransportState(whatsAppTransportConfigured)");
        controllerSource.Should().Contain("T(\"CommunicationTemplateInventoryAdminTestSubjectFallback\")");
        controllerSource.Should().Contain("T(\"CommunicationTestEmailBodyRuntimeFallback\")");
        controllerSource.Should().Contain("T(\"CommunicationTemplateInventoryAdminTestSmsBodyFallback\")");
        controllerSource.Should().Contain("T(\"CommunicationTemplateInventoryAdminTestWhatsAppBodyFallback\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestSmsCooldownMessage\"), smsCooldownUntilUtc.Value)");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationTestWhatsAppCooldownMessage\"), whatsAppCooldownUntilUtc.Value)");
        controllerSource.Should().Contain("private string DescribeCommunicationTransportState(bool isReady)");
        controllerSource.Should().Contain("return isReady ? T(\"Ready\") : T(\"CommunicationTransportStateNotReady\")");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepLocalizedRetryFallbackContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("SetSuccessMessage(\"EmailFlowRetriedSuccessfully\")");
        controllerSource.Should().Contain("SetErrorMessage(\"CommunicationEmailRetryFailedFallback\")");
    }


    [Fact]
    public void ChannelAuditsView_Should_KeepLocalizedInlineTimelineContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildChannelAuditActionPolicyState(string? state)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ActionPolicyStates.CanonicalFlow => T(\"CommunicationChannelActionPolicyCanonicalFlow\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ActionPolicyStates.RetryReady => T(\"CommunicationChannelActionPolicyRetryReady\")");
        controllerSource.Should().Contain("_ => string.IsNullOrWhiteSpace(state) ? string.Empty : T(state)");
        controllerSource.Should().Contain("private string? BuildChannelAuditActionBlockedReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationChannelActionBlockedCanonicalFlow\")");
        controllerSource.Should().Contain("return T(\"CommunicationChannelActionBlockedCooldown\")");
        controllerSource.Should().Contain("return T(\"CommunicationChannelActionBlockedUnsupported\")");
        controllerSource.Should().Contain("return T(item.ActionBlockedReason);");
        controllerSource.Should().Contain("private string? BuildChannelAuditEscalationReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationChannelEscalationPhoneVerification\")");
        controllerSource.Should().Contain("return T(\"CommunicationChannelEscalationAdminTest\")");
        controllerSource.Should().Contain("return T(item.EscalationReason);");
        controllerSource.Should().Contain("private string BuildChannelProviderRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.Guidance.ProviderRecommendedVerificationElevated => T(\"CommunicationChannelProviderRecommendedVerificationElevated\")");
        controllerSource.Should().Contain("_ => string.IsNullOrWhiteSpace(recommendedAction) ? string.Empty : T(recommendedAction)");
        controllerSource.Should().Contain("private string BuildChannelProviderEscalationHint(string escalationHint)");
        controllerSource.Should().Contain("_ => string.IsNullOrWhiteSpace(escalationHint) ? string.Empty : T(escalationHint)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.Guidance.ProviderEscalationVerificationElevated => T(\"CommunicationChannelProviderEscalationVerificationElevated\")");
        controllerSource.Should().Contain("private string BuildChannelChainRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.Guidance.ChainRecommendedVerificationRecovered => T(\"CommunicationChannelChainRecommendedVerificationRecovered\")");
        controllerSource.Should().Contain("private string BuildChannelChainEscalationHint(string escalationHint)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.Guidance.ChainEscalationVerificationBlocked => T(\"CommunicationChannelChainEscalationVerificationBlocked\")");

        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditFirstAttemptLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditLatestAttemptLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditChainContextLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditProviderLaneLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditTotalAttemptsInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditProfileInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditPriorAttemptsInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditPriorFailuresInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditLastSuccessInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditRecoveryInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditAttempts24hInlineLabel\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditFailures24hInlineLabel\")");
        channelAuditsViewSource.Should().Contain("string ProviderPressureLabel(string? pressureState) => pressureState switch");
        channelAuditsViewSource.Should().Contain("ChannelDispatchAuditVocabulary.PressureStates.Elevated => T.T(\"CommunicationProviderPressureElevated\")");
        channelAuditsViewSource.Should().Contain("ChannelDispatchAuditVocabulary.RecoveryStates.Recovered => T.T(\"CommunicationProviderRecoveryRecovered\")");
        channelAuditsViewSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.Mixed => T.T(\"CommunicationChainStatusMixed\")");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Pending\")");
        channelAuditsViewSource.Should().Contain("@ProviderPressureLabel(Model.ProviderSummary.PressureState)");
        channelAuditsViewSource.Should().Contain("@ProviderRecoveryLabel(Model.ProviderSummary.RecoveryState)");
        channelAuditsViewSource.Should().Contain("@ChainStatusMixLabel(Model.ChainSummary.StatusMix)");
        channelAuditsViewSource.Should().Contain("@ChainStatusMixLabel(item.ChainStatusMix)");
        channelAuditsViewSource.Should().Contain("@ProviderPressureLabel(item.ProviderPressureState)");
        channelAuditsViewSource.Should().Contain("@ProviderRecoveryLabel(item.ProviderRecoveryState)");
        channelAuditsViewSource.Should().Contain("@item.ActionPolicyState");
        channelAuditsViewSource.Should().Contain("@item.ActionBlockedReason");
        channelAuditsViewSource.Should().Contain("@item.EscalationReason");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.EscalationHint");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.EscalationHint");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("T.T(\"BusinessCommunicationSmsShort\")");
        channelAuditsViewSource.Should().Contain("T.T(\"BusinessCommunicationWhatsAppShort\")");
        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel: family.ChannelValue");
    }


    [Fact]
    public void ChannelAuditsView_Should_KeepLocalizedActionPolicyAndGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildChannelAuditActionPolicyState(string? state)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ActionPolicyStates.CanonicalFlow => T(\"CommunicationChannelActionPolicyCanonicalFlow\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ActionPolicyStates.Cooldown => T(\"CommunicationChannelActionPolicyCooldown\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ActionPolicyStates.RetryReady => T(\"CommunicationChannelActionPolicyRetryReady\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ActionPolicyStates.Ready => T(\"CommunicationChannelActionPolicyReady\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ActionPolicyStates.Unsupported => T(\"CommunicationChannelActionPolicyUnsupported\")");
        controllerSource.Should().Contain("private string? BuildChannelAuditActionBlockedReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("private string? BuildChannelAuditEscalationReason(ChannelDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("private string BuildChannelProviderRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("private string BuildChannelProviderEscalationHint(string escalationHint)");
        controllerSource.Should().Contain("private string BuildChannelChainRecommendedAction(string recommendedAction)");
        controllerSource.Should().Contain("private string BuildChannelChainEscalationHint(string escalationHint)");

        channelAuditsViewSource.Should().Contain("@item.ActionPolicyState");
        channelAuditsViewSource.Should().Contain("@item.ActionBlockedReason");
        channelAuditsViewSource.Should().Contain("@item.EscalationReason");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ProviderSummary.EscalationHint");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.RecommendedAction");
        channelAuditsViewSource.Should().Contain("@Model.ChainSummary.EscalationHint");
    }


    [Fact]
    public void ChannelAuditsView_Should_KeepLocalizedProviderReviewChannelContractsWired()
    {
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        channelAuditsViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        channelAuditsViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        channelAuditsViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(Model.Channel)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("<code>@Model.Channel</code>");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
    }


    [Fact]
    public void ChannelAuditsView_Should_KeepCanonicalFamilyBranchContractsWired()
    {
        var viewModelSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        viewModelSource.Should().Contain("public string FamilyKey { get; set; } = string.Empty;");
        viewModelSource.Should().Contain("public string ChannelValue { get; set; } = string.Empty;");

        controllerSource.Should().Contain("FamilyKey = \"PhoneVerification\"");
        controllerSource.Should().Contain("FamilyKey = \"AdminCommunicationTest\"");
        controllerSource.Should().Contain("ChannelValue = \"SMS\"");
        controllerSource.Should().Contain("ChannelValue = \"WhatsApp\"");

        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(family.ChannelValue, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel: family.ChannelValue");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyPhoneVerificationName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.FamilyName, T.T(\"CommunicationChannelFamilyAdminTestName\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(family.Channel, T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
    }


    [Fact]
    public void BusinessCommunicationsViews_Should_KeepLocalizedFlowLabelContractsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        indexViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        indexViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        indexViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        indexViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        indexViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        indexViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        indexViewSource.Should().Contain("<td>@FlowLabel(item.FlowKey)</td>");
        indexViewSource.Should().NotContain("@(string.IsNullOrWhiteSpace(item.FlowKey) ? T.T(\"Unclassified\") : item.FlowKey)");
        indexViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        indexViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        indexViewSource.Should().Contain("string SeverityLabel(string? severity) => severity switch");
        indexViewSource.Should().Contain("\"High\" => T.T(\"High\")");
        indexViewSource.Should().Contain("\"Medium\" => T.T(\"Medium\")");
        indexViewSource.Should().Contain("\"Watch\" => T.T(\"Watch\")");
        indexViewSource.Should().Contain("\"Slow\" => T.T(\"Slow\")");
        indexViewSource.Should().Contain("\"Normal\" => T.T(\"Normal\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(severity) ? T.T(\"CommonUnclassified\") : T.T(severity)");
        indexViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        indexViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        indexViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        indexViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        indexViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        indexViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        indexViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        indexViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        indexViewSource.Should().Contain("ChannelAuditsPhoneVerificationByChannelUrl(family.ChannelValue)");
        indexViewSource.Should().Contain("ChannelAuditsAdminTestByChannelUrl(family.ChannelValue)");
        indexViewSource.Should().Contain("@SeverityLabel(item.Severity)");
        indexViewSource.Should().Contain("T.T(\"BusinessCommunicationSmsShort\")");
        indexViewSource.Should().Contain("T.T(\"BusinessCommunicationWhatsAppShort\")");

        detailsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        detailsViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        detailsViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        detailsViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        detailsViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        detailsViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        detailsViewSource.Should().Contain("string OperationalStatusLabel(string? operationalStatus) => operationalStatus switch");
        detailsViewSource.Should().Contain("\"Approved\" => T.T(\"Approved\")");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(operationalStatus) ? T.T(\"CommonUnclassified\") : T.T(operationalStatus)");
        detailsViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        detailsViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        detailsViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        detailsViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        detailsViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        detailsViewSource.Should().Contain("@OperationalStatusLabel(Model.OperationalStatus)");
        detailsViewSource.Should().Contain("@FlowLabel(item.FlowKey)");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        detailsViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        detailsViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");

        channelAuditsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        channelAuditsViewSource.Should().Contain("ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        channelAuditsViewSource.Should().Contain("ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        channelAuditsViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        channelAuditsViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        channelAuditsViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        channelAuditsViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        channelAuditsViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(pressureState) ? T.T(\"CommonUnclassified\") : T.T(pressureState)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(recoveryState) ? T.T(\"CommonUnclassified\") : T.T(recoveryState)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : T.T(statusMix)");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        channelAuditsViewSource.Should().Contain("@ChannelLabel(Model.Channel)");
        channelAuditsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        channelAuditsViewSource.Should().Contain("channel: family.ChannelValue");
        channelAuditsViewSource.Should().Contain("string.Equals(item.Channel, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(item.Channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"SMS\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("string.Equals(Model.Channel, \"WhatsApp\", StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(item.Channel), T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(item.Channel), T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationSmsShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().NotContain("string.Equals(ChannelLabel(Model.Channel), T.T(\"BusinessCommunicationWhatsAppShort\"), StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        channelAuditsViewSource.Should().Contain("<code>@FlowLabel(Model.FlowKey)</code>");
        channelAuditsViewSource.Should().Contain("@FlowLabel(item.FlowKey)");

        emailAuditsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        emailAuditsViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        emailAuditsViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        emailAuditsViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        emailAuditsViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(retryPolicyState) ? T.T(\"CommonUnclassified\") : T.T(retryPolicyState)");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : T.T(statusMix)");
        emailAuditsViewSource.Should().Contain("<span> @T.T(\"InFlow\") <code>@FlowLabel(Model.FlowKey)</code></span>");
        emailAuditsViewSource.Should().Contain("<td>@FlowLabel(item.FlowKey)</td>");
    }


    [Fact]
    public void BusinessCommunicationsIndexView_Should_KeepLocalizedActionLabelsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        indexViewSource.Should().Contain("@T.T(\"CommunicationInspectProfileAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonOpenSetupAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonEditBusiness\")");
        indexViewSource.Should().NotContain("> Inspect");
        indexViewSource.Should().NotContain("> Open Setup");
        indexViewSource.Should().NotContain("> Edit");
    }


    [Fact]
    public void BusinessCommunicationsIndexView_Should_KeepLocalizedRecentEmailActivityContractsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));

        indexViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        indexViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        indexViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        indexViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        indexViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        indexViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        indexViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        indexViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        indexViewSource.Should().Contain("string SeverityLabel(string? severity) => severity switch");
        indexViewSource.Should().Contain("\"High\" => T.T(\"High\")");
        indexViewSource.Should().Contain("\"Medium\" => T.T(\"Medium\")");
        indexViewSource.Should().Contain("\"Watch\" => T.T(\"Watch\")");
        indexViewSource.Should().Contain("\"Slow\" => T.T(\"Slow\")");
        indexViewSource.Should().Contain("\"Normal\" => T.T(\"Normal\")");
        indexViewSource.Should().Contain("<td>@FlowLabel(item.FlowKey)</td>");
        indexViewSource.Should().NotContain("@(string.IsNullOrWhiteSpace(item.FlowKey) ? T.T(\"Unclassified\") : item.FlowKey)");
        indexViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        indexViewSource.Should().Contain("@SeverityLabel(item.Severity)");
        indexViewSource.Should().Contain("@T.T(\"CommunicationInspectProfileAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonOpenSetupAction\")");
        indexViewSource.Should().Contain("@T.T(\"CommonEditBusiness\")");
    }


    [Fact]
    public void BusinessCommunicationsDetailsView_Should_KeepLocalizedOperationalMatrixContractsWired()
    {
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationChannelTruthSnapshotTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationChannelTruthSnapshotNote\")");
        detailsViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        detailsViewSource.Should().Contain("@item.CurrentState");
        detailsViewSource.Should().Contain("@item.LiveFlows");
        detailsViewSource.Should().Contain("@item.SafeOperatorActions");
        detailsViewSource.Should().Contain("@item.RiskBoundary");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationTemplateInventorySnapshotTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationTemplateInventorySnapshotNote\")");
        detailsViewSource.Should().Contain("@item.FlowName");
        detailsViewSource.Should().Contain("@item.TemplateSurface");
        detailsViewSource.Should().Contain("@item.CurrentSubjectTemplate");
        detailsViewSource.Should().Contain("@item.CurrentBodyTemplate");
        detailsViewSource.Should().Contain("@item.OperatorControl");
        detailsViewSource.Should().Contain("@item.OperatorActionLabel");
        detailsViewSource.Should().Contain("@ChannelLabel(family.Channel)");
        detailsViewSource.Should().Contain("@T.T(\"OpenPolicyAction\")");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationResendPolicySnapshotTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationResendPolicySnapshotNote\")");
        detailsViewSource.Should().Contain("@item.CurrentSafeAction");
        detailsViewSource.Should().Contain("@item.GenericRetryStatus");
        detailsViewSource.Should().Contain("@item.OperatorActionLabel");
        detailsViewSource.Should().Contain("@T.T(\"FailedAuditsAction\")");
        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("string.Equals(family.FamilyKey, \"AdminCommunicationTest\", StringComparison.OrdinalIgnoreCase)");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationSmsShort\")");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationWhatsAppShort\")");
        detailsViewSource.Should().Contain("string ChannelLabel(string? channel) => channel switch");
        detailsViewSource.Should().Contain("\"SMS\" => T.T(\"BusinessCommunicationSmsShort\")");
        detailsViewSource.Should().Contain("\"WhatsApp\" => T.T(\"BusinessCommunicationWhatsAppShort\")");
        detailsViewSource.Should().Contain("@ChannelLabel(item.Channel)");
        detailsViewSource.Should().Contain("@ChannelLabel(\"SMS\")");
        detailsViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\")");
        detailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"BusinessCommunicationSmsShort\")</a>");
        detailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"BusinessCommunicationWhatsAppShort\")</a>");
        detailsViewSource.Should().Contain("asp-route-channel=\"@family.ChannelValue\"");
        detailsViewSource.Should().Contain("channel: family.ChannelValue");
    }


    [Fact]
    public void BusinessCommunicationsDetails_Should_KeepLocalizedBusinessProfileGuidanceContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        controllerSource.Should().Contain("private List<string> BuildActiveFlowNames(BusinessCommunicationProfileDto profile)");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowInvitation\"));");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowActivation\"));");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowPasswordReset\"));");
        controllerSource.Should().Contain("flows.Add(T(\"CommunicationDetailsActiveFlowAdminAlerts\"));");

        controllerSource.Should().Contain("private List<string> BuildReadinessIssues(");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSupportEmail\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSenderIdentity\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingSmtp\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueMissingAdminRouting\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssuePendingApproval\"));");
        controllerSource.Should().Contain("issues.Add(T(\"CommunicationDetailsReadinessIssueInactive\"));");

        controllerSource.Should().Contain("private List<string> BuildRecommendedActions(");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBusinessDefaults\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionOpenSmtp\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionConfigureAdminRouting\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewMembers\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewInvitations\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionReviewLockedMembers\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionCompleteBeforeApproval\"));");
        controllerSource.Should().Contain("actions.Add(T(\"CommunicationDetailsRecommendedActionNoImmediateAction\"));");

        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationReadinessIssuesTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationNoReadinessIssues\")");
        detailsViewSource.Should().Contain("@foreach (var issue in Model.ReadinessIssues)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationCurrentFlowsTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationNoCurrentFlows\")");
        detailsViewSource.Should().Contain("@foreach (var flow in Model.ActiveFlowNames)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationRecommendedNextActionsTitle\")");
        detailsViewSource.Should().Contain("@foreach (var action in Model.RecommendedActions)");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationViewInvitationAuditsAction\")");
        detailsViewSource.Should().Contain("T.T(\"Pending\")");
        detailsViewSource.Should().Contain("if (invitationWorkspaceFilter.HasValue)");
        detailsViewSource.Should().Contain("string MemberWorkspaceActionLabel() => memberWorkspaceFilter switch");
        detailsViewSource.Should().Contain("BusinessMemberSupportFilter.PendingActivation => T.T(\"PendingActivation\")");
        detailsViewSource.Should().Contain("BusinessMemberSupportFilter.Locked => T.T(\"UsersFilterLocked\")");
        detailsViewSource.Should().Contain("string InvitationDebtSummaryLabel() => Model.PendingInvitationCount > 0");
        detailsViewSource.Should().Contain("int InvitationDebtCount() => Model.PendingInvitationCount > 0");
        detailsViewSource.Should().Contain("string InvitationWorkspaceActionLabel() => invitationWorkspaceFilter switch");
        detailsViewSource.Should().Contain("string InvitationAuditActionLabel() => invitationWorkspaceFilter switch");
        detailsViewSource.Should().Contain("string ValueOrMissing(string? value) => string.IsNullOrWhiteSpace(value)");
        detailsViewSource.Should().Contain("string DependencyBadgeStatusLabel(bool configured) => configured");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationOpenMemberSupportAction\")");
        detailsViewSource.Should().Contain("hx-swap=\"outerHTML\">@MemberWorkspaceActionLabel()</a>");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationOpenInvitationsAction\")");
        detailsViewSource.Should().Contain("@InvitationDebtSummaryLabel(): <strong>@InvitationDebtCount()</strong>");
        detailsViewSource.Should().Contain("@InvitationWorkspaceActionLabel()</a>");
        detailsViewSource.Should().Contain("@InvitationAuditActionLabel()");
        detailsViewSource.Should().Contain("<dd class=\"col-sm-8\">@ValueOrMissing(Model.LegalName)</dd>");
        detailsViewSource.Should().Contain("<dd class=\"col-sm-8\">@ValueOrMissing(Model.ContactEmail)</dd>");
        detailsViewSource.Should().Contain("<dd class=\"col-sm-8\">@ValueOrMissing(Model.SupportEmail)</dd>");
        detailsViewSource.Should().Contain("<dd class=\"col-sm-8\">@ValueOrMissing(Model.CommunicationSenderName)</dd>");
        detailsViewSource.Should().Contain("<dd class=\"col-sm-8\">@ValueOrMissing(Model.CommunicationReplyToEmail)</dd>");
        detailsViewSource.Should().Contain("@T.T(\"SMTP\") @DependencyBadgeStatusLabel(Model.EmailTransportConfigured)");
        detailsViewSource.Should().Contain("@ChannelLabel(\"SMS\") @DependencyBadgeStatusLabel(Model.SmsTransportConfigured)");
        detailsViewSource.Should().Contain("@ChannelLabel(\"WhatsApp\") @DependencyBadgeStatusLabel(Model.WhatsAppTransportConfigured)");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationAdminAlertsShort\") @DependencyBadgeStatusLabel(Model.AdminAlertRoutingConfigured)");
        detailsViewSource.Should().Contain("string MemberWorkspaceLabel() => T.T(\"Members\")");
        detailsViewSource.Should().Contain("hx-push-url=\"true\">@MemberWorkspaceLabel()</a>");
        detailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Members\")</a>");
        detailsViewSource.Should().Contain("string InvitationWorkspaceLabel() => T.T(\"Invitations\")");
        detailsViewSource.Should().Contain("hx-push-url=\"true\">@InvitationWorkspaceLabel()</a>");
        detailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Invitations\")</a>");
    }


    [Fact]
    public void BusinessCommunicationsViews_Should_KeepCommonUnclassifiedFallbackContractsAligned()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(severity) ? T.T(\"CommonUnclassified\") : T.T(severity)");
        indexViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        indexViewSource.Should().NotContain("T.T(\"Unclassified\")");

        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(pressureState) ? T.T(\"CommonUnclassified\") : T.T(pressureState)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(recoveryState) ? T.T(\"CommonUnclassified\") : T.T(recoveryState)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : T.T(statusMix)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        channelAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        channelAuditsViewSource.Should().NotContain("T.T(\"Unclassified\")");

        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(retryPolicyState) ? T.T(\"CommonUnclassified\") : T.T(retryPolicyState)");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        emailAuditsViewSource.Should().Contain("string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : T.T(statusMix)");
        emailAuditsViewSource.Should().NotContain("T.T(\"Unclassified\")");

        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(operationalStatus) ? T.T(\"CommonUnclassified\") : T.T(operationalStatus)");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        detailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        detailsViewSource.Should().NotContain("T.T(\"Unclassified\")");
    }


    [Fact]
    public void EmailAuditsView_Should_KeepLocalizedPlaybookIntroContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildEmailAuditChainStatusMix(string? statusMix)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.Mixed => T(\"CommunicationChainStatusMixed\")");
        controllerSource.Should().Contain("private string? BuildEmailAuditRetryBlockedReason(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedUnsupported\")");
        controllerSource.Should().Contain("return T(\"CommunicationEmailRetryBlockedClosed\")");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedRateLimited\"), item.RecentAttemptCount24h)");
        controllerSource.Should().Contain("string.Format(CultureInfo.InvariantCulture, T(\"CommunicationEmailRetryBlockedCooldownUntil\"), item.RetryAvailableAtUtc.Value)");

        emailAuditsViewSource.Should().Contain("@T.T(\"CommunicationEmailAuditsPlaybookIntro\")");
        emailAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditFirstAttemptLabel\")");
        emailAuditsViewSource.Should().Contain("@T.T(\"ChannelAuditLatestAttemptLabel\")");
        emailAuditsViewSource.Should().Contain("string ChainStatusMixLabel(string? statusMix) => statusMix switch");
        emailAuditsViewSource.Should().Contain("\"Mixed success/failure\" => T.T(\"CommunicationChainStatusMixed\")");
        emailAuditsViewSource.Should().Contain("string DeliveryStatusLabel(string? status) => status switch");
        emailAuditsViewSource.Should().Contain("\"Pending\" => T.T(\"Pending\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Sent\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Failed\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(\"Pending\")");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(history.Status)");
        emailAuditsViewSource.Should().Contain("@ChainStatusMixLabel(Model.ChainSummary.StatusMix)");
        emailAuditsViewSource.Should().Contain("@string.Format(T.T(\"EmailAuditsChainProfile\"), ChainStatusMixLabel(item.ChainStatusMix))");
        emailAuditsViewSource.Should().Contain("string RetryPolicyStateLabel(string? retryPolicyState) => retryPolicyState switch");
        emailAuditsViewSource.Should().Contain("\"Unsupported\" => T.T(\"CommunicationEmailRetryPolicyUnsupported\")");
        emailAuditsViewSource.Should().Contain("\"Closed\" => T.T(\"CommunicationEmailRetryPolicyClosed\")");
        emailAuditsViewSource.Should().Contain("@RetryPolicyStateLabel(item.RetryPolicyState)");
        emailAuditsViewSource.Should().Contain("@DeliveryStatusLabel(item.Status)");
        emailAuditsViewSource.Should().Contain("@item.RetryBlockedReason");
    }


    [Fact]
    public void EmailAuditsView_Should_KeepLocalizedChainStatusMixContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));

        controllerSource.Should().Contain("private string BuildEmailAuditChainStatusMix(string? statusMix)");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.Mixed => T(\"CommunicationChainStatusMixed\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.OpenFailure => T(\"CommunicationChainStatusOpenFailure\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.FailureOnly => T(\"CommunicationChainStatusFailureOnly\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.PendingOnly => T(\"CommunicationChainStatusPendingOnly\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.SuccessOnly => T(\"CommunicationChainStatusSuccessOnly\")");
        controllerSource.Should().Contain("ChannelDispatchAuditVocabulary.ChainStatusMixes.SingleAttempt => T(\"CommunicationChainStatusSingleAttempt\")");

        emailAuditsViewSource.Should().Contain("string ChainStatusMixLabel(string? statusMix) => statusMix switch");
        emailAuditsViewSource.Should().Contain("\"Mixed success/failure\" => T.T(\"CommunicationChainStatusMixed\")");
        emailAuditsViewSource.Should().Contain("\"Open failure chain\" => T.T(\"CommunicationChainStatusOpenFailure\")");
        emailAuditsViewSource.Should().Contain("\"Failure-only chain\" => T.T(\"CommunicationChainStatusFailureOnly\")");
        emailAuditsViewSource.Should().Contain("\"Pending-only chain\" => T.T(\"CommunicationChainStatusPendingOnly\")");
        emailAuditsViewSource.Should().Contain("\"Success-only chain\" => T.T(\"CommunicationChainStatusSuccessOnly\")");
        emailAuditsViewSource.Should().Contain("\"Single attempt\" => T.T(\"CommunicationChainStatusSingleAttempt\")");
        emailAuditsViewSource.Should().Contain("@ChainStatusMixLabel(Model.ChainSummary.StatusMix)");
        emailAuditsViewSource.Should().Contain("@string.Format(T.T(\"EmailAuditsChainProfile\"), ChainStatusMixLabel(item.ChainStatusMix))");
        emailAuditsViewSource.Should().NotContain("@Model.ChainSummary.StatusMix");
        emailAuditsViewSource.Should().NotContain("@string.Format(T.T(\"EmailAuditsChainProfile\"), item.ChainStatusMix)");
        emailAuditsViewSource.Should().Contain("@T.T(\"EmailAuditLastSuccessLabel\")");
        emailAuditsViewSource.Should().NotContain("@T.T(\"EmailAuditLastSuccessLabel\").ToLowerInvariant()");
    }


    [Fact]
    public void BusinessesController_Should_KeepSupportQueueAndMerchantReadinessEndpointsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("public sealed class BusinessesController : AdminBaseController");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]");
        source.Should().Contain("public async Task<IActionResult> SupportQueue(");
        source.Should().Contain("public async Task<IActionResult> MerchantReadiness(");
        source.Should().Contain("public async Task<IActionResult> SupportQueueSummaryFragment(");
        source.Should().Contain("public async Task<IActionResult> SupportQueueAttentionFragment(");
        source.Should().Contain("public async Task<IActionResult> SupportQueueFailedEmailsFragment(");
    }


    [Fact]
    public void BusinessesController_Should_KeepSupportQueueWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueue(Guid? businessId = null, CancellationToken ct = default)");
        controllerSource.Should().Contain("var summary = await _getBusinessSupportSummary.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var attentionBusinesses = await BuildSupportQueueAttentionBusinessesAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (failedEmails, _, _) = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("page: 1,");
        controllerSource.Should().Contain("pageSize: 8,");
        controllerSource.Should().Contain("status: \"Failed\",");
        controllerSource.Should().Contain("var vm = new BusinessSupportQueueVm");
        controllerSource.Should().Contain("Summary = new BusinessSupportSummaryVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("AttentionBusinesses = attentionBusinesses,");
        controllerSource.Should().Contain("FailedEmails = failedEmails.Select(x => new BusinessSupportFailedEmailVm");
        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks(businessId)");
        controllerSource.Should().Contain("return RenderSupportQueueWorkspace(vm);");
        controllerSource.Should().Contain("return PartialView(\"SupportQueue\", vm);");
        controllerSource.Should().Contain("return View(\"SupportQueue\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSupportQueueFragmentQueriesWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueSummaryFragment(Guid? businessId = null, CancellationToken ct = default)");
        controllerSource.Should().Contain("var summary = await _getBusinessSupportSummary.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueSummary.cshtml\", new BusinessSupportQueueVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("Summary = MapSupportSummaryVm(summary)");
        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueAttentionFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueAttentionBusinesses.cshtml\", vm);");
        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueFailedEmailsFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("pageSize: 8,");
        controllerSource.Should().Contain("status: \"Failed\",");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueFailedEmails.cshtml\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSupportQueueAttentionFragmentMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueAttentionFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("var (attentionBusinesses, _) = await _getBusinessesPage.HandleAsync(1, 10, null, null, true, readinessFilter: null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var vm = attentionBusinesses.Select(x => new BusinessListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("Name = x.Name,");
        controllerSource.Should().Contain("LegalName = x.LegalName,");
        controllerSource.Should().Contain("Category = x.Category,");
        controllerSource.Should().Contain("IsActive = x.IsActive,");
        controllerSource.Should().Contain("OperationalStatus = x.OperationalStatus,");
        controllerSource.Should().Contain("MemberCount = x.MemberCount,");
        controllerSource.Should().Contain("ActiveOwnerCount = x.ActiveOwnerCount,");
        controllerSource.Should().Contain("LocationCount = x.LocationCount,");
        controllerSource.Should().Contain("PrimaryLocationCount = x.PrimaryLocationCount,");
        controllerSource.Should().Contain("InvitationCount = x.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = x.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = x.HasLegalNameConfigured,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueAttentionBusinesses.cshtml\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSupportQueueFailedEmailsFragmentMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SupportQueueFailedEmailsFragment(CancellationToken ct = default)");
        controllerSource.Should().Contain("_getEmailDispatchAuditsPage");
        controllerSource.Should().Contain("page: 1,");
        controllerSource.Should().Contain("pageSize: 8,");
        controllerSource.Should().Contain("status: \"Failed\",");
        controllerSource.Should().Contain("stalePendingOnly: false,");
        controllerSource.Should().Contain("businessLinkedFailuresOnly: false,");
        controllerSource.Should().Contain("repeatedFailuresOnly: false,");
        controllerSource.Should().Contain("priorSuccessOnly: false,");
        controllerSource.Should().Contain("retryReadyOnly: false,");
        controllerSource.Should().Contain("retryBlockedOnly: false,");
        controllerSource.Should().Contain("var vm = failedEmails.Select(x => new BusinessSupportFailedEmailVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("FlowKey = x.FlowKey ?? string.Empty,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("BusinessName = x.BusinessName,");
        controllerSource.Should().Contain("RecipientEmail = x.RecipientEmail,");
        controllerSource.Should().Contain("Subject = x.Subject,");
        controllerSource.Should().Contain("AttemptedAtUtc = x.AttemptedAtUtc,");
        controllerSource.Should().Contain("FailureMessage = x.FailureMessage,");
        controllerSource.Should().Contain("RecommendedAction = BuildSupportAuditRecommendedAction(x)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SupportQueueFailedEmails.cshtml\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessesIndexWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Index(");
        controllerSource.Should().Contain("var summary = await _getBusinessSupportSummary.HandleAsync(null, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessesPage.HandleAsync(");
        controllerSource.Should().Contain("var vm = new BusinessesListVm");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("OperationalStatus = operationalStatus,");
        controllerSource.Should().Contain("AttentionOnly = attentionOnly,");
        controllerSource.Should().Contain("ReadinessFilter = readinessFilter,");
        controllerSource.Should().Contain("Summary = MapSupportSummaryVm(summary),");
        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks(null),");
        controllerSource.Should().Contain("PageSizeItems = BuildPageSizeItems(pageSize),");
        controllerSource.Should().Contain("OperationalStatusItems = BuildBusinessStatusItems(operationalStatus),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessListItemVm");
        controllerSource.Should().Contain("return RenderBusinessesWorkspace(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> MerchantReadiness(Guid? businessId = null, CancellationToken ct = default)");
        controllerSource.Should().Contain("var summary = await _getBusinessSupportSummary.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var attentionBusinesses = await BuildMerchantReadinessBusinessesAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var items = new List<MerchantReadinessItemVm>();");
        controllerSource.Should().Contain("foreach (var business in attentionBusinesses)");
        controllerSource.Should().Contain("var vm = new MerchantReadinessWorkspaceVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("Summary = MapSupportSummaryVm(summary),");
        controllerSource.Should().Contain("Items = items,");
        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks(businessId)");
        controllerSource.Should().Contain("return RenderMerchantReadinessWorkspace(vm);");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMemberAndInvitationShortcutLabelsHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        summaryViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        summaryViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        summaryViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        summaryViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        summaryViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMemberAndInvitationCardSubtitlesHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        summaryViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMemberAndInvitationSummaryLabelsHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        indexViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        indexViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        indexViewSource.Should().NotContain("<i class=\"fa-solid fa-envelope-open-text\"></i> @T.T(\"PendingInvites\")");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        indexViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        indexViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMemberAndInvitationCardSubtitlesHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        indexViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }


    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepInvitationSignalLabelsHelperBacked()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        attentionFragmentSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        attentionFragmentSource.Should().Contain("@BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        attentionFragmentSource.Should().Contain("@BusinessMerchantReadinessUrl(item.Id)");
        attentionFragmentSource.Should().Contain("asp-route-businessId=\"@item.Id\"");
        attentionFragmentSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepMemberAndInvitationSummaryLabelsHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        merchantReadinessViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepMemberAndInvitationCardSubtitlesHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepMemberAndInvitationShortcutLabelsHelperBacked()
    {
        var supportQueueViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        supportQueueViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        supportQueueViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        supportQueueViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        supportQueueViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        supportQueueViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        supportQueueViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        supportQueueViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"LockedMembers\")</a>");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepLocalizedActionShortcutsHelperBacked()
    {
        var failedEmailsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsViewSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        failedEmailsViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        failedEmailsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        failedEmailsViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        failedEmailsViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        failedEmailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
        failedEmailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        failedEmailsViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepLocalizedFlowLabelContractsWired()
    {
        var failedEmailsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsViewSource.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        failedEmailsViewSource.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        failedEmailsViewSource.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        failedEmailsViewSource.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        failedEmailsViewSource.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        failedEmailsViewSource.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        failedEmailsViewSource.Should().Contain("string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        failedEmailsViewSource.Should().Contain("<div class=\"fw-semibold\">@FlowLabel(item.FlowKey)</div>");
        failedEmailsViewSource.Should().NotContain("T.T(\"Unclassified\")");
        failedEmailsViewSource.Should().NotContain("@(string.IsNullOrWhiteSpace(item.FlowKey) ? T.T(\"Unclassified\") : item.FlowKey)");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepDeepRowActionContractsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-target=\"#merchant-readiness-workspace-shell\"");
        merchantReadinessViewSource.Should().Contain("@BusinessEditUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@BusinessSetupUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@BusinessSubscriptionUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@BusinessSubscriptionInvoicesUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("? BusinessMembersUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain(": BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        merchantReadinessViewSource.Should().Contain("? BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        merchantReadinessViewSource.Should().Contain(": BusinessInvitationsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@BusinessOwnerOverrideAuditsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@BusinessLocationsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@BusinessCommunicationsDetailsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@EmailAuditsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@ChannelAuditsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@PaymentsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@RefundsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@FinancialAccountsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@ExpensesUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("@JournalEntriesUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("string GlobalTaxComplianceUrl() => Url.Action(\"TaxCompliance\", \"Billing\") ?? string.Empty;");
        merchantReadinessViewSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepSummaryAndQueueEntryContractsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessIntro\")");
        merchantReadinessViewSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
        merchantReadinessViewSource.Should().Contain("@BusinessesIndexUrl(attentionOnly: true)");
        merchantReadinessViewSource.Should().Contain("string GlobalPaymentsUrl() => Url.Action(\"Payments\", \"Billing\") ?? string.Empty;");
        merchantReadinessViewSource.Should().Contain("@GlobalTaxComplianceUrl()");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Payments\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"TaxComplianceTitle\")");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        merchantReadinessViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        merchantReadinessViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        merchantReadinessViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        merchantReadinessViewSource.Should().Contain("class=\"btn btn-outline-secondary\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        merchantReadinessViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().Contain("@BusinessesIndexUrl(operationalStatus: \"PendingApproval\")");
        merchantReadinessViewSource.Should().Contain("@BusinessesIndexUrl(operationalStatus: \"Suspended\")");
        merchantReadinessViewSource.Should().Contain("@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Unconfirmed)");
        merchantReadinessViewSource.Should().Contain("@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Locked)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        merchantReadinessViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        merchantReadinessViewSource.Should().Contain("asp-route-filter=\"@(item.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        merchantReadinessViewSource.Should().Contain("asp-route-filter=\"@(item.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        merchantReadinessViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        merchantReadinessViewSource.Should().Contain("@EmailAuditsUrl(status: \"Failed\", flowKey: \"PasswordReset\")");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingOwner");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.ApprovedInactive");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingPrimaryLocation");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingContactEmail");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.MissingLegalName");
        merchantReadinessViewSource.Should().Contain("BusinessReadinessQueueFilter.PendingInvites");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessQueueTitle\")");
        merchantReadinessViewSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        merchantReadinessViewSource.Should().Contain("@GlobalBusinessCommunicationsUrl()");
        merchantReadinessViewSource.Should().Contain("@T.T(\"CommunicationOps\")");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessEmptyState\")");
        merchantReadinessViewSource.Should().Contain("string GlobalBusinessesIndexUrl() => Url.Action(\"Index\", \"Businesses\") ?? string.Empty;");
        merchantReadinessViewSource.Should().Contain("@GlobalBusinessesIndexUrl()");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessesTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessPlaybooksTitle\")");
        merchantReadinessViewSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
        merchantReadinessViewSource.Should().Contain("@GlobalPaymentsUrl()");
        merchantReadinessViewSource.Should().Contain("@playbook.QueueActionUrl");
        merchantReadinessViewSource.Should().Contain("@playbook.FollowUpUrl");
        merchantReadinessViewSource.Should().Contain("@playbook.Title");
        merchantReadinessViewSource.Should().Contain("@playbook.ScopeNote");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPlaybookRemediationContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        controllerSource.Should().Contain("private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks(Guid? businessId)");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookApprovalTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"PendingApproval\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookSetupTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"NeedsAttention\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonSetup\")");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookBillingTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"ApprovedInactive\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"Payments\")");

        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessPlaybooksTitle\")");
        merchantReadinessViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        merchantReadinessViewSource.Should().Contain("playbook.QueueActionUrl");
        merchantReadinessViewSource.Should().Contain("playbook.FollowUpUrl");
        merchantReadinessViewSource.Should().Contain(">@playbook.Title</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.ScopeNote</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        merchantReadinessViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        merchantReadinessViewSource.Should().Contain("@playbook.Title\n                            }");
        merchantReadinessViewSource.Should().Contain("@playbook.ScopeNote\n                            }");
        merchantReadinessViewSource.Should().Contain("<div class=\"mb-2\">@playbook.OperatorAction</div>");
        merchantReadinessViewSource.Should().Contain("@BusinessSupportQueueUrl(Model.BusinessId)");
        merchantReadinessViewSource.Should().Contain("@GlobalPaymentsUrl()");
        merchantReadinessViewSource.Should().Contain("@BusinessesIndexUrl(attentionOnly: true)");
        merchantReadinessViewSource.Should().Contain("hx-target=\"#merchant-readiness-workspace-shell\"");
    }


    [Fact]
    public void HomeBusinessSupportQueueCard_Should_KeepHelperBackedSummaryLabelsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Home", "_BusinessSupportQueueCard.cshtml"));

        source.Should().Contain("var invitationAttentionCount = Model.BusinessSupport.PendingInvitationCount + Model.BusinessSupport.OpenInvitationCount;");
        source.Should().Contain("var memberAttentionCount = Model.BusinessSupport.PendingActivationMemberCount + Model.BusinessSupport.LockedMemberCount;");
        source.Should().Contain("var businessAttentionCount = Model.BusinessSupport.AttentionBusinessCount");
        source.Should().Contain("var totalAttentionCount = businessAttentionCount + invitationAttentionCount + memberAttentionCount;");
        source.Should().Contain("@T.T(\"BusinessesTitle\")");
        source.Should().Contain("@T.T(\"Invitations\")");
        source.Should().Contain("@T.T(\"Members\")");
        source.Should().Contain("@T.T(\"NeedsAttention\"): <strong>@totalAttentionCount</strong>");
        source.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        source.Should().Contain("@T.T(\"BusinessSupportOpenQueueAction\")");
        source.Should().NotContain("string MemberSupportLabel(");
        source.Should().NotContain("string InvitationQueueLabel(");
        source.Should().NotContain("string BusinessOperationalQueueLabel(");
    }


    [Fact]
    public void HomeBusinessSupportQueueCard_Should_KeepActionAndSelectedBusinessRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Home", "_BusinessSupportQueueCard.cshtml"));

        source.Should().Contain("string BusinessSupportQueueFragmentUrl(Guid? businessId) => Url.Action(\"BusinessSupportQueueFragment\", \"Home\", new { businessId }) ?? string.Empty;");
        source.Should().Contain("hx-get=\"@BusinessSupportQueueFragmentUrl(Model.SelectedBusinessId)\"");
        source.Should().Contain("hx-target=\"#business-support-queue-card\"");
        source.Should().Contain("@T.T(\"BusinessSupportOpenQueueAction\")");
        source.Should().Contain("asp-controller=\"Businesses\" asp-action=\"SupportQueue\"");
        source.Should().Contain("asp-controller=\"Businesses\" asp-action=\"MerchantReadiness\"");
        source.Should().NotContain("@if (Model.SelectedBusinessId.HasValue)");
        source.Should().NotContain("asp-route-businessId=\"@Model.SelectedBusinessId\"");
        source.Should().NotContain("asp-route-filter=\"@Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation\"");
    }


    [Fact]
    public void SiteSettingsEditorShell_Should_KeepHelperBackedSupportAndChannelRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "SiteSettings", "_SiteSettingsEditorShell.cshtml"));

        source.Should().Contain("string CommunicationChannelLabel(string channel) => channel switch");
        source.Should().Contain("\"SMS\" => T.T(\"SMS\")");
        source.Should().Contain("\"WhatsApp\" => T.T(\"WhatsApp\")");
        source.Should().Contain("_ => string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        source.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        source.Should().Contain("BusinessMemberSupportFilter.Attention => T.T(\"NeedsAttention\")");
        source.Should().Contain("href=\"#site-settings-whatsapp\">@CommunicationChannelLabel(\"WhatsApp\")</a>");
        source.Should().Contain("href=\"#site-settings-sms\">@CommunicationChannelLabel(\"SMS\")</a>");
        source.Should().Contain("<div class=\"card-header\">@CommunicationChannelLabel(\"WhatsApp\")</div>");
        source.Should().Contain("<field-help title=\"@CommunicationChannelLabel(\"WhatsApp\")\" content=\"@T.T(\"SiteSettingsWhatsAppEnabledHelp\")\" placement=\"right\"></field-help>");
        source.Should().Contain("<div class=\"card-header\">@CommunicationChannelLabel(\"SMS\")</div>");
        source.Should().Contain("<option value=\"Sms\">@CommunicationChannelLabel(\"SMS\")</option>");
        source.Should().Contain("<option value=\"WhatsApp\">@CommunicationChannelLabel(\"WhatsApp\")</option>");
        source.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        source.Should().NotContain("href=\"#site-settings-whatsapp\">@T.T(\"WhatsApp\")</a>");
        source.Should().NotContain("href=\"#site-settings-sms\">@T.T(\"SMS\")</a>");
        source.Should().NotContain("<div class=\"card-header\">@T.T(\"WhatsApp\")</div>");
        source.Should().NotContain("<field-help title=\"@T.T(\"WhatsApp\")\" content=\"@T.T(\"SiteSettingsWhatsAppEnabledHelp\")\" placement=\"right\"></field-help>");
        source.Should().NotContain("<div class=\"card-header\">@T.T(\"SMS\")</div>");
        source.Should().NotContain("<option value=\"Sms\">@T.T(\"SMS\")</option>");
        source.Should().NotContain("<option value=\"WhatsApp\">@T.T(\"WhatsApp\")</option>");
        source.Should().NotContain("hx-push-url=\"true\">@T.T(\"NeedsAttention\")</a>");
    }


    [Fact]
    public void SiteSettingsEditorShell_Should_KeepFormAndOwnershipMatrixContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "SiteSettings", "_SiteSettingsEditorShell.cshtml"));

        source.Should().Contain("<form asp-action=\"Edit\"");
        source.Should().Contain("method=\"post\"");
        source.Should().Contain("class=\"needs-validation\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"SiteSettings\")\"");
        source.Should().Contain("hx-target=\"#site-settings-editor-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"RowVersion\" value=\"@(Model.RowVersion is null ? string.Empty : Convert.ToBase64String(Model.RowVersion))\" />");
        source.Should().Contain("@T.T(\"SettingsCategories\")");
        source.Should().Contain("href=\"#site-settings-basics\">@T.T(\"General\")</a>");
        source.Should().Contain("href=\"#site-settings-localization\">@T.T(\"Localization\")</a>");
        source.Should().Contain("href=\"#site-settings-security\">@T.T(\"Security\")</a>");
        source.Should().Contain("href=\"#site-settings-mobile\">@T.T(\"Mobile\")</a>");
        source.Should().Contain("href=\"#site-settings-business-app\">@T.T(\"BusinessApp\")</a>");
        source.Should().Contain("@T.T(\"SettingsOwnershipMatrix\")");
        source.Should().Contain("@T.T(\"SiteSettingsOwnershipIntro\")");
        source.Should().Contain("@T.T(\"SiteSettingsLocalizationDefaultsTitle\")");
        source.Should().Contain("@T.T(\"SiteSettingsCommunicationPolicyTitle\")");
        source.Should().Contain("@T.T(\"SiteSettingsPaymentsShippingTitle\")");
        source.Should().Contain("@T.T(\"SiteSettingsBrandingDefaultsTitle\")");
        source.Should().Contain("string GlobalBusinessesIndexUrl() => Url.Action(\"Index\", \"Businesses\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalBusinessesIndexUrl()\"");
        source.Should().Contain("string GlobalBusinessSupportQueueUrl() => Url.Action(\"SupportQueue\", \"Businesses\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalBusinessSupportQueueUrl()\"");
        source.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        source.Should().Contain("string GlobalPaymentsUrl() => Url.Action(\"Payments\", \"Billing\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        source.Should().Contain("string GlobalShipmentsQueueUrl() => Url.Action(\"ShipmentsQueue\", \"Orders\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalShipmentsQueueUrl()\"");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowInvitationSignalsHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowInvitationSignalsHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        merchantReadinessViewSource.Should().NotContain("@item.InvitationCount @T.T(\"PendingInvites\")");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepLockedMemberCardContractsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        summaryViewSource.Should().Contain("@T.T(\"OpenFailedPasswordResets\")");
        summaryViewSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepLockedMemberCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        summaryViewSource.Should().Contain("@summary.LockedMemberCount");
        summaryViewSource.Should().Contain("@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Locked)");
        summaryViewSource.Should().Contain("@EmailAuditsUrl(\"Failed\", \"PasswordReset\", Model.BusinessId)");
        summaryViewSource.Should().Contain("@MobileOperationsUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepSuspendedBusinessesCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</div>");
        summaryViewSource.Should().Contain("@summary.SuspendedBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesByOperationalStatusUrl(\"Suspended\")");
        summaryViewSource.Should().Contain("@EmailAuditsUrl(\"Failed\", businessId: Model.BusinessId)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMissingOwnerCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</div>");
        summaryViewSource.Should().Contain("@summary.MissingOwnerBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepApprovedInactiveAndMissingLocationCardsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</div>");
        summaryViewSource.Should().Contain("@summary.ApprovedInactiveBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        summaryViewSource.Should().Contain("@PaymentsUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</div>");
        summaryViewSource.Should().Contain("@summary.MissingPrimaryLocationBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepMissingContactAndLegalNameCardsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</div>");
        summaryViewSource.Should().Contain("@summary.MissingContactEmailBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Edit\")</a>");
        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</div>");
        summaryViewSource.Should().Contain("@summary.MissingLegalNameBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepNeedsAttentionCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        summaryViewSource.Should().Contain("@summary.AttentionBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesAttentionUrl()");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingApprovalCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</div>");
        summaryViewSource.Should().Contain("@summary.PendingApprovalBusinessCount");
        summaryViewSource.Should().Contain("@BusinessesByOperationalStatusUrl(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepLockedMemberCardSubtitleHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepLockedMemberCardSubtitleHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"LockedMembers\")</div>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPendingActivationCardSubtitleHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPendingActivationCardSubtitleHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPendingInvitesCardSubtitleHelperBacked()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        indexViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPendingInvitesCardSubtitleHelperBacked()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        merchantReadinessViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingActivationCardSubtitleHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingActivation\")</div>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingActivationCardActionRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@T.T(\"OpenFailedActivationEmails\")");
        summaryViewSource.Should().Contain("@EmailAuditsUrl(\"Failed\", \"AccountActivation\", Model.BusinessId)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        summaryViewSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingActivationCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</div>");
        summaryViewSource.Should().Contain("@summary.PendingActivationMemberCount");
        summaryViewSource.Should().Contain("@EmailAuditsUrl(\"Failed\", \"AccountActivation\", Model.BusinessId)");
        summaryViewSource.Should().Contain("@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Unconfirmed)");
        summaryViewSource.Should().Contain("@MobileOperationsUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingInvitesCardSubtitleHelperBacked()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"PendingInvites\")</div>");
        summaryViewSource.Should().NotContain("<div class=\"text-muted small\">@T.T(\"Invitations\")</div>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingInvitesCardActionRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        summaryViewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        summaryViewSource.Should().Contain("@EmailAuditsUrl(\"Failed\", \"BusinessInvitation\", Model.BusinessId)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepPendingInvitesCardWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"text-muted small\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</div>");
        summaryViewSource.Should().Contain("@summary.PendingInvitationCount");
        summaryViewSource.Should().Contain("@EmailAuditsUrl(\"Failed\", \"BusinessInvitation\", Model.BusinessId)");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedInvitationEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingInvitesShortcutHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingInvitesActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        supportQueueSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        supportQueueSource.Should().Contain("@EmailAuditsUrl(status: \"Failed\", flowKey: \"BusinessInvitation\")");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingActivationShortcutHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepPendingActivationActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        supportQueueSource.Should().Contain("@T.T(\"OpenFailedActivationEmails\")");
        supportQueueSource.Should().Contain("@EmailAuditsUrl(status: \"Failed\", flowKey: \"AccountActivation\")");
        supportQueueSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepLockedMembersShortcutHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"LockedMembers\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepLockedMembersActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        supportQueueSource.Should().Contain("@T.T(\"OpenFailedPasswordResets\")");
        supportQueueSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepPendingActivationShortcutHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepLockedShortcutHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepOpenInvitationsShortcutHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("hx-push-url=\"true\">@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepTopLevelMemberAndInvitationShortcutsHelperBacked()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        supportQueueSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        supportQueueSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        supportQueueSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingInvites\")</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        supportQueueSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"LockedMembers\")</a>");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepInvitationAndMemberShortcutsHelperBacked()
    {
        var failedEmailsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        failedEmailsSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        failedEmailsSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        failedEmailsSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        failedEmailsSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
        failedEmailsSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepAttentionAndApprovalSummaryRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</div>");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: \"PendingApproval\")\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</div>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowOperationalStatusBadgesWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: \"PendingApproval\")\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        indexViewSource.Should().Contain("else if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: \"Suspended\")\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        indexViewSource.Should().NotContain("else if (!item.IsActive)");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowReadinessIssueBadgesWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        indexViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessLocationsUrl(item.Id)\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessEditUrl(item.Id)\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPendingInvitesSummaryCardActionRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        indexViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(status: \"Failed\", flowKey: \"BusinessInvitation\")\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedInvitationEmails\")</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMemberSummaryCardActionRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        indexViewSource.Should().Contain("hx-get=\"@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Unconfirmed)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        indexViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(status: \"Failed\", flowKey: \"AccountActivation\")\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        indexViewSource.Should().Contain("string GlobalMobileOperationsUrl() => Url.Action(\"Index\", \"MobileOperations\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalMobileOperationsUrl()\"");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        indexViewSource.Should().Contain("hx-get=\"@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Locked)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        indexViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(status: \"Failed\", flowKey: \"PasswordReset\")\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepMissingOwnerAndLocationSummaryCardActionRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</div>");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</div>");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepApprovedInactiveAndMetadataSummaryCardActionRailsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</div>");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        indexViewSource.Should().Contain("string GlobalPaymentsUrl() => Url.Action(\"Payments\", \"Billing\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</div>");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        indexViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</div>");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void HomeCommunicationOpsCard_Should_KeepRuntimeExecutionActionRailWired()
    {
        var cardSource = ReadWebAdminFile(Path.Combine("Views", "Home", "_CommunicationOpsCard.cshtml"));

        cardSource.Should().Contain("var failedFlowCount = Model.CommunicationOps.FailedInvitationCount");
        cardSource.Should().Contain("+ Model.CommunicationOps.FailedActivationCount");
        cardSource.Should().Contain("+ Model.CommunicationOps.FailedPasswordResetCount");
        cardSource.Should().Contain("+ Model.CommunicationOps.FailedAdminTestCount;");
        cardSource.Should().Contain("@T.T(\"FailedEmails\")");
        cardSource.Should().Contain("asp-controller=\"BusinessCommunications\" asp-action=\"Index\"");
        cardSource.Should().Contain("@T.T(\"OpenWorkspaceAction\")");
        cardSource.Should().NotContain("asp-action=\"EmailAudits\"");
        cardSource.Should().NotContain("asp-route-businessId=\"@Model.SelectedBusinessId\"");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepTopOperationalFilterRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.AttentionOnly ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(attentionOnly: true)\"");
        indexViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)\"");
        indexViewSource.Should().Contain("@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.Suspended ? \"btn-danger\" : \"btn-outline-danger\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)\"");
        indexViewSource.Should().Contain("@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepTopReadinessFilterRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites ? \"btn-warning\" : \"btn-outline-warning\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)\"");
        indexViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        indexViewSource.Should().Contain("class=\"btn btn-sm @(Model.ReadinessFilter == Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive ? \"btn-secondary\" : \"btn-outline-secondary\")\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)\"");
        indexViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        indexViewSource.Should().Contain("string GlobalBusinessesIndexUrl() => Url.Action(\"Index\", \"Businesses\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalBusinessesIndexUrl()\"");
        indexViewSource.Should().Contain("@T.T(\"ClearQueueFilters\")");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepSearchAndFilterFormContractWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<form method=\"get\"");
        indexViewSource.Should().Contain("hx-get=\"@GlobalBusinessesIndexUrl()\"");
        indexViewSource.Should().Contain("<input type=\"hidden\" name=\"readinessFilter\" value=\"@Model.ReadinessFilter\" />");
        indexViewSource.Should().Contain("<input type=\"text\" name=\"query\" value=\"@Model.Query\" class=\"form-control\" placeholder=\"@T.T(\"SearchBusinessesPlaceholder\")\" />");
        indexViewSource.Should().Contain("<select name=\"operationalStatus\" asp-items=\"Model.OperationalStatusItems\" class=\"form-select\"></select>");
        indexViewSource.Should().Contain("<select name=\"pageSize\" class=\"form-select\">");
        indexViewSource.Should().Contain("@foreach (var item in Model.PageSizeItems)");
        indexViewSource.Should().Contain("<input type=\"checkbox\" name=\"attentionOnly\" value=\"true\" class=\"form-check-input\" id=\"attentionOnly\" checked=\"@(Model.AttentionOnly ? \"checked\" : null)\" />");
        indexViewSource.Should().Contain("<label class=\"form-check-label\" for=\"attentionOnly\">@T.T(\"NeedsAttentionOnly\")</label>");
        indexViewSource.Should().Contain("<button type=\"submit\" class=\"btn btn-outline-secondary\"><i class=\"fa-solid fa-magnifying-glass\"></i> @T.T(\"Search\")</button>");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Reset\")</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepPagerStateContractWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<pager page=\"Model.Page\"");
        indexViewSource.Should().Contain("page-size=\"Model.PageSize\"");
        indexViewSource.Should().Contain("total=\"Model.Total\"");
        indexViewSource.Should().Contain("asp-controller=\"Businesses\"");
        indexViewSource.Should().Contain("asp-action=\"Index\"");
        indexViewSource.Should().Contain("asp-route-query=\"@Model.Query\"");
        indexViewSource.Should().Contain("asp-route-operationalStatus=\"@Model.OperationalStatus\"");
        indexViewSource.Should().Contain("asp-route-attentionOnly=\"@Model.AttentionOnly\"");
        indexViewSource.Should().Contain("asp-route-readinessFilter=\"@Model.ReadinessFilter\"");
        indexViewSource.Should().Contain("hx-target=\"#businesses-workspace-shell\"");
        indexViewSource.Should().Contain("hx-swap=\"outerHTML\"");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepHeaderActionRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"BusinessesTitle\")");
        indexViewSource.Should().Contain("@inject Darwin.WebAdmin.Infrastructure.PermissionRazorHelper Perms");
        indexViewSource.Should().Contain("var isFullAdmin = await Perms.HasAsync(\"FullAdminAccess\")");
        indexViewSource.Should().Contain("<div id=\"businesses-workspace-shell\">");
        indexViewSource.Should().Contain("<h1 class=\"mb-0\"><i class=\"fa-solid fa-building me-2\"></i>@T.T(\"BusinessesTitle\")</h1>");
        indexViewSource.Should().Contain("<p class=\"text-muted mb-0\">@T.T(\"BusinessesIntro\")</p>");
        indexViewSource.Should().Contain("string GlobalMerchantReadinessUrl() => Url.Action(\"MerchantReadiness\", \"Businesses\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalMerchantReadinessUrl()\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">");
        indexViewSource.Should().Contain("<i class=\"fa-solid fa-store\"></i> @T.T(\"MerchantReadinessTitle\")");
        indexViewSource.Should().Contain("string GlobalBusinessSupportQueueUrl() => Url.Action(\"SupportQueue\", \"Businesses\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalBusinessSupportQueueUrl()\"");
        indexViewSource.Should().Contain("<i class=\"fa-solid fa-life-ring\"></i> @T.T(\"SupportQueue\")");
        indexViewSource.Should().Contain("@if (isFullAdmin)");
        indexViewSource.Should().Contain("hx-get=\"@BusinessCreateUrl()\"");
        indexViewSource.Should().Contain("<i class=\"fa-solid fa-plus\"></i> @T.T(\"CreateBusiness\")");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepEmptyStateContractWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("@if (Model.Items.Count == 0)");
        indexViewSource.Should().Contain("<td colspan=\"8\" class=\"text-center text-muted py-4\">@T.T(\"NoBusinessesFound\")</td>");
        indexViewSource.Should().Contain("<th>@T.T(\"Business\")</th>");
        indexViewSource.Should().Contain("<th>@T.T(\"Category\")</th>");
        indexViewSource.Should().Contain("<th class=\"text-center\">@T.T(\"Status\")</th>");
        indexViewSource.Should().Contain("<th class=\"text-center\">@T.T(\"Owners\")</th>");
        indexViewSource.Should().Contain("<th class=\"text-center\">@MemberWorkspaceLabel()</th>");
        indexViewSource.Should().Contain("<th class=\"text-center\">@T.T(\"Locations\")</th>");
        indexViewSource.Should().Contain("<th>@T.T(\"Attention\")</th>");
        indexViewSource.Should().Contain("<th class=\"text-end admin-w-360\">@T.T(\"Actions\")</th>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowPrimaryActionRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("asp-route-filter=\"@(item.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        indexViewSource.Should().Contain("? BusinessMembersUrl(item.Id)");
        indexViewSource.Should().Contain(": BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention))");
        indexViewSource.Should().Contain("hx-push-url=\"true\">");
        indexViewSource.Should().Contain("string MemberWorkspaceLabel() => T.T(\"Members\")");
        indexViewSource.Should().Contain("@MemberWorkspaceLabel()");
        indexViewSource.Should().Contain("hx-get=\"@BusinessSubscriptionUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"Subscription\")");
        indexViewSource.Should().Contain("hx-get=\"@BusinessSubscriptionInvoicesUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"SubscriptionInvoicesTitle\")");
        indexViewSource.Should().Contain("string InvitationWorkspaceLabel() => T.T(\"Invitations\")");
        indexViewSource.Should().Contain("@InvitationWorkspaceLabel()");
        indexViewSource.Should().Contain("asp-route-filter=\"@(item.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        indexViewSource.Should().Contain("? BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        indexViewSource.Should().Contain(": BusinessInvitationsUrl(item.Id))");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowFullAdminActionRailWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("hx-get=\"@BusinessEditUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"Edit\")");
        indexViewSource.Should().Contain("hx-get=\"@BusinessSetupUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"Setup\")");
        indexViewSource.Should().Contain("hx-get=\"@BusinessCommunicationsDetailsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"CommunicationOps\")");
        indexViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"EmailDeliveryAudits\")");
        indexViewSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"SmsWhatsAppAuditsTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@PaymentsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"Payments\")");
        indexViewSource.Should().Contain("hx-get=\"@RefundsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"Refunds\")");
        indexViewSource.Should().Contain("hx-get=\"@FinancialAccountsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"FinancialAccountsTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@ExpensesUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"ExpensesTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@JournalEntriesUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"JournalEntriesTitle\")");
        indexViewSource.Should().Contain("string GlobalTaxComplianceUrl() => Url.Action(\"TaxCompliance\", \"Billing\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalTaxComplianceUrl()\"");
        indexViewSource.Should().Contain("@T.T(\"TaxComplianceTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@BusinessOwnerOverrideAuditsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"OwnerOverrideAuditTitle\")");
        indexViewSource.Should().Contain("hx-get=\"@BusinessLocationsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@T.T(\"Locations\")");
        indexViewSource.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Businesses\")\"");
        indexViewSource.Should().Contain("data-rowversion=\"@Convert.ToBase64String(item.RowVersion)\"");
        indexViewSource.Should().Contain("@T.T(\"Archive\")");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowIdentityDrillInWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<div class=\"fw-semibold\">");
        indexViewSource.Should().Contain("hx-get=\"@BusinessEditUrl(item.Id)\"");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@item.Name</a>");
        indexViewSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(item.LegalName))");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@item.LegalName</a>");
    }


    [Fact]
    public void BusinessesIndexWorkspace_Should_KeepRowOwnerMemberAndLocationDrillInsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));

        indexViewSource.Should().Contain("<th class=\"text-center\">@MemberWorkspaceLabel()</th>");
        indexViewSource.Should().Contain("@if (item.ActiveOwnerCount > 0)");
        indexViewSource.Should().Contain("class=\"badge text-bg-success text-decoration-none\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.Id)\"");
        indexViewSource.Should().Contain("@item.ActiveOwnerCount</a>");
        indexViewSource.Should().Contain("class=\"badge text-bg-warning text-decoration-none\"");
        indexViewSource.Should().Contain("@T.T(\"MissingText\")</a>");
        indexViewSource.Should().Contain("@item.MemberCount</a>");
        indexViewSource.Should().Contain("@if (item.LocationCount > 0)");
        indexViewSource.Should().Contain("hx-get=\"@BusinessLocationsUrl(item.Id)\"");
        indexViewSource.Should().Contain("@item.LocationCount</a>");
        indexViewSource.Should().NotContain("<th class=\"text-center\">@T.T(\"Members\")</th>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepOperationalAndReadinessSummaryRailsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</div>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: \"PendingApproval\")\"");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</div>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</div>");
        merchantReadinessViewSource.Should().Contain("<div class=\"text-muted small\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</div>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowOperationalStatusBadgesWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: \"PendingApproval\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        merchantReadinessViewSource.Should().Contain("else if (item.OperationalStatus == Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(operationalStatus: \"Suspended\")\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        merchantReadinessViewSource.Should().Contain("else if (!item.IsActive)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(readinessFilter: Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowReadinessIssueBadgesWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowIdentitySetupAndSubscriptionDrillInsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessEditUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@item.Name</a>");
        merchantReadinessViewSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(item.LegalName))");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@item.LegalName</a>");
        merchantReadinessViewSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        merchantReadinessViewSource.Should().Contain("@if (setupMissing)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSetupUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessSetupMissing\")</a>");
        merchantReadinessViewSource.Should().Contain("asp-route-filter=\"@(item.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        merchantReadinessViewSource.Should().Contain("? BusinessMembersUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain(": BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        merchantReadinessViewSource.Should().Contain("string MemberWorkspaceLabel() => T.T(\"Members\")");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessLocationsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        merchantReadinessViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
        merchantReadinessViewSource.Should().Contain("@if (item.HasSubscription)");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSubscriptionUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("string SubscriptionStatusLabel(string? status) => string.IsNullOrWhiteSpace(status) ? \"-\" : T.T(status);");
        merchantReadinessViewSource.Should().Contain("SubscriptionStatusLabel(item.SubscriptionStatus)</a>");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSubscriptionCancelAtPeriodEnd\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSubscriptionInvoicesUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSubscriptionNoActivePlan\")</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepRowActionRailWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessEditUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Edit\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSetupUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"CommonSetup\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSubscriptionUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Subscription\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSubscriptionInvoicesUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"SubscriptionInvoicesTitle\")");
        merchantReadinessViewSource.Should().Contain("asp-route-filter=\"@(item.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        merchantReadinessViewSource.Should().Contain("? BusinessMembersUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain(": BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        merchantReadinessViewSource.Should().Contain("@MemberWorkspaceLabel()");
        merchantReadinessViewSource.Should().Contain("asp-route-filter=\"@(item.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        merchantReadinessViewSource.Should().Contain("? BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        merchantReadinessViewSource.Should().Contain(": BusinessInvitationsUrl(item.Id)");
        merchantReadinessViewSource.Should().Contain("string InvitationWorkspaceLabel() => T.T(\"Invitations\")");
        merchantReadinessViewSource.Should().Contain("@InvitationWorkspaceLabel()");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessOwnerOverrideAuditsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"OwnerOverrideAuditTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessLocationsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Locations\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessCommunicationsDetailsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"CommunicationOps\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"EmailDeliveryAudits\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"SmsWhatsAppAuditsTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@PaymentsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Payments\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@RefundsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"Refunds\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@FinancialAccountsUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"FinancialAccountsTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@ExpensesUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"ExpensesTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@JournalEntriesUrl(item.Id)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"JournalEntriesTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalTaxComplianceUrl()\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"TaxComplianceTitle\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSupportQueueUrl(Model.BusinessId)\"");
        merchantReadinessViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepHeaderActionRailWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<h1 class=\"mb-1\"><i class=\"fa-solid fa-store me-2\"></i>@T.T(\"MerchantReadinessTitle\")</h1>");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessIntro\")");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSupportQueueUrl(Model.BusinessId)\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(attentionOnly: true)\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalTaxComplianceUrl()\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepEmptyStateFallbackRailWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("@if (Model.Items.Count == 0)");
        merchantReadinessViewSource.Should().Contain("@T.T(\"MerchantReadinessEmptyState\")");
        merchantReadinessViewSource.Should().Contain("string GlobalBusinessesIndexUrl() => Url.Action(\"Index\", \"Businesses\") ?? string.Empty;");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalBusinessesIndexUrl()\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessesTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSupportQueueUrl(Model.BusinessId)\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        merchantReadinessViewSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepPlaybookShellRailsWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<span>@T.T(\"MerchantReadinessPlaybooksTitle\")</span>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessSupportQueueUrl(Model.BusinessId)\"");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        merchantReadinessViewSource.Should().Contain("<div class=\"d-flex gap-2 flex-wrap mt-3\">");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@BusinessesIndexUrl(attentionOnly: true)\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
    }


    [Fact]
    public void MerchantReadinessWorkspace_Should_KeepQueueHeaderCtaWired()
    {
        var merchantReadinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));

        merchantReadinessViewSource.Should().Contain("<span>@T.T(\"MerchantReadinessQueueTitle\")</span>");
        merchantReadinessViewSource.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        merchantReadinessViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepHeaderActionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("<h1 class=\"mb-1\"><i class=\"fa-solid fa-life-ring me-2\"></i>@T.T(\"BusinessSupportQueueTitle\")</h1>");
        supportQueueSource.Should().Contain("@T.T(\"BusinessSupportQueueIntro\")");
        supportQueueSource.Should().Contain("hx-get=\"@BusinessMerchantReadinessUrl(Model.BusinessId)\"");
        supportQueueSource.Should().Contain("hx-get=\"@BusinessesAttentionUrl()\"");
        supportQueueSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        supportQueueSource.Should().Contain("string GlobalPaymentsUrl() => Url.Action(\"Payments\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalPaymentsUrl()\"");
        supportQueueSource.Should().Contain("string GlobalTaxComplianceUrl() => Url.Action(\"TaxCompliance\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("string GlobalRefundsUrl() => Url.Action(\"Refunds\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalTaxComplianceUrl()\"");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalRefundsUrl()\"");
        supportQueueSource.Should().Contain("string GlobalFinancialAccountsUrl() => Url.Action(\"FinancialAccounts\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("string GlobalExpensesUrl() => Url.Action(\"Expenses\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("string GlobalJournalEntriesUrl() => Url.Action(\"JournalEntries\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("string GlobalOwnerOverrideAuditsUrl() => Url.Action(\"OwnerOverrideAudits\", \"Businesses\") ?? string.Empty;");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalFinancialAccountsUrl()\"");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalExpensesUrl()\"");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalJournalEntriesUrl()\"");
        supportQueueSource.Should().Contain("hx-get=\"@GlobalOwnerOverrideAuditsUrl()\"");
        supportQueueSource.Should().Contain("hx-get=\"@MobileOperationsUrl(Model.BusinessId)\"");
        supportQueueSource.Should().Contain("hx-get=\"@EmailAuditsUrl(status: \"Failed\")\"");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Communications\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
    }


    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepHeaderRailWired()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("<span>@T.T(\"AttentionBusinesses\")</span>");
        attentionFragmentSource.Should().Contain("string GlobalSupportQueueAttentionFragmentUrl() => Url.Action(\"SupportQueueAttentionFragment\", \"Businesses\") ?? string.Empty;");
        attentionFragmentSource.Should().Contain("hx-get=\"@GlobalSupportQueueAttentionFragmentUrl()\"");
        attentionFragmentSource.Should().Contain("hx-target=\"#support-queue-attention\"");
        attentionFragmentSource.Should().Contain("@T.T(\"Refresh\")");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessesAttentionUrl()\"");
        attentionFragmentSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
    }


    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepEmptyStateAndTableShellWired()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("<div class=\"table-responsive\">");
        attentionFragmentSource.Should().Contain("<table class=\"table table-sm align-middle mb-0\">");
        attentionFragmentSource.Should().Contain("<th>@T.T(\"BusinessLabel\")</th>");
        attentionFragmentSource.Should().Contain("<th>@T.T(\"Status\")</th>");
        attentionFragmentSource.Should().Contain("<th>@T.T(\"Signals\")</th>");
        attentionFragmentSource.Should().Contain("<th class=\"text-end\">@T.T(\"Actions\")</th>");
        attentionFragmentSource.Should().Contain("@if (Model.Count == 0)");
        attentionFragmentSource.Should().Contain("<tr><td colspan=\"4\" class=\"text-center text-muted py-4\">@T.T(\"NoAttentionBusinessesQueued\")</td></tr>");
        attentionFragmentSource.Should().Contain("else");
        attentionFragmentSource.Should().Contain("foreach (var item in Model)");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepHeaderRefreshRailWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div id=\"support-queue-summary\" class=\"position-relative\">");
        summaryViewSource.Should().Contain("<div class=\"d-flex justify-content-end mb-2\">");
        summaryViewSource.Should().Contain("hx-get=\"@SupportQueueSummaryFragmentUrl(Model.BusinessId)\"");
        summaryViewSource.Should().Contain("hx-target=\"#support-queue-summary\"");
        summaryViewSource.Should().Contain("hx-swap=\"outerHTML\"");
        summaryViewSource.Should().Contain("@T.T(\"RefreshSummary\")");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepSummaryGridShellWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("<div class=\"row g-3 mb-4\">");
        summaryViewSource.Should().Contain("@summary.AttentionBusinessCount");
        summaryViewSource.Should().Contain("@summary.PendingApprovalBusinessCount");
        summaryViewSource.Should().Contain("@summary.PendingInvitationCount");
        summaryViewSource.Should().Contain("@summary.PendingActivationMemberCount");
        summaryViewSource.Should().Contain("@summary.ApprovedInactiveBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingPrimaryLocationBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingContactEmailBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingLegalNameBusinessCount");
        summaryViewSource.Should().Contain("@summary.SuspendedBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingOwnerBusinessCount");
        summaryViewSource.Should().Contain("@summary.LockedMemberCount");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        summaryViewSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepAttentionAndApprovalCardRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@summary.AttentionBusinessCount");
        summaryViewSource.Should().Contain("@summary.PendingApprovalBusinessCount");
        summaryViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        summaryViewSource.Should().Contain("@BusinessesAttentionUrl()");
        summaryViewSource.Should().Contain("@BusinessesByOperationalStatusUrl(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        summaryViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueSummaryFragment_Should_KeepReadinessAndGovernanceCardRailsWired()
    {
        var summaryViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueSummary.cshtml"));

        summaryViewSource.Should().Contain("@summary.ApprovedInactiveBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingPrimaryLocationBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingContactEmailBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingLegalNameBusinessCount");
        summaryViewSource.Should().Contain("@summary.SuspendedBusinessCount");
        summaryViewSource.Should().Contain("@summary.MissingOwnerBusinessCount");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        summaryViewSource.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        summaryViewSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        summaryViewSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        summaryViewSource.Should().Contain("@BusinessesByOperationalStatusUrl(\"Suspended\")");
        summaryViewSource.Should().Contain("string GlobalOwnerOverrideAuditsUrl() => Url.Action(\"OwnerOverrideAudits\", \"Businesses\") ?? string.Empty;");
        summaryViewSource.Should().Contain("@GlobalOwnerOverrideAuditsUrl()");
        summaryViewSource.Should().Contain("@PaymentsUrl(Model.BusinessId)");
        summaryViewSource.Should().Contain("@EmailAuditsUrl(\"Failed\", businessId: Model.BusinessId)");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OwnerOverrideAuditTitle\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Edit\")</a>");
        summaryViewSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepFailedEmailDrillInRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@EmailAuditsUrl(status: \"Failed\")");
        supportQueueSource.Should().Contain("@EmailAuditsUrl(status: \"Failed\", flowKey: \"BusinessInvitation\")");
        supportQueueSource.Should().Contain("@EmailAuditsUrl(status: \"Failed\", flowKey: \"AccountActivation\")");
        supportQueueSource.Should().Contain("@EmailAuditsUrl(status: \"Failed\", flowKey: \"PasswordReset\")");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedInvitationEmails\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepGovernanceAndReadinessChipRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        supportQueueSource.Should().Contain("@BusinessesByOperationalStatusUrl(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)");
        supportQueueSource.Should().Contain("@BusinessesByOperationalStatusUrl(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)");
        supportQueueSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        supportQueueSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        supportQueueSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        supportQueueSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        supportQueueSource.Should().Contain("@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepWorkspacePivotRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.BusinessId)");
        supportQueueSource.Should().Contain("@BusinessesAttentionUrl()");
        supportQueueSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        supportQueueSource.Should().Contain("@GlobalBusinessCommunicationsUrl()");
        supportQueueSource.Should().Contain("string GlobalOwnerOverrideAuditsUrl() => Url.Action(\"OwnerOverrideAudits\", \"Businesses\") ?? string.Empty;");
        supportQueueSource.Should().Contain("@GlobalOwnerOverrideAuditsUrl()");
        supportQueueSource.Should().Contain("@MobileOperationsUrl(Model.BusinessId)");
        supportQueueSource.Should().Contain("asp-controller=\"MobileOperations\"");
        supportQueueSource.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Communications\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OwnerOverrideAuditTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepBillingOperationsRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("@GlobalPaymentsUrl()");
        supportQueueSource.Should().Contain("@GlobalTaxComplianceUrl()");
        supportQueueSource.Should().Contain("@GlobalRefundsUrl()");
        supportQueueSource.Should().Contain("string GlobalFinancialAccountsUrl() => Url.Action(\"FinancialAccounts\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("string GlobalExpensesUrl() => Url.Action(\"Expenses\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("string GlobalJournalEntriesUrl() => Url.Action(\"JournalEntries\", \"Billing\") ?? string.Empty;");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Refunds\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FinancialAccountsTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ExpensesTitle\")</a>");
        supportQueueSource.Should().Contain("hx-push-url=\"true\">@T.T(\"JournalEntriesTitle\")</a>");
    }


    [Fact]
    public void SupportQueueAttentionFragment_Should_KeepRowDrillInsAndActionRailWired()
    {
        var attentionFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        attentionFragmentSource.Should().Contain("string GlobalSupportQueueAttentionFragmentUrl() => Url.Action(\"SupportQueueAttentionFragment\", \"Businesses\") ?? string.Empty;");
        attentionFragmentSource.Should().Contain("hx-get=\"@GlobalSupportQueueAttentionFragmentUrl()\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessEditUrl(item.Id)\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@item.Name</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@item.LegalName</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessesByOperationalStatusUrl(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval)\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessesByOperationalStatusUrl(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)\"");
        attentionFragmentSource.Should().Contain("@BusinessOperationalStatusLabel(item.OperationalStatus)</span>");
        attentionFragmentSource.Should().Contain("string BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus status) => status switch");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessesByReadinessUrl(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)\"");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.ApprovedInactive)");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessLocationsUrl(item.Id)\"");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        attentionFragmentSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)\"");
        attentionFragmentSource.Should().Contain("@item.InvitationCount @InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessSetupUrl(item.Id)\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Setup\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationPolicy\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Subscription\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"SubscriptionInvoicesTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"RetryBlocked\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedAdminTests\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"PhoneVerification\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ActionBlocked\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"EscalationCandidates\")</a>");
        attentionFragmentSource.Should().Contain("hx-get=\"@SiteSettingsUrl(\"site-settings-communications-policy\")\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.Id, retryBlockedOnly: true)\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.Id, status: \"Failed\", flowKey: \"AccountActivation\")\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.Id, status: \"Failed\", flowKey: \"AdminCommunicationTest\")\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.Id, phoneVerificationOnly: true)\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.Id, actionBlockedOnly: true)\"");
        attentionFragmentSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.Id, escalationCandidatesOnly: true)\"");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Refunds\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FinancialAccountsTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ExpensesTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"JournalEntriesTitle\")</a>");
        attentionFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OwnerOverrideAuditTitle\")</a>");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepPlaybookShellRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("<div class=\"card mb-3\">");
        supportQueueSource.Should().Contain("<div class=\"card-header\">@T.T(\"BusinessesOperationsPlaybooksTitle\")</div>");
        supportQueueSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        supportQueueSource.Should().Contain("<th>@T.T(\"Playbook\")</th>");
        supportQueueSource.Should().Contain("<th>@T.T(\"TaxComplianceScopeColumn\")</th>");
        supportQueueSource.Should().Contain("<th>@T.T(\"OperatorAction\")</th>");
        supportQueueSource.Should().Contain("<th>@T.T(\"UsersPlaybooksFollowUpColumn\")</th>");
        supportQueueSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        supportQueueSource.Should().Contain("href=\"@playbook.FollowUpUrl\"");
        supportQueueSource.Should().Contain("hx-target=\"#business-support-queue-workspace-shell\"");
        supportQueueSource.Should().Contain(">@playbook.Title</a>");
        supportQueueSource.Should().Contain(">@playbook.ScopeNote</a>");
        supportQueueSource.Should().Contain(">@playbook.OperatorAction</a>");
        supportQueueSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        supportQueueSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        supportQueueSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(playbook.QueueActionUrl))");
        supportQueueSource.Should().Contain("@if (!string.IsNullOrWhiteSpace(playbook.FollowUpUrl))");
    }


    [Fact]
    public void SupportQueueWorkspace_Should_KeepFragmentCompositionRailWired()
    {
        var supportQueueSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        supportQueueSource.Should().Contain("<div id=\"business-support-queue-workspace-shell\">");
        supportQueueSource.Should().Contain("<partial name=\"~/Views/Businesses/_SupportQueueSummary.cshtml\" model=\"Model\" />");
        supportQueueSource.Should().Contain("<div class=\"row g-3\">");
        supportQueueSource.Should().Contain("<div class=\"col-xl-7\">");
        supportQueueSource.Should().Contain("<partial name=\"~/Views/Businesses/_SupportQueueAttentionBusinesses.cshtml\" model=\"Model.AttentionBusinesses\" />");
        supportQueueSource.Should().Contain("<div class=\"col-xl-5\">");
        supportQueueSource.Should().Contain("<partial name=\"~/Views/Businesses/_SupportQueueFailedEmails.cshtml\" model=\"Model.FailedEmails\" />");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepHeaderAndEmptyStateRailsWired()
    {
        var failedEmailsFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsFragmentSource.Should().Contain("<span>@T.T(\"RecentFailedEmailEvents\")</span>");
        failedEmailsFragmentSource.Should().Contain("string GlobalSupportQueueFailedEmailsFragmentUrl() => Url.Action(\"SupportQueueFailedEmailsFragment\", \"Businesses\") ?? string.Empty;");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@GlobalSupportQueueFailedEmailsFragmentUrl()\"");
        failedEmailsFragmentSource.Should().Contain("hx-target=\"#support-queue-failed-emails\"");
        failedEmailsFragmentSource.Should().Contain("@T.T(\"Refresh\")");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(status: \"Failed\")\"");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedEmails\")</a>");
        failedEmailsFragmentSource.Should().Contain("@if (Model.Count == 0)");
        failedEmailsFragmentSource.Should().Contain("@T.T(\"NoFailedEmailEventsQueued\")");
    }


    [Fact]
    public void SupportQueueFailedEmailsFragment_Should_KeepRowDrillInsAndRemediationRailsWired()
    {
        var failedEmailsFragmentSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        failedEmailsFragmentSource.Should().Contain("@FlowLabel(item.FlowKey)");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessEditUrl(item.BusinessId)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessCommunicationsDetailsUrl(item.BusinessId)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@UsersUrl(item.RecipientEmail)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.BusinessId, status: \"Failed\", flowKey: item.FlowKey)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(item.BusinessId, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessSetupUrl(item.BusinessId)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessLocationsUrl(item.BusinessId)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.BusinessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.BusinessId)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.BusinessId, retryBlockedOnly: true)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.BusinessId)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.BusinessId, phoneVerificationOnly: true)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.BusinessId, actionBlockedOnly: true)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(item.BusinessId, escalationCandidatesOnly: true)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessMerchantReadinessUrl(item.BusinessId)\"");
        failedEmailsFragmentSource.Should().Contain("asp-route-businessId=\"@item.BusinessId\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@SiteSettingsUrl(\"site-settings-communications-policy\")\"");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Edit\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Setup\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Locations\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationOps\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"CommunicationPolicy\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"EmailDeliveryAudits\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"RetryBlocked\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedActivationEmails\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FailedAdminTests\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenFailedPasswordResets\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"SmsWhatsAppAuditsTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"PhoneVerification\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ActionBlocked\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"EscalationCandidates\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Payments\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Subscription\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"SubscriptionInvoicesTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"TaxComplianceTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"Refunds\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"FinancialAccountsTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"ExpensesTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"JournalEntriesTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OwnerOverrideAuditTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        failedEmailsFragmentSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        failedEmailsFragmentSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenMembers\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenUsers\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MobileOperationsTitle\")</a>");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.BusinessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation, item.RecipientEmail)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.BusinessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked, item.RecipientEmail)\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.BusinessId, status: \"Failed\", flowKey: \"AccountActivation\")\"");
        failedEmailsFragmentSource.Should().Contain("hx-get=\"@EmailAuditsUrl(item.BusinessId, status: \"Failed\", flowKey: \"AdminCommunicationTest\")\"");
    }


    [Fact]
    public void BusinessCommunicationsController_Should_KeepPhoneVerificationPlaceholderHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));

        controllerSource.Should().Contain("private static Dictionary<string, string?> BuildPhoneVerificationPlaceholders(");
        controllerSource.Should().Contain("string phoneE164,");
        controllerSource.Should().Contain("string token,");
        controllerSource.Should().Contain("DateTime expiresAtUtc)");
        controllerSource.Should().Contain("return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)");
        controllerSource.Should().Contain("[\"phone_e164\"] = phoneE164,");
        controllerSource.Should().Contain("[\"token\"] = token,");
        controllerSource.Should().Contain("[\"expires_at_utc\"] = expiresAtUtc.ToString(\"yyyy-MM-dd HH:mm:ss\")");
        controllerSource.Should().Contain("BuildPhoneVerificationPlaceholders(\"+4915112345678\", \"731904\", previewedAtUtc.AddMinutes(10))");
        controllerSource.Should().Contain("SupportedTokens = \"{phone_e164}, {token}, {expires_at_utc}\"");
    }


    [Fact]
    public void SharedBusinessCommunicationsMobileAndShippingViewModels_Should_KeepOpsSnapshotContractsWired()
    {
        var businessCommunicationOpsVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));
        var mobileOpsVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Mobile", "MobileOpsVms.cs"));
        var shippingVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Shipping", "ShippingVms.cs"));

        businessCommunicationOpsVmsSource.Should().Contain("public sealed class BusinessCommunicationOpsVm");
        businessCommunicationOpsVmsSource.Should().Contain("public BusinessCommunicationOpsTransportVm Transport { get; set; } = new();");
        businessCommunicationOpsVmsSource.Should().Contain("public BusinessCommunicationOpsSummaryPanelVm Summary { get; set; } = new();");
        businessCommunicationOpsVmsSource.Should().Contain("public int BusinessesRequiringEmailSetupCount { get; set; }");
        businessCommunicationOpsVmsSource.Should().Contain("public bool SmsTransportConfigured { get; set; }");
        businessCommunicationOpsVmsSource.Should().Contain("public bool WhatsAppTransportConfigured { get; set; }");
        businessCommunicationOpsVmsSource.Should().Contain("public sealed class ChannelMessageFamilyVm");
        businessCommunicationOpsVmsSource.Should().Contain("public string FamilyKey { get; set; } = string.Empty;");
        businessCommunicationOpsVmsSource.Should().Contain("public string ChannelValue { get; set; } = string.Empty;");
        businessCommunicationOpsVmsSource.Should().Contain("public List<EmailDispatchAuditListItemVm> RecentEmailAudits { get; set; } = new();");
        businessCommunicationOpsVmsSource.Should().Contain("public string? TemplateKey { get; set; }");
        businessCommunicationOpsVmsSource.Should().Contain("public string? CorrelationKey { get; set; }");
        businessCommunicationOpsVmsSource.Should().Contain("public string? IntendedRecipientEmail { get; set; }");
        businessCommunicationOpsVmsSource.Should().Contain("public string? ProviderMessageId { get; set; }");
        businessCommunicationOpsVmsSource.Should().Contain("public string? IntendedRecipientAddress { get; set; }");

        mobileOpsVmsSource.Should().Contain("public sealed class MobileOperationsVm");
        mobileOpsVmsSource.Should().Contain("public bool JwtSingleDeviceOnly { get; set; }");
        mobileOpsVmsSource.Should().Contain("public int MobileQrTokenRefreshSeconds { get; set; }");
        mobileOpsVmsSource.Should().Contain("public string DefaultCulture { get; set; } = string.Empty;");
        mobileOpsVmsSource.Should().Contain("public int PendingApprovalBusinessCount { get; set; }");
        mobileOpsVmsSource.Should().Contain("public int DevicesMissingPushTokenCount { get; set; }");
        mobileOpsVmsSource.Should().Contain("public MobilePlatform? PlatformFilter { get; set; }");
        mobileOpsVmsSource.Should().Contain("public List<SelectListItem> PlatformItems { get; set; } = new();");
        mobileOpsVmsSource.Should().Contain("public List<MobileOpsPlaybookVm> Playbooks { get; set; } = new();");
        mobileOpsVmsSource.Should().Contain("public List<MobileDeviceOpsListItemVm> Devices { get; set; } = new();");
        mobileOpsVmsSource.Should().Contain("public int PageSize { get; set; } = 20;");
        mobileOpsVmsSource.Should().Contain("public sealed class MobileDeviceOpsListItemVm");
        mobileOpsVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");

        shippingVmsSource.Should().Contain("public sealed class ShippingMethodsListVm");
        shippingVmsSource.Should().Contain("public ShippingMethodQueueFilter Filter { get; set; }");
        shippingVmsSource.Should().Contain("public ShippingMethodOpsSummaryVm Summary { get; set; } = new();");
        shippingVmsSource.Should().Contain("public List<ShippingMethodPlaybookVm> Playbooks { get; set; } = new();");
        shippingVmsSource.Should().Contain("public IEnumerable<SelectListItem> FilterItems { get; set; } = Array.Empty<SelectListItem>();");
        shippingVmsSource.Should().Contain("public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();");
        shippingVmsSource.Should().Contain("public sealed class ShippingMethodListItemVm");
        shippingVmsSource.Should().Contain("public bool HasGlobalCoverage { get; set; }");
        shippingVmsSource.Should().Contain("public bool HasMultipleRates { get; set; }");
        shippingVmsSource.Should().Contain("public sealed class ShippingRateEditVm");
        shippingVmsSource.Should().Contain("public long PriceMinor { get; set; }");
        shippingVmsSource.Should().Contain("public sealed class ShippingMethodEditVm");
        shippingVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        shippingVmsSource.Should().Contain("public List<ShippingRateEditVm> Rates { get; set; } = new();");
    }


    [Fact]
    public void BusinessCommunicationsAuditWorkspaces_Should_KeepShellFilterPlaybookAndPagerContractsWired()
    {
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        emailAuditsViewSource.Should().Contain("id=\"business-communication-audits-workspace-shell\"");
        emailAuditsViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EmailDeliveryAudits\");");
        emailAuditsViewSource.Should().Contain("@T.T(\"OperatorPlaybooksByFlow\")");
        emailAuditsViewSource.Should().Contain("@T.T(\"CommunicationEmailAuditsPlaybookIntro\")");
        emailAuditsViewSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        emailAuditsViewSource.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        emailAuditsViewSource.Should().Contain("hx-target=\"#business-communication-audits-workspace-shell\"");
        emailAuditsViewSource.Should().Contain("string EmailAuditsUrl(");
        emailAuditsViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(status: \"Failed\", flowKey: playbook.FlowKey, businessId: Model.BusinessId)\"");
        emailAuditsViewSource.Should().Contain("asp-route-retryReadyOnly=\"true\"");
        emailAuditsViewSource.Should().Contain("asp-route-retryBlockedOnly=\"true\"");
        emailAuditsViewSource.Should().Contain("asp-route-highChainVolumeOnly=\"true\"");
        emailAuditsViewSource.Should().Contain("asp-route-chainFollowUpOnly=\"true\"");
        emailAuditsViewSource.Should().Contain("asp-route-chainResolvedOnly=\"true\"");
        emailAuditsViewSource.Should().Contain("hx-post=\"@Url.Action(\"RetryEmailAudit\", \"BusinessCommunications\")\"");
        emailAuditsViewSource.Should().Contain("string LogicalRecipientEmail(Darwin.WebAdmin.ViewModels.Businesses.EmailDispatchAuditListItemVm item) =>");
        emailAuditsViewSource.Should().Contain("string BusinessDisplayName(Darwin.WebAdmin.ViewModels.Businesses.EmailDispatchAuditListItemVm item) =>");
        emailAuditsViewSource.Should().Contain("BusinessInvitationQueueFilter? InvitationWorkspaceFilter");
        emailAuditsViewSource.Should().Contain("BusinessMemberSupportFilter? MemberWorkspaceFilter");
        emailAuditsViewSource.Should().Contain("BusinessInvitationQueueFilter.Pending");
        emailAuditsViewSource.Should().Contain("BusinessMemberSupportFilter.PendingActivation");
        emailAuditsViewSource.Should().Contain("BusinessMemberSupportFilter.Attention");
        emailAuditsViewSource.Should().Contain("@T.T(\"EffectiveRecipient\")");
        emailAuditsViewSource.Should().Contain("@T.T(\"TemplateKey\")");
        emailAuditsViewSource.Should().Contain("@T.T(\"ProviderMessageId\")");
        emailAuditsViewSource.Should().Contain("hx-swap=\"outerHTML\">@BusinessDisplayName(item)</a>");
        emailAuditsViewSource.Should().Contain("asp-route-recipientEmail=\"@LogicalRecipientEmail(item)\"");
        emailAuditsViewSource.Should().Contain("asp-controller=\"Users\" asp-action=\"Index\" asp-route-q=\"@LogicalRecipientEmail(item)\"");
        emailAuditsViewSource.Should().Contain("asp-controller=\"Businesses\" asp-action=\"Invitations\"");
        emailAuditsViewSource.Should().Contain("string BusinessInvitationsUrl(Guid? businessId, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter? filter = null, string? query = null) => Url.Action(\"Invitations\", \"Businesses\", new { businessId, query, filter }) ?? string.Empty;");
        emailAuditsViewSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(item.BusinessId, invitationWorkspaceFilter, LogicalRecipientEmail(item))\"");
        emailAuditsViewSource.Should().Contain("asp-controller=\"MobileOperations\" asp-action=\"Index\" asp-route-businessId=\"@item.BusinessId\" asp-route-q=\"@LogicalRecipientEmail(item)\"");
        emailAuditsViewSource.Should().Contain("string.Equals(item.FlowKey, \"PhoneVerification\", StringComparison.OrdinalIgnoreCase)");
        emailAuditsViewSource.Should().Contain("asp-action=\"ChannelAudits\"");
        emailAuditsViewSource.Should().Contain("asp-route-phoneVerificationOnly=\"true\"");
        emailAuditsViewSource.Should().Contain("string BusinessMembersUrl(Guid? businessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter? filter = null, string? query = null) => Url.Action(\"Members\", \"Businesses\", new { businessId, query, filter }) ?? string.Empty;");
        emailAuditsViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.BusinessId, memberWorkspaceFilter, LogicalRecipientEmail(item))\"");
        emailAuditsViewSource.Should().Contain("asp-controller=\"SiteSettings\" asp-action=\"Edit\" asp-fragment=\"site-settings-communications-policy\"");
        emailAuditsViewSource.Should().Contain("<pager page=\"Model.Page\"");
        emailAuditsViewSource.Should().Contain("asp-action=\"EmailAudits\"");

        channelAuditsViewSource.Should().Contain("id=\"business-communication-channel-audits-shell\"");
        channelAuditsViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"SmsWhatsAppAuditsTitle\");");
        channelAuditsViewSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        channelAuditsViewSource.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        channelAuditsViewSource.Should().Contain("hx-target=\"#business-communication-channel-audits-shell\"");
        channelAuditsViewSource.Should().Contain("asp-route-failedOnly=\"true\"");
        channelAuditsViewSource.Should().Contain("asp-route-phoneVerificationOnly=\"true\"");
        channelAuditsViewSource.Should().Contain("asp-route-adminTestOnly=\"true\"");
        channelAuditsViewSource.Should().Contain("asp-route-providerReviewOnly=\"true\"");
        channelAuditsViewSource.Should().Contain("asp-route-escalationCandidatesOnly=\"true\"");
        channelAuditsViewSource.Should().Contain("asp-route-heavyChainsOnly=\"true\"");
        channelAuditsViewSource.Should().Contain("@T.T(\"ProviderReviewMode\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"LeaveProviderReview\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"PressureState\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"RecoveryState\")");
        channelAuditsViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestSms\", \"BusinessCommunications\")\"");
        channelAuditsViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestWhatsApp\", \"BusinessCommunications\")\"");
        channelAuditsViewSource.Should().Contain("string LogicalRecipientAddress(Darwin.WebAdmin.ViewModels.Businesses.ChannelDispatchAuditListItemVm item) =>");
        channelAuditsViewSource.Should().Contain("@T.T(\"EffectiveRecipient\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"TemplateKey\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"ProviderMessageId\")");
        channelAuditsViewSource.Should().Contain("@T.T(\"CommunicationQueuedDispatchesLabel\")");
        channelAuditsViewSource.Should().Contain("item.IsQueueOperation");
        channelAuditsViewSource.Should().Contain("@T.T(\"CommunicationQueuedDispatchOperationLabel\")");
        channelAuditsViewSource.Should().Contain("@string.Format(T.T(\"CommunicationQueuedDispatchAttempts\"), item.QueueAttemptCount)");
        channelAuditsViewSource.Should().Contain("string.Equals(item.FlowKey, ChannelDispatchAuditVocabulary.FlowKeys.PhoneVerification, StringComparison.OrdinalIgnoreCase)");
        channelAuditsViewSource.Should().Contain("asp-route-phoneVerificationOnly=\"true\"");
        channelAuditsViewSource.Should().Contain("asp-route-recipientAddress=\"@LogicalRecipientAddress(item)\"");
        channelAuditsViewSource.Should().Contain("asp-controller=\"MobileOperations\" asp-action=\"Index\"");
        channelAuditsViewSource.Should().Contain("string BusinessMembersUrl(Guid? businessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter? filter) => Url.Action(\"Members\", \"Businesses\", new { businessId, filter }) ?? string.Empty;");
        channelAuditsViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.BusinessId, memberSupportFilter)\"");
        channelAuditsViewSource.Should().Contain("string UsersQueueUrl(Darwin.Application.Identity.DTOs.UserQueueFilter filter, string? query = null) => Url.Action(\"Index\", \"Users\", new { filter, query }) ?? string.Empty;");
        channelAuditsViewSource.Should().Contain("hx-get=\"@UsersQueueUrl(userQueueFilter)\"");
        channelAuditsViewSource.Should().Contain("asp-controller=\"Users\" asp-action=\"Index\" asp-route-filter=\"@Darwin.Application.Identity.DTOs.UserQueueFilter.MobileLinked\"");
        channelAuditsViewSource.Should().Contain("asp-controller=\"SiteSettings\" asp-action=\"Edit\" asp-fragment=\"site-settings-communications-policy\"");
        channelAuditsViewSource.Should().Contain("asp-route-provider=\"@item.Provider\"");
        channelAuditsViewSource.Should().Contain("asp-route-recipientAddress=\"@LogicalRecipientAddress(item)\"");
        channelAuditsViewSource.Should().Contain("<pager page=\"Model.Page\"");
        channelAuditsViewSource.Should().Contain("asp-action=\"ChannelAudits\"");
    }


    [Fact]
    public void BusinessCommunicationsPrimaryWorkspaces_Should_KeepShellActionRailAndPagerContractsWired()
    {
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        indexViewSource.Should().Contain("id=\"business-communications-workspace-shell\"");
        indexViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CommunicationOpsTitle\");");
        indexViewSource.Should().Contain("string GlobalChannelAuditsUrl() => Url.Action(\"ChannelAudits\", \"BusinessCommunications\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalChannelAuditsUrl()\"");
        indexViewSource.Should().Contain("string GlobalEmailAuditsUrl() => Url.Action(\"EmailAudits\", \"BusinessCommunications\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalEmailAuditsUrl()\"");
        indexViewSource.Should().Contain("string SiteSettingsUrl(string fragment) => Url.Action(\"Edit\", \"SiteSettings\", new { fragment }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@SiteSettingsUrl(\"site-settings-smtp\")\"");
        indexViewSource.Should().Contain("hx-get=\"@SiteSettingsUrl(\"site-settings-sms\")\"");
        indexViewSource.Should().Contain("hx-get=\"@SiteSettingsUrl(\"site-settings-whatsapp\")\"");
        indexViewSource.Should().Contain("hx-get=\"@SiteSettingsUrl(\"site-settings-admin-routing\")\"");
        indexViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestEmail\", \"BusinessCommunications\")\"");
        indexViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestSms\", \"BusinessCommunications\")\"");
        indexViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestWhatsApp\", \"BusinessCommunications\")\"");
        indexViewSource.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        indexViewSource.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        indexViewSource.Should().Contain("name=\"pageSize\" class=\"form-select\"");
        indexViewSource.Should().Contain("name=\"setupOnly\" value=\"true\"");
        indexViewSource.Should().Contain("string ValueOrLabel(string? value, string missingLabel) => string.IsNullOrWhiteSpace(value)");
        indexViewSource.Should().Contain("string SupportEmailBadgeClass(string? supportEmail) => string.IsNullOrWhiteSpace(supportEmail)");
        indexViewSource.Should().Contain("string SupportEmailLabel(string? supportEmail) => string.IsNullOrWhiteSpace(supportEmail)");
        indexViewSource.Should().Contain("var communicationRuntimeExecutionAttentionCount = Model.RecentEmailAudits.Count(x =>");
        indexViewSource.Should().Contain("var failedInvitationAttentionCount = Model.RecentEmailAudits.Count(x =>");
        indexViewSource.Should().Contain("string.Equals(x.FlowKey, \"BusinessInvitation\", StringComparison.OrdinalIgnoreCase)");
        indexViewSource.Should().Contain("string DependencyStatusLabel(bool configured, string configuredLabel, string missingLabel) => configured");
        indexViewSource.Should().Contain("@T.T(\"Communications\")");
        indexViewSource.Should().Contain("@T.T(\"CommunicationTemplateInventoryTitle\")");
        indexViewSource.Should().Contain("@T.T(\"RuntimeExecution\")");
        indexViewSource.Should().Contain("@T.T(\"UsersFilterUnconfirmed\")");
        indexViewSource.Should().Contain("@T.T(\"UsersFilterLocked\")");
        indexViewSource.Should().Contain("@DependencyStatusLabel(Model.Transport.EmailTransportConfigured, \"Ready\", \"NeedsSetup\")");
        indexViewSource.Should().Contain("@DependencyStatusLabel(Model.Transport.SmsTransportConfigured, \"Ready\", \"NotReady\")");
        indexViewSource.Should().Contain("@DependencyStatusLabel(Model.Transport.WhatsAppTransportConfigured, \"Ready\", \"NotReady\")");
        indexViewSource.Should().Contain("@DependencyStatusLabel(Model.Transport.AdminAlertRoutingConfigured, \"Configured\", \"Missing\")");
        indexViewSource.Should().Contain("<span class=\"@SupportEmailBadgeClass(item.SupportEmail)\">@SupportEmailLabel(item.SupportEmail)</span>");
        indexViewSource.Should().Contain("<div>@ValueOrLabel(item.CommunicationSenderName, \"BusinessCommunicationMissingSenderName\")</div>");
        indexViewSource.Should().Contain("<div class=\"small text-muted\">@ValueOrLabel(item.CommunicationReplyToEmail, \"BusinessCommunicationMissingReplyTo\")</div>");
        indexViewSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        indexViewSource.Should().Contain("asp-action=\"Details\" asp-route-businessId=\"@item.Id\"");
        indexViewSource.Should().Contain("asp-controller=\"Businesses\" asp-action=\"Setup\" asp-route-id=\"@item.Id\"");
        indexViewSource.Should().Contain("asp-controller=\"Businesses\" asp-action=\"Edit\" asp-route-id=\"@item.Id\"");
        indexViewSource.Should().Contain("string BusinessEmailAuditsUrl(Guid businessId) => Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@BusinessEmailAuditsUrl(item.Id)\"");
        indexViewSource.Should().Contain("string BusinessFailedInvitationAuditsUrl(Guid businessId) => Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId, status = \"Failed\", flowKey = \"BusinessInvitation\" }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@BusinessFailedInvitationAuditsUrl(item.Id)\"");
        indexViewSource.Should().Contain("string BusinessChannelAuditsUrl(Guid businessId) => Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@BusinessChannelAuditsUrl(item.Id)\"");
        indexViewSource.Should().Contain("string BusinessPendingInvitationsUrl(Guid businessId) => Url.Action(\"Invitations\", \"Businesses\", new { businessId, filter = Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@BusinessPendingInvitationsUrl(item.Id)\"");
        indexViewSource.Should().Contain("string BusinessMembersUrl(Guid businessId, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => Url.Action(\"Members\", \"Businesses\", new { businessId, filter }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)\"");
        indexViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)\"");
        indexViewSource.Should().Contain("string EmailAuditsFlowUrl(string flowKey, string status) => Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { flowKey, status }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@EmailAuditsFlowUrl(\"AccountActivation\", \"Failed\")\"");
        indexViewSource.Should().Contain("hx-get=\"@EmailAuditsFlowUrl(\"BusinessInvitation\", \"Failed\")\"");
        indexViewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        indexViewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\"): @failedInvitationAttentionCount");
        indexViewSource.Should().Contain("hx-get=\"@EmailAuditsFlowUrl(\"AdminCommunicationTest\", \"Failed\")\"");
        indexViewSource.Should().Contain("string PendingInvitationsUrl() => Url.Action(\"Invitations\", \"Businesses\", new { filter = Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@PendingInvitationsUrl()\"");
        indexViewSource.Should().Contain("string UsersQueueUrl(Darwin.Application.Identity.DTOs.UserQueueFilter filter) => Url.Action(\"Index\", \"Users\", new { filter }) ?? string.Empty;");
        indexViewSource.Should().Contain("hx-get=\"@UsersQueueUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Unconfirmed)\"");
        indexViewSource.Should().Contain("hx-get=\"@UsersQueueUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Locked)\"");
        indexViewSource.Should().Contain("<pager page=\"Model.Page\"");
        indexViewSource.Should().Contain("asp-action=\"Index\"");

        detailsViewSource.Should().Contain("id=\"business-communication-profile-shell\"");
        detailsViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"BusinessCommunicationProfileTitle\");");
        detailsViewSource.Should().Contain("string GlobalBusinessCommunicationsUrl() => Url.Action(\"Index\", \"BusinessCommunications\") ?? string.Empty;");
        detailsViewSource.Should().Contain("hx-get=\"@GlobalBusinessCommunicationsUrl()\"");
        detailsViewSource.Should().Contain("string ChannelAuditsUrl(");
        detailsViewSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(Model.Id)\"");
        detailsViewSource.Should().Contain("string EmailAuditsUrl(");
        detailsViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(Model.Id)\"");
        detailsViewSource.Should().Contain("string BusinessSetupUrl(Guid id) => Url.Action(\"Setup\", \"Businesses\", new { id }) ?? string.Empty;");
        detailsViewSource.Should().Contain("hx-get=\"@BusinessSetupUrl(Model.Id)\"");
        detailsViewSource.Should().Contain("var memberWorkspaceFilter = Model.PendingActivationMemberCount > 0");
        detailsViewSource.Should().Contain("? Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation");
        detailsViewSource.Should().Contain("BusinessMembersUrl(Model.Id, memberWorkspaceFilter)");
        detailsViewSource.Should().Contain("var invitationWorkspaceFilter = Model.PendingInvitationCount > 0");
        detailsViewSource.Should().Contain("? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending");
        detailsViewSource.Should().Contain("? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open");
        detailsViewSource.Should().Contain("BusinessInvitationsUrl(Model.Id, invitationWorkspaceFilter)");
        detailsViewSource.Should().Contain("BusinessInvitationsUrl(Model.Id, invitationWorkspaceFilter, item.RecipientEmail)");
        detailsViewSource.Should().Contain("BusinessMembersUrl(Model.Id, memberWorkspaceFilter, item.RecipientEmail)");
        detailsViewSource.Should().Contain("T.T(\"BusinessSupportPendingInvitationsLabel\")");
        detailsViewSource.Should().Contain("if (invitationWorkspaceFilter.HasValue)");
        detailsViewSource.Should().Contain("T.T(\"BusinessCommunicationViewInvitationAuditsAction\")");
        detailsViewSource.Should().Contain("T.T(\"Pending\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationProfileBusinessProfileTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationProfileSenderDefaultsTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationProfileSupportSignalsTitle\")");
        detailsViewSource.Should().Contain("var communicationRuntimeExecutionAttentionCount = Model.RecentEmailAudits.Count(x =>");
        detailsViewSource.Should().Contain("var failedInvitationAttentionCount = Model.RecentEmailAudits.Count(x =>");
        detailsViewSource.Should().Contain("@T.T(\"Communications\")");
        detailsViewSource.Should().Contain("@T.T(\"CommunicationTemplateInventoryTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"RuntimeExecution\")");
        detailsViewSource.Should().Contain("@T.T(\"UsersFilterUnconfirmed\")");
        detailsViewSource.Should().Contain("@T.T(\"UsersFilterLocked\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationPolicyFlagsTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationGlobalDependencyReadinessTitle\")");
        detailsViewSource.Should().Contain("string SiteSettingsUrl(string? fragment) => Url.Action(\"Edit\", \"SiteSettings\", new { fragment }) ?? string.Empty;");
        detailsViewSource.Should().Contain("hx-get=\"@SiteSettingsUrl(\"site-settings-communications-policy\")\"");
        detailsViewSource.Should().Contain("string MobileOperationsUrl(Guid businessId, string? q = null) => Url.Action(\"Index\", \"MobileOperations\", new { businessId, q }) ?? string.Empty;");
        detailsViewSource.Should().Contain("hx-get=\"@MobileOperationsUrl(Model.Id)\"");
        detailsViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestEmail\", \"BusinessCommunications\")\"");
        detailsViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestSms\", \"BusinessCommunications\")\"");
        detailsViewSource.Should().Contain("hx-post=\"@Url.Action(\"SendTestWhatsApp\", \"BusinessCommunications\")\"");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationChannelTruthSnapshotTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationTransactionalPolicyTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationPhaseOneImplicationsTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationReadinessIssuesTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationCurrentFlowsTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationRecommendedNextActionsTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"BusinessCommunicationRecentBusinessEmailActivityTitle\")");
        detailsViewSource.Should().Contain("@T.T(\"SmsWhatsAppActivitySnapshotTitle\")");
        detailsViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(Model.Id, status: \"Failed\", flowKey: item.AuditFlowKey)\"");
        detailsViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(Model.Id, status: \"Failed\", flowKey: \"BusinessInvitation\")\"");
        detailsViewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        detailsViewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\"): @failedInvitationAttentionCount");
        detailsViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(Model.Id, status: \"Failed\", flowKey: \"AccountActivation\")\"");
        detailsViewSource.Should().Contain("hx-get=\"@EmailAuditsUrl(Model.Id, status: \"Failed\", flowKey: \"AdminCommunicationTest\")\"");
        detailsViewSource.Should().Contain("hx-get=\"@BusinessInvitationsUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)\"");
        detailsViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)\"");
        detailsViewSource.Should().Contain("hx-get=\"@BusinessMembersUrl(Model.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)\"");
        detailsViewSource.Should().NotContain("hx-get=\"@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Unconfirmed)\"");
        detailsViewSource.Should().NotContain("hx-get=\"@UsersUrl(Darwin.Application.Identity.DTOs.UserQueueFilter.Locked)\"");
        detailsViewSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(Model.Id, phoneVerificationOnly: true, channel: family.ChannelValue)\"");
        detailsViewSource.Should().Contain("hx-get=\"@ChannelAuditsUrl(Model.Id, adminTestOnly: true, channel: family.ChannelValue)\"");
    }

    [Fact]
    public void BusinessCommunicationsAuditAndPrimaryViews_Should_KeepCultureAwareDateTimeFormattingWired()
    {
        var emailAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));
        var channelAuditsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsViewSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));

        emailAuditsViewSource.Should().Contain("@using System.Globalization");
        emailAuditsViewSource.Should().Contain("@history.AttemptedAtUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        emailAuditsViewSource.Should().Contain("history.CompletedAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        emailAuditsViewSource.Should().Contain("@item.AttemptedAtUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        emailAuditsViewSource.Should().Contain("item.LastSuccessfulAttemptAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        emailAuditsViewSource.Should().Contain("item.RetryAvailableAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");

        channelAuditsViewSource.Should().Contain("@using System.Globalization");
        channelAuditsViewSource.Should().Contain("Model.ProviderSummary.LastSuccessfulAttemptAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        channelAuditsViewSource.Should().Contain("Model.ChainSummary.FirstAttemptAtUtc?.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        channelAuditsViewSource.Should().Contain("@item.AttemptedAtUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        channelAuditsViewSource.Should().Contain("item.ProviderLastSuccessfulAttemptAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        channelAuditsViewSource.Should().Contain("item.ActionAvailableAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");

        indexViewSource.Should().Contain("@using System.Globalization");
        indexViewSource.Should().Contain("@item.AttemptedAtUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        indexViewSource.Should().Contain("item.LastSuccessfulAttemptAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        indexViewSource.Should().Contain("item.CompletedAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");

        detailsViewSource.Should().Contain("@using System.Globalization");
        detailsViewSource.Should().Contain("@item.AttemptedAtUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
        detailsViewSource.Should().Contain("item.LastSuccessfulAttemptAtUtc.Value.ToLocalTime().ToString(CultureInfo.CurrentCulture)");
    }

    [Fact]
    public void BusinessCommunicationsChannelAuditViewModels_Should_KeepQueuedWorkerVisibilityContractsWired()
    {
        var viewModelSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessCommunicationOpsVms.cs"));

        viewModelSource.Should().Contain("public int QueuedPendingCount { get; set; }");
        viewModelSource.Should().Contain("public int QueuedFailedCount { get; set; }");
        viewModelSource.Should().Contain("public bool IsQueueOperation { get; set; }");
        viewModelSource.Should().Contain("public int QueueAttemptCount { get; set; }");
    }
}

