using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Mobile.Shared.Api;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Commerce
{
    /// <summary>
    /// Member-facing commerce service that encapsulates order and invoice history calls for mobile clients.
    /// </summary>
    public sealed class MemberCommerceService : IMemberCommerceService
    {
        private readonly IApiClient _apiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberCommerceService"/> class.
        /// </summary>
        public MemberCommerceService(IApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <inheritdoc />
        public async Task<Result<PagedResponse<MemberOrderSummary>>> GetMyOrdersAsync(int page, int pageSize, CancellationToken ct)
        {
            if (page <= 0)
            {
                return Result<PagedResponse<MemberOrderSummary>>.Fail("Page must be a positive integer.");
            }

            if (pageSize <= 0 || pageSize > 200)
            {
                return Result<PagedResponse<MemberOrderSummary>>.Fail("PageSize must be between 1 and 200.");
            }

            var route = $"{ApiRoutes.Orders.GetMyOrders}?page={page}&pageSize={pageSize}";
            return await ExecuteGetAsync<PagedResponse<MemberOrderSummary>>(route, "order history", ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<MemberOrderDetail>> GetOrderAsync(Guid orderId, CancellationToken ct)
        {
            if (orderId == Guid.Empty)
            {
                return Result<MemberOrderDetail>.Fail("OrderId is required.");
            }

            return await ExecuteGetAsync<MemberOrderDetail>(ApiRoutes.Orders.GetById(orderId), "order detail", ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<CreateStorefrontPaymentIntentResponse>> CreateOrderPaymentIntentAsync(
            Guid orderId,
            CreateStorefrontPaymentIntentRequest request,
            CancellationToken ct)
        {
            if (orderId == Guid.Empty)
            {
                return Result<CreateStorefrontPaymentIntentResponse>.Fail("OrderId is required.");
            }

            if (request is null)
            {
                return Result<CreateStorefrontPaymentIntentResponse>.Fail("Request body is required.");
            }

            return await ExecutePostAsync<CreateStorefrontPaymentIntentRequest, CreateStorefrontPaymentIntentResponse>(
                ApiRoutes.Orders.CreatePaymentIntent(orderId),
                request,
                "order payment intent",
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<string>> DownloadOrderDocumentAsync(Guid orderId, CancellationToken ct)
        {
            if (orderId == Guid.Empty)
            {
                return Result<string>.Fail("OrderId is required.");
            }

            return await ExecuteGetStringAsync(ApiRoutes.Orders.DownloadDocument(orderId), "order document", ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<PagedResponse<MemberInvoiceSummary>>> GetMyInvoicesAsync(int page, int pageSize, CancellationToken ct)
        {
            if (page <= 0)
            {
                return Result<PagedResponse<MemberInvoiceSummary>>.Fail("Page must be a positive integer.");
            }

            if (pageSize <= 0 || pageSize > 200)
            {
                return Result<PagedResponse<MemberInvoiceSummary>>.Fail("PageSize must be between 1 and 200.");
            }

            var route = $"{ApiRoutes.Invoices.GetMyInvoices}?page={page}&pageSize={pageSize}";
            return await ExecuteGetAsync<PagedResponse<MemberInvoiceSummary>>(route, "invoice history", ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<MemberInvoiceDetail>> GetInvoiceAsync(Guid invoiceId, CancellationToken ct)
        {
            if (invoiceId == Guid.Empty)
            {
                return Result<MemberInvoiceDetail>.Fail("InvoiceId is required.");
            }

            return await ExecuteGetAsync<MemberInvoiceDetail>(ApiRoutes.Invoices.GetById(invoiceId), "invoice detail", ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<CreateStorefrontPaymentIntentResponse>> CreateInvoicePaymentIntentAsync(
            Guid invoiceId,
            CreateStorefrontPaymentIntentRequest request,
            CancellationToken ct)
        {
            if (invoiceId == Guid.Empty)
            {
                return Result<CreateStorefrontPaymentIntentResponse>.Fail("InvoiceId is required.");
            }

            if (request is null)
            {
                return Result<CreateStorefrontPaymentIntentResponse>.Fail("Request body is required.");
            }

            return await ExecutePostAsync<CreateStorefrontPaymentIntentRequest, CreateStorefrontPaymentIntentResponse>(
                ApiRoutes.Invoices.CreatePaymentIntent(invoiceId),
                request,
                "invoice payment intent",
                ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Result<string>> DownloadInvoiceDocumentAsync(Guid invoiceId, CancellationToken ct)
        {
            if (invoiceId == Guid.Empty)
            {
                return Result<string>.Fail("InvoiceId is required.");
            }

            return await ExecuteGetStringAsync(ApiRoutes.Invoices.DownloadDocument(invoiceId), "invoice document", ct).ConfigureAwait(false);
        }

        private async Task<Result<TResponse>> ExecuteGetAsync<TResponse>(string route, string operation, CancellationToken ct)
        {
            try
            {
                var response = await _apiClient.GetResultAsync<TResponse>(route, ct).ConfigureAwait(false);
                if (!response.Succeeded || response.Value is null)
                {
                    return Result<TResponse>.Fail(response.Error ?? $"Request failed while retrieving {operation}.");
                }

                return Result<TResponse>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Fail($"Network error while retrieving {operation}: {ex.Message}");
            }
        }

        private async Task<Result<TResponse>> ExecutePostAsync<TRequest, TResponse>(
            string route,
            TRequest request,
            string operation,
            CancellationToken ct)
        {
            try
            {
                var response = await _apiClient.PostResultAsync<TRequest, TResponse>(route, request, ct).ConfigureAwait(false);
                if (!response.Succeeded || response.Value is null)
                {
                    return Result<TResponse>.Fail(response.Error ?? $"Request failed while creating {operation}.");
                }

                return Result<TResponse>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Fail($"Network error while creating {operation}: {ex.Message}");
            }
        }

        private async Task<Result<string>> ExecuteGetStringAsync(string route, string operation, CancellationToken ct)
        {
            try
            {
                var response = await _apiClient.GetStringResultAsync(route, ct).ConfigureAwait(false);
                if (!response.Succeeded || string.IsNullOrEmpty(response.Value))
                {
                    return Result<string>.Fail(response.Error ?? $"Request failed while retrieving {operation}.");
                }

                return Result<string>.Ok(response.Value);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<string>.Fail($"Network error while retrieving {operation}: {ex.Message}");
            }
        }
    }
}
