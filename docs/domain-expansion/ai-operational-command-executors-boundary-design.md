# AI Operational Command Executors Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks the boundary for future AI-assisted operational command executors beyond current note/activity/follow-up-task routing. It is documentation-only and adds no entity, migration, route, DTO, provider call, WebAdmin action, worker, prompt execution, autonomous mutation, or module command.

Decision: AI can recommend, draft, and route review work today. Direct module mutation remains blocked until one concrete command family is selected, fully mapped, risk-rated, permission-checked, row-version guarded, and approval-controlled.

## Current Darwin AI Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| AI recommendations/action drafts/approvals | Governance foundation exists in Foundation. | Keep recommendation/draft/approval as the entry point for any executor. |
| `AiActionHandoffService` and executor registry | Provider-neutral handoff exists with low-risk executors. | Add only typed command executors; no reflection or generic action execution. |
| Timeline and internal follow-up task executors | Low-risk internal evidence/task routing exists. | Treat as current production boundary. |
| Scoped context projection | Aggregated module context exists for selected modules. | Operational executors must consume approved draft payloads, not raw model output. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Draft operational action | Operators can review AI-prepared changes. | Requires typed payload with target entity, row version, and safe summary. |
| Approval-controlled execution | Prevents autonomous risky mutations. | Requires permission, approval, SoD where relevant, and idempotency. |
| Execution evidence | Proves what AI helped do. | Requires event/audit, action draft status, and result linkage. |
| Module allow-list | Keeps AI contained to selected workflows. | Requires command family registry with risk policy. |

## Executor Eligibility Rules

| Rule | Decision |
| --- | --- |
| Concrete command family | Required. Examples: create CRM follow-up, draft support case, draft project task. |
| Typed payload | Required. Payload schema is owned by the target module and validated before approval and execution. |
| Row version | Required for updates/lifecycle actions. Creation actions require duplicate/idempotency key. |
| Permission | Executor runs as approved operator context or service context with explicit audit actor; never as anonymous AI. |
| Approval | Required for medium/high risk actions; low risk may use existing action approval policy. |
| Provider output | Raw model output never directly calls module handlers. |
| Idempotency | Required by action draft id and target command key. |

## Future Executor Categories

| Category | Default decision |
| --- | --- |
| Low-risk evidence | Already allowed through timeline and follow-up task executors. |
| Draft-only module records | Candidate next step, such as draft support case or draft project task. |
| Lifecycle transitions | Blocked until module-specific approval and row-version policy. |
| Finance/inventory/payroll/treasury mutations | Blocked unless a dedicated high-risk design and approval policy exist. |
| Provider operations | Blocked until provider target and readiness policy exist. |

## WebAdmin Surface

AI Governance WebAdmin can later show executor readiness per command family, payload preview, approval status, execution result, and target links. It must not expose provider credentials, raw prompt/completion, or broad command execution controls.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | `ai-governance` plus target module capability. |
| Package role | Add-on; executor requires both AI governance and target module entitlement. |
| Disabled behavior | Hide executor action buttons; existing recommendations remain review evidence where allowed. |
| Permissions | Review AI drafts, approve AI action, execute AI action for target command. |
| SoD | AI cannot approve its own action; maker/checker applies to target risk. |

## Compatibility Boundaries

- No autonomous mutation.
- No direct finance, inventory, payroll, treasury, shipment, payment, refund, archive, or provider success mutation without dedicated design.
- No public/member/mobile executor surface by default.
- No real AI provider activation in this design.

## Implementation Slices

1. `AI Executor Candidate Selection Checkpoint`: select one command family with business value and low/medium risk.
2. `Typed AI Executor Payload Contract Slice`.
3. `Target Module Draft-Only Executor Slice`.
4. `AI Executor Approval And SoD Integration Slice`.
5. Higher-risk executors only after target module design.

## Test Plan

Future tests must cover typed payload validation, permission/approval/SoD, row-version/idempotency, disabled target capability, no raw provider payload persistence, source guards against generic execution, and no public/mobile exposure.

## No Runtime Behavior Changes

This design does not implement new AI executors.
