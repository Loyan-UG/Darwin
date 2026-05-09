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
        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        menu.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updateHandler = new UpdateMenuHandler(db, CreateLocalizer());
        await updateHandler.HandleAsync(new MenuEditDto
        {
            Id = menu.Id,
            RowVersion = fakeRowVersion,
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
        var activeItems = updated.Items.Where(i => !i.IsDeleted).ToList();
        updated.Name.Should().Be("updated-nav");
        activeItems.Should().HaveCount(2);
        activeItems.Should().NotContain(i => i.Url == "/home", "old items should be replaced");
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
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
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

        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        menu.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var updateHandler = new UpdateMenuHandler(db, CreateLocalizer());
        await updateHandler.HandleAsync(new MenuEditDto
        {
            Id = menu.Id,
            RowVersion = fakeRowVersion,
            Name = "main-navigation",
            Items = new List<MenuItemDto>()  // remove all items
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<Menu>().Include(m => m.Items).SingleAsync(TestContext.Current.CancellationToken);
        updated.Items.Where(i => !i.IsDeleted).Should().BeEmpty("all items should be removed");
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

    // ─── GetMenusPageHandler ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMenusPage_Should_Return_Empty_State()
    {
        var db = TestDbFactory.Create();
        var handler = new GetMenusPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetMenusPage_Should_Exclude_SoftDeleted_Menus()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        // Soft-delete the created menu
        var menu = await db.Set<Menu>().SingleAsync(TestContext.Current.CancellationToken);
        menu.IsDeleted = true;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetMenusPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        items.Should().BeEmpty("soft-deleted menus must not appear in the list");
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetMenusPage_Should_Return_All_NonDeleted_Menus()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);
        await createHandler.HandleAsync(new MenuCreateDto { Name = "footer", Items = new List<MenuItemDto>() }, TestContext.Current.CancellationToken);

        var handler = new GetMenusPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(2);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMenusPage_Should_Normalize_Page_Below_One_To_One()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var handler = new GetMenusPageHandler(db);
        var (items, total) = await handler.HandleAsync(-5, 20, TestContext.Current.CancellationToken);

        // Should still return results, treating page < 1 as page 1
        total.Should().BeGreaterThan(0);
        items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMenusPage_Should_Clamp_PageSize_To_MaxPageSize()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        for (var i = 0; i < 5; i++)
        {
            await createHandler.HandleAsync(new MenuCreateDto { Name = $"menu-{i}", Items = new List<MenuItemDto>() }, TestContext.Current.CancellationToken);
        }

        var handler = new GetMenusPageHandler(db);
        // Request a page size of 9999, which should be clamped to MaxPageSize (200)
        var (items, total) = await handler.HandleAsync(1, 9999, TestContext.Current.CancellationToken);

        total.Should().Be(5);
        items.Should().HaveCount(5, "all 5 menus fit within MaxPageSize so all should be returned");
    }

    [Fact]
    public async Task GetMenusPage_Should_Project_ItemsCount_Excluding_Deleted_Items()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(ValidCreateDto(), TestContext.Current.CancellationToken);

        var handler = new GetMenusPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        var item = items.Should().ContainSingle().Subject;
        item.Name.Should().Be("main-navigation");
        item.ItemsCount.Should().Be(1, "one non-deleted item was created");
    }

    [Fact]
    public async Task GetMenusPage_Should_Order_Menus_Alphabetically_By_Name()
    {
        var db = TestDbFactory.Create();
        var createHandler = new CreateMenuHandler(db, CreateLocalizer());
        await createHandler.HandleAsync(new MenuCreateDto { Name = "z-menu", Items = new List<MenuItemDto>() }, TestContext.Current.CancellationToken);
        await createHandler.HandleAsync(new MenuCreateDto { Name = "a-menu", Items = new List<MenuItemDto>() }, TestContext.Current.CancellationToken);
        await createHandler.HandleAsync(new MenuCreateDto { Name = "m-menu", Items = new List<MenuItemDto>() }, TestContext.Current.CancellationToken);

        var handler = new GetMenusPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        items[0].Name.Should().Be("a-menu");
        items[1].Name.Should().Be("m-menu");
        items[2].Name.Should().Be("z-menu");
    }
}
