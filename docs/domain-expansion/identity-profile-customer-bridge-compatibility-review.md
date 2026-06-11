# Identity Profile And Customer Bridge Compatibility Review

This review records release-sensitive identity, profile, device, and customer-bridge decisions before broader ERP domain expansion continues. It is a compatibility and guard-hardening artifact. It does not redesign identity or CRM, add migrations, rename routes, or change public/mobile DTO shapes.

If a required auth, profile, customer, device, or account-deletion change is discovered, it must be completed before mobile release with matching WebApi, contract, mobile shared service, and test updates. Delaying release is acceptable when the alternative is post-release downtime, broken login/profile flows, forced mobile migration, or duplicated source-of-truth state.

## Current Darwin Model Findings

- `User` owns identity, login, profile, contact, phone verification state, preferences, consents, attribution, default address references, and mobile-facing account state.
- `UserToken` owns tokenized security workflows such as refresh, password reset, email confirmation, and phone verification.
- `UserLogin` owns external login linkage. Provider tokens are not domain state and must not be stored on CRM/customer records.
- `UserDevice` owns mobile installation, push token, notification permission, app version, device model, and device-binding metadata.
- `Customer.UserId` links CRM customer records to identity users. When linked, display/contact data should resolve from `User`; when unlinked, CRM fallback fields remain available.
- `CustomerProfile`, `LinkedCustomerProfile`, `MemberCustomerContext`, auth token contracts, phone verification contracts, push registration contracts, and account deletion request are mobile-release-sensitive.
- Account deletion/anonymization preserves required history while disabling active auth/device paths and must remain compatible with mobile settings flows.

## Decision Matrix

| Surface | Current model | Release decision | Storage shape | Mobile/API impact | Required before mobile release | Guard evidence |
| --- | --- | --- | --- | --- | --- | --- |
| User identity/profile/contact | `User` contains login identity, email, phone, profile fields, locale, timezone, currency, preferences, consents, and default address ids. | Keep `User` as source of truth for linked member identity, profile, and contact. | Structured columns plus existing JSON for preference/attribution bags. | `CustomerProfile` remains the mobile profile contract. | Yes: profile contract must be frozen or deliberately migrated. | Profile serialization and mobile `ProfileService` tests. |
| UserToken security flows | `UserToken` stores refresh, password reset, email confirmation, phone verification, and consumed/expiry state. | Keep structured token records; do not move token/security state into custom fields or CRM. | Structured entity. | Auth, refresh, email, password, and phone verification contracts remain compatible. | Yes. | Auth serialization and identity handler tests. |
| UserLogin external login | `UserLogin` stores provider/key/display linkage. | Keep provider linkage structured; do not store raw provider tokens in domain/customer records. | Structured entity. | External-login request/response contract remains stable. | Yes. | Auth serialization and mobile `AuthService` tests. |
| UserDevice push/device binding | `UserDevice` stores device id, platform, push token, permission, app version, model, active state, and last seen data. | Keep device state structured and user-owned. | Structured entity. | Push registration contract and member notification route remain stable. | Yes. | Push contract and mobile `PushRegistrationService` tests. |
| Customer.UserId bridge | `Customer.UserId` links CRM customer to identity user; fallback CRM fields exist for unlinked records. | Keep bridge; linked display/contact resolves from `User`, unlinked customer uses CRM fallback fields. | Structured nullable FK plus CRM fallback columns. | Member linked-customer/profile/context routes remain stable. | Yes. | CRM query tests and profile serialization/mobile service tests. |
| Customer fallback CRM data | `Customer` owns CRM lifecycle, company/tax, segmentation, consent, interactions, notes, and fallback contact fields. | Keep for unlinked/guest/lead/customer operations; do not duplicate identity-owned secrets or loyalty balance. | Structured columns and CRM child entities. | No mobile profile contract change. | Yes: source-of-truth behavior must remain explicit. | Customer/CRM query and handler tests. |
| CustomerProfile contract | Mobile profile contract carries user id, email, names, image, phone, locale, timezone, currency, and row version. | Freeze property names and row-version behavior. | Public DTO. | Breaking profile changes require pre-release migration. | Yes. | Profile serialization and mobile `ProfileService` tests. |
| LinkedCustomerProfile and MemberCustomerContext | Member-facing CRM bridge projections carry linked customer id/user id, effective contact, segments, consents, and interactions. | Freeze contract shape and keep it as a read projection; no identity secrets. | Projection DTOs. | Member customer routes remain stable. | Yes. | Profile serialization and CRM context tests. |
| Phone verification | Request/confirm contracts call profile phone verification routes and use `UserToken` internally. | Freeze contract names; phone verification state remains user-owned. | Public DTO plus structured token/user state. | Mobile profile service routes remain stable. | Yes. | Profile serialization and service tests. |
| Account deletion/anonymization | Mobile submits explicit irreversible confirmation; server deactivates/anonymizes and disables security/device paths. | Keep route and payload stable; do not hard-delete history needed for business/legal continuity. | Structured workflow across user-owned records. | Settings/account deletion flow remains compatible. | Yes. | Profile serialization, account deletion handler, and integration tests. |
| Auth login/refresh/external-login contracts | Password login, refresh, external login, and token response carry token/device/business context. | Freeze field names and token semantics; device id remains part of refresh binding. | Public DTOs plus structured token/login records. | Mobile auth flow remains compatible. | Yes. | Auth serialization and mobile `AuthService` tests. |

## Compatibility Rules

- Linked member profile/contact reads from `User`; unlinked customers read from CRM fallback fields.
- `Customer` must not own auth secrets, refresh tokens, provider tokens, phone verification tokens, push tokens, or loyalty balance.
- `UserToken`, `UserLogin`, and `UserDevice` remain structured security records; JSON/custom fields are not used for these states.
- Mobile-facing auth/profile/device/customer contracts are frozen. Additive nullable fields are allowed only with serialization and mobile service tests.
- Breaking route, DTO, token, phone verification, push device, or account deletion changes must be completed before mobile release.
- Account deletion must continue to disable active user/device/security paths while preserving historical records required by business, commerce, compliance, and audit flows.

## No-Change Evidence

For this slice, the expected implementation evidence is:

- Auth serialization tests prove login, refresh, external-login, and token response field names remain stable.
- Profile serialization tests prove profile, linked customer, customer context, phone verification, and account deletion payloads remain stable.
- Mobile `AuthService` tests prove canonical routes and resolved/trimmed device id behavior.
- Mobile `ProfileService` tests prove canonical profile/customer/phone/account deletion routes.
- Mobile `PushRegistrationService` tests prove trim/null-normalization and the canonical device registration route.

## Next Slice

`Business And BusinessMember Access Compatibility Slice` is the next release-sensitive slice.

Goal: freeze business mobile access, invitation, access-state, media/profile, and future staff/employee linkage behavior without replacing `BusinessMember` as the release-time mobile business access source.
