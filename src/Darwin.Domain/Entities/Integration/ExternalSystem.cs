using Darwin.Domain.Common;
using Darwin.Domain.Enums;

namespace Darwin.Domain.Entities.Integration;

public sealed class ExternalSystem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ExternalSystemKind Kind { get; set; } = ExternalSystemKind.Unknown;
    public string? BaseUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string MetadataJson { get; set; } = "{}";

    public ICollection<ExternalReference> References { get; set; } = new List<ExternalReference>();
}
