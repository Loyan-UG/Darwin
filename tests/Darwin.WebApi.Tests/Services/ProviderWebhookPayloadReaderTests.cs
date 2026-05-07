using System.Text;
using Darwin.WebApi.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Darwin.WebApi.Tests.Services;

public sealed class ProviderWebhookPayloadReaderTests
{
    [Fact]
    public async Task ReadAsync_Should_ReturnPayload_And_ResetBodyPosition_ForSmallPayload()
    {
        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes / 2);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = payload.Length;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().Be(payload);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnTooLarge_WhenContentLengthIsOverLimit()
    {
        var payload = new string('x', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = payload.Length;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.PayloadTooLarge.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnTooLarge_WhenStreamExceedsLimitEvenWithoutContentLength()
    {
        var payload = new string('y', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = null;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.PayloadTooLarge.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_Succeed_WhenPayloadAtExactLimit()
    {
        var payload = new string('z', ProviderWebhookPayloadReader.MaxPayloadBytes);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = payload.Length;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().HaveLength(ProviderWebhookPayloadReader.MaxPayloadBytes);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_Succeed_WhenPayloadAtExactLimit_AndContentLengthIsNull()
    {
        var payload = new string('q', ProviderWebhookPayloadReader.MaxPayloadBytes);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = null;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().HaveLength(ProviderWebhookPayloadReader.MaxPayloadBytes);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_SupportMultibytePayloadAtLimitByByteLength()
    {
        var payload = new string('é', ProviderWebhookPayloadReader.MaxPayloadBytes / 2);
        var bytes = Encoding.UTF8.GetBytes(payload);
        bytes.Length.Should().Be(ProviderWebhookPayloadReader.MaxPayloadBytes);

        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().Be(payload);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnTooLarge_WhenContentLengthUnderstatesActualBody_AndBodyExceedsLimit()
    {
        var payload = new string('y', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = ProviderWebhookPayloadReader.MaxPayloadBytes / 4;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.PayloadTooLarge.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnTooLarge_WhenContentLengthUnderstatesActualBody_AndOverstatesToLimit()
    {
        var payload = new string('y', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = ProviderWebhookPayloadReader.MaxPayloadBytes;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.PayloadTooLarge.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_Succeed_WhenContentLengthOverstatesButPayloadFitsLimit()
    {
        var payload = """{"ok":true}""";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = ProviderWebhookPayloadReader.MaxPayloadBytes;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().Be(payload);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnTooLarge_WhenContentLengthIsNull_AndPayloadExceedsLimit()
    {
        var payload = new string('y', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = null;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.PayloadTooLarge.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnTooLarge_WhenPayloadExceedsLimit_DueToUtf8ByteLength()
    {
        var payload = new string('é', (ProviderWebhookPayloadReader.MaxPayloadBytes / 2) + 1);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = null;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.PayloadTooLarge.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowOperationCanceled_WhenCancellationTokenIsCancelled()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"event":"cancelled"}"""));
        context.Request.ContentLength = context.Request.Body.Length;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => ProviderWebhookPayloadReader.ReadAsync(context.Request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadAsync_Should_Succeed_WhenContentLengthIsNull_AndPayloadFitsStream()
    {
        var payload = "{}";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = null;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().Be(payload);
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnTooLarge_WhenContentLengthUnderstatesPayload()
    {
        var payload = new string('y', ProviderWebhookPayloadReader.MaxPayloadBytes + 1);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = 1;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.PayloadTooLarge.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
    }

    [Fact]
    public async Task ReadAsync_Should_ReadPayload_WhenContentLengthIsZeroButBodyHasContent()
    {
        var payload = """{"x":1}""";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = 0;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().Be(payload);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_HandleEmptyPayload()
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Array.Empty<byte>());
        context.Request.ContentLength = 0;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Payload.Should().Be(string.Empty);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task ReadAsync_Should_ReuseSameBodyAfterReading()
    {
        const string payload = "{\"ping\":\"pong\"}";
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        context.Request.ContentLength = payload.Length;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);
        var secondRead = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Payload.Should().Be(payload);
        secondRead.Succeeded.Should().BeTrue();
        secondRead.Payload.Should().Be(payload);
    }

    [Fact]
    public async Task ReadAsync_Should_SupportFourByteUtf8Characters_AtExactByteBoundary()
    {
        var payload = CreateFourByteUtf8Payload(ProviderWebhookPayloadReader.MaxPayloadBytes / 4);
        var bytes = Encoding.UTF8.GetBytes(payload);
        bytes.Length.Should().Be(ProviderWebhookPayloadReader.MaxPayloadBytes);

        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;

        var result = await ProviderWebhookPayloadReader.ReadAsync(context.Request, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.PayloadTooLarge.Should().BeFalse();
        result.Payload.Should().Be(payload);
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public void ReadAsync_Should_Throw_WhenRequestIsNull()
    {
        Func<Task> act = () => ProviderWebhookPayloadReader.ReadAsync(null!, TestContext.Current.CancellationToken);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static string CreateFourByteUtf8Payload(int emojiCount)
    {
        var payload = new StringBuilder(emojiCount * 4);
        var emoji = "\U0001F600";

        for (var i = 0; i < emojiCount; i++)
        {
            payload.Append(emoji);
        }

        return payload.ToString();
    }
}
