using Darwin.Domain.Entities.CRM;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class CrmCoreModelExpansionModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void CrmCoreFields_Should_MapToCrmSchemaWithStableIndexes(string provider)
    {
        using var context = CreateContext(provider);

        var customer = GetEntity(context, typeof(Customer));
        var lead = GetEntity(context, typeof(Lead));
        var opportunity = GetEntity(context, typeof(Opportunity));
        var consent = GetEntity(context, typeof(Consent));
        var segment = GetEntity(context, typeof(CustomerSegment));

        customer.GetSchema().Should().Be("CRM");
        customer.FindProperty(nameof(Customer.AcquisitionSource))!.GetMaxLength().Should().Be(200);
        customer.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Customers_LifecycleStatus");
        customer.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Customers_OwnerUserId");
        customer.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Customers_NextFollowUpAtUtc");

        lead.FindProperty(nameof(Lead.ClosedReason))!.GetMaxLength().Should().Be(512);
        lead.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Leads_Priority");
        lead.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Leads_QualifiedAtUtc");
        lead.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Leads_ConvertedAtUtc");

        opportunity.FindProperty(nameof(Opportunity.Currency))!.GetMaxLength().Should().Be(3);
        opportunity.FindProperty(nameof(Opportunity.CloseReason))!.GetMaxLength().Should().Be(512);
        opportunity.FindProperty(nameof(Opportunity.Source))!.GetMaxLength().Should().Be(200);
        opportunity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Opportunities_ForecastCategory");
        opportunity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Opportunities_ExpectedCloseDateUtc");
        opportunity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Opportunities_ClosedAtUtc");

        consent.FindProperty(nameof(Consent.Source))!.GetMaxLength().Should().Be(200);
        consent.FindProperty(nameof(Consent.PolicyVersion))!.GetMaxLength().Should().Be(80);
        consent.FindProperty(nameof(Consent.EvidenceJson))!.GetMaxLength().Should().Be(4000);
        consent.GetIndexes().Single(x => x.GetDatabaseName() == "IX_Consents_PolicyVersion");

        segment.FindProperty(nameof(CustomerSegment.Code))!.GetMaxLength().Should().Be(128);
        segment.FindProperty(nameof(CustomerSegment.RuleJson))!.GetMaxLength().Should().Be(4000);
        var codeIndex = segment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CustomerSegments_Code");
        codeIndex.IsUnique.Should().BeTrue();
        codeIndex.GetFilter().Should().Contain("IsDeleted");
        segment.GetIndexes().Single(x => x.GetDatabaseName() == "IX_CustomerSegments_IsActive");
    }

    [Fact]
    public void PostgreSqlModel_Should_MapCrmJsonColumnsToJsonb()
    {
        using var context = CreateContext("PostgreSql");

        GetEntity(context, typeof(Consent))
            .FindProperty(nameof(Consent.EvidenceJson))!
            .GetColumnType()
            .Should().Be("jsonb");
        GetEntity(context, typeof(CustomerSegment))
            .FindProperty(nameof(CustomerSegment.RuleJson))!
            .GetColumnType()
            .Should().Be("jsonb");
    }

    private static IEntityType GetEntity(DarwinDbContext context, Type type)
        => context.Model.FindEntityType(type)!;

    private static DarwinDbContext CreateContext(string provider)
    {
        var builder = new DbContextOptionsBuilder<DarwinDbContext>();
        if (provider == "PostgreSql")
        {
            builder.UseNpgsql(DummyPostgreSqlConnectionString);
        }
        else
        {
            builder.UseSqlServer(DummySqlServerConnectionString);
        }

        return new DarwinDbContext(builder.Options);
    }
}
