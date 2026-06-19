# Project Operations Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future Project Operations boundary before implementation. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, mobile contract, finance posting, invoice, payroll, or worker behavior.

Decision: Project Operations is a distinct module for project-based work, not a CRM custom field, not a sales quote note, and not a finance-only cost dimension. It must integrate with CRM, sales, HR/time, procurement, inventory, billing, and finance through owned services.

## Current Darwin Project Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| CRM leads/opportunities/customers | Customer/project pipeline context exists. | Projects can originate from CRM, but CRM does not own project execution. |
| Sales quotes/orders/invoices | Commercial documents exist. | Project billing can link to sales documents, but must not create parallel invoices. |
| HR/time entries/timesheets | Time tracking exists. | Project time can read approved time entries after project linkage design. |
| Supplier invoices/payments | Purchase cost evidence exists. | Project cost capture can link to procurement/payables after cost policy. |
| Finance posting/reporting | Journal entries and reporting exist. | Project accounting needs dimensions and posting rules before financial recognition. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Project master | Service/project companies can manage client work separate from simple orders. | Requires project header, customer, contract, status, budget, manager, dates. |
| Work breakdown | Teams can plan phases/tasks/milestones. | Requires project task hierarchy, dependencies, assignments, estimates. |
| Time and expense capture | Costs and utilization can be tracked. | Requires HR/time linkage and expense/procurement linkage. |
| Project billing | Supports fixed price, time-and-material, milestone, and retainer boundaries. | Requires billing policy before invoice generation. |
| Project accounting | Supports WIP, revenue recognition, margin reporting. | Requires advanced finance/controlling design. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `Project` | project-operations | business, customer, project number, name, status, type, manager, start/end dates, currency, budget totals, contract reference. |
| `ProjectPhase` | project-operations | project, parent phase, code, name, status, dates, budget. |
| `ProjectTask` | project-operations | phase, assignee, status, estimate, actuals, billing eligibility, sort order. |
| `ProjectResourceAssignment` | project-operations | employee/business member, role, planned hours, rate snapshot. |
| `ProjectCostEntry` | project-operations | source type/id, amount, currency, cost type, billable flag, approval status. |
| `ProjectBillingMilestone` | project-operations | milestone, amount, due criteria, billing status, linked sales/invoice reference. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Project | `Draft -> Active -> OnHold -> Completed -> Closed`; `Cancelled` before financial postings or with reversal policy. |
| Project task | `Open -> InProgress -> Done`; cancelled tasks preserve history. |
| Cost entry | `Draft -> Submitted -> Approved -> Locked`; locked after billing or posting linkage. |
| Billing milestone | `Planned -> ReadyToBill -> Billed -> Closed`; invoice creation remains billing/sales-owned. |

## Application Surface

Future handlers:

- Create/update/archive project, phases, tasks, resource assignments.
- Link CRM opportunity or sales quote to project.
- Import approved time entries into project cost entries.
- Link supplier invoice lines or expenses to project cost.
- Mark milestones ready for billing.
- Produce project margin and WIP projections without posting until finance policy exists.

## WebAdmin Surface

Project WebAdmin should include project list/detail/editor, task board/list, resource plan, cost review, time/expense review, milestone/billing readiness, margin overview, and read-only links to CRM, sales documents, HR time, supplier invoices, journal entries, and documents.

No public/member customer portal is added by default. Customer-visible project status requires a separate portal contract.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `project-operations`. |
| Package role | Add-on for service/project businesses. |
| Required dependencies | `crm` and `sales`; optional `hr-time`, `procurement`, `finance`. |
| Disabled behavior | Hide projects and block new project mutations; historical project links stay read-only when required. |
| Permissions | Manage projects, manage tasks, approve project cost, mark ready to bill. |
| SoD | Cost approval and billing readiness are approval candidates. |

## Compatibility Boundaries

- Do not create customer invoices, supplier payments, payroll entries, or journal entries inside the project module.
- Billing remains invoice owner; finance remains posting owner; HR/time remains time owner.
- No public/mobile/storefront contract changes in this design.

## Implementation Slices

1. `Project Master And Task Core Slice`.
2. `Project Resource And Time Linkage Design/Slice`.
3. `Project Cost Review Slice`.
4. `Project Billing Boundary Design`.
5. `Project Accounting And WIP Boundary Design`.

## Test Plan

Future tests must cover project lifecycle, task hierarchy, cross-business guards, time/cost link ownership, no invoice/posting shortcuts, WebAdmin anti-forgery/row-version, source guards for no public route, and capability disabled behavior.

## No Runtime Behavior Changes

This design does not implement Project Operations.
