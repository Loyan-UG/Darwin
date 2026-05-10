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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.CMS
{
    /// <summary>
    ///     Admin controller for CMS Pages covering full content lifecycle:
    ///     draft/publish windows, per-culture translations, and rich text content with sanitization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Features:
    ///         <list type="bullet">
    ///             <item>Quill v2 editor for content; server-side sanitization before persistence.</item>
    ///             <item>Unique slug per culture; index-level enforcement in the database.</item>
    ///             <item>Publish window fields (start/end) interpreted in UTC for unambiguous scheduling.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         UX:
    ///         Offers culture selection sourced from <c>SiteSetting</c>; displays friendly validation summaries and alerts.
    ///     </para>
    /// </remarks>
    public sealed class PagesController : AdminBaseController
    {
        private readonly CreatePageHandler _create;
        private readonly UpdatePageHandler _update;
        private readonly GetPagesPageHandler _list;
        private readonly GetPageOpsSummaryHandler _getPageOpsSummary;
        private readonly GetPageForEditHandler _get;
        private readonly GetCulturesHandler _getCultures;
        private readonly SoftDeletePageHandler _softDeletePage;
        private readonly ISiteSettingCache _siteSettingCache;

        public PagesController(
            CreatePageHandler create,
            UpdatePageHandler update,
            GetPagesPageHandler list,
            GetPageOpsSummaryHandler getPageOpsSummary,
            GetPageForEditHandler get,
            GetCulturesHandler getCultures,
            SoftDeletePageHandler softDeletePage,
            ISiteSettingCache siteSettingCache)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _getPageOpsSummary = getPageOpsSummary ?? throw new ArgumentNullException(nameof(getPageOpsSummary));
            _get = get ?? throw new ArgumentNullException(nameof(get));
            _getCultures = getCultures ?? throw new ArgumentNullException(nameof(getCultures));
            _softDeletePage = softDeletePage ?? throw new ArgumentNullException(nameof(softDeletePage));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)
        {
            var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;
            var (items, total) = await _list.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);
            var summary = await _getPageOpsSummary.HandleAsync(ct);

            var vm = new PagesIndexVm
            {
                Items = items,
                Query = query ?? string.Empty,
                Filter = filter ?? string.Empty,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Summary = new PageOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    DraftCount = summary.DraftCount,
                    PublishedCount = summary.PublishedCount,
                    WindowedCount = summary.WindowedCount,
                    LiveWindowCount = summary.LiveWindowCount
                },
                Playbooks = BuildPagePlaybooks()
            };

            return RenderIndex(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var vm = new PageCreateVm();
            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
            return RenderCreateEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(PageCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), T("PageTranslationRequired"));

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), T("PageTranslationRequired"));

            if (!ModelState.IsValid)
            {
                await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
                return RenderCreateEditor(vm);
            }

            var dto = new PageCreateDto
            {
                Status = vm.Status,
                PublishStartUtc = NormalizeNullableUtc(vm.PublishStartUtc),
                PublishEndUtc = NormalizeNullableUtc(vm.PublishEndUtc),
                Translations = translations.Select(t => new PageTranslationDto
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = NormalizeSlug(t.Slug),
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            try
            {
                await _create.HandleAsync(dto, ct);
                SetSuccessMessage("PageCreated");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
                return RenderCreateEditor(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("PageNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _get.HandleAsync(id, ct);
            if (dto == null)
            {
                SetErrorMessage("PageNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new PageEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Status = dto.Status,
                PublishStartUtc = dto.PublishStartUtc,
                PublishEndUtc = dto.PublishEndUtc,
                Translations = dto.Translations.Select(t => new PageTranslationVm
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);

            return RenderEditEditor(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(PageEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();

            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("PageNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }

            var translations = FilterCompleteTranslations(vm.Translations);
            if (translations.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.Translations), T("PageTranslationRequired"));
                await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }

            var dto = new PageEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Status = vm.Status,
                PublishStartUtc = NormalizeNullableUtc(vm.PublishStartUtc),
                PublishEndUtc = NormalizeNullableUtc(vm.PublishEndUtc),
                Translations = translations.Select(t => new PageTranslationDto
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = NormalizeSlug(t.Slug),
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            try
            {
                await _update.HandleAsync(dto, ct);
                SetSuccessMessage("PageUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, T("PageConcurrencyConflict"));
                await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);

                await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);
                return RenderEditEditor(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("PageDeleteFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            try
            {
                await _softDeletePage.HandleAsync(id, DecodeBase64RowVersion(rowVersion), ct);
                SetSuccessMessage("PageDeleted");
            }
            catch
            {
                SetErrorMessage("PageDeleteFailed");
            }
            return RedirectOrHtmx(nameof(Index), new { });
        }

        private IActionResult RenderIndex(PagesIndexVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Pages/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderCreateEditor(PageCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Pages/_PageCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderEditEditor(PageEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Pages/_PageEditEditorShell.cshtml", vm);
            }

            return View("Edit", vm);
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
        {
            return string.Equals(Request.Headers["HX-Request"], "true", StringComparison.OrdinalIgnoreCase);
        }

        private async Task EnsureTranslationsAsync(PageEditorVm vm, CancellationToken ct)
        {
            var (defaultCulture, cultures) = await _getCultures.HandleAsync(ct).ConfigureAwait(false);
            var orderedCultures = cultures
                .Prepend(defaultCulture)
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (orderedCultures.Length == 0)
            {
                orderedCultures = [defaultCulture];
            }

            vm.Cultures = orderedCultures;

            foreach (var culture in orderedCultures)
            {
                if (vm.Translations.All(x => !string.Equals(x.Culture, culture, StringComparison.OrdinalIgnoreCase)))
                {
                    vm.Translations.Add(new PageTranslationVm { Culture = culture });
                }
            }
        }

        private static List<PageTranslationVm> FilterCompleteTranslations(IEnumerable<PageTranslationVm> translations)
        {
            return translations
                .Where(static t =>
                    !string.IsNullOrWhiteSpace(t.Culture) &&
                    !string.IsNullOrWhiteSpace(t.Title) &&
                    !string.IsNullOrWhiteSpace(t.Slug))
                .ToList();
        }

        private static DateTime? NormalizeNullableUtc(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        private static string NormalizeSlug(string slug)
            => slug.Trim().ToLowerInvariant();

        private PagePlaybookVm[] BuildPagePlaybooks()
        {
            return
            [
                new PagePlaybookVm
                {
                    QueueLabel = T("Draft"),
                    WhyItMatters = T("PagesPlaybookDraftScope"),
                    OperatorAction = T("PagesPlaybookDraftAction")
                },
                new PagePlaybookVm
                {
                    QueueLabel = T("Windowed"),
                    WhyItMatters = T("PagesPlaybookWindowedScope"),
                    OperatorAction = T("PagesPlaybookWindowedAction")
                },
                new PagePlaybookVm
                {
                    QueueLabel = T("LiveWindow"),
                    WhyItMatters = T("PagesPlaybookLiveWindowScope"),
                    OperatorAction = T("PagesPlaybookLiveWindowAction")
                }
            ];
        }
    }
}

