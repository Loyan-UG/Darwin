using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Common;

namespace Darwin.Mobile.Shared.Api;

/// <summary>
/// Default API client. Serializes JSON using System.Text.Json.
/// </summary>
public sealed class ApiClient : IApiClient
{
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    public HttpClient Http { get; }
    public ApiClient(HttpClient http, ApiOptions options)
    {
        Http = http;
        Http.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    }

    public void SetBearer(string? token)
    {
        Http.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(token) ? null : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(path, ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(s, _json, ct);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body, _json);
        using var resp = await Http.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"), ct);
        resp.EnsureSuccessStatusCode();
        await using var s = await resp.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<TResponse>(s, _json, ct);
    }
}
