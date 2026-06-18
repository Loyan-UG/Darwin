# AI Provider Adapter Boundary Design

## Summary

This step locks the provider-adapter boundary for Darwin AI workflows. It adds no entity, migration, route, WebAdmin mutation, public/mobile/storefront contract, model-provider call, credential UI, prompt execution, autonomous execution, finance export format change, payment/refund flow, payroll provider submission, bank API, journal editor, or invoice archive/download behavior.

The selected direction is provider-neutral and governance-first. AI providers may only consume purpose-bound scoped projections, may only return safe structured recommendation or action-draft outputs, and may not execute operational commands. Credentials stay in secure configuration or a secret store, not in Darwin domain metadata, events, audit payloads, external references, documents, logs, tests, or documentation.

## Current Darwin AI Provider Findings

- `AiScopedContextProjectionService` provides aggregate, purpose-bound module metrics for Sales, Finance, Purchasing, Inventory, HR, Payroll, and Treasury without exposing raw entity payloads, private names, addresses, bank identifiers, document content, provider payloads, journal lines, payroll internals, object-storage contents, prompt text, or completion text.
- `AiRecommendation`, `AiActionDraft`, and `AiActionApproval` provide provider-neutral review records and human approval evidence.
- WebAdmin has an internal AI Governance workspace for recommendation review and action-draft approval. That workspace does not execute drafts or call a model provider.
- Existing Application command handlers remain the only owners for operational mutations across orders, invoices, payments, refunds, supplier finance, payroll, bank settlement, inventory, HR, journal posting, exports, and archives.
- No canonical AI provider adapter, prompt contract, model credential owner, provider retry policy, or provider smoke strategy exists yet.

## Decision Matrix

| Provider surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Security/privacy impact | Business impact | Technical impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Provider selection | No model provider is selected. | Keep v1 provider-neutral until a real target/model is selected with operational approval. | AI provider adapter boundary. | None in this design step. | Avoids vendor-specific credentials and payload assumptions. | Business can choose a provider later without redesigning governance. | Requires an adapter interface before target-specific code. | Ready for foundation. | Implement provider-neutral adapter foundation. |
| Credential owner | No AI credentials exist in domain state. | Credentials belong only in secure configuration or a secret store. No WebAdmin credential UI in v1. | Deployment/security configuration. | Deployment owner only. | Prevents API keys and private credentials entering metadata, logs, or audit. | Operators cannot accidentally expose provider secrets. | Adapter readiness must report configured/not configured without displaying secret values. | Ready for foundation. | Add readiness contract without secret inspection. |
| Prompt contract | Scoped projections exist; prompt execution does not. | Prompt input must be built only from purpose-bound scoped projections and explicit safe instructions. | AI provider orchestration service. | Read handlers only. | Keeps raw HR, payroll, bank, document, archive, provider, and credential data out of prompts. | Recommendations remain useful without broad database access. | Requires deterministic prompt envelope and source-scope metadata. | Ready for foundation. | Define prompt request DTOs over scoped context. |
| Context source | `AiScopedContextProjectionService` returns aggregate metrics. | Provider adapters may not query domain entities directly. They receive prebuilt safe context only. | AI scoped context service. | Projection handlers only. | Prevents bypassing deny-by-default classification. | Business owners can audit what AI was allowed to inspect. | Adapter request shape must exclude raw entities and object contents. | Ready. | Enforce source guards. |
| Recommendation output | `AiRecommendation` stores reviewed suggestions. | Provider output can create structured recommendations only after validation. | AI governance service. | AI recommendation handlers. | Recommendation text must be safe and reportable. | Operators get reviewable suggestions, not chat-only advice. | Requires validation of reason, confidence, scope, expiration, and safe metadata. | Ready for foundation. | Route provider result through governance service. |
| Action draft output | `AiActionDraft` stores proposed commands. | Provider output can create action drafts only as non-executing intent. | AI governance service. | Action draft handlers; later command owners after approval. | Prevents autonomous stock, payment, payroll, bank, journal, or invoice mutations. | Users can review proposed actions before business state changes. | Requires command-name allow-list and payload safety validation. | Ready for foundation. | Keep execution handoff as separate later design. |
| Human approval | WebAdmin review workspace exists. | Human approval remains mandatory before any future action handoff. | WebAdmin AI Governance plus command owners. | Existing module command handlers only. | Creates accountability for AI-assisted operations. | Business can use AI assistance without giving AI direct authority. | Provider foundation must not execute drafts. | Ready. | Preserve review-only workflow. |
| Logging and retention | Business events and audit trails exist. | Do not persist raw prompts or raw completions by default. Store only safe summaries, ids, status, model/provider code, token/cost aggregates if available, and validation outcomes. | AI provider orchestration service. | AI provider foundation handlers. | Reduces privacy and compliance exposure. | Operators can troubleshoot status without reading sensitive prompt data. | Debugging relies on safe summaries and deterministic context ids. | Ready for foundation. | Add safe execution summary contract. |
| Retry and error policy | No provider retry model exists. | Provider failures must return safe error summaries and must not create fake recommendations, approvals, or success events. | AI provider orchestration service. | Provider orchestration only. | Prevents raw provider payload and secret leakage in errors. | Users see reliable failure states instead of silent or fake success. | Requires retry classification, timeout, and idempotency policy. | Ready for foundation. | Add provider result and failure DTOs. |
| Rate limit and cost | No model-provider cost tracking exists. | Foundation may track safe aggregate token/cost estimates when provider supplies them, but not raw prompts or completions. | AI provider orchestration service. | Provider orchestration only. | Avoids storing sensitive data while preserving operational cost visibility. | Business can understand AI usage cost. | Requires nullable aggregate metrics and no provider-specific billing payloads. | Ready for foundation. | Keep optional aggregate metrics in result contract. |
| Evaluation and smoke | No AI provider smoke exists. | Foundation smoke must use a no-network deterministic adapter. Production must not register fake success adapters by default. | Tests and composition. | Test composition only. | Prevents fake production AI output. | Business does not mistake a test adapter for real AI. | Requires source guards and DI readiness tests. | Ready for foundation. | Implement test-scoped adapter only. |
| WebAdmin visibility | AI Governance workspace exists. | WebAdmin may show provider readiness and safe run status later, but no credential entry, raw prompt/completion viewer, or direct execution UI in v1. | WebAdmin AI Governance. | Review actions only. | Avoids exposing secrets or sensitive prompt data to operators. | Operators see whether AI generation is available without managing credentials in-app. | Requires localized compact readiness messages. | Later. | Add UI only after service foundation is verified. |
| Public/mobile boundary | Public/member/mobile contracts are stable. | No public WebApi, mobile/member, or storefront AI provider route in v1. | WebApi/Mobile owners. | None. | Prevents consumer-facing AI leakage. | AI starts as internal ERP assistance. | Preserves compatibility tests. | Locked. | Revisit only after internal governance is proven. |
| Provider payload security | External references, documents, events, and metadata exist. | Raw provider request/response payloads, credentials, tokens, private keys, and provider debug dumps must not be stored in metadata, `DocumentRecord`, `ExternalReference`, `BusinessEvent`, `AuditTrail`, logs, tests, or docs. | AI provider foundation plus security policy. | Provider orchestration only. | Prevents secret and data leakage. | Supports compliance review and customer trust. | Requires source guards and metadata validators. | Ready. | Enforce in tests and guards. |

## Locked Decisions

- The next implementation step is provider-neutral foundation, not a real model target.
- AI credentials stay in secure configuration or a secret store. No WebAdmin credential UI is added in v1.
- Provider adapters consume scoped context produced by Darwin services; they do not query raw domain entities, object storage, invoice archives, personnel documents, payslip HTML, bank identifiers, provider payloads, or export package contents.
- Raw prompts and raw completions are not persisted by default. Safe summaries, ids, status, aggregate token/cost metrics, validation outcomes, and provider/model codes may be stored when secret-free.
- Provider output can create `AiRecommendation` and `AiActionDraft` records only through AI governance services.
- AI action drafts are not executed in this boundary. Execution handoff remains a later dedicated design and implementation step.
- Provider failures, timeouts, rate limits, storage failures, validation failures, and unsafe responses must not produce fake recommendations, fake approvals, fake action drafts, or success events.
- A no-network deterministic adapter is allowed only in test composition. Production must not register a fake-success adapter by default.
- Public WebApi, mobile/member, storefront, finance export package format, invoice archive/download, customer payment/refund, supplier finance, payroll provider submission, bank API, and journal editor flows remain unchanged.

## Implementation Sequence

1. `AI Provider Adapter Boundary Design`
   - Outcome: this document. Provider selection, credential ownership, prompt contract, scoped context source, safe logging, retry/error policy, smoke strategy, WebAdmin limits, and compatibility boundaries are locked.
2. `AI Provider Adapter Foundation Slice`
   - Outcome: complete for current phase. A provider-neutral internal interface and orchestration service exist over `AiScopedContextProjectionService` and AI governance services.
   - The service can call a ready adapter, validate safe provider output, create governed recommendations and optional non-executing action drafts, and fail safely when no adapter is configured or provider output is unsafe.
   - Tests use a deterministic in-test adapter only. Production composition does not register a fake provider adapter.
   - No real provider credentials, real network calls, WebAdmin credential UI, public/mobile routes, raw prompt/completion persistence, or autonomous execution are added.
3. `AI Target Provider Selection And Adapter Slice`
   - Outcome: design-complete for current phase. Target selection stays deployment-approved and configurable; no real provider is activated by default.
   - A real target adapter starts only after provider/model target, credential owner, payload mapping, cost/rate policy, safe error contract, and smoke strategy are selected.
4. `AI Action Handoff Execution Boundary Design`
   - Start only after recommendation and draft generation are verified. This design must map approved drafts to owning module command handlers without creating a parallel mutation path.

## Documentation Outcome

- `BACKLOG.md` and `erp-expansion-master-status.md` move the active next gate to `AI Action Handoff Execution Boundary Design`.
- `ai-readiness-automation-governance-design.md` records this provider boundary outcome.
- `DarwinWebAdmin.md` records that AI provider readiness may be surfaced later, but credential entry, raw prompt/completion viewers, and autonomous execution remain blocked.
- `docs/README.md` links this document from the ERP expansion map.

## Compatibility Guards

- Do not add AI provider calls to WebAdmin controllers, public WebApi, mobile/member routes, background workers, or module command handlers outside the provider foundation.
- Do not store provider credentials, access tokens, refresh tokens, private keys, connection strings, raw provider payloads, raw prompts, or raw completions in database metadata, documents, external references, business events, audit trails, logs, tests, or documentation.
- Do not let provider output bypass `AiGovernanceService` validation.
- Do not execute action drafts, mutate operational state, post journals, change stock, settle payments, submit payroll, generate invoice archives, or alter finance exports in the provider adapter foundation.
- Do not register no-network or fake-success adapters in production composition.

## Documentation Verification

- `docs/README.md` links this document.
- This document contains no ambiguous provider-boundary decisions.
- Restricted vendor/source scans must return no output.
- Ambiguity scans over touched AI docs must return no output.
