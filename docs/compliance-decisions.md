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

Future async retry workflow:

- Store `Unknown` VIES outcomes with provider source, message, timestamp, customer id, and attempted VAT id.
- Add a bounded retry worker or scheduled operation that only retries provider-failure outcomes, not operator decisions.
- Use exponential backoff with a maximum attempt count and clear terminal manual-review state.
- Keep operator override higher priority than automatic retry results.
- Surface stale `Unknown` results in `Billing/TaxCompliance` without turning the dashboard into a diagnostics workspace.

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
- CSV invoice export exists.
- Retention and purge metadata exists.

These outputs are useful operational artifacts, but they are not full e-invoice compliance.

Near-term implementation design:

- Choose library/tooling for ZUGFeRD/Factur-X XML generation, PDF/A-3 embedding, and validation.
- Map immutable issued invoice snapshots to a structured invoice model.
- Validate structured data before exposing the artifact.
- Generate a downloadable PDF/A-3 artifact with embedded XML where required.
- Add WebAdmin download action and tests.
- Add XRechnung export after the primary artifact path is stable.

Open decision:

- The ZUGFeRD/Factur-X library/tooling decision is still open.

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

Rules:

- Do not claim production immutable archive storage until a provider is selected, implemented, configured, and smoke-tested.
- Keep the internal/database provider as development/internal fallback.
- Do not expose raw storage paths to UI.
- Preserve audit correlation, artifact format, content hash, and retention metadata.
