# Production Readiness Evidence Package

Reviewed: 2026-06-18

This document defines the deployment evidence package that must exist before Darwin is treated as production-ready for a customer. It is deployment-neutral and must not contain customer names, domains, credentials, access keys, webhook secrets, private signing material, raw provider payloads, bank identifiers, payroll internals, or private document contents.

Use this evidence package with [production-go-live-evidence-execution-plan.md](production-go-live-evidence-execution-plan.md), [production-setup.md](production-setup.md), [customer-deployment-onboarding-checklist.md](customer-deployment-onboarding-checklist.md), [external-smoke-inputs.md](external-smoke-inputs.md), [go-live-status.md](go-live-status.md), and [e-invoice-acceptance-checklist.md](e-invoice-acceptance-checklist.md).

For a deployment-specific working copy, generate a non-secret package from [templates/production-readiness-evidence-package-template.md](templates/production-readiness-evidence-package-template.md):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\new-production-readiness-evidence-package.ps1 -OutputPath artifacts\production-readiness\evidence-package.md -DeploymentLabel "production-candidate" -ReleaseReference "release-artifact-id" -PreparedBy "Darwin technical owner"
```

The generated package belongs in the deployment evidence repository or ignored local `artifacts` folder. Do not commit filled customer evidence, provider reports, legal approvals, private artifacts, or generated e-invoice files to source control.

Generate and validate the non-secret local readiness report bundle before final package validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-report-bundle.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-report-bundle.ps1
```

The bundle gathers current-state local readiness reports under the ignored `artifacts\production-readiness\` path, including local release-candidate build/test, local backup package structure, local PostgreSQL restore rehearsal, and evidence-package validator smoke reports when their prerequisites are present. It also refreshes the action plan, owner handoff, environment template, local execution summary, and local evidence-package draft so the validator can prove those follow-up artifacts exist and match the current branch and commit where applicable. The bundle report table records the exit code declared inside each report, so `Blocked` rows retain exit code `2` even though the exporter process succeeds after writing the artifact. The local execution summary records the current local report outcome from the same bundle run. The local draft exporter fills an ignored evidence-package draft with those report/helper references and appends a local supporting-evidence snapshot copied from the bundle while keeping deployment-specific rows blocked. The action-plan exporter turns report results into owner rows and missing evidence keys for operator follow-up. The owner-handoff exporter groups those rows by owner so deployment follow-up can be assigned without exposing secrets. The environment-template exporter creates an ignored placeholder script from the dedicated readiness reports, excluding the aggregate go-live row, so operators can prepare owner-scoped local/session values without storing values in source control. The individual helper exporters remain available for focused reruns while debugging, but the bundle exporter is the normal source of current local evidence artifacts. These reports are attachment evidence for operators and reviewers; they do not replace production-like staging proof, private provider dashboards, customer approvals, or the filled evidence package.

The bundle validator blocks stale local reports and generated helpers whose `Branch`, `Commit`, or release-reference metadata does not match the checked-out repository. It also checks that the bundle index rows match each generated report's status and exit code. Regenerate the ignored bundle after code or documentation commits before attaching it to a deployment evidence package.

Validate the filled package before go-live:

```powershell
$env:DARWIN_PRODUCTION_READINESS_EVIDENCE_PACKAGE_PATH = "artifacts\production-readiness\evidence-package.md"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-evidence-package.ps1
```

The validation check blocks placeholder text, unresolved open/blocked/failed result rows, missing required sections, missing required evidence markers, stale local readiness artifact references, and sensitive value patterns. When a `Ready` row references a local readiness bundle or generated readiness helper, the validator requires that file to exist, match the current branch and commit, remain secret-free, and report `Ready` with exit code `0` when the referenced artifact carries readiness metadata. Required markers include production-like staging rehearsal, explicit MinIO production and Azure Blob readiness preflight rows, dual e-invoice formats, provider smokes, Android release evidence, and final approval rows. The central go-live dry-run report records MinIO and Azure Blob as separate readiness checks, while the dedicated storage reports can be attached as non-secret references. The package validator proves the package is structurally complete and non-secret; it does not verify the private evidence stored behind each reference.

## Purpose

The package is the operational proof that the selected deployment is ready. It does not replace code tests, legal review, provider dashboards, object-storage controls, or customer approvals. It gathers their non-secret outcomes in one operator-owned record so go-live decisions are based on evidence rather than informal status.

## Package Ownership

| Evidence area | Owner | Required proof | Storage rule |
| --- | --- | --- | --- |
| Build and migration readiness | Darwin technical owner | Build/test command outputs, migration plan, rollback plan. | Store command summaries and artifact hashes outside source control. |
| Database readiness | Customer system admin or DevOps owner | Backup schedule, restore test result, migration role/grant review, monitoring ownership. | Store provider screenshots or reports outside source control. |
| Object storage readiness | Customer system admin or DevOps owner | Selected-provider preflight, disposable-prefix smoke result, retention/legal-hold policy, backup/restore evidence, monitoring ownership. | Store bucket/container names only when approved for the deployment record; never store access keys or bucket policy secrets in docs. |
| Invoice and e-invoice evidence | Accounting/tax owner with Darwin technical owner | Fixture list, generated ZUGFeRD/Factur-X and XRechnung artifacts, validation reports, reviewer sign-off, storage/download smoke. | Store artifacts and validation reports in the approved evidence repository or object storage, not in git. |
| Payment provider readiness | Customer business owner with Darwin technical owner | Test-mode smoke, live-readiness preflight, webhook events, callback worker status, refund/dispute playbook approval. | Store only non-secret references and timestamps in the package. |
| Shipping provider readiness | Operations owner with Darwin technical owner | Account/product validation, label storage smoke, tracking/return-label validation, callback/recovery evidence. | Store labels only in configured storage; package records hashes or operator references. |
| Communication provider readiness | Operations owner | Sender-domain checks, transactional smoke, webhook/callback monitoring, inbox-placement owner. | Do not store API keys or raw provider responses. |
| Mobile release readiness | Customer system admin with Darwin technical owner | Signed artifact validation, store/channel decision, push/maps configuration smoke, camera/device smoke. | Store build numbers, signing-profile references, and smoke outcomes outside source control. |
| WebAdmin operational readiness | Operations owner | Operator roles, queues, support workflows, alert recipients, escalation path. | Store role/queue sign-off without private user data. |

## Required Evidence Sections

### 1. Deployment Identity

Record:

- deployment environment label;
- selected app surfaces: WebAdmin, WebApi, Worker, Web, Consumer mobile, Business mobile;
- persistence provider and migration owner;
- object-storage provider family and profile decisions;
- provider scopes for payment, shipping, email, VAT, e-invoice, and mobile push/maps.

Do not record secrets, real private endpoint tokens, access keys, webhook secrets, private signing material, or customer personal data.

### 2. Build, Migration, And Rollback

Required proof:

- current commit or release artifact identifier;
- `dotnet build` lane used for the release candidate;
- focused test lanes for changed modules;
- compatibility smoke lanes for public/mobile/storefront-sensitive areas;
- readiness report bundle reference;
- owner action plan reference;
- owner handoff reference;
- evidence environment template reference;
- migration command plan for PostgreSQL or SQL Server;
- rollback plan for app binaries and database migration failure;
- support owner and escalation path.
- production-like staging rehearsal result before production execution, including the preflight result from `scripts\check-production-like-staging-readiness.ps1`.

Acceptance:

- build/test commands pass or every failure has an explicit owner and go-live decision;
- migration plan names the active provider and backup/restore checkpoint;
- rollback does not depend on ad hoc database edits;
- production is not the first environment where migration, rollback, storage, e-invoice, Android, provider, or ownership evidence is attempted.

### 3. Object Storage And Retention

Required proof:

- final decision for `InvoiceArchive`, `ShipmentLabels`, `MediaAssets`, `FinanceExports`, `FinanceExportOutbound`, `PersonnelDocuments`, and `PayrollPayslips`;
- selected-provider preflight result;
- Azure Blob readiness preflight result when Azure is the selected provider or the next hardening lane;
- disposable smoke prefix approval;
- selected-provider smoke result;
- Object Lock, retention, legal-hold, backup, restore, failed-write monitoring, and alert ownership;
- retention/legal-hold policy owner for invoice, HR personnel, and payroll artifacts.

Acceptance:

- production immutability is claimed only after provider-level controls are validated;
- file-system or database-backed storage is treated as local/internal fallback unless the deployment explicitly approves a non-immutable use case;
- finance export push remains blocked unless `FinanceExportOutbound` is a valid non-database destination.

### 4. E-Invoice Acceptance

Required proof:

- deployment scope for ZUGFeRD/Factur-X and XRechnung;
- pinned generator/validator tooling and checksum reference outside source control;
- e-invoice production readiness preflight result from `scripts\check-einvoice-production-readiness.ps1`;
- deterministic fixture list approved by accounting/tax owner;
- generated ZUGFeRD/Factur-X artifact, generated XRechnung artifact, extracted structured XML where applicable, validation report, and reviewer sign-off for each approved scenario;
- storage/download smoke through `InvoiceArchive`;
- explicit statement that JSON, HTML, CSV, and source-model exports are not presented as compliant e-invoices.

Acceptance:

- generated artifacts are exposed as compliant only after validation and review sign-off for both selected formats;
- failed validation remains operator evidence and is not customer/member-facing compliance output;
- retention preserves the structured e-invoice part unchanged for the required period.

### 5. Provider Smokes

Required proof:

- Stripe test-mode smoke and live-readiness preflight when live payments are in scope;
- DHL account/product smoke when shipping is in scope;
- Brevo sender/webhook/callback smoke when transactional email is in scope;
- VIES controlled valid/invalid/provider-failure smoke when VAT validation is in scope;
- provider callback worker and alert ownership.

Acceptance:

- browser return routes do not finalize payments or subscriptions;
- no fake labels, fake tracking values, fake provider references, fake delivered exports, fake filing submissions, or fake AI/provider success are recorded;
- every provider smoke result is tied to a run date, operator, and non-secret evidence reference.

### 6. Mobile Release Evidence

Required proof:

- launch target order across Android, iOS, MacCatalyst, and follow-up platforms;
- signed release artifacts;
- production push/maps configuration;
- Android readiness preflight result from `scripts\check-android-launch-readiness.ps1`;
- native Google sign-in configuration when enabled;
- physical camera QR validation with approved devices or camera feed;
- public/mobile route compatibility test evidence.

Acceptance:

- mobile launch is blocked until signed artifacts and device smoke pass for the selected target;
- mobile apps do not bypass WebApi contract boundaries;
- unsafe release certificate trust and broad cleartext HTTP remain blocked.

### 7. Operational Sign-Off

Required proof:

- customer business owner scope approval;
- customer accounting/tax owner invoice and e-invoice approval;
- operations owner queue/support approval;
- system admin or DevOps backup, restore, monitoring, and alert approval;
- Darwin technical owner release and smoke approval;
- rollback and support contact path.

Acceptance:

- unresolved blockers are assigned to an owner;
- customer approvals are stored outside source control;
- no deployment-specific secrets or private payloads are copied into repository documentation.

## Hard Gates

Darwin is not production-ready for a customer deployment until:

- backup and restore evidence exists for the selected database;
- object-storage retention/legal-hold behavior is validated for the selected provider;
- e-invoice compliance output is backed by approved fixtures and validation evidence when generation is in scope;
- provider smokes are passed or explicitly deferred by the relevant owner;
- monitoring and alert ownership are assigned;
- public/mobile compatibility lanes remain green for the release scope;
- final customer and Darwin technical approvals are recorded outside source control.

## Current Outcome

The no-target production readiness hardening path is documented and now includes a reusable non-secret evidence package template, local generation and validation scripts, a readiness report bundle, an owner action-plan exporter, a local execution summary exporter, and an ignored evidence environment-template exporter. A local working package is generated under the ignored `artifacts\production-readiness\` path and paired with a current-branch non-secret local execution summary from the same bundle run. The package correctly remains blocked until deployment owners provide real staging, provider, storage, e-invoice, mobile, monitoring, rollback, and approval evidence.

This readiness path adds no entity, migration, route, DTO, WebAdmin mutation, public/mobile/storefront contract, provider credential UI, finance export format change, payment/refund change, supplier finance change, invoice archive/download behavior change, bank API, AI provider, or accounting API adapter.

The next implementation work still requires target selection when it involves a real AI provider, direct operational AI command execution, two-way sync, accounting API delivery, Stripe live execution, DHL account/product validation, e-invoice legal acceptance, or mobile store launch.
