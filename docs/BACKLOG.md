# Darwin Documentation Backlog

Reviewed: 2026-05-12

This file tracks documentation-facing follow-up tasks for the next development slice. The active product and engineering roadmap remains in `../BACKLOG.md`; historical implementation notes remain in `implementation-ledger.md`.

## Next Development Slice

- Build the selected Mustangproject CLI ZUGFeRD/Factur-X generation path behind the existing `IEInvoiceGenerationService` boundary, including artifact validation-report parsing, storage through the invoice archive profile, and documentation for the pinned wrapper/tooling. Keep optional XRechnung export as a secondary item unless a deployment explicitly requires it earlier. Use `scripts\smoke-einvoice-external-command.ps1` for the adapter smoke after the deployment-approved wrapper is configured; the smoke is not a compliance substitute.
- Select and configure the production object-storage provider per deployment. MinIO remains the recommended self-hosted target through the S3-compatible provider; AWS S3 and Azure Blob remain supported alternatives. Document the selected endpoint/container/bucket, Object Lock or immutability policy, retention mode, legal-hold expectations, backup, restore, and monitoring checks outside source-controlled secrets.
- Keep hosted smoke tests for onboarding and inventory current after the 2026-05-12 expansion: the resumable admin-assisted onboarding wizard now has step/deep-link coverage, and the inventory positive mutation lane asserts stock quantities plus ledger rows for reserve, release, return receipt, transfer movement, and supplier receiving. Continue adding cases only when the underlying operator flows change.

## Documentation Guards

- Keep local MinIO smoke documented as optional development validation only; do not describe it as production Object Lock or legal-hold certification.
- Keep source-contract cleanup documented as complete only while the focused lanes continue to report zero skips.
- Keep provider smoke commands documented as opt-in and secret-free.

## Technical Debt Follow-Up

- Keep monitoring Android 16 16 KB page-size readiness for native dependencies during release builds. The latest local Android debug builds passed with `0` warnings after the .NET/MAUI workload repair and Android API guard cleanup.
- Keep WebAdmin security test analyzer warnings at zero after the `xUnit1051` cleanup by passing `TestContext.Current.CancellationToken` into new asynchronous helper calls.
