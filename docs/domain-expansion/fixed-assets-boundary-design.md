# Fixed Assets Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks Darwin's future Fixed Assets boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, depreciation job, finance posting, purchase mutation, or export format change.

Decision: Fixed assets are formal finance records with acquisition, capitalization, depreciation, impairment, disposal, and evidence. They are not normal inventory stock, supplier invoice notes, or manual journal shortcuts.

## Current Darwin Fixed Asset Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| Supplier invoice/payables | Purchase evidence and AP posting exist. | Asset acquisition can link to supplier invoice lines, but fixed asset ownership is finance-owned. |
| `FinancialAccount`, `JournalEntry`, account mapping | Posting foundation exists. | Depreciation/disposal must use finance posting roles after role design. |
| Inventory and warehouse | Stockable product/locations exist. | Fixed assets are not inventory stock unless separately tracked as equipment evidence. |
| Service management future | Maintenance service history can link later. | Asset maintenance is separate from depreciation ownership. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Asset register | Tracks capitalized company assets. | Requires asset number, category, acquisition source, location/custodian, value. |
| Capitalization | Moves qualifying purchase into asset register. | Requires acquisition policy and supplier invoice link. |
| Depreciation | Calculates period expense and accumulated depreciation. | Requires depreciation book, method, schedule, posting policy. |
| Disposal/retirement | Records sale, scrap, loss, or transfer. | Requires disposal lifecycle and finance posting/reversal design. |
| Asset documents | Stores warranty, contract, insurance, photos. | Uses `DocumentRecord`, not metadata blobs. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `FixedAsset` | fixed-assets | business, asset number, category, status, description, acquisition date, currency, cost, location/custodian, supplier invoice line link. |
| `FixedAssetCategory` | fixed-assets | code, name, default useful life, depreciation method, account mappings. |
| `FixedAssetDepreciationBook` | fixed-assets | asset, book type, method, useful life, salvage value, start date, status. |
| `FixedAssetDepreciationScheduleLine` | fixed-assets | period, planned depreciation, posted depreciation, journal link. |
| `FixedAssetTransaction` | fixed-assets | acquisition, depreciation, impairment, disposal, reversal evidence. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Fixed asset | `Draft -> Capitalized -> Active -> Suspended -> Disposed -> Archived`. |
| Depreciation line | `Planned -> Posted -> Reversed`; posted lines cannot be edited. |
| Disposal | Requires asset active state and finance posting; reversal needs separate policy. |

## Application Surface

Future handlers:

- Create/update draft asset.
- Capitalize asset from supplier invoice line or manual acquisition evidence.
- Generate depreciation schedule.
- Post depreciation through finance posting service.
- Record impairment or disposal after posting design.
- Link documents and external asset references.

## WebAdmin Surface

Finance WebAdmin should include asset register, categories, asset detail, acquisition/capitalization, depreciation schedule, depreciation posting review, disposal review, documents, and read-only links to supplier invoices, journal entries, service assets, and documents.

No public/member/mobile surface is added by default.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `fixed-assets`. |
| Package role | Add-on to Finance/Accounting. |
| Required dependencies | `finance`; optional `procurement`, `service-management`. |
| Disabled behavior | Hide fixed asset pages and block new asset mutations; historical records remain read-only where required. |
| Permissions | Manage asset register, capitalize asset, post depreciation, dispose asset. |
| SoD | Capitalization, depreciation posting, and disposal are approval candidates. |

## Compatibility Boundaries

- Fixed assets do not change inventory stock.
- Supplier invoice remains AP evidence; fixed asset posting is finance-owned.
- Finance export continues to read posted journal entries; package format unchanged.
- No public/mobile/storefront changes.

## Implementation Slices

1. `Fixed Asset Register Core Slice`.
2. `Asset Capitalization Boundary/Slice`.
3. `Depreciation Schedule Core Slice`.
4. `Depreciation Posting Slice`.
5. `Asset Disposal Boundary Design`.

## Test Plan

Future tests must cover asset lifecycle, capitalization source validation, schedule calculation determinism, posting idempotency, no inventory mutation, WebAdmin guards, export compatibility, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement Fixed Assets.
