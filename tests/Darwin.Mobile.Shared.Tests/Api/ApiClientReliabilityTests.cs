using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Resilience;
using Darwin.Mobile.Shared.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Darwin.Mobile.Shared.Tests.Api;

/// <summary>
///     Covers mobile-critical reliability behaviors in <see cref="ApiClient"/>:
///     retry execution, bearer-header synchronization with token storage,
///     and no-content endpoint normalization.
/// </summary>
public sealed class ApiClientReliabilityTests
{
    /// <summary>
    ///     Verifies that a non-expired access token stored in <see cref="ITokenStore"/>
    ///     is injected as a Bearer header before sending a request.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_ApplyBearerHeader_WhenStoredTokenIsValid()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var tokenStore = new FakeTokenStore(
            accessToken: "valid-access-token",
            accessExpiresUtc: DateTime.UtcNow.AddYears(1));

        AuthenticationHeaderValue? observedAuthorization = null;
        var handler = new StubHttpMessageHandler(_ =>
        {
            observedAuthorization = _.Headers.Authorization;
            return CreateJsonResponse(HttpStatusCode.OK, new { name = "darwin" });
        });

        var client = CreateApiClient(handler, tokenStore);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, string>>("/api/v1/meta/info", cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
        observedAuthorization.Should().NotBeNull();
        observedAuthorization!.Scheme.Should().Be("Bearer");
        observedAuthorization.Parameter.Should().Be("valid-access-token");
    }

    /// <summary>
    ///     Verifies that an expired access token is not sent to the server.
    ///     The client must clear Authorization header to prevent stale-token usage.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_ClearBearerHeader_WhenStoredTokenIsExpired()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var tokenStore = new FakeTokenStore(
            accessToken: "expired-access-token",
            accessExpiresUtc: DateTime.UtcNow.AddYears(-1));

        AuthenticationHeaderValue? observedAuthorization = new("Bearer", "placeholder");
        var handler = new StubHttpMessageHandler(request =>
        {
            observedAuthorization = request.Headers.Authorization;
            return CreateJsonResponse(HttpStatusCode.OK, new { ok = true });
        });

        var client = CreateApiClient(handler, tokenStore);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, bool>>("/api/v1/meta/health", cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
        observedAuthorization.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that when no access token is stored, the API client does not send
    ///     an Authorization header. This protects anonymous/bootstrap endpoints from
    ///     accidental credential leakage.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_NotApplyBearerHeader_WhenStoredTokenIsMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var tokenStore = new FakeTokenStore(
            accessToken: null,
            accessExpiresUtc: null);

        AuthenticationHeaderValue? observedAuthorization = new("Bearer", "placeholder");
        var handler = new StubHttpMessageHandler(request =>
        {
            observedAuthorization = request.Headers.Authorization;
            return CreateJsonResponse(HttpStatusCode.OK, new { ok = true });
        });

        var client = CreateApiClient(handler, tokenStore);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, bool>>("/api/v1/meta/health", cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
        observedAuthorization.Should().BeNull();
    }

    /// <summary>
    ///     Verifies that generic GET requests receiving HTTP 204 return a failed result
    ///     with the well-known <see cref="ApiClient.NoContentResultMessage"/> marker.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_ReturnNoContentFailureMarker_WhenServerReturns204()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateApiClient(handler);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, string>>("/api/v1/empty", cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ApiClient.NoContentResultMessage);
    }

    /// <summary>
    ///     Verifies that raw text GET requests can retrieve non-JSON payloads such as
    ///     member order or invoice document downloads.
    /// </summary>
    [Fact]
    public async Task GetStringResultAsync_Should_ReturnTextPayload_WhenServerReturnsPlainText()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Order: ORD-1001", Encoding.UTF8, "text/plain")
        });
        var client = CreateApiClient(handler);

        var result = await client.GetStringResultAsync("/api/v1/member/orders/1/document", cancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be("Order: ORD-1001");
    }

    /// <summary>
    ///     Verifies that command-style PUT endpoints returning HTTP 204 are normalized
    ///     to <c>Result.Ok()</c> by <see cref="ApiClient.PutNoContentAsync{TRequest}"/>.
    /// </summary>
    [Fact]
    public async Task PutNoContentAsync_Should_ReturnSuccess_WhenServerReturns204()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateApiClient(handler);

        // Act
        var result = await client.PutNoContentAsync("/api/v1/profile/me", new { firstName = "Ada" }, cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that command-style POST endpoints returning HTTP 204 are normalized
    ///     to <c>Result.Ok()</c> by <see cref="ApiClient.PostNoContentAsync{TRequest}"/>.
    /// </summary>
    [Fact]
    public async Task PostNoContentAsync_Should_ReturnSuccess_WhenServerReturns204()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateApiClient(handler);

        // Act
        var result = await client.PostNoContentAsync("/api/v1/auth/password/request-reset", new { email = "x@y.z" }, cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that transient transport failures are retried by
    ///     <see cref="ExponentialBackoffRetryPolicy"/> and eventually succeed when
    ///     a subsequent attempt returns a valid response.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_RetryTransientFailure_AndSucceedOnNextAttempt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;

            if (attempts == 1)
            {
                throw new HttpRequestException("Transient network glitch.");
            }

            return CreateJsonResponse(HttpStatusCode.OK, new { value = 42 });
        });

        var retryPolicy = new ExponentialBackoffRetryPolicy(maxAttempts: 3, baseDelay: TimeSpan.FromMilliseconds(1));
        var client = CreateApiClient(handler, retryPolicy: retryPolicy);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, int>>("/api/v1/retry-test", cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!["value"].Should().Be(42);
        attempts.Should().Be(2);
    }


    /// <summary>
    ///     Verifies guard clause behavior: an empty route should fail fast without
    ///     issuing an HTTP request in <see cref="ApiClient.GetResultAsync{TResponse}"/>.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_FailFast_WhenRouteIsEmpty()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var sendCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            sendCount++;
            return CreateJsonResponse(HttpStatusCode.OK, new { ok = true });
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, bool>>(string.Empty, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Route is required.");
        sendCount.Should().Be(0);
    }

    /// <summary>
    ///     Verifies guard clause behavior for command APIs: null request payload
    ///     must return a failed result without issuing an HTTP request.
    /// </summary>
    [Fact]
    public async Task PostResultAsync_Should_FailFast_WhenRequestPayloadIsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var sendCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            sendCount++;
            return CreateJsonResponse(HttpStatusCode.OK, new { ok = true });
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.PostResultAsync<object, Dictionary<string, bool>>("/api/v1/test", request: null!, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request payload is required.");
        sendCount.Should().Be(0);
    }

    /// <summary>
    ///     Verifies guard clause behavior for PUT command APIs: null request payload
    ///     must return a failed result without issuing an HTTP request.
    /// </summary>
    [Fact]
    public async Task PutResultAsync_Should_FailFast_WhenRequestPayloadIsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var sendCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            sendCount++;
            return CreateJsonResponse(HttpStatusCode.OK, new { ok = true });
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.PutResultAsync<object, Dictionary<string, bool>>("/api/v1/test", request: null!, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Request payload is required.");
        sendCount.Should().Be(0);
    }

    /// <summary>
    ///     Verifies route normalization for GET calls trims leading slashes before request dispatch.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_NormalizeLeadingSlashRoute_BeforeDispatch()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        Uri? observedRequestUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            observedRequestUri = request.RequestUri;
            return CreateJsonResponse(HttpStatusCode.OK, new { ok = true });
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, bool>>("/api/v1/meta/health", cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
        observedRequestUri.Should().NotBeNull();
        observedRequestUri!.AbsolutePath.Should().Be("/api/v1/meta/health");
    }

    /// <summary>
    ///     Verifies success-status responses with malformed JSON are converted to
    ///     a deterministic failure result instead of throwing to callers.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_ReturnFailure_WhenSuccessPayloadJsonIsInvalid()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{invalid-json", Encoding.UTF8, "application/json")
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, string>>("/api/v1/invalid-json", cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().StartWith("Invalid JSON payload:");
    }

    /// <summary>
    ///     Verifies retry exhaustion behavior: when all attempts fail with transport exceptions,
    ///     the client must return a failed result instead of throwing to UI callers.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_ReturnFailure_WhenTransientFailureExceedsRetryAttempts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            throw new HttpRequestException("Persistent network outage.");
        });

        var retryPolicy = new ExponentialBackoffRetryPolicy(maxAttempts: 2, baseDelay: TimeSpan.FromMilliseconds(1));
        var client = CreateApiClient(handler, retryPolicy: retryPolicy);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, string>>("/api/v1/retry-fail", cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        attempts.Should().Be(2);
    }

    /// <summary>
    ///     Verifies that command-style no-content helper surfaces failure for non-success HTTP status.
    /// </summary>
    [Fact]
    public async Task PostNoContentAsync_Should_ReturnFailure_WhenServerReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"message\":\"validation failed\"}", Encoding.UTF8, "application/json")
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.PostNoContentAsync("/api/v1/failing-command", new { any = "payload" }, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that generic command-style POST calls return a failed result when
    ///     the server returns Unauthorized. The client must normalize HTTP status failures
    ///     to <c>Result.Fail(...)</c> instead of throwing into UI call paths.
    /// </summary>
    [Fact]
    public async Task PostResultAsync_Should_ReturnFailure_WhenServerReturnsUnauthorized()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"message\":\"unauthorized\"}", Encoding.UTF8, "application/json")
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.PostResultAsync<object, Dictionary<string, string>>(
            "/api/v1/protected",
            new { any = "payload" },
            cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies retry exhaustion behavior for command-style no-content operations:
    ///     persistent transport failures should surface as failed results after all retry attempts.
    /// </summary>
    [Fact]
    public async Task PutNoContentAsync_Should_ReturnFailure_WhenTransportFailureExceedsRetryAttempts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var attempts = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempts++;
            throw new HttpRequestException("Persistent transport failure.");
        });

        var retryPolicy = new ExponentialBackoffRetryPolicy(maxAttempts: 2, baseDelay: TimeSpan.FromMilliseconds(1));
        var client = CreateApiClient(handler, retryPolicy: retryPolicy);

        // Act
        var result = await client.PutNoContentAsync("/api/v1/profile/me", new { firstName = "Ada" }, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        attempts.Should().Be(2);
    }

    /// <summary>
    ///     Verifies that command-style POST result calls normalize malformed success JSON
    ///     into a deterministic failure result instead of throwing to callers.
    /// </summary>
    [Fact]
    public async Task PostResultAsync_Should_ReturnFailure_WhenSuccessPayloadJsonIsInvalid()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{invalid-json", Encoding.UTF8, "application/json")
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.PostResultAsync<object, Dictionary<string, string>>(
            "/api/v1/invalid-json-post",
            new { any = "payload" },
            cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Error.Should().StartWith("Invalid JSON payload:");
    }

    /// <summary>
    ///     Verifies that HTTP 5xx responses are normalized to failed result values
    ///     for generic GET calls, preserving reliability expectations for UI consumers.
    /// </summary>
    [Fact]
    public async Task GetResultAsync_Should_ReturnFailure_WhenServerReturnsInternalServerError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"message\":\"unexpected failure\"}", Encoding.UTF8, "application/json")
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.GetResultAsync<Dictionary<string, string>>("/api/v1/server-error", cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    /// <summary>
    ///     Verifies that HTTP 5xx responses are normalized to failed result values
    ///     for command-style POST result calls.
    /// </summary>
    [Fact]
    public async Task PostResultAsync_Should_ReturnFailure_WhenServerReturnsInternalServerError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Arrange
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"message\":\"unexpected failure\"}", Encoding.UTF8, "application/json")
        });

        var client = CreateApiClient(handler);

        // Act
        var result = await client.PostResultAsync<object, Dictionary<string, string>>(
            "/api/v1/server-error-post",
            new { any = "payload" },
            cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    /// <summary>
    ///     Creates an <see cref="ApiClient"/> with deterministic test doubles.
    /// </summary>
    private static ApiClient CreateApiClient(
        HttpMessageHandler handler,
        ITokenStore? tokenStore = null,
        IRetryPolicy? retryPolicy = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost")
        };

        return new ApiClient(
            httpClient,
            retryPolicy ?? new ExponentialBackoffRetryPolicy(maxAttempts: 1, baseDelay: TimeSpan.FromMilliseconds(1)),
            tokenStore ?? new FakeTokenStore(),
            new ServiceCollection().BuildServiceProvider(),
            TimeProvider.System);
    }

    /// <summary>
    ///     Serializes a small anonymous object payload into a JSON response.
    /// </summary>
    private static HttpResponseMessage CreateJsonResponse<T>(HttpStatusCode statusCode, T payload)
    {
        var json = JsonSerializer.Serialize(payload);

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    /// <summary>
    ///     Minimal token store test double with deterministic values.
    /// </summary>
    private sealed class FakeTokenStore : ITokenStore
    {
        private readonly string? _accessToken;
        private readonly DateTime? _accessExpiresUtc;

        public FakeTokenStore(string? accessToken = null, DateTime? accessExpiresUtc = null)
        {
            _accessToken = accessToken;
            _accessExpiresUtc = accessExpiresUtc;
        }

        public Task SaveAsync(string accessToken, DateTime accessExpiresUtc, string refreshToken, DateTime refreshExpiresUtc)
            => Task.CompletedTask;

        public Task<(string? AccessToken, DateTime? AccessExpiresUtc)> GetAccessAsync()
            => Task.FromResult((_accessToken, _accessExpiresUtc));

        public Task<(string? RefreshToken, DateTime? RefreshExpiresUtc)> GetRefreshAsync()
            => Task.FromResult<(string?, DateTime?)>((null, null));

        public Task ClearAsync() => Task.CompletedTask;
    }

    /// <summary>
    ///     Delegates HTTP response creation to a function, allowing deterministic
    ///     per-request behavior in unit tests without external mocking frameworks.
    /// </summary>
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responseFactory(request));
    }
}
