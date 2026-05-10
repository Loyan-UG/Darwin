# E-Invoice Tooling Decision

Reviewed: 2026-05-10

Darwin does not yet implement full e-invoice compliance. Current invoice outputs are issued JSON snapshots, printable HTML archive output, structured invoice source-model JSON, and CSV export. The primary target for the next implementation slice is a downloadable ZUGFeRD/Factur-X invoice artifact. XRechnung remains a secondary export target.

The application now exposes `IEInvoiceGenerationService` as the provider-neutral generation boundary. The registered default is `NotConfiguredEInvoiceGenerationService`, which returns `NotConfigured` and does not produce fake compliant artifacts.

## Decision Criteria

Evaluate candidate libraries or tooling against these requirements:

- ZUGFeRD/Factur-X profile support appropriate for the target customer segment and jurisdiction.
- PDF/A-3 generation or embedding support for structured XML inside a human-readable PDF.
- Structured XML validation before download.
- Clear mapping from Darwin issued invoice snapshots and the current structured source-model export to the selected e-invoice model.
- Support for VAT, reverse charge, B2B/B2C distinctions, issuer/customer tax identities, payment terms, totals, and line-level tax data.
- Deterministic output suitable for repeatable tests.
- Long-term maintenance, licensing, and security posture acceptable for production.
- Server-side generation that does not require operator desktop tooling.
- Failure modes that keep the invoice in manual review instead of exposing invalid artifacts.
- Extensibility for a later XRechnung export path.

## Candidate Categories To Evaluate

No library or tooling is selected yet.

- A maintained .NET ZUGFeRD/Factur-X library.
- A PDF/A-3 capable PDF library plus a separate structured XML generator/validator.
- An external document-generation service with explicit data-processing and retention review.
- A deployment-specific accounting/e-invoice integration, if the production operator already mandates one.

## Implementation Requirements After Selection

- Replace the default `NotConfigured` implementation of `IEInvoiceGenerationService` with the selected provider/tooling implementation.
- Extend the existing issued-snapshot to structured source-model mapping into the selected e-invoice model.
- Validate the structured XML before artifact exposure.
- Generate or attach the structured XML to a PDF/A-3 artifact where required by the selected format.
- Store the generated artifact through `IInvoiceArchiveStorage` or a compatible archive artifact boundary.
- Expose a WebAdmin download action only after generation and validation succeed.
- Add tests for mapping, validation failure, successful artifact generation, and download authorization.
- Keep XRechnung as a later export until the primary ZUGFeRD/Factur-X path is stable.

## Explicit Non-Goals

- Do not claim full e-invoice compliance from JSON, HTML, structured source-model JSON, or CSV outputs.
- Do not expose generated e-invoice artifacts without validation.
- Do not pick a library only because it can create a PDF; structured XML and validation are required.
- Do not send invoice data to an external service without a deployment-approved data-processing decision.
