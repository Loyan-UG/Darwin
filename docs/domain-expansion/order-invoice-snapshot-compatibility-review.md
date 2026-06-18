# Order And Invoice Snapshot Compatibility Review

This review records release-sensitive order, invoice, document download, archive, and structured export decisions before broader ERP sales and finance expansion continues. It is a compatibility and guard-hardening artifact. It does not redesign sales or finance, add migrations, rename routes, or change public/mobile DTO shapes.

If a required order, invoice, archive, source-model, or member-commerce contract change is discovered, it must be completed before mobile release with matching WebApi, contract, mobile shared service, and test updates. Delaying release is acceptable when the alternative is post-release downtime, broken member order/invoice history, forced mobile migration, or duplicated invoice source-of-truth state.

## Current Darwin Model Findings

- `Order.BillingAddressJson` and `Order.ShippingAddressJson` are intentional checkout-time snapshots.
- Order line, payment, shipment, and linked invoice data exposed to members are member-facing snapshots, not live product, carrier, or payment-provider records.
- `MemberOrderDetail`, `MemberInvoiceDetail`, `MemberOrderActions`, and `MemberInvoiceActions` are mobile-release-sensitive contracts.
- Invoice document, archive document, structured source-model JSON, and structured source-model XML download routes are member-facing and compatibility-sensitive.
- ERP sales and finance expansion must evolve the current `Order` and `Invoice` foundation. It must not create a parallel member-facing sales invoice or finance invoice model for the same issued document surface.

## Decision Matrix

| Surface | Current model | Release decision | Storage shape | Mobile/API impact | Required before mobile release | Guard evidence |
| --- | --- | --- | --- | --- | --- | --- |
| Order address snapshots | `BillingAddressJson` and `ShippingAddressJson` are stored on `Order`. | Keep immutable checkout-time snapshots. New canonical address helpers may write future snapshots but must not break old reads. | JSON snapshot columns. | `MemberOrderDetail` and order confirmation keep current JSON fields. | Yes. | Contract serialization and order placement tests. |
| Order line/payment/shipment/invoice snapshots | Member order detail exposes purchased line, payment, shipment, and linked invoice snapshots. | Keep snapshot semantics; do not replace with live catalog, provider, or finance lookups. | Structured child records projected to DTO snapshots. | Member order history remains stable. | Yes. | Contract serialization and member commerce tests. |
| Member order detail/actions | `MemberOrderDetail` carries totals, shipping snapshot, address snapshots, lines, payments, shipments, invoices, and actions. | Freeze property names and action path semantics. | Public DTO. | Mobile order detail and document download remain compatible. | Yes. | Contract serialization and mobile route tests. |
| Member invoice summary/detail/actions | Member invoice contracts expose invoice totals, balance, linked order, lines, payment summary, and action paths. | Freeze property names and action path semantics. | Public DTO. | Mobile invoice history/detail remains compatible. | Yes. | Contract serialization and mobile route tests. |
| Invoice archive document | Archive document route returns the printable issued-invoice archive. | Keep route and action path stable. Archive reads must remain compatible with issued snapshots. | Archive storage artifact. | Mobile archive download remains compatible. | Yes. | Mobile route and archive/source-model tests where available. |
| Invoice structured data/XML | Structured source-model JSON and XML routes expose issued invoice source data. | Keep routes stable. These exports remain source-model exports, not separate invoice truth. | Structured export generated from issued snapshot/archive data. | Mobile copy/export flows remain compatible. | Yes. | Contract action-path and mobile route tests. |
| Payment intent routes | Order and invoice payment-intent routes create retry-payment sessions. | Keep current routes and request/response contracts. | Public DTO plus payment records. | Mobile retry-payment flow remains compatible. | Yes. | Mobile service and contract tests. |
| Member commerce mobile cache keys | Order and invoice history are cached in mobile shared by page, page size, and member token scope. | Keep member-scoped cache keys; no-token fallback may use only `anonymous`. | Mobile cache key convention. | Prevents cross-member history leakage. | Yes. | Mobile cache tests. |
| Future sales/finance expansion boundary | Current order/invoice foundation already serves member history and issued-document downloads. | Evolve this foundation; do not create a parallel member-facing sales/finance invoice for the same issued document. | Future structured modules must reference current order/invoice records. | No mobile contract split. | Yes: decision now, implementation later. | This review and future sales/finance design tests. |

## Compatibility Rules

- Issued order and invoice snapshots are immutable for member-facing history and archive behavior.
- Member-facing order/invoice contracts must not add parallel identifiers such as `salesInvoiceId`, `financeInvoiceId`, or `accountingDocumentId` for the same issued document surface.
- Member order contracts keep address snapshots as `billingAddressJson` and `shippingAddressJson`; mutable address object replacements are not allowed in this slice.
- Invoice archive, structured JSON, and structured XML routes remain under the current member invoice route family.
- Download/export methods must reject empty identifiers before API calls.
- History cache keys must remain member-scoped when a token subject exists and use `anonymous` only when no readable subject exists.

## No-Change Evidence

For this slice, the expected implementation evidence is:

- Contract serialization tests prove member order and invoice DTO field names remain stable.
- Contract guards prove no parallel sales/finance invoice identifiers or mutable address replacement fields are exposed.
- Mobile `MemberCommerceService` tests prove canonical routes for order/invoice list, detail, payment intent, document, archive, structured JSON, and structured XML.
- Mobile `MemberCommerceService` tests prove empty identifiers fail before network calls.
- Mobile cache tests prove order and invoice history are scoped by member token subject where available.
- Existing order placement tests prove saved and inline address snapshots remain compatible.

## Next Slice

`Foundation Implementation Consolidation Review` is the next release-sensitive slice.

Goal: summarize all P0 foundation guards, identify any confirmed pre-release gaps, and choose the first real domain/schema implementation after release-sensitive compatibility hardening.
