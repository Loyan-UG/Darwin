using Darwin.Domain.Entities.Inventory;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class PurchasingSupplierContactsModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("PostgreSql")]
    public void SupplierContact_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var contact = GetEntity(context, typeof(SupplierContact));

        contact.GetSchema().Should().Be("Inventory");
        contact.GetTableName().Should().Be("SupplierContacts");
        contact.FindProperty(nameof(SupplierContact.Role))!.GetMaxLength().Should().Be(64);
        contact.FindProperty(nameof(SupplierContact.Name))!.GetMaxLength().Should().Be(200);
        contact.FindProperty(nameof(SupplierContact.JobTitle))!.GetMaxLength().Should().Be(200);
        contact.FindProperty(nameof(SupplierContact.Email))!.GetMaxLength().Should().Be(320);
        contact.FindProperty(nameof(SupplierContact.Phone))!.GetMaxLength().Should().Be(50);
        contact.FindProperty(nameof(SupplierContact.LanguageCode))!.GetMaxLength().Should().Be(16);
        contact.FindProperty(nameof(SupplierContact.Notes))!.GetMaxLength().Should().Be(1000);

        var emailIndex = contact.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierContacts_BusinessId_SupplierId_Role_Email");
        emailIndex.IsUnique.Should().BeTrue();
        emailIndex.GetFilter().Should().Contain("Email");
        emailIndex.GetFilter().Should().Contain("IsDeleted");
        contact.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierContacts_BusinessId");
        contact.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierContacts_SupplierId");
        contact.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierContacts_Role");
        contact.GetIndexes().Single(x => x.GetDatabaseName() == "IX_SupplierContacts_SupplierId_IsPrimary");
    }

    [Fact]
    public void SupplierContact_Migrations_Should_Create_OnlySupplierContactTable()
    {
        var root = FindRepositoryRoot();
        var migrationFiles = Directory
            .GetFiles(Path.Combine(root, "src"), "*PurchasingDocumentsSupplierContacts.cs", SearchOption.AllDirectories)
            .Where(x => !x.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToArray();

        migrationFiles.Should().HaveCount(2);
        foreach (var file in migrationFiles)
        {
            var migration = File.ReadAllText(file);
            migration.Should().Contain("SupplierContacts");
            migration.Should().Contain("IX_SupplierContacts_BusinessId_SupplierId_Role_Email");
            migration.Should().NotContain("SupplierInvoices");
            migration.Should().NotContain("SupplierPayments");
            migration.Should().NotContain("Payables");
            migration.Should().NotContain("BankAccounts");
            migration.Should().NotContain("JournalEntries");
            migration.Should().NotContain("Mobile");
        }
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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Darwin repository root.");
    }
}
