# Darwin Mobile Suite & Contracts — Technical Guide (Part 1/2)

> Scope: **Mobile** projects and the **Darwin.Contracts** package only. This guide enables a new contributor to continue the mobile work without needing any prior chat context.

---

## 0) Executive Summary

Darwin’s mobile suite consists of two .NET MAUI apps:

- **Darwin.Mobile.Consumer** – end-user app: login, personal rotating QR code, discover businesses on a map, view/earn/redeem rewards, profile.
- **Darwin.Mobile.Business** – business tablet app: scan consumer QR, confirm identity, add points, redeem rewards, view customer snapshots.

Both apps talk to **Darwin.WebApi** via a thin **Darwin.Mobile.Shared** library (HTTP client, retry, auth helpers, abstractions for camera/location) and a stable **Darwin.Contracts** package (DTOs used by WebApi + both apps). The back end (Web/Infrastructure) already exposes composition modules for DB, identity, JWT, SMTP, and data-protection; WebApi will compose those and implement endpoints that match the Contracts. 

---

## 1) Projects & Roles

### 1.1 Darwin.Mobile.Consumer (MAUI)
- Targets iOS/Android (optionally Windows/MacCatalyst for debugging).
- Core features: Auth, QR (rotating token bound to the current user), Discover (map + directory), Rewards dashboard, Profile.
- UI binds to **Shared** services (Auth/Loyalty/Business) and uses platform services (camera, location). Initial view models and placeholder views exist and are meant to be wired to Shared soon. :contentReference[oaicite:1]{index=1}

### 1.2 Darwin.Mobile.Business (MAUI)
- Targets tablet-class devices.
- Core features (Phase 1): scan QR → show customer snapshot → +1 point (configurable) → redeem reward.
- Uses the same Shared services; provides the scanner implementation for the device.

### 1.3 Darwin.Mobile.Shared (Class Library)
- **ApiClient** with JSON (System.Text.Json), bearer handling, and **retry** via an `IRetryPolicy` abstraction (exponential backoff with jitter). Registering via `AddHttpClient<IApiClient, ApiClient>` is already wired in DI. 
- **Abstractions** for camera scanning and geolocation (`IScanner`, `ILocation`) to keep UI projects platform-specific. :contentReference[oaicite:3]{index=3}
- **Auth & Loyalty helpers** (TokenStore, QrTokenRefresher, service registrations). QrTokenRefresher raises an event when a new token is fetched; apps can subscribe and update the bound QR immediately. :contentReference[oaicite:4]{index=4}
- **DI composition** entry: `AddDarwinMobileShared(ApiOptions)` registers ApiClient, retry policy, TokenStore, and service facades. Requires **Microsoft.Extensions.Http** for `AddHttpClient`. :contentReference[oaicite:5]{index=5}

### 1.4 Darwin.Contracts (Class Library)
- **Single source of truth** for WebApi request/response models used by both mobile apps. It purposely contains **no** EF types or server concerns.
- Current/added areas:
  - **Identity**: login, token pair, refresh. 
  - **Loyalty**: start scan, accrue, redeem; reward programs; customer account snapshot.
  - **Businesses**: discovery filters, map summaries, business details.
  - **Common**: paging, geo points, problem details, sort options (reuses the existing `SortOption`). :contentReference[oaicite:6]{index=6}

---

## 2) Solution Structure & Solution Filters

- Keep mobile and web in **one repo**, but use two **Solution Filters**:
  - `Darwin.WebOnly.slnf`: includes Darwin.Web, Darwin.WebApi, Darwin.Infrastructure, Darwin.Application, Darwin.Domain, Darwin.Contracts.
  - `Darwin.MobileOnly.slnf`: includes Darwin.Mobile.Consumer, Darwin.Mobile.Business, Darwin.Mobile.Shared, Darwin.Contracts.
- Rationale: faster IDE load, focused builds; still a single codebase and PR/CI surface.

---

## 3) Back-end Composition (for reference)

Web/WebApi will consume Infrastructure composition modules:

- `AddPersistence(configuration)` for EF Core + migrations + seeding. 
- `AddIdentityInfrastructure()` for Argon2, WebAuthn, TOTP, secret-protection; `AddJwtAuthCore()` for JWT issuing + login rate limiting. :contentReference[oaicite:8]{index=8}
- `AddSharedHostingDataProtection(configuration)` for key rings (token/encryption) and `AddNotificationsInfrastructure(configuration)` for SMTP. 

> Why this matters to mobile? Token issuance/refresh, short-lived QR token endpoints, and secure storage assumptions derive from this composition.

---

## 4) Contracts (DTOs) Overview

> These DTOs are designed for **System.Text.Json** with default web naming, compatible with our Shared ApiClient. They avoid exposing internal IDs where not needed.

### 4.1 Common
- `PagingRequest`, `PagedResponse<T>` – uniform paging across discovery lists.  
- `GeoPointDto` – decimal degrees for pins/proximity.  
- `ApiProblem` – RFC 7807-like minimal error envelope.  
- `SortOption` – **already exists** and is reused by discovery. :contentReference[oaicite:10]{index=10}

### 4.2 Identity
- `PasswordLoginRequest` → `TokenResponse` (Access/Refresh pair), `RefreshTokenRequest`.  
  Access is short-lived (JWT); Refresh is opaque and longer-lived per server policy. JWT plumbing is provided in Infrastructure. :contentReference[oaicite:11]{index=11}

### 4.3 Loyalty
- `QrCodePayloadDto` (already present) – **rotating** short-lived QR token payload rendered by Consumer.  
- Business flow:
  - `StartScanRequest` (token) → `StartScanResponse` (scan session, customer display name, current points, next reward).  
  - `AccruePointsRequest` → `AccruePointsResponse`.  
  - `RedeemRewardRequest` → `RedeemRewardResponse`.  
- Reward programming:
  - `RewardProgramDto` with `RewardTierDto`.  
  - `LoyaltyAccountDto` (consumer’s per-business status).  
  These complete the Phase-1/2 surface while remaining stable for future extensions. :contentReference[oaicite:12]{index=12}

### 4.4 Businesses (Discovery)
- `BusinessDiscoveryFilter` – query, category, near/max-distance, open-now, sort, paging.  
- `BusinessSummaryDto` – id, name, category, rating, approximate location, open-now.  
- `BusinessDetailDto` – description, opening hours, phones/links, address line, images, reward program preview.  
These are sufficient for a map + directory + profile page. :contentReference[oaicite:13]{index=13}

---

## 5) Mobile Shared Library Details

### 5.1 HTTP + Retry
- `ApiClient` sets `HttpClient.BaseAddress` from `ApiOptions.BaseUrl`, serializes with `JsonSerializerDefaults.Web`, and **wraps all calls in `IRetryPolicy`**. Default policy is exponential backoff with jitter; retries `HttpRequestException` and timeouts only. Keep attempts **small** to protect UX/battery. 
- DI registration (`AddDarwinMobileShared`) wires `AddHttpClient<IApiClient, ApiClient>()` and default timeout (15s). Install **Microsoft.Extensions.Http** in projects that host the DI container. :contentReference[oaicite:15]{index=15}

### 5.2 Auth Storage & QR Refresh
- `ITokenStore` – abstraction over secure storage; mobile apps provide concrete platform implementation or use Essentials’ SecureStorage.  
- `QrTokenRefresher` – background loop that pulls a fresh QR token at intervals (from `ApiOptions.QrRefreshSeconds` or a server “bootstrap”). Emits `TokenRefreshed` event so the UI can update the QR immediately. :contentReference[oaicite:16]{index=16}

### 5.3 Integration Abstractions
- `IScanner` – camera barcode reader; implement per app (Business tablet, optionally Consumer for demos).  
- `ILocation` – geo provider; used to implement “Near me” filters. :contentReference[oaicite:17]{index=17}

---

## 6) End-to-End QR/Scan Flow (Security-aware)

**Consumer app**
1. User logs in → receives `TokenResponse` (access/refresh). Access is stored via `ITokenStore`; ApiClient bearer is set.  
2. Consumer requests `GET /loyalty/qr` → server returns **short-lived** `QrCodePayloadDto`.  
3. Consumer displays QR and optionally starts `QrTokenRefresher` to rotate the token periodically (e.g., 60s). :contentReference[oaicite:18]{index=18}

**Business app**
1. Cashier taps “Scan” → `IScanner.ScanAsync` yields the QR token string. :contentReference[oaicite:19]{index=19}  
2. App calls `POST /loyalty/start-scan` with the token → gets `StartScanResponse` (includes an **ephemeral** `ScanSessionId`).  
3. Cashier confirms the customer and:
   - Accrues points: `POST /loyalty/accrue` with `ScanSessionId` (+optional amount).  
   - Or redeems a reward: `POST /loyalty/redeem` with `ScanSessionId` and `RewardTierId`.  
4. Server updates balances and returns confirmations; Consumer app will see updated points after next pull (or via signal/polling later).

**Security notes**
- The QR payload **must not** embed internal user IDs; it’s a short-lived opaque token bound to the authenticated user, validated server-side, and exchanges for a time-boxed `ScanSessionId`.  
- Short lifetimes reduce replay risk; rotation via QrTokenRefresher cuts the attack window. Rate limiting and IP/device telemetry may be added server-side (login limiter already exists). :contentReference[oaicite:20]{index=20}

---

## 7) Configuration & Environments

### 7.1 Mobile
- `ApiOptions` in each app at composition time:
  - `BaseUrl`: WebApi root (e.g., `https://api.example.com/`).  
  - `JwtAudience`: as expected by WebApi JWT validation.  
  - `QrRefreshSeconds`: recommended rotation interval. :contentReference[oaicite:21]{index=21}
- Provide environment-specific `ApiOptions` via platform config (e.g., MAUI `appsettings.mobile.json` + build transforms) or compile-time constants; pass them into `AddDarwinMobileShared()` during startup.

### 7.2 Server
- DataProtection key ring path, SMTP, and WebAuthn data live in appsettings; Infrastructure exposes composition helpers already listed. See the repo README for current examples. :contentReference[oaicite:22]{index=22}

---

## 8) Error Handling

- WebApi should standardize on `ApiProblem` responses with field errors for validation.  
- The Shared ApiClient currently surfaces non-2xx as exceptions (via `EnsureSuccessStatusCode`); in UI, catch and map to friendly messages using `ApiProblem` if the body is present. :contentReference[oaicite:23]{index=23}

---

## 9) Versioning & Compatibility

- **Contracts** should be versioned semantically (e.g., `v1`). Breaking changes require either new fields with defaults, new endpoints, or a `v2` namespace/route.  
- Mobile apps pin to a Contracts package version; CI should run e2e contract tests to prevent accidental breaks.

---

## 10) Minimal App Startup (Sketch)

Each mobile app’s DI should call:

```csharp
// in <App>.Composition/DependencyInjection.cs
services.AddDarwinMobileShared(new ApiOptions
{
    BaseUrl = "https://api.example.com/",
    JwtAudience = "Darwin.PublicApi",
    QrRefreshSeconds = 60
});
```



# Darwin Mobile Suite & Contracts — Technical Guide (Part 2/2)

## 11) Mobile App Responsibilities

### Consumer
- **Login**: `POST /identity/login` → `TokenResponse`; store via `ITokenStore`.
- **QR**: show rotating token (subscribe to `QrTokenRefresher.TokenRefreshed`).
- **Rewards**: list per-business points with `LoyaltyAccountDto`.
- **Discover**: call discovery endpoints with `BusinessDiscoveryFilter` → paged `BusinessSummaryDto`.
- **Details**: fetch `BusinessDetailDto` including public reward preview.
- **Profile**: fetch/edit using `CustomerProfileDto` / `CustomerProfileEditDto`.

### Business
- **Scan**: `IScanner.ScanAsync` → `StartScanRequest` → `StartScanResponse`.
- **Accrue**: `AccruePointsRequest` → `AccruePointsResponse`.
- **Redeem**: `RedeemRewardRequest` → `RedeemRewardResponse`.
- **Customer Snapshot**: use response fields to verbally confirm identity before accrual/redeem.

---

## 12) Offline & Resilience Notes

- Current Shared client includes **retry** for transient network faults; keep attempts small. For heavier offline needs (future phases), introduce a local outbox with `ApiOptions.MaxOutbox` as a tuning knob; reconcile on connectivity restore. :contentReference[oaicite:25]{index=25}
- Avoid long-running loops; `QrTokenRefresher` is already cooperative via cancellation. :contentReference[oaicite:26]{index=26}

---

## 13) Security Posture (Mobile Perspective)

- **No internal IDs in QR**; only short-lived opaque token → exchange for server `ScanSessionId`.  
- **JWT & refresh**: short TTL access token; refresh via secure storage (`ITokenStore`).  
- **Rate limiting**: server login rate limiter is present; extend per endpoint as needed. :contentReference[oaicite:27]{index=27}
- **Data Protection**: server key ring persists across restarts; required for stable token protection. :contentReference[oaicite:28]{index=28}

---

## 14) Current Code Pointers

- **Shared HTTP & Retry**: `Api/ApiClient.cs`, `Resilience/IRetryPolicy.cs`, `Resilience/ExponentialBackoffRetryPolicy.cs`.   
- **Shared DI**: `Extensions/ServiceCollectionExtensions.cs` (requires `Microsoft.Extensions.Http`). :contentReference[oaicite:30]{index=30}  
- **Scanner & Location Abstractions**: `Integration/IScanner.cs`, `Integration/ILocation.cs`. :contentReference[oaicite:31]{index=31}  
- **QR Rotation Helper**: `Security/QrTokenRefresher.cs`. :contentReference[oaicite:32]{index=32}  
- **Consumer placeholders**: `ViewModels/QrViewModel.cs` (+ TODO), `Views/QrView.xaml.cs`. :contentReference[oaicite:33]{index=33}  
- **Server composition**: Infrastructure `ServiceCollectionExtensions.*` (persistence, identity/JWT, notifications, data-protection).   
- **Repo README (config examples)**: connection strings, DataProtection, SMTP, WebAuthn. :contentReference[oaicite:35]{index=35}

---

## 15) Build & Tooling

- **Target SDK**: .NET 9.  
- **Packages (apps)**: `Microsoft.Extensions.Http` (for DI `AddHttpClient`), camera/QR packages per platform (to be selected in each MAUI app), Essentials for secure storage.  
- **Migrations**: run under Infrastructure project; Web/WebApi composes Db + seeders. :contentReference[oaicite:36]{index=36}

---

## 16) Testing Considerations

- For pure DTO/serialization tests, reference **Darwin.Contracts** directly.  
- For handler/UI integration, use mock `IApiClient` or a test server.  
- Note: Application/test infra currently uses EF InMemory helpers; prefer SQLite-in-memory when relational behavior matters. :contentReference[oaicite:37]{index=37}

---

## 17) Roadmap Coupling (Phases 1–3)

- **Phase 1**: Endpoints needed are present in Contracts: login, QR fetch, start-scan, accrue, redeem, reward program/read models, discovery basics, profile.  
- **Phase 2**: Map discovery with filters & details (Contracts covered).  
- **Phase 3**: Subscriptions/analytics/notifications—additive endpoints; do **not** break v1 Contracts.

---

## 18) Contributor Checklist

1. Install SDKs (MAUI workloads for target platforms).  
2. Create/update **Solution Filters** as above.  
3. Add `Microsoft.Extensions.Http` to mobile host projects to satisfy `AddHttpClient`. :contentReference[oaicite:38]{index=38}  
4. Wire `AddDarwinMobileShared(ApiOptions)` in each app’s composition.  
5. Implement platform services: `IScanner`, `ILocation`, `ITokenStore`.  
6. Bind QR page to `QrTokenRefresher` and render the current token string as a QR image.  
7. Implement discovery screens using Contracts filters and paged responses.  
8. Handle `ApiProblem` uniformly; surface friendly UX.

---

## 19) Appendix — Contracts (Snapshot)

> The following modules are present/added in `Darwin.Contracts`:

- **Common**: `PagingRequest`, `PagedResponse<T>`, `GeoPointDto`, `ApiProblem`, `SortOption`. :contentReference[oaicite:39]{index=39}  
- **Identity**: `PasswordLoginRequest`, `TokenResponse`, `RefreshTokenRequest`.  
- **Loyalty**: `QrCodePayloadDto`, `StartScan*`, `Accrue*`, `Redeem*`, `RewardProgramDto`, `RewardTierDto`, `LoyaltyAccountDto`.  
- **Businesses**: `BusinessDiscoveryFilter`, `BusinessDiscoveryResponse`, `BusinessSummaryDto`, `BusinessDetailDto`.

> Rationale: Keep WebApi and both mobile apps in lock-step on a stable, server-agnostic schema. All server EF/domain mapping stays private in Application/Infrastructure.

---