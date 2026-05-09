# 🧪 Darwin Testing Strategy & Execution Guide (Updated)

This document is the authoritative testing guide for the Darwin repository.
It is intentionally practical: what exists today, what quality gates enforce, how to run tests locally, and what to improve next.

---

## 1) Scope and goals

Darwin testing aims to provide:

- **Regression confidence** for domain logic, application handlers, contracts, API behavior, and mobile shared client behavior.
- **Fast feedback** through layered suites (small unit tests + deeper integration tests).
- **Contract stability** for DTO/JSON payloads used by WebApi and mobile clients.
- **Delivery safety** via CI lane split and per-lane coverage thresholds.

Primary principles:

- Keep tests deterministic and isolated.
- Prefer clear AAA structure (Arrange / Act / Assert).
- Use explicit and readable test data.
- Preserve backward compatibility in contracts unless breaking change is intentional and documented.

---

## 2) Current test projects (actual state)

The repository currently contains these test projects:

1. `tests/Darwin.Tests.Unit`
   - Core unit tests for Domain/Application/Shared-level behavior.
2. `tests/Darwin.Tests.Integration`
   - End-to-end API integration coverage through in-process host infrastructure.
3. `tests/Darwin.Contracts.Tests`
   - DTO and JSON serialization/compatibility checks.
4. `tests/Darwin.WebApi.Tests`
   - WebApi-focused test coverage (e.g., mappers / API-facing conversions).
5. `tests/Darwin.Infrastructure.Tests`
   - Infrastructure-focused checks (e.g., persistence setup/design-time factory behavior).
6. `tests/Darwin.Mobile.Shared.Tests`
   - Mobile shared client/services reliability and behavior checks.
7. `tests/Darwin.WebAdmin.Tests`
   - WebAdmin-focused smoke and security tests for the admin panel (e.g., security header checks, authentication flows, anti-forgery token validation).
   - Uses `Microsoft.AspNetCore.Mvc.Testing` with an in-process `WebAdminTestFactory`.
   - Wired into `tests-quality-gates.yml` as split `webadmin` coverage lanes so slow hosted smoke matrices remain diagnosable.
8. `tests/Darwin.Tests.Common`
   - Shared test infrastructure/helpers consumed by other suites.

### Coverage lanes in CI

CI treats the main suites as independent lanes:

- unit
- contracts
- infrastructure
- webapi
- integration
- webadmin-security
- webadmin-public-smoke
- webadmin-render-smoke
- webadmin-csrf-tokenless
- webadmin-csrf-valid
- webadmin-mutation-positive
- mobile-shared


Lane coverage is validated from Cobertura reports via `scripts/ci/verify_coverage.py`.

---

## 3) Responsibilities by lane

### 3.1 Unit lane (`Darwin.Tests.Unit`)

Use for business rules and handlers that should run without external dependencies:

- Domain validations and invariants.
- Application handler behavior.
- Policy resolution and normalization logic.
- Utility/helper behavior with deterministic inputs.

### 3.2 Contracts lane (`Darwin.Contracts.Tests`)

Use for DTO compatibility and transport safety:

- Property-name compatibility (camelCase / expected naming).
- Enum and field round-trip behavior.
- Nullable/optional field transport behavior.
- Backward-compatible serialization shape for mobile/API consumers.

### 3.3 Infrastructure lane (`Darwin.Infrastructure.Tests`)

Use for persistence/infrastructure correctness:

- Design-time DbContext factory.
- Mapping/configuration safety checks.
- Migration-related guard tests where feasible.
- Provider-selection safety for `PostgreSql` and `SqlServer` registration.
- Provider-specific migration guard checks for PostgreSQL and SQL Server lanes.

### 3.4 WebApi lane (`Darwin.WebApi.Tests`)

Use for API-edge conversion and mapping stability:

- Mapper tests between application DTOs and transport contracts.
- API-oriented transformation logic.

### 3.5 Integration lane (`Darwin.Tests.Integration`)

Use for behavior across full HTTP pipeline:

- Authentication and authorization boundaries.
- Request/response shape and status code correctness.
- Multi-step endpoint flows (Identity/Profile/Loyalty/Meta).
- Realistic stateful scenarios with deterministic reset.

### 3.6 Mobile.Shared lane (`Darwin.Mobile.Shared.Tests`)

Use for mobile shared client reliability:

- API route consistency.
- Auth header injection behavior.
- Retry/reliability behavior.
- Service-level behavior and failure handling.

### 3.7 WebAdmin lane (`Darwin.WebAdmin.Tests`)

Use for the WebAdmin panel's HTTP-level correctness:

- Security header presence and correctness (CSP, X-Content-Type-Options, Referrer-Policy, Permissions-Policy).
- Authentication/redirect boundaries for admin-only routes.
- Anti-forgery token and form rendering sanity checks.
- Forwarded-header handling and HTTPS redirection behavior.
- Hosted smoke flows that exercise real Razor/HTMX forms, including row-version protected mutations.

---

## 4) Test design conventions

- Follow `MethodUnderTest_State_ExpectedResult` naming pattern.
- Keep each test focused on a single behavior.
- Prefer `Theory` + explicit data for matrices.
- Avoid sleep/time-based flakiness.
- Use deterministic IDs/timestamps where possible.
- Assert both success path and essential negative path.

For API/integration tests:

- Verify status code and payload contract together.
- Assert problem details/error envelope shape where relevant.
- Keep auth requirements explicit in test names.

---

## 5) Local execution guide

> Prerequisite: .NET SDK compatible with the solution (see root README badges and project configuration).

### 5.1 Run each lane

```bash
dotnet test tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj

dotnet test tests/Darwin.Contracts.Tests/Darwin.Contracts.Tests.csproj

dotnet test tests/Darwin.Infrastructure.Tests/Darwin.Infrastructure.Tests.csproj

dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj

dotnet test tests/Darwin.Tests.Integration/Darwin.Tests.Integration.csproj

dotnet test tests/Darwin.Mobile.Shared.Tests/Darwin.Mobile.Shared.Tests.csproj

# WebAdmin tests:
dotnet test tests/Darwin.WebAdmin.Tests/Darwin.WebAdmin.Tests.csproj
```

### 5.2 Run lane with coverage output

```bash
dotnet test tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults/unit
```

Repeat with the matching results directory name:

- `TestResults/contracts`
- `TestResults/infrastructure`
- `TestResults/webapi`
- `TestResults/integration`
- `TestResults/mobile-shared`

### 5.3 Validate coverage thresholds locally

```bash
python scripts/ci/verify_coverage.py \
  --unit-threshold 35 \
  --contracts-threshold 20 \
  --infrastructure-threshold 20 \
  --webapi-threshold 20 \
  --integration-threshold 20 \
  --mobile-shared-threshold 20
```

### 5.4 Current execution snapshot (2026-05-08)

Latest known-good signal for the recently expanded Webhook/reader/writer tests is:

- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~StripeWebhooksControllerTests"`
- 38 passed (including constructor-guard tests)
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~DhlWebhooksControllerTests"`
- 36 passed (including constructor-guard tests)
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~BrevoWebhooksControllerTests"`
- 46 passed (including constructor-guard tests)
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~WebhooksControllerTests"`
  - 98 passed
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~ProviderWebhookPayloadReaderTests"`
  - 19 passed
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~StripeWebhookSignatureVerifierTests"`
- 26 passed (including constructor-guard tests)
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~ProviderCallbackInboxWriterTests"`  
  - 18 passed
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~ProviderCallbackBackgroundServiceTests"`  
- 133 passed (including batch-level cancellation safety during callback handling, claim-save, completion save and processing, batch filtering for `MaxAttempts` on both `Pending` and `Failed` states, exact min/max normalization for `MaxAttempts` (including `MaxAttempts=0`, negative clamp, and negative-minimum skip behavior for failed rows), case-insensitive terminal status handling for `Processed`, `Succeeded`, and status-query edge-cases, exact-status enforcement for canonical `Pending`/`Failed` processing, batch ordering by `LastAttemptAtUtc ?? CreatedAtUtc` with limited batch size, stable UTC snapshot behavior for batch timestamps, mixed `Failed`/`Pending` cooldown + readiness in same batch, pending and failed `RetryCooldown` normalization boundaries including negative-value and high-value checks for both states and null-`LastAttemptAtUtc` fallbacks, `RetryCooldownSeconds` max clamp at `3600`, oversized-poll-interval one-iteration behavior, unsupported-provider no-trim behavior with extra whitespace, `WorkerFailureText` sanitization/truncation on unsupported-provider failures, DHL validation fail-fast for missing shipment references, explicit DHL handler validation-failure branch coverage, explicit DHL validation failure for missing occurredAtUtc and oversized `trackingNumber` branch coverage, provider-whitespace unsupported-provider branch coverage, queue-claim validation messaging checks, handler-missing failure path for known providers (`Stripe`, `Brevo`, `DHL`) including mixed-case provider input coverage, explicit empty-provider and space-padded-known-provider unsupported coverage, and non-canonical status-row filtering)
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~ProviderCallback"`  
- 151 passed (verified on 2026-05-07 after adding deterministic completion-fallback, completion-transient-retry, completion-concurrency, batch claim-concurrency, transient claim-save, cooldown bypass, failed-message claim-save-failure handling, batch max-attempt filtering and cancellation handling during claim, claim-save in batch, completion, and processing, plus batch UTC snapshot assertions, mixed `Failed`/`Pending` cooldown batch behavior, `RetryCooldownSeconds` normalization/clamping coverage including negative and high-value checks for both `Pending` and `Failed` plus null-`LastAttemptAtUtc` readiness behavior, worker `MaxAttempts` min/max and negative normalization including skip-at-limit behavior for failed rows, unsupported-provider whitespace/no-trim coverage, oversized-poll-interval one-iteration behavior, DHL missing-shipment validation catch-path coverage, DHL handler-level validation-failure branch coverage for invalid callback payload content including missing `occurredAtUtc` and oversized `trackingNumber` (>128), unsupported-provider whitespace-path coverage, failure-reason sanitization/truncation validation, case-insensitive provider lookup on DHL callbacks, handler-missing failure path for known providers (`Stripe`, `Brevo`, `DHL`), explicit empty-provider and space-padded-known-provider unsupported branch, and non-canonical status-row filtering)
- `dotnet test tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~ProcessBrevoTransactionalEmailWebhookHandlerTests"`  
  - 102 passed

`dotnet test tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~ShipmentCarrierEventHandlerTests"`  
  - 100 passed (as of 2026-05-08 after adding delivered exception-field merge-on-duplicate dedupe coverage)

`dotnet test tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~ApplyShipmentCarrierEvent_Should"`  
  - 36 passed (including required max-length and optional-field boundary validation coverage)

`dotnet test tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~BillingPlanAdminHandlerTests"`
  - 22 passed (verified 2026-05-08; covers GetBillingPlansAdminPageHandler pagination/filtering/search, GetBillingPlanOpsSummaryHandler empty and counted state, GetBillingPlanForEditHandler found/not-found/soft-deleted, CreateBillingPlanHandler validation and duplicate-code rejection and code/currency normalization, UpdateBillingPlanHandler not-found, concurrency, duplicate-code, and successful update with normalization)

`dotnet test tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~BillingWebhookQueryHandlersTests"`
  - 12 passed (verified 2026-05-08; adds GetBillingWebhookSubscriptionsPageHandler coverage: returns non-deleted items, empty state, search by EventType and CallbackUrl, normalizes invalid page params, maps fields correctly)

Running the full `Darwin.WebApi.Tests` suite in the current branch still shows failures in pre-existing suites (mostly `Security` / `Loyalty` areas), so the newly added webhook-focused coverage remains green as an isolated subset.

When adding or refactoring Webhook-related behavior, prefer adding/adjusting tests in this subset before widening to broader suites.

---

## 6) CI policy (tests-quality-gates workflow)

The workflow `.github/workflows/tests-quality-gates.yml`:

- Runs each lane independently.
- Publishes artifacts for each lane.
- Enforces coverage thresholds using the verification script.
- Uses a temporary PR soft-gate mode policy (configurable by repository variable and workflow input).

This separation improves diagnosability and keeps regressions localized to a specific lane.

---

## 7) Platform-specific notes

### Integration tests

- Integration lane in CI currently expects SQL Server service availability and testing environment configuration unless a lane explicitly opts into PostgreSQL.
- PostgreSQL is now the preferred local/default provider for application startup validation. Use `docker-compose.postgres.yml` for local PostgreSQL and pgAdmin.
- Keep tests resettable/deterministic and avoid hidden shared mutable state.

### Persistence provider validation backlog

Do not expand test projects until the current implementation slice calls for it, but track these required coverage additions:

- Add provider-selection smoke coverage proving `Persistence:Provider=PostgreSql` resolves Npgsql and `Persistence:Provider=SqlServer` resolves SQL Server.
- Add PostgreSQL migration/seed verification against a fresh PostgreSQL database.
- Add provider-specific migration coverage proving active Shipping Methods enforce unique `Carrier` + `Service` pairs through SQL Server filtered indexes and PostgreSQL partial indexes, matching the Application/WebAdmin validation contract.
- Add a guard that shared EF mappings do not introduce SQL Server-only column types such as `uniqueidentifier` or `nvarchar(max)`.
- Add filtered-index SQL coverage for PostgreSQL so SQL Server filters are normalized before migrations are applied.
- Add concurrency coverage for `RowVersion`: SQL Server native rowversion and PostgreSQL client-managed bytea concurrency.
- Add schema-placement coverage proving no application tables are created in PostgreSQL `public` or SQL Server `dbo`.
- Add migration-script guard coverage that generates idempotent provider scripts and fails if PostgreSQL introduces unwanted `dbo`/application tables in `public`, or if the latest SQL Server/PostgreSQL model snapshots contain unqualified application `ToTable(...)` mappings.
- Add SQL Server fresh-bootstrap coverage proving the full SQL Server migration lane applies to an empty database and leaves no application tables in `dbo`.
- Add PostgreSQL extension/index coverage proving `pg_trgm`, the expected JSON/text-JSON GIN indexes, and the 88 direct `IX_PG_*_Like_Trgm` search indexes exist after migration.
- Add PostgreSQL `citext` coverage proving identifier uniqueness and equality behave case-insensitively for login identifiers, role/permission keys, slugs, SKUs, billing plan codes, promotion codes, and tax category names.
- Add provider-neutral canonical lookup coverage proving media asset roles and shipping DHL carrier markers are normalized on write and queried with direct equality, and promotion code/tax category equality relies on canonical/case-insensitive storage rather than query-side `ToLower()` or `ToUpper()`.
- Add PostgreSQL `jsonb` coverage proving the current 21 selected operational/configuration JSON columns are created as `jsonb`, the current 14 `IX_PG_*_JsonbGin` indexes exist, and text-search/equality-sensitive paths remain on text-backed JSON columns until query code is migrated to JSON operators or generated search fields.
- Add PostgreSQL trigram coverage proving text-backed JSON search columns keep their `IX_PG_*_Trgm` indexes for event-log properties, provider callback payloads, and business admin text overrides.
- Add PostgreSQL JSON validity coverage proving the current 11 `CK_PG_*_ValidJson` constraints exist for text-backed JSON columns and reject invalid JSON on new writes.
- Add provider-neutral search coverage for high-value catalog, CMS, business discovery/list, Billing operator, CRM operator, Inventory operator, Orders/shipment operator, Shipping, Media, Loyalty, Identity user/mobile-device/permission, add-on group, variant lookup, and business/communication operations paths proving escaped substring search terms and uppercase/lowercase variants return the same relevant rows on PostgreSQL and SQL Server.
- Add CMS admin-list coverage proving soft-deleted page translations are ignored by search and localized title projection.
- Add cart/add-on localization coverage proving soft-deleted add-on option/value translations are ignored in cart summaries and Admin add-on option counts ignore soft-deleted options.
- Add WebApi JWT settings coverage proving soft-deleted `SiteSetting` rows are ignored when signing/validation parameters are refreshed.
- Add cross-application `SiteSetting` soft-delete coverage proving JWT issuing/refresh, app bootstrap, business invitation/email templates, phone verification, password reset, DHL shipment defaults, and SMS/WhatsApp transports ignore soft-deleted settings rows.
- Add WebAdmin communication cooldown coverage proving soft-deleted `ChannelDispatchAudit` rows do not extend admin test-message cooldown windows.
- Add business-invitation boundary coverage proving pending invitations become expired exactly at `ExpiresAtUtc` consistently across preview, list, and filters.
- Add provider-neutral guard coverage that fails on ad-hoc/unescaped application-query `EF.Functions.Like`, query-side `Enum.ToString()` search, or query-side `Guid.ToString()` search unless a provider-specific exception is documented.
- Add provider-neutral guard coverage that fails when EF query predicates embed moving `DateTime.UtcNow` expressions directly instead of using local UTC snapshots/cutoff parameters.
- Add model-metadata coverage proving all decimal properties have explicit precision either through entity-specific configuration or the provider-neutral fallback convention.
- Add PostgreSQL provider-registration coverage proving runtime and design-time Npgsql paths preserve normalized connection defaults for application name, retry limits, timeouts, keepalive, and auto-prepare settings.
- Add runtime composition smoke coverage for `Darwin.WebApi` and `Darwin.Worker` with `Persistence:Provider=PostgreSql`, including localization, clock, Data Protection, identity infrastructure, and background-worker DI validation without relying on WebAdmin-only registrations.
- Add Data Protection configuration coverage proving WebAdmin, WebApi, and Worker share `ApplicationName=Darwin`, use the configured shared key path, and fail startup when `DataProtection:RequireKeyEncryption=true` but the configured certificate thumbprint cannot be resolved.
- Add cart-line identity coverage proving add/update/remove operations canonicalize `SelectedAddOnValueIdsJson`, so the same selected add-on GUIDs match regardless of caller JSON ordering or duplicate IDs.
- Add billing provider-event correlation coverage proving `EventLogs.PropertiesJson` candidate rows are confirmed by JSON value matching and do not produce false correlations from unrelated substring matches in Stripe webhook payload keys or larger string values.
- Add WebAdmin/worker queue-status coverage proving provider callback inbox and shipment provider operation successful worker completions are written as `Processed`, and that legacy `Succeeded` rows still appear in processed summaries/filters until old customer data is normalized.
- Add Worker retry-timing and webhook payload-integrity coverage proving each processing iteration uses a stable UTC snapshot for retry cutoffs/attempt timestamps and webhook retries do not embed stale `PayloadHash` values into newly signed payload envelopes.
- Add Worker multi-instance queue-claim coverage proving concurrent workers skip rows already claimed through optimistic concurrency before executing external side effects.
- Add Worker completion-save resilience coverage proving queue completion and inactive webhook-subscription batch updates retry transient database failures and handle post-claim concurrency without crashing the worker loop.
- Add notification sender idempotency coverage proving SMTP/SMS/WhatsApp create a pending audit claim before external sends, skip duplicate sends when a non-deleted active audit (`Pending` or `Sent`) already exists for the same `CorrelationKey`, allow old failed/pending retry flows with new correlation keys, and enforce one active audit through SQL Server/PostgreSQL unique filtered/partial indexes.
- Add admin text override JSON coverage proving business/site-setting validators, public business text resolution, and WebAdmin admin-text localization all use the same object-of-culture-to-string-map structure, reject structurally invalid values, and do not throw on duplicate/case-variant keys.
- Add Business operations RowVersion coverage proving onboarding provisioning, provider-callback inbox actions, and communication dispatch cancellation reject missing/stale row versions and compare null database row versions without raw exceptions.
- Add WebApi provider-webhook boundary coverage proving public Stripe/DHL webhook endpoints reject payloads larger than the configured/raw 256 KiB cap with HTTP 413 before signature verification or inbox persistence, and still accept valid payloads within the cap.
- Add WebApi provider-webhook idempotency coverage proving concurrent Stripe/DHL callbacks with the same provider idempotency key create only one active inbox row and subsequent requests return `duplicate=true`.
- Add Stripe webhook processing idempotency coverage proving concurrent processing of the same Stripe event creates only one non-deleted `EventLog` row and the losing save path returns a duplicate result without applying a second observable update.
- Add WebAdmin provider-callback inbox coverage proving Stripe/DHL payload previews show bounded operational summaries instead of raw JSON, invalid legacy JSON falls back safely, and unknown-provider previews redact obvious secret/token/signature fields.
- Add WebAdmin operational action coverage proving null or empty row versions return validation/concurrency failures without raw exceptions across billing management, billing plans, catalog/CMS edits, CRM edits, identity role/user-role edits, inventory edits/lifecycle, loyalty account actions, media/pricing/SEO/settings/shipping edits, provider callback inbox, shipment provider operation, communication dispatch cancellation, business onboarding provisioning, and shipment carrier exception resolution.
- Add WebAdmin operational action coverage proving action values are trimmed and case-insensitive for billing webhook delivery, payment dispute review, provider callback inbox, CRM lead/opportunity lifecycle, and shipment provider operations.
- Add WebAdmin operational concurrency coverage proving billing webhook delivery, provider callback inbox, and shipment provider operation actions convert post-row-version-save concurrency conflicts to `ItemConcurrencyConflict` and manual requeue clears retry cooldown state so workers can pick the row immediately.
- Add CRM RowVersion coverage proving customer, lead, opportunity, invoice, invoice status/refund, customer segment, and lead/opportunity lifecycle operations reject missing/stale row versions and compare null database row versions without raw exceptions.
- Add Inventory RowVersion coverage proving warehouse, supplier, stock-level, stock-transfer, stock-transfer lifecycle, purchase-order, and purchase-order lifecycle operations reject missing/stale row versions and compare null database row versions without raw exceptions.
- Add CRM/Inventory admin post-save concurrency coverage proving customer, lead, lead conversion, opportunity, invoice edit/status/refund, customer segment, warehouse, supplier, stock-level, stock-transfer, stock-transfer lifecycle, purchase-order, and purchase-order lifecycle operations convert database concurrency exceptions after the initial row-version check into localized admin-safe conflict responses.
- Add Loyalty RowVersion coverage proving account activation/suspension/adjustment, reward confirmation, scan-session expiry, program/reward-tier edit/delete, business campaign edit/activation, and campaign delivery status operations reject missing/stale row versions where required and compare null database row versions without raw exceptions.
- Add Loyalty/Orders admin post-save concurrency coverage proving loyalty account activation/suspension/adjustment, reward confirmation, loyalty program/reward-tier edit/delete, order status update, and shipment carrier exception resolution operations convert database concurrency exceptions after the initial row-version check into localized admin-safe conflict responses.
- Add Loyalty scan-session and storefront checkout race coverage proving concurrent scan confirmations return localized scan-session conflicts instead of raw concurrency exceptions, and concurrent cart checkout attempts produce a single order with a stable unique order number while the losing request receives a safe checkout conflict.
- Add WebAdmin enum-boundary validation coverage proving out-of-range numeric enum values are rejected for business operational status, CMS page status, add-on selection mode, and promotion type instead of being persisted.
- Add WebAdmin lifecycle transition coverage proving business approval only applies to pending businesses, suspension only applies to approved businesses, reactivation only applies to suspended businesses, and closed CRM opportunities cannot be advanced.
- Add WebAdmin Business post-save concurrency coverage proving business lifecycle, business edit/delete, location edit/delete, media edit, member edit/delete, onboarding provisioning, and communication-dispatch cancel operations convert database concurrency exceptions after the initial row-version check into localized admin-safe conflict responses.
- Add Business media delete coverage proving the `BusinessMediaDeleteDto` path rejects missing/stale row versions and converts database concurrency exceptions into localized item conflict responses while the legacy id-only delete remains limited to compatibility callers.
- Add Business invitation WebAdmin concurrency coverage proving invitation list rows render Base64 row versions, resend/revoke reject missing or stale row versions, retry-from-email-audit uses the current invitation row version, and post-save conflicts surface localized item conflict messages before any duplicate invitation email is sent.
- Add worker webhook-delivery retry coverage proving a successful external callback is not re-sent when the normal completion save hits a concurrency conflict, because the fallback completion update persists status, response code, payload hash, attempt time, and retry count for the same delivery.
- Add WebAdmin billing transition coverage proving payment status edits reject unsupported transitions such as terminal `Failed`/`Refunded`/`Voided` moving back to active states and `Completed` moving anywhere except `Refunded`.
- Add Billing RowVersion coverage proving payment, financial-account, expense, journal-entry, billing-plan, webhook-delivery, dispute-review, and subscription cancel-at-period-end operations reject missing/stale row versions and compare null database row versions without raw exceptions.
- Add Billing admin post-save concurrency coverage proving payment, financial-account, expense, journal-entry, billing-plan, and payment dispute review updates convert database concurrency exceptions after the initial row-version check into localized admin-safe conflict responses.
- Add order billing boundary coverage proving payments cannot be created directly as `Refunded`/`Voided`, `Completed` payments set paid timestamps and can advance early orders to paid, and refunds are only allowed for `Captured` or `Completed` payments.
- Add WebAdmin catalog soft-delete coverage proving Category/Product delete actions post row versions from the shared confirmation modal, reject missing/stale row versions, and do not silently delete records that were modified after the list was rendered.
- Add WebAdmin Catalog/CMS edit coverage proving Brand, Category, Product, Add-on Group, Menu, and Page edit validators reject missing/empty row versions and handlers compare null database row versions without raw exceptions.
- Add WebAdmin Catalog/CMS post-save concurrency coverage proving Brand, Category, Product, Add-on Group, Menu, Page, Catalog soft-delete, CMS page soft-delete, and Add-on Group product/category/brand/variant attachment operations reject stale row versions and convert database concurrency exceptions after the initial row-version check into localized admin-safe conflict responses.
- Add WebAdmin media delete coverage proving Media soft-delete and unused-asset purge actions reject missing/stale row versions, keep stale list rows from deleting/purging newer media metadata, and surface localized validation/concurrency messages.
- Add WebAdmin identity delete coverage proving Role, Permission, User, and User Address delete actions require non-empty row versions, reject stale row versions, and surface handler validation/concurrency messages instead of swallowing failures.
- Add WebAdmin mobile-operations coverage proving single-device push-token clear and device deactivation require non-empty row versions, reject stale row versions, and surface localized validation/concurrency messages while batch operations remain bounded and filter-scoped.
- Add Identity admin post-save concurrency coverage proving role, permission, user, current-user profile, user address edit/delete/default selection, role-permission assignment, user-role assignment, role delete, permission delete, user delete, push-token clear, and device deactivation operations reject stale row versions where applicable and convert database concurrency exceptions after the initial row-version check into localized admin-safe conflict responses.
- Add SEO redirect-rule admin coverage proving redirect-rule list items expose row versions, update validation rejects missing/empty row versions, update compares row versions null-safely, and soft-delete rejects missing/stale row versions instead of silently deleting modified redirect rules.
- Add Orders RowVersion coverage proving order status changes, shipment carrier exception resolution, and shipment provider operation actions reject missing/stale row versions and compare null database row versions without raw exceptions.
- Add DHL shipment-label queue idempotency coverage proving WebAdmin posts shipment row versions when queuing labels, stale shipment rows are rejected, and concurrent label-queue requests create at most one active pending `ShipmentProviderOperation` through the SQL Server/PostgreSQL pending-operation unique index.
- Add WebAdmin pricing/settings/shipping/media edit coverage proving Promotion, Tax Category, Site Setting, Shipping Method, and Media Asset edits reject missing/empty/stale row versions and compare null database row versions without raw exceptions.
- Add WebAdmin pricing/settings/shipping/media/SEO post-save concurrency coverage proving Promotion, Tax Category, Site Setting, Shipping Method, Media Asset edit/delete/purge, and SEO redirect-rule update/delete operations convert database concurrency exceptions after the initial row-version check into localized admin-safe conflict responses.
- Add Identity role/user assignment coverage proving role-permission and user-role updates reject missing/stale row versions and compare null database row versions without raw exceptions, alongside validator coverage for address, permission, user, and profile row-version payloads.
- Add WebAdmin form-rendering coverage proving edit forms render RowVersion hidden fields as valid Base64 values instead of framework-formatted byte-array placeholders, across catalog, billing, business, CRM, inventory, media, settings, shipping, role/permission, and user-role editors.
- Add WebAdmin controller-boundary coverage proving manually posted invalid Base64 row-version values are decoded through the shared safe decoder and return validation/concurrency failures instead of unhandled `FormatException`.
- Add WebAdmin error-surface coverage proving validation exceptions in operational postbacks use controlled localized fallback or validation messages and do not expose raw exception text through `TempData` or `ModelState`.

### Mobile.Shared tests

- CI installs MAUI workload before running `Darwin.Mobile.Shared.Tests`.
- Local environments should ensure MAUI workload is available when needed.

---

## 8) What to test when adding new features

When implementing a feature, aim for this minimum matrix:

1. **Unit**: core business rule and at least one negative path.
2. **Contracts**: payload shape/serialization compatibility for new/changed DTOs.
3. **WebApi or Integration**: endpoint behavior and auth boundary.
4. **Mobile.Shared** (if affected): client/service behavior for changed routes or payloads.
5. **WebAdmin** (if affected): security headers and auth boundaries for admin routes.

If behavior crosses layers, prefer one extra integration test over many brittle mocks.

---

## 9) Quality gaps and next improvements

Current direction for stronger confidence:

- Grow the new `webadmin` coverage lane from its initial low threshold after it has stable green CI history.
- Increase depth in infrastructure and webapi lanes where currently thinner than unit/integration.
- Expand integration matrices for concurrency/authorization edge-cases.
- Raise lane thresholds gradually after sustained green history.
- Keep this document synchronized with actual test inventory and CI behavior in each related PR.

### Current local verification notes

- `Darwin.WebAdmin.Tests` compiles locally with an isolated output path. The WebAdmin security, public/auth smoke (`27` passed), render smoke (`105` passed), tokenless CSRF (`115` passed), valid-token CSRF (`8` passed), and positive mutation (`1` passed) subsets pass locally when run as split lanes. The tokenless CSRF matrix is slow locally (about 4 minutes), so it remains a separate CI job.
- The focused WebApi provider-callback run now passes 280 of 280 tests when built into isolated artifacts. Two provider-callback worker assertions had their short local waits lengthened so the first background-service iteration can complete consistently.
- The focused unit run for Shipment/Stripe/Billing/Communication/Inventory/Tax/SignIn source-contract and handler coverage now passes 602 of 602 tests after realigning dashboard, communication-provider, billing, DHL, inventory, JWT, and support-queue contracts with the current implementation.

---

## 10) Maintenance rule

When any of the following changes, update this document in the same PR:

- test project inventory,
- coverage lane definitions,
- threshold values,
- required local/CI prerequisites,
- execution commands or workflow structure.

This file should always describe **what is true now**, not only future intent.
# 2026-04-29 Test Backlog Note - Brevo Transactional Email

- Add integration coverage for `Email:Provider=Brevo` using Brevo sandbox mode (`X-Sib-Sandbox=drop`) so the API contract can be validated without sending real emails.
- Add configuration/DI coverage proving `IEmailSender` resolves to Brevo or SMTP based on `Email:Provider`, fails fast for unsupported providers, and fails startup when Brevo is active without required Brevo options.
- Add Worker coverage for queued `EmailDispatchOperation.Provider=Brevo` and legacy `SMTP` rows, verifying status transitions and `ProviderMessageId` capture.
- Add WebApi coverage for the Brevo webhook endpoint: Basic Auth required, oversized payload rejected, invalid payload rejected, duplicate event idempotency, and valid event persisted into `ProviderCallbackInboxMessages`.
  - ✅ Completed in this branch: credential whitespace handling, oversized/invalid payload paths, event normalization (case/whitespace/truncation), and duplicate detection for trimmed message-id/idempotency keys.
  - ✅ Additional coverage added in this pass: whitespace-only credential values fail configuration validation; oversize detection also validated for UTF-8 multi-byte payload bodies; mixed-case JSON field names are accepted (`EvEnT`, `MeSsAgE-iD`, `Ts`, `DATE`).
- Add WebApi coverage for the DHL webhook endpoint: missing/invalid site settings and signature validation.
  - ✅ Completed in this pass: `DhlApiSecret` whitespace treated as not configured, oversized payload detection for UTF-8 multi-byte bodies, and payload that deserializes to `null` returns invalid payload.
- Add WebApi coverage for the Stripe webhook endpoint: configured secret validation and malformed envelope robustness.
  - ✅ Completed in this pass: whitespace `StripeWebhookSecret` is treated as missing, and JSON arrays are rejected as invalid payloads.
- Add WebApi service coverage for `StripeWebhookSignatureVerifier` parser edge-cases.
  - ✅ Completed in this pass: signature header entries can be reordered (`v1` before `t`) while still validating correctly.
- Add WebApi service coverage for ProviderCallbackInboxWriter null/empty idempotency semantics.
  - Completed in this pass: empty and null idempotency key duplicate-check behavior is now tested for both insert and duplicate paths.
- Add Worker coverage for Brevo webhook processing against `EmailDispatchAudit`.
  - ✅ Completed in this pass: delivered/open/click keep successful audit state, hard/soft bounce/spam/blocked/invalid/error mark the audit failed with provider reason (including reason trimming and default missing reason), unsupported events do not overwrite successful state, failed audits do not regress to Sent on delivery events, soft-deleted audits are not matched, and correlation keys are matched through mixed-case alias fields.
