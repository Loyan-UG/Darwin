using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Application.Settings.Queries;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.CMS;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.CMS
{
    public sealed class MenusController : AdminBaseController
    {
        private const int EmptyItemSlots = 3;

        private readonly GetMenusPageHandler _getMenusPage;
        private readonly GetMenuForEditHandler _getMenuForEdit;
        private readonly CreateMenuHandler _createMenu;
        private readonly UpdateMenuHandler _updateMenu;
        private readonly GetCulturesHandler _getCultures;
        private readonly ISiteSettingCache _siteSettingCache;

        public MenusController(
            GetMenusPageHandler getMenusPage,
            GetMenuForEditHandler getMenuForEdit,
            CreateMenuHandler createMenu,
            UpdateMenuHandler updateMenu,
            GetCulturesHandler getCultures,
            ISiteSettingCache siteSettingCache)
        {
            _getMenusPage = getMenusPage ?? throw new ArgumentNullException(nameof(getMenusPage));
            _getMenuForEdit = getMenuForEdit ?? throw new ArgumentNullException(nameof(getMenuForEdit));
            _createMenu = createMenu ?? throw new ArgumentNullException(nameof(createMenu));
            _updateMenu = updateMenu ?? throw new ArgumentNullException(nameof(updateMenu));
            _getCultures = getCultures ?? throw new ArgumentNullException(nameof(getCultures));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _getMenusPage.HandleAsync(page, pageSize, ct).ConfigureAwait(false);
            var vm = new MenusIndexVm
            {
                Items = items.Select(x => new MenuListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    ItemsCount = x.ItemsCount,
                    ModifiedAtUtc = x.ModifiedAtUtc
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                Total = total
            };

            return RenderIndex(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct = default)
        {
            var vm = new MenuEditorVm();
            await EnsureEditorShapeAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor(vm, isCreate: true);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuEditorVm vm, CancellationToken ct = default)
        {
            await EnsureEditorShapeAsync(vm, ct).ConfigureAwait(false);
            var dto = BuildCreateDto(vm);
            if (dto.Items.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.Items), T("MenuAtLeastOneItemRequired"));
            }

            if (!ModelState.IsValid)
            {
                return RenderEditor(vm, isCreate: true);
            }

            try
            {
                await _createMenu.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("MenuCreated");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (ValidationException ex)
            {
                AddValidationErrors(ex);
                return RenderEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getMenuForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("MenuNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new MenuEditorVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Name = dto.Name,
                Items = dto.Items.Select(MapItem).ToList()
            };
            await EnsureEditorShapeAsync(vm, ct).ConfigureAwait(false);
            return RenderEditor(vm, isCreate: false);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuEditorVm vm, CancellationToken ct = default)
        {
            await EnsureEditorShapeAsync(vm, ct).ConfigureAwait(false);
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("MenuNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = BuildEditDto(vm);
            if (dto.Items.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.Items), T("MenuAtLeastOneItemRequired"));
            }

            if (!ModelState.IsValid)
            {
                return RenderEditor(vm, isCreate: false);
            }

            try
            {
                await _updateMenu.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("MenuUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("ConcurrencyConflictDetected");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (ValidationException ex)
            {
                AddValidationErrors(ex);
                return RenderEditor(vm, isCreate: false);
            }
        }

        private IActionResult RenderIndex(MenusIndexVm vm)
            => IsHtmxRequest() ? PartialView("~/Views/Menus/Index.cshtml", vm) : View("Index", vm);

        private IActionResult RenderEditor(MenuEditorVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            return IsHtmxRequest()
                ? PartialView("~/Views/Menus/_MenuEditorShell.cshtml", vm)
                : View(isCreate ? "Create" : "Edit", vm);
        }

        private IActionResult RedirectOrHtmx(string actionName, object routeValues)
        {
            if (IsHtmxRequest())
            {
                Response.Headers["HX-Redirect"] = Url.Action(actionName, routeValues) ?? string.Empty;
                return new EmptyResult();
            }

            return RedirectToAction(actionName, routeValues);
        }

        private bool IsHtmxRequest()
            => string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);

        private async Task EnsureEditorShapeAsync(MenuEditorVm vm, CancellationToken ct)
        {
            var (defaultCulture, cultures) = await _getCultures.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            vm.MultilingualEnabled = CountCultures(settings.SupportedCulturesCsv) > 1;
            var editorCultures = cultures
                .Prepend(defaultCulture)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (!vm.MultilingualEnabled)
            {
                editorCultures = [defaultCulture];
            }

            vm.Cultures = editorCultures.Length == 0 ? [defaultCulture] : editorCultures;

            foreach (var item in vm.Items)
            {
                EnsureItemTranslations(item, vm.Cultures);
            }

            while (vm.Items.Count == 0 || vm.Items.Count(x => string.IsNullOrWhiteSpace(x.Url)) < EmptyItemSlots)
            {
                var item = new MenuItemEditorVm { SortOrder = vm.Items.Count };
                EnsureItemTranslations(item, vm.Cultures);
                vm.Items.Add(item);
            }
        }

        private static void EnsureItemTranslations(MenuItemEditorVm item, IReadOnlyList<string> cultures)
        {
            foreach (var culture in cultures)
            {
                if (item.Translations.All(x => !string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
                {
                    item.Translations.Add(new MenuItemTranslationEditorVm { Culture = culture });
                }
            }

            item.Translations.RemoveAll(x => cultures.All(c => !string.Equals(c, x.Culture, StringComparison.OrdinalIgnoreCase)));
        }

        private MenuCreateDto BuildCreateDto(MenuEditorVm vm)
            => new()
            {
                Name = vm.Name,
                Items = BuildItems(vm.Items)
            };

        private MenuEditDto BuildEditDto(MenuEditorVm vm)
            => new()
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name,
                Items = BuildItems(vm.Items)
            };

        private static List<MenuItemDto> BuildItems(IEnumerable<MenuItemEditorVm> items)
            => items
                .Where(static x => !string.IsNullOrWhiteSpace(x.Url))
                .Select(static x => new MenuItemDto
                {
                    Id = x.Id,
                    Url = x.Url.Trim(),
                    SortOrder = x.SortOrder,
                    IsActive = x.IsActive,
                    Translations = x.Translations
                        .Where(static t => !string.IsNullOrWhiteSpace(t.Culture) && !string.IsNullOrWhiteSpace(t.Label))
                        .Select(static t => new MenuItemTranslationDto
                        {
                            Culture = t.Culture.Trim(),
                            Label = t.Label.Trim(),
                            Url = string.IsNullOrWhiteSpace(t.Url) ? null : t.Url.Trim()
                        })
                        .ToList()
                })
                .Where(static x => x.Translations.Count > 0)
                .OrderBy(static x => x.SortOrder)
                .ToList();

        private static MenuItemEditorVm MapItem(MenuItemDto dto)
            => new()
            {
                Id = dto.Id,
                Url = dto.Url,
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                Translations = dto.Translations.Select(t => new MenuItemTranslationEditorVm
                {
                    Culture = t.Culture,
                    Label = t.Label,
                    Url = t.Url
                }).ToList()
            };

        private void AddValidationErrors(ValidationException ex)
        {
            foreach (var error in ex.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
        }

        private static int CountCultures(string? supportedCulturesCsv)
            => (supportedCulturesCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
    }
}
