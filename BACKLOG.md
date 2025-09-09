# Darwin Backlog & Roadmap

This file tracks **planned work, features, and priorities**.  
For high-level overview, see [README.md](README.md).  

---

## ✅ Completed
- Solution skeleton with clean architecture  
- Core domain entities (Products, Categories, Pages, Settings)  
- Admin UI: Create/Edit for Products, Categories, Pages  
- Initial SiteSettings (partial fields)  
- FluentValidation integration  
- EF Core migrations & seeding  

---

## 🚧 In Progress
- Full SiteSettings (culture, supported cultures, units, SEO flags)  
- Consistent Quill editor integration (Products + Pages)  
- Alerts UI (success, error, warning, info)  
- Auditing (CreatedBy, ModifiedBy with WellKnownIds)  

---

## 📝 Planned Next
- Complete **Admin** panel before starting public UI  
- Extend SiteSettings with all flags (SEO, Analytics, Feature flags)  
- Add full **SEO layer**: canonical, hreflang, sitemap, robots.txt  
- Cart + Checkout implementation  
- Order lifecycle (status transitions, totals, discounts)  
- Shipping provider integration (DHL first)  
- Payment provider integration (PayPal, Klarna, SEPA)  
- Outgoing webhooks (order created, payment succeeded, etc.)  
- Public storefront UI (after Admin stable)  
- API v1 with Swagger  

---

## 🔮 Future Ideas
- Minimal CRM: customer profiles, consents, preferences  
- Multi-warehouse inventory  
- Advanced promotions (rules engine)  
- Serial/batch inventory tracking  
- Plugin system for 3rd party extensions  
- Admin UI improvements (inline editing, dashboards)  