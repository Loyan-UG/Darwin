# Darwin Production Readiness Evidence Package

Deployment label: {{DEPLOYMENT_LABEL}}

Release reference: {{RELEASE_REFERENCE}}

Prepared at UTC: {{PREPARED_AT_UTC}}

Prepared by: {{PREPARED_BY}}

This package records non-secret go-live evidence for one Darwin deployment. Do not add customer names, private domains, credentials, access keys, webhook secrets, private signing material, provider payloads, bank identifiers, payroll internals, private document contents, customer personal data, or raw logs that include private values.

## 1. Deployment Identity

| Field | Evidence |
| --- | --- |
| Environment label | {{DEPLOYMENT_LABEL}} |
| Release reference | {{RELEASE_REFERENCE}} |
| Surfaces in scope | Pending operator entry |
| Persistence provider | Pending operator entry |
| Migration owner | Pending operator entry |
| Object-storage provider family | Pending operator entry |
| Payment provider scope | Pending operator entry |
| Shipping provider scope | Pending operator entry |
| Communication provider scope | Pending operator entry |
| VAT validation scope | Pending operator entry |
| E-invoice scope | Pending operator entry |
| Mobile launch scope | Android first unless deployment record says otherwise |

## 2. Build, Migration, And Rollback Evidence

| Evidence item | Non-secret reference | Owner | Result |
| --- | --- | --- | --- |
| Release commit or artifact id | Pending operator entry | Darwin technical owner | Pending |
| Build lane | Pending operator entry | Darwin technical owner | Pending |
| Focused module test lanes | Pending operator entry | Darwin technical owner | Pending |
| Public/mobile compatibility lanes | Pending operator entry | Darwin technical owner | Pending |
| Migration plan | Pending operator entry | Migration owner | Pending |
| Backup checkpoint before migration | Pending operator entry | System admin or DevOps owner | Pending |
| Rollback plan | Pending operator entry | Darwin technical owner | Pending |
| Support and escalation path | Pending operator entry | Operations owner | Pending |
| Production-like staging rehearsal | Pending operator entry | Darwin technical owner | Pending |

## 3. Database Readiness

| Evidence item | Non-secret reference | Owner | Result |
| --- | --- | --- | --- |
| Backup schedule | Pending operator entry | System admin or DevOps owner | Pending |
| Restore test | Pending operator entry | System admin or DevOps owner | Pending |
| Migration role and grants review | Pending operator entry | System admin or DevOps owner | Pending |
| Monitoring owner | Pending operator entry | System admin or DevOps owner | Pending |
| Alert owner | Pending operator entry | System admin or DevOps owner | Pending |

## 4. Object Storage And Retention

| Profile | Provider/destination evidence | Retention or immutability evidence | Owner | Result |
| --- | --- | --- | --- | --- |
| InvoiceArchive | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| ShipmentLabels | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| MediaAssets | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| FinanceExports | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| FinanceExportOutbound | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| PersonnelDocuments | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| PayrollPayslips | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| MinIO production readiness preflight | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |
| Azure Blob readiness preflight | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |

Required notes:

- MinIO is the first production object-storage target for the current roadmap.
- Azure Blob readiness is the next storage hardening path after MinIO evidence, with `scripts\check-azure-object-storage-readiness.ps1` as the non-secret preflight.
- File-system or database-backed storage is local/internal fallback unless an approved deployment record says otherwise.
- Finance export push remains blocked unless `FinanceExportOutbound` is a valid non-database destination.

## 5. E-Invoice Acceptance

| Evidence item | Non-secret reference | Owner | Result |
| --- | --- | --- | --- |
| ZUGFeRD/Factur-X fixture list | Pending operator entry | Accounting/tax owner | Pending |
| ZUGFeRD/Factur-X generated artifact reference | Pending operator entry | Darwin technical owner | Pending |
| ZUGFeRD/Factur-X validation report reference | Pending operator entry | Accounting/tax owner | Pending |
| XRechnung fixture list | Pending operator entry | Accounting/tax owner | Pending |
| XRechnung generated artifact reference | Pending operator entry | Darwin technical owner | Pending |
| XRechnung validation report reference | Pending operator entry | Accounting/tax owner | Pending |
| InvoiceArchive storage/download smoke | Pending operator entry | Darwin technical owner | Pending |
| Accounting/tax reviewer sign-off | Pending operator entry | Accounting/tax owner | Pending |
| E-invoice production readiness preflight | Pending operator entry | Darwin technical owner | Pending |

JSON, HTML, CSV, and source-model exports are not compliant e-invoice output by themselves.

## 6. Provider Smokes

| Provider area | Smoke or preflight | Non-secret reference | Owner | Result |
| --- | --- | --- | --- | --- |
| Stripe test mode | Pending operator entry | Pending operator entry | Darwin technical owner | Pending |
| Stripe live readiness | Pending operator entry | Pending operator entry | Customer business owner | Pending |
| Stripe webhook forwarding | Pending operator entry | Pending operator entry | Darwin technical owner | Pending |
| DHL live/account product | Pending operator entry | Pending operator entry | Operations owner | Pending |
| Brevo transactional delivery | Pending operator entry | Pending operator entry | Operations owner | Pending |
| Brevo callback monitoring | Pending operator entry | Pending operator entry | Operations owner | Pending |
| VIES VAT validation | Pending operator entry | Pending operator entry | Accounting/tax owner | Pending |
| Object storage smoke | Pending operator entry | Pending operator entry | System admin or DevOps owner | Pending |

No fake labels, fake tracking values, fake provider references, fake delivered exports, fake filing submissions, or fake provider success may be recorded as production evidence.

## 7. Mobile Release Evidence

| Evidence item | Non-secret reference | Owner | Result |
| --- | --- | --- | --- |
| Android signed release artifact | Pending operator entry | Darwin technical owner | Pending |
| Android release channel decision | Pending operator entry | Customer business owner | Pending |
| Push configuration smoke | Pending operator entry | Darwin technical owner | Pending |
| Maps configuration smoke | Pending operator entry | Darwin technical owner | Pending |
| Native Google sign-in configuration when enabled | Pending operator entry | Darwin technical owner | Pending |
| Physical camera QR validation | Pending operator entry | Operations owner | Pending |
| Public/mobile route compatibility tests | Pending operator entry | Darwin technical owner | Pending |
| Android readiness preflight | Pending operator entry | Darwin technical owner | Pending |

Android is the first launch target. Later platform launch evidence must be added by a separate approved deployment record.

## 8. WebAdmin Operational Readiness

| Evidence item | Non-secret reference | Owner | Result |
| --- | --- | --- | --- |
| Operator roles | Pending operator entry | Operations owner | Pending |
| Support queues | Pending operator entry | Operations owner | Pending |
| Alert recipients | Pending operator entry | Operations owner | Pending |
| Escalation path | Pending operator entry | Operations owner | Pending |
| Finance export file-delivery readiness | Pending operator entry | Accounting/tax owner | Pending |
| Bank/treasury operational readiness | Pending operator entry | Accounting/tax owner | Pending |
| HR/payroll operational readiness when in scope | Pending operator entry | HR/payroll owner | Pending |

## 9. Final Sign-Off

| Approval | Owner | Non-secret reference | Result |
| --- | --- | --- | --- |
| Business scope approval | Customer business owner | Pending operator entry | Pending |
| Accounting/tax approval | Accounting/tax owner | Pending operator entry | Pending |
| Operations approval | Operations owner | Pending operator entry | Pending |
| System administration approval | System admin or DevOps owner | Pending operator entry | Pending |
| Legal/compliance approval when required | Legal/compliance owner | Pending operator entry | Pending |
| Darwin technical approval | Darwin technical owner | Pending operator entry | Pending |

## 10. Blockers And Owner Assignments

| Blocker | Owner | Required evidence to clear | Decision |
| --- | --- | --- | --- |
| Pending operator entry | Pending operator entry | Pending operator entry | Pending |

Go-live is blocked until every critical blocker has an owner, a decision, and a non-secret evidence reference.
