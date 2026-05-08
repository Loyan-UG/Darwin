# Darwin Persistence Providers

This document describes the current database-provider architecture for Darwin.

For migration execution details and PostgreSQL post-migration checks, see `docs/postgresql-migration-runbook.md`.
For current go-live application/provider gaps, see `docs/go-live-status.md`.

## Current Provider Model

Darwin uses one shared EF Core model and seed pipeline, with provider-specific projects for runtime registration and migrations:

- `Darwin.Infrastructure`: shared persistence core. It contains `DarwinDbContext`, entity configurations, seed sections, the `IAppDbContext` binding helper, and startup migration/seed orchestration.
- `Darwin.Infrastructure.PostgreSql`: PostgreSQL provider project. It registers Npgsql, owns PostgreSQL migrations, and is the default local/provider choice.
- `Darwin.Infrastructure.SqlServer`: SQL Server provider project. It registers Microsoft SQL Server and points at the existing SQL Server migration lane.
- `Darwin.Infrastructure.PersistenceProviders`: neutral composition helper that reads `Persistence:Provider` and selects one of the provider projects. Provider projects must not depend on each other.

The entry points (`Darwin.WebAdmin`, `Darwin.WebApi`, and `Darwin.Worker`) select the provider through:

```json
{
  "Persistence": {
    "Provider": "PostgreSql"
  }
}
```

Supported provider names:

- `PostgreSql`, `Postgres`, `Npgsql`
- `SqlServer`, `MSSQL`

Provider selection is normalized during startup. Empty or missing `Persistence:Provider` defaults to `PostgreSql`; recognized aliases are converted to the canonical provider names before provider-specific registration runs. Unsupported values fail fast during DI registration.

## Local PostgreSQL Development

PostgreSQL is the preferred local default.

Start PostgreSQL and pgAdmin:

```powershell
docker compose -f docker-compose.postgres.yml up -d
```

Default local services:

- PostgreSQL: `localhost:5432`
- Database: `darwin_dev`
- User: `darwin_app`
- pgAdmin: `http://localhost:5050`
- pgAdmin login: value from local `.env` (`DARWIN_PGADMIN_EMAIL`)

The development connection string is named `ConnectionStrings:PostgreSql`.

The PostgreSQL provider normalizes runtime and design-time Npgsql connection strings with conservative production-friendly defaults:

- `Application Name=Darwin` for server-side connection visibility.
- `Max Auto Prepare=100` and `Auto Prepare Min Usages=2` for repeated-query prepared statement reuse.
- `Keepalive=30`, `Timeout=15`, and `Command Timeout=60` unless the caller explicitly overrides them.
- EF Core Npgsql retry policy is capped at 5 retries with a 10-second maximum delay.

Apply PostgreSQL migrations:

```powershell
dotnet ef database update `
  --project src\Darwin.Infrastructure.PostgreSql\Darwin.Infrastructure.PostgreSql.csproj `
  --startup-project src\Darwin.WebAdmin\Darwin.WebAdmin.csproj `
  --context Darwin.Infrastructure.Persistence.Db.DarwinDbContext
```

Check whether the PostgreSQL model has pending changes:

```powershell
dotnet ef migrations has-pending-model-changes `
  --project src\Darwin.Infrastructure.PostgreSql\Darwin.Infrastructure.PostgreSql.csproj `
  --startup-project src\Darwin.WebAdmin\Darwin.WebAdmin.csproj `
  --context Darwin.Infrastructure.Persistence.Db.DarwinDbContext
```

## SQL Server Lane

SQL Server remains supported for customers that require it.

Set:

```json
{
  "Persistence": {
    "Provider": "SqlServer"
  }
}
```

Use either `ConnectionStrings:SqlServer` or the legacy `ConnectionStrings:DefaultConnection`.

The SQL Server migration lane now lives in `Darwin.Infrastructure.SqlServer.Migrations`. The shared `Darwin.Infrastructure` project intentionally contains no provider-specific migrations or SQL Server registration.

When running EF commands for the SQL Server lane through an entry point whose default provider is PostgreSQL, set the provider explicitly:

```powershell
$env:Persistence__Provider = "SqlServer"
dotnet ef migrations list `
  --project src\Darwin.Infrastructure.SqlServer\Darwin.Infrastructure.SqlServer.csproj `
  --startup-project src\Darwin.WebAdmin\Darwin.WebAdmin.csproj `
  --context Darwin.Infrastructure.Persistence.Db.DarwinDbContext
Remove-Item Env:\Persistence__Provider
```

## Provider-Specific Rules

- Do not duplicate entity mappings between providers. Shared mappings belong in `Darwin.Infrastructure`.
- Provider projects should only own provider registration, provider-specific design-time factories, and provider-specific migrations.
- Keep provider selection in `Darwin.Infrastructure.PersistenceProviders`; do not make `Darwin.Infrastructure.PostgreSql` depend on `Darwin.Infrastructure.SqlServer` or the reverse.
- Application tables must use explicit module schemas. Do not allow new tables to fall into provider defaults such as SQL Server `dbo` or PostgreSQL `public`.
- Current module schemas are `Billing`, `Businesses`, `CartCheckout`, `Catalog`, `CMS`, `CRM`, `Identity`, `Integration`, `Inventory`, `Loyalty`, `Marketing`, `Orders`, `Pricing`, `SEO`, `Settings`, and `Shipping`.
- PostgreSQL `public` should only contain provider/EF metadata such as `__EFMigrationsHistory`, not application tables.
- Avoid hard-coded provider SQL types in shared mappings. Let EF choose native types unless a provider-specific project intentionally overrides behavior.
- SQL Server keeps native `rowversion` concurrency semantics.
- PostgreSQL uses client-managed `bytea` concurrency bytes for the shared `RowVersion` property.
- SQL Server filtered-index syntax is normalized for PostgreSQL when the active provider is Npgsql.
- PostgreSQL enables `pg_trgm` and GIN trigram indexes for high-value search columns in catalog, CMS, business, identity, integration, CRM, orders, shipping, and loyalty surfaces.
- High-value catalog, CMS, business discovery/list, Billing operator, CRM operator, Inventory operator, Orders/shipment operator, Shipping, Media, Loyalty, Identity user/mobile-device/permission, add-on group, variant lookup, and business/communication operations search paths use provider-neutral escaped `EF.Functions.Like` patterns for substring semantics. PostgreSQL backs those paths with direct-column `gin_trgm_ops` indexes instead of query-side `lower(...)`, so the application query shape remains portable while PostgreSQL keeps native substring-search acceleration. Fixed operational categories should instead use canonical stored values and direct equality, such as media asset roles, DHL carrier markers, promotion codes, and tax category names.
- Use `EF.Functions.Like` only through escaped `QueryLikePattern` helpers for provider-neutral substring search. Avoid ad-hoc/unescaped `LIKE`, query-side `Enum.ToString()`, and query-side `Guid.ToString()` search logic in provider-neutral application queries. Resolve enum search values before SQL translation and use direct GUID equality when an identifier query parses as a GUID.
- Do not embed moving `DateTime.UtcNow` expressions directly in EF predicates. Snapshot UTC values and cutoff windows in local variables before composing queries so providers receive stable parameters instead of provider-specific translations or repeated moving timestamps.
- For command handlers, prefer one local UTC snapshot for related writes in the same operation, especially expiry, paid-at, due-date, retry, audit-marker, and lifecycle timestamps.
- Keep decimal storage explicit. Darwin stores money as minor-unit integers; rare decimal rates/ratios must use explicit precision. `DarwinDbContext` applies a provider-neutral `decimal(18,4)` fallback only for future decimal properties that do not have entity-specific precision configured.
- PostgreSQL enables `citext` for stable identifiers that must compare case-insensitively, including login identifiers, role/permission keys, slugs, SKUs, billing plan codes, promotion codes, and tax category names.
- PostgreSQL uses `jsonb` plus targeted GIN indexes for selected operational JSON documents that are safe to query as JSON without breaking current text-search paths. Current `jsonb` surfaces include user attribution/external-id JSON, campaign targeting/payload JSON, promotion conditions, subscription metadata, subscription invoice snapshots, site-setting JSON options, business opening hours, analytics export parameters, user-engagement snapshots, and order address/add-on snapshots.
- PostgreSQL `jsonb` conversion migrations use a temporary safe parse function for legacy text JSON. Empty or invalid existing values fall back to the migration's column-specific safe default (`{}`, `[]`, or `NULL`) so customer databases can migrate, while later validity constraints prevent new invalid JSON in text-backed columns.
- Do not convert text-searched JSON columns to `jsonb` until their query paths have been migrated away from raw `LIKE` or string `Contains` semantics. Current examples include provider callback payload search and business admin text override search. Event-log payment-reference lookup still uses PostgreSQL trigram search to find candidate Stripe webhook payloads, but final correlation now uses JSON-aware value matching in application code. Cart-line add-on JSON remains text-backed for equality matching, but application writes and request matching now canonicalize add-on IDs to distinct sorted GUID JSON before comparison.
- Admin text override JSON remains text-backed because business discovery still uses text search over the override document. Parsing and validation now go through the shared `AdminTextOverrideJsonCatalog`, so WebAdmin, WebApi-facing public business text resolution, business edits, and site-setting edits agree on the same structure while PostgreSQL keeps trigram acceleration and JSON validity constraints.
- Provider callback payload JSON remains text-backed so WebAdmin operator search can continue using the existing trigram index. Public Stripe/DHL webhook ingress now enforces a 256 KiB raw-payload cap before persisting `ProviderCallbackInboxMessages.PayloadJson`; WebAdmin list previews summarize Stripe/DHL payloads into operational fields and redact obvious sensitive keys for unknown providers.
- Provider callback operational views use direct equality filters for provider, status, and callback type. Shared mapping now includes `IX_ProviderCallbackInboxMessages_Provider_CallbackType_CreatedAtUtc` in both provider lanes so Brevo delivery-failure and recent-event summaries stay efficient without provider-specific query shapes.
- Queue status vocabularies must stay stable across worker writers and WebAdmin operator queries. Provider callback inbox and shipment provider operation queues use `Pending`, `Failed`, and `Processed`; read/query paths intentionally treat legacy `Succeeded` rows as processed for compatibility.
- PostgreSQL still accelerates selected text-searched JSON fields with trigram indexes while preserving current query behavior. Current examples are `Integration.EventLogs.PropertiesJson`, `Integration.ProviderCallbackInboxMessages.PayloadJson`, and `Businesses.Businesses.AdminTextOverridesJson`.
- PostgreSQL protects remaining text-backed JSON columns with `NOT VALID` check constraints backed by `public.darwin_is_valid_jsonb(text)`. This preserves low-risk migration behavior for existing databases while rejecting invalid JSON on new or updated rows.
- PostgreSQL connection behavior is normalized in `Darwin.Infrastructure.PostgreSql` so runtime startup and EF design-time migrations share the same Npgsql timeout, retry, keepalive, and auto-prepare defaults.
- Do not add new case-sensitive `string.Contains` search paths without deciding whether the behavior should be provider-neutral. Prefer explicit normalization or a documented provider-specific search strategy.

## Required Verification For Provider Changes

Run at least:

```powershell
dotnet build src\Darwin.WebAdmin\Darwin.WebAdmin.csproj --no-restore
dotnet build src\Darwin.WebApi\Darwin.WebApi.csproj --no-restore
dotnet build src\Darwin.Worker\Darwin.Worker.csproj --no-restore
```

For PostgreSQL schema changes, also run:

```powershell
dotnet ef migrations has-pending-model-changes `
  --project src\Darwin.Infrastructure.PostgreSql\Darwin.Infrastructure.PostgreSql.csproj `
  --startup-project src\Darwin.WebAdmin\Darwin.WebAdmin.csproj `
  --context Darwin.Infrastructure.Persistence.Db.DarwinDbContext
```

Before calling provider changes production-ready, validate migration application and startup seed on a fresh database.

Schema sanity check for PostgreSQL:

```powershell
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select table_schema, table_name from information_schema.tables where table_schema = 'public' order by table_name;"
```

Expected result: only `__EFMigrationsHistory` should remain in `public`.

PostgreSQL extension sanity checks:

```powershell
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select extname from pg_extension where extname in ('pg_trgm', 'citext') order by extname;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select table_schema, table_name, column_name from information_schema.columns where udt_name = 'citext' order by table_schema, table_name, column_name;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select table_schema, table_name, column_name from information_schema.columns where udt_name = 'jsonb' order by table_schema, table_name, column_name;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select schemaname, indexname from pg_indexes where indexname like 'IX_PG_%_JsonbGin' order by schemaname, indexname;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select schemaname, indexname from pg_indexes where indexname in ('IX_PG_EventLogs_PropertiesJson_Trgm', 'IX_PG_ProviderCallbackInboxMessages_PayloadJson_Trgm', 'IX_PG_Businesses_AdminTextOverridesJson_Trgm') order by schemaname, indexname;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select schemaname, count(*) as index_count from pg_indexes where indexname like 'IX_PG_%_Like_Trgm' group by schemaname order by schemaname;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select n.nspname as schema, c.relname as table_name, con.conname, con.convalidated from pg_constraint con join pg_class c on c.oid = con.conrelid join pg_namespace n on n.oid = c.relnamespace where con.conname like 'CK_PG_%_ValidJson' order by n.nspname, c.relname, con.conname;"
```

Expected current PostgreSQL JSON baseline: 21 `jsonb` columns and 14 `IX_PG_*_JsonbGin` indexes after all PostgreSQL migrations are applied.
Expected current text-JSON search baseline: 3 targeted `IX_PG_*_Trgm` indexes for JSON columns that intentionally remain text-backed.
Expected current direct `LIKE` search baseline: 88 `IX_PG_*_Like_Trgm` indexes across `Billing`, `Businesses`, `CMS`, `CRM`, `Catalog`, `Identity`, `Integration`, `Inventory`, `Loyalty`, `Orders`, and `Shipping`.
Expected current text-JSON validity baseline: 11 `CK_PG_*_ValidJson` constraints, initially `NOT VALID`.

Latest local validation result: `darwin_dev` on Docker PostgreSQL matched this baseline, including only `public.__EFMigrationsHistory` in `public`, no PostgreSQL `dbo` tables, both `citext` and `pg_trgm` installed, 21 `jsonb` columns, 14 JSONB GIN indexes, 3 targeted text-JSON trigram indexes, 88 direct `LIKE` trigram indexes, and 11 unvalidated text-JSON validity constraints.

Latest local text-backed JSON data check: `darwin_dev` reports zero invalid rows for all 11 columns protected by `CK_PG_*_ValidJson`. The constraints remain `NOT VALID` by migration design so customer databases can run validation explicitly during a suitable maintenance window after confirming their own data.

Fresh-bootstrap validation result: applying the full PostgreSQL migration lane to a newly created Docker database (`darwin_operational_like_validation`) completed successfully and matched the same baseline, including 88 direct `LIKE` trigram indexes across 11 schemas. The validation database was dropped after the check.

Runtime startup validation result: `Darwin.WebAdmin` started against the local PostgreSQL provider and returned HTTP 200 from `/`. `Darwin.WebApi` started against the same provider and returned HTTP 200 from `/api/v1/public/businesses/category-kinds`. `Darwin.Worker` also started against the same Development/PostgreSQL configuration, built its service provider successfully, and executed PostgreSQL-backed background queries during a short smoke run.

Latest Development runtime smoke result: direct DLL startup for `Darwin.WebAdmin` and `Darwin.WebApi` against PostgreSQL returned HTTP 200 from the same smoke endpoints. `Darwin.Worker` Development defaults now keep queue/dispatch workers that can mutate queues or call external systems disabled unless explicitly overridden, and a short PostgreSQL smoke run started the host without queue-query or outbound-HTTP side-effect logs.

Latest Development outbound-safety validation result: `Darwin.WebAdmin` and `Darwin.WebApi` Development settings now override SMTP to `localhost:2525` with empty credentials so local runtime paths do not inherit the production-like Office365 placeholder relay. Runtime email delivery is currently SMTP-only through `SmtpEmailSender`, so unused Development `Email:Provider`, Mailgun, and Graph placeholders were removed to keep configuration aligned with the actual composition root. `Darwin.WebApi` Development settings also disable placeholder FCM/APNS provider settings unless explicitly overridden. Both entry points still start against PostgreSQL and return HTTP 200 from the smoke endpoints after the override.

Latest queue-status validation result: `Darwin.Application`, `Darwin.Worker`, and `Darwin.WebAdmin` build successfully after aligning provider callback inbox and shipment provider operation successful completion to `Processed`; local PostgreSQL `darwin_dev` currently has no rows in those two queue tables, so no development data rewrite was needed.

Latest admin-text override validation result: `Darwin.Application`, `Darwin.WebAdmin`, and `Darwin.WebApi` build successfully after centralizing override JSON parsing. Local PostgreSQL `darwin_dev` currently has 10 business override documents and no site-setting override document; the sampled seed documents follow the expected `de-DE`/`en-US` object-of-string-values shape.

Latest provider webhook validation result: `Darwin.WebApi` builds successfully after adding bounded raw payload reading for public Stripe/DHL webhook endpoints. The provider callback payload remains text-backed and searchable; oversize payloads are rejected at the HTTP boundary with 413 before persistence.

Latest provider callback preview validation result: `Darwin.Application` and `Darwin.WebAdmin` build successfully after replacing raw provider callback list previews with JSON-aware Stripe/DHL summaries plus sensitive-key redaction for unknown providers. Local PostgreSQL `darwin_dev` currently has no provider callback inbox rows to sample.

Latest provider callback operational-index validation result: `AddProviderCallbackInboxOperationalIndexes` was generated for both `Darwin.Infrastructure.PostgreSql` and `Darwin.Infrastructure.SqlServer`. Brevo callback ingress now stores a bounded SHA-256 idempotency key derived from event name, provider message id, and provider timestamp instead of truncating the raw compound key. The PostgreSQL migration was applied to local `darwin_dev`, `dotnet ef migrations list` reports no pending PostgreSQL migrations, and `Darwin.WebAdmin` starts against the PostgreSQL provider and returns the expected authenticated redirect from `/`.

Latest restricted-runtime role validation result: `DatabaseStartup:ApplyMigrations=false` and `DatabaseStartup:Seed=false` now allow Development hosts to run against the restricted PostgreSQL `darwin_app` role after migrations/seed are applied by an owner-capable role. `Darwin.WebAdmin` returned the expected authenticated `302` from `/BusinessCommunications/ProviderCallbacks?provider=Brevo&deliveryFailureOnly=true`, and `Darwin.WebApi` returned HTTP `200` from `/api/v1/public/businesses/category-kinds` using the same restricted runtime role. `Darwin.Worker` also started with the restricted role; Development defaults kept email, channel, provider-callback, shipment-provider, inactive-reminder, and webhook-delivery workers disabled, and each worker now logs its disabled state explicitly during smoke runs.

Latest Brevo webhook smoke result: `Darwin.WebApi` accepted a Brevo `hard_bounce` payload through `/api/v1/public/notifications/brevo/webhooks` with Basic Auth configured through environment variables and the restricted PostgreSQL runtime role. Reposting the same payload returned `duplicate=true`, only one `Integration.ProviderCallbackInboxMessages` row was persisted, and the temporary smoke row was removed after validation.

Latest Brevo provider-callback worker smoke result: `Darwin.Worker` processed a temporary Brevo `hard_bounce` inbox row with only `ProviderCallbackWorker` enabled. The matching `Integration.EmailDispatchAudits` row moved from `Sent` to `Failed` with the provider reason, the inbox row moved to `Processed`, and the temporary audit/inbox rows were removed after validation.

Latest Stripe provider-callback worker smoke result: `Darwin.Worker` processed a temporary Stripe `payment_intent.succeeded` inbox row with only `ProviderCallbackWorker` enabled. The inbox row moved to `Processed`, one `Integration.EventLogs` row was written with the Stripe event id as idempotency key, and the temporary inbox/event-log rows were removed after validation.

Latest DHL provider-callback worker smoke result: `Darwin.Worker` processed a temporary DHL `shipment.delivered` inbox row with only `ProviderCallbackWorker` enabled. The matching shipment moved to `Delivered`, tracking metadata, label URL, `LastCarrierEventKey`, and one `Shipping.ShipmentCarrierEvents` row were written, the inbox row moved to `Processed`, and all temporary smoke rows were removed after validation.

Migration-script audit result: idempotent scripts generated successfully for both provider lanes. The PostgreSQL script was clean for unwanted `dbo` references and application table creation in `public`. The SQL Server script still contains historical unqualified table creation in old migrations, but the current lane includes 9 explicit `ALTER SCHEMA ... TRANSFER` moves that align those legacy tables into `Integration`, `Orders`, `Catalog`, and `Shipping`; the latest SQL Server and PostgreSQL model snapshots have no unqualified `ToTable(...)` mappings.

Latest migration freshness validation result: applying the full PostgreSQL migration lane to a fresh Docker database (`darwin_migration_validation_20260429`) and the full SQL Server migration lane to a fresh `sqlpreview` database (`Darwin_MigrationValidation_20260429`) completed successfully. Both provider databases contained the new active-operation/audit/shipping uniqueness indexes (`UX_ShipmentProviderOperations_ActivePending`, `UX_EmailDispatchAudits_ActiveCorrelation`, `UX_ChannelDispatchAudits_ActiveChannelCorrelation`, and `UX_ShippingMethods_ActiveCarrierService`), retained only migration history in the provider-default schema, and were dropped after validation.

Latest migration metadata audit result: EF discovers the provider-specific idempotency/uniqueness migrations (`20260429113000_EnforcePendingShipmentProviderOperationIdempotency`, `20260429114500_EnforceNotificationAuditIdempotency`, and `20260429120500_EnforceShippingMethodCarrierServiceUniqueness`) in both SQL Server and PostgreSQL lanes. Both provider projects build, both lanes report no pending model changes, and both idempotent scripts include the new migration history rows and provider-specific filtered/partial unique indexes.

SQL Server fresh-bootstrap validation result: applying the full SQL Server migration lane to a newly created `Darwin_FreshValidation` database on the local `sqlpreview` container completed successfully. Post-migration checks showed no application tables in `dbo`, all 16 module schemas present, and the 9 historically unqualified tables located in their final module schemas. The validation database was dropped after the check.

Development Data Protection note: Worker and web entry points use shared Data Protection registration so identity/secret services can resolve during startup. Current local development settings point WebAdmin, WebApi, and Worker at `E:\_Projects\Darwin\_shared_keys` with `DataProtection:ApplicationName=Darwin` so cookies/tokens protected by shared infrastructure remain readable across entry points that intentionally share keys.

Production Data Protection note: production settings should point `DataProtection:KeysPath` at a durable shared location. Certificate-based key encryption is supported through `DataProtection:CertificateThumbprint`, `DataProtection:CertificateStoreName` (default `My`), and `DataProtection:CertificateStoreLocation` (default `CurrentUser`). Startup now fails when a configured thumbprint cannot be resolved to a currently valid certificate with a private key, and `DataProtection:RequireKeyEncryption=true` also fails startup when no thumbprint is configured. Grant the app identity read access to the certificate private key and read/write access to the shared key-ring directory.

EF tooling alignment validation result: the local global `dotnet-ef` tool was aligned to `10.0.6`. `has-pending-model-changes` reports no pending model changes for both `Darwin.Infrastructure.SqlServer` and `Darwin.Infrastructure.PostgreSql`, and idempotent migration scripts generate successfully for both provider lanes without EF tooling version mismatch warnings.
