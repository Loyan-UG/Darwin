using Darwin.Application.Sales.DTOs;
using Darwin.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.ViewModels.Sales
{
    public sealed class SalesOverviewVm
    {
        public int OrderCount { get; set; }
        public int QuoteCount { get; set; }
        public int QuoteAttentionCount { get; set; }
        public int ReturnOrderCount { get; set; }
        public int ReturnOrderAttentionCount { get; set; }
        public long GrossTotalMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public int InvoiceCount { get; set; }
        public long OpenInvoiceBalanceMinor { get; set; }
        public int PaymentAttentionCount { get; set; }
        public int FulfillmentAttentionCount { get; set; }
        public int InvoiceAttentionCount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public List<SalesChannelBreakdownVm> ChannelBreakdown { get; set; } = new();
    }

    public sealed class SalesChannelBreakdownVm
    {
        public SalesChannel SalesChannel { get; set; }
        public int OrderCount { get; set; }
        public long GrossTotalMinor { get; set; }
    }

    public sealed class SalesOrdersListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public SalesOrderDocumentFilter Filter { get; set; }
        public SalesChannel? SalesChannel { get; set; }
        public DateTime? OrderedFromUtc { get; set; }
        public DateTime? OrderedToUtc { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> SalesChannelItems { get; set; } = new List<SelectListItem>();
        public List<SalesOrderListItemVm> Items { get; set; } = new();
    }

    public sealed class SalesOrderListItemVm
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public SalesChannel SalesChannel { get; set; }
        public DateTime OrderedAtUtc { get; set; }
        public OrderStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long GrandTotalGrossMinor { get; set; }
        public long TaxTotalMinor { get; set; }
        public int LineCount { get; set; }
        public int PaymentCount { get; set; }
        public int FailedPaymentCount { get; set; }
        public int ShipmentCount { get; set; }
        public int InvoiceCount { get; set; }
    }

    public sealed class SalesInvoicesListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public SalesInvoiceDocumentFilter Filter { get; set; }
        public DateTime? DateFromUtc { get; set; }
        public DateTime? DateToUtc { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<SalesInvoiceListItemVm> Items { get; set; } = new();
    }

    public sealed class SalesInvoiceListItemVm
    {
        public Guid Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? PaymentId { get; set; }
        public string? OrderNumber { get; set; }
        public InvoiceStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public long TotalNetMinor { get; set; }
        public long TotalTaxMinor { get; set; }
        public long TotalGrossMinor { get; set; }
        public long BalanceMinor { get; set; }
        public DateTime DueDateUtc { get; set; }
        public DateTime? IssuedAtUtc { get; set; }
        public bool HasIssuedSnapshot { get; set; }
        public bool HasArchiveMetadata { get; set; }
    }

    public sealed class SalesOrderDetailVm
    {
        public SalesOrderDocumentDto Document { get; set; } = new();
    }

    public sealed class SalesInvoiceDetailVm
    {
        public SalesInvoiceDocumentDto Document { get; set; } = new();
    }

    public sealed class SalesQuotesListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public SalesQuoteDocumentFilter Filter { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? OpportunityId { get; set; }
        public DateTime? ValidUntilFromUtc { get; set; }
        public DateTime? ValidUntilToUtc { get; set; }
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public List<SalesQuoteListItemDto> Items { get; set; } = new();
    }

    public sealed class SalesQuoteEditorVm
    {
        public SalesQuoteEditDto Quote { get; set; } = new();
        public bool IsCreate { get; set; }
    }

    public sealed class SalesQuoteDetailVm
    {
        public SalesQuoteDetailDto Document { get; set; } = new();
        public SalesQuoteConvertDto Convert { get; set; } = new();
        public SalesQuoteCreateOrderDto CreateOrder { get; set; } = new();
    }

    public sealed class DeliveryNotesListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public DeliveryNoteDocumentFilter Filter { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? IssuedFromUtc { get; set; }
        public DateTime? IssuedToUtc { get; set; }
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public DeliveryNoteCreateFromShipmentDto Create { get; set; } = new();
        public List<DeliveryNoteListItemDto> Items { get; set; } = new();
    }

    public sealed class DeliveryNoteDetailVm
    {
        public DeliveryNoteDetailDto Document { get; set; } = new();
        public DeliveryNoteLifecycleDto Lifecycle { get; set; } = new();
    }

    public sealed class ReturnOrdersListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public ReturnOrderDocumentFilter Filter { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? CreatedFromUtc { get; set; }
        public DateTime? CreatedToUtc { get; set; }
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public ReturnOrderCreateDto Create { get; set; } = new();
        public List<ReturnOrderListItemDto> Items { get; set; } = new();
    }

    public sealed class ReturnOrderDetailVm
    {
        public ReturnOrderDetailDto Document { get; set; } = new();
        public ReturnOrderApproveDto Approve { get; set; } = new();
        public ReturnOrderQueueShipmentDto QueueShipment { get; set; } = new();
        public ReturnOrderReceiveDto Receive { get; set; } = new();
        public ReturnOrderInspectDto Inspect { get; set; } = new();
        public ReturnOrderLifecycleDto Lifecycle { get; set; } = new();
        public ReturnOrderLinkRefundDto LinkRefund { get; set; } = new();
    }

    public sealed class CreditNotesListVm
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public string Query { get; set; } = string.Empty;
        public CreditNoteDocumentFilter Filter { get; set; }
        public Guid? BusinessId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? IssuedFromUtc { get; set; }
        public DateTime? IssuedToUtc { get; set; }
        public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();
        public CreditNoteCreateDto Create { get; set; } = new();
        public List<CreditNoteListItemDto> Items { get; set; } = new();
    }

    public sealed class CreditNoteDetailVm
    {
        public CreditNoteDetailDto Document { get; set; } = new();
        public CreditNoteLifecycleDto Lifecycle { get; set; } = new();
    }
}
