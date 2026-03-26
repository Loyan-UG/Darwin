using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Commerce
{
    /// <summary>
    /// Provides member-facing commerce operations for order and invoice history in mobile clients.
    /// </summary>
    public interface IMemberCommerceService
    {
        /// <summary>
        /// Loads the current member's paged order history.
        /// </summary>
        Task<Result<PagedResponse<MemberOrderSummary>>> GetMyOrdersAsync(int page, int pageSize, CancellationToken ct);

        /// <summary>
        /// Loads the detail of a single member-owned order.
        /// </summary>
        Task<Result<MemberOrderDetail>> GetOrderAsync(Guid orderId, CancellationToken ct);

        /// <summary>
        /// Creates or reuses a storefront payment intent for a member-owned order.
        /// </summary>
        Task<Result<CreateStorefrontPaymentIntentResponse>> CreateOrderPaymentIntentAsync(
            Guid orderId,
            CreateStorefrontPaymentIntentRequest request,
            CancellationToken ct);

        /// <summary>
        /// Downloads a member-friendly plain-text order document.
        /// </summary>
        Task<Result<string>> DownloadOrderDocumentAsync(Guid orderId, CancellationToken ct);

        /// <summary>
        /// Loads the current member's paged invoice history.
        /// </summary>
        Task<Result<PagedResponse<MemberInvoiceSummary>>> GetMyInvoicesAsync(int page, int pageSize, CancellationToken ct);

        /// <summary>
        /// Loads the detail of a single member-owned invoice.
        /// </summary>
        Task<Result<MemberInvoiceDetail>> GetInvoiceAsync(Guid invoiceId, CancellationToken ct);

        /// <summary>
        /// Creates or reuses a storefront payment intent for a member-owned invoice.
        /// </summary>
        Task<Result<CreateStorefrontPaymentIntentResponse>> CreateInvoicePaymentIntentAsync(
            Guid invoiceId,
            CreateStorefrontPaymentIntentRequest request,
            CancellationToken ct);

        /// <summary>
        /// Downloads a member-friendly plain-text invoice document.
        /// </summary>
        Task<Result<string>> DownloadInvoiceDocumentAsync(Guid invoiceId, CancellationToken ct);
    }
}
