# BusinessEvent And AuditTrail Foundation

## Purpose

This slice adds shared foundation records for business facts, audit evidence, automation context, and future AI-readable history before broad ERP module expansion.

The implementation is additive. It does not change public/mobile routes, DTOs, loyalty contracts, order/invoice snapshots, invoice archive/download behavior, or existing operational flows.

## Implemented Primitives

| Primitive | Decision | Storage | Notes |
| --- | --- | --- | --- |
| `BusinessEvent` | Add now. | Structured table in `Foundation` schema. | Records business facts with entity targeting, source, severity, visibility, correlation, payload, and metadata. |
| `AuditTrail` | Add now. | Structured table in `Foundation` schema. | Records auditable actions for a specific entity with optional link to a business event. |
| `BusinessEventService` | Add now. | Internal application service. | Supports add/query only. The service does not expose update or delete operations. |
| Flow instrumentation | Defer. | No behavior change. | Existing handlers are not wired to emit shared events in this slice. |
| Database append-only enforcement | Defer. | No trigger or provider-specific policy in this slice. | Append-only is enforced by the internal service surface for v1. |

## Non-Replacement Rules

| Existing surface | Decision |
| --- | --- |
| Loyalty ledger | Not replaced. Points balance and points audit remain owned by loyalty transactions and account projections. |
| Invoice archive/source model | Not replaced. Issued archive, structured JSON/XML, retention, and download behavior remain unchanged. |
| Provider operation records | Not replaced. Payment, shipment, callback, dispatch, webhook, and communication operation records keep their specialized lifecycle. |
| Existing dispatch audits | Not replaced. Communication audit records remain authoritative for delivery diagnostics and retry behavior. |
| `Activity` / `Note` | Not replaced. Timeline and human notes remain operational collaboration records, while `BusinessEvent` and `AuditTrail` are evidence-oriented foundation records. |
| Public/mobile contracts | Not changed. No new route, DTO, mobile service, or serialization contract is introduced in this slice. |

## Storage And Safety Rules

| Area | Rule |
| --- | --- |
| Entity targeting | Generic `EntityType` plus optional `EntityId` on events and required `EntityId` on audit trails keep the foundation cross-module without hard FKs. |
| Idempotency | `BusinessEvent.EventKey` is optional and unique when present. Repeated adds with the same active key return the existing event id. |
| Visibility | `FoundationVisibility` controls whether a record is internal, staff, business, member, or public safe. Mobile-safe exposure must be explicit in later API work. |
| Correlation | `CorrelationId` and `CausationId` support grouped workflows without changing route or DTO contracts. |
| Sensitive data | Secrets, credentials, auth tokens, refresh tokens, private keys, and raw sensitive provider payloads must not be stored in payload, change-set, or metadata JSON. |
| JSON | JSON fields are for evidence payloads and low-query metadata. Query-critical fields remain real columns. |

## Evidence And Tests

| Evidence | Coverage |
| --- | --- |
| Unit tests | Event add/query ordering, visibility filtering, idempotent event key, correlation lookup, audit trail add/query, validation, and sensitive-data rejection. |
| Infrastructure tests | `Foundation` schema placement, max lengths, enum string conversion, indexes, unique filtered `EventKey`, and PostgreSQL `jsonb` mapping. |
| Compatibility tests | Existing contract and mobile route/service lanes prove no public/mobile contract changes. |
| Documentation scan | Restricted-term scan must stay clean. |

## Next Slice

The next planned foundation slice is `FeatureArea / Module Visibility Foundation Slice`, unless implementation evidence shows that selected release-sensitive flows should be instrumented with `BusinessEventService` first.
