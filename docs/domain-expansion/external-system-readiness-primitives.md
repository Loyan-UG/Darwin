# External-System Readiness Primitives

## Purpose

This slice adds the first structured external-system foundation primitives before broad ERP module expansion. The goal is to prevent future CRM, sales, purchasing, inventory, finance, HR, and automation work from adding one-off external identifiers, provider payloads, or JSON-only ownership rules.

This is a foundation slice, not a sync engine, conflict-resolution UI, module expansion, or public/mobile API change.

## Current Darwin Model Findings

| Surface | Current state | Decision | Release impact | Notes |
| --- | --- | --- | --- | --- |
| User external identifiers | `User.ExternalIdsJson` already exists for identity-adjacent data. | Keep unchanged in this slice. | No mobile contract change. | Do not migrate or remove existing fields until a deliberate identity migration is planned. |
| Payment and shipment references | Payment and shipment flows already carry provider-specific references. | Keep existing columns and behavior. | No commerce contract change. | The new primitives are additive and do not replace issued order/invoice source behavior. |
| Integration inbox/outbox records | Integration schema already contains callback, dispatch, webhook, and event records. | Add structured registry/reference primitives to the same schema. | No route or DTO change. | This keeps integration foundation in one persistence area. |
| Source ownership | Ownership rules are currently implicit or local to each flow. | Add `SourceOfTruth` on external references first. | No mobile contract change. | Do not add ownership columns to `User`, `Business`, `Customer`, `Order`, or `Invoice` in this slice. |

## Implemented Primitives

| Primitive | Shape | Decision | Storage | Notes |
| --- | --- | --- | --- | --- |
| `ExternalSystem` | Structured entity in `Integration` schema. | Add now. | Real columns for `Code`, `Name`, `Kind`, `BaseUrl`, `Description`, `IsActive`, and `MetadataJson`. | No secrets, tokens, or provider payloads belong here. |
| `ExternalReference` | Structured entity in `Integration` schema. | Add now. | Real columns for lookup, uniqueness, ownership, active state, and optional metadata. | Uses `EntityType` + `EntityId` so modules are not hard-coupled by cross-module FKs. |
| `SourceOfTruth` | Enum. | Add now on external references. | Real enum-converted column. | Values describe whether the local record, external record, or both are authoritative. |
| `SyncState` | Deferred. | Do not implement now. | No table or column. | Add only when an active sync lifecycle exists. |
| `SyncConflict` | Deferred. | Do not implement now. | No table or column. | Add with conflict workflow, not as unused schema. |

## Compatibility Rules

| Rule | Decision |
| --- | --- |
| Public/mobile routes and DTOs | No change in this slice. |
| Loyalty balance and scan/session contracts | No change in this slice. |
| Order/invoice snapshots and issued documents | No change in this slice. |
| Existing provider-specific references | Keep unchanged; do not migrate implicitly. |
| Secrets and auth material | Do not store in `ExternalSystem`, `ExternalReference`, or metadata JSON. |
| Custom payloads | Use `MetadataJson` only for non-secret, uncertain, low-query metadata. Reportable identity and ownership fields are real columns. |

## Evidence And Tests

| Evidence | Coverage |
| --- | --- |
| Unit tests | Code normalization, required validation, idempotent upsert, active entity lookup, and inactive/deleted exclusion. |
| Infrastructure tests | Schema placement, max lengths, required external identity columns, lookup indexes, uniqueness filters, and PostgreSQL JSON metadata mapping. |
| Migration evidence | PostgreSQL and SQL Server migrations must add only the two new tables plus expected indexes and constraints. |
| Documentation check | Restricted-term scan must stay clean. |

## Next Slice

After this slice, the next planned foundation slice is `Custom Fields And Activity/Attachment Foundation Slice`, unless implementation evidence shows that `SyncState` or `SyncConflict` is release-sensitive and must be pulled forward.
