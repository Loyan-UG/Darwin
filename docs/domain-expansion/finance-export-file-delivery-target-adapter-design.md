# Finance Export File-Delivery Target Adapter Design

## Summary

This document locks the first real finance export target adapter before implementation. This step is documentation-only. It does not add entities, migrations, WebAdmin actions, public routes, mobile/member/storefront contracts, connector credentials, operational finance mutations, or production flow changes.

The selected v1 target is a file-delivery adapter. It copies the already generated and stored canonical finance export package to a configured outbound storage destination. It is not an accounting API adapter, does not rebuild packages, and does not read mutable operational documents directly.

## Current Darwin Finance Export Target Adapter Findings

- The canonical finance export package is JSON stored in object storage and linked to `FinanceExportBatch` by `DocumentRecord`.
- WebAdmin has an internal push surface, but production push remains blocked until a real adapter is registered for the selected accounting target.
- `IFinanceExportConnectorAdapter` receives the stored package stream and must not regenerate or mutate the package.
- `FinanceExportConnectorDeliveryService` records attempt evidence, target-side delivery ids, and delivered status only after adapter success.
- `ExternalReferenceKind.Export` is the owner for target delivery receipt identity.
- Production DI does not register a no-network adapter; no test adapter may mark a batch delivered at runtime.

## File-Delivery Target Decision Matrix

| Target surface | Current Darwin model | Decision | Owning source | Configuration owner | Delivery identity | Retry/error policy | Security boundary | WebAdmin impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Adapter identity | Provider-neutral adapter interface exists. | Implement v1 as `FinanceExportFileDeliveryAdapter`. | Application connector contract. | Infrastructure/Web composition registration. | Adapter code `file-delivery`. | Missing adapter keeps push blocked. | No fake runtime success. | Existing push becomes enabled only when adapter is registered. | Ready after this design. | Implement adapter with registration guard. |
| Destination profile | Object storage profiles exist. | Use configured outbound profile `FinanceExportOutbound`. | Object storage infrastructure. | Secure configuration, not batch metadata. | Profile name plus object key. | Missing or invalid profile returns readiness false. | No credentials displayed or stored in export records. | Push disabled when destination is not ready. | Ready. | Add readiness validation in adapter registration/options. |
| Object key and file naming | Stored package already has package file metadata. | Write deterministic safe key: `finance-export-{businessId}-{periodStart:yyyyMMdd}-{periodEnd:yyyyMMdd}-{batchId}.json`. | File-delivery adapter. | Adapter options. | Destination object key. | Unsafe key generation fails before write. | No user-supplied path segments. | Display delivery reference from `ExternalReference`. | Ready. | Build key from batch/request fields only. |
| Package source | Package is read through `DocumentRecord` and object storage before delivery. | Adapter copies only the request stream it receives. It never rebuilds package content. | `FinanceExportConnectorDeliveryService`. | None in adapter. | Source batch id and package hash. | Missing package fails before adapter success. | No mutable operational document reads. | No new UI. | Ready. | Keep package rebuild guard tests. |
| Hash verification | Package hash is included in delivery request. | Success requires destination write hash to match package SHA-256. | File-delivery adapter. | Object storage result and request hash. | Hash recorded in safe delivery summary. | Hash mismatch fails delivery and keeps batch generated. | No raw payload stored. | Push failure message remains safe. | Ready. | Verify after write or from storage write result. |
| Overwrite and idempotency | Batch delivery is retryable while generated. | Existing destination object with same hash is idempotent success; same key with different hash is failure. | File-delivery adapter. | Destination storage. | Destination object key. | Retry cannot overwrite different content. | Prevents accidental replacement. | Operators retry safely. | Ready. | Use exists/read metadata when provider supports it; otherwise fail closed on unsafe overwrite. |
| Delivered reference | `ExternalReference` stores target ids. | Remote id is destination object key or stable delivery receipt; display id is file name or business-period label. | `ExternalReferenceKind.Export`. | Delivery service after adapter success. | Remote id/display id. | Duplicate retry updates existing reference idempotently. | Metadata remains secret-free. | Delivered reference visible in Exports table. | Ready. | Return safe adapter result fields only. |
| Metadata safety | Foundation normalizers reject sensitive data. | Adapter response includes only object key, file name, hash summary, and delivered timestamp. | Adapter result and delivery service validation. | Adapter implementation. | Safe summary. | Sensitive metadata fails delivery. | No credentials, access tokens, connection strings, or provider payload dumps. | No secret display. | Ready. | Add sensitive-response tests. |
| Storage/provider failure | Object storage service returns failure or missing capabilities. | Write/read/hash/provider failures fail the delivery attempt and do not mark batch delivered. | Adapter plus delivery service. | Object storage provider configuration. | Attempt failure summary. | Safe error summary only. | No provider raw exception payload in metadata. | Existing push error flow. | Ready. | Normalize adapter errors. |
| Production readiness | WebAdmin push exists but is blocked without adapter. | Register file-delivery adapter only when outbound destination configuration is valid. | Web composition/infrastructure registration. | Secure deployment config. | Adapter availability. | Invalid config means adapter unavailable, not fake success. | No no-network adapter in production. | Push button remains blocked until ready. | Ready. | Add DI/source guard. |
| No-network test adapter | Tests already use deterministic adapter behavior. | Keep no-network adapter test-scoped only. It must never be registered by production composition. | Test projects. | Test composition. | Test remote id only. | Test success does not imply runtime delivery. | Prevents false delivered batches. | No production UI change. | Ready. | Keep source guard against production registration. |
| Future API adapter | Provider-neutral connector boundary exists. | Future accounting API adapters reuse the same stored-package delivery contract and cannot bypass batch/document evidence. | Future target adapter. | Future credential policy. | Provider receipt ids through `ExternalReference`. | API rejection is failed outbound attempt, not sync conflict. | No API-specific assumptions in file-delivery adapter. | Separate future slice. | Design later. | Do not add API mapping in file-delivery implementation. |

## Locked File-Delivery Decisions

- `FinanceExportFileDeliveryAdapter` is the first real target adapter.
- The adapter is outbound-only and copies the stored canonical package to a configured destination.
- The destination is configured through secure deployment configuration. It is never taken from batch metadata, attempt metadata, package content, `ExternalReference`, or `DocumentRecord`.
- The preferred destination profile is `FinanceExportOutbound`.
- Production push remains disabled when the outbound profile is missing, invalid, or backed by an unsupported provider.
- The adapter reports success only after object write completion and SHA-256 hash verification.
- Existing object with the same hash is idempotent success. Existing object with a different hash is a delivery failure.
- Target-side delivery identity is stored through `ExternalReferenceKind.Export`.
- The adapter response contains only safe/reportable fields: destination object key, display file name or label, delivered timestamp, and safe hash summary.
- No credentials, access tokens, refresh tokens, connection strings, private keys, raw provider payloads, provider exception dumps, package body, archive payloads, or operational document payloads are stored in export metadata.
- The adapter does not add connector credential forms, target browsing UI, package regeneration, journal editing, invoice/payment/refund/credit-note mutation, public WebApi routes, mobile/member routes, or storefront changes.
- Future accounting API adapters must reuse the same stored-package delivery boundary and must not add API-specific assumptions to file-delivery.

## Implementation Readiness

`Finance Export File Delivery Adapter Slice`

Implemented scope:

- Added `FinanceExportFileDeliveryAdapter` behind `IFinanceExportConnectorAdapter`.
- Added outbound destination readiness for profile `FinanceExportOutbound`.
- Registered the adapter only when deployment configuration selects a non-database outbound provider.
- Extended the internal delivery request with export key, period, and posting-status metadata so the adapter can build deterministic destination names without querying mutable operational documents.
- Copied only the stored package stream to deterministic outbound object key `finance-exports/outbound/{businessId}/{externalSystemId}/{yyyyMMdd-yyyyMMdd}/{batchId}.json`.
- Verified destination SHA-256 hash before returning success.
- Treated existing destination object with the same hash as idempotent success and existing destination object with a different hash as delivery failure.
- Returned safe remote id, display id, timestamp, and summary for `ExternalReference`.
- Kept WebAdmin push unchanged except that readiness becomes true when this real adapter is registered.
- Added infrastructure/source tests for success, missing config, hash mismatch, idempotent same-hash retry, and production no-network guard.
- No schema/migration, public/mobile/storefront contracts, connector credential UI, target browsing UI, package regeneration, journal edit, invoice/payment/refund/credit-note flow changes, or accounting API integration was added.

## Target Adapter Smoke/Operations Hardening Outcome

`Finance Export Target Adapter Smoke/Operations Hardening`

Implemented scope:

- Verified file-delivery with filesystem object storage from `FinanceExports` source package storage to `FinanceExportOutbound` destination storage.
- Confirmed the adapter is registered only when the outbound profile uses a supported non-database provider.
- Confirmed missing outbound configuration and destination hash conflicts leave the batch generated and retryable.
- Confirmed successful delivery writes the outbound object, verifies the package hash, marks the batch delivered, and records an `ExternalReferenceKind.Export` receipt.
- Confirmed production source and WebAdmin composition do not register no-network or test adapters.
- Added non-secret deployment examples for finance export source and outbound object-storage profiles.
- No accounting API adapter, credential UI, target browsing UI, package regeneration, journal edit, invoice/payment/refund/credit-note mutation, schema/migration, public route, mobile/member route, or storefront change was added.

Next gate:

- `Finance Export Accounting API Target Design Slice` only after a real accounting target, credential owner, payload mapping, and error contract are selected.
- Without that selection, the file-delivery adapter remains the production-safe outbound delivery option.
