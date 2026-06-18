# AI And Integration Decision Checkpoint

## Summary

This checkpoint records the current product decisions after the internal AI governance, handoff, follow-up task, module review-routing work, and SyncState/SyncConflict foundation decision. It adds no route, DTO, WebAdmin mutation, public/mobile/storefront contract, model-provider adapter, prompt execution, operational module executor, accounting API adapter, finance export format change, payment/refund flow, payroll provider submission, bank API, journal editor, stock movement, shipment mutation, or invoice archive/download behavior.

The selected direction is conservative: Darwin keeps the completed internal AI execution surfaces limited to timeline evidence, internal follow-up tasks, and module review tasks. Real provider calls, direct operational command execution, target-specific two-way sync adapters, and accounting API delivery remain blocked until their real-world targets and policies are selected.

## Current Darwin Findings

- AI governance, scoped context projections, provider-neutral adapter foundation, action handoff, timeline execution, internal follow-up task execution, and module review task routing exist.
- Production composition does not register a real model provider or fake-success provider.
- Approved AI drafts can create internal evidence or review work only. They do not mutate operational module state.
- Finance export file-delivery is the production-safe outbound accounting path when configured.
- `ExternalReference` and external-system foundations exist.
- `SyncState` and `SyncConflict` now provide provider-neutral state and conflict evidence foundations, but no target-specific inbound adapter or automatic merge engine is implemented.
- Public WebApi, mobile/member, storefront, finance export format, payment/refund, supplier finance, payroll provider submission, bank API, journal editor, stock movement, shipment, and invoice archive/download behavior remain unchanged.

## Locked Decisions

| Decision surface | Selected option | Business impact | Technical impact | Required future trigger |
| --- | --- | --- | --- | --- |
| Real AI provider target | Do not activate a real provider now. | Avoids cost, privacy, data-residency, and output-quality risk before a deployment target is selected. | Keeps production provider-free and avoids credentials, raw prompts, raw completions, live smoke, rate-limit, and provider-specific error code paths. | A real provider/model target, credential owner, payload mapping, rate/cost policy, retry policy, safe error contract, and smoke strategy. |
| Direct operational AI executor | Do not add direct module mutations now. Keep AI execution limited to internal evidence and review tasks. | Operators get AI-assisted work routing without AI changing stock, money, payroll, bank, shipment, archive, or customer-facing state. | Avoids premature permission, row-version, idempotency, rollback, reversal, and audit complexity for high-impact operations. | A concrete command family with owner, permissions, typed payload, business consequence, idempotency, failure behavior, and reversal/correction policy where applicable. |
| SyncState and SyncConflict | Implement provider-neutral state/conflict foundation now; keep target-specific adapters blocked. | Future inbound integrations get durable state and conflict evidence without tying Darwin to one external product. | Adds internal Integration schema records and service-level guards while avoiding automatic merge, provider calls, and operational mutations. | A concrete inbound or two-way integration target with identity matching, retry semantics, conflict cases, and operator resolution UX. |
| Accounting API adapter | Do not implement an accounting API adapter now. Keep file-delivery as the production-safe outbound path. | Customers can still receive audit-ready exports without committing Darwin to a target accounting API. | Avoids credential UI, provider-specific payload mapping, push retries, remote conflict handling, and target-side error contracts before a target is selected. | A real accounting target, credential owner, payload mapping, retry policy, safe error contract, and smoke strategy. |
| Future completion of deferred areas | Keep all four areas documented as future completion gates, not abandoned work. | Non-technical owners can see that these are deliberate sequencing decisions, not missing features. | Preserves architecture discipline and prevents half-built provider/sync/adapter surfaces. | Revisit when target, operational need, and owner are available. |

## Implementation Consequence

- SyncState/SyncConflict foundation implementation follows from the updated decision and is complete for provider-neutral state/conflict evidence.
- The current AI implementation remains complete for the no-provider phase: internal timeline evidence, internal follow-up tasks, and internal module review tasks.
- Future work must not treat this checkpoint as permission to add broad AI automation, provider calls, target-specific sync adapters, automatic conflict merge, or accounting API delivery.

## Next Valid Gates

1. `AI Target Provider Adapter Slice`
   - Only after provider/model target, credential owner, payload mapping, rate/cost policy, retry policy, safe error contract, and smoke strategy are selected.
2. `Operational Module Executor Design`
   - Only after one concrete command family is selected.
3. `Target-Specific Sync Adapter Design`
   - Only after a concrete inbound or two-way integration target exists.
   - Uses the implemented `SyncState`/`SyncConflict` foundation.
4. `Accounting API Target Adapter Design`
   - Only after a real accounting API target is selected.

## Documentation Verification

- `BACKLOG.md` and `erp-expansion-master-status.md` record these gates as conditional, not immediate no-decision implementation steps.
- `ai-target-provider-selection-design.md`, `ai-broader-module-executor-decision.md`, and finance export docs remain aligned with this checkpoint.
- Restricted vendor/source scans must return no output.
- Ambiguity scans over touched docs must return no output.
