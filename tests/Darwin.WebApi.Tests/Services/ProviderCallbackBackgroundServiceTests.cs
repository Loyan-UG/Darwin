using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application;
using Darwin.Application.Notifications;
using Darwin.Application.Orders.Commands;
using Darwin.Application.Orders.DTOs;
using Darwin.Application.Orders.Validators;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Integration;
using Darwin.Domain.Enums;
using Darwin.Worker;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moq;
using System.Threading;

namespace Darwin.WebApi.Tests.Services;

public sealed class ProviderCallbackBackgroundServiceTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDependenciesAreMissing()
    {
        var options = Options.Create(new ProviderCallbackWorkerOptions());
        var clock = new FixedClock(DateTime.UtcNow);

        Action noScopeFactory = () =>
            new ProviderCallbackBackgroundService(
                null!,
                options,
                clock,
                new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);
        Action noOptions = () =>
            new ProviderCallbackBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                null!,
                clock,
                new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);
        Action noClock = () =>
            new ProviderCallbackBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                options,
                null!,
                new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);
        Action noLogger = () =>
            new ProviderCallbackBackgroundService(
                new Mock<IServiceScopeFactory>().Object,
                options,
                clock,
                null!);

        noScopeFactory.Should().Throw<ArgumentNullException>().WithParameterName("scopeFactory");
        noOptions.Should().Throw<ArgumentNullException>().WithParameterName("options");
        noClock.Should().Throw<ArgumentNullException>().WithParameterName("clock");
        noLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotCreateScope_WhenWorkerIsDisabled()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        var scope = new Mock<IServiceScope>(MockBehavior.Strict);
        scope.Setup(x => x.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);

        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions { Enabled = false }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().ToList();
        scopeFactory.Verify(sf => sf.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotCreateScope_WhenCancellationIsRequestedBeforeStart()
    {
        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions()),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await service.StartAsync(cancellationTokenSource.Token);
        await service.StopAsync(cancellationTokenSource.Token);

        scopeFactory.Verify(sf => sf.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkUnsupportedProviderCallbackAsFailed()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_1",
            PayloadJson = "{}",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkInvalidDhlPayloadAsFailed()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "DHL",
            CallbackType = "shipment_event",
            IdempotencyKey = "evt_dhl_invalid",
            PayloadJson = "null",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().Contain("DHL callback payload was invalid");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessDhlCallback_AsDelivered()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        var shipment = new Darwin.Domain.Entities.Orders.Shipment
        {
            Carrier = "DHL",
            Service = "Standard",
            ProviderShipmentReference = "dhl-ref-1",
            OrderId = Guid.NewGuid(),
            Status = ShipmentStatus.Pending
        };
        db.Set<Darwin.Domain.Entities.Orders.Shipment>().Add(shipment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "dhl",
            CallbackType = "shipment_event",
            IdempotencyKey = "evt_dhl_success",
            PayloadJson = """
            {
              "providerShipmentReference": "dhl-ref-1",
              "carrierEventKey": "delivered",
              "occurredAtUtc": "2026-05-02T10:00:00Z",
              "providerStatus": "DELIVERED",
              "trackingNumber": "TRK-123",
              "labelUrl": "https://example.test/label.png"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var validator = new ApplyShipmentCarrierEventValidator(localizer.Object);
        var dhlHandler = new ApplyShipmentCarrierEventHandler(db, validator, localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyShipmentCarrierEventHandler))).Returns(dhlHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_dhl_success").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();

        var updatedShipment = await db.Set<Darwin.Domain.Entities.Orders.Shipment>()
            .SingleAsync(x => x.Id == shipment.Id, TestContext.Current.CancellationToken);
        updatedShipment.Status.Should().Be(ShipmentStatus.Delivered);
        updatedShipment.LastCarrierEventKey.Should().Be("delivered");
        updatedShipment.TrackingNumber.Should().Be("TRK-123");
        updatedShipment.LabelUrl.Should().Be("https://example.test/label.png");
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessMessage_WithinRetryCooldownWindow()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_cooldown",
            PayloadJson = "{}",
            Status = "Pending",
            LastAttemptAtUtc = now,
            AttemptCount = 1
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                RetryCooldownSeconds = 120,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Pending");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessFailedMessage_WhenLastAttemptAtIsNull_DespiteRetryCooldown()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_failed_no_attempt_time",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                RetryCooldownSeconds = 120,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(2);
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        item.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessFailedMessage_WithinRetryCooldownWindow()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_failed_cooldown",
            PayloadJson = "{}",
            Status = "Failed",
            LastAttemptAtUtc = now,
            AttemptCount = 1
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                RetryCooldownSeconds = 120,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessFailedMessage_WhenRetryCooldownElapsed()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_failed_cooldown_elapsed",
            PayloadJson = "{}",
            Status = "Failed",
            LastAttemptAtUtc = now.AddMinutes(-5),
            AttemptCount = 1
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                RetryCooldownSeconds = 120,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(2);
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        item.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessMessage_WhenStatusIsAlreadyProcessed()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_already_processed",
            PayloadJson = "{}",
            Status = "Processed",
            AttemptCount = 1,
            LastAttemptAtUtc = now.AddHours(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 30,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().BeNullOrEmpty();
        item.LastAttemptAtUtc.Should().Be(now.AddHours(-1));
    }

    [Fact]
    public async Task ExecuteAsync_Should_StopProcessingFailedMessage_AfterItReachesMaxAttempts()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_failed_reaching_max",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1,
            LastAttemptAtUtc = null,
            CreatedAtUtc = now.AddMinutes(-10)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                RetryCooldownSeconds = 0,
                BatchSize = 5,
                MaxAttempts = 2
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_failed_reaching_max", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(2);
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        item.LastAttemptAtUtc.Should().Be(now);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var itemAfterSecondCycle = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == item.Id, TestContext.Current.CancellationToken);

        itemAfterSecondCycle.AttemptCount.Should().Be(2);
        itemAfterSecondCycle.Status.Should().Be("Failed");
        itemAfterSecondCycle.LastAttemptAtUtc.Should().Be(now);
        itemAfterSecondCycle.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessMessage_WhenMaxAttemptsIsZeroAfterNormalization()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_max_attempts",
            PayloadJson = "{}",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 0,
                MaxAttempts = 0
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_RespectBatchSizeInSingleIteration()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_batch_1",
                PayloadJson = "{}",
                Status = "Pending"
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_batch_2",
                PayloadJson = "{}",
                Status = "Pending"
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_batch_3",
                PayloadJson = "{}",
                Status = "Pending"
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 3
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var processed = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Status == "Failed" && x.FailureReason!.Contains("Unsupported provider callback provider 'Unknown'"), TestContext.Current.CancellationToken);
        var pending = await db.Set<ProviderCallbackInboxMessage>()
            .CountAsync(x => x.Status == "Pending", TestContext.Current.CancellationToken);

        processed.Should().Be(1);
        pending.Should().Be(2);
        var first = await db.Set<ProviderCallbackInboxMessage>()
            .Where(x => x.IdempotencyKey == "evt_batch_1")
            .SingleAsync(TestContext.Current.CancellationToken);
        var second = await db.Set<ProviderCallbackInboxMessage>()
            .Where(x => x.IdempotencyKey == "evt_batch_2")
            .SingleAsync(TestContext.Current.CancellationToken);
        var third = await db.Set<ProviderCallbackInboxMessage>()
            .Where(x => x.IdempotencyKey == "evt_batch_3")
            .SingleAsync(TestContext.Current.CancellationToken);

        var statuses = new[] { first.Status, second.Status, third.Status };
        statuses.Count(x => x == "Failed").Should().Be(1);
        statuses.Count(x => x == "Pending").Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessMessagesInOrderByLastAttemptThenCreatedAt()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_order_a",
                PayloadJson = "{}",
                Status = "Pending",
                CreatedAtUtc = fixedTime.AddMinutes(-40),
                LastAttemptAtUtc = fixedTime.AddMinutes(-30)
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_order_b",
                PayloadJson = "{}",
                Status = "Pending",
                CreatedAtUtc = fixedTime.AddMinutes(-20),
                LastAttemptAtUtc = null
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_order_c",
                PayloadJson = "{}",
                Status = "Pending",
                CreatedAtUtc = fixedTime.AddMinutes(-10),
                LastAttemptAtUtc = fixedTime.AddMinutes(-5)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var first = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_order_a", TestContext.Current.CancellationToken);
        var second = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_order_b", TestContext.Current.CancellationToken);
        var third = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_order_c", TestContext.Current.CancellationToken);

        first.Status.Should().Be("Failed");
        second.Status.Should().Be("Failed");
        third.Status.Should().Be("Pending");
        first.AttemptCount.Should().Be(1);
        second.AttemptCount.Should().Be(1);
        third.AttemptCount.Should().Be(0);
        third.FailureReason.Should().BeNullOrEmpty();
        first.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
        second.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseCreatedAtWhenLastAttemptAtUtcIsNull_ForOrdering()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_order_null_created_first",
                PayloadJson = "{}",
                Status = "Pending",
                LastAttemptAtUtc = null,
                CreatedAtUtc = fixedTime.AddMinutes(-20)
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_order_null_created_second",
                PayloadJson = "{}",
                Status = "Pending",
                LastAttemptAtUtc = null,
                CreatedAtUtc = fixedTime.AddMinutes(-5)
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_order_null_created_blocked",
                PayloadJson = "{}",
                Status = "Pending",
                LastAttemptAtUtc = fixedTime.AddMinutes(-1),
                CreatedAtUtc = fixedTime.AddMinutes(-60)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var first = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_order_null_created_first", TestContext.Current.CancellationToken);
        var second = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_order_null_created_second", TestContext.Current.CancellationToken);
        var third = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_order_null_created_blocked", TestContext.Current.CancellationToken);

        first.Status.Should().Be("Failed");
        second.Status.Should().Be("Failed");
        third.Status.Should().Be("Pending");
        first.AttemptCount.Should().Be(1);
        second.AttemptCount.Should().Be(1);
        third.AttemptCount.Should().Be(0);
        third.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotTouchDeletedMessage_IfMixedWithActiveMessages()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_mixed_deleted",
                PayloadJson = "{}",
                Status = "Failed",
                AttemptCount = 1,
                LastAttemptAtUtc = fixedTime.AddMinutes(-10),
                CreatedAtUtc = fixedTime.AddMinutes(-10),
                IsDeleted = true
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_mixed_active",
                PayloadJson = "{}",
                Status = "Failed",
                AttemptCount = 1,
                LastAttemptAtUtc = fixedTime.AddMinutes(-10),
                CreatedAtUtc = fixedTime.AddMinutes(-10)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 10,
                RetryCooldownSeconds = 5,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var deleted = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_mixed_deleted", TestContext.Current.CancellationToken);
        var active = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_mixed_active", TestContext.Current.CancellationToken);

        deleted.AttemptCount.Should().Be(1);
        deleted.Status.Should().Be("Failed");
        deleted.LastAttemptAtUtc.Should().Be(fixedTime.AddMinutes(-10));
        deleted.FailureReason.Should().BeNullOrEmpty();
        deleted.ProcessedAtUtc.Should().BeNull();
        active.AttemptCount.Should().Be(2);
        active.Status.Should().Be("Failed");
        active.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
        active.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_RespectNormalizedRetryCooldown_ForPendingMessages()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_retry_normalized_pending",
            PayloadJson = "{}",
            Status = "Pending",
            AttemptCount = 0,
            LastAttemptAtUtc = fixedTime.AddSeconds(-1),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 0,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_retry_normalized_pending").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(0);
        item.Status.Should().Be("Pending");
        item.FailureReason.Should().BeNullOrEmpty();
        item.LastAttemptAtUtc.Should().Be(fixedTime.AddSeconds(-1));
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessValidStripeCallbackAsProcessed()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.succeeded",
            IdempotencyKey = "evt_stripe_success",
            PayloadJson = """
            {
              "id": "evt_stripe_success",
              "type": "charge.succeeded",
              "data": {
                "object": {}
              }
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stripeHandler = new Darwin.Application.Billing.ProcessStripeWebhookHandler(db);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(Darwin.Application.Billing.ProcessStripeWebhookHandler))).Returns(stripeHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkStripeCallbackAsFailed_WhenHandlerReturnsFailureResult()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Stripe",
            CallbackType = "payment_intent.succeeded",
            IdempotencyKey = "evt_stripe_invalid",
            PayloadJson = """
            {
              "id": "",
              "type": "payment_intent.succeeded",
              "data": {
                "object": {}
              }
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stripeHandler = new Darwin.Application.Billing.ProcessStripeWebhookHandler(db);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(Darwin.Application.Billing.ProcessStripeWebhookHandler))).Returns(stripeHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single().Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessSuccessfulBrevoCallbackAsProcessed()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-success",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Pending"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_success",
            PayloadJson = """
            {
              "event": "delivered",
              "message-id": "msg-success",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_success").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-success", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenBrevoHandlerReturnsFailureResult()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-fail",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Pending"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_fail",
            PayloadJson = """
            {
              "event": "hard_bounce",
              "message-id": "msg-fail",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_fail").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-fail", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Contain("Brevo event 'hard_bounce'");
        audit.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoClickEvent_AsProcessed_AndMarkEmailAuditSent()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-click",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Pending"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_click",
            PayloadJson = """
            {
              "event": "click",
              "message-id": "msg-click",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_click").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-click", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenSoftBounceEventArrives()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-soft",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Sent"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_soft",
            PayloadJson = """
            {
              "event": "soft_bounce",
              "message-id": "msg-soft",
              "email": "user@example.com",
              "subject": "Welcome",
              "reason": "Mailbox full"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_soft").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-soft", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Contain("Brevo event 'soft_bounce'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoEvent_WhenMatchingByCorrelationKey()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            CorrelationKey = "corr-123",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Pending"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_correlation",
            PayloadJson = """
            {
              "event": "request",
              "message-id": "   ",
              "x-correlation-key": "corr-123"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_correlation").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.CorrelationKey == "corr-123", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoEvent_WithUnknownEvent_WithoutChangingAuditStatus()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-unknown",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Sent"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_unknown",
            PayloadJson = """
            {
              "event": "some_future_event",
              "message-id": "msg-unknown",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_unknown").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-unknown", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Sent");
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenBlockedEventArrives()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-blocked",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            Status = "Sent"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_blocked",
            PayloadJson = """
            {
              "event": "blocked",
              "message-id": "msg-blocked",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_blocked").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-blocked", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Contain("Brevo event 'blocked'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenSpamEventArrives_WithReason()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-spam",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-3),
            Status = "Sent"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_spam",
            PayloadJson = """
            {
              "event": "spam",
              "message-id": "msg-spam",
              "email": "user@example.com",
              "subject": "Welcome",
              "reason": "Likely spam"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_spam").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-spam", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Contain("Brevo event 'spam'");
        audit.FailureMessage.Should().Contain("Likely spam");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoEvent_WhenMatchedByEmailAndSubjectFallback()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Pending"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_fallback",
            PayloadJson = """
            {
              "event": "deferred",
              "message-id": "",
              "email": "user@example.com",
              "subject": "Welcome",
              "reason": "not used"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_fallback").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.RecipientEmail == "user@example.com" && x.Subject == "Welcome", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenInvalidEventArrives()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-invalid",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Sent"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_invalid",
            PayloadJson = """
            {
              "event": "invalid",
              "message-id": "msg-invalid",
              "email": "user@example.com",
              "subject": "Welcome",
              "reason": "invalid recipient"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_invalid").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-invalid", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Contain("Brevo event 'invalid'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenErrorEventArrives()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-error",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-4),
            Status = "Sent"
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_error",
            PayloadJson = """
            {
              "event": "error",
              "message-id": "msg-error",
              "email": "user@example.com",
              "subject": "Welcome",
              "reason": "provider error"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_error").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-error", TestContext.Current.CancellationToken);

        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Contain("Brevo event 'error'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoCallback_AsProcessed_WhenNoMatchingAuditExists()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_no_audit",
            PayloadJson = """
            {
              "event": "delivered",
              "message-id": "msg-missing",
              "email": "missing@example.com",
              "subject": "Missing"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_no_audit").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        var auditCount = await db.Set<EmailDispatchAudit>().CountAsync(TestContext.Current.CancellationToken);
        auditCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenBrevoPayloadIsInvalid()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_invalid_payload",
            PayloadJson = "not-valid-json",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_invalid_payload").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.FailureReason.Should().Contain("BrevoWebhookPayloadInvalid");
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessPendingMessage_WhenAttemptCountReachedMaxAttempts()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_max_attempts_blocked",
            PayloadJson = "{}",
            Status = "Pending",
            AttemptCount = 3,
            LastAttemptAtUtc = fixedTime.AddMinutes(-30),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 0,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_max_attempts_blocked").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(3);
        item.Status.Should().Be("Pending");
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessFailedMessage_WhenAttemptCountReachedMaxAttempts()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_max_failed_blocked",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 2,
            LastAttemptAtUtc = fixedTime.AddMinutes(-30),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 0,
                MaxAttempts = 2
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_max_failed_blocked").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(2);
        item.Status.Should().Be("Failed");
        item.LastAttemptAtUtc.Should().Be(fixedTime.AddMinutes(-30));
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessMessage_WhenCallbackIsDeleted()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_deleted",
            PayloadJson = "{}",
            Status = "Pending",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 2
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_deleted").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(0);
        item.Status.Should().Be("Pending");
        item.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoCallback_WhenMatchingAuditIsDeleted_WithoutChangingAudit()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            ProviderMessageId = "msg-deleted-audit",
            AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            Status = "Sent",
            IsDeleted = true
        });
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_deleted_audit",
            PayloadJson = """
            {
              "event": "delivered",
              "message-id": "msg-deleted-audit",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(DateTime.UtcNow),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_deleted_audit").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "msg-deleted-audit", TestContext.Current.CancellationToken);

        audit.IsDeleted.Should().BeTrue();
        audit.Status.Should().Be("Sent");
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessFailedMessage_WithinRetryCooldown()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_retry_wait",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1,
            LastAttemptAtUtc = fixedTime.AddSeconds(-10),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 30,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_retry_wait").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(1);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessFailedMessage_AfterRetryCooldown()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_retry_ready",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1,
            LastAttemptAtUtc = fixedTime.AddMinutes(-10),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 5,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_retry_ready").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(2);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
        item.LastAttemptAtUtc.Should().Be(fixedTime);
        item.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessFailedMessage_WhenLastAttemptAtCutoffUtc()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_retry_cutoff",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1,
            LastAttemptAtUtc = fixedTime.AddSeconds(-5),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 5,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_retry_cutoff").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(2);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
        item.LastAttemptAtUtc.Should().Be(fixedTime);
        item.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessFailedMessage_WhenLastAttemptAtIsNull()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_failed_no_last_attempt",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1,
            LastAttemptAtUtc = null,
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 30,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_failed_no_last_attempt").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(2);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
        item.LastAttemptAtUtc.Should().Be(fixedTime);
        item.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessFailedMessage_WhenRetryCooldownIsBelowMinimum()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_retry_too_low",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1,
            LastAttemptAtUtc = fixedTime.AddSeconds(-3),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 1,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_retry_too_low").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(1);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().BeNullOrEmpty();
        item.LastAttemptAtUtc.Should().Be(fixedTime.AddSeconds(-3));
        item.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_RespectRetryCooldownNormalization_ForFailedMessages()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_retry_failed_normalized",
            PayloadJson = "{}",
            Status = "Failed",
            AttemptCount = 1,
            LastAttemptAtUtc = fixedTime.AddSeconds(-1),
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 0,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_retry_failed_normalized").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(1);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().BeNullOrEmpty();
        item.LastAttemptAtUtc.Should().Be(fixedTime.AddSeconds(-1));
        item.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ClampBatchSize_WhenBelowMinimum()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().AddRange(
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_batch_size_zero_first",
                PayloadJson = "{}",
                Status = "Pending",
                LastAttemptAtUtc = fixedTime.AddMinutes(-30),
                CreatedAtUtc = fixedTime.AddMinutes(-30)
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_batch_size_zero_second",
                PayloadJson = "{}",
                Status = "Pending",
                LastAttemptAtUtc = fixedTime.AddMinutes(-30),
                CreatedAtUtc = fixedTime.AddMinutes(-30)
            },
            new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_batch_size_zero_third",
                PayloadJson = "{}",
                Status = "Pending",
                LastAttemptAtUtc = fixedTime.AddMinutes(-30),
                CreatedAtUtc = fixedTime.AddMinutes(-30)
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 0,
                RetryCooldownSeconds = 5,
                MaxAttempts = 3
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var first = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_batch_size_zero_first").Id, TestContext.Current.CancellationToken);
        var second = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_batch_size_zero_second").Id, TestContext.Current.CancellationToken);
        var third = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_batch_size_zero_third").Id, TestContext.Current.CancellationToken);

        var failedCount = new[] { first.Status, second.Status, third.Status }.Count(s => s == "Failed");
        var pendingCount = new[] { first.Status, second.Status, third.Status }.Count(s => s == "Pending");

        failedCount.Should().Be(1);
        pendingCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ClampMaxAttemptsToUpperBound()
    {
        var fixedTime = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_max_attempts_upper",
            PayloadJson = "{}",
            Status = "Pending",
            AttemptCount = 24,
            CreatedAtUtc = fixedTime.AddMinutes(-30)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 5,
                MaxAttempts = 99
            }),
            new FixedClock(fixedTime),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_max_attempts_upper").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(25);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ClampPollIntervalToMinimum()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Unknown",
            CallbackType = "webhook_event",
            IdempotencyKey = "evt_poll_interval_min",
            PayloadJson = "{}",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 0,
                BatchSize = 5,
                RetryCooldownSeconds = 0,
                MaxAttempts = 3
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        scopeFactory.Verify(x => x.CreateScope(), Times.Exactly(1));

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_poll_interval_min").Id, TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(1);
        item.Status.Should().Be("Failed");
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ClampBatchSizeToMaximum()
    {
        var databaseName = $"darwin_provider_callback_worker_tests_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        await using var assertDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        for (var i = 1; i <= 101; i++)
        {
            db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = $"evt_batch_upper_{i:000}",
                PayloadJson = "{}",
                Status = "Pending",
                CreatedAtUtc = now.AddSeconds(-i)
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1000,
                MaxAttempts = 1,
                RetryCooldownSeconds = 5
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        for (var attempt = 0; attempt < 120; attempt++)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            var failedCount = await assertDb.Set<ProviderCallbackInboxMessage>().CountAsync(x => x.Status == "Failed", TestContext.Current.CancellationToken);
            if (failedCount >= 100)
            {
                break;
            }
        }
        await service.StopAsync(TestContext.Current.CancellationToken);

        var failed = await assertDb.Set<ProviderCallbackInboxMessage>().CountAsync(x => x.Status == "Failed", TestContext.Current.CancellationToken);
        var pending = await assertDb.Set<ProviderCallbackInboxMessage>().CountAsync(x => x.Status == "Pending", TestContext.Current.CancellationToken);

        failed.Should().BeInRange(100, 101);
        pending.Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessStripeCallback_WhenProviderCaseIsDifferent()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "sTRiPe",
            CallbackType = "payment_intent.succeeded",
            IdempotencyKey = "evt_stripe_case",
            PayloadJson = """
            {
              "id": "evt_stripe_case",
              "type": "charge.succeeded",
              "data": {
                "object": {}
              }
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var stripeHandler = new Darwin.Application.Billing.ProcessStripeWebhookHandler(db);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(Darwin.Application.Billing.ProcessStripeWebhookHandler))).Returns(stripeHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_stripe_case").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoCallback_WhenProviderCaseIsDifferent()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_1",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "bReVo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_case",
            PayloadJson = """
            {
              "event": "delivered",
              "message-id": "msg_brevo_1",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_case").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_1", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkBrevoCallbackAsFailed_WhenPayloadIsInvalid()
    {
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_invalid",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = DateTime.UtcNow,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "Brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_invalid",
            PayloadJson = "{}",
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(DateTime.UtcNow), localizer.Object));

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(DateTime.UtcNow),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_invalid").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().Contain("BrevoWebhookPayloadInvalid");
        item.ProcessedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoFailureEvent_AsAuditFailed()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_fail_1",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_failure",
            PayloadJson = """
            {
              "event": "hard_bounce",
              "message-id": "msg_brevo_fail_1",
              "reason": "Mailbox full"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_failure").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_fail_1", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Contain("Brevo event 'hard_bounce': Mailbox full");
        audit.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseBrevoCorrelationFallback_WhenMessageIdIsMissing()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            CorrelationKey = "corr-123",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "BREVO",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_correlation",
            PayloadJson = """
            {
              "event": "opened",
              "X-Correlation-Key": "corr-123"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_correlation").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.CorrelationKey == "corr-123", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBrevoCallback_WhenNoMatchingAuditExists()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_orphan",
            PayloadJson = """
            {
              "event": "clicked",
              "message-id": "msg_missing",
              "email": "unknown@example.com"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_orphan").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseBrevoEmailAndSubjectFallback_WhenMessageIdentifiersAreMissing()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            RecipientEmail = "user2@example.com",
            Subject = "Order Confirmed",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_email_subject",
            PayloadJson = """
            {
              "event": "request",
              "email": "user2@example.com",
              "subject": "Order Confirmed"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_email_subject").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.RecipientEmail == "user2@example.com", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
    }

    [Fact]
    public async Task ExecuteAsync_Should_TrimBrevoFailureReason_WhenReasonIsLongerThanAllowed()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_long_reason",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var longReason = new string('x', 3000);
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_long_reason",
            PayloadJson = $$"""
            {
              "event": "hard_bounce",
              "message-id": "msg_brevo_long_reason",
              "reason": "{{longReason}}"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_long_reason").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_long_reason", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().NotBeNullOrEmpty();
        audit.FailureMessage!.Length.Should().Be(2000);
        audit.FailureMessage.Should().Be(
            "Brevo event 'hard_bounce': " + new string('x', 1973));
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotOverwriteBrevoFailedStatus_WithDeliveryEvent()
    {
        var now = DateTime.UtcNow;
        var occurredAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_delivery_failed",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Failed",
            CompletedAtUtc = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_delivery_failed",
            PayloadJson = $$"""
            {
              "event": "delivered",
              "message-id": "msg_brevo_delivery_failed",
              "ts_event": {{new DateTimeOffset(occurredAt).ToUnixTimeSeconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_delivery_failed").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_delivery_failed", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.CompletedAtUtc.Should().Be(occurredAt);
        audit.FailureMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotChangeBrevoAudit_ForUnsupportedBrevoEvent()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_unknown_event",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending",
            CompletedAtUtc = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_unknown",
            PayloadJson = """
            {
              "event": "weird_event",
              "message-id": "msg_brevo_unknown_event"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_unknown").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_unknown_event", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Pending");
        audit.CompletedAtUtc.Should().BeNull();
        audit.FailureMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseDefaultBrevoFailureReason_WhenReasonIsMissing()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_default_reason",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_empty_reason",
            PayloadJson = """
            {
              "event": "error",
              "message-id": "msg_brevo_default_reason"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_empty_reason").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNullOrEmpty();

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_default_reason", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Failed");
        audit.FailureMessage.Should().Be("Brevo event 'error': No provider reason supplied.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreferBrevoMessageIdOverCorrelationFallback()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_primary",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            CorrelationKey = "corr-dup",
            AttemptedAtUtc = now.AddMinutes(-10),
            Status = "Pending"
        });
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            CorrelationKey = "corr-dup",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_prefer_message",
            PayloadJson = """
            {
              "event": "opened",
              "message-id": "msg_brevo_primary",
              "X-Correlation-Key": "corr-dup"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_prefer_message").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var primary = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_primary", TestContext.Current.CancellationToken);
        var fallback = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_fallback", TestContext.Current.CancellationToken);
        primary.Status.Should().Be("Sent");
        fallback.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseBrevoTsUnixSeconds_WhenTsEventMissing()
    {
        var now = DateTime.UtcNow;
        var eventAt = new DateTimeOffset(now.AddMinutes(-7)).UtcDateTime;
        eventAt = eventAt.AddTicks(-(eventAt.Ticks % TimeSpan.TicksPerSecond));
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_ts",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_ts_event",
            PayloadJson = $$"""
            {
              "event": "delivered",
              "message-id": "msg_brevo_ts",
              "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_ts_event").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_ts", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseBrevoTsUnixMilliseconds_WhenTsEventAndTsMissing()
    {
        var now = DateTime.UtcNow;
        var eventAt = new DateTimeOffset(now.AddMinutes(-8)).UtcDateTime;
        eventAt = eventAt.AddTicks(-(eventAt.Ticks % TimeSpan.TicksPerMillisecond));
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_ts_epoch_precedence",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_ts_epoch_precedence",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_ts_epoch_precedence",
              "date": {{new DateTimeOffset(now.AddMinutes(-1)).ToUnixTimeSeconds()}},
              "ts_epoch": {{new DateTimeOffset(eventAt).ToUnixTimeMilliseconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_ts_epoch_precedence").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_ts_epoch_precedence", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseBrevoTsUnixMilliseconds_WhenOnlyTsEpochProvided()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(now.AddMinutes(-9)).ToUnixTimeMilliseconds()).UtcDateTime;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_ts_epoch",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_ts_epoch",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_ts_epoch",
              "ts_epoch": {{new DateTimeOffset(eventAt).ToUnixTimeMilliseconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_ts_epoch").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_ts_epoch", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseBrevoDateUnixSeconds_WhenOtherTimestampsMissing()
    {
        var now = DateTime.UtcNow;
        var eventAt = new DateTimeOffset(now.AddMinutes(-12)).UtcDateTime;
        eventAt = eventAt.AddTicks(-(eventAt.Ticks % TimeSpan.TicksPerSecond));
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_date",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_date",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_date",
              "date": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_date").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_date", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FallbackToTsWhenTsEventIsInvalid()
    {
        var now = DateTime.UtcNow;
        var eventAt = new DateTimeOffset(now.AddMinutes(-4)).UtcDateTime;
        eventAt = eventAt.AddTicks(-(eventAt.Ticks % TimeSpan.TicksPerSecond));
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_ts_fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_ts_fallback",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_ts_fallback",
              "ts_event": "not-a-number",
              "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}},
              "ts_epoch": "x"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_ts_fallback").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_ts_fallback", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FallbackToClockNow_WhenBrevoTimestampsAreMissingOrInvalid()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_no_timestamp",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_no_timestamp",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_no_timestamp",
              "date": "n/a"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_no_timestamp").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_no_timestamp", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseBrevoTsEpoch_WhenTsIsNonIntegralNumber()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(now.AddMinutes(-10)).ToUnixTimeMilliseconds()).UtcDateTime;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_ts_epoch_fallback",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_ts_epoch_fallback",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_ts_epoch_fallback",
              "ts": 1690000.123,
              "ts_epoch": {{new DateTimeOffset(eventAt).ToUnixTimeMilliseconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_ts_epoch_fallback").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_ts_epoch_fallback", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_UseClockNow_WhenNoBrevoTimestampsProvided()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_no_ts_fields",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_no_ts_fields",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_no_ts_fields",
              "email": "user@example.com",
              "subject": "Welcome"
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_no_ts_fields").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_no_ts_fields", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MatchBrevoAuditByCorrelationKey_WhenMessageIdMisses()
    {
        var now = DateTime.UtcNow;
        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-7)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_correlation_match",
            CorrelationKey = "corr-key-123",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_correlation",
            PayloadJson = $$"""
            {
              "event": "delivered",
              "message-id": "unrelated-message-id",
              "X-Correlation-Key": "corr-key-123",
              "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_correlation").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.CorrelationKey == "corr-key-123", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PrioritizeBrevoTsEvent_OverTs()
    {
        var now = DateTime.UtcNow;
        var tsEventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-12)).ToUnixTimeSeconds()).UtcDateTime;
        var tsAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-4)).ToUnixTimeSeconds()).UtcDateTime;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "msg_brevo_ts_precedence",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_ts_precedence",
            PayloadJson = $$"""
            {
              "event": "opened",
              "message-id": "msg_brevo_ts_precedence",
              "ts_event": {{new DateTimeOffset(tsEventAt).ToUnixTimeSeconds()}},
              "ts": {{new DateTimeOffset(tsAt).ToUnixTimeSeconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_ts_precedence").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.Provider == "Brevo" && x.ProviderMessageId == "msg_brevo_ts_precedence", TestContext.Current.CancellationToken);
        audit.CompletedAtUtc.Should().Be(tsEventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MatchBrevoAuditByEmailAndSubject_WhenMessageIdAndCorrelationMissing()
    {
        var now = DateTime.UtcNow;
        var current = DateTimeOffset.Now.UtcDateTime;
        var recentAttemptedAt = current.AddDays(-3);
        var oldAttemptedAt = current.AddDays(-10);
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "old-msg",
            RecipientEmail = "user@example.com",
            IntendedRecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = oldAttemptedAt,
            Status = "Pending"
        });
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "recent-msg",
            RecipientEmail = "user@example.com",
            IntendedRecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = recentAttemptedAt,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var eventAt = DateTimeOffset.FromUnixTimeSeconds(new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()).UtcDateTime;
        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_email_subject",
            PayloadJson = $$"""
            {
              "event": "opened",
              "email": "user@example.com",
              "subject": "Welcome",
              "ts": {{new DateTimeOffset(eventAt).ToUnixTimeSeconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_email_subject").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");

        var audit = await db.Set<EmailDispatchAudit>()
            .SingleAsync(x => x.ProviderMessageId == "recent-msg", TestContext.Current.CancellationToken);
        audit.Status.Should().Be("Sent");
        audit.CompletedAtUtc.Should().Be(eventAt);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotMatchBrevoAuditByEmailAndSubject_WhenAttemptedAtOutside7Days()
    {
        var now = DateTime.UtcNow;
        await using var db = ProviderCallbackWorkerTestDbContext.Create();
        db.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
        {
            Provider = "Brevo",
            ProviderMessageId = "old-msg",
            RecipientEmail = "user@example.com",
            Subject = "Welcome",
            AttemptedAtUtc = now.AddDays(-10),
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
        {
            Provider = "brevo",
            CallbackType = "email_event",
            IdempotencyKey = "evt_brevo_email_subject_too_old",
            PayloadJson = $$"""
            {
              "event": "opened",
              "email": "user@example.com",
              "subject": "Welcome",
              "ts": {{new DateTimeOffset(now.AddMinutes(-2)).ToUnixTimeSeconds()}}
            }
            """,
            Status = "Pending"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var handler = new ProcessBrevoTransactionalEmailWebhookHandler(db, new FixedClock(now), localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(handler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(250, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        var item = await db.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.Id == db.Set<ProviderCallbackInboxMessage>().Single(y => y.IdempotencyKey == "evt_brevo_email_subject_too_old").Id, TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        db.Set<EmailDispatchAudit>().Should().ContainSingle(x => x.Provider == "Brevo" && x.ProviderMessageId == "old-msg");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessMessage_WhenClaimSaveFailsTransientlyThenSucceeds()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_recovery_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;
        var shipmentReference = "dhl-ref-retry";

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<Darwin.Domain.Entities.Orders.Shipment>().Add(new Darwin.Domain.Entities.Orders.Shipment
            {
                Carrier = "DHL",
                Service = "Standard",
                ProviderShipmentReference = shipmentReference,
                OrderId = Guid.NewGuid(),
                Status = ShipmentStatus.Pending
            });
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "dhl",
                CallbackType = "shipment_event",
                IdempotencyKey = "evt_claim_transient_success",
                PayloadJson = """
                {
                  "providerShipmentReference": "dhl-ref-retry",
                  "carrierEventKey": "delivered",
                  "occurredAtUtc": "2026-05-02T10:00:00Z",
                  "providerStatus": "DELIVERED",
                  "trackingNumber": "TRK-555",
                  "labelUrl": "https://example.test/label.png"
                }
                """,
                Status = "Pending",
                AttemptCount = 0,
                CreatedAtUtc = now.AddHours(-2)
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(databaseName, failFromCallNumber: 1);
        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var validator = new ApplyShipmentCarrierEventValidator(localizer.Object);
        var dhlHandler = new ApplyShipmentCarrierEventHandler(db, validator, localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyShipmentCarrierEventHandler))).Returns(dhlHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_transient_success", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Processed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().NotBeNull();
        item.FailureReason.Should().BeNull();
        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessMessage_WhenCompletionSaveConcurrencyOccurs()
    {
        var databaseName = $"darwin_provider_callback_worker_completion_concurrency_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_completion_concurrency",
                PayloadJson = "{}",
                Status = "Pending"
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 1,
            failWithConcurrency: true,
            failCompletionOnly: true);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_concurrency", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Pending");
        item.ProcessedAtUtc.Should().BeNull();
        item.AttemptCount.Should().Be(1);
        item.FailureReason.Should().BeNullOrEmpty();
        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBatchAndOnlyProcessMessagesAfterClaimConcurrency()
    {
        var databaseName = $"darwin_provider_callback_worker_completion_batch_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().AddRange(
                new ProviderCallbackInboxMessage
                {
                    Provider = "Unknown",
                    CallbackType = "webhook_event",
                    IdempotencyKey = "evt_completion_batch_concurrency",
                    PayloadJson = "{}",
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-10),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                },
                new ProviderCallbackInboxMessage
                {
                    Provider = "Unknown",
                    CallbackType = "webhook_event",
                    IdempotencyKey = "evt_completion_batch_failed",
                    PayloadJson = "{}",
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-5),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(databaseName, failFromCallNumber: 1, failWithConcurrency: true);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(700, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var concurrencyItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_batch_concurrency", TestContext.Current.CancellationToken);
        var failedItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_batch_failed", TestContext.Current.CancellationToken);

        concurrencyItem.Status.Should().Be("Pending");
        concurrencyItem.AttemptCount.Should().Be(1);
        concurrencyItem.ProcessedAtUtc.Should().BeNull();
        concurrencyItem.FailureReason.Should().BeNull();
        concurrencyItem.LastAttemptAtUtc.Should().Be(now);

        failedItem.Status.Should().Be("Failed");
        failedItem.AttemptCount.Should().Be(1);
        failedItem.ProcessedAtUtc.Should().BeNull();
        failedItem.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        failedItem.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessSecondMessage_WhenFirstClaimSaveRetriesAndFailsToProcess()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_batch_failure_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<Darwin.Domain.Entities.Orders.Shipment>().Add(new Darwin.Domain.Entities.Orders.Shipment
            {
                Carrier = "DHL",
                Service = "Standard",
                ProviderShipmentReference = "dhl-ref-batch-claim",
                OrderId = Guid.NewGuid(),
                Status = ShipmentStatus.Pending
            });
            setupDb.Set<ProviderCallbackInboxMessage>().AddRange(
                new ProviderCallbackInboxMessage
                {
                    Provider = "Unknown",
                    CallbackType = "webhook_event",
                    IdempotencyKey = "evt_claim_batch_first_skipped",
                    PayloadJson = "{}",
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-10),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                },
                new ProviderCallbackInboxMessage
                {
                    Provider = "DHL",
                    CallbackType = "shipment_event",
                    IdempotencyKey = "evt_claim_batch_second_processed",
                    PayloadJson = """
                    {
                      "providerShipmentReference": "dhl-ref-batch-claim",
                      "carrierEventKey": "delivered",
                      "occurredAtUtc": "2026-05-02T10:00:00Z",
                      "providerStatus": "DELIVERED",
                      "trackingNumber": "TRK-888",
                      "labelUrl": "https://example.test/label-batch.png"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-5),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(databaseName, failFromCallNumber: 1);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var validator = new ApplyShipmentCarrierEventValidator(localizer.Object);
        var dhlHandler = new ApplyShipmentCarrierEventHandler(db, validator, localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyShipmentCarrierEventHandler))).Returns(dhlHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var skippedItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_batch_first_skipped", TestContext.Current.CancellationToken);
        var processedItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_batch_second_processed", TestContext.Current.CancellationToken);

        skippedItem.Status.Should().Be("Failed");
        skippedItem.AttemptCount.Should().Be(1);
        skippedItem.ProcessedAtUtc.Should().BeNull();
        skippedItem.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        skippedItem.LastAttemptAtUtc.Should().Be(now);

        processedItem.Status.Should().Be("Processed");
        processedItem.AttemptCount.Should().Be(1);
        processedItem.ProcessedAtUtc.Should().NotBeNull();
        processedItem.FailureReason.Should().BeNullOrEmpty();
        processedItem.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessMessage_WhenSingleClaimSaveRetrySucceeds()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_retry_single_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<Darwin.Domain.Entities.Orders.Shipment>().Add(new Darwin.Domain.Entities.Orders.Shipment
            {
                Carrier = "DHL",
                Service = "Standard",
                ProviderShipmentReference = "dhl-ref-claim-retry-single",
                OrderId = Guid.NewGuid(),
                Status = ShipmentStatus.Pending
            });
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "DHL",
                CallbackType = "shipment_event",
                IdempotencyKey = "evt_claim_single_retry",
                PayloadJson = """
                {
                  "providerShipmentReference": "dhl-ref-claim-retry-single",
                  "carrierEventKey": "delivered",
                  "occurredAtUtc": "2026-05-02T10:00:00Z",
                  "providerStatus": "DELIVERED",
                  "trackingNumber": "TRK-990",
                  "labelUrl": "https://example.test/label-990.png"
                }
                """,
                Status = "Pending",
                CreatedAtUtc = now.AddMinutes(-10),
                LastAttemptAtUtc = null,
                AttemptCount = 0
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 1);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var validator = new ApplyShipmentCarrierEventValidator(localizer.Object);
        var dhlHandler = new ApplyShipmentCarrierEventHandler(db, validator, localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyShipmentCarrierEventHandler))).Returns(dhlHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var processedItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_single_retry", TestContext.Current.CancellationToken);

        processedItem.Status.Should().Be("Processed");
        processedItem.AttemptCount.Should().Be(1);
        processedItem.ProcessedAtUtc.Should().NotBeNull();
        processedItem.FailureReason.Should().BeNullOrEmpty();
        processedItem.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NormalizeInvalidWorkerOptions_WhenEnabled()
    {
        var databaseName = $"darwin_provider_callback_worker_invalid_options_normalization_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_invalid_options_normalized",
                PayloadJson = "{}",
                Status = "Pending",
                CreatedAtUtc = now.AddMinutes(-10),
                LastAttemptAtUtc = null,
                AttemptCount = 0
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackWorkerTestDbContext.Create(databaseName);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 0,
                BatchSize = 0,
                RetryCooldownSeconds = 0,
                MaxAttempts = 0
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_invalid_options_normalized", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        item.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ContinueBatch_WhenCompletionSaveFails()
    {
        var databaseName = $"darwin_provider_callback_worker_completion_batch_fallback_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<Darwin.Domain.Entities.Orders.Shipment>().Add(new Darwin.Domain.Entities.Orders.Shipment
            {
                Carrier = "DHL",
                Service = "Standard",
                ProviderShipmentReference = "dhl-ref-batch-fallback",
                OrderId = Guid.NewGuid(),
                Status = ShipmentStatus.Pending
            });
            setupDb.Set<ProviderCallbackInboxMessage>().AddRange(
                new ProviderCallbackInboxMessage
                {
                    Provider = "DHL",
                    CallbackType = "shipment_event",
                    IdempotencyKey = "evt_completion_batch_continue_failed",
                    PayloadJson = """
                    {
                      "providerShipmentReference": "dhl-ref-batch-fallback",
                      "carrierEventKey": "delivered",
                      "occurredAtUtc": "2026-05-02T10:00:00Z",
                      "providerStatus": "DELIVERED",
                      "trackingNumber": "TRK-555",
                      "labelUrl": "https://example.test/label.png"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-10),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                },
                new ProviderCallbackInboxMessage
                {
                    Provider = "Unknown",
                    CallbackType = "webhook_event",
                    IdempotencyKey = "evt_completion_batch_continue_next",
                    PayloadJson = "{}",
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-5),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 2,
            failCompletionOnly: true);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var validator = new ApplyShipmentCarrierEventValidator(localizer.Object);
        var dhlHandler = new ApplyShipmentCarrierEventHandler(db, validator, localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyShipmentCarrierEventHandler))).Returns(dhlHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var failedCompletionItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_batch_continue_failed", TestContext.Current.CancellationToken);
        var nextItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_batch_continue_next", TestContext.Current.CancellationToken);

        failedCompletionItem.Status.Should().Be("Processed");
        failedCompletionItem.AttemptCount.Should().Be(1);
        failedCompletionItem.ProcessedAtUtc.Should().NotBeNull();
        failedCompletionItem.FailureReason.Should().BeNullOrEmpty();
        failedCompletionItem.LastAttemptAtUtc.Should().Be(now);

        nextItem.Status.Should().Be("Failed");
        nextItem.AttemptCount.Should().Be(1);
        nextItem.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        nextItem.ProcessedAtUtc.Should().BeNull();
        nextItem.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessFirstItem_WhenCompletionSaveFailsForLaterItemInBatch()
    {
        var databaseName = $"darwin_provider_callback_worker_completion_batch_second_failed_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<Darwin.Domain.Entities.Orders.Shipment>().Add(new Darwin.Domain.Entities.Orders.Shipment
            {
                Carrier = "DHL",
                Service = "Standard",
                ProviderShipmentReference = "dhl-ref-batch-second-success",
                OrderId = Guid.NewGuid(),
                Status = ShipmentStatus.Pending
            });
            setupDb.Set<Darwin.Domain.Entities.Orders.Shipment>().Add(new Darwin.Domain.Entities.Orders.Shipment
            {
                Carrier = "DHL",
                Service = "Standard",
                ProviderShipmentReference = "dhl-ref-batch-second-fail",
                OrderId = Guid.NewGuid(),
                Status = ShipmentStatus.Pending
            });
            setupDb.Set<ProviderCallbackInboxMessage>().AddRange(
                new ProviderCallbackInboxMessage
                {
                    Provider = "DHL",
                    CallbackType = "shipment_event",
                    IdempotencyKey = "evt_completion_batch_first_succeeds",
                    PayloadJson = """
                    {
                      "providerShipmentReference": "dhl-ref-batch-second-success",
                      "carrierEventKey": "delivered",
                      "occurredAtUtc": "2026-05-02T10:00:00Z",
                      "providerStatus": "DELIVERED",
                      "trackingNumber": "TRK-666",
                      "labelUrl": "https://example.test/label-1.png"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-10),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                },
                new ProviderCallbackInboxMessage
                {
                    Provider = "DHL",
                    CallbackType = "shipment_event",
                    IdempotencyKey = "evt_completion_batch_second_fails",
                    PayloadJson = """
                    {
                      "providerShipmentReference": "dhl-ref-batch-second-fail",
                      "carrierEventKey": "delivered",
                      "occurredAtUtc": "2026-05-02T10:00:00Z",
                      "providerStatus": "DELIVERED",
                      "trackingNumber": "TRK-777",
                      "labelUrl": "https://example.test/label-2.png"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-5),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 2,
            failCompletionOnly: true);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var validator = new ApplyShipmentCarrierEventValidator(localizer.Object);
        var dhlHandler = new ApplyShipmentCarrierEventHandler(db, validator, localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyShipmentCarrierEventHandler))).Returns(dhlHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var firstItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_batch_first_succeeds", TestContext.Current.CancellationToken);
        var secondItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_batch_second_fails", TestContext.Current.CancellationToken);

        firstItem.Status.Should().Be("Processed");
        firstItem.AttemptCount.Should().Be(1);
        firstItem.ProcessedAtUtc.Should().NotBeNull();
        firstItem.FailureReason.Should().BeNullOrEmpty();
        firstItem.LastAttemptAtUtc.Should().Be(now);

        secondItem.Status.Should().Be("Processed");
        secondItem.AttemptCount.Should().Be(1);
        secondItem.ProcessedAtUtc.Should().NotBeNull();
        secondItem.FailureReason.Should().BeNullOrEmpty();
        secondItem.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ProcessBothMessages_WhenSecondClaimSaveRetriesAndSucceeds()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_batch_transient_retry_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<Darwin.Domain.Entities.Orders.Shipment>().AddRange(
                new Darwin.Domain.Entities.Orders.Shipment
                {
                    Carrier = "DHL",
                    Service = "Standard",
                    ProviderShipmentReference = "dhl-ref-batch-transient-1",
                    OrderId = Guid.NewGuid(),
                    Status = ShipmentStatus.Pending
                },
                new Darwin.Domain.Entities.Orders.Shipment
                {
                    Carrier = "DHL",
                    Service = "Standard",
                    ProviderShipmentReference = "dhl-ref-batch-transient-2",
                    OrderId = Guid.NewGuid(),
                    Status = ShipmentStatus.Pending
                });

            setupDb.Set<ProviderCallbackInboxMessage>().AddRange(
                new ProviderCallbackInboxMessage
                {
                    Provider = "DHL",
                    CallbackType = "shipment_event",
                    IdempotencyKey = "evt_claim_batch_retry_first",
                    PayloadJson = """
                    {
                      "providerShipmentReference": "dhl-ref-batch-transient-1",
                      "carrierEventKey": "delivered",
                      "occurredAtUtc": "2026-05-02T10:00:00Z",
                      "providerStatus": "DELIVERED",
                      "trackingNumber": "TRK-901",
                      "labelUrl": "https://example.test/label-901.png"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-10),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                },
                new ProviderCallbackInboxMessage
                {
                    Provider = "DHL",
                    CallbackType = "shipment_event",
                    IdempotencyKey = "evt_claim_batch_retry_second",
                    PayloadJson = """
                    {
                      "providerShipmentReference": "dhl-ref-batch-transient-2",
                      "carrierEventKey": "delivered",
                      "occurredAtUtc": "2026-05-02T10:00:00Z",
                      "providerStatus": "DELIVERED",
                      "trackingNumber": "TRK-902",
                      "labelUrl": "https://example.test/label-902.png"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddMinutes(-5),
                    LastAttemptAtUtc = null,
                    AttemptCount = 0
                });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(
            databaseName,
            failFromCallNumber: 3);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var validator = new ApplyShipmentCarrierEventValidator(localizer.Object);
        var dhlHandler = new ApplyShipmentCarrierEventHandler(db, validator, localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ApplyShipmentCarrierEventHandler))).Returns(dhlHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var firstItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_batch_retry_first", TestContext.Current.CancellationToken);
        var secondItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_batch_retry_second", TestContext.Current.CancellationToken);

        firstItem.Status.Should().Be("Processed");
        firstItem.AttemptCount.Should().Be(1);
        firstItem.ProcessedAtUtc.Should().NotBeNull();
        firstItem.FailureReason.Should().BeNullOrEmpty();
        firstItem.LastAttemptAtUtc.Should().Be(now);

        secondItem.Status.Should().Be("Processed");
        secondItem.AttemptCount.Should().Be(1);
        secondItem.ProcessedAtUtc.Should().NotBeNull();
        secondItem.FailureReason.Should().BeNullOrEmpty();
        secondItem.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_FallbackToDirectUpdate_WhenCompletionSaveFails()
    {
        var databaseName = $"darwin_provider_callback_worker_completion_fallback_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_completion_fallback_failed",
                PayloadJson = "{}",
                Status = "Pending"
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(databaseName, failFromCallNumber: 2);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_fallback_failed", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        item.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotFallbackToDirectUpdate_WhenCompletionSaveRecoversAfterTransientFailure()
    {
        var databaseName = $"darwin_provider_callback_worker_completion_retry_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_completion_retry_failure",
                PayloadJson = "{}",
                Status = "Pending"
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceThrowingSaveDbContext.Create(databaseName, failFromCallNumber: 2);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 1
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_retry_failure", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Failed");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().Contain("Unsupported provider callback provider 'Unknown'");
        item.LastAttemptAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessMessage_WhenClaimSaveThrowsConcurrencyException()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_concurrency_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_claim_concurrency",
                PayloadJson = "{}",
                Status = "Pending",
                AttemptCount = 0,
                LastAttemptAtUtc = null,
                CreatedAtUtc = now.AddHours(-2)
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext.Create(databaseName);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(700, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_concurrency", TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(0);
        item.Status.Should().Be("Pending");
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNull();
        item.LastAttemptAtUtc.Should().BeNull();
        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessFailedMessage_WhenClaimSaveFails()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_failure_failed_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_failed_claim_failure",
                PayloadJson = "{}",
                Status = "Failed",
                AttemptCount = 1,
                LastAttemptAtUtc = null,
                CreatedAtUtc = now.AddHours(-2)
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext.Create(databaseName);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                RetryCooldownSeconds = 0,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_failed_claim_failure", TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(1);
        item.Status.Should().Be("Failed");
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNull();
        item.LastAttemptAtUtc.Should().BeNull();
        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessMessage_WhenClaimSaveFails()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_failure_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_claim_failure",
                PayloadJson = "{}",
                Status = "Pending",
                AttemptCount = 0,
                LastAttemptAtUtc = null,
                CreatedAtUtc = now.AddHours(-2)
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceAlwaysFailingSaveDbContext.Create(databaseName);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 5,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(900, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_failure", TestContext.Current.CancellationToken);

        item.AttemptCount.Should().Be(0);
        item.Status.Should().Be("Pending");
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNull();
        item.LastAttemptAtUtc.Should().BeNull();
        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PreserveMessage_WhenCancellationRequestedDuringClaimSave()
    {
        var databaseName = $"darwin_provider_callback_worker_claim_cancel_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_claim_save_cancelled",
                PayloadJson = "{}",
                Status = "Pending",
                AttemptCount = 0,
                LastAttemptAtUtc = null,
                CreatedAtUtc = now.AddHours(-2)
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceCancellationDelayDbContext.Create(databaseName);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        cancellationTokenSource.Cancel();
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_claim_save_cancelled", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Pending");
        item.AttemptCount.Should().Be(0);
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNull();
        item.LastAttemptAtUtc.Should().BeNull();
        scopeFactory.Verify(x => x.CreateScope(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotFinalizeMessage_WhenCancellationRequestedDuringCompletionSave()
    {
        var databaseName = $"darwin_provider_callback_worker_completion_cancel_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;
        var completionSaveStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Unknown",
                CallbackType = "webhook_event",
                IdempotencyKey = "evt_completion_save_cancelled",
                PayloadJson = "{}",
                Status = "Pending",
                AttemptCount = 0,
                LastAttemptAtUtc = null,
                CreatedAtUtc = now.AddHours(-2)
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceCancellationDuringCompletionSaveDbContext.Create(
            databaseName,
            completionSaveStarted);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(cancellationTokenSource.Token);
        await completionSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellationTokenSource.Cancel();
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_completion_save_cancelled", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Pending");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNull();
        item.LastAttemptAtUtc.Should().NotBeNull();
        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotFinalizeMessage_WhenCancellationRequestedDuringCallbackHandling()
    {
        var databaseName = $"darwin_provider_callback_worker_processing_cancel_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;
        var processingStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<EmailDispatchAudit>().Add(new EmailDispatchAudit
            {
                Provider = "Brevo",
                RecipientEmail = "user@example.com",
                Subject = "Welcome",
                ProviderMessageId = "msg-cancel-processing",
                AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                Status = "Pending"
            });
            setupDb.Set<ProviderCallbackInboxMessage>().Add(new ProviderCallbackInboxMessage
            {
                Provider = "Brevo",
                CallbackType = "email_event",
                IdempotencyKey = "evt_brevo_processing_cancelled",
                PayloadJson = """
                {
                  "event": "delivered",
                  "message-id": "msg-cancel-processing",
                  "email": "user@example.com",
                  "subject": "Welcome"
                }
                """,
                Status = "Pending",
                AttemptCount = 0,
                LastAttemptAtUtc = null,
                CreatedAtUtc = now.AddHours(-2)
            });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext.Create(
            databaseName,
            processingStarted);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(now),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 1,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(cancellationTokenSource.Token);
        await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellationTokenSource.Cancel();
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var item = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_brevo_processing_cancelled", TestContext.Current.CancellationToken);

        item.Status.Should().Be("Pending");
        item.AttemptCount.Should().Be(1);
        item.ProcessedAtUtc.Should().BeNull();
        item.FailureReason.Should().BeNull();
        item.LastAttemptAtUtc.Should().NotBeNull();
        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotProcessNextItem_WhenCancellationRequestedDuringFirstItemHandlingInBatch()
    {
        var databaseName = $"darwin_provider_callback_worker_batch_processing_cancel_{Guid.NewGuid()}";
        var now = DateTime.UtcNow;
        var processingStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using (var setupDb = ProviderCallbackWorkerTestDbContext.Create(databaseName))
        {
            setupDb.Set<EmailDispatchAudit>().AddRange(
                new EmailDispatchAudit
                {
                    Provider = "Brevo",
                    RecipientEmail = "user1@example.com",
                    Subject = "Welcome",
                    ProviderMessageId = "msg-batch-cancel-first",
                    AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                    Status = "Pending"
                },
                new EmailDispatchAudit
                {
                    Provider = "Brevo",
                    RecipientEmail = "user2@example.com",
                    Subject = "Welcome",
                    ProviderMessageId = "msg-batch-cancel-second",
                    AttemptedAtUtc = DateTime.UtcNow.AddMinutes(-2),
                    Status = "Pending"
                });

            setupDb.Set<ProviderCallbackInboxMessage>().AddRange(
                new ProviderCallbackInboxMessage
                {
                    Provider = "Brevo",
                    CallbackType = "email_event",
                    IdempotencyKey = "evt_brevo_batch_processing_cancel_first",
                    PayloadJson = """
                    {
                      "event": "delivered",
                      "message-id": "msg-batch-cancel-first",
                      "email": "user1@example.com",
                      "subject": "Welcome"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddHours(-2)
                },
                new ProviderCallbackInboxMessage
                {
                    Provider = "Brevo",
                    CallbackType = "email_event",
                    IdempotencyKey = "evt_brevo_batch_processing_cancel_second",
                    PayloadJson = """
                    {
                      "event": "delivered",
                      "message-id": "msg-batch-cancel-second",
                      "email": "user2@example.com",
                      "subject": "Welcome"
                    }
                    """,
                    Status = "Pending",
                    CreatedAtUtc = now.AddHours(-1)
                });

            await setupDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext.Create(
            databaseName,
            processingStarted);

        var localizer = new Mock<IStringLocalizer<ValidationResource>>();
        localizer.Setup(x => x[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
        var brevoHandler = new ProcessBrevoTransactionalEmailWebhookHandler(
            db,
            new FixedClock(now),
            localizer.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IAppDbContext))).Returns(db);
        serviceProvider.Setup(x => x.GetService(typeof(ProcessBrevoTransactionalEmailWebhookHandler))).Returns(brevoHandler);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new ProviderCallbackBackgroundService(
            scopeFactory.Object,
            Options.Create(new ProviderCallbackWorkerOptions
            {
                Enabled = true,
                PollIntervalSeconds = 5,
                BatchSize = 2,
                MaxAttempts = 3
            }),
            new FixedClock(now),
            new Mock<ILogger<ProviderCallbackBackgroundService>>().Object);

        await service.StartAsync(cancellationTokenSource.Token);
        await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellationTokenSource.Cancel();
        await service.StopAsync(TestContext.Current.CancellationToken);

        await using var verificationDb = ProviderCallbackWorkerTestDbContext.Create(databaseName);
        var firstItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_brevo_batch_processing_cancel_first", TestContext.Current.CancellationToken);
        var secondItem = await verificationDb.Set<ProviderCallbackInboxMessage>()
            .SingleAsync(x => x.IdempotencyKey == "evt_brevo_batch_processing_cancel_second", TestContext.Current.CancellationToken);

        firstItem.Status.Should().Be("Pending");
        firstItem.AttemptCount.Should().Be(1);
        firstItem.ProcessedAtUtc.Should().BeNull();
        firstItem.FailureReason.Should().BeNull();
        firstItem.LastAttemptAtUtc.Should().NotBeNull();

        secondItem.Status.Should().Be("Pending");
        secondItem.AttemptCount.Should().Be(0);
        secondItem.ProcessedAtUtc.Should().BeNull();
        secondItem.FailureReason.Should().BeNull();
        secondItem.LastAttemptAtUtc.Should().BeNull();

        scopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class ProviderCallbackWorkerTestDbContext : DbContext, IAppDbContext
    {
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.Shipment> Shipments { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;

        private ProviderCallbackWorkerTestDbContext(DbContextOptions<ProviderCallbackWorkerTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ProviderCallbackWorkerTestDbContext Create()
        {
            return Create($"darwin_provider_callback_worker_tests_{Guid.NewGuid()}");
        }

        public static ProviderCallbackWorkerTestDbContext Create(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackWorkerTestDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ProviderCallbackWorkerTestDbContext(options);
        }
    }

    private sealed class ProviderCallbackBackgroundServiceAlwaysFailingSaveDbContext : DbContext, IAppDbContext
    {
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.Shipment> Shipments { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;

        private ProviderCallbackBackgroundServiceAlwaysFailingSaveDbContext(
            DbContextOptions<ProviderCallbackBackgroundServiceAlwaysFailingSaveDbContext> options)
            : base(options)
        {
        }

        public static ProviderCallbackBackgroundServiceAlwaysFailingSaveDbContext Create(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackBackgroundServiceAlwaysFailingSaveDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ProviderCallbackBackgroundServiceAlwaysFailingSaveDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override int SaveChanges()
        {
            throw new DbUpdateException("simulated queue claim save failure");
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<int>(new DbUpdateException("simulated queue claim save failure"));
        }
    }

    private sealed class ProviderCallbackBackgroundServiceThrowingSaveDbContext : DbContext, IAppDbContext
    {
        private readonly int _failFromCallNumber;
        private readonly bool _failWithConcurrency;
        private readonly bool _failCompletionOnly;
        private int _saveCallCount;
        private int _completionSaveCallCount;

        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.Shipment> Shipments { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;

        private ProviderCallbackBackgroundServiceThrowingSaveDbContext(
            DbContextOptions<ProviderCallbackBackgroundServiceThrowingSaveDbContext> options,
            int failFromCallNumber,
            bool failWithConcurrency,
            bool failCompletionOnly)
            : base(options)
        {
            _failFromCallNumber = failFromCallNumber;
            _failWithConcurrency = failWithConcurrency;
            _failCompletionOnly = failCompletionOnly;
        }

        public static ProviderCallbackBackgroundServiceThrowingSaveDbContext Create(
            string databaseName,
            int failFromCallNumber,
            bool failWithConcurrency = false,
            bool failCompletionOnly = false)
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackBackgroundServiceThrowingSaveDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ProviderCallbackBackgroundServiceThrowingSaveDbContext(
                options,
                failFromCallNumber,
                failWithConcurrency,
                failCompletionOnly);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override int SaveChanges()
        {
            if (ShouldFailSave())
            {
                ThrowFailure();
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ShouldFailSave())
            {
                return Task.FromException<int>(CreateFailureException());
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private bool ShouldFailSave()
        {
            Interlocked.Increment(ref _saveCallCount);
            if (!_failCompletionOnly)
            {
                return _saveCallCount == _failFromCallNumber;
            }

            if (!IsCompletionSave())
            {
                return false;
            }

            return Interlocked.Increment(ref _completionSaveCallCount) == _failFromCallNumber;
        }

        private bool IsCompletionSave()
        {
            return ChangeTracker.Entries<ProviderCallbackInboxMessage>()
                .Any(entry => entry.State != EntityState.Unchanged && !string.Equals(entry.Entity.Status, "Pending", StringComparison.OrdinalIgnoreCase));
        }

        private Exception CreateFailureException()
        {
            if (_failWithConcurrency)
            {
                return new DbUpdateConcurrencyException("simulated queue completion concurrency failure");
            }

            return new DbUpdateException("simulated queue completion save failure");
        }

        private void ThrowFailure()
        {
            throw CreateFailureException();
        }
    }

    private sealed class ProviderCallbackBackgroundServiceCancellationDuringCompletionSaveDbContext : DbContext, IAppDbContext
    {
        private readonly TaskCompletionSource<bool> _completionSaveStarted;
        private int _saveCallCount;

        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.Shipment> Shipments { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;

        private ProviderCallbackBackgroundServiceCancellationDuringCompletionSaveDbContext(
            DbContextOptions<ProviderCallbackBackgroundServiceCancellationDuringCompletionSaveDbContext> options,
            TaskCompletionSource<bool> completionSaveStarted)
            : base(options)
        {
            _completionSaveStarted = completionSaveStarted;
        }

        public static ProviderCallbackBackgroundServiceCancellationDuringCompletionSaveDbContext Create(
            string databaseName,
            TaskCompletionSource<bool> completionSaveStarted)
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackBackgroundServiceCancellationDuringCompletionSaveDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ProviderCallbackBackgroundServiceCancellationDuringCompletionSaveDbContext(options, completionSaveStarted);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override int SaveChanges()
        {
            return SaveChangesAsync().GetAwaiter().GetResult();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _saveCallCount) == 2)
            {
                _completionSaveStarted.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }

            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext : DbContext, IAppDbContext
    {
        private readonly TaskCompletionSource<bool> _processingStarted;
        private int _saveCallCount;

        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.Shipment> Shipments { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;

        private ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext(
            DbContextOptions<ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext> options,
            TaskCompletionSource<bool> processingStarted)
            : base(options)
        {
            _processingStarted = processingStarted;
        }

        public static ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext Create(
            string databaseName,
            TaskCompletionSource<bool> processingStarted)
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ProviderCallbackBackgroundServiceCancellationDuringProcessingDbContext(options, processingStarted);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override int SaveChanges()
        {
            return SaveChangesAsync().GetAwaiter().GetResult();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _saveCallCount) == 2)
            {
                _processingStarted.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }

            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ProviderCallbackBackgroundServiceCancellationDelayDbContext : DbContext, IAppDbContext
    {
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.Shipment> Shipments { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;

        private ProviderCallbackBackgroundServiceCancellationDelayDbContext(
            DbContextOptions<ProviderCallbackBackgroundServiceCancellationDelayDbContext> options)
            : base(options)
        {
        }

        public static ProviderCallbackBackgroundServiceCancellationDelayDbContext Create(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackBackgroundServiceCancellationDelayDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ProviderCallbackBackgroundServiceCancellationDelayDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override int SaveChanges()
        {
            throw new TaskCanceledException("test cancellation simulation");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return 0;
        }
    }

    private sealed class ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext : DbContext, IAppDbContext
    {
        public DbSet<ProviderCallbackInboxMessage> ProviderCallbackInboxMessages { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<EmailDispatchAudit> EmailDispatchAudits { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.Shipment> Shipments { get; set; } = null!;
        public DbSet<Darwin.Domain.Entities.Orders.ShipmentCarrierEvent> ShipmentCarrierEvents { get; set; } = null!;

        private ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext(
            DbContextOptions<ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext> options)
            : base(options)
        {
        }

        public static ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext Create(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new ProviderCallbackBackgroundServiceConcurrencyThrowingSaveDbContext(options);
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override int SaveChanges()
        {
            throw new DbUpdateConcurrencyException("simulated queue claim concurrency failure");
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<int>(new DbUpdateConcurrencyException("simulated queue claim concurrency failure"));
        }
    }
}
