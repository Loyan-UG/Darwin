using Darwin.Domain.Common;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class DocumentRecord : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DocumentRecordKind DocumentKind { get; set; } = DocumentRecordKind.General;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public string? ContentHash { get; set; }
    public string StorageProvider { get; set; } = string.Empty;
    public string StorageContainer { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public Guid? MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }
    public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;
    public string MetadataJson { get; set; } = "{}";
}
