
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.Queries;
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

namespace Darwin.WebApi.Tests.Controllers;

public sealed class DhlWebhooksControllerTests
{
    private const string DhlApiKey = "dhl_api_key";
    private const string DhlApiSecret = "dhl_api_secret";

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSiteSettingIsMissing()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        var controller = CreateController(db);

        SetRequestBody(controller, CreateDhlPayload());
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenDhlIsDisabled()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting { DhlEnabled = false, DhlApiKey = DhlApiKey, DhlApiSecret = DhlApiSecret });

        var controller = CreateController(db);
        SetRequestBody(controller, CreateDhlPayload());
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenDhlApiSecretIsWhitespace()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = "   "
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit_DueToUtf8ByteLength()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = new string('é', (ProviderWebhookPayloadReader.MaxPayloadBytes / 2) + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit_AndApiKeyIsInvalid()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: "wrong-key", dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnPayloadTooLarge_WhenPayloadExceedsLimit_AndSignatureIsInvalid()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: "sha256=invalid");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status413PayloadTooLarge, "ProviderWebhookPayloadTooLarge");
    }

    [Fact]
    public async Task ReceiveAsync_Should_AcceptPayload_WhenPayloadIsExactlyAtSizeLimit()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayloadWithExactMaxLength();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        var ok = result.Should().BeAssignableTo<ObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadIsEmpty()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = string.Empty;
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenDhlSignatureOmitsPrefix()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var signature = BuildDhlSignature(payload);
        var invalidSignature = signature.Replace("sha256=", "");

        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: invalidSignature);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Reject_WhenSignatureHasInvalidHexCharacters()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var signature = $"sha256={new string('z', 64)}";

        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: signature);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Reject_WhenSignatureHasLeadingWhitespace()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var signature = $" {BuildDhlSignature(payload)}";

        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: signature);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Reject_WhenSignatureOmitsPrefixAndHasInvalidLength()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var signature = BuildDhlSignature(payload).Replace("sha256=", "");        
        var tooLong = signature + "aa";

        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: tooLong);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenContentLengthIsZeroButBodyHasContent()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        controller.ControllerContext!.HttpContext!.Request.ContentLength = 0;

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenContentLengthIsNull()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        controller.ControllerContext!.HttpContext!.Request.ContentLength = null;

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenApiKeyMismatch()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: "wrong-api-key", dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookApiKeyInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenApiKeyIsMissing()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: string.Empty, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookApiKeyInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenSignatureInvalid()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: "sha256=zz");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadInvalid()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = "{\"providerShipmentReference\":\"\",\"carrierEventKey\":\"\"}";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenOccurredAtUtcIsMissing()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = """{"providerShipmentReference":"REF-1","carrierEventKey":"delivered"}""";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenProviderShipmentReferenceMissing()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = """{"carrierEventKey":"delivered","occurredAtUtc":"2026-05-02T10:00:00Z"}""";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadIsMalformedJson()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = "{";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenPayloadDeserializesToNull()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = "[]";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenCarrierEventKeyTooLong()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = $$"""{"providerShipmentReference":"REF-1","carrierEventKey":"{{new string('b', 65)}}","occurredAtUtc":"2026-05-02T00:00:00Z"}""";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenCarrierEventKeyIsWhitespace()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = $$"""{"providerShipmentReference":"REF-1","carrierEventKey":"   ","occurredAtUtc":"2026-05-02T10:00:00Z"}""";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_MaxLengthCarrierEventKey()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = $$"""{"providerShipmentReference":"REF-1","carrierEventKey":"{{new string('d', 64)}}","occurredAtUtc":"2026-05-02T10:00:00Z"}""";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        var (received, duplicate) = AssertOk(result);
        received.Should().BeTrue();
        duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenDhlSignatureIsMissing()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: string.Empty);
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookSignatureInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenSignaturePrefixIsUppercase()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var signature = BuildDhlSignature(payload).Replace("sha256=", "SHA256=", StringComparison.OrdinalIgnoreCase);
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: signature);

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenDhlSignatureHasTrailingWhitespace()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var signature = $"{BuildDhlSignature(payload)}   ";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: signature);

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_Accept_WhenApiKeyHasWhitespacePadding()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: $" {DhlApiKey} ", dhlSignature: BuildDhlSignature(payload));

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenOccurredAtUtcInvalid()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = """{"providerShipmentReference":"REF-1","carrierEventKey":"delivered","occurredAtUtc":"bad-date"}""";
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ReceiveAsync_Should_SaveAndMarkDuplicateFalse_OnFirstValidCallback()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(payload));

        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(result).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenSameDhlCallbackArrivesTwice()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var signature = BuildDhlSignature(payload);

        var firstController = CreateController(db);
        SetRequestBody(firstController, payload, dhlKey: DhlApiKey, dhlSignature: signature);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db);
        SetRequestBody(secondController, payload, dhlKey: DhlApiKey, dhlSignature: signature);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateFalse_WhenCarrierEventKeyCaseChanges()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = CreateDhlPayload();
        var upperPayload = """{"providerShipmentReference":"REF-1","carrierEventKey":"DELIVERED","occurredAtUtc":"2026-05-02T10:00:00Z"}""";
        var signature = BuildDhlSignature(payload);
        var upperSignature = BuildDhlSignature(upperPayload);

        var firstController = CreateController(db);
        SetRequestBody(firstController, payload, dhlKey: DhlApiKey, dhlSignature: signature);
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db);
        SetRequestBody(secondController, upperPayload, dhlKey: DhlApiKey, dhlSignature: upperSignature);
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeFalse();
    }

    [Fact]
    public async Task ReceiveAsync_Should_MarkDuplicateTrue_WhenProviderShipmentReferenceWhitespaceIsNormalized()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = true,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var firstPayload = """{"providerShipmentReference":"  REF-1  ","carrierEventKey":"delivered","occurredAtUtc":"2026-05-02T10:00:00Z"}""";
        var secondPayload = """{"providerShipmentReference":"REF-1","carrierEventKey":"delivered","occurredAtUtc":"2026-05-02T10:00:00Z"}""";

        var firstController = CreateController(db);
        SetRequestBody(firstController, firstPayload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(firstPayload));
        var firstResult = await firstController.ReceiveAsync(TestContext.Current.CancellationToken);
        AssertOk(firstResult).Duplicate.Should().BeFalse();

        var secondController = CreateController(db);
        SetRequestBody(secondController, secondPayload, dhlKey: DhlApiKey, dhlSignature: BuildDhlSignature(secondPayload));
        var secondResult = await secondController.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertOk(secondResult).Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task ReceiveAsync_Should_ReturnBadRequest_WhenDhlNotEnabledEvenIfPayloadIsOversized()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();
        await SeedSiteSettingAsync(db, new SiteSetting
        {
            DhlEnabled = false,
            DhlApiKey = DhlApiKey,
            DhlApiSecret = DhlApiSecret
        });

        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var controller = CreateController(db);
        SetRequestBody(controller, payload, dhlKey: "unused", dhlSignature: "unused");
        var result = await controller.ReceiveAsync(TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status400BadRequest, "DhlWebhookAuthenticationNotConfigured");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenInboxWriterIsNull()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();

        var act = () => new DhlWebhooksController(
            null!,
            new GetSiteSettingHandler(db),
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("inboxWriter");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenGetSiteSettingHandlerIsNull()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();

        var act = () => new DhlWebhooksController(
            new ProviderCallbackInboxWriter(db),
            null!,
            new TestValidationLocalizer());

        act.Should().Throw<ArgumentNullException>().WithParameterName("getSiteSettingHandler");
    }

    [Fact]
    public async Task ReceiveAsyncClass_Should_Throw_WhenValidationLocalizerIsNull()
    {
        await using var db = DhlWebhookControllerTestDbContext.Create();

        var act = () => new DhlWebhooksController(
            new ProviderCallbackInboxWriter(db),
            new GetSiteSettingHandler(db),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("validationLocalizer");
    }

    private static DhlWebhooksController CreateController(DhlWebhookControllerTestDbContext db)
    {
        var controller = new DhlWebhooksController(
            new ProviderCallbackInboxWriter(db),
            new GetSiteSettingHandler(db),
            new TestValidationLocalizer());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static void SetRequestBody(
        DhlWebhooksController controller,
        string payload,
        string dhlKey,
        string dhlSignature)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        controller.ControllerContext!.HttpContext!.Request.Body = new MemoryStream(bytes);
        controller.ControllerContext.HttpContext.Request.ContentLength = bytes.Length;
        controller.ControllerContext.HttpContext.Request.Headers["X-DHL-Key"] = dhlKey;
        controller.ControllerContext.HttpContext.Request.Headers["X-DHL-Signature"] = dhlSignature;
    }

    private static void SetRequestBody(DhlWebhooksController controller, string payload)
        => SetRequestBody(controller, payload, DhlApiKey, BuildDhlSignature(payload));

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

    private static async Task SeedSiteSettingAsync(DhlWebhookControllerTestDbContext db, SiteSetting siteSetting)
    {
        db.Set<SiteSetting>().Add(siteSetting);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static string CreateDhlPayload()
    {
        return $$"""{"providerShipmentReference":"REF-1","carrierEventKey":"delivered","occurredAtUtc":"2026-05-02T10:00:00Z"}""";
    }

    private static string CreateDhlPayloadWithExactMaxLength()
    {
        var basePayload = $$"""{"providerShipmentReference":"REF-1","carrierEventKey":"delivered","occurredAtUtc":"2026-05-02T10:00:00Z","x":""}""";
        var paddingLength = ProviderWebhookPayloadReader.MaxPayloadBytes - basePayload.Length;
        return $$"""{"providerShipmentReference":"REF-1","carrierEventKey":"delivered","occurredAtUtc":"2026-05-02T10:00:00Z","x":"{{new string('x', paddingLength)}}"}""";
    }

    private static string BuildDhlSignature(string payload)
    {
        var key = Encoding.UTF8.GetBytes(DhlApiSecret);
        using var hmac = new HMACSHA256(key);
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return $"sha256={signature}";
    }

    private sealed class TestValidationLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }

    private sealed class DhlWebhookControllerTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<SiteSetting> SiteSettings { get; set; } = null!;
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;

        private DhlWebhookControllerTestDbContext(DbContextOptions<DhlWebhookControllerTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static DhlWebhookControllerTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<DhlWebhookControllerTestDbContext>()
                .UseInMemoryDatabase($"darwin_dhl_webhooks_tests_{Guid.NewGuid()}")
                .Options;

            return new DhlWebhookControllerTestDbContext(options);
        }
    }
}
