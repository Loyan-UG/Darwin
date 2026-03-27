using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Application.Common.DTOs;
using Darwin.Application.Identity.Commands;
using Darwin.Application.Identity.DTOs;
using Darwin.Domain.Enums;
using Darwin.Shared.Results;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Security;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.Businesses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.Businesses
{
    /// <summary>
    /// Admin controller for merchant/business onboarding and lifecycle management.
    /// This is a FullAdmin-only surface because it controls tenant creation and owner assignment.
    /// </summary>
    [PermissionAuthorize("FullAdminAccess")]
    public sealed class BusinessesController : AdminBaseController
    {
        private readonly GetBusinessesPageHandler _getBusinessesPage;
        private readonly GetBusinessForEditHandler _getBusinessForEdit;
        private readonly CreateBusinessHandler _createBusiness;
        private readonly UpdateBusinessHandler _updateBusiness;
        private readonly SoftDeleteBusinessHandler _deleteBusiness;
        private readonly GetBusinessLocationsPageHandler _getBusinessLocationsPage;
        private readonly GetBusinessLocationForEditHandler _getBusinessLocationForEdit;
        private readonly CreateBusinessLocationHandler _createBusinessLocation;
        private readonly UpdateBusinessLocationHandler _updateBusinessLocation;
        private readonly SoftDeleteBusinessLocationHandler _deleteBusinessLocation;
        private readonly GetBusinessMembersPageHandler _getBusinessMembersPage;
        private readonly GetBusinessMemberForEditHandler _getBusinessMemberForEdit;
        private readonly CreateBusinessMemberHandler _createBusinessMember;
        private readonly UpdateBusinessMemberHandler _updateBusinessMember;
        private readonly DeleteBusinessMemberHandler _deleteBusinessMember;
        private readonly GetBusinessInvitationsPageHandler _getBusinessInvitationsPage;
        private readonly CreateBusinessInvitationHandler _createBusinessInvitation;
        private readonly ResendBusinessInvitationHandler _resendBusinessInvitation;
        private readonly RevokeBusinessInvitationHandler _revokeBusinessInvitation;
        private readonly ApproveBusinessHandler _approveBusiness;
        private readonly SuspendBusinessHandler _suspendBusiness;
        private readonly ReactivateBusinessHandler _reactivateBusiness;
        private readonly RequestPasswordResetHandler _requestPasswordReset;
        private readonly RequestEmailConfirmationHandler _requestEmailConfirmation;
        private readonly ConfirmUserEmailByAdminHandler _confirmUserEmail;
        private readonly LockUserByAdminHandler _lockUser;
        private readonly UnlockUserByAdminHandler _unlockUser;
        private readonly AdminReferenceDataService _referenceData;

        public BusinessesController(
            GetBusinessesPageHandler getBusinessesPage,
            GetBusinessForEditHandler getBusinessForEdit,
            CreateBusinessHandler createBusiness,
            UpdateBusinessHandler updateBusiness,
            SoftDeleteBusinessHandler deleteBusiness,
            GetBusinessLocationsPageHandler getBusinessLocationsPage,
            GetBusinessLocationForEditHandler getBusinessLocationForEdit,
            CreateBusinessLocationHandler createBusinessLocation,
            UpdateBusinessLocationHandler updateBusinessLocation,
            SoftDeleteBusinessLocationHandler deleteBusinessLocation,
            GetBusinessMembersPageHandler getBusinessMembersPage,
            GetBusinessMemberForEditHandler getBusinessMemberForEdit,
            CreateBusinessMemberHandler createBusinessMember,
            UpdateBusinessMemberHandler updateBusinessMember,
            DeleteBusinessMemberHandler deleteBusinessMember,
            GetBusinessInvitationsPageHandler getBusinessInvitationsPage,
            CreateBusinessInvitationHandler createBusinessInvitation,
            ResendBusinessInvitationHandler resendBusinessInvitation,
            RevokeBusinessInvitationHandler revokeBusinessInvitation,
            ApproveBusinessHandler approveBusiness,
            SuspendBusinessHandler suspendBusiness,
            ReactivateBusinessHandler reactivateBusiness,
            RequestPasswordResetHandler requestPasswordReset,
            RequestEmailConfirmationHandler requestEmailConfirmation,
            ConfirmUserEmailByAdminHandler confirmUserEmail,
            LockUserByAdminHandler lockUser,
            UnlockUserByAdminHandler unlockUser,
            AdminReferenceDataService referenceData)
        {
            _getBusinessesPage = getBusinessesPage;
            _getBusinessForEdit = getBusinessForEdit;
            _createBusiness = createBusiness;
            _updateBusiness = updateBusiness;
            _deleteBusiness = deleteBusiness;
            _getBusinessLocationsPage = getBusinessLocationsPage;
            _getBusinessLocationForEdit = getBusinessLocationForEdit;
            _createBusinessLocation = createBusinessLocation;
            _updateBusinessLocation = updateBusinessLocation;
            _deleteBusinessLocation = deleteBusinessLocation;
            _getBusinessMembersPage = getBusinessMembersPage;
            _getBusinessMemberForEdit = getBusinessMemberForEdit;
            _createBusinessMember = createBusinessMember;
            _updateBusinessMember = updateBusinessMember;
            _deleteBusinessMember = deleteBusinessMember;
            _getBusinessInvitationsPage = getBusinessInvitationsPage;
            _createBusinessInvitation = createBusinessInvitation;
            _resendBusinessInvitation = resendBusinessInvitation;
            _revokeBusinessInvitation = revokeBusinessInvitation;
            _approveBusiness = approveBusiness;
            _suspendBusiness = suspendBusiness;
            _reactivateBusiness = reactivateBusiness;
            _requestPasswordReset = requestPasswordReset;
            _requestEmailConfirmation = requestEmailConfirmation;
            _confirmUserEmail = confirmUserEmail;
            _lockUser = lockUser;
            _unlockUser = unlockUser;
            _referenceData = referenceData;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int page = 1,
            int pageSize = 20,
            string? query = null,
            BusinessOperationalStatus? operationalStatus = null,
            bool attentionOnly = false,
            CancellationToken ct = default)
        {
            var (items, total) = await _getBusinessesPage.HandleAsync(
                page,
                pageSize,
                query,
                operationalStatus,
                attentionOnly,
                ct);

            var vm = new BusinessesListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                OperationalStatus = operationalStatus,
                AttentionOnly = attentionOnly,
                PageSizeItems = BuildPageSizeItems(pageSize),
                OperationalStatusItems = BuildBusinessStatusItems(operationalStatus),
                Items = items.Select(x => new BusinessListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    LegalName = x.LegalName,
                    Category = x.Category,
                    IsActive = x.IsActive,
                    OperationalStatus = x.OperationalStatus,
                    MemberCount = x.MemberCount,
                    ActiveOwnerCount = x.ActiveOwnerCount,
                    LocationCount = x.LocationCount,
                    PrimaryLocationCount = x.PrimaryLocationCount,
                    InvitationCount = x.InvitationCount,
                    HasContactEmailConfigured = x.HasContactEmailConfigured,
                    HasLegalNameConfigured = x.HasLegalNameConfigured,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken ct = default)
        {
            var vm = new BusinessEditVm
            {
                IsActive = false
            };
            await PopulateBusinessFormOptionsAsync(vm, ct);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BusinessEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: true);
            }

            var dto = new BusinessCreateDto
            {
                Name = vm.Name,
                LegalName = vm.LegalName,
                TaxId = vm.TaxId,
                ShortDescription = vm.ShortDescription,
                WebsiteUrl = vm.WebsiteUrl,
                ContactEmail = vm.ContactEmail,
                ContactPhoneE164 = vm.ContactPhoneE164,
                Category = vm.Category,
                DefaultCurrency = vm.DefaultCurrency,
                DefaultCulture = vm.DefaultCulture,
                IsActive = vm.IsActive
            };

            try
            {
                var businessId = await _createBusiness.HandleAsync(dto, ct);

                if (vm.OwnerUserId.HasValue)
                {
                    await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto
                    {
                        BusinessId = businessId,
                        UserId = vm.OwnerUserId.Value,
                        Role = BusinessMemberRole.Owner,
                        IsActive = true
                    }, ct);
                }

                TempData["Success"] = vm.OwnerUserId.HasValue
                    ? "Business created and owner assigned."
                    : "Business created. Next, add a primary location and assign an owner.";
                return RedirectOrHtmx(nameof(Edit), new { id = businessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)
        {
            var dto = await _getBusinessForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = MapBusinessEditVm(dto);
            await PopulateBusinessFormOptionsAsync(vm, ct);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Setup(Guid id, CancellationToken ct = default)
        {
            var dto = await _getBusinessForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = MapBusinessEditVm(dto);
            await PopulateBusinessFormOptionsAsync(vm, ct);
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> SetupMembersPreview(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                return PartialView("~/Views/Businesses/_SetupMembersPreview.cshtml", new BusinessSetupMembersPreviewVm
                {
                    BusinessId = businessId
                });
            }

            var (items, _) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 50, null, ct);
            var attentionMembers = items
                .Where(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > DateTime.UtcNow))
                .Take(5)
                .Select(x => new BusinessMemberListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    UserId = x.UserId,
                    UserDisplayName = x.UserDisplayName,
                    UserEmail = x.UserEmail,
                    EmailConfirmed = x.EmailConfirmed,
                    LockoutEndUtc = x.LockoutEndUtc,
                    Role = x.Role,
                    IsActive = x.IsActive,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                })
                .ToList();

            return PartialView("~/Views/Businesses/_SetupMembersPreview.cshtml", new BusinessSetupMembersPreviewVm
            {
                BusinessId = businessId,
                AttentionCount = items.Count(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > DateTime.UtcNow)),
                Items = attentionMembers
            });
        }

        [HttpGet]
        public async Task<IActionResult> SetupInvitationsPreview(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                return PartialView("~/Views/Businesses/_SetupInvitationsPreview.cshtml", new BusinessSetupInvitationsPreviewVm
                {
                    BusinessId = businessId
                });
            }

            var (items, _) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 50, null, ct);
            var openInvitations = items
                .Where(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired)
                .Take(5)
                .Select(x => new BusinessInvitationListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Email = x.Email,
                    Role = x.Role,
                    Status = x.Status,
                    InvitedByDisplayName = x.InvitedByDisplayName,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    AcceptedAtUtc = x.AcceptedAtUtc,
                    RevokedAtUtc = x.RevokedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    Note = x.Note
                })
                .ToList();

            return PartialView("~/Views/Businesses/_SetupInvitationsPreview.cshtml", new BusinessSetupInvitationsPreviewVm
            {
                BusinessId = businessId,
                OpenCount = items.Count(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired),
                Items = openInvitations
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BusinessEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: false);
            }

            var dto = new BusinessEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name,
                LegalName = vm.LegalName,
                TaxId = vm.TaxId,
                ShortDescription = vm.ShortDescription,
                WebsiteUrl = vm.WebsiteUrl,
                ContactEmail = vm.ContactEmail,
                ContactPhoneE164 = vm.ContactPhoneE164,
                Category = vm.Category,
                DefaultCurrency = vm.DefaultCurrency,
                DefaultCulture = vm.DefaultCulture,
                IsActive = vm.IsActive
            };

            try
            {
                await _updateBusiness.HandleAsync(dto, ct);
                TempData["Success"] = "Business updated.";
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the business and try again.";
                return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(BusinessEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessSetupEditor(vm);
            }

            var dto = new BusinessEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                Name = vm.Name,
                LegalName = vm.LegalName,
                TaxId = vm.TaxId,
                ShortDescription = vm.ShortDescription,
                WebsiteUrl = vm.WebsiteUrl,
                ContactEmail = vm.ContactEmail,
                ContactPhoneE164 = vm.ContactPhoneE164,
                Category = vm.Category,
                DefaultCurrency = vm.DefaultCurrency,
                DefaultCulture = vm.DefaultCulture,
                IsActive = vm.IsActive
            };

            try
            {
                await _updateBusiness.HandleAsync(dto, ct);
                TempData["Success"] = "Business setup saved.";
                return RedirectOrHtmx(nameof(Setup), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the business and try again.";
                return RedirectOrHtmx(nameof(Setup), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessFormOptionsAsync(vm, ct);
                return RenderBusinessSetupEditor(vm);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            Result result = await _deleteBusiness.HandleAsync(new BusinessDeleteDto
            {
                Id = id,
                RowVersion = rowVersion ?? Array.Empty<byte>()
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Business archived." : (result.Error ?? "Failed to archive business.");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _approveBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>()
                }, ct);

                TempData["Success"] = "Business approved for operational use.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend([FromForm] Guid id, [FromForm] byte[]? rowVersion, [FromForm] string? note, CancellationToken ct = default)
        {
            try
            {
                await _suspendBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>(),
                    Note = note
                }, ct);

                TempData["Success"] = "Business suspended.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _reactivateBusiness.HandleAsync(new BusinessLifecycleActionDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>()
                }, ct);

                TempData["Success"] = "Business reactivated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(Edit), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Locations(Guid businessId, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var (items, total) = await _getBusinessLocationsPage.HandleAsync(businessId, page, pageSize, query, ct);

            var vm = new BusinessLocationsListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Items = items.Select(x => new BusinessLocationListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    City = x.City,
                    Region = x.Region,
                    CountryCode = x.CountryCode,
                    IsPrimary = x.IsPrimary,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLocation(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(new BusinessLocationEditVm
            {
                BusinessId = businessId,
                CountryCode = "DE",
                Business = business
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLocation(BusinessLocationEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: true);
            }

            try
            {
                await _createBusinessLocation.HandleAsync(new BusinessLocationCreateDto
                {
                    BusinessId = vm.BusinessId,
                    Name = vm.Name,
                    AddressLine1 = vm.AddressLine1,
                    AddressLine2 = vm.AddressLine2,
                    City = vm.City,
                    Region = vm.Region,
                    CountryCode = vm.CountryCode,
                    PostalCode = vm.PostalCode,
                    Coordinate = BuildCoordinate(vm),
                    IsPrimary = vm.IsPrimary,
                    OpeningHoursJson = vm.OpeningHoursJson,
                    InternalNote = vm.InternalNote
                }, ct);

                TempData["Success"] = "Business location created.";
                return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditLocation(Guid id, CancellationToken ct = default)
        {
            var dto = await _getBusinessLocationForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                TempData["Error"] = "Business location not found.";
                return RedirectToAction(nameof(Index));
            }

            var business = await LoadBusinessContextAsync(dto.BusinessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new BusinessLocationEditVm
            {
                Id = dto.Id,
                BusinessId = dto.BusinessId,
                RowVersion = dto.RowVersion,
                Name = dto.Name,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                Region = dto.Region,
                CountryCode = dto.CountryCode,
                PostalCode = dto.PostalCode,
                Latitude = dto.Coordinate?.Latitude,
                Longitude = dto.Coordinate?.Longitude,
                AltitudeMeters = dto.Coordinate?.AltitudeMeters,
                IsPrimary = dto.IsPrimary,
                OpeningHoursJson = dto.OpeningHoursJson,
                InternalNote = dto.InternalNote,
                Business = business
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(BusinessLocationEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: false);
            }

            try
            {
                await _updateBusinessLocation.HandleAsync(new BusinessLocationEditDto
                {
                    Id = vm.Id,
                    BusinessId = vm.BusinessId,
                    RowVersion = vm.RowVersion ?? Array.Empty<byte>(),
                    Name = vm.Name,
                    AddressLine1 = vm.AddressLine1,
                    AddressLine2 = vm.AddressLine2,
                    City = vm.City,
                    Region = vm.Region,
                    CountryCode = vm.CountryCode,
                    PostalCode = vm.PostalCode,
                    Coordinate = BuildCoordinate(vm),
                    IsPrimary = vm.IsPrimary,
                    OpeningHoursJson = vm.OpeningHoursJson,
                    InternalNote = vm.InternalNote
                }, ct);

                TempData["Success"] = "Business location updated.";
                return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the location and try again.";
                return RedirectOrHtmx(nameof(EditLocation), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                return RenderLocationEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation([FromForm] Guid id, [FromForm(Name = "userId")] Guid businessId, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _deleteBusinessLocation.HandleAsync(new BusinessLocationDeleteDto
            {
                Id = id,
                RowVersion = rowVersion ?? Array.Empty<byte>()
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? "Business location archived." : (result.Error ?? "Failed to archive location.");

            return RedirectToAction(nameof(Locations), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> Members(Guid businessId, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var (items, total) = await _getBusinessMembersPage.HandleAsync(businessId, page, pageSize, query, ct);

            var vm = new BusinessMembersListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Items = items.Select(x => new BusinessMemberListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    UserId = x.UserId,
                    UserDisplayName = x.UserDisplayName,
                    UserEmail = x.UserEmail,
                    EmailConfirmed = x.EmailConfirmed,
                    LockoutEndUtc = x.LockoutEndUtc,
                    Role = x.Role,
                    IsActive = x.IsActive,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Invitations(Guid businessId, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var (items, total) = await _getBusinessInvitationsPage.HandleAsync(businessId, page, pageSize, query, ct);

            var vm = new BusinessInvitationsListVm
            {
                Business = business,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = query ?? string.Empty,
                Items = items.Select(x => new BusinessInvitationListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Email = x.Email,
                    Role = x.Role,
                    Status = x.Status,
                    InvitedByDisplayName = x.InvitedByDisplayName,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    AcceptedAtUtc = x.AcceptedAtUtc,
                    RevokedAtUtc = x.RevokedAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    Note = x.Note
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateInvitation(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new BusinessInvitationCreateVm
            {
                BusinessId = businessId,
                Business = business,
                Role = BusinessMemberRole.Owner,
                ExpiresInDays = 7
            };
            PopulateInvitationFormOptions(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvitation(BusinessInvitationCreateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                PopulateInvitationFormOptions(vm);
                return RenderInvitationEditor(vm);
            }

            try
            {
                await _createBusinessInvitation.HandleAsync(new BusinessInvitationCreateDto
                {
                    BusinessId = vm.BusinessId,
                    Email = vm.Email,
                    Role = vm.Role,
                    ExpiresInDays = vm.ExpiresInDays,
                    Note = vm.Note
                }, ct);

                TempData["Success"] = "Invitation sent.";
                return RedirectOrHtmx(nameof(Invitations), new { businessId = vm.BusinessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                PopulateInvitationFormOptions(vm);
                return RenderInvitationEditor(vm);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendInvitation([FromForm] Guid id, [FromForm] Guid businessId, CancellationToken ct = default)
        {
            try
            {
                await _resendBusinessInvitation.HandleAsync(new BusinessInvitationResendDto
                {
                    Id = id,
                    ExpiresInDays = 7
                }, ct);
                TempData["Success"] = "Invitation reissued and emailed again.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Invitations), new { businessId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeInvitation([FromForm] Guid id, [FromForm] Guid businessId, CancellationToken ct = default)
        {
            try
            {
                await _revokeBusinessInvitation.HandleAsync(new BusinessInvitationRevokeDto
                {
                    Id = id,
                    Note = "Revoked from WebAdmin."
                }, ct);
                TempData["Success"] = "Invitation revoked.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Invitations), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> CreateMember(Guid businessId, CancellationToken ct = default)
        {
            var business = await LoadBusinessContextAsync(businessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new BusinessMemberEditVm
            {
                BusinessId = businessId,
                Role = BusinessMemberRole.Owner,
                IsActive = true,
                Business = business
            };
            await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMember(BusinessMemberEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);
                return RenderMemberEditor(vm, isCreate: true);
            }

            try
            {
                await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto
                {
                    BusinessId = vm.BusinessId,
                    UserId = vm.UserId,
                    Role = vm.Role,
                    IsActive = vm.IsActive
                }, ct);

                TempData["Success"] = "Business member assigned.";
                return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);
                return RenderMemberEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditMember(Guid id, CancellationToken ct = default)
        {
            var dto = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                TempData["Error"] = "Business member not found.";
                return RedirectToAction(nameof(Index));
            }

            var business = await LoadBusinessContextAsync(dto.BusinessId, ct);
            if (business is null)
            {
                TempData["Error"] = "Business not found.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new BusinessMemberEditVm
            {
                Id = dto.Id,
                BusinessId = dto.BusinessId,
                UserId = dto.UserId,
                RowVersion = dto.RowVersion,
                UserDisplayName = dto.UserDisplayName,
                UserEmail = dto.UserEmail,
                EmailConfirmed = dto.EmailConfirmed,
                LockoutEndUtc = dto.LockoutEndUtc,
                Role = dto.Role,
                IsActive = dto.IsActive,
                Business = business
            };
            await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMember(BusinessMemberEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);
                return RenderMemberEditor(vm, isCreate: false);
            }

            try
            {
                await _updateBusinessMember.HandleAsync(new BusinessMemberEditDto
                {
                    Id = vm.Id,
                    BusinessId = vm.BusinessId,
                    UserId = vm.UserId,
                    Role = vm.Role,
                    IsActive = vm.IsActive,
                    RowVersion = vm.RowVersion ?? Array.Empty<byte>()
                }, ct);

                TempData["Success"] = "Business member updated.";
                return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the membership and try again.";
                return RedirectOrHtmx(nameof(EditMember), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateBusinessContextAsync(vm, ct);
                await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);
                return RenderMemberEditor(vm, isCreate: false);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember([FromForm] Guid id, [FromForm(Name = "userId")] Guid businessId, [FromForm] byte[]? rowVersion, CancellationToken ct = default)
        {
            try
            {
                await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto
                {
                    Id = id,
                    RowVersion = rowVersion ?? Array.Empty<byte>()
                }, ct);

                TempData["Success"] = "Business member removed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Members), new { businessId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMemberActivationEmail(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                TempData["Error"] = "Business member not found.";
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _requestEmailConfirmation.HandleAsync(
                new RequestEmailConfirmationDto
                {
                    Email = member.UserEmail
                },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Activation email sent."
                : (result.Error ?? "Failed to send activation email.");

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmMemberEmail(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                TempData["Error"] = "Business member not found.";
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _confirmUserEmail.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Email marked as confirmed."
                : (result.Error ?? "Failed to confirm email.");

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMemberPasswordReset(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                TempData["Error"] = "Business member not found.";
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _requestPasswordReset.HandleAsync(
                new RequestPasswordResetDto
                {
                    Email = member.UserEmail
                },
                ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Password reset email sent."
                : (result.Error ?? "Failed to send password reset email.");

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LockMemberUser(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                TempData["Error"] = "Business member not found.";
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _lockUser.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Account locked."
                : (result.Error ?? "Failed to lock account.");

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockMemberUser(
            [FromForm] Guid id,
            [FromForm] Guid businessId,
            [FromForm] bool returnToEdit = false,
            CancellationToken ct = default)
        {
            var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);
            if (member is null)
            {
                TempData["Error"] = "Business member not found.";
                return RedirectMemberSupport(returnToEdit, id, businessId);
            }

            var result = await _unlockUser.HandleAsync(new UserAdminActionDto
            {
                Id = member.UserId
            }, ct);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Account unlocked."
                : (result.Error ?? "Failed to unlock account.");

            return RedirectMemberSupport(returnToEdit, id, businessId);
        }

        private async Task PopulateBusinessFormOptionsAsync(BusinessEditVm vm, CancellationToken ct)
        {
            vm.CategoryOptions = Enum.GetValues<BusinessCategoryKind>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Category == x))
                .ToList();

            vm.OwnerUserOptions = await _referenceData.GetUserOptionsAsync(vm.OwnerUserId, includeEmpty: true, ct);
        }

        private async Task PopulateMemberFormOptionsAsync(BusinessMemberEditVm vm, bool includeUserSelection, CancellationToken ct)
        {
            vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Role == x))
                .ToList();

            if (includeUserSelection)
            {
                vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct);
            }
        }

        private async Task PopulateBusinessContextAsync(BusinessLocationEditVm vm, CancellationToken ct)
        {
            vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };
        }

        private async Task PopulateBusinessContextAsync(BusinessMemberEditVm vm, CancellationToken ct)
        {
            vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };
        }

        private async Task PopulateBusinessContextAsync(BusinessInvitationCreateVm vm, CancellationToken ct)
        {
            vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };
        }

        private static void PopulateInvitationFormOptions(BusinessInvitationCreateVm vm)
        {
            vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), vm.Role == x))
                .ToList();
        }

        private async Task<BusinessContextVm?> LoadBusinessContextAsync(Guid id, CancellationToken ct)
        {
            var dto = await _getBusinessForEdit.HandleAsync(id, ct);
            if (dto is null)
            {
                return null;
            }

            return new BusinessContextVm
            {
                Id = dto.Id,
                Name = dto.Name,
                LegalName = dto.LegalName,
                Category = dto.Category,
                IsActive = dto.IsActive,
                OperationalStatus = dto.OperationalStatus,
                ApprovedAtUtc = dto.ApprovedAtUtc,
                SuspendedAtUtc = dto.SuspendedAtUtc,
                SuspensionReason = dto.SuspensionReason,
                MemberCount = dto.MemberCount,
                ActiveOwnerCount = dto.ActiveOwnerCount,
                LocationCount = dto.LocationCount,
                PrimaryLocationCount = dto.PrimaryLocationCount,
                InvitationCount = dto.InvitationCount,
                HasContactEmailConfigured = dto.HasContactEmailConfigured,
                HasLegalNameConfigured = dto.HasLegalNameConfigured
            };
        }

        private IActionResult RenderBusinessEditor(BusinessEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            return isCreate ? View("Create", vm) : View("Edit", vm);
        }

        private IActionResult RenderLocationEditor(BusinessLocationEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            return isCreate ? View("CreateLocation", vm) : View("EditLocation", vm);
        }

        private IActionResult RenderMemberEditor(BusinessMemberEditVm vm, bool isCreate)
        {
            ViewData["IsCreate"] = isCreate;
            return isCreate ? View("CreateMember", vm) : View("EditMember", vm);
        }

        private IActionResult RenderInvitationEditor(BusinessInvitationCreateVm vm)
        {
            return View("CreateInvitation", vm);
        }

        private IActionResult RenderBusinessSetupEditor(BusinessEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Businesses/_BusinessSetupShell.cshtml", vm);
            }

            return View("Setup", vm);
        }

        private IActionResult RedirectMemberSupport(bool returnToEdit, Guid membershipId, Guid businessId)
        {
            return returnToEdit
                ? RedirectOrHtmx(nameof(EditMember), new { id = membershipId })
                : RedirectToAction(nameof(Members), new { businessId });
        }

        private static IEnumerable<SelectListItem> BuildBusinessStatusItems(BusinessOperationalStatus? selectedStatus)
        {
            yield return new SelectListItem("All statuses", string.Empty, !selectedStatus.HasValue);

            foreach (var status in Enum.GetValues<BusinessOperationalStatus>())
            {
                yield return new SelectListItem(status.ToString(), status.ToString(), selectedStatus == status);
            }
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

        private static BusinessEditVm MapBusinessEditVm(BusinessEditDto dto)
        {
            return new BusinessEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Name = dto.Name,
                LegalName = dto.LegalName,
                TaxId = dto.TaxId,
                ShortDescription = dto.ShortDescription,
                WebsiteUrl = dto.WebsiteUrl,
                ContactEmail = dto.ContactEmail,
                ContactPhoneE164 = dto.ContactPhoneE164,
                Category = dto.Category,
                DefaultCurrency = dto.DefaultCurrency,
                DefaultCulture = dto.DefaultCulture,
                IsActive = dto.IsActive,
                OperationalStatus = dto.OperationalStatus,
                ApprovedAtUtc = dto.ApprovedAtUtc,
                SuspendedAtUtc = dto.SuspendedAtUtc,
                SuspensionReason = dto.SuspensionReason,
                MemberCount = dto.MemberCount,
                ActiveOwnerCount = dto.ActiveOwnerCount,
                LocationCount = dto.LocationCount,
                PrimaryLocationCount = dto.PrimaryLocationCount,
                InvitationCount = dto.InvitationCount,
                HasContactEmailConfigured = dto.HasContactEmailConfigured,
                HasLegalNameConfigured = dto.HasLegalNameConfigured
            };
        }

        private static GeoCoordinateDto? BuildCoordinate(BusinessLocationEditVm vm)
        {
            if (!vm.Latitude.HasValue || !vm.Longitude.HasValue)
            {
                return null;
            }

            return new GeoCoordinateDto
            {
                Latitude = vm.Latitude.Value,
                Longitude = vm.Longitude.Value,
                AltitudeMeters = vm.AltitudeMeters
            };
        }

        private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)
        {
            var sizes = new[] { 10, 20, 50, 100 };
            return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();
        }
    }
}
