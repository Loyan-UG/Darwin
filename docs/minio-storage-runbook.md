# Local MinIO Object Storage Runbook

This runbook describes the optional local MinIO smoke path for Darwin object storage. It is for development and validation only. Production credentials, access keys, secret keys, and bucket policy values must come from secure configuration and must never be committed.

MinIO is the recommended self-hosted production target for Darwin invoice archive storage through the generic S3-compatible provider. AWS S3 and Azure Blob Storage remain supported alternatives. Local MinIO smoke is not a replacement for production validation with the real bucket, retention policy, Object Lock, legal hold, backup, restore, and monitoring setup.

## Start Local MinIO

Copy `.env.example` to `.env` if needed and set local-only MinIO values:

```powershell
docker compose -f docker-compose.minio.yml up -d
```

Default local endpoints:

- MinIO API: `http://localhost:9000`
- MinIO Console: `http://localhost:9001`
- Default smoke bucket: `darwin-invoice-archive-smoke`

The compose init container creates the smoke bucket with Object Lock enabled from the start and enables versioning. Object Lock must be enabled when a bucket is created; it cannot be added later to an existing bucket.

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

Latest local result:

- Date: 2026-05-12
- Command: `dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Minio"`
- Result: `3` passed, `0` skipped, `3` total
- Bucket validation: bucket existed, versioning was enabled, and default Object Lock retention reported `COMPLIANCE` for `1DAYS`.
- Note: smoke objects remain in the local bucket until retention permits deletion. This is expected for the local Object Lock setup and is not a production retention-policy validation.

## WebAdmin Local Smoke Action

WebAdmin Site Settings includes an object-storage smoke action that writes a small test object, reads it back, verifies the hash and metadata, then attempts cleanup. When the local MinIO bucket has default COMPLIANCE retention, the smoke action can report success with cleanup blocked; that is expected because provider-level retention prevents deletion.

To validate the WebAdmin action manually against local MinIO:

1. Start MinIO with `docker compose -f docker-compose.minio.yml up -d`.
2. Configure WebAdmin through environment variables or user-secrets with `ObjectStorage:Provider=S3Compatible`, the local endpoint, path-style access, the smoke bucket, Object Lock required, and `ObjectLockValidationMode=FailFast`.
3. Supply access key and secret key only through secure local configuration.
4. Open WebAdmin Site Settings, confirm object-storage status shows `S3Compatible`, configured container/profile status, and no provider credentials.
5. Run the object-storage smoke action. A retained local bucket may show the cleanup-blocked success warning.
6. Treat this only as a local provider-path smoke. Production still needs the real MinIO deployment validation checklist below.

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

Application-level overwrite protection is not the same as provider-level immutable retention. Production immutability can only be claimed after the selected provider bucket/container enforces retention, Object Lock or legal hold, and that behavior has been validated.
