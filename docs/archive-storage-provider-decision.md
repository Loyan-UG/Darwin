# Invoice Archive Object Storage Provider Decision

Reviewed: 2026-05-10

Production provider direction is now selected: Darwin should use a reusable object-storage architecture with MinIO as the recommended self-hosted production target through an S3-compatible provider. AWS S3 and Azure Blob Storage remain supported alternatives. The current implementation keeps the internal/database-backed provider active, adds a file-system provider for non-vendor local/shared-volume scenarios, and adds a provider-neutral `ObjectStorage` configuration/capability boundary plus SDK-backed S3-compatible and Azure Blob adapters so future invoice archive, media, DHL label, export, and e-invoice artifacts can use the same storage model.

Invoice archive provider selection remains configuration-driven through `InvoiceArchiveStorage:ProviderName` while the reusable object-storage provider selection is configured under `ObjectStorage:Provider`. The file-system invoice archive provider uses `InvoiceArchiveStorage:FileSystem:RootPath`. External object-storage adapters must be wired behind the generic object-storage boundary and then consumed by invoice archive policy code without exposing provider SDK concepts to Application or Domain. S3-compatible and Azure Blob adapters are implemented in Infrastructure; provider-level immutability is still deployment-validation work, not a source-code-only claim.

Named `ObjectStorage:Profiles` are the deployment boundary for reusable consumers such as invoice archive, CMS media, DHL labels, exports, and future e-invoice artifacts. Profile `Provider`, `ContainerName`, and `Prefix` values are validated at startup and applied by the object-storage router before the selected provider is called. This keeps container/key rules centralized and avoids duplicating provider-specific storage paths in WebAdmin, WebApi, or Worker code.

## Decision Criteria

Evaluate each candidate provider against these requirements before implementation:

- Immutable retention or legal hold support that cannot be bypassed by the application runtime identity.
- Versioning or write-once behavior for archive artifacts.
- Server-side encryption and customer-managed key support where required by the deployment.
- Region, data-residency, and backup/restore alignment with the production legal entity.
- Audit logs for create, read, retention-policy change, legal-hold change, and delete/purge attempts.
- Lifecycle policy support that can preserve metadata while preventing premature payload deletion.
- Private network or least-privilege access model for WebAdmin, WebApi, and Worker.
- SDK and operational support for .NET Worker execution without exposing raw storage paths to UI.
- Local/staging testability without weakening production retention behavior.
- Cost and operational ownership for long-retention invoice artifacts.

## Production Decision

- Recommended self-hosted production target: MinIO using the generic S3-compatible provider.
- API compatibility target: S3-compatible object storage.
- Supported alternatives: AWS S3 through the S3-compatible provider and Azure Blob Storage through an Azure provider.
- Development/internal fallback: internal/database-backed archive provider.
- Optional local/on-prem fallback: file-system provider, without legal immutability claims.

Production archive immutability is still not complete until the selected deployment storage is configured and validated with provider-level versioning, object lock, retention, and legal hold or the provider-specific equivalent.
For Azure Blob deployments, Darwin sends native blob immutability/legal-hold options when retained objects are written and can fail fast when the target container lacks an immutability policy. The storage account/container still must be provisioned with immutable storage with versioning and validated before production archive traffic.

## Implementation Requirements After Selection

- Keep provider-specific implementations behind the generic `IObjectStorageService` boundary, then consume them from invoice archive policy code.
- Keep the current database/internal provider as development and emergency fallback.
- Register each provider explicitly and fail fast when a selected provider is not registered.
- Store archive artifacts through the selected provider without exposing raw storage paths in WebAdmin.
- Persist content hash, generated timestamp, retention deadline, retention policy version, and audit correlation.
- Keep provider-specific configuration validation that fails fast when production archive storage is enabled without required settings.
- Keep named profile validation for container names and object-key prefixes so unsafe traversal-style paths fail before startup completes.
- Add target-environment smoke coverage for save, read, existence check, retention metadata, legal-hold behavior, and denied premature deletion.
- Update `docs/production-setup.md`, `docs/go-live-status.md`, `docs/module-audit.md`, and `BACKLOG.md` after the provider is selected.

## MinIO Production Requirements

- Run MinIO outside the Darwin application process with dedicated storage volumes.
- Use TLS and a dedicated least-privilege Darwin access key, not root credentials.
- Create the invoice archive bucket with Object Lock enabled from the start.
- Enable versioning before writing production invoice archive objects.
- If Darwin is allowed to create the bucket (`CreateBucketIfMissing=true`), keep `RequireObjectLock=true` and `ObjectLockValidationMode=FailFast` for invoice archive production so the S3-compatible provider requests Object Lock at creation time and validates versioning/Object Lock before immutable writes.
- Configure default retention; use COMPLIANCE mode for invoice archive if the legal retention policy is confirmed.
- Keep MinIO storage separate from the database disk for serious production deployments.
- Configure backup, replication, offsite restore, and disk-usage monitoring.
- Validate restore before go-live.

Darwin must connect to MinIO through the generic S3-compatible provider. The application must not contain MinIO-only archive logic.

## Explicit Non-Goals

- Do not claim production immutable archive storage for the internal/database fallback.
- Do not claim production immutable archive storage for the file-system provider unless the deployment storage layer independently enforces immutable retention/legal hold.
- Do not claim provider-level immutable retention until the configured MinIO, AWS S3, Azure Blob, or S3-compatible storage account is validated.
- Do not store provider credentials in git, docs, logs, or test output.
- Do not expose raw bucket/container/object paths to operators or customers.
