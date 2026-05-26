# Darwin Go-Live Status

Last reviewed: 2026-05-26

This document records code-backed readiness. It deliberately separates implemented plumbing from production-complete provider behavior. External smoke inputs and command shapes are documented in [docs/external-smoke-inputs.md](external-smoke-inputs.md).

## Executive Status

| Area | Status | Remaining blocker |
| --- | --- | --- |
| WebAdmin | Operational surfaces are implemented for the current launch slice. | Continue hosted smoke coverage as workflows evolve. |
| WebApi | Public/member/business/admin/provider route boundaries are implemented. | Keep contract drift tests green. |
| Worker | Provider callback, email/channel, shipment, archive, and retry workers exist. | Deployment enablement and monitoring per provider. |
| Persistence | PostgreSQL preferred/default; SQL Server supported. | Production grants, backups, and migration validation per deployment. |
| Stripe | Test/staging paths for hosted checkout, webhook finalization, subscription finalization, refund reconciliation, and dispute follow-up exist. | Live-readiness preflight and approved live-mode smoke. |
| DHL | Real client path, provider-operation queue, label storage, return-label queueing, callbacks, and WebAdmin recovery surfaces exist. | Final account/product details and live validation. |
| Brevo | DB-backed transactional email routing, role senders, masked secrets, branded templates, webhook ingestion, and callback processing exist. | Production monitoring, inbox-placement checks, and operational alerting. |
| VIES | Soft `Unknown`/manual-review policy and live smoke harness exist. | Retry-worker ownership, monitoring, and support cadence. |
| Object storage | Reusable abstraction and providers are implemented; local MinIO smoke exists. | Production Object Lock/retention/legal-hold, backup/restore, monitoring, and selected-provider smoke. |
| E-invoice | Generation boundary, external-command adapter, local Mustangproject wrapper, storage boundary, and WebAdmin download guard exist. | Legal validation fixtures, production artifact smoke, and sign-off. |
| Mobile | Implemented Consumer and Business workflows are guarded and usable. | Signed release artifacts, production mobile config, device/camera smoke, and broader UI coverage. |

## Implemented Baseline

- Source-contract cleanup is complete for the current focused lanes, with zero skipped tests in the documented lanes.
- WebAdmin exposes compact operational surfaces for onboarding, business readiness, billing, communication, mobile operations, CRM, inventory, returns, shipping, and provider status.
- Site Settings preserves masked/blank secrets instead of clearing existing values, and WebAdmin avoids exposing provider credentials.
- WebApi payment and subscription flows require provider-hosted Stripe sessions and verified webhooks for final state.
- DHL operations do not generate fake labels, tracking numbers, provider references, or return labels.
- Brevo email routing is role-based and DB-backed. Transactional/security, billing, support, and admin-alert sender roles are configurable per deployment.
- Invoice archive/object storage uses provider-neutral abstractions. MinIO is the recommended self-hosted production target; AWS S3 and Azure Blob remain supported alternatives.
- E-invoice generated artifacts route through the invoice archive storage boundary when a generator is configured. Current JSON/HTML/source-model outputs are operational artifacts, not compliant e-invoices.
- Mobile apps reject broad Android cleartext traffic, avoid Release unsafe certificate trust, and keep mobile-used API routes under source-contract guard.

## Provider Readiness

### Stripe

Implemented:

- Checkout Session creation for storefront payments and business subscription checkout.
- Webhook ingestion with signature validation and idempotent callback processing.
- Refund reconciliation and dispute follow-up visibility.
- WebAdmin subscription, payment, refund, dispute, and webhook visibility.
- Guarded smoke scripts for test mode, webhook forwarding, runtime pipeline checks, and live readiness.

Not production-complete until:

- Live keys are configured securely.
- Live webhook endpoint and event subscriptions are confirmed.
- ProviderCallbackWorker and monitoring are enabled.
- Refund, failed-payment, and dispute playbooks are accepted.
- Approved live-mode smoke passes without relying on return-route finalization.

### DHL

Implemented:

- Real provider-client path for create shipment, labels, and queued return-label operations.
- Provider-operation queue and WebAdmin recovery actions.
- Label persistence through shared storage paths.
- Webhook/callback ingestion and failure visibility.

Not production-complete until:

- Final account, product code, billing number, shipper, receiver, label, tracking, and return-label details are validated.
- Runtime worker pipeline and storage are confirmed.
- Live smoke proves provider references, labels, tracking, callbacks, and retry/recovery behavior.
- Returns/RMA flows are visible, but full carrier-integrated RMA automation remains a go-live task.
- Carrier-integrated RMA automation remains under the DHL/shipping go-live slice.

### Brevo

Implemented:

- Transactional provider selection in Site Settings.
- Role-based sender identities and support `Reply-To` policy.
- Masked Brevo API key and webhook credential handling.
- Brevo outbound webhook ingestion with Basic Auth.
- Branded HTML templates for default transactional flows.
- Email dispatch and provider callback worker integration.

Not production-complete until:

- DNS and sender verification are monitored over time.
- Inbox placement is periodically checked after DNS, sender, template, or provider changes.
- Webhook delivery, callback backlog, and failed-send alerts have operational owners.

### VIES

Implemented:

- VIES-backed VAT lookup path.
- Policy that provider failures remain `Unknown` and require manual review.
- Guarded smoke script for valid, invalid, and provider-failure checks.
- Disabled-by-default retry worker for provider-generated `Unknown` decisions.

Not production-complete until:

- Retry cadence and support ownership are accepted.
- Monitoring and manual-review handling are operational.

## Compliance And Archive

- Full e-invoicing compliance is not implemented.
- E-invoice phase 1 now has a structured invoice source-model export from issued snapshots, a minimum source-readiness validator, JSON/XML source-model downloads, a provider-neutral `IEInvoiceGenerationService` boundary, an external-command adapter, and guarded artifact storage/download paths.
- Invoice issue-readiness guards and immutable issued snapshots exist.
- Archive metadata includes hash, generated time, retention horizon, policy version, and purge metadata.
- Archive purge worker is explicit opt-in.
- Database/internal storage is development/internal fallback and must not be described as production immutable.
- Production archive immutability requires provider-level controls, not just application-level protections.
- ZUGFeRD/Factur-X remains the primary e-invoice target. Full compliance is not claimed until legal validation evidence and production smoke pass.
- Current JSON/HTML/CSV/source-model exports are not full e-invoice compliance.
- The external-command adapter rejects non-PDF ZUGFeRD/Factur-X outputs and malformed XRechnung XML before storage; see docs/e-invoice-tooling-decision.md.

## Mobile Readiness

- Consumer and Business apps are conditionally usable for implemented workflows.
- Consumer checkout is outside first mobile-app scope; customer payment belongs to web storefront flow when enabled.
- Business subscription purchase, cancellation, SEPA mandate setup, and manual payment registration stay in web/back-office workflows for first launch.
- Business app shows read-only subscription/contract status and a management handoff.
- Store launch still requires signed release packages, production Google Maps/Firebase/APNS configuration, push/device smoke, physical QR camera validation, and broader UI/E2E coverage.
- Tizen is out of launch scope.

## Testing And CI Status

- Focused source-contract, provider-smoke guard, mobile guard, WebAdmin hosted smoke, and infrastructure storage lanes exist.
- CI includes split lanes for WebAdmin and mobile guard/build validation.
- Provider smoke scripts are opt-in and are designed not to print secrets or raw provider payloads.
- Current test commands and lane ownership are maintained in [DarwinTesting.md](../DarwinTesting.md).

## Open Go-Live Work

- Stripe live-readiness preflight and approved live smoke.
- DHL live account/product validation.
- Brevo production monitoring and periodic inbox-placement checks.
- VIES retry-worker operational policy.
- Production object-storage bucket/container validation.
- E-invoice legal validation and production artifact smoke.
- Signed mobile release and device/provider smoke.
