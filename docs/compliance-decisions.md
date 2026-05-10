# Darwin Compliance Decisions

Reviewed: 2026-05-10

This document records compliance-related decisions that affect implementation planning. It does not claim legal compliance by itself; production readiness still depends on provider smoke checks, deployment configuration, and legal review where required.

## VAT Validation / VIES

Phase 1 policy:

- Disabled VIES returns `Unknown` with operator-visible manual review.
- Unavailable, rate-limited, timeout, malformed response, and provider exceptions return `Unknown` with operator-visible source/message.
- Only clear provider responses may record `Valid` or `Invalid`.
- Customer, invoice, and operator workflows must not be blocked only because VIES is unavailable.
- Manual operator decisions remain auditable and must preserve source/message context.

Current async retry workflow:

- Store `Unknown` VIES outcomes with provider source, message, timestamp, customer id, and attempted VAT id.
- `RetryUnknownCustomerVatValidationBatchHandler` and the disabled-by-default `VatValidationRetryWorker` retry only provider-failure outcomes (`provider.unavailable`, `vies.disabled`, `vies.unavailable`), not operator decisions.
- Use the configured minimum retry age and batch size to keep retries bounded.
- Keep operator override higher priority than automatic retry results.
- Surface stale `Unknown` results in `Billing/TaxCompliance` without turning the dashboard into a diagnostics workspace.

Future hardening:

- Add explicit attempt counters and exponential backoff if VIES instability creates repeated retry churn in production.
- Add a terminal manual-review state only if operations need a maximum automatic-attempt policy beyond the current operator-visible `Unknown` queue.

## E-Invoice Direction

Primary target: ZUGFeRD/Factur-X downloadable invoice artifact.

Reason:

- SME users need a human-readable PDF.
- Structured invoice data must also be available for machine processing.
- ZUGFeRD/Factur-X supports the combined readable PDF plus embedded structured XML direction.

Secondary target: XRechnung export.

Current state:

- Issued invoice snapshot exists.
- JSON archive download exists.
- Printable HTML archive download exists.
- Structured invoice source-model JSON download exists and includes document, seller, buyer, business, line, tax-summary, and total sections.
- `IEInvoiceGenerationService` exists as the future compliant-artifact boundary, with a default `NotConfigured` implementation that does not create fake artifacts.
- `GenerateInvoiceEInvoiceArtifactHandler` owns the shared preconditions for future artifact generation and rejects missing source snapshots, unknown formats, invoice-id mismatches, and incomplete generated metadata.
- WebAdmin download routing is guarded and ready for generated artifacts, but no e-invoice compliance download button is exposed until a real generator and validation path are selected.
- CSV invoice export exists.
- Retention and purge metadata exists.

These outputs are useful operational artifacts, but they are not full e-invoice compliance. The structured source-model export is explicitly marked `NotZugferdFacturX` until a generator and validator produce a compliant artifact.

Near-term implementation design:

- Choose library/tooling for ZUGFeRD/Factur-X XML generation, PDF/A-3 embedding, and validation.
- Extend the existing immutable issued-snapshot to structured source-model mapping into the selected e-invoice format model.
- Replace the default `NotConfigured` e-invoice generator with a selected, validated generator implementation.
- Validate structured data before exposing the artifact.
- Generate a downloadable PDF/A-3 artifact with embedded XML where required.
- Add WebAdmin download action and tests.
- Add XRechnung export after the primary artifact path is stable.

Open decision:

- The ZUGFeRD/Factur-X library/tooling decision is still open.
- See `docs/e-invoice-tooling-decision.md` for the selection criteria and implementation requirements.

## Invoice Archive Storage

Current provider:

- Internal/database-backed `IInvoiceArchiveStorage` provider.

Production target:

- External object storage with immutable retention/legal hold.

Open deployment decision:

- Azure Blob immutable policy.
- AWS S3 Object Lock.
- MinIO or S3-compatible object lock if supported by the deployment.
- Data-center object storage with retention/legal hold support.

Selection checklist:

- See `docs/archive-storage-provider-decision.md`.

Rules:

- Do not claim production immutable archive storage until a provider is selected, implemented, configured, and smoke-tested.
- Keep the internal/database provider as development/internal fallback.
- Do not expose raw storage paths to UI.
- Preserve audit correlation, artifact format, content hash, and retention metadata.
