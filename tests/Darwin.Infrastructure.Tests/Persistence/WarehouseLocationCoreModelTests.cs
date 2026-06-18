using Darwin.Domain.Entities.Inventory;
using Darwin.Infrastructure.Persistence.Db;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Darwin.Infrastructure.Tests.Persistence;

public sealed class WarehouseLocationCoreModelTests
{
    private const string DummySqlServerConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=DarwinPersistenceMetadata;Trusted_Connection=True;TrustServerCertificate=True;";

    private const string DummyPostgreSqlConnectionString =
        "Host=127.0.0.1;Database=DarwinPersistenceMetadata;Username=postgres;Password=postgres;";

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void WarehouseLocation_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var entity = GetEntity(context, typeof(WarehouseLocation));

        entity.GetSchema().Should().Be("Inventory");
        entity.GetTableName().Should().Be("WarehouseLocations");
        entity.FindProperty(nameof(WarehouseLocation.Code))!.GetMaxLength().Should().Be(64);
        entity.FindProperty(nameof(WarehouseLocation.DisplayName))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(WarehouseLocation.LocationType))!.GetMaxLength().Should().Be(64);
        entity.FindProperty(nameof(WarehouseLocation.Status))!.GetMaxLength().Should().Be(64);
        entity.FindProperty(nameof(WarehouseLocation.Barcode))!.GetMaxLength().Should().Be(128);
        entity.FindProperty(nameof(WarehouseLocation.Description))!.GetMaxLength().Should().Be(1000);
        entity.FindProperty(nameof(WarehouseLocation.MetadataJson))!.GetMaxLength().Should().Be(8000);

        if (provider == "PostgreSql")
        {
            entity.FindProperty(nameof(WarehouseLocation.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        var codeIndex = entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_BusinessId_WarehouseId_Code");
        codeIndex.IsUnique.Should().BeTrue();
        codeIndex.GetFilter().Should().Contain("IsDeleted");

        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_BusinessId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_WarehouseId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_ParentLocationId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_LocationType");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_Status");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_Barcode");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLocations_SortOrder");
    }

    [Fact]
    public void WarehouseLocation_Migrations_Should_Create_Only_WarehouseLocation_Table()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*WarehouseLocationBinCoreModel.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("WarehouseLocations");
            migration.Should().Contain("IX_WarehouseLocations_BusinessId_WarehouseId_Code");
            migration.Should().NotContain("WarehouseTask");
            migration.Should().NotContain("StockCount");
            migration.Should().NotContain("HandlingUnit");
            migration.Should().NotContain("SupplierInvoice");
        }
    }

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void WarehouseLabelTemplate_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var entity = GetEntity(context, typeof(WarehouseLabelTemplate));

        entity.GetSchema().Should().Be("Inventory");
        entity.GetTableName().Should().Be("WarehouseLabelTemplates");
        entity.FindProperty(nameof(WarehouseLabelTemplate.Name))!.GetMaxLength().Should().Be(200);
        entity.FindProperty(nameof(WarehouseLabelTemplate.TemplateKey))!.GetMaxLength().Should().Be(100);
        entity.FindProperty(nameof(WarehouseLabelTemplate.Status))!.GetMaxLength().Should().Be(64);
        entity.FindProperty(nameof(WarehouseLabelTemplate.Format))!.GetMaxLength().Should().Be(64);
        entity.FindProperty(nameof(WarehouseLabelTemplate.ContentTemplate))!.GetMaxLength().Should().Be(8000);
        entity.FindProperty(nameof(WarehouseLabelTemplate.MetadataJson))!.GetMaxLength().Should().Be(8000);

        if (provider == "PostgreSql")
        {
            entity.FindProperty(nameof(WarehouseLabelTemplate.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        var keyIndex = entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLabelTemplates_BusinessId_TemplateKey");
        keyIndex.IsUnique.Should().BeTrue();
        keyIndex.GetFilter().Should().Contain("IsDeleted");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLabelTemplates_BusinessId_IsDefault");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLabelTemplates_Status");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseLabelTemplates_Format");
    }

    [Fact]
    public void WarehouseLabelTemplate_Migrations_Should_Create_Only_LabelTemplate_Table()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*WarehouseLabelTemplatePrinting.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("WarehouseLabelTemplates");
            migration.Should().Contain("IX_WarehouseLabelTemplates_BusinessId_TemplateKey");
            migration.Should().NotContain("WarehouseTask");
            migration.Should().NotContain("InventoryTransactions");
            migration.Should().NotContain("SupplierInvoice");
            migration.Should().NotContain("Payment");
        }
    }

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void WarehouseTask_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var task = GetEntity(context, typeof(WarehouseTask));
        var line = GetEntity(context, typeof(WarehouseTaskLine));

        task.GetSchema().Should().Be("Inventory");
        task.GetTableName().Should().Be("WarehouseTasks");
        task.FindProperty(nameof(WarehouseTask.TaskNumber))!.GetMaxLength().Should().Be(100);
        task.FindProperty(nameof(WarehouseTask.Title))!.GetMaxLength().Should().Be(200);
        task.FindProperty(nameof(WarehouseTask.TaskType))!.GetMaxLength().Should().Be(64);
        task.FindProperty(nameof(WarehouseTask.Status))!.GetMaxLength().Should().Be(64);
        task.FindProperty(nameof(WarehouseTask.Priority))!.GetMaxLength().Should().Be(64);
        task.FindProperty(nameof(WarehouseTask.SourceType))!.GetMaxLength().Should().Be(64);
        task.FindProperty(nameof(WarehouseTask.InternalNotes))!.GetMaxLength().Should().Be(4000);
        task.FindProperty(nameof(WarehouseTask.MetadataJson))!.GetMaxLength().Should().Be(8000);

        line.GetSchema().Should().Be("Inventory");
        line.GetTableName().Should().Be("WarehouseTaskLines");
        line.FindProperty(nameof(WarehouseTaskLine.SkuSnapshot))!.GetMaxLength().Should().Be(100);
        line.FindProperty(nameof(WarehouseTaskLine.Description))!.GetMaxLength().Should().Be(1000);
        line.FindProperty(nameof(WarehouseTaskLine.ShortReason))!.GetMaxLength().Should().Be(1000);
        line.FindProperty(nameof(WarehouseTaskLine.SourceLineType))!.GetMaxLength().Should().Be(100);
        line.FindProperty(nameof(WarehouseTaskLine.MetadataJson))!.GetMaxLength().Should().Be(8000);

        if (provider == "PostgreSql")
        {
            task.FindProperty(nameof(WarehouseTask.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            line.FindProperty(nameof(WarehouseTaskLine.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        var numberIndex = task.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTasks_BusinessId_TaskNumber");
        numberIndex.IsUnique.Should().BeTrue();
        numberIndex.GetFilter().Should().Contain("IsDeleted");

        task.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTasks_WarehouseId");
        task.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTasks_Status");
        task.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTasks_AssignedToUserId");
        task.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTasks_SourceEntityId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTaskLines_WarehouseTaskId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTaskLines_ProductVariantId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_WarehouseTaskLines_ShortQuantity");
    }

    [Fact]
    public void WarehouseTask_Migrations_Should_Create_Only_Task_Tables()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*WarehouseTaskFoundation.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("WarehouseTasks");
            migration.Should().Contain("WarehouseTaskLines");
            migration.Should().Contain("IX_WarehouseTasks_BusinessId_TaskNumber");
            migration.Should().NotContain("InventoryTransactions");
            migration.Should().NotContain("StockCount");
            migration.Should().NotContain("SupplierInvoice");
            migration.Should().NotContain("Payment");
            migration.Should().NotContain("Finance");
        }
    }

    [Fact]
    public void WarehousePickingShortageAttention_Migrations_Should_Add_Only_TaskLine_Shortage_Fields()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*WarehousePickingShortageAttention.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("WarehouseTaskLines");
            migration.Should().Contain("ShortQuantity");
            migration.Should().Contain("ShortReason");
            migration.Should().Contain("IX_WarehouseTaskLines_ShortQuantity");
            migration.Should().NotContain("InventoryTransactions");
            migration.Should().NotContain("Shipments");
            migration.Should().NotContain("SupplierInvoice");
            migration.Should().NotContain("Payment");
            migration.Should().NotContain("Finance");
        }
    }

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void StockCount_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var session = GetEntity(context, typeof(StockCountSession));
        var line = GetEntity(context, typeof(StockCountLine));

        session.GetSchema().Should().Be("Inventory");
        session.GetTableName().Should().Be("StockCountSessions");
        session.FindProperty(nameof(StockCountSession.CountNumber))!.GetMaxLength().Should().Be(100);
        session.FindProperty(nameof(StockCountSession.Title))!.GetMaxLength().Should().Be(200);
        session.FindProperty(nameof(StockCountSession.CountType))!.GetMaxLength().Should().Be(64);
        session.FindProperty(nameof(StockCountSession.Status))!.GetMaxLength().Should().Be(64);
        session.FindProperty(nameof(StockCountSession.InternalNotes))!.GetMaxLength().Should().Be(4000);
        session.FindProperty(nameof(StockCountSession.MetadataJson))!.GetMaxLength().Should().Be(8000);

        line.GetSchema().Should().Be("Inventory");
        line.GetTableName().Should().Be("StockCountLines");
        line.FindProperty(nameof(StockCountLine.SkuSnapshot))!.GetMaxLength().Should().Be(100);
        line.FindProperty(nameof(StockCountLine.Description))!.GetMaxLength().Should().Be(1000);
        line.FindProperty(nameof(StockCountLine.ReviewStatus))!.GetMaxLength().Should().Be(64);
        line.FindProperty(nameof(StockCountLine.ReviewNotes))!.GetMaxLength().Should().Be(2000);
        line.FindProperty(nameof(StockCountLine.MetadataJson))!.GetMaxLength().Should().Be(8000);

        if (provider == "PostgreSql")
        {
            session.FindProperty(nameof(StockCountSession.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            line.FindProperty(nameof(StockCountLine.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        var numberIndex = session.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountSessions_BusinessId_CountNumber");
        numberIndex.IsUnique.Should().BeTrue();
        numberIndex.GetFilter().Should().Contain("IsDeleted");
        session.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountSessions_WarehouseId");
        session.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountSessions_Status");
        session.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountSessions_CountWindowStartUtc");
        session.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountSessions_PostedAtUtc");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountLines_StockCountSessionId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountLines_ProductVariantId");
        line.GetIndexes().Single(x => x.GetDatabaseName() == "IX_StockCountLines_ReviewStatus");
    }

    [Fact]
    public void StockCount_Migrations_Should_Create_Only_StockCount_Tables()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*StockCountCoreModelInventory.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("StockCountSessions");
            migration.Should().Contain("StockCountLines");
            migration.Should().Contain("IX_StockCountSessions_BusinessId_CountNumber");
            migration.Should().NotContain("SupplierInvoice");
            migration.Should().NotContain("SupplierPayment");
            migration.Should().NotContain("FinanceExport");
            migration.Should().NotContain("Mobile");
            migration.Should().NotContain("Public");
        }
    }

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void LotSerialHandlingUnit_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var policy = GetEntity(context, typeof(ProductTrackingPolicy));
        var lot = GetEntity(context, typeof(InventoryLot));
        var serial = GetEntity(context, typeof(InventorySerialUnit));
        var handlingUnit = GetEntity(context, typeof(HandlingUnit));
        var content = GetEntity(context, typeof(HandlingUnitContent));

        policy.GetSchema().Should().Be("Inventory");
        policy.GetTableName().Should().Be("ProductTrackingPolicies");
        policy.FindProperty(nameof(ProductTrackingPolicy.TrackingMode))!.GetMaxLength().Should().Be(64);
        policy.FindProperty(nameof(ProductTrackingPolicy.Status))!.GetMaxLength().Should().Be(64);
        policy.FindProperty(nameof(ProductTrackingPolicy.Notes))!.GetMaxLength().Should().Be(2000);
        policy.FindProperty(nameof(ProductTrackingPolicy.MetadataJson))!.GetMaxLength().Should().Be(8000);
        policy.GetIndexes().Single(x => x.GetDatabaseName() == "IX_ProductTrackingPolicies_BusinessId_ProductVariantId").IsUnique.Should().BeTrue();

        lot.GetSchema().Should().Be("Inventory");
        lot.GetTableName().Should().Be("InventoryLots");
        lot.FindProperty(nameof(InventoryLot.LotCode))!.GetMaxLength().Should().Be(100);
        lot.FindProperty(nameof(InventoryLot.SupplierLotCode))!.GetMaxLength().Should().Be(100);
        lot.FindProperty(nameof(InventoryLot.Status))!.GetMaxLength().Should().Be(64);
        lot.GetIndexes().Single(x => x.GetDatabaseName() == "IX_InventoryLots_BusinessId_ProductVariantId_LotCode").IsUnique.Should().BeTrue();

        serial.GetSchema().Should().Be("Inventory");
        serial.GetTableName().Should().Be("InventorySerialUnits");
        serial.FindProperty(nameof(InventorySerialUnit.SerialNumber))!.GetMaxLength().Should().Be(128);
        serial.FindProperty(nameof(InventorySerialUnit.Status))!.GetMaxLength().Should().Be(64);
        serial.GetIndexes().Single(x => x.GetDatabaseName()!.StartsWith("IX_InventorySerialUnits_BusinessId_ProductVariantId_SerialNumb", StringComparison.Ordinal)).IsUnique.Should().BeTrue();

        handlingUnit.GetSchema().Should().Be("Inventory");
        handlingUnit.GetTableName().Should().Be("HandlingUnits");
        handlingUnit.FindProperty(nameof(HandlingUnit.Code))!.GetMaxLength().Should().Be(100);
        handlingUnit.FindProperty(nameof(HandlingUnit.DisplayName))!.GetMaxLength().Should().Be(200);
        handlingUnit.FindProperty(nameof(HandlingUnit.Barcode))!.GetMaxLength().Should().Be(128);
        handlingUnit.FindProperty(nameof(HandlingUnit.HandlingUnitType))!.GetMaxLength().Should().Be(64);
        handlingUnit.FindProperty(nameof(HandlingUnit.Status))!.GetMaxLength().Should().Be(64);
        handlingUnit.GetIndexes().Single(x => x.GetDatabaseName() == "IX_HandlingUnits_BusinessId_Code").IsUnique.Should().BeTrue();
        handlingUnit.GetIndexes().Single(x => x.GetDatabaseName() == "IX_HandlingUnits_ParentHandlingUnitId");

        content.GetSchema().Should().Be("Inventory");
        content.GetTableName().Should().Be("HandlingUnitContents");
        content.FindProperty(nameof(HandlingUnitContent.SkuSnapshot))!.GetMaxLength().Should().Be(100);
        content.FindProperty(nameof(HandlingUnitContent.Description))!.GetMaxLength().Should().Be(1000);
        content.GetIndexes().Single(x => x.GetDatabaseName() == "IX_HandlingUnitContents_HandlingUnitId");
        content.GetIndexes().Single(x => x.GetDatabaseName() == "IX_HandlingUnitContents_InventoryLotId");
        content.GetIndexes().Single(x => x.GetDatabaseName() == "IX_HandlingUnitContents_InventorySerialUnitId");

        if (provider == "PostgreSql")
        {
            policy.FindProperty(nameof(ProductTrackingPolicy.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            lot.FindProperty(nameof(InventoryLot.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            serial.FindProperty(nameof(InventorySerialUnit.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            handlingUnit.FindProperty(nameof(HandlingUnit.MetadataJson))!.GetColumnType().Should().Be("jsonb");
            content.FindProperty(nameof(HandlingUnitContent.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }
    }

    [Fact]
    public void LotSerialHandlingUnit_Migrations_Should_Create_Only_Traceability_Tables()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*LotSerialHandlingUnitCoreModel.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("ProductTrackingPolicies");
            migration.Should().Contain("InventoryLots");
            migration.Should().Contain("InventorySerialUnits");
            migration.Should().Contain("HandlingUnits");
            migration.Should().Contain("HandlingUnitContents");
            migration.Should().Contain("IX_InventoryLots_BusinessId_ProductVariantId_LotCode");
            migration.Should().Contain("IX_HandlingUnits_BusinessId_Code");
            migration.Should().NotContain("InventoryTransactions");
            migration.Should().NotContain("GoodsReceipts");
            migration.Should().NotContain("SupplierInvoice");
            migration.Should().NotContain("SupplierPayment");
            migration.Should().NotContain("Finance");
            migration.Should().NotContain("Mobile");
            migration.Should().NotContain("Public");
        }
    }

    [Theory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public void GoodsReceiptLineIdentity_Should_Map_To_Inventory_Schema_With_Stable_Indexes(string provider)
    {
        using var context = CreateContext(provider);
        var entity = GetEntity(context, typeof(GoodsReceiptLineIdentity));

        entity.GetSchema().Should().Be("Inventory");
        entity.GetTableName().Should().Be("GoodsReceiptLineIdentities");
        entity.FindProperty(nameof(GoodsReceiptLineIdentity.LotCodeSnapshot))!.GetMaxLength().Should().Be(100);
        entity.FindProperty(nameof(GoodsReceiptLineIdentity.SupplierLotCodeSnapshot))!.GetMaxLength().Should().Be(100);
        entity.FindProperty(nameof(GoodsReceiptLineIdentity.SerialNumberSnapshot))!.GetMaxLength().Should().Be(128);
        entity.FindProperty(nameof(GoodsReceiptLineIdentity.HandlingUnitCodeSnapshot))!.GetMaxLength().Should().Be(100);
        entity.FindProperty(nameof(GoodsReceiptLineIdentity.MetadataJson))!.GetMaxLength().Should().Be(8000);

        if (provider == "PostgreSql")
        {
            entity.FindProperty(nameof(GoodsReceiptLineIdentity.MetadataJson))!.GetColumnType().Should().Be("jsonb");
        }

        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLineIdentities_GoodsReceiptLineId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLineIdentities_ProductVariantId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLineIdentities_InventoryLotId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLineIdentities_InventorySerialUnitId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLineIdentities_HandlingUnitId");
        entity.GetIndexes().Single(x => x.GetDatabaseName() == "IX_GoodsReceiptLineIdentities_ExpiryDateUtc");
        var serialIndex = entity.GetIndexes().Single(x => x.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(GoodsReceiptLineIdentity.GoodsReceiptLineId), nameof(GoodsReceiptLineIdentity.InventorySerialUnitId) }));
        serialIndex.IsUnique.Should().BeTrue();
        serialIndex.GetFilter().Should().Contain("InventorySerialUnitId");
    }

    [Fact]
    public void ReceiptIdentityCapture_Migrations_Should_Add_Only_ReceiptIdentity_Table()
    {
        var root = RepositoryRoot();
        var migrations = Directory
            .GetFiles(Path.Combine(root, "src"), "*ReceiptIdentityCapture.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        migrations.Should().HaveCount(2);
        foreach (var migrationPath in migrations)
        {
            var migration = File.ReadAllText(migrationPath);
            migration.Should().Contain("GoodsReceiptLineIdentities");
            migration.Should().Contain("IX_GoodsReceiptLineIdentities_GoodsReceiptLineId");
            migration.Should().Contain("InventorySerialUnitId");
            migration.Should().Contain("unique: true");
            migration.Should().NotContain("StockCount");
            migration.Should().NotContain("WarehouseTasks");
            migration.Should().NotContain("SupplierInvoice");
            migration.Should().NotContain("SupplierPayment");
            migration.Should().NotContain("Finance");
            migration.Should().NotContain("Mobile");
            migration.Should().NotContain("Public");
        }
    }

    private static IEntityType GetEntity(DarwinDbContext context, Type type)
        => context.Model.FindEntityType(type) ?? throw new InvalidOperationException($"Entity {type.Name} not found.");

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

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Darwin.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
