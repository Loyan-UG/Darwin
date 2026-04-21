namespace Darwin.Worker;

public sealed class WebhookDeliveryWorkerOptions
{
    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 30;

    public int BatchSize { get; set; } = 10;

    public int RequestTimeoutSeconds { get; set; } = 15;

    public int RetryCooldownSeconds { get; set; } = 60;

    public int MaxAttempts { get; set; } = 5;
}
