using Darwin.Domain.Enums;

namespace Darwin.Application.HumanResources.DTOs;

public enum PayrollRuleSetQueueFilter
{
    All = 0,
    Draft = 1,
    Active = 2,
    Archived = 3
}

public class PayrollRuleSetEditDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public string JurisdictionCode { get; set; } = "DE";
    public string RuleSetCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string RuleVersion { get; set; } = string.Empty;
    public PayrollRuleSetStatus Status { get; set; } = PayrollRuleSetStatus.Draft;
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class PayrollRuleSetListItemDto : PayrollRuleSetEditDto
{
    public int ComponentCount { get; set; }
    public List<PayrollRuleComponentDto> Components { get; set; } = new();
}

public class PayrollRuleComponentDto
{
    public Guid Id { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public Guid BusinessId { get; set; }
    public Guid PayrollRuleSetId { get; set; }
    public string ComponentCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public PayrollRuleComponentType ComponentType { get; set; } = PayrollRuleComponentType.GrossPay;
    public PayrollRuleCalculationMethod CalculationMethod { get; set; } = PayrollRuleCalculationMethod.Percentage;
    public PayrollRuleBasis Basis { get; set; } = PayrollRuleBasis.GrossPay;
    public int? RateBasisPoints { get; set; }
    public long? AmountMinor { get; set; }
    public string? ThresholdJson { get; set; }
    public bool IsEmployerCost { get; set; }
    public int SortOrder { get; set; }
    public string? MetadataJson { get; set; }
}
