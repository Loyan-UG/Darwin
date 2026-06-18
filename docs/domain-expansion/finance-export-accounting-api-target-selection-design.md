# Finance Export Accounting API Target Selection Design

## Summary

This document locks the target-selection boundary for future accounting API adapters. It adds no entity, migration, route, DTO, WebAdmin mutation, connector implementation, credential UI, public/mobile/storefront contract, finance export format change, payment/refund flow, invoice archive/download behavior, bank API, payroll provider submission, or AI provider behavior.

The selected current path remains file-delivery. Future API adapters must be selected deliberately, with priority for widely used German accounting products, but no product-specific adapter starts until the target, credential owner, payload mapping, sync/conflict behavior, and smoke strategy are approved.

## Current Darwin Accounting Export Findings

- Finance export batch, package generation, durable storage, WebAdmin generate/download, connector delivery foundation, WebAdmin push surface, and file-delivery adapter exist.
- The canonical export package is provider-neutral JSON generated from posted `JournalEntry` and `JournalEntryLine` records.
- `FinanceExportOutbound` is the production-safe delivery path when configured with a valid non-database object-storage provider.
- `ExternalSystem`, `ExternalReference`, `SyncState`, and `SyncConflict` foundation exist for future target-specific integrations.
- WebAdmin push remains blocked unless a real adapter is registered for the selected accounting target.
- No accounting API target, credential owner, provider payload mapping, inbound sync adapter, automatic merge flow, or conflict-resolution UI is selected.

## Target Selection Matrix

| Selection surface | Current Darwin model | Decision | Business impact | Technical impact | Required evidence before adapter implementation |
| --- | --- | --- | --- | --- | --- |
| Target product | No accounting API target is selected. | Choose one concrete accounting product per adapter slice. Prioritize widely used German accounting products when business demand exists. | Business owners can evaluate market reach and customer demand before committing implementation effort. | Avoids broad provider assumptions and keeps adapter code testable. | Named target, customer/use-case fit, data exchange scope, and owner approval. |
| Export scope | Posted-entry package exists. | API adapters consume stored packages or mapped posted-entry projections; they do not rebuild from mutable operational documents. | Accounting exports stay audit-ready and reproducible. | Prevents duplicate calculators and source drift. | Mapping from canonical package fields to target payload fields. |
| Direction | File-delivery is outbound-only. | First API adapter for a target may be outbound-only unless inbound reconciliation is explicitly selected. | Customers get delivery automation without surprise two-way data changes. | Keeps sync/conflict behavior bounded. | Direction decision: outbound-only, inbound status sync, or two-way reconciliation. |
| Credentials | No credential UI exists. | Credentials stay in secure deployment configuration or vault-managed infrastructure, not WebAdmin forms or metadata. | Reduces operational and compliance risk. | Adapter readiness can fail closed without storing secrets. | Credential owner, storage mechanism, rotation owner, and smoke environment. |
| Target-side identity | `ExternalReference` exists. | Remote batch, document, upload, receipt, or posting ids use `ExternalReference`. | Operators can trace delivery without provider-specific columns. | Keeps finance/sales entities provider-neutral. | External reference kind usage and idempotency rules. |
| Sync state | `SyncState` exists. | Use `SyncState` only for selected target workflows that need checkpointing. File-delivery and simple push do not fake sync state. | Prevents false claims of two-way integration. | Checkpoints remain provider-neutral and explicit. | Scope key, direction, cursor/checkpoint rules, and retry semantics. |
| Conflict handling | `SyncConflict` exists. | Conflicts are recorded only when target inbound data or target-side state can contradict Darwin state. Delivery rejection remains a failed attempt, not a conflict. | Business users see conflicts only when there is a real data-resolution task. | Avoids noisy conflict queues for simple delivery failures. | Conflict cases, owner workflow, resolution options, and audit rules. |
| Error contract | Safe attempt summaries exist. | Target errors are classified into safe, operator-readable categories; raw provider payloads are not stored. | Operators know what to fix without exposing sensitive provider data. | Enables retry and support without secret leakage. | Error taxonomy, retry policy, blocked state, and alert ownership. |
| Smoke strategy | File-delivery smoke exists. | Each API target needs deterministic no-network tests plus opt-in live smoke with approved credentials. | Business owners can prove integration before launch. | Prevents accidental external calls in CI. | Test adapter, live smoke command, expected blocked behavior, and safe output rules. |
| WebAdmin impact | Push UI exists. | WebAdmin shows target readiness, push/delivery status, safe errors, and external references only after adapter service exists. | Operators get clear status without credential exposure. | No direct target browsing or raw payload display. | Readiness DTO, source guards, anti-forgery, authorization, and no credential form. |
| Public/mobile impact | No public/mobile export route exists. | Accounting API adapters remain internal/operator-only. | Customers do not see accounting integration internals. | Preserves public/mobile contracts. | Compatibility smoke confirming no public/mobile route changes. |

## Locked Decisions

- File-delivery remains the production-safe accounting export path until a real API target is selected.
- Future target selection should prioritize widely used German accounting products, but target names, credentials, payloads, and live smoke details are not committed until an implementation slice is approved.
- Each adapter starts with one concrete target. A generic multi-provider adapter is not implemented before target-specific payload and error semantics are known.
- API delivery must consume stored finance export package evidence or provider-neutral posted-entry projections. It must not recalculate from mutable invoices, orders, payments, refunds, or live catalog/tax settings.
- Credentials, access tokens, refresh tokens, private keys, connection strings, raw provider payloads, private customer data, and provider response dumps are forbidden in export batches, attempts, external references, sync records, documents, events, logs, tests, and documentation.
- `ExternalReference` owns target-side ids.
- `SyncState` owns target-specific checkpoint evidence only when the selected target workflow needs checkpointing.
- `SyncConflict` owns target-specific data conflicts only when inbound or two-way behavior is selected. Delivery failure is not a conflict.
- WebAdmin must not add connector credential forms, target browsing, raw payload viewers, package regeneration, journal editing, invoice/payment/refund/credit-note mutations, public routes, or mobile routes.

## Implementation Gates

1. Select a real accounting target with business owner approval.
2. Define outbound payload mapping from the canonical finance export package or posted-entry projection.
3. Define credential owner and secure storage policy.
4. Define target-side id handling through `ExternalReference`.
5. Define retry, idempotency, safe error taxonomy, and alert ownership.
6. Decide whether the target is outbound-only or uses `SyncState` and `SyncConflict`.
7. Add no-network adapter tests and opt-in live smoke strategy.
8. Implement a single target adapter slice with WebAdmin readiness and source guards.

## Current Outcome

The target-selection boundary is decision-complete for the no-target phase. Darwin is ready to evaluate concrete German accounting API targets later without redesigning finance export identity, package, retry, reference, or sync/conflict foundations.

Until a target is selected, no accounting API adapter, credential UI, target-specific package mapping, inbound sync, two-way reconciliation, public/mobile route, finance export format change, journal editor, invoice archive/download change, payment/refund mutation, credit-note mutation, bank API, payroll provider submission, or AI provider behavior is added.
