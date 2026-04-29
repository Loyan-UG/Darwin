using System.Text;

namespace Darwin.WebApi.Services;

public static class ProviderWebhookPayloadReader
{
    public const int MaxPayloadBytes = 256 * 1024;

    public static async Task<ProviderWebhookPayloadReadResult> ReadAsync(HttpRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ContentLength.HasValue && request.ContentLength.Value > MaxPayloadBytes)
        {
            return ProviderWebhookPayloadReadResult.TooLarge();
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        using var memory = new MemoryStream(capacity: request.ContentLength is > 0 and <= MaxPayloadBytes
            ? (int)request.ContentLength.Value
            : 0);

        var buffer = new byte[8192];
        while (true)
        {
            var read = await request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (memory.Length + read > MaxPayloadBytes)
            {
                request.Body.Position = 0;
                return ProviderWebhookPayloadReadResult.TooLarge();
            }

            memory.Write(buffer, 0, read);
        }

        request.Body.Position = 0;
        return ProviderWebhookPayloadReadResult.Ok(Encoding.UTF8.GetString(memory.ToArray()));
    }
}

public readonly record struct ProviderWebhookPayloadReadResult(
    bool Succeeded,
    bool PayloadTooLarge,
    string Payload)
{
    public static ProviderWebhookPayloadReadResult Ok(string payload)
    {
        return new ProviderWebhookPayloadReadResult(true, false, payload);
    }

    public static ProviderWebhookPayloadReadResult TooLarge()
    {
        return new ProviderWebhookPayloadReadResult(false, true, string.Empty);
    }
}
