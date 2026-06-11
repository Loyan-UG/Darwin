using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Foundation;

public sealed class CustomFieldValue : BaseEntity
{
    public Guid DefinitionId { get; set; }
    public CustomFieldDefinition? Definition { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? StringValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public DateTime? DateValue { get; set; }
    public string? JsonValue { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
