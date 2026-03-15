# 🛒 Darwin CMS + E-Commerce Platform

[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-10.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)
[![C#](https://img.shields.io/badge/C%23-14-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/maui/)
[![Visual Studio 2026](https://img.shields.io/badge/Visual%20Studio-2026-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/)

Darwin is an integrated product platform for SMBs that combines:

- **CMS** (pages, menus, content management)
- **Commerce** (catalog, pricing, checkout, orders)
- **CRM foundation** (profiles, loyalty interactions, consent-ready architecture)
- **WebApi** (contracts-first REST surface)
- **Mobile suite** (Consumer + Business MAUI apps)

---

## 1) Documentation Hub (Start Here)

Use this README as the entry point and navigate to the focused guide you need:

- **Roadmap / delivery status** → [`BACKLOG.md`](BACKLOG.md)
- **Mobile architecture and app behavior** → [`DarwinMobile.md`](DarwinMobile.md)
- **WebApi architecture, endpoint matrix, policies, API troubleshooting** → [`DarwinWebApi.md`](DarwinWebApi.md)
- **Identity & access model (admin + member)** → [`howto-identity-access.md`](howto-identity-access.md)
- **Testing workstream tracker** → [`DarwinTesting.md`](DarwinTesting.md)
- **Contribution process** → [`CONTRIBUTING.md`](CONTRIBUTING.md)

---

## 2) Current System Scope

### 2.1 Web (CMS + Commerce + CRM-oriented back-office)

- CMS pages/menus/content operations
- catalog and pricing foundations
- identity, security, and settings management
- CRM-aligned data surfaces (profiles, loyalty-linked customer context, engagement lifecycle readiness)

### 2.2 WebApi (contracts-first)

- JWT auth and policy-protected endpoints
- loyalty scan/session workflows
- discovery/business endpoints
- profile and notification device registration
- business campaign operations and promotions feed policy diagnostics

> Full API details are maintained in `DarwinWebApi.md`.

### 2.3 Mobile Suite

- `Darwin.Mobile.Consumer`: member-side QR/discovery/rewards/profile journeys
- `Darwin.Mobile.Business`: scanner/session confirmation, dashboard/reporting, campaign operations
- shared runtime: `Darwin.Mobile.Shared`

> Mobile behavior and operational rules are maintained in `DarwinMobile.md`.

---

## 3) Solution Architecture

```
src/
├─ Darwin.Domain           → domain model and core entities
├─ Darwin.Application      → use-cases, handlers, validators, DTO mapping
├─ Darwin.Infrastructure   → EF Core, persistence, identity/security infra
├─ Darwin.Web              → MVC/Razor host (admin + web surface)
├─ Darwin.WebApi           → REST API host
├─ Darwin.Shared           → shared utility/result types
├─ Darwin.Contracts        → public request/response contracts
├─ Darwin.Mobile.Shared    → shared mobile API/client/runtime services
├─ Darwin.Mobile.Consumer  → consumer MAUI app
└─ Darwin.Mobile.Business  → business MAUI app
```

Key engineering principles:

- clean architecture boundaries
- contracts-first integration for clients
- optimistic concurrency (`RowVersion`) where required
- secure-by-default auth/session patterns
- mobile UI thread safety for UI-bound state updates

---

## 4) Build & Run Quick Start

### Prerequisites

- .NET 10 SDK
- SQL Server / LocalDB
- MAUI workloads (for mobile)

### Web setup (quick)

```bash
git clone https://github.com/shahramvafadar/Darwin.git
cd Darwin
dotnet ef database update --project src/Darwin.Infrastructure
dotnet run --project src/Darwin.Web
```

### Solution filters

- `Darwin.WebOnly.slnf` → Web + WebApi focused work
- `Darwin.MobileOnly.slnf` → Mobile focused work

---

## 5) CRM Focus (Why it matters here)

CRM is a core direction of this platform, not a side topic.

Current foundation already present in the system:

- customer profile lifecycle (`/profile/me`, concurrency-safe updates)
- loyalty/account interactions as structured customer engagement signals
- campaign/promotions operations with delivery diagnostics
- push-device registration and engagement-ready telemetry surfaces

Planned CRM deepening (tracked in backlog):

- richer segmentation/audience rules
- campaign operations maturity
- engagement automation hardening and observability

Use `BACKLOG.md` as the source of truth for CRM delivery status.

---

## 6) Security Snapshot

- Argon2id password hashing
- WebAuthn/passkey and TOTP foundations
- JWT with policy-based authorization in WebApi
- data protection key persistence support for shared hosting

---

## 7) Contribution Rules

- Keep docs synchronized with code changes in every meaningful PR.
- If endpoint/contracts/policies change:
  1. update Contracts + server mapping
  2. update `DarwinWebApi.md`
  3. update mobile docs only for app-side impact
  4. update backlog status (`BACKLOG.md`)

