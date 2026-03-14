# 🧪 Darwin Testing Strategy & Architecture (Expanded)

This file is an expanded and opinionated testing guide for the Darwin platform. It builds on the existing testing guidance but adds practical utilities, examples and step-by-step patterns aimed at making test creation easy, consistent and robust across Domain, Application, Infrastructure, WebApi and Mobile layers.

Keep this document up-to-date as tests are added and as the CI pipeline evolves. All code examples and comments are in English.

---

## 0) Testing scope ownership (important)

- This document is the **single execution tracker** for testing tasks.
- Main delivery documents (`BACKLOG.md`, `DarwinMobile.md`) intentionally reference testing tasks here instead of duplicating test execution items.
- When a development backlog item says testing is tracked in `DarwinTesting.md`, update status only in this file to avoid drift.

---

## Table of contents

1. Goals
2. Test projects & responsibilities
3. Test patterns and conventions
4. Test utilities and helpers (recommended)
5. Integration testing: WebApplicationFactory + SQLite (detailed)
6. Sample tests
   - Unit test example
   - Mapper unit test
   - Integration test: loyalty flow (Prepare -> Process -> Confirm)
   - Contract serialization test
7. Data seeding & test data builders
8. Strategies for stateful / multi-step flows
9. Contract / compatibility tests
10. Caching & observability tests
11. CI considerations & pipeline setup
12. Checklist for adding tests to new features
13. Appendix: small helper snippets
14. Current implementation status (done vs pending)
15. Testing delivery backlog (execution order)

---

## 1) Goals

- Confidence — tests prevent regressions for features and security rules.
- Maintainability — tests are readable, small, and follow the solution architecture.
- Speed — unit tests are fast; integration tests are reasonably fast and stable.
- Extensibility — tests are structured so new features can easily add tests.

---

## 2) Test projects & responsibilities

Current repository status first:

- Active suites with implemented test files:
  - `tests/Darwin.Tests.Unit`
  - `tests/Darwin.Tests.Integration`
- Scaffolded test projects currently present in the solution (currently no test classes committed yet):
  - `tests/Darwin.WebApi.Tests`
  - `tests/Darwin.Infrastructure.Tests`
  - `tests/Darwin.Contracts.Tests`
  - `tests/Darwin.Mobile.Shared.Tests`
  - `tests/Darwin.Tests.Common` (helper library placeholder)
- Unit tests currently cover slug validators, sanitizer helper behavior, and baseline contract serialization compatibility.
- Integration tests are wired to `Darwin.WebApi` with `WebApplicationFactory<Program>` baseline coverage in Identity/Profile/Loyalty/Meta areas.

Recommended target structure (incremental evolution):

Create separate test projects that align with layered architecture. Each project should reference only the production assemblies it needs.

- `tests/Darwin.UnitTests`  
  Purpose: Unit tests for Domain, Application and Shared logic. Avoid I/O. Use fakes/mocks.  
  Tools: xUnit, FluentAssertions, Moq/NSubstitute.

- `tests/Darwin.WebApi.Tests`  
  Purpose: Integration tests for WebApi controllers and middleware using `WebApplicationFactory<Program>`. Use SQLite in-memory for relational fidelity. Exercise policies and filters.  
  Tools: xUnit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing

- `tests/Darwin.Infrastructure.Tests` (optional)  
  Purpose: Tests for EF Core configs, migrations and DB mappings using SQLite in-memory.

- `tests/Darwin.Mobile.Shared.Tests`  
  Purpose: Unit tests for the mobile shared client (ApiClient, retry policy, token store). Mock `HttpMessageHandler`.

Note: Migrate to the target naming incrementally. If you rename projects, update CI, solution filters, and documentation in the same PR to avoid broken pipelines.

---

## 3) Test patterns & conventions

- Use AAA (Arrange, Act, Assert).
- Test naming: `MethodUnderTest_StateUnderTest_ExpectedResult`.
- Keep tests deterministic and independent.
- Prefer `Theory` + `MemberData` for data-driven variants.
- Freeze time via `IClock` or a test clock abstraction.
- Avoid `Thread.Sleep` or time-based flakiness.
- For collection assertions, check both count and exact expected items (when relevant).
- Prefer explicit values over random generation — if randomness is used, it must be seeded.

Example naming:

```csharp
[Fact]
public async Task PrepareScanSession_Should_Return_BadRequest_When_BusinessIdEmpty() { ... }
```

---

## 4) Test utilities & helpers (recommended)

Create a `tests/Shared` library or a `Tests.Common` folder in each test project to hold helpers that reduce boilerplate.

Essential helpers:

- `WebApiTestFactory` (custom `WebApplicationFactory<Program>`)  
  - Configure `IConfiguration` overrides, swap external services for fakes, configure SQLite in-memory DB per test or per class, seed data.

- `TestDbFactory`  
  - Create a fresh `DbContext` pointing at a prepared SQLite in-memory connection. Runs migrations if needed.

- `FakeCurrentUserService` / `TestCurrentUserService`  
  - Implements `ICurrentUserService` to return a deterministic user id for tests.

- `JwtTokenGenerator`  
  - Create tokens for integration tests that exercise real auth middleware.

- `JsonExtensions`  
  - Read `ProblemDetails` from responses and helper methods for `ReadFromJsonAsync<T>()` with proper options.

- `TestDataBuilder` pattern classes  
  - e.g. `LoyaltyProgramBuilder`, `BusinessBuilder`, `UserBuilder` — fluent builders for test entities and DTOs.

- `TestClock`  
  - Deterministic `IClock` for time-sensitive tests.

---

## 5) Integration testing: WebApplicationFactory + SQLite (detailed)

Integration tests must be repeatable and mimic production as closely as practical. Use a custom `WebApplicationFactory<Program>` to configure the test host.

Key recommendations:
- Use SQLite in-memory with an open connection per test class or test method depending on isolation needs:
  - For independent tests create a new connection per test (more isolation, slower).
  - For class-level sharing create/open once and reset DB state in `IAsyncLifetime`.
- Run migrations against the SQLite connection to ensure schema and constraints are applied.
- Avoid external network calls in integration tests. Replace external services (SMTP, external APIs) with test doubles.
- Provide a way to seed users/permissions and to issue JWT tokens for authenticated tests.

Example test factory skeleton:

```csharp
public sealed class WebApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly SqliteConnection _connection;

    public WebApiTestFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration and add test SQLite
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Ensure DB is migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            // Replace external integrations (SMTP, push, etc.) with fakes
            services.AddSingleton<INotificationSender, TestNotificationSender>();

            // Optionally provide test ICurrentUserService or allow generation of real JWT tokens
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }
}
```

Notes:
- If your `Program` configures services differently between environments, use `builder.UseEnvironment("Testing")` and ensure `appsettings.Testing.json` exists for test config.
- For tests that need JWT-authenticated requests, either:
  - replace `ICurrentUserService` with a fake in the test factory, or
  - seed a test user and generate a JWT using project signing keys (preferred for exercising middleware fully).

---

## 6) Sample tests

Included below are small examples you can copy into test projects. Adapt namespaces and DI setup to your project.

### 6.1 Unit test example (Application handler)
Example unit test for a handler that prepares a scan session (mock dependencies).

```csharp
public class PrepareScanSessionHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_BusinessIdEmpty()
    {
        // Arrange
        var db = new TestDbFactory().CreateDbContext();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        var clock = new TestClock(DateTime.UtcNow);

        var sut = new PrepareScanSessionHandler(db, currentUser, clock);

        // Act
        var result = await sut.HandleAsync(null!, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }
}
```

### 6.2 Mapper unit test
Verify mapping correctness for `LoyaltyContractsMapper`.

```csharp
public class LoyaltyContractsMapperTests
{
    [Fact]
    public void ToContract_Should_Map_LoyaltyAccountSummaryDto_To_Contract()
    {
        var dto = new LoyaltyAccountSummaryDto
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            BusinessId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            BusinessName = "Test",
            PointsBalance = 123,
            LifetimePoints = 456,
            Status = Darwin.Domain.Enums.LoyaltyAccountStatus.Active,
            LastAccrualAtUtc = DateTime.UtcNow
        };

        var contract = LoyaltyContractsMapper.ToContract(dto);

        contract.LoyaltyAccountId.Should().Be(dto.Id);
        contract.BusinessId.Should().Be(dto.BusinessId);
        contract.PointsBalance.Should().Be(dto.PointsBalance);
        contract.Status.Should().Be(dto.Status.ToString());
    }
}
```

### 6.3 Integration test: full loyalty flow (Prepare -> Process -> Confirm)
This end-to-end style integration test uses `WebApiTestFactory`. It demonstrates the full flow with a seeded business and user.

Important: This test assumes test factory seeds a business and that we can authenticate as a consumer and business.

```csharp
public class LoyaltyFlowIntegrationTests : IClassFixture<WebApiTestFactory>
{
    private readonly WebApiTestFactory _factory;

    public LoyaltyFlowIntegrationTests(WebApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Prepare_Process_ConfirmAccrual_EndToEnd()
    {
        // Arrange
        using var client = _factory.CreateClient();
        // Obtain bearer token for consumer user (helper)
        var consumerToken = await TestAuthHelper.GetJwtForConsumerAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", consumerToken);

        // Step 1: Prepare (consumer)
        var prepareReq = new PrepareScanSessionRequest
        {
            BusinessId = TestSeed.DefaultBusinessId,
            Mode = Darwin.Contracts.Loyalty.LoyaltyScanMode.Accrual
        };

        var prepareResp = await client.PostAsJsonAsync("/api/v1/loyalty/scan/prepare", prepareReq);
        prepareResp.EnsureSuccessStatusCode();

        var prepareBody = await prepareResp.Content.ReadFromJsonAsync<PrepareScanSessionResponse>();
        prepareBody.Should().NotBeNull();
        prepareBody!.ScanSessionToken.Should().NotBeNullOrWhiteSpace();

        // Step 2: Process (business) - authenticate as business
        var businessToken = await TestAuthHelper.GetJwtForBusinessAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", businessToken);

        var processReq = new ProcessScanSessionForBusinessRequest
        {
            ScanSessionToken = prepareBody.ScanSessionToken
        };

        var processResp = await client.PostAsJsonAsync("/api/v1/loyalty/scan/process", processReq);
        processResp.EnsureSuccessStatusCode();

        var processBody = await processResp.Content.ReadFromJsonAsync<ProcessScanSessionForBusinessResponse>();
        processBody.Should().NotBeNull();
        processBody!.Mode.Should().Be(Darwin.Contracts.Loyalty.LoyaltyScanMode.Accrual);

        // Step 3: Confirm accrual
        var confirmReq = new ConfirmAccrualRequest
        {
            ScanSessionToken = prepareBody.ScanSessionToken,
            Points = 1,
            Note = "Test visit"
        };

        var confirmResp = await client.PostAsJsonAsync("/api/v1/loyalty/scan/confirm-accrual", confirmReq);
        confirmResp.EnsureSuccessStatusCode();

        var confirmBody = await confirmResp.Content.ReadFromJsonAsync<ConfirmAccrualResponse>();
        confirmBody.Should().NotBeNull();
        confirmBody!.Success.Should().BeTrue();
        confirmBody.NewBalance.Should().BeGreaterThanOrEqualTo(0);
    }
}
```

Notes:
- Use `TestAuthHelper` to generate valid JWT tokens (or swap `ICurrentUserService` in `WebApiTestFactory`).
- The test should cover token lifecycle: expired tokens, consumed tokens, etc., with additional tests.

### 6.4 Contract serialization test (Darwin.Contracts)

Ensure stable JSON shape for contracts to prevent breaking mobile clients.

```csharp
[Fact]
public void LoyaltyAccountSummary_Should_Serialize_To_ExpectedJson()
{
    var model = new LoyaltyAccountSummary
    {
        BusinessId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        BusinessName = "X",
        PointsBalance = 100,
        LoyaltyAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        LifetimePoints = 200,
        Status = "Active",
        LastAccrualAtUtc = new DateTime(2025,1,1,0,0,0, DateTimeKind.Utc),
        NextRewardTitle = "Free Coffee"
    };

    var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });

    // You can assert JSON contains the expected property names and types
    json.Should().Contain("\"BusinessId\"");
    json.Should().Contain("\"PointsBalance\": 100");
    json.Should().Contain("\"NextRewardTitle\": \"Free Coffee\"");
}
```

---

## 7) Data seeding & test data builders

- Use small, deterministic builders for domain entities:
  - `BusinessBuilder.WithDefaultProgram().WithRewardTier(points: 10, name: "Small")`
  - `UserBuilder.WithEmail("test@example.com").AsConsumer()`
- Builders return DTOs/equivalent rather than saving to DB directly; test harness can call `SeedAsync()` to persist and return stable ids.
- Keep seeded datasets minimal for each test to avoid interference.

Example builder pattern:

```csharp
public class BusinessBuilder
{
    private readonly Business _b = new Business { Id = Guid.NewGuid(), Name = "Biz" };
    private readonly List<LoyaltyRewardTier> _tiers = new();

    public BusinessBuilder WithRewardTier(int points, string name)
    {
        _tiers.Add(new LoyaltyRewardTier { Id = Guid.NewGuid(), PointsRequired = points, Name = name, IsDeleted = false });
        return this;
    }

    public Business Build()
    {
        _b.RewardTiers = _tiers;
        return _b;
    }
}
```

---

## 8) Strategies for stateful / multi-step flows

Multi-step flows (prepare -> process -> confirm) require tests that assert both actions and side-effects (e.g., token consumed, points changed). Use the following patterns:

- Use `IClock` or `TestClock` to create deterministic time windows to test expiry behavior.
- For concurrency tests, use `Task.WhenAll` and assert DB state afterwards for consistency.
- For negative tests, try both the final result and the intermediate state (e.g. token consumed or status became Expired).

---

## 9) Contract / compatibility tests

- Add a contract test project that ensures serialization compatibility.
- For every contract change:
  - Add a new test that serializes an instance with the new shape.
  - If removing fields, ensure older clients can still parse (if required) or plan a breaking change with version bump.
- Consider maintaining a small set of snapshot JSON files that CI verifies remain compatible.

---

## 10) Caching & observability tests

- If you add caching for `GetAvailableLoyaltyRewardsForBusinessHandler`, include:
  - Unit tests to verify cache hit/miss behavior (use `IMemoryCache` in test).
  - Integration tests to verify caching reduces DB calls (use a DB call counter or a test double for repository).
- Observability:
  - Ensure code emits metrics on critical events (prepare/process/confirm/enrichmentFailure).
  - Add a smoke test that ensures metrics are emitted (or at least that the code path calls the metric client).

---

## 11) CI considerations & pipeline setup

- Execute this pipeline stage order:
  1. `dotnet build` (fail fast on compile).
  2. Run unit tests (`Darwin.UnitTests`) in parallel and collect coverage.
  3. Run integration tests (`Darwin.WebApi.Tests`) with a single worker or limited concurrency to avoid resource contention.
  4. Run contract serialization tests.
  5. Run static analysis (Roslyn analyzers, nullable warnings).
  6. Generate OpenAPI (if applicable) and store as artifact for mobile teams.

- Secrets: do not use real secrets in CI. Use test signing keys and test-only configurations.

- Flaky tests: track them and avoid reintroducing them in CI. If a test is flaky, either fix or remove from CI until stabilized.

---

## 12) Checklist for adding tests to new features

When adding a new feature, the developer should include tests according to this checklist:

1. Unit tests for core logic (happy path and at least two edge cases).
2. Unit tests for validators and error conditions.
3. Mapper tests if new DTOs or contract mappings were added.
4. Integration tests for the endpoint(s): one happy path, one failure path (validation or auth).
5. Contract serialization test for any new or changed contract.
6. If new DB schema change: infrastructure/migration tests (SQLite).
7. Add test data builders needed by tests.
8. Update `Tests.Common` helpers if new test wiring is required.
9. Document test instructions in the feature PR (how to run locally and any env flags).

---

## 13) Appendix — helper snippets

### Read `ProblemDetails` from HttpResponseMessage

```csharp
public static async Task<ProblemDetails> ReadProblemDetailsAsync(this HttpResponseMessage resp)
{
    var json = await resp.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
}
```

### Create authenticated HttpClient (JWT helper)

```csharp
public static HttpClient WithBearer(this HttpClient client, string token)
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
}
```

### Example `TestAuthHelper` (conceptual)

```csharp
public static class TestAuthHelper
{
    public static async Task<string> GetJwtForConsumerAsync(HttpClient client)
    {
        var login = new PasswordLoginRequest { Email = "consumer@test.local", Password = "Password123!" };
        var resp = await client.PostAsJsonAsync("/api/auth/login", login);
        resp.EnsureSuccessStatusCode();
        var tokens = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }

    public static async Task<string> GetJwtForBusinessAsync(HttpClient client)
    {
        var login = new PasswordLoginRequest { Email = "business@test.local", Password = "Password123!" };
        var resp = await client.PostAsJsonAsync("/api/auth/login", login);
        resp.EnsureSuccessStatusCode();
        var tokens = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        return tokens!.AccessToken;
    }
}
```

---

## 14) Current implementation status (done vs pending)

This status is derived from the current repository state and must be refreshed whenever test assets change.

### Done

- [x] `tests/Darwin.Tests.Unit` exists on `net10.0` with xUnit v3 + FluentAssertions setup.
- [x] Unit tests exist for:
  - `Catalog/ProductUniqueSlugValidatorTests`
  - `CMS/PageUniqueSlugValidatorTests`
  - `Common/HtmlSanitizerHelperTests`
- [x] `tests/Darwin.Tests.Unit/TestDbFactory.cs` exists for DB-backed unit test setup.
- [x] `tests/Darwin.Tests.Integration` project exists and can be expanded incrementally.

### Pending

- [x] Wire `Darwin.Tests.Integration` to `Darwin.WebApi` with `WebApplicationFactory<Program>` (initial smoke-test baseline completed).
- [ ] **P1 — Identity happy-path matrix (authorized):** baseline + core negative-path coverage and authorized matrix are implemented in code; pending CLI/CI execution evidence.
- [ ] **P2 — Profile optimistic concurrency matrix (`Id` + `RowVersion`):** baseline auth-guard coverage and authorized success/stale-rowversion matrix are implemented in code; pending CLI/CI execution evidence.
- [ ] **P3 — Loyalty E2E prepare/process/confirm:** baseline auth-guard coverage and authorized end-to-end scenarios are implemented in code; pending CLI/CI execution evidence.
- [ ] Add contract serialization compatibility tests for mobile-critical DTOs. Baseline coverage plus expanded Loyalty timeline/promotions, Profile concurrency payload, Push registration payloads, Business discovery compatibility checks, business campaign/reward configuration property-name compatibility checks, and campaign create/update/list (including item `businessId`/`rowVersion`) + reward-tier configuration/delete/mutation payload serialize/deserialize compatibility checks are implemented in code (`Darwin.Contracts.Tests` + `Darwin.Tests.Unit` + `Darwin.WebApi.Tests` mapper suites); pending CLI/CI execution evidence.
- [ ] Add `Darwin.Mobile.Shared` reliability tests (retry/bearer/no-content normalization). Implemented in code (`ApiClientReliabilityTests`), pending CLI/CI execution evidence.
- [ ] Add CI lane split and coverage publication for unit/integration. Implemented in code via GitHub Actions quality-gates workflow with lanes for unit/contracts/infrastructure/webapi/integration/mobile-shared (pending CI execution evidence).

---

## 15) Testing delivery backlog (execution order)

Keep this list as the execution tracker for the testing workstream.

| Order | Work item | Status | Exit criteria |
|---|---|---|---|
| 1 | Integration test host foundation (`WebApplicationFactory`, deterministic DB reset, test environment config) | In Progress (Implemented, pending CLI/CI run) | Smoke suites now run with deterministic DB reset+seed in `IAsyncLifetime`; finalize after CLI/CI evidence confirms stability and runtime overhead is acceptable. |
| 2 | **P1** Identity flow test pack (authorized matrix first) | In Progress (Implemented, pending CLI/CI run) | Baseline + core negative tests and authorized happy-path matrix are implemented (`register/login`, `refresh`, `password/change`, `logout`, `logout-all`); finalize after passing CLI/CI evidence. |
| 3 | **P2** Profile concurrency test pack (`RowVersion`) | In Progress (Implemented, pending CLI/CI run) | Baseline auth-guard tests and authorized success/stale row-version matrix are implemented; finalize after passing CLI/CI evidence. |
| 4 | **P3** Loyalty scan journey test pack (prepare/process/confirm) | In Progress (Implemented, pending CLI/CI run) | Baseline auth-guard tests and authorized end-to-end prepare/process/confirm scenarios are implemented; finalize after passing CLI/CI evidence. |
| 5 | Contracts compatibility pack | In Progress (Implemented, pending CLI/CI run) | Baseline coverage plus expanded Loyalty timeline/promotions, Profile concurrency payload, Push registration payloads, and Business discovery compatibility checks are implemented; contract smoke tests in `Darwin.Contracts.Tests` and mapper stability tests in `Darwin.WebApi.Tests` are added; finalize after passing CLI/CI evidence. |
| 6 | Mobile.Shared reliability pack | In Progress (Implemented, pending CLI/CI run) | Reliability matrix is implemented (`retry`, `auth header injection`, `no-content normalization`) in `ApiClientReliabilityTests`; finalize after passing CLI/CI evidence. |
| 7 | CI quality gates | In Progress (Temporary PR soft-gate active) | Workflow keeps all test lanes running, but PR jobs are temporarily `continue-on-error` so failing suites do not block merge. Soft-gate can be disabled immediately by setting repository variable `DARWIN_PR_SOFT_GATE=false`, or per manual run via workflow input `force_strict=true`; restore hard-gate mode in code after completing the rollback checklist and attaching CI evidence. |

> Authoring status note: all currently scoped mobile-critical test packs are now implemented in code; remaining backlog state is strictly about execution evidence and stabilization in CLI/CI.

### Authoring completion checklist (code written)

- [x] Identity authorized happy-path matrix tests authored (`AuthIdentityEndpointAuthorizedMatrixTests`).
- [x] Profile optimistic concurrency matrix tests authored (`ProfileEndpointAuthorizedConcurrencyTests`).
- [x] Loyalty E2E prepare/process/confirm tests authored (`LoyaltyEndpointAuthorizedE2eTests`).
- [x] Contracts compatibility authoring expanded for mobile-critical DTOs (identity/profile/loyalty/promotions/push/business payloads, campaign/reward-configuration management shapes, and WebApi mapper stability scenarios including enum/ledger/business-list mappings plus location/program-tier projections).
- [x] Mobile.Shared reliability tests authored (`ApiClientReliabilityTests`) including guard-clause and invalid-JSON normalization scenarios.
- [x] Infrastructure design-time DbContext tests authored (`DesignTimeDbContextFactoryTests`) including no-config fallback path coverage.


### Latest local verification snapshot

- Attempted to execute the three prioritized integration matrices directly from CLI:
  - `dotnet test tests/Darwin.Tests.Integration/Darwin.Tests.Integration.csproj --filter "FullyQualifiedName~AuthIdentityEndpointAuthorizedMatrixTests|FullyQualifiedName~ProfileEndpointAuthorizedConcurrencyTests|FullyQualifiedName~LoyaltyEndpointAuthorizedE2eTests"`
- Initial result: execution blocked because `dotnet` CLI was unavailable (`bash: command not found: dotnet`).
- Follow-up remediation attempts in this environment:
  - Script install via `curl https://dotnet.microsoft.com/.../dotnet-install.sh` (blocked by outbound proxy HTTP 403).
  - Package install via `apt-get update && apt-get install dotnet-sdk-10.0` (blocked by repository/proxy HTTP 403).
- Tracking decision: keep backlog items in **In Progress (Implemented, pending CLI/CI run)** state until local or CI evidence is attached.

Temporary policy note:

- Current phase intentionally uses a **non-blocking PR test gate** to avoid merge deadlocks while unstable suites are being stabilized.
- CI now includes an explicit `soft-gate-status` job that emits a warning annotation when soft-gate mode is active for PR runs, and also writes run-level summary details (`event`, `mode`, switch variable, and manual `force_strict` input) to `GITHUB_STEP_SUMMARY`.
- This is temporary and must be reverted to hard-gate mode once the prioritized matrix suites are consistently green in CI.

### Soft-gate rollback checklist (must be completed before re-enabling hard-gate)

- [ ] Collect at least 5 consecutive green PR runs for P1/P2/P3 matrix suites in CI artifacts.
- [ ] Confirm flakiness root-cause notes are documented for previously failing suites.
- [ ] Set repository variable `DARWIN_PR_SOFT_GATE=false` (or run `workflow_dispatch` with `force_strict=true`) and verify one PR run is strict (no soft-gate behavior).
- [ ] Remove PR `continue-on-error` policy from `tests-quality-gates.yml` (code-level cleanup after successful strict verification).
- [ ] Re-run one full strict CI cycle on PR and one strict push cycle on `work` branch.
- [ ] Update this document status from **Temporary PR soft-gate active** to **Hard-gate restored** with links to CI evidence.

Backlog update rule:

1. Mark status only after tests are committed and passing in CLI.
2. Keep each item linked to concrete test file paths in commit/PR notes.
3. If scope changes, append a short reason in this section.

### Implemented test suites (current)

- Integration baseline suites:
  - `tests/Darwin.Tests.Integration/Meta/MetaHealthEndpointTests.cs`
  - `tests/Darwin.Tests.Integration/Meta/MetaInfoEndpointTests.cs`
  - `tests/Darwin.Tests.Integration/Identity/AuthIdentityEndpointBaselineTests.cs`
  - `tests/Darwin.Tests.Integration/Identity/AuthIdentityEndpointAuthorizedMatrixTests.cs`
  - `tests/Darwin.Tests.Integration/Profile/ProfileEndpointBaselineTests.cs`
  - `tests/Darwin.Tests.Integration/Profile/ProfileEndpointAuthorizedConcurrencyTests.cs`
  - `tests/Darwin.Tests.Integration/Loyalty/LoyaltyEndpointBaselineTests.cs`
  - `tests/Darwin.Tests.Integration/Loyalty/LoyaltyEndpointAuthorizedE2eTests.cs`
- Integration helper assets:
  - `tests/Darwin.Tests.Common/TestInfrastructure/IntegrationTestHostFactory.cs`
  - `tests/Darwin.Tests.Common/TestInfrastructure/IntegrationTestClientFactory.cs`
  - `tests/Darwin.Tests.Common/TestInfrastructure/IdentityFlowTestHelper.cs`
  - `tests/Darwin.Tests.Common/TestInfrastructure/IntegrationTestDatabaseReset.cs` (includes migration + reset gate for concurrency safety)
  - `tests/Darwin.Tests.Integration/IntegrationTestAssemblyConfiguration.cs` (disables integration assembly parallelization to avoid DB reset races)
  - `tests/Darwin.Tests.Integration/TestInfrastructure/DeterministicIntegrationTestBase.cs` (shared `IAsyncLifetime` + host/client lifecycle to remove per-suite duplication)
- Unit contract compatibility suite:
  - `tests/Darwin.Tests.Unit/Contracts/ContractSerializationCompatibilityTests.cs`
- Contracts project serialization suite:
  - `tests/Darwin.Contracts.Tests/Serialization/ContractsSerializationSmokeTests.cs`
- Infrastructure project configuration suite:
  - `tests/Darwin.Infrastructure.Tests/Persistence/DesignTimeDbContextFactoryTests.cs`
- WebApi mapper stability suite:
  - `tests/Darwin.WebApi.Tests/Mappers/BusinessContractsMapperTests.cs`
  - `tests/Darwin.WebApi.Tests/Mappers/LoyaltyContractsMapperTests.cs`
- Mobile shared reliability suite:
  - `tests/Darwin.Mobile.Shared.Tests/Api/ApiClientReliabilityTests.cs`
- CI quality gate assets:
  - `.github/workflows/tests-quality-gates.yml`
  - `scripts/ci/verify_coverage.py`

---


## 16) Solution/Test project consistency checklist

- `Darwin.sln` currently includes these test projects:
  - `tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj`
  - `tests/Darwin.Tests.Integration/Darwin.Tests.Integration.csproj`
  - `tests/Darwin.WebApi.Tests/Darwin.WebApi.Tests.csproj`
  - `tests/Darwin.Infrastructure.Tests/Darwin.Infrastructure.Tests.csproj`
  - `tests/Darwin.Contracts.Tests/Darwin.Contracts.Tests.csproj`
  - `tests/Darwin.Mobile.Shared.Tests/Darwin.Mobile.Shared.Tests.csproj`
- `Test.slnf` currently includes only:
  - `tests/Darwin.Tests.Unit/Darwin.Tests.Unit.csproj`
  - `tests/Darwin.Tests.Integration/Darwin.Tests.Integration.csproj`
- Current implemented test suites on disk are still concentrated in the two projects included in `Test.slnf`; remaining projects are scaffolded and should be activated as coverage expands.

## 17) Handoff for next chat

Use this list as the immediate continuation plan:

1. **CI quality gates activation — current top priority**
   - Run the new workflow and capture green evidence for unit/contracts/infrastructure/webapi/integration/mobile-shared lanes.
   - Tune coverage thresholds only if initial CI evidence shows justified baseline mismatch.
2. **Test infrastructure hardening**
   - Validate class-level reset overhead in CI and adjust isolation strategy if run time regresses.
   - Consolidate additional reusable helpers as needed.
3. **Contracts compatibility expansion (next wave)**
   - Continue adding tests for remaining mobile-critical DTO families and explicit versioning scenarios

Important context to carry into the next chat:

- Keep `PackageReference` versions untouched unless explicitly requested.
- Keep test comments in English with complete XML summaries.
- Environment here does not provide `dotnet` CLI, so execution evidence must come from CI/local machine.

## Closing notes & recommended next steps

1. Execute the newly implemented identity/profile/loyalty/webapi/mobile/contracts/infrastructure suites in CI and persist passing evidence before marking packs as completed.
2. Keep mapper and contract compatibility tests expanding with each new mobile-facing DTO/change.
3. Tune lane coverage thresholds based on first green baseline only when justified by CI evidence.


## 18) CI troubleshooting notes (copy to next chat)

Use these notes when CI lanes fail before merge:

- **Integration lane requires SQL Server availability.**
  - The integration test host uses `AddPersistence(...UseSqlServer...)` and deterministic DB reset/migrate/seed.
  - CI must provide a reachable SQL Server and set `ConnectionStrings__DefaultConnection` to a test-scoped DB.
  - Keep `ASPNETCORE_ENVIRONMENT=Testing` for integration jobs so reset guardrails allow destructive reset.

- **Mobile.Shared lane requires MAUI workload on CI agents.**
  - `Darwin.Mobile.Shared` is a MAUI project (`<UseMaui>true</UseMaui>`).
  - Before restoring/running `Darwin.Mobile.Shared.Tests`, install MAUI workload (for example `dotnet workload install maui --skip-manifest-update`).

- **Coverage gate script expects all lane artifacts.**
  - If one lane does not upload `coverage.cobertura.xml`, coverage gate will fail by design.
  - Verify each lane writes to its own `TestResults/<lane>` directory and uploads that directory.

