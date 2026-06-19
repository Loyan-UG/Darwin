# Module Design Master Index

Reviewed: 2026-06-19

## Summary

This index tracks Darwin module design readiness before new implementation work. It reconciles the current capability catalog, ERP expansion status, benchmark gap audit, package-readiness audit, tenant architecture, and current backlog.

Status values:

- `implemented-reviewed`: current implementation and design are adequate for the current phase.
- `needs-boundary-design`: the module or feature family needs decision-complete boundary design before implementation.
- `needs-core-design`: the boundary is clear enough that a core model/admin design is next.
- `blocked-by-target`: implementation waits for a real provider, bank, accounting, payroll, sync, or integration target.
- `not-planned`: intentionally outside near-term Darwin scope.

## Design Program Order

| Order | Batch | Module families | Purpose |
| --- | --- | --- | --- |
| 1 | Cross-Cutting Platform | Tenant/domain/on-premise, package/plan, capability enforcement, security boundary, SoD/approval governance. | Make module enablement and deployment boundaries safe before more module development. |
| 2 | Commerce And Customer Experience | CMS, storefront, catalog, cart/checkout, member portal, POS/retail, customer-facing billing. | Clarify customer-facing package surfaces and self-service boundaries. |
| 3 | CRM Sales Service | CRM, sales documents, advanced pricing/contracts/rebates, project operations, service management, support/case management. | Complete sell/service/customer lifecycle beyond current CRM and sales documents. |
| 4 | Supply Chain And Warehouse | Inventory, warehouse, quality/nonconformance, transportation/logistics, manufacturing/MRP. | Close the largest ERP supply-chain and production gaps. |
| 5 | Finance Accounting Treasury | Advanced finance, fixed assets, controlling, consolidation, DATEV/accounting API, bank API. | Define enterprise finance and integration readiness without fake adapters. |
| 6 | HR Payroll Workforce | HR/time, payroll provider submission, employee self-service, workforce planning, country expansion. | Extend strong HR/payroll baseline only through selected legal/provider targets. |
| 7 | Integration Analytics AI | Master-data import/coexistence, target-specific sync, analytics/BI semantic metrics, AI provider and operational executors. | Support coexistence with external systems and governed automation. |

## Master Index

| Module family | Current design status | Primary docs | Next design action |
| --- | --- | --- | --- |
| Foundation primitives | `implemented-reviewed` | `foundation-primitives-design.md`, `custom-fields-activity-document-foundation.md`, `number-sequence-foundation.md`, `business-event-audit-trail-foundation.md` | Keep as shared foundation; update only when a module needs a new primitive. |
| Tenant/domain/on-premise | `needs-core-design` | `tenant-architecture.md`, `tenant-domain-routing.md`, `tenant-database-strategy.md`, `tenant-migration-plan.md` | `Tenant Catalog And Domain Resolution Foundation Slice`. |
| Package/plan entitlement | `needs-core-design` | `package-plan-architecture.md`, `package-capability-matrix.md`, `package-change-rules.md` | `Package/Plan Foundation Core Slice` after tenant catalog. |
| Capability enforcement/disabled mode | `needs-boundary-design` | `modularity-audit.md`, `module-disabled-behavior.md`, `modularity-gaps-backlog.md` | Design runtime gate enforcement across WebAdmin, WebApi, mobile/Web, workers, and providers. |
| Security/SoD approval governance | `needs-boundary-design` | `security-boundary-rules.md`, identity/access docs | Design separation-of-duties and approval governance before enterprise finance claims. |
| Identity and business master | `implemented-reviewed` | `business-businessmember-access-compatibility-review.md`, tenant docs | Revisit after tenant catalog to add tenant ownership safely. |
| CMS | `implemented-reviewed` | `module-audit.md`, capability docs | Add detailed module design only if storefront/package gating changes. |
| Storefront | `implemented-reviewed` | `DarwinFrontEnd.md`, package docs, tenant-domain docs | Update URL/domain design when tenant-aware link generation is implemented. |
| Catalog | `implemented-reviewed` | capability ownership docs, module audit | Add advanced pricing/contract design before enterprise pricing changes. |
| Cart/checkout | `implemented-reviewed` | order/invoice compatibility docs, module audit | Tenant-aware checkout URL design is required before custom-domain checkout changes. |
| Member portal | `implemented-reviewed` | mobile boundary docs, employee payslip self-service docs | Keep additive contract tests for every new self-service surface. |
| Customer-facing billing/subscriptions | `implemented-reviewed` | billing plan/package docs | Reconcile `BillingPlan` with future `PlatformPlan` after package core design. |
| POS/retail | `needs-boundary-design` | `erp-benchmark-module-gap-audit.md` | Decide whether Darwin owns POS or integrates external POS before field/device design. |
| CRM | `implemented-reviewed` | `crm-expansion-design.md`, capability ownership docs | Add import/reference management only with a selected import flow. |
| Sales documents | `implemented-reviewed` | sales lifecycle and sales order document docs | Keep public/mobile exposure blocked unless separately designed. |
| Advanced pricing/contracts/rebates | `needs-boundary-design` | benchmark audit, catalog/sales docs | Design pricing ownership before adding enterprise contracts or rebate fields. |
| Project operations | `needs-boundary-design` | `erp-benchmark-module-gap-audit.md` | Decide core project module vs integration; include project accounting and billing boundaries. |
| Service management/field service | `needs-boundary-design` | `erp-benchmark-module-gap-audit.md` | Design service orders, technician scheduling, parts, and service billing as a separate module. |
| Support/case management | `needs-boundary-design` | benchmark audit, CRM docs | Decide CRM extension vs standalone support module. |
| Procurement and supplier lifecycle | `implemented-reviewed` | purchasing supplier lifecycle docs | Extend only for RFQ/tender/supplier scoring after explicit design. |
| Inventory and warehouse | `implemented-reviewed` | inventory warehouse task/PWA, stock count, lot/serial/HU docs | Continue hardening task/PWA flows through existing designs. |
| Quality/nonconformance | `needs-boundary-design` | `erp-benchmark-module-gap-audit.md`, inventory docs | Decide standalone quality module vs warehouse/inventory extension. |
| Transportation/logistics planning | `needs-boundary-design` | benchmark audit, shipping docs | Decide broader TMS vs existing DHL/shipping provider operations. |
| Manufacturing/MRP | `needs-boundary-design` | `erp-benchmark-module-gap-audit.md` | Highest ERP gap: design BOM, routing, production order, capacity, MRP, WIP, costing boundaries. |
| Finance/accounting core | `implemented-reviewed` | finance posting, account mapping, receivables, export docs | Keep export based on posted journal entries. |
| Advanced finance/controlling | `needs-boundary-design` | benchmark audit, finance docs | Design cost centers, dimensions, budgets, consolidation, and management accounting. |
| Fixed assets | `needs-boundary-design` | benchmark audit, finance docs | Design fixed asset register, depreciation, disposal, and maintenance boundary. |
| DATEV/accounting API adapters | `blocked-by-target` | finance export accounting API target selection docs | Select real target, credential owner, payload mapping, and smoke strategy. |
| Bank API adapters | `blocked-by-target` | bank/treasury docs, returned-transfer docs | Select real banks or aggregation provider before adapter design. |
| HR/time | `implemented-reviewed` | HR/time tracking docs | Extend only through workforce planning or country-specific design. |
| Payroll | `implemented-reviewed` | payroll legal, payment, returned-transfer, payslip docs | Provider submission remains blocked by target selection. |
| Payroll provider submission | `blocked-by-target` | payroll provider adapter design | Select real statutory/payroll target before implementation. |
| Workforce planning | `needs-boundary-design` | HR/time docs, benchmark audit | Design demand/capacity planning only if needed beyond schedules/timesheets. |
| Integration/sync foundation | `implemented-reviewed` | external-system readiness, sync-state conflict docs | Target-specific adapters remain separate. |
| Master-data import/coexistence | `needs-boundary-design` | benchmark audit, sync docs | Design import batches, mapping, validation report, conflict workflow, and rollback. |
| Analytics/BI semantic metrics | `needs-boundary-design` | finance reporting and AI scoped context docs | Design semantic metric catalog and dashboard governance. |
| AI governance | `implemented-reviewed` | AI governance, provider, handoff, executor docs | Real provider and operational executors remain blocked by target/command selection. |
| AI provider adapter | `blocked-by-target` | AI target provider selection docs | Select provider/model, credential owner, payload mapping, cost policy, and smoke strategy. |
| AI operational command executors | `needs-boundary-design` | AI action handoff docs | Select one low-risk command family before direct module mutation. |

## Immediate Next Design Slices

1. `Capability Enforcement And Disabled-Mode Design Slice`.
2. `Security/SoD Approval Governance Boundary Design Slice`.
3. `Manufacturing/MRP Boundary Design Slice`.
4. `Quality/Nonconformance Boundary Design Slice`.
5. `Project Operations Boundary Design Slice`.
6. `Service Management Boundary Design Slice`.

## No Runtime Behavior Changes

This index is a planning artifact. It does not assert that unfinished modules are implemented, package-safe, or independently sellable.
