# AI Next Module Executor Design

## Summary

This step selects and implements the next low-risk AI action executor after timeline notes and activities. It is not a model-provider adapter, not autonomous execution, and not a shortcut into operational module commands.

The selected executor is internal follow-up task creation. An approved AI action draft can create an internal `InternalFollowUpTask` with title, description, priority, due date, assignee, target entity, and source action draft evidence. The task is owned by Foundation/WebAdmin and is used for operator follow-up only.

## Current Darwin AI Executor Findings

- `AiActionHandoffService` and `IAiActionDraftExecutor` already enforce approved status, row-version checks, high-risk blocking, idempotent already-executed handling, and deterministic event evidence.
- `AiTimelineActionDraftExecutor` already creates internal `Note` and `Activity` evidence only.
- `Activity` is timeline evidence and does not provide task status, assignee, due date, completion, or cancellation workflow.
- WebAdmin AI Governance can execute eligible approved drafts manually from detail only.
- Public WebApi, mobile/member, storefront, finance export, payments/refunds, supplier finance, payroll provider, bank API, journal editor, stock movement, shipment mutation, and invoice archive/download flows are unchanged.

## Decision Matrix

| Executor surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Business impact | Technical impact | WebAdmin impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Next executor scope | Timeline note/activity exists, but no status-based follow-up queue exists. | Add internal follow-up task creation only. | Foundation internal task model. | AI follow-up task executor through `InternalFollowUpTaskService`. | Operators get a real task queue from approved AI suggestions without changing business state. | Adds a small explicit Foundation model instead of hiding task state in metadata. | AI Governance gains a follow-up task queue and detail close actions. | Ready. | Implement core/admin/executor. |
| Execution trigger | Manual action-draft detail execution exists. | Keep manual execute from action-draft detail only. | WebAdmin AI Governance. | `AiActionHandoffService`. | Human operators stay in control. | Avoids automatic/bulk execution semantics. | Existing execute action is reused. | Ready. | No list/bulk execution. |
| Permission policy | AI Governance is internal WebAdmin. | Keep conservative internal WebAdmin execution only. | WebAdmin authorization plus future module permissions. | Foundation task executor only. | AI cannot bypass operational module permissions. | Broader command families still need dedicated designs. | Follow-up tasks are internal. | Ready. | Document broader executor gate. |
| Attempt schema | Draft execution fields and BusinessEvent exist. | No new execution-attempt table for this low-risk executor. | AI handoff service. | Handoff service and BusinessEvent. | Enough evidence for internal task creation. | Avoids migration for retry history before higher-risk execution exists. | Shows executed timestamp/event id. | Ready. | Revisit for higher-risk executors. |
| Payload shape | Draft payload is JSON. | Use explicit JSON: title, description, priority, dueAtUtc or dueInHours, assignedToUserId, metadataJson. | AI follow-up task executor. | Internal task service. | Operators receive useful work items. | No reflection or arbitrary command invocation. | Payload remains visible on draft detail. | Ready. | Implement typed parser. |
| Idempotency | `ExecutedAtUtc` prevents repeated execution. | Also guard task creation by `SourceAiActionDraftId`. | Internal task service. | Internal task service. | Retry cannot create duplicate follow-up tasks. | Defends against partial retry paths. | Refresh remains safe. | Ready. | Add tests. |
| Closure workflow | No generic follow-up task closure exists. | Add complete and cancel actions for internal follow-up tasks. | Foundation task service. | WebAdmin AI Governance task actions. | Operators can close or cancel AI-created work without changing operational facts. | Closure is evidence only. | Compact task detail actions. | Ready. | Implement WebAdmin forms. |
| Operational boundaries | Many module command handlers exist. | Do not call module handlers from this executor. | None. | None. | AI follow-up suggests work, not business-state changes. | Prevents stock/payment/journal/provider mutation. | No operational mutation buttons. | Locked. | Future module executor design required. |

## Locked Decisions

- The next production executor is internal follow-up task creation only.
- Execution remains manual from action-draft detail after approval.
- No execution-attempt table is added for this low-risk executor.
- A real Foundation task model is used because timeline `Activity` does not carry status, assignee, due date, completion, or cancellation workflow.
- The executor parses typed JSON and does not use reflection or arbitrary command invocation.
- Retry cannot create duplicate tasks for the same source AI action draft.
- Task completion and cancellation are internal WebAdmin evidence only and do not mutate the target entity.
- Public WebApi, mobile/member, storefront, finance export, payment/refund, supplier finance, payroll provider, bank API, journal editor, stock movement, shipment mutation, and invoice archive/download flows remain unchanged.

## Implementation Outcome

- `InternalFollowUpTask` is the Foundation-owned internal task record for AI-created review work.
- `InternalFollowUpTaskService` owns create, update, complete, and cancel validation and emits safe event evidence.
- `AiInternalFollowUpTaskActionDraftExecutor` is registered as the second low-risk production executor.
- WebAdmin AI Governance exposes `FollowUpTasks` list/detail pages with complete and cancel actions.
- The executor creates only internal task evidence and does not call operational module commands.

## Next Gate

The broader AI executor decision is recorded in [ai-broader-module-executor-decision.md](ai-broader-module-executor-decision.md). The selected next family is internal module review routing over `InternalFollowUpTask`, not direct module mutation. Any future executor that mutates a module workflow, ledger, stock, payment, provider, archive, or public/mobile contract still requires its own decision matrix, permission policy, typed payload mapping, and tests.

## Documentation Verification

- `docs/README.md` links this document.
- The document contains no ambiguous executor decision for this phase.
- Restricted vendor/source scans must return no output.
- Ambiguity scans over touched AI docs must return no output.
