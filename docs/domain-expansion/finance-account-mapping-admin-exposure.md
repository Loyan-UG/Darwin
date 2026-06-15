# Finance Account Mapping Admin Exposure

## Summary

This slice exposes controlled WebAdmin configuration for business-scoped finance posting account mappings. It does not add schema, migrations, public API routes, mobile contracts, new finance documents, payment/refund mutations, credit-note mutations, or journal-entry mutation duplication.

The purpose is to make automated finance posting operationally usable: invoice, payment, refund, cancellation, and credit-note posting require reliable role-to-account mappings per business.

## Current Darwin Finance Mapping Findings

- `FinancePostingAccountMapping` already stores business-scoped role mappings.
- `FinanceAccountMappingService` already validates business ownership, compatible account type, active/inactive state, and sensitive metadata.
- `FinancialAccount` creation and editing already belongs to Billing.
- `ReceivablesProjectionService` fails closed when the required receivables mapping is missing.
- Finance reporting now exposes read-only receivables and postings, so mapping readiness needs an operator surface in the same workspace.

## Locked Decisions

| Surface | Decision | Owner | Mutation impact | Mobile/member impact | Notes |
| --- | --- | --- | --- | --- | --- |
| Mapping admin | Expose mapping configuration in Finance WebAdmin. | Finance WebAdmin | Mapping upsert only | None | This is configuration, not posting or document mutation. |
| Financial accounts | Continue creating/editing accounts in Billing. | Billing | Existing Billing mutations only | None | Finance links to Billing accounts instead of duplicating account forms. |
| Role validation | Reuse `FinanceAccountMappingService`. | Application Billing service | Existing validation only | None | UI limits selectable accounts to compatible account types. |
| Required roles | Highlight sales posting roles: receivables, sales revenue, tax payable, and cash clearing. | Finance reporting | None | None | Missing required mappings are readiness gaps. |
| Optional roles | Support refund clearing and rounding mappings without forcing them before they are needed. | Finance reporting | Mapping upsert only | None | This keeps the full role registry visible without fake defaults. |
| Secrets and payloads | Do not store secrets, provider tokens, raw provider payloads, or credentials in mapping description/metadata. | Application service | Existing validation only | None | Metadata remains `{}` from WebAdmin v1. |

## Implementation Outcome

- Added Finance account mapping page in WebAdmin.
- Added Application query and command handlers over the existing mapping service.
- Added source, render, and unit tests for mapping readiness, compatibility, registration, and mutation boundary.
- No schema, migration, public route, WebApi DTO, mobile DTO, storefront checkout, invoice archive/download, payment/refund flow, credit-note lifecycle, or journal-entry editor behavior changed.

## Next Step

`Finance Export/Accounting Integration Design` is complete in [finance-export-accounting-integration-design.md](finance-export-accounting-integration-design.md).

The next suitable implementation gate is `Finance Export Batch Foundation Slice`: add structured export batch identity and safe attempt tracking before adding package downloads or connector pushes.
