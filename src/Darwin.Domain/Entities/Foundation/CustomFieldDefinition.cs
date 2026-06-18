using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class CustomFieldDefinition : BaseEntity
{
    public Guid? BusinessId { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public CustomFieldDataType DataType { get; set; } = CustomFieldDataType.Text;
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public FoundationVisibility Visibility { get; set; } = FoundationVisibility.Internal;
    public string ValidationJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";

    public ICollection<CustomFieldValue> Values { get; set; } = new List<CustomFieldValue>();
}
