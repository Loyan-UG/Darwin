using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.Commands;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Validators;
using Darwin.Domain.Entities.Catalog;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Catalog;

/// <summary>
/// Handler-level unit tests for the Catalog module.
/// Covers <see cref="CreateBrandHandler"/>, <see cref="UpdateBrandHandler"/>,
/// <see cref="SoftDeleteBrandHandler"/>, <see cref="CreateCategoryHandler"/>,
/// <see cref="UpdateCategoryHandler"/>, and <see cref="SoftDeleteCategoryHandler"/>.
/// </summary>
public sealed class CatalogHandlerTests
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

    private static CatalogTestDbContext CreateDb() => CatalogTestDbContext.Create();

    private static BrandCreateDto BuildValidBrandCreateDto(string slug = "") => new()
    {
        Slug = string.IsNullOrEmpty(slug) ? null : slug,
        Translations = new List<BrandTranslationDto>
        {
            new() { Culture = "en-US", Name = "Acme" }
        }
    };

    private static BrandEditDto BuildValidBrandEditDto(Guid id, byte[] rowVersion, string? slug = null) => new()
    {
        Id = id,
        RowVersion = rowVersion,
        Slug = slug,
        Translations = new List<BrandTranslationDto>
        {
            new() { Culture = "en-US", Name = "Acme Updated" }
        }
    };

    private static CategoryCreateDto BuildValidCategoryCreateDto() => new()
    {
        IsActive = true,
        SortOrder = 0,
        Translations = new List<CategoryTranslationDto>
        {
            new() { Culture = "en-US", Name = "Electronics", Slug = "electronics" }
        }
    };

    private static CategoryEditDto BuildValidCategoryEditDto(Guid id, byte[] rowVersion) => new()
    {
        Id = id,
        RowVersion = rowVersion,
        IsActive = true,
        SortOrder = 1,
        Translations = new List<CategoryTranslationDto>
        {
            new() { Culture = "en-US", Name = "Electronics Updated", Slug = "electronics-updated" }
        }
    };

    // ─────────────────────────────────────────────────────────────────────────
    // CreateBrandHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBrand_Should_Persist_Brand_With_Translations()
    {
        await using var db = CreateDb();
        var handler = new CreateBrandHandler(db, CreateLocalizer());

        await handler.HandleAsync(BuildValidBrandCreateDto(), TestContext.Current.CancellationToken);

        var brand = db.Set<Brand>().Include(b => b.Translations).Single();
        brand.Should().NotBeNull();
        brand.Translations.Should().HaveCount(1);
        brand.Translations[0].Culture.Should().Be("en-US");
        brand.Translations[0].Name.Should().Be("Acme");
    }

    [Fact]
    public async Task CreateBrand_Should_Trim_Slug()
    {
        await using var db = CreateDb();
        var handler = new CreateBrandHandler(db, CreateLocalizer());
        var dto = BuildValidBrandCreateDto("  acme-slug  ");

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var brand = db.Set<Brand>().Single();
        brand.Slug.Should().Be("acme-slug", "slug must be trimmed of whitespace");
    }

    [Fact]
    public async Task CreateBrand_Should_Persist_Null_Slug_When_Not_Provided()
    {
        await using var db = CreateDb();
        var handler = new CreateBrandHandler(db, CreateLocalizer());

        await handler.HandleAsync(BuildValidBrandCreateDto(), TestContext.Current.CancellationToken);

        var brand = db.Set<Brand>().Single();
        brand.Slug.Should().BeNull("no slug was provided");
    }

    [Fact]
    public async Task CreateBrand_Should_Throw_ValidationException_When_Translations_Empty()
    {
        await using var db = CreateDb();
        var handler = new CreateBrandHandler(db, CreateLocalizer());
        var dto = new BrandCreateDto { Translations = new List<BrandTranslationDto>() };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "at least one translation is required");
    }

    [Fact]
    public async Task CreateBrand_Should_Throw_ValidationException_When_Slug_Already_Exists()
    {
        await using var db = CreateDb();
        // Seed an existing brand with the same slug
        db.Set<Brand>().Add(new Brand { Id = Guid.NewGuid(), Slug = "existing-slug" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateBrandHandler(db, CreateLocalizer());
        var dto = BuildValidBrandCreateDto("existing-slug");

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>(
            "slug must be unique across all brands");
    }

    [Fact]
    public async Task CreateBrand_Should_Sanitize_DescriptionHtml()
    {
        await using var db = CreateDb();
        var handler = new CreateBrandHandler(db, CreateLocalizer());
        var dto = new BrandCreateDto
        {
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme", DescriptionHtml = "<script>alert('xss')</script><p>About Acme</p>" }
            }
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var brand = db.Set<Brand>().Include(b => b.Translations).Single();
        brand.Translations[0].DescriptionHtml.Should().NotContain("<script>",
            "scripts must be removed by the HTML sanitizer");
        brand.Translations[0].DescriptionHtml.Should().Contain("About Acme",
            "safe content should be preserved");
    }

    [Fact]
    public async Task CreateBrand_Should_Set_LogoMediaId_When_Provided()
    {
        await using var db = CreateDb();
        var handler = new CreateBrandHandler(db, CreateLocalizer());
        var logoId = Guid.NewGuid();
        var dto = new BrandCreateDto
        {
            LogoMediaId = logoId,
            Translations = new List<BrandTranslationDto>
            {
                new() { Culture = "en-US", Name = "Acme" }
            }
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var brand = db.Set<Brand>().Single();
        brand.LogoMediaId.Should().Be(logoId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateBrandHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateBrand_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = CreateDb();
        var handler = new UpdateBrandHandler(db, CreateLocalizer());
        var dto = new BrandEditDto
        {
            Id = Guid.Empty, // violates NotEmpty rule
            RowVersion = new byte[] { 1 },
            Translations = new List<BrandTranslationDto> { new() { Culture = "en-US", Name = "X" } }
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Id must not be empty");
    }

    [Fact]
    public async Task UpdateBrand_Should_Throw_InvalidOperationException_When_Brand_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new UpdateBrandHandler(db, CreateLocalizer());
        var dto = BuildValidBrandEditDto(Guid.NewGuid(), new byte[] { 1 });

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("brand does not exist in the database");
    }

    [Fact]
    public async Task UpdateBrand_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Mismatch()
    {
        await using var db = CreateDb();
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Slug = "old-slug",
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBrandHandler(db, CreateLocalizer());
        var dto = BuildValidBrandEditDto(brand.Id, new byte[] { 9, 9, 9 }); // stale

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "a stale RowVersion triggers a concurrency error");
    }

    [Fact]
    public async Task UpdateBrand_Should_Persist_Updated_Translations()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            RowVersion = rowVersion,
            Translations = new List<BrandTranslation>
            {
                new() { Id = Guid.NewGuid(), Culture = "en-US", Name = "Old Name" }
            }
        };
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBrandHandler(db, CreateLocalizer());
        var dto = BuildValidBrandEditDto(brand.Id, rowVersion);
        dto.Translations[0].Name = "New Name";

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var updated = db.Set<Brand>().Include(b => b.Translations).Single();
        updated.Translations.Should().HaveCount(1);
        updated.Translations[0].Name.Should().Be("New Name");
    }

    [Fact]
    public async Task UpdateBrand_Should_Throw_When_New_Slug_Is_Already_Used_By_Another_Brand()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var brand = new Brand { Id = Guid.NewGuid(), Slug = "my-slug", RowVersion = rowVersion };
        var other = new Brand { Id = Guid.NewGuid(), Slug = "taken-slug", RowVersion = new byte[] { 2 } };
        db.Set<Brand>().AddRange(brand, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBrandHandler(db, CreateLocalizer());
        var dto = BuildValidBrandEditDto(brand.Id, rowVersion, slug: "taken-slug");

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("slug is already taken by another brand");
    }

    [Fact]
    public async Task UpdateBrand_Should_Allow_Same_Slug_For_Same_Brand()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            Slug = "my-slug",
            RowVersion = rowVersion,
            Translations = new List<BrandTranslation>
            {
                new() { Id = Guid.NewGuid(), Culture = "en-US", Name = "Acme" }
            }
        };
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBrandHandler(db, CreateLocalizer());
        // Same slug on same brand should succeed (no change)
        var dto = BuildValidBrandEditDto(brand.Id, rowVersion, slug: "my-slug");

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("same slug for same brand is valid");
    }

    [Fact]
    public async Task UpdateBrand_Should_Trim_Slug()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var brand = new Brand
        {
            Id = Guid.NewGuid(),
            RowVersion = rowVersion,
            Translations = new List<BrandTranslation>
            {
                new() { Id = Guid.NewGuid(), Culture = "en-US", Name = "Acme" }
            }
        };
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBrandHandler(db, CreateLocalizer());
        var dto = BuildValidBrandEditDto(brand.Id, rowVersion, slug: "  trimmed-slug  ");

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var updated = db.Set<Brand>().Single();
        updated.Slug.Should().Be("trimmed-slug");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoftDeleteBrandHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteBrand_Should_Return_Failure_When_Dto_Invalid()
    {
        await using var db = CreateDb();
        var handler = new SoftDeleteBrandHandler(db, CreateLocalizer());
        var dto = new BrandDeleteDto { Id = Guid.Empty, RowVersion = new byte[] { 1 } };

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("an empty Id must fail validation");
    }

    [Fact]
    public async Task SoftDeleteBrand_Should_Return_Failure_When_Brand_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new SoftDeleteBrandHandler(db, CreateLocalizer());
        var dto = new BrandDeleteDto { Id = Guid.NewGuid(), RowVersion = new byte[] { 1 } };

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("brand does not exist");
    }

    [Fact]
    public async Task SoftDeleteBrand_Should_Return_Failure_On_RowVersion_Mismatch()
    {
        await using var db = CreateDb();
        var brand = new Brand { Id = Guid.NewGuid(), RowVersion = new byte[] { 1, 2, 3 } };
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBrandHandler(db, CreateLocalizer());
        var dto = new BrandDeleteDto { Id = brand.Id, RowVersion = new byte[] { 9, 9, 9 } };

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must trigger concurrency failure");
    }

    [Fact]
    public async Task SoftDeleteBrand_Should_Set_IsDeleted_When_Valid()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var brand = new Brand { Id = Guid.NewGuid(), RowVersion = rowVersion };
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBrandHandler(db, CreateLocalizer());
        var dto = new BrandDeleteDto { Id = brand.Id, RowVersion = rowVersion };

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var deleted = db.Set<Brand>().Single();
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteBrand_Should_Be_Idempotent_When_Already_Deleted()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var brand = new Brand { Id = Guid.NewGuid(), RowVersion = rowVersion, IsDeleted = true };
        db.Set<Brand>().Add(brand);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteBrandHandler(db, CreateLocalizer());
        var dto = new BrandDeleteDto { Id = brand.Id, RowVersion = rowVersion };

        var result = await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("deleting an already-deleted brand is idempotent");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateCategoryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_Should_Return_New_Id_And_Persist_Category()
    {
        await using var db = CreateDb();
        var validator = new CategoryCreateDtoValidator(CreateLocalizer());
        var handler = new CreateCategoryHandler(db, validator);

        var id = await handler.HandleAsync(BuildValidCategoryCreateDto(), TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty("handler must return a valid GUID");
        var category = db.Set<Category>().Include(c => c.Translations).Single();
        category.Id.Should().Be(id);
        category.Translations.Should().HaveCount(1);
        category.Translations[0].Culture.Should().Be("en-US");
        category.Translations[0].Name.Should().Be("Electronics");
        category.Translations[0].Slug.Should().Be("electronics");
    }

    [Fact]
    public async Task CreateCategory_Should_Throw_ValidationException_When_Translations_Empty()
    {
        await using var db = CreateDb();
        var validator = new CategoryCreateDtoValidator(CreateLocalizer());
        var handler = new CreateCategoryHandler(db, validator);
        var dto = new CategoryCreateDto { Translations = new List<CategoryTranslationDto>() };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("at least one translation is required");
    }

    [Fact]
    public async Task CreateCategory_Should_Throw_When_Translation_Culture_Empty()
    {
        await using var db = CreateDb();
        var validator = new CategoryCreateDtoValidator(CreateLocalizer());
        var handler = new CreateCategoryHandler(db, validator);
        var dto = new CategoryCreateDto
        {
            Translations = new List<CategoryTranslationDto>
            {
                new() { Culture = "", Name = "Electronics", Slug = "electronics" }
            }
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Culture is required");
    }

    [Fact]
    public async Task CreateCategory_Should_Set_ParentId_When_Provided()
    {
        await using var db = CreateDb();
        var validator = new CategoryCreateDtoValidator(CreateLocalizer());
        var handler = new CreateCategoryHandler(db, validator);
        var parentId = Guid.NewGuid();
        var dto = BuildValidCategoryCreateDto();
        dto.ParentId = parentId;

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var category = db.Set<Category>().Single();
        category.ParentId.Should().Be(parentId);
    }

    [Fact]
    public async Task CreateCategory_Should_Set_IsActive_And_SortOrder()
    {
        await using var db = CreateDb();
        var validator = new CategoryCreateDtoValidator(CreateLocalizer());
        var handler = new CreateCategoryHandler(db, validator);
        var dto = BuildValidCategoryCreateDto();
        dto.IsActive = false;
        dto.SortOrder = 5;

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var category = db.Set<Category>().Single();
        category.IsActive.Should().BeFalse();
        category.SortOrder.Should().Be(5);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateCategoryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategory_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = CreateDb();
        var validator = new CategoryEditDtoValidator(CreateLocalizer());
        var handler = new UpdateCategoryHandler(db, validator, CreateLocalizer());
        var dto = new CategoryEditDto
        {
            Id = Guid.Empty, // invalid
            RowVersion = new byte[] { 1 },
            Translations = new List<CategoryTranslationDto>()
        };

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("Id must not be empty");
    }

    [Fact]
    public async Task UpdateCategory_Should_Throw_ValidationException_When_Category_Not_Found()
    {
        await using var db = CreateDb();
        var validator = new CategoryEditDtoValidator(CreateLocalizer());
        var handler = new UpdateCategoryHandler(db, validator, CreateLocalizer());
        var dto = BuildValidCategoryEditDto(Guid.NewGuid(), new byte[] { 1 });

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("category does not exist");
    }

    [Fact]
    public async Task UpdateCategory_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Mismatch()
    {
        await using var db = CreateDb();
        var category = new Category
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1, 2, 3 }
        };
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var validator = new CategoryEditDtoValidator(CreateLocalizer());
        var handler = new UpdateCategoryHandler(db, validator, CreateLocalizer());
        var dto = BuildValidCategoryEditDto(category.Id, new byte[] { 9, 9, 9 }); // stale

        var act = async () => await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "stale RowVersion must trigger concurrency error");
    }

    [Fact]
    public async Task UpdateCategory_Should_Replace_Translations_And_Persist_Fields()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            RowVersion = rowVersion,
            IsActive = true,
            SortOrder = 0,
            Translations = new List<CategoryTranslation>
            {
                new() { Id = Guid.NewGuid(), Culture = "en-US", Name = "Old Name", Slug = "old-slug" }
            }
        };
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var validator = new CategoryEditDtoValidator(CreateLocalizer());
        var handler = new UpdateCategoryHandler(db, validator, CreateLocalizer());
        var dto = BuildValidCategoryEditDto(category.Id, rowVersion);
        dto.IsActive = false;
        dto.SortOrder = 10;

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var updated = db.Set<Category>().Include(c => c.Translations).Single();
        updated.IsActive.Should().BeFalse();
        updated.SortOrder.Should().Be(10);
        updated.Translations.Should().HaveCount(1);
        updated.Translations[0].Name.Should().Be("Electronics Updated");
        updated.Translations[0].Slug.Should().Be("electronics-updated");
    }

    [Fact]
    public async Task UpdateCategory_Should_Update_ParentId()
    {
        await using var db = CreateDb();
        var rowVersion = new byte[] { 1 };
        var parentId = Guid.NewGuid();
        var category = new Category
        {
            Id = Guid.NewGuid(),
            RowVersion = rowVersion,
            Translations = new List<CategoryTranslation>
            {
                new() { Id = Guid.NewGuid(), Culture = "en-US", Name = "Old", Slug = "old" }
            }
        };
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var validator = new CategoryEditDtoValidator(CreateLocalizer());
        var handler = new UpdateCategoryHandler(db, validator, CreateLocalizer());
        var dto = BuildValidCategoryEditDto(category.Id, rowVersion);
        dto.ParentId = parentId;

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var updated = db.Set<Category>().Single();
        updated.ParentId.Should().Be(parentId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoftDeleteCategoryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteCategory_Should_Return_Failure_When_Category_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new SoftDeleteCategoryHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("category does not exist");
    }

    [Fact]
    public async Task SoftDeleteCategory_Should_Set_IsDeleted_When_Valid()
    {
        await using var db = CreateDb();
        var category = new Category { Id = Guid.NewGuid() };
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        category.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteCategoryHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(category.Id, fakeRowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var deleted = db.Set<Category>().Single();
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeleteCategory_Should_ReturnCategoryNotFound_WhenAlreadyDeleted()
    {
        await using var db = CreateDb();
        var category = new Category { Id = Guid.NewGuid(), IsDeleted = true };
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteCategoryHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(category.Id, new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("deleted categories are not found by the handler");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // In-memory DbContext for Catalog handler tests
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class CatalogTestDbContext : DbContext, IAppDbContext
    {
        private CatalogTestDbContext(DbContextOptions<CatalogTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CatalogTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CatalogTestDbContext>()
                .UseInMemoryDatabase($"darwin_catalog_{Guid.NewGuid()}")
                .Options;
            return new CatalogTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Brand>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Slug).HasMaxLength(256);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Translations)
                    .WithOne()
                    .HasForeignKey(t => t.BrandId);
            });

            modelBuilder.Entity<BrandTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(16).IsRequired();
                b.Property(x => x.Name).HasMaxLength(256).IsRequired();
                b.Property(x => x.DescriptionHtml);
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<Category>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.IsActive);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.SortOrder);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Translations)
                    .WithOne()
                    .HasForeignKey(t => t.CategoryId);
            });

            modelBuilder.Entity<CategoryTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(10).IsRequired();
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
                b.Property(x => x.IsDeleted);
            });
        }
    }
}
