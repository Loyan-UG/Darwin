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
        private readonly ExpireLoyaltyScanSessionHandler _expireScanSession;
        private readonly GetBusinessCampaignsHandler _getCampaigns;
        private readonly CreateBusinessCampaignHandler _createCampaign;
        private readonly UpdateBusinessCampaignHandler _updateCampaign;
        private readonly SetCampaignActivationHandler _setCampaignActivation;
        private readonly GetCampaignDeliveriesPageHandler _getCampaignDeliveriesPage;
        private readonly UpdateCampaignDeliveryStatusHandler _updateCampaignDeliveryStatus;
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
            ExpireLoyaltyScanSessionHandler expireScanSession,
            GetBusinessCampaignsHandler getCampaigns,
            CreateBusinessCampaignHandler createCampaign,
            UpdateBusinessCampaignHandler updateCampaign,
            SetCampaignActivationHandler setCampaignActivation,
            GetCampaignDeliveriesPageHandler getCampaignDeliveriesPage,
            UpdateCampaignDeliveryStatusHandler updateCampaignDeliveryStatus,
            AdminReferenceDataService referenceData)
        {
            _getProgramsPage = getProgramsPage ?? throw new ArgumentNullException(nameof(getProgramsPage));
            _getProgramForEdit = getProgramForEdit ?? throw new ArgumentNullException(nameof(getProgramForEdit));
            _createProgram = createProgram ?? throw new ArgumentNullException(nameof(createProgram));
            _updateProgram = updateProgram ?? throw new ArgumentNullException(nameof(updateProgram));
            _deleteProgram = deleteProgram ?? throw new ArgumentNullException(nameof(deleteProgram));
            _getRewardTiersPage = getRewardTiersPage ?? throw new ArgumentNullException(nameof(getRewardTiersPage));
            _getRewardTierForEdit = getRewardTierForEdit ?? throw new ArgumentNullException(nameof(getRewardTierForEdit));
            _createRewardTier = createRewardTier ?? throw new ArgumentNullException(nameof(createRewardTier));
            _updateRewardTier = updateRewardTier ?? throw new ArgumentNullException(nameof(updateRewardTier));
            _deleteRewardTier = deleteRewardTier ?? throw new ArgumentNullException(nameof(deleteRewardTier));
            _getAccountsPage = getAccountsPage ?? throw new ArgumentNullException(nameof(getAccountsPage));
            _getScanSessionsPage = getScanSessionsPage ?? throw new ArgumentNullException(nameof(getScanSessionsPage));
            _getRedemptionsPage = getRedemptionsPage ?? throw new ArgumentNullException(nameof(getRedemptionsPage));
            _getAccountForAdmin = getAccountForAdmin ?? throw new ArgumentNullException(nameof(getAccountForAdmin));
            _createAccountByAdmin = createAccountByAdmin ?? throw new ArgumentNullException(nameof(createAccountByAdmin));
            _getTransactions = getTransactions ?? throw new ArgumentNullException(nameof(getTransactions));
            _getRedemptions = getRedemptions ?? throw new ArgumentNullException(nameof(getRedemptions));
            _confirmRedemption = confirmRedemption ?? throw new ArgumentNullException(nameof(confirmRedemption));
            _adjustPoints = adjustPoints ?? throw new ArgumentNullException(nameof(adjustPoints));
            _suspendAccount = suspendAccount ?? throw new ArgumentNullException(nameof(suspendAccount));
            _activateAccount = activateAccount ?? throw new ArgumentNullException(nameof(activateAccount));
            _expireScanSession = expireScanSession ?? throw new ArgumentNullException(nameof(expireScanSession));
            _getCampaigns = getCampaigns ?? throw new ArgumentNullException(nameof(getCampaigns));
            _createCampaign = createCampaign ?? throw new ArgumentNullException(nameof(createCampaign));
            _updateCampaign = updateCampaign ?? throw new ArgumentNullException(nameof(updateCampaign));
            _setCampaignActivation = setCampaignActivation ?? throw new ArgumentNullException(nameof(setCampaignActivation));
            _getCampaignDeliveriesPage = getCampaignDeliveriesPage ?? throw new ArgumentNullException(nameof(getCampaignDeliveriesPage));
            _updateCampaignDeliveryStatus = updateCampaignDeliveryStatus ?? throw new ArgumentNullException(nameof(updateCampaignDeliveryStatus));
            _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
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
            await PopulateProgramOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderProgramEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProgram(LoyaltyProgramEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyProgramCreateFailed");
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateProgramOptionsAsync(vm, ct).ConfigureAwait(false);
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

                SetSuccessMessage("LoyaltyProgramCreated");
                return RedirectOrHtmx(nameof(EditProgram), new { id });
            }
            catch (ValidationException)
            {
                AddLocalizedModelError("LoyaltyProgramCreateFailed");
                await PopulateProgramOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderProgramEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProgram(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("LoyaltyProgramNotFound");
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            var dto = await _getProgramForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("LoyaltyProgramNotFound");
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
            await PopulateProgramOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderProgramEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProgram(LoyaltyProgramEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "LoyaltyProgramNotFound" : "LoyaltyProgramUpdateFailed");
                return RedirectOrHtmx(nameof(Programs), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulateProgramOptionsAsync(vm, ct).ConfigureAwait(false);
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

                SetSuccessMessage("LoyaltyProgramUpdated");
                return RedirectOrHtmx(nameof(EditProgram), new { id = vm.Id });
            }
            catch (ValidationException)
            {
                AddLocalizedModelError("LoyaltyProgramUpdateFailed");
                await PopulateProgramOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderProgramEditor(vm, isCreate: false);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgram(Guid id, Guid businessId, string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyProgramDeleteFailed");
                return RedirectOrHtmx(nameof(Programs), new { businessId = businessId == Guid.Empty ? (Guid?)null : businessId });
            }

            var result = await _deleteProgram.HandleAsync(new LoyaltyProgramDeleteDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? T("LoyaltyProgramDeleted") : T("LoyaltyProgramDeleteFailed");
            return RedirectOrHtmx(nameof(Programs), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> RewardTiers(Guid loyaltyProgramId, int page = 1, int pageSize = 20, LoyaltyRewardTierQueueFilter filter = LoyaltyRewardTierQueueFilter.All, CancellationToken ct = default)
        {
            if (loyaltyProgramId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyProgramNotFound");
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                SetErrorMessage("LoyaltyProgramNotFound");
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
            if (loyaltyProgramId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyProgramNotFound");
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                SetErrorMessage("LoyaltyProgramNotFound");
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
            if (vm.LoyaltyProgramId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyRewardTierCreateFailed");
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            if (!ModelState.IsValid)
            {
                if (!await TryPopulateRewardTierProgramContextAsync(vm, ct).ConfigureAwait(false))
                {
                    SetErrorMessage("LoyaltyProgramNotFound");
                    return RedirectOrHtmx(nameof(Programs), new { });
                }

                return RenderRewardTierEditor(vm, isCreate: true);
            }

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

                SetSuccessMessage("LoyaltyRewardTierCreated");
                return RedirectOrHtmx(nameof(EditRewardTier), new { id, loyaltyProgramId = vm.LoyaltyProgramId });
            }
            catch (ValidationException)
            {
                await TryPopulateRewardTierProgramContextAsync(vm, ct).ConfigureAwait(false);
                AddLocalizedModelError("LoyaltyRewardTierCreateFailed");
                return RenderRewardTierEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditRewardTier(Guid id, Guid loyaltyProgramId, CancellationToken ct = default)
        {
            if (id == Guid.Empty || loyaltyProgramId == Guid.Empty)
            {
                SetErrorMessage(id == Guid.Empty ? "LoyaltyRewardTierNotFound" : "LoyaltyProgramNotFound");
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                SetErrorMessage("LoyaltyProgramNotFound");
                return RedirectOrHtmx(nameof(Programs), new { });
            }

            var tier = await _getRewardTierForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (tier is null)
            {
                SetErrorMessage("LoyaltyRewardTierNotFound");
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
            if (vm.Id == Guid.Empty || vm.LoyaltyProgramId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "LoyaltyRewardTierNotFound" : "LoyaltyRewardTierUpdateFailed");
                return vm.LoyaltyProgramId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Programs), new { })
                    : RedirectOrHtmx(nameof(RewardTiers), new { loyaltyProgramId = vm.LoyaltyProgramId });
            }

            if (!ModelState.IsValid)
            {
                if (!await TryPopulateRewardTierProgramContextAsync(vm, ct).ConfigureAwait(false))
                {
                    SetErrorMessage("LoyaltyProgramNotFound");
                    return RedirectOrHtmx(nameof(Programs), new { });
                }

                return RenderRewardTierEditor(vm, isCreate: false);
            }

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

                SetSuccessMessage("LoyaltyRewardTierUpdated");
                return RedirectOrHtmx(nameof(EditRewardTier), new { id = vm.Id, loyaltyProgramId = vm.LoyaltyProgramId });
            }
            catch (ValidationException)
            {
                await TryPopulateRewardTierProgramContextAsync(vm, ct).ConfigureAwait(false);
                AddLocalizedModelError("LoyaltyRewardTierUpdateFailed");
                return RenderRewardTierEditor(vm, isCreate: false);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRewardTier(Guid id, Guid loyaltyProgramId, string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || loyaltyProgramId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyRewardTierDeleteFailed");
                return loyaltyProgramId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Programs), new { })
                    : RedirectOrHtmx(nameof(RewardTiers), new { loyaltyProgramId });
            }

            var result = await _deleteRewardTier.HandleAsync(new LoyaltyRewardTierDeleteDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? T("LoyaltyRewardTierDeleted") : T("LoyaltyRewardTierDeleteFailed");
            return RedirectOrHtmx(nameof(RewardTiers), new { loyaltyProgramId });
        }

        [HttpGet]
        public async Task<IActionResult> Accounts(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, LoyaltyAccountStatus? status = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = Array.Empty<LoyaltyAccountAdminListItemDto>();
            var total = 0;
            var summary = new LoyaltyAccountOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getAccountsPage.HandleAsync(businessId.Value, page, pageSize, q, status, ct).ConfigureAwait(false);
                var summaryDto = await _getAccountsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
                summary = new LoyaltyAccountOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    ActiveCount = summaryDto.ActiveCount,
                    SuspendedCount = summaryDto.SuspendedCount,
                    ZeroBalanceCount = summaryDto.ZeroBalanceCount,
                    RecentAccrualCount = summaryDto.RecentAccrualCount
                };
            }

            return RenderAccountsWorkspace(new LoyaltyAccountsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                StatusFilter = status,
                Summary = summary,
                Playbooks = BuildAccountPlaybooks(),
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
            await PopulateAccountCreateOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderAccountCreateEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(CreateLoyaltyAccountVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty || vm.UserId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyAccountCreateFailed");
                return RedirectOrHtmx(nameof(Accounts), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulateAccountCreateOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderAccountCreateEditor(vm);
            }

            var result = await _createAccountByAdmin.HandleAsync(new CreateLoyaltyAccountByAdminDto
            {
                BusinessId = vm.BusinessId,
                UserId = vm.UserId
            }, ct).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                AddLocalizedModelError("LoyaltyAccountCreateFailed", result.Error);
                await PopulateAccountCreateOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderAccountCreateEditor(vm);
            }

            SetSuccessMessage("LoyaltyAccountCreated");
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
                    SetLocalizedErrorMessage("LoyaltyCampaignsLoadFailed", result.Error);
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
            await PopulateCampaignOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderCampaignEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCampaign(LoyaltyCampaignEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyCampaignCreateFailed");
                return RedirectOrHtmx(nameof(Campaigns), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateCampaignOptionsAsync(vm, ct).ConfigureAwait(false);
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
                AddLocalizedModelError("LoyaltyCampaignCreateFailed", result.Error);
                await PopulateCampaignOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCampaignEditor(vm, isCreate: true);
            }

            SetSuccessMessage("LoyaltyCampaignCreated");
            return RedirectOrHtmx(nameof(EditCampaign), new { id = result.Value, businessId = vm.BusinessId });
        }

        [HttpGet]
        public async Task<IActionResult> EditCampaign(Guid id, Guid businessId, CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyCampaignNotFound");
                return RedirectOrHtmx(nameof(Campaigns), new { businessId = businessId == Guid.Empty ? (Guid?)null : businessId });
            }

            var result = await _getCampaigns.HandleAsync(businessId, 1, 200, LoyaltyCampaignQueueFilter.All, ct).ConfigureAwait(false);
            var campaign = result.Succeeded ? result.Value?.Items.FirstOrDefault(x => x.Id == id) : null;
            if (campaign is null)
            {
                SetErrorMessage("LoyaltyCampaignNotFound");
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
            await PopulateCampaignOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderCampaignEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCampaign(LoyaltyCampaignEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "LoyaltyCampaignNotFound" : "LoyaltyCampaignUpdateFailed");
                return RedirectOrHtmx(nameof(Campaigns), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulateCampaignOptionsAsync(vm, ct).ConfigureAwait(false);
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
                AddLocalizedModelError("LoyaltyCampaignUpdateFailed", result.Error);
                await PopulateCampaignOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCampaignEditor(vm, isCreate: false);
            }

            SetSuccessMessage("LoyaltyCampaignUpdated");
            return RedirectOrHtmx(nameof(EditCampaign), new { id = vm.Id, businessId = vm.BusinessId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCampaignActivation(Guid id, Guid businessId, bool isActive, string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyCampaignActivationFailed");
                return RedirectOrHtmx(nameof(Campaigns), new { businessId = businessId == Guid.Empty ? (Guid?)null : businessId });
            }

            var result = await _setCampaignActivation.HandleAsync(new SetCampaignActivationDto
            {
                Id = id,
                BusinessId = businessId,
                IsActive = isActive,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            SetLocalizedResultMessage(
                result.Succeeded,
                isActive ? "LoyaltyCampaignActivated" : "LoyaltyCampaignDeactivated",
                "LoyaltyCampaignActivationFailed",
                result.Error);

            return RedirectOrHtmx(nameof(Campaigns), new { businessId });
        }

        [HttpGet]
        public async Task<IActionResult> CampaignDeliveries(
            Guid? businessId = null,
            Guid? campaignId = null,
            int page = 1,
            int pageSize = 20,
            LoyaltyCampaignDeliveryQueueFilter filter = LoyaltyCampaignDeliveryQueueFilter.All,
            CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var result = await _getCampaignDeliveriesPage.HandleAsync(businessId, campaignId, page, pageSize, filter, ct).ConfigureAwait(false);
            var summaryDto = await _getCampaignDeliveriesPage.GetSummaryAsync(businessId, campaignId, ct).ConfigureAwait(false);
            var items = result.Succeeded && result.Value is not null
                ? result.Value.Items
                : new List<CampaignDeliveryItemDto>();

            if (!result.Succeeded)
            {
                SetLocalizedErrorMessage("LoyaltyCampaignDeliveriesLoadFailed", result.Error);
            }

            return RenderCampaignDeliveriesWorkspace(new LoyaltyCampaignDeliveriesListVm
            {
                BusinessId = businessId,
                CampaignId = campaignId,
                Filter = filter,
                FilterItems = BuildCampaignDeliveryFilterItems(filter),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Summary = new LoyaltyCampaignDeliveryOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    PendingCount = summaryDto.PendingCount,
                    InProgressCount = summaryDto.InProgressCount,
                    FailedCount = summaryDto.FailedCount,
                    SucceededCount = summaryDto.SucceededCount,
                    CancelledCount = summaryDto.CancelledCount,
                    NeedsAttentionCount = summaryDto.NeedsAttentionCount
                },
                Page = page,
                PageSize = pageSize,
                Total = result.Value?.Total ?? 0,
                Items = items.Select(x => new LoyaltyCampaignDeliveryListItemVm
                {
                    Id = x.Id,
                    CampaignId = x.CampaignId,
                    CampaignName = x.CampaignName,
                    CampaignTitle = x.CampaignTitle,
                    RecipientUserId = x.RecipientUserId,
                    BusinessId = x.BusinessId,
                    Channel = (CampaignDeliveryChannel)x.Channel,
                    Status = (CampaignDeliveryStatus)x.Status,
                    Destination = x.Destination,
                    AttemptCount = x.AttemptCount,
                    FirstAttemptAtUtc = x.FirstAttemptAtUtc,
                    LastAttemptAtUtc = x.LastAttemptAtUtc,
                    LastResponseCode = x.LastResponseCode,
                    ProviderMessageId = x.ProviderMessageId,
                    LastError = x.LastError,
                    IdempotencyKey = x.IdempotencyKey,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCampaignDeliveryStatus(
            Guid id,
            Guid? businessId,
            Guid? campaignId,
            CampaignDeliveryStatus status,
            string? rowVersion,
            int page = 1,
            int pageSize = 20,
            LoyaltyCampaignDeliveryQueueFilter filter = LoyaltyCampaignDeliveryQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("CampaignDeliveryStatusUpdateFailed");
                return RedirectOrHtmx(nameof(CampaignDeliveries), new { businessId, campaignId, page, pageSize, filter });
            }

            if (!Enum.IsDefined(status))
            {
                SetErrorMessage("CampaignDeliveryStatusUpdateFailed");
                return RedirectOrHtmx(nameof(CampaignDeliveries), new { businessId, campaignId, page, pageSize, filter });
            }

            var result = await _updateCampaignDeliveryStatus.HandleAsync(new UpdateCampaignDeliveryStatusDto
            {
                Id = id,
                BusinessId = businessId,
                Status = (short)status,
                OperatorNote = T("CampaignDeliveryUpdatedByOperator"),
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            SetLocalizedResultMessage(result.Succeeded, "CampaignDeliveryStatusUpdated", "CampaignDeliveryStatusUpdateFailed", result.Error);
            return RedirectOrHtmx(nameof(CampaignDeliveries), new { businessId, campaignId, page, pageSize, filter });
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
            var summary = new LoyaltyScanSessionOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getScanSessionsPage.HandleAsync(businessId.Value, page, pageSize, q, mode, status, ct).ConfigureAwait(false);
                var summaryDto = await _getScanSessionsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
                summary = new LoyaltyScanSessionOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    AccrualCount = summaryDto.AccrualCount,
                    RedemptionCount = summaryDto.RedemptionCount,
                    PendingCount = summaryDto.PendingCount,
                    ExpiredCount = summaryDto.ExpiredCount,
                    FailureCount = summaryDto.FailureCount
                };
            }

            return RenderScanSessionsWorkspace(new LoyaltyScanSessionsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                ModeFilter = mode,
                StatusFilter = status,
                Summary = summary,
                Playbooks = BuildScanSessionPlaybooks(),
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
                    CompletedAtUtc = x.CompletedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpireScanSession(Guid id, Guid businessId, string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty || businessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyScanSessionExpireFailed");
                return RedirectOrHtmx(nameof(ScanSessions), new { businessId = businessId == Guid.Empty ? (Guid?)null : businessId });
            }

            var result = await _expireScanSession.HandleAsync(new ExpireLoyaltyScanSessionDto
            {
                Id = id,
                BusinessId = businessId,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            SetLocalizedResultMessage(result.Succeeded, "LoyaltyScanSessionExpired", "LoyaltyScanSessionExpireFailed", result.Error);
            return RedirectOrHtmx(nameof(ScanSessions), new { businessId, status = LoyaltyScanStatus.Expired });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpireExpiredScanSessions(Guid businessId, CancellationToken ct = default)
        {
            if (businessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyScanSessionExpireFailed");
                return RedirectOrHtmx(nameof(ScanSessions), new { });
            }

            var result = await _expireScanSession.HandleExpiredAsync(new ExpireExpiredLoyaltyScanSessionsDto
            {
                BusinessId = businessId
            }, ct).ConfigureAwait(false);

            SetLocalizedResultMessage(result.Succeeded, "LoyaltyScanSessionsExpired", "LoyaltyScanSessionExpireFailed", result.Error);
            return RedirectOrHtmx(nameof(ScanSessions), new { businessId, status = LoyaltyScanStatus.Expired });
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
            var summary = new LoyaltyRedemptionOpsSummaryVm();
            if (businessId.HasValue)
            {
                var result = await _getRedemptionsPage.HandleAsync(businessId.Value, page, pageSize, q, status, ct).ConfigureAwait(false);
                var summaryDto = await _getRedemptionsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);
                items = result.Items.ToArray();
                total = result.Total;
                summary = new LoyaltyRedemptionOpsSummaryVm
                {
                    TotalCount = summaryDto.TotalCount,
                    PendingCount = summaryDto.PendingCount,
                    CompletedCount = summaryDto.CompletedCount,
                    CancelledCount = summaryDto.CancelledCount,
                    ScanFailureCount = summaryDto.ScanFailureCount
                };
            }

            return RenderRedemptionsWorkspace(new LoyaltyRedemptionsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                StatusFilter = status,
                Summary = summary,
                Playbooks = BuildRedemptionPlaybooks(),
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
            if (id == Guid.Empty)
            {
                SetErrorMessage("LoyaltyAccountNotFound");
                return RedirectOrHtmx(nameof(Accounts), new { });
            }

            var account = await _getAccountForAdmin.HandleAsync(id, ct).ConfigureAwait(false);
            if (account is null)
            {
                SetErrorMessage("LoyaltyAccountNotFound");
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
            if (loyaltyAccountId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyAccountNotFound");
                return RedirectOrHtmx(nameof(Accounts), new { });
            }

            var account = await _getAccountForAdmin.HandleAsync(loyaltyAccountId, ct).ConfigureAwait(false);
            if (account is null)
            {
                SetErrorMessage("LoyaltyAccountNotFound");
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
            if (vm.LoyaltyAccountId == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyPointsAdjustFailed");
                return RedirectOrHtmx(nameof(Accounts), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                if (!await TryPopulateAdjustPointsAccountContextAsync(vm, ct).ConfigureAwait(false))
                {
                    SetErrorMessage("LoyaltyAccountNotFound");
                    return RedirectOrHtmx(nameof(Accounts), new { businessId = vm.BusinessId });
                }

                return RenderAdjustPointsEditor(vm);
            }

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

                SetSuccessMessage("LoyaltyPointsAdjusted");
                return RedirectOrHtmx(nameof(AccountDetails), new { id = vm.LoyaltyAccountId });
            }
            catch (ValidationException)
            {
                await TryPopulateAdjustPointsAccountContextAsync(vm, ct).ConfigureAwait(false);
                AddLocalizedModelError("LoyaltyPointsAdjustFailed");
                return RenderAdjustPointsEditor(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendAccount(Guid id, string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("LoyaltyAccountSuspendFailed");
                return RedirectOrHtmx(nameof(Accounts), new { });
            }

            var result = await _suspendAccount.HandleAsync(new SuspendLoyaltyAccountDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            SetLocalizedResultMessage(result.Succeeded, "LoyaltyAccountSuspended", "LoyaltyAccountSuspendFailed", result.Error);
            return RedirectOrHtmx(nameof(AccountDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateAccount(Guid id, string? rowVersion, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("LoyaltyAccountActivateFailed");
                return RedirectOrHtmx(nameof(Accounts), new { });
            }

            var result = await _activateAccount.HandleAsync(new ActivateLoyaltyAccountDto
            {
                Id = id,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            SetLocalizedResultMessage(result.Succeeded, "LoyaltyAccountActivated", "LoyaltyAccountActivateFailed", result.Error);
            return RedirectOrHtmx(nameof(AccountDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRedemption(Guid redemptionId, Guid businessId, Guid loyaltyAccountId, string? rowVersion, CancellationToken ct = default)
        {
            if (redemptionId == Guid.Empty || businessId == Guid.Empty || loyaltyAccountId == Guid.Empty)
            {
                SetErrorMessage("LoyaltyRedemptionConfirmFailed");
                return loyaltyAccountId == Guid.Empty
                    ? RedirectOrHtmx(nameof(Redemptions), new { businessId = businessId == Guid.Empty ? (Guid?)null : businessId })
                    : RedirectOrHtmx(nameof(AccountDetails), new { id = loyaltyAccountId });
            }

            var result = await _confirmRedemption.HandleAsync(new ConfirmLoyaltyRewardRedemptionDto
            {
                RedemptionId = redemptionId,
                BusinessId = businessId,
                RowVersion = DecodeBase64RowVersion(rowVersion)
            }, ct).ConfigureAwait(false);

            SetLocalizedResultMessage(result.Succeeded, "LoyaltyRedemptionConfirmed", "LoyaltyRedemptionConfirmFailed", result.Error);

            return RedirectOrHtmx(nameof(AccountDetails), new { id = loyaltyAccountId });
        }

        private List<SelectListItem> BuildStatusItems(LoyaltyAccountStatus? selected)
        {
            var items = new List<SelectListItem>
            {
                new(T("AllStatuses"), string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyAccountStatus>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x)));

            return items;
        }

        private List<SelectListItem> BuildProgramFilterItems(LoyaltyProgramQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new(T("LoyaltyAllPrograms"), LoyaltyProgramQueueFilter.All.ToString(), selected == LoyaltyProgramQueueFilter.All),
                new(T("Active"), LoyaltyProgramQueueFilter.Active.ToString(), selected == LoyaltyProgramQueueFilter.Active),
                new(T("Inactive"), LoyaltyProgramQueueFilter.Inactive.ToString(), selected == LoyaltyProgramQueueFilter.Inactive),
                new(T("LoyaltySpendBasedAccrual"), LoyaltyProgramQueueFilter.PerCurrencyUnit.ToString(), selected == LoyaltyProgramQueueFilter.PerCurrencyUnit),
                new(T("LoyaltyMissingRules"), LoyaltyProgramQueueFilter.MissingRules.ToString(), selected == LoyaltyProgramQueueFilter.MissingRules)
            };
        }

        private List<SelectListItem> BuildRewardTierFilterItems(LoyaltyRewardTierQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new(T("LoyaltyAllRewardTiers"), LoyaltyRewardTierQueueFilter.All.ToString(), selected == LoyaltyRewardTierQueueFilter.All),
                new(T("LoyaltySelfRedemption"), LoyaltyRewardTierQueueFilter.SelfRedemption.ToString(), selected == LoyaltyRewardTierQueueFilter.SelfRedemption),
                new(T("MissingDescription"), LoyaltyRewardTierQueueFilter.MissingDescription.ToString(), selected == LoyaltyRewardTierQueueFilter.MissingDescription),
                new(T("LoyaltyDiscountRewards"), LoyaltyRewardTierQueueFilter.DiscountRewards.ToString(), selected == LoyaltyRewardTierQueueFilter.DiscountRewards),
                new(T("LoyaltyFreeItem"), LoyaltyRewardTierQueueFilter.FreeItem.ToString(), selected == LoyaltyRewardTierQueueFilter.FreeItem)
            };
        }

        private List<SelectListItem> BuildCampaignFilterItems(LoyaltyCampaignQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new(T("LoyaltyAllCampaigns"), LoyaltyCampaignQueueFilter.All.ToString(), selected == LoyaltyCampaignQueueFilter.All),
                new(T("Active"), LoyaltyCampaignQueueFilter.Active.ToString(), selected == LoyaltyCampaignQueueFilter.Active),
                new(T("Scheduled"), LoyaltyCampaignQueueFilter.Scheduled.ToString(), selected == LoyaltyCampaignQueueFilter.Scheduled),
                new(T("Draft"), LoyaltyCampaignQueueFilter.Draft.ToString(), selected == LoyaltyCampaignQueueFilter.Draft),
                new(T("Expired"), LoyaltyCampaignQueueFilter.Expired.ToString(), selected == LoyaltyCampaignQueueFilter.Expired),
                new(T("LoyaltyPushEnabled"), LoyaltyCampaignQueueFilter.PushEnabled.ToString(), selected == LoyaltyCampaignQueueFilter.PushEnabled)
            };
        }

        private List<SelectListItem> BuildCampaignDeliveryFilterItems(LoyaltyCampaignDeliveryQueueFilter selected)
        {
            return new List<SelectListItem>
            {
                new(T("AllStatuses"), LoyaltyCampaignDeliveryQueueFilter.All.ToString(), selected == LoyaltyCampaignDeliveryQueueFilter.All),
                new(T("Pending"), LoyaltyCampaignDeliveryQueueFilter.Pending.ToString(), selected == LoyaltyCampaignDeliveryQueueFilter.Pending),
                new(T("InProgress"), LoyaltyCampaignDeliveryQueueFilter.InProgress.ToString(), selected == LoyaltyCampaignDeliveryQueueFilter.InProgress),
                new(T("Failed"), LoyaltyCampaignDeliveryQueueFilter.Failed.ToString(), selected == LoyaltyCampaignDeliveryQueueFilter.Failed),
                new(T("Succeeded"), LoyaltyCampaignDeliveryQueueFilter.Succeeded.ToString(), selected == LoyaltyCampaignDeliveryQueueFilter.Succeeded),
                new(T("Cancelled"), LoyaltyCampaignDeliveryQueueFilter.Cancelled.ToString(), selected == LoyaltyCampaignDeliveryQueueFilter.Cancelled),
                new(T("NeedsAttention"), LoyaltyCampaignDeliveryQueueFilter.NeedsAttention.ToString(), selected == LoyaltyCampaignDeliveryQueueFilter.NeedsAttention)
            };
        }

        private List<LoyaltyOpsPlaybookVm> BuildProgramPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = T("LoyaltyProgramPlaybookReadinessTitle"),
                    ScopeNote = T("LoyaltyProgramPlaybookReadinessScope"),
                    OperatorAction = T("LoyaltyProgramPlaybookReadinessAction")
                },
                new()
                {
                    Title = T("LoyaltyProgramPlaybookSpendTitle"),
                    ScopeNote = T("LoyaltyProgramPlaybookSpendScope"),
                    OperatorAction = T("LoyaltyProgramPlaybookSpendAction")
                }
            };
        }

        private List<LoyaltyOpsPlaybookVm> BuildRewardTierPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = T("LoyaltyRewardTierPlaybookSelfTitle"),
                    ScopeNote = T("LoyaltyRewardTierPlaybookSelfScope"),
                    OperatorAction = T("LoyaltyRewardTierPlaybookSelfAction")
                },
                new()
                {
                    Title = T("LoyaltyRewardTierPlaybookCatalogTitle"),
                    ScopeNote = T("LoyaltyRewardTierPlaybookCatalogScope"),
                    OperatorAction = T("LoyaltyRewardTierPlaybookCatalogAction")
                }
            };
        }

        private List<LoyaltyOpsPlaybookVm> BuildCampaignPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = T("LoyaltyCampaignPlaybookWindowTitle"),
                    ScopeNote = T("LoyaltyCampaignPlaybookWindowScope"),
                    OperatorAction = T("LoyaltyCampaignPlaybookWindowAction")
                },
                new()
                {
                    Title = T("LoyaltyCampaignPlaybookPushTitle"),
                    ScopeNote = T("LoyaltyCampaignPlaybookPushScope"),
                    OperatorAction = T("LoyaltyCampaignPlaybookPushAction")
                }
            };
        }
        private List<LoyaltyOpsPlaybookVm> BuildAccountPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = T("LoyaltyAccountPlaybookSuspendedTitle"),
                    ScopeNote = T("LoyaltyAccountPlaybookSuspendedScope"),
                    OperatorAction = T("LoyaltyAccountPlaybookSuspendedAction")
                },
                new()
                {
                    Title = T("LoyaltyAccountPlaybookZeroBalanceTitle"),
                    ScopeNote = T("LoyaltyAccountPlaybookZeroBalanceScope"),
                    OperatorAction = T("LoyaltyAccountPlaybookZeroBalanceAction")
                }
            };
        }

        private List<LoyaltyOpsPlaybookVm> BuildScanSessionPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = T("LoyaltyScanPlaybookPendingTitle"),
                    ScopeNote = T("LoyaltyScanPlaybookPendingScope"),
                    OperatorAction = T("LoyaltyScanPlaybookPendingAction")
                },
                new()
                {
                    Title = T("LoyaltyScanPlaybookFailureTitle"),
                    ScopeNote = T("LoyaltyScanPlaybookFailureScope"),
                    OperatorAction = T("LoyaltyScanPlaybookFailureAction")
                },
                new()
                {
                    Title = T("LoyaltyScanPlaybookProviderTitle"),
                    ScopeNote = T("LoyaltyScanPlaybookProviderScope"),
                    OperatorAction = T("LoyaltyScanPlaybookProviderAction")
                }
            };
        }

        private List<LoyaltyOpsPlaybookVm> BuildRedemptionPlaybooks()
        {
            return new List<LoyaltyOpsPlaybookVm>
            {
                new()
                {
                    Title = T("LoyaltyRedemptionPlaybookPendingTitle"),
                    ScopeNote = T("LoyaltyRedemptionPlaybookPendingScope"),
                    OperatorAction = T("LoyaltyRedemptionPlaybookPendingAction")
                },
                new()
                {
                    Title = T("LoyaltyRedemptionPlaybookScanFailureTitle"),
                    ScopeNote = T("LoyaltyRedemptionPlaybookScanFailureScope"),
                    OperatorAction = T("LoyaltyRedemptionPlaybookScanFailureAction")
                },
                new()
                {
                    Title = T("LoyaltyRedemptionPlaybookMobileTitle"),
                    ScopeNote = T("LoyaltyRedemptionPlaybookMobileScope"),
                    OperatorAction = T("LoyaltyRedemptionPlaybookMobileAction")
                }
            };
        }

        private List<SelectListItem> BuildModeItems(LoyaltyScanMode? selected)
        {
            var items = new List<SelectListItem>
            {
                new(T("LoyaltyAllModes"), string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyScanMode>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x)));

            return items;
        }

        private List<SelectListItem> BuildScanStatusItems(LoyaltyScanStatus? selected)
        {
            var items = new List<SelectListItem>
            {
                new(T("AllStatuses"), string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyScanStatus>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x)));

            return items;
        }

        private List<SelectListItem> BuildRedemptionStatusItems(LoyaltyRedemptionStatus? selected)
        {
            var items = new List<SelectListItem>
            {
                new(T("AllStatuses"), string.Empty, !selected.HasValue)
            };

            items.AddRange(Enum.GetValues<LoyaltyRedemptionStatus>()
                .Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x)));

            return items;
        }

        private async Task PopulateProgramOptionsAsync(LoyaltyProgramEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        }

        private async Task PopulateAccountCreateOptionsAsync(CreateLoyaltyAccountVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId == Guid.Empty ? null : vm.UserId, includeEmpty: false, ct).ConfigureAwait(false);
        }

        private async Task PopulateCampaignOptionsAsync(LoyaltyCampaignEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        }

        private async Task<bool> TryPopulateRewardTierProgramContextAsync(LoyaltyRewardTierEditVm vm, CancellationToken ct)
        {
            var program = await _getProgramForEdit.HandleAsync(vm.LoyaltyProgramId, ct).ConfigureAwait(false);
            if (program is null)
            {
                return false;
            }

            vm.ProgramName = program.Name;
            vm.BusinessId = program.BusinessId;
            return true;
        }

        private async Task<bool> TryPopulateAdjustPointsAccountContextAsync(AdjustLoyaltyPointsVm vm, CancellationToken ct)
        {
            var account = await _getAccountForAdmin.HandleAsync(vm.LoyaltyAccountId, ct).ConfigureAwait(false);
            if (account is null)
            {
                return false;
            }

            vm.BusinessId = account.BusinessId;
            vm.UserId = account.UserId;
            vm.AccountLabel = $"{account.UserDisplayName} ({account.UserEmail})";
            vm.RowVersion ??= account.RowVersion;
            return true;
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

        private IActionResult RenderCampaignDeliveriesWorkspace(LoyaltyCampaignDeliveriesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Loyalty/CampaignDeliveries.cshtml", vm);
            }

            return View("CampaignDeliveries", vm);
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

        private void AddLocalizedModelError(string fallbackKey, string? error = null)
        {
            ModelState.AddModelError(string.Empty, T(fallbackKey));
        }

        private void SetLocalizedErrorMessage(string fallbackKey, string? error = null)
        {
            TempData["Error"] = T(fallbackKey);
        }

        private void SetLocalizedResultMessage(bool succeeded, string successKey, string failureKey, string? error = null)
        {
            if (succeeded)
            {
                SetSuccessMessage(successKey);
                return;
            }

            SetLocalizedErrorMessage(failureKey, error);
        }
    }
}



