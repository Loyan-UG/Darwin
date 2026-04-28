using System.IO;
using System.Text;
using System.Threading.Tasks;
using Darwin.WebApi.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Darwin.WebApi.Tests.Middleware;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDependenciesAreMissing()
    {
        var logger = new Mock<ILogger<IdempotencyMiddleware>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        Action noNext = () => new IdempotencyMiddleware(null!, cache, logger.Object);
        Action noCache = () => new IdempotencyMiddleware(_ => Task.CompletedTask, null!, logger.Object);
        Action noLogger = () => new IdempotencyMiddleware(_ => Task.CompletedTask, cache, null!);

        noNext.Should().Throw<ArgumentNullException>().WithParameterName("next");
        noCache.Should().Throw<ArgumentNullException>().WithParameterName("cache");
        noLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task InvokeAsync_Should_Continue_WhenMethodIsNotMutating()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var nextCalled = 0;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled++;
            return Task.CompletedTask;
        }, cache);

        var context = CreateContext(HttpMethods.Get, "/health");

        await middleware.InvokeAsync(context);

        nextCalled.Should().Be(1);
        cache.TryGetValue("idempotency:read-1", out var _).Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Should_Continue_WhenIdempotencyHeaderIsMissingOrBlank()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var nextCalled = 0;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled++;
            return Task.CompletedTask;
        }, cache);
        var missingHeader = CreateContext(HttpMethods.Post, "/orders");
        var blankHeader = CreateContext(HttpMethods.Post, "/orders");
        blankHeader.Request.Headers["Idempotency-Key"] = "   ";

        await middleware.InvokeAsync(missingHeader);
        await middleware.InvokeAsync(blankHeader);

        nextCalled.Should().Be(2);
        cache.Count.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_Should_CacheSuccessfulResponse_AndReturnCachedResponseOnReplay()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var middleware = CreateMiddleware(async context =>
        {
            context.Response.StatusCode = 201;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("created");
        }, cache);
        var key = "orders-create";
        var context1 = CreateContext(HttpMethods.Post, "/orders");
        context1.Request.Headers["Idempotency-Key"] = key;

        var context2 = CreateContext(HttpMethods.Post, "/orders");
        context2.Request.Headers["Idempotency-Key"] = key;

        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        context1.Response.StatusCode.Should().Be(201);
        context1.Response.ContentType.Should().Be("application/json");
        (await ReadBodyAsync(context1.Response.Body)).Should().Be("created");

        context2.Response.StatusCode.Should().Be(201);
        context2.Response.ContentType.Should().Be("application/json");
        (await ReadBodyAsync(context2.Response.Body)).Should().Be("created");
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnConflict_WhenInProgressEntryExists()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var key = "in-progress";

        var firstMiddleware = CreateMiddleware(async context =>
        {
            context.Response.StatusCode = 202;
            await context.Response.WriteAsync("processing");
            firstStarted.TrySetResult();
            await continueFirst.Task;
        }, cache);

        var firstContext = CreateContext(HttpMethods.Post, "/orders");
        firstContext.Request.Headers["Idempotency-Key"] = key;
        firstContext.Response.Body = new MemoryStream();

        var firstTask = firstMiddleware.InvokeAsync(firstContext);
        await firstStarted.Task;

        var secondCalled = 0;
        var secondMiddleware = CreateMiddleware(_ =>
        {
            secondCalled++;
            return Task.CompletedTask;
        }, cache);
        var secondContext = CreateContext(HttpMethods.Post, "/orders");
        secondContext.Request.Headers["Idempotency-Key"] = key;
        secondContext.Response.Body = new MemoryStream();

        await secondMiddleware.InvokeAsync(secondContext);
        secondCalled.Should().Be(0);
        secondContext.Response.StatusCode.Should().Be(409);
        var secondBody = await ReadBodyAsync(secondContext.Response.Body);
        secondBody.Should().Be("Request already in progress.");

        continueFirst.TrySetResult();
        await firstTask;
    }

    [Fact]
    public async Task InvokeAsync_Should_RemoveInProgressMarker_WhenDownstreamThrows()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("boom"), cache);
        var key = "idempotent-fail";
        var context = CreateContext(HttpMethods.Post, "/orders");
        context.Request.Headers["Idempotency-Key"] = key;
        context.Response.Body = new MemoryStream();

        var act = () => middleware.InvokeAsync(context);
        await act.Should().ThrowAsync<InvalidOperationException>();

        cache.TryGetValue($"idempotency:{key}", out var _).Should().BeFalse();
    }

    private static IdempotencyMiddleware CreateMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        return new IdempotencyMiddleware(next, cache, new Mock<ILogger<IdempotencyMiddleware>>().Object);
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(Stream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
