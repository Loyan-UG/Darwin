# Tenant Migration Plan

Reviewed: 2026-06-19

## Summary

This document defines the safe migration path from current `BusinessId`-centric Darwin data to explicit `PlatformTenant` architecture. It is documentation-only and changes no schema, migration, entity, query, runtime behavior, route, DTO, worker, or provider flow.

The migration must be expand/contract, reviewable, provider-neutral, and compatible with shared, dedicated, dedicated-instance, and on-premise deployments.

## Migration Goals

- Introduce `PlatformTenant` without corrupting current business-scoped data.
- Preserve public, member, mobile, WebAdmin, worker, provider callback, checkout, invoice archive, finance export, and audit compatibility during rollout.
- Allow existing records to derive tenant through `BusinessId` until direct tenant columns are proven necessary.
- Support tenant moves from shared database to dedicated database.
- Track migration status per catalog database and tenant data store.

## Safe Rollout Sequence

| Step | Slice | Expected outcome |
| --- | --- | --- |
| 1 | Tenant Catalog And Domain Resolution Foundation Slice | Add tenant/domain/data-store metadata and a default tenant for existing businesses. No broad operational `TenantId` columns. |
| 2 | Business Tenant Ownership Slice | Add optional `PlatformTenantId` to `Business`, backfill existing businesses, enforce same-tenant business operations where needed. |
| 3 | Tenant Context Read Model Slice | Add resolver/read model so WebAdmin/WebApi/Worker can resolve tenant from host, token, and selected business without changing contracts. |
| 4 | Tenant-Aware Unique Index Review Slice | Review and selectively add tenant-aware uniqueness where business-derived scope is insufficient. |
| 5 | Tenant Data Store Routing Slice | Add data-store resolver and explicit non-default path tests before enabling dedicated database routing. |
| 6 | Dedicated Tenant Move Slice | Implement export/copy/verify/cutover for moving a tenant from shared to dedicated database. |
| 7 | Required Tenant Columns Slice | Add required tenant columns only to records that cannot safely derive tenant through business or another owner. |

## Existing Data Backfill

| Data family | Backfill rule |
| --- | --- |
| Existing businesses | Create one default `PlatformTenant` and assign all existing businesses unless an operator-provided mapping file is supplied. |
| Business-owned records | Derive tenant from owning `BusinessId`; do not mass-add duplicate tenant data in the first slice. |
| User memberships | Derive tenant through business memberships. |
| Provider callbacks | Link to tenant only when business/order/payment/shipment correlation is available. Otherwise keep parked platform evidence. |
| Audit/event records | Preserve history. Future records can include tenant when available; historical rows are not destructively rewritten. |
| Global settings and lookups | Remain platform/global unless a tenant settings model is introduced. |

## Expand And Contract Rules

| Phase | Rule |
| --- | --- |
| Expand | Add nullable metadata and read paths first. Backfill with deterministic scripts. |
| Verify | Run source-contract tests for host resolution, tenant-to-business ownership, unique indexes, API compatibility, and worker skip behavior. |
| Enforce | Add required constraints only after backfill and compatibility tests pass on shared and dedicated database lanes. |
| Contract | Remove compatibility bridges only after all supported deployments are migrated and rollback plans expire. |

## Multi-Database Migration Runner Requirements

- Discover tenant data stores from the hosted control-plane catalog.
- Apply provider-specific migrations to each active shared and dedicated database.
- Record migration status, start/end timestamps, operator/service actor, migration id, provider, database role, and safe error summary.
- Support resume without duplicate destructive work.
- Skip archived data stores unless an explicit restore/migration operation is running.
- In on-premise mode, run against the local single tenant data store using local configuration.

## Tenant Move Strategy

Moving a tenant from shared database to dedicated database requires:

1. Freeze tenant writes or enter controlled maintenance mode.
2. Snapshot tenant-owned data and owner-derived business records.
3. Restore/copy to target database with same schema and migration level.
4. Validate row counts, hashes for key tables, document/storage references, finance posting totals, order/invoice history, and user membership access.
5. Update `TenantDataStore` route in the catalog.
6. Run smoke tests for WebAdmin, WebApi, Worker, storefront, checkout return URLs, provider callbacks, object storage, and finance export.
7. Keep rollback route until the cutover window is accepted.

## Backup, Smoke, And Rollback

| Area | Requirement |
| --- | --- |
| Backup | Take catalog and tenant data-store backups before tenant schema enforcement and tenant moves. |
| Smoke | Validate tenant host resolution, selected business access, capability gating, checkout URLs, provider callback parking, email links, and finance export readiness. |
| Rollback | Rollback must restore catalog route state and tenant database state together. |
| Evidence | Store migration evidence as safe metadata, never raw secrets or full provider payloads. |

## First Implementation Slice Acceptance

The first slice is complete only when it proves tenant catalog/domain metadata can be added and queried without altering existing `BusinessId` contracts or runtime behavior.
