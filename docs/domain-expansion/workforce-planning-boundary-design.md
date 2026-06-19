# Workforce Planning Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future Workforce Planning boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, mobile contract, payroll mutation, time-entry change, worker, or finance posting.

Decision: Workforce Planning is a planning and capacity capability over HR/time data. It is not payroll, not timesheet approval, and not a replacement for employee contracts or work schedules.

## Current Darwin Workforce Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| Employees, departments, positions, contracts | HR master data exists. | Workforce planning reads HR master data and can propose plans; HR remains owner. |
| Work schedules, attendance, time entries, timesheets | Time tracking exists. | Workforce planning can compare planned demand with actuals, not rewrite time entries. |
| Payroll periods/runs/payments | Payroll lifecycle exists. | Workforce planning does not calculate payroll or create payroll liabilities. |
| Project/service future | Project and service modules need resource planning later. | Workforce planning can supply capacity views after those modules exist. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Demand planning | Managers can estimate staffing needs by period/team/skill. | Requires demand plan, roles/skills, period, quantity/hours. |
| Capacity planning | Compare employee availability with demand. | Requires schedule/contract/time-off projections. |
| Scenario planning | Evaluate hiring/overtime/outsourcing options. | Requires versioned planning scenarios. |
| Utilization reporting | Shows planned vs actual resource usage. | Requires analytics/BI integration later. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `WorkforcePlan` | workforce-planning | business, planning period, status, scenario name, owner, notes. |
| `WorkforceDemandLine` | workforce-planning | plan, department/position/skill, period, required hours/headcount, source module. |
| `WorkforceCapacitySnapshot` | workforce-planning | plan, employee/position, available hours, absence/schedule assumptions. |
| `WorkforceScenario` | workforce-planning | plan, scenario type, assumptions, status, comparison metrics. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Plan | `Draft -> Reviewed -> Approved -> Locked -> Archived`. |
| Demand line | Editable until plan is locked; locked plans are read-only evidence. |
| Capacity snapshot | Derived from HR/time at generation; regenerated snapshots create new evidence. |

## Application Surface

Future handlers:

- Create/update/archive workforce plans.
- Generate capacity snapshot from HR schedules/contracts/absence.
- Add/edit demand lines.
- Compare demand vs capacity.
- Approve/lock planning scenario.

## WebAdmin Surface

HR WebAdmin should include workforce plans, demand/capacity table, scenario comparison, shortage/overcapacity view, and read-only links to employees, schedules, absences, projects, service orders, and analytics.

No employee self-service or payroll mutation is added by default.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `workforce-planning`. |
| Package role | Add-on to HR/Time and Enterprise. |
| Required dependencies | `hr-time`; optional `project-operations`, `service-management`, `analytics`. |
| Disabled behavior | Hide workforce planning; HR/time remains usable. |
| Permissions | Manage workforce plans, approve workforce plans, view workforce analytics. |
| Privacy | Employee availability and absence assumptions require HR-level permissions. |

## Compatibility Boundaries

- Does not create payroll runs, payroll payments, time entries, or employment contracts.
- Does not expose employee planning data to public/member surfaces.
- Finance export and mobile contracts remain unchanged.

## Implementation Slices

1. `Workforce Plan Core Slice`.
2. `Capacity Snapshot Slice`.
3. `Demand Capacity Review Slice`.
4. `Project/Service Resource Integration Design`.
5. `Workforce Analytics Slice`.

## Test Plan

Future tests must cover HR data scope, privacy, snapshot determinism, no payroll/time mutation, WebAdmin guards, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement Workforce Planning.
