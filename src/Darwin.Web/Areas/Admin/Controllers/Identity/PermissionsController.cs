using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Application.Identity.Commands;
using Darwin.Web.Security;
using Darwin.Web.Areas.Admin.ViewModels.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers.Identity
{
    /// <summary>
    /// Admin CRUD for permissions. 
    /// </summary>
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    [PermissionAuthorize("AccessAdminPanel")]
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

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var (items, total) = await _getPage.HandleAsync(q, page, pageSize, ct);

            var vm = new PermissionIndexVm
            {
                Q = q,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(p => new PermissionRowVm
                {
                    Id = p.Id,
                    Key = p.Key,
                    DisplayName = p.DisplayName,
                    IsSystem = p.IsSystem,
                    ModifiedAtUtc = p.ModifiedAtUtc,
                    RowVersion = p.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create() => View(new PermissionCreateVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PermissionCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new PermissionCreateDto
            {
                Key = vm.Key.Trim(),
                DisplayName = vm.DisplayName.Trim(),
                Description = vm.Description?.Trim()
            };

            var res = await _create.HandleAsync(dto, ct);
            if (!res.Succeeded)
            {
                TempData["Error"] = res.Error ?? "Failed to create permission.";
                return View(vm);
            }

            TempData["Success"] = "Permission created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getForEdit.HandleAsync(id, ct);
            if (dto == null)
            {
                TempData["Error"] = "Permission not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new PermissionEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                DisplayName = dto.DisplayName ?? string.Empty,
                Description = dto.Description,
                Key = dto.Key,
                IsSystem = dto.IsSystem
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PermissionEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new PermissionEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                DisplayName = vm.DisplayName.Trim(),
                Description = vm.Description?.Trim()
            };

            var res = await _update.HandleAsync(dto, ct);
            if (!res.Succeeded)
            {
                TempData["Error"] = res.Error ?? "Failed to update permission.";
                return View(vm);
            }

            TempData["Success"] = "Permission updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _softDelete.HandleAsync(id, rowVersion, ct);
                TempData["Success"] = "Permission deleted.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete the permission.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
