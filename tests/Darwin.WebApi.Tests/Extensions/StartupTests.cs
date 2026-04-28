using Darwin.WebApi.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;

namespace Darwin.WebApi.Tests.Extensions;

public sealed class StartupTests
{
    [Fact]
    public async Task UseWebApiStartupAsync_Should_Throw_WhenAppIsMissing()
    {
        Func<Task> act = () => ((WebApplication)null!).UseWebApiStartupAsync();

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("app");
    }
}
