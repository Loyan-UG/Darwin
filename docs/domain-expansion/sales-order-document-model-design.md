# Sales And Order Document Model Design

This document defines how Darwin should expand sales and order documents after the CRM foundation work. It is a design artifact only. It does not change entities, migrations, API routes, DTOs, mobile contracts, WebAdmin flows, invoice archive behavior, or production code.

The core rule is simple: Darwin evolves the current `Order`, `OrderLine`, `Invoice`, `InvoiceLine`, payment, shipment, and archive foundation. It must not create a parallel sales order, sales invoice, finance invoice, or issued-document source for the same business surface.

## Current Darwin Sales/Order Findings

- `Order` and `OrderLine` already exist in the `Orders` schema. They hold the order number, currency, tax mode, totals, lifecycle status, billing and shipping address snapshots, shipping method snapshots, payment records, shipment records, and internal notes.
- `Invoice` and `InvoiceLine` currently live in the `CRM` schema, but they are already shared by order, CRM, billing, member invoice history, archive, structured source-model, and compliance flows.
- Member-facing order and invoice DTOs, payment-intent routes, invoice archive downloads, structured JSON/XML exports, and mobile cache behavior are compatibility-sensitive.
- `Order.BillingAddressJson`, `Order.ShippingAddressJson`, and issued invoice archive/source-model data are intentional snapshots. New Sales design must preserve old reads and issued-document immutability.
- `NumberSequence`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`, `ExternalReference`, and canonical address mapping are available foundation primitives for future Sales expansion.
- Purchase orders already exist in the inventory area and should be handled in the purchasing lifecycle, not mixed into the Sales document model.

## Decision Matrix

| Sales surface | Current Darwin model | Target Darwin English name | Decision | Foundation primitive to reuse | Schema/API impact | Mobile/member impact | Priority | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sales quote | `Opportunity` and `OpportunityItem` can describe potential revenue, but they are not formal quote documents. | `SalesQuote`, `SalesQuoteLine` | Design as a separate sales document later; do not overload or replace `Opportunity`. | `NumberSequence`, `DocumentRecord`, `BusinessEvent`, `AuditTrail`, `ExternalReference`, `CustomFieldValue` for uncertain quote attributes. | Future additive schema in Sales slice. No change in this design step. | Low unless quotes become member-facing. | P1 | Define quote lifecycle before implementation: draft, sent, accepted, rejected, expired, converted. |
| Sales order | `Order` already owns order number, totals, status, address snapshots, payments, shipments, and lines. | `SalesOrder` as service/UI/projection name over `Order`. | Evolve `Order`; do not create a parallel `SalesOrder` table for v1. | `NumberSequence`, canonical address mapper, `ExternalReference`, `BusinessEvent`, `AuditTrail`, `DocumentRecord`. | Future additive fields only when common/reportable. Existing `Order` table remains foundation. | High: member order history and payment retry depend on current model. | P0 | Build Sales application facades/projections over `Order` before adding new tables. |
| Sales order line | `OrderLine` is a purchase-time snapshot of variant, SKU, quantity, price, tax, warehouse, and add-ons. | `SalesOrderLine` as service/UI/projection name over `OrderLine`. | Evolve `OrderLine`; preserve snapshot semantics and avoid live catalog re-resolution for issued orders. | Canonical pricing/tax snapshot rules, `ExternalReference` if imported. | Future additive line fields only when needed for fulfillment, sales reporting, or tax. | High: member detail contracts expose order lines. | P0 | Keep line snapshot compatibility tests whenever line fields are added. |
| Sales invoice | `Invoice` is currently shared and stores status, totals, due/paid/issued timestamps, reverse-charge decision, issued snapshot, archive metadata, and lines. | `SalesInvoice` as service/UI/projection name over `Invoice`. | Evolve existing `Invoice`; do not create CRM invoice, sales invoice, and finance invoice variants for the same issued document. | `NumberSequence` when invoice numbering is introduced, `DocumentRecord`, `BusinessEvent`, `AuditTrail`, `ExternalReference`. | Future additive fields or controlled schema/namespace migration only. No parallel table. | High: member invoice history, archive, structured exports, and downloads depend on current model. | P0 | Treat any invoice schema/namespace cleanup as a compatibility migration, not a new model. |
| Sales invoice line | `InvoiceLine` stores description, quantity, unit net, tax rate, total net, and total gross. | `SalesInvoiceLine` as service/UI/projection name over `InvoiceLine`. | Evolve `InvoiceLine`; preserve issued invoice line reads and archive source-model generation. | Tax snapshot rules, `DocumentRecord` for supporting documents, `AuditTrail` for changes before issue. | Future additive fields only; issued lines must remain immutable after issue. | High for invoice detail and structured export behavior. | P0 | Add immutability tests before changing issued invoice line behavior. |
| Delivery note / shipment document | `Shipment` and shipment provider operations exist; order lines may carry warehouse selection. | `DeliveryNote` | Implemented as a formal internal/WebAdmin fulfillment document over current shipments and shipped quantities. It is not a detached PDF-only record. | `NumberSequence`, `BusinessEvent`, `AuditTrail`; future generated document metadata can use `DocumentRecord`; external delivery ids use `ExternalReference`. | Additive `Sales.DeliveryNotes` and `Sales.DeliveryNoteLines` schema. Public/mobile/storefront contracts unchanged. | Medium if delivery documents become member-visible later; unchanged in v1. | P1 | Use shipment lines as v1 quantity authority; keep inventory movement authoritative in shipment/inventory handlers. |
| Return order | Refund and return-label flows exist around payments and shipments. | `ReturnOrder`, `ReturnOrderLine` | Boundary is decision-complete: ReturnOrder is a formal internal/WebAdmin document; refund and restock are allowed only after received goods are inspected. | `NumberSequence`, `BusinessEvent`, `AuditTrail`, `DocumentRecord`, `ExternalReference`. | Future additive schema likely. No change in this design step. | Medium if returns are exposed to members later; unchanged in v1. | P2 | Implement core model/admin next unless inventory receipt/restock foundation must be added first. |
| Credit note | Refunds, invoice cancellation, ReturnOrder evidence, invoice archive/source-model paths, finance posting, account mappings, and receivables projection exist. Credit-note core model is implemented. | `CreditNote`, `CreditNoteLine` | Finance-gated formal document. Do not fake credit notes as negative invoices, non-posting cosmetic records, return-order printouts, or UI-only documents. | `NumberSequenceDocumentType.CreditNote`, issued invoice source-model/archive discipline, current Billing journal foundation, `BusinessEvent`, `AuditTrail`, `ExternalReference`. | Additive Sales schema is implemented with source/archive fields, posting, tax reversal, cumulative credit validation, legal numbering, and lifecycle evidence. | Medium if members can download credit notes later; unchanged in v1. | P2 | Harden source export/reconciliation before member visibility. |
| Pricing/discount/tax snapshots | `Order` and `OrderLine` store totals and tax snapshots; cart/checkout computes current pricing. `Invoice` stores invoice totals and lines. | `SalesPricingSnapshot`, `SalesTaxSnapshot` | Keep snapshots on documents. Do not recompute historical documents from live catalog/tax settings. | Canonical address mapper for tax locality, `DocumentRecord` for evidence, `AuditTrail` for manual decisions. | Future additive snapshot fields can be real columns when reportable, JSON only for immutable low-level evidence. | High for order/invoice display and compliance. | P0 | Classify missing reportable price/tax fields before schema expansion. |
| Status history | `Order.Status` and `Invoice.Status` exist; current handlers enforce transitions and evidence. | `SalesDocumentStatusHistory` | Add status history later only if operators need audit/reporting beyond current status and event/audit evidence. | `BusinessEvent`, `AuditTrail`; possible structured history table if reporting requires it. | No change now. Future table only with clear reporting need. | Low unless status history is member-visible. | P1 | Prefer event/audit instrumentation first; add dedicated history only if queries require it. |
| Payment and settlement boundary | Payments are linked to orders and optionally invoices; payment-intent flows already exist. | `SalesSettlement` as projection, not v1 entity. | Keep payments as authoritative payment records. Do not move settlement into Sales documents prematurely. | `BusinessEvent`, `AuditTrail`, `ExternalReference`. | No schema change now. | High: retry payment routes are member-facing. | P0 | Sales design may expose settlement views, but payment mutation remains in payment/order handlers. |
| Invoice archive/source-model boundary | Current invoice archive, structured JSON, structured XML, retention, purge metadata, and artifact storage are implemented. | `IssuedInvoiceArchive` / `InvoiceSourceModel` | Keep existing archive/source-model behavior authoritative for issued invoices. Sales must consume it, not replace it. | `DocumentRecord` for metadata references only when needed; specialized archive records stay authoritative. | No schema/API change in this design. | High: member downloads and compliance evidence depend on it. | P0 | Any future archive changes require source-model and member download compatibility tests. |
| External references and import/export identity | `ExternalReference` exists but Sales does not yet broadly use it. | `SalesExternalReference` as usage pattern over `ExternalReference`. | Use `ExternalReference` for imported/synchronized order, invoice, shipment, return, and credit identities. | `ExternalSystem`, `ExternalReference`, `SourceOfTruth`. | No direct provider-specific columns unless frequent/reportable and justified. | Medium if externally sourced records are shown to members. | P1 | Add references in concrete integration/import slices, not in this design step. |
| Sales documents and attachments | Invoice archive and shipment labels are specialized; `DocumentRecord` exists for generic document metadata. | `SalesDocumentRecord` as usage pattern over `DocumentRecord`. | Use `DocumentRecord` for quote PDFs, delivery notes, customer attachments, and supporting sales documents; do not replace invoice archive or shipment provider records. | `DocumentRecord`. | No upload/download flow change now. | Medium if documents become member-visible. | P1 | Define visibility before exposing any document to member/business clients. |
| Business events/audit evidence | `BusinessEvent` and `AuditTrail` exist; order and invoice handlers have specialized evidence in places. | `SalesBusinessEvent`, `SalesAuditTrail` as usage pattern. | Instrument important Sales lifecycle transitions later; do not make events the mutation source. | `BusinessEvent`, `AuditTrail`. | No change now. | Low unless events drive member-visible status. | P1 | Add event/audit writes only after each domain transition is stable and tested. |
| Number sequences | `NumberSequence` exists; order creation can use it with fallback. Invoice numbering remains deferred. | `SalesNumberSequence` as usage pattern over `NumberSequence`. | Use `NumberSequence` for future quote, delivery, return, credit, and invoice numbers. Never rewrite issued order numbers. | `NumberSequence`. | Future additive document number fields only when needed. | High for member-visible order/invoice numbers. | P0 | Define per-document numbering policy before first Sales schema implementation. |
| Mobile/member compatibility boundary | Member order/invoice contracts and mobile services are guarded. | `MemberSalesProjection` | Keep current member projections stable. Sales/admin/internal projections may be richer but must not break mobile DTOs. | Contract serialization tests, mobile service tests, source-route guards. | Public/mobile changes must be additive or deliberate pre-release migration. | High. | P0 | Run contract/mobile smoke tests for any Sales schema, handler, or projection change. |

## Locked Design Decisions

- `SalesOrder` in v1 is the existing `Order`. If Sales UI or services need sales terminology, they should use projections, facades, labels, or handler names over `Order`.
- `SalesInvoice` in v1 is the existing `Invoice`. Its current schema placement under `CRM` is a cleanup concern, not a reason to create another invoice model.
- `SalesQuote` is separate from `Opportunity`, but implementation is deferred. An opportunity can lead to a quote; it is not itself the formal issued quote document.
- Delivery documents must be aligned with shipment and inventory fulfillment. A delivery note should not be invented as an isolated PDF-only record.
- Return orders and credit notes need refund, shipment return, inventory, tax, and accounting consequences. They should wait for inventory/finance alignment unless a concrete release need appears.
- Issued invoice snapshots, archive artifacts, structured JSON/XML, retention metadata, and member download behavior remain immutable compatibility surfaces.
- New document numbers use `NumberSequence`. Existing issued order numbers and invoice projections are not rewritten.
- External sales identities use `ExternalReference`; provider-specific payloads or raw integration state do not belong on sales documents as ad hoc JSON.
- Sales events and audits are evidence and automation context. Normal application commands remain the mutation path.

## Field Storage Rules For Sales

| Field type | Storage decision | Examples |
| --- | --- | --- |
| Frequent, reportable, filterable, compliance-relevant, inventory-relevant, finance-relevant, or cross-module data | Real columns on the owning document or structured foundation entities. | Document status, issue date, due date, customer, business, totals, tax totals, settlement status, delivery status. |
| Immutable checkout/issue evidence that must preserve historical shape | Snapshot JSON plus hash or structured archive/source model where required. | Billing/shipping address at checkout, issued invoice source model, tax evidence snapshot. |
| Provider-specific or import/export identity | `ExternalReference` and integration-specific operation records. | External order id, external invoice id, external fulfillment id. |
| Low-frequency, customer-specific, uncertain, or deployment-specific attributes | Custom fields or metadata JSON. | Local sales score, temporary approval note, deployment-specific sales classification. |
| Human collaboration | `Note` or existing compatibility fields until a migration is explicitly designed. | Internal sales handoff note, operator comment. |
| Generated/uploaded document metadata | `DocumentRecord`, except specialized archives/provider labels remain authoritative. | Quote PDF metadata, delivery note metadata, signed customer document metadata. |

## Sales Core Projection Outcome

The first implementation slice adds internal Application-level Sales projections over the current documents without schema, route, DTO, WebAdmin, mobile, checkout, archive, or download changes.

- `SalesOrderDocumentDto` is an internal projection over `Order`, `OrderLine`, payments, shipments, and linked invoice summaries.
- `SalesInvoiceDocumentDto` is an internal projection over the current shared `Invoice` and `InvoiceLine` model.
- `SalesDocumentSettlementDto` and `SalesDocumentFulfillmentDto` expose payment and shipment facts as projection data only.
- No `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` domain entity/table is introduced.
- Address JSON, order line snapshots, issued invoice snapshots, archive metadata, payment references, and shipment data are read from the current records. Historical documents are not recomputed from live catalog, address, payment, or shipping configuration.
- Member-facing order/invoice contracts, mobile `MemberCommerceService`, WebApi payment-intent paths, invoice archive/download routes, and storefront checkout/finalization routes are guarded as compatibility surfaces.
- Consumer mobile contains member commerce code for order and invoice history, but Sales/checkout remains storefront-first in this slice. The projection layer does not expand mobile commerce behavior.

## Sales Core Additive Fields Outcome

The first additive schema slice keeps Sales on the current order and invoice foundation and adds only reportable fields that do not change public or mobile contracts.

- `Order` now carries nullable `BusinessId` and `CustomerId`, plus `SalesChannel` and `OrderedAtUtc` for reporting and operational segmentation.
- `Invoice` now carries nullable `InvoiceNumber`. Existing issued invoices are not rewritten, and member-facing invoice display remains compatible when the number is absent.
- `InvoiceLine` now carries `TotalTaxMinor` so tax reporting does not need to recompute line tax from gross/net values.
- `SalesChannel` is a reporting field only. It is not an authorization source, feature gate, source-of-truth marker, or module visibility control.
- Storefront checkout sets `SalesChannel.WebStorefront`; direct application order creation sets `SalesChannel.Admin`.
- Invoice number reservation uses `NumberSequence` only when an active invoice sequence exists. Without a sequence, `InvoiceNumber` remains null.
- No `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` entity/table is introduced.
- Member/mobile order and invoice DTOs, checkout routes, payment-intent routes, archive/download behavior, structured source-model exports, and issued snapshots remain unchanged.

## Sales Admin Workspace Exposure Outcome

Darwin now exposes a dedicated WebAdmin Sales workspace over the current order and invoice foundation.

- `Sales` is a commercial/read-only workspace with overview, order list/detail, and invoice list/detail screens.
- `Orders` remains the operational workspace for payment, shipment, refund, status, and invoice-creation workflows.
- CRM invoice screens remain the operational workspace for invoice editing, archive/source-model, structured exports, and artifact actions.
- Sales projections reuse `Order`, `OrderLine`, `Invoice`, `InvoiceLine`, payments, shipments, `SalesChannel`, `OrderedAtUtc`, `InvoiceNumber`, and invoice line tax fields.
- Sales filters support document search, status, sales channel, date windows, business scope, and customer scope without introducing public or mobile contracts.
- Sales pages link into existing operational workflows instead of creating mutation forms or duplicate commands.
- No `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` entity/table is introduced.
- Mobile member commerce, Web storefront checkout, payment-intent routes, issued invoice archive/download, and structured source-model behavior remain unchanged.

## Sales Status/Event Instrumentation Outcome

Darwin now records shared sales lifecycle evidence from the existing mutation owners.

- Order status, payment, shipment, invoice creation/status, and refund flows can emit deterministic `BusinessEvent` and `AuditTrail` records.
- Event keys are deterministic so retries do not create duplicate lifecycle evidence.
- Event payloads contain only safe/reportable identifiers, status values, amounts, currency, channel, and business/customer links.
- Provider tokens, raw provider payloads, archive payloads, object-storage credentials, and mobile-only contract data are not stored in sales events.
- Sales overview invoice balance uses the shared billing reconciliation logic instead of gross-open invoice totals.
- Sales workspace remains read-only; evidence supports reporting, traceability, and automation context, not mutation.
- Specialized records remain authoritative: payment/refund records, shipment carrier timelines, invoice archive metadata, and issued source models are not replaced.
- Do not add quote, delivery note, return order, or credit note behavior until those document lifecycles are designed.

If the projection layer shows that the current `Invoice` schema placement creates real technical risk, handle that as a controlled compatibility migration proposal before adding new Sales invoice behavior.

## Sales/Foundation Verification Reconciliation Outcome

The Sales and foundation verification gaps found after lifecycle instrumentation are closed without schema, route, DTO, mobile, checkout, archive, or Sales UI changes.

- WebAdmin composition now registers the business media/profile handlers required by the existing `BusinessesController`; broad list-page smoke coverage includes the business workspace again.
- The legacy null stored row-version guard reaches `UpdateOrderStatusHandler` through a test-only relaxed model configuration while production `Order.RowVersion` mapping remains required.
- Sales render/source guards, Sales/Order/Invoice unit coverage, member commerce contracts, and mobile member commerce routes remain compatibility checks for the next sales document work.

## Sales Document Lifecycle Design Outcome

The next Sales document lifecycle layer is decision-complete in [sales-document-lifecycle-design.md](sales-document-lifecycle-design.md). This outcome is documentation-only and does not add schema, UI, routes, DTOs, mobile contracts, checkout behavior, invoice archive/download behavior, or production flows.

- `SalesQuote`, `DeliveryNote`, `ReturnOrder`, and `CreditNote` lifecycles are designed together so document boundaries are not implemented piecemeal.
- V1 lifecycle visibility is internal/WebAdmin only. Member, mobile, storefront, and public WebApi contracts remain unchanged.
- `SalesQuote` is separate from `Opportunity`; accepted quotes can create or link to the current `Order` foundation.
- `DeliveryNote` is a fulfillment document tied to shipment and warehouse quantity ownership, not a detached generated file.
- `ReturnOrder` is gated by the locked boundary in [return-order-boundary-design.md](return-order-boundary-design.md): refund and restock are allowed only after received goods are inspected.
- `CreditNote` is finance-gated and must not be implemented as a negative invoice or non-posting cosmetic document.
- `SalesQuote Core Model Slice` is now implemented; the next quote implementation is controlled quote-to-order conversion.

## SalesQuote Core Model Outcome

Darwin now implements `SalesQuote` and `SalesQuoteLine` as the first additive Sales lifecycle document.

- `SalesQuote` is a Sales schema document, separate from `Opportunity`, and does not replace CRM pipeline tracking.
- `SalesQuoteLine` stores quote-time product/service name, SKU, description, quantity, unit net/gross prices, tax rate, calculated net/tax/gross totals, and sort order.
- `QuoteNumber` is nullable while the quote is a draft. It is reserved from `NumberSequenceDocumentType.SalesQuote` when the quote is sent.
- Lifecycle states are `Draft`, `Sent`, `Accepted`, `Rejected`, `Expired`, and `Converted`.
- `Converted` links to an existing valid `Order`; automatic order creation is not hidden inside this slice.
- Lifecycle changes write deterministic `BusinessEvent` and `AuditTrail` evidence and avoid duplicate evidence on retry.
- WebAdmin Sales now includes Quotes with list/filtering, create/edit/detail, send, accept, reject, expire, and link-converted-order actions.
- The Sales workspace remains the only new UI surface. Public WebApi, mobile/member commerce, storefront checkout, payment/shipment/refund flows, and invoice archive/download behavior are unchanged.
- No `SalesOrder`, `SalesInvoice`, or `FinanceInvoice` entity/table was introduced.

## SalesQuote Order Conversion Outcome

Accepted internal quotes can now create current `Order` records without introducing a parallel Sales order model.

- Quote-to-order conversion maps quote business/customer/currency, address snapshots, totals, and line snapshots into the existing `Order` and `OrderLine` entities.
- `OrderLine.VariantId` is nullable so service, custom, and other non-catalog quote lines can become order snapshot lines without fake catalog products.
- Catalog quote lines keep their product variant link; non-catalog lines keep their quote-time name, SKU, quantity, unit prices, tax rate, and totals.
- Non-catalog order lines are ignored by inventory reserve/release/allocate logic because no stock item exists to move.
- `Create order from quote` and `Link existing converted order` are separate WebAdmin actions.
- Created orders use the shared order creation service and current order number policy. Existing direct admin order and storefront checkout fallback behavior remains compatible.
- Quote conversion emits deterministic `BusinessEvent/AuditTrail` evidence and does not create invoice, payment, shipment, refund, delivery note, or archive artifacts.
- Public WebApi, mobile/member commerce, storefront checkout, payment/shipment/refund flows, issued invoice archive/download, and structured source-model contracts remain unchanged.

## DeliveryNote Core Model Outcome

Darwin now implements `DeliveryNote` and `DeliveryNoteLine` as a formal internal/WebAdmin Sales document over the current shipment foundation.

- Delivery notes are created from existing `Shipment` records and their `ShipmentLine.Quantity` values. The UI does not accept manual delivery-note line quantities.
- `DeliveryNote` links to the current `Order` and `Shipment`, carries optional business/customer links, carrier/tracking snapshots, shipping address snapshot JSON, totals, internal notes, and metadata.
- `DeliveryNoteLine` snapshots the related order line, optional product variant, name, SKU, quantity, unit price, tax rate, and line totals. Catalog and non-catalog order lines are both supported.
- `DeliveryNoteStatus` covers `Draft`, `Prepared`, `Issued`, `Shipped`, `Delivered`, and `Cancelled`. Transition rules are centralized in `DeliveryNoteWorkflowPolicy`.
- `DeliveryNoteNumber` is nullable until issue. Issuing reserves `NumberSequenceDocumentType.DeliveryNote`; issued numbers are not recomputed.
- Each non-deleted shipment has one delivery note. Cancellation remains audit evidence for that shipment instead of creating hidden duplicate delivery documents.
- Delivery note lifecycle writes deterministic `BusinessEvent/AuditTrail` evidence. Payloads are safe/reportable and do not store provider tokens, raw payloads, archive data, or secrets.
- WebAdmin Sales now includes Delivery Notes list/filtering, create-from-shipment, detail, prepare, issue, mark shipped, mark delivered, and cancel actions.
- Shipment provider operations, labels, future inventory movement, payments, refunds, invoice archive/source-model, checkout, public WebApi, mobile/member commerce, and storefront behavior remain unchanged.

## ReturnOrder Boundary Design Outcome

ReturnOrder boundaries are now decision-complete in [return-order-boundary-design.md](return-order-boundary-design.md). This outcome is documentation-only and does not add schema, UI, routes, DTOs, mobile/member/storefront contracts, payment/refund flows, shipment flows, invoice archive/download behavior, or production code.

- `ReturnOrder` is a formal internal/WebAdmin Sales document and does not replace current `Refund`, `Shipment`, `DeliveryNote`, `Invoice`, or future `CreditNote`.
- The v1 lifecycle is `Requested`, `Approved`, `Rejected`, `ReturnShipmentQueued`, `Received`, `Inspected`, `RefundReady`, `Refunded`, `Closed`, and `Cancelled`.
- Refund and restock are forbidden before inspection. A return request, approval, or queued label is not enough to create settlement or stock movement.
- Requested, approved, received, accepted, rejected, scrapped, and restock-intended quantities have separate ownership and must be modeled explicitly.
- Current `Refund` remains the authoritative payment/order/invoice settlement record; ReturnOrder provides eligibility, evidence, and linkage.
- Return shipment/label orchestration remains provider/shipment-owned and does not prove receipt or inspection.
- `CreditNote` remains Finance-gated. No negative invoice or non-posting credit-note document is introduced.
- Public WebApi, mobile/member, storefront checkout, invoice archive/download, structured source-model, and issued snapshot contracts remain unchanged.

## ReturnOrder Core Model Outcome

Darwin now implements the ReturnOrder layer without changing current order, invoice, payment, shipment, archive, storefront, or mobile/member contracts.

- `ReturnOrder`, `ReturnOrderLine`, and `ReturnOrderRefundLink` are additive Sales schema entities.
- ReturnOrder uses current `Order`, `OrderLine`, optional `Shipment`, optional `Invoice`, and current `Refund` records as linked foundations. It does not create `SalesOrder`, `SalesInvoice`, `FinanceInvoice`, or `CreditNote`.
- Requested, approved, received, accepted, rejected, scrapped, and restock quantities remain separate, reportable fields.
- Refund eligibility and restock are only allowed after inspection. Settlement still belongs to existing refund/payment handlers; stock movement still belongs to inventory handlers.
- WebAdmin Sales exposes Return Orders as internal lifecycle documents with links to authoritative operational flows.
- Public WebApi, mobile/member commerce, storefront checkout, payment-intent routes, invoice archive/download, structured source-model, and issued snapshots remain unchanged.

## ReturnOrder Refund And Inventory Reconciliation Hardening Outcome

ReturnOrder reconciliation is hardened on the current refund/payment and inventory foundations without adding schema, public routes, mobile/member exposure, storefront behavior, invoice archive/download changes, credit-note implementation, or a parallel ledger.

- Linked refunds reconcile against current completed `Refund` records and must match the return's order and currency.
- Partial multi-refund linkage keeps the return in `RefundReady`; exact eligibility coverage moves it to `Refunded`; over-eligibility linkage is rejected.
- Duplicate refund links are idempotent and do not double-count settlement.
- Restock remains after-inspection only and uses the existing inventory return receipt owner with `ReferenceId = ReturnOrderId` to avoid duplicate stock movement on retry.
- Non-catalog return lines can carry accepted/refund evidence but cannot create restock movement.
- WebAdmin Return Orders show linked and remaining refund amounts and keep quantity inputs limited to allowed lifecycle stages.
- `CreditNote` remains blocked until Finance/accounting design defines posting, legal numbering, tax evidence, archive/source-model, and settlement policy.

## CreditNote Finance/Accounting Boundary Design Outcome

Credit-note finance and accounting boundaries are now decision-complete in [credit-note-finance-accounting-boundary-design.md](credit-note-finance-accounting-boundary-design.md). This outcome is documentation-only and does not change entities, migrations, routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, refund/payment flows, invoice archive/download behavior, or production code.

- `CreditNote` is a future formal finance/sales document, not a replacement for `Refund`, `ReturnOrder`, `Invoice`, or invoice cancellation.
- Negative invoices, non-posting cosmetic credit notes, and UI-only credit-note records are forbidden.
- Credit-note lines must be derived from immutable issued invoice snapshot/source-model evidence, not live catalog, current tax settings, or mutable invoice lines.
- Refund settlement remains authoritative in current refund/payment records; credit notes can link to settlement evidence but do not execute settlement.
- Return orders can provide goods-return evidence, but issuing a credit note requires finance/accounting readiness.
- Legal credit-note numbering, tax reversal, immutable archive/source-model, cancellation/void behavior, cumulative credit limits, and member download policy must be defined before implementation.
- Account mapping, receivables projection, and invoice/payment/refund/cancellation posting wiring are implemented and documented in [finance-account-mapping-receivables-design.md](finance-account-mapping-receivables-design.md) and [finance-receivables-projection.md](finance-receivables-projection.md). The next gate is `CreditNote Source Model And Archive Design Slice`, not immediate credit-note schema/UI implementation.

## Finance Posting Foundation Implementation Outcome

Finance posting foundation decisions and implementation outcome are now documented in [finance-posting-foundation-design.md](finance-posting-foundation-design.md). The implementation adds additive journal-entry metadata, migrations, and an internal posting service. It does not add routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, invoice archive/download changes, or production posting flows.

- Darwin should evolve existing Billing `FinancialAccount`, `JournalEntry`, and `JournalEntryLine` records rather than create a parallel ledger.
- Manual journal entries remain current Billing operations.
- Automated posting now has source linkage, idempotency, posting lifecycle/status, safe metadata, and balanced account validation through `FinancePostingService`.
- Payment/refund/invoice/return records remain operational sources of truth. Posting consumes their facts and does not replace their handlers.
- Credit-note core implementation is now gated by implementing the already-designed source/archive, tax reversal, legal numbering, cumulative credit validation, posting, and lifecycle evidence policy as one complete internal/WebAdmin slice.

## Finance Account Mapping Foundation Outcome

Finance account-role mapping is now implemented as an internal Billing foundation and documented in [finance-account-mapping-receivables-design.md](finance-account-mapping-receivables-design.md).

- `FinancePostingAccountMapping` maps business-specific roles such as receivables, sales revenue, tax payable, cash clearing, refund clearing, and rounding to current `FinancialAccount` records.
- Future sales invoice, payment, refund, return, and credit-note postings must resolve accounts by role through `FinanceAccountMappingService`; they must not hardcode account ids or infer accounts from names.
- No Sales workspace UI, public WebApi, mobile/member commerce, storefront checkout, payment/shipment/refund flow, invoice archive/download, or issued snapshot behavior changes in this foundation step.
- Receivables projection and invoice/payment/refund/cancellation posting wiring are now implemented. The next finance gate for Sales is credit-note source/archive/tax/legal design, not direct credit-note implementation.

## Invoice Payment Refund Posting Wiring Outcome

Darwin now wires current invoice, payment, refund, and invoice-cancellation facts into the finance posting foundation without changing public WebApi, mobile/member, storefront checkout, payment-intent, invoice archive/download, issued snapshot, or Sales workspace mutation ownership.

- Issued invoices post receivable, revenue, and tax entries through mapped accounts.
- Completed payments post cash-clearing and receivable settlement entries.
- Completed refunds post receivable and cash-clearing settlement reversal entries.
- Cancelled issued invoices post receivable/revenue/tax reversal entries.
- Posting keys are deterministic and idempotent, so retries do not create duplicate accounting entries.
- No `CreditNote`, negative invoice, cosmetic finance document, or parallel invoice/order model is introduced.

## CreditNote Source Model And Archive Design Outcome

Credit-note source model and archive policy is now decision-complete in [credit-note-source-model-archive-design.md](credit-note-source-model-archive-design.md). This outcome is documentation-only and does not change entities, migrations, routes, DTOs, WebAdmin UI, mobile/member/storefront contracts, refund/payment flows, invoice archive/download behavior, or production code.

- `CreditNote` must be implemented as its own formal internal/WebAdmin document with immutable issued source evidence.
- It must not be implemented as a negative invoice, a refund screen, a return-order printout, a `DocumentRecord` attachment, or a UI-only document.
- Issued credit-note source must be derived from original issued invoice source evidence and must preserve original line/tax/rounding context.
- Draft/planned credit notes do not consume legal numbers; issuing reserves `NumberSequenceDocumentType.CreditNote` exactly once.
- Credit-note structured JSON/XML and archive behavior must be generated from issued credit-note source evidence, not mutable current entities.
- V1 remains internal/WebAdmin; member/mobile/storefront exposure remains unchanged.
- `CreditNote Core Model And Admin Slice` is implemented with source/archive fields, cumulative credit validation, tax reversal, posting, and lifecycle evidence from the start.

## CreditNote Core Model And Admin Outcome

Darwin now has a formal internal/WebAdmin `CreditNote` implementation on top of the existing shared `Invoice` foundation.

- `CreditNote` and `CreditNoteLine` are additive Sales schema records; `Invoice` remains the issued invoice foundation and no parallel `SalesInvoice` or `FinanceInvoice` model was introduced.
- Draft credit notes can be created from issued invoices with optional ReturnOrder and completed Refund evidence.
- Issuing a credit note reserves `NumberSequenceDocumentType.CreditNote`, captures immutable source JSON/hash and archive metadata, writes finance posting, and records lifecycle evidence.
- Voiding a credit note creates a finance reversal posting instead of silently changing accounting state.
- WebAdmin exposes Credit Notes as an internal Sales workspace surface with create/list/detail/issue/cancel/void actions.
- Public WebApi, mobile/member commerce, storefront checkout, payment/refund flows, invoice archive/download behavior, and issued invoice contracts remain unchanged.
