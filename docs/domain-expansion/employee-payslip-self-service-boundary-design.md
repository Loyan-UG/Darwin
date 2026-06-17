# Employee Payslip Self-Service Boundary Design

## Summary

This step is documentation and design only. It adds no entity, migration, route, DTO, WebAdmin mutation, public/mobile/member contract, storefront behavior, finance export format, provider submission, customer payment/refund flow, supplier finance flow, bank API integration, journal editor shortcut, or payroll mutation.

The goal is to lock the employee-facing payslip boundary before any public, member, or mobile implementation. Employee self-service must be additive, privacy-scoped, download-audited, and based on existing immutable payroll evidence. It must not expose WebAdmin document routes, payroll run internals, employer journal entries, bank reconciliation data, provider payloads, or other employees' data.

## Current Darwin Employee Payslip Findings

- `Employee` exists in `HumanResources` and can optionally link to `BusinessMember` when the person needs system access.
- `BusinessMember` remains the business access source. Employee records do not grant authorization by themselves.
- `PayrollRun`, `PayrollRunLine`, and `PayrollRunLineComponent` snapshot approved payroll evidence and rule versions.
- `PayrollPayslip` exists with official versioned PDF output stored through object storage and linked through `DocumentRecord`, plus retained HTML source artifact evidence.
- Payroll liability posting, payroll payment, payment reversal, bank settlement, and returned-transfer correction exist as internal HR/Finance/Treasury workflows.
- Payroll provider adapter boundary design is complete, but provider submission remains blocked until a real target and credential/error contract exist.
- Current payslip download is internal/WebAdmin-only. No public/mobile/member route exposes payroll documents.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, provider submission, and invoice archive/download flows are unchanged by this design step.

## Decision Matrix

| Self-service surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Privacy impact | Finance/provider impact | UX impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Employee identity gate | `Employee` can link to `BusinessMember`; identity/access are separate. | Employee self-service requires an authenticated user linked to a `BusinessMember` that is linked to the same active employee record. | Identity, `BusinessMember`, `Employee`. | Existing identity/access handlers; future payslip read handler. | Prevents access by email/name match alone. | No finance mutation. | Employee sees only own payroll area. | Ready for implementation design. | Implement dedicated authorization guard. |
| Business access scope | `BusinessMember` owns business access state. | Payslip access requires active business membership for the payroll business and active employee linkage. | `BusinessMember` plus employee link. | Access middleware/read handler. | Prevents cross-business payslip leakage. | No finance impact. | Clear blocked state if access is inactive. | Ready. | Add contract tests in implementation. |
| Payslip list | `PayrollPayslip` and `PayrollRunLine` exist. | Employee list shows only the employee's generated payslips with period, payslip number, generated date, net pay, currency, and download readiness. | Payslip read model. | Read-only public/mobile/member handler. | Minimal payroll summary only. | No posting/provider impact. | Simple employee history list. | Ready. | Build additive endpoint/service later. |
| Payslip PDF download | Official PDF `DocumentRecord` exists. | Download reads the stored official PDF through a dedicated self-service download handler, not WebAdmin download routes. | Payslip artifact service plus document storage. | Future self-service download handler. | Download must be audited and scoped to own employee record. | No finance/provider impact. | Employee gets official PDF. | Ready. | Add route with privacy/download tests. |
| HTML source artifact | Retained internally. | HTML source artifact remains internal and is not exposed to employee self-service. | Payslip artifact service. | None in self-service. | Avoids exposing implementation artifact. | No impact. | Employee sees only official PDF. | Locked. | Keep internal only. |
| Payment status summary | Payroll payment allocations exist. | Employee self-service may show high-level payment status for the employee's own payslip: unpaid, partially paid, paid, reversed, or correction attention. It must not expose bank reconciliation, journal ids, employer bank accounts, provider ids, or other employee allocations. | Payroll payment read model. | Read-only self-service handler. | Shows useful status without treasury internals. | No mutation or posting. | Employee can understand whether salary payment is complete. | Ready after read model design. | Include in core self-service slice if scope remains small. |
| Bank settlement details | Bank settlement and reconciliation exist. | Bank statement lines, reconciliation matches, settlement journals, bank account mappings, and returned-transfer correction internals stay internal. | Treasury models. | HR/Finance WebAdmin only. | Prevents sensitive treasury disclosure. | No finance change. | Employee gets simple status, not bank operations. | Locked. | No self-service exposure. |
| Employer cost and rule details | Payslip PDF currently contains payroll totals and components. | Self-service exposes the generated PDF as-is. Additional raw rule snapshots, employer internal cost analysis, and component source JSON are not exposed as structured API fields. | Payslip PDF artifact. | Read-only download handler. | Prevents exposing internal calculation payloads. | No finance impact. | Employee sees the formal document. | Ready. | Keep API list minimal. |
| Corrections and regenerated payslips | Payment corrections exist; legal payroll adjustment is separate. | Payment/bank corrections do not regenerate payslips. If legal payroll adjustment later creates a corrected payslip, it must be a new explicit payslip artifact linked to the adjustment, not a rewrite. | Future legal payroll adjustment model. | Future correction handler. | Preserves audit history. | Corrections stay posting-owned. | Employee can later see corrected documents separately. | Boundary locked. | Design legal payroll adjustment separately. |
| Download audit | `BusinessEvent`/`AuditTrail` exist. | Self-service downloads should record privacy-safe download evidence with business id, employee id, payslip id, document id, user id, and timestamp. | Download handler plus event/audit. | Future self-service download handler. | Supports HR privacy accountability. | No finance impact. | No visible complexity for employee. | Ready. | Add in implementation. |
| Retention and legal hold | Personnel document patterns exist. | Payslip retention and legal-hold policy remain document/storage policy. Self-service cannot delete, archive, or modify payslips. | Document/storage policy. | HR/WebAdmin document retention owners. | Prevents employee-side deletion of payroll evidence. | No finance impact. | Employee can download, not manage retention. | Ready. | Keep mutation internal. |
| Provider filing relation | Provider boundary design exists. | Provider submission status is not shown to employees in v1. Filing receipts remain internal unless a future legal requirement demands exposure. | Provider submission evidence. | Future provider handlers. | Avoids exposing employer filing operations. | No provider mutation. | Employee area stays focused. | Locked. | Keep provider evidence internal. |
| Public WebApi route shape | No payroll public route exists. | Implementation must add dedicated additive route(s) under the authenticated employee/member surface. It must not reuse WebAdmin routes or broaden existing commerce invoice routes. | Public/member API owner. | Future self-service route handler. | Keeps audience boundary clear. | No finance export impact. | Consistent employee self-service entry point. | Needs implementation slice. | Add source-contract tests. |
| Mobile/member contract | Mobile member contracts are stable. | Mobile/member exposure must be additive and tested with route and DTO compatibility guards. Existing commerce order/invoice/member routes remain unchanged. | Mobile/shared contract owner. | Future self-service route and client services. | Prevents accidental payroll leakage in commerce screens. | No finance impact. | Mobile can later show payslip list/download. | Needs implementation slice. | Add contract tests before release. |
| WebAdmin relation | WebAdmin already downloads payslips. | WebAdmin remains operator surface. Self-service cannot call WebAdmin controller actions or rely on admin authorization. | WebAdmin and self-service controllers separately. | Separate handlers/controllers. | Avoids privilege confusion. | No finance impact. | Clean operator/employee separation. | Locked. | Source guard during implementation. |

## Locked Decisions

- Employee self-service is a dedicated additive surface. It must not reuse WebAdmin payslip download routes.
- Access requires authenticated user, active business membership, and explicit employee linkage. Name, email, phone, or document metadata match is not sufficient.
- Employees can see and download only their own official payslip PDF artifacts.
- The retained HTML source artifact remains internal and is not employee-visible.
- Employee-facing list fields stay minimal: period, payslip number, generated date, net pay, currency, download readiness, and safe high-level payment status when available.
- Bank reconciliation, bank statement details, journal entry ids, employer bank accounts, provider submission ids, provider payloads, payroll rule source JSON, and other employees' allocations remain internal.
- Payslip download must be audited with privacy-safe event/audit evidence.
- Employee self-service cannot archive, delete, regenerate, correct, or mutate payslips, payroll runs, payroll payments, bank settlement, provider filing, journal entries, finance export, customer payments, supplier finance, or invoice archive/download records.
- Corrected legal payroll documents later must be explicit new artifacts; existing payslip artifacts are not rewritten.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, provider submission, and invoice archive/download contracts remain unchanged in this design step.

## Implementation Sequence

1. `Employee Payslip Self-Service Boundary Design`
   - Outcome: this document. The employee-facing access, download, privacy, audit, WebAdmin separation, and mobile/member compatibility boundary is locked.
2. `Employee Payslip Self-Service Core Slice`
   - Outcome: complete for current phase. Dedicated authenticated member read/download handlers and routes, additive contract DTO, Mobile.Shared route/service, official-PDF-only download, download audit, source guards, no-regeneration missing-object failure, and safe high-level payment status are implemented.
3. `AI Governance Foundation Core Model Slice`
   - Continue the broader ERP roadmap by implementing sensitive-field policy, scoped-access policy, recommendations, action drafts, approval-required handoff evidence, and source guards.

## Implementation Outcome

- Employee payslip self-service boundaries are decision-complete.
- Employee payslip self-service core is implemented for the current phase.
- Employee-facing access uses the dedicated `api/v1/member/payroll/payslips` route and does not reuse WebAdmin payslip download routes.
- The list returns minimal employee-visible payslip fields and safe high-level payment status only: unpaid, partially paid, paid, or attention when payroll payment correction evidence exists.
- Download returns only the stored official PDF artifact. Missing storage objects fail safely and do not trigger regeneration from HTML, payroll run, or live employee/time data.
- Every successful employee download records privacy-safe event/audit evidence through the existing business event and audit foundation.
- Provider filing remains blocked until a real provider target and credential/error contract exist.
- Mobile/member contracts are changed only additively through the dedicated self-service route and Mobile.Shared service. Storefront, customer invoice archive/download, finance export package format, customer/supplier payment flows, provider filing, and WebAdmin operational routes remain unchanged.

## Compatibility Guards

- Do not expose WebAdmin payslip routes, payroll run internals, payroll rule source JSON, journal entry ids, bank reconciliation details, bank statement lines, employer bank accounts, provider payloads, provider credentials, or other employees' payroll data.
- Do not mutate payroll runs, payslips, payroll payments, bank settlement, returned-transfer correction, provider filing, journal entries, finance exports, customer payment/refund, supplier finance, or invoice archive/download records from employee self-service.
- Do not add self-service routes without route-contract, DTO, authorization, privacy, and download tests.
- Do not store credentials, tokens, private keys, raw provider payloads, raw bank payloads, private document contents, medical details, raw HR import files, or connection strings in metadata, events, audit trails, external references, tests, logs, or documentation.

## Documentation Verification

- `docs/README.md` links this document from the ERP expansion map.
- `BACKLOG.md` and `erp-expansion-master-status.md` record the self-service core outcome and move the next gate to `AI Governance Foundation Core Model Slice`.
- This document contains no deferred ambiguous decisions for the self-service boundary.
- Restricted vendor/source scans must return no output.
