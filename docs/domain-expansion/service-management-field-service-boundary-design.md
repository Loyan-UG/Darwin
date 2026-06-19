# Service Management And Field Service Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future Service Management and Field Service boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, mobile contract, worker, stock movement, billing flow, or provider integration.

Decision: Service Management is a distinct service-order capability. It can integrate with CRM, sales, inventory, HR/time, warehouse tasks, project operations, and billing, but it must not be modeled as CRM notes or generic warehouse tasks.

## Current Darwin Service Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| CRM customers, interactions, opportunities | Customer service context exists. | CRM can originate service demand; service order owns execution. |
| Inventory/warehouse tasks | Stock movement and warehouse work exist. | Field parts usage must request inventory movement through inventory owner. |
| HR employees/time | Employees, schedules, time entries, timesheets exist. | Technician scheduling can link to HR/time after service scheduling design. |
| Sales/billing | Quotes/orders/invoices/payments exist. | Service billing must use sales/billing owners, not service-owned invoices. |
| Notifications/communications | Email/channel/push foundations exist. | Appointment reminders can use communications after worker/provider design. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Service request/case intake | Customers/operators can capture repair, maintenance, installation, and warranty demand. | Requires service request with customer, asset/location, priority, SLA, status. |
| Service order | Plans and executes field or workshop work. | Requires work order, tasks, assignment, schedule, parts, labor, completion evidence. |
| Technician dispatch | Coordinates employees, date/time, location, and workload. | Requires schedule integration and mobile/PWA contract later. |
| Parts usage | Tracks spare parts consumed on jobs. | Requires inventory issue through inventory handlers and return unused parts. |
| Service billing | Bills labor, parts, contract coverage, or warranty. | Requires billing policy and sales/invoice integration. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `ServiceRequest` | service-management | business, customer, contact, priority, source, status, summary, SLA target, related asset/location. |
| `ServiceOrder` | service-management | service number, request, customer, status, service type, scheduled window, assigned technician/team, billing policy. |
| `ServiceOrderTask` | service-management | order, sequence, task type, status, estimated/actual time, checklist evidence. |
| `ServiceOrderPartLine` | service-management | product variant, planned/issued/returned quantity, warehouse/location, billable flag. |
| `ServiceOrderLaborLine` | service-management | employee, role, time entry link, hours, rate snapshot, billable flag. |
| `ServiceContract` | service-management | customer, coverage terms, SLA, included visits, billing cadence, status. |
| `ServiceAsset` | service-management | customer-owned equipment/asset identity, serial/reference, warranty coverage, location. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Service request | `New -> Triage -> Accepted` or `Rejected`; accepted request creates or links service order. |
| Service order | `Draft -> Scheduled -> Dispatched -> InProgress -> Completed -> ReadyToBill -> Closed`; cancellation after parts/labor posting needs reversal rules. |
| Parts usage | Planned parts can be edited; issued parts require inventory reference; returned parts require inventory owner. |
| Service contract | `Draft -> Active -> Suspended -> Expired -> Archived`. |

## Application Surface

Future handlers:

- Create/triage/accept/reject service requests.
- Create/schedule/dispatch/start/complete/close service orders.
- Plan parts and labor.
- Issue/return parts through inventory owner.
- Link time entries through HR/time owner.
- Mark order ready for billing without creating invoice directly.
- Manage service contracts and service assets.

## WebAdmin Surface

Service WebAdmin should include request queue, service order board/list/detail/editor, dispatch calendar, technician workload, parts/labor tabs, service contract pages, service asset pages, completion evidence, and read-only links to CRM, inventory, HR/time, sales/billing, and documents.

Mobile-business/field technician UI is a later dedicated design. No public/member route is added by default.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `service-management`. |
| Package role | Add-on for service-heavy customers. |
| Required dependencies | `crm`; optional `inventory`, `hr-time`, `sales`, `billing`. |
| Disabled behavior | Hide service nav and block new requests/orders; preserve historical read-only service evidence. |
| Permissions | Manage service requests, schedule service orders, dispatch, post parts usage request, close order. |
| SoD | Billing readiness and inventory-affecting parts issue are approval candidates. |

## Compatibility Boundaries

- Service does not create invoices, payments, refunds, journal entries, or direct inventory ledger rows.
- Warranty/returns integration must not bypass ReturnOrder or refund boundaries.
- No public/mobile/storefront changes in this design.

## Implementation Slices

1. `Service Request And Service Order Core Slice`.
2. `Service Scheduling And Dispatch Slice`.
3. `Service Parts Inventory Integration Boundary/Slice`.
4. `Service Time And Labor Integration Slice`.
5. `Service Billing Boundary Design`.

## Test Plan

Future tests must cover request/order lifecycle, cross-business guards, parts/labor ownership, no invoice or stock shortcuts, WebAdmin row-version/anti-forgery, capability disabled behavior, and mobile/public source guards.

## No Runtime Behavior Changes

This design does not implement Service Management.
