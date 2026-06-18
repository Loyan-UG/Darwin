using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Foundation;

public sealed class FeatureArea : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FeatureAreaCategory Category { get; set; } = FeatureAreaCategory.Custom;
    public FeatureAreaVisibilityScope VisibilityScope { get; set; } = FeatureAreaVisibilityScope.Internal;
    public bool DefaultEnabled { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? RequiredPermissionKey { get; set; }
    public string MetadataJson { get; set; } = "{}";
}
