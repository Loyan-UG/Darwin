# 🛠️ Contributing to Darwin

Thank you for your interest in contributing to **Darwin** – a modular e-commerce & CMS platform.  
This document describes **how to contribute safely and consistently**, covering style, architecture, and workflow.

---

## 📐 Project Architecture

Darwin follows **Clean Architecture** + **DDD (Domain-Driven Design)** principles:

- **Darwin.Domain**  
  Pure business entities and enums.  
  No EF Core / no infrastructure dependencies.  
  Entities inherit from `BaseEntity` (Id, CreatedAtUtc, ModifiedAtUtc, IsDeleted).  

- **Darwin.Application**  
  Use-cases, DTOs, services, validators.  
  Uses abstractions like `IAppDbContext` for persistence.  
  No direct dependency on EF Core or ASP.NET Core.  

- **Darwin.Infrastructure**  
  EF Core, Identity, caching, file storage, external services.  
  Implements `IAppDbContext`.  

- **Darwin.Web**  
  ASP.NET Core MVC application (Admin Area + Storefront).  
  Razor Views, Controllers, and UI assets.  

---

## 🧭 Entity & Aggregate Guidelines

1. **Aggregates** = clear boundaries (e.g., `Product`, `Order`, `User`).  
2. **Navigation Properties**  
   - ✅ Inside an aggregate → keep navigation collections.  
   - ❌ Across aggregates → store FK/IDs + snapshots instead (avoid loading large graphs).  
   - ✅ Identity (User/Role/Permission) always keeps navigation both ways.  
3. **Constructors**  
   - All `sealed` entities → **private parameterless constructor** for EF Core.  
   - Domain logic uses explicit constructors or factory methods.  
4. **System Entities**  
   - Critical records (e.g., `Admin` role, `Full Admin Access` permission) are seeded with `IsSystem = true`.  
   - Such records **cannot be deleted** through UI or API.  

---

## ✨ Coding Style

### C#  
- Use **`sealed`** for all entities and validators (no inheritance by default).  
- Use **PascalCase** for properties, **camelCase** for locals.  
- All classes require **XML summary comments** (purpose, behavior, edge cases).  
- Avoid abbreviations except for common standards (e.g., `Utc`, `Id`).  
- One class per file, namespace = folder path.

### Razor Views  
- Use tag helpers (`asp-for`, `asp-action`, …) instead of raw HTML attributes where possible.  
- All forms must include `@Html.AntiForgeryToken()`.  
- Shared partial `_Alerts.cshtml` must support all four message types (`success`, `error`, `warning`, `info`).  

### JavaScript  
- Use **Vanilla JS or lightweight libs only** (no jQuery for new code unless necessary).  
- For editors, we use **Quill.js v2**.  
- Always wrap scripts in `DOMContentLoaded`.  

---

## 🗄️ EF Core & Persistence

- `IAppDbContext` is the abstraction; use `Set<T>()` + `SaveChangesAsync()`.  
- Do **not** expose `Entry()` in `IAppDbContext`. Shadow property handling (e.g., `RowVersion`, audit fields) stays inside Infrastructure.  
- Migrations:  
  - Run with `dotnet ef` via `DesignTimeDbContextFactory`.  
  - Migrations are stored in `Darwin.Infrastructure/Migrations`.  
- Soft-delete: Entities have `IsDeleted`. Queries must use `AsNoTracking().Where(e => !e.IsDeleted)` unless explicitly retrieving deleted rows.  

---

## 🔒 Security & Identity

- Based on **custom ASP.NET Identity** (not the default schema).  
- Entities: `User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `PasswordResetToken`, `UserTwoFactorSecret`.  
- All support `IsSystem`.  
- Two-factor required for Admins.  
- Password resets use one-time tokens.  
- External logins (Google, Microsoft) are supported.  

---

## 🧪 Testing

- Unit tests for all Application handlers and validators.  
- Integration tests for EF queries.  
- Use in-memory provider only in tests, not production.  
- For every bug fix, write a regression test.  

---

## 🔀 Git & Workflow

- **Default branch**: `main` (stable).  
- **Feature branches**: `feature/<name>`.  
- **Bugfix branches**: `fix/<issue#>-<name>`.  
- Pull requests require:  
  - Passing CI tests.  
  - Code review approval.  
  - Linked issue.  
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):  


---

## 🧾 Documentation

- Each entity/service must have an **XML summary** explaining purpose, usage, and design intent.  
- README.md covers business + technical overview.  
- CONTRIBUTING.md (this file) defines rules.  
- Backlog.md tracks roadmap features and deferred tasks.  

---

## 🚀 Backlog & Future Work

- Multi-currency & multi-language full support.  
- Extended reporting & analytics.  
- Accounting integration.  
- B2B company accounts.  
- Plug-in architecture for payment/shipping providers.  
- Advanced promotions/discount engine.  
- Middleware for RedirectRules (SEO).  

---

## 🤝 How to Contribute

1. Fork the repo.  
2. Create a feature branch (`git checkout -b feature/my-feature`).  
3. Commit changes with tests + docs.  
4. Push and open a PR against `main`.  
5. Participate in code review.  

---

## 📝 Code Review Checklist

Before PR approval, check:

- ✅ Naming consistent with architecture.  
- ✅ No direct EF Core dependencies in Application.  
- ✅ All new classes documented.  
- ✅ Validators exist for all new DTOs.  
- ✅ Tests added or updated.  
- ✅ UI respects Bootstrap + shared partials.  
- ✅ Security impact considered (auth, permissions).  

---

## ❤️ Community Guidelines

- Be respectful in discussions and reviews.  
- Always explain design decisions in PRs.  
- Prefer improvements that reduce **future maintenance cost** over short fixes.  
- Remember: Darwin is designed for **multiple merchants, multiple markets**, long-term maintainability is key.
