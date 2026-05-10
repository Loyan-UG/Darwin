namespace Darwin.Infrastructure.Compliance;

public sealed class ExternalCommandEInvoiceOptions
{
    public const string SectionName = "Compliance:EInvoice:ExternalCommand";

    public bool Enabled { get; set; }

    public string ExecutablePath { get; set; } = string.Empty;

    public string? WorkingDirectory { get; set; }

    public string? TempDirectory { get; set; }

    public int TimeoutSeconds { get; set; } = 60;

    public long MaxArtifactBytes { get; set; } = 20 * 1024 * 1024;

    public bool SupportsZugferdFacturX { get; set; } = true;

    public bool SupportsXRechnung { get; set; }

    public string ValidationProfile { get; set; } = "external-command";
}
