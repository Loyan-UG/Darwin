# Payroll Legal Calculation Boundary Design

## Summary

This step is documentation and design only. It adds no entity, migration, route, DTO, WebAdmin mutation, public WebApi contract, mobile/member contract, storefront behavior, finance export format, supplier finance flow, customer payment/refund flow, invoice archive/download behavior, payslip generation, statutory filing, payroll-provider submission, or production payroll calculation.

The goal is to lock the professional legal payroll boundary before implementation. Darwin will start legal payroll as Germany-first and country/version-aware, with internal calculation ownership and future provider submission through adapters after a real target, credential owner, payload mapping, and error contract are selected.

## Current Darwin HR And Payroll Findings

- `Employee`, `Department`, `Position`, and `EmploymentContract` exist in the `HumanResources` schema as formal HR/personnel records. They do not grant authorization and do not create payroll obligations by themselves.
- `WorkSchedule`, `AttendanceEvent`, `TimeEntry`, `Timesheet`, `LeaveRequest`, and `AbsenceRecord` provide approved HR/time and absence evidence.
- `PayrollPeriod` and `PayrollPeriodLine` provide export-ready employee-level summaries from approved timesheets and confirmed absences fully inside the selected period. They are not legal payroll runs.
- Personnel documents are stored through object-storage-backed `DocumentRecord` workflows with HR privacy classification, retention metadata, legal-hold metadata, WebAdmin upload/download/archive, and audit evidence.
- `FinancialAccount`, `FinancePostingService`, account mappings, journal entries, treasury foundations, and finance export exist, but payroll liability, employer cost, salary payment, and payroll filing are not canonical.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, and invoice archive/download flows are unchanged by this design step.
- Legal payroll calculation, payslip generation, statutory filing, tax/social-insurance rules, and external payroll-provider integration must not be simulated with status fields, generic documents, manual journals, provider payload JSON, or payroll-period summaries.

## Decision Matrix

| Payroll surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Legal/compliance impact | Finance/accounting impact | Employee/mobile impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Payroll jurisdiction | No legal payroll jurisdiction model. | Start Germany-first while keeping rule sets country and legal-version aware. | Future payroll rule foundation. | HR/Payroll Application handlers. | Legal rules require effective dates and audit evidence. | Later posting must know jurisdiction. | No employee exposure in v1. | Ready for rule design. | `Payroll Calculation Rule Foundation Slice`. |
| Payroll calculation trigger | `PayrollPeriod` summary exists. | Payroll run will be created from an approved payroll period, not from live time records directly. | Payroll run. | HR/Payroll WebAdmin. | Run input must be immutable after calculation. | Posting reads payroll run results later. | No mobile impact. | Needs payroll run model. | Payroll run core after rule foundation. |
| Employee payroll subject | `Employee` exists. | Employee is the payroll subject; `BusinessMember` is access only. | HR. | HR WebAdmin for employee state; payroll handlers for payroll run inclusion. | Payroll inclusion must be auditable. | Employee identity appears in payroll liability detail. | Self-service is separate. | Ready. | Use current employee records. |
| Employment contract and rate input | `EmploymentContract` stores employment metadata. | Payroll-relevant contract terms require explicit payroll fields and effective dating before calculation. | HR contract plus payroll rule foundation. | HR WebAdmin; payroll handlers consume snapshots. | Contract changes can affect legal pay. | Inputs affect gross pay and employer cost. | No direct employee mutation. | Needs rule/input hardening. | Add in rule foundation/core payroll slices. |
| Approved time input | Timesheets and absences exist. | Payroll reads approved summaries and stores immutable input snapshots. | HR/time plus payroll run. | Timesheet approval remains HR/time; payroll only consumes approved facts. | Prevents retroactive live recompute. | Gross pay calculation uses snapshots. | No mobile impact. | Ready as input source. | Snapshot in payroll run. |
| Leave and absence input | Leave/absence records exist. | Confirmed absence feeds payroll only through approved payroll input snapshots. | HR leave/absence. | HR WebAdmin for absence; payroll consumes snapshots. | Sensitive absence details must not leak to payroll payloads. | Can affect gross/net rules later. | No mobile impact. | Ready as input source. | Snapshot safe categories only. |
| Tax and social-insurance rules | Not canonical. | Rule records must be legal-versioned, jurisdiction-scoped, and effective-dated. | Payroll rule foundation. | Payroll admin handlers. | Highest compliance sensitivity. | Drives liability and employer cost. | No mobile impact. | Needs design/core. | Implement rule foundation before payroll run. |
| Gross-to-net calculation | Not canonical. | Darwin owns calculation evidence internally; provider submission is an adapter later. | Payroll engine. | Payroll calculation handler. | Must be reproducible by rule version and source snapshot. | Net pay and deductions feed posting later. | Payslip later derives from run snapshot. | Needs engine foundation. | Build after rule foundation. |
| Employer cost calculation | Not canonical. | Employer cost must be calculated separately from employee net pay and stored as payroll run evidence. | Payroll engine. | Payroll calculation handler. | Required for true labor cost visibility. | Later posts employer liabilities and expenses. | No mobile impact. | Needs engine foundation. | Include in payroll run model. |
| Payslip generation | Internal artifact foundation exists. | Payslips are generated from immutable payroll run/source snapshots, not live employee, time, or contract data. | Payroll payslip artifact service. | HR/Payroll WebAdmin after payroll run approval. | Payslips are sensitive documents with retention controls. | Does not create posting by itself. | Employee self-service is additive later. | Implemented for internal versioned PDF artifact with retained HTML source. | `Payroll Provider Adapter Design Slice` next. |
| Payroll period close | Payroll period approval exists. | Payroll period approval is a prerequisite input gate, not payroll calculation or close. | HR/payroll period. | HR WebAdmin. | Prevents calculating unreviewed time. | No posting until payroll run. | No mobile impact. | Implemented summary gate. | Payroll run consumes approved period. |
| Payroll liability posting | Finance posting exists. | Payroll liability starts only from approved payroll run posting policy, not from summaries or payslips. | Finance posting plus payroll. | Payroll posting handler. | Requires auditable payroll result. | Journal entries become export-ready. | No mobile impact. | Implemented for approved payroll runs. | Payroll payment/treasury boundary after posting. |
| Salary payment and treasury | Supplier payment and bank settlement exist, but salary payment is not canonical. | Salary payment is a separate payroll/treasury boundary and must not reuse supplier payment or customer payment flows. | Future payroll payment boundary. | Payroll/treasury handlers. | Bank privacy and payroll privacy overlap. | Clears payroll payable through treasury. | No mobile impact in v1. | Boundary design complete. | `Payroll Payment Core Model And Admin Slice`. |
| Statutory filing/provider submission | External references and file-delivery foundations exist. | Provider submission starts only after a real target, credential owner, payload mapping, retry/error contract, and smoke strategy are selected. | Future payroll provider adapter. | Provider adapter handlers. | Filing failure must not be hidden as success. | Filing does not replace posting. | No mobile impact. | Boundary design complete; implementation blocked until target selection. | Target adapter design after target selection. |
| Correction and reversal | Payroll payment reversal, bank settlement, and bank correction now exist. | Payroll corrections require explicit adjustment/reversal models; payroll run history must not be rewritten. | Payroll correction models plus later legal payroll adjustment models. | Payroll correction handlers. | Legal audit requires original and corrected evidence. | Reversals/adjustments must post formally. | Payslip correction visibility later. | Payment-bank correction implemented; legal payroll adjustment remains separate. | Keep payslip template/PDF completion separate from payment correction. |
| Personnel document evidence | `DocumentRecord` backed personnel files exist. | Payroll may link to required HR documents, but legal payroll data must not hide inside personnel documents. | HR document foundation. | HR document handlers. | Documents carry privacy/retention controls. | Documents do not create liability. | No mobile exposure in v1. | Ready. | Link evidence only when needed. |
| External payroll identity | `ExternalReference` exists. | Store external employee, payroll run, filing, and provider receipt ids through `ExternalReference`. | Integration foundation. | Payroll/provider handlers. | No provider-specific columns or raw payloads. | Supports reconciliation to filing systems. | No mobile impact. | Ready after target exists. | Use in provider adapter slice. |
| Lifecycle event/audit | `BusinessEvent` and `AuditTrail` exist. | Payroll events store safe ids, dates, statuses, amounts, jurisdiction, and rule versions only. | Payroll handlers. | Handler-owned only. | No private document content, credentials, or raw provider payloads. | Supports finance audit. | No mobile impact. | Ready. | Apply in all payroll slices. |
| WebAdmin/internal visibility | HR workspace exists. | v1 payroll calculation, run review, payslip review, and posting stay internal/WebAdmin-only. | WebAdmin plus payroll handlers. | HR/Payroll WebAdmin. | Sensitive operator-only workflow. | Finance links read journal entries. | Employee self-service later only by additive contract. | Ready after model. | Add Payroll workspace pages with run core. |
| Public/mobile/storefront boundary | Public/mobile/storefront are not payroll surfaces. | Keep unchanged. Employee payslip self-service requires dedicated additive design and contract tests. | Existing public/mobile owners. | No payroll mutation. | Prevents accidental payroll exposure. | No finance export change. | No route or DTO change now. | Locked. | Add source guards during implementation. |

## Locked Decisions

- Legal payroll starts Germany-first and remains country/legal-rule-version aware so later countries can be added without redesigning the payroll run, payslip, posting, or provider adapter boundaries.
- Darwin owns payroll calculation evidence internally. Provider submission is a future adapter path and must not replace Darwin's payroll run, source snapshots, audit, or posting evidence.
- `PayrollPeriod` remains a review/export-ready summary container. It is not a payroll run, does not calculate gross-to-net pay, does not generate payslips, does not create statutory filings, and does not post payroll liability.
- A future payroll run must calculate from immutable source snapshots of employee, employment contract, approved time, confirmed absence, and rule versions. It must not live-recompute from mutable HR records after calculation.
- Payslips must be generated from immutable payroll run/source snapshots. They are sensitive documents and require retention, privacy, and access controls similar to personnel documents.
- Payroll liability must be posted only through finance posting handlers after payroll run approval. Manual journal shortcuts, status-only liability, generic documents, or payroll summaries must not create payroll accounting.
- Salary payment is a future payroll/treasury boundary. It must not reuse customer `Payment`, customer `Refund`, supplier payment, or supplier advance flows.
- Statutory filing and provider submission require a real target, credential owner, payload mapping, retry policy, safe error contract, and smoke strategy before implementation. Filing success must never be faked by a no-network or placeholder adapter in production.
- Payroll corrections must preserve original run history and use explicit correction/reversal records and postings. A payroll run, payslip, journal entry, or provider filing must not be rewritten silently.
- Provider credentials, access tokens, private keys, connection strings, raw provider payloads, private document contents, medical details, and raw HR import files must not be stored in metadata, document metadata, events, audit trails, external references, logs, tests, or documentation.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, and invoice archive/download contracts remain unchanged in the design step.

## Implementation Sequence

1. `Payroll Legal Calculation Boundary Design`: complete with this document.
2. `Payroll Calculation Rule Foundation Slice`: complete. `PayrollRuleSet` and `PayrollRuleComponent` provide Germany-first, country/version-aware payroll rule foundations, effective dates, safe metadata validation, WebAdmin/internal review surfaces, PostgreSQL/SQL Server migrations, and source guards without running payroll or filing externally.
3. `Payroll Run Core Model And Admin Slice`: complete. `PayrollRun`, `PayrollRunLine`, and `PayrollRunLineComponent` snapshot approved payroll periods, employee/contract/time/absence summaries, rule versions, and configured-rule component results with internal WebAdmin create/calculate/review/approve/cancel workflow.
4. `Payroll Payslip Artifact Slice`: complete. `PayrollPayslip` stores internal artifact metadata, generates immutable artifacts from approved payroll run snapshots, stores them through object storage, links official PDF output with `DocumentRecord`, retains HTML source artifacts for internal traceability, and exposes WebAdmin download without employee self-service in v1.
5. `Payroll Posting And Liability Slice`: complete. Approved payroll runs post liabilities and employer costs through finance posting handlers, preserving export compatibility through posted `JournalEntry` records and deterministic posting keys.
6. `Payroll Payment And Treasury Boundary Design`: complete. Salary payment, employee allocation, bank evidence, reversal, returned transfer, privacy, and reconciliation boundaries are locked separately from supplier and customer settlement flows.
7. `Payroll Payment Core Model And Admin Slice`: complete. Formal salary payment records, employee allocations, payroll payable clearing, WebAdmin workflow, and source guards are implemented.
8. `Payroll Payment Reversal Boundary/Core Slice`: complete. Posted salary payments can be fully reversed through journal-backed correction before bank settlement or returned-transfer handling.
9. `Payroll Payment Bank Settlement Boundary/Core Slice`: complete. Posted salary payments can be linked to matched bank reconciliation evidence and clear `CashClearing` to mapped bank Asset accounts without bank API or credential UI.
10. `Payroll Returned Transfer And Correction Boundary Slice`: complete. Failed or returned salary transfer correction is evidence-backed, full-settlement v1, journal-owned, and separated from payroll payment status edits.
11. `Payroll Returned Transfer And Correction Core Slice`: complete. `PayrollPaymentBankCorrection` provides evidence-backed returned-transfer correction and duplicate salary bank movement attention without rewriting payment, settlement, payslip, or export history.
12. `Payroll Payslip Template/PDF Completion Slice`: complete formal template/PDF rendering over the immutable payslip artifact foundation.
13. `Payroll Provider Adapter Design Slice`: complete. Provider-neutral statutory filing boundaries are locked; implementation remains blocked until a real target, credential owner, payload mapping, error contract, and smoke strategy are selected.
14. `Employee Payslip Self-Service Boundary Design`: complete. Employee-facing payslip visibility must use dedicated additive public/mobile/member contracts, own-employee authorization, official PDF downloads, privacy-safe audit, and WebAdmin route separation.

## Implementation Outcome

- Legal payroll boundaries, payroll rule foundations, payroll run core, internal payslip artifact generation, payroll liability posting, payroll payment/treasury boundary design, payroll payment core/admin, payroll payment full-reversal, payroll bank settlement, payroll returned-transfer correction boundary design, and payroll returned-transfer correction core are complete for the current phase.
- Formal payslip template/PDF completion is complete for the current phase with built-in template code/version metadata and WebAdmin PDF download.
- Payroll provider adapter boundary design is complete for the current phase.
- Provider submission remains blocked until a real provider target and credential/error contract exist.
- Employee payslip self-service boundary design and core implementation are complete for the current phase.
- AI readiness and automation governance are complete for the current phase. The current gate is production go-live evidence execution unless a real payroll provider target is selected first.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export package format, supplier finance, customer payment/refund, and invoice archive/download behavior remain unchanged.

## Compatibility Guards

- Do not create payroll calculation, payroll posting, salary payment, statutory filing, or provider submission from `PayrollPeriod` summaries alone.
- Do not reuse customer `Payment`, customer `Refund`, supplier payment, supplier advance, finance export packages, or invoice archive/download surfaces for payroll settlement or payslip delivery.
- Do not store legal payroll rule results only in generic JSON, notes, documents, or event payloads when they are reportable, compliance-relevant, or accounting-relevant.
- Do not expose payroll calculation, payslip downloads, provider filing, salary payment, or payroll correction through public/mobile/member routes until a dedicated additive contract is designed and tested.
- Do not store credentials, tokens, private keys, connection strings, raw provider payloads, private document contents, medical details, or raw HR imports in domain metadata, document metadata, external references, events, audit trails, logs, tests, or documentation.

## Documentation Verification

- `docs/README.md` links this document from the ERP expansion map.
- `BACKLOG.md` and `erp-expansion-master-status.md` record employee payslip self-service core and AI governance as complete, with production go-live evidence execution as the current gate.
- This document contains no deferred ambiguous decisions for the legal payroll boundary.
- Restricted vendor/source scans must return no output.
