# Darwin Documentation Map

This is the documentation navigation hub for Darwin. Use it to find the authoritative document for each topic and to decide where future documentation updates belong.

## Start Here

- [README.md](../README.md): concise product overview, architecture summary, current execution focus, and getting-started pointers.
- [BACKLOG.md](../BACKLOG.md): active roadmap, current go-live blockers, near-term tasks, later-phase tasks, and open decisions.
- [docs/go-live-status.md](go-live-status.md): code-backed go-live readiness, provider status, and remaining production validation blockers.
- [docs/module-audit.md](module-audit.md): concise cross-module implementation and coverage matrix.

## Source Of Truth Matrix

| Topic | Canonical document | Supporting documents | Update when |
| --- | --- | --- | --- |
| Product overview and repository entry point | [README.md](../README.md) | [BACKLOG.md](../BACKLOG.md), [docs/go-live-status.md](go-live-status.md) | The platform scope, current execution focus, or top-level architecture changes. |
| Active roadmap and blockers | [BACKLOG.md](../BACKLOG.md) | [docs/go-live-status.md](go-live-status.md), [docs/module-audit.md](module-audit.md), [docs/compliance-decisions.md](compliance-decisions.md) | A go-live blocker, near-term task, later-phase task, or open decision changes. |
| Code-backed go-live readiness | [docs/go-live-status.md](go-live-status.md) | [docs/module-audit.md](module-audit.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md), [docs/production-setup.md](production-setup.md) | A verified implementation status, test result, provider readiness signal, or production blocker changes. |
| Cross-module implementation coverage | [docs/module-audit.md](module-audit.md) | [docs/go-live-status.md](go-live-status.md), [DarwinWebAdmin.md](../DarwinWebAdmin.md), [DarwinWebApi.md](../DarwinWebApi.md) | A module gains or loses domain, application, UI, API, worker, docs, or test coverage. |
| Production deployment and smoke order | [docs/production-setup.md](production-setup.md) | [docs/external-smoke-inputs.md](external-smoke-inputs.md), [docs/minio-storage-runbook.md](minio-storage-runbook.md), [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md) | Deployment prerequisites, secure configuration, provider setup, or smoke order changes. |
| External smoke variables and commands | [docs/external-smoke-inputs.md](external-smoke-inputs.md) | [docs/production-setup.md](production-setup.md), [docs/go-live-status.md](go-live-status.md) | A smoke script, environment variable name, or execution command changes. |
| Persistence provider architecture | [docs/persistence-providers.md](persistence-providers.md) | [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md), [docs/production-setup.md](production-setup.md) | PostgreSQL/SQL Server provider selection, migration, query behavior, or provider-specific rule changes. |
| PostgreSQL migration execution | [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md) | [docs/persistence-providers.md](persistence-providers.md), [docs/production-setup.md](production-setup.md) | PostgreSQL roles, grants, JSON conversion, migration execution, or post-migration checks change. |
| Compliance policy decisions | [docs/compliance-decisions.md](compliance-decisions.md) | [docs/go-live-status.md](go-live-status.md), [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md), [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | VAT/VIES, invoice archive, object storage, or e-invoice policy changes. |
| Object storage and invoice archive provider decision | [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | [docs/minio-storage-runbook.md](minio-storage-runbook.md), [docs/production-setup.md](production-setup.md), [docs/go-live-status.md](go-live-status.md) | MinIO/S3-compatible/Azure Blob support, immutability assumptions, provider requirements, or non-goals change. |
| Local MinIO and production MinIO checklist | [docs/minio-storage-runbook.md](minio-storage-runbook.md) | [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md), [docs/production-setup.md](production-setup.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md) | Local smoke setup, bucket Object Lock/versioning/retention steps, or production MinIO checklist changes. |
| E-invoice tooling | [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md) | [docs/compliance-decisions.md](compliance-decisions.md), [docs/go-live-status.md](go-live-status.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md) | The selected generator, wrapper, validation profile, non-goals, or retained alternatives change. |
| Domain model and module rules | [DarwinDomainDesign.md](../DarwinDomainDesign.md) | [docs/compliance-decisions.md](compliance-decisions.md), [DarwinWebApi.md](../DarwinWebApi.md), [DarwinWebAdmin.md](../DarwinWebAdmin.md) | Entities, aggregate rules, lifecycle policies, or cross-module domain boundaries change. |
| WebAdmin operations | [DarwinWebAdmin.md](../DarwinWebAdmin.md) | [docs/go-live-status.md](go-live-status.md), [docs/module-audit.md](module-audit.md), [howto-identity-access.md](../howto-identity-access.md) | Back-office workflows, HTMX conventions, controller/view responsibilities, or admin operational scope changes. |
| Front-office and member portal | [DarwinFrontEnd.md](../DarwinFrontEnd.md) | [DarwinWebApi.md](../DarwinWebApi.md), [docs/go-live-status.md](go-live-status.md), [src/Darwin.Web/README.md](../src/Darwin.Web/README.md) | Public/member UX rules, customer-facing page map, API consumption, localization, or security boundaries change. |
| Mobile applications | [DarwinMobile.md](../DarwinMobile.md) | [DarwinMobile.Guidelines.md](../DarwinMobile.Guidelines.md), [DarwinWebApi.md](../DarwinWebApi.md), [docs/go-live-status.md](go-live-status.md) | MAUI app responsibilities, mobile/backend dependencies, approval gates, or notification flows change. |
| WebApi contracts | [DarwinWebApi.md](../DarwinWebApi.md) | [DarwinFrontEnd.md](../DarwinFrontEnd.md), [DarwinMobile.md](../DarwinMobile.md), [docs/go-live-status.md](go-live-status.md) | Route roots, audience boundaries, webhook rules, or DTO contract ownership changes. |
| Identity and access operations | [howto-identity-access.md](../howto-identity-access.md) | [DarwinWebAdmin.md](../DarwinWebAdmin.md), [DarwinWebApi.md](../DarwinWebApi.md), [CONTRIBUTING.md](../CONTRIBUTING.md) | Identity handlers, RowVersion rules, permission checks, or admin/public contract separation changes. |
| Testing strategy | [DarwinTesting.md](../DarwinTesting.md) | [docs/go-live-status.md](go-live-status.md), [docs/module-audit.md](module-audit.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md) | Test lanes, commands, source-contract policy, smoke status, or priority test queue changes. |
| Contribution standards | [CONTRIBUTING.md](../CONTRIBUTING.md) | [docs/README.md](README.md), [DarwinTesting.md](../DarwinTesting.md) | Engineering standards, architecture dependency rules, coding standards, or change discipline changes. |
| Historical implementation ledger | [docs/implementation-ledger.md](implementation-ledger.md) | [BACKLOG.md](../BACKLOG.md), [docs/go-live-status.md](go-live-status.md), [docs/module-audit.md](module-audit.md) | Historical progress notes or cross-chat handoff context needs to be preserved without polluting active guides. |

## Document Groups

### Product / Roadmap

- [README.md](../README.md)
- [BACKLOG.md](../BACKLOG.md)
- [docs/go-live-status.md](go-live-status.md)
- [docs/module-audit.md](module-audit.md)

### Architecture

- [DarwinDomainDesign.md](../DarwinDomainDesign.md)
- [DarwinWebApi.md](../DarwinWebApi.md)
- [docs/persistence-providers.md](persistence-providers.md)
- [CONTRIBUTING.md](../CONTRIBUTING.md)

### Delivery Applications

- [DarwinWebAdmin.md](../DarwinWebAdmin.md)
- [DarwinFrontEnd.md](../DarwinFrontEnd.md)
- [DarwinMobile.md](../DarwinMobile.md)
- [DarwinMobile.Guidelines.md](../DarwinMobile.Guidelines.md)
- [src/Darwin.Web/README.md](../src/Darwin.Web/README.md)
- [src/Darwin.Web/AGENTS.md](../src/Darwin.Web/AGENTS.md)

### Operations / Runbooks

- [docs/production-setup.md](production-setup.md)
- [docs/external-smoke-inputs.md](external-smoke-inputs.md)
- [docs/minio-storage-runbook.md](minio-storage-runbook.md)
- [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md)

### Compliance

- [docs/compliance-decisions.md](compliance-decisions.md)
- [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md)
- [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md)

### Persistence

- [docs/persistence-providers.md](persistence-providers.md)
- [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md)

### Testing

- [DarwinTesting.md](../DarwinTesting.md)
- [docs/external-smoke-inputs.md](external-smoke-inputs.md)
- [docs/go-live-status.md](go-live-status.md)

### Historical Ledger

- [docs/implementation-ledger.md](implementation-ledger.md)
- [BACKLOG v1.md](../BACKLOG%20v1.md)
- [docs/BACKLOG.md](BACKLOG.md)

## Do Not Duplicate

- Current status belongs in [docs/go-live-status.md](go-live-status.md) and [docs/module-audit.md](module-audit.md).
- Tasks belong in [BACKLOG.md](../BACKLOG.md).
- Exact smoke input names and commands belong in [docs/external-smoke-inputs.md](external-smoke-inputs.md).
- Production deployment steps belong in [docs/production-setup.md](production-setup.md).
- Local MinIO setup details belong in [docs/minio-storage-runbook.md](minio-storage-runbook.md).
- Historical implementation notes belong in [docs/implementation-ledger.md](implementation-ledger.md), not in README, BACKLOG, WebAdmin, or FrontEnd active guides.

## Missing Or Stale Links

- A stale README handoff link for the old WebAdmin subscription workspace was removed because the target file is not present. Durable WebAdmin subscription context is now covered by [DarwinWebAdmin.md](../DarwinWebAdmin.md), and historical details remain in [docs/implementation-ledger.md](implementation-ledger.md).
