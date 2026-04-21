namespace Darwin.Worker;

public sealed class EmailDispatchOperationWorkerOptions
{
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 20;
    public int RetryCooldownSeconds { get; set; } = 30;
    public int MaxAttempts { get; set; } = 10;
}
