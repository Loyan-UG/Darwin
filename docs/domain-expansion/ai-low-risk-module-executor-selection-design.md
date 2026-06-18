# AI Low-Risk Module Executor Selection Design

## Summary

This step selects the first production AI action executor scope. It adds no entity, migration, public/mobile/storefront contract, provider adapter, provider credential, raw prompt/completion storage, finance export format change, payment/refund flow, payroll provider submission, bank API, journal editor, or invoice archive/download behavior.

The selected first executor is deliberately low-risk: an approved AI action draft may create an internal `Note` or `Activity` on an existing target entity through Foundation timeline evidence. It does not mutate operational state, post stock, post journals, settle payments, submit providers, generate archives, or change customer/mobile-facing behavior.

## Current Darwin Low-Risk Executor Findings

- `AiActionHandoffService` and `IAiActionDraftExecutor` exist with approved-status checks, row-version checks, high-risk blocking, safe output validation, idempotent already-executed handling, and deterministic event evidence.
- The handoff foundation exists. The first production executor selected by this document is internal timeline evidence; the later internal follow-up task executor is documented separately as the second low-risk executor.
- `EntityTimelineService`, `Activity`, and `Note` already provide internal timeline evidence for entities.
- WebAdmin AI Governance can review action drafts and execute only eligible approved drafts handled by explicitly registered low-risk executors.
- Public WebApi, mobile/member, storefront, finance export, payment/refund, supplier finance, payroll provider, bank API, journal editor, and invoice archive/download flows are unchanged.

## Decision Matrix

| Executor surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Business impact | Technical impact | WebAdmin impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| First executor scope | Handoff foundation did not execute operational commands. | Use internal note/activity creation only. | Foundation timeline. | AI timeline executor through `Activity`/`Note` evidence. | Operators can turn approved AI suggestions into visible internal follow-up evidence. | Does not change operational state or ledgers. | Detail page shows an execute action for eligible approved drafts. | Complete. | Keep broader executors behind separate design. |
| Execution trigger | Approved drafts require a human trigger. | Manual execute action on action-draft detail only. | WebAdmin AI Governance. | `AiActionHandoffService`. | Operator sees the exact draft before execution. | Avoids bulk or automatic execution. | Compact execute form exists only for ready drafts. | Complete. | Keep automatic execution blocked. |
| Permission policy | WebAdmin AI permissions and module permissions are separate. | Future production hardening must require both AI Governance and owning module/action permission. | WebAdmin authorization plus module owners. | Module executor wrappers. | Prevents AI from bypassing normal access. | Requires module-specific policy checks for future executors. | Current timeline executor remains internal Foundation evidence. | Ready for low-risk v1. | Document and source-guard no broader executor. |
| Schema for attempts | Draft has execution fields and BusinessEvent exists. | No new attempt table for low-risk v1. Use `ExecutedAtUtc`, `ExecutionEventId`, `BusinessEvent`, and `AuditTrail` evidence. | AI handoff service. | Handoff service. | Sufficient audit for note/activity execution. | Avoids migration before higher-risk execution exists. | UI can show executed timestamp and event link/id. | Ready. | Add attempt table only before higher-risk retries. |
| Payload shape | Draft payload is JSON. | Use typed, explicit JSON payload for timeline entries: note or activity plus safe body/title/summary. | AI timeline executor. | Timeline executor. | Operators get clear internal evidence. | No reflection or arbitrary command invocation. | Payload remains visible in detail. | Ready. | Implement parser and validation. |
| Risk policy | Draft has risk level. | Only low and medium risk timeline drafts may execute. High-risk remains blocked. | AI handoff service. | Handoff service. | Avoids high-impact AI execution. | Preserves high-risk guard. | High-risk drafts stay review-only. | Ready. | Keep high-risk guard. |
| Idempotency | Handoff service has `ExecutedAtUtc`. | Retry returns already-executed result and does not create another note/activity. | AI handoff service. | Handoff service. | Prevents duplicate timeline evidence. | Uses existing draft execution fields. | UI can refresh safely. | Ready. | Keep idempotency tests. |
| Public/mobile boundary | No public/mobile AI route exists. | No public/mobile execution route. | WebApi/Mobile owners. | None. | AI execution remains internal. | Compatibility preserved. | WebAdmin only. | Locked. | Keep compatibility smoke. |

## Locked Decisions

- The first production executor is internal timeline evidence only: `Note` or `Activity`.
- Execution is manual from AI action-draft detail, not automatic after approval and not bulk from list.
- No new execution-attempt table is added for this low-risk v1.
- No payment, refund, supplier finance, payroll, bank, journal, stock, shipment, invoice archive, provider submission, public/mobile, or storefront mutation is allowed.
- Command payload is parsed explicitly; reflection-based command invocation remains forbidden.
- High-risk drafts remain blocked.
- Future broader executors require their own module-specific design and tests.

## Implementation Sequence

1. `AI Low-Risk Module Executor Selection Design`
   - Outcome: this document. The first production executor scope is internal timeline note/activity.
2. `AI Timeline Executor Slice`
   - Outcome: complete for current phase. `AiTimelineActionDraftExecutor` is registered in Application as the first low-risk production executor. WebAdmin action-draft detail can execute eligible approved timeline drafts with anti-forgery and row-version evidence.
   - The executor creates internal `Note` or `Activity` timeline evidence only and does not mutate operational state.
3. `AI Low-Risk Executor Hardening`
   - Outcome: complete for current focused phase. Unit/source guards verify low-risk note/activity execution, idempotency, high-risk blocking, and absence of broader payment, refund, bank, journal, stock, archive, provider, public, or mobile mutation surfaces.
4. `AI Next Module Executor Design`
   - Outcome: complete for current phase. The next selected command family is internal follow-up task creation with a real Foundation task model, WebAdmin queue, manual execution, and no operational module mutation.

## Documentation Outcome

- `BACKLOG.md` and `erp-expansion-master-status.md` record the module review routing executor outcome and keep the next AI gates decision-bound.
- `ai-action-handoff-execution-boundary-design.md` records the selected first executor.
- `DarwinWebAdmin.md` records that current execute surfaces are internal timeline evidence and internal follow-up task creation.
- `docs/README.md` links this document.

## Compatibility Guards

- Do not add arbitrary command execution, reflection invocation, bulk execution, automatic execution after approval, or high-risk execution.
- Do not add provider calls, credential UI, prompt/completion viewers, public/mobile routes, or storefront behavior.
- Do not mutate stock, payments, refunds, supplier payments, payroll payments, bank settlement, journal entries, invoice archives, shipments, provider submissions, finance exports, or customer-facing contracts.
- Do not store raw sensitive payloads in note/activity body, event payloads, metadata, logs, tests, or documentation.

## Documentation Verification

- `docs/README.md` links this document.
- The document contains no ambiguous low-risk executor decisions for this phase.
- Restricted vendor/source scans must return no output.
- Ambiguity scans over touched AI docs must return no output.
