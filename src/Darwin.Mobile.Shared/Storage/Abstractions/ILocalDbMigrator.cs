using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Storage.Abstractions
{
    public interface ILocalDbMigrator
    {
        Task EnsureCreatedAsync(CancellationToken ct);
        Task MigrateAsync(CancellationToken ct);
    }
}
