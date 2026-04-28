using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using Darwin.Contracts.Common;
using Darwin.WebApi.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Darwin.WebApi.Tests.Middleware;

public sealed class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_PassThrough_WhenNoExceptionOccurs()
    {
        var logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var middleware = new ErrorHandlingMiddleware(
            _ =>
            {
                return Task.CompletedTask;
            },
            logger.Object);

        var context = CreateContext("/health");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_Should_Translate_UnauthorizedAccessException_ToProblemDetails401()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new UnauthorizedAccessException("No access"),
            new Mock<ILogger<ErrorHandlingMiddleware>>().Object);
        var context = CreateContext("/secure");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/json");
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

        var body = await ReadResponseBodyAsync(context.Response.Body);
        var details = JsonSerializer.Deserialize<ProblemDetails>(body);
        details.Should().NotBeNull();
        details!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        details.Title.Should().Be("Unauthorized");
        details.Detail.Should().Be("No access");
        details.Instance.Should().Be("/secure");
    }

    [Fact]
    public async Task InvokeAsync_Should_Translate_GeneralException_ToProblemDetails500()
    {
        var middleware = new ErrorHandlingMiddleware(
            _ => throw new InvalidOperationException("Failure"),
            new Mock<ILogger<ErrorHandlingMiddleware>>().Object);
        var context = CreateContext("/orders");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.ContentType.Should().Be("application/json");
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

        var body = await ReadResponseBodyAsync(context.Response.Body);
        var details = JsonSerializer.Deserialize<ProblemDetails>(body);
        details.Should().NotBeNull();
        details!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        details.Title.Should().Be("API Error");
        details.Detail.Should().Be("Failure");
        details.Instance.Should().Be("/orders");
    }

    [Fact]
    public async Task InvokeAsync_Should_SkipWriting_WhenResponseHasAlreadyStarted()
    {
        var middleware = new ErrorHandlingMiddleware(
            async _ =>
            {
                _.Response.StatusCode = 418;
                await _.Response.WriteAsync("partial");
                throw new InvalidOperationException("Late failure");
            },
            new Mock<ILogger<ErrorHandlingMiddleware>>().Object);
        var context = CreateContext("/partial");
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(418);
        var body = await ReadResponseBodyAsync(context.Response.Body);
        body.Should().Be("partial");
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(Stream body)
    {
        body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
