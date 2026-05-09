using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Mapping;
using Darwin.Application.Catalog.Validators;
using Darwin.Application.Common.Html;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Darwin.Tests.Unit.Catalog;

/// <summary>
/// Unit tests for catalog command handlers that were not covered in the existing
/// <see cref="CatalogHandlerTests"/>:
/// CreateProductHandler, UpdateProductHandler, SoftDeleteProductHandler,
/// CreateAddOnGroupHandler, UpdateAddOnGroupHandler, SoftDeleteAddOnGroupHandler.
/// </summary>
public sealed class CatalogProductAndAddOnHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    private static IHtmlSanitizer CreatePassThroughSanitizer()
    {
        var mock = new Mock<IHtmlSanitizer>();
        mock.Setup(s => s.Sanitize(It.IsAny<string>())).Returns<string>(h => h);
        return mock.Object;
    }

    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CatalogProfile>(), NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    private static ProductTestDbContext CreateDb() => ProductTestDbContext.Create();

    private static ProductCreateDto BuildValidProductCreateDto(
        string culture = "en-US",
        string name = "Widget",
        string slug = "widget",
        string kind = "Simple")
    {
        return new ProductCreateDto
        {
            Kind = kind,
            Translations = new List<ProductTranslationDto>
            {
                new() { Culture = culture, Name = name, Slug = slug }
            },
            Variants = new List<ProductVariantCreateDto>
            {
                new()
                {
                    Sku = "SKU-001",
                    Currency = "EUR",
                    BasePriceNetMinor = 2000,
                    TaxCategoryId = Guid.NewGuid()
                }
            }
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateProductHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProduct_Should_Throw_ValidationException_When_Translations_Empty()
    {
        await using var db = CreateDb();
        var dto = new ProductCreateDto
        {
            Translations = new List<ProductTranslationDto>(),
            Variants = new List<ProductVariantCreateDto>
            {
                new() { Sku = "S", Currency = "EUR", TaxCategoryId = Guid.NewGuid() }
            }
        };
        var handler = new CreateProductHandler(db, CreateMapper(),
            new ProductCreateDtoValidator(CreateLocalizer()), CreatePassThroughSanitizer(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("at least one translation is required");
    }

    [Fact]
    public async Task CreateProduct_Should_Throw_ValidationException_When_Variants_Empty()
    {
        await using var db = CreateDb();
        var dto = new ProductCreateDto
        {
            Translations = new List<ProductTranslationDto>
            {
                new() { Culture = "en-US", Name = "Widget", Slug = "widget" }
            },
            Variants = new List<ProductVariantCreateDto>()
        };
        var handler = new CreateProductHandler(db, CreateMapper(),
            new ProductCreateDtoValidator(CreateLocalizer()), CreatePassThroughSanitizer(), CreateLocalizer());

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("at least one variant is required");
    }

    [Fact]
    public async Task CreateProduct_Should_Persist_Product_And_Return_Id()
    {
        await using var db = CreateDb();
        var dto = BuildValidProductCreateDto();
        var handler = new CreateProductHandler(db, CreateMapper(),
            new ProductCreateDtoValidator(CreateLocalizer()), CreatePassThroughSanitizer(), CreateLocalizer());

        var id = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        id.Should().NotBe(Guid.Empty);
        var product = db.Set<Product>().Include(p => p.Translations).Include(p => p.Variants).Single();
        product.Translations.Should().HaveCount(1);
        product.Translations[0].Name.Should().Be("Widget");
        product.Variants.Should().HaveCount(1);
        product.Variants[0].Sku.Should().Be("SKU-001");
    }

    [Fact]
    public async Task CreateProduct_Should_Uppercase_Currency()
    {
        await using var db = CreateDb();
        var dto = BuildValidProductCreateDto();
        dto.Variants[0].Currency = "eur";
        var handler = new CreateProductHandler(db, CreateMapper(),
            new ProductCreateDtoValidator(CreateLocalizer()), CreatePassThroughSanitizer(), CreateLocalizer());

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var variant = db.Set<ProductVariant>().Single();
        variant.Currency.Should().Be("EUR", "currency is normalized to upper-case");
    }

    [Fact]
    public async Task CreateProduct_Should_Trim_Slug()
    {
        await using var db = CreateDb();
        var dto = BuildValidProductCreateDto(slug: "  my-widget  ");
        var handler = new CreateProductHandler(db, CreateMapper(),
            new ProductCreateDtoValidator(CreateLocalizer()), CreatePassThroughSanitizer(), CreateLocalizer());

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var translation = db.Set<ProductTranslation>().Single();
        translation.Slug.Should().Be("my-widget");
    }

    [Fact]
    public async Task CreateProduct_Should_Set_Kind_Simple_By_Default()
    {
        await using var db = CreateDb();
        var dto = BuildValidProductCreateDto();
        var handler = new CreateProductHandler(db, CreateMapper(),
            new ProductCreateDtoValidator(CreateLocalizer()), CreatePassThroughSanitizer(), CreateLocalizer());

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var product = db.Set<Product>().Single();
        product.Kind.Should().Be(ProductKind.Simple);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateProductHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProduct_Should_Throw_ValidationException_When_Product_Not_Found()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var dto = new ProductEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<ProductTranslationDto> { new() { Culture = "en-US", Name = "W", Slug = "w" } },
            Variants = new List<ProductVariantCreateDto> { new() { Sku = "S", Currency = "EUR", TaxCategoryId = Guid.NewGuid() } }
        };
        var validator = new Mock<IValidator<ProductEditDto>>();
        validator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(new FluentValidation.Results.ValidationResult());
        var handler = new UpdateProductHandler(db,
            new ProductEditDtoValidator(localizer), CreatePassThroughSanitizer(), localizer);

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("product does not exist");
    }

    [Fact]
    public async Task UpdateProduct_Should_Throw_On_RowVersion_Mismatch()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var productId = Guid.NewGuid();
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            RowVersion = new byte[] { 1, 2, 3 },
            Translations = new List<ProductTranslation> { new() { Culture = "en-US", Name = "Old", Slug = "old" } },
            Variants = new List<ProductVariant> { new() { Id = Guid.NewGuid(), ProductId = productId, Sku = "SKU", Currency = "EUR" } }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new ProductEditDto
        {
            Id = productId,
            RowVersion = new byte[] { 9, 9, 9 }, // stale
            Translations = new List<ProductTranslationDto> { new() { Culture = "en-US", Name = "New", Slug = "new" } },
            Variants = new List<ProductVariantCreateDto> { new() { Sku = "SKU", Currency = "EUR", TaxCategoryId = Guid.NewGuid() } }
        };
        var handler = new UpdateProductHandler(db,
            new ProductEditDtoValidator(localizer), CreatePassThroughSanitizer(), localizer);

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale row version detected");
    }

    [Fact]
    public async Task UpdateProduct_Should_Persist_Changes_When_RowVersion_Matches()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var productId = Guid.NewGuid();
        var rowVersion = new byte[] { 1 };
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            RowVersion = rowVersion,
            Translations = new List<ProductTranslation> { new() { Culture = "en-US", Name = "Old Name", Slug = "old-slug" } },
            Variants = new List<ProductVariant> { new() { Id = Guid.NewGuid(), ProductId = productId, Sku = "SKU-OLD", Currency = "EUR" } }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new ProductEditDto
        {
            Id = productId,
            RowVersion = rowVersion,
            Kind = "Simple",
            Translations = new List<ProductTranslationDto> { new() { Culture = "en-US", Name = "New Name", Slug = "new-slug" } },
            Variants = new List<ProductVariantCreateDto> { new() { Sku = "SKU-OLD", Currency = "EUR", TaxCategoryId = Guid.NewGuid() } }
        };
        var handler = new UpdateProductHandler(db,
            new ProductEditDtoValidator(localizer), CreatePassThroughSanitizer(), localizer);

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var product = db.Set<Product>().Include(p => p.Translations).Single();
        var activeTranslations = product.Translations.Where(t => !t.IsDeleted).ToList();
        activeTranslations.Should().HaveCount(1);
        activeTranslations[0].Name.Should().Be("New Name");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoftDeleteProductHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteProduct_Should_Return_Failure_When_Id_Is_Empty()
    {
        await using var db = CreateDb();
        var handler = new SoftDeleteProductHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Guid is invalid");
    }

    [Fact]
    public async Task SoftDeleteProduct_Should_Return_Failure_When_RowVersion_Is_Null()
    {
        await using var db = CreateDb();
        var handler = new SoftDeleteProductHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("null row version is invalid");
    }

    [Fact]
    public async Task SoftDeleteProduct_Should_Return_Failure_When_Product_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new SoftDeleteProductHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent product");
    }

    [Fact]
    public async Task SoftDeleteProduct_Should_Return_Failure_On_RowVersion_Mismatch()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Set<Product>().Add(new Product { Id = id, RowVersion = new byte[] { 1, 2, 3 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteProductHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, new byte[] { 9, 9, 9 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale row version");
    }

    [Fact]
    public async Task SoftDeleteProduct_Should_Mark_As_Deleted_When_Valid()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        db.Set<Product>().Add(new Product { Id = id, RowVersion = rowVersion });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteProductHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, rowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        db.Set<Product>().Single().IsDeleted.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateAddOnGroupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAddOnGroup_Should_Throw_ValidationException_When_Name_Empty()
    {
        await using var db = CreateDb();
        var dto = new AddOnGroupCreateDto { Name = "", Currency = "EUR" };
        var handler = new CreateAddOnGroupHandler(db);

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Name is required");
    }

    [Fact]
    public async Task CreateAddOnGroup_Should_Persist_Group_With_Options_And_Values()
    {
        await using var db = CreateDb();
        var dto = new AddOnGroupCreateDto
        {
            Name = "Gift Wrap",
            Currency = "EUR",
            IsGlobal = false,
            IsActive = true,
            SelectionMode = AddOnSelectionMode.Single,
            Translations = new List<AddOnGroupTranslationDto>
            {
                new() { Culture = "en-US", Name = "Gift Wrapping" }
            },
            Options = new List<AddOnOptionDto>
            {
                new AddOnOptionDto
                {
                    Label = "Style",
                    SortOrder = 1,
                    Values = new List<AddOnOptionValueDto>
                    {
                        new AddOnOptionValueDto { Label = "Classic", PriceDeltaMinor = 100, IsActive = true }
                    }
                }
            }
        };
        var handler = new CreateAddOnGroupHandler(db);

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var group = db.Set<AddOnGroup>().Include(g => g.Translations).Include(g => g.Options).ThenInclude(o => o.Values).Single();
        group.Name.Should().Be("Gift Wrap");
        group.Currency.Should().Be("EUR");
        group.Translations.Should().HaveCount(1);
        group.Options.Should().HaveCount(1);
        group.Options[0].Values.Should().HaveCount(1);
        group.Options[0].Values[0].PriceDeltaMinor.Should().Be(100);
    }

    [Fact]
    public async Task CreateAddOnGroup_Should_Trim_Name_And_Currency()
    {
        await using var db = CreateDb();
        var dto = new AddOnGroupCreateDto { Name = "  Wrap  ", Currency = "eur" };
        var handler = new CreateAddOnGroupHandler(db);

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var group = db.Set<AddOnGroup>().Single();
        group.Name.Should().Be("Wrap");
        group.Currency.Should().Be("eur", "CreateAddOnGroupHandler stores currency as-is (trimmed but not upcased)");
    }

    [Fact]
    public async Task CreateAddOnGroup_Should_Persist_Empty_Options_List()
    {
        await using var db = CreateDb();
        var dto = new AddOnGroupCreateDto { Name = "Simple", Currency = "EUR" };
        var handler = new CreateAddOnGroupHandler(db);

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        db.Set<AddOnGroup>().Single().Options.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateAddOnGroupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAddOnGroup_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var dto = new AddOnGroupEditDto { Id = Guid.Empty, RowVersion = new byte[] { 1 }, Name = "G", Currency = "EUR" };
        var handler = new UpdateAddOnGroupHandler(db, localizer);

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Id cannot be empty");
    }

    [Fact]
    public async Task UpdateAddOnGroup_Should_Throw_InvalidOperationException_When_Group_Not_Found()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var dto = new AddOnGroupEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "Gift Wrap",
            Currency = "EUR"
        };
        var handler = new UpdateAddOnGroupHandler(db, localizer);

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("group does not exist");
    }

    [Fact]
    public async Task UpdateAddOnGroup_Should_Throw_On_RowVersion_Mismatch()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var id = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = id, Name = "G", Currency = "EUR", RowVersion = new byte[] { 1, 2, 3 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new AddOnGroupEditDto
        {
            Id = id,
            RowVersion = new byte[] { 9, 9, 9 }, // stale
            Name = "G Updated",
            Currency = "EUR"
        };
        var handler = new UpdateAddOnGroupHandler(db, localizer);

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale row version");
    }

    [Fact]
    public async Task UpdateAddOnGroup_Should_Persist_Changes_When_RowVersion_Matches()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 1 };
        db.Set<AddOnGroup>().Add(new AddOnGroup
        {
            Id = id,
            Name = "Old Name",
            Currency = "EUR",
            RowVersion = rowVersion,
            IsGlobal = false,
            IsActive = true,
            Translations = new List<AddOnGroupTranslation>(),
            Options = new List<AddOnOption>()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new AddOnGroupEditDto
        {
            Id = id,
            RowVersion = rowVersion,
            Name = "New Name",
            Currency = "USD",
            IsGlobal = true,
            IsActive = false
        };
        var handler = new UpdateAddOnGroupHandler(db, localizer);

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var updated = db.Set<AddOnGroup>().Single();
        updated.Name.Should().Be("New Name");
        updated.Currency.Should().Be("USD");
        updated.IsGlobal.Should().BeTrue();
        updated.IsActive.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoftDeleteAddOnGroupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteAddOnGroup_Should_Return_Failure_For_Invalid_Dto()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var dto = new AddOnGroupDeleteDto { Id = Guid.Empty, RowVersion = new byte[] { 1 } };
        var handler = new SoftDeleteAddOnGroupHandler(db, localizer);

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id is invalid");
    }

    [Fact]
    public async Task SoftDeleteAddOnGroup_Should_Return_Failure_When_Not_Found()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var dto = new AddOnGroupDeleteDto { Id = Guid.NewGuid(), RowVersion = new byte[] { 1 } };
        var handler = new SoftDeleteAddOnGroupHandler(db, localizer);

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("group does not exist");
    }

    [Fact]
    public async Task SoftDeleteAddOnGroup_Should_Return_Success_When_Already_Deleted()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var id = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = id, Name = "G", Currency = "EUR", IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new AddOnGroupDeleteDto { Id = id, RowVersion = new byte[] { 1 } };
        var handler = new SoftDeleteAddOnGroupHandler(db, localizer);

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("deleting an already-deleted group is idempotent");
    }

    [Fact]
    public async Task SoftDeleteAddOnGroup_Should_Return_Failure_On_RowVersion_Mismatch()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var id = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = id, Name = "G", Currency = "EUR", RowVersion = new byte[] { 1, 2, 3 } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new AddOnGroupDeleteDto { Id = id, RowVersion = new byte[] { 9, 9, 9 } };
        var handler = new SoftDeleteAddOnGroupHandler(db, localizer);

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale row version");
    }

    [Fact]
    public async Task SoftDeleteAddOnGroup_Should_Mark_As_Deleted_When_Valid()
    {
        await using var db = CreateDb();
        var localizer = CreateLocalizer();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 5, 6, 7 };
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = id, Name = "G", Currency = "EUR", RowVersion = rowVersion });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new AddOnGroupDeleteDto { Id = id, RowVersion = rowVersion };
        var handler = new SoftDeleteAddOnGroupHandler(db, localizer);

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        db.Set<AddOnGroup>().Single().IsDeleted.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared in-memory DbContext for product and add-on group tests
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class ProductTestDbContext : DbContext, IAppDbContext
    {
        private ProductTestDbContext(DbContextOptions<ProductTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ProductTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ProductTestDbContext>()
                .UseInMemoryDatabase($"darwin_product_addon_{Guid.NewGuid()}")
                .Options;
            return new ProductTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.IsActive);
                b.Property(x => x.IsVisible);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.Ignore(x => x.RelatedProductIds);
                b.HasMany(x => x.Translations).WithOne().HasForeignKey(t => t.ProductId);
                b.HasMany(x => x.Variants).WithOne().HasForeignKey(v => v.ProductId);
                b.HasMany(x => x.Options).WithOne().HasForeignKey(o => o.ProductId);
                b.HasMany(x => x.Media).WithOne().HasForeignKey(m => m.ProductId);
            });

            modelBuilder.Entity<ProductTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(10).IsRequired();
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<ProductVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Sku).HasMaxLength(128).IsRequired();
                b.Property(x => x.Currency).HasMaxLength(3);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.ProductId).IsRequired();
            });

            modelBuilder.Entity<ProductOption>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200);
                b.HasMany(x => x.Values).WithOne().HasForeignKey(v => v.ProductOptionId);
            });

            modelBuilder.Entity<ProductOptionValue>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Value).HasMaxLength(200);
            });

            modelBuilder.Entity<ProductMedia>(b =>
            {
                b.HasKey(x => x.Id);
            });

            // ── AddOnGroup aggregate ───────────────────────────────────────
            modelBuilder.Entity<AddOnGroup>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(256).IsRequired();
                b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Property(x => x.IsActive);
                b.Property(x => x.IsGlobal);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Translations).WithOne().HasForeignKey(t => t.AddOnGroupId);
                b.HasMany(x => x.Options).WithOne().HasForeignKey(o => o.AddOnGroupId);
            });

            modelBuilder.Entity<AddOnGroupTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(16).IsRequired();
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<AddOnOption>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Label).HasMaxLength(256).IsRequired();
                b.Property(x => x.IsDeleted);
                b.HasMany(x => x.Translations).WithOne().HasForeignKey(t => t.AddOnOptionId);
                b.HasMany(x => x.Values).WithOne().HasForeignKey(v => v.AddOnOptionId);
            });

            modelBuilder.Entity<AddOnOptionTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(16).IsRequired();
                b.Property(x => x.Label).HasMaxLength(256).IsRequired();
            });

            modelBuilder.Entity<AddOnOptionValue>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Label).HasMaxLength(256).IsRequired();
                b.Property(x => x.IsDeleted);
                b.HasMany(x => x.Translations).WithOne().HasForeignKey(t => t.AddOnOptionValueId);
            });

            modelBuilder.Entity<AddOnOptionValueTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(16).IsRequired();
                b.Property(x => x.Label).HasMaxLength(256).IsRequired();
            });
        }
    }
}
