using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;
using Darwin.Web.Areas.Admin.ViewModels.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Darwin.Web.Areas.Admin.Controllers.Identity
{
    /// <summary>
    /// Admin controller responsible for managing users and their addresses.
    /// Aligns strictly with Application DTOs/Handlers and Admin ViewModels present in the solution.
    /// </summary>
    [Area("Admin")]
    public sealed class UsersController : AdminBaseController
    {
        // User handlers
        private readonly RegisterUserHandler _registerUser;
        private readonly GetUsersPageHandler _getUsersPage;
        private readonly GetUserWithAddressesForEditHandler _getUserWithAddresses;
        private readonly UpdateUserHandler _updateUser;
        private readonly ChangeUserEmailHandler _changeUserEmail;
        private readonly ChangePasswordHandler _changePassword;
        private readonly SoftDeleteUserHandler _softDeleteUser;

        // Address handlers
        private readonly CreateUserAddressHandler _createAddress;
        private readonly UpdateUserAddressHandler _updateAddress;
        private readonly SoftDeleteUserAddressHandler _softDeleteAddress;
        private readonly SetDefaultUserAddressHandler _setDefaultAddress;

        /// <summary>
        /// Wires Application-layer handlers. These encapsulate validation and persistence.
        /// </summary>
        public UsersController(
            RegisterUserHandler registerUser,
            GetUsersPageHandler getUsersPage,
            GetUserWithAddressesForEditHandler getUserWithAddresses,
            UpdateUserHandler updateUser,
            ChangeUserEmailHandler changeUserEmail,
            ChangePasswordHandler changePassword,
            SoftDeleteUserHandler softDeleteUser,
            CreateUserAddressHandler createAddress,
            UpdateUserAddressHandler updateAddress,
            SoftDeleteUserAddressHandler softDeleteAddress,
            SetDefaultUserAddressHandler setDefaultAddress)
        {
            _registerUser = registerUser;
            _getUsersPage = getUsersPage;
            _getUserWithAddresses = getUserWithAddresses;
            _updateUser = updateUser;
            _changeUserEmail = changeUserEmail;
            _changePassword = changePassword;
            _softDeleteUser = softDeleteUser;
            _createAddress = createAddress;
            _updateAddress = updateAddress;
            _softDeleteAddress = softDeleteAddress;
            _setDefaultAddress = setDefaultAddress;
        }

        /// <summary>
        /// Lists users with paging and optional text filter (email/name).
        /// Handler signature follows the Roles pattern: (page, pageSize, q, ct).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            // Handler returns application-level items/total; mapping to list VM mirrors RolesController pattern.
            var (items, total) = await _getUsersPage.HandleAsync(page, pageSize, q, ct);

            var vm = new UsersListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Items = items.Select(x => new UserListItemVm
                {
                    Id = x.Id,
                    Email = x.Email,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    IsActive = x.IsActive,
                    IsSystem = x.IsSystem,
                    RowVersion = x.RowVersion
                }).ToList(),
                PageSizeItems =
                [
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("10",  "10",  pageSize == 10),
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("20",  "20",  pageSize == 20),
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("50",  "50",  pageSize == 50),
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem("100", "100", pageSize == 100),
                ]
            };

            return View(vm);
        }

        /// <summary>
        /// Renders the create form. Locale/Currency/Timezone are rendered by SettingSelectTagHelper.
        /// </summary>
        [HttpGet]
        public IActionResult Create() => View(new UserCreateVm());

        /// <summary>
        /// Creates a new user. On success, redirects to Edit so that addresses can be managed.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

            var dto = new UserCreateDto
            {
                Email = vm.Email,
                Password = vm.Password,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Locale = vm.Locale,
                Currency = vm.Currency,
                Timezone = vm.Timezone,
                PhoneE164 = vm.PhoneE164
            };

            var result = await _registerUser.HandleAsync(dto, defaultRoleId: null, ct);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create user.");
                return View(vm);
            }

            TempData["Success"] = "User created.";
            return RedirectToAction(nameof(Edit), new { id = result.Value });
        }

        /// <summary>
        /// Loads user and addresses for editing. Email and roles are NOT editable here.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var result = await _getUserWithAddresses.HandleAsync(id, ct);
            if (!result.Succeeded || result.Value is null)
            {
                TempData["Error"] = result.Error ?? "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Map user -> edit VM
            var u = result.Value;
            var vm = new UserEditVm
            {
                Id = u.Id,
                RowVersion = u.RowVersion,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Locale = u.Locale,
                Currency = u.Currency,
                Timezone = u.Timezone,
                PhoneE164 = u.PhoneE164,
                IsActive = u.IsActive
                // NOTE: IsSystem is intentionally NOT part of UserEditVm per model.
            };

            // Prepare addresses section VM for partial rendering
            ViewBag.AddressesSection = new UserAddressesSectionVm
            {
                UserId = u.Id,
                Items = result.Value.Addresses.Select(a => new UserAddressListItemVm
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    Company = a.Company,
                    Street1 = a.Street1,
                    Street2 = a.Street2,
                    PostalCode = a.PostalCode,
                    City = a.City,
                    State = a.State,
                    CountryCode = a.CountryCode,
                    PhoneE164 = a.PhoneE164,
                    IsDefaultBilling = a.IsDefaultBilling,
                    IsDefaultShipping = a.IsDefaultShipping,
                    RowVersion = a.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        /// <summary>
        /// Updates user profile fields (except Email/Roles). Handles optimistic concurrency.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return View(vm);

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

            var result = await _updateUser.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                // Could be validation or concurrency; Application layer provides message.
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update user.");
                return View(vm);
            }

            TempData["Success"] = "User updated.";
            return RedirectToAction(nameof(Edit), new { id = vm.Id });
        }




        /// <summary>
        /// GET form for changing a user's email (admin-only).
        /// The current email is displayed read-only (supplied via query string to avoid extra round-trip).
        /// </summary>
        [HttpGet]
        public IActionResult ChangeEmail([FromRoute] Guid id, [FromQuery] string? currentEmail = null)
        {
            // Prepare view model with the user id. NewEmail is intentionally left blank to force admin input.
            var vm = new UserChangeEmailVm
            {
                Id = id,
                NewEmail = string.Empty
            };

            // Current email is shown read-only in the UI; not part of the VM by design.
            ViewBag.CurrentEmail = currentEmail ?? string.Empty;
            return View(vm);
        }

        /// <summary>
        /// POST: Changes user's email using the Application handler.
        /// Shows a warning that the new email requires confirmation; upon success returns to the users list.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(UserChangeEmailVm vm, CancellationToken ct = default)
        {
            // Validate input on the same page to preserve validation messages and user input.
            if (!ModelState.IsValid)
                return View(vm);

            var dto = new UserChangeEmailDto
            {
                Id = vm.Id,
                NewEmail = vm.NewEmail?.Trim() ?? string.Empty
            };

            var result = await _changeUserEmail.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                // Application layer provides a meaningful error message when available.
                if (!string.IsNullOrWhiteSpace(result.Error))
                    ModelState.AddModelError(string.Empty, result.Error);

                TempData["Error"] = "Failed to change email.";
                return View(vm);
            }

            TempData["Success"] = "Email change requested. The user must confirm the new email before signing in.";
            // After successful operations (email/password), we go back to Index as requested.
            return RedirectToAction(nameof(Index));
        }





        [HttpGet]
        public IActionResult ChangePassword() => View(new UserChangePasswordVm());


        /// <summary>
        /// Changes password via Application handler. Proper error messages are surfaced via Result.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserChangePasswordVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return RedirectToAction(nameof(Edit), new { id = vm.Id });

            var dto = new UserChangePasswordDto
            {
                Id = vm.Id,
                NewPassword = vm.NewPassword
            };

            var result = await _changePassword.HandleAsync(dto, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Password changed."
                : (result.Error ?? "Failed to change password.");

            return RedirectToAction(nameof(Edit), new { id = vm.Id });
        }




        /// <summary>
        /// Soft-deletes a user. Requires the DTO (Id + RowVersion).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            var dto = new UserDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };
            var result = await _softDeleteUser.HandleAsync(dto, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "User deleted." : (result.Error ?? "Failed to delete user.");
            return RedirectToAction(nameof(Index));
        }

        // -------------------- Addresses (Partial Grid + AJAX refresh) --------------------

        /// <summary>
        /// Returns the addresses section partial for the specified user.
        /// Use this endpoint to (re)render the grid after create/update/delete/default operations.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddressesSection(Guid userId, CancellationToken ct = default)
        {
            var result = await _getUserWithAddresses.HandleAsync(userId, ct);
            if (!result.Succeeded || result.Value is null)
                return PartialView("~/Areas/Admin/Views/Users/_AddressesSection.cshtml",
                    new UserAddressesSectionVm { UserId = userId, Items = [] });

            var section = new UserAddressesSectionVm
            {
                UserId = userId,
                Items = result.Value.Addresses.Select(a => new UserAddressListItemVm
                {
                    Id = a.Id,
                    FullName = a.FullName,
                    Company = a.Company,
                    Street1 = a.Street1,
                    Street2 = a.Street2,
                    PostalCode = a.PostalCode,
                    City = a.City,
                    State = a.State,
                    CountryCode = a.CountryCode,
                    PhoneE164 = a.PhoneE164,
                    IsDefaultBilling = a.IsDefaultBilling,
                    IsDefaultShipping = a.IsDefaultShipping,
                    RowVersion = a.RowVersion
                }).ToList()
            };
            return PartialView("~/Areas/Admin/Views/Users/_AddressesSection.cshtml", section);
        }

        /// <summary>
        /// Creates a new address. On success returns the refreshed addresses section partial.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAddress(UserAddressEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid address.");

            var dto = new AddressCreateDto
            {
                UserId = vm.UserId,
                FullName = vm.FullName,
                Company = vm.Company,
                Street1 = vm.Street1,
                Street2 = vm.Street2,
                PostalCode = vm.PostalCode,
                City = vm.City,
                State = vm.State,
                CountryCode = vm.CountryCode,
                PhoneE164 = vm.PhoneE164,
                IsDefaultBilling = vm.IsDefaultBilling,
                IsDefaultShipping = vm.IsDefaultShipping
            };

            var result = await _createAddress.HandleAsync(dto, ct);
            if (!result.Succeeded) return BadRequest(result.Error ?? "Failed to create address.");
            return await AddressesSection(vm.UserId, ct);
        }

        /// <summary>
        /// Updates an address (with optimistic concurrency). On success returns refreshed section.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(UserAddressEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid address.");

            var dto = new AddressEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                FullName = vm.FullName,
                Company = vm.Company,
                Street1 = vm.Street1,
                Street2 = vm.Street2,
                PostalCode = vm.PostalCode,
                City = vm.City,
                State = vm.State,
                CountryCode = vm.CountryCode,
                PhoneE164 = vm.PhoneE164,
                IsDefaultBilling = vm.IsDefaultBilling,
                IsDefaultShipping = vm.IsDefaultShipping
            };

            var result = await _updateAddress.HandleAsync(dto, ct);
            if (!result.Succeeded) return BadRequest(result.Error ?? "Failed to update address.");
            return await AddressesSection(vm.UserId, ct);
        }

        /// <summary>
        /// Soft-deletes an address via confirmation modal. Returns refreshed section.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress([FromForm] Guid id, [FromForm] Guid userId, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            var dto = new AddressDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };
            var result = await _softDeleteAddress.HandleAsync(dto, ct);
            if (!result.Succeeded) return BadRequest(result.Error ?? "Failed to delete address.");
            return await AddressesSection(userId, ct);
        }

        /// <summary>
        /// Sets an address as default billing or shipping. Returns refreshed section.
        /// Expects kind to be either "Billing" or "Shipping".
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress([FromForm] Guid id, [FromForm] Guid userId, [FromForm] string kind, CancellationToken ct = default)
        {
            var asBilling = string.Equals(kind, "Billing", StringComparison.OrdinalIgnoreCase);
            var asShipping = string.Equals(kind, "Shipping", StringComparison.OrdinalIgnoreCase);

            var result = await _setDefaultAddress.HandleAsync(userId, id, asBilling, asShipping, ct);
            if (!result.Succeeded) return BadRequest(result.Error ?? "Failed to set default.");

            return await AddressesSection(userId, ct);
        }
    }
}
