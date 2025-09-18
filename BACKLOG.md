# Darwin Backlog & Roadmap

This file tracks **planned work, features, and priorities**.  
For high-level overview, see [README.md](README.md) and [CONTRIBUTING.md](CONTRIBUTING.md).  

---

## ✅ Completed
- Solution skeleton with **Clean Architecture** (Domain, Application, Infrastructure, Web)  
- Core domain entities: Products, Categories, Pages, Settings  
- Add-on groups/options/values (with price deltas)  
- Promotions (percentage, fixed amount, conditions, limits)  
- Tax categories (VAT rates, effective from dates)  
- Redirect rules for SEO (301/302)  
- Initial Admin UI: Create/Edit for Products, Categories, Pages  
- FluentValidation integration for DTOs  
- EF Core migrations & seeding  
- Soft-delete (`IsDeleted`) and system-protected (`IsSystem`) entities  

---

## 🚧 In Progress
- **SiteSettings**: culture, supported cultures, units, SEO flags  
- Consistent Quill editor integration (Products + Pages)  
- Alerts UI (success, error, warning, info)  
- Auditing (CreatedBy, ModifiedBy, WellKnownIds)  
- Order lifecycle state machine (`Created → Confirmed → Paid → PartiallyShipped → Shipped → Delivered → Cancelled → Refunded → PartiallyRefunded`)  
- Cart + Checkout with Add-on selection & Promotion validation  

---

## 📝 Planned Next
- Complete **Admin panel** before starting public storefront UI  
- Extend SiteSettings with:  
  - SEO (canonical, hreflang, sitemap, robots.txt)  
  - Analytics integrations  
  - Feature flags  
- Cart enhancements:  
  - Line merging for identical variant + add-ons  
  - Coupon application/validation  
- Full Checkout workflow (shipping method, payment method selection)  
- Order totals & adjustments (shipping, promotions, tax)  
- Shipping provider integration (DHL first)  
- Payment provider integration (PayPal, Klarna, SEPA)  
- Outgoing webhooks (order created, payment succeeded, order shipped, etc.)  
- API v1 with Swagger/OpenAPI (public + admin endpoints)  

---

## 🔒 Identity, Security & Access Control
- Custom ASP.NET Identity with extended entities:  
  - `User`, `Role`, `Permission`, `UserRole`, `RolePermission`  
  - `PasswordResetToken`, `UserTwoFactorSecret`  
- Two-factor auth (TOTP) required for admins  
- External login providers (Google, Microsoft)  
- One-time tokens for password reset  
- System seeding of base roles & permissions (e.g., `Full Admin Access`, `Access Admin Panel`)  
- Middleware: enforce `IsSystem` protection (undeletable)  

---

## 🔮 Future Ideas
- Minimal CRM: customer profiles, preferences, consents  
- Multi-currency pricing & payments (ISO 4217 per variant & shipping method)  
- Multi-warehouse inventory with stock reservations  
- Advanced promotions with **rules engine** (category/product filters, buy X get Y, bundles)  
- Serial/batch inventory tracking for electronics/pharma  
- B2B accounts: company-level billing, shared addresses, VAT handling  
- Plugin system for 3rd-party extensions (shipping, payment, ERP connectors)  
- Admin UI improvements: inline editing, dashboards, charts  
- Middleware for `RedirectRule` (automatic 301/302)  
- Accounting integration: income, expenses, POS sales (outside webshop)  
- Extended reporting & analytics (sales, stock turnover, tax reports)  
