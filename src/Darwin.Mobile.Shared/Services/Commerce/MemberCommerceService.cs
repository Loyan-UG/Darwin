using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Mobile.Shared.Api;
using Darwin.Mobile.Shared.Caching;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Security;
using Darwin.Shared.Results;

namespace Darwin.Mobile.Shared.Services.Commerce
{
    /// <summary>
    /// Member-facing commerce service that encapsulates order and invoice history calls for mobile clients.
    /// </summary>
    public sealed class MemberCommerceService : IMemberCommerceService
    {
        private static readonly TimeSpan HistoryCacheTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan HistoryFallbackMaxAge = TimeSpan.FromMinutes(5);

        private readonly IApiClient _apiClient;
        private readonly IMobileCacheService _cache;
        private readonly ITokenStore _tokenStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberCommerceService"/> class.
        /// </summary>
        public MemberCommerceService(IApiClient apiClient, IMobileCacheService cache, ITokenStore tokenStore)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
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
            return await ExecuteCachedGetAsync<PagedResponse<MemberOrderSummary>>(
                route,
                await GetScopedCacheKeyAsync($"commerce.orders:{page}:{pageSize}").ConfigureAwait(false),
                "order history",
                ct).ConfigureAwait(false);
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
            return await ExecuteCachedGetAsync<PagedResponse<MemberInvoiceSummary>>(
                route,
                await GetScopedCacheKeyAsync($"commerce.invoices:{page}:{pageSize}").ConfigureAwait(false),
                "invoice history",
                ct).ConfigureAwait(false);
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
            catch (Exception)
            {
                return Result<TResponse>.Fail(MobileErrorMessages.NetworkFailure($"retrieving {operation}"));
            }
        }

        private async Task<Result<TResponse>> ExecuteCachedGetAsync<TResponse>(
            string route,
            string cacheKey,
            string operation,
            CancellationToken ct)
        {
            var cached = await _cache.GetFreshAsync<TResponse>(cacheKey, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return Result<TResponse>.Ok(cached);
            }

            var response = await ExecuteGetAsync<TResponse>(route, operation, ct).ConfigureAwait(false);
            if (response.Succeeded && response.Value is not null)
            {
                await _cache.SetAsync(cacheKey, response.Value, HistoryCacheTtl, ct).ConfigureAwait(false);
                return response;
            }

            var fallback = await _cache.GetUsableAsync<TResponse>(cacheKey, HistoryFallbackMaxAge, ct).ConfigureAwait(false);
            return fallback is not null
                ? Result<TResponse>.Ok(fallback)
                : response;
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
            catch (Exception)
            {
                return Result<TResponse>.Fail(MobileErrorMessages.NetworkFailure($"creating {operation}"));
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
            catch (Exception)
            {
                return Result<string>.Fail(MobileErrorMessages.NetworkFailure($"retrieving {operation}"));
            }
        }

        private async Task<string> GetScopedCacheKeyAsync(string suffix)
        {
            var (accessToken, _) = await _tokenStore.GetAccessAsync().ConfigureAwait(false);
            var subject = JwtClaimReader.GetSubject(accessToken);
            return string.IsNullOrWhiteSpace(subject)
                ? $"{suffix}:{BuildFallbackScope(accessToken)}"
                : $"{suffix}:{subject}";
        }

        /// <summary>
        /// Builds a non-readable cache scope when the JWT subject cannot be parsed.
        /// This prevents member commerce history from falling back to a shared unscoped key.
        /// </summary>
        private static string BuildFallbackScope(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return "anonymous";
            }

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(accessToken.Trim()));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
