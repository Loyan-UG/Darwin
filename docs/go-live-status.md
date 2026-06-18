# Darwin Go-Live Status

Last reviewed: 2026-06-18

This document records code-backed readiness. It is not a product overview or marketing status page; use [README.md](../README.md) for the platform summary. This document deliberately separates implemented plumbing from production-complete provider behavior. External smoke inputs and command shapes are documented in [docs/external-smoke-inputs.md](external-smoke-inputs.md).

## Executive Status

| Area | Status | Remaining blocker |
| --- | --- | --- |
| WebAdmin | Operational surfaces are implemented for the current launch slice. | Continue hosted smoke coverage as workflows evolve. |
| WebApi | Public/member/business/admin/provider route boundaries are implemented. | Keep contract drift tests green. |
| Web | Front-office/member shell exists and is customer-facing. | Keep public/member wording free from internal diagnostics and complete checkout smoke when that scope is enabled. |
| Worker | Provider callback, email/channel, shipment, archive, and retry workers exist. | Deployment enablement and monitoring per provider. |
| Persistence | PostgreSQL preferred/default; SQL Server supported. | Production grants, backups, and migration validation per deployment. |
| Stripe | Test/staging paths for hosted checkout, webhook finalization, subscription finalization, refund reconciliation, and dispute follow-up exist. Local handoff and public-webhook finalized test-mode smoke passed on 2026-05-27. | Live-readiness preflight, monitoring, and approved live-mode smoke. |
| DHL | Real client path, provider-operation queue, label storage, return-label queueing, callbacks, WebAdmin recovery surfaces, and focused internal DHL/shipment tests are green. | Final account/product details and live validation. |
| Brevo | DB-backed transactional email routing, role senders, masked secrets, branded templates, webhook ingestion, callback processing, and Site Settings-backed smoke loading exist. | Production monitoring, periodic inbox-placement checks, and alert ownership. |
| VIES | `Manual Review + Scheduled Retry` policy exists. Controlled valid/invalid/provider-failure live smoke passed on 2026-05-27. | Production monitoring ownership and periodic smoke after provider or policy changes. |
| Object storage | Reusable abstraction and providers are implemented; local MinIO smoke passed again on 2026-05-27 against the S3-compatible provider path. Production-like provider smoke execution is guarded behind explicit operator confirmation for profiles, disposable prefix, retention/delete behavior, and operator runbook readiness. | Production Object Lock/retention/legal-hold, backup/restore, monitoring, and selected-provider smoke. |
| E-invoice | Generation boundary, external-command adapter, local Mustangproject wrapper, storage boundary, WebAdmin download guard, validation-report enforcement option, local adapter smoke for XRechnung XML plus ZUGFeRD/Factur-X PDF, and production readiness preflight exist. | Legal validation fixtures, production artifact smoke, and sign-off. |
| Mobile | Implemented Consumer and Business workflows are guarded and usable. Google external-login backend/service support exists. Android is the selected first launch target. | Signed Android release artifacts, production Android Google Maps/Firebase config, native Android Google sign-in UI/device smoke when enabled, Android device/camera smoke, and broader Android UI coverage. iOS and MacCatalyst follow after Android evidence is complete. |
| Web external identity | Google Identity Services handoff to WebApi external-login is implemented and keeps Darwin session cookies authoritative. | Web OAuth client ID configuration and browser smoke. |
| Production evidence package | Evidence package rules, production-like staging execution order, generation/validation scripts, non-secret readiness report exporters, and the report-bundle validator are documented in [docs/production-readiness-evidence-package.md](production-readiness-evidence-package.md) and [docs/production-go-live-evidence-execution-plan.md](production-go-live-evidence-execution-plan.md), then linked from production setup and onboarding. A local ignored working package and non-secret report bundle were generated on 2026-06-18 for the current branch. | Populate and validate the package for each deployment with non-secret build, migration, staging rehearsal, storage, e-invoice, provider, mobile, monitoring, rollback, and approval evidence. |

## Implemented Baseline

- Source-contract cleanup is complete for the current focused lanes, with zero skipped tests in the documented lanes.
- WebAdmin exposes compact operational surfaces for onboarding, business readiness, billing, communication, mobile operations, CRM, inventory, returns, shipping, and provider status.
- Site Settings preserves masked/blank secrets instead of clearing existing values, and WebAdmin avoids exposing provider credentials.
- WebAdmin shipment provider-operation mutations now surface concurrency conflicts safely and the focused DHL/shipment WebAdmin smoke lane is green.
- WebApi payment and subscription flows require provider-hosted Stripe sessions and verified webhooks for final state.
- DHL operations do not generate fake labels, tracking numbers, provider references, or return labels.
- Brevo email routing is role-based and DB-backed. Transactional/security, billing, support, and admin-alert sender roles are configurable per deployment.
- Brevo readiness smoke can load Site Settings directly for local/staging validation without printing secrets.
- VIES provider failures stay `Unknown`/manual review, are retried by the scheduled retry path, and include operator-only format hints that do not replace official validation.
- Invoice archive/object storage uses provider-neutral abstractions. MinIO is the selected first production target; Azure Blob is the next provider-hardening lane after MinIO evidence, and AWS S3 remains a supported alternative.
- Local MinIO smoke validates the development S3-compatible path, Object Lock-enabled bucket creation, versioning, metadata/hash behavior, temporary URL support, and retained-object cleanup boundaries. It does not prove production immutability.
- E-invoice generated artifacts route through the invoice archive storage boundary when a generator is configured. Current JSON/HTML/source-model outputs are operational artifacts, not compliant e-invoices.
- Mobile apps reject broad Android cleartext traffic, avoid Release unsafe certificate trust, and keep mobile-used API routes under source-contract guard.
- Google external-login support is provider-neutral at the API, Web, and mobile-service boundary. WebApi validates Google identity tokens server-side against Site Settings OAuth client IDs and issues Darwin tokens; provider tokens are not stored or logged.
- A local production-like evidence working copy and non-secret readiness report bundle were generated on 2026-06-18 under `artifacts\production-readiness\`, which is ignored by git. The report bundle covers production-like staging, Web/Mobile readiness, aggregate go-live dry-run, MinIO, Azure Blob, e-invoice, Android launch, and provider readiness, then `scripts\check-production-readiness-report-bundle.ps1` validates the local report set shape. It does not clear external deployment blockers.

## Provider Readiness

### Stripe

Implemented:

- Checkout Session creation for storefront payments and business subscription checkout.
- Webhook ingestion with signature validation and idempotent callback processing.
- Refund reconciliation and dispute follow-up visibility.
- WebAdmin subscription, payment, refund, dispute, and webhook visibility.
- Guarded smoke scripts for test mode, webhook forwarding, runtime pipeline checks, and live readiness.
- Local test-mode storefront handoff smoke passed on 2026-05-27: a disposable order was created, the checkout handoff returned a Stripe-hosted URL, the payment stayed `Pending`, and the browser return route did not finalize the payment.
- Public-webhook finalized test-mode smoke passed on 2026-05-27: a browser checkout used a Stripe-hosted test payment, signed `checkout.session.completed` and `payment_intent.succeeded` callbacks were processed by the provider callback worker, and final payment state was recorded by verified webhook processing.
- The front-office order confirmation page treats captured Stripe payments as recorded payments and no longer offers another payment handoff when the provider has finalized the order.

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
- Focused Unit/WebApi/WebAdmin DHL and shipment lanes pass without using fake provider references.

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
- Site Settings-backed smoke loading for local/staging validation.

Not production-complete until:

- DNS and sender verification are monitored over time.
- Inbox placement is periodically checked after DNS, sender, template, or provider changes.
- Webhook delivery, callback backlog, failed-send alerts, and inbox-placement checks have operational owners.

### VIES

Implemented:

- VIES-backed VAT lookup path.
- Policy that provider failures remain `Unknown` and require manual review.
- Scheduled retry for provider-generated `Unknown` outcomes.
- Operator VAT-format hints that never auto-confirm validity.
- Critical admin alert email when retry failures exceed the configured threshold.
- Guarded smoke script for valid, invalid, and provider-failure checks.
- Controlled live smoke for valid, invalid, and provider-failure behavior passed on 2026-05-27.

Not production-complete until:

- Monitoring and manual-review handling are operational in the target deployment.
- Periodic smoke is repeated after VIES provider, retry, or policy changes.

## Compliance And Archive

- Full e-invoicing compliance is not implemented.
- E-invoice phase 1 now has a structured invoice source-model export from issued snapshots, a minimum source-readiness validator, JSON/XML source-model downloads, a provider-neutral `IEInvoiceGenerationService` boundary, an external-command adapter, and guarded artifact storage/download paths.
- Local external-command smoke passed on 2026-05-27 for both XRechnung XML and ZUGFeRD/Factur-X PDF artifact shape through the Mustangproject wrapper, including `RequireValidationReport`.
- Invoice issue-readiness guards and immutable issued snapshots exist.
- Archive metadata includes hash, generated time, retention horizon, policy version, and purge metadata.
- Archive purge worker is explicit opt-in.
- Database/internal storage is development/internal fallback and must not be described as production immutable.
- Production archive immutability requires provider-level controls, not just application-level protections.
- ZUGFeRD/Factur-X and XRechnung are both required for the selected German e-invoice readiness path. Full compliance is not claimed until legal validation evidence, production artifact storage/download smoke, and accounting/tax sign-off pass for both formats.
- Current JSON/HTML/CSV/source-model exports are not full e-invoice compliance.
- The external-command adapter rejects non-PDF ZUGFeRD/Factur-X outputs and malformed XRechnung XML before storage; see docs/e-invoice-tooling-decision.md.

## Mobile Readiness

- Consumer and Business apps are conditionally usable for implemented workflows.
- Consumer checkout is outside first mobile-app scope; customer payment belongs to web storefront flow when enabled.
- Business subscription purchase, cancellation, SEPA mandate setup, and manual payment registration stay in web/back-office workflows for first launch.
- Business app shows read-only subscription/contract status and a management handoff.
- Store launch still requires signed release packages, production Google Maps/Firebase/APNS configuration, Android readiness preflight, push/device smoke, physical QR camera validation, and broader UI/E2E coverage.
- Google sign-in still requires deployment OAuth client IDs, native mobile UI integration, and device smoke before it is a launch-ready mobile feature.
- Web Google sign-in still requires a Web OAuth client ID and browser smoke before it is launch-ready.
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
- VIES production monitoring ownership and periodic controlled smoke.
- Production object-storage bucket/container validation.
- E-invoice legal validation and production artifact smoke.
- Signed Android release and device/provider smoke first; iOS and MacCatalyst after Android evidence is complete.
- Deployment-specific production readiness evidence package with passing validation, owner approvals, rollback plan, monitoring ownership, and non-secret references to smoke evidence.
