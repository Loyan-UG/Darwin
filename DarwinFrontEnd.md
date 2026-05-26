# Darwin Front-End Guide

Reviewed: 2026-05-26

`Darwin.Web` is the customer-facing Next.js application for public storefront, CMS, discovery, and member-facing web workflows. It must stay separate from WebAdmin concerns.

Historical progress notes belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). WebApi contract rules belong in [DarwinWebApi.md](DarwinWebApi.md).

## Audience

The front-office is for customers, members, and public visitors. It must not expose:

- Internal diagnostics.
- Readiness dashboards.
- Provider troubleshooting.
- Review queues.
- Admin route maps.
- Back-office language.

## Boundaries

- Consume public and member WebApi contracts.
- Do not reuse admin DTOs.
- Keep payment finalization webhook-authoritative.
- Keep public/member copy customer-friendly and localized.
- Use WebAdmin for operator actions, provider remediation, and internal support workflows.

## Current Scope

The front-office is secondary to the current WebAdmin operational lane, but it remains the intended home for:

- Public CMS and landing pages.
- Public business discovery and detail pages.
- Storefront catalog and checkout when enabled.
- Member account, order, invoice, and loyalty surfaces.
- Customer-facing legal/account handoff pages.

## Payment Policy

- Checkout must use provider-hosted payment flows.
- Browser return routes must never mark payment successful.
- Payment state changes come from verified provider webhooks.
- Native app-store in-app purchase flows are not part of the first launch scope.

## Implementation Rules

- Keep public/member routes free from admin terminology.
- Show only active/approved public business data.
- Avoid leaking provider references unless the contract explicitly marks them safe.
- Keep API route usage aligned with `DarwinWebApi.md`.
- Treat structured invoice source-model downloads as operational artifacts, not compliant e-invoices.
- Keep customer-visible errors safe and non-diagnostic.

## Testing Expectations

Use source-contract and UI tests to guard:

- Public/member route boundaries.
- No admin DTO leakage.
- No internal diagnostic wording in public UI.
- Localization parity.
- Payment return-route behavior.
- CMS/media/catalog rendering boundaries.

See [DarwinTesting.md](DarwinTesting.md).
