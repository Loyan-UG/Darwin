# Loyalty Ledger And Scan Compatibility Review

This review records the release-sensitive loyalty and scan decisions before broader ERP domain expansion continues. It is both a compatibility decision artifact and the checklist for small guard-test hardening. It does not redesign loyalty, add migrations, rename routes, or change public/mobile DTO shapes.

If a required loyalty or mobile-facing change is discovered, it must be completed before mobile release with matching WebApi, contract, mobile shared service, and test updates. Delaying release is acceptable when the alternative is post-release downtime, broken scan flows, forced mobile migration, or duplicated loyalty state.

## Current Darwin Model Findings

- `LoyaltyAccount` owns operational balance projections: `PointsBalance`, `LifetimePoints`, `LastAccrualAtUtc`, and lifecycle status.
- `LoyaltyPointsTransaction` is the points ledger and audit surface for accrual, redemption, and adjustment deltas.
- `QrCodeToken` stores the short-lived, opaque token value. The QR payload must contain only the token string.
- `ScanSession` is an internal server-side session record linked to `QrCodeToken` and `LoyaltyAccount`.
- `PrepareScanSessionResponse`, `ProcessScanSessionForBusinessResponse`, `ConfirmAccrualResponse`, and `ConfirmRedemptionResponse` are mobile-release-sensitive contracts.
- `ScanSession.SelectedRewardsJson` is an internal snapshot for redemption confirmation and must not become a mobile-controlled confirmation payload.
- Reward tier configuration and campaign/promotion payloads already use structured fields for common UI/reporting data and JSON for targeting/policy/custom payloads.
- Mobile shared `LoyaltyService` is the client-side compatibility boundary for consumer and business loyalty apps.

## Decision Matrix

| Surface | Current model | Release decision | Storage shape | Mobile/API impact | Required before mobile release | Guard evidence |
| --- | --- | --- | --- | --- | --- | --- |
| Loyalty account balance projection | `LoyaltyAccount.PointsBalance`, `LifetimePoints`, `LastAccrualAtUtc`, `Status`. | Keep as operational projection; do not move balance to CRM, profile, or customer records. | Structured columns on `LoyaltyAccount`. | Existing account summary, overview, dashboard, history, and next-reward contracts remain compatible. | Yes: no duplicate balance source is allowed. | Account/query tests and contract serialization tests. |
| Points ledger | `LoyaltyPointsTransaction` records signed deltas and audit context. | Keep as audit ledger for points changes; account projection is updated from confirmed operations. | Structured entity; no JSON for core ledger fields. | No mobile contract change. | Yes: ledger remains the source for audit and reconciliation. | Loyalty unit tests and transaction projection tests. |
| QR token payload | `QrCodeToken.Token` is opaque and short-lived. | QR payload exposes only `ScanSessionToken`; no internal ids or user ids. | Structured entity with opaque token string. | Prepare/process/confirm contracts stay token-first. | Yes: internal identifiers must not leak. | Serialization guards for forbidden identifier fields. |
| Scan session lifecycle | `ScanSession` tracks business, account, token, status, expiry, outcome, and resulting transaction. | Keep internal; route/DTO contracts use token, not session id. | Structured entity with internal lifecycle fields. | No route or DTO rename. | Yes: one-time-use and expiry semantics are release-critical. | Resolver, confirmation race, and scan-session query tests. |
| Selected rewards snapshot | `ScanSession.SelectedRewardsJson` stores redemption selections at prepare time. | Keep as internal v1 snapshot; business confirmation cannot supply a new reward list. | JSON snapshot controlled by Application layer. | No mobile contract change; selected rewards remain display/projection fields. | Yes: confirmation must be replay-safe and server-owned. | Prepare/process/confirm redemption tests. |
| Prepare/process/confirm contracts | Contracts carry token, mode, expiry, selected rewards, allowed actions, success/failure and balance/account snapshots. | Freeze property names and enum semantics; additive nullable fields only with tests. | Public DTOs in contracts. | Breaking changes require pre-release migration. | Yes. | Serialization and mobile service route/mapping tests. |
| Reward tier configuration | Reward tier common fields are structured; business mutation contracts carry row versions. | Keep structured common fields; no redesign before release. | Structured entities/contracts. | Existing business app reward config routes remain stable. | Yes: freeze current mutation contract. | Reward tier serialization and handler tests. |
| Campaign and promotion payloads | Common fields are structured; targeting and payload data use JSON. | Keep JSON for targeting/policy/custom payloads in this slice. | Hybrid structured fields plus JSON payload. | Existing promotion feed and campaign management contracts remain stable. | Yes: no schema migration for campaign JSON now. | Promotion/campaign serialization and policy tests. |
| Member overview/history/timeline | Mobile reads account summaries, history, timeline, dashboard, and promotion feed. | Freeze current projection contracts and avoid internal id leakage. | Projection contracts; ledger-derived data. | Existing mobile read flows remain compatible. | Yes. | Overview/history/timeline serialization and query tests. |
| Business scan/access boundary | Business app processes token and confirms accrual/redemption under business access rules. | Keep business access source and scan routes stable. | Existing business access plus scan handlers. | Existing business mobile flow remains compatible. | Yes. | Business source-contract, scan handler, and launch readiness tests. |
| Mobile shared service | `LoyaltyService` consumes `ApiRoutes` and maps contracts to client models. | Keep route usage canonical, token-normalized, and failure-aware. | Client service layer only. | No public route/DTO change. | Yes. | Mobile shared service tests. |

## Compatibility Rules

- `Customer`, CRM records, and profile records must not own a separate loyalty balance.
- Loyalty mobile contracts must not expose `scanSessionId`, `qrCodeTokenId`, `userId`, `ScanSession.Id`, or `QrCodeToken.Id`.
- `ScanSessionToken` is treated as a short-lived secret and must be trimmed before mobile service submission.
- Accrual and redemption confirmation failure payloads must return failed mobile results; mobile clients must not synthesize a successful balance from failure responses.
- Selected rewards used for redemption confirmation are server-owned from the prepared session snapshot.
- Campaign `TargetingJson` and `PayloadJson` stay JSON until a concrete reporting/filtering requirement justifies structured columns.
- Breaking changes to loyalty routes, DTOs, enum meanings, QR token behavior, or confirmation semantics must happen before mobile release with matching contract and mobile tests.

## No-Change Evidence

For this slice, the expected implementation evidence is:

- Serialization tests prove mobile loyalty contracts keep stable property names and do not leak internal scan identifiers.
- Mobile shared service tests prove canonical routes, selected reward id submission, token normalization, and failure response handling.
- Existing scan resolver, confirmation race, and endpoint tests continue to prove expiry, one-time-use, business binding, and status behavior.
- Existing campaign/reward tier tests continue to prove mutable business loyalty contracts and RowVersion payloads.

## Next Slice

`Identity Profile And Customer Bridge Compatibility Slice` is the next release-sensitive slice.

Goal: freeze or deliberately migrate auth/profile/customer source-of-truth behavior before mobile release, especially `User`, profile contacts, linked `Customer`, member customer context, token/external-login, phone verification, push/device, and account deletion contracts.
