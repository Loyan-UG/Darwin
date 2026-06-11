using Darwin.Domain.Common;

namespace Darwin.Domain.Entities.Foundation;

public sealed class BusinessFeatureOverride : BaseEntity
{
    public Guid BusinessId { get; set; }
    public Guid FeatureAreaId { get; set; }
    public bool IsEnabled { get; set; }
    public string? Reason { get; set; }
    public DateTime? EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
