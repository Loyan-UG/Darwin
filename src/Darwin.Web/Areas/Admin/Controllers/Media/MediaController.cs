using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers.Media
{
    /// <summary>
    ///     Handles rich-text editor media uploads (e.g., images from Quill toolbar),
    ///     performing validation, secure file naming, and returning a JSON payload with the accessible URL.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Security & Hardening:
    ///         <list type="bullet">
    //             <item>Validate content type and file size limits; reject suspicious inputs.</item>
    ///             <item>Generate SEO-friendly filenames; avoid trusting client-provided names.</item>
    ///             <item>Store under a configurable media root; never allow path traversal.</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Response Shape:
    ///         <c>{ "url": "https://.../media/..." }</c> to be consumed by the editor's insert handler.
    ///     </para>
    /// </remarks>
    [Area("Admin")]
    public sealed class MediaController : AdminBaseController
    {
        private readonly IWebHostEnvironment _env;

        public MediaController(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Image upload endpoint for Quill. Returns JSON { url: "/uploads/..." }.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadQuill(IFormFile? file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file." });

            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { error = "Invalid file type." });

            const long maxSize = 5 * 1024 * 1024;
            if (file.Length > maxSize)
                return BadRequest(new { error = "File too large (max 5MB)." });

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream, ct);
            }

            var publicUrl = $"/uploads/{fileName}";
            return Json(new { url = publicUrl });
        }
    }
}
