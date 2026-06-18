# AI Action Handoff Execution Boundary Design

## Summary

This step locks the execution boundary for approved AI action drafts. It adds no entity, migration, route, DTO, WebAdmin mutation, provider adapter, provider credential, prompt execution, public/mobile/storefront contract, finance export format change, payment/refund flow, payroll provider submission, bank API, journal editor, or invoice archive/download behavior.

The selected direction is conservative: AI may recommend and draft, but execution is only a future handoff to existing owning Application command handlers. No AI service becomes a parallel mutation owner.

## Current Darwin AI Action Handoff Findings

- `AiActionDraft` already stores proposed command intent, target entity information, safe command payload JSON, risk level, lifecycle status, approval evidence, `ExecutedAtUtc`, and `ExecutionEventId`.
- WebAdmin can submit, approve, and reject action drafts, but it does not execute them.
- `AiProviderAdapterFoundationService` can create optional non-executing action drafts from safe provider output.
- Existing Application command handlers own all operational mutations across Sales, Finance, Purchasing, Inventory, HR, Payroll, Treasury, WebAdmin, journal posting, payment settlement, stock movement, provider submissions, archive generation, and exports.
- There is no canonical execution router, command allow-list, module executor registry, execution idempotency policy, or WebAdmin execution action yet.

## Decision Matrix

| Handoff surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Security/privacy impact | Business impact | Technical impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Execution trigger | Approved drafts exist but do not execute. | Execution requires a separate future handoff service and explicit operator action. | AI action handoff service. | Owning module command handlers only. | Prevents autonomous execution. | Operators keep final control after approval. | Requires command routing and idempotency. | Design-complete. | Implement foundation router later. |
| Command ownership | Module handlers already own mutations. | AI handoff must call existing handlers or module-owned executor wrappers. It must not mutate domain entities directly. | Module command owners. | Existing module handlers. | Preserves authorization, validation, row-version, posting, archive, and stock rules. | Business state changes follow the same rules as manual operations. | Requires per-command adapter wrappers. | Ready for future foundation. | Add executor registry with no default executors. |
| Command allow-list | Draft command text is currently free-form but validated as safe text. | Future execution requires an explicit allow-list by feature area and command type. | AI handoff policy plus module owners. | Module executor registration. | Prevents arbitrary command invocation. | Business can enable AI execution per operation class. | Requires source guards and registration tests. | Ready. | Start with no production executors. |
| Payload boundary | `CommandPayloadJson` exists. | Payload must be parsed into typed DTOs by the owning executor. No reflection-based blind invocation. | Module executor wrapper. | Module command handler. | Prevents unsafe payload injection. | Operators can trust AI drafts are validated like normal forms. | Requires typed mapper per command. | Ready. | Define typed executor contract. |
| Approval relationship | `AiActionApproval` records human approval. | Approval alone does not execute. Future execution must verify current approved status and row version. | AI governance service. | AI handoff service. | Prevents stale or rejected draft execution. | Operators avoid accidental stale actions. | Requires row-version and status checks. | Ready. | Add row-version guarded execute command later. |
| Authorization | WebAdmin uses authenticated operator actions. | Future execution must require normal WebAdmin authorization and module-level permission, not only AI approval. | WebAdmin and module authorization. | Existing authorization policies. | Prevents privilege escalation through AI. | AI cannot let an operator perform actions they cannot normally perform. | Requires executor-level permission checks. | Needs implementation design per module. | Keep execution UI blocked until policy exists. |
| Idempotency | Draft has execution fields. | Execution must be one-time and idempotent per draft id. Retry cannot execute the owning command twice. | AI handoff service. | AI handoff service plus command handler idempotency. | Prevents duplicated payments, stock moves, postings, or submissions. | Business avoids duplicate operational changes. | Requires execution event key and transaction policy. | Ready. | Use deterministic event key later. |
| Risk levels | Draft has risk level. | High-risk commands remain blocked until module-specific policy enables them. | AI handoff policy. | Module owner. | Prevents AI-assisted high-impact changes by default. | Business can gradually enable low-risk automation. | Requires risk-gated allow-list. | Ready. | Begin future foundation with no high-risk executors. |
| Failure handling | No execution failures exist. | Failed execution must leave draft approved but not executed, with safe error evidence only. | AI handoff service. | AI handoff service. | Prevents raw exception/provider/payload leakage. | Operators can retry or reject without hidden state changes. | Requires safe error summary and audit event. | Ready. | Implement safe failure records later if needed. |
| Event and audit | Business events and audit trails exist. | Future execution handoff must emit deterministic secret-free events for attempted, succeeded, and failed handoff. | AI handoff service plus BusinessEvent/AuditTrail. | AI handoff service. | Enables traceability and incident review. | Business can see who executed an AI-drafted action and why. | Requires event keys and payload validation. | Ready. | Add when implementation starts. |
| WebAdmin execution UI | Review UI exists. | No execute button is added in this design step. Future UI must show command owner, risk, validation status, and row version. | WebAdmin AI Governance. | AI handoff action only. | Avoids accidental execution before policy is complete. | Operators see review workflows without hidden automation. | Requires localized compact status later. | Later. | Add UI only after foundation and permissions. |
| Public/mobile boundary | Public/mobile contracts are stable. | No public WebApi, mobile/member, or storefront execution route. | WebApi/Mobile owners. | None. | Prevents consumer-facing AI mutation. | AI execution remains internal/operator controlled. | Preserves compatibility. | Locked. | Revisit only after internal execution is proven. |

## Locked Decisions

- AI action approval is consent evidence, not execution.
- Future execution must be a handoff to existing owning Application command handlers or module-owned executor wrappers.
- AI services must not mutate operational domain entities directly.
- Future execution requires explicit command allow-list, typed payload mapping, normal authorization, row-version checks, risk policy, and idempotency.
- No production executor is enabled by default.
- High-risk commands stay blocked until module owners explicitly enable them in a future implementation.
- Failed execution cannot create fake success or partially rewrite action draft state.
- Raw command payloads, provider payloads, credentials, private document contents, payroll internals, bank details, and archive payloads must not be stored in events, audit trails, errors, logs, or documentation.
- Public WebApi, mobile/member, storefront, finance export package format, invoice archive/download, payment/refund, supplier finance, payroll provider submission, bank API, and journal editor flows remain unchanged.

## Implementation Sequence

1. `AI Action Handoff Execution Boundary Design`
   - Outcome: this document. Execution ownership, allow-list, payload, authorization, idempotency, risk, audit, WebAdmin, and compatibility boundaries are locked.
2. `AI Action Handoff Foundation Slice`
   - Outcome: complete for current phase. `IAiActionDraftExecutor` and `AiActionHandoffService` exist in Application with approved-status checks, row-version checks, high-risk blocking, safe output validation, idempotent already-executed handling, and deterministic event evidence. The foundation itself does not execute arbitrary module commands; only separately designed low-risk executors are registered.
   - Tests use test-scoped executors to verify approval, row-version, idempotency, safe failure, and event/audit behavior.
   - Real Sales, Finance, Inventory, HR, Payroll, Treasury, archive, provider, payment, bank, or journal commands remain blocked until module-specific executor mappings are approved.
3. `AI Low-Risk Module Executor Slice`
   - Outcome: complete for current phase. The first executor is internal timeline note/activity evidence only.
4. `AI Internal Follow-Up Task Executor Slice`
   - Outcome: complete for current phase. The second low-risk executor creates internal `InternalFollowUpTask` records only.
5. `AI Target Provider Adapter Slice`
   - Remains separately blocked until a real provider/model target is selected.

## Documentation Outcome

- `BACKLOG.md` and `erp-expansion-master-status.md` record the module review routing executor outcome and keep the next AI gates decision-bound.
- `ai-readiness-automation-governance-design.md` records that execution remains limited to explicitly designed low-risk internal executors until broader module executors are approved.
- `DarwinWebAdmin.md` records that WebAdmin execution is limited to approved low-risk timeline and follow-up task drafts.
- `docs/README.md` links this document.

## Compatibility Guards

- Do not execute action drafts beyond explicitly registered, reviewed low-risk executors in WebAdmin.
- Do not use reflection to invoke commands from draft text.
- Do not register broad production module executors by default.
- Do not mutate payments, refunds, supplier payments, payroll payments, bank settlement, stock, journal entries, invoice archives, provider submissions, public/mobile contracts, or storefront behavior from AI.
- Do not store raw sensitive payloads in events, audit trails, metadata, logs, tests, or documentation.

## Documentation Verification

- `docs/README.md` links this document.
- The document contains no ambiguous action-handoff decisions for this phase.
- Restricted vendor/source scans must return no output.
- Ambiguity scans over touched AI docs must return no output.
