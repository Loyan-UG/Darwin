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

- `Darwin.Mobile.Business` is usable enough that missing onboarding/admin support is now a platform blocker
- WebAdmin and backend must support business user lifecycle and operational troubleshooting

## 3. Current Mobile Status

### `Darwin.Mobile.Consumer`

Current usable areas include:

- authentication
- profile and preferences
- member addresses
- loyalty views
- orders and invoices
- CRM-linked customer context

### `Darwin.Mobile.Business`

Current usable areas include:

- loyalty scanning and operational business usage
- dashboard and business-side workflows already implemented in the app
- account/profile/password flows that now depend on cleaner backend and admin support
- business invitation preview plus token-entry and configurable magic-link acceptance for phase-1 onboarding

### `Darwin.Mobile.Shared`

Shared responsibilities include:

- API route catalog
- auth/profile/loyalty/member-commerce service abstractions
- contract-aligned shared client logic

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
