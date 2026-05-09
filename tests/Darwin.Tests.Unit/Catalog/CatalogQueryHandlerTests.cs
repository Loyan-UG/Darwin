using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Catalog.DTOs;
using Darwin.Application.Catalog.Queries;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.Pricing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Catalog;

/// <summary>
/// Unit tests for catalog read-side query handlers:
/// GetBrandsPageHandler, GetBrandOpsSummaryHandler, GetBrandForEditHandler,
/// GetCategoriesPageHandler, GetCategoryOpsSummaryHandler, GetCategoryForEditHandler,
/// GetProductsPageHandler, GetProductOpsSummaryHandler, GetProductForEditHandler,
/// GetVariantsPageHandler, GetCatalogLookupsHandler,
/// GetAddOnGroupsPageHandler, GetAddOnGroupOpsSummaryHandler, GetAddOnGroupForEditHandler,
/// GetAddOnGroupAttachedBrandIdsHandler, GetAddOnGroupAttachedCategoryIdsHandler,
/// GetAddOnGroupAttachedProductIdsHandler, GetAddOnGroupAttachedVariantIdsHandler.
/// </summary>
public sealed class CatalogQueryHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static CatalogQueryTestDbContext CreateDb() => CatalogQueryTestDbContext.Create();

    // ─────────────────────────────────────────────────────────────────────────
    // GetBrandsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBrandsPage_Should_Return_Empty_When_No_Brands()
    {
        await using var db = CreateDb();
        var handler = new GetBrandsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, "en-US", ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBrandsPage_Should_Exclude_Soft_Deleted_Brands()
    {
        await using var db = CreateDb();
        db.Set<Brand>().AddRange(
            new Brand { Id = Guid.NewGuid(), IsDeleted = false, Translations = new List<BrandTranslation> { new() { Culture = "en-US", Name = "Active" } } },
            new Brand { Id = Guid.NewGuid(), IsDeleted = true, Translations = new List<BrandTranslation> { new() { Culture = "en-US", Name = "Deleted" } } }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBrandsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "en-US", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetBrandsPage_Should_Filter_By_Unpublished()
    {
        await using var db = CreateDb();
        db.Set<Brand>().AddRange(
            new Brand { Id = Guid.NewGuid(), IsPublished = true, Translations = new List<BrandTranslation> { new() { Culture = "en-US", Name = "Published" } } },
            new Brand { Id = Guid.NewGuid(), IsPublished = false, Translations = new List<BrandTranslation> { new() { Culture = "en-US", Name = "Unpublished" } } }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBrandsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "en-US", query: null, filter: "unpublished", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Unpublished");
    }

    [Fact]
    public async Task GetBrandsPage_Should_Clamp_Invalid_Page_To_One()
    {
        await using var db = CreateDb();
        db.Set<Brand>().Add(new Brand { Id = Guid.NewGuid(), Translations = new List<BrandTranslation> { new() { Culture = "en-US", Name = "Acme" } } });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBrandsPageHandler(db);
        var (items, _) = await handler.HandleAsync(-5, 20, "en-US", ct: TestContext.Current.CancellationToken);

        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBrandsPage_Should_Map_Fields_Correctly()
    {
        await using var db = CreateDb();
        var brandId = Guid.NewGuid();
        var logo = Guid.NewGuid();
        db.Set<Brand>().Add(new Brand
        {
            Id = brandId,
            Slug = "acme",
            LogoMediaId = logo,
            IsPublished = true,
            Translations = new List<BrandTranslation> { new() { Culture = "en-US", Name = "Acme Corp" } }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBrandsPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, "en-US", ct: TestContext.Current.CancellationToken);

        var item = items.Single();
        item.Id.Should().Be(brandId);
        item.Slug.Should().Be("acme");
        item.LogoMediaId.Should().Be(logo);
        item.IsPublished.Should().BeTrue();
        item.Name.Should().Be("Acme Corp");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetBrandOpsSummaryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBrandOpsSummary_Should_Return_Zero_Counts_When_Empty()
    {
        await using var db = CreateDb();
        var handler = new GetBrandOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.UnpublishedCount.Should().Be(0);
        summary.MissingSlugCount.Should().Be(0);
        summary.MissingLogoCount.Should().Be(0);
    }

    [Fact]
    public async Task GetBrandOpsSummary_Should_Return_Correct_Counts()
    {
        await using var db = CreateDb();
        db.Set<Brand>().AddRange(
            new Brand { Id = Guid.NewGuid(), IsPublished = true, Slug = "a", LogoMediaId = Guid.NewGuid(), Translations = new List<BrandTranslation>() },
            new Brand { Id = Guid.NewGuid(), IsPublished = false, Slug = null, LogoMediaId = null, Translations = new List<BrandTranslation>() },
            new Brand { Id = Guid.NewGuid(), IsDeleted = true, IsPublished = true, Slug = "c", LogoMediaId = Guid.NewGuid(), Translations = new List<BrandTranslation>() }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBrandOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2, "soft-deleted excluded");
        summary.UnpublishedCount.Should().Be(1);
        summary.MissingSlugCount.Should().Be(1);
        summary.MissingLogoCount.Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetBrandForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBrandForEdit_Should_Return_Null_When_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new GetBrandForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBrandForEdit_Should_Return_Null_When_Soft_Deleted()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Set<Brand>().Add(new Brand { Id = id, IsDeleted = true, Translations = new List<BrandTranslation>() });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBrandForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted brands are not editable");
    }

    [Fact]
    public async Task GetBrandForEdit_Should_Return_Correct_Projection()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        db.Set<Brand>().Add(new Brand
        {
            Id = id,
            Slug = "acme",
            RowVersion = rowVersion,
            Translations = new List<BrandTranslation>
            {
                new() { Culture = "en-US", Name = "Acme", DescriptionHtml = "<p>Hi</p>" },
                new() { Culture = "de-DE", Name = "Akme" }
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBrandForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Slug.Should().Be("acme");
        result.Translations.Should().HaveCount(2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCategoriesPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesPage_Should_Return_Empty_When_No_Categories()
    {
        await using var db = CreateDb();
        var handler = new GetCategoriesPageHandler(db);

        var (items, total) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategoriesPage_Should_Exclude_Soft_Deleted()
    {
        await using var db = CreateDb();
        db.Set<Category>().AddRange(
            new Category { Id = Guid.NewGuid(), IsDeleted = false, Translations = new List<CategoryTranslation> { new() { Culture = "en-US", Name = "Active", Slug = "active" } } },
            new Category { Id = Guid.NewGuid(), IsDeleted = true, Translations = new List<CategoryTranslation> { new() { Culture = "en-US", Name = "Gone", Slug = "gone" } } }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCategoriesPageHandler(db);
        var (items, total) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetCategoriesPage_Should_Filter_Inactive()
    {
        await using var db = CreateDb();
        db.Set<Category>().AddRange(
            new Category { Id = Guid.NewGuid(), IsActive = true, Translations = new List<CategoryTranslation> { new() { Culture = "en-US", Name = "On", Slug = "on" } } },
            new Category { Id = Guid.NewGuid(), IsActive = false, Translations = new List<CategoryTranslation> { new() { Culture = "en-US", Name = "Off", Slug = "off" } } }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCategoriesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "en-US", null, "inactive", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Off");
    }

    [Fact]
    public async Task GetCategoriesPage_Should_Filter_Root_Categories()
    {
        await using var db = CreateDb();
        var parentId = Guid.NewGuid();
        db.Set<Category>().AddRange(
            new Category { Id = parentId, ParentId = null, Translations = new List<CategoryTranslation> { new() { Culture = "en-US", Name = "Root", Slug = "root" } } },
            new Category { Id = Guid.NewGuid(), ParentId = parentId, Translations = new List<CategoryTranslation> { new() { Culture = "en-US", Name = "Child", Slug = "child" } } }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCategoriesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "en-US", null, "root", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Root");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCategoryOpsSummaryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoryOpsSummary_Should_Return_Zero_Counts_When_Empty()
    {
        await using var db = CreateDb();
        var handler = new GetCategoryOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.InactiveCount.Should().Be(0);
        summary.RootCount.Should().Be(0);
        summary.ChildCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCategoryOpsSummary_Should_Return_Correct_Counts()
    {
        await using var db = CreateDb();
        var parentId = Guid.NewGuid();
        db.Set<Category>().AddRange(
            new Category { Id = parentId, IsActive = true, ParentId = null, IsPublished = true },
            new Category { Id = Guid.NewGuid(), IsActive = false, ParentId = parentId, IsPublished = false },
            new Category { Id = Guid.NewGuid(), IsDeleted = true } // excluded
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCategoryOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2);
        summary.InactiveCount.Should().Be(1);
        summary.RootCount.Should().Be(1);
        summary.ChildCount.Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCategoryForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategoryForEdit_Should_Return_Null_When_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new GetCategoryForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCategoryForEdit_Should_Return_Correct_Projection()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 7, 8, 9 };
        db.Set<Category>().Add(new Category
        {
            Id = id,
            ParentId = Guid.NewGuid(),
            IsActive = true,
            SortOrder = 5,
            RowVersion = rowVersion,
            Translations = new List<CategoryTranslation>
            {
                new() { Culture = "en-US", Name = "Electronics", Slug = "electronics" }
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCategoryForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.IsActive.Should().BeTrue();
        result.SortOrder.Should().Be(5);
        result.Translations.Should().HaveCount(1);
        result.Translations[0].Name.Should().Be("Electronics");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetProductsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductsPage_Should_Return_Empty_When_No_Products()
    {
        await using var db = CreateDb();
        var handler = new GetProductsPageHandler(db);

        var (items, total) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductsPage_Should_Exclude_Soft_Deleted_Products()
    {
        await using var db = CreateDb();
        db.Set<Product>().AddRange(
            new Product { Id = Guid.NewGuid(), IsDeleted = false, Translations = new List<ProductTranslation> { new() { Culture = "en-US", Name = "Widget", Slug = "widget" } } },
            new Product { Id = Guid.NewGuid(), IsDeleted = true, Translations = new List<ProductTranslation> { new() { Culture = "en-US", Name = "Gone", Slug = "gone" } } }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductsPageHandler(db);
        var (items, total) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().DefaultName.Should().Be("Widget");
    }

    [Fact]
    public async Task GetProductsPage_Should_Filter_Inactive()
    {
        await using var db = CreateDb();
        db.Set<Product>().AddRange(
            new Product { Id = Guid.NewGuid(), IsActive = true, Translations = new List<ProductTranslation> { new() { Culture = "en-US", Name = "Active Widget", Slug = "aw" } } },
            new Product { Id = Guid.NewGuid(), IsActive = false, Translations = new List<ProductTranslation> { new() { Culture = "en-US", Name = "Inactive Widget", Slug = "iw" } } }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "en-US", null, "inactive", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().DefaultName.Should().Be("Inactive Widget");
    }

    [Fact]
    public async Task GetProductsPage_Should_Count_Variants_Correctly()
    {
        await using var db = CreateDb();
        var productId = Guid.NewGuid();
        db.Set<Product>().Add(new Product
        {
            Id = productId,
            Translations = new List<ProductTranslation> { new() { Culture = "en-US", Name = "Multi", Slug = "multi" } },
            Variants = new List<ProductVariant>
            {
                new() { Id = Guid.NewGuid(), ProductId = productId, Sku = "SKU-1", Currency = "EUR", IsDeleted = false },
                new() { Id = Guid.NewGuid(), ProductId = productId, Sku = "SKU-2", Currency = "EUR", IsDeleted = false },
                new() { Id = Guid.NewGuid(), ProductId = productId, Sku = "SKU-X", Currency = "EUR", IsDeleted = true }
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductsPageHandler(db);
        var (items, _) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        items.Single().VariantCount.Should().Be(2, "deleted variants are excluded from count");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetProductOpsSummaryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductOpsSummary_Should_Return_Zero_Counts_When_Empty()
    {
        await using var db = CreateDb();
        var handler = new GetProductOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.InactiveCount.Should().Be(0);
        summary.HiddenCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProductOpsSummary_Should_Return_Correct_Counts()
    {
        await using var db = CreateDb();
        db.Set<Product>().AddRange(
            new Product { Id = Guid.NewGuid(), IsActive = true, IsVisible = true },
            new Product { Id = Guid.NewGuid(), IsActive = false, IsVisible = true },
            new Product { Id = Guid.NewGuid(), IsActive = true, IsVisible = false },
            new Product { Id = Guid.NewGuid(), IsDeleted = true } // excluded
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(3);
        summary.InactiveCount.Should().Be(1);
        summary.HiddenCount.Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetProductForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductForEdit_Should_Return_Null_When_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new GetProductForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProductForEdit_Should_Return_Null_When_Soft_Deleted()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Set<Product>().Add(new Product { Id = id, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProductForEdit_Should_Return_Correct_Projection()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 5, 6 };
        var variantId = Guid.NewGuid();
        db.Set<Product>().Add(new Product
        {
            Id = id,
            RowVersion = rowVersion,
            Kind = Darwin.Domain.Enums.ProductKind.Simple,
            Translations = new List<ProductTranslation>
            {
                new() { Culture = "en-US", Name = "Widget", Slug = "widget" }
            },
            Variants = new List<ProductVariant>
            {
                new() { Id = variantId, ProductId = id, Sku = "SKU-001", Currency = "EUR", IsDeleted = false },
                new() { Id = Guid.NewGuid(), ProductId = id, Sku = "SKU-DEL", Currency = "EUR", IsDeleted = true }
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Kind.Should().Be("Simple");
        result.Translations.Should().HaveCount(1);
        result.Variants.Should().HaveCount(1, "soft-deleted variant is excluded");
        result.Variants[0].Sku.Should().Be("SKU-001");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetVariantsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetVariantsPage_Should_Return_Empty_When_No_Variants()
    {
        await using var db = CreateDb();
        var handler = new GetVariantsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, null, "en-US", TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVariantsPage_Should_Exclude_Soft_Deleted_Variants()
    {
        await using var db = CreateDb();
        var productId = Guid.NewGuid();
        db.Set<ProductVariant>().AddRange(
            new ProductVariant { Id = Guid.NewGuid(), ProductId = productId, Sku = "ACTIVE", Currency = "EUR", IsDeleted = false },
            new ProductVariant { Id = Guid.NewGuid(), ProductId = productId, Sku = "GONE", Currency = "EUR", IsDeleted = true }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetVariantsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, null, "en-US", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Sku.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetVariantsPage_Should_Search_By_Sku()
    {
        await using var db = CreateDb();
        var productId = Guid.NewGuid();
        db.Set<ProductVariant>().AddRange(
            new ProductVariant { Id = Guid.NewGuid(), ProductId = productId, Sku = "FOUND-001", Currency = "EUR" },
            new ProductVariant { Id = Guid.NewGuid(), ProductId = productId, Sku = "OTHER-002", Currency = "EUR" }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetVariantsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "FOUND", "en-US", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Sku.Should().Be("FOUND-001");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCatalogLookupsHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCatalogLookups_Should_Return_Empty_When_No_Data()
    {
        await using var db = CreateDb();
        var handler = new GetCatalogLookupsHandler(db);

        var result = await handler.HandleAsync("en-US", TestContext.Current.CancellationToken);

        result.Brands.Should().BeEmpty();
        result.Categories.Should().BeEmpty();
        result.TaxCategories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCatalogLookups_Should_Include_Active_Brands_And_Categories()
    {
        await using var db = CreateDb();
        db.Set<Brand>().Add(new Brand
        {
            Id = Guid.NewGuid(),
            Translations = new List<BrandTranslation> { new() { Culture = "en-US", Name = "BrandA" } }
        });
        db.Set<Category>().Add(new Category
        {
            Id = Guid.NewGuid(),
            Translations = new List<CategoryTranslation> { new() { Culture = "en-US", Name = "CatA", Slug = "cat-a" } }
        });
        db.Set<TaxCategory>().Add(new TaxCategory { Id = Guid.NewGuid(), Name = "Standard", VatRate = 0.19m });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCatalogLookupsHandler(db);
        var result = await handler.HandleAsync("en-US", TestContext.Current.CancellationToken);

        result.Brands.Should().HaveCount(1);
        result.Brands[0].Name.Should().Be("BrandA");
        result.Categories.Should().HaveCount(1);
        result.TaxCategories.Should().HaveCount(1);
        result.TaxCategories[0].Name.Should().Contain("Standard");
    }

    [Fact]
    public async Task GetCatalogLookups_Should_Exclude_Soft_Deleted_Brands_And_Categories()
    {
        await using var db = CreateDb();
        db.Set<Brand>().Add(new Brand { Id = Guid.NewGuid(), IsDeleted = true, Translations = new List<BrandTranslation>() });
        db.Set<Category>().Add(new Category { Id = Guid.NewGuid(), IsDeleted = true });
        db.Set<TaxCategory>().Add(new TaxCategory { Id = Guid.NewGuid(), Name = "Deleted Tax", VatRate = 0m, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCatalogLookupsHandler(db);
        var result = await handler.HandleAsync("en-US", TestContext.Current.CancellationToken);

        result.Brands.Should().BeEmpty();
        result.Categories.Should().BeEmpty();
        result.TaxCategories.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAddOnGroupsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddOnGroupsPage_Should_Return_Empty_When_No_Groups()
    {
        await using var db = CreateDb();
        var handler = new GetAddOnGroupsPageHandler(db);

        var (items, total) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddOnGroupsPage_Should_Exclude_Soft_Deleted_Groups()
    {
        await using var db = CreateDb();
        db.Set<AddOnGroup>().AddRange(
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Gift Wrap", Currency = "EUR", IsDeleted = false },
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Deleted Group", Currency = "EUR", IsDeleted = true }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupsPageHandler(db);
        var (items, total) = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Gift Wrap");
    }

    [Fact]
    public async Task GetAddOnGroupsPage_Should_Filter_Inactive()
    {
        await using var db = CreateDb();
        db.Set<AddOnGroup>().AddRange(
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Active Group", Currency = "EUR", IsActive = true },
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Inactive Group", Currency = "EUR", IsActive = false }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupsPageHandler(db);
        var (items, total) = await handler.HandleAsync(filter: AddOnGroupQueueFilter.Inactive, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Inactive Group");
    }

    [Fact]
    public async Task GetAddOnGroupsPage_Should_Filter_Global()
    {
        await using var db = CreateDb();
        db.Set<AddOnGroup>().AddRange(
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Global", Currency = "EUR", IsGlobal = true },
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Specific", Currency = "EUR", IsGlobal = false }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupsPageHandler(db);
        var (items, total) = await handler.HandleAsync(filter: AddOnGroupQueueFilter.Global, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Name.Should().Be("Global");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAddOnGroupOpsSummaryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddOnGroupOpsSummary_Should_Return_Zero_Counts_When_Empty()
    {
        await using var db = CreateDb();
        var handler = new GetAddOnGroupOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.InactiveCount.Should().Be(0);
        summary.GlobalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAddOnGroupOpsSummary_Should_Return_Correct_Counts()
    {
        await using var db = CreateDb();
        db.Set<AddOnGroup>().AddRange(
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Global Active", Currency = "EUR", IsActive = true, IsGlobal = true },
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Local Inactive", Currency = "EUR", IsActive = false, IsGlobal = false },
            new AddOnGroup { Id = Guid.NewGuid(), Name = "Deleted", Currency = "EUR", IsDeleted = true }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(ct: TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2, "soft-deleted excluded");
        summary.InactiveCount.Should().Be(1);
        summary.GlobalCount.Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAddOnGroupForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddOnGroupForEdit_Should_Return_Null_When_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new GetAddOnGroupForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAddOnGroupForEdit_Should_Return_Null_When_Soft_Deleted()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = id, Name = "Deleted", Currency = "EUR", IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAddOnGroupForEdit_Should_Return_Full_Aggregate()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup
        {
            Id = id,
            Name = "Gift Wrap",
            Currency = "EUR",
            IsGlobal = false,
            IsActive = true,
            Translations = new List<AddOnGroupTranslation>
            {
                new() { Culture = "en-US", Name = "Gift Wrapping" }
            },
            Options = new List<AddOnOption>
            {
                new AddOnOption
                {
                    Id = optionId,
                    Label = "Style",
                    SortOrder = 1,
                    Values = new List<AddOnOptionValue>
                    {
                        new AddOnOptionValue { Id = valueId, Label = "Classic", PriceDeltaMinor = 150, SortOrder = 1, IsActive = true }
                    }
                }
            }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupForEditHandler(db);
        var result = await handler.HandleAsync(id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Gift Wrap");
        result.Currency.Should().Be("EUR");
        result.Translations.Should().HaveCount(1);
        result.Options.Should().HaveCount(1);
        result.Options[0].Values.Should().HaveCount(1);
        result.Options[0].Values[0].PriceDeltaMinor.Should().Be(150);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAddOnGroupAttachedBrandIdsHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddOnGroupAttachedBrandIds_Should_Return_Empty_When_Group_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new GetAddOnGroupAttachedBrandIdsHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddOnGroupAttachedBrandIds_Should_Return_Attached_Brand_Ids()
    {
        await using var db = CreateDb();
        var groupId = Guid.NewGuid();
        var brandId1 = Guid.NewGuid();
        var brandId2 = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = groupId, Name = "G", Currency = "EUR" });
        db.Set<AddOnGroupBrand>().AddRange(
            new AddOnGroupBrand { Id = Guid.NewGuid(), AddOnGroupId = groupId, BrandId = brandId1, IsDeleted = false },
            new AddOnGroupBrand { Id = Guid.NewGuid(), AddOnGroupId = groupId, BrandId = brandId2, IsDeleted = true }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupAttachedBrandIdsHandler(db);
        var result = await handler.HandleAsync(groupId, TestContext.Current.CancellationToken);

        result.Should().ContainSingle(id => id == brandId1, "only non-deleted attachments are returned");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAddOnGroupAttachedCategoryIdsHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddOnGroupAttachedCategoryIds_Should_Return_Empty_When_Group_Not_Found()
    {
        await using var db = CreateDb();
        var handler = new GetAddOnGroupAttachedCategoryIdsHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddOnGroupAttachedCategoryIds_Should_Return_Attached_Category_Ids()
    {
        await using var db = CreateDb();
        var groupId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = groupId, Name = "G", Currency = "EUR" });
        db.Set<AddOnGroupCategory>().Add(new AddOnGroupCategory { Id = Guid.NewGuid(), AddOnGroupId = groupId, CategoryId = catId, IsDeleted = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupAttachedCategoryIdsHandler(db);
        var result = await handler.HandleAsync(groupId, TestContext.Current.CancellationToken);

        result.Should().ContainSingle(id => id == catId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAddOnGroupAttachedProductIdsHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddOnGroupAttachedProductIds_Should_Return_Attached_Product_Ids()
    {
        await using var db = CreateDb();
        var groupId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = groupId, Name = "G", Currency = "EUR" });
        db.Set<AddOnGroupProduct>().Add(new AddOnGroupProduct { Id = Guid.NewGuid(), AddOnGroupId = groupId, ProductId = productId, IsDeleted = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupAttachedProductIdsHandler(db);
        var result = await handler.HandleAsync(groupId, TestContext.Current.CancellationToken);

        result.Should().ContainSingle(id => id == productId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetAddOnGroupAttachedVariantIdsHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAddOnGroupAttachedVariantIds_Should_Return_Attached_Variant_Ids()
    {
        await using var db = CreateDb();
        var groupId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.Set<AddOnGroup>().Add(new AddOnGroup { Id = groupId, Name = "G", Currency = "EUR" });
        db.Set<AddOnGroupVariant>().Add(new AddOnGroupVariant { Id = Guid.NewGuid(), AddOnGroupId = groupId, VariantId = variantId, IsDeleted = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetAddOnGroupAttachedVariantIdsHandler(db);
        var result = await handler.HandleAsync(groupId, TestContext.Current.CancellationToken);

        result.Should().ContainSingle(id => id == variantId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared in-memory DbContext
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class CatalogQueryTestDbContext : DbContext, IAppDbContext
    {
        private CatalogQueryTestDbContext(DbContextOptions<CatalogQueryTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static CatalogQueryTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CatalogQueryTestDbContext>()
                .UseInMemoryDatabase($"darwin_catalog_query_{Guid.NewGuid()}")
                .Options;
            return new CatalogQueryTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Brand>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Slug).HasMaxLength(256);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.IsPublished);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Translations).WithOne().HasForeignKey(t => t.BrandId);
            });

            modelBuilder.Entity<BrandTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(16).IsRequired();
                b.Property(x => x.Name).HasMaxLength(256).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<Category>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.IsActive);
                b.Property(x => x.IsPublished);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.SortOrder);
                b.Property(x => x.ParentId);
                b.Property(x => x.RowVersion).IsRowVersion();
                b.HasMany(x => x.Translations).WithOne().HasForeignKey(t => t.CategoryId);
            });

            modelBuilder.Entity<CategoryTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Culture).HasMaxLength(10).IsRequired();
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
                b.Property(x => x.IsDeleted);
            });

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

            modelBuilder.Entity<TaxCategory>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).HasMaxLength(200).IsRequired();
                b.Property(x => x.VatRate).HasPrecision(5, 4);
                b.Property(x => x.IsDeleted);
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

            modelBuilder.Entity<AddOnGroupBrand>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.AddOnGroupId).IsRequired();
                b.Property(x => x.BrandId).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<AddOnGroupCategory>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.AddOnGroupId).IsRequired();
                b.Property(x => x.CategoryId).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<AddOnGroupProduct>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.AddOnGroupId).IsRequired();
                b.Property(x => x.ProductId).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<AddOnGroupVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.AddOnGroupId).IsRequired();
                b.Property(x => x.VariantId).IsRequired();
                b.Property(x => x.IsDeleted);
            });
        }
    }
}
