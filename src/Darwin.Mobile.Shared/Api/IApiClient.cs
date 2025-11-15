using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Api;

/// <summary>
/// Minimal HTTP abstraction with bearer handling and JSON.
/// </summary>
public interface IApiClient
{
    HttpClient Http { get; }
    void SetBearer(string? token);
    Task<T?> GetAsync<T>(string path, CancellationToken ct);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct);
}
