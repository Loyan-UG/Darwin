using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Darwin.WebApi.Tests.Services;

public sealed class InactiveReminderBackgroundServiceTests
{
    [Fact]
    public void Ctor_Should_Throw_WhenDependenciesAreMissing()
    {
        var options = new InactiveReminderWorkerOptions();
        var optionsMonitor = new Mock<IOptionsMonitor<InactiveReminderWorkerOptions>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        Action noScopeFactory = () => new InactiveReminderBackgroundService(null!, optionsMonitor.Object, new Mock<ILogger<InactiveReminderBackgroundService>>().Object);
        Action noOptions = () => new InactiveReminderBackgroundService(new Mock<IServiceScopeFactory>().Object, null!, new Mock<ILogger<InactiveReminderBackgroundService>>().Object);
        Action noLogger = () => new InactiveReminderBackgroundService(new Mock<IServiceScopeFactory>().Object, optionsMonitor.Object, null!);

        noScopeFactory.Should().Throw<ArgumentNullException>().WithParameterName("scopeFactory");
        noOptions.Should().Throw<ArgumentNullException>().WithParameterName("optionsMonitor");
        noLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteAsync_Should_StopImmediately_WhenCancellationIsRequestedBeforeStart()
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var optionsMonitor = new Mock<IOptionsMonitor<InactiveReminderWorkerOptions>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new InactiveReminderWorkerOptions());
        var service = new InactiveReminderBackgroundService(
            scopeFactoryMock.Object,
            optionsMonitor.Object,
            new Mock<ILogger<InactiveReminderBackgroundService>>().Object);

        var tokenSource = new CancellationTokenSource();
        await tokenSource.CancelAsync();

        await service.StartAsync(tokenSource.Token);
        await service.StopAsync(tokenSource.Token);

        scopeFactoryMock.Verify(sf => sf.CreateScope(), Times.Never);
    }
}
