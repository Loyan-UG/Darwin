# Bank API Target Adapter Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks the boundary for future bank API target adapters. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, credential UI, bank connection, worker, provider adapter, statement import automation, settlement automation, or finance export change.

Decision: Bank API integration must be target-selected and credential-governed. Manual/imported bank statement evidence and reconciliation remain the safe baseline until a real bank or aggregation target, credential owner, payload mapping, error contract, and smoke strategy are selected.

## Current Darwin Bank API Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| `BankAccount`, `BankStatementImport`, `BankStatementLine` | Treasury evidence foundation exists. | API adapters should create statement import evidence, not direct settlement. |
| `BankReconciliationMatch` | Reconciliation evidence exists and does not rewrite history. | Adapter-imported lines still require reconciliation/matching policy. |
| Supplier/payroll bank settlement | Settlement is journal-backed from reconciliation evidence. | API import does not auto-settle payments in v1. |
| `ExternalSystem`, `ExternalReference`, `SyncState`, `SyncConflict` | Integration identity and sync evidence exist. | Use for bank connection identity and sync state. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Automated statement import | Reduces manual file import. | Requires bank connection, credential reference, sync worker, line identity, retry. |
| Bank account verification | Confirms configured bank account identity. | Requires safe masked identifiers and no raw credentials. |
| Error and consent management | Operators understand expired consent or failed sync. | Requires safe error contract and readiness display. |
| Duplicate prevention | Prevents repeated bank lines. | Requires deterministic line identity and idempotent import. |

## Target Selection Rules

| Rule | Decision |
| --- | --- |
| Real target required | Select a specific bank API or aggregation provider before implementation. |
| German market priority | Future target selection should evaluate widely used German banks and compliant aggregation options. |
| Credential owner | Credentials/consents are deployment secrets or secure vault references, not domain metadata. |
| Payload mapping | Statement account, booked/pending lines, counterparty, remittance, value date, and transaction ids require mapping docs. |
| Error contract | Expired consent, rate limit, provider outage, malformed response, duplicate line, and permission failure must be safe and operator-readable. |
| Smoke strategy | Use sandbox or controlled account with non-secret evidence; no live credentials in repo/docs/logs. |

## Future Entity Ownership

Bank API adapter implementation should prefer existing `ExternalSystem`, `ExternalReference`, `SyncState`, `SyncConflict`, `BankStatementImport`, and `BankStatementLine`. Add a new bank connection model only after target selection proves existing external system metadata is insufficient.

Potential future `BankConnection` must store only bank account link, external system, masked display name, consent status, status timestamps, and secret references. It must not store access tokens, refresh tokens, private keys, or raw provider payloads.

## Application Surface

Future handlers:

- Check adapter readiness for a selected bank target.
- Start sync from configured bank account connection.
- Create bank statement import evidence from fetched lines.
- Record external references and sync state.
- Record safe sync conflict or error.
- Never auto-settle supplier/payroll/customer payments in the adapter.

## WebAdmin Surface

Future WebAdmin can show bank connection readiness, last sync, safe errors, manual resync, and imported statement batches. Credential entry/configuration UI is separate and only after secure secret owner design.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `provider-bank-api` plus `bank-treasury`. |
| Package role | Provider add-on to Finance/Treasury. |
| Required dependencies | `finance`, `bank-treasury`, `integrations-sync`. |
| Disabled behavior | Manual statement import remains if enabled; API sync worker skips target. |
| Permissions | View readiness, trigger sync, review import errors. |
| Secrets | Secret references only; no raw bank credentials in metadata/events/docs/logs. |

## Compatibility Boundaries

- API adapter imports statement evidence only.
- No direct bank settlement, returned-transfer automation, duplicate-payment automation, supplier advance, payment/refund mutation, public/mobile exposure, or finance export change.
- Reconciliation and settlement remain owned by existing treasury/payment handlers.

## Implementation Slices

1. `Bank API Target Selection Checkpoint`.
2. `Bank Connection Credential Boundary Design`.
3. `Bank Statement Import Adapter Foundation Slice`.
4. `Selected Bank/Aggregator Adapter Slice`.
5. `Bank API Operations Hardening Slice`.

## Test Plan

Future tests must cover readiness false without target/config, no-secret metadata, idempotent line import, duplicate line rejection, safe errors, sync state/conflict evidence, no settlement mutation, worker skip behavior, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement a bank API adapter.
