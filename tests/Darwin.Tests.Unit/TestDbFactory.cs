using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit
{
    /// <summary>
    /// Creates an EF Core InMemory DbContext that implements IAppDbContext for unit tests.
    /// </summary>
    public static class TestDbFactory
    {
        public static IAppDbContext Create()
        {
            var options = new DbContextOptionsBuilder<TestAppDbContext>()
                .UseInMemoryDatabase(databaseName: $"darwin_test_{Guid.NewGuid()}")
                .Options;

            return new TestAppDbContext(options);
        }

        private sealed class TestAppDbContext : DbContext, IAppDbContext
        {
            public TestAppDbContext(DbContextOptions<TestAppDbContext> options) : base(options) { }

            public DbSet<T> Set<T>() where T : class => base.Set<T>();

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => base.SaveChangesAsync(cancellationToken);

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                // Minimal mapping for ProductTranslation needed by validators
                modelBuilder.Entity<Darwin.Domain.Entities.Catalog.ProductTranslation>(b =>
                {
                    b.HasKey(x => x.Id);
                    b.Property(x => x.Culture).HasMaxLength(10).IsRequired();
                    b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
                    b.HasIndex(x => new { x.Culture, x.Slug }).IsUnique(false); // validator checks uniqueness; DB uniqueness optional in unit tests
                });
            }
        }
    }
}
