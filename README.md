# 🛒 Darwin CMS + E-Commerce Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-9.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![Build](https://img.shields.io/github/actions/workflow/status/shahramvafadar/Darwin/build.yml?branch=master&logo=githubactions&label=CI)](../../actions)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Darwin is a **modern CMS + e-commerce solution** designed for **European SMBs** who need a
flexible, future-proof platform that runs even on **shared hosting**.

It combines **content management (CMS)** and **full e-commerce features** such as catalog, pricing, inventory, cart, checkout, orders, shipping, and payments — all built with **clean architecture** and extensibility in mind.

---

## ✨ Features

- 📝 **CMS**: Pages, rich text editor (Quill), SEO meta, menus, media library.
- 🛍️ **Catalog**: Brands, categories, products, variants, attributes.
- 📦 **Inventory**: Stock tracking, reserved qty, reorder levels, warehouses (future).
- 💶 **Pricing & Tax**: EU VAT (DE by default), promotions, coupons.
- 🛒 **Cart & Checkout**: Guest carts, min/max qty rules, VAT calculations.
- 📑 **Orders**: Full lifecycle (Created → Paid → Shipped → Refunded).
- 🚚 **Shipping**: DHL (DE first), extensible provider model.
- 💳 **Payments**: PayPal, Klarna/Sofort, SEPA (future Stripe).
- 🌍 **Localization**: Multi-culture content via normalized translations.
- 🔍 **SEO**: Unique slugs per culture, canonical, hreflang, sitemap, robots.
- 🧩 **Extensibility**: Feature flags, outgoing webhooks, modular architecture.
- 🛡️ **Security**: XSS sanitization, upload hardening, GDPR consent & privacy pages.
  - **Argon2id** password hashing
  - **Passkeys/WebAuthn** (FIDO2 via fido2-net-lib v4) for login/registration
  - **TOTP 2FA** (RFC 6238)
  - **Data Protection** key ring persisted for shared hosting
- 📊 **Analytics**: Google Analytics, Tag Manager, Search Console (via settings).
- 🧪 **Testing**: Unit + Integration tests, GitHub Actions CI.
- 📱 **Mobile (MAUI)**: Two apps
  - **Consumer** (session-based QR loyalty, map-based discovery, rewards dashboard)
  - **Business** (camera QR scan, process scan sessions to accrue points or confirm redemptions)
  - Shared library (**Darwin.Mobile.Shared**) provides HTTP + retry, auth helpers, and scanner/location abstractions
  - DTOs come from **Darwin.Contracts**
- 🔗 **API & Contracts**: Public **Darwin.WebApi** (JWT, Swagger) using **Darwin.Contracts** as the single source of request/response models for both Web and Mobile.

---

## 📱 Mobile/WebApi Quick Start (Contracts-first)

Darwin.WebApi is the public surface for mobile (Consumer + Business). All payloads and errors use `Darwin.Contracts` types.

### Auth & Bootstrap
- `GET /api/meta/bootstrap` — minimal mobile bootstrap (JWT audience, QR refresh seconds, outbox limits). **AllowAnonymous**
- `POST /api/auth/login` — password login → `TokenResponse` (access + refresh). **AllowAnonymous**
- `POST /api/auth/refresh` — refresh token → `TokenResponse`. **AllowAnonymous**
- `POST /api/auth/logout` — revoke refresh token (per device). **Authorize**
- `POST /api/auth/logout-all` — revoke all refresh tokens. **Authorize**
- `POST /api/auth/password/change` — change password (current → new). **Authorize**
- `POST /api/auth/password/request-reset` — request reset (always 200). **AllowAnonymous**
- `POST /api/auth/password/reset` — complete reset. **AllowAnonymous**
- `POST /api/auth/register` — consumer self-registration. **AllowAnonymous**
- Access token policies:
  - Consumer: `perm:AccessMemberArea`
  - Business: `perm:AccessLoyaltyBusiness`
- Required claim for business endpoints: `business_id` (GUID). Business id is **never** accepted from body.

### Profile (Consumer)
- `GET /api/v1/profile/me` — current user profile (edit shape with RowVersion). **perm:AccessMemberArea**
- `PUT /api/v1/profile/me` — update profile (optimistic concurrency with RowVersion). **perm:AccessMemberArea**

### Loyalty (Consumer + Business)
All responses use `Darwin.Contracts.Loyalty`; errors are `Darwin.Contracts.Common.ProblemDetails`.

Consumer:
- `POST /api/v1/loyalty/scan/prepare` — prepare scan session → `ScanSessionToken`. **perm:AccessMemberArea**
- `GET  /api/v1/loyalty/my/accounts` — list my loyalty accounts. **perm:AccessMemberArea**
- `GET  /api/v1/loyalty/my/history/{businessId}` — points history per business. **perm:AccessMemberArea**
- `GET  /api/v1/loyalty/account/{businessId}` — account summary (404 if none). **perm:AccessMemberArea**
- `GET  /api/v1/loyalty/business/{businessId}/rewards` — available rewards (consumer view). **perm:AccessMemberArea**
- `GET  /api/v1/loyalty/my/businesses` — “My places” (paged). **perm:AccessMemberArea**
- `POST /api/v1/loyalty/my/timeline` — unified timeline (cursor paging). **perm:AccessMemberArea**

Business:
- `POST /api/v1/loyalty/scan/process` — process scanned token → session view. **perm:AccessLoyaltyBusiness** (business_id from JWT)
- `POST /api/v1/loyalty/scan/confirm-accrual` — confirm accrual. **perm:AccessLoyaltyBusiness**
- `POST /api/v1/loyalty/scan/confirm-redemption` — confirm redemption. **perm:AccessLoyaltyBusiness**

### Business Discovery (Consumer)
- `POST /api/v1/businesses/list` — paged discovery (query/search, category, city, proximity). **AllowAnonymous**
- `POST /api/v1/businesses/map` — map viewport discovery. **AllowAnonymous**
- `GET  /api/v1/businesses/{id}` — public detail. **AllowAnonymous**
- `GET  /api/v1/businesses/{id}/with-my-account` — detail + my account summary. **perm:AccessMemberArea**

### Error & Result Shape
- Errors use `Darwin.Contracts.Common.ProblemDetails` (RFC 7807 shape).
- Application handlers return `Result` / `Result<T>`; controllers convert failures to `ProblemDetails`.

### Security & Composition (WebApi)
- JWT bearer auth (`JwtTokenService`); rate limiting on login/refresh (`EnableRateLimiting` policies).
- Policies: `perm:AccessMemberArea`, `perm:AccessLoyaltyBusiness`.
- `ICurrentUserService` is resolved from claims (no admin fallback).
- DI: `AddWebApiComposition` registers Application, Persistence, JWT auth core, HttpContextAccessor, CurrentUserService, Swagger (dev), RateLimiter, Controllers.

---

## 🏗️ Architecture

Darwin follows a **clean architecture** with clear separation of concerns:

```
src/
├─ Darwin.Domain           → Entities, ValueObjects, Enums, BaseEntity
├─ Darwin.Application      → Use cases, DTOs, Handlers, Validators
├─ Darwin.Infrastructure   → EF Core, DbContext, Migrations, DataSeeder
├─ Darwin.Web              → MVC + Razor (Admin + Public), DI, Middleware
├─ Darwin.WebApi           → REST API
├─ Darwin.Shared           → Result wrappers, constants, helpers
├─ Darwin.Contracts        → Shared DTOs for WebApi + Mobile (request/response contracts)
├─ Darwin.Mobile.Shared    → Mobile shared services (HTTP client, retry, auth, scanner/location abstractions)
├─ Darwin.Mobile.Consumer  → .NET MAUI consumer app (QR, discover, rewards, profile)
└─ Darwin.Mobile.Business  → .NET MAUI business app (scan, accrue, redeem)
```

### Key Principles
- **SOLID** principles applied consistently.  
- **Minor units for money** (`long` cents) to avoid floating-point errors.  
- **Audit fields** (`CreatedAtUtc`, `ModifiedAtUtc`, `CreatedByUserId`, `ModifiedByUserId`).  
- **Soft delete** with `IsDeleted`.  
- **Optimistic concurrency** via `RowVersion`.  
- **Normalized translation tables** for multilingual content.  

### Composition

- **Web composition root**: `src/Darwin.Web/Extensions/DependencyInjection.cs`
  - calls Infrastructure modules:
    - `AddSharedHostingDataProtection(configuration)`
    - `AddPersistence(configuration)`
    - `AddIdentityInfrastructure()`
    - `AddNotificationsInfrastructure(configuration)`

---

## 📱 Mobile Overview

The mobile suite consists of two .NET MAUI apps:

- **Darwin.Mobile.Consumer**: end-user app with authentication, a **short-lived scan session QR** for in-store scans (Accrual/Redemption), discover/map, rewards dashboard, and profile.
- **Darwin.Mobile.Business**: tablet app for partners to **scan the consumer QR**, load the server-side scan session, and then **accrue points** or **confirm reward redemptions**.

Shared libraries and contracts:

- **Darwin.Mobile.Shared**: typed HTTP client (System.Text.Json), **retry policy**, token storage helpers, and abstractions for camera scanning (`IScanner`) and geolocation (`ILocation`). Registered via `AddDarwinMobileShared(ApiOptions)` which requires `Microsoft.Extensions.Http` for `AddHttpClient`.
- **Darwin.Contracts**: single source of **request/response DTOs** used by both WebApi and the mobile apps (identity tokens, loyalty scan flows, discovery, paging/sorting, problem details).

Security highlights:

- **No internal user IDs in QR**; the QR is an **opaque, short-lived scan session token** exchanged server-side for an ephemeral session. Short expiry and one-time semantics limit replay risk.
- **JWT + refresh tokens** for app authentication; server uses Data Protection and Argon2/WebAuthn/TOTP per platform defaults.

WebApi provides the endpoints consumed by both apps and composes Infrastructure modules (Persistence, Identity/JWT, Notifications, Data Protection).

---

## 🔐 Security Overview

- **Passwords**: Argon2id hasher with sane defaults.
- **Passkeys/WebAuthn**: FIDO2 ceremonies via `fido2-net-lib` v4 (registration + assertion); credentials stored in `UserWebAuthnCredential`.
- **TOTP 2FA**: RFC 6238 (30s step, 6 digits, default ±1 step window).
- **Data Protection**: Key ring persisted on disk (shared-host friendly). Configure `DataProtection:KeysPath` to a writable, persistent folder.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/)
- SQL Server (local or hosted; LocalDB works for dev)
- Node/npm (optional, for front-end tooling)

### Local Setup

```bash
# clone the repo
git clone https://github.com/shahramvafadar/Darwin.git
cd Darwin
```

Configure connection string in `appsettings.Development.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Darwin;Trusted_Connection=True;"
}
```

Sample `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Darwin;Trusted_Connection=True;"
  },
  "DataProtection": {
    "KeysPath": "C:\\\\_shared\\\\DarwinKeys"
  },
  "Email": {
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "EnableSsl": true,
      "User": "no-reply@example.com",
      "Password": "YOUR_STRONG_PASSWORD",
      "From": "no-reply@example.com",
      "FromName": "Darwin"
    }
  },
  "WebAuthn": {
    "RelyingPartyId": "localhost",
    "RelyingPartyName": "Darwin",
    "AllowedOriginsCsv": "https://localhost:5001,https://localhost:7170",
    "RequireUserVerification": false
  }
}
```

> For production: set a persistent `DataProtection:KeysPath` (disk/share), configure SMTP, WebAuthn origins, culture/currency defaults (also editable from SiteSettings in Admin).

Run migrations & seed:
```bash
dotnet ef database update --project src/Darwin.Infrastructure
```

Start the app:
```bash
dotnet run --project src/Darwin.Web
```

Then open `https://localhost:7170/admin`
(default admin user is seeded — change password on first login).

---

## 🗺️ Roadmap

See `BACKLOG.md` for the full backlog and feature roadmap.

High-level milestones:
- Skeleton solution with clean architecture
- Core entities (Domain)
- Products, Categories, Pages (Admin)
- ✅ Full SiteSettings (culture, units, SEO, feature flags)
- SEO features (canonical, hreflang, sitemap, robots)
- Cart & Checkout flows
- Orders lifecycle + payments + shipping
- Webhooks (outgoing & incoming)
- Public storefront UI (after Admin completion)
- API v1 (REST with Swagger)
- Minimal CRM (user profiles, consents)
- Mobile suite: Darwin.Mobile.Shared, Darwin.Mobile.Consumer, Darwin.Mobile.Business
- Loyalty QR flow: session-based consumer QR (ScanSessionToken), secure scan session endpoints (prepare/process/confirm accrual & redemption)
- Discovery on mobile: map + directory + business details
- Contracts-first WebApi: expand Darwin.Contracts without breaking existing clients

---

## 📚 Documentation

- Setup Guide
- Architecture Decisions
- Styleguide & Conventions
- Backlog & Roadmap

---

## 🤝 Contributing

Contributions are welcome!

1. Fork the repo
2. Create a feature branch: `git checkout -b feature/myfeature`
3. Commit your changes: `git commit -m "Add feature"`
4. Push to the branch: `git push origin feature/myfeature`
5. Open a Pull Request

---

## 📜 License

This project is licensed under the MIT License.

---

## 🏢 About

Darwin is built to support small and medium businesses in Germany/EU with a system that is:
- Easy to host (shared hosting compatible)
- Legally compliant (GDPR, VAT rules, Impressum/Privacy pages)
- Extensible for growth (CRM, webhooks, API, integrations)

💡 The vision: One platform to manage content + commerce, future-proof, open-source, developer-friendly.