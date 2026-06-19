# Support And Case Management Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future Support/Case Management boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, member API, notification flow, or worker behavior.

Decision: Support is a formal case-management capability, not only CRM notes, emails, or internal follow-up tasks. CRM can provide customer context; support owns case lifecycle, SLA, queues, and resolution evidence.

## Current Darwin Support Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| CRM interactions and activities | Customer timeline evidence exists. | Support cases can write timeline evidence through foundation primitives, but case status is support-owned. |
| `InternalFollowUpTask` | Internal task evidence exists, including AI-created review tasks. | Follow-up tasks are not full support cases. |
| Notifications and communications | Email/channel audits and notification inbox exist. | Case messages can use communication providers after message privacy design. |
| Member profile/orders/invoices | Member/customer context exists. | Member support portal requires separate contract and privacy design. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Case intake | Captures customer, member, supplier, or internal issues in one queue. | Requires case header, source, requester, priority, category, and status. |
| SLA tracking | Operators can manage response and resolution targets. | Requires SLA policy, timestamps, breach indicators, and worker reminders later. |
| Assignment and queues | Teams can route cases to owners. | Requires queue/team/assignee fields and audit. |
| Case timeline | Keeps communication and evidence in order. | Uses `Activity`, `Note`, `DocumentRecord`, and communication audit links. |
| Resolution | Captures final outcome and learning. | Requires resolution category, closure notes, linked root cause/corrective action. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `SupportCase` | support-case-management | business, requester type/id, customer/member/supplier links, case number, status, priority, category, queue, assignee, SLA dates. |
| `SupportCaseMessage` | support-case-management | case, direction, channel, safe body summary, author, visibility, communication audit link. |
| `SupportSlaPolicy` | support-case-management | scope, priority, response target, resolution target, active status. |
| `SupportCaseResolution` | support-case-management | case, resolution type, root cause, closed by/date, follow-up evidence. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Support case | `New -> Open -> PendingCustomer -> PendingInternal -> Resolved -> Closed`; `Cancelled` for duplicate/spam with reason. |
| SLA | Calculated from case priority/source and frozen on case open unless explicitly recalculated with audit. |
| Messages | Append-only; sensitive provider payload is not stored as message content. |

## Application Surface

Future handlers:

- Create case from WebAdmin, CRM context, member portal, or provider callback only after source-specific design.
- Assign/reassign, change priority/category/status.
- Add notes/messages/documents.
- Resolve/close/reopen with audit.
- Calculate SLA status and attention queues.

## WebAdmin Surface

Support WebAdmin should include case queues, list/detail, create/edit, assignment, SLA attention, timeline, resolution/closure, duplicate merge evidence, and read-only links to CRM, orders, invoices, suppliers, service orders, quality nonconformance, and documents.

Member portal support is not added by default. It requires privacy, attachment, communication, and SLA contract design.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `support-case-management`. |
| Package role | Add-on to CRM or Service Management. |
| Required dependencies | `crm` for customer context; `communications` optional. |
| Disabled behavior | Hide support queues and block new case intake; preserve historical read-only cases where needed. |
| Permissions | Manage cases, assign cases, close cases, manage SLA policies. |
| SoD | Case closure usually medium risk; legal/finance complaint closure can require approval by policy. |

## Compatibility Boundaries

- Support does not mutate orders, refunds, payments, supplier finance, stock, payroll, or provider success.
- Support messages do not store raw provider payloads or credentials.
- No member route or mobile route is added in this design.

## Implementation Slices

1. `Support Case Core WebAdmin Slice`.
2. `Support SLA Policy Slice`.
3. `Support Communication Timeline Slice`.
4. `Member Support Portal Boundary Design`.
5. `Support Integration With Service/Quality Slice`.

## Test Plan

Future tests must cover case lifecycle, assignment, SLA calculation, no cross-module mutation, safe message metadata, WebAdmin anti-forgery/row-version, member route source guards, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement Support/Case Management.
