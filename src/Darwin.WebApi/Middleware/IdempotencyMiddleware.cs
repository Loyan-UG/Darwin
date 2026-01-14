using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Darwin.WebApi.Middleware
{
    /// <summary>
    /// Simple idempotency middleware that uses IMemoryCache to store HTTP responses
    /// for requests that include an "Idempotency-Key" header.
    ///
    /// Behaviour:
    /// - If a request contains an Idempotency-Key and a cached response exists, returns it.
    /// - If a request contains an Idempotency-Key and the key is marked "in-progress",
    ///   returns HTTP 409 Conflict (avoiding concurrent execution).
    /// - Otherwise, marks the key "in-progress", lets the request execute, captures the
    ///   response body and status code, stores them in cache and returns the response.
    ///
    /// Notes / limitations:
    /// - This is an in-memory, per-process idempotency guard. It is NOT durable across
    ///   process restarts and does not replace a DB-backed dedupe for financial operations.
    /// - Use relatively short TTLs (e.g., 5 minutes) to avoid unbounded memory growth.
    /// - The middleware is generic; controllers/clients must opt-in by sending the header.
    /// </summary>
    public sealed class IdempotencyMiddleware
    {
        private const string HeaderKey = "Idempotency-Key";
        private const string CachePrefix = "idempotency:";
        private const string InProgressMarker = "__IN_PROGRESS__";

        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IdempotencyMiddleware> _logger;
        private readonly TimeSpan _entryTtl = TimeSpan.FromMinutes(5);

        public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<IdempotencyMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));

            // Only consider POST/PUT/PATCH/DELETE as "mutating" by default.
            // Clients may still send header for GET but it's typically unnecessary.
            var method = context.Request.Method;
            if (string.IsNullOrWhiteSpace(method) ||
                !(method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
                  || method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase)
                  || method.Equals(HttpMethods.Patch, StringComparison.OrdinalIgnoreCase)
                  || method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            if (!context.Request.Headers.TryGetValue(HeaderKey, out var keyValues))
            {
                // No idempotency header -> proceed normally.
                await _next(context).ConfigureAwait(false);
                return;
            }

            var idempotencyKey = keyValues.ToString().Trim();
            if (string.IsNullOrEmpty(idempotencyKey))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            var cacheKey = CachePrefix + idempotencyKey;

            // Check cache
            if (_cache.TryGetValue(cacheKey, out IdempotencyEntry? existingEntry))
            {
                if (existingEntry.IsInProgress)
                {
                    _logger.LogInformation("Idempotency key {Key} is already in-progress. Returning 409.", idempotencyKey);
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    await context.Response.WriteAsync("Request already in progress.").ConfigureAwait(false);
                    return;
                }

                // Return cached response
                _logger.LogDebug("Idempotency key {Key} found in cache. Returning cached response.", idempotencyKey);
                context.Response.StatusCode = existingEntry.StatusCode;
                context.Response.ContentType = existingEntry.ContentType ?? "application/json";
                if (existingEntry.Body is not null && existingEntry.Body.Length > 0)
                {
                    await context.Response.Body.WriteAsync(existingEntry.Body, 0, existingEntry.Body.Length).ConfigureAwait(false);
                }
                return;
            }

            // Reserve the key as in-progress to prevent concurrent execution.
            var inProgress = new IdempotencyEntry { IsInProgress = true };
            _cache.Set(cacheKey, inProgress, _entryTtl);

            // Capture the response body
            var originalBodyStream = context.Response.Body;
            await using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                await _next(context).ConfigureAwait(false);

                // Read response
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var respBytes = new byte[memoryStream.Length];
                await context.Response.Body.ReadAsync(respBytes, 0, respBytes.Length).ConfigureAwait(false);

                // Create final entry and cache it
                var entry = new IdempotencyEntry
                {
                    IsInProgress = false,
                    StatusCode = context.Response.StatusCode,
                    ContentType = context.Response.ContentType,
                    Body = respBytes
                };

                // Replace cache entry atomically
                _cache.Set(cacheKey, entry, _entryTtl);

                // Rewind and copy to original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await context.Response.Body.CopyToAsync(originalBodyStream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing idempotent request with key {Key}", idempotencyKey);
                // Remove in-progress marker to allow retries after failure.
                _cache.Remove(cacheKey);

                // Re-throw so global error handling middleware can translate to problem details.
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private sealed class IdempotencyEntry
        {
            public bool IsInProgress { get; init; }
            public int StatusCode { get; init; } = 200;
            public string? ContentType { get; init; }
            public byte[]? Body { get; init; }
        }
    }
}