# AI-Readiness And Automation Governance Design

## Summary

This step locks the AI-readiness and automation governance boundary before any schema, route, model-provider integration, prompt pipeline, WebAdmin action, public/mobile contract, storefront behavior, finance export format, payroll provider submission, payment/refund flow, or invoice archive/download behavior is changed.

The selected v1 direction is conservative and auditable: AI can inspect approved, scoped, privacy-safe projections, create recommendations, and draft actions. Normal Darwin application handlers remain the only mutation owners, and a human approval step is required before any operational command executes.

## Current Darwin AI-Readiness Findings

- `BusinessEvent` and `AuditTrail` already provide append-only operational evidence across Sales, Finance, Purchasing, Inventory, HR, Payroll, Treasury, and WebAdmin workflows.
- `FeatureArea` already includes an AI category that can anchor visibility, permissions, and future module packaging.
- `DocumentRecord`, `ExternalReference`, `CustomField`, and activity/note foundations already exist, but none of them should become raw AI prompt storage.
- HR, payroll, bank, provider, finance, invoice archive, personnel document, and identity surfaces contain sensitive data that must be denied by default for AI workflows.
- Operational command handlers already own mutations for orders, invoices, payments, supplier payments, payroll payments, stock movement, bank settlement, HR workflows, and WebAdmin actions.
- There is no canonical AI recommendation, action draft, approval, sensitive-field registry, prompt execution, or model-provider adapter in the current core model.
- Public WebApi, mobile/member, storefront, invoice archive/download, finance export, payroll provider submission, customer payment/refund, supplier finance, bank API, and journal editor flows are unchanged by this design step.

## Decision Matrix

| Governance surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Security/privacy impact | Business impact | Technical impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Sensitive field classification | Sensitive data exists across HR, payroll, bank, finance, provider, document, identity, and commerce modules. | Add a canonical deny-by-default classification policy for fields and payload categories before AI can read them. | AI governance foundation plus module owners. | Governance handlers only. | Prevents raw sensitive data entering prompts, logs, events, metadata, or tests. | Business owners can trust that AI features do not expose payroll, bank, or personnel details casually. | Requires structured registry and source guards. | Ready. | Implement classification registry and tests. |
| Scoped AI data access | Current queries are module-specific and permission-scoped. | AI reads purpose-bound projections, not raw domain entities or operational document payloads. | Module projection handlers plus AI access policy. | Read handlers only. | Reduces cross-business and cross-module leakage. | Operators receive useful summaries without granting broad database access. | Requires a central access facade and per-scope validation. | Ready. | Implement AI context access service. |
| Recommendation records | Business events and activities exist, but not recommendation lifecycle records. | Store AI recommendations as structured evidence with source scope, confidence, reason, status, and expiration. | AI recommendation aggregate. | Recommendation handlers. | Recommendation text must be secret-free and must not include raw private payloads. | Operators can review suggestions as a queue instead of losing them in chat output. | Requires schema and lifecycle status. | Ready. | Implement recommendation core model. |
| Action drafts | Command handlers own mutations today. | AI may create action drafts that reference an existing application command shape, but does not execute the command directly. | AI action draft aggregate. | Draft handlers and existing command handlers after approval. | Keeps AI out of direct stock, payment, payroll, invoice, and bank mutations. | Users see proposed changes before they affect operations. | Requires payload validation and command-owner routing. | Ready. | Implement action draft core model. |
| Approval path | WebAdmin workflows already use row-version and anti-forgery patterns. | Human approval is required before any AI-drafted action is executed. | WebAdmin approval workflow plus application command owner. | Existing application command handlers. | Creates accountability for automated assistance. | Business owners can allow AI help without giving AI autonomous authority. | Requires approval timestamps, actor, and audit evidence. | Ready. | Implement approval workflow after core model. |
| Mutation ownership | Every module already has owning handlers. | AI never becomes a parallel mutation path. It prepares draft intent; the owning module command validates and executes. | Existing module command handlers. | Existing handlers only. | Preserves authorization, row-version, idempotency, posting, archive, and inventory rules. | Avoids conflicting operations or unexplained state changes. | Requires draft-to-command mapping and source guards. | Ready. | Enforce source guards in implementation. |
| HR and payroll data | HR and payroll have private records and employee self-service boundaries. | Raw personnel documents, payroll rule source, payslip HTML, bank settlement details, and provider payloads are denied by default. | HR/Payroll module owners plus AI policy. | HR/Payroll handlers only. | Prevents disclosure of employee private data. | Employees and operators get safer AI-assisted workflows. | Requires redacted summaries before AI access. | Ready. | Add HR/payroll classification coverage. |
| Finance and bank data | Posted journals, bank statements, reconciliation, and export packages exist. | AI can use aggregate financial health summaries only when scoped; raw bank details, credentials, provider payloads, package contents, and journal edit shortcuts are denied. | Finance/Treasury owners plus AI policy. | Finance/Treasury handlers only. | Prevents treasury and accounting leakage. | Finance assistance remains reviewable and non-invasive. | Requires summary projections and deny-list tests. | Ready. | Add finance/treasury classification coverage. |
| Documents and archive | `DocumentRecord` links binaries and metadata. | AI does not read object contents, archived invoice payloads, payslip HTML, personnel files, or provider files unless a dedicated extraction policy is implemented later. | Document/storage owners. | Document handlers only. | Protects legal and private documents. | Business users can trust AI suggestions are not scraping files unexpectedly. | Requires document-kind policy and tests. | Ready. | Implement metadata-only default policy. |
| Prompt/model-provider boundary | No provider adapter exists. | Do not add model-provider API, credentials, prompt execution, or external model logs in this foundation core. | Later AI provider adapter design. | No provider mutation in v1 foundation. | Avoids storing or leaking secrets before governance exists. | Enables governance-first rollout. | Keeps core provider-neutral. | Ready. | Design provider adapter after core governance. |
| WebAdmin visibility | WebAdmin is the operator surface. | WebAdmin v1 shows recommendations and drafts as review queues, with no autonomous execution and no credential entry. | WebAdmin AI workspace. | Approval actions plus existing command owners. | Keeps AI review internal. | Operators get a controlled work queue. | Requires compact, localized HTMX surfaces. | Ready after core model. | Implement WebAdmin AI workspace after core model. |
| Public/mobile/storefront boundary | Public and mobile contracts are compatibility-sensitive. | No public/mobile/storefront AI route in v1. | WebApi/Mobile owners. | None. | Avoids consumer-facing AI leakage. | AI starts as internal ERP assistance. | Preserves release contracts. | Locked. | Revisit only after internal governance is proven. |
| Audit and evidence | `BusinessEvent` and `AuditTrail` exist. | Every recommendation lifecycle change, draft approval, draft rejection, and execution handoff records secret-free audit evidence. | AI governance service plus business event/audit foundation. | AI governance handlers. | Supports accountability and incident review. | Business owners can review why AI suggested or drafted something. | Requires deterministic event keys and payload validation. | Ready. | Implement event/audit wiring. |

## Locked Decisions

- AI v1 is a governance foundation, not an autonomous operator.
- AI may recommend and draft. It does not execute operational commands by itself.
- Human approval is required before an AI-drafted action can be handed to a normal Darwin command handler.
- Existing application command handlers remain the only mutation owners for operational state.
- Sensitive data is denied by default. HR, payroll, bank, provider, credential, personnel document, invoice archive payload, payslip HTML, raw import/export package content, identity token, and private document data are not AI-readable by default.
- AI reads scoped projections and summaries, not raw domain entities, raw object-storage contents, raw provider payloads, or mutable operational document payloads.
- Recommendation and action draft records must store only safe, reportable, reviewable fields.
- Prompt/model-provider integration is not part of this design step. Provider credentials, access tokens, private keys, raw prompts, raw completions, and provider payloads are not stored in metadata, events, audit trails, external references, tests, logs, or documentation.
- Public WebApi, mobile/member, storefront, finance export package format, invoice archive/download, customer payment/refund, supplier finance, payroll provider submission, bank API, and journal editor flows remain unchanged.

## Implementation Sequence

1. `AI-Readiness And Automation Governance Design`
   - Outcome: this document. Sensitive-data boundaries, scoped access, recommendation records, action drafts, approval-required execution, provider-neutrality, and compatibility rules are locked.
2. `AI Governance Foundation Core Model Slice`
   - Outcome: complete for current phase. Internal core models and services for sensitive field policy, deny-by-default access evaluation, recommendation records, action drafts, approvals, lifecycle evidence, PostgreSQL/SQL Server migrations, and source guards are implemented.
3. `AI Governance WebAdmin Review Workspace Slice`
   - Outcome: complete for current phase. Internal WebAdmin review queues for recommendations and action drafts are implemented with row-version protected recommendation accept/dismiss/expire and action draft submit/approve/reject workflows, without prompt-provider credentials or autonomous execution.
4. `AI Scoped Context Projection Slice`
   - Outcome: complete for current phase. `AiScopedContextProjectionService` provides aggregate, purpose-bound module metrics for Sales, Finance, Purchasing, Inventory, HR, Payroll, and Treasury without exposing raw entity payloads or sensitive fields.
5. `AI Provider Adapter Boundary Design`
   - Outcome: complete for current phase. Provider selection, credential owner, prompt contract, scoped context source, safe logging, retry/error policy, no-network smoke strategy, WebAdmin limits, and compatibility boundaries are locked without adding provider credentials, prompt execution, raw prompt/completion persistence, or autonomous execution.
6. `AI Provider Adapter Foundation Slice`
   - Outcome: complete for current phase. Provider-neutral orchestration exists over scoped context and AI governance records with a test-scoped deterministic adapter only.
7. `AI Target Provider Selection And Adapter Slice`
   - Outcome: design-complete for current phase. Target selection remains deployment-approved and configurable; no real provider is activated by default.
8. `AI Action Handoff Execution Boundary Design`
   - Outcome: complete for current phase. Approved-draft execution ownership, command allow-list, typed payload mapping, authorization, row-version checks, risk policy, idempotency, safe failure, event/audit evidence, WebAdmin limits, and compatibility boundaries are locked.
9. `AI Action Handoff Foundation Slice`
   - Outcome: complete for current phase. An internal handoff service and typed executor registry exist. The foundation started without broad module executors; the only enabled production executors now are the explicitly designed internal timeline and internal follow-up task executors.
10. `AI Low-Risk Module Executor Selection Design`
   - Outcome: complete for current phase. The first production executor scope is internal timeline evidence only.
11. `AI Timeline Executor Slice`
   - Outcome: complete for current phase. Approved action drafts can create internal `Note` or `Activity` records only when the timeline command payload is valid, row-version checks pass, and the draft is not high-risk.
12. `AI Internal Follow-Up Task Executor Slice`
   - Outcome: complete for current phase. Internal follow-up task creation is implemented as the next low-risk executor after timeline evidence.
13. `AI Broader Module Executor Decision`
   - Outcome: complete for current phase. The selected broader family is internal module review routing over `InternalFollowUpTask`, not direct operational module mutation.
14. `AI Module Review Routing Executor Slice`
   - Outcome: complete for current phase. `CreateModuleReviewTask` routes eligible approved drafts into internal follow-up tasks while keeping execution manual, internal, and task-only.
15. `AI And Integration Decision Checkpoint`
   - Outcome: complete for current phase. Real AI provider activation, direct operational AI executor, SyncState/SyncConflict, and accounting API adapter remain conditional gates until concrete targets, owners, payloads, policies, and smoke strategies are selected.

## Implementation Outcome

- AI governance decisions are locked for the current phase.
- AI governance foundation core is implemented for the current phase.
- AI governance WebAdmin review workspace is implemented for the current phase.
- AI scoped context projection is implemented for the current phase.
- The AI provider adapter boundary design is complete for the current phase.
- The AI provider adapter foundation is complete for the current phase.
- The AI target provider selection design is complete for the current phase.
- The AI action handoff execution boundary design is complete for the current phase.
- The AI action handoff foundation is complete for the current phase.
- The AI low-risk timeline executor is complete for the current phase.
- The AI internal follow-up task executor is complete for the current phase.
- There is no no-decision implementation gate for real provider activation, direct operational AI execution, two-way sync, or accounting API delivery. Each is conditional on selecting a real target or command family.
- The core implementation adds Foundation schema for sensitive field policy, recommendations, action drafts, and approval evidence. It adds no public/mobile/storefront route, model-provider adapter, prompt execution, autonomous mutation, finance export format change, customer/supplier payment flow, payroll provider submission, bank API, journal editor, or invoice archive/download behavior.
- The WebAdmin implementation adds internal list/detail review queues and review actions only. It does not execute action drafts, call model providers, store prompts/completions, expose credentials, add public/mobile routes, or bypass owning Application command handlers.
- The scoped context implementation adds an Application service that returns aggregate module metrics only. It requires a purpose key, restricts module keys to the allowed ERP modules, keeps collections non-null, and excludes raw names, private addresses, bank identifiers, document content, provider payloads, payroll internals, journal line details, object-storage contents, prompt text, and completion text.
- The provider boundary design keeps the next implementation provider-neutral. Credentials stay in secure configuration or a secret store, provider adapters consume scoped context only, raw prompts and raw completions are not persisted by default, provider output routes through AI governance records, and no-network adapters are limited to test composition.
- The provider foundation adds an internal `IAiProviderAdapter` contract and `AiProviderAdapterFoundationService`. It builds scoped context, calls only ready adapters, validates safe output, creates governed `AiRecommendation` and optional non-executing `AiActionDraft` records, fails safely when no adapter is configured, and does not register a fake provider adapter in production composition.
- The target-provider selection design keeps real provider implementation blocked until a provider/model target, credential owner, payload mapping, rate/cost policy, safe error contract, and smoke strategy are selected. Provider settings remain deployment-owned and no WebAdmin credential UI is added.
- The action handoff boundary keeps approval as consent evidence only. Future execution must go through a typed executor registry, explicit command allow-list, normal authorization, row-version checks, idempotency, and owning Application command handlers.
- The action handoff foundation adds `IAiActionDraftExecutor` and `AiActionHandoffService`. Broader real AI-assisted execution remains blocked until a module command family, permission policy, typed payload mapping, and idempotency expectation are selected.
- The first low-risk executor is intentionally limited to internal `Note` and `Activity` timeline evidence. It does not mutate stock, payments, refunds, supplier finance, payroll, bank settlement, journals, shipments, invoice archives, provider submissions, public/mobile routes, or storefront behavior.

## Compatibility Guards

- Do not add AI routes to public WebApi, mobile/member, or storefront surfaces in v1.
- Do not add model-provider credentials, API keys, prompt execution, raw prompt logs, raw completion logs, or provider payload storage before a provider adapter boundary is designed.
- Do not let AI mutate orders, invoices, payments, refunds, supplier finance, payroll payments, bank settlement, stock, HR records, payroll runs, journal entries, export batches, invoice archives, or provider filings directly.
- Do not store raw sensitive data in recommendation text, draft payloads, metadata JSON, `BusinessEvent`, `AuditTrail`, `ExternalReference`, logs, tests, or documentation.
- Do not treat document metadata, notes, custom fields, or activity text as a substitute for explicit sensitive-field classification and scoped access policy.

## Documentation Verification

- `docs/README.md` links this document from the ERP expansion map.
- `BACKLOG.md` and `erp-expansion-master-status.md` record the AI/integration checkpoint and keep the next AI and integration gates decision-bound.
- This document contains no deferred ambiguous decisions for the governance boundary.
- Restricted vendor/source scans must return no output.
