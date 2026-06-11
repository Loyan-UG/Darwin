# Darwin Documentation Map

This is the navigation hub for Darwin documentation. Keep this file short and use it to decide which document is authoritative for each topic. The root README is the product and platform entry point; this file is the map for deeper architecture, operations, compliance, and testing material.

## Start Here

- [README.md](../README.md): product overview, platform capabilities, architecture summary, current direction, and quick start.
- [BACKLOG.md](../BACKLOG.md): active roadmap, blockers, near-term tasks, later-phase tasks, and open decisions.
- [docs/go-live-status.md](go-live-status.md): code-backed readiness status and production blockers.
- [docs/module-audit.md](module-audit.md): cross-module implementation and coverage matrix.

## Source Of Truth Matrix

| Topic | Canonical document | Supporting documents | Update when |
| --- | --- | --- | --- |
| Product overview and value proposition | [README.md](../README.md) | [BACKLOG.md](../BACKLOG.md) | Platform scope, delivery surfaces, business value, or strategic direction changes. |
| Roadmap and blockers | [BACKLOG.md](../BACKLOG.md) | [docs/go-live-status.md](go-live-status.md) | A blocker, near-term task, later-phase task, or open decision changes. |
| Go-live readiness | [docs/go-live-status.md](go-live-status.md) | [docs/module-audit.md](module-audit.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md) | Verified status, provider readiness, or production blocker changes. |
| Module coverage | [docs/module-audit.md](module-audit.md) | [docs/go-live-status.md](go-live-status.md) | Domain, application, UI, API, worker, docs, or tests change for a module. |
| ERP domain expansion | [docs/domain-expansion/domain-capability-catalog.md](domain-expansion/domain-capability-catalog.md) | [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [docs/domain-expansion/release-sensitive-foundation-implementation-plan.md](domain-expansion/release-sensitive-foundation-implementation-plan.md), [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md), [docs/domain-expansion/loyalty-ledger-scan-compatibility-review.md](domain-expansion/loyalty-ledger-scan-compatibility-review.md), [docs/domain-expansion/identity-profile-customer-bridge-compatibility-review.md](domain-expansion/identity-profile-customer-bridge-compatibility-review.md), [docs/domain-expansion/business-businessmember-access-compatibility-review.md](domain-expansion/business-businessmember-access-compatibility-review.md), [docs/domain-expansion/order-invoice-snapshot-compatibility-review.md](domain-expansion/order-invoice-snapshot-compatibility-review.md), [docs/domain-expansion/foundation-implementation-consolidation-review.md](domain-expansion/foundation-implementation-consolidation-review.md), [docs/domain-expansion/external-system-readiness-primitives.md](domain-expansion/external-system-readiness-primitives.md), [docs/domain-expansion/custom-fields-activity-document-foundation.md](domain-expansion/custom-fields-activity-document-foundation.md), [docs/domain-expansion/number-sequence-foundation.md](domain-expansion/number-sequence-foundation.md), [docs/domain-expansion/business-event-audit-trail-foundation.md](domain-expansion/business-event-audit-trail-foundation.md), [docs/domain-expansion/feature-area-module-visibility-foundation.md](domain-expansion/feature-area-module-visibility-foundation.md), [BACKLOG.md](../BACKLOG.md), [DarwinDomainDesign.md](../DarwinDomainDesign.md) | ERP, CRM, sales, purchasing, inventory, finance, HR/time, integration, AI-readiness, or field storage decisions change. |
| Foundation primitive design | [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md) | [docs/domain-expansion/domain-capability-catalog.md](domain-expansion/domain-capability-catalog.md), [docs/domain-expansion/release-sensitive-foundation-implementation-plan.md](domain-expansion/release-sensitive-foundation-implementation-plan.md), [DarwinDomainDesign.md](../DarwinDomainDesign.md) | Foundation primitive, release-sensitive source-of-truth, address, external reference, custom field, activity/note, attachment, number sequence, audit, or feature visibility decisions change. |
| Release-sensitive foundation implementation | [docs/domain-expansion/release-sensitive-foundation-implementation-plan.md](domain-expansion/release-sensitive-foundation-implementation-plan.md) | [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md), [docs/domain-expansion/loyalty-ledger-scan-compatibility-review.md](domain-expansion/loyalty-ledger-scan-compatibility-review.md), [docs/domain-expansion/identity-profile-customer-bridge-compatibility-review.md](domain-expansion/identity-profile-customer-bridge-compatibility-review.md), [docs/domain-expansion/business-businessmember-access-compatibility-review.md](domain-expansion/business-businessmember-access-compatibility-review.md), [docs/domain-expansion/order-invoice-snapshot-compatibility-review.md](domain-expansion/order-invoice-snapshot-compatibility-review.md), [docs/domain-expansion/foundation-implementation-consolidation-review.md](domain-expansion/foundation-implementation-consolidation-review.md), [docs/domain-expansion/domain-capability-catalog.md](domain-expansion/domain-capability-catalog.md), [DarwinTesting.md](../DarwinTesting.md) | P0 foundation implementation order, mobile-release-sensitive contracts, address compatibility, loyalty/mobile compatibility, or source-of-truth release decisions change. |
| Mobile API boundary freeze | [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md) | [docs/domain-expansion/release-sensitive-foundation-implementation-plan.md](domain-expansion/release-sensitive-foundation-implementation-plan.md), [DarwinMobile.md](../DarwinMobile.md), [DarwinWebApi.md](../DarwinWebApi.md), [DarwinTesting.md](../DarwinTesting.md) | Mobile route, DTO, serialization, source-contract, launch-readiness, or release migration rules change. |
| Loyalty ledger and scan compatibility | [docs/domain-expansion/loyalty-ledger-scan-compatibility-review.md](domain-expansion/loyalty-ledger-scan-compatibility-review.md) | [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md), [DarwinMobile.md](../DarwinMobile.md), [DarwinWebApi.md](../DarwinWebApi.md), [DarwinTesting.md](../DarwinTesting.md) | Loyalty ledger, QR/scan/session, reward, campaign, mobile loyalty, or points source-of-truth decisions change. |
| Identity profile and customer bridge compatibility | [docs/domain-expansion/identity-profile-customer-bridge-compatibility-review.md](domain-expansion/identity-profile-customer-bridge-compatibility-review.md) | [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md), [DarwinMobile.md](../DarwinMobile.md), [DarwinWebApi.md](../DarwinWebApi.md), [DarwinTesting.md](../DarwinTesting.md) | Identity, profile, linked customer, phone verification, account deletion, auth token, external login, or device registration decisions change. |
| Business and business member access compatibility | [docs/domain-expansion/business-businessmember-access-compatibility-review.md](domain-expansion/business-businessmember-access-compatibility-review.md) | [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md), [DarwinMobile.md](../DarwinMobile.md), [DarwinWebApi.md](../DarwinWebApi.md), [DarwinTesting.md](../DarwinTesting.md) | Business mobile access, invitation onboarding, access-state, business media/profile, live-operation guard, or future staff linkage decisions change. |
| Order and invoice snapshot compatibility | [docs/domain-expansion/order-invoice-snapshot-compatibility-review.md](domain-expansion/order-invoice-snapshot-compatibility-review.md) | [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md), [DarwinMobile.md](../DarwinMobile.md), [DarwinWebApi.md](../DarwinWebApi.md), [DarwinTesting.md](../DarwinTesting.md) | Member order/invoice history, checkout snapshots, invoice archive/download, structured source-model exports, or future sales/finance foundation decisions change. |
| Foundation implementation consolidation | [docs/domain-expansion/foundation-implementation-consolidation-review.md](domain-expansion/foundation-implementation-consolidation-review.md) | [docs/domain-expansion/release-sensitive-foundation-implementation-plan.md](domain-expansion/release-sensitive-foundation-implementation-plan.md), [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [DarwinTesting.md](../DarwinTesting.md) | P0 foundation evidence, remaining release risk, next foundation slice, or external-system readiness priority changes. |
| External-system readiness primitives | [docs/domain-expansion/external-system-readiness-primitives.md](domain-expansion/external-system-readiness-primitives.md) | [docs/domain-expansion/foundation-implementation-consolidation-review.md](domain-expansion/foundation-implementation-consolidation-review.md), [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [docs/persistence-providers.md](persistence-providers.md) | External system registry, external reference identity, source-of-truth, sync readiness, or integration foundation decisions change. |
| Custom fields and activity/document foundation | [docs/domain-expansion/custom-fields-activity-document-foundation.md](domain-expansion/custom-fields-activity-document-foundation.md) | [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [docs/domain-expansion/external-system-readiness-primitives.md](domain-expansion/external-system-readiness-primitives.md), [docs/persistence-providers.md](persistence-providers.md) | Custom fields, timeline activities, notes, document metadata, visibility rules, or attachment foundation decisions change. |
| Number sequence foundation | [docs/domain-expansion/number-sequence-foundation.md](domain-expansion/number-sequence-foundation.md) | [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [docs/domain-expansion/order-invoice-snapshot-compatibility-review.md](domain-expansion/order-invoice-snapshot-compatibility-review.md), [docs/persistence-providers.md](persistence-providers.md) | Number sequence, document number policy, order/purchase/invoice numbering, reset policy, or issued-number compatibility decisions change. |
| Business event and audit trail foundation | [docs/domain-expansion/business-event-audit-trail-foundation.md](domain-expansion/business-event-audit-trail-foundation.md) | [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [docs/domain-expansion/custom-fields-activity-document-foundation.md](domain-expansion/custom-fields-activity-document-foundation.md), [docs/persistence-providers.md](persistence-providers.md) | Business events, audit trail, append-only evidence, automation context, AI-readiness event history, or specialized-audit replacement rules change. |
| Feature area and module visibility foundation | [docs/domain-expansion/feature-area-module-visibility-foundation.md](domain-expansion/feature-area-module-visibility-foundation.md) | [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md), [docs/domain-expansion/business-event-audit-trail-foundation.md](domain-expansion/business-event-audit-trail-foundation.md), [docs/persistence-providers.md](persistence-providers.md) | Feature area registry, module visibility, business feature overrides, packaging, or UI visibility foundation decisions change. |
| Production setup | [docs/production-setup.md](production-setup.md) | [docs/external-smoke-inputs.md](external-smoke-inputs.md), [docs/minio-storage-runbook.md](minio-storage-runbook.md) | Deployment prerequisites, secure configuration, provider setup, or smoke order changes. |
| Customer onboarding | [docs/customer-deployment-onboarding-checklist.md](customer-deployment-onboarding-checklist.md) | [docs/production-setup.md](production-setup.md), [docs/external-smoke-inputs.md](external-smoke-inputs.md) | A repeatable deployment step, approval owner, go-live sequence, or customer handoff changes. |
| External smoke inputs | [docs/external-smoke-inputs.md](external-smoke-inputs.md) | [docs/production-setup.md](production-setup.md) | Smoke scripts, environment variable names, commands, or acceptance criteria change. |
| Persistence | [docs/persistence-providers.md](persistence-providers.md) | [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md) | PostgreSQL/SQL Server provider behavior, migrations, or runtime selection changes. |
| PostgreSQL migration | [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md) | [docs/persistence-providers.md](persistence-providers.md) | PostgreSQL roles, grants, migration execution, or validation steps change. |
| Compliance decisions | [docs/compliance-decisions.md](compliance-decisions.md) | [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md), [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | VAT/VIES, archive, retention, or e-invoice policy changes. |
| Object storage | [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | [docs/minio-storage-runbook.md](minio-storage-runbook.md), [docs/production-setup.md](production-setup.md) | Provider support, immutability assumptions, or deployment rules change. |
| MinIO runbook | [docs/minio-storage-runbook.md](minio-storage-runbook.md) | [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md) | Local smoke, bucket setup, Object Lock, retention, or production checklist changes. |
| E-invoice tooling | [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md) | [docs/e-invoice-acceptance-checklist.md](e-invoice-acceptance-checklist.md), [docs/e-invoice-validation-fixtures.md](e-invoice-validation-fixtures.md), [docs/compliance-decisions.md](compliance-decisions.md) | Generator, wrapper, validation profile, evidence, fixtures, or retained alternatives change. |
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
- [docs/domain-expansion/domain-capability-catalog.md](domain-expansion/domain-capability-catalog.md)
- [docs/domain-expansion/foundation-primitives-design.md](domain-expansion/foundation-primitives-design.md)
- [docs/domain-expansion/release-sensitive-foundation-implementation-plan.md](domain-expansion/release-sensitive-foundation-implementation-plan.md)
- [docs/domain-expansion/mobile-api-boundary-freeze-review.md](domain-expansion/mobile-api-boundary-freeze-review.md)
- [docs/domain-expansion/loyalty-ledger-scan-compatibility-review.md](domain-expansion/loyalty-ledger-scan-compatibility-review.md)
- [docs/domain-expansion/identity-profile-customer-bridge-compatibility-review.md](domain-expansion/identity-profile-customer-bridge-compatibility-review.md)
- [docs/domain-expansion/business-businessmember-access-compatibility-review.md](domain-expansion/business-businessmember-access-compatibility-review.md)
- [docs/domain-expansion/order-invoice-snapshot-compatibility-review.md](domain-expansion/order-invoice-snapshot-compatibility-review.md)
- [docs/domain-expansion/foundation-implementation-consolidation-review.md](domain-expansion/foundation-implementation-consolidation-review.md)
- [docs/domain-expansion/external-system-readiness-primitives.md](domain-expansion/external-system-readiness-primitives.md)
- [docs/domain-expansion/custom-fields-activity-document-foundation.md](domain-expansion/custom-fields-activity-document-foundation.md)
- [docs/domain-expansion/number-sequence-foundation.md](domain-expansion/number-sequence-foundation.md)
- [docs/domain-expansion/business-event-audit-trail-foundation.md](domain-expansion/business-event-audit-trail-foundation.md)
- [docs/domain-expansion/feature-area-module-visibility-foundation.md](domain-expansion/feature-area-module-visibility-foundation.md)

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
- [docs/customer-deployment-onboarding-checklist.md](customer-deployment-onboarding-checklist.md)
- [docs/external-smoke-inputs.md](external-smoke-inputs.md)
- [docs/minio-storage-runbook.md](minio-storage-runbook.md)
- [docs/postgresql-migration-runbook.md](postgresql-migration-runbook.md)

### Compliance

- [docs/compliance-decisions.md](compliance-decisions.md)
- [docs/archive-storage-provider-decision.md](archive-storage-provider-decision.md)
- [docs/e-invoice-tooling-decision.md](e-invoice-tooling-decision.md)
- [docs/e-invoice-acceptance-checklist.md](e-invoice-acceptance-checklist.md)
- [docs/e-invoice-validation-fixtures.md](e-invoice-validation-fixtures.md)

### Testing

- [DarwinTesting.md](../DarwinTesting.md)
- [docs/external-smoke-inputs.md](external-smoke-inputs.md)

### Historical

- [docs/implementation-ledger.md](implementation-ledger.md)

## Do Not Duplicate

- Current status belongs in `docs/go-live-status.md` and `docs/module-audit.md`.
- Product positioning belongs in `README.md`; avoid repeating the full capability overview in runbooks.
- Tasks belong in `BACKLOG.md`.
- Smoke input names and commands belong in `docs/external-smoke-inputs.md`.
- Production setup steps belong in `docs/production-setup.md`.
- Local MinIO details belong in `docs/minio-storage-runbook.md`.
- Historical progress belongs in `docs/implementation-ledger.md`.
- Deployment-specific domains, tenant names, emails, credentials, and provider keys do not belong in active documentation.
