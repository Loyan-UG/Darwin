# Contributing Guide

Thank you for considering contributing to **Darwin**.  
This document describes the project structure, coding standards, branching rules, review expectations, and the current development status across all modules.

This guide is meant to give new team members a complete orientation so they can begin contributing immediately without requiring additional context.

---

# 1. Project Overview

Darwin is a modular, multi-tenant capable CMS + E-Commerce + CRM platform with an additional **Loyalty system** and **mobile suite**.

The solution is built on:

- **C# 13 + .NET 9**
- **Clean Architecture / Onion Architecture**
- **Entity Framework Core**
- **ASP.NET Core MVC + WebApi**
- **.NET MAUI Mobile Applications**
- **Contracts-first WebApi design**
- **Data Protection, Argon2, TOTP, WebAuthn**
- **Solution Filters** for splitting Web and Mobile development

All repositories follow strict separation of concerns and consistency across layers.

---

# 2. Repository Structure

src/
Darwin.Domain
Darwin.Application
Darwin.Infrastructure
Darwin.Web
Darwin.WebApi
Darwin.Shared
Darwin.Contracts
Darwin.Mobile.Shared
Darwin.Mobile.Consumer
Darwin.Mobile.Business

tests/
Darwin.Tests.Unit
Darwin.Tests.Integration



### 2.1 Core Platform (Web + Server)
| Project | Description | Status |
|--------|-------------|--------|
| **Darwin.Domain** | Entities, Value Objects, Enums, domain rules | **Stable / Complete** |
| **Darwin.Application** | Handlers, DTOs, Validators | **Active** (ongoing additions when new features are added) |
| **Darwin.Infrastructure** | EF Core, DbContexts, Seed, DI composition, security, Data Protection | **Stable / Complete** |
| **Darwin.Web** | MVC Admin panel, CMS, Store modules | **Active** |
| **Darwin.WebApi** | Public API for mobile / storefront | **In Progress** |

### 2.2 Shared Libraries
| Project | Description | Status |
|--------|-------------|--------|
| **Darwin.Shared** | Result wrappers, common helpers | **Stable** |
| **Darwin.Contracts** | DTOs for WebApi + Mobile, Contracts-first design | **Active** (being extended for Loyalty + Discovery) |

### 2.3 Mobile Suite (MAUI)
| Project | Description | Status |
|--------|-------------|--------|
| **Darwin.Mobile.Shared** | HTTP client, retry policies (Polly-style), token storage, scanning/location abstractions | **Active** |
| **Darwin.Mobile.Consumer** | Consumer-facing mobile app (QR, rewards, discover, profile) | **In Progress** |
| **Darwin.Mobile.Business** | Business tablet app (scan, accrue, redeem) | **In Progress** |

---

# 3. Development Guidelines

## 3.1 Coding Standards
All code **must** follow:

- C# 13 features allowed  
- **Nullable Reference Types ON**
- **sealed classes** for all Entities and Handlers
- **records only for DTOs**
- **Async/await everywhere**
- **XML documentation comments required**  
  - On all classes  
  - On all methods  
  - On important internal code blocks  
- **No Persian comments inside code**
- **TODO only for long-term tasks**, not for cleanup or things that should be done now
- **Never use “magic strings”**—everything must be constant, enum, or strongly typed

## 3.2 Architecture Rules

- **Domain must have zero external dependencies**
- **Application only depends on Domain**
- **Infrastructure depends on Application**
- **Web and WebApi depend on both Application + Infrastructure**
- **Mobile apps depend only on Darwin.Contracts + Darwin.Mobile.Shared**

No project may “reach across” layers.  
No Domain type should ever leak into WebApi or Mobile.

## 3.3 Git & Branching

- `main` — stable, deployable
- `develop` — active integration branch
- feature branches:  
  `feature/<name>`  
- bugfix branches:  
  `fix/<issue>`
- mobile-specific features:  
  `mobile/<name>`

All PRs must be reviewed and pass tests.

---

# 4. Pull Request Requirements

Before submitting a PR:

1. **Run all tests** (unit + integration).
2. Ensure:
   - No un-commented TODOs
   - No Persian text in code
   - All classes/methods contain clean XML-doc
   - No unused using statements
3. Verify:
   - No circular dependencies
   - No memory leaks (async disposal)
   - No large objects in Shared projects
4. All public changes must match the Contracts-first approach:
   - Any request/response that leaves the server **must** be defined in `Darwin.Contracts`.
   - WebApi may never expose Application DTOs.

---

# 5. Working With Solution Filters

Two predefined Solution Filters help reduce IDE load:

### `Darwin.WebOnly.slnf`
Contains:
- Domain
- Application
- Infrastructure
- Web
- WebApi
- Shared
- Contracts

### `Darwin.MobileOnly.slnf`
Contains:
- Mobile.Shared
- Mobile.Business
- Mobile.Consumer
- Contracts

Mobile developers **must** use the Mobile solution filter to avoid loading server-side code.

---

# 6. Current Development Status (Important)

### ✔ Completed
- Core Domain model
- Application architecture
- Infrastructure (EF, security, DP, seed)
- CMS + Catalog base implementation
- SiteSettings full implementation
- Shared libraries (Darwin.Shared)
- JWT, WebAuthn, TOTP authentication stack

### 🟡 In Progress (Active Work)
- **Darwin.WebApi**
- **Darwin.Contracts expansion** (Loyalty, Discovery, Maps)
- **Darwin.Mobile.Shared** (retry, storage, integration abstractions)
- **Darwin.Mobile.Consumer** UI + integration
- **Darwin.Mobile.Business** QR scanning & loyalty operations
- Admin improvements
- Web storefront (initial scaffolding)

### ⛔ Not Started Yet
- Native push notifications (mobile)
- Full analytics dashboard
- In-app payments (Stripe)
- Offline outbox for mobile sync
- Notification center in Admin
- ERP integrations (future)

---

# 7. Mobile Contribution Rules

## 7.1 MAUI Structure

Each mobile app must follow:

/Views
/ViewModels
/Services
/Platform
/Composition (DI)
/Resources


## 7.2 Shared Library Requirements

**Darwin.Mobile.Shared** must contain:
- HTTP typed client
- Retry policy helper
- TokenStore abstraction
- Auth helper services
- Scanner + Location abstractions

It must **not** contain:
- Any database layer
- Entities
- Views/ViewModels

## 7.3 API Boundary

Mobile must only communicate with WebApi via `Darwin.Contracts`.

No Application DTOs, no Infrastructure entities, no server-internal types.

---

# 8. Testing Requirements

- Use **SQLite In-Memory** for EF tests (not InMemory provider).
- Domain tests must validate value-object rules and aggregate invariants.
- WebApi integration tests must verify:
  - Authentication flows (JWT, refresh, lockout)
  - Loyalty scan flows (QR rotation, start-scan, accrue)
  - Business discovery APIs

Mobile tests (if added):
- Should mock `IApiClient`
- Should not call real WebApi

---

# 9. How to Add New Features

1. Define DTOs in `Darwin.Contracts`
2. Add WebApi endpoints that consume them
3. Implement Application handlers
4. Wire Infrastructure (if needed)
5. Update WebApi DI
6. Update Mobile.Shared (if needed)
7. Implement UI flows in Mobile apps

Always follow **Contracts-first** design.

---

# 10. Communication & Code Reviews

- All discussions in English  
- PR reviews must be direct, explicit, and thorough  
- Architectural changes require approval  
- Breaking changes to Contracts require:
  - Version bump  
  - Changelog entry  
  - Mobile team notification  

---

# 11. License & Ownership

All code is private and owned by Darwin’s core team.  
Commercial re-use outside the organization is prohibited.

---

# 12. Final Notes

- Write code as if it is the **final production version**, not a prototype.  
- Avoid shortcuts.  
- Structure matters as much as correctness.  
- Consistency between Web + Mobile is critical.

Thank you for contributing.  
Welcome to the Darwin team!  
