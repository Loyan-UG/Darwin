using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Persistence
{
    /// <summary>
    /// Thin abstraction over EF Core DbContext to keep Application layer free of EF references in handlers.
    /// Provides access to DbSet and SaveChanges. Specialized repositories can be added later if needed.
    /// </summary>
    public interface IAppDbContext
    {
        DbSet<T> Set<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
