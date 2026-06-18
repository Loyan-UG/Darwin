# SyncState And SyncConflict Foundation Design

This document records the selected `SyncState` and `SyncConflict` foundation outcome. It adds internal Integration schema models and Application services for sync state and conflict evidence. It does not add a provider adapter, inbound connector, automatic merge engine, public/mobile/storefront contract, finance export format change, accounting API adapter, bank API, AI provider, or operational module mutation.

## Current Darwin Integration Findings

- `ExternalSystem` identifies external systems and their kind without storing credentials.
- `ExternalReference` owns external ids and source-of-truth hints for Darwin entities.
- Finance export file-delivery is outbound-only and production-safe when configured.
- AI provider activation and direct operational AI execution remain blocked until targets and command families are selected.
- There was no canonical sync state or conflict record before this slice; external references alone were not enough for inbound reconciliation.

## Locked Decisions

- `SyncState` is the per external-system/entity/direction/scope state record. It tracks status, attempts, retry timing, safe versions, and safe error summaries.
- `SyncConflict` is explicit conflict evidence linked to a `SyncState`; conflict history is not hidden in metadata or generic notes.
- Conflict records store safe summaries only. Raw external payloads, provider responses, credentials, tokens, private keys, connection strings, document contents, bank details, payroll internals, and sensitive HR data do not belong in sync metadata.
- Sync foundation is internal/provider-neutral. It does not call external systems, import data, push data, mutate operational aggregates, or resolve conflicts automatically.
- Conflict resolution records operator intent and safe summary evidence. Real business mutations must still go through the owning Application handlers for the affected module.
- `SyncScope` is required and defaults to `default`, so PostgreSQL and SQL Server both enforce stable unique sync-state identity.
- Public WebApi, mobile/member, storefront, invoice archive/download, finance export package format, payment/refund, supplier finance, payroll provider submission, bank settlement, and WebAdmin operational flows remain unchanged by this foundation.

## Implemented Foundation

| Surface | Implementation | Boundary |
| --- | --- | --- |
| Sync state model | `SyncState` in the `Integration` schema with external system, entity, direction, required scope, status, attempts, retry dates, safe versions, and metadata. | State only; no connector execution. |
| Conflict model | `SyncConflict` in the `Integration` schema with conflict key, field path, safe value summaries, resolution state, and resolution summary. | Evidence only; no automatic business mutation. |
| Application service | `SyncStateService` supports upsert state, record idempotent conflict, resolve conflict, and query open conflicts. | Rejects empty ids and sensitive metadata/summary terms. |
| Indexing | Unique active sync-state identity by system/entity/direction/scope; unique active conflict identity by system/entity/conflict key. | Prevents duplicate active evidence records. |
| Provider storage | PostgreSQL maps metadata to `jsonb`; SQL Server stores string JSON. | No provider-specific payload storage. |
| DI | Application registers `SyncStateService`. | No WebAdmin mutation or public route is added. |

## Decision Matrix

| Sync surface | Decision | Owning source | Mutation owner | Security boundary | Mobile/member impact | Next action |
| --- | --- | --- | --- | --- | --- | --- |
| External identity | Keep using `ExternalReference`. | Integration foundation. | External reference service. | Stable ids only, no raw provider payload. | None. | Reuse in future target adapters. |
| Sync state | Use `SyncState`. | Integration foundation. | Sync state service. | Safe status/error/version metadata only. | None. | Future inbound adapters update state. |
| Conflict evidence | Use `SyncConflict`. | Integration foundation. | Sync state service. | Safe summaries only. | None. | Future WebAdmin conflict review can expose records. |
| Conflict resolution | Record resolution evidence, do not mutate business objects directly. | Sync conflict record. | Future module-specific handlers for actual data changes. | Operator summary must be secret-free. | None. | Design target-specific resolution UX when target exists. |
| Automatic merge | Not implemented in this slice. | Future target-specific policy. | Future integration handlers. | No autonomous overwrite. | None. | Requires concrete inbound use case. |
| Connector retry | State can record attempts and next retry. | Future adapter orchestration. | Future connector handlers. | No raw provider response. | None. | Add per target adapter. |
| Accounting API sync | Not implemented. | Future accounting API adapter. | Future finance integration handler. | Must not bypass finance export evidence. | None. | Select real German accounting targets first. |

## Verification

- Unit coverage verifies idempotent sync state upsert, idempotent conflict recording, conflict resolution, open-conflict query filtering, and sensitive metadata rejection.
- Infrastructure coverage verifies `Integration` schema placement, stable max lengths, enum string conversion, unique filtered indexes, and PostgreSQL `jsonb` metadata.
- Migrations add only `SyncStates`, `SyncConflicts`, enum string model snapshots, indexes, and foreign keys for PostgreSQL and SQL Server.

## Remaining Gates

- Real inbound or two-way sync target selection.
- Target-specific payload mapping and identity matching.
- WebAdmin conflict review UX after a concrete operator workflow is selected.
- Accounting API adapter selection for widely used German accounting software.
- AI provider target selection remains separate and lower priority.
