# Darwin Backlog

Reviewed: 2026-06-17

This is the active roadmap. Historical implementation notes belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). Code-backed readiness belongs in [docs/go-live-status.md](docs/go-live-status.md) and [docs/module-audit.md](docs/module-audit.md).

## Current Go-Live Blockers

These items require external credentials, deployment configuration, provider accounts, legal/compliance evidence, signed artifacts, device validation, or explicit operational approval.

- `Stripe live readiness`: test-mode checkout, public-webhook finalization, subscription finalization, refund reconciliation, and dispute follow-up are implemented and have staging evidence. Local handoff and public-webhook finalized test-mode smoke passed on 2026-05-27. Before production traffic, run the live-readiness preflight, configure live keys and live webhook events securely, verify ProviderCallbackWorker processing, monitoring, alerting, refund/dispute playbooks, and complete an approved live-mode smoke.
- `DHL live validation`: the real client path, provider-operation queue, label persistence, return-label queueing, callback ingestion, WebAdmin recovery surfaces, and focused internal DHL/shipping tests exist. Final account, billing, product-code, shipper/receiver, label, tracking, and return-label validation remain blocked until complete DHL account data is available.
- `Brevo production operations`: DB-backed email routing, role sender addresses, masked credentials, webhook Basic Auth, branded HTML templates, Site Settings-backed smoke loading, and callback processing exist. Production still needs ongoing inbox-placement checks, DNS/template monitoring, webhook delivery monitoring, and operational alert ownership.
- `VAT/VIES production monitoring`: controlled valid/invalid/provider-failure live smoke passed, provider-failure mapping remains guarded, and `Manual Review + Scheduled Retry` is implemented. Production still needs periodic smoke ownership and monitoring of the manual-review queue and critical retry alerts.
- `Production object storage`: MinIO is the selected first production target through the S3-compatible provider; Azure Blob remains the next provider-hardening target after MinIO evidence is complete, and AWS S3 remains a supported alternative. Local MinIO smoke passed again on 2026-05-27 and production-like provider smoke now requires explicit operator confirmation for profiles, disposable prefix, retention/delete behavior, and runbook ownership. The production readiness evidence package is documented and has a local validation script, but production archive immutability is blocked until the real provider has TLS, dedicated least-privilege keys, versioning, Object Lock or equivalent retention/legal hold, backup, restore testing, monitoring, selected-provider smoke, and recorded deployment evidence.
- `E-invoice compliance`: structured invoice source-model JSON, minimum source-readiness validation, and a provider-neutral `IEInvoiceGenerationService` boundary now exist. The first tooling path is selected as Mustangproject CLI through the external-command adapter, and local adapter smoke passed for XRechnung XML plus ZUGFeRD/Factur-X PDF artifact shape with required validation reports on 2026-05-27. German acceptance, customer rollout, and production readiness evidence package rules are documented. ZUGFeRD/Factur-X and XRechnung must both be production-ready before compliant e-invoice rollout is treated as complete. Full compliance is blocked until deterministic/legal validation fixtures, production artifact download/storage smoke, and operator/legal sign-off are complete for both formats.
- `Mobile store launch`: Android is the selected first launch target. Implemented Consumer and Business workflows are guarded and usable, but signed Android release artifacts, production Google Maps/Firebase configuration, push/device smoke, physical camera QR validation, and broader Android UI coverage remain required before Android launch. iOS and MacCatalyst follow after Android launch evidence is complete.
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
- Complete e-invoice deterministic fixtures, legal validation evidence, production artifact smoke, and production readiness evidence package references for both ZUGFeRD/Factur-X and XRechnung before exposing compliant artifacts.
- Add deeper Consumer and Business ViewModel/UI coverage for auth, profile, business access-state gates, rewards/campaigns, member-commerce invoice artifacts, push registration, and account deletion.
- Add native Consumer Google sign-in first for Android after the Android OAuth client ID is configured, then validate account linking, new-user registration, logout, and failure states on a physical device/emulator. iOS follows after Android launch evidence.
- Configure the Web OAuth client ID, then run browser smoke for Web Google sign-in and session-cookie handoff.
- Validate signed mobile release artifacts and production push/maps configuration outside the repository.
- Keep business subscription and customer checkout in web/back-office workflows for first mobile launch; mobile apps show status and handoff only.
- Decide whether to activate mobile SQLite outbox beyond inactive scaffolding. Do not enable offline mutations without processor, idempotency, cleanup, and support visibility.

## ERP Domain Expansion Roadmap

This roadmap makes Darwin a complete independent ERP while keeping it integration-ready for customers that already use external business systems. Darwin must keep English names, its current architecture, and its own canonical model. Do not import legacy framework concepts, customer-specific names, vendor-specific naming, or non-English entity names into Darwin.

Status detail for the redesign track lives in [docs/domain-expansion/erp-expansion-master-status.md](docs/domain-expansion/erp-expansion-master-status.md). Capability decisions remain in [docs/domain-expansion/domain-capability-catalog.md](docs/domain-expansion/domain-capability-catalog.md). Purchasing and warehouse decisions are now tracked in [docs/domain-expansion/purchasing-supplier-lifecycle-design.md](docs/domain-expansion/purchasing-supplier-lifecycle-design.md), [docs/domain-expansion/inventory-warehouse-task-pwa-design.md](docs/domain-expansion/inventory-warehouse-task-pwa-design.md), [docs/domain-expansion/warehouse-picking-task-boundary-design.md](docs/domain-expansion/warehouse-picking-task-boundary-design.md), [docs/domain-expansion/stock-count-boundary-design.md](docs/domain-expansion/stock-count-boundary-design.md), [docs/domain-expansion/supplier-invoice-payables-boundary-design.md](docs/domain-expansion/supplier-invoice-payables-boundary-design.md), [docs/domain-expansion/supplier-payment-boundary-design.md](docs/domain-expansion/supplier-payment-boundary-design.md), [docs/domain-expansion/supplier-payment-reversal-bank-treasury-boundary-design.md](docs/domain-expansion/supplier-payment-reversal-bank-treasury-boundary-design.md), [docs/domain-expansion/bank-treasury-foundation-design.md](docs/domain-expansion/bank-treasury-foundation-design.md), [docs/domain-expansion/supplier-payment-bank-settlement-boundary-design.md](docs/domain-expansion/supplier-payment-bank-settlement-boundary-design.md), and [docs/domain-expansion/returned-transfer-duplicate-payment-boundary-design.md](docs/domain-expansion/returned-transfer-duplicate-payment-boundary-design.md). HR, time, and payroll decisions are tracked in [docs/domain-expansion/hr-time-tracking-boundary-design.md](docs/domain-expansion/hr-time-tracking-boundary-design.md), [docs/domain-expansion/payroll-legal-calculation-boundary-design.md](docs/domain-expansion/payroll-legal-calculation-boundary-design.md), [docs/domain-expansion/payroll-payment-treasury-boundary-design.md](docs/domain-expansion/payroll-payment-treasury-boundary-design.md), and [docs/domain-expansion/payroll-returned-transfer-correction-boundary-design.md](docs/domain-expansion/payroll-returned-transfer-correction-boundary-design.md).

Completed in the ERP expansion track:

- `Mobile and loyalty-safe foundation review`: release-sensitive identity, business access, loyalty, mobile API, and member commerce boundaries were reviewed and guarded before broad domain expansion.
- `Foundation primitives`: external systems and references, source-of-truth markers, custom fields, activity/note/document metadata, number sequences, business events/audit trails, and feature-area visibility foundations are implemented.
- `CRM expansion`: CRM core fields, customer bridge behavior, foundation primitive integration, and WebAdmin CRM exposure are implemented without creating a CRM-owned loyalty ledger.
- `Sales and order document model`: Sales projections, additive order/invoice fields, Sales WebAdmin workspace, lifecycle evidence, quotes, quote-to-order conversion, delivery notes, return orders, and credit notes are implemented over the current `Order` and `Invoice` foundations without parallel Sales invoice or Finance invoice models.
- `Finance and accounting foundation`: finance posting, account mappings, receivables projection, Finance reporting workspace, finance export batches, package generation, durable package storage, WebAdmin export workflow, connector delivery foundation, file-delivery target adapter, and operational file-delivery hardening are implemented.
- `HR and time tracking`: employee/personnel ownership, `BusinessMember` linkage, department/position separation from security roles, employment metadata, personnel document privacy, work schedule, attendance, time entry, timesheet approval, leave/absence, payroll-period summary, legal payroll calculation boundary, payroll rule foundations, payroll run snapshots, payslip artifacts, payroll liability posting, payroll payment/treasury boundary, and mobile/public boundaries are locked. HR core master data, personnel document upload/download/retention privacy controls, internal WebAdmin time/attendance workflows, leave/absence workflows, payroll-period export summaries, Germany-first country/version-aware legal payroll boundary design, payroll rule set/component foundations, payroll run core, internal payslip artifact generation with versioned PDF output, journal-backed payroll liability posting, payroll payment/treasury boundary design, payroll payment core/admin, payroll payment full-reversal, payroll bank settlement through matched reconciliation evidence, payroll returned-transfer correction core, payroll provider adapter boundary design, employee payslip self-service boundary design, and employee payslip self-service core implementation are complete. The self-service route is additive, authenticated, scoped to the linked employee, exposes official PDF only, audits downloads, includes safe high-level payment status, and does not expose provider submission, statutory remittance, WebAdmin routes, customer/supplier payment mutations, bank API, journal ids, or credential UI.
- `AI governance foundation`: AI-readiness boundary design, core Foundation schema, internal WebAdmin review workspace, scoped context projection foundation, AI provider adapter boundary design, provider-neutral adapter foundation, target-provider selection design, action-handoff execution boundary design, action-handoff foundation, low-risk internal timeline executor, internal follow-up task executor, broader module review-routing decision, module review routing executor, and AI/integration no-target checkpoint are implemented for sensitive-field policy, deny-by-default scoped access, recommendation records, action drafts, approval evidence, safe metadata validation, event evidence, provider-neutral operation, recommendation accept/dismiss/expire review, action draft submit/approve/reject review, aggregate purpose-bound module context for Sales, Finance, Purchasing, Inventory, HR, Payroll, and Treasury, provider credential/prompt/logging/retry boundaries, internal adapter orchestration over governed recommendations/drafts, deployment-owned provider target selection, execution handoff ownership, typed executor registry infrastructure, approved AI draft execution into internal notes/activities, approved AI draft execution into internal follow-up tasks, approved AI draft execution into internal module review tasks, and explicit deferral of real provider, direct operational executor, two-way sync, and accounting API adapter until their targets and policies are selected. No real model-provider implementation, prompt execution against external services, autonomous mutation, operational module command executor, public/mobile route, finance export format change, payment/refund flow, payroll provider submission, bank API, journal editor, stock movement, shipment mutation, or invoice archive/download behavior is added.

In progress:

- `Finance/accounting integration`: file-delivery is production-safe when configured. Accounting API target adapters remain blocked until a real target, credential owner, payload mapping, and error contract are selected.
- `Inventory, procurement, and treasury baseline`: `Warehouse`, structured `WarehouseLocation` bins/locations, `WarehouseLabelTemplate` print/download readiness, `WarehouseTask`, `WarehouseTaskLine`, `StockCountSession`, `StockCountLine`, `StockLevel`, `StockTransfer`, `Supplier`, `SupplierContact`, `PurchaseOrder`, formal `GoodsReceipt`, formal `SupplierInvoice`, formal `SupplierPayment`, `SupplierAdvance`, `BankAccount`, `BankStatementImport`, `BankStatementLine`, and `BankReconciliationMatch` core/admin foundations exist with WebAdmin operational coverage. Supplier master, structured contacts, supplier document metadata, purchase order core hardening, goods receipt inventory reconciliation, warehouse location/bin core, warehouse label template/printing readiness, inventory movement reference hardening, warehouse task/PWA boundary design, warehouse task foundation, warehouse receiving/putaway task execution, warehouse picking boundary design, warehouse picking task core, warehouse picking shortage attention, stock count boundary design, stock count core/admin, lot/serial/handling unit boundary design, lot/serial/handling unit core/admin, receipt identity capture, receipt inline identity creation, transfer/count/pick identity integration, bin-level stock derivation, internal/operator warehouse PWA, supplier invoice/payables boundary design, supplier invoice core/admin, supplier invoice posting, supplier payment boundary design, supplier payment core/admin, supplier payment reversal/bank-treasury boundary design, supplier payment reversal implementation, bank/treasury foundation design, bank/treasury core model, bank reconciliation core, supplier bank settlement boundary design, supplier bank settlement core, returned/duplicate transfer boundary design, returned/duplicate correction core, supplier advance/overpayment boundary design, supplier advance core/admin, and supplier advance reversal/correction hardening are complete.

Next gate:

1. `Production go-live evidence execution`: execute the production-like staging rehearsal first, then populate deployment-specific evidence for MinIO, dual-format e-invoice, Android-first mobile launch, and provider smoke ownership. Use [docs/production-go-live-evidence-execution-plan.md](docs/production-go-live-evidence-execution-plan.md) and the evidence package validator. Code-backed `SyncState`/`SyncConflict` foundation is complete; target-specific inbound sync still waits for a concrete integration target.

Remaining ERP expansion order:

1. `Production-Like Staging Evidence`: generate and fill a deployment evidence package for the production-like candidate, rehearse build/test, migration, rollback, storage, e-invoice, Android, provider, monitoring, and owner approvals before production execution.
2. `MinIO Production Evidence`: execute selected-provider readiness and smoke against the real target bucket/container with retention/legal-hold, backup/restore, monitoring, and evidence-package ownership.
3. `Azure Object Storage Readiness Hardening`: after MinIO evidence, prepare Azure Blob production readiness using the existing Azure provider boundary without changing application storage owners.
4. `Dual E-Invoice Production Evidence`: make both ZUGFeRD/Factur-X and XRechnung legally/operationally accepted for German rollout with deterministic fixtures, validation reports, storage/download smoke, and sign-off.
5. `Android-First Mobile Launch Evidence`: validate signed Android artifacts, push/maps configuration, Google sign-in when enabled, and physical QR/device smoke before iOS and MacCatalyst.
6. `Target-Specific Sync Integration`: use the implemented `SyncState`/`SyncConflict` foundation only after a concrete inbound/two-way target and conflict workflow are selected.
7. `Accounting API Target Adapter`: implement only after a real target, credential owner, payload mapping, retry policy, and error contract are selected. Future target selection should follow `docs/domain-expansion/finance-export-accounting-api-target-selection-design.md` and prioritize widely used German accounting software while file-delivery remains production-safe.
8. `AI Target Provider Adapter`: lower priority; implement only after a real provider/model target, credential owner, payload mapping, rate/cost policy, retry policy, safe error contract, and smoke strategy are selected.
9. `Operational Module Executor Design`: choose one concrete command family before allowing AI-assisted execution beyond internal evidence and review task routing.

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
- Keep ZUGFeRD/Factur-X and XRechnung production evidence aligned; both formats need deterministic fixtures, validation reports, storage/download smoke, and accounting/tax sign-off before compliant German rollout is treated as complete.
- Add persisted operational-alert aggregation if structured logs, WebAdmin queues, and email audits are not enough for production support.

## Open Decisions

- Exact MinIO production object-storage deployment profile, retention/legal-hold policy, and evidence package owner per deployment; Azure Blob readiness follows after MinIO evidence.
- Stripe live-mode execution timing, restricted live key permissions, and monitoring/alert recipients.
- DHL account/product contract and return-label payload after final account provisioning.
- E-invoice validation evidence package, legal acceptance criteria, and production readiness evidence references for generated ZUGFeRD/Factur-X and XRechnung artifacts.
- Android release timing and evidence owner before iOS, MacCatalyst, and follow-up platforms.

## Active Handoff Summary

- PostgreSQL remains the preferred/default persistence provider; SQL Server remains supported.
- WebAdmin remains MVC/Razor + HTMX and is the operational priority.
- `Darwin.Web` remains customer-facing and must not expose operator diagnostics.
- Storefront and subscription payment finalization remain verified-webhook-only.
- No provider secrets belong in git, docs, logs, or test output.
