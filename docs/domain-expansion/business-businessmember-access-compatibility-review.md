# Business And BusinessMember Access Compatibility Review

This review records release-sensitive business access, invitation, media/profile, and future staff-linkage decisions before broader ERP domain expansion continues. It is a compatibility and guard-hardening artifact. It does not redesign business access, add migrations, rename routes, or change public/mobile DTO shapes.

If a required business access, invitation, media/profile, or business app guard change is discovered, it must be completed before mobile release with matching WebApi, contract, mobile shared service, and test updates. Delaying release is acceptable when the alternative is post-release downtime, broken business app access, forced mobile migration, or duplicated access-source state.

## Current Darwin Model Findings

- `Business` is the merchant root and tenant-scoped operational root for business-owned data, lifecycle, branding, locations, communication defaults, loyalty, billing, and future ERP operations.
- `BusinessMember` is the current source of business mobile access, role, and membership activation. It determines whether an authenticated user can operate a business workspace.
- `BusinessInvitation` and `AcceptBusinessInvitationRequest` control Business app onboarding and must keep token/device behavior stable for mobile release.
- `BusinessAccessStateResponse` is the main soft-gate contract for live operational workflows in the Business app.
- Business media/profile routes under `BusinessAccount` are release-sensitive because business setup and customer-facing branding depend on them.
- Future staff or employee records must not replace the current access source. They may add HR-specific data through a structured link to `BusinessMember`.

## Decision Matrix

| Surface | Current model | Release decision | Storage shape | Mobile/API impact | Required before mobile release | Guard evidence |
| --- | --- | --- | --- | --- | --- | --- |
| Business | Merchant root with lifecycle, active state, branding, contact, default locale/currency/time zone, locations, members, invitations, media, billing, and loyalty relations. | Keep as tenant-scoped operational root. Do not split into parallel ERP tenant roots. | Structured entity with existing child entities and selected JSON preference/override fields. | Business app and public discovery contracts continue to read from this root. | Yes: lifecycle/access semantics must be stable. | Access-state handler and route/source tests. |
| BusinessMember | Links `User` to `Business` with role and active state. | Keep as release-time source of business mobile access. | Structured entity. | Token business context and business access-state depend on active non-deleted membership. | Yes. | Access-state handler, JWT/token, and mobile auth tests. |
| BusinessInvitation | Stores invitation target, role, opaque token, expiry, status, acceptance, revocation, and audit note. | Keep invitation onboarding structured and token-based. | Structured entity. | Preview/accept contracts remain stable. | Yes. | Contract serialization and mobile `AuthService` tests. |
| Invitation preview/accept contracts | Preview is unauthenticated and intentionally small; accept carries token, device id, and optional new-account profile fields. | Freeze field names. Do not expose access token, refresh token, raw provider token, auth secret, or internal membership id in preview/accept payloads. | Public DTOs. | Business onboarding flow remains compatible. | Yes. | Serialization and mobile route tests. |
| Token business context and preferred business id | Auth/refresh uses business context from active membership and preferred business id when available. | Keep active `BusinessMember` as the authority for business app access. | Structured token issuance and membership lookup. | Business app session remains tied to valid membership. | Yes. | Auth service and token service tests. |
| BusinessAccessStateResponse | Carries user, business, activation, lifecycle, setup, operation, and blocking state. | Freeze contract shape and blocking codes for release. | Public DTO projection. | Business app live operations must check current server-backed state. | Yes. | Serialization, handler, mobile shared, and business app guard tests. |
| Business media/profile routes | `BusinessAccount` routes expose media library, upload, profile image, gallery create/update/delete. | Keep routes stable and business-scoped. | Public DTOs plus structured media records. | Business setup/profile UI remains compatible. | Yes. | Mobile `BusinessMediaService` route tests. |
| Business app live-operation guards | Business app view models refresh access-state before mutating live operations. | Keep server-backed access-state as the gate for scanner, session, rewards, dashboard, and related operations. | UI/service guard behavior. | Prevents stale mobile state from allowing blocked operations. | Yes. | Business app launch-readiness guard tests. |
| Future staff/employee linkage | HR/staff records are not the current access source. | Add future staff/employee as a structured link to `BusinessMember`; do not replace `BusinessMember` for mobile access. | Future structured entity/FK, not JSON security state. | No release-time mobile contract change. | No: design later, access-source decision now. | This review and future HR design tests. |

## Compatibility Rules

- Active, non-deleted `BusinessMember` remains the source of business mobile access.
- `BusinessMember` must not be replaced by future staff/employee records in release-sensitive auth or access-state logic.
- Invitation preview and acceptance contracts must not expose auth secrets, access tokens, refresh tokens, provider tokens, or internal membership identifiers.
- Business app live-operation workflows must rely on server-backed `BusinessAccessStateResponse`, not stale local UI state.
- Business media/profile route changes are breaking unless completed before mobile release with contract and mobile service tests.
- Additive nullable fields are allowed only when serialization and mobile service tests prove compatibility.

## No-Change Evidence

For this slice, the expected implementation evidence is:

- Contract serialization tests prove invitation preview/accept and access-state payload field names remain stable.
- Contract guards prove invitation preview/accept payloads do not leak auth secrets or internal membership identifiers.
- Mobile `AuthService` tests prove invitation token trimming/escaping and device id resolution use canonical routes.
- Mobile `BusinessAccessService` tests prove access-state reads use the canonical business account route.
- Mobile `BusinessMediaService` tests prove media/profile operations use canonical business account routes.
- Application handler tests prove inactive/deleted membership and blocked user states cut off business client access.
- Business app launch-readiness tests prove live operations refresh access-state before mutating workflows.

## Next Slice

`Order And Invoice Snapshot Compatibility Slice` is the next release-sensitive slice.

Goal: freeze member-facing order/invoice archive, download, source-model, and snapshot behavior before ERP sales and finance expansion.
