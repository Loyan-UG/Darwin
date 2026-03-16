# Darwin Backlog & Roadmap

This document defines the **current status, active work, and planned roadmap**
for the Darwin Platform:

- CMS + E-Commerce + CRM (Web)
- Public REST API (WebApi)
- Loyalty System (shared between Web & Mobile)
- Mobile Suite (Business + Consumer)
- Shared packages (Contracts + Mobile.Shared)

It is designed as the **single source of truth** for development planning.


---

# 1. ✔ Completed (Stable)

## 1.1 Architecture & Core Infrastructure
- Clean Architecture solution structure  
  (`Domain`, `Application`, `Infrastructure`, `Web`)
- Complete domain model:
  - Catalog: Product, Variant, Category, Brand, Add-ons
  - CMS: Pages, Menus
  - Pricing: Promotions, Taxes
  - Cart & Checkout: Cart, CartItem
  - Orders: Order, OrderLine, Payment, Shipment (partial)
  - Users & Addresses
  - Identity: Role, Permission, UserRole, RolePermission
  - SEO: RedirectRule
  - Settings: SiteSetting (general + SEO + analytics + WebAuthn + SMTP)
- Core cross-cutting concerns:
  - Soft-delete (`IsDeleted`)
  - Audit fields
  - Concurrency (`RowVersion`)
  - Translation pattern for multilingual content

## 1.2 Security
- Argon2id password hashing
- TOTP (2FA) with Data Protection encryption
- WebAuthn (FIDO2) registration + login
- External logins (Google, Microsoft)
- SecurityStamp rotation
- Password reset workflows

## 1.3 Application Layer
- Command/Query Handlers for all modules
- FluentValidation validators
- Result<T> pattern (Darwin.Shared)
- Abstractions for Persistence, Clock, Auth, Email

## 1.4 Infrastructure Layer
- EF Core configurations for all entities
- DbContext + Migrations
- DataSeeder (Identity + Catalog)
- SMTP email sender
- Data Protection key ring support for shared hosting
- Secret-protection converters (TOTP, sensitive fields)

## 1.5 Admin Panel (Darwin.Web)
- Pages, Categories, Products, Brands, Menus
- Multilingual fields (Brand, Product, Page)
- Quill rich text editor
- Site Settings (partial)
- Robots.txt + Sitemap generation
- Canonical URL service
- Shared admin UI components (`_Alerts.cshtml`, TagHelpers)

## 1.6 Mobile Phase-1 Account & Loyalty Baseline
- Consumer account flows completed in app:
  - Register (self-service)
  - Forgot password request
  - Login + refresh token handling
  - Profile read/update (`GET/PUT /api/v1/profile/me` with `RowVersion`)
  - Change password (`POST /api/v1/auth/password/change`)
- Consumer QR behavior baseline documented and implemented:
  - Countdown/UI tick ~1s for smooth display
  - Minimum automatic network refresh interval = 5 minutes while token still valid
- Business scanner flow baseline completed:
  - Scan token → process session → confirm accrual/redemption

	- 
---

# 2. 🚧 In Progress

## 2.1 WebApi (High Priority — ACTIVE)
- JWT Authentication (already implemented in Infrastructure)
- Contracts-first endpoints using `Darwin.Contracts`
- Identity endpoints: login, refresh token, logout, logout-all, register, password reset/change
- Business endpoints:
  - Business info
  - Business locations
  - Loyalty program definitions
  - Customer lookup & reward snapshot
- Consumer endpoints:
  - QR token generation (short-lived)
  - Reward accrual (+1 point)
  - Reward redemption
  - Discover (map + list)
  - Profile (basic editable info)

## 2.2 Mobile.Shared (ACTIVE)
- HTTP client (AddHttpClient) with retry policy (Polly-style)
- Token storage (secure)
- QR token refresher
- Shared API facades (AuthService, LoyaltyService, BusinessService)
- Abstractions for Scanner + Location
- DI composition (`AddDarwinMobileShared`)

## 2.3 Mobile Consumer App (ACTIVE)
- Stabilization and UX refinement of completed Phase-1 flows
- Register / forgot-password / reset-password end-user journey hardening
- Profile UX refinements (clearer inline feedback placement)
- Rotating QR polish (countdown smoothness vs battery trade-off)
- Discovery + rewards performance tuning and small UX fixes
- Discover IA finalized: separate "My Businesses" (joined accounts + quick actions) from "Explore" (search/join flow).
- Explore discovery finalized with category + nearby filters and joined-state routing to rewards.
- Wire-up to Shared services

## 2.4 Mobile Business App (ACTIVE)
- Login
- QR Scan → Loyalty API → Add point
- Redemption workflow
- Customer snapshot display
- Wire-up to Shared services

## 2.5 Admin Panel Enhancements (ONGOING)
- SiteSettings: full completion (SMTP, analytics, WebAuthn origins, WhatsApp)
- Role & permission UI
- User management + 2FA + WebAuthn management
- Consistent Quill integration across full CMS

---

# 3. 📝 Planned Next

## 3.1 WebApi Extensions
- [x] Business onboarding endpoints
- [x] Reward configuration endpoints
- [x] Push notification registration (device tokens)
- [x] Extended discovery filters
- Public Catalog endpoints for future storefront use

## 3.2 Mobile Consumer App – Phase 2
- [x] Full map integration (Google Maps / Apple MapKit)
- [x] Business detail page
- [x] Favorites, reviews, likes
- [x] Feed/promotions module (timeline-backed + promotions endpoint)
- [x] Rewards history
- [x] Discover IA split: My Businesses + Explore (search/category/nearby)

## 3.3 Mobile Business App – Phase 2
- [x] Business dashboard
- [x] Simple reporting (visits, top customers, upcoming rewards)
- [x] Reward editing interface
- [x] Staff roles & permissions

## 3.4 Mobile Consumer App – Phase 3
### Completed
- [x] Push registration infrastructure end-to-end baseline (contracts, API endpoint, shared/mobile services, coordinator wiring).
- [x] Manual push registration sync UX in Consumer Profile (status + last sync timestamp + retry action).

### Remaining
- [x] Native platform push token providers (FCM/APNs production integration) replacing fallback/noop behavior.
- [x] Multi-business loyalty overview (aggregated balances, quick actions, and state transitions).
- [x] Promotion campaigns integration in consumer timeline.
- [~] Inactive user reminder strategy (triggering + suppression + measurement).

## 3.5 Mobile Business App – Phase 3
- [x] Full analytics module (CSV/PDF export)
- [~] Business subscription management (Stripe) — mobile settings entry point to billing portal delivered; full in-app plan state and mutation workflow pending.
- [x] Staff QR codes for internal access

## 3.6 Backlog Additions from Recent Mobile Implementation
- [x] Push provider operational readiness checklist per environment (Firebase `google-services.json`, Apple Push entitlement/certificate, runtime permission verification, and token rotation monitoring dashboard).
- [x] Consumer Register UX: auto-login after successful registration (or explicit redirect to login as fallback).
- [x] Shared mobile error mapping policy: avoid showing raw exception messages in UI-bound ViewModels.
- [x] API client no-content contract cleanup: add first-class helpers for success responses with empty body to reduce per-service workarounds.
- [x] QR countdown UX decision finalized: keep 1s display refresh (smoother UI) with a 5-minute minimum automatic network refresh limit.
- [x] Consumer Profile push registration self-service sync: added manual sync action with user-visible status and last-sync timestamp.

- [x] Externalize mobile map API keys (Android Google Maps) to secure secret providers, with Android build-time validation (warn in Debug, fail in Release) and documented iOS/MapKit requirements per environment.

## 3.7 Promotions Phase Upgrade (Next High-Value Workstream)
- [~] Introduce campaign-driven promotions model (instead of only derived/tier-based cards).
- [~] Add business-manageable campaign lifecycle (draft, scheduled, active, expired).
- [~] Add eligibility + audience rules (joined members, tier, points threshold, date window).
- [~] Add feed delivery guardrails (priority, cap, de-duplication, frequency policy).
- [x] Add tracking events for impression/open/claim to measure conversion.
- [~] Add admin/business APIs and minimal management UI for campaign CRUD and activation.


## 3.8 Quality Findings & Follow-up
- [x] Fixed: `ProfileViewModel.SaveProfileAsync` metadata refresh dead-path (refresh was previously skipped when `IsBusy == true`).
- [x] Fixed: `ProfileViewModel.SyncPushRegistrationAsync` busy-flag updates now marshaled via UI thread helper for safer property change notifications.
- [↪] Testing coverage for Profile save fallback metadata flow and push-sync command reentrancy/busy-state behavior moved to `DarwinTesting.md` (handled in dedicated testing track).
- [x] Confirmed production token-provider strategy (`ConsumerPlatformPushTokenProvider` with Android FCM + iOS/MacCatalyst APNs bridges) and removed fallback registration path from DI.
- [x] Hardened platform build wiring (Android-only Firebase package + explicit iOS/MacCatalyst entitlements binding) to prevent cross-target restore/signing misconfiguration.
- [x] Added Android 13+ startup notification-permission request bootstrap with one-time prompt persistence, and removed legacy fallback push token providers to reduce production ambiguity.
- [x] Added release-safe APNs entitlement split (Debug=development, Release=production) and Android Release guard for missing `google-services.json`.
- [x] Business dashboard now supports CSV export (summary KPIs + top customers + recent activities) through native share flow for lightweight operator reporting.
- [x] Business dashboard now supports PDF export (single-page operational snapshot) through native share flow for lightweight operator reporting handoff.
- [x] Business settings now include a rotating staff access badge page that emits short-lived internal QR payloads with expiry countdown and manual refresh.
- [x] Added Profile push "Open notification settings" self-service action to improve recovery after notification permission denial.
- [x] Added runtime push diagnostics labels in Profile (permission state + token availability) to speed up operational troubleshooting.
- [x] Profile push diagnostics refresh on every page appearance so state updates after returning from system settings.
- [x] Published environment-specific push readiness checklist in `DarwinMobile.md` for Dev/Staging/Production release gating.
- [x] Rewards page now includes aggregated multi-business overview metrics and a quick QR action for the selected business context.
- [x] Feed promotions now support scope switching (selected business vs all joined businesses) with context-aware cards.
- [x] Feed promotions now enforce initial guardrails in mobile rendering (de-duplication by business/title/CTA, 6-card cap, and 8-hour suppression window with fallback behavior).
- [x] Device registration heartbeat now updates `UserEngagementSnapshot` baseline fields (`LastActivityAtUtc`, `EventCount`, compact snapshot metadata) to support inactive-reminder triggering and measurement.
- [x] Added Application handlers for inactive-reminder candidate selection and per-outcome measurement metadata recording (sent/failed/suppressed with cooldown-ready baseline).
- [x] Added WebApi background worker scaffold for inactive reminders (`InactiveReminderBackgroundService`) with configurable interval/threshold/cooldown and batch observability counters.
- [x] Inactive reminder batch orchestration now records every attempt outcome (`Sent`, `Failed`, `Suppressed`) in engagement snapshot metadata for measurement dashboards.
- [x] Replaced no-op reminder dispatcher with HTTP push gateway dispatcher (`HttpInactiveReminderDispatcher`) using push token + platform payload and configurable auth/templates.
- [x] Added stable inactive-reminder gateway failure taxonomy codes (`Gateway.*`, `Validation.*`) for cleaner suppression/failure measurement and dashboards.
- [x] Gateway dispatcher now extracts provider-native reason fields (`providerCode`/`providerReason`/`code`/`reason`) and emits normalized `Gateway.Provider.*` taxonomy codes.
- [x] Added canonical provider mappings for common FCM/APNs reason codes (token invalid, auth/topic mismatch, rate-limit, service unavailable) to improve remediation signals.
- [x] Added promotion interaction tracking endpoint + mobile client calls for `Impression` and `Open` events (engagement snapshot metadata updates).
- [x] Added `Claim` event hook from redemption QR generation flow (`RewardClaimIntent`) to complete promotions conversion funnel telemetry coverage.
- [x] Added campaign-foundation promotion payload fields in Contracts/Application/WebApi mapping (`CampaignState`, campaign window, and normalized eligibility rules) while keeping backward-compatible derived cards active.
- [x] Promotions query now reads active in-app `Marketing.Campaign` entities (global + joined-business scoped), resolves lifecycle state, and merges campaign cards with derived loyalty cards for gradual migration.
- [x] Promotions feed now applies server-side guardrails with contract-exposed policy (`EnableDeduplication`, `MaxCards`, `SuppressionWindowMinutes`, `FrequencyWindowMinutes`) and campaign suppression/frequency controls based on recent in-app delivery attempts.
- [x] Added business campaign management WebApi endpoints (list/create/update/activation) with business-scope ownership checks and RowVersion concurrency for update/activation paths.
- [x] Wired business campaign management contracts into `Darwin.Mobile.Shared` loyalty facade (list/create/update/activation) so mobile business workflows can consume the new WebApi surface.
- [x] Business mobile Rewards screen now includes campaign list, in-app activation toggle, and minimal create/update campaign editor wired to shared campaign APIs.
- [x] Business campaign editor now supports optional UTC start/end inputs with client-side format/range validation before create/update API calls.
- [x] Business campaign editor now supports delivery-channel selection (In-App / In-App+Push) with explicit validation and localized labels in mobile UI.
- [x] Business campaign editor now validates and submits optional `targeting/payload` JSON object fields (with localized validation feedback) to reduce malformed mutation payloads.
- [x] Business campaign editor now performs client-side duplicate internal-name guardrails before create/update API calls to reduce avoidable round-trips and operator confusion.
- [x] Business campaign operations list now supports local search + lifecycle-state filtering (Draft/Scheduled/Active/Expired) to improve operator navigation in larger campaign sets.
- [x] Business campaign operations list now shows filter-result summary (visible/total) and includes one-tap filter reset for faster operator recovery in dense campaign sets.
- [x] Business campaign operations list now supports configurable client-side sort options (start date and title asc/desc) for faster operator triage workflows.
- [x] Business campaign operations list now surfaces lifecycle KPI counters (Draft/Scheduled/Active/Expired) to give operators a fast health snapshot before drilling into details.
- [x] Business campaign lifecycle KPI counters are now actionable chips that apply state filter in one tap for faster drill-down workflows.
- [x] Business campaign lifecycle KPI chips now support toggle behavior (tap active chip again to clear state filter) and include an "All" chip for one-tap reset.
- [x] "All" KPI chip now resets only lifecycle-state filter (preserving active search/sort context) for faster iterative campaign triage.
- [x] Campaign toolbar now includes a dedicated "Clear search" action that resets only search query while preserving state/sort context.
- [x] Business campaign local search now also matches campaign body text (in addition to internal name/title) for better operator discovery in dense lists.
- [x] Promotions feed now supports explicit frequency policy input (`FrequencyWindowMinutes`) and response diagnostics counters for suppression/dedup/cap observability in operations dashboards.
- [x] Business Settings now includes a subscription management entry page with environment-aware billing portal launch (HTTPS validation + copy URL action + safe fallback when portal URL is not configured).
- [x] Testing-phase login acceleration hardened: QA credentials remain prefilled only in DEBUG mobile builds (Consumer/Business), while non-DEBUG builds default to empty credentials; full removal remains mandatory before customer rollout.

## 3.9 Mobile Execution Queue (Proposed — Awaiting Confirmation)
1. **P0 — Promotions foundation:** introduce campaign entity model + contracts for lifecycle (`Draft/Scheduled/Active/Expired`) and eligibility/audience rules (from 3.7 open items).
2. **P1 — Promotions delivery consistency:** align server-side feed guardrails (priority/cap/dedup/frequency) with current mobile client guardrails and expose suppression policy in contracts.
3. **P1 — Promotions operations:** add minimal business/admin campaign management APIs (CRUD + activation) and minimal management UI.
4. **P2 — Inactive reminders completion:** extend current reminder baseline with explicit dispatch/suppression workflow (send log + cooldown policy) and provider-native sender integration hardening.

### 3.9.1 Handoff Status (Prepared for next chat)
- Current iteration status: **Paused intentionally** for chat handoff; no open in-progress code task is left half-implemented in this iteration.
- Last delivered increment: campaign local search now matches `Name` + `Title` + `Body` (Business mobile Rewards list).
- Next recommended starting point in new chat: continue from item **3.9 / P0 Promotions foundation** and keep backlog/docs updated per increment.

> Note: Testing workstreams are intentionally tracked in `DarwinTesting.md` and excluded from the main delivery queue in this backlog.

---

# 4. 🔒 Identity & Security Roadmap

- Enforce TOTP for Admin users
- Add magic-link login capability
- Harden Admin cookie security
- Expand UserToken purposes (email verification, device pairing)
- Documentation for Data Protection key rotation
- Token versioning to support session revocation
- Short-lived QR token (already planned in Contracts)

---

# 5. 🔧 Data Protection & Key Management

### Completed
- Encrypted secrets for TOTP and WebAuthn
- Configurable key directory for shared hosting
- Automatic key rotation

### TODO
1. Cloud-native key storage (Azure Blob, AWS S3, Redis)
2. Deployment checklist for Data Protection folders
3. Document multi-instance setup fully
4. Support backup/restore of key ring across environments

---

# 6. 📦 CRM Module (Future)

- Business-level customer segmentation
- Visit frequency tracking
- Customer activity timeline
- Loyalty + CRM integration
- Automated reachout: email/SMS/WhatsApp templates
- GDPR data export/deletion workflow

---

# 7. 🧩 Storefront (Future)

- Public storefront website
- Catalog browsing, product detail, filters
- Cart + checkout (consumer-facing)
- User account area
- Order history
- Loyalty points from purchases

---

# 8. 🔮 Long-term Ideas

- Plugin system (NuGet-based)
- Branching promotions (A/B tests)
- Multi-tenant mode
- POS integration
- Restaurant table management
- AI-based product recommendations
- Receipt OCR for reward auto-accrual

---

# 9. 🟥 Status Legend
- **Completed** — Stable, no major changes expected  
- **Active / In Progress** — Currently being worked on  
- **Planned Next** — Approved, scheduled  
- **Future** — Not yet scheduled  

---

# 10. Summary

The Darwin platform now consists of **five major pillars**:

1. Web CMS & E-Commerce  
2. REST API  
3. Loyalty System  
4. Mobile Consumer App  
5. Mobile Business App  

All new development must follow strict **Contracts-first**,  
**Clean Architecture**, **Data Protection**, and **Consistency** rules.

This backlog is updated continuously as components evolve.
