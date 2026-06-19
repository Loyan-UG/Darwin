# Module Surface Action Matrix

Reviewed: 2026-06-19

## Summary

This matrix records the intended surface/action design coverage for Darwin modules before new development. It is documentation-only and changes no route, controller, DTO, WebAdmin action, mobile contract, worker, provider flow, or runtime gate.

The matrix is conservative. If a surface is not explicitly designed, the default is no exposure.

## Surface Vocabulary

| Surface | Meaning |
| --- | --- |
| WebAdmin | Internal operator UI with anti-forgery, row-version lifecycle actions, permissions, and localized text. |
| Public WebApi | Anonymous or storefront-facing API. |
| Member WebApi | Authenticated member/customer API. |
| Business WebApi | Authenticated business/staff API. |
| Mobile consumer | Consumer app routes/services/contracts. |
| Mobile business | Business app routes/services/contracts. |
| Worker | Background jobs, retry loops, scheduled operations, provider callback processing. |
| Provider/storage | External provider adapter, object storage, file delivery, archive, or callback dependency. |

## Cross-Cutting Surfaces

| Module family | WebAdmin | Public/member/business APIs | Mobile/Web | Worker | Provider/storage | Next surface design need |
| --- | --- | --- | --- | --- | --- | --- |
| Tenant/domain/on-premise | Future tenant catalog/domain admin pages. | Future tenant resolution metadata; no broad public management API. | Clients need tenant/capability metadata later. | Future tenant-aware job scheduler. | Domain verification, TLS, data-store secret references. | Tenant catalog/domain foundation. |
| Package/plan entitlement | Future package/plan assignment admin pages. | Future capability metadata endpoint for clients. | Navigation gating metadata. | Worker skip policy by capability/provider. | Provider readiness checks. | Package core and capability enforcement. |
| Security/SoD governance | Permission/role/approval policy pages. | No public API by default. | No mobile exposure by default. | Approval reminders may be worker-owned later. | None by default. | SoD/approval boundary. |

## Current Core Module Surfaces

| Module family | WebAdmin actions | API/mobile exposure | Worker/provider dependencies | Design status |
| --- | --- | --- | --- | --- |
| CMS | List/detail/create/edit/archive pages and public content management. | Public content read through storefront-facing surfaces. | Media/object storage. | `implemented-reviewed`; add package gating design later. |
| Catalog | Product/category/brand/add-on admin actions. | Public catalog reads; checkout reads product snapshots. | Media/storage, search indexes. | `implemented-reviewed`; advanced pricing design pending. |
| Cart/checkout | Admin support is limited; storefront checkout orchestration owns customer flow. | Public/member checkout surfaces exist; mobile uses handoff/status where designed. | Stripe provider, provider callback worker. | `implemented-reviewed`; tenant-domain URL generation pending. |
| CRM | Customer/lead/opportunity/list/detail/actions and timeline evidence. | Member/customer context where designed. | Communications optional. | `implemented-reviewed`; support/case and import designs pending. |
| Sales documents | Quotes, delivery notes, return orders, credit notes, lifecycle pages. | Internal-only by default. | Finance, inventory, payment/refund linkage through owners. | `implemented-reviewed`. |
| Procurement | Suppliers, contacts, documents, PO, goods receipt pages/actions. | Internal-only by default. | Inventory posting and finance matching. | `implemented-reviewed`. |
| Inventory/warehouse | Warehouses, locations/bins, labels, stock transfers, counts, tasks, lot/serial/HU. | Internal/business warehouse PWA only where explicitly designed. | Inventory transaction ledger and optional scanner/PWA support. | `implemented-reviewed`; quality and manufacturing designs pending. |
| Finance/accounting | Accounts, mappings, journals/read-only links, reports, exports. | Internal-only by default. | Finance export storage/file delivery. | `implemented-reviewed`; advanced finance and target adapters pending. |
| Treasury | Bank accounts, statements, reconciliation, settlement/correction evidence. | Internal-only by default. | Bank statement import, no bank API yet. | `implemented-reviewed`; bank API target pending. |
| HR/time | Employee, contracts, schedules, attendance, time entries, leave, timesheets. | Internal and selected employee self-service only where designed. | None by default. | `implemented-reviewed`; workforce planning pending. |
| Payroll | Payroll runs, payslips, posting, payments, bank settlement/correction. | Employee payslip self-service exists where scoped. | Finance posting, bank reconciliation, object storage. | `implemented-reviewed`; provider submission blocked by target. |
| AI governance | Recommendation/action-draft queues, approvals, internal task/evidence execution. | No public/mobile exposure. | Provider-neutral adapter foundation; no real provider. | `implemented-reviewed`; real provider blocked by target. |
| Integration/sync | External systems/references/sync conflict foundations. | No public exposure by default. | Target-specific workers later. | `implemented-reviewed`; target-specific adapters pending. |

## Gap Module Surface Requirements

| Gap module | Required WebAdmin pages/actions | API/mobile default | Worker/provider default | First design focus |
| --- | --- | --- | --- | --- |
| Manufacturing/MRP | BOM, routing, work center, production order, material planning, WIP review, costing review. | No public/member exposure; business mobile/PWA only after shop-floor design. | MRP planning job only after planning policy is locked. | Boundary design for BOM, routing, production order, inventory reservation, finance costing. |
| Quality/nonconformance | Inspection plans, quality orders, inspection results, holds, nonconformance, corrective action, supplier quality. | No public/member exposure. | Optional scheduled review/escalation only after workflow design. | Decide standalone quality module vs inventory extension. |
| Project operations | Project master, tasks, resources, time/expense, project billing, WIP/revenue review. | Member/customer project portal only after contract design. | Optional billing/revenue recognition jobs later. | Boundary with CRM, sales, HR time, finance, billing. |
| Service management | Service contracts, work orders, technician schedule, parts consumption, service billing. | Business mobile/PWA possible after field-service design. | Optional dispatch/reminder jobs. | Boundary with sales, inventory, HR, mobile/PWA, billing. |
| Support/case management | Cases, queues, SLA, communication timeline, escalation, closure. | Member support API only after privacy and SLA design. | Reminder/escalation workers later. | Decide CRM extension vs standalone support module. |
| Advanced pricing/contracts/rebates | Price agreements, contract terms, rebate programs, approval/review queues. | Public/member reads only through priced checkout snapshots. | Scheduled rebate calculation only after finance policy. | Boundary with catalog, sales, billing, finance. |
| Advanced finance/controlling | Cost centers, dimensions, budgets, allocations, consolidation, management reports. | No public/member exposure. | Period close/allocation jobs only after accounting policy. | Boundary with finance posting and reporting. |
| Fixed assets | Asset register, acquisition, depreciation, disposal, maintenance links. | No public/member exposure. | Depreciation job after period policy. | Boundary with purchasing, finance posting, maintenance/service. |
| POS/retail | Counter sale, cash session, receipt, return, closing report, device handoff. | Dedicated POS/mobile surface only after device/offline design. | Payment/provider callbacks and stock posting. | Decide own POS vs external integration. |
| Master-data import/coexistence | Import batches, mapping, validation reports, conflict queues, rollback evidence. | No public/member exposure. | Import/sync workers after target selection. | Boundary with ExternalSystem, SyncState, SyncConflict. |
| DATEV/accounting API adapter | Export package delivery, remote receipt, sync status, conflict review. | No public/member exposure. | Connector worker only after real target. | Target selection and credential policy. |
| Bank API adapter | Bank connection readiness, statement import status, sync errors. | No public/member exposure. | Bank import worker only after target/provider selection. | Target selection and credential policy. |
| Analytics/BI semantic metrics | KPI catalog, dashboard definitions, report review, export permissions. | Read-only dashboards only after audience design. | Scheduled materialization only after metric governance. | Semantic metric catalog. |
| AI operational executors | Approval queue, typed executor review, execution evidence. | No public/member exposure. | No autonomous worker execution by default. | Select one command family and risk policy. |

## Required Source Guards For Future Implementation

Future implementation slices must add source guards where applicable:

- No public/mobile route unless the module design explicitly allows it.
- No worker registration without disabled-mode skip behavior.
- No provider adapter without readiness checks and no-secret metadata.
- No direct cross-module writes outside owning handlers/services.
- No package enablement used as authorization.
- No generated package/export/archive rebuilt from mutable documents after issued/generated state.
- No status-only financial, inventory, payroll, treasury, or compliance correction.

## No Runtime Behavior Changes

This matrix is a design checklist. It documents intended coverage and gaps; it does not create or expose any surface.
