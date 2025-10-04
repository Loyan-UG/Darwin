using System.Threading;
using System.Threading.Tasks;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Domain.Entities.SEO;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds a couple of redirect rules for testing SEO tooling.
    /// </summary>
    public sealed class SeoSeedSection
    {
        /// <summary>
        /// Adds sample redirects if none exist.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            if (await db.Set<RedirectRule>().AnyAsync(ct)) return;

            db.AddRange(
                new RedirectRule
                {
                    FromPath = "/old-home",
                    To = "/home",
                    IsPermanent = true
                },
                new RedirectRule
                {
                    FromPath = "/old-product",
                    To = "/p/new-product",
                    IsPermanent = false
                }
            );

            await db.SaveChangesAsync(ct);
        }
    }
}
