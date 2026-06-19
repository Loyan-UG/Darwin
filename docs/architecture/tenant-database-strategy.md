# Tenant Database Strategy

Reviewed: 2026-06-19

## Summary

This document defines future tenant database deployment strategy. It is documentation-only and changes no persistence registration, connection string, migration runner, data model, or runtime routing.

Darwin already has provider-neutral persistence with PostgreSQL as the default and SQL Server support. Tenant database routing must build on that foundation without forking schemas or creating tenant-specific EF models.

## Current Persistence Findings

| Current component | Evidence | Finding |
| --- | --- | --- |
| Provider selector | `AddConfiguredPersistence` reads `Persistence:Provider` and selects PostgreSQL or SQL Server. | Provider choice is host-level today. |
| Shared EF model | `DarwinDbContext` and provider-specific migration projects share one model. | The same schema must run in shared, dedicated, and on-premise modes. |
| Startup migration | `MigrateAndSeedAsync` migrates and seeds one configured database. | Multi-tenant hosted deployments need a migration orchestrator outside normal request startup. |
| Runbooks | `docs/persistence-providers.md` documents provider rules, schema rules, and migration validation. | Tenant database strategy must preserve provider-neutral rules and module schemas. |
| Data protection | Shared hosting data protection is configured by path, app name, and certificate. | Dedicated and on-premise deployments need explicit key-ring ownership. |

## Deployment Models

| Mode | Database model | App model | Use case |
| --- | --- | --- | --- |
| `HostedShared` | Multiple tenants in one Darwin database. | Shared app instances. | Small and standard hosted customers. |
| `HostedDedicatedDatabase` | One tenant has a dedicated Darwin database. | Shared or tenant-aware app instances route by tenant. | Large customers needing stronger data isolation or operational scale. |
| `HostedDedicatedInstance` | Dedicated Darwin database. | Dedicated app, worker, storage profiles, and domain configuration. | Enterprise hosted customers with high isolation requirements. |
| `OnPremise` | Customer-owned database. | Customer-owned app/worker/storage infrastructure. | Enterprise customers requiring local infrastructure or strict data residency. |

## Tenant Data Store Model

Future `TenantDataStore` should store:

- Tenant id.
- Deployment mode and isolation mode.
- Provider name: PostgreSQL or SQL Server.
- Database role: primary operational, reporting replica, archive, or migration staging when those roles exist.
- Secret reference for connection material.
- Safe display name and environment label.
- Migration status, last applied migration, last smoke timestamp, and move status.

It must not store raw connection strings, passwords, certificates, private keys, provider tokens, or cloud credentials.

## Control-Plane Catalog

Hosted deployments need a platform catalog database that stores tenants, domains, data-store routes, package assignments, and migration status. The catalog must be small, highly protected, and backed up separately from tenant operational databases.

On-premise deployments must be able to run without a live hosted catalog dependency. They can use an exported local catalog/configuration snapshot that defines the single local tenant, domains, package assignment, and data-store route.

## Routing Responsibilities

| Layer | Responsibility |
| --- | --- |
| Host/domain resolver | Resolve tenant before tenant-specific database work. |
| Tenant data-store resolver | Select shared or dedicated data store by tenant and operation type. |
| DbContext factory | Create `DarwinDbContext` for the selected data store without changing the EF model. |
| Migration orchestrator | Apply migrations across catalog, shared tenant databases, dedicated tenant databases, and on-premise deployments. |
| Worker scheduler | Resolve tenant batches and skip disabled or suspended tenants safely. |

## Same Schema Rule

All tenant operational databases must use the same Darwin schema and migration lane for a provider. Shared and dedicated database modes differ by routing and isolation, not by entity shape. Customer-specific schema forks are not allowed because they make migrations, support, finance export, mobile compatibility, and provider integrations brittle.

## Backup And Restore

| Deployment | Backup owner | Restore rule |
| --- | --- | --- |
| Hosted shared | Darwin platform operations. | Restore must support tenant-level export/move without corrupting other tenants. |
| Hosted dedicated database | Darwin platform operations, with tenant-specific backup evidence. | Restore can target the tenant database and must reconcile catalog route state. |
| Hosted dedicated instance | Darwin platform operations or agreed managed-service owner. | Restore includes app config, key ring, storage profiles, database, and catalog metadata. |
| On-premise | Customer infrastructure owner. | Darwin runbooks must define commands and smoke checks, but customer controls backup storage and secrets. |

## First Implementation Slice

`Tenant Catalog And Domain Resolution Foundation Slice` should add the catalog model and resolver interfaces first. Actual per-request database switching should come later, after tests prove tenant resolution and migration routing behavior.
