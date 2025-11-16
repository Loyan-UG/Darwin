using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Resilience;

namespace Darwin.Mobile.Shared.Api
{
    /// <summary>
    /// Default API client. Serializes JSON using System.Text.Json.
    /// Wrapped by a retry policy for transient failures.
    /// </summary>
    public sealed class ApiClient : IApiClient
    {
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        private readonly IRetryPolicy _retry;
        public HttpClient Http { get; }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public ApiClient(HttpClient http, ApiOptions options, IRetryPolicy retry)
        {
            Http = http;
            Http.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            _retry = retry;
        }

        public void SetBearer(string? token)
        {
            Http.DefaultRequestHeaders.Authorization =
                string.IsNullOrWhiteSpace(token)
                    ? null
                    : new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<T?> GetAsync<T>(string path, CancellationToken ct)
        {
            return await _retry.ExecuteAsync<T?>(async innerCt =>
            {
                using var resp = await Http.GetAsync(path, innerCt).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                await using var s = await resp.Content.ReadAsStreamAsync(innerCt).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<T>(s, _json, innerCt).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
        {
            return await _retry.ExecuteAsync<TResponse?>(async innerCt =>
            {
                var json = JsonSerializer.Serialize(body, _json);
                using var resp = await Http.PostAsync(
                    path,
                    new StringContent(json, Encoding.UTF8, "application/json"),
                    innerCt).ConfigureAwait(false);

                resp.EnsureSuccessStatusCode();
                await using var s = await resp.Content.ReadAsStreamAsync(innerCt).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<TResponse>(s, _json, innerCt).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
    }
}
