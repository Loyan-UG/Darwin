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
  - Rotation check tick ~15s
  - Minimum auto-refresh interval = 5 minutes while token still valid
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
- Business onboarding endpoints
- Reward configuration endpoints
- Push notification registration (device tokens)
- Extended discovery filters
- Public Catalog endpoints for future storefront use

## 3.2 Mobile Consumer App – Phase 2
- Full map integration (Google Maps / Apple MapKit)
- Business detail page
- Favorites, reviews, likes
- Feed/promotions module
- Rewards history

## 3.3 Mobile Business App – Phase 2
- Business dashboard
- Simple reporting (visits, top customers, upcoming rewards)
- Reward editing interface
- Staff roles & permissions

## 3.4 Mobile Consumer App – Phase 3
- Push notifications
- Multi-business loyalty overview
- Promotion campaigns
- Inactive user reminders

## 3.5 Mobile Business App – Phase 3
- Full analytics module (CSV/PDF export)
- Business subscription management (Stripe)
- Staff QR codes for internal access

## 3.6 Backlog Additions from Recent Mobile Implementation
- [ ] Consumer Register UX: auto-login after successful registration (or explicit redirect to login as fallback).
- [ ] Shared mobile error mapping policy: avoid showing raw exception messages in UI-bound ViewModels.
- [ ] API client no-content contract cleanup: add first-class helpers for success responses with empty body to reduce per-service workarounds.
- [ ] QR countdown UX decision: keep 15s update cadence (battery-friendly) vs 1s display refresh (smoother UI) with same 5-minute auto-rotation limit.

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
