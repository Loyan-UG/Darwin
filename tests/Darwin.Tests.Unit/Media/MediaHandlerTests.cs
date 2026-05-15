using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.CMS.Media.Commands;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Queries;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.Media;

/// <summary>
/// Handler-level unit tests for the Media module.
/// Covers <see cref="CreateMediaAssetHandler"/>, <see cref="UpdateMediaAssetHandler"/>,
/// <see cref="SoftDeleteMediaAssetHandler"/>, <see cref="GetMediaAssetForEditHandler"/>,
/// <see cref="GetMediaAssetsPageHandler"/>, and <see cref="GetMediaAssetOpsSummaryHandler"/>.
/// </summary>
public sealed class MediaHandlerTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static IStringLocalizer<Darwin.Application.ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<Darwin.Application.ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    private static MediaAsset BuildAsset(
        string url = "https://cdn.example.com/image.jpg",
        string originalFileName = "image.jpg",
        long sizeBytes = 1024,
        byte[]? rowVersion = null,
        bool isDeleted = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            Url = url,
            OriginalFileName = originalFileName,
            Alt = string.Empty,
            SizeBytes = sizeBytes,
            RowVersion = rowVersion ?? new byte[] { 1 },
            IsDeleted = isDeleted
        };

    // ─────────────────────────────────────────────────────────────────────────
    // CreateMediaAssetHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMediaAsset_Should_Throw_ValidationException_When_Url_Empty()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new CreateMediaAssetHandler(db);

        var act = () => handler.HandleAsync(
            new MediaAssetCreateDto { Url = "", OriginalFileName = "file.jpg", SizeBytes = 1024 },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty URL violates the create validator");
    }

    [Fact]
    public async Task CreateMediaAsset_Should_Throw_ValidationException_When_OriginalFileName_Empty()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new CreateMediaAssetHandler(db);

        var act = () => handler.HandleAsync(
            new MediaAssetCreateDto { Url = "https://cdn.example.com/img.jpg", OriginalFileName = "", SizeBytes = 512 },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty OriginalFileName violates the create validator");
    }

    [Fact]
    public async Task CreateMediaAsset_Should_Persist_Asset_Successfully()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new CreateMediaAssetHandler(db);

        await handler.HandleAsync(new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/photo.png",
            OriginalFileName = "photo.png",
            Alt = "A landscape",
            Title = "Landscape",
            SizeBytes = 204800,
            Width = 1920,
            Height = 1080,
            Role = "hero"
        }, TestContext.Current.CancellationToken);

        var asset = db.Set<MediaAsset>().Single();
        asset.Url.Should().Be("https://cdn.example.com/photo.png");
        asset.OriginalFileName.Should().Be("photo.png");
        asset.Alt.Should().Be("A landscape");
        asset.Title.Should().Be("Landscape");
        asset.SizeBytes.Should().Be(204800);
        asset.Width.Should().Be(1920);
        asset.Height.Should().Be(1080);
        asset.Role.Should().Be("hero");
    }

    [Fact]
    public async Task CreateMediaAsset_Should_Trim_Url_And_OriginalFileName()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new CreateMediaAssetHandler(db);

        await handler.HandleAsync(new MediaAssetCreateDto
        {
            Url = "  https://cdn.example.com/img.png  ",
            OriginalFileName = "  img.png  ",
            SizeBytes = 0
        }, TestContext.Current.CancellationToken);

        var asset = db.Set<MediaAsset>().Single();
        asset.Url.Should().Be("https://cdn.example.com/img.png", "URL should be trimmed");
        asset.OriginalFileName.Should().Be("img.png", "OriginalFileName should be trimmed");
    }

    [Fact]
    public async Task CreateMediaAsset_Should_Use_Empty_String_For_Null_Alt()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new CreateMediaAssetHandler(db);

        await handler.HandleAsync(new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/no-alt.jpg",
            OriginalFileName = "no-alt.jpg",
            Alt = null!, // Alt is non-nullable in DTO but can be empty
            SizeBytes = 100
        }, TestContext.Current.CancellationToken);

        var asset = db.Set<MediaAsset>().Single();
        asset.Alt.Should().Be(string.Empty, "null alt should default to empty string");
    }

    [Fact]
    public async Task CreateMediaAsset_Should_Store_Null_Title_When_Title_Is_Whitespace()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new CreateMediaAssetHandler(db);

        await handler.HandleAsync(new MediaAssetCreateDto
        {
            Url = "https://cdn.example.com/img.jpg",
            OriginalFileName = "img.jpg",
            Title = "   ",
            SizeBytes = 100
        }, TestContext.Current.CancellationToken);

        var asset = db.Set<MediaAsset>().Single();
        asset.Title.Should().BeNull("whitespace-only title should be stored as null");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateMediaAssetHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMediaAsset_Should_Throw_ValidationException_When_Id_Empty()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new UpdateMediaAssetHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(
            new MediaAssetEditDto { Id = Guid.Empty, RowVersion = new byte[] { 1 }, Alt = "alt" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty Id violates the edit validator");
    }

    [Fact]
    public async Task UpdateMediaAsset_Should_Throw_InvalidOperationException_When_Asset_Not_Found()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new UpdateMediaAssetHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(
            new MediaAssetEditDto { Id = Guid.NewGuid(), RowVersion = new byte[] { 1 }, Alt = "alt" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("updating a non-existent asset should fail");
    }

    [Fact]
    public async Task UpdateMediaAsset_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Mismatch()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1, 2, 3, 4 });
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateMediaAssetHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(
            new MediaAssetEditDto { Id = asset.Id, RowVersion = new byte[] { 9, 9 }, Alt = "new alt" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("a stale RowVersion must trigger a concurrency exception");
    }

    [Fact]
    public async Task UpdateMediaAsset_Should_Throw_ValidationException_When_RowVersion_Is_Empty()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1, 2, 3 });
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateMediaAssetHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(
            new MediaAssetEditDto { Id = asset.Id, RowVersion = Array.Empty<byte>(), Alt = "new alt" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty RowVersion must be rejected before the concurrency check");
    }

    [Fact]
    public async Task UpdateMediaAsset_Should_Persist_Changes_Successfully()
    {
        await using var db = MediaTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var asset = BuildAsset(rowVersion: rowVersion);
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateMediaAssetHandler(db, CreateLocalizer());
        await handler.HandleAsync(new MediaAssetEditDto
        {
            Id = asset.Id,
            RowVersion = rowVersion,
            Alt = "Updated alt",
            Title = "Updated title",
            Role = "thumbnail"
        }, TestContext.Current.CancellationToken);

        var updated = db.Set<MediaAsset>().Single();
        updated.Alt.Should().Be("Updated alt");
        updated.Title.Should().Be("Updated title");
        updated.Role.Should().Be("thumbnail");
    }

    [Fact]
    public async Task UpdateMediaAsset_Should_Not_Update_Soft_Deleted_Asset()
    {
        await using var db = MediaTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var asset = BuildAsset(rowVersion: rowVersion, isDeleted: true);
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateMediaAssetHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(
            new MediaAssetEditDto { Id = asset.Id, RowVersion = rowVersion, Alt = "updated" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("soft-deleted assets must be treated as not found");
    }

    [Fact]
    public async Task UpdateMediaAsset_Should_Set_Title_To_Null_When_Title_Is_Whitespace()
    {
        await using var db = MediaTestDbContext.Create();
        var rowVersion = new byte[] { 5 };
        var asset = BuildAsset(rowVersion: rowVersion);
        asset.Title = "Old Title";
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateMediaAssetHandler(db, CreateLocalizer());
        await handler.HandleAsync(new MediaAssetEditDto
        {
            Id = asset.Id,
            RowVersion = rowVersion,
            Alt = "alt",
            Title = "  "
        }, TestContext.Current.CancellationToken);

        var updated = db.Set<MediaAsset>().Single();
        updated.Title.Should().BeNull("whitespace title should be stored as null on update");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SoftDeleteMediaAssetHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteMediaAsset_Should_Mark_Asset_As_Deleted()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset();
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        asset.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, fakeRowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        var entity = db.Set<MediaAsset>().Single();
        entity.IsDeleted.Should().BeTrue("the handler must soft-delete the asset");
    }

    [Fact]
    public async Task SoftDeleteMediaAsset_Should_Not_Throw_When_Asset_Not_Found()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new SoftDeleteMediaAssetHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("soft-deleting a missing asset is a no-op");
    }

    [Fact]
    public async Task SoftDeleteMediaAsset_Should_Not_Throw_When_Asset_Already_Deleted()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(isDeleted: true);
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteMediaAssetHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(asset.Id, null, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("handler silently no-ops when asset is already soft-deleted");
    }

    [Fact]
    public async Task SoftDeleteMediaAsset_Should_Return_Fail_WhenIdIsEmpty()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new SoftDeleteMediaAssetHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Guid is not a valid asset identifier");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SoftDeleteMediaAsset_Should_Return_Fail_WhenRowVersionIsNull_ForExistingAsset()
    {
        await using var db = MediaTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var asset = BuildAsset(rowVersion: rowVersion);
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("null RowVersion must be rejected for an existing asset");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SoftDeleteMediaAsset_Should_Return_Fail_WhenRowVersionIsEmpty_ForExistingAsset()
    {
        await using var db = MediaTestDbContext.Create();
        var rowVersion = new byte[] { 1, 2, 3 };
        var asset = BuildAsset(rowVersion: rowVersion);
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, Array.Empty<byte>(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected for an existing asset");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SoftDeleteMediaAsset_Should_Return_Fail_WhenRowVersionIsStale()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1, 2, 3 });
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeleteMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, new byte[] { 9, 9, 9 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must trigger a concurrency conflict");
        result.Error.Should().NotBeNullOrEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMediaAssetForEditHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMediaAssetForEdit_Should_Return_Null_When_Not_Found()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new GetMediaAssetForEditHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("no asset exists with that id");
    }

    [Fact]
    public async Task GetMediaAssetForEdit_Should_Return_Null_When_Asset_Is_Deleted()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(isDeleted: true);
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetForEditHandler(db);
        var result = await handler.HandleAsync(asset.Id, ct: TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted assets must not be returned for editing");
    }

    [Fact]
    public async Task GetMediaAssetForEdit_Should_Return_Correct_Projection()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            Url = "https://cdn.example.com/photo.jpg",
            OriginalFileName = "photo.jpg",
            Alt = "alt text",
            Title = "Photo Title",
            SizeBytes = 4096,
            Width = 800,
            Height = 600,
            Role = "gallery",
            RowVersion = new byte[] { 1 },
            IsDeleted = false
        };
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetForEditHandler(db);
        var result = await handler.HandleAsync(asset.Id, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(asset.Id);
        result.Url.Should().Be(asset.Url);
        result.OriginalFileName.Should().Be(asset.OriginalFileName);
        result.Alt.Should().Be(asset.Alt);
        result.Title.Should().Be(asset.Title);
        result.SizeBytes.Should().Be(asset.SizeBytes);
        result.Width.Should().Be(asset.Width);
        result.Height.Should().Be(asset.Height);
        result.Role.Should().Be(asset.Role);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMediaAssetsPageHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMediaAssetsPage_Should_Return_Empty_When_No_Assets_Exist()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new GetMediaAssetsPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Exclude_Soft_Deleted_Assets()
    {
        await using var db = MediaTestDbContext.Create();
        db.Set<MediaAsset>().AddRange(
            BuildAsset("https://cdn.example.com/a.jpg", "a.jpg"),
            BuildAsset("https://cdn.example.com/b.jpg", "b.jpg", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "soft-deleted assets should not be counted");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Filter_By_Query_Term()
    {
        await using var db = MediaTestDbContext.Create();
        var matching = BuildAsset("https://cdn.example.com/hero.jpg", "hero.jpg");
        matching.Alt = "main hero";
        var nonMatching = BuildAsset("https://cdn.example.com/logo.png", "logo.png");
        db.Set<MediaAsset>().AddRange(matching, nonMatching);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, query: "hero", ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the asset matching the query term should be returned");
        items.Single().OriginalFileName.Should().Be("hero.jpg");
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Apply_MissingAlt_Filter()
    {
        await using var db = MediaTestDbContext.Create();
        var withAlt = BuildAsset("https://cdn.example.com/a.jpg", "a.jpg");
        withAlt.Alt = "has alt";
        var withoutAlt = BuildAsset("https://cdn.example.com/b.jpg", "b.jpg");
        withoutAlt.Alt = string.Empty;
        db.Set<MediaAsset>().AddRange(withAlt, withoutAlt);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: MediaAssetQueueFilter.MissingAlt, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the asset missing alt text should be returned");
        items.Single().OriginalFileName.Should().Be("b.jpg");
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Apply_MissingTitle_Filter()
    {
        await using var db = MediaTestDbContext.Create();
        var withTitle = BuildAsset("https://cdn.example.com/a.jpg", "a.jpg");
        withTitle.Title = "Has Title";
        var withoutTitle = BuildAsset("https://cdn.example.com/b.jpg", "b.jpg");
        withoutTitle.Title = null;
        db.Set<MediaAsset>().AddRange(withTitle, withoutTitle);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: MediaAssetQueueFilter.MissingTitle, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the asset missing a title should be returned");
        items.Single().OriginalFileName.Should().Be("b.jpg");
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Apply_EditorAssets_Filter()
    {
        await using var db = MediaTestDbContext.Create();
        var editor = BuildAsset("https://cdn.example.com/ed.jpg", "ed.jpg");
        editor.Role = "EditorAsset";
        var library = BuildAsset("https://cdn.example.com/lib.jpg", "lib.jpg");
        library.Role = "LibraryAsset";
        db.Set<MediaAsset>().AddRange(editor, library);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: MediaAssetQueueFilter.EditorAssets, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().OriginalFileName.Should().Be("ed.jpg");
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Respect_Pagination()
    {
        await using var db = MediaTestDbContext.Create();
        for (var i = 0; i < 5; i++)
            db.Set<MediaAsset>().Add(BuildAsset($"https://cdn.example.com/{i}.jpg", $"{i}.jpg"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 3, ct: TestContext.Current.CancellationToken);

        total.Should().Be(5, "total reflects all non-deleted assets");
        items.Should().HaveCount(3, "page size is 3");
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Clamp_Invalid_Page_To_One()
    {
        await using var db = MediaTestDbContext.Create();
        db.Set<MediaAsset>().Add(BuildAsset());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(0, 10, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1, "page 0 should be treated as page 1");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Apply_UsedInProducts_Filter_And_ProductReferenceCounts()
    {
        await using var db = MediaTestDbContext.Create();
        var usedAsset = BuildAsset("https://cdn.example.com/used.jpg", "used.jpg");
        var unusedAsset = BuildAsset("https://cdn.example.com/unused.jpg", "unused.jpg");
        db.Set<MediaAsset>().AddRange(usedAsset, unusedAsset);
        db.Set<ProductMedia>().Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MediaAssetId = usedAsset.Id,
            SortOrder = 0
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: MediaAssetQueueFilter.UsedInProducts, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items.Single().OriginalFileName.Should().Be("used.jpg");
        items.Single().ProductReferenceCount.Should().Be(1);
    }

    [Fact]
    public async Task GetMediaAssetsPage_Should_Apply_Unused_Filter()
    {
        await using var db = MediaTestDbContext.Create();
        var usedAsset = BuildAsset("https://cdn.example.com/used.jpg", "used.jpg");
        var unusedAsset = BuildAsset("https://cdn.example.com/unused.jpg", "unused.jpg");
        db.Set<MediaAsset>().AddRange(usedAsset, unusedAsset);
        db.Set<ProductMedia>().Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MediaAssetId = usedAsset.Id,
            SortOrder = 0
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetsPageHandler(db);
        var (items, total) = await handler.HandleAsync(
            1, 20, filter: MediaAssetQueueFilter.Unused, ct: TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().ContainSingle();
        items.Single().OriginalFileName.Should().Be("unused.jpg");
        items.Single().ProductReferenceCount.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetMediaAssetOpsSummaryHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMediaAssetOpsSummary_Should_Return_Zero_Counts_When_No_Assets()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new GetMediaAssetOpsSummaryHandler(db);

        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(0);
        summary.MissingAltCount.Should().Be(0);
        summary.MissingTitleCount.Should().Be(0);
        summary.EditorAssetCount.Should().Be(0);
        summary.LibraryAssetCount.Should().Be(0);
    }

    [Fact]
    public async Task GetMediaAssetOpsSummary_Should_Return_Correct_Counts()
    {
        await using var db = MediaTestDbContext.Create();

        // missing alt, missing title, library asset
        var lib = BuildAsset("https://cdn.example.com/lib.jpg", "lib.jpg");
        lib.Alt = string.Empty;
        lib.Title = null;
        lib.Role = "LibraryAsset";

        // has alt, has title, editor asset
        var editor = BuildAsset("https://cdn.example.com/ed.jpg", "ed.jpg");
        editor.Alt = "alt text";
        editor.Title = "Some Title";
        editor.Role = "EditorAsset";

        // soft-deleted, should not count
        var deleted = BuildAsset("https://cdn.example.com/del.jpg", "del.jpg", isDeleted: true);

        db.Set<MediaAsset>().AddRange(lib, editor, deleted);
        db.Set<ProductMedia>().Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MediaAssetId = editor.Id,
            SortOrder = 0
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMediaAssetOpsSummaryHandler(db);
        var summary = await handler.HandleAsync(TestContext.Current.CancellationToken);

        summary.TotalCount.Should().Be(2, "only non-deleted assets count toward the total");
        summary.MissingAltCount.Should().Be(1, "one asset is missing an alt text");
        summary.MissingTitleCount.Should().Be(1, "one asset has no title");
        summary.EditorAssetCount.Should().Be(1, "one asset has the EditorAsset role");
        summary.LibraryAssetCount.Should().Be(1, "one asset has the LibraryAsset role");
        summary.ProductReferencedCount.Should().Be(1, "one asset is linked to a product");
        summary.UnusedCount.Should().Be(1, "one non-deleted asset remains unused");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PurgeUnusedMediaAssetHandler — HandleAsync (single-asset purge)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_Fail_WhenAssetNotFound()
    {
        await using var db = MediaTestDbContext.Create();
        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("MediaAssetNotFound");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_Fail_WhenRowVersionIsNull()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1 });
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_Fail_WhenRowVersionIsEmpty()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1 });
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, Array.Empty<byte>(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("RowVersionRequired");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1, 2, 3 });
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, new byte[] { 9, 9, 9 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("ConcurrencyConflictDetected");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_Fail_WhenAssetIsReferencedByActiveProductMedia()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1 });
        db.Set<MediaAsset>().Add(asset);
        db.Set<ProductMedia>().Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MediaAssetId = asset.Id,
            SortOrder = 0,
            IsDeleted = false
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("MediaAssetStillReferenced");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_NotBeBlockedBy_SoftDeletedProductMediaReference()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 1 });
        db.Set<MediaAsset>().Add(asset);
        db.Set<ProductMedia>().Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MediaAssetId = asset.Id,
            SortOrder = 0,
            IsDeleted = true  // soft-deleted reference should not block purge
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, new byte[] { 1 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("a soft-deleted ProductMedia link does not count as active reference");
        db.Set<MediaAsset>().AsNoTracking().Any(m => m.Id == asset.Id).Should().BeFalse("asset must be permanently deleted");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_Succeed_AndRemoveAsset_WhenUnreferenced()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 5 });
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, new byte[] { 5 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        db.Set<MediaAsset>().AsNoTracking().Any(m => m.Id == asset.Id).Should().BeFalse("asset must be permanently removed");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Single_Should_LocateAndPurge_SoftDeletedAsset()
    {
        // HandleAsync uses IgnoreQueryFilters() so it can purge soft-deleted assets too.
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(rowVersion: new byte[] { 7 }, isDeleted: true);
        db.Set<MediaAsset>().Add(asset);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(asset.Id, new byte[] { 7 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("IgnoreQueryFilters allows finding and permanently deleting soft-deleted assets");
        db.Set<MediaAsset>().IgnoreQueryFilters().AsNoTracking().Any(m => m.Id == asset.Id)
            .Should().BeFalse("asset must be physically removed from the database");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PurgeUnusedMediaAssetHandler — HandleBatchAsync (batch purge)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PurgeUnusedMediaAsset_Batch_Should_ReturnZero_WhenNoUnusedAssetsExist()
    {
        await using var db = MediaTestDbContext.Create();
        var asset = BuildAsset(url: "https://cdn.example.com/used.jpg");
        db.Set<MediaAsset>().Add(asset);
        db.Set<ProductMedia>().Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MediaAssetId = asset.Id,
            SortOrder = 0
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleBatchAsync(take: 10, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.PurgedCount.Should().Be(0, "all assets are referenced");
        result.Value.PurgedUrls.Should().BeEmpty();
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Batch_Should_PurgeUnusedAssets_AndLeaveReferencedAssetIntact()
    {
        await using var db = MediaTestDbContext.Create();
        var unused = BuildAsset("https://cdn.example.com/orphan.jpg", "orphan.jpg");
        var used = BuildAsset("https://cdn.example.com/used.jpg", "used.jpg");
        db.Set<MediaAsset>().AddRange(unused, used);
        db.Set<ProductMedia>().Add(new ProductMedia
        {
            Id = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            MediaAssetId = used.Id,
            SortOrder = 0
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleBatchAsync(take: 50, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.PurgedCount.Should().Be(1, "only the unreferenced asset is purged");
        result.Value.PurgedUrls.Should().ContainSingle()
            .Which.Should().Be("https://cdn.example.com/orphan.jpg");

        db.Set<MediaAsset>().AsNoTracking().Any(m => m.Id == unused.Id)
            .Should().BeFalse("unused asset must be permanently removed");
        db.Set<MediaAsset>().AsNoTracking().Any(m => m.Id == used.Id)
            .Should().BeTrue("referenced asset must remain in the database");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Batch_Should_RespectTakeLimit()
    {
        await using var db = MediaTestDbContext.Create();
        for (var i = 0; i < 5; i++)
            db.Set<MediaAsset>().Add(BuildAsset($"https://cdn.example.com/{i}.jpg", $"{i}.jpg"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleBatchAsync(take: 2, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.PurgedCount.Should().Be(2, "the take parameter limits how many assets are purged per batch");
        db.Set<MediaAsset>().AsNoTracking().Count().Should().Be(3, "3 of the 5 unreferenced assets remain");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Batch_Should_ClampOversizedTake_ToMaxBulkSize()
    {
        // MaxBulkPurgeSize = 100; passing 9999 should be silently clamped.
        await using var db = MediaTestDbContext.Create();
        for (var i = 0; i < 3; i++)
            db.Set<MediaAsset>().Add(BuildAsset($"https://cdn.example.com/c{i}.jpg", $"c{i}.jpg"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleBatchAsync(take: 9999, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("oversized take is clamped internally; the handler does not throw");
        result.Value!.PurgedCount.Should().Be(3, "all 3 unreferenced assets are purged (well within the 100 cap)");
    }

    [Fact]
    public async Task PurgeUnusedMediaAsset_Batch_Should_ExcludeAssetUrlsWithEmptyUrls_FromPurgedUrls()
    {
        await using var db = MediaTestDbContext.Create();
        var noUrl = BuildAsset(url: string.Empty);
        db.Set<MediaAsset>().Add(noUrl);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PurgeUnusedMediaAssetHandler(db, CreateLocalizer());
        var result = await handler.HandleBatchAsync(take: 10, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.Value!.PurgedCount.Should().Be(1);
        result.Value.PurgedUrls.Should().BeEmpty("whitespace/empty URLs are excluded from the PurgedUrls list");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // In-memory DbContext for Media tests
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class MediaTestDbContext : DbContext, IAppDbContext
    {
        private MediaTestDbContext(DbContextOptions<MediaTestDbContext> options)
            : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static MediaTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MediaTestDbContext>()
                .UseInMemoryDatabase($"darwin_media_{Guid.NewGuid()}")
                .Options;
            return new MediaTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MediaAsset>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Url).HasMaxLength(2048).IsRequired();
                b.Property(x => x.Alt).HasMaxLength(256).IsRequired();
                b.Property(x => x.Title).HasMaxLength(256);
                b.Property(x => x.OriginalFileName).HasMaxLength(512).IsRequired();
                b.Property(x => x.SizeBytes);
                b.Property(x => x.ContentHash).HasMaxLength(128);
                b.Property(x => x.Width);
                b.Property(x => x.Height);
                b.Property(x => x.Role).HasMaxLength(64);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
            });

            modelBuilder.Entity<ProductMedia>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ProductId).IsRequired();
                b.Property(x => x.MediaAssetId).IsRequired();
                b.Property(x => x.SortOrder);
                b.Property(x => x.Role).HasMaxLength(64);
                b.Property(x => x.IsDeleted);
            });
        }
    }
}
