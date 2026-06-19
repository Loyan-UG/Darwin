# Module Design Template

Reviewed: 2026-06-19

## Summary

Use this template for every Darwin module or capability family before implementation. It is documentation-only guidance and does not add runtime behavior, entities, migrations, routes, DTOs, WebAdmin actions, mobile contracts, worker registrations, provider flows, or package gates by itself.

Every module design must be decision-complete enough that an implementation slice can proceed without inventing domain boundaries during coding.

## Required Sections

| Section | Required content |
| --- | --- |
| Summary | Business purpose, operator/customer audience, module owner, and explicit in/out-of-scope boundaries. |
| Current Darwin findings | Code-backed references to current entities, handlers, controllers, views, workers, tests, and existing docs. |
| Business capabilities | User-facing capabilities, operator workflows, expected business outcome, and package/plan positioning. |
| Entity ownership | New or existing entities, owning capability, read dependencies, write dependencies, and prohibited parallel models. |
| Field catalog | Fields classified as real column, JSON/custom field, document evidence, external reference, derived projection, or configuration. |
| Lifecycle | Status values, allowed transitions, actor/timestamp fields, row-version expectations, idempotency rules, and cancellation/archive behavior. |
| Application surface | Commands, queries, handlers, validators, policies, services, DTO ownership, and safe failure modes. |
| WebAdmin surface | Navigation, list/filter/pagination, detail, create/edit, lifecycle actions, review queues, read-only cross-links, anti-forgery, and row-version use. |
| WebApi and mobile surface | Public, member, business, admin, mobile-consumer, and mobile-business exposure. Default is no exposure unless explicitly designed. |
| Worker and provider dependencies | Background jobs, provider adapters, storage profiles, callback handling, retry policy, and readiness checks. |
| Package and disabled mode | Capability codes, required packages, optional add-ons, provider add-ons, disabled navigation, direct URL behavior, worker skip behavior, and structured API failures. |
| Security and permissions | Authentication, authorization, permission keys, separation-of-duties needs, sensitive metadata rules, and audit/event evidence. |
| Compatibility boundaries | Invoice archive, payment/refund, finance export, storefront, mobile contracts, provider callbacks, generated artifacts, and existing document snapshots. |
| Tests and source guards | Unit, infrastructure, WebAdmin render/source, WebApi contracts, mobile compatibility, worker/source guards, docs scan, and `git diff --check`. |
| Implementation slices | Ordered slices that keep schema, UI, contracts, and workers reviewable. |

## Field Classification Rules

| Field type | Use when | Examples |
| --- | --- | --- |
| Real column | Data is common, filterable, reportable, compliance-relevant, accounting-relevant, inventory-relevant, or integration-key material. | Status, document number, business id, customer id, supplier id, posted date, amount, currency. |
| JSON/custom field | Data is customer-specific, uncertain, low-frequency, provider-specific, industry-specific, or unstructured. | Optional industry attributes, low-frequency metadata, safe display hints. |
| Document evidence | A file, upload, artifact, proof, photo, signed form, or generated package is evidence rather than structured workflow state. | Remittance document, supplier document, inspection photo, export package metadata. |
| External reference | Identity owned by another system, provider, bank, accounting tool, or migration source. | Remote document id, accounting export receipt, provider shipment id. |
| Derived projection | Data can be recalculated from authoritative records and should not be manually edited. | Open payable, stock availability, finance report totals. |
| Secure configuration | Secret-bearing or deployment-specific values that must stay outside domain metadata. | API keys, connection strings, private keys, object-storage credentials. |

## Surface Defaults

| Surface | Default rule |
| --- | --- |
| WebAdmin | Internal/operator-first for ERP modules unless public/member exposure is explicitly designed. |
| Public WebApi | No exposure by default. |
| Member WebApi | No exposure by default; add only when customer/member self-service is required and contract tests exist. |
| Business WebApi | No exposure by default; add only when business/mobile users need the workflow. |
| Mobile consumer | No exposure by default. |
| Mobile business | No exposure by default; warehouse/PWA tasks require dedicated offline and scanner design. |
| Worker | No job unless retry, provider callback, scheduled evidence, or import/export behavior is explicitly owned. |
| Provider adapter | No adapter without selected target, credential owner, payload mapping, error contract, and smoke strategy. |

## Required Questions Before Implementation

Every module design must answer:

- What business problem does this module solve, and for which customer type?
- Which existing Darwin entity owns the authoritative record, if any?
- What must not be modeled as a status-only edit?
- Which fields must be reportable real columns?
- Which data must be evidence rather than mutable workflow state?
- Which surfaces remain internal-only?
- Which other modules can read this module, and which module owns writes?
- What happens when the package or provider add-on is disabled?
- What compatibility lanes must remain unchanged?
- What is the smallest safe first implementation slice?

## No Runtime Behavior Changes

Using this template does not change the system. A module design that follows this template still requires a separate implementation plan, code review, migrations when needed, tests, and compatibility verification.
