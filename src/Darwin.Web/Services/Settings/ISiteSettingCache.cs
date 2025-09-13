using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Settings.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Services.Settings
{
    /// <summary>
    ///     Abstraction for retrieving and caching site-wide settings (culture, units, SEO flags, feature toggles),
    ///     providing fast access to frequently read configuration without repeated database queries.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Guarantees:
    ///         <list type="bullet">
    ///             <item>Thread-safe retrieval of the latest settings entry.</item>
    ///             <item>Explicit invalidation API to refresh cache after changes in Admin.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Consumers:
    ///         Admin controllers (dropdown options), UI helpers (formatting, culture), SEO services (canonical/hreflang).
    ///     </para>
    /// </remarks>
    public interface ISiteSettingCache
    {
        /// <summary>
        /// Gets the current site settings with caching semantics.
        /// </summary>
        Task<SiteSettingDto> GetAsync(CancellationToken ct = default);

        /// <summary>
        /// Invalidates the in-memory cache so that the next read hits the database.
        /// </summary>
        void Invalidate();
    }


}
