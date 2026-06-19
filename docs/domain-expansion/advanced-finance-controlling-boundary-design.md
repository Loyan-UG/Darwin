# Advanced Finance And Controlling Boundary Design

Reviewed: 2026-06-19

## Summary

This document locks the future advanced finance and controlling boundary. It is documentation-only and adds no entity, migration, route, DTO, WebAdmin action, journal entry, finance export change, or reporting runtime behavior.

Decision: Advanced finance must extend the existing `JournalEntry` and account mapping foundations with formal dimensions, budgets, allocations, and controlling reports. It must not rewrite posted journal history or replace finance export packages.

## Current Darwin Finance Findings

| Current model | Finding | Decision |
| --- | --- | --- |
| `FinancialAccount`, `JournalEntry`, `JournalEntryLine` | Authoritative accounting facts exist. | Keep as posting foundation; dimensions attach through explicit design. |
| `FinancePostingService` and account mappings | Controlled posting exists for sales, payables, payroll, treasury, advances. | Advanced finance uses posting service; no manual shortcuts. |
| Finance reporting/export | Reports and export packages read posted journal entries. | Add dimensions without changing export format until adapter mapping design. |
| Supplier/payroll/bank foundations | Payables, payroll, treasury and corrections are journal-backed. | Controlling reads facts and adds dimensions/allocations. |

## Business Capabilities

| Capability | Business impact | Technical implication |
| --- | --- | --- |
| Cost centers/profit centers | Management can analyze costs and margins by organization/unit. | Requires dimension master and journal line dimension assignment. |
| Budgets | Operators can compare actuals with plan. | Requires budget version, period, dimension, account, currency, amount. |
| Allocations | Shared costs can be distributed. | Requires allocation rule, driver, generated journal or reporting-only allocation policy. |
| Period close support | Finance can lock/report periods. | Requires period status and posting lock design before enforcement. |
| Consolidation | Multi-business/tenant group reporting. | Requires tenant/data-scope design and currency policy. |

## Future Entity Ownership

| Entity | Owner | Key fields |
| --- | --- | --- |
| `FinanceDimension` | finance-controlling | business/tenant scope, dimension type, code, display name, status. |
| `FinanceDimensionValue` | finance-controlling | dimension, code, parent, status, effective dates. |
| `JournalEntryLineDimension` | finance-controlling | journal line, dimension value, allocation percent/amount when applicable. |
| `BudgetVersion` | finance-controlling | business, name, status, fiscal year/period range, currency. |
| `BudgetLine` | finance-controlling | version, account, dimension values, period, debit/credit or amount. |
| `AllocationRule` | finance-controlling | source/destination dimensions, driver type, status, effective dates. |
| `AllocationRun` | finance-controlling | rule, period, status, source totals, generated posting link if posting-owned. |

## Lifecycle Decisions

| Object | Lifecycle |
| --- | --- |
| Dimension value | `Draft -> Active -> Inactive -> Archived`; posted history keeps original value id/name snapshot. |
| Budget version | `Draft -> Approved -> Active -> Locked -> Archived`. |
| Allocation rule | `Draft -> Active -> Suspended -> Archived`. |
| Allocation run | `Draft -> Reviewed -> Posted` or `Cancelled`; posting requires finance posting policy. |

## Application Surface

Future handlers:

- Manage dimensions and values.
- Assign dimensions during controlled posting command creation.
- Manage budget versions and lines.
- Run budget vs actual projections.
- Configure allocation rules and review allocation runs.
- Post allocations only after balanced posting design.

## WebAdmin Surface

Finance WebAdmin should include dimension setup, budget versions, budget import/review, budget-vs-actual reports, allocation rules/runs, period review, and read-only links to journal entries.

No public/member/mobile exposure is added by default.

## Package, Security, And Disabled Mode

| Area | Decision |
| --- | --- |
| Capability code | Future `finance-controlling`. |
| Package role | Add-on to Finance/Accounting or Enterprise Full Suite. |
| Required dependencies | `finance`; optional `project-operations`, `manufacturing-mrp`, `hr-time`. |
| Disabled behavior | Hide controlling setup/reports; base finance posting/export remains available. |
| Permissions | Manage dimensions, manage budgets, approve budgets, run allocations, post allocations. |
| SoD | Budget approval and allocation posting are approval candidates. |

## Compatibility Boundaries

- Do not alter historical journal entry debit/credit lines after posting.
- Do not change finance export package format until adapter mapping is designed.
- Do not use dimensions to bypass module ownership or authorization.
- No public/mobile/storefront changes.

## Implementation Slices

1. `Finance Dimensions Core Slice`.
2. `Posting Dimension Assignment Slice`.
3. `Budget Version Core Slice`.
4. `Budget Actual Reporting Slice`.
5. `Allocation Rule Boundary Design And Core Slice`.
6. `Period Close Boundary Design`.

## Test Plan

Future tests must cover dimension uniqueness, active/inactive behavior, journal line dimension assignment, budget approval, allocation balancing, no posted history rewrite, export compatibility, WebAdmin guards, and disabled-mode behavior.

## No Runtime Behavior Changes

This design does not implement Advanced Finance/Controlling.
