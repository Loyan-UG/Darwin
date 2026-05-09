using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Application.CMS.Validators;
using Darwin.Application;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.CMS;

/// <summary>
/// Handler-level unit tests for CMS page CRUD operations.
/// Covers <see cref="CreatePageHandler"/>, <see cref="UpdatePageHandler"/>,
/// <see cref="SoftDeletePageHandler"/>, <see cref="GetPageForEditHandler"/>,
/// <see cref="GetPagesPageHandler"/>.
/// </summary>
public sealed class CmsPageHandlerTests
{
    // ─── Shared helpers ──────────────────────────────────────────────────────

    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    private static PageCreateDto ValidCreateDto(string culture = "de-DE") => new()
    {
        Status = PageStatus.Draft,
        Translations = new List<PageTranslationDto>
        {
            new()
            {
                Culture = culture,
                Title = "Test Page",
                Slug = "test-page",
                ContentHtml = "<p>Hello</p>"
            }
        }
    };

    // ─── CreatePageHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePage_Should_Persist_Page_With_Translations()
    {
        var db = TestDbFactory.Create();
        var validator = new PageCreateDtoValidator(CreateLocalizer());
        var handler = new CreatePageHandler(db, validator);

        var id = await handler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        id.Should().NotBeEmpty();
        var saved = await db.Set<Page>().Include(p => p.Translations).SingleAsync(TestContext.Current.CancellationToken);
        saved.Status.Should().Be(PageStatus.Draft);
        saved.Translations.Should().HaveCount(1);
        saved.Translations[0].Slug.Should().Be("test-page");
    }

    [Fact]
    public async Task CreatePage_Should_Sanitize_Content_Html()
    {
        var db = TestDbFactory.Create();
        var validator = new PageCreateDtoValidator(CreateLocalizer());
        var handler = new CreatePageHandler(db, validator);

        var dto = ValidCreateDto();
        dto.Translations[0].ContentHtml = "<p onclick=\"alert('xss')\">Safe</p><script>bad()</script>";

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var saved = await db.Set<Page>().Include(p => p.Translations).SingleAsync(TestContext.Current.CancellationToken);
        saved.Translations[0].ContentHtml.Should().NotContain("<script>");
        saved.Translations[0].ContentHtml.Should().NotContain("onclick");
    }

    [Fact]
    public async Task CreatePage_Should_Persist_Multiple_Translations()
    {
        var db = TestDbFactory.Create();
        var validator = new PageCreateDtoValidator(CreateLocalizer());
        var handler = new CreatePageHandler(db, validator);

        var dto = new PageCreateDto
        {
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Seite", Slug = "seite", ContentHtml = "<p>DE</p>" },
                new() { Culture = "en-US", Title = "Page", Slug = "page", ContentHtml = "<p>EN</p>" }
            }
        };

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var saved = await db.Set<Page>().Include(p => p.Translations).SingleAsync(TestContext.Current.CancellationToken);
        saved.Translations.Should().HaveCount(2);
        saved.Status.Should().Be(PageStatus.Published);
    }

    [Fact]
    public async Task CreatePage_Should_Persist_PublishWindow_When_Provided()
    {
        var db = TestDbFactory.Create();
        var validator = new PageCreateDtoValidator(CreateLocalizer());
        var handler = new CreatePageHandler(db, validator);

        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var dto = ValidCreateDto();
        dto.PublishStartUtc = start;
        dto.PublishEndUtc = end;

        await handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        var saved = await db.Set<Page>().SingleAsync(TestContext.Current.CancellationToken);
        saved.PublishStartUtc.Should().Be(start);
        saved.PublishEndUtc.Should().Be(end);
    }

    [Fact]
    public async Task CreatePage_Should_Throw_When_Translations_Empty()
    {
        var db = TestDbFactory.Create();
        var validator = new PageCreateDtoValidator(CreateLocalizer());
        var handler = new CreatePageHandler(db, validator);

        var dto = new PageCreateDto { Translations = new List<PageTranslationDto>() };

        var act = () => handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("at least one translation is required");
    }

    [Fact]
    public async Task CreatePage_Should_Throw_When_Slug_Empty()
    {
        var db = TestDbFactory.Create();
        var validator = new PageCreateDtoValidator(CreateLocalizer());
        var handler = new CreatePageHandler(db, validator);

        var dto = ValidCreateDto();
        dto.Translations[0].Slug = "";

        var act = () => handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty slug should fail validation");
    }

    [Fact]
    public async Task CreatePage_Should_Throw_When_PublishEnd_Before_Start()
    {
        var db = TestDbFactory.Create();
        var validator = new PageCreateDtoValidator(CreateLocalizer());
        var handler = new CreatePageHandler(db, validator);

        var dto = ValidCreateDto();
        dto.PublishStartUtc = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        dto.PublishEndUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var act = () => handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("end before start should be invalid");
    }

    // ─── UpdatePageHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePage_Should_Persist_Changed_Status_And_Translations()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var page = await db.Set<Page>().Include(p => p.Translations).SingleAsync(TestContext.Current.CancellationToken);
        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        page.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updateValidator = new PageEditDtoValidator(CreateLocalizer());
        var updateHandler = new UpdatePageHandler(db, updateValidator, CreateLocalizer());

        await updateHandler.HandleAsync(new PageEditDto
        {
            Id = id,
            RowVersion = fakeRowVersion,
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Updated", Slug = "updated", ContentHtml = "<p>Updated</p>" }
            }
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Page>().Include(p => p.Translations).SingleAsync(TestContext.Current.CancellationToken);
        updated.Status.Should().Be(PageStatus.Published);
        updated.Translations.Should().HaveCount(1);
        updated.Translations[0].Title.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdatePage_Should_Throw_When_Page_Not_Found()
    {
        var db = TestDbFactory.Create();
        var validator = new PageEditDtoValidator(CreateLocalizer());
        var handler = new UpdatePageHandler(db, validator, CreateLocalizer());

        var act = () => handler.HandleAsync(new PageEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "X", Slug = "x", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("non-existent page should not be found");
    }

    [Fact]
    public async Task UpdatePage_Should_Throw_On_Concurrency_Conflict()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var updateValidator = new PageEditDtoValidator(CreateLocalizer());
        var updateHandler = new UpdatePageHandler(db, updateValidator, CreateLocalizer());

        var act = () => updateHandler.HandleAsync(new PageEditDto
        {
            Id = id,
            RowVersion = new byte[] { 9, 9, 9 },  // stale version
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "X", Slug = "x", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale RowVersion must trigger concurrency exception");
    }

    [Fact]
    public async Task UpdatePage_Should_Sanitize_Content_Html_On_Update()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var page = await db.Set<Page>().Include(p => p.Translations).SingleAsync(TestContext.Current.CancellationToken);
        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        page.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updateValidator = new PageEditDtoValidator(CreateLocalizer());
        var updateHandler = new UpdatePageHandler(db, updateValidator, CreateLocalizer());

        await updateHandler.HandleAsync(new PageEditDto
        {
            Id = id,
            RowVersion = fakeRowVersion,
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Updated", Slug = "updated", ContentHtml = "<script>bad()</script><p>ok</p>" }
            }
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Page>().Include(p => p.Translations).SingleAsync(TestContext.Current.CancellationToken);
        updated.Translations[0].ContentHtml.Should().NotContain("<script>");
        updated.Translations[0].ContentHtml.Should().Contain("<p>");
    }

    // ─── SoftDeletePageHandler ────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeletePage_Should_Set_IsDeleted_Flag()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var page = await db.Set<Page>().FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        page!.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var deleteHandler = new SoftDeletePageHandler(db, CreateLocalizer());
        var result = await deleteHandler.HandleAsync(id, fakeRowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        page = await db.Set<Page>().FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        page!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDeletePage_Should_Fail_When_Page_Not_Found()
    {
        var db = TestDbFactory.Create();
        var handler = new SoftDeletePageHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("deleting a non-existent page should return a failure");
    }

    [Fact]
    public async Task SoftDeletePage_Should_Fail_On_Concurrency_Conflict()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var handler = new SoftDeletePageHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, new byte[] { 9, 9, 9 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion should trigger a concurrency failure");
    }

    [Fact]
    public async Task SoftDeletePage_Should_Succeed_When_RowVersion_Matches()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var page = await db.Set<Page>().FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        page!.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SoftDeletePageHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, fakeRowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("matching RowVersion should allow deletion");
        page = await db.Set<Page>().FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        page!.IsDeleted.Should().BeTrue();
    }

    // ─── GetPageForEditHandler ────────────────────────────────────────────────

    [Fact]
    public async Task GetPageForEdit_Should_Return_Dto_With_Translations()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var queryHandler = new GetPageForEditHandler(db);
        var dto = await queryHandler.HandleAsync(id, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(id);
        dto.Translations.Should().HaveCount(1);
        dto.Translations[0].Culture.Should().Be("de-DE");
        dto.Translations[0].Slug.Should().Be("test-page");
    }

    [Fact]
    public async Task GetPageForEdit_Should_Return_Null_When_Not_Found()
    {
        var db = TestDbFactory.Create();
        var queryHandler = new GetPageForEditHandler(db);

        var dto = await queryHandler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        dto.Should().BeNull("non-existent page should return null");
    }

    [Fact]
    public async Task GetPageForEdit_Should_Include_RowVersion()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var id = await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var queryHandler = new GetPageForEditHandler(db);
        var dto = await queryHandler.HandleAsync(id, TestContext.Current.CancellationToken);

        dto!.RowVersion.Should().NotBeNull("RowVersion is needed for optimistic concurrency");
    }

    // ─── GetPagesPageHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task GetPagesPage_Should_Return_Empty_When_No_Pages()
    {
        var db = TestDbFactory.Create();
        var handler = new GetPagesPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 10, "de-DE", TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetPagesPage_Should_Return_All_Pages_Without_Filter()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);

        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);
        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Published Page", Slug = "published", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagesPage_Should_Filter_By_Status_Draft()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);

        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);
        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Published", Slug = "pub", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "draft", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Title.Should().Be("Test Page");
    }

    [Fact]
    public async Task GetPagesPage_Should_Apply_Search_By_Title()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);

        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);
        await createHandler.HandleAsync(new PageCreateDto
        {
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Datenschutz", Slug = "datenschutz", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", "Datenschutz", null, TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Title.Should().Be("Datenschutz");
    }

    [Fact]
    public async Task GetPagesPage_Should_Respect_Pagination()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);

        for (var i = 1; i <= 5; i++)
        {
            await createHandler.HandleAsync(new PageCreateDto
            {
                Translations = new List<PageTranslationDto>
                {
                    new() { Culture = "de-DE", Title = $"Page {i}", Slug = $"page-{i}", ContentHtml = "" }
                }
            }, TestContext.Current.CancellationToken);
        }

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 2, "de-DE", TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagesPage_Should_Clamp_Invalid_Page_And_PageSize()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(-1, 0, "de-DE", TestContext.Current.CancellationToken);

        total.Should().Be(1, "invalid page/pageSize should be clamped to valid values");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagesPage_Should_Filter_By_Status_Published()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);

        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken); // Draft
        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Published", Slug = "pub", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "published", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Title.Should().Be("Published");
    }

    // ─── GetPagesPageHandler — windowed / live-window filters ─────────────────

    [Fact]
    public async Task GetPagesPage_Should_Filter_By_Windowed_When_PublishStartUtc_Set()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);

        // Page with a publish start window
        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Draft,
            PublishStartUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Windowed Start", Slug = "windowed-start", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        // Page with no window
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "windowed", TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the page with PublishStartUtc should appear");
        items.Single().Title.Should().Be("Windowed Start");
    }

    [Fact]
    public async Task GetPagesPage_Should_Filter_By_Windowed_When_PublishEndUtc_Set()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);

        // Page with a publish end window
        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Draft,
            PublishStartUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            PublishEndUtc = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Windowed End", Slug = "windowed-end", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        // Page with no window
        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Published,
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Always On", Slug = "always-on", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var handler = new GetPagesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "windowed", TestContext.Current.CancellationToken);

        total.Should().Be(1, "only the page with a publish window should appear");
        items.Single().Title.Should().Be("Windowed End");
    }

    [Fact]
    public async Task GetPagesPage_Should_Return_Live_Window_Page_When_Within_Active_Period()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var fixedNow = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Published,
            PublishStartUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PublishEndUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Live Window Page", Slug = "live-window-page", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var clock = new FakeClock(fixedNow);
        var handler = new GetPagesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "live-window", TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Single().Title.Should().Be("Live Window Page");
    }

    [Fact]
    public async Task GetPagesPage_Should_Exclude_Live_Window_Page_When_Before_StartUtc()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var fixedNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Published,
            PublishStartUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PublishEndUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Not Live Yet", Slug = "not-live-yet", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var clock = new FakeClock(fixedNow);
        var handler = new GetPagesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "live-window", TestContext.Current.CancellationToken);

        total.Should().Be(0, "page has not reached its publish window start");
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagesPage_Should_Exclude_Live_Window_Page_When_After_EndUtc()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var fixedNow = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Published,
            PublishStartUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PublishEndUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Expired Window", Slug = "expired-window", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var clock = new FakeClock(fixedNow);
        var handler = new GetPagesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "live-window", TestContext.Current.CancellationToken);

        total.Should().Be(0, "page publish window has expired");
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPagesPage_Should_Exclude_Draft_From_Live_Window_Filter()
    {
        var db = TestDbFactory.Create();
        var createValidator = new PageCreateDtoValidator(CreateLocalizer());
        var createHandler = new CreatePageHandler(db, createValidator);
        var fixedNow = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        await createHandler.HandleAsync(new PageCreateDto
        {
            Status = PageStatus.Draft,
            PublishStartUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PublishEndUtc = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            Translations = new List<PageTranslationDto>
            {
                new() { Culture = "de-DE", Title = "Draft With Window", Slug = "draft-window", ContentHtml = "" }
            }
        }, TestContext.Current.CancellationToken);

        var clock = new FakeClock(fixedNow);
        var handler = new GetPagesPageHandler(db, clock);
        var (items, total) = await handler.HandleAsync(1, 20, "de-DE", null, "live-window", TestContext.Current.CancellationToken);

        total.Should().Be(0, "live-window filter requires Published status");
        items.Should().BeEmpty();
    }

    // ─── FakeClock helper ─────────────────────────────────────────────────────

    private sealed class FakeClock : Darwin.Application.Abstractions.Services.IClock
    {
        public FakeClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }
}
