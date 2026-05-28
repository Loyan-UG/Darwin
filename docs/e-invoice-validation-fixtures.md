# E-Invoice Validation Fixtures

This document defines how Darwin treats e-invoice validation fixtures. It is a fixture and evidence policy, not a compliance statement.

Darwin currently has parser-oriented validation-report fixtures under `tests/Darwin.Infrastructure.Tests/Fixtures`. These fixtures exercise `ExternalCommandEInvoiceGenerationService` behavior for accepted, rejected, malformed, nested, and missing validation-report shapes. They prove adapter behavior only. They are not legal evidence that a generated ZUGFeRD/Factur-X or XRechnung artifact is compliant.

## Fixture Classes

| Class | Location | Purpose | Compliance meaning |
| --- | --- | --- | --- |
| Parser fixtures | `tests/Darwin.Infrastructure.Tests/Fixtures/einvoice-validation-report-*.json` | Verify supported validation-report shapes, failure extraction, and `RequireValidationReport` behavior. | No compliance claim. |
| Smoke fixtures | Runtime temp files produced by `scripts/smoke-einvoice-external-command.ps1` | Verify the configured command can be executed through Darwin and returns the expected artifact shape. | No compliance claim. |
| Legal-approved fixtures | Not committed yet | Future deterministic invoices and validation reports approved by the project owner/legal/accounting reviewer. | Required before exposing artifacts as compliant. |

## Current Parser Fixture Coverage

Positive report fixtures:

- `einvoice-validation-report-valid-alt-key.json`
- `einvoice-validation-report-valid-number-flag.json`
- `einvoice-validation-report-valid-string-boolean.json`

Negative or ambiguous report fixtures:

- `einvoice-validation-report-failed-array-issues.json`
- `einvoice-validation-report-failed-bool-false.json`
- `einvoice-validation-report-failed-mixed-error-fields.json`
- `einvoice-validation-report-failed-nested-containers.json`
- `einvoice-validation-report-failed-nested-status.json`
- `einvoice-validation-report-failed-non-string-issues.json`
- `einvoice-validation-report-failed-numeric-flag.json`
- `einvoice-validation-report-failed-object-issues.json`
- `einvoice-validation-report-failed-root-status-uppercase.json`
- `einvoice-validation-report-failed-selected-tool-result.json`
- `einvoice-validation-report-failed-string-message.json`
- `einvoice-validation-report-no-recognized-keys.json`

## Production Acceptance Gates

Before Darwin can expose generated e-invoice artifacts as compliant:

- A pinned generator or validator artifact must be approved for the deployment.
- Production configuration must set `Compliance:EInvoice:ExternalCommand:RequireValidationReport=true`.
- Deterministic invoice fixtures must be reviewed and approved for the target legal/business scenarios.
- The generated ZUGFeRD/Factur-X PDF must satisfy the selected PDF/A-3 and embedded XML requirements.
- The selected validation report must contain a recognized positive result and be retained as evidence.
- The generated artifact must be stored through the `InvoiceArchive` object-storage profile with hash and retention metadata.
- Operator/legal sign-off must be recorded outside source control.

## Rules

- Do not rename parser fixtures without updating the source-contract guard.
- Do not put real customer names, real production invoice data, secrets, or tenant domains into fixtures.
- Do not label parser fixtures as legal-approved fixtures.
- Do not claim compliance from JSON, HTML, CSV, source-model JSON, or adapter smoke output.
