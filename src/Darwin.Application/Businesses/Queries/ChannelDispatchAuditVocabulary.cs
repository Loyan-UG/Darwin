namespace Darwin.Application.Businesses.Queries
{
    public static class ChannelDispatchAuditVocabulary
    {
        public static class FlowKeys
        {
            public const string PhoneVerification = "PhoneVerification";
            public const string AdminCommunicationTest = "AdminCommunicationTest";
        }

        public static class PressureStates
        {
            public const string Elevated = "Elevated";
            public const string Recovering = "Recovering";
            public const string Stable = "Stable";
        }

        public static class RecoveryStates
        {
            public const string Recovered = "Recovered";
            public const string StableSuccess = "Stable success";
            public const string NoneYet = "No recovery yet";
        }

        public static class ChainStatusMixes
        {
            public const string Mixed = "Mixed success/failure";
            public const string OpenFailure = "Open failure chain";
            public const string FailureOnly = "Failure-only chain";
            public const string PendingOnly = "Pending-only chain";
            public const string SuccessOnly = "Success-only chain";
            public const string SingleAttempt = "Single attempt";
        }

        public static class ActionPolicyStates
        {
            public const string CanonicalFlow = "Canonical flow";
            public const string Cooldown = "Cooldown";
            public const string RetryReady = "Retry ready";
            public const string Ready = "Ready";
            public const string Unsupported = "Unsupported";
        }

        public static class Guidance
        {
            public const string ProviderRecommendedVerificationElevated = "Review SMS/WhatsApp readiness, fallback policy, and current verification channel choice before issuing another canonical verification code.";
            public const string ProviderRecommendedVerificationStable = "Keep verification traffic on the canonical flow, then review recent provider failures before switching channel policy or escalating.";
            public const string ProviderEscalationVerificationElevated = "Escalate as provider or channel-policy instability if verification traffic continues to fail without any successful recovery in this provider lane.";
            public const string ProviderEscalationVerificationStable = "Escalate only if the provider lane keeps degrading after readiness, fallback, and current phone verification path have been checked.";
            public const string ProviderRecommendedAdminTestElevated = "Correct provider credentials, sender identity, or reserved test-target setup before rerunning more diagnostics on this lane.";
            public const string ProviderRecommendedAdminTestStable = "Use the reserved test target for a controlled rerun only after checking provider config and template state.";
            public const string ProviderEscalationAdminTestElevated = "Escalate as provider/configuration debt when this diagnostic lane keeps failing without a successful send.";
            public const string ProviderEscalationAdminTestStable = "Escalate only when repeated transport-test failures continue after configuration corrections.";
            public const string ProviderRecommendedGenericPending = "Review the pending and failed traffic in this provider lane before taking another manual action.";
            public const string ProviderRecommendedGenericStable = "Review recent failures in this provider lane before escalating.";
            public const string ProviderEscalationGenericElevated = "Escalate this provider lane if failures keep accumulating without recovery.";
            public const string ProviderEscalationGenericStable = "Escalate only if this provider lane continues degrading after basic transport checks.";
            public const string ChainRecommendedVerificationRecovered = "Do not replay historical verification messages. If the user is still blocked, confirm the current phone number and request a fresh code through the canonical verification flow.";
            public const string ChainRecommendedVerificationBlocked = "Do not replay historical verification messages. Confirm the current phone number, review preferred-vs-fallback channel policy, then request a fresh code through the canonical verification flow.";
            public const string ChainEscalationVerificationBlocked = "Repeated verification failures without a successful send indicate a likely transport or channel-policy issue. Escalate after confirming SMS/WhatsApp readiness and fallback policy.";
            public const string ChainEscalationVerificationStable = "Escalate only if the canonical verification flow keeps failing after channel readiness and policy have been checked.";
            public const string ChainRecommendedAdminTest = "Rerun diagnostics only to the reserved channel test target after correcting provider settings, templates, or channel policy.";
            public const string ChainEscalationAdminTestBlocked = "Repeated admin-test failures without any successful send suggest provider/configuration debt. Escalate as transport setup or provider issue instead of repeatedly rerunning tests.";
            public const string ChainEscalationAdminTestStable = "If a rerun succeeds, treat this as an isolated incident. Escalate only when repeated failures continue after config fixes.";
            public const string ChainRecommendedGenericPending = "Review the latest pending or failed non-email attempts before taking manual action.";
            public const string ChainRecommendedGenericStable = "Review recent non-email delivery history before escalating.";
            public const string ChainEscalationGenericRecovered = "Escalate only if the chain continues to fail after a previously successful path has been revalidated.";
            public const string ChainEscalationGenericBlocked = "Escalate when the same non-email path fails repeatedly without a verified successful send.";
            public const string ActionBlockedCanonicalFlow = "Do not replay historical verification messages. Request a fresh code through the canonical phone-verification flow.";
            public const string ActionBlockedCooldown = "Wait for the transport cooldown window before rerunning the same diagnostic channel test.";
            public const string ActionBlockedUnsupported = "No operator rerun path is defined for this non-email flow yet.";
            public const string EscalationReasonPhoneVerification = "Repeated verification failures without any successful send. Review transport readiness and fallback policy, then escalate if the canonical flow is still blocked.";
            public const string EscalationReasonAdminTest = "Repeated diagnostic transport failures without a successful send. Treat this as provider/config debt rather than another routine rerun.";
        }
    }
}
