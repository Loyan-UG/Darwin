using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;
using Darwin.Web.Areas.Admin.ViewModels.Identity;
using Darwin.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.Web.Areas.Admin.Controllers.Identity
{
    /// <summary>
    /// Admin controller for managing permissions.
    /// Provides list with paging/search, create/edit forms and soft delete.
    /// Key and IsSystem values are immutable once created.
    /// </summary>
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    [PermissionAuthorize("FullAdminAccess")]
    public sealed class PermissionsController : Controller
    {
        private readonly GetPermissionsPageHandler _getPage;
        private readonly GetPermissionForEditHandler _getForEdit;
        private readonly CreatePermissionHandler _create;
        private readonly UpdatePermissionHandler _update;
        private readonly SoftDeletePermissionHandler _softDelete;

        public PermissionsController(
            GetPermissionsPageHandler getPage,
            GetPermissionForEditHandler getForEdit,
            CreatePermissionHandler create,
            UpdatePermissionHandler update,
            SoftDeletePermissionHandler softDelete)
        {
            _getPage = getPage;
            _getForEdit = getForEdit;
            _create = create;
            _update = update;
            _softDelete = softDelete;
        }

        /// <summary>
        /// Displays a paged list of permissions. Supports search by key/display name.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var result = await _getPage.HandleAsync(page, pageSize, q, ct);
            if (!result.Succeeded || result.Value == null)
            {
                TempData["Error"] = result.Error ?? "Failed to load permissions.";
                return View(new PermissionsListVm());
            }

            var pageData = result.Value;
            var listItems = pageData.Items
                .Select(d => new PermissionListItemVm
                {
                    Id = d.Id,
                    Key = d.Key,
                    DisplayName = d.DisplayName,
                    Description = d.Description,
                    IsSystem = d.IsSystem,
                    RowVersion = d.RowVersion
                }).ToList();

            var vm = new PermissionsListVm
            {
                Items = listItems,
                Page = page,
                PageSize = pageSize,
                Total = pageData.TotalCount,
                Query = q ?? string.Empty,
                PageSizeItems = new[]
                {
                    new SelectListItem("10",  "10",  pageSize == 10),
                    new SelectListItem("20",  "20",  pageSize == 20),
                    new SelectListItem("50",  "50",  pageSize == 50),
                    new SelectListItem("100", "100", pageSize == 100),
                }
            };
            return View(vm);
        }

        /// <summary>Shows the create permission form.</summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new PermissionCreateVm());
        }

        /// <summary>
        /// Processes creation of a new permission.
        /// On success, redirects to the index with a success message.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PermissionCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix validation errors and try again.";
                return View(vm);
            }

            var result = await _create.HandleAsync(vm.Key?.Trim() ?? string.Empty, 
                vm.DisplayName?.Trim() ?? string.Empty, 
                string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(), 
                false, ct);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error ?? "Failed to create permission.";
                return View(vm);
            }

            TempData["Success"] = "Permission created successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Loads an existing permission for editing DisplayName and Description.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var result = await _getForEdit.HandleAsync(id, ct);
            if (!result.Succeeded || result.Value is null)
            {
                TempData["Warning"] = result.Error ?? "Permission not found.";
                return RedirectToAction(nameof(Index));
            }

            var dto = result.Value;
            var vm = new PermissionEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                DisplayName = dto.DisplayName ?? string.Empty,
                Description = dto.Description
            };

            // Key/IsSystem are not editable, but we can pass them via ViewBag to display
            ViewBag.Key = dto.Key;
            ViewBag.IsSystem = dto.IsSystem;
            return View(vm);
        }

        /// <summary>
        /// Updates the editable fields of a permission using optimistic concurrency.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PermissionEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix validation errors and try again.";
                return View(vm);
            }

            var dto = new PermissionEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                DisplayName = vm.DisplayName?.Trim() ?? string.Empty,
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim()
            };

            var result = await _update.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error ?? "Failed to update permission.";
                return View(vm);
            }

            TempData["Success"] = "Permission updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Soft deletes the specified permission. System permissions are protected by the Application layer.
        /// Invoked via a confirmation modal in the index/edit views.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                var dto = new PermissionDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };
                var result = await _softDelete.HandleAsync(dto, ct);
                if (!result.Succeeded)
                    TempData["Warning"] = result.Error ?? "Failed to delete permission.";
                else
                    TempData["Success"] = "Permission deleted successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete permission.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
