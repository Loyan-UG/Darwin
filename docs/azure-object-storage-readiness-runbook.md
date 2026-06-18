# Azure Blob Object Storage Readiness Runbook

Reviewed: 2026-06-18

This runbook prepares Azure Blob Storage as the next object-storage hardening target after MinIO production evidence is complete. It is deployment-neutral and must not contain tenant names, account names, connection strings, access keys, SAS tokens, private endpoints, customer data, object payloads, or provider responses.

MinIO remains the selected first production target. Azure Blob is a supported alternative and the next readiness target for deployments that are Azure-first or require managed storage operations.

## Scope

Azure readiness covers the same Darwin storage profiles as the generic object-storage boundary:

- `InvoiceArchive`
- `ShipmentLabels`
- `MediaAssets`
- `FinanceExports`
- `FinanceExportOutbound`
- `PersonnelDocuments`
- `PayrollPayslips`

Profile names, container names, and prefixes come from secure deployment configuration. They must not be copied from batch metadata, document metadata, external-reference metadata, notes, package content, payroll data, HR data, or provider payloads.

## Required Azure Controls

Before Azure Blob is used for production archive or evidence traffic:

- Storage account and containers are provisioned outside the Darwin application process.
- TLS is enforced.
- Access uses least-privilege credentials from a deployment vault or managed identity where supported.
- Blob versioning is enabled where retention or immutability is required.
- Immutable blob storage policies or legal hold are configured for compliance-sensitive profiles.
- Backup, restore, replication, monitoring, failed-write alerting, and capacity alerting are assigned to an owner.
- Disposable smoke prefix and cleanup or retention behavior are approved before production-like smoke.
- Darwin WebAdmin and Worker use the same profile decisions for shared artifacts.

## Configuration Shape

Use secure configuration or a deployment vault. Do not commit real values.

```json
{
  "ObjectStorage": {
    "Provider": "AzureBlob",
    "AzureBlob": {
      "ContainerName": "darwin-storage",
      "RequireImmutabilityPolicy": true,
      "ObjectLockValidationMode": "FailFast"
    },
    "Profiles": {
      "InvoiceArchive": {
        "Provider": "AzureBlob",
        "ContainerName": "darwin-storage",
        "Prefix": "invoices"
      },
      "FinanceExports": {
        "Provider": "AzureBlob",
        "ContainerName": "darwin-storage",
        "Prefix": "finance-exports/packages"
      },
      "FinanceExportOutbound": {
        "Provider": "AzureBlob",
        "ContainerName": "darwin-storage",
        "Prefix": "finance-exports/outbound"
      }
    }
  }
}
```

Connection strings, account keys, SAS tokens, client secrets, certificates, and managed-identity configuration stay in secure deployment configuration. They must not be stored in appsettings files, domain metadata, document metadata, external references, logs, tests, screenshots, or docs.

## Smoke Strategy

Run the non-secret readiness preflight before any smoke that writes to Azure:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-azure-object-storage-readiness.ps1
```

To create an ignored, non-secret report for the production readiness evidence package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\export-azure-object-storage-readiness-report.ps1 -Force
```

The report is written under `artifacts\production-readiness\` by default. It is valid as current-state evidence even when it is blocked; it does not replace real Azure policy validation, disposable-prefix smoke, or owner approval.

The preflight records only environment-level confirmations and refuses to print connection strings, account keys, SAS tokens, client secrets, private endpoint details, object keys, provider payloads, or policy JSON. Configure these non-secret confirmation variables in the operator shell or deployment runbook before running it:

| Variable | Purpose |
| --- | --- |
| `DARWIN_AZURE_BLOB_PRODUCTION_ENDPOINT` | Confirms the approved HTTPS Azure Blob endpoint for this deployment record. |
| `DARWIN_AZURE_BLOB_PRODUCTION_CONTAINER` | Confirms the approved container label for this deployment record. The value must be a valid Azure container name: 3 to 63 lowercase letters, numbers, and single hyphens, with an alphanumeric start and end. |
| `DARWIN_AZURE_BLOB_PROVIDER_SELECTED_CONFIRMED` | Confirms Azure Blob is intentionally selected for this deployment or readiness lane. |
| `DARWIN_AZURE_BLOB_TLS_CONFIRMED` | Confirms TLS is enforced. |
| `DARWIN_AZURE_BLOB_DEDICATED_IDENTITY_CONFIRMED` | Confirms access uses deployment-owned identity, vault, or managed identity controls. |
| `DARWIN_AZURE_BLOB_LEAST_PRIVILEGE_POLICY_CONFIRMED` | Confirms least-privilege access has been reviewed. |
| `DARWIN_AZURE_BLOB_VERSIONING_CONFIRMED` | Confirms blob versioning where required. |
| `DARWIN_AZURE_BLOB_IMMUTABILITY_POLICY_CONFIRMED` | Confirms immutable blob policy where required. |
| `DARWIN_AZURE_BLOB_LEGAL_HOLD_POLICY_CONFIRMED` | Confirms legal-hold policy where required. |
| `DARWIN_AZURE_BLOB_BACKUP_CONFIGURED_CONFIRMED` | Confirms backup or replication ownership. |
| `DARWIN_AZURE_BLOB_RESTORE_TEST_CONFIRMED` | Confirms restore test evidence exists. |
| `DARWIN_AZURE_BLOB_MONITORING_CONFIRMED` | Confirms monitoring ownership. |
| `DARWIN_AZURE_BLOB_ALERTING_CONFIRMED` | Confirms failed-write and capacity alert ownership. |
| `DARWIN_AZURE_BLOB_DARWIN_PROFILE_CONFIGURED_CONFIRMED` | Confirms Darwin runtime profile selection. |
| `DARWIN_AZURE_BLOB_INVOICE_ARCHIVE_PROFILE_CONFIRMED` | Confirms `InvoiceArchive` profile decision. |
| `DARWIN_AZURE_BLOB_SHIPMENT_LABELS_PROFILE_CONFIRMED` | Confirms `ShipmentLabels` profile decision. |
| `DARWIN_AZURE_BLOB_MEDIA_ASSETS_PROFILE_DECIDED_CONFIRMED` | Confirms `MediaAssets` profile decision. |
| `DARWIN_AZURE_BLOB_FINANCE_EXPORTS_PROFILE_CONFIRMED` | Confirms `FinanceExports` package-source profile decision. |
| `DARWIN_AZURE_BLOB_FINANCE_EXPORT_OUTBOUND_PROFILE_CONFIRMED` | Confirms `FinanceExportOutbound` delivery-destination profile decision. |
| `DARWIN_AZURE_BLOB_PERSONNEL_DOCUMENTS_PROFILE_CONFIRMED` | Confirms `PersonnelDocuments` profile decision. |
| `DARWIN_AZURE_BLOB_PAYROLL_PAYSLIPS_PROFILE_CONFIRMED` | Confirms `PayrollPayslips` profile decision. |
| `DARWIN_AZURE_BLOB_DISPOSABLE_SMOKE_PREFIX_CONFIRMED` | Confirms the disposable smoke prefix is approved. |
| `DARWIN_AZURE_BLOB_RETENTION_DELETE_BEHAVIOR_CONFIRMED` | Confirms delete or retention behavior for smoke artifacts. |
| `DARWIN_AZURE_BLOB_OPERATOR_RUNBOOK_CONFIRMED` | Confirms the operator runbook and evidence owner are assigned. |

Run local and staging validation only after the deployment owner confirms the target container and disposable prefix.

```powershell
$env:DARWIN_OBJECT_STORAGE_PRODUCTION_SMOKE_CONFIRMED = "true"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute
```

The smoke confirms Darwin can save, read, inspect metadata, optionally create temporary access, and delete or intentionally retain a disposable object through the configured Azure provider path. It does not prove legal compliance by itself.

## Acceptance

Azure Blob is production-ready for a Darwin deployment only when:

- the selected containers and profile prefixes are approved;
- immutability or legal-hold behavior is validated for compliance-sensitive profiles;
- backup, restore, monitoring, and alerting evidence is recorded;
- `scripts\check-azure-object-storage-readiness.ps1` passes in the deployment shell;
- disposable-prefix smoke passes;
- the production readiness evidence package contains non-secret references to the Azure preflight, smoke, retention/legal-hold evidence, and approval owner.

## Non-Goals

- This runbook does not replace MinIO as the first selected target.
- This runbook does not add schema, route, DTO, WebAdmin mutation, public/mobile/storefront contract, finance export format change, provider credential UI, bank API, AI provider, or accounting API adapter.
- This runbook does not allow credentials or raw Azure provider payloads in metadata or documentation.
