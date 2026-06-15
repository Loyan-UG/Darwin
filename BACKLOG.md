# Darwin Backlog

Reviewed: 2026-06-15

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

This roadmap makes Darwin a complete independent ERP while keeping it integration-ready for customers that already use external business systems. Darwin must keep English names, its current architecture, and its own canonical model. Do not import legacy framework concepts, customer-specific names, vendor-specific naming, or non-English entity names into Darwin.

Status detail for the redesign track lives in [docs/domain-expansion/erp-expansion-master-status.md](docs/domain-expansion/erp-expansion-master-status.md). Capability decisions remain in [docs/domain-expansion/domain-capability-catalog.md](docs/domain-expansion/domain-capability-catalog.md). Purchasing decisions are now tracked in [docs/domain-expansion/purchasing-supplier-lifecycle-design.md](docs/domain-expansion/purchasing-supplier-lifecycle-design.md) and [docs/domain-expansion/supplier-invoice-payables-boundary-design.md](docs/domain-expansion/supplier-invoice-payables-boundary-design.md).

Completed in the ERP expansion track:

- `Mobile and loyalty-safe foundation review`: release-sensitive identity, business access, loyalty, mobile API, and member commerce boundaries were reviewed and guarded before broad domain expansion.
- `Foundation primitives`: external systems and references, source-of-truth markers, custom fields, activity/note/document metadata, number sequences, business events/audit trails, and feature-area visibility foundations are implemented.
- `CRM expansion`: CRM core fields, customer bridge behavior, foundation primitive integration, and WebAdmin CRM exposure are implemented without creating a CRM-owned loyalty ledger.
- `Sales and order document model`: Sales projections, additive order/invoice fields, Sales WebAdmin workspace, lifecycle evidence, quotes, quote-to-order conversion, delivery notes, return orders, and credit notes are implemented over the current `Order` and `Invoice` foundations without parallel Sales invoice or Finance invoice models.
- `Finance and accounting foundation`: finance posting, account mappings, receivables projection, Finance reporting workspace, finance export batches, package generation, durable package storage, WebAdmin export workflow, connector delivery foundation, file-delivery target adapter, and operational file-delivery hardening are implemented.

In progress:

- `Finance/accounting integration`: file-delivery is production-safe when configured. Accounting API target adapters remain blocked until a real target, credential owner, payload mapping, and error contract are selected.
- `Inventory and procurement baseline`: `Warehouse`, `StockLevel`, `StockTransfer`, `Supplier`, `PurchaseOrder`, formal `GoodsReceipt`, and formal `SupplierInvoice` core/admin/posting exist with WebAdmin operational coverage. Supplier master, purchase order core hardening, goods receipt inventory reconciliation, supplier invoice/payables boundary design, supplier invoice core/admin, and supplier invoice posting are complete; supplier contacts/documents, supplier payment settlement, and warehouse task depth still need the ERP expansion sequence below.

Next gate:

1. `Supplier Payment Boundary Design Slice`: define supplier payment settlement, partial payments, payable aging, payment posting, export impact, and reversal policy before any payment implementation, without reusing customer payment/refund flows.

Remaining ERP expansion order:

1. `Purchasing Documents And Supplier Contacts`: add structured supplier contacts and purchasing document metadata exposure after supplier payment ownership and visibility are stable.
2. `Inventory Ledger, Warehouse Tasks, And Mobile-First Warehouse PWA`: extend warehouse structure, bins, stock ledger, reservations, counts, lots, serials, handling units, receiving tasks, picking tasks, and mobile-first warehouse workflows. Use a PWA as the default warehouse surface unless device/offline requirements require native mobile.
3. `Supplier Invoice And Payables`: supplier invoice core/admin/posting is implemented; next design supplier payment settlement before implementation.
4. `HR And Time Tracking`: define employee, department, position, employment contract, personnel file, work schedule, attendance event, time entry, absence, leave request, timesheet, and payroll-period concepts. Payroll implementation remains later-phase unless a deployment requires it.
5. `AI-Readiness And Automation Governance`: prepare sensitive-field classification, scoped data access, recommendation records, action drafts, and approval-required AI execution paths. AI must propose or draft operational actions before normal application commands execute them.
6. `SyncState And SyncConflict`: design and implement two-way sync only after concrete inbound reconciliation needs exist. Outbound export and external references are already available; they are not a full sync engine.
7. `Accounting API Target Adapter`: implement only after a real target, credential owner, payload mapping, retry policy, and error contract are selected. Until then, finance export file-delivery remains the production-safe outbound path.

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
