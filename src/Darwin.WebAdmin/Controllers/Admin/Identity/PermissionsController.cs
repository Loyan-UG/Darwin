using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.ViewModels.Identity;
using Darwin.WebAdmin.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.Controllers.Admin.Identity
{
    /// <summary>
    /// Admin controller for managing permissions.
    /// Provides list with paging/search, create/edit forms and soft delete.
    /// Key and IsSystem values are immutable once created.
    /// </summary>
    [PermissionAuthorize("FullAdminAccess")]
    public sealed class PermissionsController : AdminBaseController
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
            _getPage = getPage ?? throw new ArgumentNullException(nameof(getPage));
            _getForEdit = getForEdit ?? throw new ArgumentNullException(nameof(getForEdit));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _softDelete = softDelete ?? throw new ArgumentNullException(nameof(softDelete));
        }

        /// <summary>
        /// Displays a paged list of permissions. Supports search by key/display name.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, PermissionQueueFilter filter = PermissionQueueFilter.All, CancellationToken ct = default)
        {
            var result = await _getPage.HandleAsync(1, 500, q, ct);
            if (!result.Succeeded || result.Value == null)
            {
                SetErrorMessage("PermissionsLoadFailed");
                return RenderIndexWorkspace(new PermissionsListVm());
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

            var filteredItems = ApplyPermissionFilter(listItems, filter).ToList();
            var total = filteredItems.Count;
            var pagedItems = filteredItems
                .Skip((Math.Max(page, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new PermissionsListVm
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildPermissionFilterItems(filter),
                Summary = new PermissionOpsSummaryVm
                {
                    TotalCount = listItems.Count,
                    SystemCount = listItems.Count(x => x.IsSystem),
                    CustomCount = listItems.Count(x => !x.IsSystem),
                    DelegatedSupportCount = listItems.Count(IsDelegatedSupportPermission)
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

        private static IEnumerable<PermissionListItemVm> ApplyPermissionFilter(IEnumerable<PermissionListItemVm> items, PermissionQueueFilter filter)
        {
            return filter switch
            {
                PermissionQueueFilter.System => items.Where(x => x.IsSystem),
                PermissionQueueFilter.Custom => items.Where(x => !x.IsSystem),
                PermissionQueueFilter.DelegatedSupport => items.Where(IsDelegatedSupportPermission),
                _ => items
            };
        }

        private static bool IsDelegatedSupportPermission(PermissionListItemVm item)
        {
            return string.Equals(item.Key, "ManageBusinessSupport", StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerable<SelectListItem> BuildPermissionFilterItems(PermissionQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new(T("IdentityFilterAll"), PermissionQueueFilter.All.ToString(), selected == PermissionQueueFilter.All),
                new(T("IdentityFilterSystem"), PermissionQueueFilter.System.ToString(), selected == PermissionQueueFilter.System),
                new(T("IdentityFilterCustom"), PermissionQueueFilter.Custom.ToString(), selected == PermissionQueueFilter.Custom),
                new(T("IdentityFilterDelegatedSupport"), PermissionQueueFilter.DelegatedSupport.ToString(), selected == PermissionQueueFilter.DelegatedSupport)
            };
        }

        /// <summary>Shows the create permission form.</summary>
        [HttpGet]
        public IActionResult Create()
        {
            return RenderCreateEditor(new PermissionCreateVm());
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
                SetWarningMessage("ValidationErrorsRetry");
                return RenderCreateEditor(vm);
            }

            var result = await _create.HandleAsync(vm.Key?.Trim() ?? string.Empty, 
                vm.DisplayName?.Trim() ?? string.Empty, 
                string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(), 
                false, ct);
            if (!result.Succeeded)
            {
                SetErrorMessage("PermissionCreateFailed");
                return RenderCreateEditor(vm);
            }

            SetSuccessMessage("PermissionCreated");
            return RedirectOrHtmx(nameof(Index), new { });
        }

        /// <summary>
        /// Loads an existing permission for editing DisplayName and Description.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetWarningMessage("PermissionNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _getForEdit.HandleAsync(id, ct);
            if (!result.Succeeded || result.Value is null)
            {
                SetWarningMessage("PermissionNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = result.Value;
            var vm = new PermissionEditVm
            {
                Id = dto.Id,
                Key = dto.Key,
                RowVersion = dto.RowVersion,
                DisplayName = dto.DisplayName ?? string.Empty,
                Description = dto.Description,
                IsSystem = dto.IsSystem
            };

            return RenderEditEditor(vm);
        }

        /// <summary>
        /// Updates the editable fields of a permission using optimistic concurrency.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PermissionEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetWarningMessage("PermissionNotFound");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            if (!ModelState.IsValid)
            {
                SetWarningMessage("ValidationErrorsRetry");
                return RenderEditEditor(vm);
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
                SetErrorMessage("PermissionUpdateFailed");
                return RenderEditEditor(vm);
            }

            SetSuccessMessage("PermissionUpdated");
            return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
        }

        /// <summary>
        /// Soft deletes the specified permission. System permissions are protected by the Application layer.
        /// Invoked via a confirmation modal in the index/edit views.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("PermissionDeleteFailed");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = new PermissionDeleteDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) };
            var result = await _softDelete.HandleAsync(dto, ct);
            if (result.Succeeded)
                SetSuccessMessage("PermissionDeleted");
            else
                SetErrorMessage("PermissionDeleteFailed");

            return RedirectOrHtmx(nameof(Index), new { });
        }

        private IActionResult RenderCreateEditor(PermissionCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Permissions/_PermissionCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderIndexWorkspace(PermissionsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Permissions/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderEditEditor(PermissionEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Permissions/_PermissionEditEditorShell.cshtml", vm);
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

    }
}
