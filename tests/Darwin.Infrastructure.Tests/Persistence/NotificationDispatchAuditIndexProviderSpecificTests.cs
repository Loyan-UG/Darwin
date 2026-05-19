using Darwin.Domain.Entities.Integration;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class NotificationDispatchAuditIndexProviderSpecificTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Fact]
    public void SqlServerContext_Should_HaveActiveChannelDispatchAudit_FilteredUniqueIndex()
    {
        using var context = BuildSqlServerContext();

        var index = FindChannelDispatchAuditIndex(context);

        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name)
            .Should().Equal("Channel", "CorrelationKey");
        index.GetDatabaseName().Should().Be("UX_ChannelDispatchAudits_ActiveChannelCorrelation");
        index.GetFilter().Should().Be("[CorrelationKey] IS NOT NULL AND [IsDeleted] = 0 AND [Status] IN (N'Pending', N'Sent')");
    }

    [Fact]
    public void NpgsqlContext_Should_HaveActiveChannelDispatchAudit_FilteredUniqueIndex()
    {
        using var context = BuildPostgreSqlContext();

        var index = FindChannelDispatchAuditIndex(context);

        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name)
            .Should().Equal("Channel", "CorrelationKey");
        index.GetDatabaseName().Should().Be("UX_ChannelDispatchAudits_ActiveChannelCorrelation");
        index.GetFilter().Should().Be("\"CorrelationKey\" IS NOT NULL AND \"IsDeleted\" = FALSE AND \"Status\" IN ('Pending', 'Sent')");
    }

    [Fact]
    public void SqlServerContext_Should_HaveActiveEmailDispatchAudit_FilteredUniqueIndex()
    {
        using var context = BuildSqlServerContext();

        var index = FindEmailDispatchAuditIndex(context);

        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name)
            .Should().Equal("CorrelationKey");
        index.GetDatabaseName().Should().Be("UX_EmailDispatchAudits_ActiveCorrelation");
        index.GetFilter().Should().Be("[CorrelationKey] IS NOT NULL AND [IsDeleted] = 0 AND [Status] IN (N'Pending', N'Sent')");
    }

    [Fact]
    public void NpgsqlContext_Should_HaveActiveEmailDispatchAudit_FilteredUniqueIndex()
    {
        using var context = BuildPostgreSqlContext();

        var index = FindEmailDispatchAuditIndex(context);

        index.IsUnique.Should().BeTrue();
        index.Properties.Select(p => p.Name)
            .Should().Equal("CorrelationKey");
        index.GetDatabaseName().Should().Be("UX_EmailDispatchAudits_ActiveCorrelation");
        index.GetFilter().Should().Be("\"CorrelationKey\" IS NOT NULL AND \"IsDeleted\" = FALSE AND \"Status\" IN ('Pending', 'Sent')");
    }

    private static DarwinDbContext BuildSqlServerContext()
    {
        var options = new DbContextOptionsBuilder<DarwinDbContext>()
            .UseSqlServer(DummySqlServerConnectionString)
            .Options;

        return new DarwinDbContext(options);
    }

    private static DarwinDbContext BuildPostgreSqlContext()
    {
        var options = new DbContextOptionsBuilder<DarwinDbContext>()
            .UseNpgsql(DummyPostgreSqlConnectionString)
            .Options;

        return new DarwinDbContext(options);
    }

    private static IMutableIndex FindChannelDispatchAuditIndex(DarwinDbContext context)
    {
        var entityType = context.Model.FindEntityType(typeof(ChannelDispatchAudit));
        entityType.Should().NotBeNull();

        return entityType!.GetIndexes()
            .Single(index => index.GetDatabaseName() == "UX_ChannelDispatchAudits_ActiveChannelCorrelation");
    }

    private static IMutableIndex FindEmailDispatchAuditIndex(DarwinDbContext context)
    {
        var entityType = context.Model.FindEntityType(typeof(EmailDispatchAudit));
        entityType.Should().NotBeNull();

        return entityType!.GetIndexes()
            .Single(index => index.GetDatabaseName() == "UX_EmailDispatchAudits_ActiveCorrelation");
    }
}
