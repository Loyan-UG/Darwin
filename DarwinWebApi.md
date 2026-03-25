# Darwin WebApi - Technical Guide

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)

> Scope: **Darwin.WebApi**, its Application/Contracts integration, security/policy model, and API operations guidance.

---

## 1) Purpose and Boundaries

`Darwin.WebApi` is the public REST surface consumed by:

- mobile apps (`Darwin.Mobile.Consumer`, `Darwin.Mobile.Business`) through `Darwin.Mobile.Shared`
- the front-office web application (`Darwin.Web`)
- future external clients where contracts-first integration is required

Core rules:

- `Darwin.Contracts` is the request/response source of truth.
- Domain/EF internals stay private in Application/Infrastructure.
- Handler results use `Darwin.Shared.Results` (`Result` / `Result<T>`), then controllers map failures to API problem responses.
- Public/member-facing contracts must remain separate from back-office operational DTOs.

---

## 2) Composition and Architecture

- Composition root: `src/Darwin.WebApi/Extensions/DependencyInjection.cs`
- Controller surface: `src/Darwin.WebApi/Controllers/**`
- Business logic: `src/Darwin.Application/**`
- Persistence and auth infra: `src/Darwin.Infrastructure/**`
- Public contract schema: `src/Darwin.Contracts/**`

### Audience Segmentation

`Darwin.WebApi` should be treated as multiple logical API surfaces sharing one host:

- **public storefront surface** for CMS pages, menus, SEO metadata, product/category discovery, and anonymous commerce browsing
- **member surface** for authenticated customer/profile/order/invoice/loyalty operations used by `Darwin.Web`
- **business/mobile surface** for loyalty scanning, campaigns, subscriptions, and mobile-specific flows
- **admin/integration surface** only where HTTP delivery is explicitly required

Do not combine public storefront concerns and admin operational concerns inside the same contract shape unless there is a strong, documented reason.

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
| Billing | `GET /api/v1/billing/business/subscription/current` | `perm:AccessLoyaltyBusiness` | Business |
| Billing | `GET /api/v1/billing/plans` | `perm:AccessLoyaltyBusiness` | Business |
| Billing | `POST /api/v1/billing/business/subscription/cancel-at-period-end` | `perm:AccessLoyaltyBusiness` | Business |
| Billing | `POST /api/v1/billing/business/subscription/checkout-intent` | `perm:AccessLoyaltyBusiness` | Business |

---

## 4.1 Front-Office API Direction

The front-office (`Darwin.Web`) is a separate Next.js application and should consume API-friendly contracts rather than back-office MVC models.

Required design rules:

- CMS content must be deliverable through HTTP contracts.
- Public storefront DTOs must be presentation-oriented.
- Member/account DTOs must be separate from admin DTOs.
- Endpoint grouping should remain clear by audience.

Typical front-office API groups include:

- CMS pages and structured content
- navigation menus
- SEO metadata
- product/category/catalog browsing
- customer account/profile
- loyalty balances and rewards
- invoices and order history

When concrete storefront endpoints are introduced, document them in this file with the same level of detail as the mobile-facing matrix above.

### BFF Readiness

The API design should remain compatible with a future Backend-for-Frontend layer for:

- authentication/session handling
- response composition
- caching
- reducing chatty front-end API traffic

This does not require immediate implementation, but current endpoint and contract design must not make that pattern difficult later.

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

## 7) Inactive Reminder Gateway Contract & Remediation

Inactive reminder delivery is executed by the WebApi background worker and dispatched through the configurable HTTP gateway defined under `Notifications:InactiveReminderPushGateway`.

### 7.1 Gateway request shape

The dispatcher now sends a provider-ready payload with:

- user/device targeting: `userId`, `deviceId`, `pushToken`
- routing hints: `platform`, normalized `provider` (`Fcm` / `Apns`)
- notification content: `title`, `body`, `inactiveDays`
- native-delivery hints: `androidChannelId`, `apnsTopic`, `collapseKey`, `analyticsLabel`, `deepLinkUrl`

This keeps Darwin.WebApi provider-agnostic while still giving the downstream gateway enough context to fan out directly to FCM/APNs without guessing bundle ids, Android channels, or reminder tap destinations.

### 7.2 Failure taxonomy

Stable failure codes are persisted into engagement metadata and surfaced in worker logs. Current families:

| Family | Examples | Typical remediation |
|---|---|---|
| Validation | `Validation.PushTokenRequired`, `Validation.DestinationDeviceIdRequired` | Inspect mobile registration payloads and user/device snapshot integrity. |
| Gateway HTTP/auth | `Gateway.Unauthorized`, `Gateway.Forbidden`, `Gateway.EndpointNotFound`, `Gateway.ServerError` | Verify gateway URL, auth token, deployment routing, and service health. |
| Gateway transport | `Gateway.Timeout`, `Gateway.TransportError`, `Gateway.RateLimited` | Check network path, transient provider availability, and retry budgets. |
| Provider-native FCM | `Gateway.Provider.Fcm.TokenUnregistered`, `Gateway.Provider.Fcm.SenderIdMismatch`, `Gateway.Provider.Fcm.QuotaExceeded` | Remove stale tokens, confirm Firebase project alignment, and inspect rate limiting. |
| Provider-native APNs | `Gateway.Provider.Apns.TokenInvalid`, `Gateway.Provider.Apns.TopicInvalid`, `Gateway.Provider.Apns.AuthTokenInvalid` | Verify APNs topic/bundle id, token validity, and signing credentials. |

### 7.3 Worker observability

`InactiveReminderBackgroundService` logs:

- evaluated / dispatched / suppressed / failed counters
- split suppression counters (`CooldownActive` vs `NoPushDestination`)
- aggregate failure and suppression breakdown strings for remediation playbooks
- warning events when failure-rate or cooldown-suppression thresholds exceed configured percentages

Operational guidance:

1. Treat `Gateway.Provider.*Token*` as destination hygiene work (remove stale tokens / re-register device).
2. Treat `Gateway.Provider.*Auth*` and `Gateway.Unauthorized` as credential or topic drift.
3. Treat `Gateway.RateLimited`, `Gateway.Provider.Fcm.QuotaExceeded`, and `Gateway.Provider.Apns.RateLimited` as retry/backoff pressure.
4. Treat `CooldownActive` spikes as expected policy behavior unless the share is unexpectedly high for the campaign window.

---

## 8) Postman Verification Playbook

### 8.1 Login (consumer)

- `POST /api/v1/auth/login`
- save `accessToken` / `refreshToken`.

### 8.2 Discover list

- `POST /api/v1/businesses/list`
- expected: seeded businesses in `items`.

### 8.3 Business detail

- `GET /api/v1/businesses/{businessId}`

### 8.4 Join loyalty

- `POST /api/v1/loyalty/account/{businessId}/join`

### 8.5 Prepare scan session

- `POST /api/v1/loyalty/scan/prepare`
- expected: session token + expiry.

### 8.6 Business-side process

- login as business account
- `POST /api/v1/loyalty/scan/process`

> Keep concrete payload examples in your team Postman collection/environment and update alongside Contracts changes.

---

## 9) Documentation Ownership

When API contracts or policies change, update these together in one PR:

1. `Darwin.Contracts`
2. `Darwin.Application` mapping/handler logic
3. `Darwin.WebApi` controller mapping
4. `DarwinWebApi.md`
5. related mobile docs (`DarwinMobile.md`) only for app-side impact summaries
