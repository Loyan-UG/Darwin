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
| 6 | Android-first launch evidence | Signed Android artifact, release channel, maps, push, Google sign-in when enabled, physical QR/device smoke, route compatibility, and passing mobile resource-name preflight. | Darwin technical owner with system admin owner |
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
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-production-like-staging-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-production-like-staging-readiness-report.ps1 -Force
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-web-toolchain-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-web-storefront-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-mobile-resource-names.ps1
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

The central go-live report includes separate MinIO and Azure Blob readiness rows. The dedicated MinIO and Azure reports remain useful as non-secret attachment references for the object-storage evidence rows.

The dedicated production-like staging report is a non-secret attachment reference for the staging rehearsal row. It does not replace the real build/test, migration, rollback, backup/restore, monitoring, provider, mobile, e-invoice, or owner sign-off records.

The dedicated e-invoice report is a non-secret attachment reference for accounting or tax evidence. It does not replace the real ZUGFeRD/Factur-X and XRechnung artifact, validation-report, storage/download-smoke, and sign-off records.

The dedicated Android report is a non-secret attachment reference for Android-first launch evidence. It does not replace the signed artifact, push/maps configuration smoke, physical device/camera smoke, route compatibility, or owner approval records.

The dedicated Web/Mobile report is a non-secret attachment reference for Web storefront toolchain/runtime readiness and deterministic mobile resource-name checks. It does not replace the real storefront build, route smoke, mobile package, device smoke, or owner approval records.

The dedicated provider report is a non-secret attachment reference for Stripe, DHL, Brevo, and VIES evidence. It does not replace approved live execution, controlled provider smoke results, monitoring evidence, callback processing evidence, or operational playbook approvals.

## Current Outcome

The execution plan is local and target-neutral. A local production-like evidence working copy was generated on 2026-06-18 under the ignored `artifacts\production-readiness\` path, with a non-secret summary covering the current branch, restored local PostgreSQL backup, build, focused test lanes, mobile resource-name preflight, and web toolchain preflight. That working copy is not committed because deployment evidence must remain outside source control.

Real completion still requires deployment-specific evidence from the selected staging and production environments. The remaining blockers are external owner evidence for production-like staging sign-off, selected-provider MinIO controls and smoke, Azure readiness when in scope, dual-format e-invoice validation and accounting/tax sign-off, Android signed artifact and device/provider smoke, Stripe, DHL, Brevo, VIES, monitoring, rollback, and final owner approvals.
