using Darwin.Application.Abstractions.Services;
using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Billing;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Darwin.WebAdmin.Controllers.Admin.Billing
{
    /// <summary>
    /// Admin billing controller for operational finance screens.
    /// </summary>
    public sealed class BillingController : AdminBaseController
    {
        private readonly GetPaymentsPageHandler _getPaymentsPage;
        private readonly GetPaymentOpsSummaryHandler _getPaymentOpsSummary;
        private readonly GetTaxComplianceOverviewHandler _getTaxComplianceOverview;
        private readonly GetTaxComplianceInvoiceExportHandler _getTaxComplianceInvoiceExport;
        private readonly UpdateCustomerTaxProfileHandler _updateCustomerTaxProfile;
        private readonly UpdateCustomerVatValidationDecisionHandler _updateCustomerVatValidationDecision;
        private readonly ValidateCustomerVatIdHandler _validateCustomerVatId;
        private readonly UpdateInvoiceReverseChargeDecisionHandler _updateInvoiceReverseChargeDecision;
        private readonly GetBillingPlansAdminPageHandler _getBillingPlansAdminPage;
        private readonly GetBillingPlanOpsSummaryHandler _getBillingPlanOpsSummary;
        private readonly GetBillingPlanForEditHandler _getBillingPlanForEdit;
        private readonly GetBusinessSubscriptionsPageHandler _getBusinessSubscriptionsPage;
        private readonly GetBusinessSubscriptionOpsSummaryHandler _getBusinessSubscriptionOpsSummary;
        private readonly GetBillingWebhookSubscriptionsPageHandler _getBillingWebhookSubscriptionsPage;
        private readonly GetBillingWebhookDeliveriesPageHandler _getBillingWebhookDeliveriesPage;
        private readonly GetBillingWebhookOpsSummaryHandler _getBillingWebhookOpsSummary;
        private readonly UpdateBillingWebhookDeliveryHandler _updateBillingWebhookDelivery;
        private readonly UpdatePaymentDisputeReviewHandler _updatePaymentDisputeReview;
        private readonly GetPaymentForEditHandler _getPaymentForEdit;
        private readonly GetRefundsPageHandler _getRefundsPage;
        private readonly GetRefundOpsSummaryHandler _getRefundOpsSummary;
        private readonly CreateBillingPlanHandler _createBillingPlan;
        private readonly UpdateBillingPlanHandler _updateBillingPlan;
        private readonly CreatePaymentHandler _createPayment;
        private readonly UpdatePaymentHandler _updatePayment;
        private readonly GetFinancialAccountsPageHandler _getAccountsPage;
        private readonly GetFinancialAccountForEditHandler _getAccountForEdit;
        private readonly CreateFinancialAccountHandler _createAccount;
        private readonly UpdateFinancialAccountHandler _updateAccount;
        private readonly GetExpensesPageHandler _getExpensesPage;
        private readonly GetExpenseForEditHandler _getExpenseForEdit;
        private readonly CreateExpenseHandler _createExpense;
        private readonly UpdateExpenseHandler _updateExpense;
        private readonly GetJournalEntriesPageHandler _getJournalEntriesPage;
        private readonly GetJournalEntryForEditHandler _getJournalEntryForEdit;
        private readonly CreateJournalEntryHandler _createJournalEntry;
        private readonly UpdateJournalEntryHandler _updateJournalEntry;
        private readonly AdminReferenceDataService _referenceData;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IClock _clock;

        public BillingController(
            GetPaymentsPageHandler getPaymentsPage,
            GetPaymentOpsSummaryHandler getPaymentOpsSummary,
            GetTaxComplianceOverviewHandler getTaxComplianceOverview,
            GetTaxComplianceInvoiceExportHandler getTaxComplianceInvoiceExport,
            UpdateCustomerTaxProfileHandler updateCustomerTaxProfile,
            UpdateCustomerVatValidationDecisionHandler updateCustomerVatValidationDecision,
            ValidateCustomerVatIdHandler validateCustomerVatId,
            UpdateInvoiceReverseChargeDecisionHandler updateInvoiceReverseChargeDecision,
            GetBillingPlansAdminPageHandler getBillingPlansAdminPage,
            GetBillingPlanOpsSummaryHandler getBillingPlanOpsSummary,
            GetBillingPlanForEditHandler getBillingPlanForEdit,
            GetBusinessSubscriptionsPageHandler getBusinessSubscriptionsPage,
            GetBusinessSubscriptionOpsSummaryHandler getBusinessSubscriptionOpsSummary,
            GetBillingWebhookSubscriptionsPageHandler getBillingWebhookSubscriptionsPage,
            GetBillingWebhookDeliveriesPageHandler getBillingWebhookDeliveriesPage,
            GetBillingWebhookOpsSummaryHandler getBillingWebhookOpsSummary,
            UpdateBillingWebhookDeliveryHandler updateBillingWebhookDelivery,
            UpdatePaymentDisputeReviewHandler updatePaymentDisputeReview,
            GetPaymentForEditHandler getPaymentForEdit,
            GetRefundsPageHandler getRefundsPage,
            GetRefundOpsSummaryHandler getRefundOpsSummary,
            CreateBillingPlanHandler createBillingPlan,
            UpdateBillingPlanHandler updateBillingPlan,
            CreatePaymentHandler createPayment,
            UpdatePaymentHandler updatePayment,
            GetFinancialAccountsPageHandler getAccountsPage,
            GetFinancialAccountForEditHandler getAccountForEdit,
            CreateFinancialAccountHandler createAccount,
            UpdateFinancialAccountHandler updateAccount,
            GetExpensesPageHandler getExpensesPage,
            GetExpenseForEditHandler getExpenseForEdit,
            CreateExpenseHandler createExpense,
            UpdateExpenseHandler updateExpense,
            GetJournalEntriesPageHandler getJournalEntriesPage,
            GetJournalEntryForEditHandler getJournalEntryForEdit,
            CreateJournalEntryHandler createJournalEntry,
            UpdateJournalEntryHandler updateJournalEntry,
            AdminReferenceDataService referenceData,
            ISiteSettingCache siteSettingCache,
            IClock clock)
        {
            _getPaymentsPage = getPaymentsPage ?? throw new ArgumentNullException(nameof(getPaymentsPage));
            _getPaymentOpsSummary = getPaymentOpsSummary ?? throw new ArgumentNullException(nameof(getPaymentOpsSummary));
            _getTaxComplianceOverview = getTaxComplianceOverview ?? throw new ArgumentNullException(nameof(getTaxComplianceOverview));
            _getTaxComplianceInvoiceExport = getTaxComplianceInvoiceExport ?? throw new ArgumentNullException(nameof(getTaxComplianceInvoiceExport));
            _updateCustomerTaxProfile = updateCustomerTaxProfile ?? throw new ArgumentNullException(nameof(updateCustomerTaxProfile));
            _updateCustomerVatValidationDecision = updateCustomerVatValidationDecision ?? throw new ArgumentNullException(nameof(updateCustomerVatValidationDecision));
            _validateCustomerVatId = validateCustomerVatId ?? throw new ArgumentNullException(nameof(validateCustomerVatId));
            _updateInvoiceReverseChargeDecision = updateInvoiceReverseChargeDecision ?? throw new ArgumentNullException(nameof(updateInvoiceReverseChargeDecision));
            _getBillingPlansAdminPage = getBillingPlansAdminPage ?? throw new ArgumentNullException(nameof(getBillingPlansAdminPage));
            _getBillingPlanOpsSummary = getBillingPlanOpsSummary ?? throw new ArgumentNullException(nameof(getBillingPlanOpsSummary));
            _getBillingPlanForEdit = getBillingPlanForEdit ?? throw new ArgumentNullException(nameof(getBillingPlanForEdit));
            _getBusinessSubscriptionsPage = getBusinessSubscriptionsPage ?? throw new ArgumentNullException(nameof(getBusinessSubscriptionsPage));
            _getBusinessSubscriptionOpsSummary = getBusinessSubscriptionOpsSummary ?? throw new ArgumentNullException(nameof(getBusinessSubscriptionOpsSummary));
            _getBillingWebhookSubscriptionsPage = getBillingWebhookSubscriptionsPage ?? throw new ArgumentNullException(nameof(getBillingWebhookSubscriptionsPage));
            _getBillingWebhookDeliveriesPage = getBillingWebhookDeliveriesPage ?? throw new ArgumentNullException(nameof(getBillingWebhookDeliveriesPage));
            _getBillingWebhookOpsSummary = getBillingWebhookOpsSummary ?? throw new ArgumentNullException(nameof(getBillingWebhookOpsSummary));
            _updateBillingWebhookDelivery = updateBillingWebhookDelivery ?? throw new ArgumentNullException(nameof(updateBillingWebhookDelivery));
            _updatePaymentDisputeReview = updatePaymentDisputeReview ?? throw new ArgumentNullException(nameof(updatePaymentDisputeReview));
            _getPaymentForEdit = getPaymentForEdit ?? throw new ArgumentNullException(nameof(getPaymentForEdit));
            _getRefundsPage = getRefundsPage ?? throw new ArgumentNullException(nameof(getRefundsPage));
            _getRefundOpsSummary = getRefundOpsSummary ?? throw new ArgumentNullException(nameof(getRefundOpsSummary));
            _createBillingPlan = createBillingPlan ?? throw new ArgumentNullException(nameof(createBillingPlan));
            _updateBillingPlan = updateBillingPlan ?? throw new ArgumentNullException(nameof(updateBillingPlan));
            _createPayment = createPayment ?? throw new ArgumentNullException(nameof(createPayment));
            _updatePayment = updatePayment ?? throw new ArgumentNullException(nameof(updatePayment));
            _getAccountsPage = getAccountsPage ?? throw new ArgumentNullException(nameof(getAccountsPage));
            _getAccountForEdit = getAccountForEdit ?? throw new ArgumentNullException(nameof(getAccountForEdit));
            _createAccount = createAccount ?? throw new ArgumentNullException(nameof(createAccount));
            _updateAccount = updateAccount ?? throw new ArgumentNullException(nameof(updateAccount));
            _getExpensesPage = getExpensesPage ?? throw new ArgumentNullException(nameof(getExpensesPage));
            _getExpenseForEdit = getExpenseForEdit ?? throw new ArgumentNullException(nameof(getExpenseForEdit));
            _createExpense = createExpense ?? throw new ArgumentNullException(nameof(createExpense));
            _updateExpense = updateExpense ?? throw new ArgumentNullException(nameof(updateExpense));
            _getJournalEntriesPage = getJournalEntriesPage ?? throw new ArgumentNullException(nameof(getJournalEntriesPage));
            _getJournalEntryForEdit = getJournalEntryForEdit ?? throw new ArgumentNullException(nameof(getJournalEntryForEdit));
            _createJournalEntry = createJournalEntry ?? throw new ArgumentNullException(nameof(createJournalEntry));
            _updateJournalEntry = updateJournalEntry ?? throw new ArgumentNullException(nameof(updateJournalEntry));
            _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        [HttpGet]
        public IActionResult Index() => RedirectOrHtmx(nameof(Payments), new { });

        [HttpGet]
        public async Task<IActionResult> Plans(int page = 1, int pageSize = 20, string? q = null, BillingPlanQueueFilter queue = BillingPlanQueueFilter.All, CancellationToken ct = default)
        {
            var result = await _getBillingPlansAdminPage.HandleAsync(page, pageSize, q, queue, ct).ConfigureAwait(false);

            var vm = new BillingPlansListVm
            {
                Query = q ?? string.Empty,
                QueueFilter = queue,
                Summary = await BuildBillingPlanOpsSummaryVmAsync(ct).ConfigureAwait(false),
                Playbooks = BuildBillingPlanPlaybooks(),
                Page = page,
                PageSize = pageSize,
                Total = result.Total,
                Items = result.Items.Select(x => new BillingPlanListItemVm
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    Description = x.Description,
                    PriceMinor = x.PriceMinor,
                    Currency = x.Currency,
                    Interval = x.Interval,
                    IntervalCount = x.IntervalCount,
                    TrialDays = x.TrialDays,
                    IsActive = x.IsActive,
                    HasFeatures = x.HasFeatures,
                    ActiveSubscriptionCount = x.ActiveSubscriptionCount,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderPlansWorkspace(vm);
        }

        [HttpGet]
        public IActionResult CreatePlan()
        {
            var vm = new BillingPlanEditVm();
            PopulateBillingPlanOptions(vm);
            return RenderBillingPlanEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(BillingPlanEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                PopulateBillingPlanOptions(vm);
                return RenderBillingPlanEditor(vm, isCreate: true);
            }

            var dto = new BillingPlanCreateDto
            {
                Code = vm.Code,
                Name = vm.Name,
                Description = vm.Description,
                PriceMinor = vm.PriceMinor,
                Currency = vm.Currency,
                Interval = vm.Interval,
                IntervalCount = vm.IntervalCount,
                TrialDays = vm.TrialDays,
                IsActive = vm.IsActive,
                FeaturesJson = vm.FeaturesJson
            };

            try
            {
                var id = await _createBillingPlan.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("BillingPlanCreatedMessage");
                return RedirectOrHtmx(nameof(EditPlan), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BillingPlanCreateFailedMessage");
                PopulateBillingPlanOptions(vm);
                return RenderBillingPlanEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPlan(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("BillingPlanNotFoundMessage");
                return RedirectOrHtmx(nameof(Plans), new { });
            }

            var dto = await _getBillingPlanForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("BillingPlanNotFoundMessage");
                return RedirectOrHtmx(nameof(Plans), new { });
            }

            var vm = new BillingPlanEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Code = dto.Code,
                Name = dto.Name,
                Description = dto.Description,
                PriceMinor = dto.PriceMinor,
                Currency = dto.Currency,
                Interval = dto.Interval,
                IntervalCount = dto.IntervalCount,
                TrialDays = dto.TrialDays,
                IsActive = dto.IsActive,
                FeaturesJson = dto.FeaturesJson
            };
            PopulateBillingPlanOptions(vm);
            vm.ActiveSubscriptionCount = (await _getBillingPlansAdminPage.HandleAsync(1, 1, dto.Code, BillingPlanQueueFilter.All, ct).ConfigureAwait(false))
                .Items.FirstOrDefault(x => x.Id == id)?.ActiveSubscriptionCount ?? 0;
            return RenderBillingPlanEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPlan(BillingPlanEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("BillingPlanNotFoundMessage");
                return RedirectOrHtmx(nameof(Plans), new { });
            }

            if (!ModelState.IsValid)
            {
                PopulateBillingPlanOptions(vm);
                return RenderBillingPlanEditor(vm, isCreate: false);
            }

            var dto = new BillingPlanEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                Code = vm.Code,
                Name = vm.Name,
                Description = vm.Description,
                PriceMinor = vm.PriceMinor,
                Currency = vm.Currency,
                Interval = vm.Interval,
                IntervalCount = vm.IntervalCount,
                TrialDays = vm.TrialDays,
                IsActive = vm.IsActive,
                FeaturesJson = vm.FeaturesJson
            };

            try
            {
                await _updateBillingPlan.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("BillingPlanUpdatedMessage");
                return RedirectOrHtmx(nameof(EditPlan), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("BillingPlanConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditPlan), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("BillingPlanUpdateFailedMessage");
                PopulateBillingPlanOptions(vm);
                return RenderBillingPlanEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Subscriptions(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            BusinessSubscriptionQueueFilter queue = BusinessSubscriptionQueueFilter.All,
            CancellationToken ct = default)
        {
            var result = await _getBusinessSubscriptionsPage.HandleAsync(page, pageSize, q, queue, ct).ConfigureAwait(false);
            var vm = new BusinessSubscriptionsListVm
            {
                Query = q ?? string.Empty,
                QueueFilter = queue,
                Summary = await BuildBusinessSubscriptionOpsSummaryVmAsync(ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = result.Total,
                Items = result.Items.Select(x => new BusinessSubscriptionListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    BusinessName = x.BusinessName,
                    BusinessContactEmail = x.BusinessContactEmail,
                    BillingPlanId = x.BillingPlanId,
                    PlanCode = x.PlanCode,
                    PlanName = x.PlanName,
                    Provider = x.Provider,
                    ProviderCustomerId = x.ProviderCustomerId,
                    ProviderSubscriptionId = x.ProviderSubscriptionId,
                    ProviderCheckoutSessionId = x.ProviderCheckoutSessionId,
                    Status = x.Status,
                    StartedAtUtc = x.StartedAtUtc,
                    CurrentPeriodEndUtc = x.CurrentPeriodEndUtc,
                    TrialEndsAtUtc = x.TrialEndsAtUtc,
                    CanceledAtUtc = x.CanceledAtUtc,
                    CancelAtPeriodEnd = x.CancelAtPeriodEnd,
                    UnitPriceMinor = x.UnitPriceMinor,
                    Currency = x.Currency,
                    IsStripe = x.IsStripe,
                    MissingProviderReference = x.MissingProviderReference,
                    ProviderReferenceState = x.ProviderReferenceState,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderSubscriptionsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Payments(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PaymentQueueFilter? queue = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);

            var items = new List<PaymentListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getPaymentsPage.HandleAsync(businessId.Value, page, pageSize, q, queue, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new PaymentListItemVm
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    InvoiceId = x.InvoiceId,
                    InvoiceStatus = x.InvoiceStatus,
                    InvoiceDueAtUtc = x.InvoiceDueAtUtc,
                    InvoiceTotalGrossMinor = x.InvoiceTotalGrossMinor,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    CustomerEmail = x.CustomerEmail,
                    UserId = x.UserId,
                    UserDisplayName = x.UserDisplayName,
                    UserEmail = x.UserEmail,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Status = x.Status,
                    Provider = x.Provider,
                    ProviderTransactionRef = x.ProviderTransactionRef,
                    ProviderPaymentIntentRef = x.ProviderPaymentIntentRef,
                    ProviderCheckoutSessionRef = x.ProviderCheckoutSessionRef,
                    FailureReason = x.FailureReason,
                    PaidAtUtc = x.PaidAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RefundedAmountMinor = x.RefundedAmountMinor,
                    NetCapturedAmountMinor = x.NetCapturedAmountMinor,
                    LastFinancialEventAtUtc = x.LastFinancialEventAtUtc,
                    OpenAgeHours = x.OpenAgeHours,
                    ProviderReferenceState = x.ProviderReferenceState,
                    DisputeReviewState = x.DisputeReviewState,
                    IsStripe = x.IsStripe,
                    NeedsReconciliation = x.NeedsReconciliation,
                    NeedsDisputeFollowUp = x.NeedsDisputeFollowUp,
                    NeedsSupportAttention = x.NeedsSupportAttention,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new PaymentsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                QueueFilter = queue,
                Stripe = await BuildStripeOperationsVmAsync(ct).ConfigureAwait(false),
                Tax = await BuildTaxOperationsVmAsync(ct).ConfigureAwait(false),
                Webhooks = await BuildBillingWebhookOpsSummaryVmAsync(ct).ConfigureAwait(false),
                Summary = businessId.HasValue
                    ? await BuildPaymentOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)
                    : new PaymentOpsSummaryVm(),
                Playbooks = BuildPaymentPlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };

            return RenderPaymentsWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentDisputeReview(
            Guid id,
            string rowVersion,
            string action,
            string? note = null,
            Guid? businessId = null,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            PaymentQueueFilter? queue = null,
            bool returnToEdit = false,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("PaymentDisputeReviewUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Payments), new { businessId, page, pageSize, q, queue });
            }

            var version = DecodeBase64RowVersion(rowVersion);

            var result = await _updatePaymentDisputeReview
                .HandleAsync(new UpdatePaymentDisputeReviewDto
                {
                    Id = id,
                    RowVersion = version,
                    Action = action,
                    Note = note
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage(action switch
                {
                    UpdatePaymentDisputeReviewHandler.UnderReviewAction => "PaymentDisputeMarkedUnderReviewMessage",
                    UpdatePaymentDisputeReviewHandler.EvidenceSubmittedAction => "PaymentDisputeEvidenceSubmittedMessage",
                    UpdatePaymentDisputeReviewHandler.ResolveWonAction => "PaymentDisputeResolvedWonMessage",
                    UpdatePaymentDisputeReviewHandler.ResolveLostAction => "PaymentDisputeResolvedLostMessage",
                    UpdatePaymentDisputeReviewHandler.ClearAction => "PaymentDisputeReviewClearedMessage",
                    _ => "PaymentDisputeReviewUpdatedMessage"
                });
            }
            else
            {
                SetErrorMessage("PaymentDisputeReviewUpdateFailedMessage");
            }

            if (returnToEdit)
            {
                return RedirectOrHtmx(nameof(EditPayment), new { id });
            }

            return RedirectOrHtmx(nameof(Payments), new { businessId, page, pageSize, q, queue });
        }

        [HttpGet]
        public async Task<IActionResult> TaxCompliance(CancellationToken ct = default)
        {
            var overview = await _getTaxComplianceOverview.HandleAsync(ct: ct).ConfigureAwait(false);
            var vm = new TaxComplianceListVm
            {
                Tax = await BuildTaxOperationsVmAsync(ct).ConfigureAwait(false),
                Summary = new TaxComplianceOpsSummaryVm
                {
                    BusinessCustomersMissingVatIdCount = overview.Summary.BusinessCustomersMissingVatIdCount,
                    BusinessCustomersVatValidationReviewCount = overview.Summary.BusinessCustomersVatValidationReviewCount,
                    BusinessInvoicesMissingVatIdCount = overview.Summary.BusinessInvoicesMissingVatIdCount,
                    ReverseChargeCandidateInvoiceCount = overview.Summary.ReverseChargeCandidateInvoiceCount,
                    DraftInvoiceCount = overview.Summary.DraftInvoiceCount,
                    DueSoonInvoiceCount = overview.Summary.DueSoonInvoiceCount,
                    OverdueInvoiceCount = overview.Summary.OverdueInvoiceCount
                },
                Playbooks = BuildTaxCompliancePlaybooks(),
                InvoiceItems = overview.InvoiceItems.Select(x => new TaxComplianceInvoiceReviewItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    CompanyName = x.CompanyName,
                    CustomerTaxProfileType = x.CustomerTaxProfileType,
                    CustomerVatId = x.CustomerVatId,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    PaymentId = x.PaymentId,
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalGrossMinor = x.TotalGrossMinor,
                    DueDateUtc = x.DueDateUtc,
                    RequiresVatId = x.RequiresVatId,
                    IsReverseChargeCandidate = x.IsReverseChargeCandidate,
                    ReverseChargeApplied = x.ReverseChargeApplied,
                    ReverseChargeReviewedAtUtc = x.ReverseChargeReviewedAtUtc,
                    IsDueSoon = x.IsDueSoon,
                    IsOverdue = x.IsOverdue,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ModifiedAtUtc = x.ModifiedAtUtc
                }).ToList(),
                CustomerItems = overview.CustomerItems.Select(x => new TaxComplianceCustomerReviewItemVm
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    CompanyName = x.CompanyName,
                    VatId = x.VatId,
                    RowVersion = x.RowVersion,
                    RequiresVatId = x.RequiresVatId,
                    RequiresVatValidation = x.RequiresVatValidation,
                    VatValidationStatus = x.VatValidationStatus,
                    VatValidationCheckedAtUtc = x.VatValidationCheckedAtUtc,
                    VatValidationSource = x.VatValidationSource,
                    VatValidationMessage = x.VatValidationMessage,
                    OpportunityCount = x.OpportunityCount,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ModifiedAtUtc = x.ModifiedAtUtc
                }).ToList()
            };

            return RenderTaxComplianceWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportTaxComplianceInvoices(CancellationToken ct = default)
        {
            var rows = await _getTaxComplianceInvoiceExport.HandleAsync(ct: ct).ConfigureAwait(false);
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",", new[]
            {
                "InvoiceId",
                "BusinessId",
                "CustomerId",
                "Customer",
                "Company",
                "TaxProfile",
                "VatId",
                "OrderId",
                "OrderNumber",
                "PaymentId",
                "Status",
                "Currency",
                "TotalNetMinor",
                "TotalTaxMinor",
                "TotalGrossMinor",
                "DueDateUtc",
                "RequiresVatId",
                "IsReverseChargeCandidate",
                "ReverseChargeApplied",
                "ReverseChargeReviewedAtUtc",
                "IsDueSoon",
                "IsOverdue",
                "CreatedAtUtc",
                "ModifiedAtUtc"
            }));

            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(row.InvoiceId),
                    Csv(row.BusinessId),
                    Csv(row.CustomerId),
                    Csv(row.CustomerDisplayName),
                    Csv(row.CompanyName),
                    Csv(row.CustomerTaxProfileType?.ToString()),
                    Csv(row.CustomerVatId),
                    Csv(row.OrderId),
                    Csv(row.OrderNumber),
                    Csv(row.PaymentId),
                    Csv(row.Status.ToString()),
                    Csv(row.Currency),
                    Csv(row.TotalNetMinor),
                    Csv(row.TotalTaxMinor),
                    Csv(row.TotalGrossMinor),
                    Csv(row.DueDateUtc),
                    Csv(row.RequiresVatId),
                    Csv(row.IsReverseChargeCandidate),
                    Csv(row.ReverseChargeApplied),
                    Csv(row.ReverseChargeReviewedAtUtc),
                    Csv(row.IsDueSoon),
                    Csv(row.IsOverdue),
                    Csv(row.CreatedAtUtc),
                    Csv(row.ModifiedAtUtc)
                }));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            var fileName = $"darwin-tax-compliance-invoices-{_clock.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomerTaxProfile(TaxComplianceCustomerTaxProfileUpdateVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("CustomerUpdateFailedMessage");
                return RedirectOrHtmx(nameof(TaxCompliance), new { });
            }

            var dto = new CustomerTaxProfileUpdateDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                TaxProfileType = vm.TaxProfileType,
                CompanyName = vm.CompanyName,
                VatId = vm.VatId
            };

            try
            {
                await _updateCustomerTaxProfile.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("CustomerUpdatedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("CustomerConcurrencyMessage");
            }
            catch (ValidationException ex)
            {
                SetValidationErrorMessage(ex, "CustomerUpdateFailedMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("CustomerUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(TaxCompliance), new { });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomerVatValidationDecision(CustomerVatValidationDecisionVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("CustomerVatValidationUpdateFailedMessage");
                return RedirectOrHtmx(nameof(TaxCompliance), new { });
            }

            var dto = new CustomerVatValidationDecisionDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                Status = vm.Status,
                Source = vm.Source,
                Message = vm.Message
            };

            try
            {
                await _updateCustomerVatValidationDecision.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("CustomerVatValidationUpdatedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("CustomerConcurrencyMessage");
            }
            catch (ValidationException ex)
            {
                SetValidationErrorMessage(ex, "CustomerVatValidationUpdateFailedMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("CustomerVatValidationUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(TaxCompliance), new { });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateCustomerVatId(CustomerVatValidationLookupVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("CustomerVatValidationLookupFailedMessage");
                return RedirectOrHtmx(nameof(TaxCompliance), new { });
            }

            try
            {
                var status = await _validateCustomerVatId.HandleAsync(new CustomerVatValidationLookupDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion
                }, ct).ConfigureAwait(false);

                SetSuccessMessage(status == CustomerVatValidationStatus.Unknown
                    ? "CustomerVatValidationLookupCompletedWithReviewMessage"
                    : "CustomerVatValidationLookupUpdatedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("ConcurrencyConflictDetected");
            }
            catch (ValidationException ex)
            {
                SetValidationErrorMessage(ex, "CustomerVatValidationLookupFailedMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("CustomerVatValidationLookupFailedMessage");
            }

            return RedirectOrHtmx(nameof(TaxCompliance), new { });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInvoiceReverseChargeDecision(InvoiceReverseChargeDecisionVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                SetErrorMessage("InvoiceReverseChargeDecisionUpdateFailedMessage");
                return RedirectOrHtmx(nameof(TaxCompliance), new { });
            }

            var dto = new InvoiceReverseChargeDecisionDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                Applies = vm.Applies,
                Note = vm.Note
            };

            try
            {
                await _updateInvoiceReverseChargeDecision.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("InvoiceReverseChargeDecisionUpdatedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("CustomerConcurrencyMessage");
            }
            catch (ValidationException ex)
            {
                SetValidationErrorMessage(ex, "InvoiceReverseChargeDecisionUpdateFailedMessage");
            }
            catch (Exception)
            {
                SetErrorMessage("InvoiceReverseChargeDecisionUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(TaxCompliance), new { });
        }

        [HttpGet]
        public async Task<IActionResult> Webhooks(int page = 1, int pageSize = 20, string? q = null, BillingWebhookDeliveryQueueFilter queue = BillingWebhookDeliveryQueueFilter.All, CancellationToken ct = default)
        {
            var subscriptions = await _getBillingWebhookSubscriptionsPage.HandleAsync(1, 10, q, ct).ConfigureAwait(false);
            var deliveries = await _getBillingWebhookDeliveriesPage.HandleAsync(page, pageSize, q, queue, ct).ConfigureAwait(false);

            var vm = new BillingWebhooksListVm
            {
                Query = q ?? string.Empty,
                QueueFilter = queue,
                Summary = await BuildBillingWebhookOpsSummaryVmAsync(ct).ConfigureAwait(false),
                Subscriptions = subscriptions.Items.Select(x => new BillingWebhookSubscriptionListItemVm
                {
                    Id = x.Id,
                    EventType = x.EventType,
                    CallbackUrl = x.CallbackUrl,
                    IsActive = x.IsActive,
                    CreatedAtUtc = x.CreatedAtUtc
                }).ToList(),
                Deliveries = deliveries.Items.Select(x => new BillingWebhookDeliveryListItemVm
                {
                    Id = x.Id,
                    RowVersion = x.RowVersion,
                    SubscriptionId = x.SubscriptionId,
                    EventType = x.EventType,
                    CallbackUrl = x.CallbackUrl,
                    Status = x.Status,
                    RetryCount = x.RetryCount,
                    ResponseCode = x.ResponseCode,
                    CreatedAtUtc = x.CreatedAtUtc,
                    LastAttemptAtUtc = x.LastAttemptAtUtc,
                    IdempotencyKey = x.IdempotencyKey,
                    IsActiveSubscription = x.IsActiveSubscription,
                    RetrySafetyState = x.RetrySafetyState,
                    FailureDiagnostics = x.FailureDiagnostics,
                    EscalationHint = x.EscalationHint,
                    SuggestedOperatorAction = x.SuggestedOperatorAction,
                    SuggestedQueueTarget = x.SuggestedQueueTarget
                }).ToList(),
                Playbooks = BuildWebhookPlaybooks(),
                Page = page,
                PageSize = pageSize,
                Total = deliveries.Total
            };

            return RenderWebhooksWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateWebhookDelivery(
            Guid id,
            string rowVersion,
            string action,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            BillingWebhookDeliveryQueueFilter queue = BillingWebhookDeliveryQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("WebhookDeliveryUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Webhooks), new { page, pageSize, q, queue });
            }

            var version = DecodeBase64RowVersion(rowVersion);

            var result = await _updateBillingWebhookDelivery
                .HandleAsync(new UpdateBillingWebhookDeliveryDto
                {
                    Id = id,
                    RowVersion = version,
                    Action = action
                }, ct)
                .ConfigureAwait(false);

            if (result.Succeeded)
            {
                SetSuccessMessage(action switch
                {
                    "MarkSucceeded" => "WebhookDeliveryMarkedSucceededMessage",
                    "Requeue" => "WebhookDeliveryRequeuedMessage",
                    "Suppress" => "WebhookDeliverySuppressedMessage",
                    _ => "WebhookDeliveryUpdatedMessage"
                });
            }
            else
            {
                SetErrorMessage("WebhookDeliveryUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(Webhooks), new { page, pageSize, q, queue });
        }

        [HttpGet]
        public async Task<IActionResult> Refunds(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, BillingRefundQueueFilter? queue = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);

            var items = new List<BillingRefundListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getRefundsPage.HandleAsync(businessId.Value, page, pageSize, q, queue, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new BillingRefundListItemVm
                {
                    Id = x.Id,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    PaymentId = x.PaymentId,
                    PaymentProvider = x.PaymentProvider,
                    PaymentProviderReference = x.PaymentProviderReference,
                    PaymentProviderPaymentIntentRef = x.PaymentProviderPaymentIntentRef,
                    PaymentProviderCheckoutSessionRef = x.PaymentProviderCheckoutSessionRef,
                    PaymentStatus = x.PaymentStatus,
                    RefundProvider = x.RefundProvider,
                    RefundProviderReference = x.RefundProviderReference,
                    RefundProviderPaymentReference = x.RefundProviderPaymentReference,
                    RefundProviderStatus = x.RefundProviderStatus,
                    FailureReason = x.FailureReason,
                    RequestedAtUtc = x.RequestedAtUtc,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    CustomerEmail = x.CustomerEmail,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    LastRefundEventAtUtc = x.LastRefundEventAtUtc,
                    OpenAgeHours = x.OpenAgeHours,
                    ProviderReferenceState = x.ProviderReferenceState,
                    IsStripe = x.IsStripe,
                    NeedsSupportAttention = x.NeedsSupportAttention,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new RefundsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                QueueFilter = queue,
                Webhooks = await BuildBillingWebhookOpsSummaryVmAsync(ct).ConfigureAwait(false),
                Summary = businessId.HasValue
                    ? await BuildRefundOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)
                    : new RefundOpsSummaryVm(),
                Playbooks = BuildRefundPlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };

            return RenderRefundsWorkspace(vm);
        }

        private async Task<StripeOperationsVm> BuildStripeOperationsVmAsync(CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            return new StripeOperationsVm
            {
                Enabled = settings.StripeEnabled,
                PublishableKeyConfigured = !string.IsNullOrWhiteSpace(settings.StripePublishableKey),
                SecretKeyConfigured = !string.IsNullOrWhiteSpace(settings.StripeSecretKey),
                WebhookSecretConfigured = !string.IsNullOrWhiteSpace(settings.StripeWebhookSecret),
                MerchantDisplayNameConfigured = !string.IsNullOrWhiteSpace(settings.StripeMerchantDisplayName),
                MerchantDisplayName = settings.StripeMerchantDisplayName ?? string.Empty
            };
        }

        private async Task<PaymentOpsSummaryVm> BuildPaymentOpsSummaryVmAsync(Guid businessId, CancellationToken ct)
        {
            var summary = await _getPaymentOpsSummary.HandleAsync(businessId, ct).ConfigureAwait(false);
            return new PaymentOpsSummaryVm
            {
                PendingCount = summary.PendingCount,
                FailedCount = summary.FailedCount,
                RefundedCount = summary.RefundedCount,
                UnlinkedCount = summary.UnlinkedCount,
                ProviderLinkedCount = summary.ProviderLinkedCount,
                StripeCount = summary.StripeCount,
                MissingProviderRefCount = summary.MissingProviderRefCount,
                FailedStripeCount = summary.FailedStripeCount,
                NeedsReconciliationCount = summary.NeedsReconciliationCount,
                DisputeFollowUpCount = summary.DisputeFollowUpCount
            };
        }

        private async Task<BillingPlanOpsSummaryVm> BuildBillingPlanOpsSummaryVmAsync(CancellationToken ct)
        {
            var summary = await _getBillingPlanOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            return new BillingPlanOpsSummaryVm
            {
                TotalCount = summary.TotalCount,
                ActiveCount = summary.ActiveCount,
                InactiveCount = summary.InactiveCount,
                TrialCount = summary.TrialCount,
                MissingFeaturesCount = summary.MissingFeaturesCount,
                InUseCount = summary.InUseCount
            };
        }

        private async Task<BusinessSubscriptionOpsSummaryVm> BuildBusinessSubscriptionOpsSummaryVmAsync(CancellationToken ct)
        {
            var summary = await _getBusinessSubscriptionOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            return new BusinessSubscriptionOpsSummaryVm
            {
                TotalCount = summary.TotalCount,
                ActiveCount = summary.ActiveCount,
                TrialingCount = summary.TrialingCount,
                PastDueCount = summary.PastDueCount,
                CanceledCount = summary.CanceledCount,
                StripeCount = summary.StripeCount,
                MissingProviderReferenceCount = summary.MissingProviderReferenceCount,
                CancelAtPeriodEndCount = summary.CancelAtPeriodEndCount
            };
        }

        private async Task<TaxOperationsVm> BuildTaxOperationsVmAsync(CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var issuerConfigured = !string.IsNullOrWhiteSpace(settings.InvoiceIssuerLegalName);
            var issuerTaxIdConfigured = !string.IsNullOrWhiteSpace(settings.InvoiceIssuerTaxId);
            var issuerAddressConfigured =
                !string.IsNullOrWhiteSpace(settings.InvoiceIssuerAddressLine1) &&
                !string.IsNullOrWhiteSpace(settings.InvoiceIssuerPostalCode) &&
                !string.IsNullOrWhiteSpace(settings.InvoiceIssuerCity) &&
                !string.IsNullOrWhiteSpace(settings.InvoiceIssuerCountry);
            var archiveReady = issuerConfigured && issuerTaxIdConfigured && issuerAddressConfigured;
            var eInvoiceBaselineReady = archiveReady && settings.VatEnabled;
            var structuredExportBaselineReady = archiveReady;

            return new TaxOperationsVm
            {
                VatEnabled = settings.VatEnabled,
                DefaultVatRatePercent = settings.DefaultVatRatePercent,
                PricesIncludeVat = settings.PricesIncludeVat,
                AllowReverseCharge = settings.AllowReverseCharge,
                IssuerConfigured = issuerConfigured && issuerAddressConfigured,
                InvoiceIssuerLegalName = settings.InvoiceIssuerLegalName ?? string.Empty,
                InvoiceIssuerCountry = settings.InvoiceIssuerCountry ?? string.Empty,
                InvoiceIssuerTaxIdConfigured = issuerTaxIdConfigured,
                ArchiveReadinessComplete = archiveReady,
                ArchiveReadinessLabel = archiveReady ? T("TaxPolicyArchiveReady") : T("TaxPolicyArchiveIncomplete"),
                EInvoiceBaselineReady = eInvoiceBaselineReady,
                EInvoiceBaselineLabel = eInvoiceBaselineReady ? T("TaxPolicyBaselineReady") : T("TaxPolicyBaselineIncomplete"),
                StructuredExportBaselineReady = structuredExportBaselineReady,
                StructuredExportBaselineLabel = structuredExportBaselineReady ? T("TaxPolicyStructuredExportReady") : T("TaxPolicyStructuredExportIncomplete"),
                ComplianceScopeNote = T("TaxPolicyComplianceScopeNote")
            };
        }

        private static string Csv(object? value)
        {
            var text = value switch
            {
                null => string.Empty,
                DateTime dateTime => dateTime.ToUniversalTime().ToString("O"),
                Guid guid => guid.ToString("D"),
                bool boolean => boolean ? "true" : "false",
                _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            };

            if (text.Length > 0 && (text[0] == '=' || text[0] == '+' || text[0] == '-' || text[0] == '@'))
            {
                text = "'" + text;
            }

            return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        private async Task<RefundOpsSummaryVm> BuildRefundOpsSummaryVmAsync(Guid businessId, CancellationToken ct)
        {
            var summary = await _getRefundOpsSummary.HandleAsync(businessId, ct).ConfigureAwait(false);
            return new RefundOpsSummaryVm
            {
                PendingCount = summary.PendingCount,
                CompletedCount = summary.CompletedCount,
                FailedCount = summary.FailedCount,
                StripeCount = summary.StripeCount,
                NeedsSupportCount = summary.NeedsSupportCount
            };
        }

        private async Task<BillingWebhookOpsSummaryVm> BuildBillingWebhookOpsSummaryVmAsync(CancellationToken ct)
        {
            var summary = await _getBillingWebhookOpsSummary.HandleAsync(ct).ConfigureAwait(false);
            return new BillingWebhookOpsSummaryVm
            {
                ActiveSubscriptionCount = summary.ActiveSubscriptionCount,
                PendingDeliveryCount = summary.PendingDeliveryCount,
                FailedDeliveryCount = summary.FailedDeliveryCount,
                SucceededDeliveryCount = summary.SucceededDeliveryCount,
                RetryPendingCount = summary.RetryPendingCount,
                PaymentExceptionCount = summary.PaymentExceptionCount,
                DisputeSignalCount = summary.DisputeSignalCount
            };
        }

        private static List<ProviderPlaybookVm> BuildPaymentPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "BillingPaymentsPlaybookPendingTitle",
                    ScopeNote = "BillingPaymentsPlaybookPendingScope",
                    OperatorAction = "BillingPaymentsPlaybookPendingAction",
                    SettingsDependency = "BillingPaymentsPlaybookPendingDependency"
                },
                new()
                {
                    Title = "BillingPaymentsPlaybookMissingProviderTitle",
                    ScopeNote = "BillingPaymentsPlaybookMissingProviderScope",
                    OperatorAction = "BillingPaymentsPlaybookMissingProviderAction",
                    SettingsDependency = "BillingPaymentsPlaybookMissingProviderDependency"
                },
                new()
                {
                    Title = "BillingPaymentsPlaybookFailedTitle",
                    ScopeNote = "BillingPaymentsPlaybookFailedScope",
                    OperatorAction = "BillingPaymentsPlaybookFailedAction",
                    SettingsDependency = "BillingPaymentsPlaybookFailedDependency"
                },
                new()
                {
                    Title = "BillingPaymentsPlaybookReconciliationTitle",
                    ScopeNote = "BillingPaymentsPlaybookReconciliationScope",
                    OperatorAction = "BillingPaymentsPlaybookReconciliationAction",
                    SettingsDependency = "BillingPaymentsPlaybookReconciliationDependency"
                },
                new()
                {
                    Title = "BillingPaymentsPlaybookHistoryTitle",
                    ScopeNote = "BillingPaymentsPlaybookHistoryScope",
                    OperatorAction = "BillingPaymentsPlaybookHistoryAction",
                    SettingsDependency = "BillingPaymentsPlaybookHistoryDependency"
                },
                new()
                {
                    Title = "BillingPaymentsPlaybookDisputeTitle",
                    ScopeNote = "BillingPaymentsPlaybookDisputeScope",
                    OperatorAction = "BillingPaymentsPlaybookDisputeAction",
                    SettingsDependency = "BillingPaymentsPlaybookDisputeDependency"
                },
                new()
                {
                    Title = "BillingPaymentsPlaybookLinkingTitle",
                    ScopeNote = "BillingPaymentsPlaybookLinkingScope",
                    OperatorAction = "BillingPaymentsPlaybookLinkingAction",
                    SettingsDependency = "BillingPaymentsPlaybookLinkingDependency"
                }
            };
        }

        private static List<ProviderPlaybookVm> BuildRefundPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "BillingRefundsPlaybookPendingTitle",
                    ScopeNote = "BillingRefundsPlaybookPendingScope",
                    OperatorAction = "BillingRefundsPlaybookPendingAction",
                    SettingsDependency = "BillingRefundsPlaybookPendingDependency"
                },
                new()
                {
                    Title = "BillingRefundsPlaybookCompletedTitle",
                    ScopeNote = "BillingRefundsPlaybookCompletedScope",
                    OperatorAction = "BillingRefundsPlaybookCompletedAction",
                    SettingsDependency = "BillingRefundsPlaybookCompletedDependency"
                },
                new()
                {
                    Title = "BillingRefundsPlaybookFailedTitle",
                    ScopeNote = "BillingRefundsPlaybookFailedScope",
                    OperatorAction = "BillingRefundsPlaybookFailedAction",
                    SettingsDependency = "BillingRefundsPlaybookFailedDependency"
                },
                new()
                {
                    Title = "BillingRefundsPlaybookHistoryTitle",
                    ScopeNote = "BillingRefundsPlaybookHistoryScope",
                    OperatorAction = "BillingRefundsPlaybookHistoryAction",
                    SettingsDependency = "BillingRefundsPlaybookHistoryDependency"
                }
            };
        }

        private List<ProviderPlaybookVm> BuildBillingPlanPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = T("BillingPlansPlaybookActiveTitle"),
                    ScopeNote = T("BillingPlansPlaybookActiveScope"),
                    OperatorAction = T("BillingPlansPlaybookActiveAction"),
                    SettingsDependency = T("BillingPlansPlaybookActiveDependency")
                },
                new()
                {
                    Title = T("BillingPlansPlaybookTrialTitle"),
                    ScopeNote = T("BillingPlansPlaybookTrialScope"),
                    OperatorAction = T("BillingPlansPlaybookTrialAction"),
                    SettingsDependency = T("BillingPlansPlaybookTrialDependency")
                }
            };
        }

        private List<ProviderPlaybookVm> BuildWebhookPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = T("WebhookPlaybookPendingTitle"),
                    ScopeNote = T("WebhookPlaybookPendingScope"),
                    OperatorAction = T("WebhookPlaybookPendingAction"),
                    SettingsDependency = T("WebhookPlaybookPendingDependency")
                },
                new()
                {
                    Title = T("WebhookPlaybookFailedTitle"),
                    ScopeNote = T("WebhookPlaybookFailedScope"),
                    OperatorAction = T("WebhookPlaybookFailedAction"),
                    SettingsDependency = T("WebhookPlaybookFailedDependency")
                },
                new()
                {
                    Title = T("WebhookPlaybookRetryPendingTitle"),
                    ScopeNote = T("WebhookPlaybookRetryPendingScope"),
                    OperatorAction = T("WebhookPlaybookRetryPendingAction"),
                    SettingsDependency = T("WebhookPlaybookRetryPendingDependency")
                },
                new()
                {
                    Title = T("WebhookPlaybookPaymentExceptionsTitle"),
                    ScopeNote = T("WebhookPlaybookPaymentExceptionsScope"),
                    OperatorAction = T("WebhookPlaybookPaymentExceptionsAction"),
                    SettingsDependency = T("WebhookPlaybookPaymentExceptionsDependency")
                },
                new()
                {
                    Title = T("WebhookPlaybookDisputeSignalsTitle"),
                    ScopeNote = T("WebhookPlaybookDisputeSignalsScope"),
                    OperatorAction = T("WebhookPlaybookDisputeSignalsAction"),
                    SettingsDependency = T("WebhookPlaybookDisputeSignalsDependency")
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> CreatePayment(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCurrency;
            var nowUtc = _clock.UtcNow;
            var vm = new PaymentEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                CreatedAtUtc = nowUtc,
                Currency = defaultCurrency,
                Status = PaymentStatus.Pending,
                LastFinancialEventAtUtc = nowUtc,
                ProviderReferenceState = "Reference missing",
                NeedsReconciliation = true,
                SupportPlaybooks = BuildPaymentSupportPlaybooks(new PaymentEditDto
                {
                    CreatedAtUtc = nowUtc,
                    Currency = defaultCurrency,
                    Status = PaymentStatus.Pending,
                    LastFinancialEventAtUtc = nowUtc,
                    ProviderReferenceState = "Reference missing",
                    NeedsReconciliation = true
                })
            };

            await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderPaymentEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(PaymentEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("PaymentCreateFailedMessage");
                return RedirectOrHtmx(nameof(Payments), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPaymentEditor(vm, isCreate: true);
            }

            var dto = new PaymentCreateDto
            {
                BusinessId = vm.BusinessId,
                OrderId = vm.OrderId,
                InvoiceId = vm.InvoiceId,
                CustomerId = vm.CustomerId,
                UserId = vm.UserId,
                AmountMinor = vm.AmountMinor,
                Currency = vm.Currency,
                Status = vm.Status,
                Provider = vm.Provider,
                ProviderTransactionRef = vm.ProviderTransactionRef,
                ProviderPaymentIntentRef = vm.ProviderPaymentIntentRef,
                ProviderCheckoutSessionRef = vm.ProviderCheckoutSessionRef,
                PaidAtUtc = vm.PaidAtUtc
            };

            try
            {
                var id = await _createPayment.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("PaymentCreatedMessage");
                return RedirectOrHtmx(nameof(EditPayment), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("PaymentCreateFailedMessage");
                await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPaymentEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPayment(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("PaymentNotFoundMessage");
                return RedirectOrHtmx(nameof(Payments), new { });
            }

            var dto = await _getPaymentForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("PaymentNotFoundMessage");
                return RedirectOrHtmx(nameof(Payments), new { });
            }

            var vm = new PaymentEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                CreatedAtUtc = dto.CreatedAtUtc,
                IsStripe = dto.IsStripe,
                FailureReason = dto.FailureReason,
                LastFinancialEventAtUtc = dto.LastFinancialEventAtUtc,
                OpenAgeHours = dto.OpenAgeHours,
                ProviderReferenceState = dto.ProviderReferenceState,
                DisputeReviewState = dto.DisputeReviewState,
                NeedsReconciliation = dto.NeedsReconciliation,
                NeedsDisputeFollowUp = dto.NeedsDisputeFollowUp,
                BusinessId = dto.BusinessId,
                OrderId = dto.OrderId,
                OrderNumber = dto.OrderNumber,
                InvoiceId = dto.InvoiceId,
                InvoiceStatus = dto.InvoiceStatus,
                InvoiceDueAtUtc = dto.InvoiceDueAtUtc,
                InvoiceTotalGrossMinor = dto.InvoiceTotalGrossMinor,
                CustomerId = dto.CustomerId,
                CustomerDisplayName = dto.CustomerDisplayName,
                CustomerEmail = dto.CustomerEmail,
                UserId = dto.UserId,
                UserDisplayName = dto.UserDisplayName,
                UserEmail = dto.UserEmail,
                AmountMinor = dto.AmountMinor,
                Currency = dto.Currency,
                Status = dto.Status,
                Provider = dto.Provider,
                ProviderTransactionRef = dto.ProviderTransactionRef,
                ProviderPaymentIntentRef = dto.ProviderPaymentIntentRef,
                ProviderCheckoutSessionRef = dto.ProviderCheckoutSessionRef,
                PaidAtUtc = dto.PaidAtUtc,
                RefundedAmountMinor = dto.RefundedAmountMinor,
                NetCapturedAmountMinor = dto.NetCapturedAmountMinor,
                Refunds = dto.Refunds.Select(x => new PaymentRefundHistoryItemVm
                {
                    Id = x.Id,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc
                }).ToList(),
                ProviderEvents = dto.ProviderEvents.Select(x => new PaymentProviderEventItemVm
                {
                    EventType = x.EventType,
                    OccurredAtUtc = x.OccurredAtUtc,
                    IdempotencyKey = x.IdempotencyKey,
                    CorrelationKind = x.CorrelationKind,
                    CorrelationReference = x.CorrelationReference
                }).ToList(),
                SupportPlaybooks = BuildPaymentSupportPlaybooks(dto)
            };

            await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderPaymentEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPayment(PaymentEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "PaymentNotFoundMessage" : "PaymentUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Payments), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPaymentEditor(vm, isCreate: false);
            }

            var dto = new PaymentEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                OrderId = vm.OrderId,
                InvoiceId = vm.InvoiceId,
                CustomerId = vm.CustomerId,
                UserId = vm.UserId,
                AmountMinor = vm.AmountMinor,
                Currency = vm.Currency,
                Status = vm.Status,
                Provider = vm.Provider,
                ProviderTransactionRef = vm.ProviderTransactionRef,
                ProviderPaymentIntentRef = vm.ProviderPaymentIntentRef,
                ProviderCheckoutSessionRef = vm.ProviderCheckoutSessionRef,
                PaidAtUtc = vm.PaidAtUtc
            };

            try
            {
                await _updatePayment.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("PaymentUpdatedMessage");
                return RedirectOrHtmx(nameof(EditPayment), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("PaymentConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditPayment), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("PaymentUpdateFailedMessage");
                await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPaymentEditor(vm, isCreate: false);
            }
        }

        private List<PaymentSupportPlaybookVm> BuildPaymentSupportPlaybooks(PaymentEditDto dto)
        {
            var items = new List<PaymentSupportPlaybookVm>
            {
                new()
                {
                    Title = T("PaymentSupportPlaybookProviderCorrelationTitle"),
                    ScopeNote = !HasProviderCorrelation(dto)
                        ? T("PaymentSupportPlaybookProviderCorrelationMissingScope")
                        : T("PaymentSupportPlaybookProviderCorrelationPresentScope"),
                    OperatorAction = !HasProviderCorrelation(dto)
                        ? T("PaymentSupportPlaybookProviderCorrelationMissingAction")
                        : T("PaymentSupportPlaybookProviderCorrelationPresentAction")
                },
                new()
                {
                    Title = T("PaymentSupportPlaybookFailureRefundTitle"),
                    ScopeNote = !string.IsNullOrWhiteSpace(dto.FailureReason)
                        ? T("PaymentSupportPlaybookFailureScope")
                        : dto.Refunds.Count > 0
                            ? T("PaymentSupportPlaybookRefundScope")
                            : T("PaymentSupportPlaybookBaselineScope"),
                    OperatorAction = !string.IsNullOrWhiteSpace(dto.FailureReason)
                        ? T("PaymentSupportPlaybookFailureAction")
                        : dto.Refunds.Count > 0
                              ? T("PaymentSupportPlaybookRefundAction")
                              : T("PaymentSupportPlaybookBaselineAction")
                },
                new()
                {
                    Title = T("PaymentSupportPlaybookProviderTimelineTitle"),
                    ScopeNote = dto.ProviderEvents.Count > 0
                        ? T("PaymentSupportPlaybookProviderTimelinePresentScope")
                        : T("PaymentSupportPlaybookProviderTimelineMissingScope"),
                    OperatorAction = dto.ProviderEvents.Count > 0
                        ? T("PaymentSupportPlaybookProviderTimelinePresentAction")
                        : T("PaymentSupportPlaybookProviderTimelineMissingAction")
                }
            };

            if (dto.IsStripe)
            {
                items.Add(new PaymentSupportPlaybookVm
                {
                    Title = T("PaymentSupportPlaybookStripeTitle"),
                    ScopeNote = T("PaymentSupportPlaybookStripeScope"),
                    OperatorAction = T("PaymentSupportPlaybookStripeAction")
                });
            }

            return items;
        }

        [HttpGet]
        public async Task<IActionResult> FinancialAccounts(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, AccountType? queue = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<FinancialAccountListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getAccountsPage.HandleAsync(businessId.Value, page, pageSize, q, queue, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new FinancialAccountListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    Type = x.Type,
                    Code = x.Code,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new FinancialAccountsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                QueueFilter = queue,
                Summary = businessId.HasValue
                    ? await BuildFinancialAccountOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)
                    : new FinancialAccountOpsSummaryVm(),
                Playbooks = BuildFinancialAccountPlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return RenderFinancialAccountsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateFinancialAccount(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new FinancialAccountEditVm
            {
                BusinessId = businessId ?? Guid.Empty
            };
            await PopulateFinancialAccountOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderFinancialAccountEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFinancialAccount(FinancialAccountEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("FinancialAccountCreateFailedMessage");
                return RedirectOrHtmx(nameof(FinancialAccounts), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateFinancialAccountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderFinancialAccountEditor(vm, isCreate: true);
            }

            var dto = new FinancialAccountCreateDto
            {
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Type = vm.Type,
                Code = vm.Code
            };

            try
            {
                var id = await _createAccount.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("FinancialAccountCreatedMessage");
                return RedirectOrHtmx(nameof(EditFinancialAccount), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("FinancialAccountCreateFailedMessage");
                await PopulateFinancialAccountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderFinancialAccountEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditFinancialAccount(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("FinancialAccountNotFoundMessage");
                return RedirectOrHtmx(nameof(FinancialAccounts), new { });
            }

            var dto = await _getAccountForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("FinancialAccountNotFoundMessage");
                return RedirectOrHtmx(nameof(FinancialAccounts), new { });
            }

            var vm = new FinancialAccountEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                Name = dto.Name,
                Type = dto.Type,
                Code = dto.Code
            };
            await PopulateFinancialAccountOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderFinancialAccountEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinancialAccount(FinancialAccountEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "FinancialAccountNotFoundMessage" : "FinancialAccountUpdateFailedMessage");
                return RedirectOrHtmx(nameof(FinancialAccounts), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulateFinancialAccountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderFinancialAccountEditor(vm, isCreate: false);
            }

            var dto = new FinancialAccountEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                Name = vm.Name,
                Type = vm.Type,
                Code = vm.Code
            };

            try
            {
                await _updateAccount.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("FinancialAccountUpdatedMessage");
                return RedirectOrHtmx(nameof(EditFinancialAccount), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("FinancialAccountConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditFinancialAccount), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("FinancialAccountUpdateFailedMessage");
                await PopulateFinancialAccountOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderFinancialAccountEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Expenses(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCurrency;
            var items = new List<ExpenseListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getExpensesPage.HandleAsync(businessId.Value, page, pageSize, q, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new ExpenseListItemVm
                {
                    Id = x.Id,
                    SupplierId = x.SupplierId,
                    Category = x.Category,
                    Description = x.Description,
                    Currency = defaultCurrency,
                    AmountMinor = x.AmountMinor,
                    ExpenseDateUtc = x.ExpenseDateUtc,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new ExpensesListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                Summary = businessId.HasValue
                    ? await BuildExpenseOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)
                    : new ExpenseOpsSummaryVm(),
                Playbooks = BuildExpensePlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return RenderExpensesWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateExpense(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var nowUtc = _clock.UtcNow;
            var vm = new ExpenseEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                ExpenseDateUtc = nowUtc
            };
            await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderExpenseEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense(ExpenseEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("ExpenseCreateFailedMessage");
                return RedirectOrHtmx(nameof(Expenses), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderExpenseEditor(vm, isCreate: true);
            }

            var dto = new ExpenseCreateDto
            {
                BusinessId = vm.BusinessId,
                SupplierId = vm.SupplierId,
                Category = vm.Category,
                Description = vm.Description,
                AmountMinor = vm.AmountMinor,
                ExpenseDateUtc = vm.ExpenseDateUtc
            };

            try
            {
                var id = await _createExpense.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("ExpenseCreatedMessage");
                return RedirectOrHtmx(nameof(EditExpense), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("ExpenseCreateFailedMessage");
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderExpenseEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditExpense(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("ExpenseNotFoundMessage");
                return RedirectOrHtmx(nameof(Expenses), new { });
            }

            var dto = await _getExpenseForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("ExpenseNotFoundMessage");
                return RedirectOrHtmx(nameof(Expenses), new { });
            }

            var vm = new ExpenseEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                SupplierId = dto.SupplierId,
                Category = dto.Category,
                Description = dto.Description,
                AmountMinor = dto.AmountMinor,
                ExpenseDateUtc = dto.ExpenseDateUtc
            };
            await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderExpenseEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExpense(ExpenseEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "ExpenseNotFoundMessage" : "ExpenseUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Expenses), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderExpenseEditor(vm, isCreate: false);
            }

            var dto = new ExpenseEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                SupplierId = vm.SupplierId,
                Category = vm.Category,
                Description = vm.Description,
                AmountMinor = vm.AmountMinor,
                ExpenseDateUtc = vm.ExpenseDateUtc
            };

            try
            {
                await _updateExpense.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("ExpenseUpdatedMessage");
                return RedirectOrHtmx(nameof(EditExpense), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("ExpenseConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditExpense), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("ExpenseUpdateFailedMessage");
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderExpenseEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> JournalEntries(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, JournalEntryQueueFilter? queue = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var defaultCurrency = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCurrency;
            var items = new List<JournalEntryListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getJournalEntriesPage.HandleAsync(businessId.Value, page, pageSize, q, queue, ct).ConfigureAwait(false);
                items = result.Items.Select(x => new JournalEntryListItemVm
                {
                    Id = x.Id,
                    EntryDateUtc = x.EntryDateUtc,
                    Description = x.Description,
                    LineCount = x.LineCount,
                    Currency = defaultCurrency,
                    TotalDebitMinor = x.TotalDebitMinor,
                    TotalCreditMinor = x.TotalCreditMinor,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new JournalEntriesListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                QueueFilter = queue,
                Summary = businessId.HasValue
                    ? await BuildJournalEntryOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)
                    : new JournalEntryOpsSummaryVm(),
                Playbooks = BuildJournalEntryPlaybooks(),
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
            return RenderJournalEntriesWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateJournalEntry(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var nowUtc = _clock.UtcNow;
            var vm = new JournalEntryEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                EntryDateUtc = nowUtc,
                Lines =
                [
                    new JournalEntryLineVm(),
                    new JournalEntryLineVm()
                ]
            };
            await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderJournalEntryEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJournalEntry(JournalEntryEditVm vm, CancellationToken ct = default)
        {
            if (vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage("JournalEntryCreateFailedMessage");
                return RedirectOrHtmx(nameof(JournalEntries), new { });
            }

            if (!ModelState.IsValid)
            {
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderJournalEntryEditor(vm, isCreate: true);
            }

            var dto = new JournalEntryCreateDto
            {
                BusinessId = vm.BusinessId,
                EntryDateUtc = vm.EntryDateUtc,
                Description = vm.Description,
                Lines = vm.Lines.Select(x => new JournalEntryLineDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    DebitMinor = x.DebitMinor,
                    CreditMinor = x.CreditMinor,
                    Memo = x.Memo
                }).ToList()
            };

            try
            {
                var id = await _createJournalEntry.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("JournalEntryCreatedMessage");
                return RedirectOrHtmx(nameof(EditJournalEntry), new { id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("JournalEntryCreateFailedMessage");
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderJournalEntryEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditJournalEntry(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("JournalEntryNotFoundMessage");
                return RedirectOrHtmx(nameof(JournalEntries), new { });
            }

            var dto = await _getJournalEntryForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("JournalEntryNotFoundMessage");
                return RedirectOrHtmx(nameof(JournalEntries), new { });
            }

            var vm = new JournalEntryEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                EntryDateUtc = dto.EntryDateUtc,
                Description = dto.Description,
                Lines = dto.Lines.Select(x => new JournalEntryLineVm
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    DebitMinor = x.DebitMinor,
                    CreditMinor = x.CreditMinor,
                    Memo = x.Memo
                }).ToList()
            };
            EnsureJournalEntryRows(vm);
            await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderJournalEntryEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJournalEntry(JournalEntryEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty || vm.BusinessId == Guid.Empty)
            {
                SetErrorMessage(vm.Id == Guid.Empty ? "JournalEntryNotFoundMessage" : "JournalEntryUpdateFailedMessage");
                return RedirectOrHtmx(nameof(JournalEntries), new { businessId = vm.BusinessId == Guid.Empty ? (Guid?)null : vm.BusinessId });
            }

            if (!ModelState.IsValid)
            {
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderJournalEntryEditor(vm, isCreate: false);
            }

            var dto = new JournalEntryEditDto
            {
                Id = vm.Id,
                RowVersion = vm.RowVersion,
                BusinessId = vm.BusinessId,
                EntryDateUtc = vm.EntryDateUtc,
                Description = vm.Description,
                Lines = vm.Lines.Select(x => new JournalEntryLineDto
                {
                    Id = x.Id,
                    AccountId = x.AccountId,
                    DebitMinor = x.DebitMinor,
                    CreditMinor = x.CreditMinor,
                    Memo = x.Memo
                }).ToList()
            };

            try
            {
                await _updateJournalEntry.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("JournalEntryUpdatedMessage");
                return RedirectOrHtmx(nameof(EditJournalEntry), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("JournalEntryConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditJournalEntry), new { id = vm.Id });
            }
            catch (Exception)
            {
                AddModelErrorMessage("JournalEntryUpdateFailedMessage");
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderJournalEntryEditor(vm, isCreate: false);
            }
        }

        private async Task PopulatePaymentOptionsAsync(PaymentEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private async Task PopulateFinancialAccountOptionsAsync(FinancialAccountEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
        }

        private async Task PopulateExpenseOptionsAsync(ExpenseEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            if (vm.BusinessId != Guid.Empty)
            {
                vm.SupplierOptions = await _referenceData.GetSupplierOptionsAsync(vm.BusinessId, vm.SupplierId, includeEmpty: true, ct).ConfigureAwait(false);
            }
        }

        private async Task PopulateJournalEntryOptionsAsync(JournalEntryEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            if (vm.BusinessId != Guid.Empty)
            {
                vm.AccountOptions = await _referenceData.GetFinancialAccountOptionsAsync(vm.BusinessId, null, includeEmpty: false, ct).ConfigureAwait(false);
            }
        }

        private static void EnsureJournalEntryRows(JournalEntryEditVm vm)
        {
            vm.Lines ??= new List<JournalEntryLineVm>();
            while (vm.Lines.Count < 2)
            {
                vm.Lines.Add(new JournalEntryLineVm());
            }
        }

        private IActionResult RenderPaymentEditor(PaymentEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Billing/_PaymentEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreatePayment", vm) : View("EditPayment", vm);
        }

        private static bool HasProviderCorrelation(PaymentEditDto dto) =>
            !string.IsNullOrWhiteSpace(dto.ProviderTransactionRef) ||
            !string.IsNullOrWhiteSpace(dto.ProviderPaymentIntentRef) ||
            !string.IsNullOrWhiteSpace(dto.ProviderCheckoutSessionRef);

        private IActionResult RenderFinancialAccountEditor(FinancialAccountEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Billing/_FinancialAccountEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateFinancialAccount", vm) : View("EditFinancialAccount", vm);
        }

        private IActionResult RenderExpenseEditor(ExpenseEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Billing/_ExpenseEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateExpense", vm) : View("EditExpense", vm);
        }

        private IActionResult RenderJournalEntryEditor(JournalEntryEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Billing/_JournalEntryEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreateJournalEntry", vm) : View("EditJournalEntry", vm);
        }

        private async Task<FinancialAccountOpsSummaryVm> BuildFinancialAccountOpsSummaryVmAsync(Guid businessId, CancellationToken ct)
        {
            var summary = await _getAccountsPage.GetSummaryAsync(businessId, ct).ConfigureAwait(false);
            return new FinancialAccountOpsSummaryVm
            {
                TotalCount = summary.TotalCount,
                AssetCount = summary.AssetCount,
                RevenueCount = summary.RevenueCount,
                ExpenseCount = summary.ExpenseCount,
                MissingCodeCount = summary.MissingCodeCount
            };
        }

        private async Task<JournalEntryOpsSummaryVm> BuildJournalEntryOpsSummaryVmAsync(Guid businessId, CancellationToken ct)
        {
            var summary = await _getJournalEntriesPage.GetSummaryAsync(businessId, ct).ConfigureAwait(false);
            return new JournalEntryOpsSummaryVm
            {
                TotalCount = summary.TotalCount,
                RecentCount = summary.RecentCount,
                MultiLineCount = summary.MultiLineCount
            };
        }

        private async Task<ExpenseOpsSummaryVm> BuildExpenseOpsSummaryVmAsync(Guid businessId, CancellationToken ct)
        {
            var summary = await _getExpensesPage.GetSummaryAsync(businessId, ct).ConfigureAwait(false);
            return new ExpenseOpsSummaryVm
            {
                TotalCount = summary.TotalCount,
                SupplierLinkedCount = summary.SupplierLinkedCount,
                RecentCount = summary.RecentCount,
                HighValueCount = summary.HighValueCount
            };
        }

        private List<ProviderPlaybookVm> BuildFinancialAccountPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = T("FinancialAccountPlaybookAssetsTitle"),
                    ScopeNote = T("FinancialAccountPlaybookAssetsScope"),
                    OperatorAction = T("FinancialAccountPlaybookAssetsAction"),
                    SettingsDependency = T("FinancialAccountPlaybookAssetsDependency")
                },
                new()
                {
                    Title = T("FinancialAccountPlaybookMappingTitle"),
                    ScopeNote = T("FinancialAccountPlaybookMappingScope"),
                    OperatorAction = T("FinancialAccountPlaybookMappingAction"),
                    SettingsDependency = T("FinancialAccountPlaybookMappingDependency")
                }
            };
        }

        private List<ProviderPlaybookVm> BuildJournalEntryPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = T("JournalEntryPlaybookRecentTitle"),
                    ScopeNote = T("JournalEntryPlaybookRecentScope"),
                    OperatorAction = T("JournalEntryPlaybookRecentAction"),
                    SettingsDependency = T("JournalEntryPlaybookRecentDependency")
                },
                new()
                {
                    Title = T("JournalEntryPlaybookMultilineTitle"),
                    ScopeNote = T("JournalEntryPlaybookMultilineScope"),
                    OperatorAction = T("JournalEntryPlaybookMultilineAction"),
                    SettingsDependency = T("JournalEntryPlaybookMultilineDependency")
                }
            };
        }

        private List<ProviderPlaybookVm> BuildExpensePlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = T("ExpensePlaybookRecentTitle"),
                    ScopeNote = T("ExpensePlaybookRecentScope"),
                    OperatorAction = T("ExpensePlaybookRecentAction"),
                    SettingsDependency = T("ExpensePlaybookRecentDependency")
                },
                new()
                {
                    Title = T("ExpensePlaybookHighValueTitle"),
                    ScopeNote = T("ExpensePlaybookHighValueScope"),
                    OperatorAction = T("ExpensePlaybookHighValueAction"),
                    SettingsDependency = T("ExpensePlaybookHighValueDependency")
                }
            };
        }

        private List<ProviderPlaybookVm> BuildTaxCompliancePlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = T("TaxCompliancePlaybookVatCleanupTitle"),
                    ScopeNote = T("TaxCompliancePlaybookVatCleanupScope"),
                    OperatorAction = T("TaxCompliancePlaybookVatCleanupAction"),
                    SettingsDependency = T("TaxCompliancePlaybookVatCleanupDependency")
                },
                new()
                {
                    Title = T("TaxCompliancePlaybookIssuerTitle"),
                    ScopeNote = T("TaxCompliancePlaybookIssuerScope"),
                    OperatorAction = T("TaxCompliancePlaybookIssuerAction"),
                    SettingsDependency = T("TaxCompliancePlaybookIssuerDependency")
                },
                new()
                {
                    Title = T("TaxCompliancePlaybookInvoiceReviewTitle"),
                    ScopeNote = T("TaxCompliancePlaybookInvoiceReviewScope"),
                    OperatorAction = T("TaxCompliancePlaybookInvoiceReviewAction"),
                    SettingsDependency = T("TaxCompliancePlaybookInvoiceReviewDependency")
                }
            };
        }

        private void PopulateBillingPlanOptions(BillingPlanEditVm vm)
        {
            vm.IntervalItems = Enum.GetValues<BillingInterval>()
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T(x.ToString()), x.ToString(), x == vm.Interval))
                .ToList();
        }

        private IActionResult RenderPlansWorkspace(BillingPlansListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/Plans.cshtml", vm);
            }

            return View("Plans", vm);
        }

        private IActionResult RenderPaymentsWorkspace(PaymentsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/Payments.cshtml", vm);
            }

            return View("Payments", vm);
        }

        private IActionResult RenderSubscriptionsWorkspace(BusinessSubscriptionsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/Subscriptions.cshtml", vm);
            }

            return View("Subscriptions", vm);
        }

        private IActionResult RenderTaxComplianceWorkspace(TaxComplianceListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/TaxCompliance.cshtml", vm);
            }

            return View("TaxCompliance", vm);
        }

        private IActionResult RenderWebhooksWorkspace(BillingWebhooksListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/Webhooks.cshtml", vm);
            }

            return View("Webhooks", vm);
        }

        private IActionResult RenderRefundsWorkspace(RefundsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/Refunds.cshtml", vm);
            }

            return View("Refunds", vm);
        }

        private IActionResult RenderFinancialAccountsWorkspace(FinancialAccountsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/FinancialAccounts.cshtml", vm);
            }

            return View("FinancialAccounts", vm);
        }

        private IActionResult RenderExpensesWorkspace(ExpensesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/Expenses.cshtml", vm);
            }

            return View("Expenses", vm);
        }

        private IActionResult RenderJournalEntriesWorkspace(JournalEntriesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Billing/JournalEntries.cshtml", vm);
            }

            return View("JournalEntries", vm);
        }

        private IActionResult RenderBillingPlanEditor(BillingPlanEditVm vm, bool isCreate)
        {
            if (IsHtmxRequest())
            {
                ViewData["IsCreate"] = isCreate;
                return PartialView("~/Views/Billing/_BillingPlanEditorShell.cshtml", vm);
            }

            return isCreate ? View("CreatePlan", vm) : View("EditPlan", vm);
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

