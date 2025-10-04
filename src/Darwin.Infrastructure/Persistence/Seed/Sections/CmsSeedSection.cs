using System.Threading;
using System.Threading.Tasks;
using Darwin.Infrastructure.Persistence.Db;
using Darwin.Domain.Entities.CMS;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Seed.Sections
{
    /// <summary>
    /// Seeds a small set of media assets to make the admin UI less empty.
    /// </summary>
    public sealed class CmsSeedSection
    {
        /// <summary>
        /// Adds a few sample images if no assets exist.
        /// </summary>
        public async Task SeedAsync(DarwinDbContext db, CancellationToken ct = default)
        {
            if (await db.Set<MediaAsset>().AnyAsync(ct)) return;

            db.AddRange(
                new MediaAsset
                {
                    Url = "/media/hero-1.jpg",
                    Alt = "Hero banner 1",
                    Title = "Summer Campaign",
                    OriginalFileName = "hero-1.jpg",
                    SizeBytes = 356_000,
                    ContentHash = "sha256:demo1",
                    Width = 1920,
                    Height = 800
                },
                new MediaAsset
                {
                    Url = "/media/product-1.jpg",
                    Alt = "Product sample",
                    Title = "Demo Product",
                    OriginalFileName = "product-1.jpg",
                    SizeBytes = 210_000,
                    ContentHash = "sha256:demo2",
                    Width = 1200,
                    Height = 1200
                }
            );

            await db.SaveChangesAsync(ct);
        }
    }
}
