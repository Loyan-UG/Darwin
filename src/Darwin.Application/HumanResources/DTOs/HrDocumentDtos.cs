using Darwin.Domain.Enums;

namespace Darwin.Application.HumanResources.DTOs;

public sealed class PersonnelDocumentUploadDto
{
    public Guid BusinessId { get; set; }
    public Guid EmployeeId { get; set; }
    public DocumentRecordKind DocumentKind { get; set; } = DocumentRecordKind.StaffDocument;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Stream Content { get; set; } = Stream.Null;
    public DateTime? RetentionUntilUtc { get; set; }
    public bool LegalHold { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Restricted;
    public string? MetadataJson { get; set; }
}

public sealed class PersonnelDocumentListItemDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid EmployeeId { get; set; }
    public DocumentRecordKind DocumentKind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public string? ContentHash { get; set; }
    public HrPrivacyClassification PrivacyClassification { get; set; } = HrPrivacyClassification.Restricted;
    public DateTime? RetentionUntilUtc { get; set; }
    public bool LegalHold { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed record PersonnelDocumentDownloadResult(Stream Content, string ContentType, string FileName, long? ContentLength, string? Sha256Hash);
