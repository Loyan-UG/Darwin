using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Application.Identity.Queries;
using Darwin.Shared.Results;
using Darwin.WebAdmin.Services.Settings;
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
        private readonly GetEmailDispatchAuditsPageHandler _getEmailDispatchAuditsPage;
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
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IClock _clock;


        /// <summary>
        /// Wires Application-layer handlers. These encapsulate validation and persistence.
        /// </summary>
        public UsersController(
            RegisterUserHandler registerUser,
            GetUsersPageHandler getUsersPage,
            GetUserOpsSummaryHandler getUserOpsSummary,
            GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage,
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
            UpdateUserRolesHandler updateUserRoles,
            ISiteSettingCache siteSettingCache,
            IClock clock)
        {
            _registerUser = registerUser ?? throw new ArgumentNullException(nameof(registerUser));
            _getUsersPage = getUsersPage ?? throw new ArgumentNullException(nameof(getUsersPage));
            _getUserOpsSummary = getUserOpsSummary ?? throw new ArgumentNullException(nameof(getUserOpsSummary));
            _getEmailDispatchAuditsPage = getEmailDispatchAuditsPage ?? throw new ArgumentNullException(nameof(getEmailDispatchAuditsPage));
            _getUserWithAddresses = getUserWithAddresses ?? throw new ArgumentNullException(nameof(getUserWithAddresses));
            _updateUser = updateUser ?? throw new ArgumentNullException(nameof(updateUser));
            _changeUserEmail = changeUserEmail ?? throw new ArgumentNullException(nameof(changeUserEmail));
            _setUserPasswordByAdmin = setUserPasswordByAdmin ?? throw new ArgumentNullException(nameof(setUserPasswordByAdmin));
            _requestPasswordReset = requestPasswordReset ?? throw new ArgumentNullException(nameof(requestPasswordReset));
            _requestEmailConfirmation = requestEmailConfirmation ?? throw new ArgumentNullException(nameof(requestEmailConfirmation));
            _confirmUserEmail = confirmUserEmail ?? throw new ArgumentNullException(nameof(confirmUserEmail));
            _lockUser = lockUser ?? throw new ArgumentNullException(nameof(lockUser));
            _unlockUser = unlockUser ?? throw new ArgumentNullException(nameof(unlockUser));
            _softDeleteUser = softDeleteUser ?? throw new ArgumentNullException(nameof(softDeleteUser));
            _createAddress = createAddress ?? throw new ArgumentNullException(nameof(createAddress));
            _updateAddress = updateAddress ?? throw new ArgumentNullException(nameof(updateAddress));
            _softDeleteAddress = softDeleteAddress ?? throw new ArgumentNullException(nameof(softDeleteAddress));
            _setDefaultAddress = setDefaultAddress ?? throw new ArgumentNullException(nameof(setDefaultAddress));
            _getUserRoles = getUserRoles ?? throw new ArgumentNullException(nameof(getUserRoles));
            _updateUserRoles = updateUserRoles ?? throw new ArgumentNullException(nameof(updateUserRoles));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        /// Lists users with paging and optional text filter (email/name).
        /// Handler signature follows the Roles pattern: (page, pageSize, q, ct).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, UserQueueFilter filter = UserQueueFilter.All, CancellationToken ct = default)
        {
            var vm = await BuildUsersListVmAsync(page, pageSize, q, filter, ct);
            return RenderIndexWorkspace(vm);
        }

        private IEnumerable<SelectListItem> BuildUserFilterItems(UserQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("UsersFilterAll"), UserQueueFilter.All.ToString(), selectedFilter == UserQueueFilter.All);
            yield return new SelectListItem(T("UsersFilterUnconfirmed"), UserQueueFilter.Unconfirmed.ToString(), selectedFilter == UserQueueFilter.Unconfirmed);
            yield return new SelectListItem(T("UsersFilterLocked"), UserQueueFilter.Locked.ToString(), selectedFilter == UserQueueFilter.Locked);
            yield return new SelectListItem(T("UsersFilterInactive"), UserQueueFilter.Inactive.ToString(), selectedFilter == UserQueueFilter.Inactive);
            yield return new SelectListItem(T("UsersFilterMobileLinked"), UserQueueFilter.MobileLinked.ToString(), selectedFilter == UserQueueFilter.MobileLinked);
        }

        private List<UserSupportPlaybookVm> BuildUserSupportPlaybooks()
        {
            return new List<UserSupportPlaybookVm>
            {
                new()
                {
                    Title = T("UsersPlaybookUnconfirmedTitle"),
                    ScopeNote = T("UsersPlaybookUnconfirmedScope"),
                    OperatorAction = T("UsersPlaybookUnconfirmedAction"),
                    FollowUp = T("UsersPlaybookUnconfirmedFollowUp"),
                    QueueFilter = UserQueueFilter.Unconfirmed,
                    AuditFlowKey = "AccountActivation"
                },
                new()
                {
                    Title = T("UsersPlaybookLockedTitle"),
                    ScopeNote = T("UsersPlaybookLockedScope"),
                    OperatorAction = T("UsersPlaybookLockedAction"),
                    FollowUp = T("UsersPlaybookLockedFollowUp"),
                    QueueFilter = UserQueueFilter.Locked,
                    AuditFlowKey = "PasswordReset"
                },
                new()
                {
                    Title = T("UsersPlaybookInactiveTitle"),
                    ScopeNote = T("UsersPlaybookInactiveScope"),
                    OperatorAction = T("UsersPlaybookInactiveAction"),
                    FollowUp = T("UsersPlaybookInactiveFollowUp"),
                    QueueFilter = UserQueueFilter.Inactive
                },
                new()
                {
                    Title = T("UsersPlaybookMobileLinkedTitle"),
                    ScopeNote = T("UsersPlaybookMobileLinkedScope"),
                    OperatorAction = T("UsersPlaybookMobileLinkedAction"),
                    FollowUp = T("UsersPlaybookMobileLinkedFollowUp"),
                    QueueFilter = UserQueueFilter.MobileLinked,
                    OpensMobileOperations = true
                }
            };
        }

        /// <summary>
        /// Renders the create form. Locale/Currency/Timezone are rendered by SettingSelectTagHelper.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct = default) => await RenderCreateEditorAsync(new UserCreateVm(), ct);

        /// <summary>
        /// Creates a new user. On success, redirects to Edit so that addresses can be managed.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return await RenderCreateEditorAsync(vm, ct);

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
                AddModelErrorMessage("UserCreateFailedMessage");
                return await RenderCreateEditorAsync(vm, ct);
            }

            SetSuccessMessage("UserCreatedMessage");
            return RedirectOrHtmx(nameof(Edit), new { id = result.Value });
        }

        /// <summary>
        /// Loads user and addresses for editing. Email and roles are NOT editable here.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, bool returnToIndex = false, string? q = null, UserQueueFilter filter = UserQueueFilter.All, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var result = await _getUserWithAddresses.HandleAsync(id, ct);
            if (!result.Succeeded || result.Value is null)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            // Map user -> edit VM
            var u = result.Value;
            var nowUtc = _clock.UtcNow;

            var vm = new UserEditVm
            {
                Id = u.Id,
                RowVersion = u.RowVersion,
                Email = u.Email,
                EmailConfirmed = u.EmailConfirmed,
                LockoutEndUtc = u.LockoutEndUtc,
                IsLockedOut = u.LockoutEndUtc.HasValue && u.LockoutEndUtc.Value > nowUtc,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Locale = u.Locale,
                Currency = u.Currency,
                Timezone = u.Timezone,
                PhoneE164 = u.PhoneE164,
                IsActive = u.IsActive,
                ReturnToIndex = returnToIndex,
                Query = q ?? string.Empty,
                Filter = filter,
                Page = page,
                PageSize = pageSize
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
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                AddModelErrorMessage("UserUpdateFailedMessage");
                return await RenderEditEditorAsync(vm, ct);
            }

            SetSuccessMessage("UserUpdatedMessage");
            return RedirectOrHtmx(nameof(Edit), new { id = vm.Id, returnToIndex = vm.ReturnToIndex, q = vm.Query, filter = vm.Filter, page = vm.Page, pageSize = vm.PageSize });
        }




        /// <summary>
        /// GET form for changing a user's email (admin-only).
        /// The current email is displayed read-only (supplied via query string to avoid extra round-trip).
        /// </summary>
        [HttpGet]
        public IActionResult ChangeEmail([FromRoute] Guid id, [FromQuery] string? currentEmail = null, [FromQuery] bool returnToIndex = false, [FromQuery] string? q = null, [FromQuery] UserQueueFilter filter = UserQueueFilter.All, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            // Prepare view model with the user id. NewEmail is intentionally left blank to force admin input.
            var vm = new UserChangeEmailVm
            {
                Id = id,
                CurrentEmail = currentEmail ?? string.Empty,
                NewEmail = string.Empty,
                ReturnToIndex = returnToIndex,
                Query = q ?? string.Empty,
                Filter = filter,
                Page = page,
                PageSize = pageSize
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
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                AddModelErrorMessage("ChangeEmailFailedMessage");
                return RenderChangeEmailEditor(vm);
            }

            SetSuccessMessage("EmailChangeRequestedMessage");
            return RedirectToUsersWorkspaceOrEdit(vm.Id, vm.ReturnToIndex, vm.Query, vm.Filter, vm.Page, vm.PageSize);
        }

        /// <summary>
        /// Allows an administrator to mark the user's email as confirmed when support operations require a manual override.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail([FromForm] Guid id, [FromForm] bool returnToIndex = false, [FromForm] string? q = null, [FromForm] UserQueueFilter filter = UserQueueFilter.All, [FromForm] int page = 1, [FromForm] int pageSize = 20, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var result = await _confirmUserEmail.HandleAsync(new UserAdminActionDto { Id = id }, ct);
            if (result.Succeeded)
            {
                SetSuccessMessage("EmailConfirmedMessage");
            }
            else
            {
                SetErrorMessage("ConfirmEmailFailedMessage");
            }

            return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
        }

        /// <summary>
        /// Sends or resends an activation email to the user's current email address.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendActivationEmail([FromForm] Guid id, [FromForm] bool returnToIndex = false, [FromForm] string? q = null, [FromForm] UserQueueFilter filter = UserQueueFilter.All, [FromForm] int page = 1, [FromForm] int pageSize = 20, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var userResult = await _getUserWithAddresses.HandleAsync(id, ct);
            if (!userResult.Succeeded || userResult.Value is null)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var result = await _requestEmailConfirmation.HandleAsync(
                new RequestEmailConfirmationDto { Email = userResult.Value.Email },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("ActivationEmailSentMessage")
                : T("ActivationEmailSendFailedMessage");

            return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
        }

        /// <summary>
        /// Sends a password reset email to the user's current email address without exposing token details in the admin UI.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswordReset([FromForm] Guid id, [FromForm] bool returnToIndex = false, [FromForm] string? q = null, [FromForm] UserQueueFilter filter = UserQueueFilter.All, [FromForm] int page = 1, [FromForm] int pageSize = 20, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var userResult = await _getUserWithAddresses.HandleAsync(id, ct);
            if (!userResult.Succeeded || userResult.Value is null)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var result = await _requestPasswordReset.HandleAsync(
                new RequestPasswordResetDto { Email = userResult.Value.Email },
                ct);

            if (result.Succeeded)
            {
                SetSuccessMessage("PasswordResetEmailSentMessage");
            }
            else
            {
                SetErrorMessage("PasswordResetEmailSendFailedMessage");
            }

            return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
        }

        /// <summary>
        /// Reactivates the specified user account from the support queue.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate([FromForm] Guid id, [FromForm] bool returnToIndex = false, [FromForm] string? q = null, [FromForm] UserQueueFilter filter = UserQueueFilter.All, [FromForm] int page = 1, [FromForm] int pageSize = 20, CancellationToken ct = default)
        {
            return await SetUserActiveStateAsync(id, true, returnToIndex, q, filter, page, pageSize, ct);
        }

        /// <summary>
        /// Deactivates the specified user account from the support queue.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate([FromForm] Guid id, [FromForm] bool returnToIndex = false, [FromForm] string? q = null, [FromForm] UserQueueFilter filter = UserQueueFilter.All, [FromForm] int page = 1, [FromForm] int pageSize = 20, CancellationToken ct = default)
        {
            return await SetUserActiveStateAsync(id, false, returnToIndex, q, filter, page, pageSize, ct);
        }

        /// <summary>
        /// Locks the specified user account and revokes active refresh tokens.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock([FromForm] Guid id, [FromForm] bool returnToIndex = false, [FromForm] string? q = null, [FromForm] UserQueueFilter filter = UserQueueFilter.All, [FromForm] int page = 1, [FromForm] int pageSize = 20, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var result = await _lockUser.HandleAsync(new UserAdminActionDto { Id = id }, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("AccountLockedMessage")
                : T("AccountLockFailedMessage");

            return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
        }

        /// <summary>
        /// Unlocks the specified user account.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock([FromForm] Guid id, [FromForm] bool returnToIndex = false, [FromForm] string? q = null, [FromForm] UserQueueFilter filter = UserQueueFilter.All, [FromForm] int page = 1, [FromForm] int pageSize = 20, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var result = await _unlockUser.HandleAsync(new UserAdminActionDto { Id = id }, ct);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? T("AccountUnlockedMessage")
                : T("AccountUnlockFailedMessage");

            return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
        }





        // <summary>
        /// Renders the Change Password page for admins.
        /// Admin can set a new password for a user without knowing the current one.
        /// The email is provided via query string for display only.
        /// </summary>
        /// <param name="id">Target user id.</param>
        /// <param name="email">Optional email to display; provided by the caller to avoid extra round-trips.</param>
        [HttpGet]
        public IActionResult ChangePassword(Guid id, string? email = null, bool returnToIndex = false, string? q = null, UserQueueFilter filter = UserQueueFilter.All, int page = 1, int pageSize = 20)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = new UserChangePasswordVm
            {
                Id = id,
                Email = email ?? string.Empty,
                ReturnToIndex = returnToIndex,
                Query = q ?? string.Empty,
                Filter = filter,
                Page = page,
                PageSize = pageSize
            };
            return RenderChangePasswordEditor(vm);
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
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            // Server-side safety: ensure confirmation matches before calling Application
            if (!ModelState.IsValid || vm.NewPassword != vm.ConfirmNewPassword)
            {
                if (vm.NewPassword != vm.ConfirmNewPassword)
                    ModelState.AddModelError(nameof(vm.ConfirmNewPassword), T("PasswordsDoNotMatchMessage"));
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
                ? T("PasswordChangedMessage")
                : T("ChangePasswordFailedMessage");

            if (!result.Succeeded)
            {
                AddModelErrorMessage("ChangePasswordFailedMessage");
                return RenderChangePasswordEditor(vm);
            }

            return RedirectToUsersWorkspaceOrEdit(vm.Id, vm.ReturnToIndex, vm.Query, vm.Filter, vm.Page, vm.PageSize);
        }




        /// <summary>
        /// Soft-deletes a user. Requires the DTO (Id + RowVersion).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var dto = new UserDeleteDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) };
            var result = await _softDeleteUser.HandleAsync(dto, ct);
            if (result.Succeeded)
            {
                TempData["Success"] = T(result.Value?.WasDeactivatedDueToReferences == true
                    ? "UserDeactivatedDueToOrderHistoryMessage"
                    : "UserDeletedMessage");
            }
            else
            {
                SetErrorMessage("UserDeleteFailedMessage");
            }

            return RedirectOrHtmx(nameof(Index), new { });
        }




        // -------------------- Addresses (Partial Grid + AJAX refresh) --------------------

        /// <summary>
        /// Returns the addresses section partial for the specified user.
        /// Use this endpoint to (re)render the grid after create/update/delete/default operations.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AddressesSection(Guid userId, CancellationToken ct = default)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(T("UserNotFoundMessage"));
            }

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
            if (!ModelState.IsValid || vm.UserId == Guid.Empty) return BadRequest(T("InvalidAddressMessage"));

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
            if (!result.Succeeded) return BadRequest(T("AddressCreateFailedMessage"));

            // Success message for alerts
            SetSuccessMessage("AddressCreatedMessage");

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
            if (!ModelState.IsValid || vm.Id == Guid.Empty || vm.UserId == Guid.Empty) return BadRequest(T("InvalidAddressMessage"));

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
            if (!result.Succeeded) return BadRequest(T("AddressUpdateFailedMessage"));

            SetSuccessMessage("AddressUpdatedMessage");

            return await AddressesSection(vm.UserId, ct);
        }


        /// <summary>
        /// Soft-deletes an address via confirmation modal. Returns refreshed section.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress([FromForm] Guid id, [FromForm] Guid userId, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || userId == Guid.Empty)
            {
                return BadRequest(T("AddressDeleteFailedMessage"));
            }

            var dto = new AddressDeleteDto { Id = id, RowVersion = DecodeBase64RowVersion(rowVersion) };
            var result = await _softDeleteAddress.HandleAsync(dto, ct);
            if (!result.Succeeded) return BadRequest(T("AddressDeleteFailedMessage"));

            SetSuccessMessage("AddressDeletedMessage");
            return await AddressesSection(userId, ct);
        }


        /// <summary>
        /// Sets an address as default billing or shipping. Returns refreshed section.
        /// Expects kind to be either "Billing" or "Shipping".
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress([FromForm] Guid id, [FromForm] Guid userId, [FromForm] string kind, [FromForm] string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || userId == Guid.Empty)
            {
                return BadRequest(T("SetDefaultAddressFailedMessage"));
            }

            var asBilling = string.Equals(kind, "Billing", StringComparison.OrdinalIgnoreCase);
            var asShipping = string.Equals(kind, "Shipping", StringComparison.OrdinalIgnoreCase);
            if (!asBilling && !asShipping)
            {
                return BadRequest(T("SetDefaultAddressFailedMessage"));
            }

            var parsedRowVersion = DecodeBase64RowVersion(rowVersion);
            if (parsedRowVersion.Length == 0)
            {
                return BadRequest(T("SetDefaultAddressFailedMessage"));
            }

            var result = await _setDefaultAddress.HandleAsync(userId, id, asBilling, asShipping, parsedRowVersion, ct);
            if (!result.Succeeded) return BadRequest(T("SetDefaultAddressFailedMessage"));

            TempData["Success"] = asBilling
                ? T("DefaultBillingAddressSetMessage")
                : T("DefaultShippingAddressSetMessage");

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
        public async Task<IActionResult> Roles(Guid id, bool returnToIndex = false, string? q = null, UserQueueFilter filter = UserQueueFilter.All, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserRolesLoadFailedMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            var vm = await BuildUserRolesVmAsync(id, ct);
            if (vm is null)
            {
                SetErrorMessage("UserRolesLoadFailedMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            vm.ReturnToIndex = returnToIndex;
            vm.Query = q ?? string.Empty;
            vm.Filter = filter;
            vm.Page = page;
            vm.PageSize = pageSize;

            return await RenderRolesEditorAsync(vm, ct);
        }

        /// <summary>
        /// Saves the role selection for the user and redirects to Users/Index.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Roles(UserRolesEditVm vm, CancellationToken ct = default)
        {
            if (vm.UserId == Guid.Empty)
            {
                SetErrorMessage("UserRolesLoadFailedMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

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
                AddModelErrorMessage("UserRolesUpdateFailedMessage");
                return await RenderRolesEditorAsync(vm, ct);
            }

            SetSuccessMessage("UserRolesUpdatedMessage");
            return await RedirectToUserRolesReturnTargetAsync(vm, ct);
        }

        private async Task<IActionResult> RenderCreateEditorAsync(UserCreateVm vm, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(vm.Locale) ||
                string.IsNullOrWhiteSpace(vm.Currency) ||
                string.IsNullOrWhiteSpace(vm.Timezone))
            {
                var settings = await _siteSettingCache.GetAsync(ct);
                vm.Locale = string.IsNullOrWhiteSpace(vm.Locale) ? settings.DefaultCulture : vm.Locale;
                vm.Currency = string.IsNullOrWhiteSpace(vm.Currency) ? settings.DefaultCurrency : vm.Currency;
                vm.Timezone = string.IsNullOrWhiteSpace(vm.Timezone)
                    ? (settings.TimeZone ?? string.Empty)
                    : vm.Timezone;
            }

            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/_UserCreateEditorShell.cshtml", vm);
            }

            return View("Create", vm);
        }

        private IActionResult RenderIndexWorkspace(UsersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/Index.cshtml", vm);
            }

            return View("Index", vm);
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
            if (string.IsNullOrWhiteSpace(vm.Locale) ||
                string.IsNullOrWhiteSpace(vm.Currency) ||
                string.IsNullOrWhiteSpace(vm.Timezone))
            {
                var settings = await _siteSettingCache.GetAsync(ct);
                vm.Locale = string.IsNullOrWhiteSpace(vm.Locale) ? settings.DefaultCulture : vm.Locale;
                vm.Currency = string.IsNullOrWhiteSpace(vm.Currency) ? settings.DefaultCurrency : vm.Currency;
                vm.Timezone = string.IsNullOrWhiteSpace(vm.Timezone) ? (settings.TimeZone ?? string.Empty) : vm.Timezone;
            }

            vm.AddressesSection = await BuildAddressesSectionVmAsync(vm.Id, ct);

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
                SetErrorMessage("UserRolesLoadFailedMessage");
                return RedirectOrHtmx(nameof(Index), new { });
            }

            hydrated.SelectedRoleIds = vm.SelectedRoleIds?.ToList() ?? new List<Guid>();
            hydrated.RowVersion = vm.RowVersion ?? hydrated.RowVersion;
            hydrated.ReturnToIndex = vm.ReturnToIndex;
            hydrated.Query = vm.Query;
            hydrated.Filter = vm.Filter;
            hydrated.Page = vm.Page;
            hydrated.PageSize = vm.PageSize;

            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Users/_UserRolesEditorShell.cshtml", hydrated);
            }

            return View("Roles", hydrated);
        }

        private async Task<IActionResult> RedirectToUserRolesReturnTargetAsync(UserRolesEditVm vm, CancellationToken ct)
        {
            if (vm.ReturnToIndex)
            {
                if (IsHtmxRequest())
                {
                    var workspace = await BuildUsersListVmAsync(vm.Page, vm.PageSize, vm.Query, vm.Filter, ct);
                    return RenderIndexWorkspace(workspace);
                }

                return RedirectOrHtmx(nameof(Index), new { page = vm.Page, pageSize = vm.PageSize, q = vm.Query, filter = vm.Filter });
            }

            return RedirectOrHtmx(nameof(Edit), new { id = vm.UserId });
        }

        private IActionResult RedirectToUsersWorkspaceOrEdit(Guid id, bool returnToIndex, string? q, UserQueueFilter filter, int page, int pageSize)
        {
            if (returnToIndex)
            {
                return RedirectOrHtmx(nameof(Index), new { page, pageSize, q, filter });
            }

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        private async Task<IActionResult> SetUserActiveStateAsync(Guid id, bool isActive, bool returnToIndex, string? q, UserQueueFilter filter, int page, int pageSize, CancellationToken ct)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var userResult = await _getUserWithAddresses.HandleAsync(id, ct);
            if (!userResult.Succeeded || userResult.Value is null)
            {
                SetErrorMessage("UserNotFoundMessage");
                return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
            }

            var user = userResult.Value;
            var result = await _updateUser.HandleAsync(
                new UserEditDto
                {
                    Id = user.Id,
                    RowVersion = user.RowVersion ?? Array.Empty<byte>(),
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Locale = user.Locale,
                    Currency = user.Currency,
                    Timezone = user.Timezone,
                    PhoneE164 = user.PhoneE164,
                    IsActive = isActive
                },
                ct);

            if (result.Succeeded)
            {
                SetSuccessMessage(isActive ? "UserActivatedMessage" : "UserDeactivatedMessage");
            }
            else
            {
                SetErrorMessage(isActive ? "UserActivationFailedMessage" : "UserDeactivationFailedMessage");
            }

            return RedirectToUsersWorkspaceOrEdit(id, returnToIndex, q, filter, page, pageSize);
        }

        private async Task<UserRolesEditVm?> BuildUserRolesVmAsync(Guid userId, CancellationToken ct)
        {
            if (userId == Guid.Empty)
            {
                return null;
            }

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

        private async Task<UsersListVm> BuildUsersListVmAsync(int page, int pageSize, string? q, UserQueueFilter filter, CancellationToken ct)
        {
            var (items, total) = await _getUsersPage.HandleAsync(page, pageSize, q, filter, ct);
            var summary = await _getUserOpsSummary.HandleAsync(ct);
            var auditSummary = await _getEmailDispatchAuditsPage.GetSummaryAsync(null, ct);

            var nowUtc = _clock.UtcNow;
            return new UsersListVm
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
                    MobileLinkedCount = summary.MobileLinkedCount,
                    FailedActivationEmailCount = auditSummary.FailedActivationCount,
                    FailedPasswordResetEmailCount = auditSummary.FailedPasswordResetCount
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
                    IsLockedOut = x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > nowUtc,
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
