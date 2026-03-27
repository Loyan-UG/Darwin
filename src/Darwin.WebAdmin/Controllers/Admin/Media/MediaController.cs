using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.CMS.Media.Commands;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Queries;
using Darwin.WebAdmin.ViewModels.CMS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.Media
{
    /// <summary>
    /// Handles Admin media-library operations and rich-text editor uploads.
    /// </summary>
    /// <remarks>
    /// Security & hardening:
    /// <list type="bullet">
    /// <item>Validate extensions and file-size limits before writing to disk.</item>
    /// <item>Generate server-side file names and never trust the original file name for storage paths.</item>
    /// <item>Persist uploaded editor assets into the same media table used by the Admin library.</item>
    /// </list>
    /// </remarks>
    public sealed class MediaController : AdminBaseController
    {
        private static readonly string[] AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"];
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        private readonly IWebHostEnvironment _env;
        private readonly GetMediaAssetsPageHandler _getPage;
        private readonly GetMediaAssetForEditHandler _getForEdit;
        private readonly CreateMediaAssetHandler _create;
        private readonly UpdateMediaAssetHandler _update;
        private readonly SoftDeleteMediaAssetHandler _softDelete;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaController"/> class.
        /// </summary>
        public MediaController(
            IWebHostEnvironment env,
            GetMediaAssetsPageHandler getPage,
            GetMediaAssetForEditHandler getForEdit,
            CreateMediaAssetHandler create,
            UpdateMediaAssetHandler update,
            SoftDeleteMediaAssetHandler softDelete)
        {
            _env = env;
            _getPage = getPage;
            _getForEdit = getForEdit;
            _create = create;
            _update = update;
            _softDelete = softDelete;
        }

        /// <summary>
        /// Lists media assets already registered in the Admin library.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 24, string? query = null, MediaAssetQueueFilter filter = MediaAssetQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getPage.HandleAsync(page, pageSize, query, filter, ct).ConfigureAwait(false);
            var vm = new MediaAssetsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                FilterItems = BuildFilterItems(filter),
                Items = items.Select(x => new MediaAssetListItemVm
                {
                    Id = x.Id,
                    Url = x.Url,
                    Alt = x.Alt,
                    Title = x.Title,
                    OriginalFileName = x.OriginalFileName,
                    SizeBytes = x.SizeBytes,
                    Width = x.Width,
                    Height = x.Height,
                    Role = x.Role,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        private static IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> BuildFilterItems(MediaAssetQueueFilter selectedFilter)
        {
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("All media", MediaAssetQueueFilter.All.ToString(), selectedFilter == MediaAssetQueueFilter.All);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("Missing alt", MediaAssetQueueFilter.MissingAlt.ToString(), selectedFilter == MediaAssetQueueFilter.MissingAlt);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("Editor assets", MediaAssetQueueFilter.EditorAssets.ToString(), selectedFilter == MediaAssetQueueFilter.EditorAssets);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("Library assets", MediaAssetQueueFilter.LibraryAssets.ToString(), selectedFilter == MediaAssetQueueFilter.LibraryAssets);
        }

        /// <summary>
        /// Returns the upload screen for a new media asset.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new MediaAssetCreateVm());
        }

        /// <summary>
        /// Accepts an uploaded media file, stores it safely under <c>wwwroot/uploads</c>, and persists metadata.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MediaAssetCreateVm vm, CancellationToken ct = default)
        {
            if (vm.File is null)
            {
                ModelState.AddModelError(nameof(vm.File), "Select a file to upload.");
                return RenderCreateEditor(vm);
            }

            var validationError = ValidateFile(vm.File);
            if (validationError is not null)
            {
                ModelState.AddModelError(nameof(vm.File), validationError);
                return RenderCreateEditor(vm);
            }

            try
            {
                var stored = await SaveUploadAsync(vm.File, ct).ConfigureAwait(false);
                await _create.HandleAsync(new MediaAssetCreateDto
                {
                    Url = stored.PublicUrl,
                    Alt = vm.Alt,
                    Title = vm.Title,
                    OriginalFileName = vm.File.FileName,
                    SizeBytes = stored.SizeBytes,
                    ContentHash = stored.ContentHash,
                    Role = string.IsNullOrWhiteSpace(vm.Role) ? "LibraryAsset" : vm.Role
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Media uploaded.";
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return RenderCreateEditor(vm);
            }
        }

        /// <summary>
        /// Returns the metadata editor for a stored media asset.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Media asset not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(new MediaAssetEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Url = dto.Url,
                Alt = dto.Alt,
                Title = dto.Title,
                OriginalFileName = dto.OriginalFileName,
                SizeBytes = dto.SizeBytes,
                Width = dto.Width,
                Height = dto.Height,
                Role = dto.Role,
                ModifiedAtUtc = dto.ModifiedAtUtc
            });
        }

        /// <summary>
        /// Updates descriptive metadata for a stored media asset.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MediaAssetEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return RenderEditEditor(vm);
            }

            try
            {
                await _update.HandleAsync(new MediaAssetEditDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    Alt = vm.Alt,
                    Title = vm.Title,
                    Role = vm.Role
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Media updated.";
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the media asset and try again.";
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return RenderEditEditor(vm);
            }
        }

        /// <summary>
        /// Soft-deletes a media asset from the library while leaving the physical file in place.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)
        {
            await _softDelete.HandleAsync(id, ct).ConfigureAwait(false);
            TempData["Success"] = "Media asset deleted from the library.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Image upload endpoint for Quill. Returns JSON <c>{ url: "/uploads/..." }</c>.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadQuill(IFormFile? file, CancellationToken ct)
        {
            if (file is null)
            {
                return BadRequest(new { error = "No file." });
            }

            var validationError = ValidateFile(file);
            if (validationError is not null)
            {
                return BadRequest(new { error = validationError });
            }

            StoredUploadResult? stored = null;
            try
            {
                stored = await SaveUploadAsync(file, ct).ConfigureAwait(false);
                await _create.HandleAsync(new MediaAssetCreateDto
                {
                    Url = stored.PublicUrl,
                    Alt = string.Empty,
                    Title = file.FileName,
                    OriginalFileName = file.FileName,
                    SizeBytes = stored.SizeBytes,
                    ContentHash = stored.ContentHash,
                    Role = "EditorAsset"
                }, ct).ConfigureAwait(false);

                return Json(new { url = stored.PublicUrl });
            }
            catch (Exception)
            {
                if (stored is not null && System.IO.File.Exists(stored.PhysicalPath))
                {
                    System.IO.File.Delete(stored.PhysicalPath);
                }

                throw;
            }
        }

        private IActionResult RenderCreateEditor(MediaAssetCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Media/_MediaAssetCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderEditEditor(MediaAssetEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Media/_MediaAssetEditEditorShell.cshtml", vm);
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

        private string? ValidateFile(IFormFile file)
        {
            if (file.Length == 0)
            {
                return "The uploaded file is empty.";
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return "Invalid file type.";
            }

            if (file.Length > MaxUploadBytes)
            {
                return "File too large (max 5MB).";
            }

            return null;
        }

        private async Task<StoredUploadResult> SaveUploadAsync(IFormFile file, CancellationToken ct)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uploadsRoot = Path.Combine(GetWebRootPath(), "uploads");
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using var input = file.OpenReadStream();
            await using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer, ct).ConfigureAwait(false);

            var bytes = buffer.ToArray();
            await System.IO.File.WriteAllBytesAsync(fullPath, bytes, ct).ConfigureAwait(false);

            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            return new StoredUploadResult(
                PhysicalPath: fullPath,
                PublicUrl: $"/uploads/{fileName}",
                SizeBytes: bytes.LongLength,
                ContentHash: hash);
        }

        private string GetWebRootPath()
        {
            if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                return _env.WebRootPath;
            }

            return Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        private sealed record StoredUploadResult(string PhysicalPath, string PublicUrl, long SizeBytes, string ContentHash);
    }
}
