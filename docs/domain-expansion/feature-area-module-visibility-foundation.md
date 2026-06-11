# FeatureArea And Module Visibility Foundation

## Purpose

This slice adds shared foundation records for logical module visibility and feature gating. It supports future ERP packaging, UI visibility, and customer-specific enablement without splitting projects, databases, or deployment units.

The implementation is additive. It does not change public/mobile routes, DTOs, auth contracts, loyalty contracts, order/invoice snapshots, WebAdmin navigation, or business mobile launch guards.

## Implemented Primitives

| Primitive | Decision | Storage | Notes |
| --- | --- | --- | --- |
| `FeatureArea` | Add now. | Structured table in `Foundation` schema. | Defines a feature/module area with code, category, default enablement, visibility scope, optional permission key, and metadata. |
| `BusinessFeatureOverride` | Add now. | Structured table in `Foundation` schema. | Enables or disables a feature for a business with optional effective dates. |
| `FeatureAreaService` | Add now. | Internal application service. | Supports create, override, enabled lookup, and ordered listing. |
| UI/API/mobile gating | Defer. | No behavior change. | Existing screens and routes are not wired to this service in this slice. |
| Seed catalog | Defer. | No required seed. | Feature areas can be created by tests or later admin/internal setup. |

## Non-Replacement Rules

| Existing surface | Decision |
| --- | --- |
| Permissions and roles | Not replaced. Authorization remains owned by permission and role infrastructure. |
| Auth policies | Not replaced. Feature areas do not grant access to protected endpoints. |
| Business access state | Not replaced. Business onboarding, activation, blocked-operation, and live-operation gates remain authoritative. |
| Mobile launch guards | Not replaced. Existing mobile readiness and route/contract guards remain unchanged. |
| Site settings feature flags | Not replaced in this slice. Existing operational provider flags remain where they are. |
| Navigation | Not changed. WebAdmin, mobile, and storefront navigation can consume feature areas later through explicit work. |

## Storage And Behavior Rules

| Area | Rule |
| --- | --- |
| Code | `FeatureArea.Code` is normalized to lower-kebab-case and unique while active. |
| Default behavior | In absence of a business override, `DefaultEnabled` is used. Most current product areas should remain enabled by default. |
| Missing/inactive feature | Treated as disabled by `FeatureAreaService`. |
| Business override | A valid active override wins over the default when its effective window includes current UTC time. |
| Visibility scope | `FeatureAreaVisibilityScope` indicates intended surface visibility; it is not an authorization grant. |
| Sensitive data | Secrets, credentials, auth tokens, refresh tokens, private keys, and raw sensitive provider payloads must not be stored in reason or metadata JSON. |

## Evidence And Tests

| Evidence | Coverage |
| --- | --- |
| Unit tests | Code normalization, duplicate rejection, default enablement, business overrides, effective windows, ordering, and sensitive-data rejection. |
| Infrastructure tests | `Foundation` schema placement, max lengths, enum string conversion, lookup indexes, unique filtered indexes, and PostgreSQL `jsonb` mapping. |
| Compatibility tests | Existing contract and mobile route/service lanes prove no public/mobile contract changes. |
| Documentation scan | Restricted-term scan must stay clean. |

## Next Slice

After this slice, create a clean checkpoint or start `CRM Expansion Design` using the completed foundation primitives. Wire UI/API/mobile feature visibility only through explicit later work.
