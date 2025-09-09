# 🛒 Darwin CMS + E-Commerce Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet?logo=dotnet)](https://dotnet.microsoft.com/)  
[![EF Core](https://img.shields.io/badge/EntityFrameworkCore-9.0-512BD4?logo=nuget)](https://learn.microsoft.com/ef/)  
[![Build](https://img.shields.io/github/actions/workflow/status/YOURORG/Darwin/build.yml?branch=main&logo=githubactions&label=CI)](../../actions)  
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)  

Darwin is a **modern CMS + e-commerce solution** designed for **European SMBs** who need a  
flexible, future-proof platform that runs even on **shared hosting**.  

It combines **content management (CMS)** and **full e-commerce features** such as catalog, pricing, inventory, cart, checkout, orders, shipping, and payments — all built with **clean architecture** and extensibility in mind.  

---

## ✨ Features

- 📝 **CMS**: Pages, rich text editor (Quill), SEO meta, menus, media library.  
- 🛍️ **Catalog**: Brands, categories, products, product variants, attributes.  
- 📦 **Inventory**: Stock tracking, reserved qty, reorder levels, warehouses (future).  
- 💶 **Pricing & Tax**: EU VAT (DE by default), multiple price fields, promotions, coupons.  
- 🛒 **Cart & Checkout**: Guest carts, min/max qty rules, VAT calculations.  
- 📑 **Orders**: Full lifecycle (Created → Paid → Shipped → Refunded).  
- 🚚 **Shipping**: DHL integration (DE first), extensible provider model.  
- 💳 **Payments**: PayPal, Klarna/Sofort, SEPA (future Stripe).  
- 🌍 **Localization**: Multi-culture content (normalized translation tables),  
  default: `de-DE` with support for more cultures via SiteSettings.  
- 🔍 **SEO**: Unique slugs per culture, meta tags, canonical, hreflang, sitemap, robots.txt.  
- 🧩 **Extensibility**: Feature flags, outgoing webhooks, modular architecture.  
- 🛡️ **Security**: XSS sanitization, upload hardening, GDPR consent & privacy pages.  
- 📊 **Analytics**: Google Analytics, Tag Manager, Search Console (via settings).  
- 🧪 **Testing**: Unit + Integration tests, GitHub Actions CI.  

---

## 🏗️ Architecture

Darwin follows a **clean architecture** with clear separation of concerns:

src/
├─ Darwin.Domain → Entities, ValueObjects, Enums, BaseEntity
├─ Darwin.Application → Use cases, DTOs, Handlers, Validators
├─ Darwin.Infrastructure→ EF Core, DbContext, Migrations, DataSeeder
├─ Darwin.Web → MVC + Razor (Admin + Public), DI, Middleware
├─ Darwin.WebApi → Reserved for REST API (future v1)
└─ Darwin.Shared → Result wrappers, constants, helpers


### Key Principles
- **SOLID** principles applied consistently.  
- **Minor units for money** (`long` cents) to avoid floating-point errors.  
- **Audit fields** (`CreatedAtUtc`, `ModifiedAtUtc`, `CreatedByUserId`, `ModifiedByUserId`).  
- **Soft delete** with `IsDeleted`.  
- **Optimistic concurrency** via `RowVersion`.  
- **Normalized translation tables** for multilingual content.  

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/)  
- SQL Server (local or hosted; LocalDB works for dev)  
- Node/npm (optional, for front-end tooling)

### Local Setup

```bash
# clone the repo
git clone https://github.com/YOURORG/Darwin.git
cd Darwin

# configure connection string in appsettings.Development.json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=Darwin;Trusted_Connection=True;"
}

# run migrations & seed
dotnet ef database update --project src/Darwin.Infrastructure

# start the app
dotnet run --project src/Darwin.Web

Then open https://localhost:7170/admin
(default admin user is seeded — change password on first login).

🗺️ Roadmap

See BACKLOG.md
 for the full backlog and feature roadmap.

High-level milestones:

 Skeleton solution with clean architecture

 Core entities (Domain)

 Products, Categories, Pages (Admin)

 ✅ Full SiteSettings (culture, units, SEO, feature flags)

 SEO features (canonical, hreflang, sitemap, robots)

 Cart & Checkout flows

 Orders lifecycle + payments + shipping

 Webhooks (outgoing & incoming)

 Public storefront UI (after Admin completion)

 API v1 (REST with Swagger)

 Minimal CRM (user profiles, consents)

📚 Documentation

Setup Guide

Architecture Decisions

Styleguide & Conventions

Backlog & Roadmap

🤝 Contributing

Contributions are welcome!

Fork the repo

Create a feature branch (git checkout -b feature/myfeature)

Commit your changes (git commit -m 'Add feature')

Push to the branch (git push origin feature/myfeature)

Open a Pull Request

📜 License

This project is licensed under the MIT License
.

🏢 About

Darwin is built to support small and medium businesses in Germany/EU
with a system that is:

Easy to host (shared hosting compatible)

Legally compliant (GDPR, VAT rules, Impressum/Privacy pages)

Extensible for growth (CRM, webhooks, API, integrations)

💡 The vision: One platform to manage content + commerce,
future-proof, open-source, developer-friendly.