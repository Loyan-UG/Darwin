<img src="./src/Darwin.WebAdmin/wwwroot/images/DarwinJustLogo.png" width="64" style="vertical-align: middle;" />

# Darwin Platform - CMS + CRM + E-Commerce + Loyalty + Mobile

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-10.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Visual Studio 2026](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/)
[![Build](https://img.shields.io/github/actions/workflow/status/shahramvafadar/Darwin/build.yml?branch=master&logo=githubactions&label=CI)](../../actions)

Darwin is a unified platform that combines a content management system (CMS) with robust eÃ¢â‚¬â€˜commerce, a flexible loyalty engine, a forthcoming customer relationship management (CRM) module and modern .NET MAUI mobile apps. The goal of the platform is to allow businesses to manage content, products, loyalty programmes and customer engagement from a single codebase while exposing a RESTful WebApi for integration and providing firstÃ¢â‚¬â€˜party mobile experiences for both consumers and business staff. ContractÃ¢â‚¬â€˜first development ensures that mobile apps, WebApi and web portals all stay in sync through the shared Darwin.Contracts library.

## Ã°Å¸â€œÅ’ Documentation Map (Start Here)

Use this README as the entry point, then jump to the specialized docs:

- **Backlog & Roadmap (source of truth)**: [`BACKLOG.md`](BACKLOG.md)
- **WebApi technical guide (endpoints, policies, Postman playbook)**: [`DarwinWebApi.md`](DarwinWebApi.md)
- **Mobile technical guide (Consumer/Business apps + Contracts)**: [`DarwinMobile.md`](DarwinMobile.md)
- **Mobile UI/UX guidelines**: [`DarwinMobile.Guidelines.md`](DarwinMobile.Guidelines.md)
- **Testing strategy & test execution tracker**: [`DarwinTesting.md`](DarwinTesting.md)
- **Admin Identity module how-to (Users/Roles/Permissions)**: [`howto-identity-access.md`](howto-identity-access.md)
- **Contributing**: [`CONTRIBUTING.md`](CONTRIBUTING.md)

## Ã¢Å“â€¦ Current Status Snapshot

This is a **high-level snapshot**. For detailed planning and the authoritative status per workstream, use `BACKLOG.md`.

## Status Snapshot

| Pillar                     | Status         | Notes                                                                                                                                                                                                       |
| -------------------------- | -------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Domain & Application**   | Ã¢Å“â€Ã¯Â¸Å½ Complete    | Clean architecture implemented with Commands/Queries, DTOs and validators.                                                                                                                                  |
| **Infrastructure**         | Ã¢Å“â€Ã¯Â¸Å½ Complete    | Persistence (EFÃ‚Â Core), Identity & JWT, DataÃ‚Â Protection, Notifications.                                                                                                                                      |
| **Web CMS & EÃ¢â‚¬â€˜Commerce**   | Ã¢â€“Â¶Ã¯Â¸Å½ InÃ‚Â Progress   | Content & page management, product catalogue, cart & order flows.                                                                                                                                           |
| **Loyalty System**         | Ã¢â€“Â¶Ã¯Â¸Å½ PhaseÃ‚Â 3+    | ScanÃ¢â‚¬â€˜session based accrual/redemption, rewards & campaigns, serverÃ¢â‚¬â€˜side guardrails, push notifications and analytics implemented. Remaining work: advanced campaign editor & dispatch/suppression workflow. |
| **REST WebApi**            | Ã¢â€“Â¶Ã¯Â¸Å½ Mature      | All mobileÃ¢â‚¬â€˜facing and admin endpoints delivered; continuous improvements & new endpoints are tracked in the backlog.                                                                                        |
| **Mobile Apps (Consumer)** | Ã¢â€“Â¶Ã¯Â¸Å½ PhaseÃ‚Â 3+    | Login/register, QR display, discovery & map, rewards dashboard, promotions feed, push registration and analytics done; improvements in progress (multiÃ¢â‚¬â€˜business wallet, reminders).                         |
| **Mobile Apps (Business)** | Ã¢â€“Â¶Ã¯Â¸Å½ PhaseÃ‚Â 3+    | QR scanning, point accrual/redemption, reward management, dashboard & reporting, business campaign editor implemented; enhancements to campaign editor and reminder workflows planned.                      |
| **CRM Module**             | Ã¢Å¡â„¢Ã¯Â¸Å½ Planned     | Entities and flows for leads, contacts, opportunities and segmentation under design; integration with loyalty and commerce to follow.                                                                       |
| **Storefront**             | Ã¢Å¡â„¢Ã¯Â¸Å½ Planned     | CustomerÃ¢â‚¬â€˜facing shop interface (ASP.NETÃ‚Â MVC/Blazor) with checkout and payment integration.                                                                                                                  |
| **Testing & QA**           | Ã¢â€“Â¶Ã¯Â¸Å½ InÃ‚Â Progress | Integration, contract and concurrency tests exist; additional coverage for new features and pushÃ¢â‚¬â€˜sync scenarios planned.                                                                                    |

---

## Ã¢Å“Â¨ Features

- Ã°Å¸â€œÂ **CMS**: Pages, rich text editor (Quill), SEO meta, menus, media library.
- Ã°Å¸â€ºÂÃ¯Â¸Â **Catalog**: Brands, categories, products, variants, attributes.
- Ã°Å¸â€œÂ¦ **Inventory**: Stock tracking, reserved qty, reorder levels, warehouses (future).
- Ã°Å¸â€™Â¶ **Pricing & Tax**: EU VAT (DE by default), promotions, coupons.
- Ã°Å¸â€ºâ€™ **Cart & Checkout**: Guest carts, min/max qty rules, VAT calculations.
- Ã°Å¸â€œâ€˜ **Orders**: Full lifecycle (Created Ã¢â€ â€™ Paid Ã¢â€ â€™ Shipped Ã¢â€ â€™ Refunded).
- Ã°Å¸Å¡Å¡ **Shipping**: DHL (DE first), extensible provider model.
- Ã°Å¸â€™Â³ **Payments**: PayPal, Klarna/Sofort, SEPA (future Stripe).
- Ã°Å¸Å’Â **Localization**: Multi-culture content via normalized translations.
- Ã°Å¸â€Â **SEO**: Unique slugs per culture, canonical, hreflang, sitemap, robots.
- Ã°Å¸Â§Â© **Extensibility**: Feature flags, outgoing webhooks, modular architecture.
- Ã°Å¸â€ºÂ¡Ã¯Â¸Â **Security**: XSS sanitization, upload hardening, GDPR consent & privacy pages.
  - **Argon2id** password hashing
  - **Passkeys/WebAuthn** (FIDO2 via fido2-net-lib v4) for login/registration
  - **TOTP 2FA** (RFC 6238)
  - **Data Protection** key ring persisted for shared hosting
- Ã°Å¸â€œÅ  **Analytics**: Google Analytics, Tag Manager, Search Console (via settings).
- Ã°Å¸Â§Âª **Testing**: Unit + Integration tests, GitHub Actions CI.
- Ã°Å¸ÂªÂª **Identity & Access (Admin + Mobile)**: roles/permissions, concurrency patterns, and member self-service flows.
- Ã°Å¸Å½Â **Loyalty & Engagement**:
  - Session-based QR (opaque scan token), accrual, redemption
  - Rewards tiers and business-facing management (evolving)
  - Campaigns/promotions feed & engagement tracking (in progress; see `BACKLOG.md`)
- Ã°Å¸â€œÂ± **Mobile (MAUI)**:
  - Consumer app (discover, QR, rewards, profile, promotions)
  - Business app (scan, accrue, redeem, dashboard/reporting)
- Ã°Å¸â€â€” **API & Contracts**:
  - Public **Darwin.WebApi** (JWT, Swagger) using **Darwin.Contracts** as the single source of request/response models for Web + Mobile.
- Ã°Å¸Â¤Â **CRM (next major pillar)**:
  - Customer profiles + segmentation + activity timeline
  - Consent & GDPR workflows
  - Outreach automations (email/SMS/WhatsApp) planned

---

## Ã°Å¸Ââ€”Ã¯Â¸Â Architecture

Darwin follows a **clean architecture** with clear separation of concerns:

```
src/
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Domain           Ã¢â€ â€™ Entities, ValueObjects, Enums, BaseEntity
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Application      Ã¢â€ â€™ Use cases, DTOs, Handlers, Validators
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Infrastructure   Ã¢â€ â€™ EF Core, DbContext, Migrations, DataSeeder
Ã¢â€Å“Ã¢â€â‚¬ Darwin.WebAdmin              Ã¢â€ â€™ MVC + Razor (Admin + Public), DI, Middleware
Ã¢â€Å“Ã¢â€â‚¬ Darwin.WebApi           Ã¢â€ â€™ Public REST API (mobile + future integrations)
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Worker           Ã¢â€ â€™ Background jobs / schedulers (where applicable)
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Shared           Ã¢â€ â€™ Result wrappers, constants, helpers
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Contracts        Ã¢â€ â€™ Shared DTOs for WebApi + Mobile (request/response contracts)
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Mobile.Shared    Ã¢â€ â€™ Mobile shared services (HTTP client, retry, auth, scanner/location abstractions)
Ã¢â€Å“Ã¢â€â‚¬ Darwin.Mobile.Consumer  Ã¢â€ â€™ .NET MAUI consumer app (QR, discover, rewards, profile)
Ã¢â€â€Ã¢â€â‚¬ Darwin.Mobile.Business  Ã¢â€ â€™ .NET MAUI business app (scan, accrue, redeem, dashboard)
```

### Key Principles
- **SOLID** applied consistently.
- **Minor units for money** (`long` cents) to avoid floating-point errors.
- **Audit fields** (`CreatedAtUtc`, `ModifiedAtUtc`, `CreatedByUserId`, `ModifiedByUserId`).
- **Soft delete** with `IsDeleted`.
- **Optimistic concurrency** via `RowVersion`.
- **Normalized translation tables** for multilingual content.

### Composition

- **Web composition root**: `src/Darwin.WebAdmin/Extensions/DependencyInjection.cs`
  - `AddSharedHostingDataProtection(configuration)`
  - `AddPersistence(configuration)`
  - `AddIdentityInfrastructure()`
  - `AddNotificationsInfrastructure(configuration)`
- **WebApi composition root**: `src/Darwin.WebApi/Extensions/DependencyInjection.cs`
  - Adds Application, Persistence, JWT auth core, HttpContextAccessor, `ICurrentUserService`, Swagger (dev), RateLimiter, Controllers.
  - Policies: `perm:AccessMemberArea`, `perm:AccessLoyaltyBusiness`.

---

## Ã°Å¸â€œÂ± Mobile Overview

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

* **Lead & Opportunity Management** Ã¢â‚¬â€œ Define lead entities with lifecycle stages, convert leads to opportunities and track opportunity pipelines.
* **CustomerÃ‚Â 360Ã‚Â°** Ã¢â‚¬â€œ Store customer profiles, interactions, loyalty balances and purchasing history in a unified view.
* **Segmentation & Campaigns** Ã¢â‚¬â€œ Segment customers based on attributes or behaviour (e.g. loyalty tier, purchase frequency, geography) and target them with personalised campaigns.
* **Integration with Loyalty & Commerce** Ã¢â‚¬â€œ CRM data will feed into loyalty rules and eÃ¢â‚¬â€˜commerce workflows, enabling crossÃ¢â‚¬â€˜sell/upsell scenarios and measuring campaign ROI.


---

## Ã°Å¸â€Â Security Overview

- **Passwords**: Argon2id hasher with sane defaults.
- **Passkeys/WebAuthn**: FIDO2 ceremonies via `fido2-net-lib` v4 (registration + assertion); credentials stored in `UserWebAuthnCredential`.
- **TOTP 2FA**: RFC 6238 (30s step, 6 digits, default Ã‚Â±1 step window).
- **Data Protection**: Key ring persisted on disk (shared-host friendly). Configure `DataProtection:KeysPath` to a writable, persistent folder.

---

## Ã°Å¸Å¡â‚¬ Getting Started

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
dotnet run --project src/Darwin.WebAdmin
```

Optional: start WebApi (mobile surface):
```bash
dotnet run --project src/Darwin.WebApi
```

Then open:
- Web Admin: `https://localhost:7170/`
- WebApi Swagger (dev): `/swagger` on the WebApi host (see `DarwinWebApi.md`)

(default admin user is seeded Ã¢â‚¬â€ change password on first login).

---

## Ã°Å¸â€”ÂºÃ¯Â¸Â Roadmap

**Source of truth:** [`BACKLOG.md`](BACKLOG.md)

This section is purposefully **high-level**, with clear status signals:

- Ã¢Å“â€¦ **Core architecture + security baseline** (clean architecture, EF Core configs, DP, Argon2, WebAuthn, TOTP)
- Ã°Å¸Å¸Â¡ **Web CMS + Admin** (core in place; UX/polish/coverage ongoing)
- Ã°Å¸Å¸Â¡ **E-commerce end-to-end** (foundation present; checkout/orders/shipping/payments still not Ã¢â‚¬Å“final formÃ¢â‚¬Â)
- Ã°Å¸Å¡Â§ **WebApi** (contracts-first API expanding for mobile + future integrations)
- Ã°Å¸Å¡Â§ **Mobile suite** (Consumer + Business apps; stabilizing and expanding)
- Ã°Å¸Å¸Â¡ **Loyalty campaigns/promotions** (delivered baseline + ongoing upgrades)
- Ã°Å¸â€™Â¤ **CRM module** (planned after the mobile wave; must integrate with loyalty + engagement + consent)
- Ã°Å¸â€™Â¤ **Storefront** (public consumer-facing storefront future)

---

## Ã°Å¸â€œÅ¡ Documentation

- **Backlog & Roadmap (authoritative)**: [`BACKLOG.md`](BACKLOG.md)
- **WebApi (endpoint matrix + policies + Postman walkthroughs)**: [`DarwinWebApi.md`](DarwinWebApi.md)
- **Mobile suite (projects, phases, contracts, platform setup)**: [`DarwinMobile.md`](DarwinMobile.md)
- **Mobile UI/UX system & tokens**: [`DarwinMobile.Guidelines.md`](DarwinMobile.Guidelines.md)
- **Testing strategy & execution tracker**: [`DarwinTesting.md`](DarwinTesting.md)
- **Admin identity module how-to**: [`howto-identity-access.md`](howto-identity-access.md)
- **Contributing**: [`CONTRIBUTING.md`](CONTRIBUTING.md)

---

## Ã°Å¸Â¤Â Contributing

Contributions are welcome!

1. Fork the repo
2. Create a feature branch: `git checkout -b feature/myfeature`
3. Commit your changes: `git commit -m "Add feature"`
4. Push to the branch: `git push origin feature/myfeature`
5. Open a Pull Request

Also see: [`CONTRIBUTING.md`](CONTRIBUTING.md)

---


## Ã°Å¸ÂÂ¢ About

Darwin is built to support small and medium businesses in Germany/EU with a system that is:
- Easy to host (shared hosting compatible)
- Legally compliant (GDPR, VAT rules, Impressum/Privacy pages)
- Extensible for growth (**Loyalty**, **Mobile apps**, and upcoming **CRM**, plus WebApi integrations)

Ã°Å¸â€™Â¡ The vision: One platform to manage **content + commerce + loyalty + CRM**, future-proof and developer-friendly.
