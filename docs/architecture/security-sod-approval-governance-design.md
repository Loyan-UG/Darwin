# Security, Separation Of Duties, And Approval Governance Design

Reviewed: 2026-06-19

## Summary

This document designs Darwin's security, separation-of-duties, and approval governance boundary before enterprise-grade module implementation continues. It is documentation-only and adds no entity, migration, authorization policy, route, DTO, WebAdmin mutation, worker registration, provider flow, or runtime behavior.

The core rule is strict: capability enablement, tenant entitlement, and package assignment are not permission grants. Sensitive operations require explicit permission, audit evidence, and for selected workflows an approval step before execution.

## Current Darwin Findings

| Current area | Code-backed finding | Design consequence |
| --- | --- | --- |
| Permissions | `src/Darwin.Domain/Entities/Identity/Permission.cs`, `Role.cs`, `RolePermission.cs`, `UserRole.cs`, `src/Darwin.Application/Identity/Services/IPermissionService.cs`, and WebAdmin `PermissionAuthorizeAttribute.cs` provide permission checks. | Keep as the authorization foundation. Add SoD and approval governance above it. |
| WebAdmin base | `AdminBaseController.cs` requires admin access and many controllers use permission attributes or base access. | Sensitive module actions still need action-level permission and future approval requirements. |
| Audit evidence | `BusinessEvent`, `AuditTrail`, module lifecycle services, finance posting, payroll posting, bank settlement, inventory movement references, and AI action approvals already create deterministic evidence. | Governance must reuse event/audit evidence and not invent status-only approval history. |
| AI review | AI governance has recommendation, action draft, approval, and internal task execution boundaries. | AI approval is not a general SoD model, but its approval evidence pattern is reusable. |
| Finance and treasury | Supplier invoice/payment, bank settlement, payroll payment, reversals, corrections, and finance posting are journal-backed. | Financial corrections must require stricter maker/checker and no status-only edit. |
| Worker/provider flows | Provider callbacks, email, shipment, invoice archive, VAT retry, and webhook workers exist. | Provider retry or replay operations need permission and audit evidence when triggered by operators. |

## Governance Concepts

| Concept | Canonical meaning |
| --- | --- |
| Permission | A user is allowed to perform an action family. |
| Capability gate | The tenant/business is entitled to use the module. |
| Approval policy | A sensitive action requires review before execution. |
| Maker/checker | The initiator and approver must be different actors for selected high-risk actions. |
| SoD conflict | A role or user combination creates unacceptable authority over conflicting duties. |
| Delegation | A temporary approval authority with explicit scope and expiry. |
| Evidence | Append-only business event/audit trail and linked domain records proving who requested, approved, executed, or rejected an action. |

## Action Risk Levels

| Risk level | Examples | Required governance |
| --- | --- | --- |
| Low | Read-only pages, list filters, internal notes, non-sensitive metadata edits. | Authentication, authorization, normal audit where already available. |
| Medium | Master-data changes, supplier/contact edits, catalog edits, warehouse location edits, non-financial lifecycle changes. | Permission, row-version, audit event. Approval optional by package/policy. |
| High | Finance posting, payment posting, payment reversal, bank settlement, inventory stock adjustment, payroll run posting, credit note posting, provider replay. | Permission, row-version, deterministic event, maker/checker approval unless the workflow is explicitly single-operator by policy. |
| Critical | Role/permission administration, package entitlement change, tenant/domain/data-store change, provider credential readiness, legal archive/e-invoice production activation. | Full admin permission, approval, actor separation, audit evidence, and operational sign-off where applicable. |

## Separation-Of-Duties Matrix

| Conflict pair | Business reason | Technical guard required |
| --- | --- | --- |
| Create supplier invoice and approve/post same invoice | Prevent fabricated payable liability. | Maker/checker for approval/posting when SoD policy is active. |
| Create supplier payment and post/reverse same payment | Prevent unauthorized AP settlement or reversal. | Distinct approver for posting and reversal in controlled packages. |
| Create bank statement import and settle payment from same reconciliation | Prevent self-certified bank settlement. | Optional checker for bank settlement when enabled. |
| Create stock adjustment and approve/post adjustment | Prevent unreviewed inventory value change. | Approval for high-value or reason-sensitive adjustments. |
| Create payroll run and post/pay same run | Protect salary liability and employee privacy. | Distinct approver for posting/payment in payroll packages. |
| Manage roles and approve own sensitive action | Prevent permission escalation. | Role administration cannot approve actions created by the same actor after permission changes. |
| Configure provider readiness and trigger provider delivery | Prevent fake external success after self-configured provider. | Readiness evidence plus approval for production provider activation. |
| Package downgrade/override and module data access | Prevent commercial gating bypass. | Entitlement changes audited and separate from permissions. |

## Approval Policy Model

Future implementation should add a provider-neutral policy model instead of embedding approval checks inside each controller.

| Future concept | Purpose |
| --- | --- |
| `ApprovalPolicy` | Defines action key, risk level, required approval count, maker/checker rule, expiry, package applicability, and active status. |
| `ApprovalRequest` | Records requested action, target entity, safe summary, requested by, row-version snapshot, status, expiry, and metadata. |
| `ApprovalDecision` | Records approve/reject/expire/cancel decisions, actor, timestamp, safe reason, and row-version validation result. |
| `IApprovalGate` | Application service that checks whether an action can execute immediately or needs an approval request. |
| `GovernedActionKey` | Stable code such as `finance.supplier_payment.post`, `inventory.stock_adjustment.post`, or `identity.role_permissions.update`. |

The first implementation slice should introduce approval foundation without changing existing actions until specific module workflows opt in.

## WebAdmin Governance Surface

| Page/action | Decision |
| --- | --- |
| Approval queue | Internal WebAdmin list/detail for pending approvals, assigned or available to authorized approvers. |
| Request detail | Shows safe action summary, target entity link, requested actor, timestamps, row-version status, and current risk level. |
| Approve/reject | Row-version protected, anti-forgery protected, permission protected, and maker/checker enforced. |
| Delegations | Dedicated design required before implementation; no implicit delegation through role sharing. |
| Audit report | Read-only governance report for sensitive action history. |

## WebApi, Mobile, Worker, And Provider Rules

| Surface | Governance decision |
| --- | --- |
| Public WebApi | No approval management by default. |
| Member WebApi | No approval management by default; future employee/customer approvals require dedicated privacy design. |
| Business WebApi | No approval management by default until business mobile workflow is designed. |
| Mobile business | Can show approval tasks only after contract and push/privacy design. |
| Worker | Workers may expire approval requests or send reminders after a worker design; workers do not auto-approve. |
| Provider flows | Provider callbacks do not approve actions. They provide evidence only. |

## Sensitive Metadata Rules

Approval summaries, audit payloads, notes, and metadata must not contain credentials, access tokens, private keys, raw provider payloads, full bank identifiers, private payroll detail beyond the approved audience, or document archive contents.

## Implementation Order

1. `Approval Governance Foundation Design-To-Core Slice`: approval policy/request/decision model, service boundary, no module opt-in.
2. `WebAdmin Approval Queue Slice`: internal queue and read-only target links.
3. `Finance And Treasury SoD Opt-In Slice`: supplier invoice posting, supplier payment posting/reversal/settlement/correction, supplier advance posting/application/reversal.
4. `Inventory Adjustment SoD Opt-In Slice`: stock adjustment, count variance approval, and high-value movement approval.
5. `Payroll SoD Opt-In Slice`: payroll posting, payment, bank settlement, returned-transfer correction.
6. `Identity And Package Critical Action SoD Slice`: role/permission changes, tenant/package assignments, provider production activation.

## Test Plan For Future Slices

| Lane | Required coverage |
| --- | --- |
| Unit | Approval policy resolution, maker/checker, delegation expiry, action-key matching, row-version snapshot validation. |
| Infrastructure | Governance schema, indexes, unique active policies, safe JSON metadata, PostgreSQL and SQL Server migrations. |
| WebAdmin | Approval queue render, anti-forgery, row-version, permission checks, maker/checker source guards. |
| Domain workflows | Sensitive action cannot execute without required approval when policy is active. |
| Source guards | No status-only financial/inventory/payroll correction, no permission bypass, no secrets in approval metadata. |

## No Runtime Behavior Changes

This design does not enforce approvals yet. Current permission and audit behavior remains unchanged until implementation slices opt in module actions.
