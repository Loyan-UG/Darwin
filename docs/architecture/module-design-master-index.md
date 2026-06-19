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
| Capability enforcement/disabled mode | `needs-core-design` | `capability-enforcement-design.md`, `modularity-audit.md`, `module-disabled-behavior.md`, `modularity-gaps-backlog.md` | `Capability Enforcement Foundation Core Slice` before surface-specific gates. |
| Security/SoD approval governance | `needs-core-design` | `security-sod-approval-governance-design.md`, `security-boundary-rules.md`, identity/access docs | `Approval Governance Foundation Design-To-Core Slice` before module opt-in. |
| Identity and business master | `implemented-reviewed` | `business-businessmember-access-compatibility-review.md`, tenant docs | Revisit after tenant catalog to add tenant ownership safely. |
| CMS | `implemented-reviewed` | `module-audit.md`, capability docs | Add detailed module design only if storefront/package gating changes. |
| Storefront | `implemented-reviewed` | `DarwinFrontEnd.md`, package docs, tenant-domain docs | Update URL/domain design when tenant-aware link generation is implemented. |
| Catalog | `implemented-reviewed` | capability ownership docs, module audit | Add advanced pricing/contract design before enterprise pricing changes. |
| Cart/checkout | `implemented-reviewed` | order/invoice compatibility docs, module audit | Tenant-aware checkout URL design is required before custom-domain checkout changes. |
| Member portal | `implemented-reviewed` | mobile boundary docs, employee payslip self-service docs | Keep additive contract tests for every new self-service surface. |
| Customer-facing billing/subscriptions | `implemented-reviewed` | billing plan/package docs | Reconcile `BillingPlan` with future `PlatformPlan` after package core design. |
| POS/retail | `needs-core-design` | `pos-retail-boundary-design.md`, `erp-benchmark-module-gap-audit.md` | `POS Terminal And Cash Session Design/Core Slice`; offline mode remains separate. |
| CRM | `implemented-reviewed` | `crm-expansion-design.md`, capability ownership docs | Add import/reference management only with a selected import flow. |
| Sales documents | `implemented-reviewed` | sales lifecycle and sales order document docs | Keep public/mobile exposure blocked unless separately designed. |
| Advanced pricing/contracts/rebates | `needs-core-design` | `advanced-pricing-contracts-rebates-boundary-design.md`, benchmark audit, catalog/sales docs | `Price Agreement Core Slice`; rebate finance settlement remains separate. |
| Project operations | `needs-core-design` | `project-operations-boundary-design.md`, `erp-benchmark-module-gap-audit.md` | `Project Master And Task Core Slice`. |
| Service management/field service | `needs-core-design` | `service-management-field-service-boundary-design.md`, `erp-benchmark-module-gap-audit.md` | `Service Request And Service Order Core Slice`. |
| Support/case management | `needs-core-design` | `support-case-management-boundary-design.md`, CRM docs | `Support Case Core WebAdmin Slice`; member portal support remains separate. |
| Procurement and supplier lifecycle | `implemented-reviewed` | purchasing supplier lifecycle docs | Core purchase-to-pay is complete for current phase. Strategic sourcing is tracked separately. |
| Strategic sourcing/RFQ/supplier scoring | `needs-core-design` | `strategic-sourcing-rfq-supplier-scoring-boundary-design.md`, purchasing supplier lifecycle docs | `Purchase Request Core Slice` before RFQ and bid evaluation. |
| Inventory and warehouse | `implemented-reviewed` | inventory warehouse task/PWA, stock count, lot/serial/HU docs | Continue hardening task/PWA flows through existing designs. |
| Quality/nonconformance | `needs-core-design` | `quality-nonconformance-boundary-design.md`, inventory docs | `Quality Inspection Plan Core Slice`. |
| Transportation/logistics planning | `needs-core-design` | `transportation-logistics-planning-boundary-design.md`, shipping docs | `Transport Load Planning Core Slice`; carrier APIs remain target-gated. |
| Manufacturing/MRP | `needs-core-design` | `manufacturing-mrp-boundary-design.md`, `erp-benchmark-module-gap-audit.md` | `Manufacturing Core Master Data Slice` for BOM, routing, and work centers. |
| Finance/accounting core | `implemented-reviewed` | finance posting, account mapping, receivables, export docs | Keep export based on posted journal entries. |
| Advanced finance/controlling | `needs-core-design` | `advanced-finance-controlling-boundary-design.md`, finance docs | `Finance Dimensions Core Slice`. |
| Fixed assets | `needs-core-design` | `fixed-assets-boundary-design.md`, finance docs | `Fixed Asset Register Core Slice`. |
| DATEV/accounting API adapters | `blocked-by-target` | finance export accounting API target selection docs | Select real target, credential owner, payload mapping, and smoke strategy. |
| Bank API adapters | `blocked-by-target` | `bank-api-target-adapter-boundary-design.md`, bank/treasury docs, returned-transfer docs | Select real banks or aggregation provider before adapter implementation. |
| HR/time | `implemented-reviewed` | HR/time tracking docs | Extend only through workforce planning or country-specific design. |
| Payroll | `implemented-reviewed` | payroll legal, payment, returned-transfer, payslip docs | Provider submission remains blocked by target selection. |
| Payroll provider submission | `blocked-by-target` | payroll provider adapter design | Select real statutory/payroll target before implementation. |
| Workforce planning | `needs-core-design` | `workforce-planning-boundary-design.md`, HR/time docs | `Workforce Plan Core Slice`. |
| Integration/sync foundation | `implemented-reviewed` | external-system readiness, sync-state conflict docs | Target-specific adapters remain separate. |
| Master-data import/coexistence | `needs-core-design` | `master-data-import-coexistence-boundary-design.md`, sync docs | `Import Batch And Mapping Profile Boundary/Core Slice`. |
| Analytics/BI semantic metrics | `needs-core-design` | `analytics-bi-semantic-layer-boundary-design.md`, finance reporting and AI scoped context docs | `Semantic Metric Catalog Core Slice`. |
| AI governance | `implemented-reviewed` | AI governance, provider, handoff, executor docs | Real provider and operational executors remain blocked by target/command selection. |
| AI provider adapter | `blocked-by-target` | AI target provider selection docs | Select provider/model, credential owner, payload mapping, cost policy, and smoke strategy. |
| AI operational command executors | `blocked-by-target` | `ai-operational-command-executors-boundary-design.md`, AI action handoff docs | Select one concrete command family before direct module mutation. |

## Immediate Next Implementation-Planning Slices

1. `Tenant Catalog And Domain Resolution Foundation Slice`.
2. `Package/Plan Foundation Core Slice`.
3. `Capability Enforcement Foundation Core Slice`.
4. `Approval Governance Foundation Design-To-Core Slice`.
5. `Manufacturing Core Master Data Slice`.
6. `Quality Inspection Plan Core Slice`.
7. `Project Master And Task Core Slice`.
8. `Service Request And Service Order Core Slice`.

The remaining module families now have boundary designs. Implementation still needs separate core/admin slices, migrations, tests, and compatibility gates.

## No Runtime Behavior Changes

This index is a planning artifact. It does not assert that unfinished modules are implemented, package-safe, or independently sellable.
