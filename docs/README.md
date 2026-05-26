# Darwin Documentation Map

This is the navigation hub for Darwin documentation. Keep this file short and use it to decide which document is authoritative for each topic.

## Start Here

- [README.md](../README.md): product overview, architecture summary, current direction, and quick start.
- [BACKLOG.md](../BACKLOG.md): active roadmap, blockers, near-term tasks, later-phase tasks, and open decisions.
- [docs/go-live-status.md](go-live-status.md): code-backed readiness status and production blockers.
- [docs/module-audit.md](module-audit.md): cross-module implementation and coverage matrix.

## Source Of Truth Matrix

| Topic | Canonical document | Supporting documents | Update when |
| --- | --- | --- | --- |
| Product overview | [README.md](../README.md) | [BACKLOG.md](../BACKLOG.md) | Platform scope, delivery surfaces, or strategic direction changes. |
| Roadmap and blockers | [BACKLOG.md](../BACKLOG.md) | [docs/go-live-status.md](go-live-status.md) | A blocker, near-term task, later-phase task, or open decision changes. |
| Go-live readiness | [docs/go-live-status.md](go-live-status.md) | [docs/module-audit.md](module-audit.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md) | Verified status, provider readiness, or production blocker changes. |
| Module coverage | [docs/module-audit.md](module-audit.md) | [docs/go-live-status.md](go-live-status.md) | Domain, application, UI, API, worker, docs, or tests change for a module. |
| Production setup | [docs/production-setup.md](production-setup.md) | [docs/external-smoke-inputs.md](external-smoke-inputs.md), [docs/minio-storage-runbook.md](minio-storage-runbook.md) | Deployment prerequisites, secure configuration, provider setup, or smoke order changes. |
| External smoke inputs | [docs/external-smoke-inputs.md](external-smoke-inputs.md) | [docs/production-setup.md](production-setup.md) | Smoke scripts, environment variable names, commands, or acceptance criteria change. |
| Persistence | [docs/persistence-providers.md](persistence-providers.md) | [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md) | PostgreSQL/SQL Server provider behavior, migrations, or runtime selection changes. |
| PostgreSQL migration | [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md) | [docs/persistence-providers.md](persistence-providers.md) | PostgreSQL roles, grants, migration execution, or validation steps change. |
| Compliance decisions | [docs/compliance-decisions.md](compliance-decisions.md) | [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md), [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | VAT/VIES, archive, retention, or e-invoice policy changes. |
| Object storage | [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | [docs/minio-storage-runbook.md](minio-storage-runbook.md), [docs/production-setup.md](production-setup.md) | Provider support, immutability assumptions, or deployment rules change. |
| MinIO runbook | [docs/minio-storage-runbook.md](minio-storage-runbook.md) | [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | Local smoke, bucket setup, Object Lock, retention, or production checklist changes. |
| E-invoice tooling | [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md) | [docs/compliance-decisions.md](compliance-decisions.md) | Generator, wrapper, validation profile, evidence, or retained alternatives change. |
| Domain model | [DarwinDomainDesign.md](../DarwinDomainDesign.md) | [DarwinWebApi.md](../DarwinWebApi.md) | Entity, aggregate, lifecycle, or cross-module domain rules change. |
| WebAdmin | [DarwinWebAdmin.md](../DarwinWebAdmin.md) | [howto-identity-access.md](../howto-identity-access.md) | Back-office workflows, HTMX conventions, or operational support surfaces change. |
| Front-office | [DarwinFrontEnd.md](../DarwinFrontEnd.md) | [src/Darwin.Web/README.md](../src/Darwin.Web/README.md), [DarwinWebApi.md](../DarwinWebApi.md) | Public/member UX, API consumption, or customer-facing boundaries change. |
| Mobile | [DarwinMobile.md](../DarwinMobile.md) | [DarwinMobile.Guidelines.md](../DarwinMobile.Guidelines.md), [DarwinWebApi.md](../DarwinWebApi.md) | MAUI responsibilities, mobile routes, release readiness, or backend dependencies change. |
| WebApi | [DarwinWebApi.md](../DarwinWebApi.md) | [DarwinMobile.md](../DarwinMobile.md), [DarwinFrontEnd.md](../DarwinFrontEnd.md) | Route roots, audience boundaries, DTO ownership, or webhook rules change. |
| Identity/access | [howto-identity-access.md](../howto-identity-access.md) | [DarwinWebAdmin.md](../DarwinWebAdmin.md), [CONTRIBUTING.md](../CONTRIBUTING.md) | Identity handlers, permissions, RowVersion rules, or role boundaries change. |
| Testing | [DarwinTesting.md](../DarwinTesting.md) | [docs/go-live-status.md](go-live-status.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md) | Test lanes, commands, source-contract policy, or priority gaps change. |
| Contribution rules | [CONTRIBUTING.md](../CONTRIBUTING.md) | [DarwinTesting.md](../DarwinTesting.md) | Engineering standards or change discipline changes. |
| Historical ledger | [docs/implementation-ledger.md](implementation-ledger.md) | [BACKLOG.md](../BACKLOG.md), [docs/go-live-status.md](go-live-status.md) | Historical context needs to be preserved outside active guides. |

## Document Groups

### Product And Roadmap

- [README.md](../README.md)
- [BACKLOG.md](../BACKLOG.md)
- [docs/go-live-status.md](go-live-status.md)
- [docs/module-audit.md](module-audit.md)

### Architecture

- [DarwinDomainDesign.md](../DarwinDomainDesign.md)
- [DarwinWebApi.md](../DarwinWebApi.md)
- [docs/persistence-providers.md](persistence-providers.md)
- [CONTRIBUTING.md](../CONTRIBUTING.md)

### Applications

- [DarwinWebAdmin.md](../DarwinWebAdmin.md)
- [DarwinFrontEnd.md](../DarwinFrontEnd.md)
- [DarwinMobile.md](../DarwinMobile.md)
- [DarwinMobile.Guidelines.md](../DarwinMobile.Guidelines.md)
- [src/Darwin.Web/README.md](../src/Darwin.Web/README.md)
- [src/Darwin.Web/AGENTS.md](../src/Darwin.Web/AGENTS.md)

### Operations And Runbooks

- [docs/production-setup.md](production-setup.md)
- [docs/external-smoke-inputs.md](external-smoke-inputs.md)
- [docs/minio-storage-runbook.md](minio-storage-runbook.md)
- [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md)

### Compliance

- [docs/compliance-decisions.md](compliance-decisions.md)
- [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md)
- [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md)

### Testing

- [DarwinTesting.md](../DarwinTesting.md)
- [docs/external-smoke-inputs.md](external-smoke-inputs.md)

### Historical

- [docs/implementation-ledger.md](implementation-ledger.md)

## Do Not Duplicate

- Current status belongs in `docs/go-live-status.md` and `docs/module-audit.md`.
- Tasks belong in `BACKLOG.md`.
- Smoke input names and commands belong in `docs/external-smoke-inputs.md`.
- Production setup steps belong in `docs/production-setup.md`.
- Local MinIO details belong in `docs/minio-storage-runbook.md`.
- Historical progress belongs in `docs/implementation-ledger.md`.
- Deployment-specific domains, tenant names, emails, credentials, and provider keys do not belong in active documentation.
