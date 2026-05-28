# E-Invoice Acceptance Checklist

Reviewed: 2026-05-27

This checklist defines the acceptance path before Darwin may expose generated e-invoice artifacts as compliant for a German deployment. It is not legal advice. A deployment owner, tax/accounting reviewer, and where needed legal/compliance reviewer must approve the evidence for the target customer.

## Current German Baseline

The current implementation target is based on the German B2B e-invoice rollout from 2025. The official BMF FAQ states that domestic businesses must be able to receive e-invoices from 1 January 2025, with transition rules for issuing e-invoices through 2026 and, for issuers with prior-year turnover up to EUR 800,000 or certain EDI cases, through 2027. It also states that an e-invoice must enable electronic processing and that EN 16931-compliant structured formats are the baseline.

Official references:

- BMF FAQ on mandatory e-invoicing from 1 January 2025: <https://www.bundesfinanzministerium.de/Content/DE/FAQ/e-rechnung.html>
- German federal e-invoice information page for B2B context and B2G separation: <https://e-rechnung-bund.de/e-rechnung/e-rechnung-zwischen-unternehmen-b2b/>
- Federal administration FAQ for XRechnung/B2G requirements and current-standard expectations: <https://e-rechnung-bund.de/en/faq/#xrechnung>

Implementation rules for Darwin:

- Primary target: ZUGFeRD/Factur-X with EN 16931 profile support.
- Secondary target: XRechnung export when a deployment requires it.
- ZUGFeRD/Factur-X `MINIMUM` and `BASIC-WL` profiles must not be accepted as German VAT-compliant e-invoice output.
- The structured XML part is authoritative for hybrid ZUGFeRD/Factur-X artifacts when it differs from the human-readable PDF representation.
- Validation is strongly recommended and required by Darwin production policy, even where validation alone is not the only legal condition.
- E-invoice structured content must contain the required VAT invoice data in the structured part; a reference to an unstructured attachment is not enough for required invoice fields.
- Retention must preserve the structured part unchanged and available for the legally required retention period.

## Roles And Approvals

| Approval area | Primary approver | Supporting approver | Evidence |
| --- | --- | --- | --- |
| Business scenario selection | Project owner or customer system owner | Accounting lead | Approved list of invoice scenarios. |
| VAT/tax correctness | Tax advisor or accounting lead | Project owner | Fixture invoice values, VAT/reverse-charge decisions, rounding review. |
| Technical format validation | Darwin technical owner | Customer technical admin | Validator reports, generated artifacts, smoke logs. |
| Legal/compliance interpretation | Customer legal/compliance reviewer when required | Tax advisor | Written sign-off outside source control. |
| Production storage and retention | Customer technical admin or DevOps owner | Darwin technical owner | Object-storage preflight, smoke result, backup/restore evidence. |
| Operational go-live | Project owner | Customer system owner | Final checklist sign-off and rollback plan. |

## Required Fixture Scenarios

Create deterministic invoice fixtures for the deployment before compliance sign-off. The exact set may vary by customer, but the minimum German B2B set should include:

- Domestic German B2B sale with standard VAT.
- Domestic German B2B sale with reduced VAT if the tenant sells reduced-rate items.
- VAT-exempt or reverse-charge case only when the tenant legally uses that scenario.
- EU B2B customer with VAT ID and documented reverse-charge decision if in scope.
- Invoice with multiple line items, discounts, shipping/fees if supported, and rounding-sensitive totals.
- Credit note or invoice correction if the tenant issues corrections through Darwin.
- Small-value or excluded invoice scenario only if the tenant relies on that exception.
- B2G/XRechnung scenario only if the tenant invoices public authorities.

Each fixture must include:

- Issued invoice snapshot.
- Generated artifact.
- Extracted structured XML.
- Validator report.
- Expected totals, taxes, dates, seller/buyer identifiers, and payment terms.
- Reviewer sign-off record outside source control.

Legal-approved fixtures are created only after the tax/accounting reviewer and, where required, legal/compliance reviewer accept the generated artifact, extracted structured XML, validation report, and business values for the target scenario. Parser fixtures and smoke fixtures must not be promoted to legal-approved fixtures without this review.

## Acceptance Steps

1. Confirm scope:
   - Customer is domestic B2B, B2C, B2G, mixed, or cross-border.
   - Determine whether ZUGFeRD/Factur-X, XRechnung, or both are required.
   - Determine whether transition rules allow non-e-invoice output for a limited period.

2. Pin tooling:
   - Select and pin the generator/validator artifact version.
   - Record checksum/hash outside source control.
   - Package Java/runtime dependencies if the selected tooling requires them.
   - Re-run smoke whenever tooling changes.

3. Configure Darwin:
   - Enable `Compliance:EInvoice:ExternalCommand`.
   - Set an absolute command path.
   - Set bounded timeout and max artifact size.
   - Set `RequireValidationReport=true`.
   - Configure the `InvoiceArchive` object-storage profile.

4. Generate fixture artifacts:
   - Generate ZUGFeRD/Factur-X for the approved fixture set.
   - Generate XRechnung only for approved B2G or XML-only scenarios.
   - Extract and review structured XML.
   - Confirm the PDF visual representation matches the structured XML where applicable.

5. Validate artifacts:
   - Run the selected validator.
   - Require a recognized positive validation report.
   - Fail the fixture if the report lacks a pass/fail result.
   - Fail the fixture if the artifact shape is wrong, the XML is malformed, or the PDF is not the expected PDF/A-3 carrier where required.

6. Review accounting and tax content:
   - Check seller legal data, VAT ID, tax number if used, address, invoice number, issue date, service date/period, currency, payment terms, line descriptions, tax categories, VAT rates, totals, rounding, reverse-charge notices, and correction references.
   - Record reviewer decisions outside source control.

7. Validate storage and delivery:
   - Store generated artifacts through the `InvoiceArchive` profile.
   - Verify SHA-256 hash, metadata, retention horizon, validation profile, and overwrite-disallow policy.
   - Verify download from WebAdmin and any exposed customer/member surface.
   - Confirm production object storage retention, backup, restore, and monitoring.

8. Approve release:
   - Confirm no UI labels JSON/HTML/source-model exports as compliant e-invoices.
   - Confirm generated artifacts are hidden or disabled until validation and sign-off are complete.
   - Record go-live sign-off, rollback plan, and support owner.

## Failure Handling

- If source data is incomplete, block generation and route the invoice to operator remediation.
- If the generator fails, keep the invoice issued state unchanged and show an operator-safe error.
- If validation fails, do not expose the artifact as compliant; retain the failed validation evidence outside customer-facing downloads.
- If storage fails, do not claim artifact availability and keep the archive/download surface in manual review.
- If legal/accounting review rejects a fixture, fix source mapping or tenant configuration before re-running the fixture set.

## Non-Goals

- This checklist does not approve a specific tax position.
- This checklist does not replace advice from a tax advisor or legal reviewer.
- Passing adapter smoke does not prove German e-invoice compliance.
- Parser fixtures in `tests/Darwin.Infrastructure.Tests/Fixtures` are not legal-approved fixtures.
