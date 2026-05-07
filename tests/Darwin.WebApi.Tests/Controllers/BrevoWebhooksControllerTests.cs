using System.Text;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using ContractProblemDetails = Darwin.Contracts.Common.ProblemDetails;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Darwin.Infrastructure.Notifications.Brevo;
using Darwin.WebApi;
using Darwin.WebApi.Controllers.Public;
using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Darwin.WebApi.Tests.Controllers;

public sealed class BrevoWebhooksControllerTests
{
    private const string BrevoUserName = "brevo-user";
    private const string BrevoPassword = "brevo-password";

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenWebhookCredentialsAreNotConfigured()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions());

        SetRequestBody(controller, CreatePayload());
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenWebhookCredentialsAreWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = "   ",
            WebhookPassword = "   "
        });

        SetRequestBody(controller, CreatePayload());
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenWebhookUsernameIsWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = "   ",
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, CreatePayload(), BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenWebhookPasswordIsWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = "   "
        });

        SetRequestBody(controller, CreatePayload(), BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenBasicAuthIsInvalid()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, CreatePayload(), BuildBasicAuth("wrong-user", BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenBasicAuthPasswordIsMissing()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, CreatePayload(), BuildMalformedAuth($"{BrevoUserName}:"));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenAuthorizationSchemeIsNotBasic()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, CreatePayload(), "Bearer token");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenAuthorizationSchemeIsLowercaseBasic()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, CreatePayload(), BuildBasicAuth(BrevoUserName, BrevoPassword).Replace("Basic ", "basic "));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenAuthorizationHeaderIsMissing()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, CreatePayload());
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, payload, BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit_DueToUtf8ByteLength()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var unicodePayload = """{"event":"opened","message-id":"MSG-UNICODE","ts_event":"1714689600","x":""}"""
            .PadRight(ProviderWebhookPayloadReader.MaxPayloadBytes + 50, 'é');
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, unicodePayload, BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenContentLengthIsZeroButBodyHasContent()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var payload = """{"event":"opened","message-id":"MSG-CONTENT-LEN-0","ts_event":"1714689600"}""";
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, payload, BuildBasicAuth(BrevoUserName, BrevoPassword));
        controller.ControllerContext!.HttpContext!.Request.ContentLength = 0;

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenContentLengthIsNull()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var payload = """{"event":"opened","message-id":"MSG-CONTENT-LEN-NULL","ts_event":"1714689600"}""";
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, payload, BuildBasicAuth(BrevoUserName, BrevoPassword));
        controller.ControllerContext!.HttpContext!.Request.ContentLength = null;

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenPrimaryMessageIdTypeInvalidButMessageIdLongIsPresent()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"clicked","message-id":123,"messageIdLong":"MSG-ALIAS","ts":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadIsMalformedJson()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, "{", BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadIsWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, "   ", BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenAuthorizationTokenMissingColon()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            CreatePayload(),
            BuildMalformedAuth("nocolon"));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenRequiredFieldsMissing()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"message-id":"MSG-1","ts_event":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEventIsNotString()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"Event":123,"message-id":"MSG-1","Date":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenMessageIdAliasesAreMissing()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","Date":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenMessageIdTypeIsInvalid()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","messageId":123,"ts_event":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenTimestampIsInvalidType()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","messageId":"MSG-1","ts_event":{}}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenMessageIdAndTimestampUseAlternateFieldNames()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"Opened","messageId":"MSG_ALT_1","ts":1714689600}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenMessageIdAndTimestampUseAlternativeDateFieldNames()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"Opened","message_id":"MSG_ALT_2","Date":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_AcceptPayload_WhenEnvelopeIsValidWithCaseInsensitiveEvent()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, """{"Event":"BOUNCED","messageId":"MSG-1","ts_event":"1714689600"}""", BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_AcceptPayload_WhenEventIsLongerThan64Characters()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        var payload = $$"""{"event":"{{new string('e', 128)}}","message-id":"MSG-LONG","ts_event":"1714689600"}""";
        SetRequestBody(controller, payload, BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenEventNameIsLongerThan64Characters()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var options = new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        };

        var firstPayload = $$"""{"event":"{{new string('e', 128)}}","message-id":"MSG-LONG-KEY","ts_event":"1714689600"}""";
        var secondPayload = $$"""{"event":"{{new string('e', 64)}}{{new string('x', 64)}}","message-id":"MSG-LONG-KEY","ts_event":"1714689600"}""";
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);

        var firstController = CreateController(db, options);
        SetRequestBody(firstController, firstPayload, authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, options);
        SetRequestBody(secondController, secondPayload, authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenTimestampDateFieldIsNumber()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","messageId":"MSG-NUM","Date":1714689600}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenEventNameAndMessageIdHaveExtraWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"Event":"  DELIVERED  ","message-id":"  MSG-WRAP  ","ts_event":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenMessageIdIsWrappedInWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var firstController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        var secondController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);

        SetRequestBody(firstController, """{"event":"clicked","message-id":"MSG-WS-DUP","ts_event":"1714689600"}""", authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        SetRequestBody(secondController, """{"event":"clicked","message-id":"  MSG-WS-DUP  ","ts_event":"1714689600"}""", authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_AcceptPayload_WhenPayloadIsExactlyAtSizeLimit()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var payload = CreatePayloadWithExactMaxLength();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        Encoding.UTF8.GetByteCount(payload).Should().Be(ProviderWebhookPayloadReader.MaxPayloadBytes);
        SetRequestBody(controller, payload, BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenPrimaryMessageIdIsEmptyButMessageIdLongIsPresent()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","message-id":"","messageIdLong":"MSG-ALIAS-ONLY","ts_event":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenMessageIdLongAliasIsUsed()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","messageIdLong":"MSG-ALIAS","ts":1714689600}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenTimestampAliasIsWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","messageId":"MSG-WS","Date":"   "}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenTimestampAliasTsEventIsNumber()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"clicked","messageId":"MSG-NUM","ts_event":1714689600}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenEventCasingChangesButIdempotencyKeyMatches()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var firstPayload = CreatePayload(messageId: "MSG-DUP", eventName: "OpenED");
        var secondPayload = CreatePayload(messageId: "MSG-DUP", eventName: "opened");
        var options = new BrevoEmailOptions { WebhookUsername = BrevoUserName, WebhookPassword = BrevoPassword };
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);

        var firstController = CreateController(db, options);
        SetRequestBody(firstController, firstPayload, authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, options);
        SetRequestBody(secondController, secondPayload, authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenMessageIdAliasChangesButValuesMatch()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var firstPayload = """{"event":"clicked","message-id":"MSG-ALIAS","ts_event":"1714689600"}""";
        var secondPayload = """{"event":"clicked","messageIdLong":"MSG-ALIAS","ts":"1714689600"}""";
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);

        var firstController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(firstController, firstPayload, authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(secondController, secondPayload, authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenSameBrevoCallbackArrivesTwice()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var payload = CreatePayload();
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);

        var firstController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(firstController, payload, authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(secondController, payload, authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateFalse_WhenSameEventButDifferentTimestampAlias()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var firstPayload = CreatePayload(eventName: "opened", messageId: "MSG-DIFF", timestamp: "1714689600");
        var secondPayload = """{"event":"Opened","messageId":"MSG-DIFF","Date":"1714689601"}""";
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);

        var firstController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(firstController, firstPayload, authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(secondController, secondPayload, authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenMessageIdHasAlternativeAliasAndUppercaseEvent()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var firstController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        var secondController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            firstController,
            """{"message_id":"MSG-ALIAS","event":"opened","ts":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        SetRequestBody(
            secondController,
            """{"messageId":"MSG-ALIAS","Event":"OPENED","ts_epoch":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenPayloadUsesCaseVariantFieldNames()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"EvEnT":"opened","MeSsAgE-iD":"MSG-MIX-CASE","Ts":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenCaseVariantAndCanonicalFieldNamesRepresentSameWebhook()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);
        var firstPayload = """{"event":"opened","message-id":"MSG-MIX-CASE","ts_event":1714689600}""";
        var secondPayload = """{"EvEnT":"OPENED","MeSsAgE-iD":"MSG-MIX-CASE","Ts":"1714689600"}""";

        var firstController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(firstController, firstPayload, authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(secondController, secondPayload, authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenDateTimestampAliasUsesUpperCaseFieldName()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"opened","message-id":"MSG-DATE","DATE":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (_, duplicate) = AssertOk(result);
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenTimestampAliasCaseVariantRepresentsSameWebhook()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var authHeader = BuildBasicAuth(BrevoUserName, BrevoPassword);
        var firstPayload = """{"event":"opened","message-id":"MSG-DATE-DUP","Date":"1714689600"}""";
        var secondPayload = """{"event":"opened","message-id":"MSG-DATE-DUP","DATE":"1714689600"}""";

        var firstController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(firstController, firstPayload, authHeader);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });
        SetRequestBody(secondController, secondPayload, authHeader);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenAuthorizationHeaderNotBase64()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(controller, CreatePayload(), "Basic !!!not-base64!!!");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEventNameIsWhitespace()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        SetRequestBody(
            controller,
            """{"event":"   ","message-id":"MSG-1","ts_event":"1714689600"}""",
            BuildBasicAuth(BrevoUserName, BrevoPassword));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenAuthenticationInvalid_EvenIfPayloadIsOversized()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();
        var controller = CreateController(db, new BrevoEmailOptions
        {
            WebhookUsername = BrevoUserName,
            WebhookPassword = BrevoPassword
        });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        SetRequestBody(controller, payload, BuildBasicAuth(BrevoUserName, "wrong-password"));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "BrevoWebhookAuthenticationInvalid");
    }

    [Fact]
    public void ReceiveAsyncClass_Should_Throw_WhenInboxWriterIsNull()
    {
        var act = () => new BrevoWebhooksController(
            null!,
            Microsoft.Extensions.Options.Options.Create(new BrevoEmailOptions()),
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("inboxWriter");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenOptionsIsNull()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();

        var act = () => new BrevoWebhooksController(
            new ProviderCallbackInboxWriter(db),
            null!,
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenValidationLocalizerIsNull()
    {
        await using var db = BrevoWebhookControllerTestDbContext.Create();

        var act = () => new BrevoWebhooksController(
            new ProviderCallbackInboxWriter(db),
            Microsoft.Extensions.Options.Options.Create(new BrevoEmailOptions()),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validationLocalizer");
    }

    private static BrevoWebhooksController CreateController(
        BrevoWebhookControllerTestDbContext db,
        BrevoEmailOptions options)
    {
        var controller = new BrevoWebhooksController(
            new ProviderCallbackInboxWriter(db),
            Options.Create(options),
            new TestValidationLocalizer());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static void SetRequestBody(BrevoWebhooksController controller, string payload, string? authorization = null)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        controller.ControllerContext!.HttpContext!.Request.Body = new MemoryStream(bytes);
        controller.ControllerContext.HttpContext.Request.ContentLength = bytes.Length;

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            controller.ControllerContext.HttpContext.Request.Headers["Authorization"] = authorization;
        }
    }

    private static void AssertProblem(IActionResult result, int expectedStatusCode, string expectedTitle)
    {
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatusCode);

        var problem = objectResult.Value.Should().BeOfType<ContractProblemDetails>().Subject;
        problem.Status.Should().Be(expectedStatusCode);
        problem.Title.Should().Be(expectedTitle);
    }

    private static (bool Received, bool Duplicate) AssertOk(IActionResult result)
    {
        var ok = result.Should().BeAssignableTo<ObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);

        var value = ok.Value;
        value.Should().NotBeNull();
        var received = GetBoolProperty(value, "received");
        var duplicate = GetBoolProperty(value, "duplicate");
        return (received, duplicate);
    }

    private static bool GetBoolProperty(object value, string name)
    {
        var property = value.GetType().GetProperty(
            name,
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        property.Should().NotBeNull();
        property.GetValue(value).Should().BeAssignableTo<bool>($"{name} should be a boolean.");
        return (bool)property.GetValue(value)!;
    }

    private static string BuildBasicAuth(string username, string password)
    {
        var credential = $"{username}:{password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credential));
        return $"Basic {encoded}";
    }

    private static string BuildMalformedAuth(string credential)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credential));
        return $"Basic {encoded}";
    }

    private static string CreatePayload()
    {
        return CreatePayload(eventName: "delivered", messageId: "msg-123");
    }

    private static string CreatePayload(string eventName, string messageId, string? timestamp = "1714689600", string? alias = "message-id")
    {
        return $$"""{"{{alias}}":"{{messageId}}","event":"{{eventName}}","ts_event":"{{timestamp}}"}""";
    }

    private static string CreatePayloadWithExactMaxLength()
    {
        var basePayload = """{"event":"delivered","messageId":"MSG-EXACT","ts_event":"1714689600","x":""}""";
        var paddingLength = ProviderWebhookPayloadReader.MaxPayloadBytes - basePayload.Length;
        return $$"""{"event":"delivered","messageId":"MSG-EXACT","ts_event":"1714689600","x":"{{new string('x', paddingLength)}}"}""";
    }

    private sealed class TestValidationLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }

    private sealed class BrevoWebhookControllerTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<SiteSetting> SiteSettings { get; set; } = null!;
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;

        private BrevoWebhookControllerTestDbContext(DbContextOptions<BrevoWebhookControllerTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BrevoWebhookControllerTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BrevoWebhookControllerTestDbContext>()
                .UseInMemoryDatabase($"darwin_brevo_webhooks_tests_{Guid.NewGuid()}")
                .Options;

            return new BrevoWebhookControllerTestDbContext(options);
        }
    }
}
