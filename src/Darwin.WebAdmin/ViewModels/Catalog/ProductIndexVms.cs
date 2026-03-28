using System.Collections.Generic;
using Darwin.Application.Catalog.DTOs;

namespace Darwin.WebAdmin.ViewModels.Catalog
{
    public sealed class ProductsIndexVm
    {
        public IReadOnlyList<ProductListItemDto> Items { get; set; } = new List<ProductListItemDto>();
        public ProductOpsSummaryVm Summary { get; set; } = new();
        public IReadOnlyList<OperationalPlaybookVm> Playbooks { get; set; } = new List<OperationalPlaybookVm>();
        public string Query { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }

    public sealed class ProductOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int InactiveCount { get; set; }
        public int HiddenCount { get; set; }
        public int SingleVariantCount { get; set; }
        public int ScheduledCount { get; set; }
    }

    public sealed class OperationalPlaybookVm
    {
        public string QueueLabel { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
    }
}
