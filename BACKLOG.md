# Darwin Backlog

Reviewed: 2026-06-11

This is the active roadmap. Historical implementation notes belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). Code-backed readiness belongs in [docs/go-live-status.md](docs/go-live-status.md) and [docs/module-audit.md](docs/module-audit.md).

## Current Go-Live Blockers

These items require external credentials, deployment configuration, provider accounts, legal/compliance evidence, signed artifacts, device validation, or explicit operational approval.

- `Stripe live readiness`: test-mode checkout, public-webhook finalization, subscription finalization, refund reconciliation, and dispute follow-up are implemented and have staging evidence. Local handoff and public-webhook finalized test-mode smoke passed on 2026-05-27. Before production traffic, run the live-readiness preflight, configure live keys and live webhook events securely, verify ProviderCallbackWorker processing, monitoring, alerting, refund/dispute playbooks, and complete an approved live-mode smoke.
- `DHL live validation`: the real client path, provider-operation queue, label persistence, return-label queueing, callback ingestion, WebAdmin recovery surfaces, and focused internal DHL/shipping tests exist. Final account, billing, product-code, shipper/receiver, label, tracking, and return-label validation remain blocked until complete DHL account data is available.
- `Brevo production operations`: DB-backed email routing, role sender addresses, masked credentials, webhook Basic Auth, branded HTML templates, Site Settings-backed smoke loading, and callback processing exist. Production still needs ongoing inbox-placement checks, DNS/template monitoring, webhook delivery monitoring, and operational alert ownership.
- `VAT/VIES production monitoring`: controlled valid/invalid/provider-failure live smoke passed, provider-failure mapping remains guarded, and `Manual Review + Scheduled Retry` is implemented. Production still needs periodic smoke ownership and monitoring of the manual-review queue and critical retry alerts.
- `Production object storage`: MinIO is the recommended self-hosted target through the S3-compatible provider; AWS S3 and Azure Blob are alternatives. Local MinIO smoke passed again on 2026-05-27 and production-like provider smoke now requires explicit operator confirmation for profiles, disposable prefix, retention/delete behavior, and runbook ownership, but production archive immutability is blocked until the real provider has TLS, dedicated least-privilege keys, versioning, Object Lock or equivalent retention/legal hold, backup, restore testing, monitoring, and selected-provider smoke.
- `E-invoice compliance`: structured invoice source-model JSON, minimum source-readiness validation, and a provider-neutral `IEInvoiceGenerationService` boundary now exist. The first tooling path is selected as Mustangproject CLI through the external-command adapter, and local adapter smoke passed for XRechnung XML plus ZUGFeRD/Factur-X PDF artifact shape with required validation reports on 2026-05-27. German acceptance and customer rollout checklists are documented, but full ZUGFeRD/Factur-X compliance is blocked until deterministic/legal validation fixtures, production artifact download/storage smoke, and operator/legal sign-off are complete. XRechnung remains secondary unless a deployment requires it earlier.
- `Mobile store launch`: implemented Consumer and Business workflows are guarded and usable, but signed Android/iOS/MacCatalyst release artifacts, production Google Maps/Firebase/APNS configuration, push/device smoke, physical camera QR validation, and broader device UI coverage remain required.
- `External identity`: Google external-login backend, Site Settings configuration, WebApi endpoint, Web Google Identity Services handoff, and shared mobile service route are implemented. Web OAuth client ID, native mobile Google UI, iOS client ID, and device/browser smoke remain required before this is a launch feature.

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
- Repeat Stripe test-mode smoke after payment, subscription, webhook, or front-office confirmation changes, then run the live-readiness preflight before any live-mode validation.
- Keep DHL parked until final account/product data arrives, then run the guarded live smoke and validate label storage, tracking, returns, callbacks, and WebAdmin recovery.
- Repeat Brevo inbox-placement checks after sender-domain, template, or provider changes; monitor webhook/callback and failed-send queues.
- Repeat VIES live smoke after VIES provider, retry, or policy changes; keep provider failure as `Unknown`/manual review.
- Keep local MinIO smoke in the release checklist; run production MinIO readiness and selected-provider smoke only against the final target bucket.
- Complete e-invoice deterministic fixtures, legal validation evidence, and production artifact smoke before exposing compliant artifacts.
- Add deeper Consumer and Business ViewModel/UI coverage for auth, profile, business access-state gates, rewards/campaigns, member-commerce invoice artifacts, push registration, and account deletion.
- Add native Consumer Google sign-in after Android/iOS OAuth client IDs are configured, then validate account linking, new-user registration, logout, and failure states on a physical device/emulator.
- Configure the Web OAuth client ID, then run browser smoke for Web Google sign-in and session-cookie handoff.
- Validate signed mobile release artifacts and production push/maps configuration outside the repository.
- Keep business subscription and customer checkout in web/back-office workflows for first mobile launch; mobile apps show status and handoff only.
- Decide whether to activate mobile SQLite outbox beyond inactive scaffolding. Do not enable offline mutations without processor, idempotency, cleanup, and support visibility.

## ERP Domain Expansion Roadmap

This roadmap captures the first planning track for making Darwin a complete independent ERP while keeping it integration-ready for customers that already use an external ERP, CRM, accounting, warehouse, payroll, or time-tracking system. Darwin must keep English names, its current architecture, and its own canonical model. Do not import legacy framework concepts, customer-specific names, vendor-specific naming, or non-English entity names into Darwin.

The first implementation step is documentation-only: maintain the capability decisions in [docs/domain-expansion/domain-capability-catalog.md](docs/domain-expansion/domain-capability-catalog.md) before changing entities, migrations, API contracts, mobile contracts, or tests.

Execution order:

1. `Mobile and loyalty-safe foundation review`: identify all future domain changes that touch loyalty/mobile launch dependencies first, including `User`, `Business`, `BusinessMember`, `Customer`, `Address`, order/invoice snapshots, WebApi contracts, and mobile contracts. Make required foundation changes before mobile release rather than shipping a contract that will need disruptive redesign.
2. `Domain capability catalog from product requirements, external-system readiness, and Darwin current model`: classify candidate capabilities as canonical, optional, extension/custom, industry-specific, customer-specific, legacy technical, or deferred.
3. `Canonical English domain design`: translate accepted capabilities into Darwin-owned English concepts and lifecycle rules. CMS and loyalty core stay outside the general ERP redesign unless a shared foundation or mobile-release change is required; required loyalty/mobile changes must be completed before release, not deferred until after launch.
4. `Foundation primitives`: add shared primitives such as external-system references, source-of-truth markers, activity/note, attachment, custom-field metadata, number sequences, and feature-area visibility only after catalog decisions are approved.
5. `CRM expansion`: strengthen account, contact, customer, lead, opportunity, activity, consent, segmentation, and support lifecycle modeling without reintroducing a CRM-owned loyalty ledger.
6. `Sales and order document model`: define quote, order, delivery, invoice, credit, return, pricing, discount, tax, and status-history concepts with immutable snapshots where required.
7. `Purchasing and supplier lifecycle`: define supplier, supplier contact, purchase request, purchase order, goods receipt, supplier invoice, supplier pricing, and purchase terms.
8. `Inventory ledger, warehouse tasks, and mobile/PWA planning`: define warehouse locations, bins, stock items, stock balances, stock ledger entries, reservations, transfers, adjustments, stock counts, lots, serials, handling units, and mobile-first warehouse task workflows. Treat a mobile-first PWA as the default warehouse surface unless device/offline requirements prove a native app is necessary.
9. `Finance and accounting`: define chart of accounts, accounts, journal entries, tax codes, payment terms, receivables, payables, bank transactions, reconciliation, cost centers, and audit-safe financial state transitions.
10. `HR and time tracking`: define employee, department, position, employment contract, personnel file, work schedule, attendance event, time entry, absence, leave request, timesheet, and payroll-period concepts. Payroll implementation remains later-phase unless a deployment requires it.
11. `AI-readiness and automation governance`: prepare audit trails, business events, scoped data access, sensitive-field classification, recommendation records, action drafts, and approval-required AI execution paths. AI must propose or draft operational actions before normal application commands execute them.

Storage rules:

- Use real columns for common, reportable, filterable, compliance-relevant, accounting-relevant, inventory-relevant, integration-key, or cross-module fields.
- Use custom fields or JSON for customer-specific, uncertain, low-frequency, provider-specific payload, industry-specific, or unstructured metadata.
- Keep `ExternalSystem`, `ExternalReference`, `SourceOfTruth`, `SyncState`, and `SyncConflict` visible in the design for integration with external ERP/CRM/accounting systems even when Darwin is the primary ERP.
- Keep module separation logical through UI navigation, permissions, feature visibility, and clear ownership rules. Do not split projects or databases solely to model modules.

## Later-Phase Tasks

- Carrier-integrated DHL RMA/returns automation beyond the current return-label queue path, returns queue, and shipment provider operations.
- Brevo provider-managed template synchronization, if operations choose provider-side authoring.
- Self-service business onboarding in the front-office after WebAdmin-assisted onboarding remains stable.
- Expanded catalog/CMS/search/facet performance work for public storefront scale.
- Optional XRechnung export after the ZUGFeRD/Factur-X path is legally validated.
- Add persisted operational-alert aggregation if structured logs, WebAdmin queues, and email audits are not enough for production support.

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
