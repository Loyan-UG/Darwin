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

The existing SQL Server migrations currently remain in `Darwin.Infrastructure.Migrations` for compatibility with the established migration lane. The runtime registration now has a clearer home in `Darwin.Infrastructure.SqlServer`.

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
- PostgreSQL enables `pg_trgm` and GIN trigram indexes for high-value search columns in catalog, CMS, business, and identity surfaces.

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
