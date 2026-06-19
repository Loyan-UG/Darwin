# Master Data Import And Coexistence Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's master-data import and coexistence boundary for customers that migrate from or run alongside external systems. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, worker, adapter, sync job, or production flow.

Decision: Import/coexistence is a governed Integration capability using `ExternalSystem`, `ExternalReference`, `SyncState`, and `SyncConflict`. It must not be ad hoc CSV upload directly into domain tables or target-specific code hidden in module handlers.

## Current Darwin Integration Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| `ExternalSystem` and `ExternalReference` | External identity foundation exists. | Use for source system identity and import source references. |
| `SyncState` and `SyncConflict` | Provider-neutral sync evidence exists. | Use for target-specific coexistence state and conflict evidence. |
| Domain handlers | Many modules have validation and lifecycle handlers. | Import must call owning handlers or approved import services, not bypass invariants. |
| Finance export file delivery | Outbound export foundation exists. | Import/coexistence is separate from finance export package delivery. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Migration import | Customers can move master data into Darwin with validation and evidence. | Requires import batch, mapping, validation report, staged rows, apply step. |
| Coexistence mapping | Darwin can work beside existing systems. | Requires stable external references and source-of-truth policy. |
| Conflict review | Operators can resolve mismatches safely. | Requires SyncConflict workflow and no silent overwrite. |
| Rollback evidence | Failed import can be traced and corrected. | Requires batch status, row state, applied entity refs, audit. |
| Target adapters | Selected external systems can synchronize. | Requires target-specific design and credential policy. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `ImportBatch` | integrations-sync | business/tenant, external system, module, source file/document, status, row counts, validation summary. |
| `ImportMappingProfile` | integrations-sync | module, source format, field mappings, transformation rules, status. |
| `ImportStagedRow` | integrations-sync | batch, row number, source identity, normalized payload snapshot, validation status, target entity reference. |
| `ImportValidationIssue` | integrations-sync | row, severity, field, code, safe message. |
| `ImportApplyResult` | integrations-sync | batch, target entity type/id, action, status, event reference. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Import batch | `Uploaded -> Mapped -> Validated -> Approved -> Applied` or `Cancelled`; failed rows remain evidence. |
| Mapping profile | `Draft -> Active -> Archived`; mapping changes do not rewrite completed batches. |
| Conflict | Use existing `SyncConflict` lifecycle for active target-specific conflicts. |

## Application Surface

Future handlers:

- Create import batch and register document evidence.
- Define/select mapping profile.
- Validate staged rows with domain-specific validators.
- Approve apply.
- Apply using owning module handlers or dedicated import services that preserve invariants.
- Record external references and sync state.
- Review conflicts.

## WebAdmin Surface

Integration WebAdmin should include import batches, mapping profiles, validation report, row detail, apply review, conflict queue, and read-only links to target entities and external references.

No public/member/mobile route is added by default.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | `integrations-sync` plus future `master-data-import`. |
| Package role | Enterprise add-on and migration tool. |
| Required dependencies | Owning target module; `integrations-sync`. |
| Disabled behavior | Hide import/sync pages; target modules remain usable. |
| Permissions | Manage mappings, upload import, approve apply, resolve conflicts. |
| SoD | Apply import into production data is an approval candidate. |

## Compatibility Boundaries

- No direct table writes that bypass module invariants.
- No raw credentials, connection strings, or full sensitive provider payload in metadata.
- No target-specific adapter without target selection, credential owner, payload mapping, error contract, and smoke strategy.
- Finance export format is unchanged.

## Implementation Slices

1. `Import Batch And Mapping Profile Boundary/Core Slice`.
2. `Validation Report And Staging Slice`.
3. `Apply Through Owner Handlers Slice`.
4. `Conflict Review WebAdmin Slice`.
5. Target-specific adapter designs only after a real target is selected.

## Test Plan

Future tests must cover mapping validation, staged-row safety, duplicate external identities, no direct bypass, conflict evidence, rollback/audit, WebAdmin anti-forgery, source guards, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement import/coexistence workflows.
