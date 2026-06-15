using Darwin.Domain.Enums;

namespace Darwin.Application.CRM.DTOs
{
    public enum OpportunityQueueFilter
    {
        All = 0,
        Open = 1,
        ClosingSoon = 2,
        HighValue = 3
    }

    public enum InvoiceQueueFilter
    {
        All = 0,
        Draft = 1,
        DueSoon = 2,
        Overdue = 3,
        MissingVatId = 4,
        Refunded = 5
    }

    public sealed class OpportunityItemDto
    {
        public Guid? Id { get; set; }
        public Guid ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public long UnitPriceMinor { get; set; }
    }

    public sealed class OpportunityListItemDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public long EstimatedValueMinor { get; set; }
        public string Currency { get; set; } = Darwin.Domain.Common.DomainDefaults.DefaultCurrency;
        public OpportunityStage Stage { get; set; }
        public int? ProbabilityPercent { get; set; }
        public OpportunityForecastCategory ForecastCategory { get; set; }
        public DateTime? ExpectedCloseDateUtc { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? AssignedToUserDisplayName { get; set; }
        public DateTime? ClosedAtUtc { get; set; }
        public string? CloseReason { get; set; }
        public string? Source { get; set; }
        public int ItemCount { get; set; }
        public int InteractionCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ModifiedAtUtc { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    public class OpportunityCreateDto
    {
        public Guid CustomerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public long EstimatedValueMinor { get; set; }
        public string Currency { get; set; } = Darwin.Domain.Common.DomainDefaults.DefaultCurrency;
        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;
        public int? ProbabilityPercent { get; set; }
        public OpportunityForecastCategory ForecastCategory { get; set; } = OpportunityForecastCategory.Pipeline;
        public DateTime? ExpectedCloseDateUtc { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTime? ClosedAtUtc { get; set; }
        public string? CloseReason { get; set; }
        public string? Source { get; set; }
        public List<OpportunityItemDto> Items { get; set; } = new();
    }

    public sealed class OpportunityEditDto : OpportunityCreateDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string CustomerDisplayName { get; set; } = string.Empty;
        public string? AssignedToUserDisplayName { get; set; }
        public int InteractionCount { get; set; }
    }

    public sealed class UpdateOpportunityLifecycleDto
    {
        public Guid Id { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public string Action { get; set; } = string.Empty;
        public string? CloseReason { get; set; }
    }
}
