# Package Change Rules

Reviewed: 2026-06-19

These rules define how package, plan, add-on, and tenant entitlement changes should behave. They are design rules only and do not change runtime behavior.

## Core Rules

| Rule | Business impact | Technical impact |
| --- | --- | --- |
| Packages assign entitlement; gates enforce behavior. | A customer buys a bundle, but the system still needs runtime checks to honor it. | `PlatformPackage` and `PlatformPlan` should feed capability gates through assignment records. |
| Capability enablement is not permission grant. | Buying Finance does not let every user see Finance. | Existing auth policies and permissions remain mandatory. |
| Downgrade preserves data. | A customer can return to a higher plan later and still see history. | Records are hidden/blocked for new actions, not deleted. |
| Package changes are auditable. | Sales/support can explain why a capability became available or unavailable. | Use package assignment history and business event/audit evidence. |
| Provider add-ons require readiness. | A customer cannot accidentally use an unconfigured payment, shipping, email, or storage target. | Check secure config, credentials, callback readiness, storage profile, and worker readiness before success paths. |
| Tenant entitlement comes before business override. | A tenant-level contract is the commercial source of truth. | `BusinessFeatureOverride` should be an exception inside entitlement, not the top-level package assignment. |
| Package names are product-generic. | Product can be sold consistently across customers. | Avoid customer-specific package codes in canonical docs or seed data. |

## Upgrade Rules

| Step | Expected behavior |
| --- | --- |
| Assignment | Create or update tenant package assignment with effective date and actor evidence. |
| Dependency validation | Validate required base capabilities and unsafe standalone combinations. |
| Provider validation | For provider add-ons, check runtime readiness before showing enabled state. |
| UI/API enablement | Feature gates expose navigation/routes only after assignment and readiness pass. |
| Historical records | Existing records become visible if the capability is now entitled and the user is authorized. |

## Downgrade Rules

| Step | Expected behavior |
| --- | --- |
| Assignment change | Record downgrade with actor, reason, effective date, and previous assignment. |
| Behavior block | Stop new commands and hide navigation for disabled capabilities. |
| Data preservation | Keep existing orders, invoices, loyalty ledger, stock ledger, journal entries, provider callbacks, documents, and audit records. |
| Direct access | Return `FeatureDisabled` for entitled tenant mismatch, while preserving authorization checks. |
| Background work | Workers skip new disabled work and continue only mandatory maintenance for historical/compliance records when legally required. |

## Provider Add-On Rules

| Provider/storage type | Required checks |
| --- | --- |
| Payment provider | Owning billing/checkout entitlement, secure keys, webhook auth, callback worker readiness, no fake payment success. |
| Shipping provider | Owning shipping entitlement, carrier account/product readiness, label storage readiness, provider operation worker readiness. |
| Communication provider | Owning communication entitlement, sender identity readiness, template/sandbox policy, callback processing readiness. |
| Object storage profile | Owning feature entitlement, selected provider readiness, retention/legal hold when required, no database fallback for compliance evidence unless explicitly allowed. |
| Accounting/bank/API target | Real target selected, credential owner defined, payload mapping, error contract, smoke strategy, external references and sync/conflict policy. |

## Audit Evidence

Every package-level change should record:

- Tenant id or current business bridge id.
- Package/plan code.
- Effective start and end timestamps.
- Actor id or system actor.
- Reason summary without secrets.
- Previous and new assignment.
- Dependency/readiness result.

## Source-Of-Truth Rules

- `PlatformPackage` owns bundle composition.
- `PlatformPlan` owns pricing and billing variant.
- `TenantPackageAssignment` owns tenant entitlement.
- `TenantFeatureOverride` owns tenant-level exception.
- `BusinessFeatureOverride` owns business-level exception only after tenant entitlement.
- `FeatureArea` owns technical capability identity.
- Permissions own user authorization.

## Non-Deletion Rule

Package changes must not delete tenant data automatically. Data deletion, anonymization, retention, archive purging, or legal hold changes require a separate data-retention design and explicit operator approval.
