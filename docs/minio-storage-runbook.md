# Local MinIO Object Storage Runbook

Reviewed: 2026-06-17

This runbook describes the optional local MinIO smoke path for Darwin object storage. It is for development and validation only. Production credentials, access keys, secret keys, and bucket policy values must come from secure configuration and must never be committed.

MinIO is the recommended self-hosted production target for Darwin invoice archive storage through the generic S3-compatible provider. AWS S3 and Azure Blob Storage remain supported alternatives. Local MinIO smoke is not a replacement for production validation with the real bucket, retention policy, Object Lock, legal hold, backup, restore, and monitoring setup.

## Start Local MinIO

Copy `.env.example` to `.env` if needed and set local-only MinIO values:

```powershell
docker compose -f docker-compose.minio.yml up -d
docker compose -f docker-compose.minio.yml ps
docker compose -f docker-compose.minio.yml logs --tail=100
```

Default local endpoints:

- MinIO API: `http://localhost:9000`
- MinIO Console: `http://localhost:9001`
- Default smoke bucket: `darwin-invoice-archive-smoke`

The compose init container creates the smoke bucket with Object Lock enabled from the start and enables versioning. Object Lock must be enabled when a bucket is created; it cannot be added later to an existing bucket.

If the bucket must be recreated manually, delete the local development bucket only after confirming it contains no needed smoke artifacts, then create it with Object Lock enabled at creation time. Use local development credentials only:

```powershell
docker compose -f docker-compose.minio.yml exec minio mc alias set local http://localhost:9000 $env:DARWIN_MINIO_ACCESS_KEY $env:DARWIN_MINIO_SECRET_KEY
docker compose -f docker-compose.minio.yml exec minio mc mb --with-lock local/darwin-invoice-archive-smoke
docker compose -f docker-compose.minio.yml exec minio mc version enable local/darwin-invoice-archive-smoke
docker compose -f docker-compose.minio.yml exec minio mc retention set --default COMPLIANCE 1d local/darwin-invoice-archive-smoke
```

Do not run destructive bucket commands against production from this local runbook.

## Configure Darwin For Local Smoke

Use environment variables or user-secrets. Do not put real production credentials into source-controlled `appsettings` files.

```powershell
$env:DARWIN_RUN_MINIO_SMOKE = "true"
$env:DARWIN_MINIO_ENDPOINT = "http://localhost:9000"
$env:DARWIN_MINIO_REGION = "us-east-1"
$env:DARWIN_MINIO_ACCESS_KEY = "darwin-local-minio"
$env:DARWIN_MINIO_SECRET_KEY = "change-this-local-minio-password"
$env:DARWIN_MINIO_BUCKET = "darwin-invoice-archive-smoke"
```

Equivalent Darwin object-storage configuration shape:

```json
{
  "ObjectStorage": {
    "Provider": "S3Compatible",
    "S3Compatible": {
      "Endpoint": "http://localhost:9000",
      "Region": "us-east-1",
      "BucketName": "darwin-invoice-archive-smoke",
      "UseSsl": false,
      "UsePathStyle": true,
      "ForcePathStyle": true,
      "RequireObjectLock": true,
      "DefaultRetentionMode": "Compliance",
      "LegalHoldEnabled": true,
      "ObjectLockValidationMode": "FailFast"
    }
  }
}
```

Supply `AccessKey` and `SecretKey` only through environment variables, user-secrets, or the deployment secret store.

## Run Optional Smoke Tests

The MinIO smoke tests are intentionally skipped unless `DARWIN_RUN_MINIO_SMOKE=true` is set.

```powershell
$env:DARWIN_RUN_MINIO_SMOKE = "true"
dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio"
```

The smoke test writes a disposable object, verifies metadata and SHA-256, reads it back, checks temporary URL support, verifies overwrite rejection with `ObjectOverwritePolicy.Disallow`, and confirms that retained objects cannot be deleted without an explicit locked-delete override.

Interpretation:

- `3` passed means the local S3-compatible provider path reached MinIO, wrote/read objects, verified hash/metadata behavior, and exercised configured overwrite/delete boundaries.
- A skipped run means `DARWIN_RUN_MINIO_SMOKE=true` or required `DARWIN_MINIO_*` variables are missing; that is expected for normal CI and machines without local MinIO.
- Cleanup warnings for retained smoke objects are expected when default COMPLIANCE retention is active.
- Passing local smoke does not prove the production bucket, TLS, credentials, retention policy, legal hold, backup, restore, or monitoring setup.

Latest local result:

- Date: 2026-05-12
- Command: `dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio"`
- Result: `3` passed, `0` skipped, `3` total
- Bucket validation: bucket existed, versioning was enabled, and default Object Lock retention reported `COMPLIANCE` for `1DAYS`.
- Note: smoke objects remain in the local bucket until retention permits deletion. This is expected for the local Object Lock setup and is not a production retention-policy validation.

Latest local recheck:

- Date: 2026-05-26
- Docker validation: `darwin-minio` was healthy; `mc version info` reported versioning enabled; `mc retention info --default` reported Object Lock `COMPLIANCE` for `1DAYS`.
- Test command: `dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio" --no-restore /p:UseSharedCompilation=false`
- Test result: `3` passed, `0` skipped, `3` total.
- Provider smoke command: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute -SmokeRetention`
- Provider smoke result: save/read/metadata/temporary-url checks completed against the local S3-compatible provider path; cleanup was skipped because retention smoke was enabled.
- Local configuration: WebApi, WebAdmin, and Worker User Secrets route `InvoiceArchive` and `ShipmentLabels` profiles to the local MinIO bucket through a dedicated bucket-scoped app user. These secrets are machine-local and must not be copied to repository files.

Latest local recheck:

- Date: 2026-05-27
- Docker validation: `darwin-minio` was healthy; init logs showed the smoke bucket created with Object Lock, versioning enabled, and default `COMPLIANCE` retention for `1DAYS`.
- Test command: `dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio" --no-restore /p:UseSharedCompilation=false`
- Test result: `3` passed, `0` skipped, `3` total.
- Provider smoke command: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute -SmokeRetention`
- Provider smoke result: save/read/metadata checks completed through the S3-compatible provider path; temporary URL generation was available; cleanup was skipped because retention smoke was enabled.
- Guarded smoke behavior: local loopback MinIO smoke can execute without production confirmation; production-like endpoints are blocked unless `DARWIN_OBJECT_STORAGE_PRODUCTION_SMOKE_CONFIRMED=true` or `-AllowProductionEndpoint` is supplied.
- Production readiness preflight result: blocked until the real production endpoint, bucket, TLS, dedicated least-privilege keys, Object Lock, versioning, retention, legal-hold policy, backup, restore, monitoring, alerting, and all required Darwin object-storage profile confirmations are supplied. This includes `InvoiceArchive`, `ShipmentLabels`, `MediaAssets`, `FinanceExports`, `FinanceExportOutbound`, `PersonnelDocuments`, and `PayrollPayslips`. This is expected and prevents claiming production immutability from local smoke alone.

## WebAdmin Local Smoke Action

WebAdmin Site Settings includes an object-storage smoke action that writes a small test object, reads it back, verifies the hash and metadata, then attempts cleanup. When the local MinIO bucket has default COMPLIANCE retention, the smoke action can report success with cleanup blocked; that is expected because provider-level retention prevents deletion.

To validate the WebAdmin action manually against local MinIO:

1. Start MinIO with `docker compose -f docker-compose.minio.yml up -d`.
2. Configure WebAdmin through environment variables or user-secrets with `ObjectStorage:Provider=S3Compatible`, the local endpoint, path-style access, the smoke bucket, Object Lock required, and `ObjectLockValidationMode=FailFast`.
3. Supply access key and secret key only through secure local configuration.
4. Open WebAdmin Site Settings, confirm object-storage status shows `S3Compatible`, configured container/profile status, and no provider credentials.
5. Run the object-storage smoke action. A retained local bucket may show the cleanup-blocked success warning.
6. Treat this only as a local provider-path smoke. Production still needs the real MinIO deployment validation checklist below.

## Configure App Profiles With User Secrets

For local or staging validation, configure WebApi, WebAdmin, and Worker through `.NET User Secrets` or deployment secrets. Do not copy these values into `appsettings` files.

Use the same provider profile names across the processes that need object storage:

- `InvoiceArchive` for invoice archive and e-invoice artifacts.
- `ShipmentLabels` for DHL or carrier label artifacts.
- `MediaAssets` for CMS/media uploads when external object storage is selected.
- `FinanceExports` for generated canonical finance export package source files.
- `FinanceExportOutbound` for outbound file-delivery connector targets.
- `PersonnelDocuments` for internal HR personnel-file binaries linked through `DocumentRecord`.
- `PayrollPayslips` for internal HR payslip artifacts generated from approved payroll run snapshots.

Example local/staging command shape:

```powershell
$projects = @(
  "src\Darwin.WebApi\Darwin.WebApi.csproj",
  "src\Darwin.WebAdmin\Darwin.WebAdmin.csproj",
  "src\Darwin.Worker\Darwin.Worker.csproj"
)

foreach ($project in $projects) {
  dotnet user-secrets --project $project set "ObjectStorage:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:Endpoint" "https://object-storage.example.com"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:Region" "us-east-1"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:BucketName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:UseSsl" "true"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:UsePathStyle" "true"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:ForcePathStyle" "true"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:RequireObjectLock" "true"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:DefaultRetentionMode" "Compliance"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:LegalHoldEnabled" "true"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:ObjectLockValidationMode" "FailFast"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:AccessKey" "<set-in-secret-store>"
  dotnet user-secrets --project $project set "ObjectStorage:S3Compatible:SecretKey" "<set-in-secret-store>"

  dotnet user-secrets --project $project set "ObjectStorage:Profiles:InvoiceArchive:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:InvoiceArchive:ContainerName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:InvoiceArchive:Prefix" "invoices"

  dotnet user-secrets --project $project set "ObjectStorage:Profiles:ShipmentLabels:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:ShipmentLabels:ContainerName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:ShipmentLabels:Prefix" "shipment-labels"

  dotnet user-secrets --project $project set "ObjectStorage:Profiles:MediaAssets:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:MediaAssets:ContainerName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:MediaAssets:Prefix" "media"

  dotnet user-secrets --project $project set "ObjectStorage:Profiles:FinanceExports:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:FinanceExports:ContainerName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:FinanceExports:Prefix" "finance-exports/packages"

  dotnet user-secrets --project $project set "ObjectStorage:Profiles:FinanceExportOutbound:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:FinanceExportOutbound:ContainerName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:FinanceExportOutbound:Prefix" "finance-exports/outbound"

  dotnet user-secrets --project $project set "ObjectStorage:Profiles:PersonnelDocuments:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:PersonnelDocuments:ContainerName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:PersonnelDocuments:Prefix" "hr/personnel-documents"

  dotnet user-secrets --project $project set "ObjectStorage:Profiles:PayrollPayslips:Provider" "S3Compatible"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:PayrollPayslips:ContainerName" "darwin-invoice-archive"
  dotnet user-secrets --project $project set "ObjectStorage:Profiles:PayrollPayslips:Prefix" "hr/payroll-payslips"
}
```

Replace placeholder values through the secret store used by the target environment. For production, prefer a deployment vault and least-privilege credentials over developer user-secrets. `FinanceExports` is the stored package source; `FinanceExportOutbound` is the delivery destination; `PersonnelDocuments` is the HR personnel-file binary store; `PayrollPayslips` is the HR payroll artifact store. Do not replace these profiles with batch metadata, employee metadata, payroll run metadata, document metadata, external-reference metadata, notes, or package content.

## Manual Validation Checklist

Before using MinIO for production invoice archive traffic:

- Run MinIO outside the Darwin application process.
- Use TLS and a reverse proxy or network path approved for the deployment.
- Use dedicated Darwin access keys, not root credentials.
- Keep MinIO storage on dedicated volumes; do not share the same disk as the database for serious production deployments.
- Create the invoice archive bucket with Object Lock enabled from the start.
- Enable versioning.
- Configure default retention after legal retention policy is confirmed.
- Use COMPLIANCE mode for invoice archive only when the legal retention policy is confirmed.
- Test backup, restore, and offsite copy before go-live.
- Monitor disk usage, failed writes, and retention-related failures.
- Run Darwin WebAdmin object-storage smoke against a disposable profile before routing invoice archive traffic to the production bucket.

Run the production readiness preflight after the operator has confirmed the final deployment settings:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-minio-production-readiness.ps1
```

The preflight checks only non-secret confirmations. It does not accept MinIO access keys, secret keys, bucket policy JSON, object payloads, object keys, or provider responses. A passing preflight means the deployment checklist is complete enough to run the selected-provider smoke and WebAdmin smoke against the production bucket; it is not a production immutability claim by itself.

The preflight requires separate confirmation that:

- `InvoiceArchive`, `ShipmentLabels`, `MediaAssets`, `FinanceExports`, `FinanceExportOutbound`, `PersonnelDocuments`, and `PayrollPayslips` profiles are configured or deliberately decided for the deployment.
- The disposable production smoke prefix is approved before any production-like write is executed.
- Retention/delete behavior is understood before retained smoke objects are written.
- The operator runbook, ownership, and escalation path are available outside source control.

After the preflight is complete and a disposable production smoke prefix is approved, run the selected-provider smoke with an explicit production confirmation:

```powershell
$env:DARWIN_OBJECT_STORAGE_PRODUCTION_SMOKE_CONFIRMED = "true"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\smoke-object-storage.ps1 -Execute
```

For production-like endpoints, `scripts\smoke-object-storage.ps1 -Execute` is intentionally blocked unless `DARWIN_OBJECT_STORAGE_PRODUCTION_SMOKE_CONFIRMED=true` is present or `-AllowProductionEndpoint` is passed. Use this only after the target bucket, profile, retention policy, and cleanup behavior are approved for a disposable smoke object. The smoke confirms Darwin can save, read, inspect metadata, optionally create a temporary URL, and delete or intentionally retain a smoke object through the configured provider path; it still does not replace Object Lock, legal-hold, backup, restore, monitoring, or operator sign-off.

Application-level overwrite protection is not the same as provider-level immutable retention. Production immutability can only be claimed after the selected provider bucket/container enforces retention, Object Lock or legal hold, and that behavior has been validated.

## Production Evidence Package Entries

For each production or production-like deployment, record the following non-secret entries in the deployment evidence package defined by [production-readiness-evidence-package.md](production-readiness-evidence-package.md):

- selected provider family and approved profile decisions for `InvoiceArchive`, `ShipmentLabels`, `MediaAssets`, `FinanceExports`, `FinanceExportOutbound`, `PersonnelDocuments`, and `PayrollPayslips`;
- preflight command date, operator, and result;
- selected-provider smoke command date, operator, disposable prefix, and result;
- retention mode, legal-hold behavior, Object Lock or provider-equivalent validation, and retention-policy owner;
- backup, restore, monitoring, failed-write alert, disk/capacity alert, and escalation owner;
- confirmation that access keys, secret keys, bucket policies, raw provider responses, object payloads, private documents, payroll contents, and customer data are not copied into source control or documentation.

Do not use local MinIO smoke as evidence for production immutability. Local smoke proves Darwin's provider path and retained-object behavior only for the local development bucket.
