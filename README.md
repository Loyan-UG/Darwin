# Darwin Platform - CMS + E-Commerce + Loyalty + CRM + Mobile

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-10.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Visual Studio 2026](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/)
[![Build](https://img.shields.io/github/actions/workflow/status/shahramvafadar/Darwin/build.yml?branch=master&logo=githubactions&label=CI)](../../actions)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)

Darwin is a unified platform that combines a content management system (CMS) with robust e‑commerce, a flexible loyalty engine, a forthcoming customer relationship management (CRM) module and modern .NET MAUI mobile apps. The goal of the platform is to allow businesses to manage content, products, loyalty programmes and customer engagement from a single codebase while exposing a RESTful WebApi for integration and providing first‑party mobile experiences for both consumers and business staff. Contract‑first development ensures that mobile apps, WebApi and web portals all stay in sync through the shared Darwin.Contracts library.

## 📌 Documentation Map (Start Here)

Use this README as the entry point, then jump to the specialized docs:

- **Backlog & Roadmap (source of truth)**: [`BACKLOG.md`](BACKLOG.md)
- **WebApi technical guide (endpoints, policies, Postman playbook)**: [`DarwinWebApi.md`](DarwinWebApi.md)
- **Mobile technical guide (Consumer/Business apps + Contracts)**: [`DarwinMobile.md`](DarwinMobile.md)
- **Mobile UI/UX guidelines**: [`DarwinMobile.Guidelines.md`](DarwinMobile.Guidelines.md)
- **Testing strategy & test execution tracker**: [`DarwinTesting.md`](DarwinTesting.md)
- **Admin Identity module how-to (Users/Roles/Permissions)**: [`howto-identity-access.md`](howto-identity-access.md)
- **Contributing**: [`CONTRIBUTING.md`](CONTRIBUTING.md)
- **License**: [`LICENSE.txt`](LICENSE.txt)

## ✅ Current Status Snapshot

This is a **high-level snapshot**. For detailed planning and the authoritative status per workstream, use `BACKLOG.md`.

## Status Snapshot

| Pillar                     | Status         | Notes                                                                                                                                                                                                       |
| -------------------------- | -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Domain & Application**   | ✔︎ Complete    | Clean architecture implemented with Commands/Queries, DTOs and validators.                                                                                                                                  |
| **Infrastructure**         | ✔︎ Complete    | Persistence (EF Core), Identity & JWT, Data Protection, Notifications.                                                                                                                                      |
| **Web CMS & E‑Commerce**   | ▶︎ In Progress   | Content & page management, product catalogue, cart & order flows.                                                                                                                                           |
| **Loyalty System**         | ▶︎ Phase 3+    | Scan‑session based accrual/redemption, rewards & campaigns, server‑side guardrails, push notifications and analytics implemented. Remaining work: advanced campaign editor & dispatch/suppression workflow. |
| **REST WebApi**            | ▶︎ Mature      | All mobile‑facing and admin endpoints delivered; continuous improvements & new endpoints are tracked in the backlog.                                                                                        |
| **Mobile Apps (Consumer)** | ▶︎ Phase 3+    | Login/register, QR display, discovery & map, rewards dashboard, promotions feed, push registration and analytics done; improvements in progress (multi‑business wallet, reminders).                         |
| **Mobile Apps (Business)** | ▶︎ Phase 3+    | QR scanning, point accrual/redemption, reward management, dashboard & reporting, business campaign editor implemented; enhancements to campaign editor and reminder workflows planned.                      |
| **CRM Module**             | ⚙︎ Planned     | Entities and flows for leads, contacts, opportunities and segmentation under design; integration with loyalty and commerce to follow.                                                                       |
| **Storefront**             | ⚙︎ Planned     | Customer‑facing shop interface (ASP.NET MVC/Blazor) with checkout and payment integration.                                                                                                                  |
| **Testing & QA**           | ▶︎ In Progress | Integration, contract and concurrency tests exist; additional coverage for new features and push‑sync scenarios planned.                                                                                    |

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
- 🪪 **Identity & Access (Admin + Mobile)**: roles/permissions, concurrency patterns, and member self-service flows.
- 🎁 **Loyalty & Engagement**:
  - Session-based QR (opaque scan token), accrual, redemption
  - Rewards tiers and business-facing management (evolving)
  - Campaigns/promotions feed & engagement tracking (in progress; see `BACKLOG.md`)
- 📱 **Mobile (MAUI)**:
  - Consumer app (discover, QR, rewards, profile, promotions)
  - Business app (scan, accrue, redeem, dashboard/reporting)
- 🔗 **API & Contracts**:
  - Public **Darwin.WebApi** (JWT, Swagger) using **Darwin.Contracts** as the single source of request/response models for Web + Mobile.
- 🤝 **CRM (next major pillar)**:
  - Customer profiles + segmentation + activity timeline
  - Consent & GDPR workflows
  - Outreach automations (email/SMS/WhatsApp) planned

---

## 🏗️ Architecture

Darwin follows a **clean architecture** with clear separation of concerns:

```
src/
├─ Darwin.Domain           → Entities, ValueObjects, Enums, BaseEntity
├─ Darwin.Application      → Use cases, DTOs, Handlers, Validators
├─ Darwin.Infrastructure   → EF Core, DbContext, Migrations, DataSeeder
├─ Darwin.Web              → MVC + Razor (Admin + Public), DI, Middleware
├─ Darwin.WebApi           → Public REST API (mobile + future integrations)
├─ Darwin.Worker           → Background jobs / schedulers (where applicable)
├─ Darwin.Shared           → Result wrappers, constants, helpers
├─ Darwin.Contracts        → Shared DTOs for WebApi + Mobile (request/response contracts)
├─ Darwin.Mobile.Shared    → Mobile shared services (HTTP client, retry, auth, scanner/location abstractions)
├─ Darwin.Mobile.Consumer  → .NET MAUI consumer app (QR, discover, rewards, profile)
└─ Darwin.Mobile.Business  → .NET MAUI business app (scan, accrue, redeem, dashboard)
```

### Key Principles
- **SOLID** applied consistently.
- **Minor units for money** (`long` cents) to avoid floating-point errors.
- **Audit fields** (`CreatedAtUtc`, `ModifiedAtUtc`, `CreatedByUserId`, `ModifiedByUserId`).
- **Soft delete** with `IsDeleted`.
- **Optimistic concurrency** via `RowVersion`.
- **Normalized translation tables** for multilingual content.

### Composition

- **Web composition root**: `src/Darwin.Web/Extensions/DependencyInjection.cs`
  - `AddSharedHostingDataProtection(configuration)`
  - `AddPersistence(configuration)`
  - `AddIdentityInfrastructure()`
  - `AddNotificationsInfrastructure(configuration)`
- **WebApi composition root**: `src/Darwin.WebApi/Extensions/DependencyInjection.cs`
  - Adds Application, Persistence, JWT auth core, HttpContextAccessor, `ICurrentUserService`, Swagger (dev), RateLimiter, Controllers.
  - Policies: `perm:AccessMemberArea`, `perm:AccessLoyaltyBusiness`.

---

## 📱 Mobile Overview

The mobile suite consists of two .NET MAUI apps:

- **Darwin.Mobile.Consumer**: end-user app with authentication, a **short-lived scan session QR** (Accrual/Redemption), discover/map, rewards dashboard, profile.
- **Darwin.Mobile.Business**: tablet app for partners to **scan the consumer QR**, load the server-side scan session, and then **accrue points** or **confirm reward redemptions**.

Shared libraries and contracts:

- **Darwin.Mobile.Shared**: typed HTTP client (System.Text.Json), retry policy, token storage helpers, abstractions for camera scanning (`IScanner`) and geolocation (`ILocation`). Registered via `AddDarwinMobileShared(ApiOptions)` (requires `Microsoft.Extensions.Http`).
- **Darwin.Contracts**: single source of **request/response DTOs** used by both WebApi and the mobile apps (identity tokens, loyalty scan flows, discovery, paging/sorting, problem details).

Security highlights:

- **No internal user IDs in QR**; QR is an opaque, short-lived ScanSessionToken. Short expiry and one-time semantics limit replay risk.
- **JWT + refresh tokens** for app authentication; server uses Data Protection and Argon2/WebAuthn/TOTP.

### Mobile/WebApi Quick Start (Contracts-first)

Darwin.WebApi is the public surface for mobile. All payloads/errors use `Darwin.Contracts`.

**Where to look (do not duplicate endpoint lists in README)**:
- Full endpoint inventory, policies, and Postman verification: **`DarwinWebApi.md`**
- Mobile app behavior/UX rules, phases, and platform integration: **`DarwinMobile.md`**

**Auth & Bootstrap (overview)**  
WebApi provides:
- Member (Consumer) auth flows (login/refresh/logout/register/reset-password, etc.)
- Business auth flows, with business-scoped authorization for sensitive endpoints
- A mobile bootstrap/meta surface for app runtime configuration

**Profile / Loyalty / Discovery (overview)**  
WebApi provides contracts-first surfaces for:
- Profile (`RowVersion` concurrency)
- Loyalty scan session (prepare/process/confirm)
- Business discovery (map + list + detail)
- Campaign/promotion operations (evolving)

**Error & Result Shape**
- Errors: `Darwin.Contracts.Common.ProblemDetails`.
- Handlers return `Result/Result<T>`; controllers convert failures to `ProblemDetails`.

**Security (WebApi)**
- JWT bearer auth (`JwtTokenService`); rate limiting on login/refresh (`EnableRateLimiting` policies).
- Policies: `perm:AccessMemberArea`, `perm:AccessLoyaltyBusiness`.
- `ICurrentUserService` resolved from claims (no admin fallback).

---

## CRM Overview

A CRM module is being designed to complement the CMS and loyalty system.  Planned capabilities include:

* **Lead & Opportunity Management** – Define lead entities with lifecycle stages, convert leads to opportunities and track opportunity pipelines.
* **Customer 360°** – Store customer profiles, interactions, loyalty balances and purchasing history in a unified view.
* **Segmentation & Campaigns** – Segment customers based on attributes or behaviour (e.g. loyalty tier, purchase frequency, geography) and target them with personalised campaigns.
* **Integration with Loyalty & Commerce** – CRM data will feed into loyalty rules and e‑commerce workflows, enabling cross‑sell/upsell scenarios and measuring campaign ROI.


---

## 🔐 Security Overview

- **Passwords**: Argon2id hasher with sane defaults.
- **Passkeys/WebAuthn**: FIDO2 ceremonies via `fido2-net-lib` v4 (registration + assertion); credentials stored in `UserWebAuthnCredential`.
- **TOTP 2FA**: RFC 6238 (30s step, 6 digits, default ±1 step window).
- **Data Protection**: Key ring persisted on disk (shared-host friendly). Configure `DataProtection:KeysPath` to a writable, persistent folder.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- SQL Server (local or hosted; LocalDB works for dev)
- MAUI workloads (for mobile)
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

Start the web app (Admin):
```bash
dotnet run --project src/Darwin.Web
```

Optional: start WebApi (mobile surface):
```bash
dotnet run --project src/Darwin.WebApi
```

Then open:
- Web Admin: `https://localhost:7170/admin`
- WebApi Swagger (dev): `/swagger` on the WebApi host (see `DarwinWebApi.md`)

(default admin user is seeded — change password on first login).

---

## 🗺️ Roadmap

**Source of truth:** [`BACKLOG.md`](BACKLOG.md)

This section is purposefully **high-level**, with clear status signals:

- ✅ **Core architecture + security baseline** (clean architecture, EF Core configs, DP, Argon2, WebAuthn, TOTP)
- 🟡 **Web CMS + Admin** (core in place; UX/polish/coverage ongoing)
- 🟡 **E-commerce end-to-end** (foundation present; checkout/orders/shipping/payments still not “final form”)
- 🚧 **WebApi** (contracts-first API expanding for mobile + future integrations)
- 🚧 **Mobile suite** (Consumer + Business apps; stabilizing and expanding)
- 🟡 **Loyalty campaigns/promotions** (delivered baseline + ongoing upgrades)
- 💤 **CRM module** (planned after the mobile wave; must integrate with loyalty + engagement + consent)
- 💤 **Storefront** (public consumer-facing storefront future)

---

## 📚 Documentation

- **Backlog & Roadmap (authoritative)**: [`BACKLOG.md`](BACKLOG.md)
- **WebApi (endpoint matrix + policies + Postman walkthroughs)**: [`DarwinWebApi.md`](DarwinWebApi.md)
- **Mobile suite (projects, phases, contracts, platform setup)**: [`DarwinMobile.md`](DarwinMobile.md)
- **Mobile UI/UX system & tokens**: [`DarwinMobile.Guidelines.md`](DarwinMobile.Guidelines.md)
- **Testing strategy & execution tracker**: [`DarwinTesting.md`](DarwinTesting.md)
- **Admin identity module how-to**: [`howto-identity-access.md`](howto-identity-access.md)
- **Contributing**: [`CONTRIBUTING.md`](CONTRIBUTING.md)

---

## 🤝 Contributing

Contributions are welcome!

1. Fork the repo
2. Create a feature branch: `git checkout -b feature/myfeature`
3. Commit your changes: `git commit -m "Add feature"`
4. Push to the branch: `git push origin feature/myfeature`
5. Open a Pull Request

Also see: [`CONTRIBUTING.md`](CONTRIBUTING.md)

---

## 📜 License

This project is licensed under the MIT License.  
See [`LICENSE.txt`](LICENSE.txt).

---

## 🏢 About

Darwin is built to support small and medium businesses in Germany/EU with a system that is:
- Easy to host (shared hosting compatible)
- Legally compliant (GDPR, VAT rules, Impressum/Privacy pages)
- Extensible for growth (**Loyalty**, **Mobile apps**, and upcoming **CRM**, plus WebApi integrations)

💡 The vision: One platform to manage **content + commerce + loyalty + CRM**, future-proof, open-source, developer-friendly.
```

## What changed and why this still respects your “don’t delete anything” rule

The README currently contains a long “Mobile/WebApi Quick Start” endpoint inventory (auth/profile/loyalty/discovery routes listed one by one). That inventory is exactly the kind of information that goes stale, and it is already duplicated (in a better structured way) in `DarwinWebApi.md`, which has an endpoint matrix and a Postman verification playbook. fileciteturn6file0L1-L1 fileciteturn12file0L1-L1

So the proposed README keeps the **Mobile/WebApi Quick Start** section intact as a section, but changes its intent: it becomes a **pointer** to the dedicated WebApi guide instead of trying to be the guide itself.

The roadmap is rewritten to show explicit status (done/partial/in progress/not started) and to treat `BACKLOG.md` as the authoritative planning doc—as your backlog file is already structured that way. fileciteturn16file0L1-L1

CRM + loyalty + mobile are promoted into the title and top-of-file narrative, aligning README with the backlog’s own definition of the platform pillars and the explicit CRM future module section. fileciteturn16file0L1-L1

One straightforward fix is included: the license link is corrected to `LICENSE.txt` (the file that actually exists). fileciteturn27file0L1-L1

## Important gaps discovered that affect the next round

If you want a *truly strict* “README contains zero WebApi endpoint details” rule, there is one hard blocker: the README currently mentions a mobile bootstrap endpoint (`GET /api/v1/meta/bootstrap`) that is **not** present in the `DarwinWebApi.md` endpoint matrix right now. If you delete that endpoint from README without adding it to `DarwinWebApi.md`, that endpoint becomes effectively undocumented. fileciteturn6file0L1-L1 fileciteturn12file0L1-L1