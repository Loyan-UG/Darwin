# Darwin WebAdmin Guide

Reviewed: 2026-05-26

`Darwin.WebAdmin` is the operational back-office for Darwin. It is the primary control surface for business onboarding, support, provider readiness, billing, communication, inventory, CRM, loyalty, mobile operations, and compliance review workflows.

Historical progress notes belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). Current readiness status belongs in [docs/go-live-status.md](docs/go-live-status.md).

## Architecture

- ASP.NET Core MVC/Razor with HTMX.
- Operator-only UI. Do not move diagnostics, readiness dashboards, review queues, or back-office wording into public/member UI.
- Controllers should keep request/response orchestration thin and delegate business rules to Application handlers.
- Use localized resources for visible operator text.
- Use row-version protected posts for mutations.
- Keep anti-forgery validation on form posts.
- Keep provider credentials masked or hidden.

## Core Workspaces

| Workspace | Purpose |
| --- | --- |
| Dashboard | Compact operational command center with high-value summaries and attention items. |
| Businesses | Business profile, lifecycle, approval, suspension, reactivation, members, invitations, locations, and setup readiness. |
| Merchant readiness / onboarding | Admin-assisted setup checklist that links to existing profile, plan, users, locations, loyalty, communications, visibility, and review surfaces. |
| Billing | Payments, refunds, disputes, subscriptions, webhook deliveries, tax/compliance review, and provider reconciliation. |
| Finance | Receivables, journal posting, credit-note reconciliation reporting, supplier invoice/payment workflows, finance export, and internal bank/treasury evidence over the existing Billing finance foundation. |
| BusinessCommunications | Email/channel dispatch, provider callbacks, failed sends, retry decisions, and communication readiness. |
| MobileOperations | Device registration, push token state, stale/disabled notification review, and mobile support handoffs. |
| CRM | Customers, leads, opportunities, invoices, invoice archive/source-model/e-invoice artifact actions, and support workflows. |
| Sales | Commercial sales workspace for read-only order/invoice overview plus internal quote creation, quote lifecycle, and conversion linkage into existing operational order workflows. |
| Inventory | Warehouses, locations/bins, suppliers, purchase orders, goods receipts, stock transfers, reservations, returns, and ledger review. |
| HR | Employee/personnel master data, departments, positions, employment metadata, personnel document metadata, time, attendance, leave, timesheets, and payroll-period summary workflows as internal HR operations. |
| Shipping | Shipments, returns, provider operations, label handling, tracking, and carrier exception recovery. |
| Site Settings | Secure operational configuration and secret-free provider readiness. |

## Business Onboarding

WebAdmin owns the first-launch business onboarding path:

- New businesses start inactive and pending approval.
- Operators can create, review, approve, suspend, and reactivate businesses.
- Approval/reactivation requires minimum setup prerequisites such as owner, legal/contact data, and primary location.
- Invitation preview/acceptance supports pending onboarding without allowing suspended/unavailable businesses.
- Invitation resend/revoke actions are status-aware.
- The onboarding wizard summarizes existing workspaces and does not bypass their validation.

Future self-service onboarding in `Darwin.Web` must reuse the same backend rules instead of creating a parallel policy.

## Provider Operations

WebAdmin must expose provider state without exposing provider secrets:

- Stripe: payment, subscription, refund, dispute, webhook, and reconciliation queues.
- DHL: shipment, label, return-label, tracking, failed/stale provider-operation recovery.
- Brevo: dispatch audits, provider callbacks, failed sends, retry and support handoffs.
- Object storage: configured provider/profile status and safe smoke action.
- VIES/e-invoice: review queues, validation state, and operator-visible blockers.

Provider-specific live readiness is not complete until the matching provider smoke and operational monitoring pass.

## Billing And Subscription Policy

- Storefront and business payments use provider-hosted checkout.
- Browser return routes must not finalize payment or subscription state.
- Verified provider webhooks are authoritative.
- Business mobile subscription management is read-only for first launch; plan purchase, cancellation, SEPA mandate setup, and manual payment registration are web/back-office workflows.
- WebAdmin must show provider references, status, failures, and reconciliation state without exposing secret keys.

## Sales Workspace

- Sales is a commercial workspace over the current `Order` and `Invoice` foundation plus internal `SalesQuote` and `DeliveryNote` documents.
- Orders remains the operational fulfillment, payment, shipment, refund, and invoice-creation workspace.
- CRM invoice screens remain the operational invoice editing, archive, structured export, and artifact workspace.
- Sales must not introduce parallel order or invoice mutation paths; it links to existing operational flows.
- Sales quote mutations are allowed only for quote drafts and quote lifecycle: create draft, update draft, send, accept, reject, expire, create an order from an accepted quote, and link an accepted quote to an existing order.
- Sales quote order creation uses the current `Order` and `OrderLine` model with quote-time snapshots. It does not create a parallel Sales order model.
- Non-catalog quote lines can convert to order lines without fake products; these order lines have no product variant and are not treated as warehouse stock movement lines.
- Delivery notes are formal internal/WebAdmin documents created from existing shipments. Their line quantities come from `ShipmentLine.Quantity`; the UI must not accept manual delivery-note quantities.
- Delivery note lifecycle mutations are limited to create from shipment, prepare, issue, mark shipped, mark delivered, and cancel. They must not create parallel shipment, payment, refund, invoice, or inventory mutation paths.
- Delivery note workflow rules belong in Application policy/services so customer-specific variation can be introduced without forking controllers or views.
- Return orders are formal internal/WebAdmin Sales documents for request, approval, return-shipment linkage, receipt, inspection, refund readiness, refund linkage, and closure.
- Return order refund and restock eligibility is available only after receipt and inspection. Request, approval, or return-label creation alone must not trigger refund or stock movement.
- Return order restock uses the existing inventory return receipt owner for variant-backed accepted quantities. Non-catalog lines can be returned as commercial evidence but do not create stock movement.
- Return order refund settlement stays in the existing refund/payment flows; Sales only links completed refund records back to the return.
- Return order refund reconciliation shows linked and remaining amounts. Partial completed refund linkage keeps the return refund-ready; exact eligibility coverage marks it refunded; over-linking, wrong-currency, wrong-order, pending, failed, or deleted refunds are rejected.
- Return order restock retry is guarded by the return id reference so repeated inspection attempts do not create duplicate stock movement.
- Return orders must not create credit notes, negative invoices, parallel refund ledgers, or parallel shipment/payment/invoice mutations.
- Credit notes are formal internal/WebAdmin Sales/Finance documents. They are not negative invoices, refund actions, return-order printouts, or UI-only documents. Credit-note issue reserves legal numbering, validates cumulative credit, captures immutable source/archive evidence, posts receivable/revenue/tax reversal, and records lifecycle evidence.
- Credit-note reconciliation is hardened in WebAdmin: duplicate line input is aggregated, refund evidence must reconcile to the linked invoice through payment/order/currency boundaries, line prefill uses remaining creditable issued-invoice quantities, and source-model download is internal-only for issued/voided documents with stored source hashes.
- Finance posting evolves the existing Billing journal entry foundation. Manual journal entries remain Billing operations; automated posting has source linkage, idempotency, posting lifecycle/status, safe internal posting service support, business-scoped account-role mappings, read-only receivables projection, and invoice/payment/refund/cancellation/credit-note posting wiring. WebAdmin finance configuration/reporting UI is not exposed until it can include authorization, validation, and operator UX as a complete slice.
- Finance reporting is exposed as a separate workspace with overview, receivables, postings, account-mapping readiness pages, export workflow, supplier invoice/payment operations, and bank/treasury evidence pages. Reporting remains projection-oriented where it is reading posted finance state; account mapping, supplier invoice/payment lifecycle, bank account master data, bank statement import evidence, and bank reconciliation evidence are the only Finance-owned mutations in this workspace. Billing remains the owner for financial account creation/editing and manual journal entry mutations.
- Finance export and accounting integration packages posted journal entries from the existing Billing finance foundation. Finance Exports is an internal/operator-only workflow with batch identity, safe retry evidence, durable object-storage package files, and `DocumentRecord` metadata; it does not export mutable operational documents directly or create public/mobile/storefront contracts.
- Finance export generation stores the canonical package before marking a batch generated. Operators can download stored packages from Finance, but connector push, journal editing, invoice/payment/refund/credit-note mutations, and operational document export shortcuts do not belong in this workspace.
- Finance export connector push stays behind the provider-neutral adapter foundation. The internal push action reads the stored package, records target-side ids through `ExternalReference`, stores only safe retry evidence, and remains outbound-only. `SyncState`/`SyncConflict` foundation exists for future inbound/two-way targets, but file-delivery does not use it as a fake accounting API integration. Production WebAdmin keeps push blocked unless a real accounting connector adapter is registered; no no-network adapter may mark a batch delivered at runtime.
- Finance export file-delivery is the first implemented live connector target. It copies only the stored canonical export package to the configured `FinanceExportOutbound` destination and reports success only after destination write and package-hash verification. Production WebAdmin enables push only when that outbound profile is configured with a non-database object-storage provider. It does not add connector credential forms, package regeneration, journal editing, invoice/payment/refund/credit-note mutations, public routes, or mobile/storefront contracts.
- Finance export deployment readiness requires both `FinanceExports` and `FinanceExportOutbound` object-storage profiles when export push is in scope. `FinanceExports` is the source package profile; `FinanceExportOutbound` is the delivery destination. Missing or database-backed outbound configuration keeps push blocked rather than creating fake delivered batches.
- Production readiness evidence is recorded through the deployment evidence package in `docs/production-readiness-evidence-package.md`. WebAdmin smoke actions, provider queues, finance export package status, e-invoice artifact visibility, personnel document storage, payroll payslip storage, and object-storage status are operational evidence surfaces; they do not replace selected-provider retention/legal-hold validation, legal/tax sign-off, credential ownership, monitoring ownership, rollback planning, or customer approvals outside source control.
- Sales lifecycle evidence comes from `BusinessEvent` and `AuditTrail` recorded by the existing order, payment, shipment, refund, and invoice command handlers.
- Sales events are for traceability, reporting, and automation context; they are not a second source of operational state.
- Sales must not expose provider secrets, raw payment payloads, archive object credentials, or mobile-only contracts.

## Inventory And Purchasing Workspace

- Inventory remains the internal/WebAdmin owner for warehouses, stock levels, stock transfers, suppliers, purchase orders, goods receipts, and inventory ledger review.
- Supplier master data is business-scoped and uses the current `Supplier` model with code, status, preferred currency, payment terms, lead time, tax reference, website, and operational notes. WebAdmin must not introduce a parallel supplier model.
- Supplier contacts are structured `SupplierContact` records under the existing supplier master. WebAdmin supports contact create, update, archive, role selection, primary-contact flag, and row-version protection; contacts must not be hidden in supplier notes or duplicated into invoice/payment/warehouse-task records.
- Supplier document metadata is registered through the shared `DocumentRecord` foundation and displayed in the supplier editor. This surface is metadata-only; it must not add supplier invoice/payment shortcuts, binary upload/download ownership, public/mobile routes, or finance export changes.
- Purchase orders use the current `PurchaseOrder` and `PurchaseOrderLine` models. Manual purchase order numbers are allowed, but empty purchase order numbers must reserve `NumberSequenceDocumentType.PurchaseOrder`; fake fallback numbers are not acceptable.
- Purchase order lifecycle mutations are limited to the current owner flow: create/update draft, issue, receive, and cancel. Row-version and anti-forgery protections must remain on mutations.
- Goods receipts are formal internal/WebAdmin documents for receiving, inspection, and inventory posting. Receipt numbers are reserved on receive, not while the receipt is only draft.
- Goods receipt line quantities are controlled by lifecycle stage: received quantity in receive, accepted/rejected/damaged quantity in inspection, and accepted quantity only in post-to-inventory.
- Goods receipt posting delegates stock changes to existing stock-level and `InventoryTransaction` owners with `GoodsReceiptPosted` and `ReferenceId = GoodsReceiptId`; it must not create a parallel stock ledger.
- Legacy purchase order receive behavior stays compatible by creating a formal posted goods receipt behind the existing purchase order action.
- Goods receipt detail can create formal warehouse receiving tasks before posting and directed putaway tasks after posting. Putaway tasks use posted accepted receipt quantities and active warehouse locations as execution evidence; they do not replace the goods receipt lifecycle or create stock movement by themselves.
- Warehouse locations and bins are managed through `WarehouseLocation` under the current `Warehouse` foundation. WebAdmin supports list, hierarchy, detail, create, edit, and archive with row-version checks. This surface stores code, display name, type, status, barcode, sort order, description, and safe metadata only.
- Warehouse label templates are managed as provider-neutral `WarehouseLabelTemplate` records. WebAdmin supports template list/create/edit/archive, location/bin label preview, browser print, and download package output using existing location code/display/barcode fields.
- Warehouse location/bin label UI must not store stock quantity, create inventory movement, create warehouse tasks, create stock counts, mutate supplier invoice/payment, mutate finance postings, add printer credentials, push to printer hardware, or expose public/mobile routes. Printer-specific integration requires a concrete target and a separate adapter slice.
- Warehouse depth is designed in [docs/domain-expansion/inventory-warehouse-task-pwa-design.md](docs/domain-expansion/inventory-warehouse-task-pwa-design.md). WebAdmin remains the review and configuration surface for warehouses, bins, stock levels, transfers, receipts, counts, and task oversight.
- `InventoryTransaction` remains the authoritative stock movement ledger and `StockLevel` remains an availability summary. Future bin, task, lot, serial, count, or PWA workflows must call Application handlers and must not write stock directly.
- Inventory movement references are hardened for current stock owners. WebAdmin inventory actions must preserve the shared reason/reference policy: system-owned movement reasons require an owning aggregate reference, sensitive reason text is rejected, and retry paths must not create duplicate ledger rows or change stock twice.
- Warehouse Tasks are implemented in the Inventory workspace as internal review and planning records. Operators can create, edit, assign, ready, start, complete, and cancel formal `WarehouseTask` records with source document linkage, warehouse/location context, quantities, row-version checks, and event/audit evidence.
- Warehouse Task WebAdmin actions must not post stock movement, mutate goods receipts, create supplier invoice/payment shortcuts, create finance postings, add stock count adjustments, create public/mobile routes, or change public/storefront/customer invoice/payment/refund behavior.
- Putaway task completion must remain handler-guarded by posted goods receipt evidence, source receipt line linkage, accepted quantity, and active destination location.
- Warehouse PWA is implemented as an internal/operator online-first WebAdmin surface. It uses existing warehouse task lifecycle handlers for start, complete, and cancel actions, shows scan/search-friendly task cards, and displays read-only Bin Stock attention derived from current movement and identity evidence.
- Warehouse PWA remains separate from public storefront and Consumer/Business mobile DTOs. It must not add offline mutation queues, service-worker outbox behavior, direct database writes, stock ledger shortcuts, shipment/payment/invoice/refund mutations, supplier finance mutations, or finance export actions. Native app work starts only if scanner, managed-device, or offline requirements prove that PWA is insufficient.
- Picking core is implemented through allocation-backed `WarehouseTask` creation from current orders. Picking task completion validates active order state, source order lines, allocation evidence, and active same-warehouse source locations. Picking WebAdmin/PWA surfaces must not create shipment, payment, invoice, refund, supplier finance, finance export, public/mobile, or storefront mutations.
- Picking shortage attention is implemented as explicit `WarehouseTaskLine` evidence only for picking tasks. WebAdmin may show short quantity, short reason, shortage filters, and attention counts for picking, but it must not auto-cancel orders, auto-refund, substitute items, create shipments, create invoices, change payment state, notify customers, or post stock movement from shortage data.
- Stock count core/admin is complete for internal WebAdmin. Stock Count screens use formal `StockCountSession` and `StockCountLine` records, expected quantity snapshots, variance review, approval, and idempotent adjustment posting through Inventory handlers. They must not edit `StockLevel` directly, create a second ledger, hide lot/serial evidence in metadata, mutate finance/supplier/customer flows, or expose public/mobile routes.
- Lot/serial/handling-unit core admin is complete for internal WebAdmin. Inventory Traceability screens use structured product tracking policy, lot identity, serial unit identity, handling unit identity, and handling unit content records. They must not overload `WarehouseLocation` as a pallet/carton, hide lot/serial/expiry/recall evidence in generic notes, create a second stock ledger, mutate finance/supplier/customer flows, or expose public/mobile routes.
- Goods Receipt inspection now captures structured receipt identity evidence by linking existing lot, serial unit, and handling unit records to `GoodsReceiptLineIdentity` rows. It can also create missing lot, serial unit, and handling-unit records in place for the current receipt line. Posting enforces active product tracking policy requirements before accepted quantity reaches stock. These actions are internal/WebAdmin-only and must not create transfer/count/pick identity mutations, public/mobile routes, finance/supplier/customer mutations, or a second stock ledger.
- Stock Transfer, Stock Count, and Warehouse Task/Picking screens now capture structured identity evidence by linking lot, serial unit, and handling-unit records to line-level identity evidence. Transfer dispatch/receive, stock count approval/posting, and picking completion enforce active product tracking policies before tracked quantities move forward. These actions remain internal/WebAdmin-only and must not create bin stock storage, public/mobile routes, finance/supplier/customer mutations, or a second stock ledger.
- Bin Stock is implemented as an internal/WebAdmin read-only Inventory projection. It derives location/bin, product, lot, serial-unit, and handling-unit availability from current stock levels plus warehouse task, stock count, and identity evidence. It surfaces unassigned quantity and negative evidence as attention, and must not store authoritative bin quantities, create stock movement, mutate finance/supplier/customer flows, add public/mobile routes, or create a second stock ledger.
- Supplier invoice/payables boundary design is complete. WebAdmin must add supplier invoice only through the future formal `SupplierInvoice` core workflow and must not reuse customer-facing `Invoice`, create negative receivables, or add document-only payable shortcuts.
- Supplier invoice core/admin/posting is implemented in the Finance workspace. Operators can create, update, match, approve, post, and void eligible formal supplier invoices linked to suppliers, purchase orders, and goods receipts.
- Supplier payable liability comes only from the supplier invoice posting command and existing finance posting services, not from WebAdmin status text, supplier invoice attachment upload, approval state alone, or manual journal shortcuts.
- Posted supplier invoices link back to read-only Finance posting review. They must not create supplier payment, customer payment, refund, customer invoice, archive/download, or manual journal edit shortcuts.
- Supplier payment core/admin is implemented in the Finance workspace as a formal settlement flow from posted supplier invoices. It supports draft/update/post/cancel-draft, full-payment reversal, partial allocations across one or more posted supplier invoices, cumulative overpayment guards, read-only journal links, and deterministic event/audit evidence.
- Supplier payment posting debits `AccountsPayable` and credits `CashClearing` through finance posting services. Partial supplier payment is allowed, overpayment is blocked by default, and posted supplier payment correction is full-payment reversal only.
- Supplier payment reversal is implemented in WebAdmin only for posted payments. It posts a balanced reversal journal through finance posting services, stores the reversal reason and reversal journal link, and keeps reversed payments out of paid/open-payable totals.
- Bank/treasury foundation core is implemented in the Finance workspace with Bank Accounts, Bank Statements, and Bank Reconciliation pages. Bank accounts are operational treasury identities and do not replace `FinancialAccount`; optional `FinancialAccountId` is mapping evidence only.
- Bank statement imports and lines are evidence/source facts. Bank reconciliation matches link those statement lines to posted or reversed finance facts as evidence only; they do not create journal entries, settle supplier or customer payments, reverse payments, rewrite payment/refund/journal history, or change finance export output.
- Supplier payment bank settlement core is implemented in WebAdmin as an internal, handler-backed, full-payment workflow. It is available only for posted, not reversed, not bank-settled supplier payments with matched zero-difference bank reconciliation evidence and a mapped bank Asset account.
- Supplier payment bank settlement posts through finance services by debiting `CashClearing` and crediting the mapped bank Asset account. The detail page shows settlement readiness, reconciliation evidence, settlement timestamp, and read-only links to the payment posting, reversal posting when present, and bank-settlement posting.
- Returned/failed transfer and duplicate-payment correction core is implemented in the Finance workspace. `SupplierPaymentBankCorrection` is internal, evidence-backed by bank statement and reconciliation, full-settlement in v1 for returned transfers, handler-owned, and unable to rewrite supplier payment, journal, reconciliation, customer payment/refund, or finance export history.
- Duplicate bank movement appears as evidence/attention only and cannot auto-post or auto-reverse.
- Supplier advance core/admin is implemented in the Finance workspace. Operators can create/update draft advances, post standalone advances, cancel drafts, and explicitly apply posted advance balance to posted supplier invoices.
- Supplier advance posting debits `SupplierAdvance` and credits `CashClearing`; advance application debits `AccountsPayable` and credits `SupplierAdvance`. Both use finance posting services with deterministic posting keys and read-only journal links.
- Supplier advance reversal hardening is implemented. Operators can reverse posted unapplied advances through a finance posting, and can reverse active advance applications through a separate finance posting before reversing the parent advance. No advance correction is a status-only edit.
- Supplier payment overpayment guards remain active. WebAdmin must not silently convert invoice overpayment into credit or advance; advance application remains explicit and handler-owned.
- Bank API target adapters remain separate future slices.
- Bank API configuration, bank credential UI, returned-transfer automation, duplicate-payment automation, and status-only bank settlement must not appear in WebAdmin before their owning Application handlers and finance posting boundaries exist.
- WebAdmin purchasing pages must not create supplier payment, refund, customer invoice, journal shortcut, note-only settlement, attachment-only settlement, or archive/download mutations as shortcuts.
- Public WebApi, mobile/member, storefront checkout, issued invoice archive/download, finance export, payment, refund, and credit-note flows remain outside this purchasing hardening surface.

## AI Governance Workspace

- AI Governance is implemented as an internal WebAdmin review workspace over the Foundation governance records. Operators can review recommendations, accept, dismiss, or expire them, review action drafts through submit, approve, and reject workflows, and manually execute only explicitly registered low-risk executors with anti-forgery and row-version checks.
- AI Governance approval is consent evidence only until a separate manual execute action runs an explicitly registered executor. WebAdmin must not call provider APIs, build prompts, store raw prompts or completions, expose credentials, create public/mobile routes, edit journals, mutate payments/refunds/payroll/bank settlement, or bypass the owning Application command handlers.
- Scoped context projection is implemented in Application as purpose-bound aggregate module metrics for Sales, Finance, Purchasing, Inventory, HR, Payroll, and Treasury. WebAdmin AI review screens must continue to use recommendation and draft records; they must not display raw prompt context, private employee details, bank identifiers, document content, provider payloads, or journal line payloads.
- The current production executors are limited to internal timeline `Note`/`Activity` evidence and internal `InternalFollowUpTask` records. Follow-up task pages under AI Governance are internal operator queues with complete/cancel actions only; they do not mutate the target business object.
- The broader AI executor decision selects internal module review routing over `InternalFollowUpTask`, and `CreateModuleReviewTask` is implemented as a task-only command family. Provider implementation remains blocked until credential ownership, prompt contract, safe logging, retry/error policy, and smoke strategy are locked.
- The current AI/integration checkpoint keeps real provider calls, direct operational AI executors, target-specific sync adapters, and accounting API push blocked until their concrete target, owner, payload, policy, and smoke strategy are selected. Provider-neutral `SyncState`/`SyncConflict` records exist for future sync evidence; internal AI evidence and task routing remain the only active AI execution surfaces.

## HR And Time Workspace

- HR is an internal/WebAdmin workspace over formal HR records, not an extension of public/member surfaces.
- `BusinessMember` remains the source for business access, invitations, roles, and permissions. `Employee` records may link to a business member when the person also needs system access, but HR department or position must not grant authorization by itself.
- Employee, department, position, and employment-contract metadata are implemented as the HR core boundary. They must not introduce payroll calculation, payroll filing, public/mobile/storefront routes, supplier finance mutations, customer payment/refund mutations, finance export format changes, or invoice archive/download behavior.
- Personnel documents use object-storage-backed `DocumentRecord` workflows with HR privacy classification. Employee detail supports internal upload, download, and archive with retention metadata, legal-hold metadata, row-version checks, and audit evidence. Personnel files must not be stored in notes, employee metadata, event payloads, payroll-provider payloads, public/mobile routes, finance export packages, or invoice archive/download flows.
- Work schedules, schedule exceptions, attendance events, time entries, and period timesheets are implemented as internal/WebAdmin-first HR time surfaces. Timesheets use submit, review, approve, and reject lifecycle evidence with row-version checks. Native or PWA time-clock work requires a separate device, offline, and privacy design.
- Leave requests and absence records are implemented as dedicated HR records. Approved leave creates formal absence evidence; absence must not be hidden in timesheet notes, generic metadata, or payroll summaries.
- Payroll-period summaries are implemented in the HR workspace. Operators can create review periods, prepare employee-level summaries from approved timesheets and confirmed absences, review, approve, and cancel the summary container. Legal payroll boundary design is complete as Germany-first and country/version-aware.
- Payroll rule foundations are implemented in the HR workspace with internal Payroll Rules pages for versioned rule sets and components, effective dates, jurisdiction/currency metadata, overlap guards, and safe audit evidence. Payroll rules do not run payroll, generate payslips, post liability, pay salaries, submit to providers, mutate finance, or expose public/mobile routes.
- Payroll runs are implemented in the HR workspace with internal create, calculate, review, approve, and cancel workflow over approved payroll periods and immutable employee, contract, time, absence, and rule snapshots.
- Internal payslip artifact generation is implemented for approved payroll runs. Payslip artifacts are generated from payroll run snapshots, stored through object storage, linked with `DocumentRecord`, and downloadable only in WebAdmin. The official payslip download is a versioned built-in PDF artifact; the HTML source artifact is retained internally for traceability.
- Payroll liability posting is implemented for approved payroll runs through finance posting handlers. Posted runs keep a `JournalEntry` linkage for read-only operational traceability.
- Payroll payment core/admin is implemented. Payroll payments are internal HR/Payroll records over posted payroll runs and employee allocations; posting debits `PayrollPayable`, credits `CashClearing`, and keeps read-only journal linkage in WebAdmin.
- Posted payroll payment full-reversal is implemented in WebAdmin only for posted payments. It posts a balanced reversal journal through finance posting services, stores the reversal reason and reversal journal link, and keeps reversed payroll payments out of paid/open-payable totals.
- Payroll bank settlement is implemented in WebAdmin for posted, unreversed payroll payments when a matched zero-difference bank reconciliation links to the payroll payment posting and the bank account maps to an Asset financial account. Settlement posts `PayrollPaymentBankSettled`, clears `CashClearing` to the mapped bank Asset account, stores read-only settlement journal/reconciliation evidence, and blocks simple reversal after bank settlement.
- Payroll returned-transfer correction core is implemented in WebAdmin. `PayrollPaymentBankCorrection` records returned salary transfer and duplicate salary bank movement evidence separately from `PayrollPayment`; returned-transfer corrections are full-settlement v1 and post through `PayrollPaymentBankCorrection`, while duplicate salary bank movement remains attention/evidence and cannot auto-post.
- Payroll provider adapter boundaries are documented. Statutory filing and payroll-provider submission remain blocked until a real target, credential owner, payload mapping, retry policy, safe error contract, and smoke strategy are selected; WebAdmin must not expose provider credential forms or fake submission success.
- Employee payslip self-service core is complete for the current phase. It uses dedicated additive member/mobile routes with own-employee authorization, privacy-safe download audit, official PDF `DocumentRecord` retrieval, no missing-object regeneration, and safe high-level payment status. It does not reuse WebAdmin download routes or expose retained HTML source, bank reconciliation, journal, provider, or payroll source payload internals.
- AI-readiness and automation governance design, core Foundation model, scoped context projection, provider adapter boundary design, and provider-neutral adapter foundation are complete. WebAdmin AI surfaces must start as internal review queues over `AiRecommendation` and `AiActionDraft`, with human approval before any operational command handler executes. They must not add autonomous execution, model-provider credentials, raw prompt/completion logs, public/mobile routes, stock/payment/payroll/bank mutations, journal edit shortcuts, or access to raw HR, payroll, bank, provider, credential, document, or archive payloads.
- AI provider foundation is Application-internal and may be surfaced later as compact internal readiness/status only after a real provider target is selected. Target-provider selection remains deployment-approved and configurable; no real provider is activated by default. WebAdmin v1 must not add provider credential entry, raw prompt viewers, raw completion viewers, provider payload dumps, fake-success provider controls, or execution buttons that bypass the owning Application command handlers.
- AI action draft approval remains consent evidence until an approved draft is executed through an explicitly registered low-risk executor. The current production executors create internal timeline `Note`/`Activity` evidence, internal `InternalFollowUpTask` records, or module review tasks backed by `InternalFollowUpTask` from eligible approved drafts. WebAdmin must not add arbitrary command invocation, reflection-based command routing, bulk execution, automatic execution after approval, or high-risk AI execution shortcuts.
- HR events and audit payloads must store only safe reportable ids, dates, statuses, and business/employee references. Medical details, private document content, provider credentials, raw payroll payloads, and raw HR import files must not appear in metadata, audit trails, logs, or documentation.

## Security And UX Rules

- Never display provider secrets, access keys, webhook secrets, connection strings, SAS tokens, or private credentials.
- Mask secret fields and preserve existing values when posts contain blank or placeholder values.
- Use anti-forgery tokens and row-version checks for mutations.
- Keep CSP, secure cookies, and self-hosted asset assumptions intact.
- Prefer compact operator dashboards over long explanatory pages.
- Keep diagnostics in module workspaces; the dashboard should summarize and link.

## Testing Expectations

Use hosted WebAdmin tests for:

- Authentication/authorization and anti-forgery.
- Render and HTMX fragment stability.
- Row-version protected mutation flows.
- Business onboarding and invitation lifecycle.
- Inventory/returns operator flows.
- Billing/payment/refund/dispute/provider callback surfaces.
- Mobile support surfaces.
- E-invoice artifact download safety.

See [DarwinTesting.md](DarwinTesting.md) for active commands and coverage priorities.
