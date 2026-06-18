# AI Broader Module Executor Decision

## Summary

This decision closes the broader AI executor gate after the internal timeline and internal follow-up task executors. The selected direction remains conservative: the next broader executor family routes approved AI drafts into internal module review work, backed by `InternalFollowUpTask`, and does not mutate operational module state.

This step is documentation and decision only. It adds no entity, migration, route, DTO, WebAdmin mutation, public/mobile/storefront contract, model-provider adapter, prompt execution, finance export format change, payment/refund flow, payroll provider submission, bank API, journal editor, stock movement, shipment mutation, or invoice archive/download behavior.

## Current Darwin AI Executor Findings

- `AiActionHandoffService` and `IAiActionDraftExecutor` exist and enforce approved status, row-version checks, high-risk blocking, idempotent already-executed handling, safe output validation, and event evidence.
- `AiTimelineActionDraftExecutor` creates internal `Note` and `Activity` timeline evidence only.
- `AiInternalFollowUpTaskActionDraftExecutor` creates `InternalFollowUpTask` records only, with priority, due date, assignee, target entity, source draft linkage, completion, and cancellation.
- WebAdmin AI Governance has manual detail execution only. It has no bulk, automatic, background, public, or mobile execution surface.
- No real model provider is enabled. Provider implementation remains blocked until target, credential owner, payload mapping, rate/cost policy, safe error contract, and smoke strategy are selected.
- Operational modules already have authoritative command handlers for Sales, Finance, Purchasing, Inventory, HR, Payroll, Treasury, bank settlement, stock, shipments, journals, and archives. AI executors must not bypass those owners.

## Decision Matrix

| Executor surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Business impact | Technical impact | WebAdmin impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Broader executor family | Timeline and follow-up task executors exist. | Select module review routing, not operational mutation. | Foundation `InternalFollowUpTask`. | AI module review routing executor through task service only. | Operators receive actionable review work for a module without AI changing business facts. | Reuses explicit task model and keeps ledgers/workflows untouched. | AI draft detail can execute eligible review-routing drafts. | Ready. | Implement module review routing aliases and tests. |
| Operational command execution | Module handlers own business mutations. | Keep direct module commands blocked. | Module owners. | Existing module command handlers only. | Business avoids unexpected AI changes to stock, finance, payroll, bank, shipments, and customer-facing records. | Avoids premature rollback/reversal/idempotency complexity. | No new operational mutation button. | Locked for current phase. | Design each operational executor separately later. |
| Execution trigger | Manual action-draft detail execution exists. | Keep manual detail execution only. | WebAdmin AI Governance. | `AiActionHandoffService`. | Human operator reads the draft before execution. | Avoids bulk/background semantics and keeps audit clear. | Existing execute form remains the only trigger. | Ready. | No list, bulk, or automatic execution. |
| Risk policy | High-risk drafts are blocked. | Review-routing drafts can be low or medium risk only; high risk remains blocked. | AI handoff service. | Handoff service plus executor guard. | Prevents high-impact actions from being hidden as tasks. | Preserves current risk gate. | High-risk drafts stay review-only. | Ready. | Keep high-risk tests. |
| Payload shape | Draft payload is JSON. | Use the same safe task payload shape: title, description, priority, dueAtUtc or dueInHours, assignedToUserId, metadataJson. | Module review routing executor. | Internal task service. | Operators get complete work items with owner, priority, and due date. | No reflection, arbitrary command invocation, or module DTO spoofing. | Payload remains visible in draft detail. | Ready. | Reuse task parser and validation. |
| Module target | Draft already stores feature area, entity type, and target id. | Route task to the draft target entity and feature area. | AI action draft record. | Task service validates safe target identifiers only. | Work appears tied to the correct business object. | No target rewrite or hidden module lookup. | Task detail links source draft and target evidence. | Ready. | Keep target immutable during execution. |
| Permission policy | AI Governance is internal WebAdmin. | Require AI Governance execution permission now; module mutation permissions are required only before future operational executors. | WebAdmin authorization and future module policies. | WebAdmin and future module owners. | Current task routing does not grant business mutation power. | Avoids fake module permission checks before mutation exists. | Internal review task pages remain under AI Governance. | Ready. | Add module permission checks before any operational executor. |
| Execution evidence | Draft execution fields and `BusinessEvent` exist. | Keep no separate attempt table for review-routing. | Handoff service and task service. | Handoff service. | Audit is sufficient for internal task creation. | Attempt table can wait until retries have business consequences. | Executed timestamp/event remains visible. | Ready. | Add attempt table before higher-risk or provider-driven execution. |
| Provider dependency | Provider foundation exists without real target. | Do not require a real model provider for executor routing. | AI governance records. | None. | Operators can use manually created or test-created approved drafts safely. | Provider implementation remains independently gated. | No credential or prompt UI. | Locked. | Provider target remains a separate gate. |
| Compatibility boundary | Public/mobile/storefront contracts are stable. | Keep execution WebAdmin/internal only. | WebAdmin AI Governance. | None outside WebAdmin. | No customer/member-facing behavior changes. | Compatibility smoke remains stable. | No public/mobile route. | Locked. | Keep contracts and mobile tests green. |

## Locked Decisions

- The next broader executor family is module review routing backed by `InternalFollowUpTask`.
- It remains internal/WebAdmin-only and manually executed from approved action-draft detail.
- It creates operator review work only. It does not mutate the target entity or call Sales, Finance, Purchasing, Inventory, HR, Payroll, Treasury, shipment, payment, refund, journal, archive, provider, public, mobile, or storefront handlers.
- It reuses the explicit follow-up task payload shape and task service validation.
- High-risk drafts remain blocked.
- No execution-attempt table is added for this review-routing family.
- No real model provider is required or enabled by this decision.
- Future direct operational executors require separate module-specific design, permission policy, typed payload mapping, idempotency policy, reversal or correction policy where applicable, and focused tests.
- Provider implementation remains blocked until a real provider/model target, credential owner, payload mapping, rate/cost policy, retry policy, safe error contract, and smoke strategy are selected.

## Implementation Plan

1. `AI Broader Module Executor Decision`
   - Outcome: this document. The selected family is module review routing through internal follow-up tasks.
2. `AI Module Review Routing Executor Slice`
   - Outcome: complete for current phase. `AiModuleReviewTaskActionDraftExecutor` registers the `CreateModuleReviewTask` command family and routes approved drafts into `InternalFollowUpTaskService`.
   - Execution is manual, idempotent, safe, and internal.
   - Source guards verify that no operational module mutation, provider call, public/mobile route, or finance/export behavior is introduced.
3. `AI Target Provider Adapter`
   - Remains blocked until a real target and secure deployment policy are selected.
4. `Operational Module Executor Design`
   - Only after a concrete command family is selected. Each candidate must define business consequence, owner, permissions, typed payload, row-version target, idempotency, failure, and rollback/reversal behavior.

## Documentation Outcome

- `BACKLOG.md` and `erp-expansion-master-status.md` record module review routing as complete and keep the next gates limited to decisions that require a real target or operational command family.
- `ai-readiness-automation-governance-design.md`, `ai-next-module-executor-design.md`, and `DarwinWebAdmin.md` record that current AI execution remains limited to internal timeline, internal follow-up task, and the planned module review routing family.
- `docs/README.md` links this document.

## Current Checkpoint

- The follow-up operational-executor decision remains option A: no direct operational module executor is added now.
- Completing direct execution remains a future gate, not abandoned work.
- A future operational executor starts only after a concrete command family, owner, permission policy, typed payload, row-version target, idempotency policy, failure behavior, and reversal or correction policy are selected.

## Compatibility Verification

- Documentation-only step; no code test is required.
- Restricted vendor/source scans must return no output.
- Ambiguity scans over touched AI docs must return no output.
- The document must explicitly preserve public WebApi, mobile/member, storefront, finance export, payment/refund, payroll provider, bank API, journal editor, stock movement, shipment, and invoice archive/download boundaries.
