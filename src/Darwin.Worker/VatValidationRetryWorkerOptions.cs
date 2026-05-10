namespace Darwin.Worker;

public sealed class VatValidationRetryWorkerOptions
{
    public bool Enabled { get; set; }
    public int PollIntervalMinutes { get; set; } = 240;
    public int BatchSize { get; set; } = 50;
    public int MinRetryAgeMinutes { get; set; } = 240;
}
