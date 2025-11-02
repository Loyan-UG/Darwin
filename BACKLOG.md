# Darwin Backlog & Roadmap

This file tracks **planned work, features, and priorities** for the Darwin eCommerce platform.  
For a high-level overview, see [README.md](README.md).

---

## ✅ Completed
- **Clean Architecture** solution (`Domain`, `Application`, `Infrastructure`, `Web`)
- Core domain entities:
  - Products, Categories, Pages, Brands, Orders, Cart, Add-ons, Promotions, Taxes, Site Settings
- **Identity system** (custom ASP.NET Identity-compatible):
  - `User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `UserToken`, `UserLogin`, `UserTwoFactorSecret`, `UserWebAuthnCredential`
- **Security**:
  - TOTP (two-factor authentication) with secret encryption (Data Protection)
  - Password reset workflow (email token)
  - External login (Google, Microsoft)
  - WebAuthn (Passkey) registration & login
- **Application Layer**:
  - DTOs, Validators, and Handlers for all modules
  - Result-based architecture with consistent validation patterns
- **Infrastructure Layer**:
  - EF Core configurations for all entities
  - Data seeding (demo users, products, orders)
  - Argon2 password hashing
  - SMTP email sender (with planned Outlook/Office365 support)
  - Data Protection for encryption-at-rest
  - DataProtection KeyRing support for shared hosting and scalable deployments
- **Admin Panel (partial)**:
  - Categories, Pages, Products, Site Settings
  - Multilingual fields (Brand, Product)
  - Quill rich-text editor integration
- **Cross-cutting features**:
  - Soft-delete & `IsSystem` protection
  - Auditing fields (`CreatedBy`, `ModifiedBy`, timestamps)
  - `WellKnownIds` for system-level seeding
  - Application-wide validation and AutoMapper setup

---

## 🚧 In Progress
- Extended **SiteSettings**:
  - SMTP, WhatsApp, SMS, WebAuthn, Analytics, and Culture configuration
- Consistent Quill editor integration across all Admin forms
- Admin UI alert/notification system (success/error/info)
- Role and permission-based access control in Admin area
- Order workflow logic (status transitions, totals, taxes, promotions)
- Checkout pipeline (Cart → Order → Payment)
- Seeding of realistic demo data (multiple users, sample orders, categories)
- Optimization of EF performance and seeding parallelization

---

## 📝 Planned Next
- Complete **Admin UI**:
  - Roles & Permissions management
  - User management (with activation/deactivation, 2FA, Passkey management)
  - Orders management (view, update status, refunds)
  - Site Settings (SMTP, WhatsApp, SMS, Analytics)
- Implement **Public Storefront UI**:
  - Product catalog, Cart, Checkout, Register/Login, My Account
- **SEO Layer**:
  - Canonical URLs, sitemap.xml, robots.txt, hreflang generation
- **Cart Enhancements**:
  - Add-on option syncing, promo validation, tax inclusion
- **Checkout**:
  - Shipping selection, payment integration, order review page
- **Shipping Integrations**:
  - DHL, UPS (configurable via SiteSettings)
- **Payment Integrations**:
  - PayPal, Klarna, SEPA (with webhooks)
- **Background Jobs**:
  - For order status updates, email dispatch, cleanup
- **API v1**:
  - Public REST API with Swagger/OpenAPI
- **Webhooks**:
  - Order created, payment succeeded, shipment dispatched

---

## 🔒 Identity & Security
- Passwordless login (WebAuthn) — ✅ Infrastructure ready, pending Admin UI integration
- TOTP (2FA) enforcement for administrators
- Role-based and permission-based authorization filters (Darwin.Web)
- Admin area cookie isolation & security hardening
- Extend `UserToken` for “magic link” authentication
- Encrypt sensitive data at-rest using Data Protection
- Key rotation strategy for Data Protection (multi-environment safe)

---

## 🔧 Data Protection & Key Management
### Current Implementation
- ASP.NET Core **Data Protection** used for encryption of TOTP and sensitive values.
- Key ring persisted to `DataProtection:KeysPath` (configurable in `appsettings.json`).

### TODOs (for backlog)
1. **Shared Hosting Support** — use physical file system path (implemented).
2. **Multi-instance deployment** — use shared network path or blob container.
3. **Cloud-native** — for Azure/AWS, persist keys in Key Vault, Blob Storage, or Redis.
4. **Key rotation policy** — document periodic rotation in Admin docs.
5. **TODO (Deployment)** — ensure `/App_Data/keys` or configured folder exists on host.

> ⚙️ Example appsettings.json snippet:
> ```json
> "DataProtection": {
>   "KeysPath": "C:\\inetpub\\wwwroot\\Darwin\\App_Data\\keys"
> }
> ```

---

## 🔮 Future Ideas
- **CRM module**: customer profiles, preferences, consents
- Multi-currency / multi-locale pricing
- Multi-warehouse stock management
- Advanced promotion engine (rule-based)
- B2B features (company accounts, shared carts, VAT billing)
- Plugin system for external integrations
- Reporting & analytics dashboards
- Accounting connectors (QuickBooks, SAP, etc.)
- Redis caching for performance
- Background queue for async jobs (e.g., email sending)
- CMS extensions: widgets, menus, and dynamic sections

---

## 💡 Next Logical Step

> **Phase:** Web Layer Completion — Authentication & Admin Access Control

Now that the foundation layers (`Domain`, `Application`, `Infrastructure`) are complete,
the next milestone is to **finalize Darwin.Web** by implementing:

1. **Authentication UI**:
   - Login, Logout, Register (Password + Passkey + TOTP)
   - Password Reset
2. **Authorization**:
   - Role-based filters for Admin area
   - Permission-based page access
3. **Admin Enhancements**:
   - User management (activate/deactivate, 2FA toggle)
   - Role & permission management
   - Alerts system (success/error/info)
4. **Settings Management**:
   - Editable SMTP, WhatsApp, and WebAuthn settings
5. **Visual consistency**:
   - Quill editor integration across all forms
   - Unified design for admin dashboards

Once Admin is stable, proceed to the **public storefront UI** (customer-facing).

---
---
---
# Darwin Backlog – Inventory & Order Fulfillment

## Admin (Web)
- [ ] Orders: Add **Inventory Allocation** action on order details:
  - Button **Allocate Now** calls `AllocateInventoryForOrderHandler` with all lines.
  - Show TempData alert on success/failure using `Alerts.cshtml`.
  - Disable the button if already allocated (idempotent check is handled in Application; still reflect state in UI by reading ledger).
- [ ] Variants: Add **Inventory Ledger** tab:
  - Use `GetInventoryLedgerHandler` with paging.
  - Columns: CreatedAtUtc, Reason, QuantityDelta, ReferenceId.
- [ ] Returns: Add **Return Receipt** form in Admin:
  - Per-line return input (VariantId, Quantity).
  - Optional ReferenceId for idempotency.
  - Calls `ProcessReturnReceiptHandler`.
- [ ] Carts/Orders: Call **Reserve** on cart add / order submit; **Release** on removal/timeout/cancel.

## Member (Web – to do after Admin)
- [ ] Order placement path must:
  - Reserve during cart/checkout.
  - Allocate right after order confirmation (or at pick/pack stage, depending on policy).
- [ ] Returns portal:
  - Allow initiating a return request; after approval in Admin, Admin triggers `ProcessReturnReceiptHandler`.
  - Show user-friendly messages and reflect stock changes indirectly (e.g., not blocking reorders).

## Application – Future Enhancements
- [ ] Optional **soft ceiling** configuration: allow negative `available = onHand - reserved` for backorders with a feature flag.
- [ ] Batch allocation: a single command that validates all lines and rolls back if any fail (transaction scope).
- [ ] Enrich ledger reasons and standardize to enum + localized labels at Web layer.
- [ ] Reporting endpoints for stock valuation snapshots (out of scope for now).

---
---

# Darwin – Backlog (Orders & Inventory integration)

## Orders
- [ ] Admin: Add Payment form uses `AddPaymentHandler`. On success show TempData["Success"].
- [ ] Admin: Change Status action uses `UpdateOrderStatusHandler` with RowVersion.
- [ ] Admin: Orders Index consumes `OrderListItemDto` (now includes RowVersion) for concurrency-safe operations.

## Inventory Side-Effects (Next Increment)
- [ ] Implement inventory side-effects in `UpdateOrderStatusHandler`:
  - [ ] On transition to **Paid**: create `InventoryTransaction` records with reason `"OrderPaid-Reserve"`; optionally update variant-level reserved counters if present.
  - [ ] On transition to **Cancelled** (before any shipment): create `InventoryTransaction` with reason `"OrderCancelled-Release"` to undo reservations.
  - [ ] On transition to **Shipped**: create `InventoryTransaction` with reason `"OrderShipped-Finalize"` representing stock leaving warehouse.
- [ ] Ensure single-shot queries and bulk operations to avoid N+1 when touching multiple lines.

## Web (Admin)
- [ ] Wire up Alerts via `Alerts.cshtml` for all operations (Success/Error/Warning/Info).
- [ ] Orders Index: add actions for Add Payment, Change Status (with RowVersion in hidden field).
- [ ] Order Details page: render Lines/Payments/Shipments using `GetOrderForViewHandler`.

## Testing
- [ ] Unit tests for `AddPaymentHandler` (currency/amount validation, captured flow).
- [ ] Unit tests for `UpdateOrderStatusHandler` transitions and guards (cancel after shipped, etc.).
- [ ] Integration tests for optimistic concurrency conflicts using `RowVersion`.



© 2025 Darwin Commerce Platform — Internal Development Roadmap
