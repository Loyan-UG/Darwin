# 🧪 Darwin Testing Strategy & Architecture

Darwin has been built with **clean architecture** from day one.  To keep that
architecture healthy and future‑proof, testing is just as important as the
production code.  This guide describes how to structure and write tests
for Darwin’s various layers — Domain, Application, Infrastructure, WebApi and
Mobile — so that quality scales alongside features.

## 🎯 Goals

- **Confidence**: every feature and bug fix should have tests that prevent
  regressions.
- **Maintainability**: the test suite should mirror the solution layout and
  remain easy to navigate as the codebase grows.
- **Speed**: keep unit tests fast and isolated; integration tests may be
  slower but should still run inside CI in a reasonable time.
- **Extensibility**: leave room for additional platforms (e.g. future MAUI
  apps) without forcing a wholesale rewrite of the test structure.

---

## 🏗️ Proposed Test Projects

To align with the solution’s clean architecture while avoiding unnecessary
bloat, the `tests/` folder should host **separate test projects per
architecture layer**.  Each project targets a specific audience (unit vs.
integration) and references only the production assemblies it exercises.

| Test Project             | Purpose                                                      | Target Frameworks | References |
|--------------------------|--------------------------------------------------------------|-------------------|------------|
| **Darwin.UnitTests**     | Pure unit tests for **Domain**, **Application** and **Shared** logic.  No I/O, no DB.  Uses fakes/mocks. | `net10.0`         | `Darwin.Domain`, `Darwin.Application`, `Darwin.Shared` |
| **Darwin.WebApi.Tests**  | Integration tests for the **WebApi**: spin up an in‑memory `WebApplicationFactory<Program>` to test controllers/endpoints, policies and filters against a test database. | `net10.0` | `Darwin.WebApi`, `Darwin.Infrastructure` |
| **Darwin.Infrastructure.Tests** (optional) | Tests targeting EF Core **Infrastructure** components directly: migrations, mappings, repository patterns.  Uses SQLite in‑memory to enforce constraints. | `net10.0` | `Darwin.Infrastructure` |
| **Darwin.Mobile.Shared.Tests** | Unit tests for the **mobile shared library** (`Darwin.Mobile.Shared`): API client behaviour, retry policy, token store abstractions. | `net10.0` | `Darwin.Mobile.Shared` |

> The existing `Darwin.Tests.Unit` project can be **renamed to `Darwin.UnitTests`** and expanded.  The empty `Darwin.Tests.Integration` project should be replaced by `Darwin.WebApi.Tests` (and `Darwin.Infrastructure.Tests` if you decide to separate infrastructure tests).

### Darwin.UnitTests

- **Scope**: Domain entities/ValueObjects, Application handlers, validators,
  helpers in `Darwin.Shared` and `Darwin.Contracts`.
- **Tools**: `xUnit` for the test framework; `FluentAssertions` for
  expressive assertions; optional `Moq` or `NSubstitute` for stubbing
  interfaces when required.
- **DB Access**: avoid hitting a real database.  For tests that need a
  `DbContext` (e.g. validators), use an **in‑memory `IAppDbContext`**
  factory similar to the existing `TestDbFactory`.  Note that the EF
  InMemory provider does not enforce relational constraints; if you need
  constraints, promote the test into an integration test and use SQLite.
- **Structure**: mirror the namespaces of the production code:

  ```text
  tests/Darwin.UnitTests/
  ├── Domain/
  │   └── Loyalty/
  │       └── LoyaltyScanModeTests.cs
  ├── Application/
  │   ├── Catalog/
  │   │   └── ProductUniqueSlugValidatorTests.cs
  │   ├── CMS/
  │   │   └── PageUniqueSlugValidatorTests.cs
  │   └── Settings/
  │       └── SiteSettingsHandlerTests.cs
  ├── Shared/
  │   └── HtmlSanitizerHelperTests.cs
  └── Contracts/
      └── TokenResponseSerializationTests.cs

### Darwin.WebApi.Tests

- **Scope**: End‑to‑end testing of API endpoints, filters and policies.  Each
  test starts a lightweight web server in memory using
  `WebApplicationFactory<Program>` (from `Microsoft.AspNetCore.Mvc.Testing`) and
  uses an `HttpClient` to make real HTTP calls against the API.
- **Database**: configure the WebApi host to use an **in‑memory SQLite
  database** for each test run.  This ensures that relational constraints,
  concurrency tokens, soft‑delete filters, etc. behave like production.
- **Auth & Policies**: provide helper methods to authenticate requests
  (e.g. seeding test users and generating JWT tokens).  You can stub
  `ICurrentUserService` when testing endpoints that require certain
  claims/permissions.
- **Structure**: group tests by controller area:

  ```text
  tests/Darwin.WebApi.Tests/
  ├── Auth/
  │   └── LoginEndpointTests.cs
  ├── Profile/
  │   └── ProfileEndpointsTests.cs
  ├── Loyalty/
  │   ├── PrepareScanSessionTests.cs
  │   ├── ProcessScanSessionTests.cs
  │   └── ConfirmAccrualTests.cs
  ├── Businesses/
  │   └── DiscoveryEndpointTests.cs
  └── Common/
      └── ApiProblemTests.cs




### Darwin.Infrastructure.Tests

- **Scope**: EF Core migrations, configuration and repository patterns.  Test
  that migrations apply correctly, indexes are created, and mapping
  conventions enforce constraints like max lengths, required fields, and
  concurrency tokens.
- **Database**: use **SQLite in-memory mode** with `UseSqlite("DataSource=:memory:")`
  and `OpenConnection()` to create a relational database.  Run migrations
  against it before executing tests.
- **Example Tests**:
  - Ensure `ProductTranslation.Slug` is limited to 200 characters.
  - Verify that soft‑deleted rows are not returned by default queries.
  - Validate seeding logic in `DataSeeder`.

### Darwin.Mobile.Shared.Tests

- **Scope**: `ApiClient`, retry policies, token storage abstractions,
  service facades (`ILoyaltyService`, `IIdentityService`, etc.).  These
  tests should avoid network calls by mocking out the underlying
  `HttpMessageHandler` of `HttpClient`.
- **Approach**:
  - Use `DelegatingHandler` stubs to simulate API responses (success,
    network failures, timeouts).
  - Test that the retry policy waits and re‑tries appropriately.
  - Validate that token refresh is invoked when a 401 response is
    encountered and that the `ITokenStore` is updated.

---


## 🔧 Common Patterns & Guidelines

### 1. Test Naming

Follow the **`MethodUnderTest_StateUnderTest_ExpectedResult`** pattern to
communicate behaviour clearly.  When using xUnit, method names can be long
and descriptive because they don't clutter a test runner UI.

```csharp
[Fact]
public async Task ProcessScanSession_Should_Return_NotFound_When_Token_Expired() { /* … */ }
```


### 2. Arrange–Act–Assert (AAA)

Structure each test into clear sections:
**1. Arrange:** set up the system under test (SUT), dependencies and data.
**2. Act:** invoke the SUT.
**3. Assert:** verify the result/state. Use a single logical assertion per
test; additional assertions are fine when they strengthen the same
behaviour.



### 3. Avoid Test Interdependence

Each test should be **independent** and reproducible. Use fresh
in‑memory databases or reset static/shared state between tests. In
integration tests, you can create a new host per test or share a host
across a class and reset the database state in IAsyncLifetime hooks.



### 4. Use Theory Data for Variations

When validating multiple input variations for the same logic, use xUnit
[Theory] with InlineData or MemberData to reduce duplication. For
example, verifying allowed cultures or slug lengths can be consolidated
into one theory.

```csharp
[Theory]
[InlineData("de-DE", "foo", true)]
[InlineData("en-US", "foo", false)]
public async Task SlugValidator_Should_Enforce_Uniqueness(string culture, string slug, bool expectedValid) { /* … */ }
```


### 5. Test Utilities

Create helper classes under a Tests.Common namespace to reduce
boilerplate:

- TestHostBuilder: builds a WebApplicationFactory with test services
registered (e.g. SQLite DB, seeded users, stubbed email services).

- FakeCurrentUserService: implements ICurrentUserService to
simulate authenticated users with different roles/permissions.

- JwtTokenGenerator: issues JWT tokens for authenticated requests
during WebApi tests.

- JsonExtensions: helper methods to serialize/deserialize request
payloads and read ProblemDetails from responses.

- 

### 6. Code Coverage

Use coverlet to collect code coverage in CI. Focus on
**business logic**; auto‑generated code or trivial boilerplate (e.g.
program or startup classes) can be excluded via configuration in
Directory.Build.props.



### 7. Running Tests

The root solution defines a **solution filter** Test.slnf that loads only
the test projects and their dependencies for faster IDE start‑up. You can
run the entire test suite with:

```bash
# run all tests
dotnet test Test.slnf --verbosity minimal

# run only WebApi tests
dotnet test tests/Darwin.WebApi.Tests
```

In CI, the GitHub Action defined in build.yml already executes tests and
uploads coverage reports.



## 🚦 Adding Tests for New Features

When developing a new feature, aim to write tests along with the
implementation.  Use the **Test Pyramid** as a guideline:

1. **Unit tests** for the business logic (Domain & Application).  These
   should cover entities, value objects, validation, and use‑case
   handlers without touching external dependencies.
2. **Integration tests** for endpoints and persistence.  Use the test
   host to exercise API controllers or infrastructure classes with a
   real database provider.
3. **End‑to‑end (E2E) tests** for UI flows (web or mobile).  These
   simulate user interactions.  They are the most expensive tests and
   should be used sparingly to cover critical paths.  Playwright can
   automate Web UI flows; .NET MAUI’s UITest framework (future) can
   automate mobile flows.

### Example: Testing a New WebApi Endpoint

Suppose you add an endpoint to list active promotions:

1. **Unit Test**: Add tests to `Darwin.UnitTests` verifying that the
   query/handler returns the expected DTOs when given various
   promotion states (active/inactive, culture, business id).  Use
   `TestDbFactory` or a `Mock<IPromotionRepository>`.
2. **Integration Test**: In `Darwin.WebApi.Tests`, create a test that
   seeds the database with sample promotions, calls `/api/v1/promotions`
   using an `HttpClient`, and asserts that the JSON response matches
   the expected structure and filtering rules.

---

## 📦 Directory.Build.props for Test Projects

To enforce consistent test settings and package references across all test
projects, add a `Directory.Build.props` file under `tests/`:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.11.0" />
    <PackageReference Include="Moq" Version="4.20.76" />
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```


## 📃 Conclusion

By aligning the test suite with Darwin’s clean architecture, you build a
foundation for **reliable, maintainable** and **scalable** quality
assurance. The proposed projects and guidelines keep the scope of each
test clear, making it easier for contributors to find where to place new
tests and understand the behaviour being exercised. As the platform
expands — adding more endpoints, domains or mobile features — you can
introduce additional test projects (e.g. for MAUI UI tests) without
breaking the existing structure.