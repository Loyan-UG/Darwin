namespace Darwin.Mobile.Shared.Api;

/// <summary>
/// Raw file payload returned by API endpoints that intentionally do not return JSON.
/// </summary>
public sealed class ApiFileResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string FileName { get; set; } = string.Empty;
    public long? ContentLength { get; set; }
}
