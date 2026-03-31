using SQLite;
using System;

namespace Darwin.Mobile.Shared.Storage.Sqlite;

/// <summary>
/// Internal row used for lightweight persisted key-value state.
/// </summary>
[Table("app_key_values")]
internal sealed class KeyValueRecord
{
    [PrimaryKey]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [NotNull]
    public string Value { get; set; } = string.Empty;

    [Indexed]
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// Internal row used for persisted outbox messages.
/// </summary>
[Table("outbox_messages")]
internal sealed class OutboxMessageRecord
{
    [PrimaryKey]
    [MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    [NotNull]
    public string Path { get; set; } = string.Empty;

    [NotNull]
    [MaxLength(16)]
    public string Method { get; set; } = "POST";

    [NotNull]
    public string JsonBody { get; set; } = "{}";

    [Indexed]
    public DateTime EnqueuedAtUtc { get; set; }

    public int Attempts { get; set; }

    public bool IsSucceeded { get; set; }

    public string? LastError { get; set; }

    [Indexed]
    public DateTime? LastAttemptedAtUtc { get; set; }
}
