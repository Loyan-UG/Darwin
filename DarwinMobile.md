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
  - Business discovery (full in-app map integration + directory) and business detail.
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
- Phase-2 increment delivered: business-side reward tier editing (load/create/update/delete) backed by loyalty reward-configuration APIs.
- Phase-2 increment delivered: business dashboard + lightweight reporting cards (sessions/accruals/redemptions/top customers/recent activity).
- Phase-2 increment delivered: staff role visibility + client-side permission guards for reward edit and redemption/accrual confirmations.
- Phase-3 increment delivered: subscription settings entry point with portal configuration diagnostics (missing/invalid/non-HTTPS/host-allowlist), HTTPS-validated launch, copy-url support, server-backed read-only subscription status snapshot, cancel-at-period-end toggle, available-plans + checkout flow, and subscription funnel telemetry/KPI counters in dashboard reporting.

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
  - **Loyalty** (session-based scan model + business reward-configuration contracts for tier CRUD; see §4.3).
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

- `BusinessDiscoveryFilter` – query, category, near/max-distance, open-now, min-rating, active-loyalty-only, sort, paging.
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
- During active integration testing, login view models in both mobile apps prefill QA credentials only for DEBUG builds to speed manual verification loops; non-DEBUG builds keep credentials empty. This remains a temporary exception and must be fully removed during final pre-release hardening before customer rollout.
- **Consumer QR auto-refresh policy (current app behavior)**: the UI countdown check runs every ~1 second for a smooth countdown, while the app enforces a **minimum 5-minute interval** between automatic network refresh calls when the token is still valid. Configuration lives in `Darwin.Mobile.Consumer/ViewModels/QrViewModel.cs` (`MinimumAutoRotationInterval`, `RotationCheckInterval`, `RotationRenewThreshold`) so this value can be changed centrally later.

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

For map providers:
- **Android (Google Maps)**: provide `GoogleMapsApiKey` via environment/CI secret and pass it to Android manifest placeholder (`googleMapsApiKey`). Avoid committing production API keys in `.csproj` or manifest files.
- **iOS/MacCatalyst (MapKit)**: no Google Maps key is required for built-in MAUI MapKit rendering, but location/privacy entries (e.g., `NSLocationWhenInUseUsageDescription`) must remain configured.


### 7.1.1 Android Google Maps key setup (required for Consumer map on Android)

The Consumer project reads map key through MSBuild property `GoogleMapsApiKey`, with fallback to env var `GOOGLE_MAPS_API_KEY`.

Where to set it:
- **Windows / PowerShell (current shell):** `<c>$env:GOOGLE_MAPS_API_KEY="YOUR_KEY"</c>`
- **macOS/Linux / bash (current shell):** `<c>export GOOGLE_MAPS_API_KEY="YOUR_KEY"</c>`
- **Alternative env var (Android-only pipelines):** `ANDROID_GOOGLE_MAPS_API_KEY`
- **CI/CD:** add secure variable named `GOOGLE_MAPS_API_KEY` (preferred) or `ANDROID_GOOGLE_MAPS_API_KEY`.
- **Local persistent MSBuild override (optional):** set `GoogleMapsApiKey` in `Directory.Build.props` outside source control.

The project now validates this key during Android build:
- **Debug/Dev build:** missing key => warning (build continues, map can fail at runtime).
- **Release build:** missing key => build error (prevents shipping a broken map config).


### 7.1.2 Push provider setup (FCM/APNs production integration)

Consumer app now resolves push tokens from native runtime providers:
- At startup, Android requests Android 13+ `POST_NOTIFICATIONS` permission to align user-consent state with registration payload, with one-time prompt persistence to avoid repeated dialogs after hard deny.
- **Android**: Firebase Cloud Messaging token (`Xamarin.Firebase.Messaging`, wired only for Android target in `.csproj`).
- **iOS/MacCatalyst**: APNs device token via `RegisterForRemoteNotifications` with explicit `CodesignEntitlements` binding to platform entitlements files.

Required setup per environment:
1. **Firebase project**
   - Create Android app entry with package id `com.loyan.darwin.mobile.consumer`.
   - Download `google-services.json` and place it at `src/Darwin.Mobile.Consumer/google-services.json` (not committed to source control for private environments).
   - Android Release builds now fail when `google-services.json` is missing (Debug warns only), to avoid shipping broken FCM configuration.

2. **Apple Push capability**
   - Enable Push Notifications capability in Apple Developer portal for the app identifier.
   - Ensure provisioning profile includes push entitlement.
   - Keep `aps-environment` entitlement aligned with target build (`development` vs `production`). Consumer project now uses separate entitlements for Debug vs Release to enforce this split.
3. **Runtime permissions**
   - Android 13+: grant `POST_NOTIFICATIONS` permission at runtime.
   - iOS/MacCatalyst: allow notifications in system prompt/settings.

Operational note:
- If permissions are denied, registration still upserts device metadata with `NotificationsEnabled = false`; token can be null until the user enables permissions.
- Legacy fallback config/noop providers are removed from the Consumer project to avoid environment drift in production builds.


### 7.1.3 Push operational readiness checklist (Dev / Staging / Production)

Use this checklist before promoting builds between environments.

| Check | Dev | Staging | Production | Notes |
|------|-----|---------|------------|-------|
| Android `google-services.json` present and mapped to correct Firebase project | [ ] | [ ] | [ ] | Release build fails when missing. |
| Android Firebase package name matches `com.loyan.darwin.mobile.consumer` | [ ] | [ ] | [ ] | Keep package id and Firebase app id aligned. |
| iOS/MacCatalyst provisioning profile includes Push capability | [ ] | [ ] | [ ] | Validate on Apple Developer portal and CI signing profile. |
| APNs entitlement environment is correct (`development` for Debug, `production` for Release) | [ ] | [ ] | [ ] | Project uses separate entitlements by configuration. |
| Runtime permission path verified on fresh install (allow + deny + hard deny) | [ ] | [ ] | [ ] | Confirm one-time prompt behavior and settings recovery path. |
| Profile push diagnostics labels verified (`permission`, `token availability`) | [ ] | [ ] | [ ] | Should reflect device runtime state without exposing raw token value. |
| Profile "Open notification settings" action verified | [ ] | [ ] | [ ] | Must route user to app/system settings on device. |
| Manual push registration sync returns success for authenticated user | [ ] | [ ] | [ ] | Check status text + last sync timestamp in Profile. |
| WebApi `/api/v1/notifications/devices/register` observed in logs | [ ] | [ ] | [ ] | Confirm no auth/validation failures in API logs. |
| Token rotation scenario validated (app reinstall or token refresh callback) | [ ] | [ ] | [ ] | Ensure backend receives updated token and old one is superseded. |

Escalation guidance:
- If diagnostics show `permission: disabled`, direct user to the in-app settings shortcut first.
- If diagnostics show `token: missing` while permission is enabled, validate Firebase/APNs credentials and registration callbacks.
- If sync fails with auth issues, validate access token freshness and retry after login refresh.

### 7.2 Server

- DataProtection key ring path, SMTP, and WebAuthn settings live in appsettings.
- Infrastructure exposes composition helpers (`AddSharedHostingDataProtection`, `AddPersistence`, `AddIdentityInfrastructure`, `AddNotificationsInfrastructure`, `AddJwtAuthCore`).
- Password-reset emails are sent through `IEmailSender` (`SmtpEmailSender` by default), bound from `Email:Smtp` configuration. If this section is missing or points to a non-working relay, reset requests still return generic success (anti-enumeration) but no email is delivered.
- For troubleshooting delivery issues, inspect WebApi logs (`logs/api-log-*.txt`) for `Error processing password reset request` and SMTP send diagnostics.

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
  - Discover tab is split into two journeys:
    1) **My Businesses**: joined loyalty businesses with per-business points and quick actions (Open QR / Open Rewards).
    2) **Explore**: searchable businesses by name/address for joining new programs, with category and nearby filters.
  - Call discovery endpoints with `BusinessDiscoveryFilter` → paged `BusinessSummary`.
  - Navigate to `BusinessDetail` for full info, or route directly to Rewards when the business is already joined.
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

## 17) API Dependencies (Mobile-facing View)

Mobile apps rely on `Darwin.Contracts` as the single source of payload truth and consume endpoints through `Darwin.Mobile.Shared` service facades.

- For complete endpoint inventory, policies, request/response contracts, and troubleshooting playbooks, use **`DarwinWebApi.md`**.
- Keep this mobile guide focused on app-side behavior, UX rules, threading, platform integration, and mobile roadmap status.

---

## 18) Roadmap Coupling (Phases 1–3)

- **Phase 1**: Endpoints: login/refresh/logout, consumer register + forgot-password + reset-password, session-based loyalty scan (prepare/process/confirm), basic rewards read models, discovery basics, and profile read/update/change-password.
- **Phase 2**: Map discovery with filters & details, richer reward dashboard, feed/promo endpoints.
- **Phase 3**: Subscriptions/analytics/notifications; additive endpoints on top of existing Contracts.

---


## 18.1 Current Snapshot (Done vs Remaining)

### Done in current codebase
- Consumer push-registration baseline is integrated end-to-end (contracts + API + shared service + coordinator + profile manual sync UI).
- Consumer Profile includes a self-service "Open notification settings" action to recover from denied notification permissions without leaving users blocked.
- Consumer Profile now shows runtime push diagnostics labels (permission state + token availability) for faster support and troubleshooting.
- Consumer Profile refreshes push diagnostics on every `OnAppearing` so permission/token changes are reflected after returning from system settings.
- Rewards tab now includes multi-business overview metrics (joined business count, aggregated points, top business) and a quick action to open selected-business QR.
- Feed promotions now support scope switching between selected-business and all-joined-business campaigns.
- Feed promotions keep lightweight client fallback guardrails for resilience, while primary guardrail enforcement now runs server-side with applied-policy + diagnostics returned by the promotions endpoint.
- Device registration now updates `UserEngagementSnapshot` baseline engagement metrics (last activity + heartbeat count metadata) to prepare inactive-reminder targeting and measurement.
- Application now includes inactive-reminder orchestration handlers: candidate selection query (threshold + cooldown + push-enabled device check) and per-outcome recorder that persists `Sent`/`Failed`/`Suppressed` metadata in `UserEngagementSnapshot`.
- WebApi now includes `InactiveReminderBackgroundService` scaffold (config-gated) that periodically runs reminder batch orchestration and logs evaluated/dispatched/suppressed/failed counters.
- Reminder batch orchestration now writes measurement counters into snapshot metadata (`inactiveReminderSentCount`, `inactiveReminderFailedCount`, `inactiveReminderSuppressedCount`) for downstream analytics.
- Inactive reminder dispatch now uses `HttpInactiveReminderDispatcher` (server-side HTTP gateway integration) and sends user/device/token/platform payload with configurable title/body templates.
- HTTP gateway dispatcher now maps provider transport/HTTP errors to stable outcome codes (for example `Gateway.Unauthorized`, `Gateway.RateLimited`, `Gateway.ServerError`) to improve reminder analytics quality.
- When gateway returns provider reason payload, dispatcher now emits normalized `Gateway.Provider.*` codes to preserve APNs/FCM-specific failure semantics in analytics.
- Dispatcher now canonicalizes common FCM/APNs reasons into stable categories (for example `Gateway.Provider.Fcm.TokenUnregistered`, `Gateway.Provider.Apns.TokenInvalid`) for clearer operational actions.
- Promotions analytics tracking is now wired end-to-end for `Impression` and `Open` events (mobile feed emits events to WebApi; Application persists counters in `UserEngagementSnapshot` metadata).
- Promotions `Claim` tracking is now hooked from redemption QR generation (`RewardClaimIntent`) so conversion funnel has event coverage for all three stages.
- Promotions response contracts now include campaign-foundation metadata (`CampaignState`, campaign window, eligibility rules) with backward-compatible defaults for derived cards.
- Promotions feed query now includes active in-app campaign entities from server-side marketing data and merges them with legacy derived cards to preserve rollout safety.
- Promotions endpoint now returns the server-applied feed policy and enforces server-side guardrails (de-duplication, max-card cap, suppression/frequency windows) for better client/server consistency.
- Business campaign operations endpoint set is now available for list/create/update/activation workflows, enabling controlled campaign lifecycle management from business interfaces.
- `Darwin.Mobile.Shared` now exposes business campaign operations in `ILoyaltyService` to unblock Business app integration without duplicating API plumbing in UI projects.
- Business app Rewards page now ships minimal campaign operations UI (list + activate/deactivate + create/update editor) for mobile-first campaign lifecycle management.
- Business campaign editor now includes optional UTC schedule inputs with pre-submit validation for date format and start/end range.
- Business campaign editor now includes channel selection controls (In-App / In-App+Push) and channel validation before API mutations.
- Business campaign editor now validates and submits optional `targeting/payload` JSON object fields with localized guardrail errors before API mutations.
- Business campaign editor now blocks duplicate internal campaign names on the client before API mutations, reducing avoidable retry loops for operators.
- Business campaign list now supports local search + lifecycle-state filtering to keep large campaign sets manageable on tablet screens.
- Business campaign list now shows visible/total result summary and supports one-tap filter reset to reduce operator friction during frequent context switching.
- Business campaign list now supports operator-selectable sort modes (start date and title ascending/descending) for faster review in high-volume campaign inventories.
- Business campaign list now shows lifecycle KPI counters (Draft/Scheduled/Active/Expired) for faster day-to-day operational monitoring.
- Business campaign lifecycle KPI counters now act as one-tap filter chips so operators can jump directly to a state-specific list.
- Business campaign lifecycle KPI chips now include "All" quick-reset and toggle-to-clear behavior on active state chips for faster context switching.
- "All" lifecycle KPI chip now resets only the state filter while keeping search/sort criteria intact for faster repeated operator triage loops.
- Campaign management toolbar now includes "Clear search" action that resets only search query while preserving state/sort context.
- Business campaign search now also matches campaign body text (besides internal name/title) to improve findability across long-form campaign content.
- Business campaign list now shows localized audience/eligibility summary parsed from targeting JSON (with compatibility fallback to `eligibilityRules[0]`) so operators can verify segmentation without opening editor.
- Business campaign toolbar now includes audience-kind filtering (all/joined/tier/points/date-window) so operators can isolate segmentation cohorts in one tap.
- Business campaign audience metrics now include actionable audience KPI chips (all/joined/tier/points/date-window) with toggle-to-clear behavior for faster cohort drill-down workflows.
- Business campaign editor now offers quick audience targeting presets (joined/tier/points/date-window) to accelerate common segmentation setup while keeping JSON editable.
- Business campaign editor now renders inline targeting guidance from current targeting JSON so segmentation intent is visible before submit.
- Business campaign editor now validates audience-specific targeting schema inline (tier/minimumPoints/date-window UTC fields) and blocks invalid saves with localized feedback.
- Business campaign editor now offers one-tap schema quick-fix action for common targeting errors to accelerate operator recovery before save.
- Business campaign editor quick-fix now shows localized status feedback (applied/no-change) after auto-correction attempts.
- Business campaign editor now shows quick-fix applied/no-change telemetry counters inline for lightweight operational diagnostics.
- Business campaign editor now allows resetting quick-fix telemetry counters to start a fresh monitoring window during daily operations.
- Quick-fix telemetry now shows monitoring-window context (window start + last reset) to improve operational diagnostics readability.
- Campaign targeting quick-fix telemetry is now persisted in business activity logs and surfaced in dashboard/report exports for shift-level diagnostics.
- Promotions feed policy now supports an explicit frequency-window contract field (`FrequencyWindowMinutes`) with backward-compatible fallback to suppression-window behavior.
- Promotions feed response now emits guardrail diagnostics counters (initial candidates, suppressed by frequency, deduplicated, cap-trimmed, final count) for operations observability.
- Consumer now uses production platform push token providers (`ConsumerPlatformPushTokenProvider`) with Android FCM token bridge + iOS/MacCatalyst APNs runtime bridge (fallback config provider removed from DI path).
- Android map key is externalized and validated at build-time (warning in Debug, error in Release when missing).
- Business Phase-2 dashboard/rewards flows and authorization guards are implemented.
- Business Dashboard now includes CSV + PDF export via native share sheet (summary KPIs, top customers, recent activity rows) for lightweight operator reporting workflows.
- Business Settings now includes a rotating Staff Access Badge page for internal QR-based staff checkpoints (short-lived payload, expiry countdown, manual refresh).

### Remaining / follow-up
- **Handoff note (this chat):** current iteration is paused cleanly for continuation in a new chat; latest completed increment hardened promotions feed mapping (priority extraction from payload, eligibility-rules array compatibility parsing, and lifecycle-state normalization).
- **Recommended next step (new chat):** start with a fresh baseline validation from latest repository state, then execute Promotions verification/hardening (automated tests for lifecycle/priority/eligibility parsing paths).
- Testing coverage and execution evidence are tracked in `DarwinTesting.md` (dedicated testing stream); update that file alongside status changes.
- Continue Promotions Phase upgrade with advanced campaign editor UX polish and admin-side campaign operations refinement on top of delivered business APIs/contracts.
- Finalize inactive-reminder provider-native sender integration behind gateway (FCM/APNs), and extend provider-specific failure taxonomy/remediation mapping.
- Re-run end-to-end mobile/server build + test evidence collection from current branch before rollout decisions.

### Next-chat startup checklist
1. Re-read `BACKLOG.md`, `DarwinMobile.md`, and current mobile ViewModels from disk before coding.
2. Run mobile-only compile/build checks to capture only active failures from latest branch state.
3. Resolve blocker errors in small isolated commits, then resume promotions/reminders queue delivery.

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
- **Notifications**: `MobileDevicePlatform`, `RegisterPushDeviceRequest`, `RegisterPushDeviceResponse`.

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

## WebApi Verification

WebApi endpoint verification and Postman walkthroughs were moved to **`DarwinWebApi.md`**.
Use that document as the source of truth for:

- auth/device-binding preconditions,
- endpoint-specific request/response examples,
- role/policy requirements,
- and operational diagnostics/troubleshooting.

- [x] Campaign targeting quick-fix telemetry is now persisted in business activity logs and surfaced in dashboard/report exports for shift-level diagnostics.
