using System;

namespace Darwin.Mobile.Shared.Storage.Outbox;

/// <summary>
/// Outbox entry to queue mutations when offline (idempotent on server).
/// </summary>
public sealed class OutboxMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Path { get; set; } = default!;
    public string Method { get; set; } = "POST";
    public string JsonBody { get; set; } = "{}";
    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;
    public int Attempts { get; set; }
}
