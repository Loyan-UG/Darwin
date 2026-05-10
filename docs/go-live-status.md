# Darwin Go-Live Status

Last reviewed: 2026-05-10

This status is code-backed. It intentionally distinguishes implemented plumbing from production-complete provider behavior.

External smoke input names and commands are centralized in `docs/external-smoke-inputs.md`; do not commit real provider values.

## WebAdmin Dashboard

Status: compact operational command center implemented.

- `HomeController` now avoids catalog, CMS, and identity list queries on the first dashboard paint.
- First paint keeps high-value summaries: CRM, orders, business support, communication readiness, mobile device health, and selected-business billing/inventory/loyalty summaries.
- `/Home/Index` renders KPI cards, a prioritized attention list capped at eight items, selected-business context, compact module summaries, and a short quick-link strip.
- Detailed diagnostics remain in their module workspaces: BusinessCommunications, Businesses support/readiness, Billing/TaxCompliance, MobileOperations, Orders, Inventory, and Loyalty.
- Existing fragment endpoints remain available: `Home/CommunicationOpsFragment` and `Home/BusinessSupportQueueFragment`.

## Stripe / Payments

Status: webhook boundary, internal reconciliation, and real Stripe Checkout Session creation path are implemented; test-mode handoff validation passed locally; hosted checkout payment and verified webhook validation remain pending.

Verified implementation:

- `StripeWebhooksController` validates `Stripe-Signature`, enforces bounded payload reads, extracts Stripe event id/type, and writes idempotent inbox entries.
- `ProcessStripeWebhookHandler` handles checkout session, payment intent, refund, invoice, and subscription events, logs Stripe event ids in `EventLog`, and updates matched payments/orders/invoices when provider references match.
- `CreateStorefrontPaymentIntentHandler` can now create a real Stripe Checkout Session through the WebApi `StripeCheckoutSessionClient` when `SiteSetting.StripeEnabled=true`, `StripeSecretKey` is configured, and return/cancel URLs are provided.
- Storefront return handling no longer captures or voids Stripe payments; verified Stripe webhooks remain the source of truth for captured/failed/cancelled provider state.
- WebAdmin billing views expose payments, refunds, disputes, webhook deliveries, failed/pending/unlinked queues, and dispute review actions.
- `scripts/smoke-stripe-testmode.ps1` provides a guarded local WebApi handoff, optional disposable public checkout order creation, optional return-route check, and optional webhook-finalization polling without accepting or printing Stripe secrets or provider references.
- Stripe test-mode handoff smoke passed locally on 2026-05-10 against an isolated WebApi build from the current source after test settings were entered: the script created a disposable public checkout smoke order, created a Stripe-hosted Checkout Session (`checkout.stripe.com`), and confirmed the storefront return route left the payment `Pending`.
- Focused WebApi Stripe webhook and provider-callback tests passed locally on 2026-05-10 with an isolated output path to avoid running-binary file locks (`191` passed, `0` skipped). This covers webhook ingress, signature/idempotency behavior, provider-callback processing, and callback worker boundaries; it does not replace the external Stripe hosted-checkout/webhook smoke.
- `scripts/check-go-live-readiness.ps1` includes the Stripe smoke prerequisite dry-run in the local go-live readiness summary without executing hosted checkout.

Gaps:

- Stripe test-mode hosted checkout still needs to be paid through Stripe test checkout and verified webhook events (`checkout.session.completed`, `payment_intent.succeeded`, failure/refund/dispute where applicable) must be confirmed before production traffic. Later, repeat a live-mode smoke before production traffic.
- Subscription checkout creation and refund/dispute operator actions still need a separate live-provider verification pass.
- Keep Stripe secrets only in configuration/environment; no committed secrets were found by `scripts/check-secrets.ps1` in the latest pass.

See `docs/module-audit.md` for the cross-module audit matrix and `docs/compliance-decisions.md` for compliance planning decisions.

## DHL / Shipping

Status: provider operation queue, callbacks, labels/tracking presentation, shared label storage, and a real DHL HTTP client path are wired; live-provider validation remains pending.

Verified implementation:

- `DhlWebhooksController` validates API key and HMAC signature, bounds payload size, and writes idempotent provider callback inbox entries.
- `ApplyDhlShipmentCreateOperationHandler` calls `IDhlShipmentProviderClient`, stores the DHL shipment reference/tracking number returned by the provider, persists returned PDF labels through shared media storage, and queues label generation only when the create response does not include a label.
- `ApplyDhlShipmentLabelOperationHandler` now requires an existing provider shipment reference and retrieves/saves the carrier label instead of creating local fake references or label URLs.
- `GenerateDhlShipmentLabelHandler` safely queues or retries label operations.
- WebAdmin has shipment queues, returns queue, and shipment provider operations visibility.
- `scripts/smoke-dhl-live.ps1` provides a guarded local prerequisite check and optional real DHL validation request without printing secrets or raw provider response payloads.
- `scripts/check-go-live-readiness.ps1` includes the DHL smoke prerequisite dry-run in the local go-live readiness summary without executing DHL calls.

Gaps:

- The DHL client still needs a live smoke against the target DHL account to confirm the exact base URL, authentication headers, shipment payload, label response shape, WebAdmin/Worker operation flow, and callback lifecycle.
- Returns/RMA flows are visible, but full carrier-integrated RMA automation remains a go-live task.

## Brevo / Communication

Status: production-capable provider plumbing is present when configured correctly.

Verified implementation:

- `BrevoEmailOptionsValidator` fails startup when `Email:Provider=Brevo` is active without required API key/sender/timeout configuration.
- `BrevoEmailSender` sends transactional email through Brevo, records audit state, supports sandbox mode, correlation idempotency, sanitized logging, and optional provider-managed `templateId` delivery mapped by Darwin `TemplateKey`/`FlowKey`.
- `BrevoWebhooksController` requires Basic Auth, bounds payloads, normalizes provider events, and writes idempotent callback inbox entries.
- BusinessCommunications remains the detailed workspace for retry chains, failed sends, provider review, and callback state.
- `scripts/smoke-brevo-readiness.ps1` provides a guarded direct Brevo sandbox/controlled-inbox readiness check without printing secrets or raw provider response payloads.
- `scripts/check-go-live-readiness.ps1` includes the Brevo smoke prerequisite dry-run in the local go-live readiness summary without executing Brevo calls.

Gaps:

- Template content is still owned in Darwin settings by default. Optional Brevo `templateId` mapping exists for provider-managed send-time delivery, but provider-side template authoring/synchronization remains a later operational workflow if needed.
- Production setup must verify sender domain, DKIM/DMARC, sandbox and controlled-inbox sends, webhook credentials, worker deployment, and sandbox disabled before live traffic.

## Tax / VAT / E-Invoice

Status: operator review exists, VAT/collection/reverse-charge follow-up is data-backed, missing customer VAT IDs can be corrected from the TaxCompliance workspace, VAT IDs can be checked through a VIES-backed provider path, final invoice-level reverse-charge decisions can be recorded with row-version concurrency checks, invoice review data can be exported as CSV for archive/accounting review, draft invoices now have a minimum issue-readiness guard, and CRM invoices capture an immutable issue snapshot with retention metadata when first opened/paid; downloadable JSON, printable HTML, and structured invoice source-model JSON artifacts are available from the CRM invoice editor for issued invoices. Expired invoice archive payloads can now be purged by the Worker with audit metadata retained on the invoice. Full e-invoicing compliance is not implemented. The primary planned downloadable e-invoice target is ZUGFeRD/Factur-X; XRechnung is a secondary export backlog item.

Verified implementation:

- WebAdmin `Billing/TaxCompliance` summarizes invoice/customer/business tax signals, links operators to relevant billing/CRM workspaces, and lets operators correct missing B2B customer company/VAT profile data with row-version concurrency checks.
- WebAdmin `Billing/TaxCompliance` now also surfaces B2B customers whose VAT ID exists but still needs validation review. Operators can mark the VAT ID as valid, invalid, or not applicable; the decision is stored on the customer with source, optional note, timestamp, and row-version concurrency protection.
- `ValidateCustomerVatIdHandler` can validate B2B customer VAT IDs through `IVatValidationProvider`. The infrastructure implementation calls the EU VIES SOAP endpoint when `Compliance:VatValidation:Vies:Enabled=true`; disabled or unavailable providers leave the customer in `Unknown` with an operator-visible source/message instead of creating a false valid/invalid decision.
- `RetryUnknownCustomerVatValidationBatchHandler` and the disabled-by-default `VatValidationRetryWorker` can retry provider-generated `Unknown` VAT validation decisions after a configured age without overwriting operator/manual review decisions.
- VIES provider policy coverage now exercises disabled provider, failed HTTP status, malformed XML, valid response, invalid response, VAT ID request normalization, and retry-batch filtering without making external network calls.
- `scripts/smoke-vies-live.ps1` provides a guarded direct VIES smoke harness for configured valid/invalid VAT IDs and the provider-failure `Unknown` expectation.
- `scripts/check-go-live-readiness.ps1` includes the VIES smoke prerequisite dry-run in the local go-live readiness summary without executing VIES calls.
- WebAdmin `Billing/TaxCompliance` now exposes a CSV invoice export with invoice/customer/order/payment/status/tax totals and VAT/reverse-charge follow-up flags. The export is UTF-8 with formula-injection protection for spreadsheet safety.
- Invoice review rows now receive explicit `RequiresVatId`, `IsReverseChargeCandidate`, `IsDueSoon`, and `IsOverdue` flags from `GetTaxComplianceOverviewHandler` instead of deriving collection or compliance follow-up from display status text.
- Reverse-charge review candidates are surfaced when VAT and reverse-charge settings are enabled, the customer is B2B, a VAT ID exists, and the CRM default billing country differs from the invoice issuer country.
- TaxCompliance operators can now mark an invoice-level reverse-charge decision as applies or not applicable. The decision is persisted on the invoice with `ReverseChargeApplied` and `ReverseChargeReviewedAtUtc`, guarded by the invoice row version, and the candidate count drops once reviewed.
- Draft invoices cannot be posted/opened or marked paid until issuer legal name, tax id, address, postal code, city, and country are configured. When VAT is enabled, business-customer invoices also require a customer VAT ID before issue.
- CRM invoices now persist `IssuedAtUtc` plus `IssuedSnapshotJson` the first time a draft invoice is opened or marked paid. The snapshot freezes issuer, customer, business, totals, due/payment timestamps, and line data for later archive/export work and is not rewritten by later status transitions.
- Issued invoice archive metadata now includes `IssuedSnapshotHashSha256`, `ArchiveGeneratedAtUtc`, `ArchiveRetainUntilUtc`, and `ArchiveRetentionPolicyVersion`. The retention horizon is controlled by the site-level `InvoiceArchiveRetentionYears` setting and is written once when the invoice is issued.
- WebAdmin CRM invoice editors expose downloadable JSON, printable HTML, and structured invoice source-model JSON artifacts for invoices that already have an issued snapshot. Draft invoices and legacy issued invoices without a snapshot do not return a generated archive artifact silently.
- The structured invoice source-model export maps immutable issued snapshots into document, seller, buyer, business, line, tax-summary, and total sections with an explicit `NotZugferdFacturX` compliance status. It is groundwork for future ZUGFeRD/Factur-X generation and must not be treated as a compliant e-invoice artifact.
- `IEInvoiceGenerationService` is registered behind a provider-neutral boundary, but the current default implementation is `NotConfiguredEInvoiceGenerationService` and does not generate fake compliant artifacts.
- `GenerateInvoiceEInvoiceArtifactHandler` now enforces the generation preconditions centrally: the invoice must exist, the issued snapshot must still be available, the requested format must be known, and generated artifacts must match the requested invoice with complete metadata.
- WebAdmin has a guarded e-invoice artifact download endpoint wired to the generation handler. It returns a file only for generated and validated artifacts; while the default generator remains `NotConfigured`, operators receive a localized unavailable/configuration message and no misleading e-invoice download button is shown in the invoice editor.
- `PurgeExpiredInvoiceArchivesHandler` and `InvoiceArchiveMaintenanceBackgroundService` clear expired issued-snapshot payloads after `ArchiveRetainUntilUtc`, record `ArchivePurgedAtUtc` and `ArchivePurgeReason`, and write an `InvoiceArchivePurged` event log entry. The worker is explicit opt-in through `InvoiceArchiveMaintenanceWorker:Enabled`.
- TaxCompliance playbooks are default-collapsed so the workspace keeps operational queues first and long guidance available on demand.
- Invoice and order entities carry tax snapshots and VAT-related fields sufficient for current review workflows.

Gaps:

- TaxCompliance still lacks live VIES smoke in the target deployment, WebAdmin operator-flow VIES smoke against production settings, external immutable archive storage beyond the current `IInvoiceArchiveStorage` internal/database and file-system providers, final invoice document rendering beyond the current printable archive HTML, and compliant e-invoice generation/validation.
- VIES production smoke, retry-worker activation policy, external S3-compatible object-storage smoke with MinIO Object Lock/versioning validation, and ZUGFeRD/Factur-X generation/validation require dedicated go-live completion. Current JSON/HTML/CSV archive/export is useful operational output, not full e-invoice compliance.
- The VIES retry workflow, e-invoice generation direction, and archive storage provider decision are documented in `docs/compliance-decisions.md`.

## Business Onboarding / Public Visibility

Status: onboarding state, support queues, and lifecycle actions exist; front-office visibility is now guarded by both approval and active state.

Verified implementation:

- New businesses are created as inactive and `PendingApproval`.
- Admin lifecycle handlers approve, suspend, and reactivate businesses with row-version checks.
- Approval and reactivation now require the minimum go-live prerequisites: active owner, primary location, contact email, and legal name.
- Invitation preview and acceptance allow pending-approval onboarding businesses even while inactive, but continue to reject suspended or unavailable businesses.
- Invitation resend/revoke operations are scoped to actionable invitation states: accepted and revoked invitations cannot be reissued, accepted invitations cannot be revoked, and already revoked invitations remain closed.
- Both API password login and WebAdmin sign-in now require confirmed email addresses before issuing tokens or admin cookies.
- Public discovery, map discovery, public detail, and member engagement queries now require `OperationalStatus=Approved` and `IsActive=true`.

Coverage:

- WebAdmin hosted smoke covers creation into inactive `PendingApproval`, approval prerequisite failure, approve/suspend/reactivate lifecycle forms, invitation resend/revoke, and business-location row-version mutation.
- WebApi-hosted smoke tests for email-confirm enforcement and public discovery/detail visibility now pass against the local PostgreSQL `darwin_integration_tests` database with `2` passed and `0` skipped.

## Inventory / Returns

Status: inventory handlers cover manual adjustment, reservation/release, return receipt, and operator workspaces; core stock movement handlers are now safer for retried workflows.

Verified implementation:

- Manual adjustments validate variant/warehouse stock and reject negative adjustments that exceed available quantity.
- Reservation moves stock from available to reserved and release moves reserved stock back to available.
- Return receipt increases available stock and is idempotent when a return/reference id is supplied.
- Reservation and release are now also idempotent when a `ReferenceId` is supplied, preventing retried order/cart workflows from double-moving stock.
- WebAdmin hosted positive mutation smoke coverage now posts the real Razor/HTMX forms for reserve stock, release reservation, return receipt, stock-transfer MarkInTransit/Complete, and purchase-order Issue/Receive. The same pass hardened row-version handling for EF InMemory smoke tests and base64 row-version delete posts on Media, Brands, and Business Locations.
- Inventory/returns hosted operator-flow smoke now covers stock reserve/release, order cancel stock release, explicit return receipt idempotency, refund coordination without unintended stock movement, stock-transfer lifecycle, and purchase-order receiving through WebAdmin forms.

Gaps:

- Carrier-integrated RMA automation remains under the DHL/shipping go-live slice.

## Testing / CI

Status: WebAdmin test project exists and is wired into split CI lanes; provider-focused WebApi tests and the split WebAdmin smoke subsets are green locally.

- `.github/workflows/tests-quality-gates.yml` now restores and runs `tests/Darwin.WebAdmin.Tests/Darwin.WebAdmin.Tests.csproj` as separate security, public/auth smoke, render smoke, tokenless CSRF, valid-token CSRF, and positive mutation coverage lanes.
- `scripts/ci/verify_coverage.py` now accepts `--webadmin-threshold`.
- Initial WebAdmin coverage threshold is intentionally low (`10`) so the lane can run continuously while coverage grows.
- `Darwin.Infrastructure.Tests` passed locally on 2026-05-10 with `45` passed and `0` skipped after restoring compatibility with injected clock services, provider-specific design-time factories, and whitespace-safe connection-string precedence.
- The focused WebApi provider boundary run for Stripe, DHL, Brevo, and provider-callback worker tests passed locally on 2026-05-10 with an isolated output path to avoid running-binary file locks (`280` passed, `0` skipped).
- `Darwin.WebAdmin.Tests` builds locally with an isolated output path. The non-hosted security subset, public/auth hosted smoke subset (`27` passed), render hosted smoke subset (`105` passed), tokenless CSRF matrix (`115` passed), valid-token CSRF matrix (`8` passed), and positive mutation smoke flow (`1` passed) passed locally. The positive mutation flow was re-run on 2026-05-10 after adding hosted inventory lifecycle coverage and hardening base64 row-version delete posts. The focused hosted business onboarding smoke for creation, approval prerequisite failure, and approve/suspend/reactivate lifecycle forms passed locally on 2026-05-10 with `3` passed, `0` skipped. The focused WebAdmin render smoke including the admin-assisted onboarding wizard passed locally on 2026-05-10 with `45` passed and `0` skipped.
- `BusinessOnboardingApiSmokeTests` under `Darwin.Tests.Integration` passed locally against PostgreSQL on 2026-05-10 with `2` passed and `0` skipped after the Testing host was isolated from production Data Protection certificate requirements and real email delivery.
- `ViesVatValidationProviderTests` under `Darwin.WebApi.Tests` passed locally on 2026-05-10 with `7` passed and `0` skipped, covering the phase-one soft `Unknown`/manual-review policy without external VIES calls.
- The focused unit/source-contract run for Shipment/Stripe/Billing/Communication/Inventory/Tax/SignIn passed locally on 2026-05-09 (`602` passed). The broader Inventory/Business/Invitation/SignIn/Tax/Invoice/VAT filter passed on 2026-05-10 with `981` passed, `0` skipped after replacing stale exact-source assertions with stable contracts for JWT, business discovery, invitation auth, row-version forms, dashboard compactness, and business setup/loyalty API wiring.
- The focused WebAdmin source-contract lane passed locally on 2026-05-10 with `257` passed, `0` skipped after converting stale media/dashboard/settings/mobile/orders/page-editor, CMS/media, role/permission, catalog, product, add-on group, CRM, shipping, shared view-model, and users contracts into stable security, localization, HTMX, route, row-version, and mutation-safety assertions. The dedicated business source-contract lane also passes with `87` passed, `0` skipped after Base64 row-version lifecycle/delete assertions were aligned with the current WebAdmin form-post contract. The all-source `SecurityAndPerformance` filter now passes with `615` passed, `0` skipped after converting stale contracts/packaging and Darwin.Web exact-layout assertions into stable security, route, localization, contract-shape, public-storefront, go-live readiness, and provider-smoke dry-run contracts.
- Provider smoke script source-contract coverage now verifies the Stripe, DHL, VIES, and Brevo harnesses remain opt-in for external calls, avoid committed secret patterns, and do not print raw provider response payloads. The focused test `ProviderSmokeScripts_Should_StayGuardedAndAvoidSecretOutput` passed locally on 2026-05-10 with `1` passed and `0` skipped.
- Go-live readiness dry-run behavior coverage now executes `scripts/check-go-live-readiness.ps1` from the unit suite and verifies the readiness summary, provider prerequisite sections, accepted ready/blocked status, provider-ready dry-run status with fake non-secret prerequisites, open archive/e-invoice decision blocking, and secret-output guard. The focused tests `GoLiveReadinessScript_Should_RunDryRunAndAvoidSecretOutput` and `GoLiveReadinessScript_Should_ReportProviderReadyAndKeepOpenDecisionsBlocked` passed locally on 2026-05-10 with `2` passed and `0` skipped.
- Provider smoke script dry-run behavior coverage now executes the Stripe, DHL, Brevo, and VIES smoke scripts with `DARWIN_*` inputs cleared in the child process and verifies each one blocks safely with exit code `2`, reports missing prerequisites, and avoids provider secret output. It also executes the scripts with fake non-secret prerequisites and no `-Execute` flag to verify exit code `0` readiness reporting without external calls. The focused tests `ProviderSmokeScripts_Should_BlockDryRunWhenPrerequisitesAreMissing` and `ProviderSmokeScripts_Should_ReportReadyDryRunWithoutExecutingExternalCalls` passed locally on 2026-05-10 with `8` passed and `0` skipped.
- Invoice archive storage coverage now includes the source-contract boundary, router behavior, and behavioral tests for the internal/database fallback. The focused run `dotnet test tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~InvoiceArchiveStorageRouterTests|FullyQualifiedName~DatabaseInvoiceArchiveStorage|FullyQualifiedName~InvoiceArchiveStorage_Should_KeepInternalFallbackBoundaryAndAvoidImplicitProviderChoice" --no-restore /p:UseSharedCompilation=false` passed locally on 2026-05-10 with `8` passed and `0` skipped, covering named provider routing, unregistered-provider failure, save/read/exists, exact SHA-256 metadata, retention policy metadata, purge audit metadata, mismatched invoice id rejection, and empty payload rejection.
- Invoice archive storage source-contract coverage also verifies `IInvoiceArchiveStorage` keeps save/read/exists/purge operations, hash and retention metadata, named provider registration, router-based fallback registration, and no implicit Azure/S3/MinIO implementation choice inside the Application layer.
- WebAdmin CSP was tightened back to self-hosted script/style/font/connect sources, and admin/auth/anti-forgery cookies now keep secure defaults instead of relying on environment-specific relaxation.


## Compliance Decisions

- VIES phase 1 policy is soft `Unknown` plus manual review for disabled, unavailable, rate-limited, timeout, malformed response, or provider-exception outcomes. Only clear valid/invalid provider responses may record `Valid` or `Invalid`.
- Invoice archive phase 1 uses the internal/database provider through the named `IInvoiceArchiveStorage` router. The router supports multiple providers, and the reusable `IObjectStorageService` abstraction/options boundary now defines provider-neutral save/read/existence/metadata/delete/temporary-url/capability contracts for future archive, media, DHL label, export, and e-invoice artifacts. A policy wrapper can route invoice archive artifacts through the generic S3-compatible object-storage adapter when `InvoiceArchiveStorage:ProviderName=S3Compatible`.
- Object-storage production direction is MinIO through the S3-compatible provider, with AWS S3 and Azure Blob as supported alternatives. Provider-level immutability is not complete until deployment Object Lock/retention/legal-hold validation is smoke-tested with the target bucket.
- E-invoice phase 1 now has a structured invoice source-model export from issued snapshots plus a provider-neutral `IEInvoiceGenerationService` boundary, but the default generator is explicitly `NotConfigured` and no compliant e-invoice artifact is generated. The primary target remains ZUGFeRD/Factur-X because SME users need a human-readable PDF plus structured data. XRechnung remains a secondary export backlog item. Current JSON/HTML/CSV/source-model exports are not full e-invoice compliance.
- E-invoice tooling selection criteria are documented in `docs/e-invoice-tooling-decision.md`; no library or external generation service is selected yet.
- WebAdmin now has an admin-assisted Loyan business onboarding wizard that summarizes the existing setup, invitation, location, loyalty, communication, storefront-visibility, and activation workspaces without bypassing their validations. Later self-service onboarding in `Darwin.Web` remains separate.
- Detailed compliance planning is in `docs/compliance-decisions.md`.
