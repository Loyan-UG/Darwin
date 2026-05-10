namespace Darwin.Application.CRM.Services;

/// <summary>
/// Configures the file-system invoice archive storage provider.
/// </summary>
public sealed class FileSystemInvoiceArchiveStorageOptions
{
    /// <summary>
    /// Gets or sets the root directory used for archive artifact files.
    /// </summary>
    public string? RootPath { get; set; }
}
