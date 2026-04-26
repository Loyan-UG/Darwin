using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Domain.Entities.CMS;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.CMS;

/// <summary>
/// Handler-level unit tests for CMS menu CRUD operations.
/// Covers <see cref="CreateMenuHandler"/>, <see cref="UpdateMenuHandler"/>,
/// and <see cref="GetMenuForEditHandler"/>.
/// </summary>
public sealed class CmsMenuHandlerTests
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

    private static MenuCreateDto ValidCreateDto() => new()
    {
        Name = "main-navigation",
        Items = new List<MenuItemDto>
        {
            new()
            {
                Url = "/home",
                SortOrder = 0,
                IsActive = true,
                Translations = new List<MenuItemTranslationDto>
                {
                    new() { Culture = "de-DE", Label = "Startseite" }
                }
            }
        }
    };

    // ─── CreateMenuHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMenu_Should_Persist_Menu_With_Items()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        await handler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var saved = await db.Set<Menu>()
            .Include(m => m.Items)
            .ThenInclude(i => i.Translations)
            .SingleAsync(TestContext.Current.CancellationToken);

        saved.Name.Should().Be("main-navigation");
        saved.Items.Should().HaveCount(1);
        saved.Items[0].Url.Should().Be("/home");
        saved.Items[0].Translations.Should().HaveCount(1);
        saved.Items[0].Translations[0].Culture.Should().Be("de-DE");
        saved.Items[0].Translations[0].Label.Should().Be("Startseite");
    }

    [Fact]
    public async Task CreateMenu_Should_Trim_Name_And_Item_Url()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        await handler.HandleAsync(new MenuCreateDto
        {
            Name = "  footer  ",
            Items = new List<MenuItemDto>
            {
                new()
                {
                    Url = "  /about  ",
                    SortOrder = 1,
                    Translations = new List<MenuItemTranslationDto>
                    {
                        new() { Culture = "de-DE", Label = "  Über uns  " }
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Menu>()
            .Include(m => m.Items)
            .ThenInclude(i => i.Translations)
            .SingleAsync(TestContext.Current.CancellationToken);

        saved.Name.Should().Be("footer");
        saved.Items[0].Url.Should().Be("/about");
        saved.Items[0].Translations[0].Label.Should().Be("Über uns");
    }

    [Fact]
    public async Task CreateMenu_Should_Persist_Multiple_Items_With_Multiple_Translations()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        await handler.HandleAsync(new MenuCreateDto
        {
            Name = "main",
            Items = new List<MenuItemDto>
            {
                new()
                {
                    Url = "/home",
                    SortOrder = 0,
                    Translations = new List<MenuItemTranslationDto>
                    {
                        new() { Culture = "de-DE", Label = "Startseite" },
                        new() { Culture = "en-US", Label = "Home" }
                    }
                },
                new()
                {
                    Url = "/contact",
                    SortOrder = 1,
                    Translations = new List<MenuItemTranslationDto>
                    {
                        new() { Culture = "de-DE", Label = "Kontakt" }
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Menu>()
            .Include(m => m.Items)
            .ThenInclude(i => i.Translations)
            .SingleAsync(TestContext.Current.CancellationToken);

        saved.Items.Should().HaveCount(2);
        saved.Items.Single(i => i.Url == "/home").Translations.Should().HaveCount(2);
        saved.Items.Single(i => i.Url == "/contact").Translations.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateMenu_Should_Support_External_Urls()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        await handler.HandleAsync(new MenuCreateDto
        {
            Name = "external",
            Items = new List<MenuItemDto>
            {
                new()
                {
                    Url = "https://example.com/help",
                    SortOrder = 0,
                    Translations = new List<MenuItemTranslationDto>
                    {
                        new() { Culture = "de-DE", Label = "Hilfe" }
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Menu>().Include(m => m.Items).SingleAsync(TestContext.Current.CancellationToken);
        saved.Items[0].Url.Should().Be("https://example.com/help");
    }

    [Fact]
    public async Task CreateMenu_Should_Throw_When_Name_Is_Empty()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new MenuCreateDto
        {
            Name = "",
            Items = new List<MenuItemDto>()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty menu name should fail validation");
    }

    [Fact]
    public async Task CreateMenu_Should_Throw_When_Item_Url_Is_Empty()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        var dto = ValidCreateDto();
        dto.Items[0].Url = "";

        var act = () => handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("item with empty URL should fail validation");
    }

    [Fact]
    public async Task CreateMenu_Should_Throw_When_Item_Has_No_Translations()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        var dto = ValidCreateDto();
        dto.Items[0].Translations = new List<MenuItemTranslationDto>();

        var act = () => handler.HandleAsync(dto, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("item with no translations should fail validation");
    }

    [Fact]
    public async Task CreateMenu_Should_Create_Menu_With_No_Items()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        await handler.HandleAsync(new MenuCreateDto { Name = "empty-menu", Items = new List<MenuItemDto>() },
            TestContext.Current.CancellationToken);

        var saved = await db.Set<Menu>().Include(m => m.Items).SingleAsync(TestContext.Current.CancellationToken);
        saved.Name.Should().Be("empty-menu");
        saved.Items.Should().BeEmpty("creating a menu with no items is valid");
    }

    [Fact]
    public async Task CreateMenu_Should_Persist_SortOrder_And_IsActive()
    {
        var db = TestDbFactory.Create();
        var handler = new CreateMenuHandler(db, CreateLocalizer());

        await handler.HandleAsync(new MenuCreateDto
        {
            Name = "nav",
            Items = new List<MenuItemDto>
            {
                new()
                {
                    Url = "/hidden",
                    SortOrder = 5,
                    IsActive = false,
                    Translations = new List<MenuItemTranslationDto>
                    {
                        new() { Culture = "de-DE", Label = "Versteckt" }
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<Menu>().Include(m => m.Items).SingleAsync(TestContext.Current.CancellationToken);
        saved.Items[0].SortOrder.Should().Be(5);
        saved.Items[0].IsActive.Should().BeFalse();
    }

    // ─── UpdateMenuHandler ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMenu_Should_Replace_Items_And_Update_Name()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var menu = await db.Set<Menu>().Include(m => m.Items).ThenInclude(i => i.Translations).SingleAsync(TestContext.Current.CancellationToken);

        var updateHandler = new UpdateMenuHandler(db, CreateLocalizer());
        await updateHandler.HandleAsync(new MenuEditDto
        {
            Id = menu.Id,
            RowVersion = menu.RowVersion,
            Name = "updated-nav",
            Items = new List<MenuItemDto>
            {
                new()
                {
                    Url = "/catalog",
                    SortOrder = 0,
                    Translations = new List<MenuItemTranslationDto>
                    {
                        new() { Culture = "de-DE", Label = "Katalog" }
                    }
                },
                new()
                {
                    Url = "/contact",
                    SortOrder = 1,
                    Translations = new List<MenuItemTranslationDto>
                    {
                        new() { Culture = "de-DE", Label = "Kontakt" }
                    }
                }
            }
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Menu>().Include(m => m.Items).ThenInclude(i => i.Translations).SingleAsync(TestContext.Current.CancellationToken);
        updated.Name.Should().Be("updated-nav");
        updated.Items.Should().HaveCount(2);
        updated.Items.Should().NotContain(i => i.Url == "/home", "old items should be replaced");
    }

    [Fact]
    public async Task UpdateMenu_Should_Throw_When_Menu_Not_Found()
    {
        var db = TestDbFactory.Create();
        var handler = new UpdateMenuHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new MenuEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            Name = "does-not-exist",
            Items = new List<MenuItemDto>()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("updating a non-existent menu should throw");
    }

    [Fact]
    public async Task UpdateMenu_Should_Throw_On_Concurrency_Conflict()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var menu = await db.Set<Menu>().SingleAsync(TestContext.Current.CancellationToken);

        var updateHandler = new UpdateMenuHandler(db, CreateLocalizer());

        var act = () => updateHandler.HandleAsync(new MenuEditDto
        {
            Id = menu.Id,
            RowVersion = new byte[] { 9, 9, 9 },  // stale row version
            Name = "conflict-update",
            Items = new List<MenuItemDto>()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("stale RowVersion should raise concurrency exception");
    }

    [Fact]
    public async Task UpdateMenu_Should_Throw_When_Name_Empty()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var menu = await db.Set<Menu>().SingleAsync(TestContext.Current.CancellationToken);

        var updateHandler = new UpdateMenuHandler(db, CreateLocalizer());

        var act = () => updateHandler.HandleAsync(new MenuEditDto
        {
            Id = menu.Id,
            RowVersion = menu.RowVersion,
            Name = "",
            Items = new List<MenuItemDto>()
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty name should fail update validation");
    }

    [Fact]
    public async Task UpdateMenu_Should_Clear_Items_When_New_Items_List_Is_Empty()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var menu = await db.Set<Menu>().Include(m => m.Items).SingleAsync(TestContext.Current.CancellationToken);
        menu.Items.Should().HaveCount(1, "sanity check: one item was created");

        var updateHandler = new UpdateMenuHandler(db, CreateLocalizer());
        await updateHandler.HandleAsync(new MenuEditDto
        {
            Id = menu.Id,
            RowVersion = menu.RowVersion,
            Name = "main-navigation",
            Items = new List<MenuItemDto>()  // remove all items
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Menu>().Include(m => m.Items).SingleAsync(TestContext.Current.CancellationToken);
        updated.Items.Should().BeEmpty("all items should be removed");
    }

    // ─── GetMenuForEditHandler ────────────────────────────────────────────────

    [Fact]
    public async Task GetMenuForEdit_Should_Return_Dto_With_Items_And_Translations()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var menu = await db.Set<Menu>().SingleAsync(TestContext.Current.CancellationToken);

        var queryHandler = new GetMenuForEditHandler(db);
        var dto = await queryHandler.HandleAsync(menu.Id, TestContext.Current.CancellationToken);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(menu.Id);
        dto.Name.Should().Be("main-navigation");
        dto.Items.Should().HaveCount(1);
        dto.Items[0].Url.Should().Be("/home");
        dto.Items[0].Translations.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMenuForEdit_Should_Return_Null_When_Not_Found()
    {
        var db = TestDbFactory.Create();
        var handler = new GetMenuForEditHandler(db);

        var dto = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        dto.Should().BeNull("querying a non-existent menu should return null");
    }

    [Fact]
    public async Task GetMenuForEdit_Should_Include_RowVersion_For_Concurrency()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var menu = await db.Set<Menu>().SingleAsync(TestContext.Current.CancellationToken);
        var queryHandler = new GetMenuForEditHandler(db);
        var dto = await queryHandler.HandleAsync(menu.Id, TestContext.Current.CancellationToken);

        dto!.RowVersion.Should().NotBeNull("RowVersion is required for optimistic concurrency control");
    }
}
