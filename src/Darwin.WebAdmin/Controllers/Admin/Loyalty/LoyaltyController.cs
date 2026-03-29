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
        public IActionResult Index() => RedirectOrHtmx(nameof(Programs), new { });

        [HttpGet]
        public async Task<IActionResult> Programs(Guid? businessId = null, int page = 1, int pageSize = 20, LoyaltyProgramQueueFilter filter = LoyaltyProgramQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<LoyaltyProgramListItemDto>();
            var total = 0;
            var summary = new LoyaltyProgramOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getProgramsPage.HandleAsync(page, pageSize, businessId.Value, filter, ct).ConfigureAwait(false);
                var summaryDto = await _getProgramsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
                summary = new LoyaltyProgramOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    ActiveCount = summaryDto.ActiveCount,
                    InactiveCount = summaryDto.InactiveCount,
                    PerCurrencyUnitCount = summaryDto.PerCurrencyUnitCount,
                    MissingRulesCount = summaryDto.MissingRulesCount
                };
            }

            return RenderProgramsWorkspace(new LoyaltyProgramsListVm
            {
                BusinessId = businessId,
                Filter = filter,
                FilterItems = BuildProgramFilterItems(filter),
                Summary = summary,
                Playbooks = BuildProgramPlaybooks(),
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
            return RenderProgramEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProgram(LoyaltyProgramEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderProgramEditor(vm, isCreate: true);
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
                return RedirectOrHtmx(nameof(EditProgram), new { id });
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderProgramEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProgram(Guid id, CancellationToken ct = default)
        {
            var dto = await _getProgramForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Loyalty program not found.";
                return RedirectOrHtmx(nameof(Programs), new { });
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
            return RenderProgramEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProgram(LoyaltyProgramEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderProgramEditor(vm, isCreate: false);
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
                return RedirectOrHtmx(nameof(EditProgram), new { id = vm.Id });
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderProgramEditor(vm, isCreate: false);
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
            return RedirectOrHtmx(nameof(Programs), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> RewardTiers(Guid loyaltyProgramId, int page = 1, int pageSize = 20, LoyaltyRewardTierQueueFilter filter = LoyaltyRewardTierQueueFilter.All, CancellationToken ct = default)
        {
            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                TempData["Error"] = "Loyalty program not found.";
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            var result = await _getRewardTiersPage.HandleAsync(loyaltyProgramId, page, pageSize, filter, ct).ConfigureAwait(false);
            var summaryDto = await _getRewardTiersPage.GetSummaryAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            return RenderRewardTiersWorkspace(new LoyaltyRewardTiersListVm
            {
                LoyaltyProgramId = loyaltyProgramId,
                ProgramName = program.Name,
                BusinessId = program.BusinessId,
                Filter = filter,
                FilterItems = BuildRewardTierFilterItems(filter),
                Summary = new LoyaltyRewardTierOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    SelfRedemptionCount = summaryDto.SelfRedemptionCount,
                    MissingDescriptionCount = summaryDto.MissingDescriptionCount,
                    DiscountRewardCount = summaryDto.DiscountRewardCount,
                    FreeItemCount = summaryDto.FreeItemCount
                },
                Playbooks = BuildRewardTierPlaybooks(),
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
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            return RenderRewardTierEditor(new LoyaltyRewardTierEditVm
            {
                LoyaltyProgramId = program.Id,
                ProgramName = program.Name,
                BusinessId = program.BusinessId,
                AllowSelfRedemption = false
            }, isCreate: true);
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
                return RedirectOrHtmx(nameof(EditRewardTier), new { id, loyaltyProgramId = vm.LoyaltyProgramId });
            }
            catch (ValidationException ex)
            {
                var program = await _getProgramForEdit.HandleAsync(vm.LoyaltyProgramId, ct).ConfigureAwait(false);
                vm.ProgramName = program?.Name ?? vm.ProgramName;
                vm.BusinessId = program?.BusinessId ?? vm.BusinessId;
                ModelState.AddModelError(string.Empty, ex.Message);
                return RenderRewardTierEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditRewardTier(Guid id, Guid loyaltyProgramId, CancellationToken ct = default)
        {
            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                TempData["Error"] = "Loyalty program not found.";
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            var tier = await _getRewardTierForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (tier is null)
            {
                TempData["Error"] = "Reward tier not found.";
                return RedirectOrHtmx(nameof(RewardTiers), new { loyaltyProgramId });
            }

            return RenderRewardTierEditor(new LoyaltyRewardTierEditVm
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
            }, isCreate: false);
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
                return RedirectOrHtmx(nameof(EditRewardTier), new { id = vm.Id, loyaltyProgramId = vm.LoyaltyProgramId });
            }
            catch (ValidationException ex)
            {
                var program = await _getProgramForEdit.HandleAsync(vm.LoyaltyProgramId, ct).ConfigureAwait(false);
                vm.ProgramName = program?.Name ?? vm.ProgramName;
                vm.BusinessId = program?.BusinessId ?? vm.BusinessId;
                ModelState.AddModelError(string.Empty, ex.Message);
                return RenderRewardTierEditor(vm, isCreate: false);
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
            return RedirectOrHtmx(nameof(RewardTiers), new { loyaltyProgramId });
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

            return RenderAccountsWorkspace(new LoyaltyAccountsListVm
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
            return RenderAccountCreateEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(CreateLoyaltyAccountVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct).ConfigureAwait(false);
                return RenderAccountCreateEditor(vm);
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
                return RenderAccountCreateEditor(vm);
            }

            TempData["Success"] = "Loyalty account created.";
            return RedirectOrHtmx(nameof(AccountDetails), new { id = result.Value.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Campaigns(Guid? businessId = null, int page = 1, int pageSize = 20, LoyaltyCampaignQueueFilter filter = LoyaltyCampaignQueueFilter.All, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<BusinessCampaignItemDto>();
            var total = 0;
            var summary = new LoyaltyCampaignOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getCampaigns.HandleAsync(businessId.Value, page, pageSize, filter, ct).ConfigureAwait(false);
                if (!result.Succeeded || result.Value is null)
                {
                    TempData["Error"] = result.Error ?? "Unable to load loyalty campaigns.";
                }
                else
                {
                    var summaryDto = await _getCampaigns.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
                    items = result.Value.Items.ToArray();
                    total = result.Value.Total;
                    summary = new LoyaltyCampaignOpsSummaryVm
                    {
                        TotalCount = summaryDto.TotalCount,
                        ActiveCount = summaryDto.ActiveCount,
                        ScheduledCount = summaryDto.ScheduledCount,
                        DraftCount = summaryDto.DraftCount,
                        ExpiredCount = summaryDto.ExpiredCount,
                        PushEnabledCount = summaryDto.PushEnabledCount
                    };
                }
            }

            return RenderCampaignsWorkspace(new LoyaltyCampaignsListVm
            {
                BusinessId = businessId,
                Filter = filter,
                FilterItems = BuildCampaignFilterItems(filter),
                Summary = summary,
                Playbooks = BuildCampaignPlaybooks(),
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
            return RenderCampaignEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCampaign(LoyaltyCampaignEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderCampaignEditor(vm, isCreate: true);
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
                return RenderCampaignEditor(vm, isCreate: true);
            }

            TempData["Success"] = "Loyalty campaign created.";
            return RedirectOrHtmx(nameof(EditCampaign), new { id = result.Value, businessId = vm.BusinessId });
        }

        [HttpGet]
        public async Task<IActionResult> EditCampaign(Guid id, Guid businessId, CancellationToken ct = default)
        {
            var result = await _getCampaigns.HandleAsync(businessId, 1, 200, LoyaltyCampaignQueueFilter.All, ct).ConfigureAwait(false);
            var campaign = result.Succeeded ? result.Value?.Items.FirstOrDefault(x => x.Id == id) : null;
            if (campaign is null)
            {
                TempData["Error"] = "Loyalty campaign not found.";
                return RedirectOrHtmx(nameof(Campaigns), new { businessId });
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
            return RenderCampaignEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCampaign(LoyaltyCampaignEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderCampaignEditor(vm, isCreate: false);
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
                return RenderCampaignEditor(vm, isCreate: false);
            }

            TempData["Success"] = "Loyalty campaign updated.";
            return RedirectOrHtmx(nameof(EditCampaign), new { id = vm.Id, businessId = vm.BusinessId });
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

            return RedirectOrHtmx(nameof(Campaigns), new { businessId });
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

            return RenderScanSessionsWorkspace(new LoyaltyScanSessionsListVm
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

            return RenderRedemptionsWorkspace(new LoyaltyRedemptionsListVm
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
                return RedirectOrHtmx(nameof(Accounts), new { });
            }

            var transactions = await _getTransactions.HandleAsync(id, 50, ct).ConfigureAwait(false);
            var redemptions = await _getRedemptions.HandleAsync(id, 50, ct).ConfigureAwait(false);

            return RenderAccountDetailsWorkspace(new LoyaltyAccountDetailsVm
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
                return RedirectOrHtmx(nameof(Accounts), new { });
            }

            return RenderAdjustPointsEditor(new AdjustLoyaltyPointsVm
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
                return RedirectOrHtmx(nameof(AccountDetails), new { id = vm.LoyaltyAccountId });
            }
            catch (ValidationException ex)
            {
                var account = await _getAccountForAdmin.HandleAsync(vm.LoyaltyAccountId, ct).ConfigureAwait(false);
                vm.AccountLabel = account is null ? vm.AccountLabel : $"{account.UserDisplayName} ({account.UserEmail})";
                ModelState.AddModelError(string.Empty, ex.Message);
                return RenderAdjustPointsEditor(vm);
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
            return RedirectOrHtmx(nameof(AccountDetails), new { id });
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
            return RedirectOrHtmx(nameof(AccountDetails), new { id });
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

            return RedirectOrHtmx(nameof(AccountDetails), new { id = loyaltyAccountId });
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

        private static List<SelectListItem> BuildProgramFilterItems(LoyaltyProgramQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new("All programs", LoyaltyProgramQueueFilter.All.ToString(), selected == LoyaltyProgramQueueFilter.All),
                new("Active", LoyaltyProgramQueueFilter.Active.ToString(), selected == LoyaltyProgramQueueFilter.Active),
                new("Inactive", LoyaltyProgramQueueFilter.Inactive.ToString(), selected == LoyaltyProgramQueueFilter.Inactive),
                new("Spend-based accrual", LoyaltyProgramQueueFilter.PerCurrencyUnit.ToString(), selected == LoyaltyProgramQueueFilter.PerCurrencyUnit),
                new("Missing rules", LoyaltyProgramQueueFilter.MissingRules.ToString(), selected == LoyaltyProgramQueueFilter.MissingRules)
            };
        }

        private static List<SelectListItem> BuildRewardTierFilterItems(LoyaltyRewardTierQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new("All reward tiers", LoyaltyRewardTierQueueFilter.All.ToString(), selected == LoyaltyRewardTierQueueFilter.All),
                new("Self redemption", LoyaltyRewardTierQueueFilter.SelfRedemption.ToString(), selected == LoyaltyRewardTierQueueFilter.SelfRedemption),
                new("Missing description", LoyaltyRewardTierQueueFilter.MissingDescription.ToString(), selected == LoyaltyRewardTierQueueFilter.MissingDescription),
                new("Discount rewards", LoyaltyRewardTierQueueFilter.DiscountRewards.ToString(), selected == LoyaltyRewardTierQueueFilter.DiscountRewards),
                new("Free item", LoyaltyRewardTierQueueFilter.FreeItem.ToString(), selected == LoyaltyRewardTierQueueFilter.FreeItem)
            };
        }

        private static List<SelectListItem> BuildCampaignFilterItems(LoyaltyCampaignQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new("All campaigns", LoyaltyCampaignQueueFilter.All.ToString(), selected == LoyaltyCampaignQueueFilter.All),
                new("Active", LoyaltyCampaignQueueFilter.Active.ToString(), selected == LoyaltyCampaignQueueFilter.Active),
                new("Scheduled", LoyaltyCampaignQueueFilter.Scheduled.ToString(), selected == LoyaltyCampaignQueueFilter.Scheduled),
                new("Draft", LoyaltyCampaignQueueFilter.Draft.ToString(), selected == LoyaltyCampaignQueueFilter.Draft),
                new("Expired", LoyaltyCampaignQueueFilter.Expired.ToString(), selected == LoyaltyCampaignQueueFilter.Expired),
                new("Push enabled", LoyaltyCampaignQueueFilter.PushEnabled.ToString(), selected == LoyaltyCampaignQueueFilter.PushEnabled)
            };
        }

        private static List<LoyaltyOpsPlaybookVm> BuildProgramPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = "Program readiness",
                    ScopeNote = "Inactive programs and rule-light programs are the first place to review when accrual or redemption expectations drift between mobile, staff, and admin support.",
                    OperatorAction = "Keep one clear active program path per business, and review inactive or missing-rule programs before go-live support escalates."
                },
                new()
                {
                    Title = "Spend-based accrual review",
                    ScopeNote = "Spend-based programs tie loyalty balances more tightly to orders, refunds, and financial support workflows.",
                    OperatorAction = "Validate spend-based accrual settings before investigating balance mismatches so loyalty and payment support stay aligned."
                }
            };
        }

        private static List<LoyaltyOpsPlaybookVm> BuildRewardTierPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = "Self-redemption review",
                    ScopeNote = "Self-redeemable tiers affect customer-facing behavior without staff mediation and should be reviewed before promotions or reward support cases.",
                    OperatorAction = "Prioritize self-redemption tiers when investigating unexpected redemption flow or scanner support issues."
                },
                new()
                {
                    Title = "Reward catalog hygiene",
                    ScopeNote = "Missing descriptions or mixed discount/free-item tiers make campaign copy and support guidance harder to interpret consistently.",
                    OperatorAction = "Fill in descriptions and verify reward type intent before operators, campaigns, or storefront messaging rely on the catalog."
                }
            };
        }

        private static List<LoyaltyOpsPlaybookVm> BuildCampaignPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = "Activation and window review",
                    ScopeNote = "Scheduled and expired campaigns are the first place to inspect when support sees stale mobile banners or campaigns that never went live.",
                    OperatorAction = "Review activation state and the UTC campaign window together before escalating a campaign timing issue."
                },
                new()
                {
                    Title = "Push-enabled campaign hygiene",
                    ScopeNote = "Push-enabled campaigns increase delivery expectations across mobile operations and communications support.",
                    OperatorAction = "Prioritize push-enabled campaigns when checking rollout readiness so content, transport, and device diagnostics stay aligned."
                }
            };
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

        private IActionResult RenderProgramEditor(LoyaltyProgramEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Loyalty/_ProgramEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateProgram", vm) : View("EditProgram", vm);
        }

        private IActionResult RenderProgramsWorkspace(LoyaltyProgramsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/Programs.cshtml", vm);
            }

            return View("Programs", vm);
        }

        private IActionResult RenderAccountCreateEditor(CreateLoyaltyAccountVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/_AccountCreateEditorShell.cshtml", vm);
            }

            return View("CreateAccount", vm);
        }

        private IActionResult RenderAdjustPointsEditor(AdjustLoyaltyPointsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/_AdjustPointsEditorShell.cshtml", vm);
            }

            return View("AdjustPoints", vm);
        }

        private IActionResult RenderAccountsWorkspace(LoyaltyAccountsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/Accounts.cshtml", vm);
            }

            return View("Accounts", vm);
        }

        private IActionResult RenderCampaignsWorkspace(LoyaltyCampaignsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/Campaigns.cshtml", vm);
            }

            return View("Campaigns", vm);
        }

        private IActionResult RenderScanSessionsWorkspace(LoyaltyScanSessionsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/ScanSessions.cshtml", vm);
            }

            return View("ScanSessions", vm);
        }

        private IActionResult RenderRedemptionsWorkspace(LoyaltyRedemptionsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/Redemptions.cshtml", vm);
            }

            return View("Redemptions", vm);
        }

        private IActionResult RenderAccountDetailsWorkspace(LoyaltyAccountDetailsVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/AccountDetails.cshtml", vm);
            }

            return View("AccountDetails", vm);
        }

        private IActionResult RenderRewardTiersWorkspace(LoyaltyRewardTiersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/RewardTiers.cshtml", vm);
            }

            return View("RewardTiers", vm);
        }

        private IActionResult RenderRewardTierEditor(LoyaltyRewardTierEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Loyalty/_RewardTierEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateRewardTier", vm) : View("EditRewardTier", vm);
        }

        private IActionResult RenderCampaignEditor(LoyaltyCampaignEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Loyalty/_CampaignEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateCampaign", vm) : View("EditCampaign", vm);
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
