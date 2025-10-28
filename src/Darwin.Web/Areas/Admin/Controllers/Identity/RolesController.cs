using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Web.Areas.Admin.Controllers;
using Darwin.Web.Areas.Admin.ViewModels.Identity;
using Darwin.Web.Auth;
using Darwin.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Web.Areas.Admin.Controllers.Identity
{
    /// <summary>
    /// Admin controller for managing roles. Provides listing with paging/search,
    /// create/edit forms with concurrency handling, and soft delete with confirmation.
    /// Access is limited to administrators with FullAdminAccess.
    /// </summary>
    [Area("Admin")]
    [PermissionAuthorize("FullAdminAccess")]
    public sealed class RolesController : AdminBaseController
    {
        private readonly GetRolesPageHandler _getRole;
        private readonly GetRoleForEditHandler _getRoleForEdit;
        private readonly CreateRoleHandler _create;
        private readonly UpdateRoleHandler _update;
        private readonly DeleteRoleHandler _delete;

        /// <summary>
        /// Wires the controller to Application-layer handlers. These handlers encapsulate
        /// validation and persistence logic, keeping the Web layer thin and testable.
        /// </summary>
        public RolesController(
            GetRolesPageHandler getRole,
            GetRoleForEditHandler getRoleForEdit,
            CreateRoleHandler create,
            UpdateRoleHandler update,
            DeleteRoleHandler delete)
        {
            _getRole = getRole;
            _getRoleForEdit = getRoleForEdit;
            _create = create;
            _update = update;
            _delete = delete;
        }

        /// <summary>
        /// Displays a paged list of roles. Supports simple text search.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _getRole.HandleAsync(page, pageSize, q, ct);

            // Map Application DTOs to lightweight view models for listing.
            var listVms = items.Select(dto => new RoleListItemVm
            {
                Id = dto.Id,
                Key = dto.Key,
                DisplayName = dto.DisplayName,
                Description = dto.Description,
                RowVersion = dto.RowVersion,
                IsSystem = dto.IsSystem
            }).ToList();

            var vm = new RolesListItemVm
            {
                Items = listVms,
                Page = page,
                PageSize = pageSize,
                Total = total,
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

        /// <summary>
        /// Renders the create form.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            // Default model is empty; business defaults are handled in the Application layer.
            return View(new RoleCreateVm());
        }

        /// <summary>
        /// Processes creation of a new role. On success, redirects to the list with a success message.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleCreateVm model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix validation errors and try again.";
                return View(model);
            }

            var dto = new RoleCreateDto
            {
                Key = model.Key?.Trim() ?? string.Empty,
                DisplayName = model.DisplayName?.Trim() ?? string.Empty,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
            };

            var result = await _create.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error ?? "Failed to create role.";
                return View(model);
            }

            TempData["Success"] = "Role created successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Renders the edit form for the specified role.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getRoleForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                TempData["Warning"] = "Role not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new RoleEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                DisplayName = dto.DisplayName,
                Description = dto.Description
            };
            return View(vm);
        }

        /// <summary>
        /// Updates a role with optimistic concurrency. On conflict or validation failure,
        /// returns to the edit view with a friendly message.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleEditDto model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix validation errors and try again.";
                return View(model);
            }

            var dto = new RoleEditDto
            {
                Id = model.Id,
                RowVersion = model.RowVersion,
                DisplayName = model.DisplayName?.Trim() ?? string.Empty,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim()
            };

            var result = await _update.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error ?? "Failed to update role.";
                return View(model);
            }

            TempData["Success"] = "Role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Soft deletes a role. System roles are protected and fail with a warning.
        /// This action is invoked via the shared confirmation modal.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)
        {
            try
            {
                await _delete.HandleAsync(id, ct);
                TempData["Success"] = "Role deleted successfully.";
            }
            catch (InvalidOperationException ex)
            {
                // System role or business rule violation.
                TempData["Warning"] = string.IsNullOrWhiteSpace(ex.Message)
                    ? "This role is system-protected and cannot be deleted."
                    : ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to delete role.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
