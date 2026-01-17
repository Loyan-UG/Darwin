using Darwin.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Infrastructure.Persistence.Db
{
    /// <summary>
    /// Businesses DbSets partial for <see cref="DarwinDbContext"/>.
    /// Keeps module-specific DbSet declarations isolated from the core context file.
    /// </summary>
    public sealed partial class DarwinDbContext
    {
        /// <summary>
        /// Partner businesses (merchant tenant roots).
        /// </summary>
        public DbSet<Business> Businesses => Set<Business>();

        /// <summary>
        /// Business branches / locations.
        /// </summary>
        public DbSet<BusinessLocation> BusinessLocations => Set<BusinessLocation>();

        /// <summary>
        /// Business members (join between Business and User with role).
        /// </summary>
        public DbSet<BusinessMember> BusinessMembers => Set<BusinessMember>();

        /// <summary>
        /// Business media items (logo, gallery, etc.).
        /// </summary>
        public DbSet<BusinessMedia> BusinessMedias => Set<BusinessMedia>();

        /// <summary>
        /// User favorites for businesses (soft-deletable).
        /// </summary>
        public DbSet<BusinessFavorite> BusinessFavorites => Set<BusinessFavorite>();

        /// <summary>
        /// User likes for businesses (soft-deletable).
        /// </summary>
        public DbSet<BusinessLike> BusinessLikes => Set<BusinessLike>();

        /// <summary>
        /// User reviews for businesses (soft-deletable + hide/unhide moderation).
        /// </summary>
        public DbSet<BusinessReview> BusinessReviews => Set<BusinessReview>();

        /// <summary>
        /// Aggregated engagement statistics per business (rating sum/count, totals).
        /// </summary>
        public DbSet<BusinessEngagementStats> BusinessEngagementStats => Set<BusinessEngagementStats>();
    }
}
