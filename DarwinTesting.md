# 🧪 Darwin Testing Strategy & Architecture (Expanded)

This file is an expanded and opinionated testing guide for the Darwin platform. It builds on the existing testing guidance but adds practical utilities, examples and step-by-step patterns aimed at making test creation easy, consistent and robust across Domain, Application, Infrastructure, WebApi and Mobile layers.

Keep this document up-to-date as tests are added and as the CI pipeline evolves. All code examples and comments are in English.

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

---

## 1) Goals

- Confidence — tests prevent regressions for features and security rules.
- Maintainability — tests are readable, small, and follow the solution architecture.
- Speed — unit tests are fast; integration tests are reasonably fast and stable.
- Extensibility — tests are structured so new features can easily add tests.

---

## 2) Test projects & responsibilities

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

Note: Rename or restructure existing test projects to match these names to keep CI filter rules consistent.

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

## Closing notes & recommended next steps

1. Add `tests/Tests.Common` with the `WebApiTestFactory`, `TestDbFactory`, `TestAuthHelper`, and `TestClock`. These helpers will greatly reduce boilerplate in the integration tests.
2. Prioritize integration tests for the loyalty flow (Prepare/Create/Process/Confirm) — these are business-critical and will catch semantic issues early.
3. Add mapping unit tests (BusinessContractsMapper & LoyaltyContractsMapper) immediately — mapping mismatches are frequent causes of runtime errors.
4. Add contract serialization tests to protect mobile clients from accidental breaking changes.
