using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;
using Darwin.WebAdmin.ViewModels.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.Controllers.Admin.Identity
{
    /// <summary>
    /// Admin controller responsible for managing users and their addresses.
    /// Aligns strictly with Application DTOs/Handlers and Admin ViewModels present in the solution.
    /// </summary>
    public sealed class UsersController : AdminBaseController
    {
        // User handlers
        private readonly RegisterUserHandler _registerUser;
        private readonly GetUsersPageHandler _getUsersPage;
        private readonly GetUserOpsSummaryHandler _getUserOpsSummary;
        private readonly GetUserWithAddressesForEditHandler _getUserWithAddresses;
        private readonly UpdateUserHandler _updateUser;
        private readonly ChangeUserEmailHandler _changeUserEmail;
        private readonly SetUserPasswordByAdminHandler _setUserPasswordByAdmin;
        private readonly RequestPasswordResetHandler _requestPasswordReset;
        private readonly RequestEmailConfirmationHandler _requestEmailConfirmation;
        private readonly ConfirmUserEmailByAdminHandler _confirmUserEmail;
        private readonly LockUserByAdminHandler _lockUser;
        private readonly UnlockUserByAdminHandler _unlockUser;
        private readonly SoftDeleteUserHandler _softDeleteUser;

        // Address handlers
        private readonly CreateUserAddressHandler _createAddress;
        private readonly UpdateUserAddressHandler _updateAddress;
        private readonly SoftDeleteUserAddressHandler _softDeleteAddress;
        private readonly SetDefaultUserAddressHandler _setDefaultAddress;

        // Users Roles Handler
        private readonly GetUserWithRolesForEditHandler _getUserRoles;
        private readonly UpdateUserRolesHandler _updateUserRoles;


        /// <summary>
        /// Wires Application-layer handlers. These encapsulate validation and persistence.
        /// </summary>
        public UsersController(
            RegisterUserHandler registerUser,
            GetUsersPageHandler getUsersPage,
            GetUserOpsSummaryHandler getUserOpsSummary,
            GetUserWithAddressesForEditHandler getUserWithAddresses,
            UpdateUserHandler updateUser,
            ChangeUserEmailHandler changeUserEmail,
            SetUserPasswordByAdminHandler setUserPasswordByAdmin,
            RequestPasswordResetHandler requestPasswordReset,
            RequestEmailConfirmationHandler requestEmailConfirmation,
            ConfirmUserEmailByAdminHandler confirmUserEmail,
            LockUserByAdminHandler lockUser,
            UnlockUserByAdminHandler unlockUser,
            SoftDeleteUserHandler softDeleteUser,
            CreateUserAddressHandler createAddress,
            UpdateUserAddressHandler updateAddress,
            SoftDeleteUserAddressHandler softDeleteAddress,
            SetDefaultUserAddressHandler setDefaultAddress,
            GetUserWithRolesForEditHandler getUserRoles,
            UpdateUserRolesHandler updateUserRoles)
        {
            _registerUser = registerUser;
            _getUsersPage = getUsersPage;
            _getUserOpsSummary = getUserOpsSummary;
            _getUserWithAddresses = getUserWithAddresses;
            _updateUser = updateUser;
            _changeUserEmail = changeUserEmail;
            _setUserPasswordByAdmin = setUserPasswordByAdmin;
            _requestPasswordReset = requestPasswordReset;
            _requestEmailConfirmation = requestEmailConfirmation;
            _confirmUserEmail = confirmUserEmail;
            _lockUser = lockUser;
            _unlockUser = unlockUser;
            _softDeleteUser = softDeleteUser;
            _createAddress = createAddress;
            _updateAddress = updateAddress;
            _softDeleteAddress = softDeleteAddress;
            _setDefaultAddress = setDefaultAddress;
            _getUserRoles = getUserRoles;
            _updateUserRoles = updateUserRoles;
        }

        /// <summary>
        /// Lists users with paging and optional text filter (email/name).
        /// Handler signature follows the Roles pattern: (page, pageSize, q, ct).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, UserQueueFilter filter = UserQueueFilter.All, CancellationToken ct = default)
        {
            // Handler returns application-level items/total; mapping to list VM mirrors RolesController pattern.
            var (items, total) = await _getUsersPage.HandleAsync(page, pageSize, q, filter, ct);
            var summary = await _getUserOpsSummary.HandleAsync(ct);

            var vm = new UsersListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                Summary = new UserOpsSummaryVm
                {
                    TotalCount = summary.TotalCount,
                    UnconfirmedCount = summary.UnconfirmedCount,
                    LockedCount = summary.LockedCount,
                    InactiveCount = summary.InactiveCount,
                    MobileLinkedCount = summary.MobileLinkedCount
                },
                Playbooks = BuildUserSupportPlaybooks(),
                Items = items.Select(x => new UserListItemVm
                {
                    Id = x.Id,
                    Email = x.Email,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    PhoneE164 = x.PhoneE164,
                    IsActive = x.IsActive,
                    IsSystem = x.IsSystem,
                    EmailConfirmed = x.EmailConfirmed,
                    LockoutEndUtc = x.LockoutEndUtc,
                    MobileDeviceCount = x.MobileDeviceCount,
                    RowVersion = x.RowVersion
                }).ToList(),
                FilterItems = BuildUserFilterItems(filter),
                PageSizeItems =
                [
                    new SelectListItem("10",  "10",  pageSize == 10),
                    new SelectListItem("20",  "20",  pageSize == 20),
                    new SelectListItem("50",  "50",  pageSize == 50),
                    new SelectListItem("100", "100", pageSize == 100),
                ]
            };

            return View(vm);
        }

        private static IEnumerable<SelectListItem> BuildUserFilterItems(UserQueueFilter selectedFilter)
        {
            yield return new SelectListItem("All users", UserQueueFilter.All.ToString(), selectedFilter == UserQueueFilter.All);
            yield return new SelectListItem("Unconfirmed", UserQueueFilter.Unconfirmed.ToString(), selectedFilter == UserQueueFilter.Unconfirmed);
            yield return new SelectListItem("Locked", UserQueueFilter.Locked.ToString(), selectedFilter == UserQueueFilter.Locked);
            yield return new SelectListItem("Inactive", UserQueueFilter.Inactive.ToString(), selectedFilter == UserQueueFilter.Inactive);
            yield return new SelectListItem("Mobile-linked", UserQueueFilter.MobileLinked.ToString(), selectedFilter == UserQueueFilter.MobileLinked);
        }

        private static List<UserSupportPlaybookVm> BuildUserSupportPlaybooks()
        {
            return new List<UserSupportPlaybookVm>
            {
                new()
                {
                    Title = "Unconfirmed accounts",
                    ScopeNote = "Use this queue for activation support and onboarding follow-up.",
                    OperatorAction = "Open the user, then send activation email or confirm the email directly only where policy allows.",
                    FollowUp = "If repeated email failures happen, continue in Business Communications and Email Audits."
                },
                new()
                {
                    Title = "Locked accounts",
                    ScopeNote = "Treat lockouts as support/security events, not casual cleanup.",
                    OperatorAction = "Open the user, validate the support context, then unlock only when the issue is resolved.",
                    FollowUp = "If the lockout was triggered by security concerns, rotate password or review recent auth flows."
                },
                new()
                {
                    Title = "Mobile-linked accounts",
                    ScopeNote = "These users are important for device and push-related support.",
                    OperatorAction = "Use the user profile together with Mobile Operations when stale devices, missing push tokens, or notification issues are reported.",
                    FollowUp = "Cross-check device fleet health before assuming the user profile itself is wrong."
                }
            };
        }

        /// <summary>
        /// Renders the create form. Locale/Currency/Timezone are rendered by SettingSelectTagHelper.
        /// </summary>
        [HttpGet]
        public IActionResult Create() => RenderCreateEditor(new UserCreateVm());

        /// <summary>
        /// Creates a new user. On success, redirects to Edit so that addresses can be managed.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return RenderCreateEditor(vm);

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
                return RenderCreateEditor(vm);
            }

            TempData["Success"] = "User created.";
            return RedirectOrHtmx(nameof(Edit), new { id = result.Value });
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
                Email = u.Email,
                EmailConfirmed = u.EmailConfirmed,
                LockoutEndUtc = u.LockoutEndUtc,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Locale = u.Locale,
                Currency = u.Currency,
                Timezone = u.Timezone,
                PhoneE164 = u.PhoneE164,
                IsActive = u.IsActive
                // NOTE: IsSystem is intentionally NOT part of UserEditVm per model.
            };

            return await RenderEditEditorAsync(vm, ct);
        }

        /// <summary>
        /// Updates user profile fields (except Email/Roles). Handles optimistic concurrency.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return await RenderEditEditorAsync(vm, ct);
            }

            var dto = new UserEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Email = vm.Email,
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
                return await RenderEditEditorAsync(vm, ct);
            }

            TempData["Success"] = "User updated.";
            return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
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
                CurrentEmail = currentEmail ?? string.Empty,
                NewEmail = string.Empty
            };
            return RenderChangeEmailEditor(vm);
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
                return RenderChangeEmailEditor(vm);

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
                return RenderChangeEmailEditor(vm);
            }

            TempData["Success"] = "Email change requested. The user must confirm the new email before signing in.";
            return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
        }

        /// <summary>
        /// Allows an administrator to mark the user's email as confirmed when support operations require a manual override.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail([FromForm] Guid id, CancellationToken ct = default)
        {
            var result = await _confirmUserEmail.HandleAsync(new UserAdminActionDto { Id = id }, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Email marked as confirmed."
                : (result.Error ?? "Failed to confirm email.");

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        /// <summary>
        /// Sends or resends an activation email to the user's current email address.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendActivationEmail([FromForm] Guid id, CancellationToken ct = default)
        {
            var userResult = await _getUserWithAddresses.HandleAsync(id, ct);
            if (!userResult.Succeeded || userResult.Value is null)
            {
                TempData["Error"] = userResult.Error ?? "User not found.";
                return RedirectOrHtmx(nameof(Edit), new { id });
            }

            var result = await _requestEmailConfirmation.HandleAsync(
                new RequestEmailConfirmationDto { Email = userResult.Value.Email },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Activation email sent."
                : (result.Error ?? "Failed to send activation email.");

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        /// <summary>
        /// Sends a password reset email to the user's current email address without exposing token details in the admin UI.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswordReset([FromForm] Guid id, CancellationToken ct = default)
        {
            var userResult = await _getUserWithAddresses.HandleAsync(id, ct);
            if (!userResult.Succeeded || userResult.Value is null)
            {
                TempData["Error"] = userResult.Error ?? "User not found.";
                return RedirectOrHtmx(nameof(Edit), new { id });
            }

            var result = await _requestPasswordReset.HandleAsync(
                new RequestPasswordResetDto { Email = userResult.Value.Email },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Password reset email sent."
                : (result.Error ?? "Failed to send password reset email.");

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        /// <summary>
        /// Locks the specified user account and revokes active refresh tokens.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock([FromForm] Guid id, CancellationToken ct = default)
        {
            var result = await _lockUser.HandleAsync(new UserAdminActionDto { Id = id }, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Account locked."
                : (result.Error ?? "Failed to lock account.");

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        /// <summary>
        /// Unlocks the specified user account.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock([FromForm] Guid id, CancellationToken ct = default)
        {
            var result = await _unlockUser.HandleAsync(new UserAdminActionDto { Id = id }, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Account unlocked."
                : (result.Error ?? "Failed to unlock account.");

            return RedirectOrHtmx(nameof(Edit), new { id });
        }





        // <summary>
        /// Renders the Change Password page for admins.
        /// Admin can set a new password for a user without knowing the current one.
        /// The email is provided via query string for display only.
        /// </summary>
        /// <param name="id">Target user id.</param>
        /// <param name="email">Optional email to display; provided by the caller to avoid extra round-trips.</param>
        [HttpGet]
        public IActionResult ChangePassword(Guid id, string? email = null)
        {
            var vm = new UserChangePasswordVm
            {
                Id = id,
                Email = email ?? string.Empty
            };
            return View(vm);
        }

        /// <summary>
        /// Sets a new password for the user (admin flow).
        /// Uses Application-level handler that does NOT require the current password.
        /// On success, SecurityStamp gets rotated at Application layer so active sessions are invalidated.
        /// </summary>
        /// <param name="vm">View model containing the user id and the new password + confirmation.</param>
        /// <param name="ct">Cancellation token.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserChangePasswordVm vm, CancellationToken ct = default)
        {
            // Server-side safety: ensure confirmation matches before calling Application
            if (!ModelState.IsValid || vm.NewPassword != vm.ConfirmNewPassword)
            {
                if (vm.NewPassword != vm.ConfirmNewPassword)
                    ModelState.AddModelError(nameof(vm.ConfirmNewPassword), "Passwords do not match.");
                return RenderChangePasswordEditor(vm);
            }

            // Build DTO for the admin-set password operation
            var dto = new UserAdminSetPasswordDto
            {
                Id = vm.Id,
                NewPassword = vm.NewPassword
            };

            var result = await _setUserPasswordByAdmin.HandleAsync(dto, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Password changed successfully."
                : (result.Error ?? "Failed to change password.");

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to change password.");
                return RenderChangePasswordEditor(vm);
            }

            return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
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
            var section = await BuildAddressesSectionVmAsync(userId, ct);

            return PartialView("~/Views/Users/_AddressesSection.cshtml", section);
        }

        /// <summary>
        /// Creates a new address for the specified user. Returns the refreshed addresses section partial on success.
        /// Uses <see cref="UserAddressCreateVm"/> to avoid binding an Id for creation scenarios.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAddress(UserAddressCreateVm vm, CancellationToken ct = default)
        {
            // Server-side validation is enforced in the Application layer; Web layer keeps basic shape checks.
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

            // Success message for alerts
            TempData["Success"] = "Address created successfully.";

            // Return refreshed addresses section (partial)
            return await AddressesSection(vm.UserId, ct);
        }

        /// <summary>
        /// Updates an existing address (optimistic concurrency via RowVersion). Returns refreshed addresses section on success.
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

            TempData["Success"] = "Address updated successfully.";

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

            TempData["Success"] = "Address deleted successfully.";
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

            TempData["Success"] = asBilling
                ? "Default billing address set."
                : "Default shipping address set.";

            return await AddressesSection(userId, ct);
        }



        /// <summary>
        /// Builds a non-null UserAddressesSectionVm for the specified user.
        /// Guarantees Items is an instantiated list to avoid null refs in the partial.
        /// </summary>
        private async Task<UserAddressesSectionVm> BuildAddressesSectionVmAsync(Guid userId, CancellationToken ct)
        {
            var section = new UserAddressesSectionVm
            {
                UserId = userId,
                Items = new List<UserAddressListItemVm>()
            };

            var result = await _getUserWithAddresses.HandleAsync(userId, ct);
            if (!result.Succeeded || result.Value is null)
                return section; // Return empty section on failure to avoid null refs

            // Map addresses
            foreach (var a in result.Value.Addresses)
            {
                section.Items.Add(new UserAddressListItemVm
                {
                    Id = a.Id,
                    RowVersion = a.RowVersion ?? Array.Empty<byte>(),
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
                    IsDefaultShipping = a.IsDefaultShipping
                });
            }

            return section;
        }

        /// <summary>
        /// Shows all roles and the selection for the specified user.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Roles(Guid id, CancellationToken ct = default)
        {
            var vm = await BuildUserRolesVmAsync(id, ct);
            if (vm is null)
            {
                TempData["Error"] = "Failed to load user roles.";
                return RedirectToAction(nameof(Index));
            }

            return await RenderRolesEditorAsync(vm, ct);
        }

        /// <summary>
        /// Saves the role selection for the user and redirects to Users/Index.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Roles(UserRolesEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return await RenderRolesEditorAsync(vm, ct);

            var dto = new UserRolesUpdateDto
            {
                UserId = vm.UserId,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                RoleIds = vm.SelectedRoleIds?.ToList() ?? new List<Guid>()
            };

            var result = await _updateUserRoles.HandleAsync(dto, ct);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update user roles.");
                return await RenderRolesEditorAsync(vm, ct);
            }

            TempData["Success"] = "User roles updated.";
            return RedirectOrHtmx(nameof(Edit), new { id = vm.UserId });
        }

        private IActionResult RenderCreateEditor(UserCreateVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/_UserCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderChangeEmailEditor(UserChangeEmailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/_UserChangeEmailEditorShell.cshtml", vm);
            }

            return View("ChangeEmail", vm);
        }

        private IActionResult RenderChangePasswordEditor(UserChangePasswordVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/_UserChangePasswordEditorShell.cshtml", vm);
            }

            return View("ChangePassword", vm);
        }

        private async Task<IActionResult> RenderEditEditorAsync(UserEditVm vm, CancellationToken ct)
        {
            ViewBag.AddressesSection = await BuildAddressesSectionVmAsync(vm.Id, ct);

            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/_UserEditEditorShell.cshtml", vm);
            }

            return View("Edit", vm);
        }

        private async Task<IActionResult> RenderRolesEditorAsync(UserRolesEditVm vm, CancellationToken ct)
        {
            var hydrated = await BuildUserRolesVmAsync(vm.UserId, ct);
            if (hydrated is null)
            {
                TempData["Error"] = "Failed to load user roles.";
                return RedirectToAction(nameof(Index));
            }

            hydrated.SelectedRoleIds = vm.SelectedRoleIds?.ToList() ?? new List<Guid>();
            hydrated.RowVersion = vm.RowVersion ?? hydrated.RowVersion;

            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/_UserRolesEditorShell.cshtml", hydrated);
            }

            return View("Roles", hydrated);
        }

        private async Task<UserRolesEditVm?> BuildUserRolesVmAsync(Guid userId, CancellationToken ct)
        {
            var result = await _getUserRoles.HandleAsync(userId, ct);
            if (!result.Succeeded || result.Value is null)
            {
                return null;
            }

            var dto = result.Value;
            return new UserRolesEditVm
            {
                UserId = dto.UserId,
                UserEmail = dto.UserEmail,
                RowVersion = dto.RowVersion,
                AllRoles = dto.AllRoles.Select(x => new RoleItemVm
                {
                    Id = x.Id,
                    Key = x.Key,
                    DisplayName = x.DisplayName,
                    Description = x.Description,
                    IsSystem = x.IsSystem,
                    RowVersion = x.RowVersion
                }).ToList(),
                SelectedRoleIds = dto.RoleIds.ToList()
            };
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
