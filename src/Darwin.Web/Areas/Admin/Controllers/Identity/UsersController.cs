using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Web.Areas.Admin.ViewModels.Identity;
using Darwin.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Web.Areas.Admin.Controllers.Identity
{
    /// <summary>
    /// Admin controller for managing Users (list, create, edit, email/password change, soft delete).
    /// Follows the same conventions and UX patterns used by Roles and other Admin controllers.
    /// </summary>
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    [PermissionAuthorize("AccessAdminPanel")]
    public sealed class UsersController : Controller
    {
        private readonly GetUsersPageHandler _getUsersPage;
        private readonly GetUserForEditHandler _getForEdit;
        private readonly UpdateUserHandler _update;
        private readonly CreateUserHandler _create;
        private readonly ChangeUserEmailHandler _changeEmail;
        private readonly ChangePasswordHandler _changePassword;
        private readonly SoftDeleteUserHandler _softDelete;

        private readonly GetUserWithAddressesForEditHandler _getUserWithAddresses;
        private readonly CreateUserAddressHandler _createAddress;
        private readonly UpdateUserAddressHandler _updateAddress;
        private readonly SoftDeleteUserAddressHandler _softDeleteAddress;
        private readonly SetDefaultUserAddressHandler _setDefaultAddress;

        /// <summary>
        /// Initializes the UsersController with required Application handlers.
        /// </summary>
        public UsersController(
            GetUsersPageHandler getUsersPage,
            GetUserForEditHandler getForEdit,
            UpdateUserHandler update,
            CreateUserHandler create,
            ChangeUserEmailHandler changeEmail,
            ChangePasswordHandler changePassword,
            SoftDeleteUserHandler softDelete,
            GetUserWithAddressesForEditHandler getUserWithAddresses,
            CreateUserAddressHandler createAddress,
            UpdateUserAddressHandler updateAddress,
            SoftDeleteUserAddressHandler softDeleteAddress,
            SetDefaultUserAddressHandler setDefaultAddress)
        {
            _getUsersPage = getUsersPage;
            _getForEdit = getForEdit;
            _update = update;
            _create = create;
            _changeEmail = changeEmail;
            _changePassword = changePassword;
            _softDelete = softDelete;
            _getUserWithAddresses = getUserWithAddresses;
            _createAddress = createAddress;
            _updateAddress = updateAddress;
            _softDeleteAddress = softDeleteAddress;
            _setDefaultAddress = setDefaultAddress;
        }

        /// <summary>
        /// Lists users with simple email filter and paging.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            // Handler returns (Items, Total). Email filter is simple CONTAINS on Email. 
            // See Application handler for details. 
            // Ref: GetUsersPageHandler.HandleAsync(page, pageSize, emailFilter)
            var (items, total) = await _getUsersPage.HandleAsync(page, pageSize, q, ct);

            var vm = new UsersListVm
            {
                Page = page < 1 ? 1 : page,
                PageSize = pageSize <= 0 ? 20 : pageSize,
                Total = total,
                Query = q,
                Items = items.Select(u => new UserListItemVm
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    IsActive = u.IsActive,
                    IsSystem = u.IsSystem
                }).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Renders the "Create User" form.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            // Provide sane defaults for selectable fields (not free-text).
            var vm = new UserCreateVm
            {
                // TODO: Optionally load select lists (Locale/Currency/Timezone) from SiteSetting cache
                //       once those lists are finalized in SiteSetting. For now, the VM can carry static options.
            };
            return View(vm);
        }

        /// <summary>
        /// Creates a new user via Application handler. On success, redirect to Edit.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix the validation errors and try again.";
                return View(vm);
            }

            var dto = new UserCreateDto
            {
                // Email and password come from VM; other profile fields are optional.
                Email = vm.Email?.Trim() ?? string.Empty,
                Password = vm.Password ?? string.Empty,
                FirstName = string.IsNullOrWhiteSpace(vm.FirstName) ? null : vm.FirstName!.Trim(),
                LastName = string.IsNullOrWhiteSpace(vm.LastName) ? null : vm.LastName!.Trim(),
                Locale = vm.Locale,
                Currency = vm.Currency,
                Timezone = vm.Timezone,
                PhoneE164 = vm.PhoneE164,
                IsActive = vm.IsActive
            };

            var result = await _create.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                // Push validator/business errors to ModelState for display
                if (!string.IsNullOrWhiteSpace(result.Error))
                    ModelState.AddModelError(string.Empty, result.Error);

                TempData["Error"] = "Failed to create user.";
                return View(vm);
            }

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Edit), new { id = result.Value }); // value is new UserId
        }

        /// <summary>
        /// Loads user for editing (email and roles are NOT editable here).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var res = await _getForEdit.HandleAsync(id, ct);
            if (!res.Succeeded || res.Value is null)
            {
                TempData["Warning"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var dto = res.Value;
            var vm = new UserEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Locale = dto.Locale,
                Currency = dto.Currency,
                Timezone = dto.Timezone,
                PhoneE164 = dto.PhoneE164,
                IsActive = dto.IsActive
            };

            return View(vm);
        }

        /// <summary>
        /// Updates non-sensitive user fields with optimistic concurrency (RowVersion).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix the validation errors and try again.";
                return View(vm);
            }

            var dto = new UserEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Locale = vm.Locale,
                Currency = vm.Currency,
                Timezone = vm.Timezone,
                PhoneE164 = vm.PhoneE164,
                IsActive = vm.IsActive
            };

            try
            {
                var result = await _update.HandleAsync(dto, ct);
                if (!result.Succeeded)
                {
                    if (!string.IsNullOrWhiteSpace(result.Error))
                        ModelState.AddModelError(string.Empty, result.Error);

                    TempData["Error"] = "Failed to update user.";
                    return View(vm);
                }

                TempData["Success"] = "User updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Concurrency error: show a friendly message, allow retry.
                ModelState.AddModelError(string.Empty, "Concurrency conflict: another admin modified this user. Please reload and try again.");
                return View(vm);
            }
        }

        /// <summary>
        /// GET form for changing user's email (admin-only flow).
        /// </summary>
        [HttpGet]
        public IActionResult ChangeEmail(Guid id, string? currentEmail = null)
        {
            var vm = new UserChangeEmailVm
            {
                Id = id,
                NewEmail = currentEmail
            };
            return View(vm);
        }

        /// <summary>
        /// POST to change user's email via Application handler.
        /// Shows a warning about email confirmation before submission.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(UserChangeEmailVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix the validation errors and try again.";
                return View(vm);
            }

            var dto = new UserChangeEmailDto
            {
                Id = vm.Id,
                NewEmail = vm.NewEmail?.Trim() ?? string.Empty
            };

            var result = await _changeEmail.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(result.Error))
                    ModelState.AddModelError(string.Empty, result.Error);

                TempData["Error"] = "Failed to change email.";
                return View(vm);
            }

            TempData["Success"] = "Email changed. The user must confirm the new email before signing in.";
            return RedirectToAction(nameof(Edit), new { id = vm.Id });
        }

        /// <summary>
        /// GET form for changing user's password (admin-only reset).
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword(Guid id)
        {
            var vm = new UserChangePasswordVm { Id = id };
            return View(vm);
        }

        /// <summary>
        /// POST to change user's password via Application handler.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserChangePasswordVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                TempData["Warning"] = "Please fix the validation errors and try again.";
                return View(vm);
            }

            var dto = new UserChangePasswordDto
            {
                Id = vm.Id,
                CurrentPassword = vm.CurrentPassword ?? string.Empty,
                NewPassword = vm.NewPassword ?? string.Empty
            };

            var result = await _changePassword.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                if (!string.IsNullOrWhiteSpace(result.Error))
                    ModelState.AddModelError(string.Empty, result.Error);

                TempData["Error"] = "Failed to change password.";
                return View(vm);
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Edit), new { id = vm.Id });
        }

        /// <summary>
        /// Deletes (soft) a user via shared confirmation modal.
        /// System users are protected at Application level and will fail with a warning.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)
        {
            try
            {
                var dto = new UserDeleteDto
                {
                    Id = id,
                    RowVersion = Array.Empty<byte>() // Now: For simplicity, no concurrency on delete here
                    // TODO: Implement RowVersion
                };
                var result = await _softDelete.HandleAsync(dto, ct);
                if (!result.Succeeded)
                {
                    TempData["Warning"] = result.Error ?? "Failed to delete user.";
                }
                else
                {
                    TempData["Success"] = "User deleted.";
                }
            }
            catch
            {
                TempData["Error"] = "Failed to delete the user.";
            }
            return RedirectToAction(nameof(Index));
        }



        /// <summary>
        /// Returns the address grid partial for the specified user.
        /// Use this to initially load the grid and to refresh it after CRUD operations.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddressesGrid(Guid userId, CancellationToken ct)
        {
            var r = await _getUserWithAddresses.HandleAsync(userId, ct);
            if (!r.Succeeded|| r.Value is null) return PartialView("~/Areas/Admin/Views/Users/_UserAddressesGrid.cshtml", Array.Empty<UserAddressListItemViewModel>());

            // Map DTO -> VM (only properties needed by the grid).
            var list = r.Value.Addresses.Select(a => new UserAddressListItemVm
            {
                Id = a.Id,
                UserId = userId,
                FullName = a.FullName,
                Company = a.Company,
                Street1 = a.Street1,
                Street2 = a.Street2,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                CountryCode = a.CountryCode,
                PhoneE164 = a.PhoneE164,
                IsDefaultBilling = a.IsDefaultBilling,
                IsDefaultShipping = a.IsDefaultShipping,
                RowVersion = a.RowVersion
            }).ToList();

            return PartialView("~/Areas/Admin/Views/Users/_UserAddressesGrid.cshtml", list);
        }

    }
}
