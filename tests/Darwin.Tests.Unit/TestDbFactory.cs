using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit
{
    /// <summary>
    /// Factory helper for creating an in‑memory implementation of
    /// <see cref="IAppDbContext"/> suitable for unit tests.  The returned
    /// DbContext uses EF Core's InMemory provider, which does not enforce
    /// relational constraints or transactions but is sufficient for
    /// exercising validators and simple handlers.  Each call to
    /// <see cref="Create"/> will construct a fresh database with a unique
    /// name so that state is not shared between tests.
    /// </summary>
    public static class TestDbFactory
    {
        /// <summary>
        /// Creates a new <see cref="IAppDbContext"/> backed by an in‑memory
        /// database.  Use this in unit tests to seed entities and perform
        /// queries without hitting a real SQL Server instance.  Note that
        /// EF Core's InMemory provider does not enforce global query
        /// filters—if you need to test soft delete behaviour, use the
        /// SQLite provider configured for in‑memory mode instead.
        /// </summary>
        public static IAppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<TestAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"darwin_test_{Guid.NewGuid()}")
                .Options;
            return new TestAppDbContext(options);
        }

        /// <summary>
        /// Minimal EF Core DbContext implementing <see cref="IAppDbContext"/>.
        /// Only entity sets required for the tests are mapped explicitly.
        /// Additional sets can be added as needed.  The OnModelCreating
        /// override sets up basic constraints such as keys and string
        /// lengths to mirror the production DbContext, but avoids
        /// configuration of global conventions.
        /// </summary>
        private sealed class TestAppDbContext : DbContext, IAppDbContext
        {
            public TestAppDbContext(DbContextOptions<TestAppDbContext> options)
                : base(options) { }

            /// <inheritdoc/>
            public DbSet<T> Set<T>() where T : class => base.Set<T>();

            /// <inheritdoc/>
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
                base.SaveChangesAsync(cancellationToken);

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Map ProductTranslation for slug validators
                modelBuilder.Entity<ProductTranslation>(b =>
                {
                    b.HasKey(x => x.Id);
                    b.Property(x => x.Culture).HasMaxLength(10).IsRequired();
                    b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
                    b.Property(x => x.IsDeleted);
                });

                // Map CategoryTranslation so that category slug validators can be tested
                modelBuilder.Entity<CategoryTranslation>(b =>
                {
                    b.HasKey(x => x.Id);
                    b.Property(x => x.Culture).HasMaxLength(10).IsRequired();
                    b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
                    b.Property(x => x.IsDeleted);
                });

                // Map PageTranslation so the page slug validators can access the translations table.
                // EF Core needs the entity to be part of the model in order to create a DbSet<PageTranslation>.
                modelBuilder.Entity<PageTranslation>(b =>
                {
                    b.HasKey(x => x.Id);
                    b.Property(x => x.PageId).IsRequired();
                    b.Property(x => x.Culture).HasMaxLength(10).IsRequired();
                    b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
                    // Include the IsDeleted flag for completeness even though soft delete is not tested here.
                    b.Property(x => x.IsDeleted);
                });

                // Map SiteSetting for settings handler tests.  RowVersion is configured as a concurrency token.
                modelBuilder.Entity<SiteSetting>(b =>
                {
                    b.HasKey(x => x.Id);
                    b.Property(x => x.RowVersion).IsRowVersion();
                    b.Property(x => x.Title).IsRequired();
                    b.Property(x => x.DefaultCulture).IsRequired();
                    b.Property(x => x.SupportedCulturesCsv).IsRequired();
                    // Additional properties can be configured here as needed for settings tests.
                });
            }
        }
    }
}