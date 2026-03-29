using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.Billing
{
    /// <summary>
    /// Admin billing controller for operational finance screens.
    /// </summary>
    public sealed class BillingController : AdminBaseController
    {
        private readonly GetPaymentsPageHandler _getPaymentsPage;
        private readonly GetPaymentOpsSummaryHandler _getPaymentOpsSummary;
        private readonly GetBillingPlansAdminPageHandler _getBillingPlansAdminPage;
        private readonly GetBillingPlanOpsSummaryHandler _getBillingPlanOpsSummary;
        private readonly GetBillingPlanForEditHandler _getBillingPlanForEdit;
        private readonly GetBillingWebhookSubscriptionsPageHandler _getBillingWebhookSubscriptionsPage;
        private readonly GetBillingWebhookDeliveriesPageHandler _getBillingWebhookDeliveriesPage;
        private readonly GetBillingWebhookOpsSummaryHandler _getBillingWebhookOpsSummary;
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

        public BillingController(
            GetPaymentsPageHandler getPaymentsPage,
            GetPaymentOpsSummaryHandler getPaymentOpsSummary,
            GetBillingPlansAdminPageHandler getBillingPlansAdminPage,
            GetBillingPlanOpsSummaryHandler getBillingPlanOpsSummary,
            GetBillingPlanForEditHandler getBillingPlanForEdit,
            GetBillingWebhookSubscriptionsPageHandler getBillingWebhookSubscriptionsPage,
            GetBillingWebhookDeliveriesPageHandler getBillingWebhookDeliveriesPage,
            GetBillingWebhookOpsSummaryHandler getBillingWebhookOpsSummary,
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
            ISiteSettingCache siteSettingCache)
        {
            _getPaymentsPage = getPaymentsPage;
            _getPaymentOpsSummary = getPaymentOpsSummary;
            _getBillingPlansAdminPage = getBillingPlansAdminPage;
            _getBillingPlanOpsSummary = getBillingPlanOpsSummary;
            _getBillingPlanForEdit = getBillingPlanForEdit;
            _getBillingWebhookSubscriptionsPage = getBillingWebhookSubscriptionsPage;
            _getBillingWebhookDeliveriesPage = getBillingWebhookDeliveriesPage;
            _getBillingWebhookOpsSummary = getBillingWebhookOpsSummary;
            _getPaymentForEdit = getPaymentForEdit;
            _getRefundsPage = getRefundsPage;
            _getRefundOpsSummary = getRefundOpsSummary;
            _createBillingPlan = createBillingPlan;
            _updateBillingPlan = updateBillingPlan;
            _createPayment = createPayment;
            _updatePayment = updatePayment;
            _getAccountsPage = getAccountsPage;
            _getAccountForEdit = getAccountForEdit;
            _createAccount = createAccount;
            _updateAccount = updateAccount;
            _getExpensesPage = getExpensesPage;
            _getExpenseForEdit = getExpenseForEdit;
            _createExpense = createExpense;
            _updateExpense = updateExpense;
            _getJournalEntriesPage = getJournalEntriesPage;
            _getJournalEntryForEdit = getJournalEntryForEdit;
            _createJournalEntry = createJournalEntry;
            _updateJournalEntry = updateJournalEntry;
            _referenceData = referenceData;
            _siteSettingCache = siteSettingCache;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Payments));

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
                TempData["Success"] = "Billing plan created.";
                return RedirectOrHtmx(nameof(EditPlan), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                PopulateBillingPlanOptions(vm);
                return RenderBillingPlanEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPlan(Guid id, CancellationToken ct = default)
        {
            var dto = await _getBillingPlanForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Billing plan not found.";
                return RedirectToAction(nameof(Plans));
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
                TempData["Success"] = "Billing plan updated.";
                return RedirectToAction(nameof(EditPlan), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the billing plan and try again.";
                return RedirectToAction(nameof(EditPlan), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                PopulateBillingPlanOptions(vm);
                return RenderBillingPlanEditor(vm, isCreate: false);
            }
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
                    FailureReason = x.FailureReason,
                    PaidAtUtc = x.PaidAtUtc,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RefundedAmountMinor = x.RefundedAmountMinor,
                    NetCapturedAmountMinor = x.NetCapturedAmountMinor,
                    IsStripe = x.IsStripe,
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
                    SubscriptionId = x.SubscriptionId,
                    EventType = x.EventType,
                    CallbackUrl = x.CallbackUrl,
                    Status = x.Status,
                    RetryCount = x.RetryCount,
                    ResponseCode = x.ResponseCode,
                    CreatedAtUtc = x.CreatedAtUtc,
                    LastAttemptAtUtc = x.LastAttemptAtUtc,
                    IdempotencyKey = x.IdempotencyKey,
                    IsActiveSubscription = x.IsActiveSubscription
                }).ToList(),
                Playbooks = BuildWebhookPlaybooks(),
                Page = page,
                PageSize = pageSize,
                Total = deliveries.Total
            };

            return RenderWebhooksWorkspace(vm);
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
                    PaymentStatus = x.PaymentStatus,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    CustomerEmail = x.CustomerEmail,
                    AmountMinor = x.AmountMinor,
                    Currency = x.Currency,
                    Reason = x.Reason,
                    Status = x.Status,
                    CreatedAtUtc = x.CreatedAtUtc,
                    CompletedAtUtc = x.CompletedAtUtc,
                    IsStripe = x.IsStripe,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new RefundsListVm
            {
                BusinessId = businessId,
                Query = q ?? string.Empty,
                QueueFilter = queue,
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
                FailedStripeCount = summary.FailedStripeCount
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

        private async Task<TaxOperationsVm> BuildTaxOperationsVmAsync(CancellationToken ct)
        {
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            return new TaxOperationsVm
            {
                VatEnabled = settings.VatEnabled,
                DefaultVatRatePercent = settings.DefaultVatRatePercent,
                PricesIncludeVat = settings.PricesIncludeVat,
                AllowReverseCharge = settings.AllowReverseCharge,
                IssuerConfigured =
                    !string.IsNullOrWhiteSpace(settings.InvoiceIssuerLegalName) &&
                    !string.IsNullOrWhiteSpace(settings.InvoiceIssuerAddressLine1) &&
                    !string.IsNullOrWhiteSpace(settings.InvoiceIssuerPostalCode) &&
                    !string.IsNullOrWhiteSpace(settings.InvoiceIssuerCity) &&
                    !string.IsNullOrWhiteSpace(settings.InvoiceIssuerCountry),
                InvoiceIssuerLegalName = settings.InvoiceIssuerLegalName ?? string.Empty,
                InvoiceIssuerCountry = settings.InvoiceIssuerCountry ?? string.Empty,
                InvoiceIssuerTaxIdConfigured = !string.IsNullOrWhiteSpace(settings.InvoiceIssuerTaxId)
            };
        }

        private async Task<RefundOpsSummaryVm> BuildRefundOpsSummaryVmAsync(Guid businessId, CancellationToken ct)
        {
            var summary = await _getRefundOpsSummary.HandleAsync(businessId, ct).ConfigureAwait(false);
            return new RefundOpsSummaryVm
            {
                PendingCount = summary.PendingCount,
                CompletedCount = summary.CompletedCount,
                FailedCount = summary.FailedCount,
                StripeCount = summary.StripeCount
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
                RetryPendingCount = summary.RetryPendingCount
            };
        }

        private static List<ProviderPlaybookVm> BuildPaymentPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "Pending payments",
                    ScopeNote = "Use this for payments waiting on capture or manual completion.",
                    OperatorAction = "Review the order and payment rows, then record or correct payment status from the payment editor when the provider outcome is known.",
                    SettingsDependency = "Stripe secret key and merchant identity should already be configured before live capture workflows are relied on."
                },
                new()
                {
                    Title = "Stripe rows without provider reference",
                    ScopeNote = "These rows are risky because callback/reconciliation correlation is weak.",
                    OperatorAction = "Open the payment or linked order, verify the real Stripe intent/charge id, and document or correct the provider reference before treating the record as operationally complete.",
                    SettingsDependency = "Stripe webhook secret and secret key should be configured before assuming provider-linked automation is trustworthy."
                },
                new()
                {
                    Title = "Failed payments",
                    ScopeNote = "Treat these as support items first, not silent retries.",
                    OperatorAction = "Open the linked order/customer, verify the provider reference or failure state, and only add or edit payments after support validation.",
                    SettingsDependency = "Webhook secret and provider credentials must be configured before trusting automated Stripe lifecycle handling."
                },
                new()
                {
                    Title = "Unlinked or provider-linked rows",
                    ScopeNote = "These rows usually represent reconciliation or data-hygiene work.",
                    OperatorAction = "Link order/invoice context where known, or keep the row documented as a standalone support payment until Stripe-specific reconciliation matures.",
                    SettingsDependency = "Phase-1 Stripe readiness should be green before treating provider-linked rows as production-grade evidence."
                }
            };
        }

        private static List<ProviderPlaybookVm> BuildRefundPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "Pending refunds",
                    ScopeNote = "Use this queue for refunds that have been requested but are not yet operationally settled.",
                    OperatorAction = "Open the linked order and payment, confirm the provider-side outcome, and only leave the refund pending when support expects a later completion signal.",
                    SettingsDependency = "Stripe webhook and provider references should be trusted before using pending state as anything more than a manual support marker."
                },
                new()
                {
                    Title = "Completed refunds",
                    ScopeNote = "These rows represent already-recorded customer giveback and should stay aligned with payment and invoice context.",
                    OperatorAction = "Review linked payment, invoice, and order settlement together when customers ask about refunded value or net collected totals.",
                    SettingsDependency = "Stripe readiness and provider reference hygiene should already be green before these rows are treated as reconciliation evidence."
                },
                new()
                {
                    Title = "Failed or provider-sensitive refunds",
                    ScopeNote = "Use this subset when refund status or Stripe handling needs explicit finance support review.",
                    OperatorAction = "Start from the refund row, then move into the linked payment workbench or order refund flow to correct support notes and retry paths manually.",
                    SettingsDependency = "Webhook/callback audit depth is still near-term, so operator review remains the source of truth for failed refund follow-up."
                }
            };
        }

        private static List<ProviderPlaybookVm> BuildBillingPlanPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "Active plans in live use",
                    ScopeNote = "Plan edits affect future subscription handoff and current support expectations for businesses already on those plans.",
                    OperatorAction = "Review active-subscription count before changing price, trial, or availability so support knows whether a plan change is safe or rollout-sensitive.",
                    SettingsDependency = "Business management website and Stripe readiness should already be configured before operators rely on plan handoff."
                },
                new()
                {
                    Title = "Trial and missing-feature plans",
                    ScopeNote = "Trial-heavy or feature-empty plans usually indicate packaging debt rather than just pricing setup.",
                    OperatorAction = "Normalize trial duration and enrich feature metadata before using the plan as a business-facing offer in onboarding or upgrade workflows.",
                    SettingsDependency = "No extra provider secret is required, but external billing handoff depends on business-management website readiness."
                }
            };
        }

        private static List<ProviderPlaybookVm> BuildWebhookPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "Pending deliveries",
                    ScopeNote = "Treat pending webhook rows as lifecycle visibility, not proof that payment automation has completed.",
                    OperatorAction = "Review event type, callback target, and attempt timeline before assuming Stripe-side updates have reached the platform.",
                    SettingsDependency = "Stripe webhook secret and payment settings should be configured before pending rows are used as operational evidence."
                },
                new()
                {
                    Title = "Failed deliveries",
                    ScopeNote = "Use this queue when provider callback correlation is weak or callback endpoints are unhealthy.",
                    OperatorAction = "Review callback URL, response code, retry count, and the linked payment/provider reference, then escalate through settings or infrastructure rather than editing finance records blindly.",
                    SettingsDependency = "Webhook endpoint registration and Stripe secret must be correct before failed deliveries are treated as application bugs."
                },
                new()
                {
                    Title = "Retry-pending deliveries",
                    ScopeNote = "These rows show callbacks that have already had at least one failed/manual retry path.",
                    OperatorAction = "Use them to prioritize reconciliation review and confirm whether payment/refund state must be corrected manually in WebAdmin.",
                    SettingsDependency = "Phase-1 still has no automated resend daemon, so operator review remains the source of truth."
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> CreatePayment(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new PaymentEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                CreatedAtUtc = DateTime.UtcNow,
                Currency = "EUR",
                Status = PaymentStatus.Pending,
                SupportPlaybooks = BuildPaymentSupportPlaybooks(new PaymentEditDto
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    Currency = "EUR",
                    Status = PaymentStatus.Pending
                })
            };

            await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderPaymentEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(PaymentEditVm vm, CancellationToken ct = default)
        {
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
                PaidAtUtc = vm.PaidAtUtc
            };

            try
            {
                var id = await _createPayment.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Payment created.";
                return RedirectOrHtmx(nameof(EditPayment), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPaymentEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditPayment(Guid id, CancellationToken ct = default)
        {
            var dto = await _getPaymentForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Payment not found.";
                return RedirectToAction(nameof(Payments));
            }

            var vm = new PaymentEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                CreatedAtUtc = dto.CreatedAtUtc,
                IsStripe = dto.IsStripe,
                FailureReason = dto.FailureReason,
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
                SupportPlaybooks = BuildPaymentSupportPlaybooks(dto)
            };

            await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderPaymentEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPayment(PaymentEditVm vm, CancellationToken ct = default)
        {
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
                PaidAtUtc = vm.PaidAtUtc
            };

            try
            {
                await _updatePayment.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Payment updated.";
                return RedirectOrHtmx(nameof(EditPayment), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the payment and try again.";
                return RedirectToAction(nameof(EditPayment), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderPaymentEditor(vm, isCreate: false);
            }
        }

        private static List<PaymentSupportPlaybookVm> BuildPaymentSupportPlaybooks(PaymentEditDto dto)
        {
            var items = new List<PaymentSupportPlaybookVm>
            {
                new()
                {
                    Title = "Provider correlation",
                    ScopeNote = string.IsNullOrWhiteSpace(dto.ProviderTransactionRef)
                        ? "This payment does not have a provider reference yet."
                        : "This payment already has a provider reference and should stay aligned with provider evidence.",
                    OperatorAction = string.IsNullOrWhiteSpace(dto.ProviderTransactionRef)
                        ? "Verify the real provider-side reference before treating this record as reconciled. If support confirms an offline/manual payment, keep the row documented clearly."
                        : "Use the provider reference when matching support tickets, refunds, or invoice disputes. Avoid overwriting it unless the current value is clearly wrong."
                },
                new()
                {
                    Title = "Failure and refund review",
                    ScopeNote = !string.IsNullOrWhiteSpace(dto.FailureReason)
                        ? "This payment carries a recorded failure reason and needs operator review before assuming recovery."
                        : dto.Refunds.Count > 0
                            ? "This payment already has refund activity and should be reviewed together with order/invoice context."
                            : "Use this payment as the source of truth for payment, refund, and invoice coordination.",
                    OperatorAction = !string.IsNullOrWhiteSpace(dto.FailureReason)
                        ? "Review the linked order, invoice, and customer before changing status. Keep the failure note unless support has verified a corrected outcome."
                        : dto.Refunds.Count > 0
                            ? "Review the refund timeline below and keep payment status, refund status, and invoice settlement aligned."
                            : "If a refund or settlement issue appears later, start from this workspace and then follow the linked order or invoice."
                }
            };

            if (dto.IsStripe)
            {
                items.Add(new PaymentSupportPlaybookVm
                {
                    Title = "Stripe-first support path",
                    ScopeNote = "Stripe is the only phase-1 payment provider expected to be production-ready.",
                    OperatorAction = "Check Stripe readiness from the payments queue, then validate provider reference, failure reason, and linked order/invoice before changing local status."
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
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateFinancialAccount(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new FinancialAccountEditVm
            {
                BusinessId = businessId ?? Guid.Empty
            };
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return RenderFinancialAccountEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFinancialAccount(FinancialAccountEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
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
                TempData["Success"] = "Financial account created.";
                return RedirectOrHtmx(nameof(EditFinancialAccount), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderFinancialAccountEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditFinancialAccount(Guid id, CancellationToken ct = default)
        {
            var dto = await _getAccountForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Financial account not found.";
                return RedirectToAction(nameof(FinancialAccounts));
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
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            return RenderFinancialAccountEditor(vm, isCreate: false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinancialAccount(FinancialAccountEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
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
                TempData["Success"] = "Financial account updated.";
                return RedirectToAction(nameof(EditFinancialAccount), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the financial account and try again.";
                return RedirectToAction(nameof(EditFinancialAccount), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return RenderFinancialAccountEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Expenses(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
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
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateExpense(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new ExpenseEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                ExpenseDateUtc = DateTime.UtcNow
            };
            await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderExpenseEditor(vm, isCreate: true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense(ExpenseEditVm vm, CancellationToken ct = default)
        {
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
                TempData["Success"] = "Expense created.";
                return RedirectOrHtmx(nameof(EditExpense), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderExpenseEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditExpense(Guid id, CancellationToken ct = default)
        {
            var dto = await _getExpenseForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Expense not found.";
                return RedirectToAction(nameof(Expenses));
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
                TempData["Success"] = "Expense updated.";
                return RedirectToAction(nameof(EditExpense), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the expense and try again.";
                return RedirectToAction(nameof(EditExpense), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderExpenseEditor(vm, isCreate: false);
            }
        }

        [HttpGet]
        public async Task<IActionResult> JournalEntries(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, JournalEntryQueueFilter? queue = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
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
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateJournalEntry(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new JournalEntryEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                EntryDateUtc = DateTime.UtcNow,
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
                TempData["Success"] = "Journal entry created.";
                return RedirectOrHtmx(nameof(EditJournalEntry), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderJournalEntryEditor(vm, isCreate: true);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditJournalEntry(Guid id, CancellationToken ct = default)
        {
            var dto = await _getJournalEntryForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Journal entry not found.";
                return RedirectToAction(nameof(JournalEntries));
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
                TempData["Success"] = "Journal entry updated.";
                return RedirectToAction(nameof(EditJournalEntry), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the journal entry and try again.";
                return RedirectToAction(nameof(EditJournalEntry), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
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
            if (vm.Lines.Count == 0)
            {
                vm.Lines.Add(new JournalEntryLineVm());
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

        private static List<ProviderPlaybookVm> BuildFinancialAccountPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "Assets without clean coding",
                    ScopeNote = "Asset accounts become a support liability when operators cannot tell which ledger bucket is actually canonical.",
                    OperatorAction = "Normalize code and name before payment, inventory, or manual journal flows depend on the account.",
                    SettingsDependency = "Keep invoice/VAT settings aligned so downstream exports remain understandable."
                },
                new()
                {
                    Title = "Revenue and expense mapping review",
                    ScopeNote = "Operational finance support depends on clear separation between incoming revenue and outgoing expenses.",
                    OperatorAction = "Confirm revenue and expense accounts exist with stable codes before scaling manual posting or export workflows.",
                    SettingsDependency = "No extra provider setting; this is business-scoped accounting hygiene."
                }
            };
        }

        private static List<ProviderPlaybookVm> BuildJournalEntryPlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "Recent journal review",
                    ScopeNote = "Recent entries help finance support correlate operational incidents with accounting impact.",
                    OperatorAction = "Review recent entries first when reconciling payment, refund, or invoice anomalies.",
                    SettingsDependency = "Use alongside current payment/refund queues and VAT policy visibility."
                },
                new()
                {
                    Title = "Multi-line journal review",
                    ScopeNote = "Multi-line entries usually indicate more complex accounting events and deserve a manual pass.",
                    OperatorAction = "Open the entry, review coding and memo quality, and confirm the split is audit-friendly.",
                    SettingsDependency = "No extra provider setting; this is a finance-control surface."
                }
            };
        }

        private static List<ProviderPlaybookVm> BuildExpensePlaybooks()
        {
            return new List<ProviderPlaybookVm>
            {
                new()
                {
                    Title = "Recent expense review",
                    ScopeNote = "Recent expenses are the fastest path for spotting supplier or operating-cost anomalies.",
                    OperatorAction = "Review the latest rows first when reconciling supplier disputes, payment issues, or unexpected cost spikes.",
                    SettingsDependency = "No extra provider setting; this is a business-scoped finance review surface."
                },
                new()
                {
                    Title = "High-value expense review",
                    ScopeNote = "Higher-value expenses deserve a manual pass before they quietly distort margin or cash reporting.",
                    OperatorAction = "Open the expense, confirm amount/category accuracy, and use the supplier deep-link when external vendor follow-up is needed.",
                    SettingsDependency = "Pairs well with supplier administration and journal-entry review."
                }
            };
        }

        private static void PopulateBillingPlanOptions(BillingPlanEditVm vm)
        {
            vm.IntervalItems = Enum.GetValues<BillingInterval>()
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.ToString(), x.ToString(), x == vm.Interval))
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
