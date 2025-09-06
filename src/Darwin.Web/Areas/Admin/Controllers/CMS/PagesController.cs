using Darwin.Application.CMS.Commands;
using Darwin.Application.CMS.DTOs;
using Darwin.Application.CMS.Queries;
using Darwin.Web.Areas.Admin.ViewModels.CMS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.CMS
{
    [Area("Admin")]
    public sealed class PagesController : Controller
    {
        private readonly CreatePageHandler _create;
        private readonly UpdatePageHandler _update;
        private readonly GetPagesPageHandler _list;
        private readonly GetPageForEditHandler _get;

        public PagesController(CreatePageHandler create, UpdatePageHandler update,
            GetPagesPageHandler list, GetPageForEditHandler get)
        {
            _create = create; _update = update; _list = list; _get = get;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _list.HandleAsync(page, pageSize, "de-DE", ct);
            ViewBag.Total = total; ViewBag.Page = page; ViewBag.PageSize = pageSize;
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Cultures = new[] { "de-DE", "en-US" }; // later from SiteSetting
            return View(new PageCreateVm());
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(PageCreateVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();
            if (vm.Translations.Count == 0)
                ModelState.AddModelError(nameof(vm.Translations), "At least one translation is required.");

            if (!ModelState.IsValid)
            {
                ViewBag.Cultures = new[] { "de-DE", "en-US" };
                if (vm.Translations.Count == 0) vm.Translations.Add(new PageTranslationVm());
                return View(vm);
            }

            var dto = new PageCreateDto
            {
                Status = vm.Status,
                PublishStartUtc = vm.PublishStartUtc,
                PublishEndUtc = vm.PublishEndUtc,
                Translations = vm.Translations.Select(t => new PageTranslationDto
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            try
            {
                await _create.HandleAsync(dto, ct);
                TempData["Success"] = "Page created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                ViewBag.Cultures = new[] { "de-DE", "en-US" };
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var dto = await _get.HandleAsync(id, ct);
            if (dto == null) return NotFound();

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

            ViewBag.Cultures = new[] { "de-DE", "en-US" };
            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Edit(PageEditVm vm, CancellationToken ct)
        {
            vm.Translations ??= new();

            if (!ModelState.IsValid)
            {
                ViewBag.Cultures = new[] { "de-DE", "en-US" };
                return View(vm);
            }

            var dto = new PageEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Status = vm.Status,
                PublishStartUtc = vm.PublishStartUtc,
                PublishEndUtc = vm.PublishEndUtc,
                Translations = vm.Translations.Select(t => new PageTranslationDto
                {
                    Culture = t.Culture,
                    Title = t.Title,
                    Slug = t.Slug,
                    MetaTitle = t.MetaTitle,
                    MetaDescription = t.MetaDescription,
                    ContentHtml = t.ContentHtml
                }).ToList()
            };

            try
            {
                await _update.HandleAsync(dto, ct);
                TempData["Success"] = "Page updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Concurrency conflict: the record was modified by another user.");
                ViewBag.Cultures = new[] { "de-DE", "en-US" };
                return View(vm);
            }
            catch (FluentValidation.ValidationException ex)
            {
                foreach (var e in ex.Errors)
                    ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
                ViewBag.Cultures = new[] { "de-DE", "en-US" };
                return View(vm);
            }
        }
    }
}
