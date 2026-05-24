# Darwin Mobile Guide

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)

> Scope: `Darwin.Mobile.Consumer`, `Darwin.Mobile.Business`, `Darwin.Mobile.Shared`, and mobile-facing contract dependencies.

Current go-live status is summarized in `docs/go-live-status.md`. Mobile should treat DHL tracking/callback fields as available presentation signals, but not assume live DHL shipment/label API integration is complete until the provider gap is closed.

## 1. Purpose

Darwin has two MAUI apps:

- `Darwin.Mobile.Consumer`: member-facing app
- `Darwin.Mobile.Business`: business/staff-facing app

The mobile suite is already important enough to affect platform priorities. Early operational usage is expected to start from business/mobile-facing workflows, which means backend and WebAdmin must support mobile onboarding and account lifecycle needs correctly.

## 2. Current Priority Clarification

Current delivery priority is not "mobile first" in isolation.

The actual priority is:

1. keep mobile-used backend flows stable
2. complete `Darwin.WebAdmin` and backend workflows needed by mobile operations
3. support real business onboarding, activation, setup, and support scenarios

Practical consequence:

- implemented Consumer and Business mobile workflows are operationally usable in the current code-backed scope
- WebAdmin and backend must continue to support business user lifecycle, operational troubleshooting, provider readiness, and release support before a store-launch claim is made

## 3. Current Mobile Status

The mobile apps are conditionally launch-ready for the workflows that are already implemented and guarded by tests. They are not yet final store-launch artifacts because external provider smoke, production mobile configuration, signing/package validation, and several product-scope decisions remain open.

Code-backed status as of 2026-05-24:

- `Darwin.Mobile.Shared.Tests`: `228` passed, `0` skipped.
- `Darwin.Mobile.Consumer.Tests`: `119` passed, `0` skipped.
- `Darwin.Mobile.Business.Tests`: `11` passed, `0` skipped.
- CI runs the shared, Consumer, and Business mobile guard lanes on Windows with the MAUI workload installed, plus release-like Windows target build validation and Android Debug build validation.
- Android cleartext traffic and Android platform backup are disabled for both mobile apps.
- Unsafe certificate trust remains debug-only guarded.
- Consumer and Business English/German resource parity is guarded.
- Tizen is out-of-scope for the current mobile launch; Tizen placeholder manifests are not release blockers for Android, iOS, MacCatalyst, or the Windows validation target.
- Android emulator runtime smoke on 2026-05-24 validated Consumer login, Discover, Google Maps rendering, Consumer QR generation, Business login, Business Home, and the Business Scan tab without runtime crashes.
- The loyalty QR non-camera path passed against `https://api.loyan.de` with a real member/business token flow: Consumer prepared an accrual scan session, Business processed the token, and Business confirmed accrual. Camera-based QR scan from a second device/emulator remains required before treating the physical scanner path as launch-complete.

### `Darwin.Mobile.Consumer`

Current usable areas include:

- authentication
- profile and preferences
- member addresses
- loyalty views
- orders and invoices
- invoice document, archive document, and structured JSON/XML source-model copy actions where WebApi exposes those artifacts
- CRM-linked customer context
- local Android emulator validation for login, Discover, Google Maps, and QR generation

Launch conditions still open:

- Google Maps and Firebase/APNS production configuration must be supplied through secure local/CI/provider configuration. `Darwin.Mobile.Consumer` expects the Android Google Maps key through `GoogleMapsApiKey`, `GOOGLE_MAPS_API_KEY`, or `ANDROID_GOOGLE_MAPS_API_KEY`; local developers may store it in `.NET User Secrets` and synchronize the build-time variables with `scripts/sync-mobile-google-maps-user-secret.ps1`. Android Release builds require `src/Darwin.Mobile.Consumer/google-services.json` for Firebase Cloud Messaging. iOS/MacCatalyst Release entitlements already use production APNS, but signed builds still require Apple Developer provisioning and APNS provider configuration outside the repository.
- Android/iOS/MacCatalyst release artifacts, signing, store metadata, and device smoke must be validated.
- Product scope must confirm whether Consumer checkout remains hosted/web-payment handoff or requires full in-app checkout.
- More end-to-end UI/device coverage is still needed beyond the current source-contract and ViewModel guard lanes.
- Camera-based QR validation still requires a real device/emulator-to-device scan. The API path is validated, but the physical camera path is not launch-complete until the Business app scans a QR displayed on a second screen/device.

### `Darwin.Mobile.Business`

Current usable areas include:

- loyalty scanning and operational business usage
- dashboard and business-side workflows already implemented in the app
- account/profile/password flows that now depend on cleaner backend and admin support
- business invitation preview plus token-entry and configurable magic-link acceptance for phase-1 onboarding
- Android cleartext traffic and platform backup are disabled for release hardening; local HTTP testing must stay debug-only if it is ever reintroduced.
- local Android emulator validation for login, Home, and Scan tab startup
- non-camera scan-session processing and accrual confirmation through the business WebApi path

Launch conditions still open:

- Product scope must confirm whether Business subscription management remains read-only status plus HTTPS management-website handoff or expands to full in-app checkout/cancel management.
- Production Stripe, Brevo, DHL, VIES, object-storage, push, and app-link configuration must be smoke-tested with real deployment settings before production claims.
- Android/iOS/MacCatalyst signing, entitlement, package, and device validation remain required.
- Broader Business UI/device coverage is still needed beyond source-contract soft-gate guards.
- Camera-based QR scanning must be validated with a second device/emulator camera feed before scanner hardware behavior is considered launch-complete.

### `Darwin.Mobile.Shared`

Shared responsibilities include:

- API route catalog
- auth/profile/loyalty/member-commerce service abstractions
- contract-aligned shared client logic
- launch-readiness guards for canonical routes, resource parity, cleartext-traffic hardening, and committed mobile key checks
- local cache support is active for scoped read models; the SQLite outbox repository is inactive scaffolding until an offline mutation processor and account/business-switch lifecycle policy are implemented

## 4. Business User Lifecycle

This lifecycle matters now because it directly affects early go-live.

Required scenarios:

- signup
- invitation
- activation
- login
- forgot password
- reset password
- status-based access
- onboarding completion
- lock/suspend/reactivate where required

### Current state

- `In Progress`: login and operational app usage exist
- `Completed`: business invitation preview and acceptance now work in `Darwin.Mobile.Business` through canonical business-auth endpoints
- `Completed`: preferred business context is preserved during token refresh so invitation-based onboarding is safer for multi-business operators
- `Completed`: password sign-in now enforces email confirmation and account lockout, so mobile clients follow the same activation/support policy as admin-backed operations
- `Completed`: `Darwin.Mobile.Consumer` registration now stops after account creation when confirmation is pending instead of attempting an immediate auto-login
- `Completed`: both mobile apps now expose self-service activation-email request actions on their login surfaces
- `Completed`: `Darwin.Mobile.Business` now uses a phase-1 `Soft Gate` approval policy; pending-approval businesses can sign in and continue setup, while live operational screens stay blocked until approval
- `Planned / Near-term`: the surrounding onboarding, activation, invitation, and support lifecycle must be completed end-to-end through backend and WebAdmin

### Phase-1 invitation acceptance mode

Current implementation is intentionally staged:

- token-entry invitation acceptance already exists in `Darwin.Mobile.Business`
- `Decision made`: phase-1 invitation acceptance should support both token-entry and magic-link entry points
- `Completed`: onboarding invitation emails now include a configurable magic link in addition to the manual token path
- `Planned / Near-term`: if production app-link registration needs stronger guarantees, extend the current config-driven magic-link base to verified universal/app-link handling per platform
- a full multi-business switcher is still later work; current behavior only preserves the active business context during refresh

## 5. Email Dependency

The following scenarios depend on reliable email sending:

- signup confirmation
- invitation
- account activation
- forgot password
- password reset

This dependency should not be treated as optional infrastructure. It is a go-live-critical platform capability and one of the main reasons Communication Core must be delivered early with email-first scope.

Because email confirmation is now enforced during password sign-in, the activation email path is operationally critical for both consumer and business onboarding. Mobile now exposes self-service resend-confirmation actions, while comparable web/front-office UX is still near-term work.

## 6. Mobile and WebAdmin Dependency

Mobile usage depends on WebAdmin and backend being able to:

- create businesses
- provision owner/admin users
- manage business account state
- support activation and password recovery
- inspect and troubleshoot account/payment/shipment issues
- apply initial defaults and configuration
- apply approval, suspension, and reactivation decisions that now directly affect business-app operational access

### Business approval access policy

Current phase-1 behavior:

- `Completed`: `Darwin.Mobile.Business` can sign in while the linked business is `PendingApproval`
- `Completed`: `Home` and `Settings` remain accessible so operators can continue setup and support troubleshooting
- `Completed`: `Scanner`, `Session`, `Dashboard`, and `Rewards` now check the authenticated business access-state API and block live operations until the business is both `Approved` and active
- `Completed`: `Suspended` businesses are treated as operationally blocked and now surface suspension context to the operator
- `Planned / Near-term`: decide whether later phases should move from screen-level soft gating to a dedicated onboarding workspace or stricter route composition

This is why WebAdmin completion is currently more important than broader front-office expansion.

## 7. Mobile and WebApi Dependency

Mobile apps rely on `Darwin.WebApi` and `Darwin.Contracts` as the delivery boundary.

Rules:

- keep mobile-used endpoints stable
- preserve compatibility when changing routes or payloads
- if a mobile-used endpoint changes, update `Darwin.Mobile.Shared` and then the consuming mobile UI path

The current platform rule remains:

- mobile-used WebApi flows must continue to work
- broader WebApi expansion can proceed after admin/backend priorities are met

## 8. Localization Note

Current state:

- mobile apps already support bilingual operation
- adding another mobile resource language is comparatively straightforward

Platform implication:

- future platform-wide multilingual support should stay aligned with the mobile localization approach
- WebAdmin is not yet fully multilingual, but it should be built in a way that makes later multilingual rollout easier

## 9. Communication and Notification Implication

Mobile-related user lifecycle flows depend on Communication Core:

- invitation
- signup confirmation
- activation
- forgot password
- reset password
- important account notifications

This means Communication Core is not only a web/admin concern. It is directly tied to mobile onboarding success.

## 10. Security and Operational Notes

Important cross-cutting concerns for mobile-backed flows:

- secure token handling
- account status-based access control
- tenant/business isolation
- safe reset and activation flows
- PII protection
- auditability for support-sensitive actions
- Android manifests must not enable app-wide cleartext HTTP. Future local HTTP testing must use a debug-only network-security path instead of weakening Release.
- Both MAUI apps disable Android platform backup in their manifests so token-adjacent local state, cached preferences, and app data are not included in broad OS backup/restore flows.
- Google Maps and Firebase/APNS configuration must come from secure local, CI, or provider-managed configuration. Real production keys must not be committed.
- Mobile maps/push input names and device-smoke steps are tracked in `docs/external-smoke-inputs.md`. The repository must not be treated as the source of truth for production provider credentials.
- Structured invoice JSON/XML downloads are operational artifacts only. They are not compliant e-invoices until the selected ZUGFeRD/Factur-X generator path is completed and validated.

The admin and backend systems must expose enough operator visibility to support these flows without leaking sensitive internals into the mobile clients.

## 11. Phase-1 Provider Assumptions

### Payments

- phase-1 payment implementation is `Stripe-first`
- mobile business/account lifecycle support may need Stripe-related payment visibility from WebAdmin and backend even before deep mobile payment features expand

### Shipping

- phase-1 shipping implementation is `DHL-first`
- shipping support and order troubleshooting in admin/backend should assume DHL-first operational flow in the first go-live wave

## 12. Near-Term Mobile-Side Priorities

- keep loyalty, auth, profile, and business-operational flows stable
- ensure backend/admin onboarding support is sufficient for business app usage
- keep `Darwin.Mobile.Shared` aligned whenever contracts or canonical routes change
- avoid introducing drift between mobile route assumptions and WebApi ownership
- keep the Business subscription mobile surface as status plus HTTPS management-website handoff until the product owner approves full in-app billing management
- keep `Darwin.Mobile.Consumer.Tests` green for Consumer release guards, resource parity, push-registration orchestration, and settings navigation command coverage
- keep `Darwin.Mobile.Business.Tests` green for Business release guards, resource parity, read-only subscription handoff, WebAdmin support-surface guards, and source-contract soft-gate coverage
- keep shared package versions aligned between Consumer and Business to avoid UI/runtime drift across the two MAUI apps
- keep customer-facing Consumer surfaces free of WebAdmin/operator diagnostics; operational diagnostics belong in WebAdmin and support tooling

## 12.1 Review Checklist for the Dedicated Mobile Audit

When the separate mobile-review chat starts, it should explicitly verify these areas against current platform reality:

- business approval soft-gate behavior and all approval-gated screens/actions
- invitation acceptance, activation, confirmation, forgot-password, and reset-password journeys
- self-service resend-activation behavior on login surfaces
- preferred business-context preservation during refresh and onboarding
- loyalty UX assumptions against the now-expanded loyalty admin/operator surfaces
- device diagnostics and remediation assumptions against the `Mobile Operations` workspace
- communication failure and retry assumptions for:
  - business invitation
  - account activation
  - password reset
- subscription/billing issue handling assumptions now that admin has richer invoice/payment/refund/webhook triage
- locale/fallback assumptions now that CRM/admin exposes locale source and platform fallback visibility
- invoice/tax/compliance assumptions now that admin exposes B2B/B2C tax profile and archive/e-invoice readiness signals

This review should treat these as real platform changes to verify, not just as documentation notes.

## 13. What Is Deliberately Not the Current Priority

- broad new mobile feature expansion that depends on unfinished onboarding/admin capabilities
- major mobile-only UX investment before backend and WebAdmin support gaps are closed
- aggressive new API surface changes that could destabilize current business/mobile usage
