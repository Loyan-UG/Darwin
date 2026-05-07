using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.CMS.Media.Commands;
using Darwin.Application.CMS.Media.DTOs;
using Darwin.Application.CMS.Media.Queries;
using Darwin.Infrastructure.Media;
using Darwin.WebAdmin.ViewModels.CMS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
        private readonly MediaStorageOptions _mediaStorageOptions;
        private readonly GetMediaAssetsPageHandler _getPage;
        private readonly GetMediaAssetOpsSummaryHandler _getSummary;
        private readonly GetMediaAssetForEditHandler _getForEdit;
        private readonly CreateMediaAssetHandler _create;
        private readonly UpdateMediaAssetHandler _update;
        private readonly SoftDeleteMediaAssetHandler _softDelete;
        private readonly PurgeUnusedMediaAssetHandler _purgeUnused;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaController"/> class.
        /// </summary>
        public MediaController(
            IWebHostEnvironment env,
            IOptions<MediaStorageOptions> mediaStorageOptions,
            GetMediaAssetsPageHandler getPage,
            GetMediaAssetOpsSummaryHandler getSummary,
            GetMediaAssetForEditHandler getForEdit,
            CreateMediaAssetHandler create,
            UpdateMediaAssetHandler update,
            SoftDeleteMediaAssetHandler softDelete,
            PurgeUnusedMediaAssetHandler purgeUnused)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _mediaStorageOptions = mediaStorageOptions?.Value ?? throw new ArgumentNullException(nameof(mediaStorageOptions));
            _getPage = getPage ?? throw new ArgumentNullException(nameof(getPage));
            _getSummary = getSummary ?? throw new ArgumentNullException(nameof(getSummary));
            _getForEdit = getForEdit ?? throw new ArgumentNullException(nameof(getForEdit));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _softDelete = softDelete ?? throw new ArgumentNullException(nameof(softDelete));
            _purgeUnused = purgeUnused ?? throw new ArgumentNullException(nameof(purgeUnused));
        }

        /// <summary>
        /// Lists media assets already registered in the Admin library.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 24, string? query = null, MediaAssetQueueFilter filter = MediaAssetQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getPage.HandleAsync(page, pageSize, query, filter, ct).ConfigureAwait(false);
            var summary = await _getSummary.HandleAsync(ct).ConfigureAwait(false);
            var vm = new MediaAssetsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Filter = filter,
                Summary = new MediaAssetOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    MissingAltCount = summary.MissingAltCount,
                    MissingTitleCount = summary.MissingTitleCount,
                    EditorAssetCount = summary.EditorAssetCount,
                    LibraryAssetCount = summary.LibraryAssetCount,
                    ProductReferencedCount = summary.ProductReferencedCount,
                    UnusedCount = summary.UnusedCount
                },
                Playbooks = BuildPlaybooks(),
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
                    ProductReferenceCount = x.ProductReferenceCount,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderIndex(vm);
        }

        private IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> BuildFilterItems(MediaAssetQueueFilter selectedFilter)
        {
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T("MediaAll"), MediaAssetQueueFilter.All.ToString(), selectedFilter == MediaAssetQueueFilter.All);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T("MediaMissingAlt"), MediaAssetQueueFilter.MissingAlt.ToString(), selectedFilter == MediaAssetQueueFilter.MissingAlt);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T("MediaEditorAssets"), MediaAssetQueueFilter.EditorAssets.ToString(), selectedFilter == MediaAssetQueueFilter.EditorAssets);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T("MediaLibraryAssets"), MediaAssetQueueFilter.LibraryAssets.ToString(), selectedFilter == MediaAssetQueueFilter.LibraryAssets);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T("MediaMissingTitle"), MediaAssetQueueFilter.MissingTitle.ToString(), selectedFilter == MediaAssetQueueFilter.MissingTitle);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T("MediaUsedInProducts"), MediaAssetQueueFilter.UsedInProducts.ToString(), selectedFilter == MediaAssetQueueFilter.UsedInProducts);
            yield return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T("MediaUnusedAssets"), MediaAssetQueueFilter.Unused.ToString(), selectedFilter == MediaAssetQueueFilter.Unused);
        }

        private List<MediaAssetPlaybookVm> BuildPlaybooks()
        {
            return new List<MediaAssetPlaybookVm>
            {
                new()
                {
                    Title = T("MediaPlaybookMissingAltTitle"),
                    ScopeNote = T("MediaPlaybookMissingAltScope"),
                    OperatorAction = T("MediaPlaybookMissingAltAction")
                },
                new()
                {
                    Title = T("MediaPlaybookMissingTitleTitle"),
                    ScopeNote = T("MediaPlaybookMissingTitleScope"),
                    OperatorAction = T("MediaPlaybookMissingTitleAction")
                },
                new()
                {
                    Title = T("MediaPlaybookEditorAssetsTitle"),
                    ScopeNote = T("MediaPlaybookEditorAssetsScope"),
                    OperatorAction = T("MediaPlaybookEditorAssetsAction")
                },
                new()
                {
                    Title = T("MediaPlaybookUnusedAssetsTitle"),
                    ScopeNote = T("MediaPlaybookUnusedAssetsScope"),
                    OperatorAction = T("MediaPlaybookUnusedAssetsAction")
                }
            };
        }

        /// <summary>
        /// Returns the upload screen for a new media asset.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return RenderCreateEditor(new MediaAssetCreateVm());
        }

        /// <summary>
        /// Accepts an uploaded media file, stores it safely under shared media storage, and persists metadata.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes, ValueLengthLimit = 4096, ValueCountLimit = 64)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MediaAssetCreateVm vm, CancellationToken ct = default)
        {
            if (vm.File is null)
            {
                ModelState.AddModelError(nameof(vm.File), T("MediaUploadFileRequired"));
                return RenderCreateEditor(vm);
            }

            var validationError = ValidateFile(vm.File);
            if (validationError is not null)
            {
                ModelState.AddModelError(nameof(vm.File), validationError);
                return RenderCreateEditor(vm);
            }

            StoredUploadResult? stored = null;
            try
            {
                stored = await SaveUploadAsync(vm.File, ct).ConfigureAwait(false);
                var originalFileName = SanitizeOriginalFileName(vm.File.FileName);
                await _create.HandleAsync(new MediaAssetCreateDto
                {
                    Url = stored.PublicUrl,
                    Alt = vm.Alt,
                    Title = vm.Title,
                    OriginalFileName = originalFileName,
                    SizeBytes = stored.SizeBytes,
                    ContentHash = stored.ContentHash,
                    Role = string.IsNullOrWhiteSpace(vm.Role) ? "LibraryAsset" : vm.Role
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("MediaUploaded");
                return RedirectOrHtmx(nameof(Index), new { });
            }
            catch (InvalidDataException)
            {
                ModelState.AddModelError(nameof(vm.File), T("MediaUploadInvalidFileType"));
                return RenderCreateEditor(vm);
            }
            catch (Exception)
            {
                if (stored is not null && System.IO.File.Exists(stored.PhysicalPath))
                {
                    System.IO.File.Delete(stored.PhysicalPath);
                }

                AddModelErrorMessage("MediaCreateFailed");
                return RenderCreateEditor(vm);
            }
        }

        /// <summary>
        /// Returns the metadata editor for a stored media asset.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("MediaAssetNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("MediaAssetNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            return RenderEditEditor(new MediaAssetEditVm
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
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("MediaAssetNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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

                SetSuccessMessage("MediaUpdated");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("MediaConcurrencyConflict");
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("MediaUpdateFailed");
                return RenderEditEditor(vm);
            }
        }

        /// <summary>
        /// Soft-deletes a media asset from the library while leaving the physical file in place.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("MediaAssetNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _softDelete.HandleAsync(id, rowVersion, ct).ConfigureAwait(false);
            if (result.Succeeded)
                SetSuccessMessage("MediaDeleted");
            else
                TempData["Error"] = result.Error ?? T("MediaAssetNotFound");

            return RedirectOrHtmx(nameof(Index), new { });
        }

        /// <summary>
        /// Permanently removes an unreferenced media asset and its local upload file when safe.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeUnused([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("MediaAssetNotFound");
                return RedirectOrHtmx(nameof(Index), new { filter = MediaAssetQueueFilter.Unused });
            }

            var dto = await _getForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("MediaAssetNotFound");
                return RedirectOrHtmx(nameof(Index), new { filter = MediaAssetQueueFilter.Unused });
            }

            var localPath = TryResolveLocalUploadPath(dto.Url);
            var result = await _purgeUnused.HandleAsync(id, rowVersion, ct).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                SetErrorMessage(string.IsNullOrWhiteSpace(result.Error) ? "MediaPurgeFailed" : result.Error);
                return RedirectOrHtmx(nameof(Index), new { filter = MediaAssetQueueFilter.Unused });
            }

            if (!string.IsNullOrWhiteSpace(localPath) && System.IO.File.Exists(localPath))
            {
                System.IO.File.Delete(localPath);
            }

            SetSuccessMessage("MediaPurged");
            return RedirectOrHtmx(nameof(Index), new { filter = MediaAssetQueueFilter.Unused });
        }

        /// <summary>
        /// Permanently removes a bounded batch of currently unreferenced media assets and their local upload files.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PurgeUnusedBatch(CancellationToken ct = default)
        {
            var result = await _purgeUnused.HandleBatchAsync(ct: ct).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                SetErrorMessage(string.IsNullOrWhiteSpace(result.Error) ? "MediaBulkPurgeFailed" : result.Error);
                return RedirectOrHtmx(nameof(Index), new { filter = MediaAssetQueueFilter.Unused });
            }

            foreach (var url in result.Value.PurgedUrls)
            {
                var localPath = TryResolveLocalUploadPath(url);
                if (!string.IsNullOrWhiteSpace(localPath) && System.IO.File.Exists(localPath))
                {
                    System.IO.File.Delete(localPath);
                }
            }

            TempData["Success"] = string.Format(T("MediaBulkPurged"), result.Value.PurgedCount);
            return RedirectOrHtmx(nameof(Index), new { filter = MediaAssetQueueFilter.Unused });
        }

        /// <summary>
        /// Image upload endpoint for Quill. Returns JSON <c>{ url: "..." }</c> with the configured public media URL.
        /// </summary>
        /// <remarks>
        /// Quill sends multipart uploads through fetch with the admin form anti-forgery token. The endpoint remains
        /// protected by admin authorization and strict file validation.
        /// </remarks>
        [HttpPost]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes, ValueLengthLimit = 4096, ValueCountLimit = 16)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadQuill(IFormFile? file, CancellationToken ct)
        {
            if (file is null)
            {
                return BadRequest(new { error = T("MediaUploadFileRequired") });
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
                var originalFileName = SanitizeOriginalFileName(file.FileName);
                await _create.HandleAsync(new MediaAssetCreateDto
                {
                    Url = stored.PublicUrl,
                    Alt = string.Empty,
                    Title = ToMediaTitle(originalFileName),
                    OriginalFileName = originalFileName,
                    SizeBytes = stored.SizeBytes,
                    ContentHash = stored.ContentHash,
                    Role = "EditorAsset"
                }, ct).ConfigureAwait(false);

                return Json(new { url = stored.PublicUrl });
            }
            catch (InvalidDataException)
            {
                return BadRequest(new { error = T("MediaUploadInvalidFileType") });
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

        private IActionResult RenderIndex(MediaAssetsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Media/Index.cshtml", vm);
            }

            return View("Index", vm);
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
                return T("MediaUploadFileEmpty");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return T("MediaUploadInvalidFileType");
            }

            if (file.Length > MaxUploadBytes)
            {
                return T("MediaUploadFileTooLarge");
            }

            return null;
        }

        private static string SanitizeOriginalFileName(string? fileName)
        {
            var original = Path.GetFileName(fileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(original))
            {
                return "upload";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitizedChars = original
                .Select(ch => char.IsControl(ch) || invalidChars.Contains(ch) ? '_' : ch)
                .ToArray();
            var sanitized = new string(sanitizedChars).Trim(' ', '.');
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return "upload";
            }

            return sanitized.Length <= 260 ? sanitized : sanitized[..260];
        }

        private static string ToMediaTitle(string fileName)
        {
            return fileName.Length <= 256 ? fileName : fileName[..256];
        }

        private async Task<StoredUploadResult> SaveUploadAsync(IFormFile file, CancellationToken ct)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            await using var input = file.OpenReadStream();
            await using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer, ct).ConfigureAwait(false);

            var bytes = buffer.ToArray();
            if (!HasValidImageSignature(ext, bytes))
            {
                throw new InvalidDataException("Uploaded file signature does not match an allowed image format.");
            }

            var uploadsRoot = MediaStoragePathResolver.ResolveRootPath(_env.ContentRootPath, _mediaStorageOptions);
            Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);
            if (!IsPathUnderRoot(fullPath, uploadsRoot))
            {
                throw new InvalidDataException("Resolved upload path is outside the configured upload root.");
            }

            await System.IO.File.WriteAllBytesAsync(fullPath, bytes, ct).ConfigureAwait(false);

            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            return new StoredUploadResult(
                PhysicalPath: fullPath,
                PublicUrl: MediaStoragePathResolver.BuildPublicUrl(_mediaStorageOptions, fileName),
                SizeBytes: bytes.LongLength,
                ContentHash: hash);
        }

        private static bool HasValidImageSignature(string extension, byte[] bytes)
        {
            return extension switch
            {
                ".png" => bytes.Length >= 8 &&
                    bytes[0] == 0x89 &&
                    bytes[1] == 0x50 &&
                    bytes[2] == 0x4E &&
                    bytes[3] == 0x47 &&
                    bytes[4] == 0x0D &&
                    bytes[5] == 0x0A &&
                    bytes[6] == 0x1A &&
                    bytes[7] == 0x0A,
                ".jpg" or ".jpeg" => bytes.Length >= 3 &&
                    bytes[0] == 0xFF &&
                    bytes[1] == 0xD8 &&
                    bytes[2] == 0xFF,
                ".gif" => bytes.Length >= 6 &&
                    bytes[0] == 0x47 &&
                    bytes[1] == 0x49 &&
                    bytes[2] == 0x46 &&
                    bytes[3] == 0x38 &&
                    (bytes[4] == 0x37 || bytes[4] == 0x39) &&
                    bytes[5] == 0x61,
                ".webp" => bytes.Length >= 12 &&
                    bytes[0] == 0x52 &&
                    bytes[1] == 0x49 &&
                    bytes[2] == 0x46 &&
                    bytes[3] == 0x46 &&
                    bytes[8] == 0x57 &&
                    bytes[9] == 0x45 &&
                    bytes[10] == 0x42 &&
                    bytes[11] == 0x50,
                _ => false
            };
        }

        private string? TryResolveLocalUploadPath(string? url)
        {
            var uploadRelativePath = TryGetUploadRelativePath(url);
            if (string.IsNullOrWhiteSpace(uploadRelativePath))
            {
                return null;
            }

            var uploadsRoot = MediaStoragePathResolver.ResolveRootPath(_env.ContentRootPath, _mediaStorageOptions);
            var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, uploadRelativePath.Replace('/', Path.DirectorySeparatorChar)));
            return IsPathUnderRoot(fullPath, uploadsRoot) ? fullPath : null;
        }

        private string? TryGetUploadRelativePath(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var requestPath = MediaStoragePathResolver.NormalizeRequestPath(_mediaStorageOptions.RequestPath);
            var path = url.Trim();

            if (Uri.TryCreate(path, UriKind.Absolute, out var absoluteUri))
            {
                path = absoluteUri.AbsolutePath;
            }

            if (!path.StartsWith(requestPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var relative = path[(requestPath.Length + 1)..];
            return relative.Contains("..", StringComparison.Ordinal) ? null : Uri.UnescapeDataString(relative);
        }

        private static bool IsPathUnderRoot(string path, string root)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var normalizedPath = Path.GetFullPath(path);
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }

        private sealed record StoredUploadResult(string PhysicalPath, string PublicUrl, long SizeBytes, string ContentHash);
    }
}
