# Mobile API Boundary Freeze Review

This review defines the mobile API freeze and migration rules that must be respected before broader ERP domain expansion continues. It is a documentation-only decision artifact: it does not change entities, migrations, DTOs, API routes, mobile contracts, or test code.

If a required mobile-facing foundation change is discovered, it must be completed before mobile release with its matching compatibility tests. Delaying release is acceptable when the alternative is post-release downtime, broken clients, forced contract migration, or duplicated source-of-truth state.

## Current Mobile Boundary

- `ApiRoutes` is the canonical shared route catalog for mobile clients.
- Current route roots are audience-first: `api/v1/member`, `api/v1/business`, `api/v1/public`, and `api/v1/meta`.
- `Darwin.Contracts` owns public DTO shapes used across WebApi and mobile clients.
- `Darwin.Mobile.Shared` owns mobile service calls and route consumption.
- Route compatibility is guarded by `ApiRoutesCanonicalRouteTests` and WebApi route alias/source tests.
- Contract compatibility is guarded by profile, auth, commerce, content, loyalty, business, and promotion serialization tests.
- Mobile release readiness is guarded by shared, consumer, and business launch readiness tests plus mobile source-contract tests.
- ERP and back-office expansion must add audience-scoped surfaces without overloading current member/business mobile workflows.

## Freeze Decision Matrix

| Area | Current route/contract surface | Decision | Allowed changes before release | Breaking change rule | Required tests | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Meta and bootstrap | `api/v1/meta/health`, `api/v1/meta/info`, `api/v1/meta/bootstrap`. | Freeze current route names and response shape for mobile startup. | Add nullable fields only when older clients can ignore them. | Any removal, rename, required field addition, or changed semantic must be migrated before release. | Route catalog tests, mobile launch readiness guards, bootstrap/mobile service tests when touched. | Startup diagnostics must remain stable because app boot depends on these endpoints. |
| Auth and identity | Member login, register, external login, refresh, logout, password, email confirmation, token, phone verification, account deletion, and device flows. | Freeze unless a deliberate pre-release migration is approved. | Add nullable profile or diagnostic fields with serialization compatibility coverage. | Breaking changes must update WebApi, contracts, mobile shared services, mobile view models, source-contract tests, and serialization tests in the same slice. | Auth serialization tests, profile serialization tests, mobile auth/profile service tests, launch readiness guards, relevant source-contract tests. | Identity/profile changes are release-sensitive because they affect login, activation, verification, and push/device registration. |
| Profile, addresses, and customer bridge | Member profile, avatar, member addresses, preferences, linked customer, and customer context. | Freeze public DTO property names; allow internal mapping improvements. | Add nullable fields only when they do not change existing mobile behavior. | Address/profile/customer contract changes must be completed before release and covered by serialization plus mobile service tests. | Profile contract serialization tests, mobile `ProfileService` tests, member address source-contract tests, customer context tests when touched. | The canonical address implementation remains internal; existing mobile address wire shape stays compatible. |
| Business auth and access | Business invitation preview/accept, access-state, media, gallery, and profile image routes. | Freeze route names and access-state semantics. | Add nullable response fields for richer access or media metadata when older clients can ignore them. | Breaking invitation, access-state, or media behavior requires coordinated WebApi, shared service, business mobile, and launch guard updates before release. | Business launch readiness guards, business source-contract tests, route catalog tests, contract serialization tests. | `BusinessMember` remains the release-time business access source. |
| Public business discovery | Public business list, map, category kinds, business detail, member business account context, engagement, likes, favorites, and reviews. | Freeze current public/member route split. | Add filters or nullable projection fields without changing existing list/detail semantics. | Moving a public route, changing required query behavior, or renaming contract fields must be migrated before release. | Route catalog tests, business mapper tests, contract serialization tests, consumer source-contract tests. | ERP features must not repurpose discovery routes for back-office workflows. |
| Loyalty scan and account | Scan prepare/process/confirm, loyalty accounts, overview, history, business dashboard, rewards, join, next reward, promotions, and campaign/reward configuration. | Freeze ledger-facing mobile contracts unless a required fix is found. | Add nullable display/support fields when ledger state and scan behavior remain unchanged. | Any QR/session, accrual, redemption, reward, account, or campaign contract change must be implemented before release with mobile and WebApi tests. | Loyalty contract serialization tests, mobile loyalty service tests, scan/session tests, consumer/business source-contract tests. | Loyalty ledger remains the points source of truth; no duplicate balance is introduced elsewhere. |
| Member commerce | Member orders, invoices, payment intent, document download, archive document, structured data, and structured XML routes. | Freeze route names, archive/download behavior, and member-facing document contracts. | Add nullable display fields or new additive document links only with serialization coverage. | Any order/invoice contract or artifact route break must be resolved before release, including mobile command readiness and archive/source-model tests. | Commerce/content serialization tests, member commerce source-contract tests, order/invoice query tests, archive/source-model tests. | Existing issued snapshots and archive payloads must remain readable and downloadable. |
| Billing and subscriptions | Business subscription current state, cancel-at-period-end, checkout intent, and plans. | Freeze current route names and subscription state semantics. | Add nullable plan or invoice metadata only when older business clients can ignore it. | Breaking billing flow changes require coordinated business mobile and WebApi migration before release. | Business launch readiness guards, billing contract tests when touched, route catalog tests. | Subscription flows are operationally sensitive and should stay isolated from ERP finance expansion. |
| Notifications and devices | Member device registration route and push/device registration contracts. | Freeze route and required payload shape. | Add nullable device capability metadata if older clients can ignore it. | Required field changes, token semantics changes, or route changes must be migrated before release. | Mobile service tests, launch readiness guards, auth/profile serialization tests when touched. | Device registration is part of mobile operability, not a generic CRM communication surface. |
| Route catalog and aliases | `ApiRoutes`, audience-first routes, route alias/source tests. | Freeze current route constants after this review unless a deliberate migration is approved. | Add new routes under the correct audience root. | Do not remove or rename current mobile routes without same-slice tests and client updates before release. | `ApiRoutesCanonicalRouteTests`, WebApi route alias/source tests, mobile service tests for touched routes. | New ERP routes should be additive and scoped by audience, not hidden behind existing mobile route names. |
| Contract serialization | Public DTOs consumed by WebApi, mobile shared services, consumer app, and business app. | Freeze existing property names, required fields, enum meanings, and date/number semantics. | Add nullable fields with explicit serialization compatibility tests. | Any required field, rename, enum semantic change, or payload structure change must be treated as a pre-release migration. | Contract serialization compatibility tests plus source-contract tests for affected app surfaces. | JSON/custom data must not replace common mobile contract fields that are filterable, reportable, compliance-relevant, or cross-module. |

## Contract Change Rules

- Route removals and renames are not allowed after freeze unless they are part of an explicit pre-release migration.
- Additive routes are allowed when they use the correct audience root and do not change existing route semantics.
- Additive DTO fields must be nullable or default-tolerant unless the change is intentionally migrated before release.
- Existing DTO property names, enum meanings, identifier meanings, date/time semantics, and monetary semantics are compatibility-sensitive.
- ERP routes must be introduced as new audience-scoped endpoints; they must not overload member profile, discovery, loyalty, commerce, or business access routes.
- Mobile route and contract changes must update WebApi, shared contracts, mobile shared services, app-level source-contract tests, route tests, and serialization tests together.
- Internal mapping improvements are allowed when the public wire shape and persisted snapshot readability stay compatible.

## Required Test Lanes

| Change type | Minimum required verification |
| --- | --- |
| Route catalog addition or route usage change | `ApiRoutesCanonicalRouteTests`, relevant WebApi route alias/source tests, and touched mobile service tests. |
| Public DTO field addition | Relevant contract serialization compatibility tests and touched mobile service/source-contract tests. |
| Auth/profile/customer change | Auth/profile serialization tests, mobile profile/auth service tests, and consumer source-contract tests. |
| Business access change | Business launch readiness guards, business source-contract tests, contract serialization tests, and affected WebApi tests. |
| Loyalty scan/account/reward/campaign change | Loyalty serialization tests, scan/session tests, mobile loyalty service tests, and consumer/business source-contract tests. |
| Order/invoice/artifact change | Commerce/content serialization tests, member commerce source-contract tests, order/invoice tests, and archive/source-model tests. |
| Snapshot or internal mapping change | Existing snapshot read tests plus focused mapper tests proving current wire shape remains unchanged. |

## Release Gate

Before mobile release, every change touching mobile-facing routes, DTOs, authentication, profile, address, business access, loyalty, commerce, billing, notifications, or snapshot/artifact behavior must have one of these outcomes:

- `Frozen`: current behavior is intentionally kept and covered by existing tests.
- `Additive`: nullable/default-tolerant behavior is added with matching tests.
- `Migrated before release`: breaking behavior is deliberately changed before release with WebApi, contract, mobile, and test updates in the same implementation slice.
- `Deferred with reason`: change is confirmed not to affect release-sensitive mobile behavior.

No mobile-sensitive change should be left for an unplanned post-release migration.

## Next Implementation Slice

`Loyalty Ledger And Scan Compatibility Review` is the next release-sensitive slice.

Goal: confirm whether loyalty ledger, scan session, QR/session, reward, campaign, accrual, redemption, and mobile loyalty contracts require any pre-release change. If a change is required, implement it before release; if not, record no-change evidence and keep the ledger as the single points source of truth.
