# Production Go-Live Evidence Execution Plan

Reviewed: 2026-06-18

This plan turns the current production-readiness decisions into an execution order. It adds no entity, migration, route, DTO, WebAdmin mutation, credential UI, public/mobile/storefront contract, finance export format change, provider adapter, AI provider, bank API, or accounting API adapter.

## Decision Baseline

- First execution environment: production-like staging before real production.
- Evidence ownership: one Darwin technical owner coordinates the package, with separate business, accounting/tax, operations, and system administration owners signing their own evidence rows.
- Object storage: MinIO is the first production target; Azure Blob readiness remains prepared as the next storage hardening lane.
- E-invoice: both ZUGFeRD/Factur-X and XRechnung need production evidence before German compliant rollout is claimed.
- Mobile: Android is the first launch target.
- Accounting export: file-delivery remains the production-safe path until a concrete accounting API target is selected.
- Integration: SyncState and SyncConflict are foundation only until a concrete inbound or two-way target is selected.
- AI: AI provider activation remains lower priority and target-gated.

## Execution Sequence

| Step | Gate | Required evidence | Owner |
| --- | --- | --- | --- |
| 1 | Create evidence package | Generated package from `scripts\new-production-readiness-evidence-package.ps1` using non-secret deployment labels. | Darwin technical owner |
| 2 | Production-like staging rehearsal | Build/test summary, Web storefront toolchain/runtime readiness, migration rehearsal, rollback rehearsal, object-storage preflights, provider preflights, e-invoice evidence, Android evidence, monitoring/alerting readiness, owner sign-off for the staging candidate, and a passing `scripts\check-production-like-staging-readiness.ps1` preflight. | Darwin technical owner with area owners |
| 3 | MinIO production evidence | Real target endpoint, TLS, dedicated least-privilege identity, versioning, Object Lock or equivalent retention/legal hold, backup, restore test, monitoring, alerting, disposable-prefix smoke, and runbook owner. | System admin or DevOps owner |
| 4 | Azure readiness preparation | Azure Blob preflight and runbook readiness as the next storage hardening lane; this is not a replacement for MinIO evidence unless the deployment explicitly selects Azure. | System admin or DevOps owner |
| 5 | Dual e-invoice evidence | ZUGFeRD/Factur-X and XRechnung fixtures, generated artifacts, validation reports, storage/download smoke, and accounting/tax sign-off. | Accounting/tax owner |
| 6 | Android-first launch evidence | Signed Android artifact, release channel, maps, push, Google sign-in when enabled, physical QR/device smoke, route compatibility, and passing mobile resource-name plus Android project preflights. | Darwin technical owner with system admin owner |
| 7 | Provider smokes | Stripe, DHL, Brevo, VIES, and object-storage smokes according to deployment scope. | Area owners |
| 8 | Final approval | Business, accounting/tax, operations, system administration, legal/compliance when required, and Darwin technical approvals. | Area owners |
| 9 | Production execution | Run final deployment only after the filled evidence package passes `scripts\check-production-readiness-evidence-package.ps1`. | Darwin technical owner |

## Hard Rules

- Production is not the first place to discover missing storage, e-invoice, Android, provider, migration, rollback, or ownership evidence.
- A staging rehearsal may use production-like endpoints only with explicit disposable-prefix and cleanup or retention approval.
- No package row may contain credentials, tokens, private keys, connection strings, raw provider payloads, private bank identifiers, private HR/payroll material, customer personal data, generated e-invoice payloads, or private approval documents.
- Fake labels, fake tracking values, fake payment success, fake delivered export success, fake e-invoice compliance, fake AI/provider success, and fake accounting API delivery are not valid evidence.
- File-delivery remains the accounting export path until a target-specific accounting API adapter is selected and implemented.
- SyncState and SyncConflict records must not be used to imply an inbound or two-way integration before a concrete target adapter exists.

## Validation Commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\new-production-readiness-evidence-package.ps1 -OutputPath artifacts\production-readiness\evidence-package.md -DeploymentLabel "production-like-staging" -ReleaseReference "release-artifact-id" -PreparedBy "Darwin technical owner"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-report-bundle.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-report-bundle.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-report-bundle-clean-smoke.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-local-package-draft.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-action-plan.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-owner-handoff.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-env-template.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-like-staging-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-like-staging-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-local-backup-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-local-backup-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-local-postgres-restore-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-local-postgres-restore-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-local-release-candidate-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-local-release-candidate-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-readiness-evidence-validator-smoke.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-web-toolchain-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-web-storefront-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-mobile-resource-names.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-android-project-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-web-mobile-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-go-live-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-minio-production-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-azure-object-storage-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-einvoice-production-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-android-launch-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-provider-readiness-report.ps1 -Force
$env:DARWIN_PRODUCTION_READINESS_EVIDENCE_PACKAGE_PATH = "artifacts\production-readiness\evidence-package.md"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-readiness-evidence-package.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-go-live-readiness.ps1
```

The bundle exporter runs every dedicated non-secret readiness report exporter and writes `readiness-report-bundle.md` under `artifacts\production-readiness\`. It also refreshes the owner action plan, owner handoff, environment template, and local evidence-package draft so a single bundle run leaves all local follow-up artifacts current. During bundle generation only, the embedded aggregate go-live report skips the report-bundle self-check to avoid depending on a previous bundle. The normal `scripts\check-go-live-readiness.ps1` dry-run still validates the finished report bundle, and `scripts\check-production-readiness-report-bundle.ps1` remains the authoritative bundle-shape validator after generation. `scripts\check-production-readiness-report-bundle-clean-smoke.ps1` separately proves the bundle can be generated and validated from an empty temporary directory, then validated by the top-level go-live dry-run, without relying on old ignored artifacts. The local package draft exporter creates an ignored evidence-package draft with local report/helper references marked ready, appends a supporting-evidence snapshot from the bundle, and leaves deployment-specific rows blocked. The bundle validator confirms every expected report and helper exists, has parseable non-failed shape where applicable, and does not contain sensitive assignment patterns. The action-plan exporter reads those report files and writes `production-readiness-action-plan.md` with owner rows, missing evidence keys, and next actions. The owner-handoff exporter writes `production-readiness-owner-handoff.md` with those rows grouped by owner for deployment follow-up. The environment-template exporter writes an ignored helper script with placeholder assignments from the dedicated readiness reports, not the aggregate go-live row, so filled copies can stay owner-scoped; filled copies must stay outside git and secret-like values must come from secure deployment configuration or the current process environment. The individual report commands remain useful when one evidence area needs to be rerun or debugged.

The central go-live report includes separate MinIO and Azure Blob readiness rows. The dedicated MinIO and Azure reports remain useful as non-secret attachment references for the object-storage evidence rows.

The dedicated production-like staging report is a non-secret attachment reference for the staging rehearsal row. It does not replace the real build/test, migration, rollback, backup/restore, monitoring, provider, mobile, e-invoice, or owner sign-off records.

The dedicated local backup report verifies the local backup manifest, PostgreSQL dump integrity, MinIO mirror presence, private local configuration group presence, and Docker container inventory without printing private backup contents. It supports the staging backup/restore evidence row, but it does not replace a real restore rehearsal, production backup policy, monitoring owner, or deployment owner approval.

The dedicated local PostgreSQL restore report restores the daily PostgreSQL dump into an isolated temporary database with `pg_restore --no-owner`, verifies application schema/table presence and EF migration history table presence, and removes the temporary database. It is local restore rehearsal evidence only; production restore approval still needs the deployment backup policy, offsite restore evidence, monitoring owner, and owner sign-off.

The dedicated local release-candidate report runs focused .NET build/test lanes for WebAdmin, WebApi, Worker, Contracts compatibility, and Mobile Shared compatibility. It does not replace Web storefront npm build evidence, signed mobile artifacts, production-like route smoke, or owner approval; those remain covered by the Web/Mobile readiness and mobile launch evidence rows.

The dedicated evidence-package validator smoke report proves the template and validator contract remains internally consistent, including required rows such as owner handoff. It is tooling evidence only; it does not replace a filled deployment evidence package or owner approvals.

The dedicated e-invoice report is a non-secret attachment reference for accounting or tax evidence. It does not replace the real ZUGFeRD/Factur-X and XRechnung artifact, validation-report, storage/download-smoke, and sign-off records.

The dedicated Android report is a non-secret attachment reference for Android-first launch evidence. It first verifies checked-in Android project metadata and guards, then validates deployment-owner Android launch confirmations. It does not replace the signed artifact, push/maps configuration smoke, physical device/camera smoke, route compatibility, or owner approval records.

The dedicated Web/Mobile report is a non-secret attachment reference for Web storefront toolchain/runtime readiness, deterministic mobile resource-name checks, and checked-in Android project metadata guards. It does not replace the real storefront build, route smoke, signed mobile package, device smoke, or owner approval records.

The dedicated provider report is a non-secret attachment reference for Stripe, DHL, Brevo, and VIES evidence. It does not replace approved live execution, controlled provider smoke results, monitoring evidence, callback processing evidence, or operational playbook approvals.

## Current Outcome

The execution plan is local and target-neutral. A local production-like evidence working copy was generated on 2026-06-18 under the ignored `artifacts\production-readiness\` path, with a non-secret summary covering the current branch, restored local PostgreSQL backup, build, focused test lanes, mobile resource-name and Android project preflights, and web toolchain preflight. That working copy is not committed because deployment evidence must remain outside source control.

Real completion still requires deployment-specific evidence from the selected staging and production environments. The generated action plan can assign those blockers to owners, but it cannot approve them. The remaining blockers are external owner evidence for production-like staging sign-off, selected-provider MinIO controls and smoke, Azure readiness when in scope, dual-format e-invoice validation and accounting/tax sign-off, Android signed artifact and device/provider smoke, Stripe, DHL, Brevo, VIES, monitoring, rollback, and final owner approvals.
