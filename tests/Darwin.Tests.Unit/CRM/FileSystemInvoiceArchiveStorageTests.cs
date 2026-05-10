using Darwin.Application.Abstractions.Invoicing;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CRM.Services;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Settings;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Darwin.Application.Extensions;

namespace Darwin.Tests.Unit.CRM;

public sealed class FileSystemInvoiceArchiveStorageTests
{
    [Fact]
    public async Task SaveReadExistsAndPurge_Should_UseConfiguredRootWithoutExposingRawPath()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_invoice_archive_{Guid.NewGuid():N}");
        try
        {
            await using var db = ArchiveDbContext.Create();
            var invoice = new Invoice { Id = Guid.NewGuid() };
            db.Set<Invoice>().Add(invoice);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var storage = new FileSystemInvoiceArchiveStorage(
                db,
                new FileSystemInvoiceArchiveStorageOptions { RootPath = root });
            var artifact = new InvoiceArchiveStorageArtifact(
                invoice.Id,
                new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc),
                "application/vnd.darwin.invoice+json",
                "../unsafe.json",
                "{\"invoiceNumber\":\"INV-FS-1\"}");

            var result = await storage.SaveAsync(invoice, artifact, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            result.HashSha256.Should().HaveLength(64);
            result.RetentionPolicyVersion.Should().Contain(InvoiceArchiveStorageProviderNames.FileSystem);
            Directory.GetFiles(root, "*issued-snapshot.json", SearchOption.AllDirectories).Should().ContainSingle();
            Directory.GetFiles(root, "*metadata.json", SearchOption.AllDirectories).Should().ContainSingle();

            (await storage.ExistsAsync(invoice.Id, TestContext.Current.CancellationToken)).Should().BeTrue();
            var read = await storage.ReadAsync(invoice.Id, TestContext.Current.CancellationToken);
            read.Should().NotBeNull();
            read!.Payload.Should().Be(artifact.Payload);
            read.ContentType.Should().Be("application/vnd.darwin.invoice+json");
            read.FileName.Should().Be($"invoice-{invoice.Id:N}-issued-snapshot.json");
            read.FileName.Should().NotContain("..");

            await storage.PurgePayloadAsync(invoice, "retention elapsed", new DateTime(2036, 5, 10, 0, 0, 0, DateTimeKind.Utc), TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            Directory.GetFiles(root, "*.json", SearchOption.AllDirectories).Should().BeEmpty();
            (await storage.ExistsAsync(invoice.Id, TestContext.Current.CancellationToken)).Should().BeFalse();
            (await storage.ReadAsync(invoice.Id, TestContext.Current.CancellationToken)).Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Router_Should_Select_FileSystem_Provider_When_Configured()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_invoice_archive_router_{Guid.NewGuid():N}");
        try
        {
            await using var db = ArchiveDbContext.Create();
            var invoice = new Invoice { Id = Guid.NewGuid() };
            db.Set<Invoice>().Add(invoice);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var router = new InvoiceArchiveStorageRouter(
                new IInvoiceArchiveStorageProvider[]
                {
                    new DatabaseInvoiceArchiveStorage(db),
                    new FileSystemInvoiceArchiveStorage(db, new FileSystemInvoiceArchiveStorageOptions { RootPath = root })
                },
                new InvoiceArchiveStorageSelection { ProviderName = InvoiceArchiveStorageProviderNames.FileSystem });
            var artifact = new InvoiceArchiveStorageArtifact(
                invoice.Id,
                new DateTime(2026, 5, 10, 12, 30, 0, DateTimeKind.Utc),
                "application/json",
                "invoice.json",
                "{\"invoiceNumber\":\"INV-FS-ROUTED\"}");

            var result = await router.SaveAsync(invoice, artifact, TestContext.Current.CancellationToken);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            result.RetentionPolicyVersion.Should().Contain(InvoiceArchiveStorageProviderNames.FileSystem);
            Directory.GetFiles(root, "*issued-snapshot.json", SearchOption.AllDirectories).Should().ContainSingle();
            Directory.GetFiles(root, "*metadata.json", SearchOption.AllDirectories).Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AddApplication_Should_Bind_FileSystem_Archive_Provider_From_Configuration()
    {
        var root = Path.Combine(Path.GetTempPath(), $"darwin_invoice_archive_di_{Guid.NewGuid():N}");
        try
        {
            await using var db = ArchiveDbContext.Create();
            var invoice = new Invoice { Id = Guid.NewGuid() };
            db.Set<Invoice>().Add(invoice);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InvoiceArchiveStorage:ProviderName"] = InvoiceArchiveStorageProviderNames.FileSystem,
                    ["InvoiceArchiveStorage:FileSystem:RootPath"] = root
                })
                .Build();
            var services = new ServiceCollection();
            services.AddSingleton<IAppDbContext>(db);
            services.AddApplication(configuration);

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var scope = provider.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IInvoiceArchiveStorage>();

            var artifact = new InvoiceArchiveStorageArtifact(
                invoice.Id,
                new DateTime(2026, 5, 10, 13, 0, 0, DateTimeKind.Utc),
                "application/json",
                "invoice.json",
                "{\"invoiceNumber\":\"INV-FS-DI\"}");

            var result = await storage.SaveAsync(invoice, artifact, TestContext.Current.CancellationToken);

            result.RetentionPolicyVersion.Should().Contain(InvoiceArchiveStorageProviderNames.FileSystem);
            Directory.GetFiles(root, "*issued-snapshot.json", SearchOption.AllDirectories).Should().ContainSingle();
            Directory.GetFiles(root, "*metadata.json", SearchOption.AllDirectories).Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AddApplication_Should_Keep_Internal_Database_Default_When_FileSystem_Root_Is_Not_Configured()
    {
        await using var db = ArchiveDbContext.Create();
        var invoice = new Invoice { Id = Guid.NewGuid() };
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var services = new ServiceCollection();
        services.AddSingleton<IAppDbContext>(db);
        services.AddApplication(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IInvoiceArchiveStorage>();
        var artifact = new InvoiceArchiveStorageArtifact(
            invoice.Id,
            new DateTime(2026, 5, 10, 13, 30, 0, DateTimeKind.Utc),
            "application/json",
            "invoice.json",
            "{\"invoiceNumber\":\"INV-DB-DEFAULT\"}");

        var result = await storage.SaveAsync(invoice, artifact, TestContext.Current.CancellationToken);

        result.RetentionPolicyVersion.Should().NotContain(InvoiceArchiveStorageProviderNames.FileSystem);
        invoice.IssuedSnapshotJson.Should().Be(artifact.Payload);
    }

    [Fact]
    public async Task AddApplication_Should_Fail_FileSystem_Save_When_Root_Is_Not_Configured()
    {
        await using var db = ArchiveDbContext.Create();
        var invoice = new Invoice { Id = Guid.NewGuid() };
        db.Set<Invoice>().Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InvoiceArchiveStorage:ProviderName"] = InvoiceArchiveStorageProviderNames.FileSystem
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IAppDbContext>(db);
        services.AddApplication(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IInvoiceArchiveStorage>();
        var artifact = new InvoiceArchiveStorageArtifact(
            invoice.Id,
            new DateTime(2026, 5, 10, 14, 0, 0, DateTimeKind.Utc),
            "application/json",
            "invoice.json",
            "{\"invoiceNumber\":\"INV-FS-MISSING-ROOT\"}");

        var action = () => storage.SaveAsync(invoice, artifact, TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Invoice archive file-system root path is not configured.");
    }

    private sealed class ArchiveDbContext : DbContext, IAppDbContext
    {
        private ArchiveDbContext(DbContextOptions<ArchiveDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ArchiveDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseInMemoryDatabase($"darwin_archive_storage_{Guid.NewGuid():N}")
                .Options;
            return new ArchiveDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<GeoCoordinate>();

            modelBuilder.Entity<Invoice>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
                b.Ignore(x => x.Lines);
            });

            modelBuilder.Entity<SiteSetting>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired();
                b.Property(x => x.ContactEmail).IsRequired();
                b.Property(x => x.HomeSlug).IsRequired();
                b.Property(x => x.DefaultCulture).IsRequired();
                b.Property(x => x.SupportedCulturesCsv).IsRequired();
                b.Property(x => x.DefaultCountry).IsRequired();
                b.Property(x => x.DefaultCurrency).IsRequired();
                b.Property(x => x.TimeZone).IsRequired();
                b.Property(x => x.DateFormat).IsRequired();
                b.Property(x => x.TimeFormat).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
