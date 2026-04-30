# PostgreSQL Migration Runbook

This runbook covers the current PostgreSQL provider migration posture for Darwin.

## Baseline

- PostgreSQL is the preferred local/default provider.
- SQL Server remains supported through `Darwin.Infrastructure.SqlServer`.
- PostgreSQL migrations live in `Darwin.Infrastructure.PostgreSql`.
- SQL Server migrations live in `Darwin.Infrastructure.SqlServer`.
- Shared mappings stay in `Darwin.Infrastructure`.

## Before Running PostgreSQL Migrations

1. Back up the target database.
2. Confirm the target connection string points at the intended database.
3. Confirm the migration connection uses a migration owner role, not the runtime app role.
4. Confirm extensions can be created by the migration user:
   - `citext`
   - `pg_trgm`
5. Run a pending-model check from the same application version:

```powershell
dotnet ef migrations has-pending-model-changes `
  --project src\Darwin.Infrastructure.PostgreSql\Darwin.Infrastructure.PostgreSql.csproj `
  --startup-project src\Darwin.WebAdmin\Darwin.WebAdmin.csproj `
  --context Darwin.Infrastructure.Persistence.Db.DarwinDbContext
```

## Production Role Model

Use separate PostgreSQL roles for migration and runtime:

- `darwin_owner`: migration/schema owner role. Use this only for EF migration application and controlled maintenance.
- `darwin_app`: runtime application role. Use this for WebAdmin, WebApi, and Worker connection strings.

Recommended provisioning shape, with placeholders replaced outside source control:

```sql
create database darwin;

create role darwin_owner login password 'REPLACE_WITH_OWNER_PASSWORD';
create role darwin_app login password 'REPLACE_WITH_APP_PASSWORD';

grant connect on database darwin to darwin_owner, darwin_app;
grant create on database darwin to darwin_owner;
```

Run migrations as `darwin_owner`. After migrations and seed complete, grant runtime access to the explicit module schemas:

```sql
grant usage on schema "Billing", "Businesses", "CartCheckout", "Catalog", "CMS", "CRM", "Identity", "Integration", "Inventory", "Loyalty", "Marketing", "Orders", "Pricing", "SEO", "Settings", "Shipping" to darwin_app;

grant select, insert, update, delete on all tables in schema "Billing", "Businesses", "CartCheckout", "Catalog", "CMS", "CRM", "Identity", "Integration", "Inventory", "Loyalty", "Marketing", "Orders", "Pricing", "SEO", "Settings", "Shipping" to darwin_app;

grant usage, select, update on all sequences in schema "Billing", "Businesses", "CartCheckout", "Catalog", "CMS", "CRM", "Identity", "Integration", "Inventory", "Loyalty", "Marketing", "Orders", "Pricing", "SEO", "Settings", "Shipping" to darwin_app;
```

Keep extension and schema ownership with the migration role. Do not grant `superuser`, `createdb`, or broad `create` privileges to `darwin_app` after initial setup.

If future migrations create new schemas or sequences, update this grant block in the same deployment run before starting the runtime processes.

When starting WebAdmin, WebApi, or Worker with the restricted `darwin_app` role, disable host-side bootstrap:

```powershell
$env:DatabaseStartup__ApplyMigrations = "false"
$env:DatabaseStartup__Seed = "false"
```

Keep these values enabled only for local self-bootstrapping with an owner-capable development connection. Runtime roles should not need access to `public.__EFMigrationsHistory` beyond what a dedicated migration step requires.

## JSON Conversion Behavior

Selected operational/configuration JSON columns are converted from text to PostgreSQL `jsonb`.

During conversion, the migration uses a temporary helper:

```sql
public.darwin_try_parse_jsonb(value text, fallback jsonb)
```

This helper prevents legacy bad JSON from aborting the whole migration:

- `NULL` or empty text becomes the column-specific fallback.
- Invalid JSON becomes the column-specific fallback.
- Valid JSON is preserved as `jsonb`.

Current fallback shapes are:

- Object-like state: `{}`.
- Array-like state: `[]`.
- Optional state: `NULL`.

The helper is dropped after each conversion migration finishes.

## Text-Backed JSON That Intentionally Remains Text

Do not convert these to `jsonb` until the consuming query paths are changed:

- `Integration.EventLogs.PropertiesJson`: billing/event-log search uses PostgreSQL trigram substring matching only to find candidate Stripe webhook payloads, then applies JSON-aware value matching in application code before rendering correlations.
- `Integration.ProviderCallbackInboxMessages.PayloadJson`: callback inbox search currently performs payload substring matching.
- `Businesses.Businesses.AdminTextOverridesJson`: public/business discovery search currently performs localized override substring matching.
- `CartCheckout.CartItems.SelectedAddOnValueIdsJson`: cart line identity still uses JSON text equality, but application writes and request matching now canonicalize add-on IDs by distinct sorted GUID order before comparison.

PostgreSQL currently protects remaining text-backed JSON with `NOT VALID` JSON validity constraints where applicable and accelerates selected text search with trigram indexes.

## Post-Migration Checks

Run these checks after applying all PostgreSQL migrations:

```powershell
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select table_schema, table_name from information_schema.tables where table_schema = 'public' order by table_name;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select extname from pg_extension where extname in ('pg_trgm', 'citext') order by extname;"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select count(*) from information_schema.columns where udt_name = 'citext';"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select count(*) from information_schema.columns where udt_name = 'jsonb';"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select count(*) from pg_indexes where indexname like 'IX_PG_%_JsonbGin';"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select count(*) from pg_indexes where indexname in ('IX_PG_EventLogs_PropertiesJson_Trgm', 'IX_PG_ProviderCallbackInboxMessages_PayloadJson_Trgm', 'IX_PG_Businesses_AdminTextOverridesJson_Trgm');"
docker exec darwin-postgres psql -U darwin_app -d darwin_dev -c "select count(*), count(*) filter (where not convalidated) from pg_constraint where conname like 'CK_PG_%_ValidJson';"
```

For local Docker installs created from `.env.example`, `darwin_app` is the default user. If your local `.env` overrides `DARWIN_POSTGRES_USER`, use that value in the `psql -U` argument.

Expected current baseline:

- Only `public.__EFMigrationsHistory` in `public`.
- `citext` and `pg_trgm` installed.
- 20 `citext` columns.
- 21 `jsonb` columns.
- 14 JSONB GIN indexes.
- 3 targeted text-JSON trigram indexes.
- 11 text-JSON validity constraints, initially `NOT VALID`.

## Text-Backed JSON Validation

Before validating `NOT VALID` JSON constraints on an existing database, confirm there are no invalid rows:

```sql
select 'Integration.EventLogs.PropertiesJson' as column_name, count(*) filter (where not public.darwin_is_valid_jsonb("PropertiesJson")) as invalid_count from "Integration"."EventLogs"
union all select 'Integration.EventLogs.UtmSnapshotJson', count(*) filter (where not public.darwin_is_valid_jsonb("UtmSnapshotJson")) from "Integration"."EventLogs"
union all select 'Integration.ProviderCallbackInboxMessages.PayloadJson', count(*) filter (where not public.darwin_is_valid_jsonb("PayloadJson")) from "Integration"."ProviderCallbackInboxMessages"
union all select 'Businesses.Businesses.AdminTextOverridesJson', count(*) filter (where "AdminTextOverridesJson" is not null and not public.darwin_is_valid_jsonb("AdminTextOverridesJson")) from "Businesses"."Businesses"
union all select 'Settings.SiteSettings.AdminTextOverridesJson', count(*) filter (where "AdminTextOverridesJson" is not null and not public.darwin_is_valid_jsonb("AdminTextOverridesJson")) from "Settings"."SiteSettings"
union all select 'CartCheckout.CartItems.SelectedAddOnValueIdsJson', count(*) filter (where not public.darwin_is_valid_jsonb("SelectedAddOnValueIdsJson")) from "CartCheckout"."CartItems"
union all select 'Billing.BillingPlans.FeaturesJson', count(*) filter (where "FeaturesJson" is not null and not public.darwin_is_valid_jsonb("FeaturesJson")) from "Billing"."BillingPlans"
union all select 'Loyalty.LoyaltyPrograms.RulesJson', count(*) filter (where "RulesJson" is not null and not public.darwin_is_valid_jsonb("RulesJson")) from "Loyalty"."LoyaltyPrograms"
union all select 'Loyalty.LoyaltyRewardRedemptions.MetadataJson', count(*) filter (where "MetadataJson" is not null and not public.darwin_is_valid_jsonb("MetadataJson")) from "Loyalty"."LoyaltyRewardRedemptions"
union all select 'Loyalty.LoyaltyRewardTiers.MetadataJson', count(*) filter (where "MetadataJson" is not null and not public.darwin_is_valid_jsonb("MetadataJson")) from "Loyalty"."LoyaltyRewardTiers"
union all select 'Loyalty.ScanSessions.SelectedRewardsJson', count(*) filter (where "SelectedRewardsJson" is not null and not public.darwin_is_valid_jsonb("SelectedRewardsJson")) from "Loyalty"."ScanSessions";
```

Only validate constraints after every `invalid_count` is zero and the deployment window can tolerate the validation scan:

```sql
alter table "Billing"."BillingPlans" validate constraint "CK_PG_BillingPlans_FeaturesJson_ValidJson";
alter table "Businesses"."Businesses" validate constraint "CK_PG_Businesses_AdminTextOverridesJson_ValidJson";
alter table "CartCheckout"."CartItems" validate constraint "CK_PG_CartItems_SelectedAddOnValueIdsJson_ValidJson";
alter table "Integration"."EventLogs" validate constraint "CK_PG_EventLogs_PropertiesJson_ValidJson";
alter table "Integration"."EventLogs" validate constraint "CK_PG_EventLogs_UtmSnapshotJson_ValidJson";
alter table "Integration"."ProviderCallbackInboxMessages" validate constraint "CK_PG_ProviderCallbackInboxMessages_PayloadJson_ValidJson";
alter table "Loyalty"."LoyaltyPrograms" validate constraint "CK_PG_LoyaltyPrograms_RulesJson_ValidJson";
alter table "Loyalty"."LoyaltyRewardRedemptions" validate constraint "CK_PG_LoyaltyRewardRedemptions_MetadataJson_ValidJson";
alter table "Loyalty"."LoyaltyRewardTiers" validate constraint "CK_PG_LoyaltyRewardTiers_MetadataJson_ValidJson";
alter table "Loyalty"."ScanSessions" validate constraint "CK_PG_ScanSessions_SelectedRewardsJson_ValidJson";
alter table "Settings"."SiteSettings" validate constraint "CK_PG_SiteSettings_AdminTextOverridesJson_ValidJson";
```

The local Docker `darwin_dev` database currently reports zero invalid rows for all 11 text-backed JSON constraints. The constraints are still intentionally left `NOT VALID` in migrations so customer databases can choose the validation window explicitly.

## Future Cleanup Path

When a text-backed JSON column is ready to migrate:

1. Replace string `Contains` or text equality behavior with explicit JSON semantics or a generated/search column.
2. Add provider-neutral query code first.
3. Add PostgreSQL migration using `jsonb` and an appropriate GIN or btree/generated-column index.
4. Keep SQL Server behavior available through equivalent persisted computed columns or application-side projections if needed.
5. Record the decision in `docs/persistence-providers.md` and `DarwinTesting.md`.
