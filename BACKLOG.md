# Darwin Backlog

Reviewed: 2026-05-26

This is the active roadmap. Historical implementation notes belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). Code-backed readiness belongs in [docs/go-live-status.md](docs/go-live-status.md) and [docs/module-audit.md](docs/module-audit.md).

## Current Go-Live Blockers

These items require external credentials, deployment configuration, provider accounts, legal/compliance evidence, signed artifacts, device validation, or explicit operational approval.

- `Stripe live readiness`: test-mode checkout, webhook finalization, subscription finalization, refund reconciliation, and dispute follow-up are implemented and have staging evidence. Before production traffic, run the live-readiness preflight, configure live keys and live webhook events securely, verify ProviderCallbackWorker processing, monitoring, alerting, refund/dispute playbooks, and complete an approved live-mode smoke.
- `DHL live validation`: the real client path, provider-operation queue, label persistence, return-label queueing, callback ingestion, and WebAdmin recovery surfaces exist. Final account, billing, product-code, shipper/receiver, label, tracking, and return-label validation remain blocked until complete DHL account data is available.
- `Brevo production operations`: DB-backed email routing, role sender addresses, masked credentials, webhook Basic Auth, branded HTML templates, and callback processing exist. Production still needs ongoing inbox-placement checks, DNS/template monitoring, webhook delivery monitoring, and operational alert ownership.
- `VAT/VIES production ownership`: VIES live smoke and provider-failure mapping are guarded. Before depending on VIES in production review workflows, approve retry cadence, support ownership, monitoring, and the disabled-by-default retry worker policy.
- `Production object storage`: MinIO is the recommended self-hosted target through the S3-compatible provider; AWS S3 and Azure Blob are alternatives. Production archive immutability is blocked until the real provider has TLS, dedicated least-privilege keys, versioning, Object Lock or equivalent retention/legal hold, backup, restore testing, monitoring, and selected-provider smoke.
- `E-invoice compliance`: structured invoice source-model JSON, minimum source-readiness validation, and a provider-neutral `IEInvoiceGenerationService` boundary now exist. The first tooling path is selected as Mustangproject CLI through the external-command adapter. Local adapter smoke and storage contracts exist, but full ZUGFeRD/Factur-X compliance is blocked until deterministic/legal validation fixtures, production artifact download/storage smoke, and operator/legal sign-off are complete. XRechnung remains secondary unless a deployment requires it earlier.
- `Mobile store launch`: implemented Consumer and Business workflows are guarded and usable, but signed Android/iOS/MacCatalyst release artifacts, production Google Maps/Firebase/APNS configuration, push/device smoke, physical camera QR validation, and broader device UI coverage remain required.

## Completed Internal Baseline

- WebAdmin operational workflows exist for onboarding, support queues, billing, communication, mobile operations, CRM, inventory, returns, shipping/provider operations, and site settings.
- WebApi keeps audience-first public/member/business/admin/provider callback boundaries, with payment completion verified by webhooks rather than browser return routes.
- Worker paths exist for provider callbacks, email/channel dispatch, shipment provider operations, invoice archive maintenance, and retry workflows.
- Object-storage architecture is modular and reusable; local MinIO smoke exists; database/internal archive storage is development/internal fallback only.
- Mobile cleartext and backup hardening, route guards, resource parity checks, package parity, and launch guard lanes exist for Consumer and Business.
- Source-contract cleanup is complete for the current focused lanes with zero skipped tests in the documented lanes.
- Provider smoke harnesses are opt-in and designed to avoid printing secrets or raw provider payloads.

## Near-Term Tasks

- Keep the focused WebAdmin, WebApi, Application, Infrastructure, mobile guard, and source-contract lanes green as implementation continues.
- Repeat Stripe test-mode smoke after payment or subscription changes, then run the live-readiness preflight before any live-mode validation.
- Keep DHL parked until final account/product data arrives, then run the guarded live smoke and validate label storage, tracking, returns, callbacks, and WebAdmin recovery.
- Repeat Brevo inbox-placement checks after sender-domain, template, or provider changes; monitor webhook/callback and failed-send queues.
- Run VIES live smoke before production traffic or after VIES provider changes; keep provider failure as `Unknown`/manual review.
- Keep local MinIO smoke in the release checklist; run production MinIO readiness and selected-provider smoke only against the final target bucket.
- Complete e-invoice deterministic fixtures, legal validation evidence, and production artifact smoke before exposing compliant artifacts.
- Add deeper Consumer and Business ViewModel/UI coverage for auth, profile, business access-state gates, rewards/campaigns, member-commerce invoice artifacts, push registration, and account deletion.
- Validate signed mobile release artifacts and production push/maps configuration outside the repository.
- Keep business subscription and customer checkout in web/back-office workflows for first mobile launch; mobile apps show status and handoff only.
- Decide whether to activate mobile SQLite outbox beyond inactive scaffolding. Do not enable offline mutations without processor, idempotency, cleanup, and support visibility.

## Later-Phase Tasks

- Carrier-integrated DHL RMA/returns automation beyond the current return-label queue path, returns queue, and shipment provider operations.
- Brevo provider-managed template synchronization, if operations choose provider-side authoring.
- Self-service business onboarding in the front-office after WebAdmin-assisted onboarding remains stable.
- Expanded catalog/CMS/search/facet performance work for public storefront scale.
- Optional XRechnung export after the ZUGFeRD/Factur-X path is legally validated.
- Additional asynchronous VIES retry and alerting once production ownership is approved.

## Open Decisions

- Exact production object-storage deployment profile and retention/legal-hold policy per deployment.
- Stripe live-mode execution timing, restricted live key permissions, and monitoring/alert recipients.
- DHL account/product contract and return-label payload after final account provisioning.
- E-invoice validation evidence package and legal acceptance criteria for generated artifacts.
- Mobile launch target order across Android, iOS, MacCatalyst, and follow-up platforms.

## Active Handoff Summary

- PostgreSQL remains the preferred/default persistence provider; SQL Server remains supported.
- WebAdmin remains MVC/Razor + HTMX and is the operational priority.
- `Darwin.Web` remains customer-facing and must not expose operator diagnostics.
- Storefront and subscription payment finalization remain verified-webhook-only.
- No provider secrets belong in git, docs, logs, or test output.
