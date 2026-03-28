using Darwin.Application.Loyalty.Commands;
using Darwin.Application.Loyalty.Campaigns;
using Darwin.Application.Loyalty.DTOs;
using Darwin.Application.Loyalty.Queries;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.Loyalty;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.Controllers.Admin.Loyalty
{
    public sealed class LoyaltyController : AdminBaseController
    {
        private readonly GetLoyaltyProgramsPageHandler _getProgramsPage;
        private readonly GetLoyaltyProgramForEditHandler _getProgramForEdit;
        private readonly CreateLoyaltyProgramHandler _createProgram;
        private readonly UpdateLoyaltyProgramHandler _updateProgram;
        private readonly SoftDeleteLoyaltyProgramHandler _deleteProgram;
        private readonly GetLoyaltyRewardTiersPageHandler _getRewardTiersPage;
        private readonly GetLoyaltyRewardTierForEditHandler _getRewardTierForEdit;
        private readonly CreateLoyaltyRewardTierHandler _createRewardTier;
        private readonly UpdateLoyaltyRewardTierHandler _updateRewardTier;
        private readonly SoftDeleteLoyaltyRewardTierHandler _deleteRewardTier;
        private readonly GetLoyaltyAccountsPageHandler _getAccountsPage;
        private readonly GetRecentLoyaltyScanSessionsPageHandler _getScanSessionsPage;
        private readonly GetLoyaltyRedemptionsPageHandler _getRedemptionsPage;
        private readonly GetLoyaltyAccountForAdminHandler _getAccountForAdmin;
        private readonly CreateLoyaltyAccountByAdminHandler _createAccountByAdmin;
        private readonly GetLoyaltyAccountTransactionsHandler _getTransactions;
        private readonly GetLoyaltyAccountRedemptionsHandler _getRedemptions;
        private readonly ConfirmLoyaltyRewardRedemptionHandler _confirmRedemption;
        private readonly AdjustLoyaltyPointsHandler _adjustPoints;
        private readonly SuspendLoyaltyAccountHandler _suspendAccount;
        private readonly ActivateLoyaltyAccountHandler _activateAccount;
        private readonly GetBusinessCampaignsHandler _getCampaigns;
        private readonly CreateBusinessCampaignHandler _createCampaign;
        private readonly UpdateBusinessCampaignHandler _updateCampaign;
        private readonly SetCampaignActivationHandler _setCampaignActivation;
        private readonly AdminReferenceDataService _referenceData;

        public LoyaltyController(
            GetLoyaltyProgramsPageHandler getProgramsPage,
            GetLoyaltyProgramForEditHandler getProgramForEdit,
            CreateLoyaltyProgramHandler createProgram,
            UpdateLoyaltyProgramHandler updateProgram,
            SoftDeleteLoyaltyProgramHandler deleteProgram,
            GetLoyaltyRewardTiersPageHandler getRewardTiersPage,
            GetLoyaltyRewardTierForEditHandler getRewardTierForEdit,
            CreateLoyaltyRewardTierHandler createRewardTier,
            UpdateLoyaltyRewardTierHandler updateRewardTier,
            SoftDeleteLoyaltyRewardTierHandler deleteRewardTier,
            GetLoyaltyAccountsPageHandler getAccountsPage,
            GetRecentLoyaltyScanSessionsPageHandler getScanSessionsPage,
            GetLoyaltyRedemptionsPageHandler getRedemptionsPage,
            GetLoyaltyAccountForAdminHandler getAccountForAdmin,
            CreateLoyaltyAccountByAdminHandler createAccountByAdmin,
            GetLoyaltyAccountTransactionsHandler getTransactions,
            GetLoyaltyAccountRedemptionsHandler getRedemptions,
            ConfirmLoyaltyRewardRedemptionHandler confirmRedemption,
            AdjustLoyaltyPointsHandler adjustPoints,
            SuspendLoyaltyAccountHandler suspendAccount,
            ActivateLoyaltyAccountHandler activateAccount,
            GetBusinessCampaignsHandler getCampaigns,
            CreateBusinessCampaignHandler createCampaign,
            UpdateBusinessCampaignHandler updateCampaign,
            SetCampaignActivationHandler setCampaignActivation,
            AdminReferenceDataService referenceData)
        {
            _getProgramsPage = getProgramsPage;
            _getProgramForEdit = getProgramForEdit;
            _createProgram = createProgram;
            _updateProgram = updateProgram;
            _deleteProgram = deleteProgram;
            _getRewardTiersPage = getRewardTiersPage;
            _getRewardTierForEdit = getRewardTierForEdit;
            _createRewardTier = createRewardTier;
            _updateRewardTier = updateRewardTier;
            _deleteRewardTier = deleteRewardTier;
            _getAccountsPage = getAccountsPage;
            _getScanSessionsPage = getScanSessionsPage;
            _getRedemptionsPage = getRedemptionsPage;
            _getAccountForAdmin = getAccountForAdmin;
            _createAccountByAdmin = createAccountByAdmin;
            _getTransactions = getTransactions;
            _getRedemptions = getRedemptions;
            _confirmRedemption = confirmRedemption;
            _adjustPoints = adjustPoints;
            _suspendAccount = suspendAccount;
            _activateAccount = activateAccount;
            _getCampaigns = getCampaigns;
            _createCampaign = createCampaign;
            _updateCampaign = updateCampaign;
            _setCampaignActivation = setCampaignActivation;
            _referenceData = referenceData;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Programs));

        [HttpGet]
        public async Task<IActionResult> Programs(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<LoyaltyProgramListItemDto>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getProgramsPage.HandleAsync(page, pageSize, businessId.Value, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
            }

            return View(new LoyaltyProgramsListVm
            {
                BusinessId = businessId,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new LoyaltyProgramListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    AccrualMode = x.AccrualMode,
                    IsActive = x.IsActive,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateProgram(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new LoyaltyProgramEditVm { BusinessId = businessId ?? Guid.Empty, IsActive = true };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProgram(LoyaltyProgramEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            try
            {
                var id = await _createProgram.HandleAsync(new LoyaltyProgramCreateDto
                {
                    BusinessId = vm.BusinessId,
                    Name = vm.Name,
                    AccrualMode = vm.AccrualMode,
                    PointsPerCurrencyUnit = vm.PointsPerCurrencyUnit,
                    IsActive = vm.IsActive,
                    RulesJson = vm.RulesJson
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Loyalty program created.";
                return RedirectToAction(nameof(EditProgram), new { id });
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProgram(Guid id, CancellationToken ct = default)
        {
            var dto = await _getProgramForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Loyalty program not found.";
                return RedirectToAction(nameof(Programs));
            }

            var vm = new LoyaltyProgramEditVm
            {
                Id = dto.Id,
                BusinessId = dto.BusinessId,
                Name = dto.Name,
                AccrualMode = dto.AccrualMode,
                PointsPerCurrencyUnit = dto.PointsPerCurrencyUnit,
                IsActive = dto.IsActive,
                RulesJson = dto.RulesJson,
                RowVersion = dto.RowVersion
            };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProgram(LoyaltyProgramEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            try
            {
                await _updateProgram.HandleAsync(new LoyaltyProgramEditDto
                {
                    Id = vm.Id,
                    BusinessId = vm.BusinessId,
                    Name = vm.Name,
                    AccrualMode = vm.AccrualMode,
                    PointsPerCurrencyUnit = vm.PointsPerCurrencyUnit,
                    IsActive = vm.IsActive,
                    RulesJson = vm.RulesJson,
                    RowVersion = vm.RowVersion
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Loyalty program updated.";
                return RedirectToAction(nameof(EditProgram), new { id = vm.Id });
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgram(Guid id, Guid businessId, byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _deleteProgram.HandleAsync(new LoyaltyProgramDeleteDto
            {
                Id = id,
                RowVersion = rowVersion
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Loyalty program deleted." : result.Error;
            return RedirectToAction(nameof(Programs), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> RewardTiers(Guid loyaltyProgramId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                TempData["Error"] = "Loyalty program not found.";
                return RedirectToAction(nameof(Programs));
            }

            var result = await _getRewardTiersPage.HandleAsync(loyaltyProgramId, page, pageSize, ct).ConfigureAwait(false);
            return View(new LoyaltyRewardTiersListVm
            {
                LoyaltyProgramId = loyaltyProgramId,
                ProgramName = program.Name,
                BusinessId = program.BusinessId,
                Page = page,
                PageSize = pageSize,
                Total = result.Total,
                Items = result.Items.Select(x => new LoyaltyRewardTierListItemVm
                {
                    Id = x.Id,
                    LoyaltyProgramId = x.LoyaltyProgramId,
                    PointsRequired = x.PointsRequired,
                    RewardType = x.RewardType,
                    RewardValue = x.RewardValue,
                    Description = x.Description,
                    AllowSelfRedemption = x.AllowSelfRedemption,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateRewardTier(Guid loyaltyProgramId, CancellationToken ct = default)
        {
            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                TempData["Error"] = "Loyalty program not found.";
                return RedirectToAction(nameof(Programs));
            }

            return View(new LoyaltyRewardTierEditVm
            {
                LoyaltyProgramId = program.Id,
                ProgramName = program.Name,
                BusinessId = program.BusinessId,
                AllowSelfRedemption = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRewardTier(LoyaltyRewardTierEditVm vm, CancellationToken ct = default)
        {
            try
            {
                var id = await _createRewardTier.HandleAsync(new LoyaltyRewardTierCreateDto
                {
                    LoyaltyProgramId = vm.LoyaltyProgramId,
                    PointsRequired = vm.PointsRequired,
                    RewardType = vm.RewardType,
                    RewardValue = vm.RewardValue,
                    Description = vm.Description,
                    AllowSelfRedemption = vm.AllowSelfRedemption,
                    MetadataJson = vm.MetadataJson
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Reward tier created.";
                return RedirectToAction(nameof(EditRewardTier), new { id, loyaltyProgramId = vm.LoyaltyProgramId });
            }
            catch (ValidationException ex)
            {
                var program = await _getProgramForEdit.HandleAsync(vm.LoyaltyProgramId, ct).ConfigureAwait(false);
                vm.ProgramName = program?.Name ?? vm.ProgramName;
                vm.BusinessId = program?.BusinessId ?? vm.BusinessId;
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditRewardTier(Guid id, Guid loyaltyProgramId, CancellationToken ct = default)
        {
            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                TempData["Error"] = "Loyalty program not found.";
                return RedirectToAction(nameof(Programs));
            }

            var tier = await _getRewardTierForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (tier is null)
            {
                TempData["Error"] = "Reward tier not found.";
                return RedirectToAction(nameof(RewardTiers), new { loyaltyProgramId });
            }

            return View(new LoyaltyRewardTierEditVm
            {
                Id = tier.Id,
                LoyaltyProgramId = tier.LoyaltyProgramId,
                BusinessId = program.BusinessId,
                ProgramName = program.Name,
                PointsRequired = tier.PointsRequired,
                RewardType = tier.RewardType,
                RewardValue = tier.RewardValue,
                Description = tier.Description,
                AllowSelfRedemption = tier.AllowSelfRedemption,
                MetadataJson = tier.MetadataJson,
                RowVersion = tier.RowVersion
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRewardTier(LoyaltyRewardTierEditVm vm, CancellationToken ct = default)
        {
            try
            {
                await _updateRewardTier.HandleAsync(new LoyaltyRewardTierEditDto
                {
                    Id = vm.Id,
                    LoyaltyProgramId = vm.LoyaltyProgramId,
                    PointsRequired = vm.PointsRequired,
                    RewardType = vm.RewardType,
                    RewardValue = vm.RewardValue,
                    Description = vm.Description,
                    AllowSelfRedemption = vm.AllowSelfRedemption,
                    MetadataJson = vm.MetadataJson,
                    RowVersion = vm.RowVersion
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Reward tier updated.";
                return RedirectToAction(nameof(EditRewardTier), new { id = vm.Id, loyaltyProgramId = vm.LoyaltyProgramId });
            }
            catch (ValidationException ex)
            {
                var program = await _getProgramForEdit.HandleAsync(vm.LoyaltyProgramId, ct).ConfigureAwait(false);
                vm.ProgramName = program?.Name ?? vm.ProgramName;
                vm.BusinessId = program?.BusinessId ?? vm.BusinessId;
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRewardTier(Guid id, Guid loyaltyProgramId, byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _deleteRewardTier.HandleAsync(new LoyaltyRewardTierDeleteDto
            {
                Id = id,
                RowVersion = rowVersion
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Reward tier deleted." : result.Error;
            return RedirectToAction(nameof(RewardTiers), new { loyaltyProgramId });
        }

        [HttpGet]
        public async Task<IActionResult> Accounts(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, LoyaltyAccountStatus? status = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<LoyaltyAccountAdminListItemDto>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getAccountsPage.HandleAsync(businessId.Value, page, pageSize, q, status, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
            }

            return View(new LoyaltyAccountsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                StatusFilter = status,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                StatusItems = BuildStatusItems(status),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new LoyaltyAccountListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    UserId = x.UserId,
                    UserEmail = x.UserEmail,
                    UserDisplayName = x.UserDisplayName,
                    Status = x.Status,
                    PointsBalance = x.PointsBalance,
                    LifetimePoints = x.LifetimePoints,
                    LastAccrualAtUtc = x.LastAccrualAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateAccount(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new CreateLoyaltyAccountVm
            {
                BusinessId = businessId ?? Guid.Empty
            };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false);
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(null, includeEmpty: false, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(CreateLoyaltyAccountVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct).ConfigureAwait(false);
                return View(vm);
            }

            var result = await _createAccountByAdmin.HandleAsync(new CreateLoyaltyAccountByAdminDto
            {
                BusinessId = vm.BusinessId,
                UserId = vm.UserId
            }, ct).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Unable to create loyalty account.");
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct).ConfigureAwait(false);
                return View(vm);
            }

            TempData["Success"] = "Loyalty account created.";
            return RedirectToAction(nameof(AccountDetails), new { id = result.Value.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Campaigns(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<BusinessCampaignItemDto>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getCampaigns.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
                if (!result.Succeeded || result.Value is null)
                {
                    TempData["Error"] = result.Error ?? "Unable to load loyalty campaigns.";
                }
                else
                {
                    items = result.Value.Items.ToArray();
                    total = result.Value.Total;
                }
            }

            return View(new LoyaltyCampaignsListVm
            {
                BusinessId = businessId,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new LoyaltyCampaignListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    Name = x.Name,
                    Title = x.Title,
                    CampaignState = x.CampaignState,
                    IsActive = x.IsActive,
                    Channels = x.Channels,
                    StartsAtUtc = x.StartsAtUtc,
                    EndsAtUtc = x.EndsAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> CreateCampaign(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new LoyaltyCampaignEditVm { BusinessId = businessId ?? Guid.Empty };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCampaign(LoyaltyCampaignEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            var result = await _createCampaign.HandleAsync(new CreateBusinessCampaignDto
            {
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Title = vm.Title,
                Subtitle = vm.Subtitle,
                Body = vm.Body,
                MediaUrl = vm.MediaUrl,
                LandingUrl = vm.LandingUrl,
                Channels = vm.Channels,
                StartsAtUtc = vm.StartsAtUtc,
                EndsAtUtc = vm.EndsAtUtc,
                TargetingJson = vm.TargetingJson ?? "{}",
                PayloadJson = vm.PayloadJson ?? "{}"
            }, ct).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Unable to create loyalty campaign.");
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            TempData["Success"] = "Loyalty campaign created.";
            return RedirectToAction(nameof(Campaigns), new { businessId = vm.BusinessId });
        }

        [HttpGet]
        public async Task<IActionResult> EditCampaign(Guid id, Guid businessId, CancellationToken ct = default)
        {
            var result = await _getCampaigns.HandleAsync(businessId, 1, 200, ct).ConfigureAwait(false);
            var campaign = result.Succeeded ? result.Value?.Items.FirstOrDefault(x => x.Id == id) : null;
            if (campaign is null)
            {
                TempData["Error"] = "Loyalty campaign not found.";
                return RedirectToAction(nameof(Campaigns), new { businessId });
            }

            var vm = new LoyaltyCampaignEditVm
            {
                Id = campaign.Id,
                BusinessId = campaign.BusinessId,
                Name = campaign.Name,
                Title = campaign.Title,
                Subtitle = campaign.Subtitle,
                Body = campaign.Body,
                MediaUrl = campaign.MediaUrl,
                LandingUrl = campaign.LandingUrl,
                Channels = campaign.Channels,
                StartsAtUtc = campaign.StartsAtUtc,
                EndsAtUtc = campaign.EndsAtUtc,
                IsActive = campaign.IsActive,
                CampaignState = campaign.CampaignState,
                TargetingJson = campaign.TargetingJson,
                PayloadJson = campaign.PayloadJson,
                RowVersion = campaign.RowVersion
            };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCampaign(LoyaltyCampaignEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            var result = await _updateCampaign.HandleAsync(new UpdateBusinessCampaignDto
            {
                BusinessId = vm.BusinessId,
                Id = vm.Id,
                Name = vm.Name,
                Title = vm.Title,
                Subtitle = vm.Subtitle,
                Body = vm.Body,
                MediaUrl = vm.MediaUrl,
                LandingUrl = vm.LandingUrl,
                Channels = vm.Channels,
                StartsAtUtc = vm.StartsAtUtc,
                EndsAtUtc = vm.EndsAtUtc,
                TargetingJson = vm.TargetingJson ?? "{}",
                PayloadJson = vm.PayloadJson ?? "{}",
                RowVersion = vm.RowVersion ?? Array.Empty<byte>()
            }, ct).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Unable to update loyalty campaign.");
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
            }

            TempData["Success"] = "Loyalty campaign updated.";
            return RedirectToAction(nameof(EditCampaign), new { id = vm.Id, businessId = vm.BusinessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCampaignActivation(Guid id, Guid businessId, bool isActive, byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _setCampaignActivation.HandleAsync(new SetCampaignActivationDto
            {
                Id = id,
                BusinessId = businessId,
                IsActive = isActive,
                RowVersion = rowVersion ?? Array.Empty<byte>()
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? (isActive ? "Loyalty campaign activated." : "Loyalty campaign deactivated.")
                : result.Error;

            return RedirectToAction(nameof(Campaigns), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> ScanSessions(
            Guid? businessId = null,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            LoyaltyScanMode? mode = null,
            LoyaltyScanStatus? status = null,
            CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<LoyaltyScanSessionAdminListItemDto>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getScanSessionsPage.HandleAsync(businessId.Value, page, pageSize, q, mode, status, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
            }

            return View(new LoyaltyScanSessionsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                ModeFilter = mode,
                StatusFilter = status,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                ModeItems = BuildModeItems(mode),
                StatusItems = BuildScanStatusItems(status),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new LoyaltyScanSessionListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    CustomerEmail = x.CustomerEmail,
                    Mode = x.Mode,
                    Status = x.Status,
                    Outcome = x.Outcome,
                    FailureReason = x.FailureReason,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ExpiresAtUtc = x.ExpiresAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Redemptions(
            Guid? businessId = null,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            LoyaltyRedemptionStatus? status = null,
            CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<LoyaltyRewardRedemptionListItemDto>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getRedemptionsPage.HandleAsync(businessId.Value, page, pageSize, q, status, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
            }

            return View(new LoyaltyRedemptionsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                StatusFilter = status,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                StatusItems = BuildRedemptionStatusItems(status),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new LoyaltyRedemptionQueueItemVm
                {
                    Id = x.Id,
                    LoyaltyAccountId = x.LoyaltyAccountId,
                    BusinessId = x.BusinessId,
                    ConsumerDisplayName = x.ConsumerDisplayName,
                    ConsumerEmail = x.ConsumerEmail,
                    RewardLabel = x.RewardLabel,
                    PointsSpent = x.PointsSpent,
                    Status = x.Status,
                    RedeemedAtUtc = x.RedeemedAtUtc,
                    Note = x.Note,
                    ScanStatus = x.ScanStatus,
                    ScanOutcome = x.ScanOutcome,
                    ScanFailureReason = x.ScanFailureReason,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> AccountDetails(Guid id, CancellationToken ct = default)
        {
            var account = await _getAccountForAdmin.HandleAsync(id, ct).ConfigureAwait(false);
            if (account is null)
            {
                TempData["Error"] = "Loyalty account not found.";
                return RedirectToAction(nameof(Accounts));
            }

            var transactions = await _getTransactions.HandleAsync(id, 50, ct).ConfigureAwait(false);
            var redemptions = await _getRedemptions.HandleAsync(id, 50, ct).ConfigureAwait(false);

            return View(new LoyaltyAccountDetailsVm
            {
                Id = account.Id,
                BusinessId = account.BusinessId,
                UserId = account.UserId,
                UserEmail = account.UserEmail,
                UserDisplayName = account.UserDisplayName,
                Status = account.Status,
                PointsBalance = account.PointsBalance,
                LifetimePoints = account.LifetimePoints,
                LastAccrualAtUtc = account.LastAccrualAtUtc,
                RowVersion = account.RowVersion,
                Transactions = transactions.Select(x => new LoyaltyTransactionListItemVm
                {
                    Id = x.Id,
                    PointsDelta = x.PointsDelta,
                    Note = x.Note,
                    OccurredAtUtc = x.OccurredAtUtc,
                    RewardTierId = x.RewardTierId
                }).ToList(),
                Redemptions = redemptions.Select(x => new LoyaltyRedemptionListItemVm
                {
                    Id = x.Id,
                    RewardTierId = x.RewardTierId,
                    RewardLabel = x.RewardLabel,
                    PointsSpent = x.PointsSpent,
                    Status = x.Status,
                    RedeemedAtUtc = x.RedeemedAtUtc,
                    Note = x.Note,
                    ScanStatus = x.ScanStatus,
                    ScanOutcome = x.ScanOutcome,
                    ScanFailureReason = x.ScanFailureReason,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> AdjustPoints(Guid loyaltyAccountId, CancellationToken ct = default)
        {
            var account = await _getAccountForAdmin.HandleAsync(loyaltyAccountId, ct).ConfigureAwait(false);
            if (account is null)
            {
                TempData["Error"] = "Loyalty account not found.";
                return RedirectToAction(nameof(Accounts));
            }

            return View(new AdjustLoyaltyPointsVm
            {
                LoyaltyAccountId = account.Id,
                BusinessId = account.BusinessId,
                UserId = account.UserId,
                AccountLabel = $"{account.UserDisplayName} ({account.UserEmail})",
                RowVersion = account.RowVersion
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustPoints(AdjustLoyaltyPointsVm vm, CancellationToken ct = default)
        {
            try
            {
                await _adjustPoints.HandleAsync(new AdjustLoyaltyPointsDto
                {
                    LoyaltyAccountId = vm.LoyaltyAccountId,
                    BusinessId = vm.BusinessId,
                    PerformedByUserId = null,
                    PointsDelta = vm.PointsDelta,
                    Reason = vm.Reason,
                    Reference = vm.Reference,
                    RowVersion = vm.RowVersion
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Loyalty points adjusted.";
                return RedirectToAction(nameof(AccountDetails), new { id = vm.LoyaltyAccountId });
            }
            catch (ValidationException ex)
            {
                var account = await _getAccountForAdmin.HandleAsync(vm.LoyaltyAccountId, ct).ConfigureAwait(false);
                vm.AccountLabel = account is null ? vm.AccountLabel : $"{account.UserDisplayName} ({account.UserEmail})";
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendAccount(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _suspendAccount.HandleAsync(new SuspendLoyaltyAccountDto
            {
                Id = id,
                RowVersion = rowVersion
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Loyalty account suspended." : result.Error;
            return RedirectToAction(nameof(AccountDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAccount(Guid id, byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _activateAccount.HandleAsync(new ActivateLoyaltyAccountDto
            {
                Id = id,
                RowVersion = rowVersion
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Loyalty account activated." : result.Error;
            return RedirectToAction(nameof(AccountDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRedemption(Guid redemptionId, Guid businessId, Guid loyaltyAccountId, byte[]? rowVersion, CancellationToken ct = default)
        {
            var result = await _confirmRedemption.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
            {
                RedemptionId = redemptionId,
                BusinessId = businessId,
                RowVersion = rowVersion
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Redemption confirmed."
                : result.Error;

            return RedirectToAction(nameof(AccountDetails), new { id = loyaltyAccountId });
        }

        private static List<SelectListItem> BuildStatusItems(LoyaltyAccountStatus? selected)
        {
            var items = new List<SelectListItem>
            {
                new("All statuses", string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyAccountStatus>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), selected == x)));

            return items;
        }

        private static List<SelectListItem> BuildModeItems(LoyaltyScanMode? selected)
        {
            var items = new List<SelectListItem>
            {
                new("All modes", string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyScanMode>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), selected == x)));

            return items;
        }

        private static List<SelectListItem> BuildScanStatusItems(LoyaltyScanStatus? selected)
        {
            var items = new List<SelectListItem>
            {
                new("All statuses", string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyScanStatus>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), selected == x)));

            return items;
        }

        private static List<SelectListItem> BuildRedemptionStatusItems(LoyaltyRedemptionStatus? selected)
        {
            var items = new List<SelectListItem>
            {
                new("All statuses", string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyRedemptionStatus>()
                .Select(x => new SelectListItem(x.ToString(), x.ToString(), selected == x)));

            return items;
        }
    }
}
