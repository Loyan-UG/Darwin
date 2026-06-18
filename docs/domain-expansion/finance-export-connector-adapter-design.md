# Finance Export Connector Adapter Design

## Summary

This document locks the connector adapter boundary for finance export before any provider-specific delivery code, public route, mobile/member/storefront contract, or operational finance flow is implemented.

The decision is explicit: connector delivery is an adapter over the existing finance export batch, durable package, and retry evidence model. It must not rebuild export packages, export mutable operational documents directly, bypass batch idempotency, or store secrets and raw provider payloads in Darwin records.

## Current Darwin Finance Export Connector Findings

- `FinanceExportBatch` and `FinanceExportAttempt` hold batch identity, lifecycle status, generated package metadata, and safe retry evidence.
- The canonical finance export package is JSON generated from posted `JournalEntry` and `JournalEntryLine` records.
- Generated packages are stored durably in object storage before a batch is completed as generated.
- `DocumentRecord` metadata links stored package evidence to `FinanceExportBatch`.
- `ExternalSystemKind.Accounting` is the valid target kind for finance export targets.
- `ExternalReference` exists for target-side batch, package, delivery, document, or upload receipt identifiers.
- `SyncState` and `SyncConflict` foundation exists for future target-specific inbound or two-way integrations; v1 connector delivery is outbound-only.
- WebAdmin supports generate, download, and an internal push surface. Push remains blocked unless a real `IFinanceExportConnectorAdapter` is registered for the selected accounting target.

## Connector Adapter Decision Matrix

| Connector surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | External identity | Retry/error policy | Security boundary | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Package source | Stored package is linked by `DocumentRecord` and object storage. | Connector reads the stored package only. It must not rebuild or mutate generated packages. | `FinanceExportBatch`, `DocumentRecord`, object storage. | Future connector adapter service. | Package document id and batch id. | Missing package or object fails delivery. | No raw operational payloads are read outside the canonical package. | Unchanged. | Ready for adapter foundation. | Build package reader guard into adapter service. |
| Target eligibility | `ExternalSystemKind.Accounting` exists. | Only active accounting external systems are valid connector targets. | `ExternalSystem`. | Future connector adapter service. | Target external system id. | Inactive or non-accounting targets fail before delivery. | Credentials are not read from batch or document metadata. | Unchanged. | Ready. | Validate target before attempt start. |
| Delivery attempt | Export attempts already hold retry evidence. | Connector delivery must create safe attempt evidence and cannot report success unless target delivery succeeds. | `FinanceExportAttempt`. | Future connector adapter service. | Attempt id plus target receipt reference. | Network, target, storage, and validation failures record safe failure summaries. | Failure details are summarized and secret-free. | Unchanged. | Requires adapter implementation. | Add delivery orchestration over existing attempts. |
| Target-side ids | `ExternalReference` exists. | Remote batch, upload receipt, package, and document ids are stored through `ExternalReference`, not provider-specific columns. | `ExternalReference`. | Future connector adapter service. | Reference kind identifies target id purpose. | Duplicate target ids are idempotent updates when the same batch is retried. | External reference metadata remains secret-free. | Unchanged. | Ready. | Define reference kinds in adapter code without schema changes unless enum expansion is required. |
| Delivery payload | Canonical JSON package exists. | Adapter maps the stored package to the target payload. The canonical package remains the source; target-specific CSV or JSON is adapter output, not the source package. | Stored package. | Future connector adapter service. | Target upload id or receipt id. | Mapping failure fails attempt before delivery. | Target payload must exclude credentials and raw archives. | Unchanged. | Needs target policy. | Keep first adapter provider-neutral or mock/file-delivery. |
| Connector credentials | External system metadata exists but is not a secret store. | Credentials must come from secure provider configuration, not from export batches, attempts, references, documents, or package content. | Secure configuration/provider secret infrastructure. | Future connector adapter service. | None in export records. | Missing credentials fail readiness. | Secrets, access tokens, refresh tokens, private keys, and credentials are forbidden in export metadata. | Unchanged. | Needs connector-specific policy. | Design credential owner before real target adapter. |
| Delivery status | Batch statuses exist for generated and delivered. | Delivered status is set only after target success and target evidence is stored. Generated remains downloadable but not delivered. | `FinanceExportBatch`. | Future connector adapter service. | External target receipt reference. | Failed delivery keeps batch retryable without fake success. | Safe status only; no provider payload dump. | Unchanged. | Requires implementation. | Add delivered transition tests with failure cases. |
| Conflict handling | `SyncConflict` foundation exists. | V1 file-delivery is outbound-only. Target-side rejection is a failed delivery attempt, not a sync conflict. | Sync/conflict foundation plus future target adapter. | Future sync service, not v1 file-delivery. | No conflict ids in v1 file-delivery. | Rejection stores safe failure summary. | No inbound target records are trusted as source of truth. | Unchanged. | Foundation ready. | Do not implement two-way reconciliation in connector v1. |
| WebAdmin push UI | Finance Exports has generate/download and internal push readiness. | Push UI calls only the connector delivery service. It is disabled when no adapter is available and never registers fake production delivery. | Finance WebAdmin. | Finance controller push action over application delivery service. | Batch and target receipt display only. | UI must not allow regenerate-on-push. | No secrets, credentials, or raw target responses displayed. | Unchanged. | Implemented for provider-neutral push surface. | Design a real target adapter and credential policy before enabling live delivery. |
| Public/mobile boundary | No public/mobile finance export exists. | Connector delivery remains internal/operator-only. | WebAdmin/internal services. | Finance WebAdmin or background worker. | None for member contracts. | Public/mobile failures are not applicable. | No customer-facing accounting export route. | Unchanged. | Decision locked. | Keep compatibility smoke unchanged. |

## Locked Connector Decisions

- Connector adapter reads the stored package through `DocumentRecord` and object storage; it never regenerates the package.
- Connector delivery uses the existing batch and attempt evidence model. Provider, storage, validation, and network failures fail the attempt and never create fake success.
- Target-side ids such as remote batch id, remote document id, upload id, or receipt id use `ExternalReference`.
- Provider-specific columns are not added to `JournalEntry`, `Invoice`, `CreditNote`, `Payment`, `Refund`, sales documents, or export batches for target ids.
- Secrets, access tokens, refresh tokens, private keys, raw target payloads, credentials, connection strings, and sensitive provider responses are forbidden in batch metadata, attempt metadata, external reference metadata, document metadata, and package content.
- V1 connector delivery is outbound-only. `SyncState`/`SyncConflict` foundation is available, but inbound sync, target reconciliation, and conflict resolution still require a concrete target-specific adapter design.
- The adapter interface must be provider-neutral. A real accounting API target adapter starts only after target credential ownership, payload mapping, delivery status semantics, sync/conflict behavior, and smoke strategy are locked. Future target selection should follow [finance-export-accounting-api-target-selection-design.md](finance-export-accounting-api-target-selection-design.md) and prioritize several widely used German accounting products.
- WebAdmin push action is internal/operator-only and uses the provider-neutral delivery service. It stays disabled unless a connector adapter is available for the selected accounting target.
- Public WebApi, mobile/member, storefront, invoice archive/download, payment/refund, credit-note, and journal editor flows remain unchanged.

## Finance Export Target Adapter Smoke/Operations Hardening Outcome

`Finance Export Target Adapter Smoke/Operations Hardening`

Implemented scope:

- Added operational smoke coverage for copying a stored package from `FinanceExports` to `FinanceExportOutbound`.
- Verified production readiness behavior: valid non-database outbound profile enables the real file-delivery adapter; missing or database-backed outbound profile keeps push blocked.
- Verified failed outbound delivery does not mark a generated batch delivered.
- Verified test/no-network adapter implementations are not registered by production source or WebAdmin composition.
- Updated deployment runbooks with non-secret profile examples and profile ownership guidance.

Next gate:

- Design an accounting API target adapter only after a real target, credential owner, payload mapping, error contract, sync/conflict behavior, and smoke strategy are known. Future target selection should follow [finance-export-accounting-api-target-selection-design.md](finance-export-accounting-api-target-selection-design.md) and prioritize widely used German accounting software.
- Until then, file-delivery remains the provider-neutral production-safe outbound path.

## Finance Export Connector Adapter Foundation Outcome

`Finance Export Connector Adapter Foundation Slice`

Implemented scope:

- Added a provider-neutral `IFinanceExportConnectorAdapter` contract for delivery of stored finance export packages.
- Added `FinanceExportConnectorDeliveryService` to validate generated batches, active accounting targets, stored package availability, adapter selection, delivery result safety, and target reference recording.
- Added centralized delivery completion through the finance export batch service so batches move to `Delivered` only after a successful adapter response and safe metadata validation.
- Delivery failures create failed attempt evidence without turning a valid generated package batch into a fake delivered batch.
- Target-side delivery ids are stored with `ExternalReferenceKind.Export` for `EntityType = FinanceExportBatch`.
- Tests use a no-network adapter to prove success, failure, missing package, invalid target, retry, and sensitive metadata behavior.
- No real connector, credential policy, WebAdmin push UI, public WebApi route, mobile/member route, storefront contract, schema/migration, journal editor, invoice/payment/refund flow, or credit-note flow was introduced.

## Finance Export WebAdmin Connector Push Outcome

`Finance Export WebAdmin Connector Push Slice`

Implemented scope:

- Added WebAdmin push readiness for selected accounting targets based on registered `IFinanceExportConnectorAdapter` instances.
- Added an internal push command/action over `FinanceExportConnectorDeliveryService`; it never rebuilds packages and never bypasses stored package evidence.
- Finance export batches can be pushed only when they are generated, have a stored package document, and have an adapter available for the selected target.
- Delivered timestamp and target delivery reference are displayed from batch state and `ExternalReference`.
- Production composition does not register a no-network adapter, so push remains blocked until a real adapter is deliberately registered.
- Tests use no-network adapter behavior only in test scope to verify orchestration without creating fake production delivery.
- No real connector, credential form, connector configuration UI, public WebApi route, mobile/member route, storefront contract, schema/migration, journal editor, invoice/payment/refund flow, credit-note flow, or package regeneration path was introduced.

## Finance Export File-Delivery Target Adapter Design Outcome

`Finance Export File-Delivery Target Adapter Design Slice`

Decision scope:

- Selected `FinanceExportFileDeliveryAdapter` as the first real connector target.
- File-delivery copies only the stored canonical package stream to a configured outbound object-storage or file-system destination.
- The preferred outbound profile is `FinanceExportOutbound`.
- Destination configuration is owned by secure deployment configuration, not by batch, attempt, package, document, or reference metadata.
- Success requires destination write completion and SHA-256 hash verification against the package hash.
- Existing destination object with the same hash is idempotent success; same key with different hash is failure.
- Target receipt identity is stored through `ExternalReferenceKind.Export`.
- Production push remains blocked until a real file-delivery adapter is registered with valid destination configuration.
- No API accounting adapter, connector credential form, target browsing UI, package regeneration, schema/migration, public/mobile/storefront route, journal editor, invoice/payment/refund mutation, or credit-note mutation is part of this design.

See [finance-export-file-delivery-target-adapter-design.md](finance-export-file-delivery-target-adapter-design.md).

## Finance Export File-Delivery Adapter Outcome

`Finance Export File Delivery Adapter Slice`

Implemented scope:

- Added `FinanceExportFileDeliveryAdapter` as the first real `IFinanceExportConnectorAdapter`.
- The adapter writes only the stored canonical package stream to the configured `FinanceExportOutbound` destination.
- The outbound object key is deterministic by business, accounting target, period, and batch id.
- Delivery success requires object write completion and SHA-256 hash verification.
- Existing same-key/same-hash destination objects are idempotent success; same-key/different-hash objects fail delivery.
- Production registration is conditional on a configured non-database outbound object-storage profile.
- No no-network adapter is registered by production composition.
- No schema/migration, package regeneration, connector credential form, target browsing UI, public/mobile/storefront route, journal editor, invoice/payment/refund mutation, or credit-note mutation was added.
