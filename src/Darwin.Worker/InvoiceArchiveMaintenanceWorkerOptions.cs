namespace Darwin.Worker;

public sealed class InvoiceArchiveMaintenanceWorkerOptions
{
    public bool Enabled { get; set; }
    public int PollIntervalMinutes { get; set; } = 1440;
    public int BatchSize { get; set; } = 100;
}
