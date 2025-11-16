using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Abstractions
{
    public interface IKeyValueStore
    {
        Task SetAsync(string key, string value, CancellationToken ct);
        Task<string?> GetAsync(string key, CancellationToken ct);
        Task RemoveAsync(string key, CancellationToken ct);
    }
}
