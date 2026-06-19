# On-Premise Deployment Architecture

Reviewed: 2026-06-19

## Summary

This document defines the on-premise tenant architecture for Darwin. It is documentation-only and changes no deployment scripts, runtime configuration, database schema, storage setup, provider adapter, or authentication behavior.

On-premise Darwin is a single-tenant installation operated on customer infrastructure. It uses the same application code, same EF model, same module schemas, same package/feature concepts, and same audit rules as hosted Darwin. It must not require a live Darwin-hosted control plane to run day-to-day operations.

## On-Premise Principles

| Area | Decision |
| --- | --- |
| Tenant identity | A local `PlatformTenant` record represents the customer installation. |
| Business model | One on-premise tenant can still own multiple `Business` records. |
| Database | Customer-owned PostgreSQL or SQL Server database using the same schema as hosted deployments. |
| Control plane | No runtime dependency on hosted control plane. Provisioning data can be imported as a signed or operator-approved configuration package. |
| Domains | Customer-owned admin, API, and storefront domains are configured locally and verified by deployment smoke. |
| Secrets | Secrets stay in customer secret store, environment, user-secrets equivalent, vault, or secure deployment configuration. Tenant metadata stores references only. |
| Data protection | Customer owns key-ring storage, certificate, and private-key access. |
| Object storage | Customer can use file-system, MinIO/S3-compatible, Azure Blob, or another supported profile if readiness checks pass. |

## Required Local Configuration

- Tenant id and tenant code.
- Active tenant status and deployment mode `OnPremise`.
- Domain records for admin, API, storefront, and provider callback surfaces used by the installation.
- Database provider and secret reference.
- Object-storage profile names and secret references.
- Package/plan assignment and feature overrides.
- Email/provider/storage readiness configuration.
- Data Protection key-ring path, application name, and certificate policy.

## Operational Boundaries

| Boundary | Rule |
| --- | --- |
| Updates | Application updates and database migrations must run through the same provider-specific migration lane as hosted deployments. |
| Support | Support bundles must redact secrets and raw provider payloads. |
| Telemetry | Any outbound telemetry or support sync must be opt-in and documented. |
| Provider callbacks | Public callback URLs are customer-owned. Provider configuration must use those verified URLs. |
| Mobile apps | Mobile base URLs point to customer API host when the tenant uses private infrastructure. |
| Finance export | Export file-delivery and future accounting API adapters use local storage/provider configuration. |

## Backup And Disaster Recovery

On-premise backup ownership belongs to the customer. Darwin documentation should provide:

- Database backup and restore commands per provider.
- Object-storage backup requirements.
- Data Protection key backup requirements.
- Configuration and secret-reference inventory.
- Post-restore smoke checks for WebAdmin, WebApi, Worker, storage, provider callbacks, checkout links, and finance export.

## Security Requirements

- No raw connection strings, credentials, private keys, access tokens, or provider payloads are stored in tenant/domain/package metadata.
- Admin cookies and member cookies remain separated by host and purpose.
- TLS termination must be configured for every public surface.
- Suspended or archived local tenant status must block tenant operations consistently, even in single-tenant on-premise mode.

## First Implementation Slice

On-premise support starts as metadata and documentation in `Tenant Catalog And Domain Resolution Foundation Slice`. Runtime installers, offline license handling, update packaging, and customer-managed smoke automation are later slices after the tenant catalog is in place.
