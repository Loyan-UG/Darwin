# AI Target Provider Selection Design

## Summary

This step locks the target-provider selection boundary for Darwin AI. It adds no entity, migration, route, DTO, WebAdmin mutation, provider credential, provider network call, public/mobile/storefront contract, autonomous execution, finance export format change, customer/supplier payment flow, payroll provider submission, bank API, journal editor, or invoice archive/download behavior.

The selected decision is to keep the provider target configurable and deployment-owned until a real provider/model is explicitly selected. Darwin can now host a provider-neutral adapter foundation, but no production AI target is activated by default.

## Current Darwin AI Target Provider Findings

- `IAiProviderAdapter` and `AiProviderAdapterFoundationService` exist in Application as an internal provider-neutral foundation.
- The provider foundation consumes `AiScopedContextProjectionService` output only and creates governed `AiRecommendation` and optional non-executing `AiActionDraft` records.
- The provider foundation fails safely when no ready adapter is configured.
- Production composition does not register a no-network or fake-success adapter.
- WebAdmin AI Governance is review-only and does not expose credential entry, prompt viewers, completion viewers, provider payloads, or execution bypass actions.
- No real AI provider/model target, credential owner implementation, payload mapping, rate/cost policy, provider smoke strategy, or provider-specific error contract exists yet.

## Decision Matrix

| Target-provider surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Credential impact | Business impact | Technical impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Target selection | Provider foundation exists without real target. | Do not select a real provider in this step. Keep target selection deployment-approved and configurable. | AI target provider design. | None. | No credential is introduced. | Business can choose provider later based on privacy, cost, data residency, and customer requirements. | Avoids premature vendor coupling. | Design-complete. | Pick real provider only through a target adapter slice. |
| Provider configuration | No production adapter is registered by default. | Future real adapter reads provider code/model/deployment settings from secure configuration. | Deployment/security configuration. | Deployment owner. | Settings must not include secrets in docs or metadata. | Operators get predictable per-deployment setup without in-app credential handling. | Composition can enable an adapter only when configuration is valid. | Ready for later adapter. | Add adapter-specific readiness when target exists. |
| Credential ownership | No AI credential storage exists in Darwin domain state. | Credentials stay in secure configuration or secret store; no WebAdmin credential UI. | Operations/security owner. | Deployment owner. | API keys, tokens, private keys, and endpoints with embedded secrets stay out of DB. | Reduces breach risk and operational mistakes. | Requires deployment runbook rather than operator UI. | Locked. | Document target-specific secret keys in target slice with non-secret examples only. |
| Prompt contract | Provider foundation accepts safe request summary and scoped context. | Real adapters must keep the same contract: scoped context plus safe instruction, never raw entity payloads. | AI provider foundation. | Read/projection handlers only. | Prevents prompt construction from private documents or raw operational data. | Business gets controlled AI assistance. | Target adapter maps the safe request to provider-specific API shape. | Ready. | Preserve contract in real adapter tests. |
| Raw prompt/completion retention | No raw prompt/completion storage is required. | Do not persist raw prompts or raw completions in v1. | AI provider orchestration. | Provider foundation only. | Lower privacy and compliance exposure. | Business can enable AI with less sensitive-data risk. | Debugging uses safe summaries, ids, status, and aggregate usage. | Locked. | Revisit only with retention/redaction design. |
| Rate and cost policy | Provider foundation supports nullable aggregate usage fields. | Future real adapter may return safe aggregate usage; no provider billing payload is stored. | AI provider adapter. | Provider foundation only. | Avoids storing provider account or billing payloads. | Business can monitor rough usage while avoiding sensitive provider data. | Requires adapter-specific unit tests for aggregate fields. | Ready for target adapter. | Add per-provider usage mapping later. |
| Error contract | Provider foundation returns safe errors. | Real adapter must sanitize timeout, rate limit, validation, and provider failures. | AI provider adapter. | Provider foundation only. | Raw provider response and secrets never enter errors. | Operators see actionable failure states without sensitive dumps. | Requires provider-specific error classification. | Ready for target adapter. | Add safe error tests in target slice. |
| Smoke strategy | Tests use deterministic in-test adapter only. | Real provider smoke is opt-in and deployment-scoped. No live smoke runs by default. | Test and deployment operations. | Test composition only. | Prevents accidental external calls or secret output. | Business validates real provider only in controlled environment. | Requires target-specific guarded smoke command. | Later. | Define smoke after target selection. |
| WebAdmin impact | AI Governance review workspace exists. | WebAdmin does not add credential entry, prompt viewer, completion viewer, provider payload viewer, or generation button until a real target adapter is implemented and verified. | WebAdmin AI Governance. | Review actions only. | Prevents exposing secrets or sensitive prompt data. | Operators continue using review queues safely. | UI readiness can be added later as status only. | Blocked until target adapter. | Add compact readiness only after adapter exists. |
| Public/mobile boundary | Public and mobile contracts are compatibility-sensitive. | No public WebApi, mobile/member, or storefront provider route. | WebApi/Mobile owners. | None. | Prevents consumer-facing AI leakage. | AI remains internal ERP assistance. | Compatibility lanes remain stable. | Locked. | Revisit only after internal provider is proven. |

## Locked Decisions

- No real provider/model target is selected in this step.
- Future target adapters must be configurable per deployment and must not require WebAdmin credential entry in v1.
- Credentials, tokens, private keys, connection strings, and raw provider payloads stay out of domain metadata, documents, external references, business events, audit trails, logs, tests, and documentation.
- Raw prompts and raw completions are not persisted by default.
- Real adapters must consume the provider-neutral foundation contract and may not query domain entities directly.
- Real adapters may create governed recommendations and optional non-executing action drafts only through the provider foundation and AI governance service.
- A provider target implementation starts only after provider/model target, credential owner, payload mapping, rate/cost policy, safe error contract, and smoke strategy are selected.
- WebAdmin credential UI, raw prompt/completion viewers, public/mobile routes, autonomous execution, and action handoff execution remain outside this step.

## Implementation Sequence

1. `AI Target Provider Selection Design`
   - Outcome: this document. Target selection stays deployment-approved and configurable; no real provider is activated by default.
2. `AI Action Handoff Execution Boundary Design`
   - Define how approved action drafts can later be handed to owning Application command handlers without creating a parallel mutation path.
3. `AI Target Provider Adapter Slice`
   - Start only when a real provider/model target is selected with credential owner, payload mapping, safe error contract, rate/cost policy, and smoke strategy.
4. `AI Action Handoff Foundation Slice`
   - Start only after handoff boundaries are locked; initial implementation must not register module execution shortcuts without explicit command-owner mapping.

## Documentation Outcome

- `BACKLOG.md` and `erp-expansion-master-status.md` record that no real AI provider target is selected for the current phase.
- `ai-provider-adapter-boundary-design.md` records that target selection remains configurable and blocked until a real target is chosen.
- `DarwinWebAdmin.md` keeps WebAdmin AI surfaces review-only.
- `docs/README.md` links this document.

## Current Checkpoint

- The follow-up target decision remains option A: no real provider/model target is activated now.
- Provider implementation is a future completion gate, not abandoned work.
- A real target adapter starts only after provider/model target, credential owner, payload mapping, rate/cost policy, retry policy, safe error contract, and smoke strategy are selected.
- Until then, production composition must remain free of fake-success provider adapters and real provider credentials.

## Compatibility Guards

- Do not register a real provider adapter until a target-specific slice exists.
- Do not add provider credentials, provider endpoint secrets, raw prompts, raw completions, raw provider responses, or provider billing payloads to database state or docs.
- Do not add WebAdmin credential forms, generation buttons, prompt viewers, completion viewers, or provider payload viewers in this step.
- Do not add public/mobile/storefront routes or change finance export, payment/refund, payroll provider, bank API, journal editor, invoice archive, or invoice download behavior.

## Documentation Verification

- `docs/README.md` links this document.
- The document contains no ambiguous target-provider decisions for this phase.
- Restricted vendor/source scans must return no output.
- Ambiguity scans over touched AI docs must return no output.
