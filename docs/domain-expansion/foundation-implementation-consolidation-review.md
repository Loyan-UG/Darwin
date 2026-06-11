# Foundation Implementation Consolidation Review

This review consolidates the release-sensitive foundation slices completed before broader ERP domain expansion continues. It is a decision and evidence artifact only. It does not change entities, migrations, API routes, DTOs, mobile contracts, or production code.

If any real mobile, loyalty, or foundation gap is discovered after this review, it must be completed before mobile release with matching WebApi, contract, mobile shared service, and test updates. Release delay is acceptable when the alternative is post-release downtime, duplicated source-of-truth state, or forced mobile migration.

## Current P0 Foundation Baseline

- Address foundation: an internal canonical mapper exists and is guarded across `Address`, `CustomerAddress`, `BusinessLocation`, public business location mapping, and checkout order snapshots.
- Mobile API boundary: route and DTO freeze rules are documented, and route/service/serialization guards exist for release-sensitive mobile surfaces.
- Loyalty ledger and scan: loyalty ledger remains the points source of truth, and QR/session contracts remain opaque-token-first without internal identifier leakage.
- Identity profile and customer bridge: `User` is the source for linked member identity/profile/contact; `Customer` keeps CRM lifecycle, segmentation, tax/commercial, and fallback data.
- Business and BusinessMember access: `Business` remains the merchant root, and `BusinessMember` remains the current source of business mobile access.
- Order and invoice snapshots: member order/invoice snapshots, archive/download routes, and structured source-model export routes are guarded and remain part of the current order/invoice foundation.

## P0 Evidence Matrix

| Surface | Decision | Implemented evidence | Tests/guards | Remaining release risk | Required before mobile release | Next action |
| --- | --- | --- | --- | --- | --- | --- |
| Address foundation | Keep canonical address as an internal application concept; preserve current public/mobile wire shapes and immutable order/invoice snapshots. | Canonical address mapper/normalizer exists and is used by identity addresses, customer addresses, business locations, public business mapping, and checkout snapshots. | Canonical mapper tests, focused order placement tests, business mapper tests, mobile profile route tests, contract serialization tests. | Low: schema remains unchanged; future risk is inconsistent use if new modules bypass the mapper. | No further change unless a release-sensitive address gap is found. | Enforce mapper use in future ERP address-bearing records. |
| Mobile API boundary | Freeze current member/business mobile routes and DTO semantics; breaking changes require deliberate pre-release migration. | `ApiRoutes` catalog, service tests, serialization guards, and launch/source-contract guards cover current release-sensitive routes. | `ApiRoutesCanonicalRouteTests`, mobile shared service tests, launch readiness guards, contract serialization tests. | Low to medium: any new ERP route must remain additive and audience-scoped. | No further change unless a route/DTO incompatibility is discovered. | Keep all ERP/back-office routes separate from mobile member/business contracts. |
| Loyalty ledger and scan | Keep loyalty ledger as points source of truth; QR/session contracts expose opaque tokens, not internal ids. | Ledger/source-of-truth decision documented; mobile loyalty service and serialization guards cover scan prepare/process/confirm and timeline/account surfaces. | Loyalty serialization guards, mobile `LoyaltyService` tests, business access guard tests where scan operations are gated. | Low: campaign/promotion payload JSON remains appropriate for policy/custom payloads. | No further change unless scan/ledger contract evidence changes. | Reuse ledger events for future analytics/AI; do not add CRM-owned balance. |
| Identity profile and customer bridge | `User` owns linked member identity/profile/contact; `Customer` owns CRM lifecycle and fallback data. | Auth/profile/customer contracts and mobile services are guarded; device/push flows remain structured and user-owned. | Auth/profile serialization tests, mobile `AuthService`, `ProfileService`, `PushRegistrationService` tests, customer bridge tests where present. | Low to medium: broad external identity references are not yet structured. | No release blocker unless external identity must launch with mobile. | Feed external identity needs into external-system readiness primitives. |
| Business and BusinessMember access | `BusinessMember` remains the release-time business mobile access source; future staff/employee links must not replace it. | Business access-state, invitation, media/profile, and live-operation guards exist. | Business access-state handler tests, business auth/mobile service tests, `BusinessMediaService` tests, business launch readiness guards. | Low: HR/staff expansion could create duplication if it ignores this decision. | No further change unless business access-state behavior changes. | Future HR/staff design must link to `BusinessMember`. |
| Order and invoice snapshots | Evolve current order/invoice foundation; do not create a parallel member-facing sales/finance invoice model. | Snapshot contracts, member commerce routes, archive/download/source-model paths, cache scoping, and checkout concurrency behavior are guarded. | Commerce serialization tests, `MemberCommerceService` tests, `PlaceOrderFromCart` tests, route tests, contracts tests. | Low: future sales/finance expansion must respect issued snapshot immutability. | No further change unless archive/source-model compatibility evidence changes. | Extend current records/projections for sales and finance later. |
| External-system readiness primitives | Structured external identity primitives are needed before large ERP expansion. | Design exists, but no shared implementation for `ExternalSystem`, `ExternalReference`, `SourceOfTruth`, `SyncState`, or `SyncConflict`. | Foundation design docs only; no implementation guards yet. | Medium: later integrations may add ad hoc external ids if this is delayed. | Yes only if a release-sensitive mobile/loyalty/foundation integration must launch before mobile release; otherwise first post-hardening slice. | Implement minimal `ExternalSystem`, `ExternalReference`, and `SourceOfTruth` first; defer `SyncState` and `SyncConflict` unless immediately required. |
| Module visibility and feature gating | Module separation remains logical through UI navigation, permissions, and feature visibility; no project/database split. | Current permissions, roles, launch guards, business access-state, and UI route guards cover release-sensitive gates. | Existing permission/navigation/source tests and mobile launch guards. | Medium for future ERP packaging if feature visibility remains implicit. | No broad implementation required before mobile release. | Design `FeatureArea` after external-system primitives unless a release gate needs it. |
| Audit/business-event readiness | Current specialized audit evidence remains acceptable for release-sensitive flows; shared business events can wait unless a gap is found. | Loyalty transactions, provider operations, dispatch records, invoice archive metadata, business access/audit records, and event logs cover major current flows. | Existing domain, provider, archive, dispatch, and security/source tests. | Medium for future AI/automation and cross-module traceability. | No unless a release-sensitive audit proof gap is identified. | Design `BusinessEvent/AuditTrail` after external reference primitives. |
| Custom fields readiness | Use real columns for frequent/reportable/cross-module fields; use custom fields or JSON for uncertain, customer-specific, or industry-specific data. | Decision is documented; no shared custom-field implementation yet. | Documentation evidence only. | Medium for future customization if modules start adding one-off columns. | No before mobile release unless a release-sensitive customization requirement appears. | Implement after external references and before broad CRM/ERP customization rollout. |

## Consolidated Decisions

- Release-sensitive hardening for the main mobile/loyalty/foundation surfaces is in place.
- The next implementation direction must be foundation primitives for external-system readiness, not broad CRM, sales, finance, purchasing, inventory, or HR feature expansion.
- `ExternalSystem`, `ExternalReference`, and `SourceOfTruth` are the first shared domain/schema primitives to implement.
- `SyncState` and `SyncConflict` should remain deferred unless a concrete release-sensitive integration requires them immediately.
- `CustomFieldDefinition`, `CustomFieldValue`, `Activity`, `Note`, `Attachment`, `DocumentRecord`, `NumberSequence`, `BusinessEvent`, `AuditTrail`, and `FeatureArea` should be prioritized after external identity primitives.
- No future ERP module may create a parallel source of truth for loyalty balance, business access, linked member profile/contact, order snapshots, or issued invoice source documents.

## Next Implementation Slice

`External-System Readiness Primitives Slice`

Goal: design and implement the minimum shared foundation for `ExternalSystem`, `ExternalReference`, and `SourceOfTruth` on foundation records, while deferring `SyncState` and `SyncConflict` unless an immediate release-sensitive need is found.

Default first scope:

- Structured `ExternalSystem` registry.
- Structured `ExternalReference` child records for selected foundation entities.
- Explicit `SourceOfTruth` decision field or enum where reconciliation and operator support need it.
- Tests proving uniqueness, lookup, soft-delete behavior, and no leakage into mobile contracts unless intentionally projected.

Broad ERP feature expansion should wait until this slice is complete.
