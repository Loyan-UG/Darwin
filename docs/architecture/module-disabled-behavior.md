# Module Disabled Behavior

Reviewed: 2026-06-19

This document defines target disabled-mode behavior. It documents future guard behavior and does not change runtime behavior in this audit.

## Target Semantics

| Surface | Expected behavior when tenant lacks capability | Notes |
| --- | --- | --- |
| WebAdmin navigation | Hide the module navigation section and links. | Permission checks still apply. Capability gates do not grant authorization. |
| WebAdmin direct URL | Return `403 FeatureDisabled` for an authenticated operator in a real tenant when the feature is disabled. | Use `404 NotFound` only for nonexistent resources, public discovery concealment, or security-sensitive enumeration cases. |
| WebApi public routes | Omit disabled public surfaces or return structured problem response when the route is capability-specific. | Public discovery can avoid exposing disabled modules. |
| WebApi member/business routes | Return structured problem response with code `FeatureDisabled`. | Response should be stable for mobile clients and not expose provider secrets or internal package configuration. |
| Worker jobs | Skip tenant/provider-disabled work safely and record safe operational evidence. | A disabled worker path must not mark provider delivery, settlement, shipment, archive, or export as successful. |
| Mobile Consumer | Receive capability metadata for navigation gating and hide unavailable surfaces. | Direct API calls still require server-side gates. |
| Mobile Business | Receive business capability metadata and hide unavailable business operations. | Business access-state remains separate from capability entitlement. |
| Darwin.Web storefront | Hide disabled storefront/member capabilities and avoid starting checkout/payment flows when dependencies are disabled. | Checkout disablement must be server-enforced. |
| Provider callbacks | Accept only configured provider callback routes and process only enabled provider capabilities. | Callback authentication and provider inbox safety remain mandatory. |
| Object storage/provider-backed features | Report blocked readiness rather than fake success when storage/provider profile is missing. | Applies to archive, e-invoice, finance export, labels, and document artifacts. |

## Error Contract

Future API and WebAdmin guard work should use a stable internal result shape:

| Field | Expected value |
| --- | --- |
| `code` | `FeatureDisabled` |
| `capability` | Stable capability code such as `finance` or `provider-stripe`. |
| `businessId` | Included only when safe for the audience. |
| `message` | Localized or audience-safe text. |
| `correlationId` | Use existing request correlation conventions where available. |

## Capability Metadata For Clients

Client capability metadata should be read-only and audience-scoped.

| Client | Minimum metadata |
| --- | --- |
| WebAdmin | Enabled module sections, allowed route families, provider readiness status without secrets. |
| Web public/storefront | Public storefront capabilities such as catalog, CMS, cart, checkout, shipping options, and provider readiness without secrets. |
| Mobile Consumer | Member portal, loyalty, commerce, invoice documents, payroll self-service, notifications. |
| Mobile Business | Business account, loyalty operations, billing subscription, media, notifications, future warehouse operations. |

## Current Gap Summary

- `FeatureAreaService` can answer whether a feature is enabled, but most surfaces do not call it yet.
- WebAdmin `_Layout.cshtml` is hardcoded by section and only partially permission-conditional.
- WebApi controllers are audience-scoped but not uniformly capability-scoped.
- Worker registrations are global and need tenant/provider skip policies.
- Mobile `ApiRoutes` is static and does not expose a capability metadata contract.
- `BillingPlanFeatures` is not a complete ERP packaging model.

## Non-Goals For This Audit

- No feature middleware.
- No authorization policy changes.
- No mobile DTO additions.
- No WebAdmin filter or navigation refactor.
- No worker registration changes.
- No source-contract tests.
