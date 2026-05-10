using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.ViewModels.Identity;
using Darwin.WebAdmin.Auth;
using Darwin.WebAdmin.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.WebAdmin.Controllers.Admin.Identity
{
    /// <summary>
    /// Admin controller for managing roles. Provides listing with paging/search,
    /// create/edit forms with concurrency handling, and soft delete with confirmation.
    /// Access is limited to administrators with FullAdminAccess.
    /// </summary>
    [PermissionAuthorize("FullAdminAccess")]
    public sealed class RolesController : AdminBaseController
    {
        private readonly GetRolesPageHandler _getRole;
        private readonly GetRoleForEditHandler _getRoleForEdit;
        private readonly CreateRoleHandler _create;
        private readonly UpdateRoleHandler _update;
        private readonly DeleteRoleHandler _delete;
        private readonly GetRoleWithPermissionsForEditHandler _getRolePerms;
        private readonly UpdateRolePermissionsHandler _updateRolePerms;

        /// <summary>
        /// Wires the controller to Application-layer handlers. These handlers encapsulate
        /// validation and persistence logic, keeping the Web layer thin and testable.
        /// </summary>
        public RolesController(
            GetRolesPageHandler getRole,
            GetRoleForEditHandler getRoleForEdit,
            CreateRoleHandler create,
            UpdateRoleHandler update,
            DeleteRoleHandler delete,
            GetRoleWithPermissionsForEditHandler getRolePerms,
            UpdateRolePermissionsHandler updateRolePerms)
        {
            _getRole = getRole ?? throw new ArgumentNullException(nameof(getRole));
            _getRoleForEdit = getRoleForEdit ?? throw new ArgumentNullException(nameof(getRoleForEdit));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _delete = delete ?? throw new ArgumentNullException(nameof(delete));
            _getRolePerms = getRolePerms ?? throw new ArgumentNullException(nameof(getRolePerms));
            _updateRolePerms = updateRolePerms ?? throw new ArgumentNullException(nameof(updateRolePerms));
        }

        /// <summary>
        /// Displays a paged list of roles. Supports simple text search.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, RoleQueueFilter filter = RoleQueueFilter.All, CancellationToken ct = default)
        {
            var (items, _) = await _getRole.HandleAsync(1, 500, q, ct);

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

            var filteredItems = ApplyRoleFilter(listVms, filter).ToList();
            var total = filteredItems.Count;
            var pagedItems = filteredItems
                .Skip((Math.Max(page, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new RolesListItemVm
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildRoleFilterItems(filter),
                Summary = new RoleOpsSummaryVm
                {
                    TotalCount = listVms.Count,
                    SystemCount = listVms.Count(x => x.IsSystem),
                    CustomCount = listVms.Count(x => !x.IsSystem),
                    DelegatedSupportCount = listVms.Count(IsDelegatedSupportRole)
                },
                PageSizeItems = new[]
                {
                    new SelectListItem("10",  "10",  pageSize == 10),
                    new SelectListItem("20",  "20",  pageSize == 20),
                    new SelectListItem("50",  "50",  pageSize == 50),
                    new SelectListItem("100", "100", pageSize == 100),
                }
            };
            return RenderIndexWorkspace(vm);
        }

        /// <summary>
        /// Renders the create form.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            // Default model is empty; business defaults are handled in the Application layer.
            return RenderCreateEditor(new RoleCreateVm());
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
                SetWarningMessage("ValidationErrorsRetry");
                return RenderCreateEditor(model);
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
                SetErrorMessage("RoleCreateFailed");
                return RenderCreateEditor(model);
            }

            SetSuccessMessage("RoleCreated");
            return RedirectOrHtmx(nameof(Index), new { });
        }

        /// <summary>
        /// Renders the edit form for the specified role.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetWarningMessage("RoleNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = await _getRoleForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                SetWarningMessage("RoleNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new RoleEditVm
            {
                Id = dto.Id,
                Key = dto.Key,
                RowVersion = dto.RowVersion,
                DisplayName = dto.DisplayName,
                Description = dto.Description,
                IsSystem = dto.IsSystem
            };
            return RenderEditEditor(vm);
        }

        private static IEnumerable<RoleListItemVm> ApplyRoleFilter(IEnumerable<RoleListItemVm> items, RoleQueueFilter filter)
        {
            return filter switch
            {
                RoleQueueFilter.System => items.Where(x => x.IsSystem),
                RoleQueueFilter.Custom => items.Where(x => !x.IsSystem),
                RoleQueueFilter.DelegatedSupport => items.Where(IsDelegatedSupportRole),
                _ => items
            };
        }

        private static bool IsDelegatedSupportRole(RoleListItemVm item)
        {
            return string.Equals(item.Key, "business-support-admins", StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<SelectListItem> BuildRoleFilterItems(RoleQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new(T("IdentityFilterAll"), RoleQueueFilter.All.ToString(), selected == RoleQueueFilter.All),
                new(T("IdentityFilterSystem"), RoleQueueFilter.System.ToString(), selected == RoleQueueFilter.System),
                new(T("IdentityFilterCustom"), RoleQueueFilter.Custom.ToString(), selected == RoleQueueFilter.Custom),
                new(T("IdentityFilterDelegatedSupport"), RoleQueueFilter.DelegatedSupport.ToString(), selected == RoleQueueFilter.DelegatedSupport)
            };
        }

        /// <summary>
        /// Updates a role with optimistic concurrency. On conflict or validation failure,
        /// returns to the edit view with a friendly message.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleEditVm model, CancellationToken ct = default)
        {
            if (model.Id == Guid.Empty)
            {
                SetWarningMessage("RoleNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                SetWarningMessage("ValidationErrorsRetry");
                return RenderEditEditor(model);
            }

            var dto = new RoleEditDto
            {
                Id = model.Id,
                Key = model.Key,
                RowVersion = model.RowVersion,
                DisplayName = model.DisplayName?.Trim() ?? string.Empty,
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                IsSystem = model.IsSystem
            };

            var result = await _update.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                SetErrorMessage("RoleUpdateFailed");
                return RenderEditEditor(model);
            }

            SetSuccessMessage("RoleUpdated");
            return RedirectOrHtmx(nameof(Edit), new { id = model.Id });
        }

        /// <summary>
        /// Soft deletes a role. System roles are protected and fail with a warning.
        /// This action is invoked via the shared confirmation modal.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("RoleDeleteFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _delete.HandleAsync(id, DecodeBase64RowVersion(rowVersion), ct);
            if (result.Succeeded)
                SetSuccessMessage("RoleDeleted");
            else
                SetErrorMessage("RoleDeleteFailed");

            return RedirectOrHtmx(nameof(Index), new { });
        }

        private IActionResult RenderCreateEditor(RoleCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Roles/_RoleCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderIndexWorkspace(RolesListItemVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Roles/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderEditEditor(RoleEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Roles/_RoleEditEditorShell.cshtml", vm);
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



        /// <summary>
        /// Shows a checklist of all permissions for a specific role and marks selected ones.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Permissions(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("RolePermissionsLoadFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = await BuildRolePermissionsVmAsync(id, null, null, ct);
            if (vm is null)
            {
                SetErrorMessage("RolePermissionsLoadFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            return RenderPermissionsEditor(vm);
        }

        /// <summary>
        /// Saves the role-permission assignment changes and redirects back to Roles/Index.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permissions(RolePermissionsEditVm vm, CancellationToken ct = default)
        {
            if (vm.RoleId == Guid.Empty)
            {
                SetErrorMessage("RolePermissionsLoadFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = await BuildRolePermissionsVmAsync(vm.RoleId, vm.SelectedPermissionIds, vm.RowVersion, ct);
                return invalidVm is null ? RedirectOrHtmx(nameof(Index), new { }) : RenderPermissionsEditor(invalidVm);
            }

            var dto = new RolePermissionsUpdateDto
            {
                RoleId = vm.RoleId,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                PermissionIds = vm.SelectedPermissionIds?.ToList() ?? new List<Guid>()
            };

            var result = await _updateRolePerms.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                SetErrorMessage("RolePermissionsUpdateFailed");
                var failedVm = await BuildRolePermissionsVmAsync(vm.RoleId, vm.SelectedPermissionIds, vm.RowVersion, ct);
                return failedVm is null ? RedirectOrHtmx(nameof(Index), new { }) : RenderPermissionsEditor(failedVm);
            }

            SetSuccessMessage("RolePermissionsUpdated");
            return RedirectOrHtmx(nameof(Permissions), new { id = vm.RoleId });
        }

        private IActionResult RenderPermissionsEditor(RolePermissionsEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Roles/_RolePermissionsEditorShell.cshtml", vm);
            }

            return View("Permissions", vm);
        }

        private async Task<RolePermissionsEditVm?> BuildRolePermissionsVmAsync(
            Guid roleId,
            IEnumerable<Guid>? selectedPermissionIds,
            byte[]? rowVersion,
            CancellationToken ct)
        {
            if (roleId == Guid.Empty)
            {
                return null;
            }

            var result = await _getRolePerms.HandleAsync(roleId, ct);
            if (!result.Succeeded || result.Value is null)
            {
                return null;
            }

            var dto = result.Value;
            return new RolePermissionsEditVm
            {
                RoleId = dto.RoleId,
                RoleDisplayName = dto.RoleDisplayName,
                RowVersion = rowVersion ?? dto.RowVersion,
                AllPermissions = dto.AllPermissions.Select(p => new PermissionItemVm
                {
                    Id = p.Id,
                    Key = p.Key,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    IsSystem = p.IsSystem,
                    RowVersion = p.RowVersion
                }).ToList(),
                SelectedPermissionIds = selectedPermissionIds?.ToList() ?? dto.PermissionIds.ToList()
            };
        }
    }
}
