# E-Invoice Tooling Decision

Reviewed: 2026-05-27

Darwin does not yet implement full e-invoice compliance. Current invoice outputs are issued JSON snapshots, printable HTML archive output, structured invoice source-model JSON, and CSV export. The primary target for the next implementation slice is a downloadable ZUGFeRD/Factur-X invoice artifact. XRechnung remains a secondary export target.

The application now exposes `IEInvoiceGenerationService` as the provider-neutral generation boundary. The registered default is `NotConfiguredEInvoiceGenerationService`, which returns `NotConfigured` and does not produce fake compliant artifacts. `EInvoiceSourceReadinessValidator` verifies minimum issued-snapshot source fields before any future generator runs; this is a safety gate only and does not validate ZUGFeRD/Factur-X or XRechnung compliance.

Infrastructure also supports a disabled-by-default external command adapter behind `IEInvoiceGenerationService`. It is intended for a deployment-approved generator or validation tool after the tooling decision is made. The command receives `--input <issued-snapshot-json> --output <artifact-path> --format <zugferd-factur-x|xrechnung> --validation-profile <profile-name> --validation-report <path-to-json>` and must create a validated artifact file. The adapter rejects empty output, output larger than the configured `MaxArtifactBytes`, non-PDF ZUGFeRD/Factur-X output, and malformed XRechnung XML output before storage. It also parses the optional validation report when present and can fail generation on a negative legal-validation result. This adapter does not make Darwin compliant by itself; compliance still depends on the selected tool, its validation behavior, and production smoke.

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

## Selected Tooling Path

Selected first implementation path: `Mustangproject` CLI behind Darwin's `ExternalCommandEInvoiceGenerationService`.

Reasoning:

- The existing Darwin boundary already supports an out-of-process generator/validator through `--input`, `--output`, `--format`, and optional `--validation-profile`.
- `Mustangproject` is maintained as an e-invoice focused Java library/CLI/server and documents read, write, validate, and convert support for ZUGFeRD/Factur-X and XRechnung artifacts.
- Keeping the first slice out-of-process avoids adding provider/tooling SDK references to Domain or Application and keeps the implementation replaceable if a deployment later mandates another generator.
- The selected path now has a local pinned-tooling installer (`scripts/install-mustang-cli.ps1`) and wrapper (`scripts/mustang-einvoice-wrapper.cmd`). Local smoke passed again on 2026-05-27 for both XRechnung XML and ZUGFeRD/Factur-X PDF artifact generation through Darwin's external-command adapter, including the production-like `RequireValidationReport` path. Generated artifact storage is guarded through `IEInvoiceArtifactStorage`; tests verify the `InvoiceArchive` object-storage profile, SHA-256 hash, validation-profile metadata, compliance retention, retention horizon, and overwrite-disallow policy. Hosted WebAdmin smoke also verifies that generated artifacts are persisted through `IEInvoiceArtifactStorage` before file download responses are returned, and that invalid format requests do not call the generator. The selected path still requires a pinned artifact in production packaging, approved deterministic fixtures, legal validation evidence, production artifact download/storage smoke, and deployment sign-off before any generated artifact is exposed as compliant. Deterministic validation-report fixture parsing is covered by `ExternalCommandEInvoiceGenerationServiceTests` (including alternate boolean keys and failed issue-message extraction).

Alternatives retained for later:

- `ZUGFeRD-csharp` as a .NET-side XML/model candidate if it proves sufficient for the required profiles and PDF/A-3 embedding path.
- A PDF/A-3 capable PDF library plus a separate structured XML generator/validator.
- An external document-generation service with explicit data-processing and retention review.
- A deployment-specific accounting/e-invoice integration, if the production operator already mandates one.

## Current Candidate Notes

These notes are a shortlist for the next implementation slice, not an approved production decision.

- `ZUGFeRD-csharp`: current NuGet package metadata shows version `18.0.0` and targets .NET 8.0 plus .NET Standard 2.0. It remains a plausible .NET-side XML/model fallback, but Darwin still needs proof of the exact profile support, PDF/A-3 embedding path, validation behavior, license review, and generated sample acceptance before switching away from the selected external-command path. Reference: <https://www.nuget.org/packages/ZUGFeRD-csharp/>.
- `Mustangproject`: current project documentation describes a Java library/CLI/server that can read, write, validate, and convert ZUGFeRD/Factur-X and XRechnung artifacts. Its 2026 release line documents support for ZUGFeRD 2.4 / Factur-X 1.08 and XRechnung 3.0.x. Darwin selected it as the first external-command path, but production use still requires pinning the artifact, JVM/runtime packaging, command wrapper hardening, and deployment smoke. Reference: <https://www.mustangproject.org/>.
- `KoSIT XRechnung validator configuration`: relevant for the later XRechnung export path and for German CIUS validation evidence. It is not by itself a ZUGFeRD/Factur-X PDF generator.
- `FeRD ZUGFeRD/Factur-X 2.4 package`: this remains the reference specification and validation artifact source for the target format. Implementation work must align generated profile/version output with the active deployment requirement. Reference: <https://www.ferd-net.de/en/standards/zugferd/factur-x>.

The next implementation slice is a production-evidence package for the selected external-command path: pin the deployment artifact, record deterministic source-to-artifact fixtures, collect selected-tool validation reports, and run artifact storage/download smoke in the target deployment before any WebAdmin download is treated as compliant. Local adapter smoke is already green and confirms process execution plus artifact-shape checks only.

## Implementation Requirements After Selection

- Keep the default `NotConfigured` implementation for unconfigured deployments and enable the selected Mustangproject external-command path only through secure deployment configuration.
- If the selected tool is operated out-of-process, configure `Compliance:EInvoice:ExternalCommand` with an absolute executable path, bounded timeout, supported formats, and an approved working/temp directory.
- Set `RequireValidationReport=true` in production so a generated artifact is rejected when the selected tool does not write a recognized positive validation result.
- Use `scripts/smoke-einvoice-external-command.ps1` to verify the selected external command can be called through Darwin's adapter before wiring it to operator-facing flows. A successful smoke confirms process execution and artifact-shape checks only; it is not legal validation.
- Keep parser fixtures, smoke fixtures, and future legal-approved fixtures separated; see [docs/e-invoice-validation-fixtures.md](e-invoice-validation-fixtures.md).
- Use [docs/e-invoice-acceptance-checklist.md](e-invoice-acceptance-checklist.md) for German deployment approval, reviewer ownership, fixture scenarios, and evidence requirements.
- Reuse or extend `EInvoiceSourceReadinessValidator` so missing source fields fail before provider-specific generation.
- Extend the existing issued-snapshot to structured source-model mapping into the selected e-invoice model.
- Validate the structured XML before artifact exposure.
- Generate or attach the structured XML to a PDF/A-3 artifact where required by the selected format.
- Store the generated artifact through `IEInvoiceArtifactStorage`, which routes to the reusable object-storage `InvoiceArchive` profile when configured.
- Expose a WebAdmin download action only after generation and validation succeed.
- Add tests for mapping, validation failure, successful artifact generation, and download authorization.
- Keep XRechnung as a later export until the primary ZUGFeRD/Factur-X path is stable.

## Explicit Non-Goals

- Do not claim full e-invoice compliance from JSON, HTML, structured source-model JSON, or CSV outputs.
- Do not expose generated e-invoice artifacts without validation.
- Do not pick a library only because it can create a PDF; structured XML and validation are required.
- Do not send invoice data to an external service without a deployment-approved data-processing decision.
