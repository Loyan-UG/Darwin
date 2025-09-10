namespace Darwin.Web.Services.Settings
{
    /// <summary>
    ///     <see cref="ISiteSettingCache"/> implementation backed by <c>IMemoryCache</c>.
    ///     Loads the single <c>SiteSetting</c> row from the database and caches a lightweight snapshot for fast reads.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Behavior:
    ///         <list type="bullet">
    ///             <item>Cache key is constant; cache entry stores a DTO/snapshot immutable by consumers.</item>
    ///             <item>On update, callers must invoke <c>InvalidateAsync</c> to force reload on the next read.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Threading:
    ///         Uses default <c>IMemoryCache</c> behavior; avoid heavy computations inside the factory.
    ///     </para>
    ///     <para>
    ///         Considerations:
    ///         Provide reasonable absolute/sliding expirations; but rely primarily on explicit invalidation after Admin saves.
    ///     </para>
    /// </remarks>
    public class SiteSettingCache
    {
    }
}
