using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Application.Abstractions.Persistence
{
    /// <summary>
    ///     Application-layer abstraction over the EF Core DbContext that exposes only the members
    ///     required by use-case handlers (queries/commands). This decouples Application from EF Core types where possible,
    ///     improves testability (mock/fake implementations), and prevents leaking infrastructure concerns into use cases.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Typical members include:
    ///         <list type="bullet">
    ///             <item><c>DbSet&lt;T&gt; Set&lt;T&gt;()</c> to access aggregates.</item>
    ///             <item><c>Task&lt;int&gt; SaveChangesAsync(...)</c> to persist mutations.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         In tests, provide an in-memory or stub implementation to simulate persistence without a real database.
    ///     </para>
    /// </remarks>
    public interface IAppDbContext
    {
        DbSet<T> Set<T>() where T : class;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
