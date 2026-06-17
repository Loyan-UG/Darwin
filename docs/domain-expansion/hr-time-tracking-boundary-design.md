# HR And Time Tracking Boundary Design

## Summary

This step is documentation and design only. It adds no entity, migration, route, DTO, WebAdmin mutation, public WebApi contract, mobile/member contract, storefront behavior, finance export format, supplier finance flow, customer payment/refund flow, invoice archive/download behavior, payroll calculation, payroll filing, or production time-clock flow.

The goal is to lock the professional HR and time-tracking boundaries before adding schema or UI. Darwin will model HR records as formal internal ERP records while keeping the existing business access model stable.

## Current Darwin HR And Time Findings

- `BusinessMember` and existing identity/access workflows are business access and permission records. They are not personnel master records, employment contracts, payroll subjects, or time-tracking records.
- WebAdmin already owns business users, roles, invitations, feature visibility, and operational administration. HR must not overload those access records with personnel, employment, payroll, or attendance state.
- Foundation primitives exist for `DocumentRecord`, `BusinessEvent`, `AuditTrail`, `ExternalReference`, custom fields, feature areas, and number sequences.
- Darwin now has canonical `Employee`, `Department`, `Position`, `EmploymentContract`, `WorkSchedule`, `AttendanceEvent`, `TimeEntry`, `Timesheet`, `LeaveRequest`, `AbsenceRecord`, `PayrollPeriod`, and `PayrollPeriodLine` models.
- Personnel documents, attendance records, absence records, payroll-period summaries, and approval evidence are privacy-sensitive and require stricter metadata and audit discipline than ordinary operational notes.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export, supplier invoice/payment, customer payment/refund, and warehouse PWA flows are not HR/time surfaces.
- Legal payroll calculation, payslip generation, statutory filing, tax/social-insurance rules, and external payroll-provider integration are not canonical yet and must not be simulated with status fields, notes, manual journals, or document-only records.

## Decision Matrix

| HR/time surface | Current Darwin model | Decision | Owning source | Allowed mutation owner | Privacy/audit impact | Payroll impact | Mobile/member impact | Implementation readiness | Next action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Employee/personnel master | No canonical HR record. | Create future `Employee` as the formal personnel record. | HR/Application handlers. | HR WebAdmin only in v1. | Personal data requires safe metadata and audit. | Payroll subject identity later derives from employee. | No public/mobile exposure in v1. | Ready after this design. | `HR Core Model And Admin Slice`. |
| Business member linkage | `BusinessMember` controls business access. | Optional employee-to-business-member link; no reuse as employee. | Identity/access remains `BusinessMember`; HR owns employee link. | HR may link/unlink; access remains identity handlers. | Link changes are audited. | Link is not payroll eligibility. | No member contract change. | Ready. | Implement optional link field. |
| Department/team | No HR org structure. | Create HR department/team structure independent from permissions. | HR. | HR WebAdmin. | Normal internal HR audit. | Can support payroll grouping later. | No impact. | Ready. | Include in HR core. |
| Position/job role | Roles exist for access, not jobs. | Create HR position/job role separate from security roles. | HR. | HR WebAdmin. | Employment-related history is audited. | Can support payroll/rate policy later. | No impact. | Ready. | Include in HR core. |
| Employment contract | No canonical contract model. | Model contract metadata as HR employment evidence. | HR. | HR WebAdmin. | Privacy classification is required. | Payroll calculation remains gated. | No impact. | Ready for metadata; payroll terms later. | Include contract metadata in HR core. |
| Personnel document metadata | `DocumentRecord` exists. | Use `DocumentRecord` metadata with HR privacy classification; binary upload/download/retention comes in a dedicated privacy slice. | HR plus document foundation. | HR WebAdmin metadata only in first core slice. | Sensitive document categories must not leak through notes/events. | Does not calculate payroll. | No impact. | Metadata ready; binary flow later. | Core metadata now, privacy hardening later. |
| Work schedule | No canonical schedule. | Future formal schedule model owned by HR/time. | HR/time. | HR WebAdmin. | Schedule changes are audited. | Feeds time/payroll-period summaries later. | No impact in v1. | Needs HR core first. | `HR Time And Attendance Core Slice`. |
| Attendance event | No canonical time clock. | Future attendance events are internal facts, not payroll by themselves. | HR/time. | WebAdmin/internal time surface first. | Attendance is privacy-sensitive. | Feeds summaries, not legal payroll. | Native/PWA clock later only after device/privacy design. | Needs HR core. | Time and attendance slice. |
| Time entry | No canonical time entry. | Time entries are HR/time records with approval state. | HR/time. | WebAdmin/internal in v1. | Changes and approvals are audited. | Feeds payroll-period summaries only. | No mobile/member exposure in v1. | Needs HR core. | Time and attendance slice. |
| Absence and leave request | No canonical absence/leave model. | Formal leave requests and absences with submit/review/approve/reject. | HR/time. | HR WebAdmin. | Absence reason categories require privacy discipline. | Feeds summaries, not legal payroll. | No public/member exposure. | Needs HR core. | Leave and absence slice. |
| Timesheet | No canonical timesheet. | Timesheets aggregate time entries for submit/review/approve/reject. | HR/time. | HR WebAdmin. | Approval history is audit evidence. | Payroll-period summaries read approved time. | No mobile/member exposure in v1. | Needs time entry model. | Time and attendance slice. |
| Approval workflow | General audit exists. | Timesheet and leave use explicit submit/review/approve/reject lifecycle. | HR/time policy handlers. | HR WebAdmin. | Deterministic events and audit entries. | Approved facts feed summaries. | No mobile/member impact. | Ready after models. | Implement central workflow policies. |
| Payroll period | No payroll-period model. | Payroll periods summarize approved HR/time facts for review/export readiness. | HR/payroll boundary. | HR/Finance-adjacent WebAdmin after design. | Payroll summaries are sensitive. | Summary/export-ready only; no legal calculation. | No impact. | Needs HR/time facts. | Payroll period summary slice. |
| Payroll calculation and filing | Not canonical. | Block legal payroll calculation, payslips, statutory filing, and provider submission until dedicated payroll design. | Future payroll foundation. | None in current phase. | Highest privacy/compliance sensitivity. | Not implemented now. | No impact. | Not ready. | Dedicated payroll design after summaries. |
| External HR/payroll identity | `ExternalReference` exists. | Store external employee/payroll ids through `ExternalReference`, not provider-specific columns. | HR plus integration foundation. | HR/integration handlers. | No credentials or raw provider payload. | Supports future payroll export. | No impact. | Ready when external target exists. | Use in HR core/import slices. |
| Lifecycle events and audit | `BusinessEvent` and `AuditTrail` exist. | HR events store safe ids/status/dates only; no medical details, raw documents, credentials, or private payloads. | HR/Application handlers. | Handler-owned only. | Required for every lifecycle mutation. | Supports compliance evidence. | No impact. | Ready. | Apply in all HR slices. |
| WebAdmin/internal visibility | No HR workspace yet. | HR starts as internal WebAdmin workspace. | WebAdmin plus HR handlers. | WebAdmin only in v1. | HR pages must avoid broad diagnostic exposure. | Payroll UI limited to summaries until legal design. | No public/mobile/storefront impact. | Ready. | Add HR workspace with core slice. |
| Public/mobile/storefront boundary | Public/mobile/storefront not HR. | Keep unchanged. Native/PWA time clock requires separate device/offline/privacy design. | Existing public/mobile owners. | No HR mutation. | Protect personnel data from non-HR surfaces. | No impact. | No route or DTO change in v1. | Locked. | Add source/contract guards during implementation. |

## Locked Decisions

- Future `Employee` is a formal HR/personnel record. It is not the current `BusinessMember`, and it does not replace identity or business access records.
- `BusinessMember` remains the business access, role, invitation, and permission source. An employee may link to a business member when the person also has system access.
- Department and position are HR organization records. They are not security roles and must not drive authorization by themselves.
- Employment contract records in the first implementation store HR metadata and employment evidence. They do not calculate payroll or create legal payroll obligations.
- Personnel documents use object-storage-backed `DocumentRecord` workflows with HR privacy classification, retention metadata, legal-hold metadata, WebAdmin upload/download/archive, and audit evidence.
- Work schedule, attendance events, time entries, leave requests, absences, and timesheets are internal/WebAdmin-first in v1.
- Timesheet and leave workflows use submit, review, approve, and reject states. Approval state is evidence and must be row-version protected.
- Payroll v1 is limited to payroll periods and export-ready summaries derived from approved HR/time facts. Legal payroll calculation, payslip generation, statutory filing, tax/social-insurance rules, and external payroll submission stay blocked until a dedicated payroll design is complete.
- HR events and audit payloads contain only safe reportable ids, dates, statuses, totals, and business/employee ids. Medical details, private document contents, provider credentials, tokens, raw payroll payloads, and raw HR import files must not be stored in metadata, events, audit, logs, or documentation.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export, supplier invoice/payment, customer payment/refund, and warehouse PWA contracts remain unchanged.

## Implementation Sequence

1. `HR Core Model And Admin Slice`: complete. `Employee`, `Department`, `Position`, and employment-contract metadata are implemented with optional `BusinessMember` linkage, WebAdmin list/detail/create/edit/archive, source guards, and docs.
2. `HR Time And Attendance Core Slice`: complete. `WorkSchedule`, `WorkScheduleException`, `AttendanceEvent`, `TimeEntry`, `Timesheet`, and `TimesheetLine` are implemented with WebAdmin schedule, attendance, time-entry, and period approval workflows.
3. `HR Leave And Absence Core Slice`: complete. `LeaveRequest` and `AbsenceRecord` are implemented with WebAdmin approval workflow, confirmed absence evidence, privacy classification, row-version protection, and event/audit evidence.
4. `Payroll Period And Export Summary Slice`: complete. Payroll periods and approved-time/absence summaries are implemented without legal payroll calculation or provider filing.
5. `Personnel Documents Privacy Hardening Slice`: complete. HR personnel document upload/download/archive/retention/privacy controls are implemented through object-storage-backed `DocumentRecord` workflows.
6. `Payroll Legal Calculation Boundary Design`: complete. Legal payroll is Germany-first and country/version-aware; Darwin owns calculation evidence internally, payroll runs must snapshot approved source facts and rule versions, payslips come from immutable run snapshots, posting requires finance posting handlers, salary payment has a separate treasury boundary, and provider filing waits for a real target and credential/error contract.
7. `Payroll Calculation Rule Foundation Slice`: complete. `PayrollRuleSet` and `PayrollRuleComponent` are implemented with Germany-first jurisdiction defaults, country/version-aware rule identity, effective dates, component methods/bases, internal WebAdmin review surfaces, safe metadata validation, and event/audit evidence.
8. `Payroll Run Core Model And Admin Slice`: complete. Payroll runs snapshot approved payroll periods, employee/contract/time/absence summaries, rule versions, and configured rule results without posting liability, paying salaries, submitting to providers, or exposing public/mobile routes.
9. `Payroll Payslip Artifact Slice`: complete. Internal payslip artifacts are generated only from approved payroll run snapshots, stored in object storage, linked through `DocumentRecord`, and downloadable in WebAdmin without employee self-service in v1. Formal versioned PDF output is the official download, with retained HTML source artifact evidence.
10. `Payroll Posting And Liability Slice`: complete. Approved payroll runs post liabilities and employer costs through finance posting handlers with deterministic journal-backed evidence.
11. `Payroll Payment And Treasury Boundary Design`: complete. Salary payment is separated from customer payment/refund, supplier payment, supplier advance, statutory filing, payslip artifacts, and manual journal shortcuts before payroll payment implementation.
12. `Payroll Payment Core Model And Admin Slice`: complete. Formal salary payment records and employee allocations are implemented over posted payroll runs, with payroll payable clearing through finance posting and internal WebAdmin workflow.
13. `Payroll Payment Reversal Boundary/Core Slice`: complete. Posted payroll payments can be fully reversed through finance posting while preserving original payment history.

## Implementation Outcome

- HR core master data is implemented in the `HumanResources` schema with internal WebAdmin pages for employees, departments, positions, and employment contracts.
- HR time and attendance is implemented in the `HumanResources` schema with internal WebAdmin pages for work schedules, schedule exceptions, attendance events, time entries, and timesheets.
- The selected v1 time approach is WebAdmin/internal: weekly schedules with date exceptions, raw time entries, attendance evidence, and period timesheet submit/review/approve/reject workflow.
- Leave and absence are implemented as dedicated HR records. Approved leave creates formal absence evidence and absence is not hidden in notes, time entries, or timesheet metadata.
- Payroll-period summaries are implemented from approved timesheets and confirmed absence facts fully inside the selected period. Legal payroll calculation, payslip generation, statutory filing, tax/social-insurance rules, and payroll-provider submission remain blocked.
- Personnel document privacy hardening is implemented through internal WebAdmin upload, download, archive, retention metadata, legal-hold metadata, and `DocumentRecord` evidence. Personnel files are not stored in notes, metadata, events, audit payloads, public/mobile routes, finance export, or invoice archive flows.
- Legal payroll calculation boundary design, payroll rule foundation, payroll run core, payslip artifact generation, formal payslip template/PDF completion, payroll liability posting, payroll payment/treasury boundary design, payroll payment core/admin, payroll payment full-reversal, payroll bank settlement, payroll returned-transfer correction boundary design, and payroll returned-transfer correction core are complete. Statutory filing, payroll-provider submission, and employee self-service remain blocked until their own implementation slices are complete.
- Public WebApi, mobile/member, storefront, customer invoice archive/download, finance export, supplier finance, customer payment/refund, and warehouse PWA contracts remain unchanged.

## Compatibility Guards

- Do not add public WebApi, mobile/member, or storefront HR routes in the first HR slices.
- Do not reuse customer `Payment`, `Refund`, supplier payment, finance export, or invoice archive/download surfaces for payroll or HR documents.
- Do not create payroll liability, payslips, tax/social-insurance calculations, statutory filing, or external payroll submission from HR core records.
- Do not store full identity documents, medical details, bank details, credentials, tokens, connection strings, or raw provider/import payloads in generic metadata.
- Keep HR authorization separate from HR organization structure: security roles still come from identity/access, not department or position.

## Documentation Verification

- `docs/README.md` links this document from the ERP expansion map.
- `BACKLOG.md` and `erp-expansion-master-status.md` record employee payslip self-service core as complete and move the next implementation gate to `AI-Readiness And Automation Governance`.
- This document contains no deferred ambiguous decisions for the first HR implementation slice.
- Restricted vendor/source scans must return no output.
