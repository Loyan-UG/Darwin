using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceBusinessesSourceTests : SecurityAndPerformanceSourceTestBase
{

    [Fact]
    public void MemberBusinessesController_Should_KeepAuthenticatedOnboardingAndEngagementAliases()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberBusinessesController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("public async Task<IActionResult> OnboardAsync(");
        source.Should().Contain("[HttpPost(\"onboarding\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/businesses/onboarding\")]");
        source.Should().Contain("public async Task<IActionResult> GetWithMyAccountAsync(");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessMemberArea\")]");
        source.Should().Contain("[HttpGet(\"{id:guid}/with-my-account\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/businesses/{id:guid}/with-my-account\")]");
        source.Should().Contain("public async Task<IActionResult> GetMyEngagementAsync(");
        source.Should().Contain("[HttpGet(\"{id:guid}/engagement/my\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/businesses/{id:guid}/engagement/my\")]");
    }


    [Fact]
    public void PublicBusinessesController_Should_KeepAnonymousDiscoveryAndDetailAliases()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicBusinessesController.cs"));

        source.Should().Contain("[AllowAnonymous]");
        source.Should().Contain("[Route(\"api/v1/public/businesses\")]");
        source.Should().Contain("public async Task<IActionResult> ListAsync(");
        source.Should().Contain("[HttpPost(\"list\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/businesses/list\")]");
        source.Should().Contain("public async Task<IActionResult> MapAsync(");
        source.Should().Contain("[HttpPost(\"map\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/businesses/map\")]");
        source.Should().Contain("public async Task<IActionResult> GetAsync(");
        source.Should().Contain("[HttpGet(\"{id:guid}\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/businesses/{id:guid}\")]");
    }


    [Fact]
    public void BusinessesController_Should_KeepSensitiveLifecycleActionsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("[HttpPost, ValidateAntiForgeryToken]");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.FullAdminAccess)]");
        source.Should().Contain("public async Task<IActionResult> Delete(");
        source.Should().Contain("public async Task<IActionResult> Approve(");
        source.Should().Contain("public async Task<IActionResult> Suspend(");
        source.Should().Contain("public async Task<IActionResult> Reactivate(");
        source.Should().Contain("public async Task<IActionResult> CreateMember(");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessSupportAndMembershipMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("[PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]");
        source.Should().Contain("[PermissionAuthorize(PermissionKeys.FullAdminAccess)]");
        source.Should().Contain("[HttpPost, ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateInvitation(BusinessInvitationCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> ResendInvitation(");
        source.Should().Contain("public async Task<IActionResult> RevokeInvitation(");
        source.Should().Contain("public async Task<IActionResult> CreateLocation(BusinessLocationEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditLocation(BusinessLocationEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteLocation(");
        source.Should().Contain("public async Task<IActionResult> CreateMember(BusinessMemberEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditMember(BusinessMemberEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteMember(");
        source.Should().Contain("public async Task<IActionResult> SendMemberActivationEmail(");
        source.Should().Contain("public async Task<IActionResult> ConfirmMemberEmail(");
        source.Should().Contain("public async Task<IActionResult> SendMemberPasswordReset(");
        source.Should().Contain("public async Task<IActionResult> LockMemberUser(");
        source.Should().Contain("public async Task<IActionResult> UnlockMemberUser(");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessesIndexItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("Name = x.Name,");
        controllerSource.Should().Contain("LegalName = x.LegalName,");
        controllerSource.Should().Contain("Category = x.Category,");
        controllerSource.Should().Contain("IsActive = x.IsActive,");
        controllerSource.Should().Contain("OperationalStatus = x.OperationalStatus,");
        controllerSource.Should().Contain("MemberCount = x.MemberCount,");
        controllerSource.Should().Contain("ActiveOwnerCount = x.ActiveOwnerCount,");
        controllerSource.Should().Contain("LocationCount = x.LocationCount,");
        controllerSource.Should().Contain("PrimaryLocationCount = x.PrimaryLocationCount,");
        controllerSource.Should().Contain("InvitationCount = x.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = x.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = x.HasLegalNameConfigured,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessesIndexRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderBusinessesWorkspace(BusinessesListVm vm)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("return PartialView(\"Index\", vm);");
        controllerSource.Should().Contain("return View(\"Index\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepMembersWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Members(");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessMembersPage.HandleAsync(businessId, page, pageSize, query, filter, ct);");
        controllerSource.Should().Contain("var vm = new BusinessMembersListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessMemberFilterItems(filter),");
        controllerSource.Should().Contain("Summary = await BuildBusinessMemberOpsSummaryAsync(businessId, ct).ConfigureAwait(false),");
        controllerSource.Should().Contain("Playbooks = BuildBusinessMemberPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessMemberListItemVm");
        controllerSource.Should().Contain("return RenderMembersWorkspace(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepMembersItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessMemberListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("UserId = x.UserId,");
        controllerSource.Should().Contain("UserDisplayName = x.UserDisplayName,");
        controllerSource.Should().Contain("UserEmail = x.UserEmail,");
        controllerSource.Should().Contain("EmailConfirmed = x.EmailConfirmed,");
        controllerSource.Should().Contain("LockoutEndUtc = x.LockoutEndUtc,");
        controllerSource.Should().Contain("Role = x.Role,");
        controllerSource.Should().Contain("IsActive = x.IsActive,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
    }


    [Fact]
    public void BusinessesController_Should_KeepMembersRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderMembersWorkspace(BusinessMembersListVm vm)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("return PartialView(\"Members\", vm);");
        controllerSource.Should().Contain("return View(\"Members\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepInvitationsWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Invitations(");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessInvitationsPage.HandleAsync(businessId, page, pageSize, query, filter, ct);");
        controllerSource.Should().Contain("var vm = new BusinessInvitationsListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessInvitationFilterItems(filter),");
        controllerSource.Should().Contain("Summary = await BuildBusinessInvitationOpsSummaryAsync(businessId, ct).ConfigureAwait(false),");
        controllerSource.Should().Contain("Playbooks = BuildBusinessInvitationPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessInvitationListItemVm");
        controllerSource.Should().Contain("return RenderInvitationsWorkspace(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepInvitationsItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessInvitationListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("Email = x.Email,");
        controllerSource.Should().Contain("Role = x.Role,");
        controllerSource.Should().Contain("Status = x.Status,");
        controllerSource.Should().Contain("InvitedByDisplayName = x.InvitedByDisplayName,");
        controllerSource.Should().Contain("ExpiresAtUtc = x.ExpiresAtUtc,");
        controllerSource.Should().Contain("AcceptedAtUtc = x.AcceptedAtUtc,");
        controllerSource.Should().Contain("RevokedAtUtc = x.RevokedAtUtc,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc,");
        controllerSource.Should().Contain("Note = x.Note");
    }


    [Fact]
    public void BusinessesController_Should_KeepInvitationsRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderInvitationsWorkspace(BusinessInvitationsListVm vm)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("return PartialView(\"Invitations\", vm);");
        controllerSource.Should().Contain("return View(\"Invitations\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepLocationEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> EditLocation(Guid id, int page = 1, int pageSize = 20, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessLocationForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessLocationNotFound\");");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(dto.BusinessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var vm = new BusinessLocationEditVm");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("BusinessId = dto.BusinessId,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("RowVersion = dto.RowVersion,");
        controllerSource.Should().Contain("Name = dto.Name,");
        controllerSource.Should().Contain("AddressLine1 = dto.AddressLine1,");
        controllerSource.Should().Contain("AddressLine2 = dto.AddressLine2,");
        controllerSource.Should().Contain("City = dto.City,");
        controllerSource.Should().Contain("Region = dto.Region,");
        controllerSource.Should().Contain("CountryCode = dto.CountryCode,");
        controllerSource.Should().Contain("PostalCode = dto.PostalCode,");
        controllerSource.Should().Contain("Latitude = dto.Coordinate?.Latitude,");
        controllerSource.Should().Contain("Longitude = dto.Coordinate?.Longitude,");
        controllerSource.Should().Contain("AltitudeMeters = dto.Coordinate?.AltitudeMeters,");
        controllerSource.Should().Contain("IsPrimary = dto.IsPrimary,");
        controllerSource.Should().Contain("OpeningHoursJson = dto.OpeningHoursJson,");
        controllerSource.Should().Contain("InternalNote = dto.InternalNote,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("return RenderLocationEditor(vm, isCreate: false);");
    }


    [Fact]
    public void BusinessesController_Should_KeepCreateLocationEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateLocation(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("return RenderLocationEditor(new BusinessLocationEditVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("CountryCode = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("}, isCreate: true);");
    }


    [Fact]
    public void BusinessesController_Should_KeepLocationEditorRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderLocationEditor(BusinessLocationEditVm vm, bool isCreate)");
        controllerSource.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessLocationEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return isCreate ? View(\"CreateLocation\", vm) : View(\"EditLocation\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepMemberEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> EditMember(Guid id, int page = 1, int pageSize = 20, string? query = null, BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessMemberForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessMemberNotFound\");");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(dto.BusinessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var vm = new BusinessMemberEditVm");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("BusinessId = dto.BusinessId,");
        controllerSource.Should().Contain("UserId = dto.UserId,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("RowVersion = dto.RowVersion,");
        controllerSource.Should().Contain("UserDisplayName = dto.UserDisplayName,");
        controllerSource.Should().Contain("UserEmail = dto.UserEmail,");
        controllerSource.Should().Contain("EmailConfirmed = dto.EmailConfirmed,");
        controllerSource.Should().Contain("LockoutEndUtc = dto.LockoutEndUtc,");
        controllerSource.Should().Contain("Role = dto.Role,");
        controllerSource.Should().Contain("IsActive = dto.IsActive,");
        controllerSource.Should().Contain("IsLastActiveOwner = dto.IsLastActiveOwner,");
        controllerSource.Should().Contain("OverrideReason = null,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("await PopulateMemberFormOptionsAsync(vm, includeUserSelection: false, ct);");
        controllerSource.Should().Contain("return RenderMemberEditor(vm, isCreate: false);");
    }


    [Fact]
    public void BusinessesController_Should_KeepCreateMemberEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateMember(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var vm = new BusinessMemberEditVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("Role = BusinessMemberRole.Owner,");
        controllerSource.Should().Contain("IsActive = true,");
        controllerSource.Should().Contain("Business = business");
        controllerSource.Should().Contain("await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);");
        controllerSource.Should().Contain("return RenderMemberEditor(vm, isCreate: true);");
    }


    [Fact]
    public void BusinessesController_Should_KeepMemberEditorRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderMemberEditor(BusinessMemberEditVm vm, bool isCreate)");
        controllerSource.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessMemberEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return isCreate ? View(\"CreateMember\", vm) : View(\"EditMember\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepInvitationEditorCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateInvitation(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessInvitationQueueFilter filter = BusinessInvitationQueueFilter.All, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var vm = new BusinessInvitationCreateVm");
        controllerSource.Should().Contain("BusinessId = businessId,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Role = BusinessMemberRole.Owner,");
        controllerSource.Should().Contain("ExpiresInDays = 7");
        controllerSource.Should().Contain("PopulateInvitationFormOptions(vm);");
        controllerSource.Should().Contain("return RenderInvitationEditor(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepCreateLocationSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateLocation(BusinessLocationEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("if (!ModelState.IsValid)");
        controllerSource.Should().Contain("await PopulateBusinessContextAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderLocationEditor(vm, isCreate: true);");
        controllerSource.Should().Contain("await _createBusinessLocation.HandleAsync(new BusinessLocationCreateDto");
        controllerSource.Should().Contain("BusinessId = vm.BusinessId,");
        controllerSource.Should().Contain("Coordinate = BuildCoordinate(vm),");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessLocationCreated\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Locations), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });");
    }


    [Fact]
    public void BusinessesController_Should_KeepCreateMemberSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateMember(BusinessMemberEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("await PopulateMemberFormOptionsAsync(vm, includeUserSelection: true, ct);");
        controllerSource.Should().Contain("return RenderMemberEditor(vm, isCreate: true);");
        controllerSource.Should().Contain("await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto");
        controllerSource.Should().Contain("BusinessId = vm.BusinessId,");
        controllerSource.Should().Contain("UserId = vm.UserId,");
        controllerSource.Should().Contain("Role = vm.Role,");
        controllerSource.Should().Contain("IsActive = vm.IsActive");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessMemberAssigned\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Members), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });");
    }


    [Fact]
    public void BusinessesController_Should_KeepCreateInvitationSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> CreateInvitation(BusinessInvitationCreateVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("PopulateInvitationFormOptions(vm);");
        controllerSource.Should().Contain("return RenderInvitationEditor(vm);");
        controllerSource.Should().Contain("await _createBusinessInvitation.HandleAsync(new BusinessInvitationCreateDto");
        controllerSource.Should().Contain("BusinessId = vm.BusinessId,");
        controllerSource.Should().Contain("Email = vm.Email,");
        controllerSource.Should().Contain("Role = vm.Role,");
        controllerSource.Should().Contain("ExpiresInDays = vm.ExpiresInDays,");
        controllerSource.Should().Contain("Note = vm.Note");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessInvitationSent\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Invitations), new { businessId = vm.BusinessId, page = vm.Page, pageSize = vm.PageSize, query = vm.Query, filter = vm.Filter });");
    }


    [Fact]
    public void BusinessesController_Should_KeepInvitationEditorRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderInvitationEditor(BusinessInvitationCreateVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessInvitationEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"CreateInvitation\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorCreateCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct = default)");
        controllerSource.Should().Contain("var vm = new BusinessEditVm");
        controllerSource.Should().Contain("IsActive = false");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: true);");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorCreateSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Create(BusinessEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("if (!ModelState.IsValid)");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: true);");
        controllerSource.Should().Contain("var dto = new BusinessCreateDto");
        controllerSource.Should().Contain("Name = vm.Name,");
        controllerSource.Should().Contain("LegalName = vm.LegalName,");
        controllerSource.Should().Contain("DefaultCurrency = vm.DefaultCurrency,");
        controllerSource.Should().Contain("CommunicationReplyToEmail = vm.CommunicationReplyToEmail,");
        controllerSource.Should().Contain("OperationalAlertEmailsEnabled = vm.OperationalAlertEmailsEnabled,");
        controllerSource.Should().Contain("IsActive = vm.IsActive");
        controllerSource.Should().Contain("var businessId = await _createBusiness.HandleAsync(dto, ct);");
        controllerSource.Should().Contain("if (vm.OwnerUserId.HasValue)");
        controllerSource.Should().Contain("await _createBusinessMember.HandleAsync(new BusinessMemberCreateDto");
        controllerSource.Should().Contain("Role = BusinessMemberRole.Owner,");
        controllerSource.Should().Contain("TempData[\"Success\"] = vm.OwnerUserId.HasValue");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Edit), new { id = businessId });");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorEditCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var vm = MapBusinessEditVm(dto);");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: false);");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorEditSubmitContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Edit(BusinessEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessEditor(vm, isCreate: false);");
        controllerSource.Should().Contain("var dto = new BusinessEditDto");
        controllerSource.Should().Contain("Id = vm.Id,");
        controllerSource.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        controllerSource.Should().Contain("SupportEmail = vm.SupportEmail,");
        controllerSource.Should().Contain("CustomerMarketingEmailsEnabled = vm.CustomerMarketingEmailsEnabled,");
        controllerSource.Should().Contain("await _updateBusiness.HandleAsync(dto, ct);");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessUpdated\");");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessConcurrencyConflict\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Edit), new { id = vm.Id });");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessEditorRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderBusinessEditor(BusinessEditVm vm, bool isCreate)");
        controllerSource.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessEditorShell.cshtml\", vm);");
        controllerSource.Should().Contain("return isCreate ? View(\"Create\", vm) : View(\"Edit\", vm);");
        controllerSource.Should().Contain("private IActionResult RenderBusinessSetupEditor(BusinessEditVm vm)");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_BusinessSetupShell.cshtml\", vm);");
        controllerSource.Should().Contain("return View(\"Setup\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessSetupContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Setup(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("var vm = MapBusinessEditVm(dto);");
        controllerSource.Should().Contain("await PopulateBusinessFormOptionsAsync(vm, ct);");
        controllerSource.Should().Contain("return RenderBusinessSetupEditor(vm);");
        controllerSource.Should().Contain("public async Task<IActionResult> Setup(BusinessEditVm vm, CancellationToken ct = default)");
        controllerSource.Should().Contain("if (!ModelState.IsValid)");
        controllerSource.Should().Contain("return RenderBusinessSetupEditor(vm);");
        controllerSource.Should().Contain("await _updateBusiness.HandleAsync(dto, ct);");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessUpdated\");");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Subscription(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("var vm = await BuildBusinessSubscriptionWorkspaceAsync(business, ct);");
        controllerSource.Should().Contain("return RenderSubscriptionWorkspace(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionWorkspaceBuilderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessSubscriptionWorkspaceVm> BuildBusinessSubscriptionWorkspaceAsync(BusinessContextVm business, CancellationToken ct)");
        controllerSource.Should().Contain("var subscription = await BuildBusinessSubscriptionSnapshotAsync(business.Id, ct);");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct);");
        controllerSource.Should().Contain("var workspaceManagementWebsiteUrl = BuildSubscriptionManagementWebsiteUrl(managementWebsiteUrl, business.Id, planCode: null);");
        controllerSource.Should().Contain("var plans = await _getBillingPlans.HandleAsync(");
        controllerSource.Should().Contain("var recentInvoices = await _getBusinessSubscriptionInvoicesPage.HandleAsync(");
        controllerSource.Should().Contain("pageSize: 5,");
        controllerSource.Should().Contain("var invoiceSummary = await _getBusinessSubscriptionInvoiceOpsSummary.HandleAsync(business.Id, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var planVms = new List<BusinessBillingPlanVm>();");
        controllerSource.Should().Contain("var validation = await _createSubscriptionCheckoutIntent.ValidateAsync(business.Id, x.Id, ct);");
        controllerSource.Should().Contain("planVms.Add(new BusinessBillingPlanVm");
        controllerSource.Should().Contain("InvoiceSummary = MapBusinessSubscriptionInvoiceOpsSummaryVm(invoiceSummary),");
        controllerSource.Should().Contain("RecentInvoices = recentInvoices.Items.Select(MapBusinessSubscriptionInvoiceListItemVm).ToList(),");
        controllerSource.Should().Contain("Playbooks = BuildSubscriptionPlaybooks(business.Id, subscription, managementWebsiteConfigured)");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionRenderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderSubscriptionWorkspace(BusinessSubscriptionWorkspaceVm vm)");
        controllerSource.Should().Contain("return PartialView(\"Subscription\", vm);");
        controllerSource.Should().Contain("return View(\"Subscription\", vm);");
        controllerSource.Should().Contain("private IActionResult RenderSubscriptionInvoicesWorkspace(BusinessSubscriptionInvoicesListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"SubscriptionInvoices\", vm);");
        controllerSource.Should().Contain("return View(\"SubscriptionInvoices\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionInvoicesWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SubscriptionInvoices(");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var result = await _getBusinessSubscriptionInvoicesPage.HandleAsync(");
        controllerSource.Should().Contain("var summary = await _getBusinessSubscriptionInvoiceOpsSummary.HandleAsync(businessId, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var vm = new BusinessSubscriptionInvoicesListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = result.Total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessSubscriptionInvoiceFilterItems(filter),");
        controllerSource.Should().Contain("Summary = MapBusinessSubscriptionInvoiceOpsSummaryVm(summary),");
        controllerSource.Should().Contain("Items = result.Items.Select(MapBusinessSubscriptionInvoiceListItemVm).ToList()");
        controllerSource.Should().Contain("return RenderSubscriptionInvoicesWorkspace(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionInvoiceMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static BusinessSubscriptionInvoiceOpsSummaryVm MapBusinessSubscriptionInvoiceOpsSummaryVm(BusinessSubscriptionInvoiceOpsSummaryDto dto)");
        controllerSource.Should().Contain("private static BusinessSubscriptionInvoiceListItemVm MapBusinessSubscriptionInvoiceListItemVm(BusinessSubscriptionInvoiceListItemDto dto)");
        controllerSource.Should().Contain("TotalCount = dto.TotalCount,");
        controllerSource.Should().Contain("OpenCount = dto.OpenCount,");
        controllerSource.Should().Contain("PaidCount = dto.PaidCount,");
        controllerSource.Should().Contain("DraftCount = dto.DraftCount,");
        controllerSource.Should().Contain("UncollectibleCount = dto.UncollectibleCount,");
        controllerSource.Should().Contain("HostedLinkMissingCount = dto.HostedLinkMissingCount,");
        controllerSource.Should().Contain("StripeCount = dto.StripeCount,");
        controllerSource.Should().Contain("OverdueCount = dto.OverdueCount,");
        controllerSource.Should().Contain("PdfMissingCount = dto.PdfMissingCount");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("BusinessId = dto.BusinessId,");
        controllerSource.Should().Contain("BusinessSubscriptionId = dto.BusinessSubscriptionId,");
        controllerSource.Should().Contain("Provider = dto.Provider,");
        controllerSource.Should().Contain("ProviderInvoiceId = dto.ProviderInvoiceId,");
        controllerSource.Should().Contain("Status = dto.Status,");
        controllerSource.Should().Contain("TotalMinor = dto.TotalMinor,");
        controllerSource.Should().Contain("Currency = dto.Currency,");
        controllerSource.Should().Contain("IssuedAtUtc = dto.IssuedAtUtc,");
        controllerSource.Should().Contain("DueAtUtc = dto.DueAtUtc,");
        controllerSource.Should().Contain("PaidAtUtc = dto.PaidAtUtc,");
        controllerSource.Should().Contain("HostedInvoiceUrl = dto.HostedInvoiceUrl,");
        controllerSource.Should().Contain("PdfUrl = dto.PdfUrl,");
        controllerSource.Should().Contain("FailureReason = dto.FailureReason,");
        controllerSource.Should().Contain("PlanName = dto.PlanName,");
        controllerSource.Should().Contain("PlanCode = dto.PlanCode,");
        controllerSource.Should().Contain("HasHostedInvoiceUrl = dto.HasHostedInvoiceUrl,");
        controllerSource.Should().Contain("HasPdfUrl = dto.HasPdfUrl,");
        controllerSource.Should().Contain("IsStripe = dto.IsStripe,");
        controllerSource.Should().Contain("IsOverdue = dto.IsOverdue");
    }


    [Fact]
    public void BusinessesController_Should_KeepLocationsWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Locations(Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessLocationQueueFilter filter = BusinessLocationQueueFilter.All, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessLocationsPage.HandleAsync(businessId, page, pageSize, query, filter, ct);");
        controllerSource.Should().Contain("var summary = await _getBusinessLocationsPage.GetSummaryAsync(businessId, ct);");
        controllerSource.Should().Contain("var vm = new BusinessLocationsListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Filter = filter,");
        controllerSource.Should().Contain("FilterItems = BuildBusinessLocationFilterItems(filter),");
        controllerSource.Should().Contain("Summary = new BusinessLocationOpsSummaryVm");
        controllerSource.Should().Contain("Playbooks = BuildBusinessLocationPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessLocationListItemVm");
        controllerSource.Should().Contain("return RenderLocationsWorkspace(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepLocationsSummaryAndItemMappingContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("TotalCount = summary.TotalCount,");
        controllerSource.Should().Contain("PrimaryCount = summary.PrimaryCount,");
        controllerSource.Should().Contain("MissingAddressCount = summary.MissingAddressCount,");
        controllerSource.Should().Contain("MissingCoordinatesCount = summary.MissingCoordinatesCount");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessLocationListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("Name = x.Name,");
        controllerSource.Should().Contain("City = x.City,");
        controllerSource.Should().Contain("Region = x.Region,");
        controllerSource.Should().Contain("CountryCode = x.CountryCode,");
        controllerSource.Should().Contain("IsPrimary = x.IsPrimary,");
        controllerSource.Should().Contain("HasAddress = x.HasAddress,");
        controllerSource.Should().Contain("HasCoordinates = x.HasCoordinates,");
        controllerSource.Should().Contain("ModifiedAtUtc = x.ModifiedAtUtc,");
        controllerSource.Should().Contain("RowVersion = x.RowVersion");
    }


    [Fact]
    public void BusinessesController_Should_KeepLocationsRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderLocationsWorkspace(BusinessLocationsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"Locations\", vm);");
        controllerSource.Should().Contain("return View(\"Locations\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepOwnerOverrideAuditsWorkspaceCompositionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> OwnerOverrideAudits(Guid businessId, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var (items, total) = await _getBusinessOwnerOverrideAuditsPage.HandleAsync(businessId, page, pageSize, query, ct);");
        controllerSource.Should().Contain("var vm = new BusinessOwnerOverrideAuditsListVm");
        controllerSource.Should().Contain("Business = business,");
        controllerSource.Should().Contain("Page = page,");
        controllerSource.Should().Contain("PageSize = pageSize,");
        controllerSource.Should().Contain("Total = total,");
        controllerSource.Should().Contain("Query = query ?? string.Empty,");
        controllerSource.Should().Contain("Playbooks = BuildBusinessOwnerOverrideAuditPlaybooks(businessId),");
        controllerSource.Should().Contain("Items = items.Select(x => new BusinessOwnerOverrideAuditListItemVm");
        controllerSource.Should().Contain("return RenderOwnerOverrideAuditsWorkspace(vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepOwnerOverrideAuditItemMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("Items = items.Select(x => new BusinessOwnerOverrideAuditListItemVm");
        controllerSource.Should().Contain("Id = x.Id,");
        controllerSource.Should().Contain("BusinessId = x.BusinessId,");
        controllerSource.Should().Contain("BusinessMemberId = x.BusinessMemberId,");
        controllerSource.Should().Contain("AffectedUserId = x.AffectedUserId,");
        controllerSource.Should().Contain("AffectedUserDisplayName = x.AffectedUserDisplayName,");
        controllerSource.Should().Contain("AffectedUserEmail = x.AffectedUserEmail,");
        controllerSource.Should().Contain("ActionKind = x.ActionKind,");
        controllerSource.Should().Contain("Reason = x.Reason,");
        controllerSource.Should().Contain("ActorDisplayName = x.ActorDisplayName,");
        controllerSource.Should().Contain("CreatedAtUtc = x.CreatedAtUtc");
    }


    [Fact]
    public void BusinessesController_Should_KeepOwnerOverrideAuditsRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderOwnerOverrideAuditsWorkspace(BusinessOwnerOverrideAuditsListVm vm)");
        controllerSource.Should().Contain("return PartialView(\"OwnerOverrideAudits\", vm);");
        controllerSource.Should().Contain("return View(\"OwnerOverrideAudits\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionCancelAtPeriodEndPostContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SetSubscriptionCancelAtPeriodEnd(");
        controllerSource.Should().Contain("var parsedRowVersion = DecodeBase64RowVersion(rowVersion)");
        controllerSource.Should().Contain("var result = await _setCancelAtPeriodEnd.HandleAsync(");
        controllerSource.Should().Contain("businessId,");
        controllerSource.Should().Contain("subscriptionId,");
        controllerSource.Should().Contain("cancelAtPeriodEnd,");
        controllerSource.Should().Contain("parsedRowVersion,");
        controllerSource.Should().Contain("TempData[result.Succeeded ? \"Success\" : \"Error\"] = result.Succeeded");
        controllerSource.Should().Contain("T(\"BusinessSubscriptionCancelAtPeriodEndUpdated\")");
        controllerSource.Should().Contain("T(\"BusinessSubscriptionRenewalRestored\")");
        controllerSource.Should().Contain("T(\"BusinessSubscriptionCancelAtPeriodEndUpdateFailed\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Subscription), new { businessId });");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessLifecyclePostContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Approve([FromForm] Guid id, [FromForm] string? rowVersion, [FromForm] bool returnToSetup = false, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _approveBusiness.HandleAsync(new BusinessLifecycleActionDto");
        controllerSource.Should().Contain("RowVersion = DecodeBase64RowVersion(rowVersion)");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessApproved\")");
        controllerSource.Should().Contain("public async Task<IActionResult> Suspend([FromForm] Guid id, [FromForm] string? rowVersion, [FromForm] string? note, [FromForm] bool returnToSetup = false, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _suspendBusiness.HandleAsync(new BusinessLifecycleActionDto");
        controllerSource.Should().Contain("Note = note");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessSuspended\")");
        controllerSource.Should().Contain("public async Task<IActionResult> Reactivate([FromForm] Guid id, [FromForm] string? rowVersion, [FromForm] bool returnToSetup = false, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _reactivateBusiness.HandleAsync(new BusinessLifecycleActionDto");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessReactivated\")");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessApproveFailed\");");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessSuspendFailed\");");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessReactivateFailed\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(returnToSetup ? nameof(Setup) : nameof(Edit), new { id });");
    }


    [Fact]
    public void BusinessesController_Should_KeepDeleteActionPostContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] string? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("Result result = await _deleteBusiness.HandleAsync(new BusinessDeleteDto");
        controllerSource.Should().Contain("T(\"BusinessArchived\")");
        controllerSource.Should().Contain("T(\"BusinessArchiveFailed\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        controllerSource.Should().Contain("public async Task<IActionResult> DeleteLocation([FromForm] Guid id, [FromForm(Name = \"userId\")] Guid businessId, [FromForm] string? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("var result = await _deleteBusinessLocation.HandleAsync(new BusinessLocationDeleteDto");
        controllerSource.Should().Contain("T(\"BusinessLocationArchived\")");
        controllerSource.Should().Contain("T(\"BusinessLocationArchiveFailed\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Locations), new { businessId });");
        controllerSource.Should().Contain("public async Task<IActionResult> DeleteMember([FromForm] Guid id, [FromForm(Name = \"userId\")] Guid businessId, [FromForm] string? rowVersion, CancellationToken ct = default)");
        controllerSource.Should().Contain("await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessMemberRemoved\")");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Members), new { businessId });");
    }


    [Fact]
    public void BusinessesController_Should_KeepInvitationActionPostContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> ResendInvitation(");
        controllerSource.Should().Contain("await _resendBusinessInvitation.HandleAsync(new BusinessInvitationResendDto");
        controllerSource.Should().Contain("ExpiresInDays = 7");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessInvitationReissued\")");
        controllerSource.Should().Contain("public async Task<IActionResult> RevokeInvitation(");
        controllerSource.Should().Contain("await _revokeBusinessInvitation.HandleAsync(new BusinessInvitationRevokeDto");
        controllerSource.Should().Contain("Note = T(\"BusinessInvitationRevokedFromWebAdminNote\")");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessInvitationRevoked\")");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessInvitationResendFailed\");");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessInvitationRevokeFailed\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Invitations), new { businessId");
    }


    [Fact]
    public void BusinessesController_Should_KeepSetupMembersPreviewFragmentContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SetupMembersPreview(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SetupMembersPreview.cshtml\", new BusinessSetupMembersPreviewVm");
        controllerSource.Should().Contain("BusinessId = businessId");
        controllerSource.Should().Contain("var (items, _) = await _getBusinessMembersPage.HandleAsync(");
        controllerSource.Should().Contain("filter: BusinessMemberSupportFilter.All,");
        controllerSource.Should().Contain("var attentionMembers = items");
        controllerSource.Should().Contain(".Where(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > nowUtc))");
        controllerSource.Should().Contain(".Take(5)");
        controllerSource.Should().Contain(".Select(x => new BusinessMemberListItemVm");
        controllerSource.Should().Contain("UserDisplayName = x.UserDisplayName,");
        controllerSource.Should().Contain("UserEmail = x.UserEmail,");
        controllerSource.Should().Contain("EmailConfirmed = x.EmailConfirmed,");
        controllerSource.Should().Contain("LockoutEndUtc = x.LockoutEndUtc,");
        controllerSource.Should().Contain("AttentionCount = items.Count(x => !x.EmailConfirmed || (x.LockoutEndUtc.HasValue && x.LockoutEndUtc.Value > nowUtc)),");
        controllerSource.Should().Contain("Items = attentionMembers");
    }


    [Fact]
    public void BusinessesController_Should_KeepSetupInvitationsPreviewFragmentContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SetupInvitationsPreview(Guid businessId, CancellationToken ct = default)");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(businessId, ct);");
        controllerSource.Should().Contain("return PartialView(\"~/Views/Businesses/_SetupInvitationsPreview.cshtml\", new BusinessSetupInvitationsPreviewVm");
        controllerSource.Should().Contain("BusinessId = businessId");
        controllerSource.Should().Contain("var (items, _) = await _getBusinessInvitationsPage.HandleAsync(");
        controllerSource.Should().Contain("filter: BusinessInvitationQueueFilter.All,");
        controllerSource.Should().Contain("var openInvitations = items");
        controllerSource.Should().Contain(".Where(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired)");
        controllerSource.Should().Contain(".Take(5)");
        controllerSource.Should().Contain(".Select(x => new BusinessInvitationListItemVm");
        controllerSource.Should().Contain("Email = x.Email,");
        controllerSource.Should().Contain("Role = x.Role,");
        controllerSource.Should().Contain("Status = x.Status,");
        controllerSource.Should().Contain("InvitedByDisplayName = x.InvitedByDisplayName,");
        controllerSource.Should().Contain("OpenCount = items.Count(x => x.Status == BusinessInvitationStatus.Pending || x.Status == BusinessInvitationStatus.Expired),");
        controllerSource.Should().Contain("Items = openInvitations");
    }


    [Fact]
    public void BusinessesController_Should_KeepSupportSummaryMappingContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static BusinessSupportSummaryVm MapSupportSummaryVm(BusinessSupportSummaryDto summary)");
        controllerSource.Should().Contain("AttentionBusinessCount = summary.AttentionBusinessCount,");
        controllerSource.Should().Contain("PendingApprovalBusinessCount = summary.PendingApprovalBusinessCount,");
        controllerSource.Should().Contain("SuspendedBusinessCount = summary.SuspendedBusinessCount,");
        controllerSource.Should().Contain("ApprovedInactiveBusinessCount = summary.ApprovedInactiveBusinessCount,");
        controllerSource.Should().Contain("MissingOwnerBusinessCount = summary.MissingOwnerBusinessCount,");
        controllerSource.Should().Contain("MissingPrimaryLocationBusinessCount = summary.MissingPrimaryLocationBusinessCount,");
        controllerSource.Should().Contain("MissingContactEmailBusinessCount = summary.MissingContactEmailBusinessCount,");
        controllerSource.Should().Contain("MissingLegalNameBusinessCount = summary.MissingLegalNameBusinessCount,");
        controllerSource.Should().Contain("PendingInvitationCount = summary.PendingInvitationCount,");
        controllerSource.Should().Contain("OpenInvitationCount = summary.OpenInvitationCount,");
        controllerSource.Should().Contain("PendingActivationMemberCount = summary.PendingActivationMemberCount,");
        controllerSource.Should().Contain("LockedMemberCount = summary.LockedMemberCount,");
        controllerSource.Should().Contain("FailedInvitationCount = summary.FailedInvitationCount,");
        controllerSource.Should().Contain("FailedActivationCount = summary.FailedActivationCount,");
        controllerSource.Should().Contain("FailedPasswordResetCount = summary.FailedPasswordResetCount,");
        controllerSource.Should().Contain("FailedAdminTestCount = summary.FailedAdminTestCount");
    }


    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessSubscriptionSnapshotPipelineWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("var subscription = await BuildBusinessSubscriptionSnapshotAsync(business.Id, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("items.Add(new MerchantReadinessItemVm");
        controllerSource.Should().Contain("HasSubscription = subscription.HasSubscription,");
        controllerSource.Should().Contain("SubscriptionStatus = subscription.Status,");
        controllerSource.Should().Contain("SubscriptionPlanName = subscription.PlanName,");
        controllerSource.Should().Contain("CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,");
        controllerSource.Should().Contain("CurrentPeriodEndUtc = subscription.CurrentPeriodEndUtc");
        controllerSource.Should().Contain("private async Task<BusinessSubscriptionSnapshotVm> BuildBusinessSubscriptionSnapshotAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("var result = await _getBusinessSubscriptionStatus.HandleAsync(");
        controllerSource.Should().Contain("Status = T(\"Unavailable\")");
    }


    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessRenderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RenderMerchantReadinessWorkspace(MerchantReadinessWorkspaceVm vm)");
        controllerSource.Should().Contain("return PartialView(\"MerchantReadiness\", vm);");
        controllerSource.Should().Contain("return View(\"MerchantReadiness\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepLocalizedMutationFeedbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        source.Should().Contain("? T(\"BusinessCreateOwnerAssigned\")");
        source.Should().Contain(": T(\"BusinessCreateNextSteps\")");
        source.Should().Contain("? (cancelAtPeriodEnd ? T(\"BusinessSubscriptionCancelAtPeriodEndUpdated\") : T(\"BusinessSubscriptionRenewalRestored\"))");
        source.Should().Contain(": T(\"BusinessSubscriptionCancelAtPeriodEndUpdateFailed\")");
        source.Should().Contain("result.Succeeded ? T(\"BusinessArchived\") : T(\"BusinessArchiveFailed\")");
        source.Should().Contain("result.Succeeded ? T(\"BusinessLocationArchived\") : T(\"BusinessLocationArchiveFailed\")");
        source.Should().Contain("Note = T(\"BusinessInvitationRevokedFromWebAdminNote\")");
    }


    [Fact]
    public void BusinessesWorkspaces_Should_KeepMerchantPlaybookContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var indexViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));
        var supportQueueViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SupportQueue.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildMerchantReadinessPlaybooks(null)");
        controllerSource.Should().Contain("private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks(Guid? businessId)");
        controllerSource.Should().Contain("QueueActionLabel = T(\"PendingApproval\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"NeedsAttention\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonSetup\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"ApprovedInactive\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"Payments\")");

        indexViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        indexViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        indexViewSource.Should().Contain("hx-target=\"#businesses-workspace-shell\"");
        indexViewSource.Should().Contain(">@playbook.Title</a>");
        indexViewSource.Should().Contain(">@playbook.ScopeNote</a>");
        indexViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        indexViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        indexViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        supportQueueViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        supportQueueViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        supportQueueViewSource.Should().Contain("hx-target=\"#business-support-queue-workspace-shell\"");
        supportQueueViewSource.Should().Contain(">@playbook.Title</a>");
        supportQueueViewSource.Should().Contain(">@playbook.ScopeNote</a>");
        supportQueueViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        supportQueueViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        supportQueueViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        indexViewSource.Should().Contain("@BusinessesIndexUrl(operationalStatus: \"Suspended\")");
        indexViewSource.Should().Contain("hx-push-url=\"true\">@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.Suspended)</a>");
        indexViewSource.Should().Contain("@BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        indexViewSource.Should().Contain("asp-route-filter=\"@(item.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        indexViewSource.Should().Contain("? BusinessMembersUrl(item.Id)");
        indexViewSource.Should().Contain(": BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention))");
        indexViewSource.Should().Contain("asp-route-filter=\"@(item.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        indexViewSource.Should().Contain("? BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        indexViewSource.Should().Contain(": BusinessInvitationsUrl(item.Id))");
        var readinessViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));
        readinessViewSource.Should().Contain("@BusinessMembersUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        readinessViewSource.Should().Contain("@BusinessInvitationsUrl(item.Id, Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        readinessViewSource.Should().Contain("asp-route-filter=\"@(item.ActiveOwnerCount > 0 ? null : Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)\"");
        readinessViewSource.Should().Contain("asp-route-filter=\"@(item.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        readinessViewSource.Should().Contain("string SubscriptionPlanDisplay(string? planName, string? status) => !string.IsNullOrWhiteSpace(planName)");
        readinessViewSource.Should().Contain("hx-push-url=\"true\">@SubscriptionPlanDisplay(item.SubscriptionPlanName, item.SubscriptionStatus)</a>");
    }


    [Fact]
    public void BusinessLocationsWorkspace_Should_KeepHeaderSearchRowActionsAndArchiveBridgeWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Locations.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"BusinessLocationsTitle\")");
        source.Should().Contain("id=\"business-locations-workspace-shell\"");
        source.Should().Contain("<h1 class=\"mb-1\"><i class=\"fa-solid fa-location-dot me-2\"></i>@T.T(\"BusinessLocationsTitle\")</h1>");
        source.Should().Contain("@T.T(\"BusinessMembersBackToBusinessAction\")");
        source.Should().Contain("@InvitationWorkspaceLabel()");
        source.Should().Contain("asp-route-filter=\"@(Model.Business.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        source.Should().Contain("hx-get=\"@BusinessInvitationsUrl(Model.Business.Id, Model.Business.InvitationCount > 0 ? Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending : null)\"");
        source.Should().Contain("@T.T(\"BusinessLocationsAddLocationAction\")");
        source.Should().Contain("hx-get=\"@CreateLocationUrl(Model.Business.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)\"");
        source.Should().Contain("string GlobalBusinessLocationsUrl() => Url.Action(\"Locations\", \"Businesses\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalBusinessLocationsUrl()\"");
        source.Should().Contain("name=\"businessId\" value=\"@Model.Business.Id\"");
        source.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\" class=\"form-select\"");
        source.Should().Contain("placeholder=\"@T.T(\"BusinessLocationsSearchPlaceholder\")\"");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("@T.T(\"Name\")");
        source.Should().Contain("@T.T(\"City\")");
        source.Should().Contain("@T.T(\"Region\")");
        source.Should().Contain("@T.T(\"Country\")");
        source.Should().Contain("@T.T(\"Primary\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"BusinessLocationsAddressMissingBadge\")");
        source.Should().Contain("@T.T(\"BusinessLocationsCoordinatesMissingBadge\")");
        source.Should().Contain("hx-get=\"@EditLocationUrl(item.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)\"");
        source.Should().Contain("@T.T(\"CommonEdit\")");
        source.Should().Contain("hx-get=\"@BusinessSetupUrl(Model.Business.Id)\"");
        source.Should().Contain("@T.T(\"Setup\")");
        source.Should().Contain("hx-get=\"@BusinessMerchantReadinessUrl(Model.Business.Id)\"");
        source.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        source.Should().Contain("data-action=\"@Url.Action(\"DeleteLocation\", \"Businesses\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(item.RowVersion)\"");
        source.Should().Contain("data-hx-target=\"#business-locations-workspace-shell\"");
        source.Should().NotContain("data-hx-success=");
        source.Should().Contain("@T.T(\"Archive\")");
        source.Should().Contain("asp-action=\"Locations\"");
        source.Should().Contain("asp-route-businessId=\"@Model.Business.Id\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void BusinessMembersWorkspace_Should_KeepSummaryAndPlaybookOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        controllerSource.Should().Contain("Summary = await BuildBusinessMemberOpsSummaryAsync(businessId, ct).ConfigureAwait(false)");
        controllerSource.Should().Contain("Playbooks = BuildBusinessMemberPlaybooks(businessId)");
        controllerSource.Should().Contain("private async Task<BusinessMemberOpsSummaryVm> BuildBusinessMemberOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessMembersAllLabel\"), BusinessMemberSupportFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"NeedsAttention\"), BusinessMemberSupportFilter.Attention.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"PendingActivation\"), BusinessMemberSupportFilter.PendingActivation.ToString()");
        controllerSource.Should().Contain("new SelectListItem(T(\"BusinessMembersLockedLabel\"), BusinessMemberSupportFilter.Locked.ToString()");
        controllerSource.Should().Contain("BusinessMemberSupportFilter.PendingActivation");
        controllerSource.Should().Contain("BusinessMemberSupportFilter.Locked");
        controllerSource.Should().Contain("BusinessMemberSupportFilter.Attention");
        controllerSource.Should().Contain("private List<BusinessMemberPlaybookVm> BuildBusinessMemberPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueActionLabel = T(\"PendingActivation\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"MobileOperationsTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"UsersFilterLocked\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"UsersFilterLocked\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"NeedsAttention\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"OwnerOverrideAuditTitle\")");

        membersViewSource.Should().Contain("Model.Summary.TotalCount");
        membersViewSource.Should().Contain("Model.Summary.PendingActivationCount");
        membersViewSource.Should().Contain("Model.Summary.LockedCount");
        membersViewSource.Should().Contain("Model.Summary.AttentionCount");
        membersViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        membersViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        membersViewSource.Should().Contain("hx-target=\"#business-members-workspace-shell\"");
        membersViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        membersViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        membersViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        membersViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        membersViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        membersViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        membersViewSource.Should().Contain("BusinessMemberSupportFilter.PendingActivation");
        membersViewSource.Should().Contain("string InvitationWorkspaceLabel() => T.T(\"Invitations\")");
        membersViewSource.Should().Contain("hx-push-url=\"true\">@InvitationWorkspaceLabel()</a>");
        membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Invitations\")</a>");
        membersViewSource.Should().Contain("BusinessMemberSupportFilter.Locked");
        membersViewSource.Should().Contain("BusinessMemberSupportFilter.Attention");
        membersViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        membersViewSource.Should().Contain("string MemberConfirmationStatusLabel(bool emailConfirmed) => emailConfirmed");
        membersViewSource.Should().Contain("string MemberActiveStatusLabel(bool isActive) => isActive ? T.T(\"Yes\") : T.T(\"No\")");
        membersViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        membersViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        membersViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)");
        membersViewSource.Should().Contain("@T.T(\"BusinessMembersNoActiveOwnerWarning\")");
        membersViewSource.Should().Contain("@BusinessOwnerOverrideAuditsUrl(Model.Business.Id)");
          membersViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Business.Id)");
          membersViewSource.Should().Contain("@BusinessSetupUrl(Model.Business.Id)");
          membersViewSource.Should().Contain("@T.T(\"BusinessMembersPendingActivationNote\")");
          membersViewSource.Should().Contain("@BusinessMembersUrl(Model.Business.Id, Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
          membersViewSource.Should().Contain("@EmailAuditsUrl(Model.Business.Id, status: \"Failed\", flowKey: \"AccountActivation\")");
          membersViewSource.Should().Contain("@T.T(\"OpenFailedActivationEmails\")");
          membersViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
          membersViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"UsersFilterUnconfirmed\")</a>");
          membersViewSource.Should().NotContain("<div class=\"text-muted small text-uppercase\">@T.T(\"NeedsAttention\")</div>");
          membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"NeedsAttention\")</a>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-warning\">@T.T(\"PendingActivation\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-danger ms-1\">@T.T(\"Locked\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Confirmed\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Yes\")</span>");
          membersViewSource.Should().NotContain("<span class=\"badge text-bg-secondary\">@T.T(\"No\")</span>");
      }


    [Fact]
    public void BusinessMembersWorkspace_Should_KeepHeaderPermissionGateRowActionsAndMutationRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"BusinessMembersTitle\")");
        source.Should().Contain("@inject Darwin.WebAdmin.Infrastructure.PermissionRazorHelper Perms");
        source.Should().Contain("var isFullAdmin = await Perms.HasAsync(\"FullAdminAccess\")");
        source.Should().Contain("id=\"business-members-workspace-shell\"");
        source.Should().Contain("<h1 class=\"mb-1\"><i class=\"fa-solid fa-user-group me-2\"></i>@T.T(\"BusinessMembersTitle\")</h1>");
        source.Should().Contain("@T.T(\"BusinessMembersBackToBusinessAction\")");
        source.Should().Contain("@T.T(\"BusinessMembersOwnerOverrideAuditAction\")");
        source.Should().Contain("@T.T(\"BusinessMembersAssignMemberAction\")");
        source.Should().Contain("hx-get=\"@CreateMemberUrl(Model.Business.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)\"");
        source.Should().Contain("string GlobalBusinessMembersUrl() => Url.Action(\"Members\", \"Businesses\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalBusinessMembersUrl()\"");
        source.Should().Contain("name=\"businessId\" value=\"@Model.Business.Id\"");
        source.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\" class=\"form-select\"");
        source.Should().Contain("placeholder=\"@T.T(\"BusinessMembersSearchPlaceholder\")\"");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("@T.T(\"User\")");
        source.Should().Contain("@T.T(\"Email\")");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"Role\")");
        source.Should().Contain("@T.T(\"Active\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"BusinessMembersBadgeAction\")");
        source.Should().Contain("hx-get=\"@StaffAccessBadgeUrl(item.Id)\"");
        source.Should().Contain("hx-get=\"@EditMemberUrl(item.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)\"");
        source.Should().Contain("hx-get=\"@UserEditUrl(item.UserId)\"");
        source.Should().Contain("hx-get=\"@MobileOperationsUrl(Model.Business.Id, item.UserEmail)\"");
        source.Should().Contain("hx-get=\"@LoyaltyAccountsUrl(item.UserEmail)\"");
        source.Should().Contain("data-action=\"@Url.Action(\"DeleteMember\", \"Businesses\")\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"SendMemberActivationEmail\", \"Businesses\")\"");
        source.Should().Contain("@T.T(\"BusinessMemberSendActivationAction\")");
        source.Should().Contain("hx-post=\"@Url.Action(\"ConfirmMemberEmail\", \"Businesses\")\"");
        source.Should().Contain("@T.T(\"BusinessMemberConfirmEmailAction\")");
        source.Should().Contain("hx-post=\"@Url.Action(\"SendMemberPasswordReset\", \"Businesses\")\"");
        source.Should().Contain("@T.T(\"BusinessMemberSendResetAction\")");
        source.Should().Contain("hx-post=\"@Url.Action(\"UnlockMemberUser\", \"Businesses\")\"");
        source.Should().Contain("@T.T(\"UsersUnlockAction\")");
        source.Should().Contain("hx-post=\"@Url.Action(\"LockMemberUser\", \"Businesses\")\"");
        source.Should().Contain("@T.T(\"UsersLockAction\")");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("asp-action=\"Members\"");
        source.Should().Contain("asp-route-businessId=\"@Model.Business.Id\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void BusinessInvitationsWorkspace_Should_KeepSummaryAndPlaybookOpsContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var invitationsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Invitations.cshtml"));

        controllerSource.Should().Contain("Summary = await BuildBusinessInvitationOpsSummaryAsync(businessId, ct).ConfigureAwait(false)");
        controllerSource.Should().Contain("Playbooks = BuildBusinessInvitationPlaybooks(businessId)");
        controllerSource.Should().Contain("private async Task<BusinessInvitationOpsSummaryVm> BuildBusinessInvitationOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("private string DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter filter)");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.All => T(\"BusinessInvitationsAllLabel\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Open => T(\"OpenInvitations\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Pending => T(\"Pending\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Expired => T(\"Expired\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Accepted => T(\"Accepted\")");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Revoked => T(\"Revoked\")");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.All), BusinessInvitationQueueFilter.All.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Open), BusinessInvitationQueueFilter.Open.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Pending), BusinessInvitationQueueFilter.Pending.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Expired), BusinessInvitationQueueFilter.Expired.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Accepted), BusinessInvitationQueueFilter.Accepted.ToString()");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Revoked), BusinessInvitationQueueFilter.Revoked.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"BusinessInvitationsAllLabel\"), BusinessInvitationQueueFilter.All.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"OpenInvitations\"), BusinessInvitationQueueFilter.Open.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Pending\"), BusinessInvitationQueueFilter.Pending.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Expired\"), BusinessInvitationQueueFilter.Expired.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Accepted\"), BusinessInvitationQueueFilter.Accepted.ToString()");
        controllerSource.Should().NotContain("new SelectListItem(T(\"Revoked\"), BusinessInvitationQueueFilter.Revoked.ToString()");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Open");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Pending");
        controllerSource.Should().Contain("BusinessInvitationQueueFilter.Expired");
        controllerSource.Should().Contain("private List<BusinessInvitationPlaybookVm> BuildBusinessInvitationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueActionLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessInvitationsPlaybookOpenWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessInvitationsPlaybookOpenAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"FailedInvitations\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"Pending\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessInvitationsPlaybookPendingWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessInvitationsPlaybookPendingAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("QueueActionLabel = T(\"Expired\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessInvitationsPlaybookExpiredWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessInvitationsPlaybookExpiredAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessInvitationsInviteUserAction\")");

        invitationsViewSource.Should().Contain("Model.Summary.TotalCount");
        invitationsViewSource.Should().Contain("Model.Summary.OpenCount");
        invitationsViewSource.Should().Contain("Model.Summary.PendingCount");
        invitationsViewSource.Should().Contain("Model.Summary.ExpiredCount");
        invitationsViewSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Open => T.T(\"OpenInvitations\")");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Pending => T.T(\"Pending\")");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Expired => T.T(\"Expired\")");
        invitationsViewSource.Should().Contain("string InvitationStatusLabel(Darwin.Domain.Enums.BusinessInvitationStatus status) => status switch");
        invitationsViewSource.Should().Contain("BusinessInvitationStatus.Accepted => T.T(\"Accepted\")");
        invitationsViewSource.Should().Contain("BusinessInvitationStatus.Revoked => T.T(\"Revoked\")");
        invitationsViewSource.Should().Contain("string MemberWorkspaceLabel() => T.T(\"Members\")");
        invitationsViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        invitationsViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        invitationsViewSource.Should().Contain("hx-target=\"#business-invitations-workspace-shell\"");
        invitationsViewSource.Should().Contain("hx-push-url=\"true\">@MemberWorkspaceLabel()</a>");
        invitationsViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        invitationsViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        invitationsViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        invitationsViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        invitationsViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        invitationsViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Open");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Pending");
        invitationsViewSource.Should().Contain("BusinessInvitationQueueFilter.Expired");
        invitationsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        invitationsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        invitationsViewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        invitationsViewSource.Should().Contain("@InvitationStatusLabel(item.Status)");
        invitationsViewSource.Should().NotContain("@T.T(\"Pending\")</a>");
        invitationsViewSource.Should().NotContain("@T.T(\"Expired\")</a>");
        invitationsViewSource.Should().NotContain("@T.T(\"Pending\")</span>");
        invitationsViewSource.Should().NotContain("@T.T(\"Accepted\")</span>");
        invitationsViewSource.Should().NotContain("@T.T(\"Revoked\")</span>");
        invitationsViewSource.Should().NotContain("@T.T(\"Expired\")</span>");
          invitationsViewSource.Should().Contain("@T.T(\"BusinessInvitationsEmptyState\")");
          invitationsViewSource.Should().Contain("@CreateInvitationUrl(Model.Business.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)");
          invitationsViewSource.Should().Contain("@BusinessSetupUrl(Model.Business.Id)");
          invitationsViewSource.Should().Contain("@BusinessSupportQueueUrl(Model.Business.Id)");
          invitationsViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Business.Id)");
          invitationsViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
          invitationsViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
      }


    [Fact]
    public void BusinessInvitationsWorkspace_Should_KeepHeaderPermissionGateRowActionsAndMutationRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Invitations.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"BusinessInvitationsTitle\")");
        source.Should().Contain("@inject Darwin.WebAdmin.Infrastructure.PermissionRazorHelper Perms");
        source.Should().Contain("var isFullAdmin = await Perms.HasAsync(\"FullAdminAccess\")");
        source.Should().Contain("id=\"business-invitations-workspace-shell\"");
        source.Should().Contain("<h1 class=\"mb-1\"><i class=\"fa-solid fa-envelope me-2\"></i>@T.T(\"BusinessInvitationsTitle\")</h1>");
        source.Should().Contain("@MemberWorkspaceLabel()");
        source.Should().Contain("@if (isFullAdmin)");
        source.Should().Contain("@T.T(\"BusinessMembersBackToBusinessAction\")");
        source.Should().Contain("@T.T(\"Setup\")");
        source.Should().Contain("@T.T(\"BusinessInvitationsInviteUserAction\")");
        source.Should().Contain("hx-get=\"@CreateInvitationUrl(Model.Business.Id, Model.Page, Model.PageSize, Model.Query, Model.Filter)\"");
        source.Should().Contain("string GlobalBusinessInvitationsUrl() => Url.Action(\"Invitations\", \"Businesses\") ?? string.Empty;");
        source.Should().Contain("hx-get=\"@GlobalBusinessInvitationsUrl()\"");
        source.Should().Contain("name=\"businessId\" value=\"@Model.Business.Id\"");
        source.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\" class=\"form-select\"");
        source.Should().Contain("placeholder=\"@T.T(\"BusinessInvitationsSearchPlaceholder\")\"");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("@T.T(\"Email\")");
        source.Should().Contain("@T.T(\"Role\")");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"BusinessInvitationsInvitedByColumn\")");
        source.Should().Contain("@T.T(\"BusinessInvitationsExpiresUtcColumn\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("hx-post=\"@Url.Action(\"ResendInvitation\", \"Businesses\")\"");
        source.Should().Contain("@T.T(\"Resend\")");
        source.Should().Contain("hx-post=\"@Url.Action(\"RevokeInvitation\", \"Businesses\")\"");
        source.Should().Contain("@T.T(\"Revoke\")");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("asp-action=\"Invitations\"");
        source.Should().Contain("asp-route-businessId=\"@Model.Business.Id\"");
        source.Should().Contain("hx-get=\"@BusinessSupportQueueUrl(Model.Business.Id)\"");
        source.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        source.Should().Contain("hx-get=\"@BusinessMerchantReadinessUrl(Model.Business.Id)\"");
        source.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessOwnerOverrideAuditsWorkspace_Should_KeepPlaybookAndDrillInContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var auditsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "OwnerOverrideAudits.cshtml"));

        controllerSource.Should().Contain("Playbooks = BuildBusinessOwnerOverrideAuditPlaybooks(businessId)");
        controllerSource.Should().Contain("private List<BusinessOwnerOverrideAuditPlaybookVm> BuildBusinessOwnerOverrideAuditPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideForceRemove\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessOwnerOverridePlaybookForceRemoveWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessOwnerOverridePlaybookForceRemoveAction\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty");
        controllerSource.Should().Contain("FollowUpLabel = T(\"BusinessSupportQueueTitle\")");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideDemoteDeactivate\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessOwnerOverridePlaybookDemoteWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessOwnerOverridePlaybookDemoteAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"MerchantReadinessTitle\")");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingActiveOwner\")");
        controllerSource.Should().Contain("WhyItMatters = T(\"BusinessOwnerOverridePlaybookMissingOwnerWhyItMatters\")");
        controllerSource.Should().Contain("OperatorAction = T(\"BusinessOwnerOverridePlaybookMissingOwnerAction\")");
        controllerSource.Should().Contain("FollowUpLabel = T(\"CommonSetup\")");

        auditsViewSource.Should().Contain("BusinessesOperationsPlaybooksTitle");
        auditsViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        auditsViewSource.Should().Contain("hx-target=\"#business-owner-override-audits-workspace-shell\"");
        auditsViewSource.Should().Contain("href=\"@playbook.QueueActionUrl\"");
        auditsViewSource.Should().Contain(">@playbook.WhyItMatters</a>");
        auditsViewSource.Should().Contain(">@playbook.QueueLabel</a>");
        auditsViewSource.Should().Contain(">@playbook.OperatorAction</a>");
        auditsViewSource.Should().Contain(">@playbook.QueueActionLabel</a>");
        auditsViewSource.Should().Contain(">@playbook.FollowUpLabel</a>");
        auditsViewSource.Should().Contain("OpenUserAction");
          auditsViewSource.Should().Contain("CommonMembers");
          auditsViewSource.Should().Contain("@BusinessMembersAttentionUrl(Model.Business.Id)");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideAuditsIntro\")");
        auditsViewSource.Should().Contain("@BusinessSupportQueueUrl(Model.Business.Id)");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideAuditsEmptyState\")");
        auditsViewSource.Should().Contain("@BusinessSetupUrl(Model.Business.Id)");
        auditsViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Business.Id)");
        auditsViewSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        auditsViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessMembersWorkspace_Should_KeepRowStatusBadgesHelperBacked()
    {
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        membersViewSource.Should().Contain("<span class=\"badge text-bg-warning\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-danger ms-1\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberConfirmationStatusLabel(item.EmailConfirmed)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-secondary\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-warning\">@T.T(\"PendingActivation\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-danger ms-1\">@T.T(\"Locked\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Confirmed\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Yes\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-secondary\">@T.T(\"No\")</span>");
    }


    [Fact]
    public void BusinessMembersWorkspace_Should_KeepAttentionSummaryCardHelperBacked()
    {
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        membersViewSource.Should().Contain("<div class=\"text-muted small text-uppercase\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</div>");
        membersViewSource.Should().Contain("hx-push-url=\"true\">@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention)</a>");
        membersViewSource.Should().NotContain("<div class=\"text-muted small text-uppercase\">@T.T(\"NeedsAttention\")</div>");
        membersViewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"NeedsAttention\")</a>");
    }


    [Fact]
    public void BusinessMembersWorkspace_Should_KeepConfirmedAndActiveBadgesHelperBacked()
    {
        var membersViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));

        membersViewSource.Should().Contain("string MemberConfirmationStatusLabel(bool emailConfirmed) => emailConfirmed");
        membersViewSource.Should().Contain("string MemberActiveStatusLabel(bool isActive) => isActive ? T.T(\"Yes\") : T.T(\"No\")");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberConfirmationStatusLabel(item.EmailConfirmed)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-success\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().Contain("<span class=\"badge text-bg-secondary\">@MemberActiveStatusLabel(item.IsActive)</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Confirmed\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-success\">@T.T(\"Yes\")</span>");
        membersViewSource.Should().NotContain("<span class=\"badge text-bg-secondary\">@T.T(\"No\")</span>");
    }


    [Fact]
    public void BusinessInvitationsWorkspace_Should_KeepRowStatusBadgesHelperBacked()
    {
        var invitationsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Invitations.cshtml"));

        invitationsViewSource.Should().Contain("<span class=\"badge text-bg-primary\">@InvitationStatusLabel(item.Status)</span>");
        invitationsViewSource.Should().Contain("<span class=\"badge text-bg-warning\">@InvitationStatusLabel(item.Status)</span>");
        invitationsViewSource.Should().NotContain("<span class=\"badge text-bg-primary\">@T.T(\"Pending\")</span>");
        invitationsViewSource.Should().NotContain("<span class=\"badge text-bg-warning\">@T.T(\"Expired\")</span>");
    }


    [Fact]
    public void BusinessesController_Should_KeepStaffAccessBadgeWorkspaceContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> StaffAccessBadge(Guid id, CancellationToken ct = default)");
        controllerSource.Should().Contain("var dto = await _getBusinessMemberForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessMemberNotFound\");");
        controllerSource.Should().Contain("var business = await LoadBusinessContextAsync(dto.BusinessId, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessNotFound\");");
        controllerSource.Should().Contain("var issuedAtUtc = _clock.UtcNow;");
        controllerSource.Should().Contain("var expiresAtUtc = issuedAtUtc.AddMinutes(2);");
        controllerSource.Should().Contain("var payload = BuildStaffAccessBadgePayload(dto, business, issuedAtUtc, expiresAtUtc);");
        controllerSource.Should().Contain("var vm = new BusinessStaffAccessBadgeVm");
        controllerSource.Should().Contain("BadgePayload = payload,");
        controllerSource.Should().Contain("BadgeImageDataUrl = BuildQrCodeDataUrl(payload)");
        controllerSource.Should().Contain("return RenderStaffAccessBadgeWorkspace(vm);");
        controllerSource.Should().Contain("private IActionResult RenderStaffAccessBadgeWorkspace(BusinessStaffAccessBadgeVm vm)");
        controllerSource.Should().Contain("return PartialView(\"StaffAccessBadge\", vm);");
        controllerSource.Should().Contain("return View(\"StaffAccessBadge\", vm);");
    }


    [Fact]
    public void BusinessesController_Should_KeepMemberSupportActionLaneWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> SendMemberActivationEmail(");
        controllerSource.Should().Contain("var result = await _requestEmailConfirmation.HandleAsync(");
        controllerSource.Should().Contain("? T(\"BusinessMemberActivationEmailSent\")");
        controllerSource.Should().Contain(": T(\"BusinessMemberActivationEmailFailed\");");

        controllerSource.Should().Contain("public async Task<IActionResult> ConfirmMemberEmail(");
        controllerSource.Should().Contain("var result = await _confirmUserEmail.HandleAsync(new UserAdminActionDto");
        controllerSource.Should().Contain("? T(\"BusinessMemberEmailConfirmed\")");
        controllerSource.Should().Contain(": T(\"BusinessMemberEmailConfirmFailed\");");

        controllerSource.Should().Contain("public async Task<IActionResult> SendMemberPasswordReset(");
        controllerSource.Should().Contain("var result = await _requestPasswordReset.HandleAsync(");
        controllerSource.Should().Contain("? T(\"BusinessMemberPasswordResetSent\")");
        controllerSource.Should().Contain(": T(\"BusinessMemberPasswordResetFailed\");");

        controllerSource.Should().Contain("public async Task<IActionResult> LockMemberUser(");
        controllerSource.Should().Contain("var result = await _lockUser.HandleAsync(new UserAdminActionDto");
        controllerSource.Should().Contain("? T(\"BusinessMemberAccountLocked\")");
        controllerSource.Should().Contain(": T(\"BusinessMemberAccountLockFailed\");");

        controllerSource.Should().Contain("public async Task<IActionResult> UnlockMemberUser(");
        controllerSource.Should().Contain("var result = await _unlockUser.HandleAsync(new UserAdminActionDto");
        controllerSource.Should().Contain("? T(\"BusinessMemberAccountUnlocked\")");
        controllerSource.Should().Contain(": T(\"BusinessMemberAccountUnlockFailed\");");

        controllerSource.Should().Contain("var member = await _getBusinessMemberForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("SetErrorMessage(\"BusinessMemberNotFound\");");
        controllerSource.Should().Contain("TempData[result.Succeeded ? \"Success\" : \"Error\"] = result.Succeeded");
        controllerSource.Should().Contain("return RedirectMemberSupport(returnToEdit, id, businessId, page, pageSize, query, filter);");
        controllerSource.Should().Contain("private IActionResult RedirectMemberSupport(bool returnToEdit, Guid membershipId, Guid businessId, int page = 1, int pageSize = 20, string? query = null, BusinessMemberSupportFilter filter = BusinessMemberSupportFilter.All)");
        controllerSource.Should().Contain("? RedirectOrHtmx(nameof(EditMember), new { id = membershipId, page, pageSize, query, filter })");
        controllerSource.Should().Contain(": RedirectOrHtmx(nameof(Members), new { businessId, page, pageSize, query, filter });");
    }


    [Fact]
    public void BusinessesController_Should_KeepForceDeleteMemberOverrideActionWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("public async Task<IActionResult> ForceDeleteMember(");
        controllerSource.Should().Contain("await _deleteBusinessMember.HandleAsync(new BusinessMemberDeleteDto");
        controllerSource.Should().Contain("AllowLastOwnerOverride = true,");
        controllerSource.Should().Contain("OverrideReason = overrideReason,");
        controllerSource.Should().Contain("OverrideActorDisplayName = GetCurrentActorDisplayName()");
        controllerSource.Should().Contain("RowVersion = DecodeBase64RowVersion(rowVersion),");
        controllerSource.Should().Contain("SetSuccessMessage(\"BusinessMemberRemovedOverride\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(Members), new { businessId });");
        controllerSource.Should().Contain("AddModelErrorMessage(\"BusinessMemberForceDeleteFailed\");");
        controllerSource.Should().Contain("return RedirectOrHtmx(nameof(EditMember), new { id });");
    }


    [Fact]
    public void BusinessesController_Should_KeepStaffAccessBadgePayloadAndQrContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static string BuildStaffAccessBadgePayload(BusinessMemberDetailDto member, BusinessContextVm business, DateTime issuedAtUtc, DateTime expiresAtUtc)");
        controllerSource.Should().Contain("Type = \"staff-access-badge\",");
        controllerSource.Should().Contain("Version = 1,");
        controllerSource.Should().Contain("BusinessId = business.Id,");
        controllerSource.Should().Contain("BusinessName = business.Name,");
        controllerSource.Should().Contain("OperatorEmail = member.UserEmail,");
        controllerSource.Should().Contain("Role = member.Role.ToString(),");
        controllerSource.Should().Contain("IssuedAtUtc = issuedAtUtc,");
        controllerSource.Should().Contain("ExpiresAtUtc = expiresAtUtc,");
        controllerSource.Should().Contain("Nonce = Guid.NewGuid().ToString(\"N\")");
        controllerSource.Should().Contain("return JsonSerializer.Serialize(payload);");

        controllerSource.Should().Contain("private static string BuildQrCodeDataUrl(string payload)");
        controllerSource.Should().Contain("using var generator = new QRCodeGenerator();");
        controllerSource.Should().Contain("using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);");
        controllerSource.Should().Contain("var png = new PngByteQRCode(data);");
        controllerSource.Should().Contain("var bytes = png.GetGraphic(20);");
        controllerSource.Should().Contain("return $\"data:image/png;base64,{Convert.ToBase64String(bytes)}\";");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessMemberSummaryAndPlaybookHelperContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessMemberOpsSummaryVm> BuildBusinessMemberOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("var (_, totalCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.All, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, pendingActivationCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.PendingActivation, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, lockedCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.Locked, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, attentionCount) = await _getBusinessMembersPage.HandleAsync(businessId, 1, 1, null, BusinessMemberSupportFilter.Attention, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("TotalCount = totalCount,");
        controllerSource.Should().Contain("PendingActivationCount = pendingActivationCount,");
        controllerSource.Should().Contain("LockedCount = lockedCount,");
        controllerSource.Should().Contain("AttentionCount = attentionCount");

        controllerSource.Should().Contain("private List<BusinessMemberPlaybookVm> BuildBusinessMemberPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"PendingActivation\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.PendingActivation }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Index\", \"MobileOperations\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"Locked\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Locked }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingActiveOwner\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId }) ?? string.Empty");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessInvitationSummaryAndPlaybookHelperContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessInvitationOpsSummaryVm> BuildBusinessInvitationOpsSummaryAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("var (_, totalCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.All, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, openCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Open, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, pendingCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Pending, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("var (_, expiredCount) = await _getBusinessInvitationsPage.HandleAsync(businessId, 1, 1, null, BusinessInvitationQueueFilter.Expired, ct).ConfigureAwait(false);");
        controllerSource.Should().Contain("TotalCount = totalCount,");
        controllerSource.Should().Contain("OpenCount = openCount,");
        controllerSource.Should().Contain("PendingCount = pendingCount,");
        controllerSource.Should().Contain("ExpiredCount = expiredCount");

        controllerSource.Should().Contain("private List<BusinessInvitationPlaybookVm> BuildBusinessInvitationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"OpenInvitations\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Invitations\", \"Businesses\", new { businessId, filter = BusinessInvitationQueueFilter.Open }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { flowKey = \"BusinessInvitation\", status = \"Failed\" }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"Pending\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Invitations\", \"Businesses\", new { businessId, filter = BusinessInvitationQueueFilter.Pending }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"Expired\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Invitations\", \"Businesses\", new { businessId, filter = BusinessInvitationQueueFilter.Expired }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"CreateInvitation\", \"Businesses\", new { businessId }) ?? string.Empty");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessLocationAndOwnerOverridePlaybookHelperContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private List<BusinessLocationPlaybookVm> BuildBusinessLocationPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsPrimaryLocationLabel\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.Primary }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingAddress\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingAddress }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessLocationsMissingCoordinatesLabel\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Locations\", \"Businesses\", new { businessId, filter = BusinessLocationQueueFilter.MissingCoordinates }) ?? string.Empty");

        controllerSource.Should().Contain("private List<BusinessOwnerOverrideAuditPlaybookVm> BuildBusinessOwnerOverrideAuditPlaybooks(Guid businessId)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideForceRemove\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId, filter = BusinessMemberSupportFilter.Attention }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessOwnerOverrideDemoteDeactivate\")");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"MerchantReadiness\", \"Businesses\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"MissingActiveOwner\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Members\", \"Businesses\", new { businessId }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Setup\", \"Businesses\", new { id = businessId }) ?? string.Empty");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessFormOptionsPopulationContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task PopulateBusinessFormOptionsAsync(BusinessEditVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct);");
        controllerSource.Should().Contain("vm.DefaultCurrency = string.IsNullOrWhiteSpace(vm.DefaultCurrency) ? (effectiveSettings?.DefaultCurrency ?? settings.DefaultCurrency) : vm.DefaultCurrency;");
        controllerSource.Should().Contain("vm.DefaultCulture = string.IsNullOrWhiteSpace(vm.DefaultCulture) ? (effectiveSettings?.DefaultCulture ?? settings.DefaultCulture) : vm.DefaultCulture;");
        controllerSource.Should().Contain("vm.DefaultTimeZoneId = string.IsNullOrWhiteSpace(vm.DefaultTimeZoneId) ? (effectiveSettings?.DefaultTimeZoneId ?? settings.TimeZone ?? string.Empty) : vm.DefaultTimeZoneId;");
        controllerSource.Should().Contain("vm.ContactEmail = string.IsNullOrWhiteSpace(vm.ContactEmail) ? settings.ContactEmail : vm.ContactEmail;");
        controllerSource.Should().Contain("vm.BrandDisplayName = string.IsNullOrWhiteSpace(vm.BrandDisplayName) ? (effectiveSettings?.BrandDisplayName ?? settings.Title) : vm.BrandDisplayName;");
        controllerSource.Should().Contain("vm.BrandLogoUrl = string.IsNullOrWhiteSpace(vm.BrandLogoUrl) ? (effectiveSettings?.BrandLogoUrl ?? settings.LogoUrl) : vm.BrandLogoUrl;");
        controllerSource.Should().Contain("vm.SupportEmail = string.IsNullOrWhiteSpace(vm.SupportEmail)");
        controllerSource.Should().Contain("? (!string.IsNullOrWhiteSpace(settings.ContactEmail) ? settings.ContactEmail : settings.SmtpFromAddress)");
        controllerSource.Should().Contain("vm.CommunicationSenderName = string.IsNullOrWhiteSpace(vm.CommunicationSenderName)");
        controllerSource.Should().Contain("? (!string.IsNullOrWhiteSpace(settings.SmtpFromDisplayName) ? settings.SmtpFromDisplayName : settings.Title)");
        controllerSource.Should().Contain("vm.CommunicationReplyToEmail = string.IsNullOrWhiteSpace(vm.CommunicationReplyToEmail)");
        controllerSource.Should().Contain("vm.CategoryOptions = Enum.GetValues<BusinessCategoryKind>()");
        controllerSource.Should().Contain(".Select(x => new SelectListItem(T(x.ToString()), x.ToString(), vm.Category == x))");
        controllerSource.Should().Contain("vm.OwnerUserOptions = await _referenceData.GetUserOptionsAsync(vm.OwnerUserId, includeEmpty: true, ct);");
        controllerSource.Should().Contain("vm.CommunicationReadiness = await BuildBusinessCommunicationReadinessAsync(vm.Id, ct);");
        controllerSource.Should().Contain("vm.Subscription = await BuildBusinessSubscriptionSnapshotAsync(vm.Id, ct);");
    }


    [Fact]
    public void BusinessesController_Should_KeepMemberFormOptionsPopulationContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task PopulateMemberFormOptionsAsync(BusinessMemberEditVm vm, bool includeUserSelection, CancellationToken ct)");
        controllerSource.Should().Contain("vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()");
        controllerSource.Should().Contain(".Select(x => new SelectListItem(T(x.ToString()), x.ToString(), vm.Role == x))");
        controllerSource.Should().Contain("if (includeUserSelection)");
        controllerSource.Should().Contain("vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct);");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessContextPopulationHelpersWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task PopulateBusinessContextAsync(BusinessLocationEditVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("private async Task PopulateBusinessContextAsync(BusinessMemberEditVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("private async Task PopulateBusinessContextAsync(BusinessInvitationCreateVm vm, CancellationToken ct)");
        controllerSource.Should().Contain("vm.Business = await LoadBusinessContextAsync(vm.BusinessId, ct) ?? new BusinessContextVm { Id = vm.BusinessId };");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessCommunicationReadinessHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessCommunicationReadinessVm> BuildBusinessCommunicationReadinessAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("var failedEmailSummary = await _getEmailDispatchAuditsPage");
        controllerSource.Should().Contain(".GetSummaryAsync(businessId, ct)");
        controllerSource.Should().Contain("FailedInvitationCount = failedEmailSummary.FailedInvitationCount,");
        controllerSource.Should().Contain("FailedActivationCount = failedEmailSummary.FailedActivationCount,");
        controllerSource.Should().Contain("FailedPasswordResetCount = failedEmailSummary.FailedPasswordResetCount,");
        controllerSource.Should().Contain("FailedAdminTestCount = failedEmailSummary.FailedAdminTestCount");
        controllerSource.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct);");
        controllerSource.Should().Contain("var emailConfigured = settings.SmtpEnabled &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmtpHost) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmtpFromAddress);");
        controllerSource.Should().Contain("var smsConfigured = settings.SmsEnabled &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.SmsProvider);");
        controllerSource.Should().Contain("var whatsAppConfigured = settings.WhatsAppEnabled &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.WhatsAppBusinessPhoneId) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.WhatsAppAccessToken) &&");
        controllerSource.Should().Contain("!string.IsNullOrWhiteSpace(settings.WhatsAppFromPhoneE164);");
        controllerSource.Should().Contain("var adminEmailRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertEmailsCsv);");
        controllerSource.Should().Contain("var adminSmsRoutingConfigured = !string.IsNullOrWhiteSpace(settings.AdminAlertSmsRecipientsCsv);");
        controllerSource.Should().Contain("EmailTransportEnabled = settings.SmtpEnabled,");
        controllerSource.Should().Contain("SmsTransportEnabled = settings.SmsEnabled,");
        controllerSource.Should().Contain("WhatsAppTransportEnabled = settings.WhatsAppEnabled,");
        controllerSource.Should().Contain("AdminAlertEmailsConfigured = adminEmailRoutingConfigured,");
        controllerSource.Should().Contain("AdminAlertSmsConfigured = adminSmsRoutingConfigured,");
        controllerSource.Should().Contain("EmailTransportSummary = emailConfigured");
        controllerSource.Should().Contain("SmsTransportSummary = smsConfigured");
        controllerSource.Should().Contain("WhatsAppTransportSummary = whatsAppConfigured");
        controllerSource.Should().Contain("AdminRoutingSummary = adminEmailRoutingConfigured || adminSmsRoutingConfigured");
    }


    [Fact]
    public void BusinessesController_Should_KeepInvitationFormOptionsPopulationContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private void PopulateInvitationFormOptions(BusinessInvitationCreateVm vm)");
        controllerSource.Should().Contain("vm.RoleOptions = Enum.GetValues<BusinessMemberRole>()");
        controllerSource.Should().Contain(".Select(x => new SelectListItem(T(x.ToString()), x.ToString(), vm.Role == x))");
        controllerSource.Should().Contain(".ToList();");
    }


    [Fact]
    public void BusinessesController_Should_KeepCurrentActorDisplayNameHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private string? GetCurrentActorDisplayName()");
        controllerSource.Should().Contain("var explicitName = User.FindFirstValue(ClaimTypes.Name)");
        controllerSource.Should().Contain("?? User.FindFirstValue(ClaimTypes.Email)");
        controllerSource.Should().Contain("?? User.Identity?.Name;");
        controllerSource.Should().Contain("return string.IsNullOrWhiteSpace(explicitName) ? null : explicitName.Trim();");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessContextLoaderHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessContextVm?> LoadBusinessContextAsync(Guid id, CancellationToken ct)");
        controllerSource.Should().Contain("var dto = await _getBusinessForEdit.HandleAsync(id, ct);");
        controllerSource.Should().Contain("if (dto is null)");
        controllerSource.Should().Contain("return null;");
        controllerSource.Should().Contain("return new BusinessContextVm");
        controllerSource.Should().Contain("Id = dto.Id,");
        controllerSource.Should().Contain("Name = dto.Name,");
        controllerSource.Should().Contain("LegalName = dto.LegalName,");
        controllerSource.Should().Contain("Category = dto.Category,");
        controllerSource.Should().Contain("IsActive = dto.IsActive,");
        controllerSource.Should().Contain("OperationalStatus = dto.OperationalStatus,");
        controllerSource.Should().Contain("ApprovedAtUtc = dto.ApprovedAtUtc,");
        controllerSource.Should().Contain("SuspendedAtUtc = dto.SuspendedAtUtc,");
        controllerSource.Should().Contain("SuspensionReason = dto.SuspensionReason,");
        controllerSource.Should().Contain("MemberCount = dto.MemberCount,");
        controllerSource.Should().Contain("ActiveOwnerCount = dto.ActiveOwnerCount,");
        controllerSource.Should().Contain("LocationCount = dto.LocationCount,");
        controllerSource.Should().Contain("PrimaryLocationCount = dto.PrimaryLocationCount,");
        controllerSource.Should().Contain("InvitationCount = dto.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = dto.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = dto.HasLegalNameConfigured");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessEditMappingHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static BusinessEditVm MapBusinessEditVm(BusinessEditDto dto)");
        controllerSource.Should().Contain("return new BusinessEditVm");
        controllerSource.Should().Contain("RowVersion = dto.RowVersion,");
        controllerSource.Should().Contain("Name = dto.Name,");
        controllerSource.Should().Contain("LegalName = dto.LegalName,");
        controllerSource.Should().Contain("TaxId = dto.TaxId,");
        controllerSource.Should().Contain("ShortDescription = dto.ShortDescription,");
        controllerSource.Should().Contain("WebsiteUrl = dto.WebsiteUrl,");
        controllerSource.Should().Contain("ContactEmail = dto.ContactEmail,");
        controllerSource.Should().Contain("ContactPhoneE164 = dto.ContactPhoneE164,");
        controllerSource.Should().Contain("DefaultCurrency = dto.DefaultCurrency,");
        controllerSource.Should().Contain("DefaultCulture = dto.DefaultCulture,");
        controllerSource.Should().Contain("DefaultTimeZoneId = dto.DefaultTimeZoneId,");
        controllerSource.Should().Contain("AdminTextOverridesJson = dto.AdminTextOverridesJson,");
        controllerSource.Should().Contain("BrandDisplayName = dto.BrandDisplayName,");
        controllerSource.Should().Contain("BrandLogoUrl = dto.BrandLogoUrl,");
        controllerSource.Should().Contain("BrandPrimaryColorHex = dto.BrandPrimaryColorHex,");
        controllerSource.Should().Contain("BrandSecondaryColorHex = dto.BrandSecondaryColorHex,");
        controllerSource.Should().Contain("SupportEmail = dto.SupportEmail,");
        controllerSource.Should().Contain("CommunicationSenderName = dto.CommunicationSenderName,");
        controllerSource.Should().Contain("CommunicationReplyToEmail = dto.CommunicationReplyToEmail,");
        controllerSource.Should().Contain("CustomerEmailNotificationsEnabled = dto.CustomerEmailNotificationsEnabled,");
        controllerSource.Should().Contain("CustomerMarketingEmailsEnabled = dto.CustomerMarketingEmailsEnabled,");
        controllerSource.Should().Contain("OperationalAlertEmailsEnabled = dto.OperationalAlertEmailsEnabled,");
        controllerSource.Should().Contain("OperationalStatus = dto.OperationalStatus,");
        controllerSource.Should().Contain("MemberCount = dto.MemberCount,");
        controllerSource.Should().Contain("InvitationCount = dto.InvitationCount,");
        controllerSource.Should().Contain("HasContactEmailConfigured = dto.HasContactEmailConfigured,");
        controllerSource.Should().Contain("HasLegalNameConfigured = dto.HasLegalNameConfigured");
    }


    [Fact]
    public void BusinessesController_Should_KeepRedirectAndHtmxHelpersWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        controllerSource.Should().Contain("if (IsHtmxRequest())");
        controllerSource.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        controllerSource.Should().Contain("return new EmptyResult();");
        controllerSource.Should().Contain("return RedirectToAction(actionName, routeValues);");
        controllerSource.Should().Contain("private bool IsHtmxRequest()");
        controllerSource.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
    }


    [Fact]
    public void BusinessesController_Should_KeepLocationCoordinateBuilderHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static GeoCoordinateDto? BuildCoordinate(BusinessLocationEditVm vm)");
        controllerSource.Should().Contain("if (!vm.Latitude.HasValue || !vm.Longitude.HasValue)");
        controllerSource.Should().Contain("return null;");
        controllerSource.Should().Contain("return new GeoCoordinateDto");
        controllerSource.Should().Contain("Latitude = vm.Latitude.Value,");
        controllerSource.Should().Contain("Longitude = vm.Longitude.Value,");
        controllerSource.Should().Contain("AltitudeMeters = vm.AltitudeMeters");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionManagementWebsiteUrlHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static string? BuildSubscriptionManagementWebsiteUrl(string? baseUrl, Guid businessId, string? planCode)");
        controllerSource.Should().Contain("if (string.IsNullOrWhiteSpace(baseUrl))");
        controllerSource.Should().Contain("return null;");
        controllerSource.Should().Contain("var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? \"&\" : \"?\";");
        controllerSource.Should().Contain("var url = $\"{baseUrl}{separator}businessId={WebUtility.UrlEncode(businessId.ToString())}\";");
        controllerSource.Should().Contain("if (!string.IsNullOrWhiteSpace(planCode))");
        controllerSource.Should().Contain("url = $\"{url}&planCode={WebUtility.UrlEncode(planCode)}\";");
        controllerSource.Should().Contain("return url;");
    }


    [Fact]
    public void BusinessesController_Should_KeepPageSizeAndBusinessStatusItemBuilderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)");
        controllerSource.Should().Contain("var sizes = new[] { 10, 20, 50, 100 };");
        controllerSource.Should().Contain("return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selectedPageSize)).ToList();");

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessStatusItems(BusinessOperationalStatus? selectedStatus)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"AllStatuses\"), string.Empty, !selectedStatus.HasValue);");
        controllerSource.Should().Contain("foreach (var status in Enum.GetValues<BusinessOperationalStatus>())");
        controllerSource.Should().Contain("yield return new SelectListItem(T(status.ToString()), status.ToString(), selectedStatus == status);");
    }


    [Fact]
    public void BusinessesController_Should_KeepWorkspaceFilterItemBuilderContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessMemberFilterItems(BusinessMemberSupportFilter selectedFilter)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessMembersAllLabel\"), BusinessMemberSupportFilter.All.ToString(), selectedFilter == BusinessMemberSupportFilter.All);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"NeedsAttention\"), BusinessMemberSupportFilter.Attention.ToString(), selectedFilter == BusinessMemberSupportFilter.Attention);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"PendingActivation\"), BusinessMemberSupportFilter.PendingActivation.ToString(), selectedFilter == BusinessMemberSupportFilter.PendingActivation);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessMembersLockedLabel\"), BusinessMemberSupportFilter.Locked.ToString(), selectedFilter == BusinessMemberSupportFilter.Locked);");

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessInvitationFilterItems(BusinessInvitationQueueFilter selectedFilter)");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.All), BusinessInvitationQueueFilter.All.ToString(), selectedFilter == BusinessInvitationQueueFilter.All);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Open), BusinessInvitationQueueFilter.Open.ToString(), selectedFilter == BusinessInvitationQueueFilter.Open);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Pending), BusinessInvitationQueueFilter.Pending.ToString(), selectedFilter == BusinessInvitationQueueFilter.Pending);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Expired), BusinessInvitationQueueFilter.Expired.ToString(), selectedFilter == BusinessInvitationQueueFilter.Expired);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Accepted), BusinessInvitationQueueFilter.Accepted.ToString(), selectedFilter == BusinessInvitationQueueFilter.Accepted);");
        controllerSource.Should().Contain("new SelectListItem(DescribeBusinessInvitationQueueLabel(BusinessInvitationQueueFilter.Revoked), BusinessInvitationQueueFilter.Revoked.ToString(), selectedFilter == BusinessInvitationQueueFilter.Revoked);");

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessLocationFilterItems(BusinessLocationQueueFilter selectedFilter)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonAll\"), BusinessLocationQueueFilter.All.ToString(), selectedFilter == BusinessLocationQueueFilter.All);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessLocationsPrimaryLocationLabel\"), BusinessLocationQueueFilter.Primary.ToString(), selectedFilter == BusinessLocationQueueFilter.Primary);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"MissingAddress\"), BusinessLocationQueueFilter.MissingAddress.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingAddress);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessLocationsMissingCoordinatesLabel\"), BusinessLocationQueueFilter.MissingCoordinates.ToString(), selectedFilter == BusinessLocationQueueFilter.MissingCoordinates);");
    }


    [Fact]
    public void BusinessesController_Should_KeepSupportAuditRecommendedActionHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private string BuildSupportAuditRecommendedAction(EmailDispatchAuditListItemDto item)");
        controllerSource.Should().Contain("if (string.Equals(item.FlowKey, \"BusinessInvitation\", StringComparison.OrdinalIgnoreCase))");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditInvitationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditInvitationGenericAction\")");
        controllerSource.Should().Contain("if (string.Equals(item.FlowKey, \"AccountActivation\", StringComparison.OrdinalIgnoreCase))");
        controllerSource.Should().Contain("? T(\"BusinessSupportAuditActivationBusinessAction\")");
        controllerSource.Should().Contain(": T(\"BusinessSupportAuditActivationGenericAction\")");
        controllerSource.Should().Contain("if (string.Equals(item.FlowKey, \"PasswordReset\", StringComparison.OrdinalIgnoreCase))");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditPasswordResetAction\")");
        controllerSource.Should().Contain("return T(\"BusinessSupportAuditGenericAction\")");
    }


    [Fact]
    public void BusinessesController_Should_KeepMerchantReadinessPlaybookHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private List<MerchantReadinessPlaybookVm> BuildMerchantReadinessPlaybooks(Guid? businessId)");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookApprovalTitle\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Index\", \"Businesses\", new { operationalStatus = BusinessOperationalStatus.PendingApproval }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookSetupTitle\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Index\", \"Businesses\", new { attentionOnly = true }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"MerchantReadiness\", \"Businesses\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("Title = T(\"MerchantReadinessPlaybookBillingTitle\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Index\", \"Businesses\", new { readinessFilter = BusinessReadinessQueueFilter.ApprovedInactive }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Payments\", \"Billing\") ?? string.Empty");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionPlaybookHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private List<BusinessSubscriptionPlaybookVm> BuildSubscriptionPlaybooks(Guid businessId, BusinessSubscriptionSnapshotVm subscription, bool managementWebsiteConfigured)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessManagementWebsite\")");
        controllerSource.Should().Contain("OperatorAction = managementWebsiteConfigured");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-business-app\" }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"Payments\", \"Billing\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionCancellationPolicy\")");
        controllerSource.Should().Contain("OperatorAction = subscription.HasSubscription");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.Open }) ?? string.Empty,");
        controllerSource.Should().Contain("items.Add(new BusinessSubscriptionPlaybookVm");
        controllerSource.Should().Contain("if (!subscription.HasSubscription)");
        controllerSource.Should().Contain("QueueLabel = T(\"BusinessSubscriptionNoActivePlan\")");
        controllerSource.Should().Contain("QueueActionUrl = Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId, filter = BusinessSubscriptionInvoiceQueueFilter.All }) ?? string.Empty,");
        controllerSource.Should().Contain("FollowUpUrl = Url.Action(\"SupportQueue\", \"Businesses\", new { businessId }) ?? string.Empty");
        controllerSource.Should().Contain("return items;");
    }


    [Fact]
    public void BusinessesController_Should_KeepBusinessSubscriptionSnapshotHelperContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private async Task<BusinessSubscriptionSnapshotVm> BuildBusinessSubscriptionSnapshotAsync(Guid businessId, CancellationToken ct)");
        controllerSource.Should().Contain("if (businessId == Guid.Empty)");
        controllerSource.Should().Contain("return new BusinessSubscriptionSnapshotVm();");
        controllerSource.Should().Contain("var result = await _getBusinessSubscriptionStatus.HandleAsync(");
        controllerSource.Should().Contain("if (!result.Succeeded || result.Value is null)");
        controllerSource.Should().Contain("HasSubscription = false,");
        controllerSource.Should().Contain("Status = T(\"Unavailable\")");
        controllerSource.Should().Contain("return new BusinessSubscriptionSnapshotVm");
        controllerSource.Should().Contain("HasSubscription = result.Value.HasSubscription,");
        controllerSource.Should().Contain("SubscriptionId = result.Value.SubscriptionId,");
        controllerSource.Should().Contain("RowVersion = result.Value.RowVersion,");
        controllerSource.Should().Contain("Status = result.Value.Status,");
        controllerSource.Should().Contain("Provider = result.Value.Provider,");
        controllerSource.Should().Contain("PlanCode = result.Value.PlanCode,");
        controllerSource.Should().Contain("PlanName = result.Value.PlanName,");
        controllerSource.Should().Contain("Currency = result.Value.Currency,");
        controllerSource.Should().Contain("UnitPriceMinor = result.Value.UnitPriceMinor,");
        controllerSource.Should().Contain("StartedAtUtc = result.Value.StartedAtUtc,");
        controllerSource.Should().Contain("CurrentPeriodEndUtc = result.Value.CurrentPeriodEndUtc,");
        controllerSource.Should().Contain("TrialEndsAtUtc = result.Value.TrialEndsAtUtc,");
        controllerSource.Should().Contain("CancelAtPeriodEnd = result.Value.CancelAtPeriodEnd");
    }


    [Fact]
    public void BusinessesController_Should_KeepSubscriptionInvoiceFilterItemBuilderContractWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));

        controllerSource.Should().Contain("private IEnumerable<SelectListItem> BuildBusinessSubscriptionInvoiceFilterItems(BusinessSubscriptionInvoiceQueueFilter selectedFilter)");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessSubscriptionAllInvoicesLabel\"), BusinessSubscriptionInvoiceQueueFilter.All.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.All);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonOpen\"), BusinessSubscriptionInvoiceQueueFilter.Open.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Open);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonPaid\"), BusinessSubscriptionInvoiceQueueFilter.Paid.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Paid);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonDraft\"), BusinessSubscriptionInvoiceQueueFilter.Draft.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Draft);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonUncollectible\"), BusinessSubscriptionInvoiceQueueFilter.Uncollectible.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Uncollectible);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessSubscriptionHostedLinkMissing\"), BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.HostedLinkMissing);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonStripe\"), BusinessSubscriptionInvoiceQueueFilter.Stripe.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Stripe);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"CommonOverdue\"), BusinessSubscriptionInvoiceQueueFilter.Overdue.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.Overdue);");
        controllerSource.Should().Contain("yield return new SelectListItem(T(\"BusinessSubscriptionReviewPdfMissing\"), BusinessSubscriptionInvoiceQueueFilter.PdfMissing.ToString(), selectedFilter == BusinessSubscriptionInvoiceQueueFilter.PdfMissing);");
    }


    [Fact]
    public void BusinessOwnerOverrideAuditsWorkspace_Should_KeepSearchPlaybookAndFollowUpContractsWired()
    {
        var auditsViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "OwnerOverrideAudits.cshtml"));

        auditsViewSource.Should().Contain("id=\"business-owner-override-audits-workspace-shell\"");
        auditsViewSource.Should().Contain("string GlobalOwnerOverrideAuditsUrl() => Url.Action(\"OwnerOverrideAudits\", \"Businesses\") ?? string.Empty;");
        auditsViewSource.Should().Contain("hx-get=\"@GlobalOwnerOverrideAuditsUrl()\"");
        auditsViewSource.Should().Contain("name=\"businessId\" value=\"@Model.Business.Id\"");
        auditsViewSource.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        auditsViewSource.Should().Contain("placeholder=\"@T.T(\"BusinessOwnerOverrideAuditsSearchPlaceholder\")\"");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideAuditsIntro\")");
        auditsViewSource.Should().Contain("string ActorDisplayName(string? actorDisplayName) => string.IsNullOrWhiteSpace(actorDisplayName)");
        auditsViewSource.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        auditsViewSource.Should().Contain("hx-target=\"#business-owner-override-audits-workspace-shell\"");
        auditsViewSource.Should().Contain("@playbook.QueueActionLabel");
        auditsViewSource.Should().Contain("@playbook.FollowUpLabel");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideAuditsEmptyState\")");
          auditsViewSource.Should().Contain("@BusinessMembersAttentionUrl(Model.Business.Id)");
        auditsViewSource.Should().Contain("@BusinessSetupUrl(Model.Business.Id)");
        auditsViewSource.Should().Contain("@BusinessMerchantReadinessUrl(Model.Business.Id)");
        auditsViewSource.Should().Contain("@BusinessSupportQueueUrl(Model.Business.Id)");
        (auditsViewSource.Split("asp-route-businessId=\"@Model.Business.Id\"", StringSplitOptions.None).Length - 1).Should().BeGreaterThanOrEqualTo(11);
        auditsViewSource.Should().Contain("@UserEditUrl(item.AffectedUserId)");
        auditsViewSource.Should().Contain("<td>@ActorDisplayName(item.ActorDisplayName)</td>");
        auditsViewSource.Should().Contain("item.ActionKind == Darwin.Domain.Enums.BusinessOwnerOverrideActionKind.ForceRemove");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideForceRemove\")");
        auditsViewSource.Should().Contain("@T.T(\"BusinessOwnerOverrideDemoteDeactivate\")");
        auditsViewSource.Should().Contain("asp-action=\"OwnerOverrideAudits\"");
        auditsViewSource.Should().Contain("asp-route-businessId=\"@Model.Business.Id\"");
    }
}
