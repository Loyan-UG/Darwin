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
   - **Not yet wired into CI or coverage-gate** — local execution only at this stage.
8. `tests/Darwin.Tests.Common`
   - Shared test infrastructure/helpers consumed by other suites.

### Coverage lanes in CI

CI treats the main suites as independent lanes:

- unit
- contracts
- infrastructure
- webapi
- integration
- mobile-shared

The `webadmin` lane exists as a project but is **not yet included** in the `tests-quality-gates.yml` workflow or `scripts/ci/verify_coverage.py`.

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

### 3.7 WebAdmin lane (`Darwin.WebAdmin.Tests`) *(not yet in CI)*

Use for the WebAdmin panel's HTTP-level correctness:

- Security header presence and correctness (CSP, X-Content-Type-Options, Referrer-Policy, Permissions-Policy).
- Authentication/redirect boundaries for admin-only routes.
- Anti-forgery token and form rendering sanity checks.
- Forwarded-header handling and HTTPS redirection behavior.

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

# WebAdmin tests (local only — not yet in CI lane):
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
- Add a guard that shared EF mappings do not introduce SQL Server-only column types such as `uniqueidentifier` or `nvarchar(max)`.
- Add filtered-index SQL coverage for PostgreSQL so SQL Server filters are normalized before migrations are applied.
- Add concurrency coverage for `RowVersion`: SQL Server native rowversion and PostgreSQL client-managed bytea concurrency.
- Add schema-placement coverage proving no application tables are created in PostgreSQL `public` or SQL Server `dbo`.
- Add PostgreSQL extension/index coverage proving `pg_trgm` and the expected `IX_PG_*` GIN indexes exist after migration.
- Add PostgreSQL `citext` coverage proving identifier uniqueness and equality behave case-insensitively for login identifiers, role/permission keys, slugs, SKUs, billing plan codes, promotion codes, and tax category names.
- Add PostgreSQL `jsonb` coverage proving the current 21 selected operational/configuration JSON columns are created as `jsonb`, the current 14 `IX_PG_*_JsonbGin` indexes exist, and text-search/equality-sensitive paths remain on text-backed JSON columns until query code is migrated to JSON operators or generated search fields.
- Add PostgreSQL trigram coverage proving text-backed JSON search columns keep their `IX_PG_*_Trgm` indexes for event-log properties, provider callback payloads, and business admin text overrides.
- Add PostgreSQL JSON validity coverage proving the current 11 `CK_PG_*_ValidJson` constraints exist for text-backed JSON columns and reject invalid JSON on new writes.
- Add provider-neutral search coverage for high-value catalog, CMS, business discovery/list, Billing operator, CRM operator, Inventory operator, Orders/shipment operator, Shipping, Media, Loyalty, Identity user/mobile-device/permission, add-on group, variant lookup, and business/communication operations paths proving uppercase/lowercase search terms return the same relevant rows on PostgreSQL and SQL Server.
- Add provider-neutral guard coverage that fails on new application-query `EF.Functions.Like`, query-side `Enum.ToString()` search, or query-side `Guid.ToString()` search unless a provider-specific exception is documented.
- Add provider-neutral guard coverage that fails when EF query predicates embed moving `DateTime.UtcNow` expressions directly instead of using local UTC snapshots/cutoff parameters.
- Add model-metadata coverage proving all decimal properties have explicit precision either through entity-specific configuration or the provider-neutral fallback convention.
- Add PostgreSQL provider-registration coverage proving runtime and design-time Npgsql paths preserve normalized connection defaults for application name, retry limits, timeouts, keepalive, and auto-prepare settings.

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

- Wire `Darwin.WebAdmin.Tests` into CI (`tests-quality-gates.yml`) as a dedicated `webadmin` lane and add it to `scripts/ci/verify_coverage.py` with an initial threshold.
- Increase depth in infrastructure and webapi lanes where currently thinner than unit/integration.
- Expand integration matrices for concurrency/authorization edge-cases.
- Raise lane thresholds gradually after sustained green history.
- Keep this document synchronized with actual test inventory and CI behavior in each related PR.

---

## 10) Maintenance rule

When any of the following changes, update this document in the same PR:

- test project inventory,
- coverage lane definitions,
- threshold values,
- required local/CI prerequisites,
- execution commands or workflow structure.

This file should always describe **what is true now**, not only future intent.
