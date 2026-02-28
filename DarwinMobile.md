# Darwin Mobile Suite & Contracts — Technical Guide

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Visual Studio 2026](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/)


> Scope: **Mobile** projects and the **Darwin.Contracts** package only. This guide enables a new contributor to continue the mobile work without needing any prior chat context.

---

## 0) Executive Summary

Darwin’s mobile suite consists of two .NET MAUI apps that sit on top of the existing Darwin platform:

- **Darwin.Mobile.Consumer** – end-user app: login, personal **QR based on a server-side scan session**, discover businesses on a map, view/earn/redeem rewards, profile.
- **Darwin.Mobile.Business** – business tablet app: **scan the customer’s QR**, load the current **scan session** from the server, then **accrue points** or **confirm a redemption**.

Key principles for the loyalty flow:

- The QR code **only contains an opaque ScanSessionToken** (string).
- All sensitive data (customer identity, rewards, balances, business rules) lives on the server.
- The **Consumer app** calls `PrepareScanSession` to create a short-lived scan session (Mode = Accrual or Redemption) and renders the returned `ScanSessionToken` as QR.
- The **Business app** scans the QR and calls `ProcessScanSessionForBusiness` to load the session (Mode + summary + selected rewards), then calls `ConfirmAccrual` or `ConfirmRedemption` to finalize.
- The same Contracts (`Darwin.Contracts`) are used by WebApi and both apps.

---

## 1) Projects & Roles

### 1.1 Darwin.Mobile.Consumer (MAUI)

- Targets iOS/Android (optionally Windows/MacCatalyst for debugging).
- Core features:
  - Authentication (JWT access + refresh tokens).
  - **Loyalty QR page**: shows a QR built from a server-issued `ScanSessionToken`.
  - Rewards dashboard per business.
  - Business discovery (map + directory) and business detail.
  - Profile (read/update with optimistic concurrency via `RowVersion`).
- UI binds to **Shared** services (`ILoyaltyService`, `IAuthService`, `IProfileService`, `IBusinessService`) and uses platform services (camera, location) via abstractions in `Darwin.Mobile.Shared`.

### 1.2 Darwin.Mobile.Business (MAUI)

- Targets tablet-class devices (Android/iPad; Windows/Mac for debugging if needed).
- Phase-1 core features:
  - **Scan** customer QR with the device camera.
  - Call `ProcessScanSessionForBusiness` to load the session:
    - Mode = Accrual → show account summary and allow point accrual.
    - Mode = Redemption → show selected rewards for confirmation.
  - Call `ConfirmAccrual` / `ConfirmRedemption` to complete the operation.
- Uses the same Shared services; provides the scanner implementation for the device.

### 1.3 Darwin.Mobile.Shared (Class Library)

- **ApiClient** with JSON (System.Text.Json), bearer handling, and **retry** via an `IRetryPolicy` abstraction (exponential backoff with jitter).
- **Abstractions** for camera scanning and geolocation (`IScanner`, `ILocation`) to keep UI projects platform-specific.
- **Auth/Profile/Loyalty/Discovery facades**:
  - `IAuthService` for login/refresh/logout/register/change-password/forgot-password/reset-password.
  - `IProfileService` for `GET/PUT /api/v1/profile/me` flows.
  - `ILoyaltyService` for session-based scan flows and loyalty accounts.
  - `IBusinessService` for map/list/detail discovery calls.
  - `ITokenStore` abstraction over secure storage.
- **DI composition** entry:
  - `AddDarwinMobileShared(ApiOptions)` registers ApiClient, retry policy, TokenStore, and service facades. Requires **Microsoft.Extensions.Http** for `AddHttpClient`.

### 1.4 Darwin.Contracts (Class Library)

- **Single source of truth** for WebApi request/response models used by both mobile apps and Web.
- Contains **no** EF types or server implementation details.
- Current/added areas:
  - **Identity**: login, refresh, logout, logout-all, register, change-password, request-reset, reset-password.
  - **Profile**: member self profile read/update models (`RowVersion` based).
  - **Loyalty** (session-based scan model; see §4.3).
  - **Businesses**: discovery filters, map summaries, business details.
  - **Common**: paging, geo coordinates, problem details, sort options.

---

## 2) Solution Structure & Solution Filters

- Keep mobile and web in **one repo**, but use two **Solution Filters**:
  - `Darwin.WebOnly.slnf`: Darwin.Web, Darwin.WebApi, Darwin.Infrastructure, Darwin.Application, Darwin.Domain, Darwin.Contracts.
  - `Darwin.MobileOnly.slnf`: Darwin.Mobile.Consumer, Darwin.Mobile.Business, Darwin.Mobile.Shared, Darwin.Contracts.
- Rationale: faster IDE load and focused builds while preserving a single codebase and shared Contracts.

---

## 3) Back-end Composition (for reference)

Web/WebApi compose Infrastructure modules:

- `AddPersistence(configuration)` for EF Core + migrations + seeding.
- `AddIdentityInfrastructure()` for Argon2, WebAuthn, TOTP, secret-protection; `AddJwtAuthCore()` for JWT issuing + login rate limiting.
- `AddSharedHostingDataProtection(configuration)` for key rings (token/encryption) and `AddNotificationsInfrastructure(configuration)` for SMTP.

> Why this matters to mobile? Token issuance/refresh, **session-based loyalty endpoints**, and secure storage assumptions derive from this composition.

---

## 4) Contracts Overview

> Contracts are designed for **System.Text.Json** with default web naming, compatible with the Shared ApiClient. They avoid exposing internal IDs where not needed and keep the QR payload opaque.

### 4.1 Common

- `PagingRequest`, `PagedResponse<T>` – uniform paging across discovery lists.
- `GeoCoordinateModel` – decimal degrees for pins/proximity.
- `ApiProblem` – RFC 7807-like minimal error envelope.
- `SortOption` – already exists and is reused by discovery.

### 4.2 Identity

- `PasswordLoginRequest` → `TokenResponse` (Access/Refresh pair), `RefreshTokenRequest`.
  - Access is short-lived (JWT); Refresh is opaque and longer-lived per server policy.

### 4.3 Loyalty (Session-based Scan Model)

Located under `Darwin.Contracts.Loyalty`:

- **Enums / flags**
  - `LoyaltyScanMode` – `Accrual` or `Redemption`.
  - `LoyaltyScanAllowedActions` – flags for what the Business app is allowed to do (e.g. `CanConfirmRedemption`, `CanAccruePoints`).

- **Read models**
  - `LoyaltyRewardSummary` – compact reward model for both apps:
    - `Id`, `BusinessId`, `Name`, `Description`, `RequiredPoints`, `IsActive`, `IsSelectable`.
  - `LoyaltyAccountSummary` – snapshot for the current consumer at a business:
    - Non-personal alias (not real name/email), `CurrentPoints`, `NextReward`, etc.

- **Scan session creation (Consumer app)**
  - `PrepareScanSessionRequest`:
    - `BusinessId` – where the user is visiting.
    - `Mode` – `LoyaltyScanMode.Accrual` or `Redemption`.
    - `SelectedRewardIds` – optional list of reward IDs (for Redemption).
  - `PrepareScanSessionResponse`:
    - `ScanSessionToken` – opaque string encoded into the QR.
    - `Mode` – confirmed mode.
    - `ExpireAtUtc` – server-side expiry of the scan session.
    - `SelectedRewards` – optional list of `LoyaltyRewardSummary` for confirmation on the Consumer side.

- **Scan session processing (Business app)**
  - `ProcessScanSessionForBusinessRequest`:
    - `ScanSessionToken` – scanned from the consumer’s QR.
  - `ProcessScanSessionForBusinessResponse`:
    - `Mode` – `Accrual` / `Redemption`.
    - `BusinessId` – must match the logged-in business (validated server-side).
    - `AccountSummary` – optional `LoyaltyAccountSummary`.
    - `SelectedRewards` – optional list of `LoyaltyRewardSummary`.
    - `AllowedActions` – `LoyaltyScanAllowedActions` flags.

- **Accrual confirmation (Business app)**
  - `ConfirmAccrualRequest`:
    - `ScanSessionToken`.
    - `Points` or `Amount` depending on current business rules.
  - `ConfirmAccrualResponse`:
    - `Success`, `NewBalance`, `ErrorCode`, `ErrorMessage` (optional).

- **Redemption confirmation (Business app)**
  - `ConfirmRedemptionRequest`:
    - `ScanSessionToken` (in Phase 1 we confirm the rewards already stored in the session).
  - `ConfirmRedemptionResponse`:
    - `Success`, `NewBalance`, `ErrorCode`, `ErrorMessage` (optional).

These Contracts support Phase-1/2 of the mobile roadmap while being extendable (e.g. extra modes, more summary data) without breaking existing clients.

### 4.4 Businesses (Discovery)

- `BusinessDiscoveryFilter` – query, category, near/max-distance, open-now, sort, paging.
- `BusinessSummary` – id, name, category, rating, approximate location, open-now.
- `BusinessDetail` – description, opening hours, phones/links, address, images, loyalty program preview.

These are sufficient for the map, directory and profile pages on Consumer.

---

## 5) Mobile Shared Library Details

### 5.1 HTTP + Retry

- `ApiClient` sets `HttpClient.BaseAddress` from `ApiOptions.BaseUrl`, serializes with `JsonSerializerDefaults.Web`, and wraps calls in `IRetryPolicy`.
- Default policy: exponential backoff with jitter; retries `HttpRequestException` and timeouts only. Keep attempts small to protect UX/battery.
- DI registration (`AddDarwinMobileShared`) wires `AddHttpClient<IApiClient, ApiClient>()` and default timeout (e.g. 15s). `Microsoft.Extensions.Http` must be referenced in the host project.

### 5.2 Auth Storage

- `ITokenStore` – abstraction over secure storage; mobile apps provide concrete implementations (e.g. Essentials’ `SecureStorage`).
- `IAuthService` – wraps Contracts for auth flows and integrates `ITokenStore` with `IApiClient` (Bearer handling).
- `IProfileService` – wraps member profile endpoints and handles update payload shape (`Id` + `RowVersion`).
- **Consumer QR auto-refresh policy (current app behavior)**: automatic refresh checks run every ~15 seconds, and the app enforces a **minimum 5-minute interval** between automatic refresh calls when the token is still valid. Configuration lives in `Darwin.Mobile.Consumer/ViewModels/QrViewModel.cs` (`MinimumAutoRotationInterval`, `RotationCheckInterval`, `RotationRenewThreshold`) so this value can be changed centrally later.

### 5.3 Integration Abstractions

- `IScanner` – camera barcode/QR reader; implemented per app (Business tablet, optionally Consumer for demos).
- `ILocation` – geo provider; used to implement “near me” filters.

---

## 6) End-to-End QR/Scan Flow (Security-aware)

### Consumer app

1. User logs in → `POST /identity/login` → `TokenResponse`.  
   Access token is stored through `ITokenStore`; ApiClient bearer is set.

2. To show QR in **Accrual** mode:
   - Consumer calls `POST /api/v1/loyalty/scan/prepare` with `PrepareScanSessionRequest { BusinessId, Mode = Accrual }`.
   - Server returns `PrepareScanSessionResponse` with `ScanSessionToken` and `ExpireAtUtc`.
   - Consumer renders the QR using `ScanSessionToken`.

3. To show QR in **Redemption** mode:
   - Consumer queries available rewards for the business and current balance.
   - User selects one or more rewards.
   - Consumer calls `POST /api/v1/loyalty/scan/prepare` with `Mode = Redemption` and `SelectedRewardIds`.
   - Server validates that points are sufficient and returns `ScanSessionToken` + `SelectedRewards`.
   - Consumer refreshes the QR with the new token.

4. If the session expires before being scanned, WebApi returns an error; the app should call `POST /api/v1/loyalty/scan/prepare` again and show a new QR.

### Business app

1. Cashier taps “Scan” → `IScanner.ScanAsync()` yields the token string.
2. App calls `POST /api/v1/loyalty/scan/process` with `ProcessScanSessionForBusinessRequest { ScanSessionToken }`.
3. WebApi:
   - Validates the token and business, loads mode and account summary.
   - Returns `ProcessScanSessionForBusinessResponse`:
     - Mode, `AccountSummary`, `SelectedRewards`, `AllowedActions`.

4. UI logic:
   - If `Mode = Accrual` and `AllowedActions` contains `CanAccruePoints`:
     - Show current balance, optionally ask for amount.
     - Call `POST /api/v1/loyalty/scan/confirm-accrual` with `ConfirmAccrualRequest { ScanSessionToken, Points }`.
   - If `Mode = Redemption` and `AllowedActions` contains `CanConfirmRedemption`:
     - Show list of `SelectedRewards`.
     - Cashier confirms usage → call `POST /api/v1/loyalty/scan/confirm-redemption` with `ConfirmRedemptionRequest { ScanSessionToken }`.

5. Server updates balances and returns confirmations; Consumer app will see updated points on next refresh (or via push/polling in later phases).

### Security notes

- QR payload contains **only `ScanSessionToken`**, not internal IDs.
- Tokens are **short-lived** and bound to both customer and business.
- Processing always goes through a server-side scan session, not directly against the loyalty account.
- Replay risk is limited by short expiry, single-use semantics, server-side validation and potential rate limiting.

---

## 7) Configuration & Environments

### 7.1 Mobile

`ApiOptions` is provided in each app at composition time:

- `BaseUrl`: WebApi root (e.g. `https://api.example.com/`).
- `JwtAudience`: as expected by WebApi JWT validation.
- Other tuning knobs (retry, timeouts, etc.) can be added as the Shared layer evolves.

Provide environment-specific `ApiOptions` via platform config (e.g. MAUI config class per build configuration) or compile-time constants; pass them into `AddDarwinMobileShared()` during startup.

### 7.2 Server

- DataProtection key ring path, SMTP, and WebAuthn settings live in appsettings.
- Infrastructure exposes composition helpers (`AddSharedHostingDataProtection`, `AddPersistence`, `AddIdentityInfrastructure`, `AddNotificationsInfrastructure`, `AddJwtAuthCore`).

---

## 8) Error Handling

- WebApi should standardize on `ApiProblem` responses with field errors for validation.
- Shared `ApiClient` surfaces non-2xx as exceptions; in UI, catch and map to friendly messages using `ApiProblem` if available.

---

## 9) Versioning & Compatibility

- Contracts should be versioned semantically (e.g. v1).  
- Breaking changes require new fields with defaults, new endpoints, or a `v2` namespace/route.
- Mobile apps pin to a specific Contracts package version; CI should run contract tests to detect breaking changes early.

---

## 10) Minimal App Startup (Sketch)

Each mobile app’s DI should call something like:

```csharp
// in <App>.Composition/DependencyInjection.cs
services.AddDarwinMobileShared(new ApiOptions
{
    BaseUrl = "https://api.example.com/",
    JwtAudience = "Darwin.PublicApi"
});
```

---

## 11) Mobile App Responsibilities

### Consumer

- **Login**: `PasswordLoginRequest` → `TokenResponse`; store via `ITokenStore` and configure `ApiClient`.
- **QR/Scan session**:
  - Call `PrepareScanSession` (Accrual or Redemption) via `ILoyaltyService.PrepareScanSessionAsync`.
  - Render the returned `ScanSessionToken` as QR.
- **Rewards**:
  - Fetch loyalty account + available rewards per business.
  - Enable selection of rewards for Redemption mode.
- **Discover**:
  - Call discovery endpoints with `BusinessDiscoveryFilter` → paged `BusinessSummary`.
  - Navigate to `BusinessDetail` for full info.
- **Profile**:
  - Fetch/edit using identity/profile Contracts as they evolve.

### Business

- **Scan**:
  - Use `IScanner.ScanAsync` to obtain the scanned token.
  - Call `ILoyaltyService.ProcessScanSessionForBusinessAsync` with that token.
- **Accrue**:
  - If Mode = Accrual and allowed, call `ILoyaltyService.ConfirmAccrualAsync`.
- **Redeem**:
  - If Mode = Redemption and allowed, call `ILoyaltyService.ConfirmRedemptionAsync`.
- **Customer Snapshot**:
  - Use `LoyaltyAccountSummary` and `SelectedRewards` to verbally confirm with the customer when needed.

---

## 12) Offline & Resilience Notes

- Shared client includes retry for transient network faults; keep attempts small.
- For heavier offline needs, introduce a local outbox in the apps and replay stored operations when connectivity is restored (future work).
- Avoid long-running loops; prefer one-shot operations with explicit user actions.

---

## 13) Security Posture (Mobile Perspective)

- **No internal IDs in QR**; only a short-lived opaque `ScanSessionToken` exchanged for a server-side scan session.
- **JWT & refresh**: short TTL access token; refresh via secure storage (`ITokenStore`).
- **Rate limiting**: login rate limiter already exists; per-endpoint throttling can be added on WebApi.
- **Data Protection**: server key ring persists across restarts; required for stable token and secret protection.

---

## 14) Current Code Pointers

- **Shared HTTP & Retry**: `Darwin.Mobile.Shared/Api/ApiClient.cs`, `Resilience/IRetryPolicy.cs`, `Resilience/ExponentialBackoffRetryPolicy.cs`.
- **Shared DI**: `Darwin.Mobile.Shared/Extensions/ServiceCollectionExtensions.cs` (requires `Microsoft.Extensions.Http`).
- **Scanner & Location Abstractions**: `Darwin.Mobile.Shared/Integration/IScanner.cs`, `Integration/ILocation.cs`.
- **Loyalty Service**: `Darwin.Mobile.Shared/Services/Loyalty/LoyaltyService.cs` (session-based APIs).
- **Consumer ViewModels**: `Darwin.Mobile.Consumer/ViewModels/QrViewModel.cs`, `RewardsViewModel.cs`.
- **Business ViewModels**: `Darwin.Mobile.Business/ViewModels/ScannerViewModel.cs`.
- **Server composition**: Infrastructure `ServiceCollectionExtensions.*` (persistence, identity/JWT, notifications, data-protection, JWT).
- **Repo README**: configuration examples for DataProtection, SMTP, WebAuthn, etc.

---

## 15) Build & Tooling

- **Target SDK**: .NET 10.
- **Packages (apps)**:
  - `Microsoft.Extensions.Http` for `AddHttpClient`.
  - Camera/QR packages per platform (to be selected in each MAUI app).
  - Essentials for secure storage.
- **Migrations**: run under Infrastructure project; Web/WebApi composes Db + seeders.

---

## 16) Testing Considerations

- For contract/serialization tests, reference `Darwin.Contracts` directly.
- For handler/UI integration, use a mock `IApiClient` or a test WebApi host.
- For relational tests, prefer SQLite in-memory over EF InMemory when behaviour matters.

---

## 17) Mobile API Matrix (Consumer/Business)

| Area      | Endpoint                                     | Policy                      | Client     |
|-----------|----------------------------------------------|-----------------------------|------------|
| Auth      | POST /api/v1/auth/login                      | AllowAnonymous              | Both       |
| Auth      | POST /api/v1/auth/refresh                    | AllowAnonymous              | Both       |
| Auth      | POST /api/v1/auth/logout                     | Authorize                   | Both       |
| Auth      | POST /api/v1/auth/logout-all                 | Authorize                   | Both       |
| Auth      | POST /api/v1/auth/register                   | AllowAnonymous              | Consumer   |
| Auth      | POST /api/v1/auth/password/request-reset     | AllowAnonymous              | Consumer   |
| Auth      | POST /api/v1/auth/password/reset             | AllowAnonymous              | Consumer   |
| Auth      | POST /api/v1/auth/password/change            | Authorize                   | Both       |
| Profile   | GET /api/v1/profile/me                       | perm:AccessMemberArea       | Consumer   |
| Profile   | PUT /api/v1/profile/me                       | perm:AccessMemberArea       | Consumer   |
| Loyalty   | POST /api/v1/loyalty/scan/prepare            | perm:AccessMemberArea       | Consumer   |
| Loyalty   | POST /api/v1/loyalty/scan/process            | perm:AccessLoyaltyBusiness  | Business   |
| Loyalty   | POST /api/v1/loyalty/scan/confirm-accrual    | perm:AccessLoyaltyBusiness  | Business   |
| Loyalty   | POST /api/v1/loyalty/scan/confirm-redemption | perm:AccessLoyaltyBusiness  | Business   |
| Loyalty   | GET /api/v1/loyalty/my/accounts              | perm:AccessMemberArea       | Consumer   |
| Loyalty   | GET /api/v1/loyalty/my/history/{businessId}  | perm:AccessMemberArea       | Consumer   |
| Loyalty   | GET /api/v1/loyalty/account/{businessId}     | perm:AccessMemberArea       | Consumer   |
| Loyalty   | GET /api/v1/loyalty/business/{id}/rewards    | perm:AccessMemberArea       | Consumer   |
| Loyalty   | GET /api/v1/loyalty/my/businesses            | perm:AccessMemberArea       | Consumer   |
| Loyalty   | POST /api/v1/loyalty/my/timeline             | perm:AccessMemberArea       | Consumer   |
| Discovery | POST /api/v1/businesses/list                 | AllowAnonymous              | Consumer   |
| Discovery | POST /api/v1/businesses/map                  | AllowAnonymous              | Consumer   |
| Discovery | GET /api/v1/businesses/{id}                  | AllowAnonymous              | Consumer   |
| Discovery | GET /api/v1/businesses/{id}/with-my-account  | perm:AccessMemberArea       | Consumer   |

---

## 18) Roadmap Coupling (Phases 1–3)

- **Phase 1**: Endpoints: login/refresh/logout, consumer register + forgot-password + reset-password, session-based loyalty scan (prepare/process/confirm), basic rewards read models, discovery basics, and profile read/update/change-password.
- **Phase 2**: Map discovery with filters & details, richer reward dashboard, feed/promo endpoints.
- **Phase 3**: Subscriptions/analytics/notifications; additive endpoints on top of existing Contracts.

---

## 19) Contributor Checklist

1. Install SDKs (MAUI workloads for target platforms).
2. Use the appropriate Solution Filter (`Darwin.MobileOnly.slnf`).
3. Add `Microsoft.Extensions.Http` to mobile host projects to satisfy `AddHttpClient`.
4. Wire `AddDarwinMobileShared(ApiOptions)` in each app’s composition.
5. Implement platform services: `IScanner`, `ILocation`, `ITokenStore`.
6. Bind Consumer QR page to `ILoyaltyService.PrepareScanSessionAsync` and render the returned `ScanSessionToken` as a QR image.
7. Implement scan/confirm flows in Business app using `ProcessScanSessionForBusinessAsync`, `ConfirmAccrualAsync`, `ConfirmRedemptionAsync`.
8. Handle `ApiProblem` uniformly and map server errors to friendly UX.

---

## 20) Appendix — Contracts (Snapshot)

> Modules present/added in `Darwin.Contracts`:

- **Common**: `PagedRequest`, `PagedResponse<T>`, `GeoCoordinateModel`, `ApiProblem`, `SortOption`.
- **Identity**: `PasswordLoginRequest`, `TokenResponse`, `RefreshTokenRequest`.
- **Loyalty**: `LoyaltyScanMode`, `LoyaltyScanAllowedActions`, `LoyaltyRewardSummary`, `LoyaltyAccountSummary`, `PrepareScanSessionRequest/Response`, `ProcessScanSessionForBusinessRequest/Response`, `ConfirmAccrualRequest/Response`, `ConfirmRedemptionRequest/Response`.
- **Businesses**: `BusinessDiscoveryFilter`, `BusinessDiscoveryResponse`, `BusinessSummary`, `BusinessDetail`.

> Rationale: keep WebApi and both mobile apps aligned on a stable, server-agnostic schema. All server EF/domain mapping stays private in Application/Infrastructure.

> Rationale: keep WebApi and both mobile apps aligned on a stable, server-agnostic schema. All server EF/domain mapping stays private in Application/Infrastructure.

---


## Mobile Discovery & QR Troubleshooting (Consumer + Business)

### Symptoms: Discover tab is empty after successful login

If login succeeds (for example `cons1@darwin.de`) but no businesses are shown in the Discover tab, validate in this order:

1. **Seed data exists**
   - `IdentitySeedSection` creates consumer users (`cons1..cons10`) and member-role assignments.
   - `BusinessesSeedSection` creates 10+ active businesses and primary locations.
   - `LoyaltySeedSection` creates active loyalty programs/accounts for seeded businesses.
2. **Consumer Discover page has a bound ViewModel**
   - `DiscoverPage` must receive `DiscoverViewModel` through DI and call `OnAppearingAsync()`.
   - Without this, no API call is triggered and list remains empty.
3. **Business context reaches QR page**
   - Join flow must navigate with `businessId` query parameter.
   - `QrPage` must parse `businessId` and call `QrViewModel.SetBusiness(...)` before session refresh.

### Expected end-to-end flow

1. Consumer logs in (`cons1@darwin.de` / seeded password).
2. Consumer sees business list in Discover.
3. Consumer opens a business detail and taps Join.
4. App prepares a scan session and opens QR tab with business-scoped token.
5. Business app scans the QR and calls `ProcessScanSessionForBusiness`.

---

## Postman verification guide (WebApi)

Use this flow to confirm whether the backend is healthy independently from the mobile app.

- **Important:** In this environment `JwtRequireDeviceBinding = true`, so `deviceId` must be sent on login and refresh calls.

### 1) Login and capture tokens

- **POST** `{{baseUrl}}/api/v1/auth/login`
- Body:

```json
{
  "email": "cons1@darwin.de",
  "password": "Consumer123!",
  "deviceId": "postman-consumer-device"
}
```

- Save `accessToken` and `refreshToken` from response.

### 2) List businesses (discover endpoint)

- **POST** `{{baseUrl}}/api/v1/businesses/list`
- Authorization: `Bearer {{accessToken}}`
- Body:

```json
{
  "page": 1,
  "pageSize": 20,
  "query": null,
  "city": null,
  "countryCode": null,
  "addressQuery": null,
  "categoryKindKey": null,
  "near": null,
  "radiusMeters": null
}
```

- Expected: `items` contains seeded businesses (e.g., Café Aurora, Bäckerei König, ...).

### 3) Get business detail

- **GET** `{{baseUrl}}/api/v1/businesses/{{businessId}}`
- Authorization: `Bearer {{accessToken}}`

### 4) Join loyalty program

- **POST** `{{baseUrl}}/api/v1/loyalty/account/{{businessId}}/join`
- Authorization: `Bearer {{accessToken}}`
- Body:

```json
{
  "businessLocationId": null
}
```

### 5) Prepare scan session (QR token)

- **POST** `{{baseUrl}}/api/v1/loyalty/scan/prepare`
- Authorization: `Bearer {{accessToken}}`
- Body:

```json
{
  "businessId": "{{businessId}}",
  "mode": "Accrual",
  "selectedRewardTierIds": [],
  "businessLocationId": null,
  "deviceId": "postman-consumer-device"
}
```

- Expected: response includes session token used by consumer QR.

### 6) Business-side processing test (requires business account token)

1. Login as business user (`biz1@darwin.de` / `Business123!`).
2. Call process endpoint with token from step 5.

- **POST** `{{baseUrl}}/api/v1/loyalty/scan/process`

```json
{
  "scanSessionToken": "{{scanSessionToken}}"
}
```

Then confirm accrual/redemption with corresponding endpoints.

> If Postman works but app does not, the issue is inside mobile UI/data-binding flow, not backend contracts or handlers.