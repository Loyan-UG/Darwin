using System.Collections.Generic;
using Darwin.Application.CMS.DTOs;

namespace Darwin.WebAdmin.ViewModels.CMS
{
    public sealed class PagesIndexVm
    {
        public IReadOnlyList<PageListItemDto> Items { get; set; } = new List<PageListItemDto>();
        public PageOpsSummaryVm Summary { get; set; } = new();
        public IReadOnlyList<PagePlaybookVm> Playbooks { get; set; } = new List<PagePlaybookVm>();
        public string Query { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }

    public sealed class PageOpsSummaryVm
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int PublishedCount { get; set; }
        public int WindowedCount { get; set; }
        public int LiveWindowCount { get; set; }
    }

    public sealed class PagePlaybookVm
    {
        public string QueueLabel { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string OperatorAction { get; set; } = string.Empty;
    }
}
