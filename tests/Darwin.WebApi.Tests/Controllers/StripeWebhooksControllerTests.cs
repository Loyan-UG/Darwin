
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.Settings.Queries;
using System.Text.Json;
using ContractProblemDetails = Darwin.Contracts.Common.ProblemDetails;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Entities.Settings;
using Darwin.WebApi.Controllers.Public;
using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.WebApi.Tests.Controllers;

public sealed class StripeWebhooksControllerTests
{
    private const string StripeWebhookSecret = "whsec_test_secret";

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSiteSettingIsMissing()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var controller = CreateController(db);

        SetRequestBody(controller, "{}");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSecretNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenStripeWebhookSecretNotConfigured()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting());

        var controller = CreateController(db);
        SetRequestBody(controller, "{\"id\":\"evt_1\",\"type\":\"payment_intent.succeeded\"}");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSecretNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenStripeWebhookSecretIsWhitespace()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = "   " });

        var controller = CreateController(db);
        SetRequestBody(controller, "{\"id\":\"evt_1\",\"type\":\"payment_intent.succeeded\"}");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSecretNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit_AndSignatureIsMissing()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit_AndSignatureIsInvalid()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload, signature: "t=123,v1=invalid");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenContentLengthIsZeroButBodyHasContent()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = """{"id":"evt_cl_0","type":"charge.succeeded"}""";
        var now = DateTimeOffset.UtcNow;
        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, now));
        controller.ControllerContext!.HttpContext!.Request.ContentLength = 0;

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadIsWhitespace()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var controller = CreateController(db);
        SetRequestBody(controller, "   ");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSignatureIsMissing()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var payload = "{\"id\":\"evt_1\",\"type\":\"payment_intent.succeeded\"}";

        var controller = CreateController(db);
        SetRequestBody(controller, payload);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSignatureHeaderRequired");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSignatureHeaderTooLong()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = "{\"id\":\"evt_1\",\"type\":\"payment_intent.succeeded\"}";
        var header = $"{new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes)}";

        var controller = CreateController(db);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSignatureHeaderHasTooManySignatures()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var timestamp = DateTimeOffset.UtcNow;
        var payload = "{\"id\":\"evt_1\",\"type\":\"payment_intent.succeeded\"}";
        var tooManySignatures = $"t={timestamp.ToUnixTimeSeconds()},v1={new string('0', 64)},v1={new string('1', 64)},v1={new string('2', 64)},v1={new string('3', 64)},v1={new string('4', 64)},v1={new string('5', 64)}";

        var controller = CreateController(db);
        SetRequestBody(controller, payload, signature: tooManySignatures);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSignatureHeaderHasNoV1Entries()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = "{\"id\":\"evt_no_v1\",\"type\":\"payment_intent.succeeded\"}";
        var timestamp = DateTimeOffset.UtcNow;
        var header = $"t={timestamp.ToUnixTimeSeconds()},v0={ComputeSha256(payload)}";

        var controller = CreateController(db);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadLengthIsNotByteLengthLimitCompliant()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var asciiPayload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes);
        var emojiPayload = "\U0001F600";
        var payload = asciiPayload + emojiPayload;
        var now = DateTimeOffset.UtcNow;

        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, now));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenSignedEventTypeHasDifferentCase()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var payload = "{\"id\":\"evt_1\",\"type\":\"Payment_Succeeded\"}";
        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, now));

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenSignatureHeaderKeysAreUppercase()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var payload = "{\"id\":\"evt_upper\",\"type\":\"payment_intent.succeeded\"}";
        var signature = BuildStripeSignature(payload, StripeWebhookSecret, now)
            .Replace("t=", "T=", StringComparison.OrdinalIgnoreCase)
            .Replace("v1=", "V1=", StringComparison.OrdinalIgnoreCase);

        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: signature);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenSignatureHeaderHasWhitespaceAroundParts()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var payload = "{\"id\":\"evt_ws\",\"type\":\"charge.succeeded\"}";
        var signature = BuildStripeSignature(payload, StripeWebhookSecret, now);
        signature = $" {signature.Replace(",", ", ", StringComparison.Ordinal)} ";

        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: signature);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenSignatureHeaderHasUnknownParameters()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var payload = "{\"id\":\"evt_unknown\",\"type\":\"charge.updated\"}";
        var signature = BuildStripeSignature(payload, StripeWebhookSecret, now);
        var header = $"x-key=ignored,v0=zz,{signature},foo=bar";
        var headerWithValid = $"t={now.ToUnixTimeSeconds()},v1={signature.Substring(0, 2)}{new string('0', 62)},v1={signature},x={signature}";
        header = $"{header},{headerWithValid}";

        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenTimestampIsAtBoundary()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var payload = "{\"id\":\"evt_boundary\",\"type\":\"charge.updated\"}";
        var timestamp = now.AddMinutes(-10);
        var signature = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: signature);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenPayloadPropertiesHaveWhitespaceAroundValues()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var payload = """{"id":"   evt_ws_trim   ","type":"  charge.succeeded  "}""";
        var signature = BuildStripeSignature(payload, StripeWebhookSecret, now);

        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: signature);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenContentLengthIsNull()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = "{\"id\":\"evt_cl_null\",\"type\":\"charge.succeeded\"}";
        var now = DateTimeOffset.UtcNow;
        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, now));
        controller.ControllerContext!.HttpContext!.Request.ContentLength = null;

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenEventIdIsExactlyAtMaxLength()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var eventId = new string('i', 100);
        var payload = $$"""{"id":"{{eventId}}","type":"charge.failed"}""";
        var controller = CreateController(db, now.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, now));

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        var (received, duplicate) = AssertOk(result);

        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenEventIdWhitespaceIsNormalized()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var now = DateTimeOffset.UtcNow;
        var firstPayload = """{"id":"  evt_ws_norm  ","type":"charge.succeeded"}""";
        var secondPayload = """{"id":"evt_ws_norm","type":"charge.succeeded"}""";

        var firstController = CreateController(db, now.UtcDateTime);
        SetRequestBody(firstController, firstPayload, signature: BuildStripeSignature(firstPayload, StripeWebhookSecret, now));
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, now.UtcDateTime);
        SetRequestBody(secondController, secondPayload, signature: BuildStripeSignature(secondPayload, StripeWebhookSecret, now));
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEventTypeIsNotString()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var timestamp = DateTimeOffset.UtcNow;
        var payload = """{"id":"evt_non_string_type","type":123}""";

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, timestamp));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSignatureTimestampExpired()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-11);
        var payload = "{\"id\":\"evt_1\",\"type\":\"payment_intent.succeeded\"}";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEnvelopeIsInvalid()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var payload = "{\"type\":\"payment_intent.succeeded\"}";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenInboxWriterIsNull()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();

        var act = () => new StripeWebhooksController(
            null!,
            new GetSiteSettingHandler(db),
            new StripeWebhookSignatureVerifier(CreateClock()),
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("inboxWriter");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenGetSiteSettingHandlerIsNull()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();

        var act = () => new StripeWebhooksController(
            new ProviderCallbackInboxWriter(db),
            null!,
            new StripeWebhookSignatureVerifier(CreateClock()),
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("getSiteSettingHandler");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenSignatureVerifierIsNull()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();

        var act = () => new StripeWebhooksController(
            new ProviderCallbackInboxWriter(db),
            new GetSiteSettingHandler(db),
            null!,
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("signatureVerifier");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenValidationLocalizerIsNull()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();

        var act = () => new StripeWebhooksController(
            new ProviderCallbackInboxWriter(db),
            new GetSiteSettingHandler(db),
            new StripeWebhookSignatureVerifier(CreateClock()),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validationLocalizer");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEnvelopeIsMissingEventId()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var payload = "{\"type\":\"payment_intent.succeeded\"}";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEnvelopeIsWhitespaceEventId()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var payload = "{\"id\":\"   \",\"type\":\"payment_intent.succeeded\"}";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadIsMalformedJson()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var payload = "{";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadIsArray()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = "[]";
        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, timestamp));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEventTypeTooLong()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var eventType = new string('a', 65);
        var payload = $$"""{"id":"evt_1","type":"{{eventType}}"}""";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, DateTimeOffset.UtcNow);
        header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_EventTypeAtMaxLength()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var eventType = new string('a', 64);
        var payload = $$"""{"id":"evt_edge","type":"{{eventType}}"}""";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenEventIdTooLong()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        var timestamp = DateTimeOffset.UtcNow;
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });
        var eventId = new string('e', 101);
        var payload = $$"""{"id":"{{eventId}}","type":"payment_intent.succeeded"}""";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "StripeWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_SaveAndMarkDuplicateFalse_OnFirstValidCallback()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var timestamp = DateTimeOffset.UtcNow;
        var payload = "{\"id\":\"evt_100\",\"type\":\"payment_intent.succeeded\"}";
        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, timestamp));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenSameStripeEventArrivesTwice()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = "{\"id\":\"evt_200\",\"type\":\"charge.refunded\"}";
        var timestamp = DateTimeOffset.UtcNow;
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var firstResult = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(secondController, payload, signature: BuildStripeSignature(payload, StripeWebhookSecret, timestamp));
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenEventTypeCaseChangesButEventIdIsSame()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var payload = "{\"id\":\"evt_case_type\",\"type\":\"payment_intent.succeeded\"}";
        var timestamp = DateTimeOffset.UtcNow;
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);

        var controller = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(controller, payload, signature: header);
        var firstResult = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondPayload = "{\"id\":\"evt_case_type\",\"type\":\"Payment_IntenT.SUCCEEDED\"}";
        var secondController = CreateController(db, timestamp.UtcDateTime);
        SetRequestBody(secondController, secondPayload, signature: BuildStripeSignature(secondPayload, StripeWebhookSecret, timestamp));
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenSignatureTimestampHasLeadingPlus()
    {
        await using var db = StripeWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { StripeWebhookSecret = StripeWebhookSecret });

        var timestamp = DateTimeOffset.UtcNow;
        var payload = """{"id":"evt_plus_expired","type":"charge.succeeded"}""";
        var header = BuildStripeSignature(payload, StripeWebhookSecret, timestamp);
        header = $"t=+{timestamp.ToUnixTimeSeconds()},{header[(header.IndexOf(',') + 1)..]}";

        var controller = CreateController(db);
        SetRequestBody(controller, payload, signature: header);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    private static StripeWebhooksController CreateController(
        StripeWebhookControllerTestDbContext db,
        DateTime? now = null)
    {
        var controller = new StripeWebhooksController(
            new ProviderCallbackInboxWriter(db),
            new GetSiteSettingHandler(db),
            new StripeWebhookSignatureVerifier(CreateClock(now ?? DateTime.UtcNow)),
            new TestValidationLocalizer());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static void SetRequestBody(StripeWebhooksController controller, string payload, string? signature = null)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        controller.ControllerContext!.HttpContext!.Request.Body = new MemoryStream(bytes);
        controller.ControllerContext!.HttpContext.Request.ContentLength = bytes.Length;

        if (!string.IsNullOrWhiteSpace(signature))
        {
            controller.ControllerContext.HttpContext.Request.Headers["Stripe-Signature"] = signature;
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
        if (value is IDictionary<string, object?> dictionary)
        {
            if (dictionary.TryGetValue(name, out var dictValue))
            {
                dictValue.Should().BeAssignableTo<bool>($"{name} should be a boolean.");
                return (bool)dictValue!;
            }

            var fallback = dictionary.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase));
            fallback.Value.Should().NotBeNull($"{name} should exist on response payload.");
            fallback.Value.Should().BeAssignableTo<bool>($"{name} should be a boolean.");
            return (bool)fallback.Value!;
        }

        var property = value.GetType().GetProperty(
            name,
            System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (property is not null)
        {
            property.GetValue(value).Should().BeAssignableTo<bool>($"{name} should be a boolean.");
            return (bool)property.GetValue(value)!;
        }

        var json = JsonSerializer.Serialize(value);
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty(name, out var element);
        (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            .Should().BeTrue($"{name} should be a boolean.");

        return element.GetBoolean();
    }

    private static async Task SeedSiteSettingAsync(StripeWebhookControllerTestDbContext db, SiteSetting siteSetting)
    {
        db.Set<SiteSetting>().Add(siteSetting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static string BuildStripeSignature(string payload, string secret, DateTimeOffset timestamp)
    {
        var signedPayload = $"{timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={timestamp.ToUnixTimeSeconds()},v1={signature}";
    }

    private static string ComputeSha256(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(StripeWebhookSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return signature;
    }

    private static IClock CreateClock(DateTime? now = null)
    {
        var clock = new Mock<IClock>();
        clock.Setup(x => x.UtcNow).Returns(now ?? DateTime.UtcNow);
        return clock.Object;
    }

    private sealed class TestValidationLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class StripeWebhookControllerTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<SiteSetting> SiteSettings { get; set; } = null!;
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;

        private StripeWebhookControllerTestDbContext(DbContextOptions<StripeWebhookControllerTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static StripeWebhookControllerTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<StripeWebhookControllerTestDbContext>()
                .UseInMemoryDatabase($"darwin_stripe_webhooks_tests_{Guid.NewGuid()}")
                .Options;

            return new StripeWebhookControllerTestDbContext(options);
        }
    }
}
