# Darwin Current Development Handoff

Last updated: 2026-04-30.

Use this file as the compact continuation context when starting a new Codex chat.

## Working Rules

- Do not develop or expand test projects in this thread. Running builds/smokes is allowed.
- If a test/backlog note is important, update the existing testing/backlog document instead of writing tests.
- Keep secrets out of git. Use process environment variables, local `.env`, Docker env, or user-secrets only.
- Do not revert unrelated dirty worktree changes.
- Prefer fixing real implementation, operational hardening, and documentation gaps over adding new features.

## Current Focus

The recent work is centered on:

- PostgreSQL as the preferred/default provider while keeping SQL Server support.
- Restricted PostgreSQL runtime role support (`darwin_app`) with owner-role migration/seed separation.
- Brevo transactional email integration and provider callback processing.
- WebAdmin operational visibility for provider callbacks, especially Brevo delivery failures.
- Worker reliability and side-effect-safe Development defaults.

## Important Implemented Changes

- PostgreSQL and SQL Server provider lanes both contain the provider callback operational index migration:
  - `src/Darwin.Infrastructure.PostgreSql/Migrations/20260429205436_AddProviderCallbackInboxOperationalIndexes.cs`
  - `src/Darwin.Infrastructure.SqlServer/Migrations/20260429205641_AddProviderCallbackInboxOperationalIndexes.cs`
- `DatabaseStartup:ApplyMigrations` and `DatabaseStartup:Seed` now control Development-time host bootstrap in WebAdmin/WebApi.
- Runtime apps can run with restricted `darwin_app` after migrations/seed are applied by an owner-capable role.
- WebAdmin Provider Callbacks page supports `deliveryFailureOnly` filtering for Brevo failure events.
- Brevo webhook ingress:
  - Uses Basic Auth from `Email:Brevo:WebhookUsername` and `Email:Brevo:WebhookPassword`.
  - Stores bounded SHA-256 idempotency keys.
  - Accepts common Brevo field variants for event, message id, and timestamp.
- Worker provider callback processing now has required DI registrations:
  - `ProcessBrevoTransactionalEmailWebhookHandler`
  - `ProcessStripeWebhookHandler`
  - `ApplyShipmentCarrierEventHandler`
- Worker Development defaults keep side-effect workers disabled unless explicitly enabled.
- Disabled worker services now log their disabled state explicitly.

## Recent Validations

The following validations passed recently:

- `dotnet build src\Darwin.Infrastructure\Darwin.Infrastructure.csproj --no-restore`
- `dotnet build src\Darwin.WebAdmin\Darwin.WebAdmin.csproj --no-restore`
- `dotnet build src\Darwin.WebApi\Darwin.WebApi.csproj --no-restore`
- `dotnet build src\Darwin.Worker\Darwin.Worker.csproj --no-restore`
- `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\check-secrets.ps1`
- `git diff --check`

Runtime smoke results:

- WebAdmin with restricted PostgreSQL runtime role returned authenticated `302` from `/BusinessCommunications/ProviderCallbacks?provider=Brevo&deliveryFailureOnly=true`.
- WebApi with restricted PostgreSQL runtime role returned `200` from `/api/v1/public/businesses/category-kinds`.
- Worker with restricted PostgreSQL runtime role stayed running with all side-effect workers disabled.
- Brevo webhook smoke:
  - First post returned `duplicate=false`.
  - Repost of same payload returned `duplicate=true`.
  - Exactly one provider callback inbox row was persisted.
  - Temporary smoke row was removed.
- Brevo provider-callback worker smoke:
  - Temporary Brevo `hard_bounce` inbox row was processed.
  - Matching `EmailDispatchAudit` moved from `Sent` to `Failed`.
  - Inbox moved to `Processed`.
  - Temporary rows were removed.
- Stripe provider-callback worker smoke:
  - Temporary Stripe `payment_intent.succeeded` inbox row was processed.
  - Inbox moved to `Processed`.
  - One `EventLog` row was written with the Stripe event id as idempotency key.
  - Temporary rows were removed.

## Key Documents Updated

- `docs/persistence-providers.md`
- `docs/postgresql-migration-runbook.md`
- `docs/production-setup.md`
- `.env.example`

## Known Dirty Worktree Scope

There are many intentional modified files. Do not assume a clean tree.

Key changed areas:

- Provider callback DTO/query/view/controller resources in WebAdmin/Application.
- Brevo webhook ingress in WebApi.
- Worker provider callback processing and disabled-worker logs.
- PostgreSQL/SQL Server migrations and snapshots.
- Persistence startup bootstrap controls.
- Secret checking and production/PostgreSQL docs.

Run `git status --short` before making any further changes.

## Suggested Next Work

Continue from the most valuable remaining verification/hardening:

1. Smoke DHL provider callback Worker path.
2. Review WebAdmin visibility after Worker processing for `Processed`/`Failed` provider callbacks.
3. Finalize production runbook ordering:
   - provision PostgreSQL roles
   - run migrations/seed with owner role
   - grant runtime role
   - start WebAdmin/WebApi/Worker with bootstrap disabled
   - configure Brevo webhook URL and Basic Auth secret
4. Review provider callback error handling for unsupported providers and max-attempt operational guidance.
5. If everything is stable, prepare a concise implementation summary before staging/commit.

## Important Local Notes

- Docker PostgreSQL container name: `darwin-postgres`.
- pgAdmin container name: `darwin-pgadmin`.
- SQL Server container name seen locally: `sqlpreview`.
- Local PostgreSQL may not have `darwin_app` by default; smoke scripts created/updated it temporarily without committing secrets.
- Never print or persist generated passwords/API keys.
