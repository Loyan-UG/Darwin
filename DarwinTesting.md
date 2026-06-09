# Darwin Testing Guide

Reviewed: 2026-06-08

This document defines the active testing strategy. Historical test-run logs belong in [docs/implementation-ledger.md](docs/implementation-ledger.md). Code-backed readiness status belongs in [docs/go-live-status.md](docs/go-live-status.md).

## Principles

- Prefer focused tests around the behavior being changed.
- Keep source-contract tests stable: assert security, route, contract, localization, mutation, and provider-boundary invariants rather than exact layout copy.
- External provider calls must be opt-in and guarded.
- Provider smoke scripts must not print secrets, raw provider payloads, access keys, webhook secrets, or provider references.
- Tests must not weaken security, immutability, webhook-authoritative payment behavior, VIES failure policy, or provider-boundary rules to pass.
- Use hosted WebAdmin/WebApi tests for operator or API behavior; use unit tests for low-level invariants.

## Core Lanes

### Application and Domain

Use for handlers, validators, policies, source contracts, and provider-neutral abstractions.

```powershell
dotnet test tests\Darwin.Tests.Unit\Darwin.Tests.Unit.csproj --no-restore /p:UseSharedCompilation=false
```

Focused examples:

```powershell
dotnet test tests\Darwin.Tests.Unit\Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~Storage|FullyQualifiedName~Archive|FullyQualifiedName~Invoice|FullyQualifiedName~SourceContract" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.Tests.Unit\Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~Brevo|FullyQualifiedName~Communication|FullyQualifiedName~EmailDispatch|FullyQualifiedName~ProviderCallbackInbox" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.Tests.Unit\Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~Billing|FullyQualifiedName~Stripe|FullyQualifiedName~Subscription|FullyQualifiedName~Refund|FullyQualifiedName~Dispute" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.Tests.Unit\Darwin.Tests.Unit.csproj --filter "FullyQualifiedName~LoginWithExternalProviderHandlerTests" --no-restore /p:UseSharedCompilation=false
```

### Infrastructure

Use for provider adapters, storage, persistence behavior, and external-command adapter contracts.

```powershell
dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --no-restore /p:UseSharedCompilation=false
```

Focused examples:

```powershell
dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Storage|FullyQualifiedName~Archive" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Notifications|FullyQualifiedName~Brevo" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.Infrastructure.Tests\Darwin.Infrastructure.Tests.csproj --filter "FullyQualifiedName~ExternalCommandEInvoiceGenerationServiceTests|FullyQualifiedName~Compliance" --no-restore /p:UseSharedCompilation=false
```

### WebApi

Use for public/member/business/admin/provider API behavior, route boundaries, provider webhooks, and callback workers.

```powershell
dotnet test tests\Darwin.WebApi.Tests\Darwin.WebApi.Tests.csproj --no-restore /p:UseSharedCompilation=false
```

Focused examples:

```powershell
dotnet test tests\Darwin.WebApi.Tests\Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~Stripe|FullyQualifiedName~ProviderCallback|FullyQualifiedName~Webhook" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.WebApi.Tests\Darwin.WebApi.Tests.csproj --filter "FullyQualifiedName~Brevo|FullyQualifiedName~EmailDispatchOperationBackgroundService|FullyQualifiedName~ProviderCallback" --no-restore /p:UseSharedCompilation=false
```

### WebAdmin

Use for MVC/Razor/HTMX operator flows, anti-forgery, row-version mutation safety, localization, and support surfaces.

```powershell
dotnet test tests\Darwin.WebAdmin.Tests\Darwin.WebAdmin.Tests.csproj --no-restore /p:UseSharedCompilation=false
```

Focused examples:

```powershell
dotnet test tests\Darwin.WebAdmin.Tests\Darwin.WebAdmin.Tests.csproj --filter "FullyQualifiedName~Business|FullyQualifiedName~Onboarding|FullyQualifiedName~Invitation" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.WebAdmin.Tests\Darwin.WebAdmin.Tests.csproj --filter "FullyQualifiedName~InvoiceEInvoiceDownload" --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.WebAdmin.Tests\Darwin.WebAdmin.Tests.csproj --filter "FullyQualifiedName~Shipment|FullyQualifiedName~Return|FullyQualifiedName~ProviderOperation|FullyQualifiedName~Dhl" --no-restore /p:UseSharedCompilation=false
```

For routine local work, prefer the segmented WebAdmin lane runner instead of a broad `Security|Render` filter. The broad filter can select a very large hosted smoke set and is prone to local timeout or leftover process locks.
Run these lanes sequentially rather than in parallel because hosted WebAdmin tests share generated MVC test-manifest outputs.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-webadmin-focused.ps1 -Lane CoreSecurity -NoRestore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-webadmin-focused.ps1 -Lane RenderLists -NoRestore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-webadmin-focused.ps1 -Lane RenderEditors -NoRestore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-webadmin-focused.ps1 -Lane BillingProvider -NoRestore
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\test-webadmin-focused.ps1 -Lane ShippingProvider -NoRestore
```

### Web Front Office

`Darwin.Web` is a Next.js/React project that is included in `Darwin.sln` through `src\Darwin.Web\Darwin.Web.esproj` so Visual Studio can show and manage the project alongside the .NET projects. Use the Node tooling as the source of truth for frontend validation; the solution project is a Visual Studio integration surface, not a .NET application project.

```powershell
pushd src\Darwin.Web
npm test
npm audit --audit-level=moderate
npm run build
popd
```

Do not commit `.env.local`, `.next`, `node_modules`, generated build output, or local smoke artifacts. Front-office payment completion remains webhook-authoritative; browser return routes must not mark provider payments successful by themselves.

### Mobile

Use for route constants, API clients, service behavior, release guards, resources, and ViewModel behavior where testable.

```powershell
dotnet test tests\Darwin.Mobile.Shared.Tests\Darwin.Mobile.Shared.Tests.csproj --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.Mobile.Consumer.Tests\Darwin.Mobile.Consumer.Tests.csproj --no-restore /p:UseSharedCompilation=false
dotnet test tests\Darwin.Mobile.Business.Tests\Darwin.Mobile.Business.Tests.csproj --no-restore /p:UseSharedCompilation=false
```

External-login route guards belong in `Darwin.Mobile.Shared.Tests`. Google provider verification is covered server-side through the Application handler tests and should not require real Google calls in normal CI. `Darwin.Web` covers the Google Identity Services sign-in surface with source-contract rendering tests; real Google browser smoke remains deployment-configured.

Build validation examples:

```powershell
dotnet build src\Darwin.Mobile.Shared\Darwin.Mobile.Shared.csproj -f net10.0-windows10.0.19041.0 -c Release --no-restore /p:UseSharedCompilation=false
dotnet build src\Darwin.Mobile.Consumer\Darwin.Mobile.Consumer.csproj -f net10.0-windows10.0.19041.0 -c Release --no-restore /p:UseSharedCompilation=false
dotnet build src\Darwin.Mobile.Business\Darwin.Mobile.Business.csproj -f net10.0-windows10.0.19041.0 -c Release --no-restore /p:UseSharedCompilation=false
```

## Provider Smoke

Run dry-runs before any provider execution:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-go-live-readiness.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-secrets.ps1
```

Provider scripts are documented in [docs/external-smoke-inputs.md](docs/external-smoke-inputs.md).
The aggregate go-live dry-run uses DB-backed Brevo Site Settings when available, but still treats webhook, worker, and delivery-pipeline confirmations as explicit operator prerequisites.

Smoke categories:

- Stripe test-mode and live-readiness preflight.
- Brevo sandbox/drop and controlled-inbox readiness.
- DHL live account validation after complete account data is available.
- VIES valid/invalid/provider-failure policy. Controlled live smoke passed on 2026-05-27 and should be repeated after provider, retry, or policy changes.
- Object storage and MinIO local/production-readiness checks.
- E-invoice external-command adapter smoke. Local XRechnung XML and ZUGFeRD/Factur-X PDF artifact-shape smoke passed through the Mustangproject wrapper with `RequireValidationReport` on 2026-05-27; this is not legal validation.
- E-invoice validation-report parser fixtures are documented in `docs/e-invoice-validation-fixtures.md`. These fixtures verify adapter behavior only and must not be treated as legal-approved compliance evidence.

## Current Priority Coverage

Keep these areas green before adding lower-risk coverage:

- Payment and subscription checkout must stay provider-hosted and webhook-authoritative.
- Stripe storefront handoff smoke should confirm the checkout URL is Stripe-hosted and the return route leaves payment state pending until verified webhooks finalize it.
- Stripe public-webhook finalized test-mode smoke passed on 2026-05-27. Keep the browser checkout, provider callback worker processing, and captured-payment front-office confirmation behavior guarded after payment or confirmation changes.
- Stripe refunds, disputes, and webhook callbacks must stay idempotent and visible to operators.
- DHL must never generate fake labels, references, or tracking values.
- Brevo secrets must stay masked and preserve-on-blank.
- VIES provider failure must remain `Unknown`/manual review, with retry and operator format hints never turning format plausibility into official validity.
- Object-storage production immutability must not be claimed without provider-level validation.
- E-invoice artifacts must not be described as compliant until legal validation and production smoke pass.
- WebAdmin row-version mutation posts must remain anti-forgery protected and concurrency-safe.
- Mobile Release must reject broad cleartext traffic and unsafe certificate trust.

## Known Gaps

- Stripe live-mode smoke and monitoring sign-off.
- DHL live account and return-label smoke.
- VIES production monitoring ownership and periodic controlled smoke.
- Production object-storage validation against the final provider.
- E-invoice legal validation fixtures and production artifact smoke.
- Signed Android/iOS/MacCatalyst release artifacts and device/provider smoke.
- Native Google sign-in UI/device smoke after deployment OAuth client IDs are configured.
- Web Google Identity Services smoke after a Web OAuth client ID is configured.
- Physical camera QR end-to-end validation.
- Broader Consumer and Business ViewModel/UI coverage.
- Mobile SQLite outbox activation policy and processor tests if offline mutations are enabled later.

## Repository Checks

Run before handing off a documentation or provider-readiness slice:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-secrets.ps1
git diff --check
```

Run broader build/test verification when production code or source-contract guards changed.
