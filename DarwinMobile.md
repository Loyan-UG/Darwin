# Darwin Mobile Guide

Reviewed: 2026-06-08

Darwin includes two MAUI applications:

- `Darwin.Mobile.Consumer`: consumer/member app.
- `Darwin.Mobile.Business`: business/operator app.

Shared API client, route, storage, resilience, and service logic lives in `Darwin.Mobile.Shared`.

## Launch Position

The mobile apps are conditionally usable for implemented workflows. They are not fully store-launch ready until signed release artifacts, production mobile configuration, physical device smoke, push smoke, and camera-based QR validation are complete.

Tizen is out-of-scope for the current launch scope.

## Consumer App Scope

Consumer app responsibilities:

- Authentication and account lifecycle.
- Google external login service/route support after deployment OAuth client IDs are configured. Native mobile UI remains disabled until the device flow is wired and smoke-tested.
- Profile, preferences, phone verification, addresses, and account deletion request flow.
- Business discovery/detail and member engagement.
- Loyalty overview, account, rewards, history, join, promotions/timeline where exposed, and QR preparation.
- Member commerce history code exists for orders and invoices through `MemberCommercePage`, `MemberCommerceViewModel`, `MemberCommerceService`, and `ApiRoutes.Orders` / `ApiRoutes.Invoices`.
- Member order and invoice detail, retry-payment, document, archive, and structured source-model routes are compatibility-sensitive when those surfaces are enabled.
- Push/device registration.
- Legal links and permission disclosures.

Current first-launch payment policy:

- Consumer mobile does not process payment directly.
- Customer checkout, cart placement, and payment finalization belong to the web storefront and WebApi checkout routes when enabled.
- Payment finalization remains verified-webhook-authoritative.
- Sales expansion currently preserves mobile commerce compatibility only; it does not expand consumer mobile checkout or make mobile the primary order/invoice surface.

## Business App Scope

Business app responsibilities:

- Login/logout/refresh and account lifecycle support.
- Invitation preview and acceptance.
- Preferred business context.
- Access-state soft gate.
- Dashboard and read-only status surfaces.
- QR scanner/session processing.
- Loyalty accrual/redemption confirmation.
- Reward/campaign surfaces where enabled.
- Subscription/contract/billing status.
- Legal/settings/account deletion handoff.

Business subscription policy:

- Business mobile shows read-only plan/contract/subscription status.
- Plan purchase, subscription cancellation, SEPA mandate setup, and manual payment registration are web/back-office workflows for first launch.
- No local/mock subscription success is allowed.

## Access-State Gate

Business app live operations must respect access state:

- Pending approval: Home and Settings/read-only surfaces may remain available; live scan/accrual/redemption/reward/campaign/subscription mutation must be blocked.
- Approved and active: live operations may be allowed.
- Suspended/inactive: live operations must be blocked with clear operator messaging.

## Security And Release Rules

- Release builds must not allow broad cleartext HTTP.
- Unsafe certificate trust must be debug-only and fail-fast outside debug.
- Android app backup is disabled for both mobile apps.
- Google Maps, Firebase, APNS, signing profiles, and store credentials are supplied through secure local/CI/provider configuration.
- Google external login client IDs are deployment configuration, not source code constants. Provider tokens must be validated by WebApi and must not be stored or logged by mobile clients.
- Tokens must be stored through secure storage on supported MAUI targets.
- Logout/account switch must clear sensitive local state.
- The SQLite outbox repository remains inactive scaffolding until an offline mutation processor, idempotency, cleanup, and support visibility are implemented.
- No secrets or PII should be logged.

## WebApi Dependencies

Mobile uses canonical audience-first routes:

- `api/v1/public/*`
- `api/v1/member/*`
- `api/v1/business/*`

Admin DTOs must not leak into mobile contracts. Mobile route constants and services should stay aligned with `DarwinWebApi.md` and `src/Darwin.Contracts`.

## WebAdmin Dependencies

WebAdmin must support mobile operations through:

- Business onboarding and approval.
- Owner/member/invitation lifecycle.
- Activation, reset, lock/unlock, and communication audit support.
- Billing/subscription reconciliation.
- Loyalty setup, reward, campaign, scan session, and redemption support.
- Mobile device diagnostics and push-token support.
- Shipment/payment/invoice support where mobile exposes related data.

## Testing Expectations

Required guard coverage:

- Route constants and canonical route usage.
- No legacy aliases where canonical mobile routes exist.
- Resource parity for supported locales.
- Android manifest hardening.
- Unsafe certificate trust rejection outside debug.
- No committed mobile API keys.
- Consumer settings/navigation/account deletion/push registration behavior.
- Business subscription read-only handoff and soft-gate source contracts.

Remaining device validation:

- Native Google sign-in UI and device smoke after Android/iOS OAuth client IDs are configured.
- Signed Android/iOS/MacCatalyst artifacts.
- Production push registration and notification smoke.
- Physical camera QR scan end-to-end.
- Broader UI/E2E device smoke.

See [DarwinTesting.md](DarwinTesting.md).
