# Number Sequence Foundation

## Purpose

This slice adds a shared number-sequence foundation for future ERP documents. It prepares Darwin to generate stable document numbers for orders, invoices, purchasing, inventory, finance, and HR without adding more ad hoc generators.

This implementation is additive. It does not change public/mobile contracts, issued order numbers, issued invoice archive behavior, order/invoice snapshots, or member-facing downloads.

## Implemented Primitive

| Primitive | Decision | Storage | Notes |
| --- | --- | --- | --- |
| `NumberSequence` | Add now. | Structured table in `Foundation` schema. | Stores document type, scope, pattern, next value, reset policy, active state, and metadata. |
| `NumberSequenceService` | Add now. | Internal application service. | Supports preview and reserve operations. Reservation increments the sequence; preview does not. |
| Order creation integration | Add guarded use now. | Existing `Order.OrderNumber` remains the only stored order number. | If an active order sequence exists, it is used; otherwise the previous fallback generator stays active. |
| Purchase order integration | Defer. | No behavior change. | `PurchaseOrder.OrderNumber` remains manually supplied until purchasing expansion. |
| Invoice numbering | Defer. | No new invoice number column. | Current invoice projections that use linked order numbers remain unchanged. |

## Formatting Rules

| Rule | Decision |
| --- | --- |
| Pattern placeholders | Support `{yyyy}`, `{yy}`, `{MM}`, `{dd}`, and `{seq}`. |
| Required placeholder | `{seq}` is required. |
| Sequence padding | `{seq}` uses `PaddingLength`, currently 1 to 12. |
| Reset policy | `Never`, `Daily`, `Monthly`, and `Yearly` based on UTC. |
| Scope | `BusinessId` plus `DocumentType` plus `ScopeKey`; global sequences use `BusinessId = null`. |
| Nullable uniqueness | Global and business-scoped unique indexes are separate so nullable `BusinessId` is handled correctly across providers. |

## Compatibility Rules

| Surface | Decision |
| --- | --- |
| Existing order numbers | Not changed or migrated. |
| Order/mobile contracts | Not changed. |
| Invoice archive/source model | Not changed. |
| Member order/invoice history | Not changed. |
| Purchase order entry | Not changed in this slice. |
| Missing sequence | Existing order number fallback remains active. |

## Evidence And Tests

| Evidence | Coverage |
| --- | --- |
| Unit tests | Preview without increment, reserve increment, padding, UTC period reset, duplicate prevention, invalid pattern rejection, and order fallback/use. |
| Infrastructure tests | `Foundation` schema placement, max lengths, enum string conversion, unique filters, and PostgreSQL `jsonb` mapping. |
| Compatibility tests | Existing order, invoice, checkout, commerce, and mobile route/service lanes remain unchanged. |
| Documentation scan | Restricted-term scan must stay clean. |

## Next Slice

The next planned foundation slice is `BusinessEvent/AuditTrail Foundation Slice`, unless sequence implementation reveals a stronger need to implement feature-area visibility first.
