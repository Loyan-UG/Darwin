# Custom Fields And Activity/Document Foundation

## Purpose

This slice adds shared foundation primitives for customer-specific data, operational timeline entries, human notes, and document metadata before broad ERP module expansion. The implementation is additive and does not change public/mobile contracts, object upload/download behavior, loyalty contracts, order/invoice snapshots, or invoice archive behavior.

## Implemented Primitives

| Primitive | Decision | Storage | Notes |
| --- | --- | --- | --- |
| `CustomFieldDefinition` | Add now. | Structured table in `Foundation` schema. | Defines approved custom fields for customer-specific, uncertain, or low-frequency data. |
| `CustomFieldValue` | Add now. | Structured table with typed value columns plus JSON value. | Values are scoped by definition and `EntityType`/`EntityId`. |
| `Activity` | Add now. | Structured timeline table. | Supports cross-module operational timelines without replacing specialized ledgers or audit records. |
| `Note` | Add now. | Structured note table. | Supports multiple notes per entity with explicit visibility. |
| `DocumentRecord` | Add now. | Structured metadata table with storage reference. | Tracks document metadata only; upload/download remains owned by existing storage services. |
| `NumberSequence` | Defer. | No table in this slice. | Needs document-numbering policy before sales/finance expansion. |
| `BusinessEvent` / `AuditTrail` | Defer. | No table in this slice. | Needs append-only event policy and automation/AI read rules. |
| `FeatureArea` | Defer. | No table in this slice. | Needs UI, permission, and feature-visibility policy. |

## Non-Replacement Rules

| Existing surface | Decision |
| --- | --- |
| Loyalty ledger | Not replaced. Points balance and audit remain owned by loyalty transactions and account projections. |
| Invoice archive/source model | Not replaced. Issued archive, structured JSON/XML, download behavior, retention, and source-model rules remain unchanged. |
| Media assets | Not replaced. `MediaAsset` remains the media library record; `DocumentRecord.MediaAssetId` is only an optional link. |
| Provider operation records | Not replaced. Payment, shipment, callback, dispatch, and webhook operation records keep their specialized lifecycle. |
| Compliance/audit records | Not replaced. This slice does not implement audit evidence or append-only business events. |
| Object storage | Not changed. `DocumentRecord` stores provider/container/key metadata only and does not upload, delete, or read payloads. |

## Storage And Visibility Rules

| Area | Rule |
| --- | --- |
| Custom fields | Use only for customer-specific, uncertain, or low-frequency data. Common/reportable/cross-module/compliance/integration identity fields remain real columns. |
| Sensitive data | Secrets, credentials, auth tokens, refresh tokens, raw provider payloads, and private keys must not be stored in custom fields, notes, or document metadata. |
| Visibility | `FoundationVisibility` is explicit on activities, notes, and documents. Internal content is not mobile-safe unless a caller explicitly asks for a visibility level that allows it. |
| Entity targeting | Generic `EntityType` + `EntityId` keeps foundation primitives cross-module without hard-coupling every aggregate by foreign key. |
| JSON | JSON columns are reserved for validation hints, low-query metadata, and unstructured values; query-critical fields remain real columns. |

## Evidence And Tests

| Evidence | Coverage |
| --- | --- |
| Unit tests | Custom field key normalization, duplicate rejection, active-definition filtering, typed value upsert, activity/note visibility and ordering, document metadata registration. |
| Infrastructure tests | `Foundation` schema placement, max lengths, enum string conversion, indexes, uniqueness filters, decimal precision, and PostgreSQL `jsonb` mapping. |
| Compatibility tests | Existing contract and mobile route/service lanes prove no public/mobile contract changes. |
| Documentation scan | Restricted-term scan must stay clean. |

## Next Slice

The next planned foundation slice is `Number Sequence Foundation Slice`, unless implementation evidence shows `BusinessEvent/AuditTrail` is more urgent for release evidence, automation, or AI-readiness.
