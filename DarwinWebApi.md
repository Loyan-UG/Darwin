# Darwin WebApi — Technical Guide

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)

> Scope: **Darwin.WebApi**, its Application/Contracts integration, security/policy model, and API operations guidance.

---

## 1) Purpose and Boundaries

`Darwin.WebApi` is the public REST surface consumed by:

- Mobile apps (`Darwin.Mobile.Consumer`, `Darwin.Mobile.Business`) through `Darwin.Mobile.Shared`.
- Web and future external clients where contracts-first integration is required.

Core rules:

- `Darwin.Contracts` is the request/response source of truth.
- Domain/EF internals stay private in Application/Infrastructure.
- Handler results use `Darwin.Shared.Results` (`Result` / `Result<T>`), then controllers map failures to API problem responses.

---

## 2) Composition and Architecture

- Composition root: `src/Darwin.WebApi/Extensions/DependencyInjection.cs`
- Controller surface: `src/Darwin.WebApi/Controllers/**`
- Business logic: `src/Darwin.Application/**`
- Persistence and auth infra: `src/Darwin.Infrastructure/**`
- Public contract schema: `src/Darwin.Contracts/**`

Execution pattern:

1. Controller validates HTTP/auth context.
2. Controller maps Contracts request DTO -> Application request DTO/command/query.
3. Handler executes use case and returns `Result`.
4. Controller maps success to Contracts response, failure to Problem payload.

---

## 3) Security Model

- Auth: JWT bearer.
- Policies (current core):
  - `perm:AccessMemberArea`
  - `perm:AccessLoyaltyBusiness`
- Claims-based current user resolution through `ICurrentUserService`.
- Login/refresh routes may be rate-limited depending on active configuration.

### Device binding note

If device binding is enabled in your environment, login/refresh must carry device identity fields expected by auth contracts and server validation.

---

## 4) Mobile-facing Endpoint Matrix (v1)

> This section is the API source of truth referenced by mobile docs.

| Area | Endpoint | Policy | Primary Client |
|---|---|---|---|
| Auth | `POST /api/v1/auth/login` | AllowAnonymous | Both |
| Auth | `POST /api/v1/auth/refresh` | AllowAnonymous | Both |
| Auth | `POST /api/v1/auth/logout` | Authorize | Both |
| Auth | `POST /api/v1/auth/logout-all` | Authorize | Both |
| Auth | `POST /api/v1/auth/register` | AllowAnonymous | Consumer |
| Auth | `POST /api/v1/auth/password/request-reset` | AllowAnonymous | Consumer |
| Auth | `POST /api/v1/auth/password/reset` | AllowAnonymous | Consumer |
| Auth | `POST /api/v1/auth/password/change` | Authorize | Both |
| Notifications | `POST /api/v1/notifications/devices/register` | Authorize | Both |
| Profile | `GET /api/v1/profile/me` | `perm:AccessMemberArea` | Consumer |
| Profile | `PUT /api/v1/profile/me` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `POST /api/v1/loyalty/scan/prepare` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `POST /api/v1/loyalty/scan/process` | `perm:AccessLoyaltyBusiness` | Business |
| Loyalty | `POST /api/v1/loyalty/scan/confirm-accrual` | `perm:AccessLoyaltyBusiness` | Business |
| Loyalty | `POST /api/v1/loyalty/scan/confirm-redemption` | `perm:AccessLoyaltyBusiness` | Business |
| Loyalty | `GET /api/v1/loyalty/my/accounts` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `GET /api/v1/loyalty/my/history/{businessId}` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `GET /api/v1/loyalty/account/{businessId}` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `GET /api/v1/loyalty/business/{id}/rewards` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `GET /api/v1/loyalty/my/businesses` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `POST /api/v1/loyalty/my/timeline` | `perm:AccessMemberArea` | Consumer |
| Loyalty | `GET /api/v1/loyalty/business/campaigns` | `perm:AccessLoyaltyBusiness` | Business |
| Loyalty | `POST /api/v1/loyalty/business/campaigns` | `perm:AccessLoyaltyBusiness` | Business |
| Loyalty | `PUT /api/v1/loyalty/business/campaigns/{id}` | `perm:AccessLoyaltyBusiness` | Business |
| Loyalty | `POST /api/v1/loyalty/business/campaigns/{id}/activation` | `perm:AccessLoyaltyBusiness` | Business |
| Discovery | `POST /api/v1/businesses/list` | AllowAnonymous | Consumer |
| Discovery | `POST /api/v1/businesses/map` | AllowAnonymous | Consumer |
| Discovery | `GET /api/v1/businesses/{id}` | AllowAnonymous | Consumer |
| Discovery | `GET /api/v1/businesses/{id}/with-my-account` | `perm:AccessMemberArea` | Consumer |
| Business | `POST /api/v1/businesses/onboarding` | Authorize | Both |

---

## 5) Promotions Feed Policy & Diagnostics

Promotions feed supports contract-driven server guardrails:

- `EnableDeduplication`
- `MaxCards`
- `SuppressionWindowMinutes` (legacy)
- `FrequencyWindowMinutes` (preferred)

Precedence:

- When `FrequencyWindowMinutes` is supplied, it is used as primary repeat-delivery control.
- If absent, server falls back to `SuppressionWindowMinutes` behavior.

Response observability includes diagnostics counters:

- `InitialCandidates`
- `SuppressedByFrequency`
- `Deduplicated`
- `TrimmedByCap`
- `FinalCount`

---

## 6) Error Shape & Concurrency

- Problem responses follow shared API problem contracts.
- Profile update uses optimistic concurrency (`Id` + `RowVersion`) and should return deterministic conflict semantics.
- Password reset may intentionally return success-like behavior for anti-enumeration scenarios.

---

## 7) Postman Verification Playbook

### 7.1 Login (consumer)

- `POST /api/v1/auth/login`
- save `accessToken` / `refreshToken`.

### 7.2 Discover list

- `POST /api/v1/businesses/list`
- expected: seeded businesses in `items`.

### 7.3 Business detail

- `GET /api/v1/businesses/{businessId}`

### 7.4 Join loyalty

- `POST /api/v1/loyalty/account/{businessId}/join`

### 7.5 Prepare scan session

- `POST /api/v1/loyalty/scan/prepare`
- expected: session token + expiry.

### 7.6 Business-side process

- login as business account
- `POST /api/v1/loyalty/scan/process`

> Keep concrete payload examples in your team Postman collection/environment and update alongside Contracts changes.

---

## 8) Documentation Ownership

When API contracts or policies change, update these together in one PR:

1. `Darwin.Contracts`
2. `Darwin.Application` mapping/handler logic
3. `Darwin.WebApi` controller mapping
4. `DarwinWebApi.md`
5. related mobile docs (`DarwinMobile.md`) only for app-side impact summaries

