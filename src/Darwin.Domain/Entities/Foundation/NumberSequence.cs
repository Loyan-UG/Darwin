using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class NumberSequence : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public NumberSequenceDocumentType DocumentType { get; set; } = NumberSequenceDocumentType.Custom;
    public string ScopeKey { get; set; } = string.Empty;
    public string PrefixPattern { get; set; } = string.Empty;
    public long NextValue { get; set; } = 1;
    public int PaddingLength { get; set; } = 5;
    public NumberSequenceResetPolicy ResetPolicy { get; set; } = NumberSequenceResetPolicy.Never;
    public string? CurrentPeriodKey { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
