# Payroll Provider Adapter Design

## Summary

This step is documentation and design only. It adds no entity, migration, route, DTO, WebAdmin mutation, public WebApi contract, mobile/member contract, storefront behavior, finance export format, supplier finance flow, customer payment/refund flow, invoice archive/download behavior, bank API integration, journal editor shortcut, statutory filing implementation, or production payroll-provider submission.

The goal is to lock the provider-neutral statutory filing boundary before any adapter is implemented. Payroll provider submission must use Darwin's approved payroll run, immutable calculation snapshots, formal payslip artifacts, payroll posting evidence, salary payment evidence, `ExternalSystem`, `ExternalReference`, `DocumentRecord`, and `BusinessEvent/AuditTrail` foundations. It must not rebuild payroll from live HR data, generate fake success, store credentials in metadata, or bypass payroll/finance owners.

## Current Darwin Payroll Provider Findings

- `Employee`, `EmploymentContract`, approved HR/time facts, confirmed absence records, payroll periods, payroll rule sets, payroll runs, payroll run lines, payroll run components, payslip PDF artifacts, payroll liability posting, payroll payment, bank settlement, and returned-transfer correction exist.
- `PayrollRun` snapshots approved source facts and rule versions. Provider submission must read those immutable snapshots and calculated components, not live employee, time, absence, contract, or rule records.
- `PayrollPayslip` now stores official versioned PDF artifacts through object storage and `DocumentRecord`, with retained HTML source artifacts. Provider submission does not rewrite payslips.
- `JournalEntry` remains the authoritative accounting evidence for payroll liability, payment, bank settlement, and correction. Provider filing does not replace finance posting.
- `ExternalSystem` can represent a selected payroll filing target. `ExternalReference` can store safe target-side ids, provider receipt ids, submission ids, and filing-period ids.
- `DocumentRecord` can store generated filing packages, validation reports, and provider receipt documents. It is evidence, not proof of successful submission by itself.
- `BusinessEvent` and `AuditTrail` can record filing lifecycle events with safe ids, statuses, dates, period, jurisdiction, and result summaries.
- No real payroll provider target, credential owner, payload mapping, error contract, or external smoke strategy has been selected. Therefore adapter implementation remains blocked.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, and invoice archive/download flows are unchanged by this design step.

## Decision Matrix

| Provider surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Compliance impact | Finance impact | Security boundary | Employee/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Filing trigger | Approved payroll runs, payslip PDFs, payroll posting, and payroll payment evidence exist. | Provider filing can only be designed from approved and calculated payroll run snapshots, never live HR data. Submission implementation requires a selected target. | `PayrollRun` and provider adapter service. | Future payroll provider submission handler. | Prevents filing unapproved or mutable payroll data. | Filing does not create liability or payment. | No provider credentials in payroll metadata. | No employee exposure in v1. | Design complete; implementation blocked until target selection. | Select real target before adapter implementation. |
| Filing target identity | `ExternalSystem` exists. | A payroll provider target must be an active external system configured as a payroll/statutory filing target before push is enabled. | Integration foundation. | Provider adapter registration and future submission handler. | Target-specific requirements stay outside core payroll models. | No accounting mutation from target selection. | Secrets remain in secure configuration only. | No mobile impact. | Ready after target kind/config policy is selected. | Define target contract in target adapter design. |
| Source data package | Payroll run lines/components and source snapshots exist. | Build filing payloads from immutable payroll run snapshots, rule versions, and safe payroll line data. | Payroll run. | Package builder in future provider slice. | Supports reproducible filing evidence. | Reads payroll posting references only as evidence. | Exclude medical details, raw personnel documents, bank credentials, and private document payloads. | No employee-facing package. | Ready conceptually. | Define concrete payload mapping after target selection. |
| Payslip relation | Official PDF payslips exist. | Provider filing does not regenerate or mutate payslips. If a target needs payslip attachments, it reads stored `DocumentRecord` artifacts. | Payslip artifact service. | Future provider package builder only reads stored documents. | Payslip artifact history stays immutable. | No finance impact. | No document contents in metadata/events. | Self-service remains separate. | Ready. | Use document references only when target requires attachments. |
| Submission package artifact | Object storage and `DocumentRecord` exist. | Generated filing package, validation report, and provider receipt evidence use object storage plus `DocumentRecord`. | Provider package service. | Future provider submission handler. | Keeps auditable filed artifact evidence. | No export format change. | No raw secret or credential payload in object keys, filenames, or metadata. | No mobile impact. | Ready pattern. | Add only with implementation slice. |
| Target-side ids | `ExternalReference` exists. | Provider submission id, remote receipt id, correction id, and filing-period id are stored through `ExternalReference`, not provider-specific payroll columns. | External reference foundation. | Future provider submission handler. | Supports reconciliation without schema churn. | No posting state decision from external ids alone. | Store safe ids/display ids only. | No mobile impact. | Ready. | Use in target adapter implementation. |
| Retry and idempotency | Business events and external references exist. | Submission retry must be idempotent by payroll run, target, period, and submission key. Failed attempts must not produce fake success. | Future provider submission service. | Provider submission handler. | Prevents duplicate statutory filing. | No duplicate finance posting. | Safe error summaries only. | No mobile impact. | Requires implementation slice. | Define submission attempt model only with target needs. |
| Validation failure | Payroll validators and safe metadata patterns exist. | Validation failure records safe operator-readable failure evidence and blocks submission success. | Provider package validator. | Future provider submission handler. | Compliance errors must be visible before release. | No finance mutation. | No raw provider payload in logs/events. | No mobile impact. | Requires target validation contract. | Define error contract per target. |
| Provider network failure | No provider adapter exists. | Network/provider/storage failure must fail the submission attempt and keep payroll history unchanged. | Provider adapter. | Future provider submission handler. | Avoids false filing proof. | No accounting mutation. | Safe error code/summary only. | No mobile impact. | Requires adapter implementation. | Include in target smoke strategy. |
| Credential owner | Secure configuration exists outside domain metadata. | Credentials, access tokens, refresh tokens, private keys, certificates, and connection strings are owned by secure deployment configuration, not payroll records, documents, external references, events, tests, or docs. | Deployment configuration. | Future provider adapter factory. | Reduces compliance and breach risk. | No finance impact. | Credentials never appear in WebAdmin forms in v1. | No mobile impact. | Requires deployment-specific target design. | Select credential owner before implementation. |
| WebAdmin visibility | HR workspace exists. | WebAdmin can later show submission readiness, validation result, attempt status, provider receipt reference, and document links. It must not expose credentials or raw provider payload. | HR WebAdmin read models. | Future provider controller actions. | Operators see filing status without secret exposure. | Read-only finance links only. | Credential/config UI is blocked in this design. | No mobile impact. | Design complete. | Add UI only after provider service is testable. |
| Statutory correction filing | Payroll payment correction exists; legal payroll adjustment is separate. | Corrected payroll filing requires a dedicated adjustment/correction design. Returned bank movement correction does not automatically create statutory filing correction. | Future payroll legal adjustment model. | Future correction filing handler. | Prevents sending financial bank corrections as legal payroll corrections. | Finance correction remains journal-owned. | Same credential boundaries. | No mobile impact. | Not implementation-ready. | Design legal payroll adjustment before correction filing. |
| Provider receipt documents | `DocumentRecord` exists. | Provider receipts and validation reports are evidence documents; they do not mark filing successful unless the adapter returns a verified success state. | Provider submission handler. | Provider submission handler. | Receipts support audit and operations. | No accounting mutation. | No private payload in metadata. | No mobile impact. | Ready after target contract. | Store receipt artifacts in implementation. |
| Public/mobile/storefront boundary | Payroll is internal. | Provider filing remains internal/WebAdmin-only. No public WebApi, mobile/member, storefront, or employee self-service route is added by provider filing. | Existing public/mobile owners. | None. | Prevents accidental payroll exposure. | No invoice/archive impact. | No external secrets in client apps. | Unchanged. | Locked. | Keep compatibility smoke lanes. |

## Locked Decisions

- Provider submission is future adapter work over Darwin's canonical payroll evidence. It must not replace payroll runs, payslip artifacts, payroll posting, payroll payment, bank settlement, or correction models.
- Filing payloads must be generated from immutable payroll run snapshots, payroll run line components, rule version metadata, and safe evidence links. They must not live-recompute from mutable HR/time/absence/contract/rule records.
- A selected real target, credential owner, payload mapping, retry policy, safe error contract, and smoke strategy are required before implementation.
- No no-network, placeholder, or test adapter may mark production statutory filing as submitted.
- Target-side filing ids, provider receipts, remote correction ids, and filing-period ids use `ExternalReference`. Provider-specific columns are not added to payroll entities in this design.
- Generated filing packages, validation reports, and receipts use object storage plus `DocumentRecord`. Document existence alone is not submission success.
- Credentials, access tokens, refresh tokens, private keys, certificates, connection strings, raw provider payloads, private document contents, medical details, and raw HR import files must not be stored in domain metadata, document metadata, external references, events, audit trails, logs, tests, or documentation.
- Provider filing must not mutate customer payments, customer refunds, supplier payments, supplier advances, finance export packages, invoice archive/download records, payslips, or posted journal history.
- Employee payslip self-service is a separate additive contract. Provider filing does not expose payslip downloads to employees.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, and invoice archive/download contracts remain unchanged.

## Implementation Sequence

1. `Payroll Provider Adapter Design Slice`
   - Outcome: this document. The provider-neutral statutory filing boundary is locked before schema, route, WebAdmin action, or adapter changes.
2. `Payroll Provider Target Adapter Design Slice`
   - Start only after a real target is selected with credential owner, payload mapping, retry policy, safe error contract, and smoke strategy.
3. `Payroll Provider Package And Submission Foundation Slice`
   - Implement only after target design confirms whether a generic submission attempt model is required. If target needs can be handled with existing primitives and `ExternalReference`, avoid unnecessary schema.
4. `Employee Payslip Self-Service Boundary Design`
   - Outcome: complete. Employee-visible payslip download, privacy, retention, WebAdmin separation, and public/mobile/member contract boundaries are locked.

## Implementation Outcome

- Payroll provider adapter boundaries are decision-complete for the current phase.
- Implementation remains blocked until a real provider target and credential/error contract are selected.
- Formal payslip PDF artifacts remain internal/WebAdmin-only.
- Provider submission cannot fake success, rebuild payroll from live data, store secrets in metadata, or bypass payroll/finance mutation owners.
- Public WebApi, mobile/member, storefront, finance export, customer payment/refund, supplier finance, and invoice archive/download behavior remain unchanged.
- Employee payslip self-service core is complete for the current phase. The next roadmap gate is `AI-Readiness And Automation Governance` unless a real payroll provider target is selected first.

## Compatibility Guards

- Do not implement provider submission, provider package generation, provider push UI, provider credential UI, public route, mobile route, employee self-service route, or statutory filing storage in this design step.
- Do not store credentials, tokens, private keys, certificates, raw provider payloads, private document contents, medical details, raw HR import files, or connection strings in metadata, documents, external references, events, tests, logs, or documentation.
- Do not use provider filing to create or change payroll liability, salary payment, bank settlement, returned-transfer correction, finance export package content, customer payment/refund, supplier finance, or invoice archive/download state.
- Do not rewrite payroll runs, payroll run lines, payroll components, payslip artifacts, journal entries, payment records, bank reconciliation, or correction history after submission.

## Documentation Verification

- `docs/README.md` links this document from the ERP expansion map.
- `BACKLOG.md` and `erp-expansion-master-status.md` record employee payslip self-service core as complete and move the next gate to `AI-Readiness And Automation Governance` unless a real payroll provider target is selected first.
- This document contains no deferred ambiguous decisions for the provider-neutral boundary.
- Restricted vendor/source scans must return no output.
