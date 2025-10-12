using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;
using Darwin.Web.Areas.Admin.Infrastructure;
using Darwin.Web.Areas.Admin.ViewModels.Identity;
using Darwin.Web.Security;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Identity
{
    /// <summary>
    /// CRUD for Role entities in the Admin area. The controller follows the same
    /// pattern used by other Admin controllers: thin layer, delegates to Application
    /// handlers, uses TempData for post-redirect alerts, and relies on permission-
    /// based authorization.
    /// </summary>
    [Area("Admin")]
    [PermissionAuthorize("FullAdminAccess")] // require super-admin for role management
    public sealed class RolesController : AdminBaseController
    {
        private readonly GetRolesPageHandler _getPage;
        private readonly GetRoleForEditHandler _getForEdit;
        private readonly CreateRoleHandler _create;
        private readonly UpdateRoleHandler _update;
        private readonly DeleteRoleHandler _delete;

        /// <summary>
        /// Wires all required application handlers for the Role CRUD flows.
        /// </summary>
        public RolesController(
            GetRolesPageHandler getPage,
            GetRoleForEditHandler getForEdit,
            CreateRoleHandler create,
            UpdateRoleHandler update,
            DeleteRoleHandler delete)
        {
            _getPage = getPage;
            _getForEdit = getForEdit;
            _create = create;
            _update = update;
            _delete = delete;
        }

        /// <summary>
        /// Displays paginated list of roles with optional search.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int size = 20, string? q = null, CancellationToken ct = default)
        {
            var res = await _getPage.HandleAsync(new RolePageQueryDto { Page = page, Size = size, Search = q }, ct);
            if (!res.Succeeded) { TempData["Error"] = res.Error ?? "Failed to load roles."; return View("Index", RoleIndexVm.Empty()); }

            var vm = RoleIndexVm.From(res.Value);
            vm.Search = q;
            return View("Index", vm);
        }

        /// <summary>
        /// Renders the empty create form.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View("Create", new RoleCreateVm());
        }

        /// <summary>
        /// Creates a new role.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View("Create", vm);

            var dto = new RoleCreateDto { Name = vm.Name?.Trim() ?? string.Empty, Description = vm.Description?.Trim() };
            var res = await _create.HandleAsync(dto, ct);
            if (!res.Succeeded)
            {
                TempData["Error"] = res.Error ?? "Failed to create role.";
                return View("Create", vm);
            }

            TempData["Success"] = "Role created successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Renders the edit form with current role data.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var res = await _getForEdit.HandleAsync(id, ct);
            if (!res.Succeeded || res.Value is null)
            {
                TempData["Error"] = res.Error ?? "Role not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new RoleEditVm
            {
                Id = res.Value.Id,
                RowVersion = res.Value.RowVersion,
                Name = res.Value.Name,
                Description = res.Value.Description
            };
            return View("Edit", vm);
        }

        /// <summary>
        /// Saves the edited role.
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View("Edit", vm);

            var dto = new RoleEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name?.Trim() ?? string.Empty,
                Description = vm.Description?.Trim()
            };
            var res = await _update.HandleAsync(dto, ct);
            if (!res.Succeeded)
            {
                TempData["Error"] = res.Error ?? "Failed to update role.";
                return View("Edit", vm);
            }

            TempData["Success"] = "Role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Deletes a role (soft-delete).
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            var res = await _delete.HandleAsync(id, ct);
            if (!res.Succeeded)
            {
                TempData["Error"] = res.Error ?? "Failed to delete role.";
            }
            else
            {
                TempData["Success"] = "Role deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
