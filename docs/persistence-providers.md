# Darwin Persistence Providers

This document describes the current database-provider architecture for Darwin.

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

## Local PostgreSQL Development

PostgreSQL is the preferred local default.

Start PostgreSQL and pgAdmin:

```powershell
docker compose -f docker-compose.postgres.yml up -d
```

Default local services:

- PostgreSQL: `localhost:5432`
- Database: `darwin_dev`
- User: `darwin`
- pgAdmin: `http://localhost:5050`
- pgAdmin login: `admin@darwin.dev.example.com`

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
- PostgreSQL enables `pg_trgm` and GIN trigram indexes for high-value search columns in catalog, CMS, business, identity, provider callback, and event-log correlation surfaces.
- High-value catalog, CMS, business discovery/list, Billing operator, CRM operator, Inventory operator, Orders/shipment operator, Shipping, Media, Loyalty, Identity user/mobile-device/permission, add-on group, variant lookup, and business/communication operations search paths normalize query terms and searchable columns to lowercase in LINQ, keeping PostgreSQL behavior aligned with SQL Server's common case-insensitive collation behavior while matching the existing `lower(...) gin_trgm_ops` PostgreSQL indexes where provider-specific indexes are present.
- Avoid `EF.Functions.Like` and query-side `Enum.ToString()` or `Guid.ToString()` search logic in provider-neutral application queries. Resolve enum search values before SQL translation and use direct GUID equality when an identifier query parses as a GUID.
- Do not embed moving `DateTime.UtcNow` expressions directly in EF predicates. Snapshot UTC values and cutoff windows in local variables before composing queries so providers receive stable parameters instead of provider-specific translations or repeated moving timestamps.
- For command handlers, prefer one local UTC snapshot for related writes in the same operation, especially expiry, paid-at, due-date, retry, audit-marker, and lifecycle timestamps.
- Keep decimal storage explicit. Darwin stores money as minor-unit integers; rare decimal rates/ratios must use explicit precision. `DarwinDbContext` applies a provider-neutral `decimal(18,4)` fallback only for future decimal properties that do not have entity-specific precision configured.
- PostgreSQL enables `citext` for stable identifiers that must compare case-insensitively, including login identifiers, role/permission keys, slugs, SKUs, billing plan codes, promotion codes, and tax category names.
- PostgreSQL uses `jsonb` plus targeted GIN indexes for selected operational JSON documents that are safe to query as JSON without breaking current text-search paths. Current `jsonb` surfaces include user attribution/external-id JSON, campaign targeting/payload JSON, promotion conditions, subscription metadata, subscription invoice snapshots, site-setting JSON options, business opening hours, analytics export parameters, user-engagement snapshots, and order address/add-on snapshots.
- Do not convert text-searched JSON columns to `jsonb` until their query paths have been migrated away from raw `LIKE` or string `Contains` semantics. Current examples include provider callback payload search, event-log payment-reference search, business admin text override search, and cart-line JSON equality matching.
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
docker exec darwin-postgres psql -U darwin -d darwin_dev -c "select table_schema, table_name from information_schema.tables where table_schema = 'public' order by table_name;"
```

Expected result: only `__EFMigrationsHistory` should remain in `public`.

PostgreSQL extension sanity checks:

```powershell
docker exec darwin-postgres psql -U darwin -d darwin_dev -c "select extname from pg_extension where extname in ('pg_trgm', 'citext') order by extname;"
docker exec darwin-postgres psql -U darwin -d darwin_dev -c "select table_schema, table_name, column_name from information_schema.columns where udt_name = 'citext' order by table_schema, table_name, column_name;"
docker exec darwin-postgres psql -U darwin -d darwin_dev -c "select table_schema, table_name, column_name from information_schema.columns where udt_name = 'jsonb' order by table_schema, table_name, column_name;"
docker exec darwin-postgres psql -U darwin -d darwin_dev -c "select schemaname, indexname from pg_indexes where indexname like 'IX_PG_%_JsonbGin' order by schemaname, indexname;"
docker exec darwin-postgres psql -U darwin -d darwin_dev -c "select schemaname, indexname from pg_indexes where indexname in ('IX_PG_EventLogs_PropertiesJson_Trgm', 'IX_PG_ProviderCallbackInboxMessages_PayloadJson_Trgm', 'IX_PG_Businesses_AdminTextOverridesJson_Trgm') order by schemaname, indexname;"
docker exec darwin-postgres psql -U darwin -d darwin_dev -c "select n.nspname as schema, c.relname as table_name, con.conname, con.convalidated from pg_constraint con join pg_class c on c.oid = con.conrelid join pg_namespace n on n.oid = c.relnamespace where con.conname like 'CK_PG_%_ValidJson' order by n.nspname, c.relname, con.conname;"
```

Expected current PostgreSQL JSON baseline: 21 `jsonb` columns and 14 `IX_PG_*_JsonbGin` indexes after all PostgreSQL migrations are applied.
Expected current text-JSON search baseline: 3 targeted `IX_PG_*_Trgm` indexes for JSON columns that intentionally remain text-backed.
Expected current text-JSON validity baseline: 11 `CK_PG_*_ValidJson` constraints, initially `NOT VALID`.
