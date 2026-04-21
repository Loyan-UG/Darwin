using FluentAssertions;

namespace Darwin.Tests.Unit.Security;

public sealed class SecurityAndPerformanceWebAdminSurfacesSourceTests : SecurityAndPerformanceSourceTestBase
{

    [Fact]
    public void WebAdminOperationalControlCenter_Should_KeepCoreAdminModulesReachable()
    {
        var homeSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "HomeController.cs"));
        var businessesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var communicationsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessCommunicationsController.cs"));
        var mobileOpsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Mobile", "MobileOperationsController.cs"));
        var siteSettingsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Settings", "SiteSettingsController.cs"));
        var ordersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Orders", "OrdersController.cs"));
        var billingSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Billing", "BillingController.cs"));
        var crmSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "CRM", "CrmController.cs"));
        var inventorySource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var loyaltySource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));
        var mediaSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Media", "MediaController.cs"));
        var pagesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "CMS", "PagesController.cs"));
        var productsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "ProductsController.cs"));
        var categoriesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "CategoriesController.cs"));
        var brandsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "BrandsController.cs"));
        var addOnGroupsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "AddOnGroupsController.cs"));
        var usersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));
        var rolesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "RolesController.cs"));
        var permissionsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "PermissionsController.cs"));
        var shippingSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Shipping", "ShippingMethodsController.cs"));

        homeSource.Should().Contain("public async Task<IActionResult> Index(");
        businessesSource.Should().Contain("public async Task<IActionResult> Index(");
        businessesSource.Should().Contain("public async Task<IActionResult> Create(");
        businessesSource.Should().Contain("public async Task<IActionResult> Edit(");
        businessesSource.Should().Contain("public async Task<IActionResult> Setup(");
        businessesSource.Should().Contain("public async Task<IActionResult> Members(");
        businessesSource.Should().Contain("public async Task<IActionResult> Invitations(");
        businessesSource.Should().Contain("public async Task<IActionResult> ProvisionSupportCustomer(");
        communicationsSource.Should().Contain("public async Task<IActionResult> Index(");
        communicationsSource.Should().Contain("public async Task<IActionResult> Details(");
        mobileOpsSource.Should().Contain("public async Task<IActionResult> Index(");
        siteSettingsSource.Should().Contain("public async Task<IActionResult> Edit(");
        ordersSource.Should().Contain("public async Task<IActionResult> Index(");
        ordersSource.Should().Contain("public async Task<IActionResult> Details(");
        billingSource.Should().Contain("public async Task<IActionResult> Plans(");
        billingSource.Should().Contain("public async Task<IActionResult> Payments(");
        crmSource.Should().Contain("public async Task<IActionResult> Index(");
        crmSource.Should().Contain("public async Task<IActionResult> CreateCustomer(");
        inventorySource.Should().Contain("public async Task<IActionResult> Warehouses(");
        inventorySource.Should().Contain("public async Task<IActionResult> Suppliers(");
        inventorySource.Should().Contain("public async Task<IActionResult> StockLevels(");
        inventorySource.Should().Contain("public async Task<IActionResult> PurchaseOrders(");
        loyaltySource.Should().Contain("public async Task<IActionResult> Programs(");
        loyaltySource.Should().Contain("public async Task<IActionResult> Accounts(");
        loyaltySource.Should().Contain("public async Task<IActionResult> Campaigns(");
        mediaSource.Should().Contain("public async Task<IActionResult> Index(");
        pagesSource.Should().Contain("public async Task<IActionResult> Index(");
        productsSource.Should().Contain("public async Task<IActionResult> Index(");
        categoriesSource.Should().Contain("public async Task<IActionResult> Index(");
        brandsSource.Should().Contain("public async Task<IActionResult> Index(");
        addOnGroupsSource.Should().Contain("public async Task<IActionResult> Index(");
        usersSource.Should().Contain("public async Task<IActionResult> Index(");
        rolesSource.Should().Contain("public async Task<IActionResult> Index(");
        permissionsSource.Should().Contain("public async Task<IActionResult> Index(");
        shippingSource.Should().Contain("public async Task<IActionResult> Index(");
    }

    [Fact]
    public void MemberAndLoyaltyControllers_Should_RemainAuthenticated()
    {
        ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberBusinessesController.cs"))
            .Should().Contain("[Authorize]");
        ReadWebApiFile(Path.Combine("Controllers", "Loyalty", "LoyaltyController.cs"))
            .Should().Contain("[Authorize]");
        ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessLoyaltyController.cs"))
            .Should().Contain("[Authorize]");
        ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAccountController.cs"))
            .Should().Contain("[Authorize]");
    }


    [Fact]
    public void LoyaltyController_Should_KeepAuthenticatedMemberRoutesAndPolicies()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/member/loyalty\")]");
        source.Should().Contain("[Route(\"api/v1/loyalty\")]");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessMemberArea\")]");
        source.Should().Contain("public async Task<IActionResult> PrepareScanSessionAsync(");
        source.Should().Contain("[HttpPost(\"scan/prepare\")]");
        source.Should().Contain("public async Task<IActionResult> GetMyAccountsAsync(");
        source.Should().Contain("[HttpGet(\"my/accounts\")]");
        source.Should().Contain("public async Task<IActionResult> GetMyOverviewAsync(");
        source.Should().Contain("[HttpGet(\"my/overview\")]");
        source.Should().Contain("public async Task<IActionResult> JoinLoyaltyAsync(");
    }


    [Fact]
    public void BusinessLoyaltyController_Should_KeepAuthenticatedBusinessRoutesAndPolicies()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessLoyaltyController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/business/loyalty\")]");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessLoyaltyBusiness\")]");
        source.Should().Contain("public async Task<IActionResult> GetBusinessRewardConfigurationAsync(");
        source.Should().Contain("[HttpGet(\"reward-config\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/loyalty/business/reward-config\")]");
        source.Should().Contain("public async Task<IActionResult> CreateBusinessRewardTierAsync(");
        source.Should().Contain("[HttpPost(\"reward-config/tiers\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/loyalty/business/reward-config/tiers\")]");
        source.Should().Contain("public async Task<IActionResult> ProcessScanSessionForBusinessAsync(");
    }


    [Fact]
    public void BusinessBillingController_Should_KeepAuthenticatedPolicyGatedBillingEndpoints()
    {
        var source = ReadWebApiFile(Path.Combine("Controllers", "Billing", "BillingController.cs"));

        source.Should().Contain("[Authorize]");
        source.Should().Contain("[Route(\"api/v1/business/billing\")]");
        source.Should().Contain("[Authorize(Policy = \"perm:AccessLoyaltyBusiness\")]");
        source.Should().Contain("public async Task<IActionResult> GetCurrentBusinessSubscriptionAsync(");
        source.Should().Contain("[HttpGet(\"subscription/current\")]");
        source.Should().Contain("[HttpGet(\"/api/v1/billing/business/subscription/current\")]");
        source.Should().Contain("public async Task<IActionResult> SetCancelAtPeriodEndAsync(");
        source.Should().Contain("[HttpPost(\"subscription/cancel-at-period-end\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/billing/business/subscription/cancel-at-period-end\")]");
        source.Should().Contain("public async Task<IActionResult> CreateSubscriptionCheckoutIntentAsync(");
        source.Should().Contain("[HttpPost(\"subscription/checkout-intent\")]");
        source.Should().Contain("[HttpPost(\"/api/v1/billing/business/subscription/checkout-intent\")]");
    }


    [Fact]
    public void BillingController_Should_KeepFinancialMutationPostsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Billing", "BillingController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreatePlan(BillingPlanEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditPlan(BillingPlanEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreatePayment(PaymentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditPayment(PaymentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateFinancialAccount(FinancialAccountEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditFinancialAccount(FinancialAccountEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateExpense(ExpenseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditExpense(ExpenseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateJournalEntry(JournalEntryEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditJournalEntry(JournalEntryEditVm vm");
    }


    [Fact]
    public void BillingController_Should_KeepAdminFinanceWorkspacesAndEditorsReachable()
    {
        var billingSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Billing", "BillingController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        billingSource.Should().Contain("public sealed class BillingController : AdminBaseController");
        billingSource.Should().Contain("public IActionResult Index() => RedirectOrHtmx(nameof(Payments), new { });");
        billingSource.Should().Contain("public async Task<IActionResult> Plans(");
        billingSource.Should().Contain("public async Task<IActionResult> Payments(");
        billingSource.Should().Contain("public async Task<IActionResult> TaxCompliance(");
        billingSource.Should().Contain("public async Task<IActionResult> Webhooks(");
        billingSource.Should().Contain("public async Task<IActionResult> Refunds(");
        billingSource.Should().Contain("public async Task<IActionResult> FinancialAccounts(");
        billingSource.Should().Contain("public async Task<IActionResult> Expenses(");
        billingSource.Should().Contain("public async Task<IActionResult> JournalEntries(");
        billingSource.Should().Contain("public IActionResult CreatePlan()");
        billingSource.Should().Contain("public async Task<IActionResult> EditPlan(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreatePayment(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditPayment(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreateFinancialAccount(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditFinancialAccount(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreateExpense(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditExpense(Guid id");
        billingSource.Should().Contain("public async Task<IActionResult> CreateJournalEntry(Guid? businessId = null");
        billingSource.Should().Contain("public async Task<IActionResult> EditJournalEntry(Guid id");
    }


    [Fact]
    public void BillingController_Should_KeepPlansPaymentsWebhookAndTaxContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Billing", "BillingController.cs"));

        source.Should().Contain("public async Task<IActionResult> Plans(int page = 1, int pageSize = 20, string? q = null, BillingPlanQueueFilter queue = BillingPlanQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getBillingPlansAdminPage.HandleAsync(page, pageSize, q, queue, ct).ConfigureAwait(false);");
        source.Should().Contain("Summary = await BuildBillingPlanOpsSummaryVmAsync(ct).ConfigureAwait(false),");
        source.Should().Contain("Playbooks = BuildBillingPlanPlaybooks(),");
        source.Should().Contain("return RenderPlansWorkspace(vm);");
        source.Should().Contain("PopulateBillingPlanOptions(vm);");
        source.Should().Contain("await _createBillingPlan.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updateBillingPlan.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"BillingPlanConcurrencyMessage\");");
        source.Should().Contain("private async Task<BillingPlanOpsSummaryVm> BuildBillingPlanOpsSummaryVmAsync(CancellationToken ct)");
        source.Should().Contain("private List<ProviderPlaybookVm> BuildBillingPlanPlaybooks()");
        source.Should().Contain("Title = T(\"BillingPlansPlaybookActiveTitle\")");
        source.Should().Contain("Title = T(\"BillingPlansPlaybookTrialTitle\")");
        source.Should().Contain("private void PopulateBillingPlanOptions(BillingPlanEditVm vm)");
        source.Should().Contain(".Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T(x.ToString()), x.ToString(), x == vm.Interval))");
        source.Should().Contain("private IActionResult RenderPlansWorkspace(BillingPlansListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Billing/Plans.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderBillingPlanEditor(BillingPlanEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Billing/_BillingPlanEditorShell.cshtml\", vm);");

        source.Should().Contain("public async Task<IActionResult> Payments(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PaymentQueueFilter? queue = null, CancellationToken ct = default)");
        source.Should().Contain("businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("var result = await _getPaymentsPage.HandleAsync(businessId.Value, page, pageSize, q, queue, ct).ConfigureAwait(false);");
        source.Should().Contain("Stripe = await BuildStripeOperationsVmAsync(ct).ConfigureAwait(false),");
        source.Should().Contain("Tax = await BuildTaxOperationsVmAsync(ct).ConfigureAwait(false),");
        source.Should().Contain("Webhooks = await BuildBillingWebhookOpsSummaryVmAsync(ct).ConfigureAwait(false),");
        source.Should().Contain("Summary = businessId.HasValue");
        source.Should().Contain("? await BuildPaymentOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)");
        source.Should().Contain("Playbooks = BuildPaymentPlaybooks(),");
        source.Should().Contain("return RenderPaymentsWorkspace(vm);");
        source.Should().Contain("private async Task<PaymentOpsSummaryVm> BuildPaymentOpsSummaryVmAsync(Guid businessId, CancellationToken ct)");
        source.Should().Contain("private async Task<StripeOperationsVm> BuildStripeOperationsVmAsync(CancellationToken ct)");
        source.Should().Contain("private async Task<TaxOperationsVm> BuildTaxOperationsVmAsync(CancellationToken ct)");
        source.Should().Contain("private static List<ProviderPlaybookVm> BuildPaymentPlaybooks()");
        source.Should().Contain("private IActionResult RenderPaymentsWorkspace(PaymentsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Billing/Payments.cshtml\", vm);");

        source.Should().Contain("public async Task<IActionResult> TaxCompliance(CancellationToken ct = default)");
        source.Should().Contain("var overview = await _getTaxComplianceOverview.HandleAsync(ct: ct).ConfigureAwait(false);");
        source.Should().Contain("Playbooks = BuildTaxCompliancePlaybooks(),");
        source.Should().Contain("return RenderTaxComplianceWorkspace(vm);");
        source.Should().Contain("private List<ProviderPlaybookVm> BuildTaxCompliancePlaybooks()");
        source.Should().Contain("private IActionResult RenderTaxComplianceWorkspace(TaxComplianceListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Billing/TaxCompliance.cshtml\", vm);");

        source.Should().Contain("public async Task<IActionResult> Webhooks(int page = 1, int pageSize = 20, string? q = null, BillingWebhookDeliveryQueueFilter queue = BillingWebhookDeliveryQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var subscriptions = await _getBillingWebhookSubscriptionsPage.HandleAsync(1, 10, q, ct).ConfigureAwait(false);");
        source.Should().Contain("var deliveries = await _getBillingWebhookDeliveriesPage.HandleAsync(page, pageSize, q, queue, ct).ConfigureAwait(false);");
        source.Should().Contain("Summary = await BuildBillingWebhookOpsSummaryVmAsync(ct).ConfigureAwait(false),");
        source.Should().Contain("Playbooks = BuildWebhookPlaybooks(),");
        source.Should().Contain("return RenderWebhooksWorkspace(vm);");
        source.Should().Contain("private async Task<BillingWebhookOpsSummaryVm> BuildBillingWebhookOpsSummaryVmAsync(CancellationToken ct)");
        source.Should().Contain("private List<ProviderPlaybookVm> BuildWebhookPlaybooks()");
        source.Should().Contain("private IActionResult RenderWebhooksWorkspace(BillingWebhooksListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Billing/Webhooks.cshtml\", vm);");
    }


    [Fact]
    public void BillingController_Should_KeepRefundFinanceEditorAndSharedHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Billing", "BillingController.cs"));

        source.Should().Contain("public async Task<IActionResult> Refunds(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, BillingRefundQueueFilter? queue = null, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getRefundsPage.HandleAsync(businessId.Value, page, pageSize, q, queue, ct).ConfigureAwait(false);");
        source.Should().Contain("Webhooks = await BuildBillingWebhookOpsSummaryVmAsync(ct).ConfigureAwait(false),");
        source.Should().Contain("? await BuildRefundOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)");
        source.Should().Contain("Playbooks = BuildRefundPlaybooks(),");
        source.Should().Contain("return RenderRefundsWorkspace(vm);");
        source.Should().Contain("private async Task<RefundOpsSummaryVm> BuildRefundOpsSummaryVmAsync(Guid businessId, CancellationToken ct)");
        source.Should().Contain("private static List<ProviderPlaybookVm> BuildRefundPlaybooks()");
        source.Should().Contain("private IActionResult RenderRefundsWorkspace(RefundsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Billing/Refunds.cshtml\", vm);");

        source.Should().Contain("public async Task<IActionResult> CreatePayment(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("await PopulatePaymentOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderPaymentEditor(vm, isCreate: true);");
        source.Should().Contain("var dto = await _getPaymentForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SupportPlaybooks = BuildPaymentSupportPlaybooks(dto)");
        source.Should().Contain("await _createPayment.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updatePayment.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"PaymentConcurrencyMessage\");");
        source.Should().Contain("private async Task PopulatePaymentOptionsAsync(PaymentEditVm vm, CancellationToken ct)");
        source.Should().Contain("private List<PaymentSupportPlaybookVm> BuildPaymentSupportPlaybooks(PaymentEditDto dto)");
        source.Should().Contain("Title = T(\"PaymentSupportPlaybookProviderCorrelationTitle\")");
        source.Should().Contain("Title = T(\"PaymentSupportPlaybookFailureRefundTitle\")");
        source.Should().Contain("Title = T(\"PaymentSupportPlaybookStripeTitle\")");
        source.Should().Contain("private IActionResult RenderPaymentEditor(PaymentEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Billing/_PaymentEditorShell.cshtml\", vm);");

        source.Should().Contain("public async Task<IActionResult> FinancialAccounts(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, AccountType? queue = null, CancellationToken ct = default)");
        source.Should().Contain("? await BuildFinancialAccountOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)");
        source.Should().Contain("Playbooks = BuildFinancialAccountPlaybooks(),");
        source.Should().Contain("return RenderFinancialAccountsWorkspace(vm);");
        source.Should().Contain("await _createAccount.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updateAccount.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("private async Task<FinancialAccountOpsSummaryVm> BuildFinancialAccountOpsSummaryVmAsync(Guid businessId, CancellationToken ct)");
        source.Should().Contain("private List<ProviderPlaybookVm> BuildFinancialAccountPlaybooks()");
        source.Should().Contain("Title = T(\"FinancialAccountPlaybookAssetsTitle\")");
        source.Should().Contain("Title = T(\"FinancialAccountPlaybookMappingTitle\")");
        source.Should().Contain("private IActionResult RenderFinancialAccountsWorkspace(FinancialAccountsListVm vm)");
        source.Should().Contain("private IActionResult RenderFinancialAccountEditor(FinancialAccountEditVm vm, bool isCreate)");

        source.Should().Contain("public async Task<IActionResult> Expenses(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)");
        source.Should().Contain("? await BuildExpenseOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)");
        source.Should().Contain("Playbooks = BuildExpensePlaybooks(),");
        source.Should().Contain("await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("await _createExpense.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updateExpense.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("private async Task<ExpenseOpsSummaryVm> BuildExpenseOpsSummaryVmAsync(Guid businessId, CancellationToken ct)");
        source.Should().Contain("private List<ProviderPlaybookVm> BuildExpensePlaybooks()");
        source.Should().Contain("Title = T(\"ExpensePlaybookRecentTitle\")");
        source.Should().Contain("Title = T(\"ExpensePlaybookHighValueTitle\")");
        source.Should().Contain("private async Task PopulateExpenseOptionsAsync(ExpenseEditVm vm, CancellationToken ct)");
        source.Should().Contain("private IActionResult RenderExpensesWorkspace(ExpensesListVm vm)");
        source.Should().Contain("private IActionResult RenderExpenseEditor(ExpenseEditVm vm, bool isCreate)");

        source.Should().Contain("public async Task<IActionResult> JournalEntries(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, JournalEntryQueueFilter? queue = null, CancellationToken ct = default)");
        source.Should().Contain("? await BuildJournalEntryOpsSummaryVmAsync(businessId.Value, ct).ConfigureAwait(false)");
        source.Should().Contain("Playbooks = BuildJournalEntryPlaybooks(),");
        source.Should().Contain("EnsureJournalEntryRows(vm);");
        source.Should().Contain("await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("await _createJournalEntry.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updateJournalEntry.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("private async Task<JournalEntryOpsSummaryVm> BuildJournalEntryOpsSummaryVmAsync(Guid businessId, CancellationToken ct)");
        source.Should().Contain("private List<ProviderPlaybookVm> BuildJournalEntryPlaybooks()");
        source.Should().Contain("Title = T(\"JournalEntryPlaybookRecentTitle\")");
        source.Should().Contain("Title = T(\"JournalEntryPlaybookMultilineTitle\")");
        source.Should().Contain("private async Task PopulateJournalEntryOptionsAsync(JournalEntryEditVm vm, CancellationToken ct)");
        source.Should().Contain("private static void EnsureJournalEntryRows(JournalEntryEditVm vm)");
        source.Should().Contain("private IActionResult RenderJournalEntriesWorkspace(JournalEntriesListVm vm)");
        source.Should().Contain("private IActionResult RenderJournalEntryEditor(JournalEntryEditVm vm, bool isCreate)");

        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("private bool IsHtmxRequest()");
    }


    [Fact]
    public void BillingFinancialAccountsView_Should_KeepLocalizedTypeAndWorkspaceContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Billing", "FinancialAccounts.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"FinancialAccountsTitle\");");
        source.Should().Contain("string LocalizeAccountType(object? type) => type is null ? \"-\" : T.T(type.ToString() ?? string.Empty);");
        source.Should().Contain("<h1 class=\"mb-3\">@T.T(\"FinancialAccountsTitle\")</h1>");
        source.Should().Contain("placeholder=\"@T.T(\"SearchFinancialAccountsPlaceholder\")\"");
        source.Should().Contain("@T.T(\"FinancialAccountsPlaybooksTitle\")");
        source.Should().Contain("<th>@T.T(\"Type\")</th><th class=\"text-end\">@T.T(\"CommonActions\")</th>");
        source.Should().Contain("@LocalizeAccountType(item.Type)");
        source.Should().Contain("@T.T(\"FinancialAccountsCodeMissing\")");
        source.Should().Contain("@T.T(\"JournalEntriesTitle\")");
        source.Should().Contain("asp-action=\"FinancialAccounts\"");
        source.Should().Contain("hx-target=\"#billing-financial-accounts-workspace-shell\"");
    }


    [Fact]
    public void UsersController_Should_KeepIdentityAndAddressMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> Create(UserCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> Edit(UserEditVm vm");
        source.Should().Contain("public async Task<IActionResult> ChangeEmail(UserChangeEmailVm vm");
        source.Should().Contain("public async Task<IActionResult> ConfirmEmail(");
        source.Should().Contain("public async Task<IActionResult> SendActivationEmail(");
        source.Should().Contain("public async Task<IActionResult> SendPasswordReset(");
        source.Should().Contain("public async Task<IActionResult> ChangePassword(UserChangePasswordVm vm");
        source.Should().Contain("public async Task<IActionResult> Lock(");
        source.Should().Contain("public async Task<IActionResult> Unlock(");
        source.Should().Contain("public async Task<IActionResult> Delete(");
        source.Should().Contain("public async Task<IActionResult> Roles(UserRolesEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateAddress(");
        source.Should().Contain("public async Task<IActionResult> EditAddress(");
        source.Should().Contain("public async Task<IActionResult> DeleteAddress(");
        source.Should().Contain("public async Task<IActionResult> SetDefaultAddress(");
    }


    [Fact]
    public void UsersController_Should_KeepAdminWorkspacesAndEditorGetsReachable()
    {
        var usersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        usersSource.Should().Contain("public sealed class UsersController : AdminBaseController");
        usersSource.Should().Contain("public async Task<IActionResult> Index(");
        usersSource.Should().Contain("public IActionResult Create() => RenderCreateEditor(new UserCreateVm())");
        usersSource.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        usersSource.Should().Contain("public IActionResult ChangeEmail([FromRoute] Guid id, [FromQuery] string? currentEmail = null)");
        usersSource.Should().Contain("public IActionResult ChangePassword(Guid id, string? email = null)");
        usersSource.Should().Contain("public async Task<IActionResult> AddressesSection(Guid userId, CancellationToken ct = default)");
        usersSource.Should().Contain("public async Task<IActionResult> Roles(Guid id, bool returnToIndex = false, string? q = null, UserQueueFilter filter = UserQueueFilter.All, int page = 1, int pageSize = 20, CancellationToken ct = default)");
    }


    [Fact]
    public void UsersController_Should_KeepIdentitySupportAuditAndPlaybookContracts()
    {
        var usersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));

        usersSource.Should().Contain("private readonly GetEmailDispatchAuditsPageHandler _getEmailDispatchAuditsPage;");
        usersSource.Should().Contain("GetEmailDispatchAuditsPageHandler getEmailDispatchAuditsPage");
        usersSource.Should().Contain("_getEmailDispatchAuditsPage = getEmailDispatchAuditsPage;");
        usersSource.Should().Contain("var auditSummary = await _getEmailDispatchAuditsPage.GetSummaryAsync(null, ct);");
        usersSource.Should().Contain("FailedActivationEmailCount = auditSummary.FailedActivationCount");
        usersSource.Should().Contain("FailedPasswordResetEmailCount = auditSummary.FailedPasswordResetCount");
        usersSource.Should().Contain("QueueFilter = UserQueueFilter.Unconfirmed");
        usersSource.Should().Contain("AuditFlowKey = \"AccountActivation\"");
        usersSource.Should().Contain("QueueFilter = UserQueueFilter.Locked");
        usersSource.Should().Contain("AuditFlowKey = \"PasswordReset\"");
        usersSource.Should().Contain("QueueFilter = UserQueueFilter.MobileLinked");
        usersSource.Should().Contain("OpensMobileOperations = true");
    }


    [Fact]
    public void UsersWorkspace_Should_KeepShellSummaryFilterGridAndMutationRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Users", "Index.cshtml"));

        source.Should().Contain("id=\"users-workspace-shell\"");
        source.Should().Contain("@T.T(\"UsersPageTitle\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"Unconfirmed\" })\"");
        source.Should().Contain("@T.T(\"UsersFilterUnconfirmed\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" })\"");
        source.Should().Contain("@T.T(\"UsersFilterLocked\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"Inactive\" })\"");
        source.Should().Contain("@T.T(\"UsersFilterInactive\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"MobileLinked\" })\"");
        source.Should().Contain("@T.T(\"UsersFilterMobileLinked\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Users\")\"");
        source.Should().Contain("@T.T(\"UsersCreateAction\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.UnconfirmedCount");
        source.Should().Contain("@Model.Summary.LockedCount");
        source.Should().Contain("@Model.Summary.InactiveCount");
        source.Should().Contain("@Model.Summary.MobileLinkedCount");
        source.Should().Contain("@T.T(\"UsersPlaybooksTitle\")");
        source.Should().Contain("@T.T(\"UsersPlaybooksQueueColumn\")");
        source.Should().Contain("@T.T(\"UsersPlaybooksFollowUpColumn\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { status = \"Failed\", flowKey = item.AuditFlowKey })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\")\"");
        source.Should().Contain("placeholder=\"@T.T(\"UsersSearchPlaceholder\")\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("name=\"pageSize\" class=\"form-select\"");
        source.Should().Contain("@T.T(\"UsersEmailColumn\")");
        source.Should().Contain("@T.T(\"UsersLifecycleColumn\")");
        source.Should().Contain("@T.T(\"UsersActionsColumn\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Users\", new { id = u.Id })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"ChangeEmail\", \"Users\", new { id = u.Id, currentEmail = u.Email })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"ChangePassword\", \"Users\", new { id = u.Id, email = u.Email })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Roles\", \"Users\", new { id = u.Id, returnToIndex = true, q = Model.Query, filter = Model.Filter, page = Model.Page, pageSize = Model.PageSize })\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"SendActivationEmail\", \"Users\")\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"Unlock\", \"Users\")\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"Lock\", \"Users\")\"");
        source.Should().Contain("data-bs-target=\"#confirmDeleteModal\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Users\")\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("hx-target=\"#users-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void UserEditorShells_Should_KeepCreateEditRolesAndCredentialChangeContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "Users", "Create.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "Users", "Edit.cshtml"));
        var rolesSource = ReadWebAdminFile(Path.Combine("Views", "Users", "Roles.cshtml"));
        var changeEmailSource = ReadWebAdminFile(Path.Combine("Views", "Users", "ChangeEmail.cshtml"));
        var changePasswordSource = ReadWebAdminFile(Path.Combine("Views", "Users", "ChangePassword.cshtml"));
        var createShellSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_UserCreateEditorShell.cshtml"));
        var editShellSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_UserEditEditorShell.cshtml"));
        var rolesShellSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_UserRolesEditorShell.cshtml"));
        var changeEmailShellSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_UserChangeEmailEditorShell.cshtml"));
        var changePasswordShellSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_UserChangePasswordEditorShell.cshtml"));
        var profileFieldsSource = ReadWebAdminFile(Path.Combine("Views", "Users", "_UserProfileFields.cshtml"));

        createSource.Should().Contain("<partial name=\"_UserCreateEditorShell\" model=\"Model\" />");
        createSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        editSource.Should().Contain("<partial name=\"_UserEditEditorShell\" model=\"Model\" />");
        editSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        rolesSource.Should().Contain("<partial name=\"_UserRolesEditorShell\" model=\"Model\" />");
        rolesSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        changeEmailSource.Should().Contain("<partial name=\"_UserChangeEmailEditorShell\" model=\"Model\" />");
        changeEmailSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        changePasswordSource.Should().Contain("<partial name=\"_UserChangePasswordEditorShell\" model=\"Model\" />");
        changePasswordSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        createShellSource.Should().Contain("id=\"user-create-editor-shell\"");
        createShellSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"Users\")\"");
        createShellSource.Should().Contain("asp-for=\"Email\"");
        createShellSource.Should().Contain("asp-for=\"Password\" type=\"password\"");
        createShellSource.Should().Contain("<partial name=\"_UserProfileFields\" model=\"Model\" />");
        createShellSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\")\"");
        createShellSource.Should().Contain("@T.T(\"CreateAction\")");

        editShellSource.Should().Contain("id=\"user-edit-editor-shell\"");
        editShellSource.Should().Contain("@T.T(\"UserAdministrativeActionsTitle\")");
        editShellSource.Should().Contain("@T.T(\"UserEmailConfirmedBadge\")");
        editShellSource.Should().Contain("@T.T(\"UserEmailUnconfirmedBadge\")");
        editShellSource.Should().Contain("@T.T(\"UserUnlockedBadge\")");
        editShellSource.Should().Contain("hx-get=\"@Url.Action(\"ChangeEmail\", \"Users\", new { id = Model.Id, currentEmail = Model.Email })\"");
        editShellSource.Should().Contain("hx-get=\"@Url.Action(\"ChangePassword\", \"Users\", new { id = Model.Id, email = Model.Email })\"");
        editShellSource.Should().Contain("hx-get=\"@Url.Action(\"Roles\", \"Users\", new { id = Model.Id })\"");
        editShellSource.Should().Contain("hx-post=\"@Url.Action(\"SendActivationEmail\", \"Users\")\"");
        editShellSource.Should().Contain("hx-post=\"@Url.Action(\"ConfirmEmail\", \"Users\")\"");
        editShellSource.Should().Contain("hx-post=\"@Url.Action(\"SendPasswordReset\", \"Users\")\"");
        editShellSource.Should().Contain("hx-post=\"@Url.Action(\"Unlock\", \"Users\")\"");
        editShellSource.Should().Contain("hx-post=\"@Url.Action(\"Lock\", \"Users\")\"");
        editShellSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"Users\")\"");
        editShellSource.Should().Contain("asp-for=\"Id\"");
        editShellSource.Should().Contain("asp-for=\"RowVersion\"");
        editShellSource.Should().Contain("asp-for=\"Email\"");
        editShellSource.Should().Contain("asp-for=\"IsActive\"");
        editShellSource.Should().Contain("@await Html.PartialAsync(\"~/Views/Users/_AddressesSection.cshtml\", addressesSection)");
        editShellSource.Should().Contain("@await Html.PartialAsync(\"~/Views/Users/_AddressEditModal.cshtml\", new Darwin.WebAdmin.ViewModels.Identity.UserAddressEditVm())");
        editShellSource.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
        editShellSource.Should().Contain("window.darwinAdmin.initUserEditScreen");
        editShellSource.Should().Contain("this.value = this.value.toUpperCase();");

        rolesShellSource.Should().Contain("id=\"user-roles-editor-shell\"");
        rolesShellSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        rolesShellSource.Should().Contain("hx-post=\"@Url.Action(\"Roles\", \"Users\")\"");
        rolesShellSource.Should().Contain("asp-for=\"UserId\"");
        rolesShellSource.Should().Contain("asp-for=\"RowVersion\"");
        rolesShellSource.Should().Contain("asp-for=\"ReturnToIndex\"");
        rolesShellSource.Should().Contain("@T.T(\"AssignRolesToUserTitle\")");
        rolesShellSource.Should().Contain("@T.T(\"DelegatedSupportRoleTitle\")");
        rolesShellSource.Should().Contain("name=\"SelectedRoleIds\"");
        rolesShellSource.Should().Contain("@T.T(\"DelegatedSupportBadge\")");
        rolesShellSource.Should().Contain("@T.T(\"NoRolesFound\")");
        rolesShellSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { q = Model.Query, filter = Model.Filter, page = Model.Page, pageSize = Model.PageSize })\"");
        rolesShellSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Users\", new { id = Model.UserId })\"");

        changeEmailShellSource.Should().Contain("id=\"user-change-email-editor-shell\"");
        changeEmailShellSource.Should().Contain("@T.T(\"UserChangeEmailWarning\")");
        changeEmailShellSource.Should().Contain("hx-post=\"@Url.Action(\"ChangeEmail\", \"Users\")\"");
        changeEmailShellSource.Should().Contain("asp-for=\"Id\"");
        changeEmailShellSource.Should().Contain("asp-for=\"CurrentEmail\"");
        changeEmailShellSource.Should().Contain("asp-for=\"NewEmail\"");
        changeEmailShellSource.Should().Contain("CurrentEmailReadonlyHelp");
        changeEmailShellSource.Should().Contain("NewEmailHelp");
        changeEmailShellSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Users\", new { id = Model.Id })\"");

        changePasswordShellSource.Should().Contain("id=\"user-change-password-editor-shell\"");
        changePasswordShellSource.Should().Contain("hx-post=\"@Url.Action(\"ChangePassword\", \"Users\")\"");
        changePasswordShellSource.Should().Contain("asp-for=\"Id\"");
        changePasswordShellSource.Should().Contain("asp-for=\"Email\"");
        changePasswordShellSource.Should().Contain("asp-for=\"NewPassword\" type=\"password\"");
        changePasswordShellSource.Should().Contain("asp-for=\"ConfirmNewPassword\" type=\"password\"");
        changePasswordShellSource.Should().Contain("@T.T(\"PasswordPolicyHelp\")");
        changePasswordShellSource.Should().Contain("@T.T(\"MustMatchNewPassword\")");
        changePasswordShellSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Users\", new { id = Model.Id })\"");

        profileFieldsSource.Should().Contain("asp-for=\"FirstName\"");
        profileFieldsSource.Should().Contain("asp-for=\"LastName\"");
        profileFieldsSource.Should().Contain("asp-for=\"Locale\"");
        profileFieldsSource.Should().Contain("setting=\"SupportedCulturesCsv\"");
        profileFieldsSource.Should().Contain("asp-for=\"Currency\"");
        profileFieldsSource.Should().Contain("setting=\"SupportedCurrenciesCsv\"");
        profileFieldsSource.Should().Contain("asp-for=\"Timezone\"");
        profileFieldsSource.Should().Contain("setting=\"SupportedTimezonesCsv\"");
        profileFieldsSource.Should().Contain("asp-for=\"PhoneE164\"");
        profileFieldsSource.Should().Contain("@T.T(\"UserLocaleHelp\")");
        profileFieldsSource.Should().Contain("@T.T(\"UserCurrencyHelp\")");
        profileFieldsSource.Should().Contain("@T.T(\"UserTimezoneHelp\")");
    }


    [Fact]
    public void UsersController_Should_KeepWorkspaceBuilderAddressAndRenderHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));

        source.Should().Contain("var vm = await BuildUsersListVmAsync(page, pageSize, q, filter, ct);");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("private async Task<UserAddressesSectionVm> BuildAddressesSectionVmAsync(Guid userId, CancellationToken ct)");
        source.Should().Contain("Items = new List<UserAddressListItemVm>()");
        source.Should().Contain("if (!result.Succeeded || result.Value is null)");
        source.Should().Contain("return section; // Return empty section on failure to avoid null refs");
        source.Should().Contain("section.Items.Add(new UserAddressListItemVm");
        source.Should().Contain("RowVersion = a.RowVersion ?? Array.Empty<byte>()");
        source.Should().Contain("public async Task<IActionResult> AddressesSection(Guid userId, CancellationToken ct = default)");
        source.Should().Contain("var section = await BuildAddressesSectionVmAsync(userId, ct);");
        source.Should().Contain("return PartialView(\"~/Views/Users/_AddressesSection.cshtml\", section);");
        source.Should().Contain("private IActionResult RenderCreateEditor(UserCreateVm vm)");
        source.Should().Contain("_siteSettingCache.GetAsync().GetAwaiter().GetResult()");
        source.Should().Contain("vm.Locale = string.IsNullOrWhiteSpace(vm.Locale) ? settings.DefaultCulture : vm.Locale;");
        source.Should().Contain("vm.Currency = string.IsNullOrWhiteSpace(vm.Currency) ? settings.DefaultCurrency : vm.Currency;");
        source.Should().Contain("vm.Timezone = string.IsNullOrWhiteSpace(vm.Timezone)");
        source.Should().Contain("PartialView(\"~/Views/Users/_UserCreateEditorShell.cshtml\", vm)");
        source.Should().Contain("return View(\"Create\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(UsersListVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Users/Index.cshtml\", vm)");
        source.Should().Contain("return View(\"Index\", vm);");
        source.Should().Contain("private IActionResult RenderChangeEmailEditor(UserChangeEmailVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Users/_UserChangeEmailEditorShell.cshtml\", vm)");
        source.Should().Contain("return View(\"ChangeEmail\", vm);");
        source.Should().Contain("private IActionResult RenderChangePasswordEditor(UserChangePasswordVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Users/_UserChangePasswordEditorShell.cshtml\", vm)");
        source.Should().Contain("return View(\"ChangePassword\", vm);");
        source.Should().Contain("private async Task<IActionResult> RenderEditEditorAsync(UserEditVm vm, CancellationToken ct)");
        source.Should().Contain("ViewBag.AddressesSection = await BuildAddressesSectionVmAsync(vm.Id, ct);");
        source.Should().Contain("PartialView(\"~/Views/Users/_UserEditEditorShell.cshtml\", vm)");
        source.Should().Contain("return View(\"Edit\", vm);");
        source.Should().Contain("private async Task<IActionResult> RenderRolesEditorAsync(UserRolesEditVm vm, CancellationToken ct)");
        source.Should().Contain("var hydrated = await BuildUserRolesVmAsync(vm.UserId, ct);");
        source.Should().Contain("hydrated.SelectedRoleIds = vm.SelectedRoleIds?.ToList() ?? new List<Guid>()");
        source.Should().Contain("PartialView(\"~/Views/Users/_UserRolesEditorShell.cshtml\", hydrated)");
        source.Should().Contain("return View(\"Roles\", hydrated);");
        source.Should().Contain("private async Task<IActionResult> RedirectToUserRolesReturnTargetAsync(UserRolesEditVm vm, CancellationToken ct)");
        source.Should().Contain("var workspace = await BuildUsersListVmAsync(vm.Page, vm.PageSize, vm.Query, vm.Filter, ct);");
        source.Should().Contain("return RenderIndexWorkspace(workspace);");
        source.Should().Contain("return RedirectOrHtmx(nameof(Index), new { page = vm.Page, pageSize = vm.PageSize, q = vm.Query, filter = vm.Filter });");
        source.Should().Contain("return RedirectOrHtmx(nameof(Edit), new { id = vm.UserId });");
        source.Should().Contain("private IActionResult RedirectToUsersWorkspaceOrEdit(Guid id, bool returnToIndex, string? q, UserQueueFilter filter, int page, int pageSize)");
        source.Should().Contain("private async Task<UserRolesEditVm?> BuildUserRolesVmAsync(Guid userId, CancellationToken ct)");
        source.Should().Contain("AllRoles = dto.AllRoles.Select(x => new RoleItemVm");
        source.Should().Contain("SelectedRoleIds = dto.RoleIds.ToList()");
        source.Should().Contain("private async Task<UsersListVm> BuildUsersListVmAsync(int page, int pageSize, string? q, UserQueueFilter filter, CancellationToken ct)");
        source.Should().Contain("var summary = await _getUserOpsSummary.HandleAsync(ct);");
        source.Should().Contain("var auditSummary = await _getEmailDispatchAuditsPage.GetSummaryAsync(null, ct);");
        source.Should().Contain("Playbooks = BuildUserSupportPlaybooks()");
        source.Should().Contain("FilterItems = BuildUserFilterItems(filter)");
        source.Should().Contain("new SelectListItem(\"10\",  \"10\",  pageSize == 10)");
        source.Should().Contain("new SelectListItem(\"100\", \"100\", pageSize == 100)");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("Request.Headers[\"HX-Request\"]");
    }


    [Fact]
    public void CurrentUserService_Should_KeepClaimResolutionAndFallbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Auth", "CurrentUserService.cs"));

        source.Should().Contain("public sealed class CurrentUserService : ICurrentUserService");
        source.Should().Contain("private readonly IHttpContextAccessor _http;");
        source.Should().Contain("public CurrentUserService(IHttpContextAccessor http) => _http = http;");
        source.Should().Contain("public Guid GetCurrentUserId()");
        source.Should().Contain("var user = _http.HttpContext?.User;");
        source.Should().Contain("if (user?.Identity?.IsAuthenticated == true)");
        source.Should().Contain("var id = user.FindFirstValue(ClaimTypes.NameIdentifier)");
        source.Should().Contain("?? user.FindFirstValue(\"sub\")");
        source.Should().Contain("?? user.FindFirstValue(\"uid\");");
        source.Should().Contain("if (Guid.TryParse(id, out var guid))");
        source.Should().Contain("return guid;");
        source.Should().Contain("return WellKnownIds.AdministratorUserId;");
    }


    [Fact]
    public void SharedDashboardAndBusinessViewModels_Should_KeepOperationalContractShapesWired()
    {
        var adminDashboardVmSource = ReadWebAdminFile(Path.Combine("ViewModels", "Admin", "AdminDashboardVm.cs"));
        var businessVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Businesses", "BusinessVms.cs"));

        adminDashboardVmSource.Should().Contain("public sealed class AdminDashboardVm");
        adminDashboardVmSource.Should().Contain("public CrmSummaryVm Crm { get; set; } = new();");
        adminDashboardVmSource.Should().Contain("public int BusinessCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int ProductCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int PageCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int OrderCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int UserCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public Guid? SelectedBusinessId { get; set; }");
        adminDashboardVmSource.Should().Contain("public string SelectedBusinessLabel { get; set; } = string.Empty;");
        adminDashboardVmSource.Should().Contain("public int? PaymentCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int? WarehouseCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int? SupplierCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int? PurchaseOrderCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int? LoyaltyAccountCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int? PendingRedemptionCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int? ScanSessionCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int MobileActiveDeviceCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int MobileStaleDeviceCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int MobileMissingPushTokenCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public BusinessSupportSummaryVm BusinessSupport { get; set; } = new();");
        adminDashboardVmSource.Should().Contain("public BusinessCommunicationOpsSummaryVm CommunicationOps { get; set; } = new();");
        adminDashboardVmSource.Should().Contain("public IReadOnlyList<SelectListItem> BusinessOptions { get; set; } = Array.Empty<SelectListItem>();");
        adminDashboardVmSource.Should().Contain("public sealed class BusinessSupportSummaryVm");
        adminDashboardVmSource.Should().Contain("public int AttentionBusinessCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int PendingApprovalBusinessCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int SuspendedBusinessCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int MissingOwnerBusinessCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int OpenInvitationCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int PendingActivationMemberCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public int LockedMemberCount { get; set; }");
        adminDashboardVmSource.Should().Contain("public sealed class BusinessCommunicationOpsSummaryVm");
        adminDashboardVmSource.Should().Contain("public bool EmailTransportConfigured { get; set; }");
        adminDashboardVmSource.Should().Contain("public bool SmsTransportConfigured { get; set; }");
        adminDashboardVmSource.Should().Contain("public bool WhatsAppTransportConfigured { get; set; }");
        adminDashboardVmSource.Should().Contain("public bool AdminAlertRoutingConfigured { get; set; }");

        businessVmsSource.Should().Contain("public sealed class BusinessListItemVm");
        businessVmsSource.Should().Contain("public BusinessOperationalStatus OperationalStatus { get; set; } = BusinessOperationalStatus.PendingApproval;");
        businessVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        businessVmsSource.Should().Contain("public sealed class BusinessesListVm");
        businessVmsSource.Should().Contain("public BusinessReadinessQueueFilter? ReadinessFilter { get; set; }");
        businessVmsSource.Should().Contain("public BusinessSupportSummaryVm Summary { get; set; } = new();");
        businessVmsSource.Should().Contain("public List<MerchantReadinessPlaybookVm> Playbooks { get; set; } = new();");
        businessVmsSource.Should().Contain("public IEnumerable<SelectListItem> PageSizeItems { get; set; } = Array.Empty<SelectListItem>();");
        businessVmsSource.Should().Contain("public sealed class BusinessSupportQueueVm");
        businessVmsSource.Should().Contain("public List<BusinessSupportFailedEmailVm> FailedEmails { get; set; } = new();");
        businessVmsSource.Should().Contain("public sealed class MerchantReadinessItemVm");
        businessVmsSource.Should().Contain("public bool HasSubscription { get; set; }");
        businessVmsSource.Should().Contain("public string SubscriptionPlanName { get; set; } = string.Empty;");
        businessVmsSource.Should().Contain("public sealed class BusinessEditVm");
        businessVmsSource.Should().Contain("public string DefaultCulture { get; set; } = AdminCultureCatalog.DefaultCulture;");
        businessVmsSource.Should().Contain("public BusinessCommunicationReadinessVm CommunicationReadiness { get; set; } = new();");
        businessVmsSource.Should().Contain("public BusinessSubscriptionSnapshotVm Subscription { get; set; } = new();");
        businessVmsSource.Should().Contain("public IEnumerable<SelectListItem> OwnerUserOptions { get; set; } = Array.Empty<SelectListItem>();");
        businessVmsSource.Should().Contain("public sealed class BusinessSubscriptionSnapshotVm");
        businessVmsSource.Should().Contain("public bool HasSubscription { get; set; }");
        businessVmsSource.Should().Contain("public bool CancelAtPeriodEnd { get; set; }");
        businessVmsSource.Should().Contain("public sealed class BusinessSubscriptionWorkspaceVm");
        businessVmsSource.Should().Contain("public BusinessContextVm Business { get; set; } = new();");
        businessVmsSource.Should().Contain("public BusinessSubscriptionInvoiceOpsSummaryVm InvoiceSummary { get; set; } = new();");
        businessVmsSource.Should().Contain("public List<BusinessSubscriptionPlaybookVm> Playbooks { get; set; } = new();");
    }


    [Fact]
    public void SharedSettingsAndIdentityViewModels_Should_KeepAdminFormContractShapesWired()
    {
        var siteSettingVmSource = ReadWebAdminFile(Path.Combine("ViewModels", "Settings", "SiteSettingVm.cs"));
        var userVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Identity", "UserVms.cs"));

        siteSettingVmSource.Should().Contain("public sealed class SiteSettingVm");
        siteSettingVmSource.Should().Contain("[Required]");
        siteSettingVmSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingDefaultCulture\"), Required, MaxLength(10)]");
        siteSettingVmSource.Should().Contain("public string DefaultCulture { get; set; } = AdminCultureCatalog.DefaultCulture;");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingSupportedCulturesCsv\"), Required]");
        siteSettingVmSource.Should().Contain("public string SupportedCulturesCsv { get; set; } = AdminCultureCatalog.SupportedCulturesCsvDefault;");
        siteSettingVmSource.Should().Contain("public string? DefaultCountry { get; set; } = SiteSettingDto.DefaultCountryDefault;");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingJwtEnabled\")]");
        siteSettingVmSource.Should().Contain("public bool JwtEnabled { get; set; } = true;");
        siteSettingVmSource.Should().Contain("public string JwtIssuer { get; set; } = \"Darwin\";");
        siteSettingVmSource.Should().Contain("public string JwtAudience { get; set; } = \"Darwin.PublicApi\";");
        siteSettingVmSource.Should().Contain("public decimal DefaultVatRatePercent { get; set; } = 19m;");
        siteSettingVmSource.Should().Contain("public bool PricesIncludeVat { get; set; } = true;");
        siteSettingVmSource.Should().Contain("public int ShipmentAttentionDelayHours { get; set; } = 24;");
        siteSettingVmSource.Should().Contain("public int ShipmentTrackingGraceHours { get; set; } = 12;");
        siteSettingVmSource.Should().Contain("public int SoftDeleteRetentionDays { get; set; } = 90;");
        siteSettingVmSource.Should().Contain("public int SoftDeleteCleanupBatchSize { get; set; } = 500;");
        siteSettingVmSource.Should().Contain("public string MeasurementSystem { get; set; } = \"Metric\";");
        siteSettingVmSource.Should().Contain("public bool EnableCanonical { get; set; } = true;");
        siteSettingVmSource.Should().Contain("public bool HreflangEnabled { get; set; } = true;");
        siteSettingVmSource.Should().Contain("public string WebAuthnRelyingPartyId { get; set; } = \"localhost\";");
        siteSettingVmSource.Should().Contain("public string WebAuthnRelyingPartyName { get; set; } = \"Darwin\";");
        siteSettingVmSource.Should().Contain("public string WebAuthnAllowedOriginsCsv { get; set; } = \"https://localhost:5001\";");
        siteSettingVmSource.Should().Contain("public bool SmtpEnableSsl { get; set; } = true;");
        siteSettingVmSource.Should().Contain("public bool PhoneVerificationAllowFallback { get; set; }");

        userVmsSource.Should().Contain("public sealed class UsersListVm");
        userVmsSource.Should().Contain("public UserQueueFilter Filter { get; set; } = UserQueueFilter.All;");
        userVmsSource.Should().Contain("public UserOpsSummaryVm Summary { get; set; } = new();");
        userVmsSource.Should().Contain("public List<UserSupportPlaybookVm> Playbooks { get; set; } = new();");
        userVmsSource.Should().Contain("public sealed class UserListItemVm");
        userVmsSource.Should().Contain("public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;");
        userVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        userVmsSource.Should().Contain("public abstract class UserEditorVm");
        userVmsSource.Should().Contain("[Display(Name = \"Locale\")]");
        userVmsSource.Should().Contain("public string Locale { get; set; } = AdminCultureCatalog.DefaultCulture;");
        userVmsSource.Should().Contain("[StringLength(3, MinimumLength = 3)]");
        userVmsSource.Should().Contain("public sealed class UserCreateVm : UserEditorVm");
        userVmsSource.Should().Contain("[Display(Name = \"Email\")]");
        userVmsSource.Should().Contain("[Display(Name = \"Password\")]");
        userVmsSource.Should().Contain("public bool IsActive { get; set; } = true;");
        userVmsSource.Should().Contain("public bool IsSystem { get; set; } = false;");
        userVmsSource.Should().Contain("public sealed class UserEditVm : UserEditorVm");
        userVmsSource.Should().Contain("public bool EmailConfirmed { get; set; }");
        userVmsSource.Should().Contain("public bool IsLockedOut => LockoutEndUtc.HasValue && LockoutEndUtc.Value > DateTime.UtcNow;");
        userVmsSource.Should().Contain("public sealed class UserProfileEditVm");
        userVmsSource.Should().Contain("public sealed class UserChangeEmailVm");
        userVmsSource.Should().Contain("[Display(Name = \"NewEmail\")]");
        userVmsSource.Should().Contain("public sealed class UserDeleteVm");
        userVmsSource.Should().Contain("public sealed class UserChangePasswordVm");
        userVmsSource.Should().Contain("[MinLength(8, ErrorMessage = \"Password must be at least 8 characters.\")]");
    }


    [Fact]
    public void SharedBillingAndOrderViewModels_Should_KeepFinanceAndOrderOpsContractShapesWired()
    {
        var billingVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Billing", "BillingVms.cs"));
        var orderVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Orders", "OrderVms.cs"));

        billingVmsSource.Should().Contain("public sealed class PaymentsListVm");
        billingVmsSource.Should().Contain("public PaymentQueueFilter? QueueFilter { get; set; }");
        billingVmsSource.Should().Contain("public StripeOperationsVm Stripe { get; set; } = new();");
        billingVmsSource.Should().Contain("public TaxOperationsVm Tax { get; set; } = new();");
        billingVmsSource.Should().Contain("public BillingWebhookOpsSummaryVm Webhooks { get; set; } = new();");
        billingVmsSource.Should().Contain("public int Page { get; set; } = 1;");
        billingVmsSource.Should().Contain("public int PageSize { get; set; } = 20;");
        billingVmsSource.Should().Contain("public sealed class TaxOperationsVm");
        billingVmsSource.Should().Contain("public bool VatEnabled { get; set; }");
        billingVmsSource.Should().Contain("public string ArchiveReadinessLabel { get; set; } = string.Empty;");
        billingVmsSource.Should().Contain("public string EInvoiceBaselineLabel { get; set; } = string.Empty;");
        billingVmsSource.Should().Contain("public sealed class BillingPlanListItemVm");
        billingVmsSource.Should().Contain("public BillingInterval Interval { get; set; } = BillingInterval.Month;");
        billingVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        billingVmsSource.Should().Contain("public sealed class BillingWebhookDeliveryListItemVm");
        billingVmsSource.Should().Contain("public string SuggestedOperatorAction { get; set; } = string.Empty;");
        billingVmsSource.Should().Contain("public string SuggestedQueueTarget { get; set; } = string.Empty;");
        billingVmsSource.Should().Contain("public sealed class ProviderPlaybookVm");
        billingVmsSource.Should().Contain("public sealed class PaymentListItemVm");
        billingVmsSource.Should().Contain("public PaymentStatus Status { get; set; }");
        billingVmsSource.Should().Contain("public long AmountMinor { get; set; }");
        billingVmsSource.Should().Contain("public DateTime? PaidAtUtc { get; set; }");
        billingVmsSource.Should().Contain("public sealed class PaymentEditVm");
        billingVmsSource.Should().Contain("public DateTime? PaidAtUtc { get; set; }");
        billingVmsSource.Should().Contain("public List<SelectListItem> BusinessOptions { get; set; } = new();");

        orderVmsSource.Should().Contain("public sealed class OrdersListVm");
        orderVmsSource.Should().Contain("public OrderQueueFilter Filter { get; set; } = OrderQueueFilter.All;");
        orderVmsSource.Should().Contain("public IEnumerable<SelectListItem> FilterItems { get; set; } = new List<SelectListItem>();");
        orderVmsSource.Should().Contain("public sealed class OrderDetailVm");
        orderVmsSource.Should().Contain("public TaxPolicySnapshotVm TaxPolicy { get; set; } = new();");
        orderVmsSource.Should().Contain("public ReturnSupportBaselineVm ReturnSupport { get; set; } = new();");
        orderVmsSource.Should().Contain("public List<SelectListItem> WarehouseOptions { get; set; } = new();");
        orderVmsSource.Should().Contain("public List<OrderLineVm> Lines { get; set; } = new();");
        orderVmsSource.Should().Contain("public sealed class PaymentCreateVm");
        orderVmsSource.Should().Contain("public PaymentStatus Status { get; set; }");
        orderVmsSource.Should().Contain("public sealed class OrderPaymentsPageVm");
        orderVmsSource.Should().Contain("public PaymentQueueFilter Filter { get; set; } = PaymentQueueFilter.All;");
        orderVmsSource.Should().Contain("public sealed class PaymentListItemVm");
        orderVmsSource.Should().Contain("public long RefundedAmountMinor { get; set; }");
        orderVmsSource.Should().Contain("public long NetCapturedAmountMinor { get; set; }");
        orderVmsSource.Should().Contain("public sealed class OrderShipmentsPageVm");
        orderVmsSource.Should().Contain("public Guid? DefaultRefundPaymentId { get; set; }");
        orderVmsSource.Should().Contain("public ShipmentQueueFilter Filter { get; set; } = ShipmentQueueFilter.All;");
        orderVmsSource.Should().Contain("public sealed class ShipmentListItemVm");
        orderVmsSource.Should().Contain("public bool NeedsCarrierReview { get; set; }");
        orderVmsSource.Should().Contain("public bool TrackingOverdue { get; set; }");
        orderVmsSource.Should().Contain("public int AttentionDelayHours { get; set; }");
        orderVmsSource.Should().Contain("public int TrackingGraceHours { get; set; }");
        orderVmsSource.Should().Contain("public bool HasRefundablePayment { get; set; }");
        orderVmsSource.Should().Contain("public sealed class ShipmentCreateVm");
        orderVmsSource.Should().Contain("[Range(1, int.MaxValue)]");
        orderVmsSource.Should().Contain("public List<ShipmentLineCreateVm> Lines { get; set; } = new();");
    }


    [Fact]
    public void SharedInventoryAndLoyaltyViewModels_Should_KeepOpsContractShapesWired()
    {
        var inventoryVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Inventory", "InventoryVms.cs"));
        var loyaltyVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Loyalty", "LoyaltyVms.cs"));

        inventoryVmsSource.Should().Contain("public sealed class InventoryLedgerListVm");
        inventoryVmsSource.Should().Contain("public InventoryLedgerQueueFilter Filter { get; set; } = InventoryLedgerQueueFilter.All;");
        inventoryVmsSource.Should().Contain("public InventoryLedgerOpsSummaryVm Summary { get; set; } = new();");
        inventoryVmsSource.Should().Contain("public List<InventoryOpsPlaybookVm> Playbooks { get; set; } = new();");
        inventoryVmsSource.Should().Contain("public IEnumerable<SelectListItem> PageSizeItems { get; set; } = new List<SelectListItem>();");
        inventoryVmsSource.Should().Contain("public int Available => StockOnHand - StockReserved;");
        inventoryVmsSource.Should().Contain("public sealed class InventoryAdjustVm");
        inventoryVmsSource.Should().Contain("public string Reason { get; set; } = \"ManualAdjustment\";");
        inventoryVmsSource.Should().Contain("public sealed class InventoryReserveVm");
        inventoryVmsSource.Should().Contain("public string Reason { get; set; } = \"ManualReserve\";");
        inventoryVmsSource.Should().Contain("public sealed class InventoryReleaseReservationVm");
        inventoryVmsSource.Should().Contain("public string Reason { get; set; } = \"ManualRelease\";");
        inventoryVmsSource.Should().Contain("public sealed class WarehousesListVm");
        inventoryVmsSource.Should().Contain("public WarehouseQueueFilter Filter { get; set; } = WarehouseQueueFilter.All;");
        inventoryVmsSource.Should().Contain("public WarehouseOpsSummaryVm Summary { get; set; } = new();");
        inventoryVmsSource.Should().Contain("public sealed class WarehouseEditVm");
        inventoryVmsSource.Should().Contain("[Required]");
        inventoryVmsSource.Should().Contain("[StringLength(200)]");
        inventoryVmsSource.Should().Contain("public List<SelectListItem> BusinessOptions { get; set; } = new();");
        inventoryVmsSource.Should().Contain("public sealed class SuppliersListVm");
        inventoryVmsSource.Should().Contain("public SupplierQueueFilter Filter { get; set; } = SupplierQueueFilter.All;");
        inventoryVmsSource.Should().Contain("public SupplierOpsSummaryVm Summary { get; set; } = new();");

        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyProgramsListVm");
        loyaltyVmsSource.Should().Contain("public LoyaltyProgramQueueFilter Filter { get; set; } = LoyaltyProgramQueueFilter.All;");
        loyaltyVmsSource.Should().Contain("public LoyaltyProgramOpsSummaryVm Summary { get; set; } = new();");
        loyaltyVmsSource.Should().Contain("public List<LoyaltyOpsPlaybookVm> Playbooks { get; set; } = new();");
        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyProgramEditVm");
        loyaltyVmsSource.Should().Contain("public LoyaltyAccrualMode AccrualMode { get; set; } = LoyaltyAccrualMode.PerVisit;");
        loyaltyVmsSource.Should().Contain("[Range(0, 100000)]");
        loyaltyVmsSource.Should().Contain("public bool IsActive { get; set; } = true;");
        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyRewardTiersListVm");
        loyaltyVmsSource.Should().Contain("public LoyaltyRewardTierQueueFilter Filter { get; set; } = LoyaltyRewardTierQueueFilter.All;");
        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyRewardTierEditVm");
        loyaltyVmsSource.Should().Contain("public LoyaltyRewardType RewardType { get; set; } = LoyaltyRewardType.FreeItem;");
        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyAccountsListVm");
        loyaltyVmsSource.Should().Contain("public LoyaltyAccountStatus? StatusFilter { get; set; }");
        loyaltyVmsSource.Should().Contain("public List<SelectListItem> StatusItems { get; set; } = new();");
        loyaltyVmsSource.Should().Contain("public sealed class CreateLoyaltyAccountVm");
        loyaltyVmsSource.Should().Contain("public List<SelectListItem> UserOptions { get; set; } = new();");
        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyCampaignsListVm");
        loyaltyVmsSource.Should().Contain("public LoyaltyCampaignQueueFilter Filter { get; set; } = LoyaltyCampaignQueueFilter.All;");
        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyCampaignEditVm");
        loyaltyVmsSource.Should().Contain("public short Channels { get; set; } = 1;");
        loyaltyVmsSource.Should().Contain("public string CampaignState { get; set; } = \"Draft\";");
        loyaltyVmsSource.Should().Contain("public string? TargetingJson { get; set; } = \"{}\";");
        loyaltyVmsSource.Should().Contain("public string? PayloadJson { get; set; } = \"{}\";");
        loyaltyVmsSource.Should().Contain("public List<SelectListItem> ChannelItems { get; set; } = new()");
        loyaltyVmsSource.Should().Contain("new(\"In-app only\", \"1\"),");
        loyaltyVmsSource.Should().Contain("new(\"In-app + Push\", \"3\")");
        loyaltyVmsSource.Should().Contain("public sealed class LoyaltyScanSessionsListVm");
        loyaltyVmsSource.Should().Contain("public LoyaltyScanMode? ModeFilter { get; set; }");
        loyaltyVmsSource.Should().Contain("public LoyaltyScanStatus? StatusFilter { get; set; }");
        loyaltyVmsSource.Should().Contain("public LoyaltyScanSessionOpsSummaryVm Summary { get; set; } = new();");
    }


    [Fact]
    public void HomeDashboardAndCultureControllers_Should_KeepAdminAndLocalizationBoundaries()
    {
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));
        var homeSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "HomeController.cs"));
        var dashboardSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "DashboardController.cs"));
        var cultureSource = ReadWebAdminFile(Path.Combine("Controllers", "CultureController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");

        homeSource.Should().Contain("public sealed class HomeController : AdminBaseController");
        homeSource.Should().Contain("public async Task<IActionResult> Index(Guid? businessId = null, CancellationToken ct = default)");
        homeSource.Should().Contain("public async Task<IActionResult> CommunicationOpsFragment(Guid? businessId = null, CancellationToken ct = default)");
        homeSource.Should().Contain("public async Task<IActionResult> BusinessSupportQueueFragment(Guid? businessId = null, CancellationToken ct = default)");
        homeSource.Should().Contain("public IActionResult AlertsFragment()");

        dashboardSource.Should().Contain("public sealed class DashboardController : AdminBaseController");
        dashboardSource.Should().Contain("[HttpGet(\"/admin\")]");
        dashboardSource.Should().Contain("[HttpGet(\"/dashboard\")]");
        dashboardSource.Should().Contain("public IActionResult Index()");

        cultureSource.Should().Contain("public sealed class CultureController : Controller");
        cultureSource.Should().Contain("[HttpPost]");
        cultureSource.Should().Contain("[ValidateAntiForgeryToken]");
        cultureSource.Should().Contain("public IActionResult SetCulture(string culture, string? returnUrl = null)");
        cultureSource.Should().Contain("AdminCultureCatalog.NormalizeUiCulture(culture)");
        cultureSource.Should().Contain("CookieRequestCultureProvider.DefaultCookieName");
        cultureSource.Should().Contain("Url.IsLocalUrl(returnUrl)");
        cultureSource.Should().Contain("return LocalRedirect(returnUrl);");
        cultureSource.Should().Contain("return RedirectToAction(\"Index\", \"Home\");");
    }


    [Fact]
    public void HomeCommunicationOpsCard_Should_KeepHelperBackedChannelShortcutsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Home", "_CommunicationOpsCard.cshtml"));

        source.Should().Contain("string CommunicationChannelLabel(string channel) => channel switch");
        source.Should().Contain("\"SMS\" => T.T(\"SMS\")");
        source.Should().Contain("\"WhatsApp\" => T.T(\"WhatsApp\")");
        source.Should().Contain("asp-fragment=\"site-settings-sms\" class=\"btn btn-sm btn-outline-secondary\">@CommunicationChannelLabel(\"SMS\")</a>");
        source.Should().Contain("asp-fragment=\"site-settings-whatsapp\" class=\"btn btn-sm btn-outline-secondary\">@CommunicationChannelLabel(\"WhatsApp\")</a>");
        source.Should().NotContain("asp-fragment=\"site-settings-sms\" class=\"btn btn-sm btn-outline-secondary\">@T.T(\"SMS\")</a>");
        source.Should().NotContain("asp-fragment=\"site-settings-whatsapp\" class=\"btn btn-sm btn-outline-secondary\">@T.T(\"WhatsApp\")</a>");
    }


    [Fact]
    public void SiteSettingsController_Should_KeepRenderRedirectAndMappingContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Settings", "SiteSettingsController.cs"));

        source.Should().Contain("public async Task<IActionResult> Edit(CancellationToken ct)");
        source.Should().Contain("var dto = await _cache.GetAsync(ct);");
        source.Should().Contain("var vm = MapToVm(dto);");
        source.Should().Contain("return RenderEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Edit(SiteSettingVm vm, CancellationToken ct)");
        source.Should().Contain("if (!ModelState.IsValid)");
        source.Should().Contain("var dto = MapToUpdateDto(vm);");
        source.Should().Contain("await _update.HandleAsync(dto, ct);");
        source.Should().Contain("_cache.Invalidate();");
        source.Should().Contain("SetSuccessMessage(\"SettingsUpdatedMessage\")");
        source.Should().Contain("return RedirectOrHtmx(nameof(Edit));");
        source.Should().Contain("catch (DbUpdateConcurrencyException)");
        source.Should().Contain("ModelState.AddModelError(string.Empty, T(\"SettingsConcurrencyMessage\"));");
        source.Should().Contain("catch (FluentValidation.ValidationException ex)");
        source.Should().Contain("ModelState.AddModelError(e.PropertyName, e.ErrorMessage);");
        source.Should().Contain("return PartialView(\"~/Views/SiteSettings/_SiteSettingsEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Edit\", vm);");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName) ?? string.Empty;");
        source.Should().Contain("return RedirectToAction(actionName);");
        source.Should().Contain("private static SiteSettingVm MapToVm(SiteSettingDto dto) => new()");
        source.Should().Contain("DefaultCulture = dto.DefaultCulture,");
        source.Should().Contain("BusinessManagementWebsiteUrl = dto.BusinessManagementWebsiteUrl,");
        source.Should().Contain("WhatsAppBusinessPhoneId = dto.WhatsAppBusinessPhoneId,");
        source.Should().Contain("SmsProvider = dto.SmsProvider,");
        source.Should().Contain("CommunicationTestInboxEmail = dto.CommunicationTestInboxEmail,");
        source.Should().Contain("PhoneVerificationPreferredChannel = dto.PhoneVerificationPreferredChannel,");
        source.Should().Contain("private static SiteSettingDto MapToUpdateDto(SiteSettingVm vm) => new()");
        source.Should().Contain("DefaultCulture = vm.DefaultCulture,");
        source.Should().Contain("BusinessManagementWebsiteUrl = vm.BusinessManagementWebsiteUrl,");
        source.Should().Contain("WhatsAppBusinessPhoneId = vm.WhatsAppBusinessPhoneId,");
        source.Should().Contain("SmsProvider = vm.SmsProvider,");
        source.Should().Contain("CommunicationTestInboxEmail = vm.CommunicationTestInboxEmail,");
        source.Should().Contain("PhoneVerificationPreferredChannel = vm.PhoneVerificationPreferredChannel,");
    }

    [Fact]
    public void WebAdminLocalizationMetadataAndIdentityForms_Should_KeepResxBackedDisplayContractsWired()
    {
        var dependencyInjectionSource = ReadWebAdminFile(Path.Combine("Extensions", "DependencyInjection.cs"));
        var metadataProviderSource = ReadWebAdminFile(Path.Combine("Localization", "SharedDisplayMetadataProvider.cs"));
        var metadataOptionsSource = ReadWebAdminFile(Path.Combine("Localization", "ConfigureDisplayMetadataLocalization.cs"));
        var userVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Identity", "UserVms.cs"));
        var siteSettingVmSource = ReadWebAdminFile(Path.Combine("ViewModels", "Settings", "SiteSettingVm.cs"));

        dependencyInjectionSource.Should().Contain("services.AddLocalization(options => options.ResourcesPath = \"Resources\");");
        dependencyInjectionSource.Should().Contain("services.AddSingleton<IDisplayMetadataProvider, SharedDisplayMetadataProvider>();");
        dependencyInjectionSource.Should().Contain("services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureDisplayMetadataLocalization>();");
        dependencyInjectionSource.Should().Contain(".AddDataAnnotationsLocalization(options =>");
        dependencyInjectionSource.Should().Contain("factory.Create(typeof(SharedResource));");

        metadataProviderSource.Should().Contain("public sealed class SharedDisplayMetadataProvider : IDisplayMetadataProvider");
        metadataProviderSource.Should().Contain("var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();");
        metadataProviderSource.Should().Contain("var resourceKey = displayAttribute.Name!;");
        metadataProviderSource.Should().Contain("context.DisplayMetadata.DisplayName = () => _localizer[resourceKey];");

        metadataOptionsSource.Should().Contain("public sealed class ConfigureDisplayMetadataLocalization : IConfigureOptions<MvcOptions>");
        metadataOptionsSource.Should().Contain("options.ModelMetadataDetailsProviders.Add(_displayMetadataProvider);");

        userVmsSource.Should().Contain("[Display(Name = \"Locale\")]");
        userVmsSource.Should().Contain("[Display(Name = \"TimeZone\")]");
        userVmsSource.Should().Contain("[Display(Name = \"Currency\")]");
        userVmsSource.Should().Contain("[Display(Name = \"Email\")]");
        userVmsSource.Should().Contain("[Display(Name = \"NewPassword\")]");
        userVmsSource.Should().Contain("[Display(Name = \"ConfirmNewPassword\")]");
        userVmsSource.Should().Contain("[MinLength(8, ErrorMessage = \"Password must be at least 8 characters.\")]");
        userVmsSource.Should().Contain("[Compare(nameof(NewPassword), ErrorMessage = \"Passwords do not match.\")]");

        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingDefaultCulture\"), Required, MaxLength(10)]");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingSupportedCulturesCsv\"), Required]");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingAdminTextOverridesJson\")]");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingPhoneVerificationPreferredChannel\")]");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingPhoneVerificationAllowFallback\")]");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingBusinessInvitationEmailSubjectTemplate\")]");
        siteSettingVmSource.Should().Contain("[Display(Name = \"SiteSettingPasswordResetEmailBodyTemplate\")]");
    }


    [Fact]
    public void HomeCommunicationOpsCard_Should_KeepHeaderMetricsAndSelectedBusinessRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Home", "_CommunicationOpsCard.cshtml"));

        source.Should().Contain("hx-get=\"@Url.Action(\"CommunicationOpsFragment\", \"Home\", new { businessId = Model.SelectedBusinessId })\"");
        source.Should().Contain("hx-target=\"#communication-ops-card\"");
        source.Should().Contain("@T.T(\"OpenWorkspaceAction\")");
        source.Should().Contain("asp-fragment=\"site-settings-smtp\"");
        source.Should().Contain("asp-fragment=\"site-settings-admin-routing\"");
        source.Should().Contain("@T.T(\"EmailTransport\")");
        source.Should().Contain("@T.T(\"SmsTransport\")");
        source.Should().Contain("@T.T(\"WhatsAppTransport\")");
        source.Should().Contain("@T.T(\"AdminAlertRouting\")");
        source.Should().Contain("@T.T(\"BusinessesRequiringEmailSetup\")");
        source.Should().Contain("@T.T(\"MissingSupportEmail\")");
        source.Should().Contain("@T.T(\"MissingSenderIdentity\")");
        source.Should().Contain("@T.T(\"CommunicationTransactionalEmailEnabledLabel\")");
        source.Should().Contain("@T.T(\"CommunicationMarketingEmailEnabledLabel\")");
        source.Should().Contain("@T.T(\"CommunicationOperationalAlertsEnabledLabel\")");
        source.Should().Contain("@if (Model.SelectedBusinessId.HasValue)");
        source.Should().Contain("@T.T(\"CommunicationOpenCurrentBusinessSetupAction\")");
        source.Should().Contain("@T.T(\"CommunicationOpenMemberSupportAction\")");
        source.Should().Contain("@T.T(\"CommunicationInspectProfileAction\")");
    }


    [Fact]
    public void HomeDashboardView_Should_KeepShellAndQuickLinksWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Home", "Index.cshtml"));

        source.Should().Contain("ViewData[\"Title\"] = T.T(\"AdminDashboardTitle\")");
        source.Should().Contain("var canManageIdentity = await Perms.HasAsync(\"FullAdminAccess\")");
        source.Should().Contain("var canManageBusinesses = await Perms.HasAsync(\"ManageBusinessSupport\")");
        source.Should().Contain("<h1 class=\"mb-1\">@T.T(\"AdminDashboardTitle\")</h1>");
        source.Should().Contain("@T.T(\"AdminDashboardIntro\")");
        source.Should().Contain("<form asp-action=\"Index\" method=\"get\" class=\"d-flex align-items-center gap-2\">");
        source.Should().Contain("<label for=\"businessId\" class=\"form-label mb-0 text-muted\">@T.T(\"BusinessContext\")</label>");
        source.Should().Contain("<select id=\"businessId\" name=\"businessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\">");
        source.Should().Contain("@T.T(\"NoActiveBusiness\")");
        source.Should().Contain("@T.T(\"Apply\")");
        source.Should().Contain("<partial name=\"~/Views/Home/_CommunicationOpsCard.cshtml\" model=\"Model\" />");
        source.Should().Contain("<partial name=\"~/Views/Home/_BusinessSupportQueueCard.cshtml\" model=\"Model\" />");
        source.Should().Contain("@T.T(\"DashboardProductsLabel\")");
        source.Should().Contain("@T.T(\"DashboardPagesLabel\")");
        source.Should().Contain("@T.T(\"DashboardOrdersLabel\")");
        source.Should().Contain("@T.T(\"DashboardUsersLabel\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinksTitle\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkProductsAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkPagesAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkMediaAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkOrdersAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkCustomersAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkLeadsAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkLoyaltyAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkBusinessesAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkMobileOpsAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkWarehousesAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkPaymentsAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkUsersAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkMobileLinkedUsersAction\")");
        source.Should().Contain("@T.T(\"DashboardQuickLinkSettingsAction\")");
    }


    [Fact]
    public void HomeDashboardView_Should_KeepCrmBusinessLoyaltyAndMobileCardsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Home", "Index.cshtml"));

        source.Should().Contain("@T.T(\"DashboardCrmTitle\")");
        source.Should().Contain("@T.T(\"DashboardCrmIntro\")");
        source.Should().Contain("@T.T(\"DashboardOpenCrmAction\")");
        source.Should().Contain("@T.T(\"DashboardCustomersLabel\")");
        source.Should().Contain("@T.T(\"DashboardLeadsLabel\")");
        source.Should().Contain("@T.T(\"DashboardQualifiedLeadsLabel\")");
        source.Should().Contain("@T.T(\"DashboardOpenOpportunitiesLabel\")");
        source.Should().Contain("@T.T(\"DashboardSegmentsLabel\")");
        source.Should().Contain("@T.T(\"DashboardInteractions7dLabel\")");
        source.Should().Contain("@T.T(\"DashboardOpenPipelineLabel\")");
        source.Should().Contain("@T.T(\"DashboardBusinessOperationsTitle\")");
        source.Should().Contain("@if (Model.SelectedBusinessId.HasValue)");
        source.Should().Contain("@string.Format(T.T(\"DashboardCurrentBusinessLabel\"), Model.SelectedBusinessLabel)");
        source.Should().Contain("@T.T(\"DashboardNoBusinessOperationsIntro\")");
        source.Should().Contain("@T.T(\"DashboardPaymentsLabel\")");
        source.Should().Contain("@T.T(\"DashboardWarehousesLabel\")");
        source.Should().Contain("@T.T(\"DashboardSuppliersLabel\")");
        source.Should().Contain("@T.T(\"DashboardPurchaseOrdersLabel\")");
        source.Should().Contain("@T.T(\"DashboardOpenPaymentsAction\")");
        source.Should().Contain("@T.T(\"DashboardOpenInventoryAction\")");
        source.Should().Contain("@T.T(\"DashboardOpenPurchaseOrdersAction\")");
        source.Should().Contain("@T.T(\"LoyaltyOperations\")");
        source.Should().Contain("@T.T(\"LoyaltyOperationsIntro\")");
        source.Should().Contain("@T.T(\"OpenLoyalty\")");
        source.Should().Contain("@T.T(\"Accounts\")");
        source.Should().Contain("@T.T(\"PendingRedemptions\")");
        source.Should().Contain("@T.T(\"RecentScanSessions\")");
        source.Should().Contain("@T.T(\"SelectBusinessForLoyaltyMobile\")");
        source.Should().Contain("@T.T(\"MobileOperations\")");
        source.Should().Contain("@T.T(\"MobileOperationsDashboardIntro\")");
        source.Should().Contain("@T.T(\"OpenMobileOps\")");
        source.Should().Contain("@T.T(\"ActiveDevices\")");
        source.Should().Contain("@T.T(\"StaleDevices\")");
        source.Should().Contain("@T.T(\"MissingPush\")");
        source.Should().Contain("@T.T(\"DeviceDiagnostics\")");
        source.Should().Contain("@T.T(\"DashboardOpenMobileLinkedUsersAction\")");
        source.Should().Contain("@T.T(\"SupportQueue\")");
        source.Should().Contain("@T.T(\"Communications\")");
    }


    [Fact]
    public void HomeController_Should_KeepDashboardCompositionAndFragmentBuildersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "HomeController.cs"));

        source.Should().Contain("public async Task<IActionResult> Index(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("var businessOptions = await _referenceData.GetBusinessOptionsAsync(selectedBusinessId: businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("var selectedBusinessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("var crmSummary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("var products = await _getProductsPage.HandleAsync(page: 1, pageSize: 1, culture: siteSettings.DefaultCulture, ct).ConfigureAwait(false);");
        source.Should().Contain("var pages = await _getPagesPage.HandleAsync(page: 1, pageSize: 1, culture: siteSettings.DefaultCulture, ct: ct).ConfigureAwait(false);");
        source.Should().Contain("var orders = await _getOrdersPage.HandleAsync(page: 1, pageSize: 1, ct: ct).ConfigureAwait(false);");
        source.Should().Contain("var users = await _getUsersPage.HandleAsync(page: 1, pageSize: 1, emailFilter: null, filter: Darwin.Application.Identity.DTOs.UserQueueFilter.All, ct: ct).ConfigureAwait(false);");
        source.Should().Contain("var businessSupport = await _getBusinessSupportSummary.HandleAsync(selectedBusinessId, ct).ConfigureAwait(false);");
        source.Should().Contain("var communicationOps = await _getBusinessCommunicationOpsSummary.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("var mobileDeviceOps = await _getMobileDeviceOpsSummary.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("var vm = new AdminDashboardVm");
        source.Should().Contain("Crm = MapCrmSummary(crmSummary, siteSettings.DefaultCurrency),");
        source.Should().Contain("BusinessSupport = MapBusinessSupportSummary(businessSupport),");
        source.Should().Contain("CommunicationOps = MapCommunicationOpsSummary(communicationOps, siteSettings),");
        source.Should().Contain("return View(vm);");
        source.Should().Contain("public async Task<IActionResult> CommunicationOpsFragment(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("var vm = await BuildCommunicationOpsCardVmAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("return PartialView(\"~/Views/Home/_CommunicationOpsCard.cshtml\", vm);");
        source.Should().Contain("public async Task<IActionResult> BusinessSupportQueueFragment(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("var vm = await BuildBusinessSupportCardVmAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("return PartialView(\"~/Views/Home/_BusinessSupportQueueCard.cshtml\", vm);");
    }


    [Fact]
    public void HomeController_Should_KeepDashboardSummaryMappersAndCardPayloadBuildersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "HomeController.cs"));

        source.Should().Contain("private async Task<AdminDashboardVm> BuildCommunicationOpsCardVmAsync(Guid? businessId, CancellationToken ct)");
        source.Should().Contain("SelectedBusinessId = selectedBusinessId,");
        source.Should().Contain("SelectedBusinessLabel = businessOptions.FirstOrDefault(x => x.Selected)?.Text ?? string.Empty,");
        source.Should().Contain("CommunicationOps = MapCommunicationOpsSummary(communicationOps, siteSettings)");
        source.Should().Contain("private async Task<AdminDashboardVm> BuildBusinessSupportCardVmAsync(Guid? businessId, CancellationToken ct)");
        source.Should().Contain("BusinessSupport = MapBusinessSupportSummary(businessSupport)");
        source.Should().Contain("private static CrmSummaryVm MapCrmSummary(CrmSummaryDto dto, string currency)");
        source.Should().Contain("Currency = string.IsNullOrWhiteSpace(currency) ? string.Empty : currency.Trim().ToUpperInvariant(),");
        source.Should().Contain("private static BusinessSupportSummaryVm MapBusinessSupportSummary(Darwin.Application.Businesses.DTOs.BusinessSupportSummaryDto dto)");
        source.Should().Contain("AttentionBusinessCount = dto.AttentionBusinessCount,");
        source.Should().Contain("PendingApprovalBusinessCount = dto.PendingApprovalBusinessCount,");
        source.Should().Contain("OpenInvitationCount = dto.OpenInvitationCount,");
        source.Should().Contain("PendingActivationMemberCount = dto.PendingActivationMemberCount,");
        source.Should().Contain("LockedMemberCount = dto.LockedMemberCount,");
        source.Should().Contain("private static BusinessCommunicationOpsSummaryVm MapCommunicationOpsSummary(");
        source.Should().Contain("EmailTransportConfigured = siteSettings.SmtpEnabled &&");
        source.Should().Contain("SmsTransportConfigured = siteSettings.SmsEnabled &&");
        source.Should().Contain("WhatsAppTransportConfigured = siteSettings.WhatsAppEnabled &&");
        source.Should().Contain("AdminAlertRoutingConfigured = !string.IsNullOrWhiteSpace(siteSettings.AdminAlertEmailsCsv) ||");
    }


    [Fact]
    public void HomeController_Should_KeepSelectedBusinessHydrationAndAlertsFragmentWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Home", "HomeController.cs"));

        source.Should().Contain("int? paymentCount = null;");
        source.Should().Contain("int? warehouseCount = null;");
        source.Should().Contain("int? supplierCount = null;");
        source.Should().Contain("int? purchaseOrderCount = null;");
        source.Should().Contain("int? loyaltyAccountCount = null;");
        source.Should().Contain("int? pendingRedemptionCount = null;");
        source.Should().Contain("int? scanSessionCount = null;");
        source.Should().Contain("if (selectedBusinessId.HasValue)");
        source.Should().Contain("paymentCount = (await _getPaymentsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: null, ct).ConfigureAwait(false)).Total;");
        source.Should().Contain("warehouseCount = (await _getWarehousesPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: Darwin.Application.Inventory.DTOs.WarehouseQueueFilter.All, ct).ConfigureAwait(false)).Total;");
        source.Should().Contain("supplierCount = (await _getSuppliersPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: Darwin.Application.Inventory.DTOs.SupplierQueueFilter.All, ct).ConfigureAwait(false)).Total;");
        source.Should().Contain("purchaseOrderCount = (await _getPurchaseOrdersPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, filter: Darwin.Application.Inventory.DTOs.PurchaseOrderQueueFilter.All, ct).ConfigureAwait(false)).Total;");
        source.Should().Contain("loyaltyAccountCount = (await _getLoyaltyAccountsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, status: null, ct).ConfigureAwait(false)).Total;");
        source.Should().Contain("pendingRedemptionCount = (await _getLoyaltyRedemptionsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, status: LoyaltyRedemptionStatus.Pending, ct).ConfigureAwait(false)).Total;");
        source.Should().Contain("scanSessionCount = (await _getLoyaltyScanSessionsPage.HandleAsync(selectedBusinessId.Value, page: 1, pageSize: 1, query: null, mode: null, status: null, ct).ConfigureAwait(false)).Total;");
        source.Should().Contain("PaymentCount = paymentCount,");
        source.Should().Contain("WarehouseCount = warehouseCount,");
        source.Should().Contain("SupplierCount = supplierCount,");
        source.Should().Contain("PurchaseOrderCount = purchaseOrderCount,");
        source.Should().Contain("LoyaltyAccountCount = loyaltyAccountCount,");
        source.Should().Contain("PendingRedemptionCount = pendingRedemptionCount,");
        source.Should().Contain("ScanSessionCount = scanSessionCount,");
        source.Should().Contain("public IActionResult AlertsFragment()");
        source.Should().Contain("return PartialView(\"~/Views/Shared/_Alerts.cshtml\");");
    }


    [Fact]
    public void MobileOperationsWorkspace_Should_KeepHelperBackedChannelAndInvitationLabelsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "MobileOperations", "Index.cshtml"));

        source.Should().Contain("string CommunicationChannelLabel(string channel) => channel switch");
        source.Should().Contain("\"Sms\" => T.T(\"Sms\")");
        source.Should().Contain("\"WhatsApp\" => T.T(\"WhatsApp\")");
        source.Should().Contain("_ => string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        source.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        source.Should().Contain("BusinessInvitationQueueFilter.Open => T.T(\"OpenInvitations\")");
        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open): <strong>@Model.OpenInvitationCount</strong>");
        source.Should().Contain("@CommunicationChannelLabel(\"Sms\")");
        source.Should().Contain("@CommunicationChannelLabel(\"WhatsApp\")");
        source.Should().NotContain("@T.T(\"OpenInvitations\"): <strong>@Model.OpenInvitationCount</strong>");
        source.Should().NotContain("<div class=\"text-muted small\">@T.T(\"Sms\")</div>");
        source.Should().NotContain("<div class=\"text-muted small\">@T.T(\"WhatsApp\")</div>");
    }

    [Fact]
    public void HomeCommunicationOpsCard_Should_KeepLocalizedChannelFallbackContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Home", "_CommunicationOpsCard.cshtml"));

        source.Should().Contain("string CommunicationChannelLabel(string channel) => channel switch");
        source.Should().Contain("\"SMS\" => T.T(\"SMS\")");
        source.Should().Contain("\"WhatsApp\" => T.T(\"WhatsApp\")");
        source.Should().Contain("_ => string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
        source.Should().Contain("@CommunicationChannelLabel(\"SMS\")");
        source.Should().Contain("@CommunicationChannelLabel(\"WhatsApp\")");
    }


    [Fact]
    public void MobileOperationsWorkspace_Should_KeepHelperBackedOnboardingDependencyLabelsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "MobileOperations", "Index.cshtml"));

        source.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        source.Should().Contain("BusinessMemberSupportFilter.Attention => T.T(\"NeedsAttention\")");
        source.Should().Contain("BusinessMemberSupportFilter.PendingActivation => T.T(\"PendingActivationMembers\")");
        source.Should().Contain("BusinessMemberSupportFilter.Locked => T.T(\"LockedMembers\")");
        source.Should().Contain("string BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus status) => status switch");
        source.Should().Contain("BusinessOperationalStatus.PendingApproval => T.T(\"PendingApprovalBusinesses\")");
        source.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention): <strong>@Model.AttentionBusinessCount</strong>");
        source.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval): <strong>@Model.PendingApprovalBusinessCount</strong>");
        source.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation): <strong>@Model.PendingActivationMemberCount</strong>");
        source.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked): <strong>@Model.LockedMemberCount</strong>");
        source.Should().NotContain("@T.T(\"BusinessesNeedingAttention\"): <strong>@Model.AttentionBusinessCount</strong>");
        source.Should().NotContain("@T.T(\"PendingApprovalBusinesses\"): <strong>@Model.PendingApprovalBusinessCount</strong>");
        source.Should().NotContain("@T.T(\"PendingActivationMembers\"): <strong>@Model.PendingActivationMemberCount</strong>");
        source.Should().NotContain("@T.T(\"LockedMembers\"): <strong>@Model.LockedMemberCount</strong>");
    }


    [Fact]
    public void MobileOperationsWorkspace_Should_KeepHeaderAndLoyaltyFollowUpRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "MobileOperations", "Index.cshtml"));

        source.Should().Contain("<h1 class=\"mb-1\">@T.T(\"MobileOperationsTitle\")</h1>");
        source.Should().Contain("@T.T(\"MobileOperationsIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-mobile\" })\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"SiteSettings\")</a>");
        source.Should().Contain("hx-get=\"@Url.Action(\"SupportQueue\", \"Businesses\")\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"SupportQueue\")</a>");
        source.Should().Contain("hx-get=\"@Url.Action(\"ScanSessions\", \"Loyalty\")\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"LoyaltyScanSessionsTitle\")</a>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"MobileLinked\" })\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"UsersFilterMobileLinked\")</a>");
        source.Should().Contain("<div class=\"card-header\">@T.T(\"LoyaltyMobileFollowUp\")</div>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Accounts\", \"Loyalty\")\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Redemptions\", \"Loyalty\")\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Campaigns\", \"Loyalty\")\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Programs\", \"Loyalty\")\"");
    }


    [Fact]
    public void MobileOperationsWorkspace_Should_KeepOnboardingAndTransportActionRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "MobileOperations", "Index.cshtml"));

        source.Should().Contain("<div class=\"card-header\">@T.T(\"MobileOnboardingDependenciesTitle\")</div>");
        source.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Attention): <strong>@Model.AttentionBusinessCount</strong>");
        source.Should().Contain("@BusinessOperationalQueueLabel(Darwin.Domain.Enums.BusinessOperationalStatus.PendingApproval): <strong>@Model.PendingApprovalBusinessCount</strong>");
        source.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open): <strong>@Model.OpenInvitationCount</strong>");
        source.Should().Contain("hx-get=\"@Url.Action(\"SupportQueue\", \"Businesses\")\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"Unconfirmed\" })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"Locked\" })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"MobileLinked\" })\"");
        source.Should().Contain("<div class=\"card-header\">@T.T(\"TransportReadiness\")</div>");
        source.Should().Contain("@CommunicationChannelLabel(\"Sms\")");
        source.Should().Contain("@CommunicationChannelLabel(\"WhatsApp\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { providerReviewOnly = true })\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"ReviewProviderLane\")</a>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"BusinessCommunications\")\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenCommunicationsWorkspace\")</a>");
    }


    [Fact]
    public void MobileOperationsWorkspace_Should_KeepBootstrapAndBusinessAppCardsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "MobileOperations", "Index.cshtml"));

        source.Should().Contain("<div class=\"card-header\">@T.T(\"MobileBootstrapTitle\")</div>");
        source.Should().Contain("@T.T(\"MobileBootstrapJwtEnabled\"): <strong>@(Model.JwtEnabled ? readyText : missingText)</strong>");
        source.Should().Contain("@T.T(\"MobileBootstrapSingleDeviceOnly\"): <strong>@(Model.JwtSingleDeviceOnly ? readyText : missingText)</strong>");
        source.Should().Contain("@T.T(\"MobileBootstrapDeviceBindingRequired\"): <strong>@(Model.JwtRequireDeviceBinding ? readyText : missingText)</strong>");
        source.Should().Contain("@T.T(\"MobileBootstrapAccessTokenMinutes\"): <strong>@Model.JwtAccessTokenMinutes</strong>");
        source.Should().Contain("@T.T(\"MobileBootstrapRefreshTokenDays\"): <strong>@Model.JwtRefreshTokenDays</strong>");
        source.Should().Contain("@T.T(\"MobileBootstrapQrRefreshSeconds\"): <strong>@Model.MobileQrTokenRefreshSeconds</strong>");
        source.Should().Contain("@T.T(\"MobileBootstrapMaxOutboxItems\"): <strong>@Model.MobileMaxOutboxItems</strong>");
        source.Should().Contain("@T.T(\"DefaultCulture\"): <strong>@Model.DefaultCulture</strong>");
        source.Should().Contain("@T.T(\"TimeZone\"): <strong>@Model.TimeZone</strong>");
        source.Should().Contain("<div class=\"card-header\">@T.T(\"MobileBusinessAppHandoffsTitle\")</div>");
        source.Should().Contain("@T.T(\"SubscriptionWebsite\")");
        source.Should().Contain("@T.T(\"AccountDeletion\")");
        source.Should().Contain("@T.T(\"Impressum\")");
        source.Should().Contain("@T.T(\"Privacy\")");
        source.Should().Contain("@T.T(\"BusinessTerms\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-business-app\" })\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenBusinessAppSettings\")</a>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-mobile\" })\"");
        source.Should().Contain("hx-push-url=\"true\">@T.T(\"OpenMobileBootstrap\")</a>");
    }


    [Fact]
    public void MobileOperationsWorkspace_Should_KeepDeviceFleetVersionsAndGridWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "MobileOperations", "Index.cshtml"));

        source.Should().Contain("string LocalizeMobilePlatform(object? platform) => platform is null ? \"-\" : T.T(platform.ToString() ?? string.Empty);");
        source.Should().Contain("<div class=\"card-header\">@T.T(\"DeviceFleetSnapshotTitle\")</div>");
        source.Should().Contain("@T.T(\"ActiveDevices\")");
        source.Should().Contain("@T.T(\"BusinessMemberDevices\")");
        source.Should().Contain("@T.T(\"MobileStaleDevices\")");
        source.Should().Contain("@T.T(\"MobileMissingPushToken\")");
        source.Should().Contain("@T.T(\"MobileNotificationsDisabled\")");
        source.Should().Contain("@T.T(\"Android\")");
        source.Should().Contain("@T.T(\"iOS\")");
        source.Should().Contain("<div class=\"card-header\">@T.T(\"RecentAppVersions\")</div>");
        source.Should().Contain("@T.T(\"NoDeviceVersionData\")");
        source.Should().Contain("<div class=\"card-header\">@T.T(\"RecentMobileDevices\")</div>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\")\"");
        source.Should().Contain("placeholder=\"@T.T(\"MobileSearchUserDeviceVersion\")\"");
        source.Should().Contain("<select name=\"platform\" asp-items=\"Model.PlatformItems\" class=\"form-select\"></select>");
        source.Should().Contain("<select name=\"state\" asp-items=\"Model.StateItems\" class=\"form-select\"></select>");
        source.Should().Contain("@T.T(\"NoDevicesMatchCurrentFilters\")");
        source.Should().Contain("@LocalizeMobilePlatform(item.Platform)");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Users\", new { id = item.UserId })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Accounts\", \"Loyalty\", new { q = item.UserEmail })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"ScanSessions\", \"Loyalty\", new { q = item.UserEmail })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { providerReviewOnly = true })\"");
        source.Should().Contain("@T.T(\"ClearPush\")");
        source.Should().Contain("@T.T(\"Deactivate\")");
    }


    [Fact]
    public void MobileOperationsController_Should_KeepWorkspaceCompositionAndRenderContractWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Mobile", "MobileOperationsController.cs"));

        source.Should().Contain("public sealed class MobileOperationsController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(");
        source.Should().Contain("var settings = await _getSiteSettings.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("var support = await _getBusinessSupportSummary.HandleAsync(ct: ct).ConfigureAwait(false);");
        source.Should().Contain("var comms = await _getCommunicationSummary.HandleAsync(ct: ct).ConfigureAwait(false);");
        source.Should().Contain("var deviceSummary = await _getDeviceSummary.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("var devicesPage = await _getDevicesPage.HandleAsync(page, pageSize, q, platform, state, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"MobileOperationsSiteSettingsMissing\")");
        source.Should().Contain("return RedirectOrHtmx(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-mobile\" });");
        source.Should().Contain("var vm = new MobileOperationsVm");
        source.Should().Contain("Playbooks = BuildPlaybooks(),");
        source.Should().Contain("PlatformItems = BuildPlatformItems(platform),");
        source.Should().Contain("StateItems = BuildStateItems(state),");
        source.Should().Contain("return RenderIndex(vm);");
        source.Should().Contain("return PartialView(\"~/Views/MobileOperations/Index.cshtml\", vm);");
        source.Should().Contain("return View(vm);");
    }


    [Fact]
    public void MobileOperationsController_Should_KeepMutationActionsAndRedirectContractWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Mobile", "MobileOperationsController.cs"));

        source.Should().Contain("public async Task<IActionResult> ClearPushToken(Guid id, byte[]? rowVersion, string? q = null, MobilePlatform? platform = null, string? state = null, int page = 1, CancellationToken ct = default)");
        source.Should().Contain("var result = await _clearDevicePushToken.HandleAsync(id, rowVersion, ct).ConfigureAwait(false);");
        source.Should().Contain("SetSuccessMessage(\"MobilePushTokenCleared\")");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"MobilePushTokenClearFailed\")");
        source.Should().Contain("public async Task<IActionResult> DeactivateDevice(Guid id, byte[]? rowVersion, string? q = null, MobilePlatform? platform = null, string? state = null, int page = 1, CancellationToken ct = default)");
        source.Should().Contain("var result = await _deactivateDevice.HandleAsync(id, rowVersion, ct).ConfigureAwait(false);");
        source.Should().Contain("SetSuccessMessage(\"MobileDeviceDeactivated\")");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"MobileDeviceDeactivateFailed\")");
        source.Should().Contain("return RedirectOrHtmx(nameof(Index), null, new { q, platform, state, page });");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, controllerName, routeValues) ?? string.Empty;");
        source.Should().Contain("return new EmptyResult();");
        source.Should().Contain("return RedirectToAction(actionName, controllerName, routeValues);");
    }


    [Fact]
    public void MobileOperationsController_Should_KeepPlaybookAndFilterBuildersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Mobile", "MobileOperationsController.cs"));

        source.Should().Contain("private List<MobileOpsPlaybookVm> BuildPlaybooks()");
        source.Should().Contain("Title = T(\"MobilePlaybookPushDebtTitle\")");
        source.Should().Contain("Title = T(\"MobilePlaybookLoyaltyScanTitle\")");
        source.Should().Contain("Title = T(\"MobilePlaybookTransportTitle\")");
        source.Should().Contain("Title = T(\"MobilePlaybookProviderLaneTitle\")");
        source.Should().Contain("Title = T(\"MobilePlaybookMemberLifecycleTitle\")");
        source.Should().Contain("private List<SelectListItem> BuildPlatformItems(MobilePlatform? selected)");
        source.Should().Contain("new(T(\"MobileAllPlatforms\"), string.Empty, !selected.HasValue)");
        source.Should().Contain("Enum.GetValues<MobilePlatform>()");
        source.Should().Contain(".Where(x => x != MobilePlatform.Unknown)");
        source.Should().Contain(".Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x))");
        source.Should().Contain("private List<SelectListItem> BuildStateItems(string? selected)");
        source.Should().Contain("new(T(\"MobileAllStates\"), string.Empty, string.IsNullOrWhiteSpace(selected))");
        source.Should().Contain("new(T(\"MobileStaleDevices\"), \"stale\", string.Equals(selected, \"stale\", StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain("new(T(\"MobileMissingPushToken\"), \"missing-push\", string.Equals(selected, \"missing-push\", StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain("new(T(\"MobileNotificationsDisabled\"), \"notifications-disabled\", string.Equals(selected, \"notifications-disabled\", StringComparison.OrdinalIgnoreCase))");
        source.Should().Contain("new(T(\"MobileBusinessMembersOnly\"), \"business-members\", string.Equals(selected, \"business-members\", StringComparison.OrdinalIgnoreCase))");
    }


    [Fact]
    public void OrdersController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Orders", "OrdersController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> AddPayment(PaymentCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> AddShipment(ShipmentCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> AddRefund(RefundCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateInvoice(OrderInvoiceCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> ChangeStatus(OrderStatusChangeVm vm");
    }


    [Fact]
    public void OrdersController_Should_KeepAdminOrderWorkspacesAndReviewEndpointsReachable()
    {
        var ordersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Orders", "OrdersController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        ordersSource.Should().Contain("public sealed class OrdersController : AdminBaseController");
        ordersSource.Should().Contain("public async Task<IActionResult> Index(");
        ordersSource.Should().Contain("public async Task<IActionResult> ShipmentsQueue(");
        ordersSource.Should().Contain("public async Task<IActionResult> ReturnsQueue(");
        ordersSource.Should().Contain("public async Task<IActionResult> Details(Guid id");
        ordersSource.Should().Contain("public async Task<IActionResult> Payments(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> Shipments(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> Refunds(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> Invoices(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> AddPayment(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> AddShipment(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> AddRefund(Guid orderId");
        ordersSource.Should().Contain("public async Task<IActionResult> CreateInvoice(Guid orderId");
    }


    [Fact]
    public void OrdersWorkspaces_Should_KeepQueueSummaryGridAndPagerContractsWired()
    {
        var indexSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "Index.cshtml"));
        var shipmentsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "ShipmentsQueue.cshtml"));
        var returnsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "ReturnsQueue.cshtml"));

        indexSource.Should().Contain("id=\"orders-workspace-shell\"");
        indexSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        indexSource.Should().Contain("@T.T(\"OrdersTitle\")");
        indexSource.Should().Contain("hx-get=\"@Url.Action(\"ShipmentsQueue\", \"Orders\")\"");
        indexSource.Should().Contain("@T.T(\"ShipmentsQueue\")");
        indexSource.Should().Contain("hx-get=\"@Url.Action(\"ReturnsQueue\", \"Orders\")\"");
        indexSource.Should().Contain("@T.T(\"ReturnsQueueTitle\")");
        indexSource.Should().Contain("placeholder=\"@T.T(\"SearchOrdersPlaceholder\")\"");
        indexSource.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        indexSource.Should().Contain("@T.T(\"OpenQueue\")");
        indexSource.Should().Contain("@T.T(\"PaymentIssues\")");
        indexSource.Should().Contain("@T.T(\"FulfillmentAttention\")");
        indexSource.Should().Contain("@T.T(\"ClearQueueFilters\")");
        indexSource.Should().Contain("@T.T(\"PaymentsCount\")");
        indexSource.Should().Contain("@T.T(\"FailedCount\")");
        indexSource.Should().Contain("@T.T(\"ShipmentsCount\")");
        indexSource.Should().Contain("hx-get=\"@Url.Action(\"AddPayment\", \"Orders\", new { orderId = o.Id })\"");
        indexSource.Should().Contain("hx-get=\"@Url.Action(\"AddShipment\", \"Orders\", new { orderId = o.Id })\"");
        indexSource.Should().Contain("hx-get=\"@Url.Action(\"CreateInvoice\", \"Orders\", new { orderId = o.Id })\"");
        indexSource.Should().Contain("hx-get=\"@Url.Action(\"Details\", \"Orders\", new { id = o.Id })\"");
        indexSource.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        indexSource.Should().Contain("hx-target=\"#orders-workspace-shell\"");

        shipmentsSource.Should().Contain("id=\"shipments-queue-workspace-shell\"");
        shipmentsSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        shipmentsSource.Should().Contain("@T.T(\"ShipmentQueueTitle\")");
        shipmentsSource.Should().Contain("@T.T(\"ShipmentQueueIntro\")");
        shipmentsSource.Should().Contain("@Model.Summary.PendingCount");
        shipmentsSource.Should().Contain("@Model.Summary.ShippedCount");
        shipmentsSource.Should().Contain("@Model.Summary.MissingTrackingCount");
        shipmentsSource.Should().Contain("@Model.Summary.ReturnedCount");
        shipmentsSource.Should().Contain("@Model.Summary.CarrierReviewCount");
        shipmentsSource.Should().Contain("@Model.Summary.ReturnFollowUpCount");
        shipmentsSource.Should().Contain("@T.T(\"DhlPhaseOneReadiness\")");
        shipmentsSource.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-shipping\" })\"");
        shipmentsSource.Should().Contain("@T.T(\"ShipmentSupportPlaybooks\")");
        shipmentsSource.Should().Contain("placeholder=\"@T.T(\"SearchShipmentsPlaceholder\")\"");
        shipmentsSource.Should().Contain("name=\"pageSize\" asp-items=\"Model.PageSizeItems\"");
        shipmentsSource.Should().Contain("@T.T(\"NoShipmentsFound\")");
        shipmentsSource.Should().Contain("string LocalizeShipmentStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        shipmentsSource.Should().Contain("@LocalizeShipmentStatus(item.Status)");
        shipmentsSource.Should().Contain("hx-get=\"@Url.Action(\"Details\", \"Orders\", new { id = item.OrderId })\"");
        shipmentsSource.Should().Contain("asp-fragment=\"refunds\"");
        shipmentsSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"ShippingMethods\", new { filter = \"Dhl\" })\"");
        shipmentsSource.Should().Contain("hx-get=\"@Url.Action(\"AddRefund\", \"Orders\", new { orderId = item.OrderId, paymentId = item.DefaultRefundPaymentId })\"");
        shipmentsSource.Should().Contain("hx-get=\"@Url.Action(\"AddShipment\", \"Orders\", new { orderId = item.OrderId })\"");
        shipmentsSource.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        shipmentsSource.Should().Contain("hx-target=\"#shipments-queue-workspace-shell\"");

        returnsSource.Should().Contain("id=\"returns-queue-workspace-shell\"");
        returnsSource.Should().Contain("@T.T(\"ReturnsQueueTitle\")");
        returnsSource.Should().Contain("@T.T(\"ReturnsQueueIntro\")");
        returnsSource.Should().Contain("hx-get=\"@Url.Action(\"ShipmentsQueue\", \"Orders\")\"");
        returnsSource.Should().Contain("hx-get=\"@Url.Action(\"Refunds\", \"Billing\")\"");
        returnsSource.Should().Contain("@Model.Summary.ReturnedCount");
        returnsSource.Should().Contain("@Model.Summary.ReturnFollowUpCount");
        returnsSource.Should().Contain("@Model.Summary.CarrierReviewCount");
        returnsSource.Should().Contain("@T.T(\"ReturnsQueuePlaybooksTitle\")");
        returnsSource.Should().Contain("placeholder=\"@T.T(\"ReturnsQueueSearchPlaceholder\")\"");
        returnsSource.Should().Contain("@T.T(\"AllReturnCases\")");
        returnsSource.Should().Contain("@T.T(\"NoReturnCases\")");
        returnsSource.Should().Contain("string LocalizeReturnStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        returnsSource.Should().Contain("@LocalizeReturnStatus(item.Status)");
        returnsSource.Should().Contain("hx-get=\"@Url.Action(\"Details\", \"Orders\", new { id = item.OrderId })\"");
        returnsSource.Should().Contain("hx-get=\"@Url.Action(\"Refunds\", \"Billing\", new { q = item.OrderNumber })\"");
        returnsSource.Should().Contain("hx-get=\"@Url.Action(\"AddRefund\", \"Orders\", new { orderId = item.OrderId, paymentId = item.DefaultRefundPaymentId })\"");
        returnsSource.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        returnsSource.Should().Contain("hx-target=\"#returns-queue-workspace-shell\"");
    }


    [Fact]
    public void OrdersController_Should_KeepWorkspaceBuilderAndRenderContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Orders", "OrdersController.cs"));

        source.Should().Contain("FilterItems = BuildOrderFilterItems(filter)");
        source.Should().Contain("return RenderOrdersWorkspace(vm);");
        source.Should().Contain("Dhl = BuildDhlOperationsVm(settings)");
        source.Should().Contain("Summary = await BuildShipmentOpsSummaryVmAsync(settings.ShipmentAttentionDelayHours, settings.ShipmentTrackingGraceHours, ct).ConfigureAwait(false)");
        source.Should().Contain("Playbooks = BuildShipmentPlaybooks()");
        source.Should().Contain("FilterItems = BuildShipmentFilterItems(filter)");
        source.Should().Contain("PageSizeItems = BuildPageSizeItems(pageSize)");
        source.Should().Contain("return RenderShipmentsQueueWorkspace(vm);");
        source.Should().Contain("Playbooks = BuildReturnPlaybooks()");
        source.Should().Contain("FilterItems = BuildReturnFilterItems(filter)");
        source.Should().Contain("return RenderReturnsQueueWorkspace(vm);");
        source.Should().Contain("TaxPolicy = MapTaxPolicy(settings)");
        source.Should().Contain("ReturnSupport = BuildReturnSupportBaseline(dto)");
        source.Should().Contain("return RenderOrderDetailsWorkspace(vm);");
        source.Should().Contain("private async Task<DhlOperationsVm> BuildDhlOperationsVmAsync(CancellationToken ct)");
        source.Should().Contain("return BuildDhlOperationsVm(settings);");
        source.Should().Contain("private static DhlOperationsVm BuildDhlOperationsVm(");
        source.Should().Contain("private TaxPolicySnapshotVm MapTaxPolicy(");
        source.Should().Contain("private async Task<ShipmentOpsSummaryVm> BuildShipmentOpsSummaryVmAsync(");
        source.Should().Contain("private List<ShipmentPlaybookVm> BuildShipmentPlaybooks()");
        source.Should().Contain("private List<ShipmentPlaybookVm> BuildReturnPlaybooks()");
        source.Should().Contain("private static ReturnSupportBaselineVm BuildReturnSupportBaseline(OrderDetailDto dto)");
        source.Should().Contain("private static Guid? ResolveDefaultRefundPaymentId(OrderDetailDto? dto)");
        source.Should().Contain("private static OrderHeaderVm CreateHeader(OrderDetailDto dto)");
        source.Should().Contain("private IActionResult RenderShipmentsQueueWorkspace(ShipmentsQueueVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/ShipmentsQueue.cshtml\", vm)");
        source.Should().Contain("return View(\"ShipmentsQueue\", vm);");
        source.Should().Contain("private IActionResult RenderReturnsQueueWorkspace(ReturnsQueueVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/ReturnsQueue.cshtml\", vm)");
        source.Should().Contain("return View(\"ReturnsQueue\", vm);");
        source.Should().Contain("private IActionResult RenderOrdersWorkspace(OrdersListVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/Index.cshtml\", vm)");
        source.Should().Contain("return View(\"Index\", vm);");
        source.Should().Contain("private IActionResult RenderOrderDetailsWorkspace(OrderDetailVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/Details.cshtml\", vm)");
        source.Should().Contain("return View(\"Details\", vm);");
        source.Should().Contain("private IActionResult RenderPaymentEditor(PaymentCreateVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/_PaymentCreateShell.cshtml\", vm)");
        source.Should().Contain("return View(\"AddPayment\", vm);");
        source.Should().Contain("private IActionResult RenderShipmentEditor(ShipmentCreateVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/_ShipmentCreateShell.cshtml\", vm)");
        source.Should().Contain("return View(\"AddShipment\", vm);");
        source.Should().Contain("private IActionResult RenderRefundEditor(RefundCreateVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/_RefundCreateShell.cshtml\", vm)");
        source.Should().Contain("return View(\"AddRefund\", vm);");
        source.Should().Contain("private IActionResult RenderInvoiceCreateEditor(OrderInvoiceCreateVm vm)");
        source.Should().Contain("PartialView(\"~/Views/Orders/_InvoiceCreateShell.cshtml\", vm)");
        source.Should().Contain("return View(\"CreateInvoice\", vm);");
    }


    [Fact]
    public void OrdersController_Should_KeepFilterPopulationAndRedirectHelpersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Orders", "OrdersController.cs"));

        source.Should().Contain("private IEnumerable<SelectListItem> BuildOrderFilterItems(OrderQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"AllOrders\"), OrderQueueFilter.All.ToString()");
        source.Should().Contain("new SelectListItem(T(\"Open\"), OrderQueueFilter.Open.ToString()");
        source.Should().Contain("new SelectListItem(T(\"PaymentIssues\"), OrderQueueFilter.PaymentIssues.ToString()");
        source.Should().Contain("new SelectListItem(T(\"FulfillmentAttention\"), OrderQueueFilter.FulfillmentAttention.ToString()");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildPaymentFilterItems(PaymentQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"AllPayments\"), PaymentQueueFilter.All.ToString()");
        source.Should().Contain("new SelectListItem(T(\"Failed\"), PaymentQueueFilter.Failed.ToString()");
        source.Should().Contain("new SelectListItem(T(\"Refunded\"), PaymentQueueFilter.Refunded.ToString()");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildShipmentFilterItems(ShipmentQueueFilter selectedFilter)");
        source.Should().Contain("T(\"AllShipments\")");
        source.Should().Contain("T(\"PendingPacked\")");
        source.Should().Contain("T(\"MissingTracking\")");
        source.Should().Contain("T(\"TrackingOverdue\")");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildRefundFilterItems(RefundQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"AllRefunds\"), RefundQueueFilter.All.ToString()");
        source.Should().Contain("new SelectListItem(T(\"Pending\"), RefundQueueFilter.Pending.ToString()");
        source.Should().Contain("new SelectListItem(T(\"Completed\"), RefundQueueFilter.Completed.ToString()");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildReturnFilterItems(ReturnQueueFilter selectedFilter)");
        source.Should().Contain("T(\"AllReturnCases\")");
        source.Should().Contain("T(\"ReturnFollowUp\")");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildInvoiceFilterItems(InvoiceQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"AllInvoices\"), InvoiceQueueFilter.All.ToString()");
        source.Should().Contain("new SelectListItem(T(\"Outstanding\"), InvoiceQueueFilter.Outstanding.ToString()");
        source.Should().Contain("new SelectListItem(T(\"Paid\"), InvoiceQueueFilter.Paid.ToString()");
        source.Should().Contain("private static IEnumerable<SelectListItem> BuildPageSizeItems(int selectedPageSize)");
        source.Should().Contain("var sizes = new[] { 10, 20, 50, 100 }");
        source.Should().Contain("private void SetOrderHeader(OrderHeaderVm? header)");
        source.Should().Contain("ViewData[\"OrderHeader\"] = header;");
        source.Should().Contain("private async Task<OrderHeaderVm?> GetOrderHeaderAsync(Guid orderId, CancellationToken ct)");
        source.Should().Contain("return dto is null ? null : CreateHeader(dto);");
        source.Should().Contain("private async Task PopulateRefundOptionsAsync(RefundCreateVm vm, CancellationToken ct)");
        source.Should().Contain("vm.PaymentOptions = new List<SelectListItem>();");
        source.Should().Contain("vm.Currency = string.IsNullOrWhiteSpace(vm.Currency) ? dto.Currency : vm.Currency;");
        source.Should().Contain("Text = $\"{x.Provider} | {x.Currency} {(x.AmountMinor / 100.0M):0.00} | {x.Status}\"");
        source.Should().Contain("private async Task PopulateOrderInvoiceOptionsAsync(OrderInvoiceCreateVm vm, CancellationToken ct)");
        source.Should().Contain("var businessOptions = await _getBusinessLookup.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("var customerOptions = await _getCustomerLookup.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("await PopulateOrderInvoiceOptionsAsync(vm, businessOptions, customerOptions, orderDto).ConfigureAwait(false);");
        source.Should().Contain("Text = \"No business scope\"");
        source.Should().Contain("Text = \"Auto-resolve from user\"");
        source.Should().Contain("vm.PaymentOptions = (orderDto?.Payments ?? new List<PaymentDetailDto>()).Select(x => new SelectListItem");
        source.Should().Contain("Text = \"No linked payment\"");
        source.Should().Contain("DueAtUtc = DateTime.UtcNow.AddDays(14)");
        source.Should().Contain("private IActionResult RedirectOrHtmxDetails(Guid orderId)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(nameof(Details), new { id = orderId }) ?? string.Empty;");
        source.Should().Contain("return RedirectToAction(nameof(Details), new { id = orderId });");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("return RedirectToAction(actionName, routeValues);");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("Request.Headers[\"HX-Request\"]");
        source.Should().Contain("StringComparison.OrdinalIgnoreCase");
    }


    [Fact]
    public void OrderDetailAndFinancialGrids_Should_KeepResourceBackedStatusContractsWired()
    {
        var detailsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "Details.cshtml"));
        var invoicesSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_InvoicesGrid.cshtml"));
        var refundsSource = ReadWebAdminFile(Path.Combine("Views", "Orders", "_RefundsGrid.cshtml"));

        detailsSource.Should().Contain("string LocalizeOrderStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        detailsSource.Should().Contain("@LocalizeOrderStatus(Model.Status)");

        invoicesSource.Should().Contain("string LocalizeInvoiceStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        invoicesSource.Should().Contain("string LocalizeCustomerTaxProfileType(object? taxProfileType) => taxProfileType is null ? \"-\" : T.T(taxProfileType.ToString() ?? string.Empty);");
        invoicesSource.Should().Contain("@LocalizeInvoiceStatus(item.PaymentStatus)");
        invoicesSource.Should().Contain("@LocalizeInvoiceStatus(item.Status)");
        invoicesSource.Should().Contain("@LocalizeCustomerTaxProfileType(item.CustomerTaxProfileType)");

        refundsSource.Should().Contain("string LocalizeRefundStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        refundsSource.Should().Contain("@LocalizeRefundStatus(item.PaymentStatus)");
        refundsSource.Should().Contain("@LocalizeRefundStatus(item.Status)");
    }


    [Fact]
    public void LowerTrafficBillingCrmInventoryAndSubscriptionViews_Should_KeepResourceBackedStatusContractsWired()
    {
        var subscriptionInvoicesSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "SubscriptionInvoices.cshtml"));
        var crmLeadsSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "Leads.cshtml"));
        var crmInvoicesSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "Invoices.cshtml"));
        var crmLeadEditorSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "_LeadEditorShell.cshtml"));
        var crmInvoiceEditorSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "_InvoiceEditorShell.cshtml"));
        var paymentsSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Payments.cshtml"));
        var billingRefundsSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Refunds.cshtml"));
        var taxComplianceSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "TaxCompliance.cshtml"));
        var webhooksSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "Webhooks.cshtml"));
        var paymentFormSource = ReadWebAdminFile(Path.Combine("Views", "Billing", "_PaymentForm.cshtml"));
        var purchaseOrdersSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "PurchaseOrders.cshtml"));
        var stockTransfersSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "StockTransfers.cshtml"));

        subscriptionInvoicesSource.Should().Contain("string LocalizeSubscriptionInvoiceStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        subscriptionInvoicesSource.Should().Contain("@LocalizeSubscriptionInvoiceStatus(item.Status)");

        crmLeadsSource.Should().Contain("string LocalizeLeadStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        crmLeadsSource.Should().Contain("@LocalizeLeadStatus(item.Status)");
        crmInvoicesSource.Should().Contain("string LocalizeCrmInvoiceStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        crmInvoicesSource.Should().Contain("@LocalizeCrmInvoiceStatus(item.Status)");
        crmLeadEditorSource.Should().Contain("string LocalizeLeadStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        crmLeadEditorSource.Should().Contain("@LocalizeLeadStatus(Model.Status)");
        crmInvoiceEditorSource.Should().Contain("string LocalizeCrmInvoiceStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        crmInvoiceEditorSource.Should().Contain("@LocalizeCrmInvoiceStatus(Model.Status)");

        paymentsSource.Should().Contain("string LocalizePaymentStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        paymentsSource.Should().Contain("@LocalizePaymentStatus(item.Status)");
        paymentsSource.Should().Contain("string LocalizeInvoiceStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        paymentsSource.Should().Contain("string LocalizeProviderReferenceState(string? state) => state switch");
        paymentsSource.Should().Contain("@LocalizeProviderReferenceState(item.ProviderReferenceState)");
        paymentsSource.Should().Contain("@LocalizeInvoiceStatus(item.InvoiceStatus)");
        billingRefundsSource.Should().Contain("string LocalizeRefundStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        billingRefundsSource.Should().Contain("@LocalizeRefundStatus(item.Status)");
        billingRefundsSource.Should().Contain("string LocalizePaymentStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        billingRefundsSource.Should().Contain("string LocalizeProviderReferenceState(string? state) => state switch");
        billingRefundsSource.Should().Contain("@LocalizeProviderReferenceState(item.ProviderReferenceState)");
        billingRefundsSource.Should().Contain("@LocalizePaymentStatus(item.PaymentStatus)");
        taxComplianceSource.Should().Contain("string LocalizeTaxInvoiceStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        taxComplianceSource.Should().Contain("@LocalizeTaxInvoiceStatus(item.Status)");
        webhooksSource.Should().Contain("string LocalizeWebhookStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        webhooksSource.Should().Contain("@LocalizeWebhookStatus(item.Status)");
        paymentFormSource.Should().Contain("string LocalizePaymentStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        paymentFormSource.Should().Contain("@LocalizePaymentStatus(Model.Status)");
        paymentFormSource.Should().Contain("string LocalizeInvoiceStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        paymentFormSource.Should().Contain("string LocalizeRefundStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        paymentFormSource.Should().Contain("string LocalizeProviderReferenceState(string? state) => state switch");
        paymentFormSource.Should().Contain("@LocalizeInvoiceStatus(Model.InvoiceStatus)");
        paymentFormSource.Should().Contain("@LocalizeProviderReferenceState(Model.ProviderReferenceState)");
        paymentFormSource.Should().Contain("@LocalizeRefundStatus(refund.Status)");

        purchaseOrdersSource.Should().Contain("string LocalizePurchaseOrderStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        purchaseOrdersSource.Should().Contain("@LocalizePurchaseOrderStatus(item.Status)");
        stockTransfersSource.Should().Contain("string LocalizeStockTransferStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        stockTransfersSource.Should().Contain("@LocalizeStockTransferStatus(item.Status)");
    }


    [Fact]
    public void BusinessCommunicationViews_Should_KeepResourceBackedFallbackLabelsWired()
    {
        var indexSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Index.cshtml"));
        var detailsSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "Details.cshtml"));
        var emailAuditsSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "EmailAudits.cshtml"));
        var channelAuditsSource = ReadWebAdminFile(Path.Combine("Views", "BusinessCommunications", "ChannelAudits.cshtml"));

        indexSource.Should().Contain("_ => string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        indexSource.Should().Contain("_ => string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        indexSource.Should().Contain("_ => string.IsNullOrWhiteSpace(severity) ? T.T(\"CommonUnclassified\") : T.T(severity)");
        indexSource.Should().Contain("_ => string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");

        detailsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        detailsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(operationalStatus) ? T.T(\"CommonUnclassified\") : T.T(operationalStatus)");
        detailsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        detailsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");

        emailAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        emailAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(retryPolicyState) ? T.T(\"CommonUnclassified\") : T.T(retryPolicyState)");
        emailAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        emailAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : T.T(statusMix)");

        channelAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        channelAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(pressureState) ? T.T(\"CommonUnclassified\") : T.T(pressureState)");
        channelAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(recoveryState) ? T.T(\"CommonUnclassified\") : T.T(recoveryState)");
        channelAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(statusMix) ? T.T(\"CommonUnclassified\") : T.T(statusMix)");
        channelAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(status) ? T.T(\"CommonUnclassified\") : T.T(status)");
        channelAuditsSource.Should().Contain("_ => string.IsNullOrWhiteSpace(channel) ? T.T(\"CommonUnclassified\") : T.T(channel)");
    }

    [Fact]
    public void BusinessSupportFailedEmailsFragment_Should_KeepResourceBackedFlowFallbackLabelsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueFailedEmails.cshtml"));

        source.Should().Contain("string FlowLabel(string? flowKey) => flowKey switch");
        source.Should().Contain("\"BusinessInvitation\" => T.T(\"CommunicationDetailsActiveFlowInvitation\")");
        source.Should().Contain("\"AccountActivation\" => T.T(\"CommunicationDetailsActiveFlowActivation\")");
        source.Should().Contain("\"PasswordReset\" => T.T(\"CommunicationTemplateInventoryPasswordResetFlow\")");
        source.Should().Contain("\"AdminCommunicationTest\" => T.T(\"CommunicationTemplateInventoryAdminTestFlow\")");
        source.Should().Contain("\"PhoneVerification\" => T.T(\"CommunicationTemplateInventoryPhoneVerificationFlow\")");
        source.Should().Contain("_ => string.IsNullOrWhiteSpace(flowKey) ? T.T(\"CommonUnclassified\") : T.T(flowKey)");
        source.Should().Contain("@FlowLabel(item.FlowKey)");
    }


    [Fact]
    public void InventoryController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateWarehouse(WarehouseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditWarehouse(WarehouseEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateSupplier(SupplierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditSupplier(SupplierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> AdjustStock(InventoryAdjustActionVm vm");
        source.Should().Contain("public async Task<IActionResult> ReserveStock(InventoryReserveActionVm vm");
        source.Should().Contain("public async Task<IActionResult> ReleaseReservation(InventoryReleaseReservationActionVm vm");
        source.Should().Contain("public async Task<IActionResult> ReturnReceipt(InventoryReturnReceiptActionVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateStockLevel(StockLevelEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditStockLevel(StockLevelEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateStockTransfer(StockTransferEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditStockTransfer(StockTransferEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditPurchaseOrder(PurchaseOrderEditVm vm");
    }


    [Fact]
    public void InventoryController_Should_KeepAdminWorkspacesAndEditorGetsReachable()
    {
        var inventorySource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        inventorySource.Should().Contain("public sealed class InventoryController : AdminBaseController");
        inventorySource.Should().Contain("public IActionResult Index() => RedirectOrHtmx(nameof(Warehouses), new { });");
        inventorySource.Should().Contain("public async Task<IActionResult> Warehouses(");
        inventorySource.Should().Contain("public async Task<IActionResult> Suppliers(");
        inventorySource.Should().Contain("public async Task<IActionResult> StockLevels(");
        inventorySource.Should().Contain("public async Task<IActionResult> StockTransfers(");
        inventorySource.Should().Contain("public async Task<IActionResult> PurchaseOrders(");
        inventorySource.Should().Contain("public async Task<IActionResult> VariantLedger(");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateWarehouse(Guid? businessId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditWarehouse(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateSupplier(Guid? businessId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditSupplier(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> AdjustStock(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> ReserveStock(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> ReleaseReservation(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> ReturnReceipt(Guid stockLevelId");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateStockLevel(Guid? businessId = null, Guid? warehouseId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditStockLevel(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> CreateStockTransfer(Guid? businessId = null, Guid? warehouseId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditStockTransfer(Guid id");
        inventorySource.Should().Contain("public async Task<IActionResult> CreatePurchaseOrder(Guid? businessId = null");
        inventorySource.Should().Contain("public async Task<IActionResult> EditPurchaseOrder(Guid id");
    }


    [Fact]
    public void InventoryController_Should_KeepWarehouseAndSupplierWorkspaceEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));

        source.Should().Contain("public async Task<IActionResult> Warehouses(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, WarehouseQueueFilter filter = WarehouseQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("var result = await _getWarehousesPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getWarehousesPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildWarehouseFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildWarehousePlaybooks(),");
        source.Should().Contain("return RenderWarehousesWorkspace(vm);");
        source.Should().Contain("public async Task<IActionResult> CreateWarehouse(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("var vm = new WarehouseEditVm { BusinessId = businessId ?? Guid.Empty };");
        source.Should().Contain("return RenderWarehouseEditor(vm, isCreate: true);");
        source.Should().Contain("await _createWarehouse.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("SetSuccessMessage(\"WarehouseCreatedMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditWarehouse), new { id });");
        source.Should().Contain("var dto = await _getWarehouseForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"WarehouseNotFoundMessage\");");
        source.Should().Contain("await _updateWarehouse.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"WarehouseConcurrencyMessage\");");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildWarehouseFilterItems(WarehouseQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"AllWarehouses\"), WarehouseQueueFilter.All.ToString(), selectedFilter == WarehouseQueueFilter.All)");
        source.Should().Contain("new SelectListItem(T(\"Default\"), WarehouseQueueFilter.Default.ToString(), selectedFilter == WarehouseQueueFilter.Default)");
        source.Should().Contain("new SelectListItem(T(\"NoStockLevels\"), WarehouseQueueFilter.NoStockLevels.ToString(), selectedFilter == WarehouseQueueFilter.NoStockLevels)");
        source.Should().Contain("private List<InventoryOpsPlaybookVm> BuildWarehousePlaybooks()");
        source.Should().Contain("Title = T(\"WarehousePlaybookDefaultTitle\")");
        source.Should().Contain("Title = T(\"WarehousePlaybookEmptyTitle\")");
        source.Should().Contain("private IActionResult RenderWarehousesWorkspace(WarehousesListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/Warehouses.cshtml\", vm);");
        source.Should().Contain("return View(\"Warehouses\", vm);");
        source.Should().Contain("private IActionResult RenderWarehouseEditor(WarehouseEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_WarehouseEditorShell.cshtml\", vm);");
        source.Should().Contain("return isCreate ? View(\"CreateWarehouse\", vm) : View(\"EditWarehouse\", vm);");

        source.Should().Contain("public async Task<IActionResult> Suppliers(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, SupplierQueueFilter filter = SupplierQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getSuppliersPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getSuppliersPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildSupplierFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildSupplierPlaybooks(),");
        source.Should().Contain("return RenderSuppliersWorkspace(vm);");
        source.Should().Contain("public async Task<IActionResult> CreateSupplier(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("var vm = new SupplierEditVm { BusinessId = businessId ?? Guid.Empty };");
        source.Should().Contain("return RenderSupplierEditor(vm, isCreate: true);");
        source.Should().Contain("await _createSupplier.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("SetSuccessMessage(\"SupplierCreatedMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditSupplier), new { id });");
        source.Should().Contain("var dto = await _getSupplierForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"SupplierNotFoundMessage\");");
        source.Should().Contain("await _updateSupplier.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"SupplierConcurrencyMessage\");");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildSupplierFilterItems(SupplierQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"AllSuppliers\"), SupplierQueueFilter.All.ToString(), selectedFilter == SupplierQueueFilter.All)");
        source.Should().Contain("new SelectListItem(T(\"MissingAddress\"), SupplierQueueFilter.MissingAddress.ToString(), selectedFilter == SupplierQueueFilter.MissingAddress)");
        source.Should().Contain("new SelectListItem(T(\"HasPurchaseOrders\"), SupplierQueueFilter.HasPurchaseOrders.ToString(), selectedFilter == SupplierQueueFilter.HasPurchaseOrders)");
        source.Should().Contain("private List<InventoryOpsPlaybookVm> BuildSupplierPlaybooks()");
        source.Should().Contain("Title = T(\"SupplierPlaybookContactHygieneTitle\")");
        source.Should().Contain("Title = T(\"SupplierPlaybookActiveReviewTitle\")");
        source.Should().Contain("private IActionResult RenderSuppliersWorkspace(SuppliersListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/Suppliers.cshtml\", vm);");
        source.Should().Contain("return View(\"Suppliers\", vm);");
        source.Should().Contain("private IActionResult RenderSupplierEditor(SupplierEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_SupplierEditorShell.cshtml\", vm);");
        source.Should().Contain("return isCreate ? View(\"CreateSupplier\", vm) : View(\"EditSupplier\", vm);");
    }


    [Fact]
    public void InventoryController_Should_KeepStockProcurementAndSharedHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));

        source.Should().Contain("public async Task<IActionResult> StockLevels(Guid? warehouseId = null, Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, StockLevelQueueFilter filter = StockLevelQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("warehouseId = await _referenceData.ResolveWarehouseIdAsync(warehouseId, businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("var result = await _getStockLevelsPage.HandleAsync(warehouseId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildStockLevelFilterItems(filter),");
        source.Should().Contain("WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(warehouseId, businessId, ct).ConfigureAwait(false),");
        source.Should().Contain("ViewBag.BusinessId = businessId;");
        source.Should().Contain("return RenderStockLevelsWorkspace(vm);");
        source.Should().Contain("await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("await _adjustInventory.HandleAsync(new InventoryAdjustDto");
        source.Should().Contain("await _reserveInventory.HandleAsync(new InventoryReserveDto");
        source.Should().Contain("await _releaseInventoryReservation.HandleAsync(new InventoryReleaseReservationDto");
        source.Should().Contain("await _processReturnReceipt.HandleAsync(new InventoryReturnReceiptDto");
        source.Should().Contain("return RedirectOrHtmx(nameof(StockLevels), new { businessId = vm.BusinessId, warehouseId = vm.WarehouseId, filter = StockLevelQueueFilter.Reserved });");
        source.Should().Contain("public async Task<IActionResult> CreateStockLevel(Guid? businessId = null, Guid? warehouseId = null, CancellationToken ct = default)");
        source.Should().Contain("await PopulateStockLevelOptionsAsync(vm, businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("await _createStockLevel.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updateStockLevel.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("private IActionResult RenderStockLevelsWorkspace(StockLevelsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/StockLevels.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderStockLevelEditor(StockLevelEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_StockLevelEditorShell.cshtml\", vm);");

        source.Should().Contain("public async Task<IActionResult> StockTransfers(Guid? warehouseId = null, Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, StockTransferQueueFilter filter = StockTransferQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getStockTransfersPage.HandleAsync(warehouseId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildStockTransferFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildStockTransferPlaybooks(),");
        source.Should().Contain("return RenderStockTransfersWorkspace(vm);");
        source.Should().Contain("await PopulateStockTransferOptionsAsync(vm, businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("await _createStockTransfer.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updateStockTransfer.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("private IActionResult RenderStockTransfersWorkspace(StockTransfersListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/StockTransfers.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderStockTransferEditor(StockTransferEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_StockTransferEditorShell.cshtml\", vm);");

        source.Should().Contain("public async Task<IActionResult> PurchaseOrders(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, PurchaseOrderQueueFilter filter = PurchaseOrderQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getPurchaseOrdersPage.HandleAsync(businessId.Value, page, pageSize, q, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getPurchaseOrdersPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildPurchaseOrderFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildPurchaseOrderPlaybooks(),");
        source.Should().Contain("return RenderPurchaseOrdersWorkspace(vm);");
        source.Should().Contain("await PopulatePurchaseOrderOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("await _createPurchaseOrder.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("await _updatePurchaseOrder.HandleAsync(dto, ct).ConfigureAwait(false);");
        source.Should().Contain("private IActionResult RenderPurchaseOrdersWorkspace(PurchaseOrdersListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/PurchaseOrders.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderPurchaseOrderEditor(PurchaseOrderEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_PurchaseOrderEditorShell.cshtml\", vm);");

        source.Should().Contain("private async Task PopulateStockLevelOptionsAsync(StockLevelEditVm vm, Guid? businessId, CancellationToken ct)");
        source.Should().Contain("vm.WarehouseOptions = await _referenceData.GetWarehouseOptionsAsync(vm.WarehouseId, resolvedBusinessId, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(vm.ProductVariantId, ct).ConfigureAwait(false);");
        source.Should().Contain("private async Task PopulateInventoryStockActionOptionsAsync(InventoryStockActionVm vm, CancellationToken ct)");
        source.Should().Contain("private async Task PopulateStockTransferOptionsAsync(StockTransferEditVm vm, Guid? businessId, CancellationToken ct)");
        source.Should().Contain("private async Task PopulatePurchaseOrderOptionsAsync(PurchaseOrderEditVm vm, CancellationToken ct)");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildStockLevelFilterItems(StockLevelQueueFilter selectedFilter)");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildPurchaseOrderFilterItems(PurchaseOrderQueueFilter selectedFilter)");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildStockTransferFilterItems(StockTransferQueueFilter selectedFilter)");
        source.Should().Contain("private List<InventoryOpsPlaybookVm> BuildStockTransferPlaybooks()");
        source.Should().Contain("private List<InventoryOpsPlaybookVm> BuildPurchaseOrderPlaybooks()");
        source.Should().Contain("Title = T(\"InventoryTransfersPlaybookDraftTitle\")");
        source.Should().Contain("Title = T(\"InventoryTransfersPlaybookInTransitTitle\")");
        source.Should().Contain("Title = T(\"InventoryPurchaseOrdersPlaybookDraftTitle\")");
        source.Should().Contain("Title = T(\"InventoryPurchaseOrdersPlaybookIssuedTitle\")");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
    }


    [Fact]
    public void InventoryController_Should_KeepVariantLedgerWorkspaceAndBuilderContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));

        source.Should().Contain("public async Task<IActionResult> VariantLedger(Guid variantId, Guid? warehouseId = null, int page = 1, int pageSize = 20, InventoryLedgerQueueFilter filter = InventoryLedgerQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getLedger.HandleAsync(variantId, page, pageSize, warehouseId, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var summary = await _getLedger.GetSummaryAsync(variantId, warehouseId, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildInventoryLedgerFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildInventoryLedgerPlaybooks(),");
        source.Should().Contain("Items = dto.Items.Select(x => new InventoryLedgerItemVm");
        source.Should().Contain("return RenderVariantLedgerWorkspace(vm);");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildInventoryLedgerFilterItems(InventoryLedgerQueueFilter selectedFilter)");
        source.Should().Contain("yield return new SelectListItem(T(\"AllLedgerEntries\"), InventoryLedgerQueueFilter.All.ToString(), selectedFilter == InventoryLedgerQueueFilter.All);");
        source.Should().Contain("yield return new SelectListItem(T(\"Inbound\"), InventoryLedgerQueueFilter.Inbound.ToString(), selectedFilter == InventoryLedgerQueueFilter.Inbound);");
        source.Should().Contain("yield return new SelectListItem(T(\"Outbound\"), InventoryLedgerQueueFilter.Outbound.ToString(), selectedFilter == InventoryLedgerQueueFilter.Outbound);");
        source.Should().Contain("yield return new SelectListItem(T(\"Reservations\"), InventoryLedgerQueueFilter.Reservations.ToString(), selectedFilter == InventoryLedgerQueueFilter.Reservations);");
        source.Should().Contain("private List<InventoryOpsPlaybookVm> BuildInventoryLedgerPlaybooks()");
        source.Should().Contain("Title = T(\"InventoryLedgerPlaybookInboundTitle\")");
        source.Should().Contain("Title = T(\"InventoryLedgerPlaybookOutboundTitle\")");
        source.Should().Contain("private IActionResult RenderVariantLedgerWorkspace(InventoryLedgerListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/VariantLedger.cshtml\", vm);");
        source.Should().Contain("return View(\"VariantLedger\", vm);");
    }


    [Fact]
    public void InventoryController_Should_KeepStockActionVmBuilderContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Inventory", "InventoryController.cs"));

        source.Should().Contain("private async Task<InventoryAdjustActionVm?> BuildInventoryAdjustActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)");
        source.Should().Contain("var dto = await _getStockLevelForEdit.HandleAsync(stockLevelId, ct).ConfigureAwait(false);");
        source.Should().Contain("if (dto is null)");
        source.Should().Contain("return null;");
        source.Should().Contain("var vm = new InventoryAdjustActionVm");
        source.Should().Contain("StockLevelId = dto.Id,");
        source.Should().Contain("BusinessId = businessId,");
        source.Should().Contain("WarehouseId = dto.WarehouseId,");
        source.Should().Contain("ProductVariantId = dto.ProductVariantId,");
        source.Should().Contain("AvailableQuantity = dto.AvailableQuantity,");
        source.Should().Contain("ReservedQuantity = dto.ReservedQuantity");
        source.Should().Contain("private async Task<InventoryReserveActionVm?> BuildInventoryReserveActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)");
        source.Should().Contain("var vm = new InventoryReserveActionVm");
        source.Should().Contain("private async Task<InventoryReleaseReservationActionVm?> BuildInventoryReleaseActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)");
        source.Should().Contain("var vm = new InventoryReleaseReservationActionVm");
        source.Should().Contain("Quantity = dto.ReservedQuantity > 0 ? dto.ReservedQuantity : 1");
        source.Should().Contain("private async Task<InventoryReturnReceiptActionVm?> BuildInventoryReturnReceiptActionVmAsync(Guid stockLevelId, Guid? businessId, CancellationToken ct)");
        source.Should().Contain("var vm = new InventoryReturnReceiptActionVm");
        source.Should().Contain("await PopulateInventoryStockActionOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("return vm;");
        source.Should().Contain("private IActionResult RenderAdjustStockEditor(InventoryAdjustActionVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_AdjustStockEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderReserveStockEditor(InventoryReserveActionVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_ReserveStockEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderReleaseReservationEditor(InventoryReleaseReservationActionVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_ReleaseReservationEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderReturnReceiptEditor(InventoryReturnReceiptActionVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Inventory/_ReturnReceiptEditorShell.cshtml\", vm);");
    }


    [Fact]
    public void CrmController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateCustomer(CustomerEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditCustomer(CustomerEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditInvoice(InvoiceEditVm vm");
        source.Should().Contain("public async Task<IActionResult> TransitionInvoiceStatus(InvoiceStatusTransitionVm vm");
        source.Should().Contain("public async Task<IActionResult> RefundInvoice(InvoiceRefundCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateLead(LeadEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditLead(LeadEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateOpportunity(OpportunityEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditOpportunity(OpportunityEditVm vm");
        source.Should().Contain("public async Task<IActionResult> ConvertLead(ConvertLeadVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateSegment(CustomerSegmentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditSegment(CustomerSegmentEditVm vm");
        source.Should().Contain("public async Task<IActionResult> CustomerInteractions(InteractionCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> LeadInteractions(InteractionCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> OpportunityInteractions(InteractionCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CustomerConsents(ConsentCreateVm vm");
        source.Should().Contain("public async Task<IActionResult> CustomerSegmentMemberships(AssignCustomerSegmentVm vm");
        source.Should().Contain("public async Task<IActionResult> RemoveCustomerSegmentMembership(");
    }


    [Fact]
    public void CrmController_Should_KeepAdminWorkspacesAndReviewGetsReachable()
    {
        var crmSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        crmSource.Should().Contain("public sealed class CrmController : AdminBaseController");
        crmSource.Should().Contain("public async Task<IActionResult> Index(");
        crmSource.Should().Contain("public async Task<IActionResult> Customers(");
        crmSource.Should().Contain("public async Task<IActionResult> Invoices(");
        crmSource.Should().Contain("public async Task<IActionResult> Leads(");
        crmSource.Should().Contain("public async Task<IActionResult> Opportunities(");
        crmSource.Should().Contain("public async Task<IActionResult> Segments(");
        crmSource.Should().Contain("public async Task<IActionResult> CreateCustomer(CancellationToken ct = default)");
        crmSource.Should().Contain("public async Task<IActionResult> EditCustomer(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> EditInvoice(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> CreateLead(CancellationToken ct = default)");
        crmSource.Should().Contain("public async Task<IActionResult> EditLead(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> CreateOpportunity(Guid? customerId = null");
        crmSource.Should().Contain("public async Task<IActionResult> EditOpportunity(Guid id");
        crmSource.Should().Contain("public IActionResult CreateSegment() => RenderSegmentEditor(new CustomerSegmentEditVm(), nameof(CreateSegment));");
        crmSource.Should().Contain("public async Task<IActionResult> EditSegment(Guid id");
        crmSource.Should().Contain("public async Task<IActionResult> CustomerInteractions(Guid customerId");
        crmSource.Should().Contain("public async Task<IActionResult> LeadInteractions(Guid leadId");
        crmSource.Should().Contain("public async Task<IActionResult> OpportunityInteractions(Guid opportunityId");
        crmSource.Should().Contain("public async Task<IActionResult> CustomerConsents(Guid customerId");
        crmSource.Should().Contain("public async Task<IActionResult> CustomerSegmentMemberships(Guid customerId");
    }


    [Fact]
    public void CrmController_Should_KeepInvoiceEditorEntryAndRefundWorkflowWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("public async Task<IActionResult> EditInvoice(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getInvoiceForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("if (dto is null)");
        source.Should().Contain("SetErrorMessage(\"InvoiceNotFoundMessage\")");
        source.Should().Contain("return RedirectOrHtmx(nameof(Invoices), new { });");
        source.Should().Contain("var vm = new InvoiceEditVm");
        source.Should().Contain("Refund = new InvoiceRefundCreateVm");
        source.Should().Contain("var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("TaxPolicy = MapTaxPolicy(settings)");
        source.Should().Contain("await PopulateInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderInvoiceEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> RefundInvoice(InvoiceRefundCreateVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _createInvoiceRefund.HandleAsync(new InvoiceRefundCreateDto");
        source.Should().Contain("InvoiceId = vm.InvoiceId,");
        source.Should().Contain("AmountMinor = vm.AmountMinor,");
        source.Should().Contain("Currency = vm.Currency,");
        source.Should().Contain("Reason = vm.Reason");
        source.Should().Contain("SetSuccessMessage(\"InvoiceRefundRecordedMessage\")");
        source.Should().Contain("SetErrorMessage(\"InvoiceConcurrencyMessage\")");
        source.Should().Contain("SetLocalizedError(\"InvoiceRefundRecordFailedMessage\", ex)");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.InvoiceId });");
    }


    [Fact]
    public void CrmController_Should_KeepInvoiceWorkspaceAndRenderHelpersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("public async Task<IActionResult> Invoices(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)");
        source.Should().Contain("var (items, total) = await _getInvoicesPage.HandleAsync(page, pageSize, q, ct).ConfigureAwait(false);");
        source.Should().Contain("OpsSummary = BuildInvoiceOpsSummary(invoiceItems),");
        source.Should().Contain("Playbooks = BuildInvoicePlaybooks(),");
        source.Should().Contain("return RenderInvoicesWorkspace(new InvoicesListVm");
        source.Should().Contain("private IActionResult RenderInvoicesWorkspace(InvoicesListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/Invoices.cshtml\", vm);");
        source.Should().Contain("return View(\"Invoices\", vm);");
        source.Should().Contain("private IActionResult RenderInvoiceEditor(InvoiceEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_InvoiceEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"EditInvoice\", vm);");
        source.Should().Contain("private InvoiceOpsSummaryVm BuildInvoiceOpsSummary(List<InvoiceListItemVm> items)");
        source.Should().Contain("DraftCount = items.Count(x => x.Status == Darwin.Domain.Enums.InvoiceStatus.Draft),");
        source.Should().Contain("DueSoonCount = items.Count(x => x.BalanceMinor > 0 && x.DueDateUtc.Date >= now && x.DueDateUtc.Date <= now.AddDays(7)),");
        source.Should().Contain("OverdueCount = items.Count(x => x.BalanceMinor > 0 && x.DueDateUtc.Date < now),");
        source.Should().Contain("MissingVatIdCount = items.Count(x => x.CustomerTaxProfileType == Darwin.Domain.Enums.CustomerTaxProfileType.Business && string.IsNullOrWhiteSpace(x.CustomerVatId)),");
        source.Should().Contain("RefundedCount = items.Count(x => x.RefundedAmountMinor > 0)");
        source.Should().Contain("private List<CrmPlaybookVm> BuildInvoicePlaybooks()");
        source.Should().Contain("Title = \"CrmInvoicesPlaybookDueSoonTitle\"");
        source.Should().Contain("Title = \"CrmInvoicesPlaybookVatGapTitle\"");
        source.Should().Contain("Title = \"CrmInvoicesPlaybookRefundTitle\"");
    }


    [Fact]
    public void CrmController_Should_KeepCustomerEditorEntryAndSubmitContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("public async Task<IActionResult> CreateCustomer(CancellationToken ct = default)");
        source.Should().Contain("var vm = new CustomerEditVm();");
        source.Should().Contain("vm.Addresses.Add(new CustomerAddressVm");
        source.Should().Contain("Country = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault,");
        source.Should().Contain("IsDefaultBilling = true,");
        source.Should().Contain("IsDefaultShipping = true");
        source.Should().Contain("await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderCustomerEditor(vm, nameof(CreateCustomer));");
        source.Should().Contain("public async Task<IActionResult> CreateCustomer(CustomerEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("EnsureCustomerAddressRows(vm);");
        source.Should().Contain("var id = await _createCustomer.HandleAsync(new CustomerCreateDto");
        source.Should().Contain("Addresses = vm.Addresses.Select(MapCustomerAddress).ToList()");
        source.Should().Contain("SetSuccessMessage(\"CustomerCreatedMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditCustomer), new { id });");
        source.Should().Contain("AddLocalizedModelError(\"CustomerCreateFailedMessage\", ex);");
        source.Should().Contain("public async Task<IActionResult> EditCustomer(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getCustomerForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"CustomerNotFoundMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Customers), new { });");
        source.Should().Contain("var vm = new CustomerEditVm");
        source.Should().Contain("EffectiveLocale = dto.EffectiveLocale,");
        source.Should().Contain("UsesPlatformLocaleFallback = dto.UsesPlatformLocaleFallback,");
        source.Should().Contain("NewInteraction = new InteractionCreateVm { CustomerId = dto.Id },");
        source.Should().Contain("NewConsent = new ConsentCreateVm { CustomerId = dto.Id, GrantedAtUtc = DateTime.UtcNow },");
        source.Should().Contain("SegmentAssignment = new AssignCustomerSegmentVm { CustomerId = dto.Id }");
        source.Should().Contain("return RenderCustomerEditor(vm, \"EditCustomer\");");
        source.Should().Contain("public async Task<IActionResult> EditCustomer(CustomerEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _updateCustomer.HandleAsync(new CustomerEditDto");
        source.Should().Contain("SetSuccessMessage(\"CustomerUpdatedMessage\");");
        source.Should().Contain("SetErrorMessage(\"CustomerConcurrencyMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"CustomerUpdateFailedMessage\", ex);");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditCustomer), new { id = vm.Id });");
    }


    [Fact]
    public void CrmController_Should_KeepCustomerFragmentsAndMembershipMutationsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("public async Task<IActionResult> CustomerInteractions(Guid customerId, int page = 1, int pageSize = 10, CancellationToken ct = default)");
        source.Should().Contain("var (items, total) = await _getCustomerInteractionsPage.HandleAsync(customerId, page, pageSize, ct).ConfigureAwait(false);");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_InteractionsSection.cshtml\", new InteractionsPageVm");
        source.Should().Contain("Scope = \"customer\",");
        source.Should().Contain("EntityId = customerId,");
        source.Should().Contain("Items = items.Select(MapInteraction).ToList()");
        source.Should().Contain("public async Task<IActionResult> CustomerInteractions(InteractionCreateVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _createInteraction.HandleAsync(MapInteraction(vm), ct).ConfigureAwait(false);");
        source.Should().Contain("SetSuccessMessage(\"InteractionAddedMessage\");");
        source.Should().Contain("SetLocalizedError(\"InteractionAddFailedMessage\", ex);");
        source.Should().Contain("return await CustomerInteractions(vm.CustomerId ?? Guid.Empty, ct: ct).ConfigureAwait(false);");
        source.Should().Contain("public async Task<IActionResult> CustomerConsents(Guid customerId, int page = 1, int pageSize = 10, CancellationToken ct = default)");
        source.Should().Contain("var (items, total) = await _getCustomerConsentsPage.HandleAsync(customerId, page, pageSize, ct).ConfigureAwait(false);");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_ConsentsSection.cshtml\", new ConsentsPageVm");
        source.Should().Contain("Items = items.Select(x => new ConsentListItemVm");
        source.Should().Contain("public async Task<IActionResult> CustomerConsents(ConsentCreateVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _createConsent.HandleAsync(new ConsentCreateDto");
        source.Should().Contain("SetSuccessMessage(\"ConsentAddedMessage\");");
        source.Should().Contain("SetLocalizedError(\"ConsentAddFailedMessage\", ex);");
        source.Should().Contain("return await CustomerConsents(vm.CustomerId, ct: ct).ConfigureAwait(false);");
        source.Should().Contain("public async Task<IActionResult> CustomerSegmentMemberships(Guid customerId, CancellationToken ct = default)");
        source.Should().Contain("var items = await _getCustomerSegmentMemberships.HandleAsync(customerId, ct).ConfigureAwait(false);");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_CustomerSegmentsSection.cshtml\", new CustomerMembershipsVm");
        source.Should().Contain("Items = items.Select(x => new CustomerSegmentMembershipVm");
        source.Should().Contain("public async Task<IActionResult> CustomerSegmentMemberships(AssignCustomerSegmentVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _assignCustomerSegment.HandleAsync(new AssignCustomerSegmentDto");
        source.Should().Contain("CustomerSegmentId = vm.CustomerSegmentId");
        source.Should().Contain("SetSuccessMessage(\"SegmentAssignedMessage\");");
        source.Should().Contain("SetLocalizedError(\"SegmentAssignFailedMessage\", ex);");
        source.Should().Contain("return await CustomerSegmentMemberships(vm.CustomerId, ct).ConfigureAwait(false);");
        source.Should().Contain("public async Task<IActionResult> RemoveCustomerSegmentMembership(Guid customerId, Guid membershipId, CancellationToken ct = default)");
        source.Should().Contain("await _removeCustomerSegmentMembership.HandleAsync(membershipId, ct).ConfigureAwait(false);");
        source.Should().Contain("SetSuccessMessage(\"SegmentRemovedMessage\");");
        source.Should().Contain("SetLocalizedError(\"SegmentRemoveFailedMessage\", ex);");
        source.Should().Contain("return await CustomerSegmentMemberships(customerId, ct).ConfigureAwait(false);");
    }


    [Fact]
    public void CrmController_Should_KeepLeadAndOpportunityEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("public async Task<IActionResult> CreateLead(CancellationToken ct = default)");
        source.Should().Contain("var vm = new LeadEditVm();");
        source.Should().Contain("await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderLeadEditor(vm, nameof(CreateLead));");
        source.Should().Contain("public async Task<IActionResult> CreateLead(LeadEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var id = await _createLead.HandleAsync(new LeadCreateDto");
        source.Should().Contain("AssignedToUserId = vm.AssignedToUserId,");
        source.Should().Contain("CustomerId = vm.CustomerId");
        source.Should().Contain("SetSuccessMessage(\"LeadCreatedMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"LeadCreateFailedMessage\", ex);");
        source.Should().Contain("public async Task<IActionResult> EditLead(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getLeadForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"LeadNotFoundMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Leads), new { });");
        source.Should().Contain("Conversion = new ConvertLeadVm");
        source.Should().Contain("CopyNotesToCustomer = true");
        source.Should().Contain("NewInteraction = new InteractionCreateVm { LeadId = dto.Id }");
        source.Should().Contain("return RenderLeadEditor(vm, nameof(EditLead));");
        source.Should().Contain("public async Task<IActionResult> EditLead(LeadEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _updateLead.HandleAsync(new LeadEditDto");
        source.Should().Contain("SetSuccessMessage(\"LeadUpdatedMessage\");");
        source.Should().Contain("SetErrorMessage(\"LeadConcurrencyMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"LeadUpdateFailedMessage\", ex);");
        source.Should().Contain("public async Task<IActionResult> CreateOpportunity(Guid? customerId = null, CancellationToken ct = default)");
        source.Should().Contain("CustomerId = customerId ?? Guid.Empty");
        source.Should().Contain("EnsureOpportunityLineRows(vm);");
        source.Should().Contain("await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderOpportunityEditor(vm, nameof(CreateOpportunity));");
        source.Should().Contain("public async Task<IActionResult> CreateOpportunity(OpportunityEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var id = await _createOpportunity.HandleAsync(new OpportunityCreateDto");
        source.Should().Contain("Items = vm.Items.Select(x => new OpportunityItemDto");
        source.Should().Contain("ProductVariantId = x.ProductVariantId,");
        source.Should().Contain("UnitPriceMinor = x.UnitPriceMinor");
        source.Should().Contain("SetSuccessMessage(\"OpportunityCreatedMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"OpportunityCreateFailedMessage\", ex);");
        source.Should().Contain("public async Task<IActionResult> EditOpportunity(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getOpportunityForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"OpportunityNotFoundMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Opportunities), new { });");
        source.Should().Contain("Currency = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCurrency,");
        source.Should().Contain("Items = dto.Items.Select(x => new OpportunityItemVm");
        source.Should().Contain("NewInteraction = new InteractionCreateVm { OpportunityId = dto.Id }");
        source.Should().Contain("return RenderOpportunityEditor(vm, nameof(EditOpportunity));");
        source.Should().Contain("public async Task<IActionResult> EditOpportunity(OpportunityEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _updateOpportunity.HandleAsync(new OpportunityEditDto");
        source.Should().Contain("SetSuccessMessage(\"OpportunityUpdatedMessage\");");
        source.Should().Contain("SetErrorMessage(\"OpportunityConcurrencyMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"OpportunityUpdateFailedMessage\", ex);");
    }


    [Fact]
    public void CrmController_Should_KeepLeadConversionAndSegmentEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("public async Task<IActionResult> ConvertLead(ConvertLeadVm vm, CancellationToken ct = default)");
        source.Should().Contain("var customerId = await _convertLeadToCustomer.HandleAsync(new ConvertLeadToCustomerDto");
        source.Should().Contain("LeadId = vm.LeadId,");
        source.Should().Contain("RowVersion = vm.RowVersion,");
        source.Should().Contain("UserId = vm.UserId,");
        source.Should().Contain("CopyNotesToCustomer = vm.CopyNotesToCustomer");
        source.Should().Contain("SetSuccessMessage(\"LeadConvertedMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditCustomer), new { id = customerId });");
        source.Should().Contain("SetErrorMessage(\"LeadConcurrencyMessage\");");
        source.Should().Contain("SetLocalizedError(\"LeadConvertFailedMessage\", ex);");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditLead), new { id = vm.LeadId });");
        source.Should().Contain("public IActionResult CreateSegment() => RenderSegmentEditor(new CustomerSegmentEditVm(), nameof(CreateSegment));");
        source.Should().Contain("public async Task<IActionResult> CreateSegment(CustomerSegmentEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var id = await _createCustomerSegment.HandleAsync(new CustomerSegmentEditDto");
        source.Should().Contain("Name = vm.Name,");
        source.Should().Contain("Description = vm.Description");
        source.Should().Contain("SetSuccessMessage(\"SegmentCreatedMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"SegmentCreateFailedMessage\", ex);");
        source.Should().Contain("public async Task<IActionResult> EditSegment(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getCustomerSegmentForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"SegmentNotFoundMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Segments), new { });");
        source.Should().Contain("return RenderSegmentEditor(new CustomerSegmentEditVm");
        source.Should().Contain("RowVersion = dto.RowVersion,");
        source.Should().Contain("public async Task<IActionResult> EditSegment(CustomerSegmentEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _updateCustomerSegment.HandleAsync(new CustomerSegmentEditDto");
        source.Should().Contain("SetSuccessMessage(\"SegmentUpdatedMessage\");");
        source.Should().Contain("SetErrorMessage(\"SegmentConcurrencyMessage\");");
        source.Should().Contain("AddLocalizedModelError(\"SegmentUpdateFailedMessage\", ex);");
        source.Should().Contain("return RedirectOrHtmx(nameof(EditSegment), new { id = vm.Id });");
    }


    [Fact]
    public void CrmController_Should_KeepLeadAndOpportunityWorkspaceCompositionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("public async Task<IActionResult> Leads(int page = 1, int pageSize = 20, string? q = null, LeadQueueFilter filter = LeadQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var (items, total) = await _getLeadsPage.HandleAsync(page, pageSize, q, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var leadItems = items.ToList();");
        source.Should().Contain("OpsSummary = BuildLeadOpsSummary(leadItems),");
        source.Should().Contain("Playbooks = BuildLeadPlaybooks(),");
        source.Should().Contain("FilterItems = BuildLeadFilterItems(filter),");
        source.Should().Contain("FullName = (x.FirstName + \" \" + x.LastName).Trim(),");
        source.Should().Contain("return RenderLeadsWorkspace(vm);");
        source.Should().Contain("public async Task<IActionResult> Opportunities(int page = 1, int pageSize = 20, string? q = null, OpportunityQueueFilter filter = OpportunityQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var (items, total) = await _getOpportunitiesPage.HandleAsync(page, pageSize, q, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var opportunityItems = items.ToList();");
        source.Should().Contain("OpsSummary = BuildOpportunityOpsSummary(opportunityItems),");
        source.Should().Contain("Playbooks = BuildOpportunityPlaybooks(),");
        source.Should().Contain("FilterItems = BuildOpportunityFilterItems(filter),");
        source.Should().Contain("Currency = settings.DefaultCurrency,");
        source.Should().Contain("ItemCount = x.ItemCount,");
        source.Should().Contain("return RenderOpportunitiesWorkspace(vm);");
        source.Should().Contain("private IActionResult RenderLeadsWorkspace(LeadsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/Leads.cshtml\", vm);");
        source.Should().Contain("return View(\"Leads\", vm);");
        source.Should().Contain("private IActionResult RenderOpportunitiesWorkspace(OpportunitiesListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/Opportunities.cshtml\", vm);");
        source.Should().Contain("return View(\"Opportunities\", vm);");
    }


    [Fact]
    public void CrmController_Should_KeepLeadOpportunityAndSegmentHelperBuildersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("private static LeadOpsSummaryVm BuildLeadOpsSummary(IReadOnlyCollection<LeadListItemDto> items)");
        source.Should().Contain("QualifiedCount = items.Count(x => x.Status == Darwin.Domain.Enums.LeadStatus.Qualified),");
        source.Should().Contain("UnassignedCount = items.Count(x => !x.AssignedToUserId.HasValue),");
        source.Should().Contain("UnconvertedCount = items.Count(x => !x.CustomerId.HasValue),");
        source.Should().Contain("private static List<CrmPlaybookVm> BuildLeadPlaybooks()");
        source.Should().Contain("Title = \"CrmLeadQualifiedPlaybookTitle\"");
        source.Should().Contain("Title = \"CrmLeadUnassignedPlaybookTitle\"");
        source.Should().Contain("Title = \"CrmLeadUnconvertedPlaybookTitle\"");
        source.Should().Contain("private static OpportunityOpsSummaryVm BuildOpportunityOpsSummary(IReadOnlyCollection<OpportunityListItemDto> items)");
        source.Should().Contain("var closingSoonThreshold = DateTime.UtcNow.Date.AddDays(14);");
        source.Should().Contain("ClosingSoonCount = items.Count(x => x.ExpectedCloseDateUtc.HasValue && x.ExpectedCloseDateUtc.Value.Date <= closingSoonThreshold),");
        source.Should().Contain("HighValueCount = items.Count(x => x.EstimatedValueMinor >= 100000),");
        source.Should().Contain("private static List<CrmPlaybookVm> BuildOpportunityPlaybooks()");
        source.Should().Contain("Title = \"CrmOpportunityClosingSoonPlaybookTitle\"");
        source.Should().Contain("Title = \"CrmOpportunityUnassignedPlaybookTitle\"");
        source.Should().Contain("Title = \"CrmOpportunityHighInteractionPlaybookTitle\"");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildLeadFilterItems(LeadQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"Qualified\"), LeadQueueFilter.Qualified.ToString(), selectedFilter == LeadQueueFilter.Qualified)");
        source.Should().Contain("new SelectListItem(T(\"Unconverted\"), LeadQueueFilter.Unconverted.ToString(), selectedFilter == LeadQueueFilter.Unconverted)");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildOpportunityFilterItems(OpportunityQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"ClosingSoon\"), OpportunityQueueFilter.ClosingSoon.ToString(), selectedFilter == OpportunityQueueFilter.ClosingSoon)");
        source.Should().Contain("new SelectListItem(T(\"HighValue\"), OpportunityQueueFilter.HighValue.ToString(), selectedFilter == OpportunityQueueFilter.HighValue)");
        source.Should().Contain("public async Task<IActionResult> Segments(int page = 1, int pageSize = 20, string? q = null, CustomerSegmentQueueFilter filter = CustomerSegmentQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var segmentSummary = await _getCustomerSegmentsPage.GetSummaryAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("SegmentSummary = new CustomerSegmentOpsSummaryVm");
        source.Should().Contain("Playbooks = BuildSegmentPlaybooks(),");
        source.Should().Contain("FilterItems = BuildSegmentFilterItems(filter),");
        source.Should().Contain("return RenderSegmentsWorkspace(vm);");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildSegmentFilterItems(CustomerSegmentQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"Empty\"), CustomerSegmentQueueFilter.Empty.ToString(), selectedFilter == CustomerSegmentQueueFilter.Empty)");
        source.Should().Contain("new SelectListItem(T(\"MissingDescription\"), CustomerSegmentQueueFilter.MissingDescription.ToString(), selectedFilter == CustomerSegmentQueueFilter.MissingDescription)");
        source.Should().Contain("private List<CrmPlaybookVm> BuildSegmentPlaybooks()");
        source.Should().Contain("Title = T(\"CrmSegmentsPlaybookEmptyTitle\")");
        source.Should().Contain("Title = T(\"CrmSegmentsPlaybookMissingDescriptionTitle\")");
        source.Should().Contain("private IActionResult RenderSegmentsWorkspace(CustomerSegmentsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/Segments.cshtml\", vm);");
        source.Should().Contain("return View(\"Segments\", vm);");
    }


    [Fact]
    public void CrmController_Should_KeepCrmOptionPopulationHelpersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("private async Task PopulateCustomerOptionsAsync(CustomerEditVm vm, CancellationToken ct)");
        source.Should().Contain("vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.NewInteraction.UserOptions = await _referenceData.GetUserOptionsAsync(vm.NewInteraction.UserId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.SegmentOptions = await _referenceData.GetCustomerSegmentOptionsAsync(vm.SegmentAssignment.CustomerSegmentId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("private async Task PopulateLeadOptionsAsync(LeadEditVm vm, CancellationToken ct)");
        source.Should().Contain("vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.Conversion.UserOptions = await _referenceData.GetUserOptionsAsync(vm.Conversion.UserId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("private async Task PopulateOpportunityOptionsAsync(OpportunityEditVm vm, CancellationToken ct)");
        source.Should().Contain("vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: false, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.AssignedToUserId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(null, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.NewInteraction.UserOptions = await _referenceData.GetUserOptionsAsync(vm.NewInteraction.UserId, includeEmpty: true, ct).ConfigureAwait(false);");
    }


    [Fact]
    public void CrmController_Should_KeepCrmEditorAndLineRowHelpersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("private static void EnsureCustomerAddressRows(CustomerEditVm vm)");
        source.Should().Contain("vm.Addresses ??= new List<CustomerAddressVm>();");
        source.Should().Contain("Country = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault,");
        source.Should().Contain("IsDefaultBilling = true,");
        source.Should().Contain("IsDefaultShipping = true");
        source.Should().Contain("private static void EnsureOpportunityLineRows(OpportunityEditVm vm)");
        source.Should().Contain("vm.Items ??= new List<OpportunityItemVm>();");
        source.Should().Contain("vm.Items.Add(new OpportunityItemVm());");
        source.Should().Contain("private IActionResult RenderLeadEditor(LeadEditVm vm, string actionName)");
        source.Should().Contain("ViewData[\"FormAction\"] = actionName;");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_LeadEditorShell.cshtml\", vm);");
        source.Should().Contain("return actionName == nameof(CreateLead) ? View(\"CreateLead\", vm) : View(\"EditLead\", vm);");
        source.Should().Contain("private IActionResult RenderOpportunityEditor(OpportunityEditVm vm, string actionName)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_OpportunityEditorShell.cshtml\", vm);");
        source.Should().Contain("return actionName == nameof(CreateOpportunity) ? View(\"CreateOpportunity\", vm) : View(\"EditOpportunity\", vm);");
        source.Should().Contain("private IActionResult RenderSegmentEditor(CustomerSegmentEditVm vm, string actionName)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_SegmentEditorShell.cshtml\", vm);");
        source.Should().Contain("return actionName == nameof(CreateSegment) ? View(\"CreateSegment\", vm) : View(\"EditSegment\", vm);");
    }


    [Fact]
    public void CrmController_Should_KeepCustomerWorkspaceBuildersAndRenderHelpersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("private IEnumerable<SelectListItem> BuildCustomerFilterItems(CustomerQueueFilter selectedFilter)");
        source.Should().Contain("new SelectListItem(T(\"AllCustomers\"), CustomerQueueFilter.All.ToString(), selectedFilter == CustomerQueueFilter.All)");
        source.Should().Contain("new SelectListItem(T(\"LinkedUser\"), CustomerQueueFilter.LinkedUser.ToString(), selectedFilter == CustomerQueueFilter.LinkedUser)");
        source.Should().Contain("new SelectListItem(T(\"NeedsSegmentation\"), CustomerQueueFilter.NeedsSegmentation.ToString(), selectedFilter == CustomerQueueFilter.NeedsSegmentation)");
        source.Should().Contain("new SelectListItem(T(\"HasOpportunities\"), CustomerQueueFilter.HasOpportunities.ToString(), selectedFilter == CustomerQueueFilter.HasOpportunities)");
        source.Should().Contain("new SelectListItem(T(\"B2BMissingVatId\"), CustomerQueueFilter.MissingVatId.ToString(), selectedFilter == CustomerQueueFilter.MissingVatId)");
        source.Should().Contain("private static CustomerOpsSummaryVm BuildCustomerOpsSummary(IReadOnlyCollection<CustomerListItemDto> items)");
        source.Should().Contain("LinkedUserCount = items.Count(x => x.UserId.HasValue),");
        source.Should().Contain("LocaleFallbackCount = items.Count(x => x.UsesPlatformLocaleFallback),");
        source.Should().Contain("NeedsSegmentationCount = items.Count(x => x.SegmentCount == 0),");
        source.Should().Contain("HasOpportunitiesCount = items.Count(x => x.OpportunityCount > 0)");
        source.Should().Contain("private static List<CrmPlaybookVm> BuildCustomerPlaybooks()");
        source.Should().Contain("Title = \"CrmCustomerLocaleFallbackPlaybookTitle\"");
        source.Should().Contain("Title = \"CrmCustomerMissingVatPlaybookTitle\"");
        source.Should().Contain("Title = \"CrmCustomerUnsegmentedPlaybookTitle\"");
        source.Should().Contain("private IActionResult RenderCustomerEditor(CustomerEditVm vm, string actionName)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/_CustomerEditorShell.cshtml\", vm);");
        source.Should().Contain("return actionName == nameof(CreateCustomer) ? View(\"CreateCustomer\", vm) : View(\"EditCustomer\", vm);");
        source.Should().Contain("private IActionResult RenderCustomersWorkspace(CustomersListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/Customers.cshtml\", vm);");
        source.Should().Contain("return View(\"Customers\", vm);");
        source.Should().Contain("private IActionResult RenderOverviewWorkspace(CrmSummaryVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Crm/Overview.cshtml\", vm);");
        source.Should().Contain("return View(\"Overview\", vm);");
    }


    [Fact]
    public void CrmController_Should_KeepSharedMappingAndErrorHelpersWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Crm", "CrmController.cs"));

        source.Should().Contain("private async Task PopulateInvoiceOptionsAsync(InvoiceEditVm vm, CancellationToken ct)");
        source.Should().Contain("vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("vm.PaymentOptions = await _referenceData.GetPaymentOptionsAsync(vm.PaymentId, includeEmpty: true, ct).ConfigureAwait(false);");
        source.Should().Contain("private void AddLocalizedModelError(string fallbackKey, Exception ex)");
        source.Should().Contain("ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(ex.Message) ? T(fallbackKey) : ex.Message);");
        source.Should().Contain("private void SetLocalizedError(string fallbackKey, Exception ex)");
        source.Should().Contain("TempData[\"Error\"] = string.IsNullOrWhiteSpace(ex.Message) ? T(fallbackKey) : ex.Message;");
        source.Should().Contain("private static CustomerAddressDto MapCustomerAddress(CustomerAddressVm vm)");
        source.Should().Contain("AddressId = vm.AddressId,");
        source.Should().Contain("IsDefaultBilling = vm.IsDefaultBilling,");
        source.Should().Contain("IsDefaultShipping = vm.IsDefaultShipping");
        source.Should().Contain("private static InteractionCreateDto MapInteraction(InteractionCreateVm vm)");
        source.Should().Contain("LeadId = vm.LeadId,");
        source.Should().Contain("OpportunityId = vm.OpportunityId,");
        source.Should().Contain("Channel = vm.Channel,");
        source.Should().Contain("private static InteractionListItemVm MapInteraction(InteractionListItemDto dto)");
        source.Should().Contain("CreatedAtUtc = dto.CreatedAtUtc");
        source.Should().Contain("private static CrmSummaryVm MapSummary(CrmSummaryDto dto, string currency)");
        source.Should().Contain("Currency = string.IsNullOrWhiteSpace(currency) ? string.Empty : currency.Trim().ToUpperInvariant(),");
        source.Should().Contain("OpenPipelineMinor = dto.OpenPipelineMinor,");
        source.Should().Contain("RecentInteractionCount = dto.RecentInteractionCount");
    }


    [Fact]
    public void CrmSegmentsWorkspace_Should_KeepHelperBackedMemberColumnHeaderWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Segments.cshtml"));

        source.Should().Contain("string SegmentMemberCountLabel() => T.T(\"Members\")");
        source.Should().Contain("<th class=\"text-end\">@SegmentMemberCountLabel()</th>");
        source.Should().NotContain("<th class=\"text-end\">@T.T(\"Members\")</th>");
    }


    [Fact]
    public void CrmCustomersWorkspace_Should_KeepShellSummaryAndFilterRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Customers.cshtml"));

        source.Should().Contain("<div id=\"crm-customers-workspace-shell\">");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<h1 class=\"mb-3\">@T.T(\"Customers\")</h1>");
        source.Should().Contain("<form asp-action=\"Customers\" method=\"get\" class=\"d-flex gap-2\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Customers\", \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-customers-workspace-shell\"");
        source.Should().Contain("placeholder=\"@T.T(\"SearchCustomersPlaceholder\")\"");
        source.Should().Contain("<select name=\"filter\" asp-items=\"Model.FilterItems\" class=\"form-select\"></select>");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateCustomer\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"NewCustomer\")");
        source.Should().Contain("@T.T(\"CrmCustomersFootprintNote\")");
        source.Should().Contain("@T.T(\"CrmCustomerSegmentsCoverageNote\")");
        source.Should().Contain("@T.T(\"CrmCustomerPipelineNote\")");
        source.Should().Contain("@T.T(\"CrmCustomerRecentInteractionsNote\")");
        source.Should().Contain("@T.T(\"CrmPlatformFallbackCultureNote\")");
        source.Should().Contain("@T.T(\"CrmVisibleLocaleFallbacksNote\")");
        source.Should().Contain("@T.T(\"CrmVisibleLinkedUserLocalesNote\")");
        source.Should().Contain("@T.T(\"CrmOperatorPlaybooks\")");
        source.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        source.Should().Contain("@T.T(\"LinkedUser\")");
        source.Should().Contain("@T.T(\"NeedsSegmentation\")");
        source.Should().Contain("@T.T(\"HasOpportunities\")");
        source.Should().Contain("@T.T(\"B2BMissingVatId\")");
        source.Should().Contain("@T.T(\"LocaleFallback\")");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
    }


    [Fact]
    public void CrmCustomersWorkspace_Should_KeepTablePagerAndRowActionRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Customers.cshtml"));

        source.Should().Contain("string LocalizeCustomerTaxProfileType(object? taxProfileType) => taxProfileType is null ? \"-\" : T.T(taxProfileType.ToString() ?? string.Empty);");
        source.Should().Contain("<th>@T.T(\"Name\")</th>");
        source.Should().Contain("<th>@T.T(\"Email\")</th>");
        source.Should().Contain("<th>@T.T(\"Phone\")</th>");
        source.Should().Contain("<th>@T.T(\"Company\")</th>");
        source.Should().Contain("<th>@T.T(\"Locale\")</th>");
        source.Should().Contain("<th>@T.T(\"TaxProfile\")</th>");
        source.Should().Contain("<th>@T.T(\"Segments\")</th>");
        source.Should().Contain("<th>@T.T(\"Opportunities\")</th>");
        source.Should().Contain("<th>@T.T(\"Updated\")</th>");
        source.Should().Contain("<th class=\"text-end\">@T.T(\"Actions\")</th>");
        source.Should().Contain("@T.T(\"NoCustomersFound\")");
        source.Should().Contain("@if (item.UsesPlatformLocaleFallback)");
        source.Should().Contain("@T.T(\"Fallback\")");
        source.Should().Contain("@Model.PlatformDefaultCulture");
        source.Should().Contain("@T.T(\"UserLocale\")");
        source.Should().Contain("@T.T(\"VatIdMissing\")");
        source.Should().Contain("@LocalizeCustomerTaxProfileType(item.TaxProfileType)");
        source.Should().Contain("@T.T(item.ModifiedAtUtc.HasValue ? \"Updated\" : \"Created\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = item.Id })\"");
        source.Should().Contain("@T.T(\"Edit\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Users\", new { id = item.UserId.Value })\"");
        source.Should().Contain("@T.T(\"Users\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = item.Id, fragment = \"customer-interactions-section\" })\"");
        source.Should().Contain("@T.T(\"Interactions\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = item.Id, fragment = \"customer-segments-section\" })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateOpportunity\", \"Crm\", new { customerId = item.Id })\"");
        source.Should().Contain("@T.T(\"Opportunity\")");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("asp-controller=\"Crm\"");
        source.Should().Contain("asp-action=\"Customers\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("hx-target=\"#crm-customers-workspace-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
    }


    [Fact]
    public void CrmLeadsWorkspace_Should_KeepShellSummaryAndFilterRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Leads.cshtml"));

        source.Should().Contain("<div id=\"crm-leads-workspace-shell\">");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<h1 class=\"mb-0\">@T.T(\"Leads\")</h1>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-leads-workspace-shell\"");
        source.Should().Contain("<form asp-action=\"Leads\" method=\"get\" class=\"d-flex gap-2\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Leads\", \"Crm\")\"");
        source.Should().Contain("placeholder=\"@T.T(\"SearchLeadsPlaceholder\")\"");
        source.Should().Contain("<select name=\"filter\" asp-items=\"Model.FilterItems\" class=\"form-select\"></select>");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateLead\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"NewLead\")");
        source.Should().Contain("@T.T(\"CrmOperatorPlaybooks\")");
        source.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        source.Should().Contain("@T.T(\"Qualified leads visible in this queue slice.\")");
        source.Should().Contain("@T.T(\"Rows that still need an explicit lead owner.\")");
        source.Should().Contain("@T.T(\"Leads not yet attached to a CRM customer record.\")");
        source.Should().Contain("@T.T(\"Leads already carrying linked customer context.\")");
        source.Should().Contain("@T.T(\"Visible leads with heavier interaction history needing review.\")");
        source.Should().Contain("@T.T(\"Qualified\")");
        source.Should().Contain("@T.T(\"Unassigned\")");
        source.Should().Contain("@T.T(\"Unconverted\")");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
    }


    [Fact]
    public void CrmLeadsWorkspace_Should_KeepTablePagerAndRowActionRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Leads.cshtml"));

        source.Should().Contain("<th>@T.T(\"Name\")</th>");
        source.Should().Contain("<th>@T.T(\"Company\")</th>");
        source.Should().Contain("<th>@T.T(\"Email\")</th>");
        source.Should().Contain("<th>@T.T(\"Phone\")</th>");
        source.Should().Contain("<th>@T.T(\"Status\")</th>");
        source.Should().Contain("<th>@T.T(\"Owner\")</th>");
        source.Should().Contain("<th>@T.T(\"Interactions\")</th>");
        source.Should().Contain("<th>@T.T(\"UpdatedUtc\")</th>");
        source.Should().Contain("<th class=\"text-end\">@T.T(\"Actions\")</th>");
        source.Should().Contain("@T.T(\"NoLeadsFound\")");
        source.Should().Contain("string LocalizeLeadStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        source.Should().Contain("<span class=\"badge text-bg-light\">@LocalizeLeadStatus(item.Status)</span>");
        source.Should().Contain("@(string.IsNullOrWhiteSpace(item.AssignedToUserDisplayName) ? \"-\" : item.AssignedToUserDisplayName)");
        source.Should().Contain("@item.ModifiedAtUtc?.ToString(\"yyyy-MM-dd HH:mm\")");
        source.Should().Contain("@if (item.CustomerId.HasValue)");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = item.CustomerId.Value })\"");
        source.Should().Contain("@T.T(\"Customer\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateOpportunity\", \"Crm\", new { customerId = item.CustomerId.Value })\"");
        source.Should().Contain("@T.T(\"Opportunity\")");
        source.Should().Contain("else if (item.Status == Darwin.Domain.Enums.LeadStatus.Qualified)");
        source.Should().Contain("<form asp-controller=\"Crm\" asp-action=\"ConvertLead\" method=\"post\" class=\"d-inline\">");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" name=\"LeadId\" value=\"@item.Id\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"RowVersion\" value=\"@Convert.ToBase64String(item.RowVersion)\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"CopyNotesToCustomer\" value=\"true\" />");
        source.Should().Contain("@T.T(\"Convert\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditLead\", \"Crm\", new { id = item.Id, fragment = \"lead-interactions-grid\" })\"");
        source.Should().Contain("@T.T(\"Interactions\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditLead\", \"Crm\", new { id = item.Id })\"");
        source.Should().Contain("@T.T(\"Edit\")");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("asp-controller=\"Crm\"");
        source.Should().Contain("asp-action=\"Leads\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("hx-target=\"#crm-leads-workspace-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
    }


    [Fact]
    public void CrmOpportunitiesWorkspace_Should_KeepShellSummaryAndFilterRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Opportunities.cshtml"));

        source.Should().Contain("<div id=\"crm-opportunities-workspace-shell\">");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<h1 class=\"mb-0\">@T.T(\"Opportunities\")</h1>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-opportunities-workspace-shell\"");
        source.Should().Contain("<form asp-action=\"Opportunities\" method=\"get\" class=\"d-flex gap-2\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Opportunities\", \"Crm\")\"");
        source.Should().Contain("placeholder=\"@T.T(\"SearchOpportunitiesPlaceholder\")\"");
        source.Should().Contain("<select name=\"filter\" asp-items=\"Model.FilterItems\" class=\"form-select\"></select>");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateOpportunity\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"NewOpportunity\")");
        source.Should().Contain("@T.T(\"CrmOperatorPlaybooks\")");
        source.Should().Contain("@foreach (var playbook in Model.Playbooks)");
        source.Should().Contain("@T.T(\"Opportunity rows still active in the pipeline.\")");
        source.Should().Contain("@T.T(\"Visible opportunities requiring near-term close-date review.\")");
        source.Should().Contain("@T.T(\"High-value pipeline rows visible in the current slice.\")");
        source.Should().Contain("@T.T(\"Opportunities still missing an explicit owner.\")");
        source.Should().Contain("@T.T(\"Rows with heavier interaction trails that may need operator review.\")");
        source.Should().Contain("@T.T(\"OpenQueue\")");
        source.Should().Contain("@T.T(\"ClosingSoon\")");
        source.Should().Contain("@T.T(\"HighValue\")");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
    }


    [Fact]
    public void CrmOpportunitiesWorkspace_Should_KeepTablePagerAndRowActionRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Opportunities.cshtml"));

        source.Should().Contain("<th>@T.T(\"Title\")</th>");
        source.Should().Contain("<th>@T.T(\"Customer\")</th>");
        source.Should().Contain("<th>@T.T(\"Stage\")</th>");
        source.Should().Contain("<th>@T.T(\"Estimated\")</th>");
        source.Should().Contain("<th>@T.T(\"CloseDate\")</th>");
        source.Should().Contain("<th>@T.T(\"Owner\")</th>");
        source.Should().Contain("<th>@T.T(\"Items\")</th>");
        source.Should().Contain("<th>@T.T(\"UpdatedUtc\")</th>");
        source.Should().Contain("<th class=\"text-end\">@T.T(\"Actions\")</th>");
        source.Should().Contain("@T.T(\"NoOpportunitiesFound\")");
        source.Should().Contain("string LocalizeOpportunityStage(object? stage) => stage is null ? \"-\" : T.T(stage.ToString() ?? string.Empty);");
        source.Should().Contain("<span class=\"badge text-bg-light\">@LocalizeOpportunityStage(item.Stage)</span>");
        source.Should().Contain("@item.Currency @((item.EstimatedValueMinor / 100M).ToString(\"0.00\"))");
        source.Should().Contain("@item.ExpectedCloseDateUtc?.ToString(\"yyyy-MM-dd\")");
        source.Should().Contain("@(string.IsNullOrWhiteSpace(item.AssignedToUserDisplayName) ? \"-\" : item.AssignedToUserDisplayName)");
        source.Should().Contain("@item.ItemCount");
        source.Should().Contain("@item.ModifiedAtUtc?.ToString(\"yyyy-MM-dd HH:mm\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = item.CustomerId })\"");
        source.Should().Contain("@T.T(\"Customer\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditOpportunity\", \"Crm\", new { id = item.Id, fragment = \"opportunity-interactions-grid\" })\"");
        source.Should().Contain("@T.T(\"Interactions\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditOpportunity\", \"Crm\", new { id = item.Id })\"");
        source.Should().Contain("@T.T(\"Edit\")");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("asp-controller=\"Crm\"");
        source.Should().Contain("asp-action=\"Opportunities\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("hx-target=\"#crm-opportunities-workspace-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
    }


    [Fact]
    public void CrmOverviewWorkspace_Should_KeepShellMetricsAndHeaderRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Overview.cshtml"));

        source.Should().Contain("<div id=\"crm-overview-workspace-shell\">");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<h1 class=\"mb-0\">@T.T(\"CrmOverviewTitle\")</h1>");
        source.Should().Contain("@T.T(\"CrmOverviewIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Customers\", \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-overview-workspace-shell\"");
        source.Should().Contain("@T.T(\"Customers\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Invoices\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"Invoices\")");
        source.Should().Contain("@T.T(\"Customers\")</div><div class=\"fs-3 fw-semibold\">@Model.CustomerCount</div>");
        source.Should().Contain("@T.T(\"Leads\")</div><div class=\"fs-3 fw-semibold\">@Model.LeadCount</div>");
        source.Should().Contain("@T.T(\"QualifiedLeads\")</div><div class=\"fs-3 fw-semibold\">@Model.QualifiedLeadCount</div>");
        source.Should().Contain("@T.T(\"OpenOpportunities\")</div><div class=\"fs-3 fw-semibold\">@Model.OpenOpportunityCount</div>");
        source.Should().Contain("@T.T(\"Segments\")</div><div class=\"fs-3 fw-semibold\">@Model.SegmentCount</div>");
        source.Should().Contain("@T.T(\"Interactions7d\")</div><div class=\"fs-3 fw-semibold\">@Model.RecentInteractionCount</div>");
        source.Should().Contain("@T.T(\"OpenPipeline\")");
        source.Should().Contain("@Model.Currency @((Model.OpenPipelineMinor / 100.0M).ToString(\"0.00\"))");
        source.Should().Contain("@T.T(\"CrmOverviewPipelineNote\")");
    }


    [Fact]
    public void CrmOverviewWorkspace_Should_KeepPlaybooksAndWorkspacePivotsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Overview.cshtml"));

        source.Should().Contain("@T.T(\"CrmOperatorPlaybooks\")");
        source.Should().Contain("@T.T(\"CrmOverviewPlaybookQualifiedLeadsTitle\")");
        source.Should().Contain("@T.T(\"CrmOverviewPlaybookQualifiedLeadsScope\")");
        source.Should().Contain("@T.T(\"CrmOverviewPlaybookQualifiedLeadsAction\")");
        source.Should().Contain("@T.T(\"CrmOverviewPlaybookPipelineTitle\")");
        source.Should().Contain("@T.T(\"CrmOverviewPlaybookPipelineScope\")");
        source.Should().Contain("@T.T(\"CrmOverviewPlaybookPipelineAction\")");
        source.Should().Contain("@T.T(\"CrmNextStep\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Customers\", \"Crm\")\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Leads\", \"Crm\")\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Opportunities\", \"Crm\")\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Segments\", \"Crm\")\"");
        source.Should().Contain("class=\"btn btn-outline-primary\">@T.T(\"Customers\")</a>");
        source.Should().Contain("class=\"btn btn-outline-primary\">@T.T(\"Leads\")</a>");
        source.Should().Contain("class=\"btn btn-outline-primary\">@T.T(\"Opportunities\")</a>");
        source.Should().Contain("class=\"btn btn-outline-primary\">@T.T(\"Segments\")</a>");
    }


    [Fact]
    public void CrmInvoicesWorkspace_Should_KeepShellTaxPolicyAndSearchRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Invoices.cshtml"));

        source.Should().Contain("<div id=\"crm-invoices-workspace-shell\">");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<h1 class=\"mb-1\">@T.T(\"Invoices\")</h1>");
        source.Should().Contain("@T.T(\"InvoicesQueueIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-invoices-workspace-shell\"");
        source.Should().Contain("@T.T(\"Overview\")");
        source.Should().Contain("@T.T(\"Draft\")");
        source.Should().Contain("@T.T(\"DueSoon\")");
        source.Should().Contain("@T.T(\"Overdue\")");
        source.Should().Contain("@T.T(\"VatIdMissing\")");
        source.Should().Contain("@T.T(\"Refunded\")");
        source.Should().Contain("@T.T(\"CrmOperatorPlaybooks\")");
        source.Should().Contain("@foreach (var item in Model.Playbooks)");
        source.Should().Contain("@T.T(\"TaxInvoicingPolicy\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"SiteSettings\", new { fragment = \"site-settings-tax\" })\"");
        source.Should().Contain("@T.T(\"OpenTaxSettings\")");
        source.Should().Contain("@T.T(\"VatEnabled\")");
        source.Should().Contain("@T.T(\"DefaultVat\")");
        source.Should().Contain("@T.T(\"PriceMode\")");
        source.Should().Contain("@T.T(\"ReverseCharge\")");
        source.Should().Contain("@T.T(\"Issuer\")");
        source.Should().Contain("@T.T(\"TaxId\")");
        source.Should().Contain("@T.T(\"InvoicesTaxPolicyNote\")");
        source.Should().Contain("@T.T(\"ArchiveReadiness\")");
        source.Should().Contain("@T.T(\"EInvoiceBaseline\")");
        source.Should().Contain("@T.T(\"TaxSettings\")");
        source.Should().Contain("@T.T(\"CompleteIssuerData\")");
        source.Should().Contain("@T.T(\"ReviewEInvoiceBaseline\")");
        source.Should().Contain("<form asp-action=\"Invoices\" method=\"get\" class=\"row g-2\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Invoices\", \"Crm\")\"");
        source.Should().Contain("placeholder=\"@T.T(\"SearchInvoicesPlaceholder\")\"");
        source.Should().Contain("@T.T(\"Filter\")");
    }


    [Fact]
    public void CrmInvoicesWorkspace_Should_KeepGridActionAndMutationContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "Invoices.cshtml"));

        source.Should().Contain("<th>@T.T(\"Invoice\")</th>");
        source.Should().Contain("<th>@T.T(\"Customer\")</th>");
        source.Should().Contain("<th>@T.T(\"Order\")</th>");
        source.Should().Contain("<th>@T.T(\"Payment\")</th>");
        source.Should().Contain("<th>@T.T(\"TaxProfile\")</th>");
        source.Should().Contain("<th class=\"text-end\">@T.T(\"NetTaxGross\")</th>");
        source.Should().Contain("<th>@T.T(\"Status\")</th>");
        source.Should().Contain("<th>@T.T(\"Due\")</th>");
        source.Should().Contain("<th class=\"text-end\">@T.T(\"Actions\")</th>");
        source.Should().Contain("@T.T(\"NoInvoicesFound\")");
        source.Should().Contain("string LocalizeCustomerTaxProfileType(object? taxProfileType) => taxProfileType is null ? \"-\" : T.T(taxProfileType.ToString() ?? string.Empty);");
        source.Should().Contain("<code>@item.Id</code>");
        source.Should().Contain("@T.T(\"Unlinked\")");
        source.Should().Contain("@T.T(\"VatIdMissing\")");
        source.Should().Contain("@T.T(\"Net\"):");
        source.Should().Contain("@T.T(\"Tax\"):");
        source.Should().Contain("@T.T(\"Gross\"):");
        source.Should().Contain("@T.T(\"Refunded\"):");
        source.Should().Contain("@T.T(\"Settled\"):");
        source.Should().Contain("@T.T(\"Balance\"):");
        source.Should().Contain("@item.DueDateUtc.ToString(\"yyyy-MM-dd\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditInvoice\", \"Crm\", new { id = item.Id })\"");
        source.Should().Contain("@T.T(\"Open\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = item.CustomerId.Value })\"");
        source.Should().Contain("@T.T(\"Customer\")");
        source.Should().Contain("@T.T(\"FixVatId\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Details\", \"Orders\", new { id = item.OrderId.Value })\"");
        source.Should().Contain("@T.T(\"Order\")");
        source.Should().Contain("fragment = \"payments\"");
        source.Should().Contain("@T.T(\"Payment\")");
        source.Should().Contain("@LocalizeCustomerTaxProfileType(item.CustomerTaxProfileType)");
        source.Should().Contain("<form asp-action=\"TransitionInvoiceStatus\" method=\"post\" class=\"d-inline\">");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" name=\"TargetStatus\" value=\"@Darwin.Domain.Enums.InvoiceStatus.Open\" />");
        source.Should().Contain("@T.T(\"Post\")");
        source.Should().Contain("<input type=\"hidden\" name=\"TargetStatus\" value=\"@Darwin.Domain.Enums.InvoiceStatus.Paid\" />");
        source.Should().Contain("@T.T(\"MarkPaid\")");
        source.Should().Contain("<pager page=\"Model.Page\" page-size=\"Model.PageSize\" total=\"Model.Total\" asp-controller=\"Crm\" asp-action=\"Invoices\" asp-route-q=\"@Model.Query\" hx-target=\"#crm-invoices-workspace-shell\" hx-swap=\"outerHTML\"></pager>");
    }


    [Fact]
    public void CrmCustomerEditorShell_Should_KeepSummaryAndFollowUpRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_CustomerEditorShell.cshtml"));

        source.Should().Contain("<div id=\"crm-customer-editor-shell\">");
        source.Should().Contain("var formAction = ViewData[\"FormAction\"]?.ToString() ?? \"EditCustomer\";");
        source.Should().Contain("var heading = formAction == \"CreateCustomer\" ? T.T(\"CreateCustomer\") : T.T(\"EditCustomer\");");
        source.Should().Contain("string LocalizeCustomerTaxProfileType(object? taxProfileType) => taxProfileType is null ? \"-\" : T.T(taxProfileType.ToString() ?? string.Empty);");
        source.Should().Contain("<h1 class=\"mb-3\">@heading</h1>");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@if (Model.Id != Guid.Empty)");
        source.Should().Contain("@T.T(\"CustomerLinkedUser\")");
        source.Should().Contain("@T.T(\"Company\")");
        source.Should().Contain("@T.T(\"TaxProfile\")");
        source.Should().Contain("@LocalizeCustomerTaxProfileType(Model.TaxProfileType)");
        source.Should().Contain("@T.T(\"TaxId\")");
        source.Should().Contain("@T.T(\"Locale\")");
        source.Should().Contain("@T.T(\"Segments\")");
        source.Should().Contain("@T.T(\"Opportunities\")");
        source.Should().Contain("@T.T(\"Interactions\") / @T.T(\"Consents\")");
        source.Should().Contain("@T.T(\"CustomerFollowUpWorkspace\")");
        source.Should().Contain("@T.T(\"CustomerFollowUpWorkspaceNote\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateOpportunity\", \"Crm\", new { customerId = Model.Id })\"");
        source.Should().Contain("@T.T(\"CreateOpportunity\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Invoices\", \"Crm\", new { q = Model.Id })\"");
        source.Should().Contain("@T.T(\"ReviewInvoices\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Customers\", \"Crm\", new { filter = \"NeedsSegmentation\" })\"");
        source.Should().Contain("@T.T(\"ReviewSegmentation\")");
    }


    [Fact]
    public void CrmCustomerEditorShell_Should_KeepFormAndFragmentContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_CustomerEditorShell.cshtml"));

        source.Should().Contain("<form asp-action=\"@formAction\"");
        source.Should().Contain("hx-post=\"@Url.Action(formAction, \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-customer-editor-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        source.Should().Contain("<partial name=\"_CustomerForm\" model=\"Model\" />");
        source.Should().Contain("@T.T(\"Save\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Customers\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"Back\")");
        source.Should().Contain("id=\"customer-interactions-section\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"CustomerInteractions\", \"Crm\", new { customerId = Model.Id })\"");
        source.Should().Contain("id=\"customer-consents-section\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"CustomerConsents\", \"Crm\", new { customerId = Model.Id })\"");
        source.Should().Contain("id=\"customer-segments-section\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"CustomerSegmentMemberships\", \"Crm\", new { customerId = Model.Id })\"");
        source.Should().Contain("fragment = \"customer-interactions-section\"");
        source.Should().Contain("fragment = \"customer-consents-section\"");
        source.Should().Contain("fragment = \"customer-segments-section\"");
        source.Should().Contain("@T.T(\"Open\")");
        source.Should().Contain("@T.T(\"Loading\")...");
        source.Should().Contain("<partial name=\"~/Views/Crm/_CustomerFormScript.cshtml\" />");
    }


    [Fact]
    public void CrmLeadEditorShell_Should_KeepSummaryAndFollowUpRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_LeadEditorShell.cshtml"));

        source.Should().Contain("<div id=\"crm-lead-editor-shell\">");
        source.Should().Contain("var isCreate = Model.Id == Guid.Empty;");
        source.Should().Contain("var formAction = (string?)ViewData[\"FormAction\"] ?? (isCreate ? \"CreateLead\" : \"EditLead\");");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@if (!isCreate)");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"Owner\")");
        source.Should().Contain("@T.T(\"Customer\")");
        source.Should().Contain("@T.T(\"Source\")");
        source.Should().Contain("@T.T(\"Interactions\")");
        source.Should().Contain("@T.T(\"LeadFollowUpWorkspace\")");
        source.Should().Contain("@T.T(\"LeadFollowUpWorkspaceNote\")");
        source.Should().Contain("@if (Model.CustomerId.HasValue)");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = Model.CustomerId.Value })\"");
        source.Should().Contain("@T.T(\"OpenCustomer\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateOpportunity\", \"Crm\", new { customerId = Model.CustomerId.Value })\"");
        source.Should().Contain("@T.T(\"CreateOpportunity\")");
        source.Should().Contain("else");
        source.Should().Contain("<button type=\"button\" class=\"btn btn-sm btn-outline-success\" disabled>");
    }


    [Fact]
    public void CrmLeadEditorShell_Should_KeepFormAndFragmentContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_LeadEditorShell.cshtml"));

        source.Should().Contain("<form asp-action=\"@formAction\"");
        source.Should().Contain("hx-post=\"@Url.Action(formAction, \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-lead-editor-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        source.Should().Contain("<partial name=\"_LeadForm\" model=\"Model\" />");
        source.Should().Contain("@T.T(\"Save\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Leads\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"Back\")");
        source.Should().Contain("asp-fragment=\"lead-interactions-section\"");
        source.Should().Contain("fragment = \"lead-interactions-section\"");
        source.Should().Contain("id=\"lead-interactions-section\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"LeadInteractions\", \"Crm\", new { leadId = Model.Id })\"");
        source.Should().Contain("@T.T(\"Open\")");
        source.Should().Contain("@T.T(\"Loading\")...");
    }


    [Fact]
    public void CrmOpportunityEditorShell_Should_KeepSummaryAndFollowUpRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_OpportunityEditorShell.cshtml"));

        source.Should().Contain("<div id=\"crm-opportunity-editor-shell\">");
        source.Should().Contain("var isCreate = Model.Id == Guid.Empty;");
        source.Should().Contain("var formAction = (string?)ViewData[\"FormAction\"] ?? (isCreate ? \"CreateOpportunity\" : \"EditOpportunity\");");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@if (!isCreate)");
        source.Should().Contain("@T.T(\"Stage\")");
        source.Should().Contain("@T.T(\"Customer\")");
        source.Should().Contain("@T.T(\"Estimated\")");
        source.Should().Contain("@T.T(\"Items\")");
        source.Should().Contain("@T.T(\"Owner\")");
        source.Should().Contain("@T.T(\"Interactions\")");
        source.Should().Contain("@T.T(\"OpportunityFollowUpWorkspace\")");
        source.Should().Contain("@T.T(\"OpportunityFollowUpWorkspaceNote\")");
        source.Should().Contain("string LocalizeOpportunityStage(object? stage) => stage is null ? \"-\" : T.T(stage.ToString() ?? string.Empty);");
        source.Should().Contain("<div class=\"fw-semibold\">@LocalizeOpportunityStage(Model.Stage)</div>");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCustomer\", \"Crm\", new { id = Model.CustomerId })\"");
        source.Should().Contain("@T.T(\"OpenCustomer\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Invoices\", \"Crm\", new { q = Model.CustomerId })\"");
        source.Should().Contain("@T.T(\"ReviewInvoices\")");
    }


    [Fact]
    public void CrmOpportunityEditorShell_Should_KeepFormAndFragmentContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_OpportunityEditorShell.cshtml"));

        source.Should().Contain("<form asp-action=\"@formAction\"");
        source.Should().Contain("hx-post=\"@Url.Action(formAction, \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-opportunity-editor-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        source.Should().Contain("<partial name=\"_OpportunityForm\" model=\"Model\" />");
        source.Should().Contain("@T.T(\"Save\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Opportunities\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"Back\")");
        source.Should().Contain("asp-fragment=\"opportunity-interactions-section\"");
        source.Should().Contain("fragment = \"opportunity-interactions-section\"");
        source.Should().Contain("id=\"opportunity-interactions-section\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"OpportunityInteractions\", \"Crm\", new { opportunityId = Model.Id })\"");
        source.Should().Contain("@T.T(\"Open\")");
        source.Should().Contain("@T.T(\"Loading\")...");
    }


    [Fact]
    public void CrmOpportunityForm_Should_KeepFieldAndSummaryContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_OpportunityForm.cshtml"));

        source.Should().Contain("<label asp-for=\"CustomerId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"CustomerId\" asp-items=\"Model.CustomerOptions\" class=\"form-select\"></select>");
        source.Should().Contain("<label asp-for=\"AssignedToUserId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"AssignedToUserId\" asp-items=\"Model.UserOptions\" class=\"form-select\"></select>");
        source.Should().Contain("<label asp-for=\"Title\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Title\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"Title\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"Stage\" class=\"form-label\"></label>");
        source.Should().Contain("var opportunityStageOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.OpportunityStage>().Select");
        source.Should().Contain("Text = T.T(option.Text)");
        source.Should().Contain("asp-for=\"Stage\" asp-items=\"opportunityStageOptions\" class=\"form-select\"");
        source.Should().Contain("<label asp-for=\"ExpectedCloseDateUtc\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"ExpectedCloseDateUtc\" class=\"form-control\" type=\"date\" />");
        source.Should().Contain("<label asp-for=\"EstimatedValueMinor\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"EstimatedValueMinor\" class=\"form-control\" />");
        source.Should().Contain("@T.T(\"MinorUnitsExample\")");
        source.Should().Contain("<span asp-validation-for=\"EstimatedValueMinor\" class=\"text-danger\"></span>");
        source.Should().Contain("@T.T(\"OpportunityItems\")");
        source.Should().Contain("id=\"addOpportunityLine\"");
        source.Should().Contain("@T.T(\"AddLine\")");
    }


    [Fact]
    public void CrmOpportunityForm_Should_KeepLineItemAndTemplateContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_OpportunityForm.cshtml"));

        source.Should().Contain("id=\"opportunityLines\"");
        source.Should().Contain("@for (var i = 0; i < Model.Items.Count; i++)");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Items[i].Id\" />");
        source.Should().Contain("<label asp-for=\"Items[i].ProductVariantId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"Items[i].ProductVariantId\" asp-items=\"Model.VariantOptions\" class=\"form-select\"></select>");
        source.Should().Contain("<label asp-for=\"Items[i].Quantity\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Items[i].Quantity\" class=\"form-control\" />");
        source.Should().Contain("<label asp-for=\"Items[i].UnitPriceMinor\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Items[i].UnitPriceMinor\" class=\"form-control\" />");
        source.Should().Contain("@T.T(\"Remove\")");
        source.Should().Contain("<template id=\"opportunityLineTemplate\">");
        source.Should().Contain("name=\"Items[__index__].Id\"");
        source.Should().Contain("@foreach (var option in Model.VariantOptions)");
        source.Should().Contain("name=\"Items[__index__].ProductVariantId\"");
        source.Should().Contain("@T.T(\"ProductVariant\")");
        source.Should().Contain("name=\"Items[__index__].Quantity\" value=\"1\"");
        source.Should().Contain("@T.T(\"Quantity\")");
        source.Should().Contain("name=\"Items[__index__].UnitPriceMinor\" value=\"0\"");
        source.Should().Contain("@T.T(\"UnitPriceMinor\")");
        source.Should().Contain("class=\"btn btn-sm btn-outline-danger remove-line\">@T.T(\"Remove\")</button>");
    }


    [Fact]
    public void CrmCustomerForm_Should_KeepFieldAndEffectiveProfileContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_CustomerForm.cshtml"));

        source.Should().Contain("@using Darwin.Application.Settings.DTOs");
        source.Should().Contain("string LocalizeCustomerTaxProfileType(object? taxProfileType) => taxProfileType is null ? \"-\" : T.T(taxProfileType.ToString() ?? string.Empty);");
        source.Should().Contain("<label asp-for=\"UserId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"UserId\" asp-items=\"Model.UserOptions\" class=\"form-select\"></select>");
        source.Should().Contain("@T.T(\"LinkedUserOwnershipNote\")");
        source.Should().Contain("<label asp-for=\"CompanyName\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"CompanyName\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"CompanyName\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"TaxProfileType\" class=\"form-label\"></label>");
        source.Should().Contain("var customerTaxProfileTypeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.CustomerTaxProfileType>().Select");
        source.Should().Contain("Text = T.T(option.Text)");
        source.Should().Contain("asp-for=\"TaxProfileType\" asp-items=\"customerTaxProfileTypeOptions\" class=\"form-select\"");
        source.Should().Contain("<span asp-validation-for=\"TaxProfileType\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"VatId\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"VatId\" class=\"form-control\" />");
        source.Should().Contain("<label asp-for=\"FirstName\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"LastName\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Email\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Phone\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Notes\" class=\"form-label\"></label>");
        source.Should().Contain("<textarea asp-for=\"Notes\" class=\"form-control\" rows=\"4\"></textarea>");
        source.Should().Contain("@if (!string.IsNullOrWhiteSpace(Model.EffectiveEmail) || !string.IsNullOrWhiteSpace(Model.EffectiveFirstName))");
        source.Should().Contain("@T.T(\"TaxProfile\"): @LocalizeCustomerTaxProfileType(Model.TaxProfileType)");
        source.Should().Contain("<div class=\"alert alert-info mt-4 mb-0\">");
        source.Should().Contain("@T.T(\"EffectiveProfile\")");
        source.Should().Contain("@T.T(\"Name\"):");
        source.Should().Contain("@T.T(\"Email\"):");
        source.Should().Contain("@T.T(\"Phone\"):");
        source.Should().Contain("@T.T(\"TaxProfile\"):");
        source.Should().Contain("@T.T(\"TaxId\"):");
    }


    [Fact]
    public void CrmCustomerForm_Should_KeepAddressAndTemplateContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_CustomerForm.cshtml"));

        source.Should().Contain("@T.T(\"CrmAddresses\")");
        source.Should().Contain("id=\"addCustomerAddress\"");
        source.Should().Contain("@T.T(\"AddAddress\")");
        source.Should().Contain("id=\"customerAddresses\"");
        source.Should().Contain("@for (var i = 0; i < Model.Addresses.Count; i++)");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Addresses[i].Id\" />");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Addresses[i].AddressId\" />");
        source.Should().Contain("<label asp-for=\"Addresses[i].Line1\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Addresses[i].Line2\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Addresses[i].PostalCode\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Addresses[i].City\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Addresses[i].State\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"Addresses[i].Country\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Addresses[i].IsDefaultBilling\" class=\"form-check-input\" />");
        source.Should().Contain("<input asp-for=\"Addresses[i].IsDefaultShipping\" class=\"form-check-input\" />");
        source.Should().Contain("@T.T(\"Remove\")");
        source.Should().Contain("<template id=\"customerAddressTemplate\">");
        source.Should().Contain("name=\"Addresses[__index__].Id\"");
        source.Should().Contain("name=\"Addresses[__index__].AddressId\"");
        source.Should().Contain("@T.T(\"AddressLine1\")");
        source.Should().Contain("@T.T(\"AddressLine2\")");
        source.Should().Contain("@T.T(\"PostalCode\")");
        source.Should().Contain("@T.T(\"City\")");
        source.Should().Contain("@T.T(\"State\")");
        source.Should().Contain("@T.T(\"Country\")");
        source.Should().Contain("value=\"@SiteSettingDto.DefaultCountryDefault\"");
        source.Should().Contain("name=\"Addresses[__index__].IsDefaultBilling\" value=\"false\"");
        source.Should().Contain("@T.T(\"DefaultBilling\")");
        source.Should().Contain("name=\"Addresses[__index__].IsDefaultShipping\" value=\"false\"");
        source.Should().Contain("@T.T(\"DefaultShipping\")");
        source.Should().Contain("class=\"btn btn-sm btn-outline-danger remove-address\">@T.T(\"Remove\")</button>");
    }


    [Fact]
    public void CrmLeadForm_Should_KeepFieldContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_LeadForm.cshtml"));

        source.Should().Contain("<label asp-for=\"FirstName\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"FirstName\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"FirstName\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"LastName\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"LastName\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"LastName\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"CompanyName\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"CompanyName\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"CompanyName\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"Status\" class=\"form-label\"></label>");
        source.Should().Contain("var leadStatusOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.LeadStatus>().Select");
        source.Should().Contain("Text = T.T(option.Text)");
        source.Should().Contain("asp-for=\"Status\" asp-items=\"leadStatusOptions\" class=\"form-select\"");
        source.Should().Contain("<label asp-for=\"Email\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Email\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"Email\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"Phone\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Phone\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"Phone\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"AssignedToUserId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"AssignedToUserId\" asp-items=\"Model.UserOptions\" class=\"form-select\"></select>");
        source.Should().Contain("<label asp-for=\"CustomerId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"CustomerId\" asp-items=\"Model.CustomerOptions\" class=\"form-select\"></select>");
        source.Should().Contain("<label asp-for=\"Source\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Source\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"Source\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"Notes\" class=\"form-label\"></label>");
        source.Should().Contain("<textarea asp-for=\"Notes\" class=\"form-control\" rows=\"4\"></textarea>");
        source.Should().Contain("<span asp-validation-for=\"Notes\" class=\"text-danger\"></span>");
    }


    [Fact]
    public void CrmSegmentEditorShell_Should_KeepHeadingAndFormCompositionWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_SegmentEditorShell.cshtml"));

        source.Should().Contain("var actionName = ViewData[\"FormAction\"] as string ?? \"EditSegment\";");
        source.Should().Contain("var isCreate = string.Equals(actionName, \"CreateSegment\", StringComparison.Ordinal);");
        source.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"CreateSegment\") : T.T(\"EditSegment\");");
        source.Should().Contain("<div class=\"d-flex justify-content-between align-items-center mb-3\">");
        source.Should().Contain("<h1 class=\"mb-1\">@(isCreate ? T.T(\"CreateSegment\") : T.T(\"EditSegment\"))</h1>");
        source.Should().Contain("@T.T(\"CustomerSegmentationTaxonomyHelp\")");
        source.Should().Contain("<partial name=\"_SegmentForm\" model=\"Model\" />");
    }


    [Fact]
    public void CrmSegmentForm_Should_KeepSubmitAndFieldContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_SegmentForm.cshtml"));

        source.Should().Contain("var actionName = ViewData[\"FormAction\"] as string ?? \"EditSegment\";");
        source.Should().Contain("var isCreate = string.Equals(actionName, \"CreateSegment\", StringComparison.Ordinal);");
        source.Should().Contain("<form asp-action=\"@actionName\" method=\"post\" class=\"row g-3\">");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("@if (!isCreate)");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        source.Should().Contain("<div asp-validation-summary=\"ModelOnly\" class=\"text-danger\"></div>");
        source.Should().Contain("<label asp-for=\"Name\" class=\"form-label\"></label>");
        source.Should().Contain("<input asp-for=\"Name\" class=\"form-control\" />");
        source.Should().Contain("<span asp-validation-for=\"Name\" class=\"text-danger\"></span>");
        source.Should().Contain("<label asp-for=\"Description\" class=\"form-label\"></label>");
        source.Should().Contain("<textarea asp-for=\"Description\" class=\"form-control\" rows=\"4\"></textarea>");
        source.Should().Contain("<span asp-validation-for=\"Description\" class=\"text-danger\"></span>");
        source.Should().Contain("<button type=\"submit\" class=\"btn btn-primary\">@(isCreate ? T.T(\"Create\") : T.T(\"Save\"))</button>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Segments\", \"Crm\")\"");
        source.Should().Contain("hx-target=\"#crm-segment-editor-shell\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
        source.Should().Contain("hx-push-url=\"true\"");
        source.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void CrmCustomerFormScript_Should_KeepAddressEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_CustomerFormScript.cshtml"));

        source.Should().Contain("window.darwinAdmin = window.darwinAdmin || {};");
        source.Should().Contain("window.darwinAdmin.initCustomerAddressEditor = window.darwinAdmin.initCustomerAddressEditor || function (root) {");
        source.Should().Contain("if (!root) return;");
        source.Should().Contain("const container = root.querySelector('#customerAddresses');");
        source.Should().Contain("const template = root.querySelector('#customerAddressTemplate');");
        source.Should().Contain("const addButton = root.querySelector('#addCustomerAddress');");
        source.Should().Contain("if (addButton && !addButton.dataset.customerAddressesBound)");
        source.Should().Contain("addButton.dataset.customerAddressesBound = 'true';");
        source.Should().Contain("addButton.addEventListener('click', function () {");
        source.Should().Contain("if (!container || !template) return;");
        source.Should().Contain("const index = container.querySelectorAll('.address-row').length;");
        source.Should().Contain("container.insertAdjacentHTML('beforeend', template.innerHTML.replaceAll('__index__', index));");
        source.Should().Contain("if (container && !container.dataset.customerAddressesRemoveBound)");
        source.Should().Contain("container.dataset.customerAddressesRemoveBound = 'true';");
        source.Should().Contain("container.addEventListener('click', function (event) {");
        source.Should().Contain("const button = event.target.closest('.remove-address');");
        source.Should().Contain("button.closest('.address-row')?.remove();");
        source.Should().Contain("window.darwinAdmin.initCustomerAddressEditor(document.getElementById('crm-customer-editor-shell'));");
    }


    [Fact]
    public void CrmCustomerSegmentsSection_Should_KeepMembershipGridAndRemovalContractWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_CustomerSegmentsSection.cshtml"));

        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<th>@T.T(\"Name\")</th>");
        source.Should().Contain("<th>@T.T(\"Description\")</th>");
        source.Should().Contain("@if (Model.Items.Count == 0)");
        source.Should().Contain("@T.T(\"NoAssignedSegments\")");
        source.Should().Contain("foreach (var item in Model.Items)");
        source.Should().Contain("asp-action=\"RemoveCustomerSegmentMembership\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"RemoveCustomerSegmentMembership\", \"Crm\")\"");
        source.Should().Contain("hx-target=\"#customer-segments-grid\"");
        source.Should().Contain("hx-swap=\"innerHTML\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("<input type=\"hidden\" name=\"customerId\" value=\"@Model.CustomerId\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"membershipId\" value=\"@item.MembershipId\" />");
        source.Should().Contain("@T.T(\"Remove\")");
    }


    [Fact]
    public void CrmInteractionsSection_Should_KeepGridAndScopePagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_InteractionsSection.cshtml"));

        source.Should().Contain("<div hx-boost=\"true\"");
        source.Should().Contain("hx-target=\"this\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
        source.Should().Contain("hx-push-url=\"false\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<th>@T.T(\"Type\")</th>");
        source.Should().Contain("<th>@T.T(\"Channel\")</th>");
        source.Should().Contain("<th>@T.T(\"Subject\")</th>");
        source.Should().Contain("<th>@T.T(\"Content\")</th>");
        source.Should().Contain("<th>@T.T(\"CreatedAtUtc\")</th>");
        source.Should().Contain("string LocalizeInteractionType(object? type) => type is null ? \"-\" : T.T(type.ToString() ?? string.Empty);");
        source.Should().Contain("string LocalizeInteractionChannel(object? channel) => channel is null ? \"-\" : T.T(channel.ToString() ?? string.Empty);");
        source.Should().Contain("@if (Model.Items.Count == 0)");
        source.Should().Contain("@T.T(\"NoInteractionsYet\")");
        source.Should().Contain("foreach (var item in Model.Items)");
        source.Should().Contain("@LocalizeInteractionType(item.Type)");
        source.Should().Contain("@LocalizeInteractionChannel(item.Channel)");
        source.Should().Contain("@item.Subject");
        source.Should().Contain("@item.Content");
        source.Should().Contain("@item.CreatedAtUtc.ToString(\"yyyy-MM-dd HH:mm:ss\")");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("page-size=\"Model.PageSize\"");
        source.Should().Contain("total=\"Model.Total\"");
        source.Should().Contain("asp-controller=\"Crm\"");
        source.Should().Contain("asp-action=\"@(Model.Scope == \"customer\" ? \"CustomerInteractions\" : Model.Scope == \"lead\" ? \"LeadInteractions\" : \"OpportunityInteractions\")\"");
        source.Should().Contain("asp-route-customerId=\"@(Model.Scope == \"customer\" ? Model.EntityId : (Guid?)null)\"");
        source.Should().Contain("asp-route-leadId=\"@(Model.Scope == \"lead\" ? Model.EntityId : (Guid?)null)\"");
        source.Should().Contain("asp-route-opportunityId=\"@(Model.Scope == \"opportunity\" ? Model.EntityId : (Guid?)null)\"");
    }


    [Fact]
    public void CrmConsentsSection_Should_KeepGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_ConsentsSection.cshtml"));

        source.Should().Contain("<div hx-boost=\"true\"");
        source.Should().Contain("hx-target=\"this\"");
        source.Should().Contain("hx-swap=\"outerHTML\"");
        source.Should().Contain("hx-push-url=\"false\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("<th>@T.T(\"Type\")</th>");
        source.Should().Contain("<th>@T.T(\"Status\")</th>");
        source.Should().Contain("<th>@T.T(\"GrantedAtUtcShort\")</th>");
        source.Should().Contain("<th>@T.T(\"RevokedAtUtcShort\")</th>");
        source.Should().Contain("@if (Model.Items.Count == 0)");
        source.Should().Contain("@T.T(\"NoConsentHistory\")");
        source.Should().Contain("string LocalizeConsentType(object? type) => type is null ? \"-\" : T.T(type.ToString() ?? string.Empty);");
        source.Should().Contain("foreach (var item in Model.Items)");
        source.Should().Contain("@LocalizeConsentType(item.Type)");
        source.Should().Contain("@(item.Granted ? T.T(\"Granted\") : T.T(\"Revoked\"))");
        source.Should().Contain("@item.GrantedAtUtc.ToString(\"yyyy-MM-dd HH:mm:ss\")");
        source.Should().Contain("@(item.RevokedAtUtc?.ToString(\"yyyy-MM-dd HH:mm:ss\") ?? \"-\")");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("page-size=\"Model.PageSize\"");
        source.Should().Contain("total=\"Model.Total\"");
        source.Should().Contain("asp-controller=\"Crm\"");
        source.Should().Contain("asp-action=\"CustomerConsents\"");
        source.Should().Contain("asp-route-customerId=\"@Model.CustomerId\"");
    }


    [Fact]
    public void CrmInvoiceEditorShell_Should_KeepSummaryStatusAndFollowUpRailsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_InvoiceEditorShell.cshtml"));

        source.Should().Contain("<div id=\"crm-invoice-editor-shell\">");
        source.Should().Contain("<h1 class=\"mb-3\">@T.T(\"EditInvoice\")</h1>");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"Invoice\"):");
        source.Should().Contain("@T.T(\"Order\"):");
        source.Should().Contain("@T.T(\"Customer\"):");
        source.Should().Contain("@T.T(\"VatIdMissing\")");
        source.Should().Contain("string LocalizeCustomerTaxProfileType(object? taxProfileType) => taxProfileType is null ? \"-\" : T.T(taxProfileType.ToString() ?? string.Empty);");
        source.Should().Contain("@LocalizeCustomerTaxProfileType(Model.CustomerTaxProfileType)");
        source.Should().Contain("@T.T(\"QuickStatus\")");
        source.Should().Contain("hx-post=\"@Url.Action(\"TransitionInvoiceStatus\", \"Crm\")\"");
        source.Should().Contain("T.T(\"PostInvoice\")");
        source.Should().Contain("T.T(\"SetOpen\")");
        source.Should().Contain("@T.T(\"MarkPaid\")");
        source.Should().Contain("@T.T(\"VoidInvoice\")");
        source.Should().Contain("@T.T(\"Current\"):");
        source.Should().Contain("@T.T(\"InvoiceFollowUpWorkspace\")");
        source.Should().Contain("@T.T(\"InvoiceFollowUpWorkspaceNote\")");
        source.Should().Contain("@T.T(\"OpenCustomer\")");
        source.Should().Contain("@T.T(\"ReviewOrder\")");
        source.Should().Contain("@T.T(\"ReviewPaymentTrail\")");
        source.Should().Contain("@T.T(\"RelatedInvoices\")");
        source.Should().Contain("@T.T(\"TaxInvoicingPolicy\")");
        source.Should().Contain("@T.T(\"OpenTaxSettings\")");
        source.Should().Contain("@T.T(\"TaxSettings\")");
        source.Should().Contain("@T.T(\"CustomerTaxProfile\")");
        source.Should().Contain("@T.T(\"FixVatId\")");
        source.Should().Contain("@T.T(\"CompleteIssuerData\")");
        source.Should().Contain("@T.T(\"NetTaxGross\")");
        source.Should().Contain("@T.T(\"RefundedSettled\")");
        source.Should().Contain("@T.T(\"OpenBalance\")");
    }


    [Fact]
    public void CrmInvoiceEditorShell_Should_KeepRefundAndEditFormContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Crm", "_InvoiceEditorShell.cshtml"));

        source.Should().Contain("@if (Model.PaymentId.HasValue && Model.SettledAmountMinor > 0)");
        source.Should().Contain("@T.T(\"RefundWorkflow\")");
        source.Should().Contain("@T.T(\"RefundWorkflowNote\")");
        source.Should().Contain("@T.T(\"MaxRefundableNow\")");
        source.Should().Contain("<form asp-action=\"RefundInvoice\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"RefundInvoice\", \"Crm\")\"");
        source.Should().Contain("<input type=\"hidden\" name=\"InvoiceId\" value=\"@Model.Id\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"RowVersion\" value=\"@Convert.ToBase64String(Model.RowVersion)\" />");
        source.Should().Contain("<input type=\"hidden\" name=\"Currency\" value=\"@Model.Currency\" />");
        source.Should().Contain("@T.T(\"RefundAmountMinor\")");
        source.Should().Contain("@T.T(\"Reason\")");
        source.Should().Contain("@T.T(\"Refund\")");
        source.Should().Contain("<form asp-action=\"EditInvoice\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"EditInvoice\", \"Crm\")\"");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        source.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        source.Should().Contain("<label asp-for=\"BusinessId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"></select>");
        source.Should().Contain("<label asp-for=\"CustomerId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"CustomerId\" asp-items=\"Model.CustomerOptions\" class=\"form-select\"></select>");
        source.Should().Contain("<label asp-for=\"PaymentId\" class=\"form-label\"></label>");
        source.Should().Contain("<select asp-for=\"PaymentId\" asp-items=\"Model.PaymentOptions\" class=\"form-select\"></select>");
        source.Should().Contain("@T.T(\"Current\"): @Model.PaymentSummary");
        source.Should().Contain("var invoiceStatusOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.InvoiceStatus>().Select");
        source.Should().Contain("Text = T.T(option.Text)");
        source.Should().Contain("asp-for=\"Status\" asp-items=\"invoiceStatusOptions\" class=\"form-select\"");
        source.Should().Contain("<label asp-for=\"Currency\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"DueDateUtc\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"PaidAtUtc\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"TotalNetMinor\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"TotalTaxMinor\" class=\"form-label\"></label>");
        source.Should().Contain("<label asp-for=\"TotalGrossMinor\" class=\"form-label\"></label>");
        source.Should().Contain("@T.T(\"Save\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Invoices\", \"Crm\")\"");
        source.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void LoyaltyController_Should_KeepOperationalMutationsProtected()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("[ValidateAntiForgeryToken]");
        source.Should().Contain("public async Task<IActionResult> CreateProgram(LoyaltyProgramEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditProgram(LoyaltyProgramEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteProgram(");
        source.Should().Contain("public async Task<IActionResult> CreateRewardTier(LoyaltyRewardTierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditRewardTier(LoyaltyRewardTierEditVm vm");
        source.Should().Contain("public async Task<IActionResult> DeleteRewardTier(");
        source.Should().Contain("public async Task<IActionResult> CreateAccount(CreateLoyaltyAccountVm vm");
        source.Should().Contain("public async Task<IActionResult> CreateCampaign(LoyaltyCampaignEditVm vm");
        source.Should().Contain("public async Task<IActionResult> EditCampaign(LoyaltyCampaignEditVm vm");
        source.Should().Contain("public async Task<IActionResult> SetCampaignActivation(");
        source.Should().Contain("public async Task<IActionResult> AdjustPoints(AdjustLoyaltyPointsVm vm");
        source.Should().Contain("public async Task<IActionResult> SuspendAccount(");
        source.Should().Contain("public async Task<IActionResult> ActivateAccount(");
        source.Should().Contain("public async Task<IActionResult> ConfirmRedemption(");
    }


    [Fact]
    public void LoyaltyController_Should_KeepAdminWorkspacesAndEditorGetsReachable()
    {
        var loyaltySource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));
        var adminBaseSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "AdminBaseController.cs"));

        adminBaseSource.Should().Contain("[PermissionAuthorize(\"AccessAdminPanel\")]");
        loyaltySource.Should().Contain("public sealed class LoyaltyController : AdminBaseController");
        loyaltySource.Should().Contain("public IActionResult Index() => RedirectOrHtmx(nameof(Programs), new { });");
        loyaltySource.Should().Contain("public async Task<IActionResult> Programs(");
        loyaltySource.Should().Contain("public async Task<IActionResult> RewardTiers(Guid loyaltyProgramId");
        loyaltySource.Should().Contain("public async Task<IActionResult> Accounts(");
        loyaltySource.Should().Contain("public async Task<IActionResult> Campaigns(");
        loyaltySource.Should().Contain("public async Task<IActionResult> ScanSessions(");
        loyaltySource.Should().Contain("public async Task<IActionResult> Redemptions(");
        loyaltySource.Should().Contain("public async Task<IActionResult> AccountDetails(Guid id");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateProgram(Guid? businessId = null");
        loyaltySource.Should().Contain("public async Task<IActionResult> EditProgram(Guid id");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateRewardTier(Guid loyaltyProgramId");
        loyaltySource.Should().Contain("public async Task<IActionResult> EditRewardTier(Guid id, Guid loyaltyProgramId");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateAccount(Guid? businessId = null");
        loyaltySource.Should().Contain("public async Task<IActionResult> CreateCampaign(Guid? businessId = null");
        loyaltySource.Should().Contain("public async Task<IActionResult> EditCampaign(Guid id, Guid businessId");
        loyaltySource.Should().Contain("public async Task<IActionResult> AdjustPoints(Guid loyaltyAccountId");
    }


    [Fact]
    public void LoyaltyController_Should_KeepProgramAndRewardTierWorkspaceContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("public async Task<IActionResult> Programs(Guid? businessId = null, int page = 1, int pageSize = 20, LoyaltyProgramQueueFilter filter = LoyaltyProgramQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("var summary = new LoyaltyProgramOpsSummaryVm();");
        source.Should().Contain("var result = await _getProgramsPage.HandleAsync(page, pageSize, businessId.Value, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getProgramsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildProgramFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildProgramPlaybooks(),");
        source.Should().Contain("BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),");
        source.Should().Contain("return RenderProgramsWorkspace(new LoyaltyProgramsListVm");
        source.Should().Contain("public async Task<IActionResult> CreateProgram(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("var vm = new LoyaltyProgramEditVm { BusinessId = businessId ?? Guid.Empty, IsActive = true };");
        source.Should().Contain("return RenderProgramEditor(vm, isCreate: true);");
        source.Should().Contain("public async Task<IActionResult> EditProgram(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getProgramForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"LoyaltyProgramNotFound\");");
        source.Should().Contain("return RenderProgramEditor(vm, isCreate: false);");
        source.Should().Contain("public async Task<IActionResult> RewardTiers(Guid loyaltyProgramId, int page = 1, int pageSize = 20, LoyaltyRewardTierQueueFilter filter = LoyaltyRewardTierQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var program = await _getProgramForEdit.HandleAsync(loyaltyProgramId, ct).ConfigureAwait(false);");
        source.Should().Contain("var result = await _getRewardTiersPage.HandleAsync(loyaltyProgramId, page, pageSize, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getRewardTiersPage.GetSummaryAsync(loyaltyProgramId, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildRewardTierFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildRewardTierPlaybooks(),");
        source.Should().Contain("return RenderRewardTiersWorkspace(new LoyaltyRewardTiersListVm");
        source.Should().Contain("public async Task<IActionResult> CreateRewardTier(Guid loyaltyProgramId, CancellationToken ct = default)");
        source.Should().Contain("AllowSelfRedemption = false");
        source.Should().Contain("return RenderRewardTierEditor(new LoyaltyRewardTierEditVm");
        source.Should().Contain("public async Task<IActionResult> EditRewardTier(Guid id, Guid loyaltyProgramId, CancellationToken ct = default)");
        source.Should().Contain("var tier = await _getRewardTierForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"LoyaltyRewardTierNotFound\");");
        source.Should().Contain("private List<SelectListItem> BuildProgramFilterItems(LoyaltyProgramQueueFilter selected)");
        source.Should().Contain("new(T(\"LoyaltyAllPrograms\"), LoyaltyProgramQueueFilter.All.ToString(), selected == LoyaltyProgramQueueFilter.All)");
        source.Should().Contain("new(T(\"LoyaltyMissingRules\"), LoyaltyProgramQueueFilter.MissingRules.ToString(), selected == LoyaltyProgramQueueFilter.MissingRules)");
        source.Should().Contain("private List<LoyaltyOpsPlaybookVm> BuildProgramPlaybooks()");
        source.Should().Contain("Title = T(\"LoyaltyProgramPlaybookReadinessTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyProgramPlaybookSpendTitle\")");
        source.Should().Contain("private List<SelectListItem> BuildRewardTierFilterItems(LoyaltyRewardTierQueueFilter selected)");
        source.Should().Contain("new(T(\"LoyaltySelfRedemption\"), LoyaltyRewardTierQueueFilter.SelfRedemption.ToString(), selected == LoyaltyRewardTierQueueFilter.SelfRedemption)");
        source.Should().Contain("new(T(\"MissingDescription\"), LoyaltyRewardTierQueueFilter.MissingDescription.ToString(), selected == LoyaltyRewardTierQueueFilter.MissingDescription)");
        source.Should().Contain("private List<LoyaltyOpsPlaybookVm> BuildRewardTierPlaybooks()");
        source.Should().Contain("Title = T(\"LoyaltyRewardTierPlaybookSelfTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyRewardTierPlaybookCatalogTitle\")");
        source.Should().Contain("private IActionResult RenderProgramEditor(LoyaltyProgramEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/_ProgramEditorShell.cshtml\", vm);");
        source.Should().Contain("return isCreate ? View(\"CreateProgram\", vm) : View(\"EditProgram\", vm);");
        source.Should().Contain("private IActionResult RenderProgramsWorkspace(LoyaltyProgramsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/Programs.cshtml\", vm);");
        source.Should().Contain("return View(\"Programs\", vm);");
        source.Should().Contain("private IActionResult RenderRewardTiersWorkspace(LoyaltyRewardTiersListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/RewardTiers.cshtml\", vm);");
        source.Should().Contain("return View(\"RewardTiers\", vm);");
        source.Should().Contain("private IActionResult RenderRewardTierEditor(LoyaltyRewardTierEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/_RewardTierEditorShell.cshtml\", vm);");
        source.Should().Contain("return isCreate ? View(\"CreateRewardTier\", vm) : View(\"EditRewardTier\", vm);");
    }


    [Fact]
    public void LoyaltyController_Should_KeepAccountAndCampaignWorkspaceContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("public async Task<IActionResult> Accounts(Guid? businessId = null, int page = 1, int pageSize = 20, string? q = null, LoyaltyAccountStatus? status = null, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getAccountsPage.HandleAsync(businessId.Value, page, pageSize, q, status, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getAccountsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("Playbooks = BuildAccountPlaybooks(),");
        source.Should().Contain("StatusItems = BuildStatusItems(status),");
        source.Should().Contain("return RenderAccountsWorkspace(new LoyaltyAccountsListVm");
        source.Should().Contain("public async Task<IActionResult> CreateAccount(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("vm.UserOptions = await _referenceData.GetUserOptionsAsync(null, includeEmpty: false, ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderAccountCreateEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Campaigns(Guid? businessId = null, int page = 1, int pageSize = 20, LoyaltyCampaignQueueFilter filter = LoyaltyCampaignQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getCampaigns.HandleAsync(businessId.Value, page, pageSize, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("SetLocalizedErrorMessage(\"LoyaltyCampaignsLoadFailed\", result.Error);");
        source.Should().Contain("var summaryDto = await _getCampaigns.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("FilterItems = BuildCampaignFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildCampaignPlaybooks(),");
        source.Should().Contain("return RenderCampaignsWorkspace(new LoyaltyCampaignsListVm");
        source.Should().Contain("public async Task<IActionResult> CreateCampaign(Guid? businessId = null, CancellationToken ct = default)");
        source.Should().Contain("var vm = new LoyaltyCampaignEditVm { BusinessId = businessId ?? Guid.Empty };");
        source.Should().Contain("return RenderCampaignEditor(vm, isCreate: true);");
        source.Should().Contain("public async Task<IActionResult> EditCampaign(Guid id, Guid businessId, CancellationToken ct = default)");
        source.Should().Contain("var campaign = result.Succeeded ? result.Value?.Items.FirstOrDefault(x => x.Id == id) : null;");
        source.Should().Contain("SetErrorMessage(\"LoyaltyCampaignNotFound\");");
        source.Should().Contain("return RenderCampaignEditor(vm, isCreate: false);");
        source.Should().Contain("private List<SelectListItem> BuildStatusItems(LoyaltyAccountStatus? selected)");
        source.Should().Contain("new(T(\"AllStatuses\"), string.Empty, !selected.HasValue)");
        source.Should().Contain("Enum.GetValues<LoyaltyAccountStatus>()");
        source.Should().Contain(".Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x))");
        source.Should().Contain("private List<LoyaltyOpsPlaybookVm> BuildAccountPlaybooks()");
        source.Should().Contain("Title = T(\"LoyaltyAccountPlaybookSuspendedTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyAccountPlaybookZeroBalanceTitle\")");
        source.Should().Contain("private List<SelectListItem> BuildCampaignFilterItems(LoyaltyCampaignQueueFilter selected)");
        source.Should().Contain("new(T(\"LoyaltyPushEnabled\"), LoyaltyCampaignQueueFilter.PushEnabled.ToString(), selected == LoyaltyCampaignQueueFilter.PushEnabled)");
        source.Should().Contain("private List<SelectListItem> BuildModeItems(LoyaltyScanMode? selected)");
        source.Should().Contain("Enum.GetValues<LoyaltyScanMode>()");
        source.Should().Contain("private List<SelectListItem> BuildScanStatusItems(LoyaltyScanStatus? selected)");
        source.Should().Contain("Enum.GetValues<LoyaltyScanStatus>()");
        source.Should().Contain("private List<SelectListItem> BuildRedemptionStatusItems(LoyaltyRedemptionStatus? selected)");
        source.Should().Contain("Enum.GetValues<LoyaltyRedemptionStatus>()");
        source.Should().Contain(".Select(x => new SelectListItem(T(x.ToString()), x.ToString(), selected == x))");
        source.Should().Contain("private List<LoyaltyOpsPlaybookVm> BuildCampaignPlaybooks()");
        source.Should().Contain("Title = T(\"LoyaltyCampaignPlaybookWindowTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyCampaignPlaybookPushTitle\")");
        source.Should().Contain("private IActionResult RenderAccountCreateEditor(CreateLoyaltyAccountVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/_AccountCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"CreateAccount\", vm);");
        source.Should().Contain("private IActionResult RenderAccountsWorkspace(LoyaltyAccountsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/Accounts.cshtml\", vm);");
        source.Should().Contain("return View(\"Accounts\", vm);");
        source.Should().Contain("private IActionResult RenderCampaignsWorkspace(LoyaltyCampaignsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/Campaigns.cshtml\", vm);");
        source.Should().Contain("return View(\"Campaigns\", vm);");
        source.Should().Contain("private IActionResult RenderCampaignEditor(LoyaltyCampaignEditVm vm, bool isCreate)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/_CampaignEditorShell.cshtml\", vm);");
        source.Should().Contain("return isCreate ? View(\"CreateCampaign\", vm) : View(\"EditCampaign\", vm);");
    }


    [Fact]
    public void LoyaltyController_Should_KeepScanRedemptionAndAccountDetailsContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("public async Task<IActionResult> ScanSessions(");
        source.Should().Contain("var result = await _getScanSessionsPage.HandleAsync(businessId.Value, page, pageSize, q, mode, status, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getScanSessionsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("Playbooks = BuildScanSessionPlaybooks(),");
        source.Should().Contain("ModeItems = BuildModeItems(mode),");
        source.Should().Contain("StatusItems = BuildScanStatusItems(status),");
        source.Should().Contain("FailureReason = x.FailureReason,");
        source.Should().Contain("return RenderScanSessionsWorkspace(new LoyaltyScanSessionsListVm");
        source.Should().Contain("public async Task<IActionResult> Redemptions(");
        source.Should().Contain("var result = await _getRedemptionsPage.HandleAsync(businessId.Value, page, pageSize, q, status, ct).ConfigureAwait(false);");
        source.Should().Contain("var summaryDto = await _getRedemptionsPage.GetSummaryAsync(businessId.Value, ct).ConfigureAwait(false);");
        source.Should().Contain("Playbooks = BuildRedemptionPlaybooks(),");
        source.Should().Contain("StatusItems = BuildRedemptionStatusItems(status),");
        source.Should().Contain("ScanFailureReason = x.ScanFailureReason,");
        source.Should().Contain("return RenderRedemptionsWorkspace(new LoyaltyRedemptionsListVm");
        source.Should().Contain("public async Task<IActionResult> AccountDetails(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var account = await _getAccountForAdmin.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("var transactions = await _getTransactions.HandleAsync(id, 50, ct).ConfigureAwait(false);");
        source.Should().Contain("var redemptions = await _getRedemptions.HandleAsync(id, 50, ct).ConfigureAwait(false);");
        source.Should().Contain("Transactions = transactions.Select(x => new LoyaltyTransactionListItemVm");
        source.Should().Contain("Redemptions = redemptions.Select(x => new LoyaltyRedemptionListItemVm");
        source.Should().Contain("return RenderAccountDetailsWorkspace(new LoyaltyAccountDetailsVm");
        source.Should().Contain("public async Task<IActionResult> AdjustPoints(Guid loyaltyAccountId, CancellationToken ct = default)");
        source.Should().Contain("AccountLabel = $\"{account.UserDisplayName} ({account.UserEmail})\",");
        source.Should().Contain("return RenderAdjustPointsEditor(new AdjustLoyaltyPointsVm");
        source.Should().Contain("public async Task<IActionResult> AdjustPoints(AdjustLoyaltyPointsVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _adjustPoints.HandleAsync(new AdjustLoyaltyPointsDto");
        source.Should().Contain("PerformedByUserId = null,");
        source.Should().Contain("SetSuccessMessage(\"LoyaltyPointsAdjusted\");");
        source.Should().Contain("AddLocalizedModelError(\"LoyaltyPointsAdjustFailed\", ex.Message);");
        source.Should().Contain("return RenderAdjustPointsEditor(vm);");
        source.Should().Contain("private IActionResult RenderAdjustPointsEditor(AdjustLoyaltyPointsVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/_AdjustPointsEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"AdjustPoints\", vm);");
        source.Should().Contain("private IActionResult RenderScanSessionsWorkspace(LoyaltyScanSessionsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/ScanSessions.cshtml\", vm);");
        source.Should().Contain("return View(\"ScanSessions\", vm);");
        source.Should().Contain("private IActionResult RenderRedemptionsWorkspace(LoyaltyRedemptionsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/Redemptions.cshtml\", vm);");
        source.Should().Contain("return View(\"Redemptions\", vm);");
        source.Should().Contain("private IActionResult RenderAccountDetailsWorkspace(LoyaltyAccountDetailsVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Loyalty/AccountDetails.cshtml\", vm);");
        source.Should().Contain("return View(\"AccountDetails\", vm);");
    }


    [Fact]
    public void LoyaltyController_Should_KeepOperationalHelperBuildersAndResultMessagingWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Loyalty", "LoyaltyController.cs"));

        source.Should().Contain("private List<LoyaltyOpsPlaybookVm> BuildScanSessionPlaybooks()");
        source.Should().Contain("Title = T(\"LoyaltyScanPlaybookPendingTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyScanPlaybookFailureTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyScanPlaybookProviderTitle\")");
        source.Should().Contain("private List<LoyaltyOpsPlaybookVm> BuildRedemptionPlaybooks()");
        source.Should().Contain("Title = T(\"LoyaltyRedemptionPlaybookPendingTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyRedemptionPlaybookScanFailureTitle\")");
        source.Should().Contain("Title = T(\"LoyaltyRedemptionPlaybookMobileTitle\")");
        source.Should().Contain("private List<SelectListItem> BuildModeItems(LoyaltyScanMode? selected)");
        source.Should().Contain("new(T(\"LoyaltyAllModes\"), string.Empty, !selected.HasValue)");
        source.Should().Contain("Enum.GetValues<LoyaltyScanMode>()");
        source.Should().Contain("private List<SelectListItem> BuildScanStatusItems(LoyaltyScanStatus? selected)");
        source.Should().Contain("Enum.GetValues<LoyaltyScanStatus>()");
        source.Should().Contain("private List<SelectListItem> BuildRedemptionStatusItems(LoyaltyRedemptionStatus? selected)");
        source.Should().Contain("Enum.GetValues<LoyaltyRedemptionStatus>()");
        source.Should().Contain("public async Task<IActionResult> SuspendAccount(Guid id, byte[]? rowVersion, CancellationToken ct = default)");
        source.Should().Contain("SetLocalizedResultMessage(result.Succeeded, \"LoyaltyAccountSuspended\", \"LoyaltyAccountSuspendFailed\", result.Error);");
        source.Should().Contain("public async Task<IActionResult> ActivateAccount(Guid id, byte[]? rowVersion, CancellationToken ct = default)");
        source.Should().Contain("SetLocalizedResultMessage(result.Succeeded, \"LoyaltyAccountActivated\", \"LoyaltyAccountActivateFailed\", result.Error);");
        source.Should().Contain("public async Task<IActionResult> ConfirmRedemption(Guid redemptionId, Guid businessId, Guid loyaltyAccountId, byte[]? rowVersion, CancellationToken ct = default)");
        source.Should().Contain("SetLocalizedResultMessage(result.Succeeded, \"LoyaltyRedemptionConfirmed\", \"LoyaltyRedemptionConfirmFailed\", result.Error);");
        source.Should().Contain("private void AddLocalizedModelError(string fallbackKey, string? error = null)");
        source.Should().Contain("ModelState.AddModelError(string.Empty, string.IsNullOrWhiteSpace(error) ? T(fallbackKey) : error);");
        source.Should().Contain("private void SetLocalizedResultMessage(bool succeeded, string successKey, string failureKey, string? error = null)");
        source.Should().Contain("SetSuccessMessage(successKey);");
        source.Should().Contain("SetLocalizedErrorMessage(failureKey, error);");
    }


    [Fact]
    public void ShippingMethodsController_Should_KeepWorkspaceAndEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Shipping", "ShippingMethodsController.cs"));

        source.Should().Contain("public sealed class ShippingMethodsController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(");
        source.Should().Contain("var (items, total) = await _getPage.HandleAsync(page, pageSize, query, filter, ct);");
        source.Should().Contain("var summary = await _getSummary.HandleAsync(ct);");
        source.Should().Contain("Playbooks = BuildPlaybooks(),");
        source.Should().Contain("FilterItems = BuildFilterItems(filter),");
        source.Should().Contain("PageSizeItems = BuildPageSizeItems(pageSize),");
        source.Should().Contain("HasGlobalCoverage = x.HasGlobalCoverage,");
        source.Should().Contain("HasMultipleRates = x.HasMultipleRates,");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("public IActionResult Create()");
        source.Should().Contain("var defaultCurrency = _siteSettingCache.GetAsync().GetAwaiter().GetResult().DefaultCurrency;");
        source.Should().Contain("Rates = new List<ShippingRateEditVm> { new() { SortOrder = 0 } }");
        source.Should().Contain("return RenderEditor(vm, true);");
        source.Should().Contain("public async Task<IActionResult> Create(ShippingMethodEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("EnsureRates(vm);");
        source.Should().Contain("var dto = new ShippingMethodCreateDto");
        source.Should().Contain("Rates = vm.Rates.Select(MapRateDto).ToList()");
        source.Should().Contain("SetSuccessMessage(\"ShippingMethodCreatedMessage\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getForEdit.HandleAsync(id, ct);");
        source.Should().Contain("SetErrorMessage(\"ShippingMethodNotFoundMessage\");");
        source.Should().Contain("Rates = dto.Rates.Select(x => new ShippingRateEditVm");
        source.Should().Contain("EnsureRates(vm);");
        source.Should().Contain("return RenderEditor(vm, false);");
        source.Should().Contain("public async Task<IActionResult> Edit(ShippingMethodEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new ShippingMethodEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        source.Should().Contain("SetSuccessMessage(\"ShippingMethodUpdatedMessage\");");
        source.Should().Contain("SetErrorMessage(\"ShippingMethodConcurrencyMessage\");");
        source.Should().Contain("private IActionResult RenderEditor(ShippingMethodEditVm vm, bool isCreate)");
        source.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        source.Should().Contain("return PartialView(\"~/Views/ShippingMethods/_ShippingMethodEditorShell.cshtml\", vm);");
        source.Should().Contain("return isCreate ? View(\"Create\", vm) : View(\"Edit\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(ShippingMethodsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/ShippingMethods/Index.cshtml\", vm);");
        source.Should().Contain("return View(\"Index\", vm);");
    }


    [Fact]
    public void ShippingMethodsController_Should_KeepRateAndQueueHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Shipping", "ShippingMethodsController.cs"));

        source.Should().Contain("private static ShippingRateDto MapRateDto(ShippingRateEditVm vm)");
        source.Should().Contain("MaxShipmentMass = vm.MaxShipmentMass,");
        source.Should().Contain("MaxSubtotalNetMinor = vm.MaxSubtotalNetMinor,");
        source.Should().Contain("PriceMinor = vm.PriceMinor,");
        source.Should().Contain("SortOrder = vm.SortOrder");
        source.Should().Contain("private static void EnsureRates(ShippingMethodEditVm vm)");
        source.Should().Contain("vm.Rates ??= new List<ShippingRateEditVm>();");
        source.Should().Contain(".Where(x => x.MaxShipmentMass.HasValue || x.MaxSubtotalNetMinor.HasValue || x.PriceMinor > 0 || x.Id.HasValue)");
        source.Should().Contain(".OrderBy(x => x.SortOrder)");
        source.Should().Contain("vm.Rates.Add(new ShippingRateEditVm { SortOrder = 0 });");
        source.Should().Contain("vm.Rates[i].SortOrder = i;");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildFilterItems(ShippingMethodQueueFilter selected)");
        source.Should().Contain("new SelectListItem(T(\"AllMethods\"), ShippingMethodQueueFilter.All.ToString(), selected == ShippingMethodQueueFilter.All)");
        source.Should().Contain("new SelectListItem(T(\"MissingRates\"), ShippingMethodQueueFilter.MissingRates.ToString(), selected == ShippingMethodQueueFilter.MissingRates)");
        source.Should().Contain("new SelectListItem(T(\"DhlMethods\"), ShippingMethodQueueFilter.Dhl.ToString(), selected == ShippingMethodQueueFilter.Dhl)");
        source.Should().Contain("private List<ShippingMethodPlaybookVm> BuildPlaybooks()");
        source.Should().Contain("Title = T(\"MissingRates\")");
        source.Should().Contain("Title = T(\"GlobalCoverage\")");
        source.Should().Contain("Title = T(\"DhlMethods\")");
        source.Should().Contain("private static IEnumerable<SelectListItem> BuildPageSizeItems(int selected)");
        source.Should().Contain("var sizes = new[] { 10, 20, 50, 100 };");
        source.Should().Contain("return sizes.Select(x => new SelectListItem(x.ToString(), x.ToString(), x == selected)).ToList();");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
    }


    [Fact]
    public void ShippingMethodsWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "ShippingMethods", "Index.cshtml"));

        source.Should().Contain("id=\"shipping-methods-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"ShippingMethods\")");
        source.Should().Contain("@T.T(\"ShippingMethodsWorkspaceIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"ShippingMethods\")\"");
        source.Should().Contain("@T.T(\"CreateMethod\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.ActiveCount");
        source.Should().Contain("@Model.Summary.InactiveCount");
        source.Should().Contain("@Model.Summary.MissingRatesCount");
        source.Should().Contain("@Model.Summary.DhlCount");
        source.Should().Contain("@Model.Summary.GlobalCoverageCount");
        source.Should().Contain("asp-route-filter=\"MissingRates\"");
        source.Should().Contain("asp-route-filter=\"Dhl\"");
        source.Should().Contain("asp-route-filter=\"Inactive\"");
        source.Should().Contain("asp-route-filter=\"GlobalCoverage\"");
        source.Should().Contain("asp-route-filter=\"MultiRate\"");
        source.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("name=\"pageSize\" asp-items=\"Model.PageSizeItems\"");
        source.Should().Contain("@T.T(\"SearchShippingMethodsPlaceholder\")");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("@T.T(\"ShippingMethodPlaybooks\")");
        source.Should().Contain("@item.Title");
        source.Should().Contain("@item.ScopeNote");
        source.Should().Contain("@T.T(\"OperatorAction\"):");
        source.Should().Contain("@T.T(\"CarrierService\")");
        source.Should().Contain("@T.T(\"Countries\")");
        source.Should().Contain("@T.T(\"Currency\")");
        source.Should().Contain("@T.T(\"Rates\")");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"Updated\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoShippingMethodsFound\")");
        source.Should().Contain("T.T(\"All\") : item.CountriesCsv");
        source.Should().Contain("item.RatesCount > 0 ? \"text-bg-success\" : \"text-bg-warning\"");
        source.Should().Contain("item.IsActive ? \"text-bg-success\" : \"text-bg-secondary\"");
        source.Should().Contain("item.IsActive ? T.T(\"Active\") : T.T(\"Inactive\")");
        source.Should().Contain("@T.T(\"DhlMethods\")");
        source.Should().Contain("@T.T(\"GlobalCoverage\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"ShippingMethods\", new { id = item.Id })\"");
        source.Should().Contain("asp-controller=\"ShippingMethods\"");
        source.Should().Contain("asp-route-query=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
    }


    [Fact]
    public void PurchaseOrdersWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Inventory", "PurchaseOrders.cshtml"));

        source.Should().Contain("id=\"inventory-purchase-orders-workspace-shell\"");
        source.Should().Contain("@T.T(\"PurchaseOrders\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"PurchaseOrders\", \"Inventory\")\"");
        source.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        source.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("@T.T(\"SearchPurchaseOrdersPlaceholder\")");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreatePurchaseOrder\", \"Inventory\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"NewPurchaseOrder\")");
        source.Should().Contain("asp-route-filter=\"Draft\"");
        source.Should().Contain("asp-route-filter=\"Issued\"");
        source.Should().Contain("asp-route-filter=\"Received\"");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.DraftCount");
        source.Should().Contain("@Model.Summary.IssuedCount");
        source.Should().Contain("@Model.Summary.ReceivedCount");
        source.Should().Contain("@T.T(\"PurchaseOrderOpsPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("@T.T(\"OrderNumber\")");
        source.Should().Contain("@T.T(\"Supplier\")");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"Lines\")");
        source.Should().Contain("@T.T(\"Ordered\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoPurchaseOrdersFound\")");
        source.Should().Contain("T.T(\"Issued\")");
        source.Should().Contain("T.T(\"Received\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditPurchaseOrder\", \"Inventory\", new { id = item.Id })\"");
        source.Should().Contain("asp-controller=\"Inventory\"");
        source.Should().Contain("asp-action=\"PurchaseOrders\"");
        source.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
    }


    [Fact]
    public void InventoryAdjustmentShells_Should_KeepAdjustAndReserveContractsWired()
    {
        var adjustSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_AdjustStockEditorShell.cshtml"));
        var reserveSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_ReserveStockEditorShell.cshtml"));

        adjustSource.Should().Contain("id=\"inventory-adjust-stock-editor-shell\"");
        adjustSource.Should().Contain("@T.T(\"InventoryAdjustStockTitle\")");
        adjustSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger mb-3\"");
        adjustSource.Should().Contain("@T.T(\"Warehouse\")");
        adjustSource.Should().Contain("@T.T(\"InventoryVariantLabel\")");
        adjustSource.Should().Contain("@T.T(\"InventoryAvailableLabel\")");
        adjustSource.Should().Contain("@T.T(\"Reserved\")");
        adjustSource.Should().Contain("hx-post=\"@Url.Action(\"AdjustStock\", \"Inventory\")\"");
        adjustSource.Should().Contain("@Html.AntiForgeryToken()");
        adjustSource.Should().Contain("input asp-for=\"StockLevelId\" type=\"hidden\"");
        adjustSource.Should().Contain("input asp-for=\"BusinessId\" type=\"hidden\"");
        adjustSource.Should().Contain("input asp-for=\"WarehouseId\" type=\"hidden\"");
        adjustSource.Should().Contain("input asp-for=\"ProductVariantId\" type=\"hidden\"");
        adjustSource.Should().Contain("input asp-for=\"AvailableQuantity\" type=\"hidden\"");
        adjustSource.Should().Contain("input asp-for=\"ReservedQuantity\" type=\"hidden\"");
        adjustSource.Should().Contain("asp-for=\"QuantityDelta\" class=\"form-control\"");
        adjustSource.Should().Contain("@T.T(\"InventoryAdjustQuantityHelp\")");
        adjustSource.Should().Contain("asp-for=\"Reason\" class=\"form-control\"");
        adjustSource.Should().Contain("asp-for=\"ReferenceId\" class=\"form-control\"");
        adjustSource.Should().Contain("@T.T(\"InventoryReferenceIdAuditHelp\")");
        adjustSource.Should().Contain("@T.T(\"InventoryApplyAdjustment\")");
        adjustSource.Should().Contain("hx-get=\"@Url.Action(\"StockLevels\", \"Inventory\", new { businessId = Model.BusinessId, warehouseId = Model.WarehouseId })\"");
        adjustSource.Should().Contain("@T.T(\"InventoryBackToStockLevels\")");

        reserveSource.Should().Contain("id=\"inventory-reserve-stock-editor-shell\"");
        reserveSource.Should().Contain("@T.T(\"InventoryReserveStockTitle\")");
        reserveSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger mb-3\"");
        reserveSource.Should().Contain("@T.T(\"Warehouse\")");
        reserveSource.Should().Contain("@T.T(\"InventoryVariantLabel\")");
        reserveSource.Should().Contain("@T.T(\"InventoryAvailableLabel\")");
        reserveSource.Should().Contain("@T.T(\"Reserved\")");
        reserveSource.Should().Contain("hx-post=\"@Url.Action(\"ReserveStock\", \"Inventory\")\"");
        reserveSource.Should().Contain("@Html.AntiForgeryToken()");
        reserveSource.Should().Contain("input asp-for=\"StockLevelId\" type=\"hidden\"");
        reserveSource.Should().Contain("input asp-for=\"BusinessId\" type=\"hidden\"");
        reserveSource.Should().Contain("input asp-for=\"WarehouseId\" type=\"hidden\"");
        reserveSource.Should().Contain("input asp-for=\"ProductVariantId\" type=\"hidden\"");
        reserveSource.Should().Contain("input asp-for=\"AvailableQuantity\" type=\"hidden\"");
        reserveSource.Should().Contain("input asp-for=\"ReservedQuantity\" type=\"hidden\"");
        reserveSource.Should().Contain("asp-for=\"Quantity\" class=\"form-control\"");
        reserveSource.Should().Contain("asp-for=\"Reason\" class=\"form-control\"");
        reserveSource.Should().Contain("asp-for=\"ReferenceId\" class=\"form-control\"");
        reserveSource.Should().Contain("@T.T(\"InventoryReferenceIdAuditHelp\")");
        reserveSource.Should().Contain("class=\"btn btn-warning\">@T.T(\"InventoryReserveStockTitle\")</button>");
        reserveSource.Should().Contain("hx-get=\"@Url.Action(\"StockLevels\", \"Inventory\", new { businessId = Model.BusinessId, warehouseId = Model.WarehouseId })\"");
        reserveSource.Should().Contain("@T.T(\"InventoryBackToStockLevels\")");
    }


    [Fact]
    public void InventoryReservationAndReturnShells_Should_KeepReleaseAndReturnContractsWired()
    {
        var releaseSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_ReleaseReservationEditorShell.cshtml"));
        var returnSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "_ReturnReceiptEditorShell.cshtml"));

        releaseSource.Should().Contain("id=\"inventory-release-reservation-editor-shell\"");
        releaseSource.Should().Contain("@T.T(\"InventoryReleaseReservationTitle\")");
        releaseSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger mb-3\"");
        releaseSource.Should().Contain("@T.T(\"Warehouse\")");
        releaseSource.Should().Contain("@T.T(\"InventoryVariantLabel\")");
        releaseSource.Should().Contain("@T.T(\"InventoryAvailableLabel\")");
        releaseSource.Should().Contain("@T.T(\"Reserved\")");
        releaseSource.Should().Contain("hx-post=\"@Url.Action(\"ReleaseReservation\", \"Inventory\")\"");
        releaseSource.Should().Contain("@Html.AntiForgeryToken()");
        releaseSource.Should().Contain("input asp-for=\"StockLevelId\" type=\"hidden\"");
        releaseSource.Should().Contain("input asp-for=\"BusinessId\" type=\"hidden\"");
        releaseSource.Should().Contain("input asp-for=\"WarehouseId\" type=\"hidden\"");
        releaseSource.Should().Contain("input asp-for=\"ProductVariantId\" type=\"hidden\"");
        releaseSource.Should().Contain("input asp-for=\"AvailableQuantity\" type=\"hidden\"");
        releaseSource.Should().Contain("input asp-for=\"ReservedQuantity\" type=\"hidden\"");
        releaseSource.Should().Contain("asp-for=\"Quantity\" class=\"form-control\"");
        releaseSource.Should().Contain("@T.T(\"InventoryCurrentReservedQuantity\"): @Model.ReservedQuantity");
        releaseSource.Should().Contain("asp-for=\"Reason\" class=\"form-control\"");
        releaseSource.Should().Contain("asp-for=\"ReferenceId\" class=\"form-control\"");
        releaseSource.Should().Contain("@T.T(\"InventoryReferenceIdAuditHelp\")");
        releaseSource.Should().Contain("class=\"btn btn-info\">@T.T(\"InventoryReleaseReservationTitle\")</button>");
        releaseSource.Should().Contain("hx-get=\"@Url.Action(\"StockLevels\", \"Inventory\", new { businessId = Model.BusinessId, warehouseId = Model.WarehouseId })\"");
        releaseSource.Should().Contain("@T.T(\"InventoryBackToStockLevels\")");

        returnSource.Should().Contain("id=\"inventory-return-receipt-editor-shell\"");
        returnSource.Should().Contain("@T.T(\"InventoryProcessReturnReceiptTitle\")");
        returnSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger mb-3\"");
        returnSource.Should().Contain("@T.T(\"Warehouse\")");
        returnSource.Should().Contain("@T.T(\"InventoryVariantLabel\")");
        returnSource.Should().Contain("@T.T(\"InventoryAvailableLabel\")");
        returnSource.Should().Contain("@T.T(\"Reserved\")");
        returnSource.Should().Contain("hx-post=\"@Url.Action(\"ReturnReceipt\", \"Inventory\")\"");
        returnSource.Should().Contain("@Html.AntiForgeryToken()");
        returnSource.Should().Contain("input asp-for=\"StockLevelId\" type=\"hidden\"");
        returnSource.Should().Contain("input asp-for=\"BusinessId\" type=\"hidden\"");
        returnSource.Should().Contain("input asp-for=\"WarehouseId\" type=\"hidden\"");
        returnSource.Should().Contain("input asp-for=\"ProductVariantId\" type=\"hidden\"");
        returnSource.Should().Contain("input asp-for=\"AvailableQuantity\" type=\"hidden\"");
        returnSource.Should().Contain("input asp-for=\"ReservedQuantity\" type=\"hidden\"");
        returnSource.Should().Contain("asp-for=\"Quantity\" class=\"form-control\"");
        returnSource.Should().Contain("asp-for=\"Reason\" class=\"form-control\"");
        returnSource.Should().Contain("asp-for=\"ReferenceId\" class=\"form-control\"");
        returnSource.Should().Contain("@T.T(\"InventoryReturnReferenceHelp\")");
        returnSource.Should().Contain("class=\"btn btn-dark\">@T.T(\"InventoryProcessReturn\")</button>");
        returnSource.Should().Contain("hx-get=\"@Url.Action(\"StockLevels\", \"Inventory\", new { businessId = Model.BusinessId, warehouseId = Model.WarehouseId })\"");
        returnSource.Should().Contain("@T.T(\"InventoryBackToStockLevels\")");
    }


    [Fact]
    public void LoyaltyProgramsWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "Programs.cshtml"));

        source.Should().Contain("id=\"loyalty-programs-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"LoyaltyProgramsTitle\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Programs\", \"Loyalty\")\"");
        source.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Campaigns\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"Campaigns\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"ScanSessions\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"LoyaltyScanSessionsTitle\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateProgram\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"LoyaltyNewProgram\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.ActiveCount");
        source.Should().Contain("@Model.Summary.InactiveCount");
        source.Should().Contain("@Model.Summary.PerCurrencyUnitCount");
        source.Should().Contain("@Model.Summary.MissingRulesCount");
        source.Should().Contain("@T.T(\"LoyaltyProgramPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("asp-route-filter=\"Active\"");
        source.Should().Contain("asp-route-filter=\"Inactive\"");
        source.Should().Contain("asp-route-filter=\"PerCurrencyUnit\"");
        source.Should().Contain("asp-route-filter=\"MissingRules\"");
        source.Should().Contain("@T.T(\"LoyaltyClearQueueFilters\")");
        source.Should().Contain("@T.T(\"Name\")");
        source.Should().Contain("@T.T(\"LoyaltyAccrual\")");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"Updated\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"LoyaltyNoProgramsFound\")");
        source.Should().Contain("item.IsActive ? T.T(\"Active\") : T.T(\"Inactive\")");
        source.Should().Contain("@T.T(\"LoyaltySpendBased\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"RewardTiers\", \"Loyalty\", new { loyaltyProgramId = item.Id })\"");
        source.Should().Contain("@T.T(\"LoyaltyRewards\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditProgram\", \"Loyalty\", new { id = item.Id })\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"DeleteProgram\", \"Loyalty\")\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("name=\"rowVersion\" value=\"@Convert.ToBase64String(item.RowVersion)\"");
        source.Should().Contain("asp-controller=\"Loyalty\"");
        source.Should().Contain("asp-action=\"Programs\"");
        source.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
    }


    [Fact]
    public void LoyaltyProgramEditorShellAndForm_Should_KeepCreateEditAndRewardTierHandoffContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_ProgramEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_ProgramForm.cshtml"));

        shellSource.Should().Contain("id=\"loyalty-program-editor-shell\"");
        shellSource.Should().Contain("ViewData[\"IsCreate\"] as bool? == true");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"LoyaltyCreateProgram\") : T.T(\"LoyaltyEditProgram\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"LoyaltyCreateProgram\") : T.T(\"LoyaltyEditProgram\"))");
        shellSource.Should().Contain("<partial name=\"_ProgramForm\" model=\"Model\" />");

        formSource.Should().Contain("asp-action=\"@(ViewData[\"IsCreate\"] as bool? == true ? \"CreateProgram\" : \"EditProgram\")\"");
        formSource.Should().Contain("hx-post=\"@Url.Action(ViewData[\"IsCreate\"] as bool? == true ? \"CreateProgram\" : \"EditProgram\", \"Loyalty\")\"");
        formSource.Should().Contain("@Html.AntiForgeryToken()");
        formSource.Should().Contain("<input asp-for=\"Id\" type=\"hidden\" />");
        formSource.Should().Contain("<input asp-for=\"RowVersion\" type=\"hidden\" />");
        formSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger\"");
        formSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Name\" class=\"form-control\"");
        formSource.Should().Contain("var accrualModeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.LoyaltyAccrualMode>().Select");
        formSource.Should().Contain("Text = T.T(option.Text)");
        formSource.Should().Contain("asp-for=\"AccrualMode\" asp-items=\"accrualModeOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"PointsPerCurrencyUnit\" class=\"form-control\"");
        formSource.Should().Contain("textarea asp-for=\"RulesJson\" class=\"form-control\" rows=\"6\"></textarea>");
        formSource.Should().Contain("@T.T(\"LoyaltyRulesJsonHelp\")");
        formSource.Should().Contain("asp-for=\"IsActive\" class=\"form-check-input\"");
        formSource.Should().Contain("class=\"btn btn-primary\">@(ViewData[\"IsCreate\"] as bool? == true ? T.T(\"Create\") : T.T(\"Save\"))</button>");
        formSource.Should().Contain("hx-get=\"@Url.Action(\"Programs\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        formSource.Should().Contain("@T.T(\"Back\")");
        formSource.Should().Contain("hx-get=\"@Url.Action(\"RewardTiers\", \"Loyalty\", new { loyaltyProgramId = Model.Id })\"");
        formSource.Should().Contain("@T.T(\"LoyaltyRewardTiersTitle\")");
    }

    [Fact]
    public void LoyaltyCampaignEditorForm_Should_KeepLocalizedReadonlyCampaignStateContractsWired()
    {
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_CampaignForm.cshtml"));

        formSource.Should().Contain("string LocalizeCampaignState(object? state) => state is null ? \"-\" : T.T(state.ToString() ?? string.Empty);");
        formSource.Should().Contain("<input asp-for=\"CampaignState\" type=\"hidden\" />");
        formSource.Should().Contain("<input value=\"@LocalizeCampaignState(Model.CampaignState)\" class=\"form-control\" readonly />");
    }


    [Fact]
    public void LoyaltyRewardTiersWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "RewardTiers.cshtml"));
        var sharedResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.resx"));
        var sharedGermanResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.de-DE.resx"));

        source.Should().Contain("id=\"loyalty-reward-tiers-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("string LocalizeRewardType(object? rewardType) => rewardType is null ? \"-\" : T.T(rewardType.ToString() ?? string.Empty);");
        source.Should().Contain("@T.T(\"LoyaltyRewardTiersTitle\")");
        source.Should().Contain("@Model.ProgramName");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditProgram\", \"Loyalty\", new { id = Model.LoyaltyProgramId })\"");
        source.Should().Contain("@T.T(\"LoyaltyProgram\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateRewardTier\", \"Loyalty\", new { loyaltyProgramId = Model.LoyaltyProgramId })\"");
        source.Should().Contain("@T.T(\"LoyaltyNewRewardTier\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.SelfRedemptionCount");
        source.Should().Contain("@Model.Summary.MissingDescriptionCount");
        source.Should().Contain("@Model.Summary.DiscountRewardCount");
        source.Should().Contain("@Model.Summary.FreeItemCount");
        source.Should().Contain("@T.T(\"LoyaltyRewardTierPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("hx-get=\"@Url.Action(\"RewardTiers\", \"Loyalty\")\"");
        source.Should().Contain("name=\"loyaltyProgramId\" value=\"@Model.LoyaltyProgramId\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("asp-route-filter=\"SelfRedemption\"");
        source.Should().Contain("asp-route-filter=\"MissingDescription\"");
        source.Should().Contain("asp-route-filter=\"DiscountRewards\"");
        source.Should().Contain("asp-route-filter=\"FreeItem\"");
        source.Should().Contain("@T.T(\"LoyaltyClearQueueFilters\")");
        source.Should().Contain("@T.T(\"LoyaltyPoints\")");
        source.Should().Contain("@T.T(\"Type\")");
        source.Should().Contain("@T.T(\"Value\")");
        source.Should().Contain("@T.T(\"LoyaltySelfRedeem\")");
        source.Should().Contain("@T.T(\"Description\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"LoyaltyNoRewardTiersFound\")");
        source.Should().Contain("@LocalizeRewardType(item.RewardType)");
        source.Should().Contain("item.AllowSelfRedemption ? T.T(\"Enabled\") : T.T(\"LoyaltyManual\")");
        source.Should().Contain("@T.T(\"LoyaltyMissingDescription\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditRewardTier\", \"Loyalty\", new { id = item.Id, loyaltyProgramId = item.LoyaltyProgramId })\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"DeleteRewardTier\", \"Loyalty\")\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("name=\"rowVersion\" value=\"@Convert.ToBase64String(item.RowVersion)\"");
        source.Should().Contain("asp-controller=\"Loyalty\"");
        source.Should().Contain("asp-action=\"RewardTiers\"");
        source.Should().Contain("asp-route-loyaltyProgramId=\"@Model.LoyaltyProgramId\"");

        sharedResourceSource.Should().Contain("<data name=\"FreeItem\"");
        sharedResourceSource.Should().Contain("<data name=\"PercentDiscount\"");
        sharedResourceSource.Should().Contain("<data name=\"AmountDiscount\"");
        sharedGermanResourceSource.Should().Contain("<data name=\"FreeItem\"");
        sharedGermanResourceSource.Should().Contain("<data name=\"PercentDiscount\"");
        sharedGermanResourceSource.Should().Contain("<data name=\"AmountDiscount\"");
    }

    [Fact]
    public void LoyaltyProgramsWorkspace_Should_KeepLocalizedAccrualModeContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "Programs.cshtml"));
        var sharedResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.resx"));
        var sharedGermanResourceSource = ReadWebAdminFile(Path.Combine("Resources", "SharedResource.de-DE.resx"));

        source.Should().Contain("string LocalizeAccrualMode(object? accrualMode) => accrualMode is null ? \"-\" : T.T(accrualMode.ToString() ?? string.Empty);");
        source.Should().Contain("@LocalizeAccrualMode(item.AccrualMode)");
        source.Should().Contain("item.AccrualMode == Darwin.Domain.Enums.LoyaltyAccrualMode.PerCurrencyUnit");

        sharedResourceSource.Should().Contain("<data name=\"PerVisit\"");
        sharedResourceSource.Should().Contain("<data name=\"PerCurrencyUnit\"");
        sharedResourceSource.Should().Contain("<data name=\"AmountBased\"");
        sharedGermanResourceSource.Should().Contain("<data name=\"PerVisit\"");
        sharedGermanResourceSource.Should().Contain("<data name=\"PerCurrencyUnit\"");
        sharedGermanResourceSource.Should().Contain("<data name=\"AmountBased\"");
    }


    [Fact]
    public void LoyaltyRewardTierEditorShellAndForm_Should_KeepCreateEditAndProgramContextContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_RewardTierEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_RewardTierForm.cshtml"));

        shellSource.Should().Contain("id=\"loyalty-reward-tier-editor-shell\"");
        shellSource.Should().Contain("ViewData[\"IsCreate\"] as bool? == true");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"LoyaltyCreateRewardTier\") : T.T(\"LoyaltyEditRewardTier\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"LoyaltyCreateRewardTier\") : T.T(\"LoyaltyEditRewardTier\"))");
        shellSource.Should().Contain("@Model.ProgramName");
        shellSource.Should().Contain("<partial name=\"_RewardTierForm\" model=\"Model\" />");

        formSource.Should().Contain("asp-action=\"@(ViewData[\"IsCreate\"] as bool? == true ? \"CreateRewardTier\" : \"EditRewardTier\")\"");
        formSource.Should().Contain("hx-post=\"@Url.Action(ViewData[\"IsCreate\"] as bool? == true ? \"CreateRewardTier\" : \"EditRewardTier\", \"Loyalty\")\"");
        formSource.Should().Contain("@Html.AntiForgeryToken()");
        formSource.Should().Contain("<input asp-for=\"Id\" type=\"hidden\" />");
        formSource.Should().Contain("<input asp-for=\"RowVersion\" type=\"hidden\" />");
        formSource.Should().Contain("<input asp-for=\"LoyaltyProgramId\" type=\"hidden\" />");
        formSource.Should().Contain("<input asp-for=\"BusinessId\" type=\"hidden\" />");
        formSource.Should().Contain("<input asp-for=\"ProgramName\" type=\"hidden\" />");
        formSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger\"");
        formSource.Should().Contain("asp-for=\"PointsRequired\" class=\"form-control\"");
        formSource.Should().Contain("var rewardTypeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.LoyaltyRewardType>().Select");
        formSource.Should().Contain("Text = T.T(option.Text)");
        formSource.Should().Contain("asp-for=\"RewardType\" asp-items=\"rewardTypeOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"RewardValue\" class=\"form-control\"");
        formSource.Should().Contain("textarea asp-for=\"Description\" class=\"form-control\" rows=\"3\"></textarea>");
        formSource.Should().Contain("textarea asp-for=\"MetadataJson\" class=\"form-control\" rows=\"4\"></textarea>");
        formSource.Should().Contain("@T.T(\"LoyaltyRewardMetadataHelp\")");
        formSource.Should().Contain("asp-for=\"AllowSelfRedemption\" class=\"form-check-input\"");
        formSource.Should().Contain("class=\"btn btn-primary\">@(ViewData[\"IsCreate\"] as bool? == true ? T.T(\"Create\") : T.T(\"Save\"))</button>");
        formSource.Should().Contain("hx-get=\"@Url.Action(\"RewardTiers\", \"Loyalty\", new { loyaltyProgramId = Model.LoyaltyProgramId })\"");
        formSource.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void LoyaltyAccountsWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "Accounts.cshtml"));

        source.Should().Contain("id=\"loyalty-accounts-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"LoyaltyAccountsTitle\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Accounts\", \"Loyalty\")\"");
        source.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        source.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("@T.T(\"LoyaltySearchByMember\")");
        source.Should().Contain("name=\"status\" asp-items=\"Model.StatusItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateAccount\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"LoyaltyCreateAccount\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Programs\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"Programs\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Campaigns\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"Campaigns\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Redemptions\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"Redemptions\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"ScanSessions\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"LoyaltyScanSessionsTitle\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.ActiveCount");
        source.Should().Contain("@Model.Summary.SuspendedCount");
        source.Should().Contain("@Model.Summary.ZeroBalanceCount");
        source.Should().Contain("@Model.Summary.RecentAccrualCount");
        source.Should().Contain("@T.T(\"LoyaltyAccountPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("asp-route-status=\"@Darwin.Domain.Enums.LoyaltyAccountStatus.Active\"");
        source.Should().Contain("asp-route-status=\"@Darwin.Domain.Enums.LoyaltyAccountStatus.Suspended\"");
        source.Should().Contain("string LocalizeAccountStatus(Darwin.Domain.Enums.LoyaltyAccountStatus status) => T.T(status.ToString());");
        source.Should().Contain("@T.T(\"Member\")");
        source.Should().Contain("@T.T(\"Status\")");
        source.Should().Contain("@T.T(\"Points\")");
        source.Should().Contain("@T.T(\"Lifetime\")");
        source.Should().Contain("@T.T(\"LastAccrual\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"LoyaltyNoAccountsFound\")");
        source.Should().Contain("@LocalizeAccountStatus(item.Status)");
        source.Should().Contain("hx-get=\"@Url.Action(\"AccountDetails\", \"Loyalty\", new { id = item.Id })\"");
        source.Should().Contain("@T.T(\"Details\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"AdjustPoints\", \"Loyalty\", new { loyaltyAccountId = item.Id })\"");
        source.Should().Contain("@T.T(\"Adjust\")");
        source.Should().Contain("asp-controller=\"Loyalty\"");
        source.Should().Contain("asp-action=\"Accounts\"");
        source.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-status=\"@Model.StatusFilter\"");
    }


    [Fact]
    public void LoyaltyAccountCreateAndDetailsSurfaces_Should_KeepCreateDetailsAndMutationContractsWired()
    {
        var createShellSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_AccountCreateEditorShell.cshtml"));
        var createFormSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_AccountCreateForm.cshtml"));
        var detailsSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "AccountDetails.cshtml"));

        createShellSource.Should().Contain("id=\"loyalty-account-create-editor-shell\"");
        createShellSource.Should().Contain("@T.T(\"LoyaltyCreateAccountTitle\")");
        createShellSource.Should().Contain("@T.T(\"LoyaltyCreateAccountIntro\")");
        createShellSource.Should().Contain("<partial name=\"_AccountCreateForm\" model=\"Model\" />");

        createFormSource.Should().Contain("hx-post=\"@Url.Action(\"CreateAccount\", \"Loyalty\")\"");
        createFormSource.Should().Contain("@Html.AntiForgeryToken()");
        createFormSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger\"");
        createFormSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        createFormSource.Should().Contain("@T.T(\"Business\")");
        createFormSource.Should().Contain("asp-for=\"UserId\" asp-items=\"Model.UserOptions\" class=\"form-select\"");
        createFormSource.Should().Contain("@T.T(\"Member\")");
        createFormSource.Should().Contain("@T.T(\"LoyaltyCreateAccountHelp\")");
        createFormSource.Should().Contain("@T.T(\"LoyaltyCreateAccount\")");
        createFormSource.Should().Contain("hx-get=\"@Url.Action(\"Accounts\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        createFormSource.Should().Contain("@T.T(\"Cancel\")");

        detailsSource.Should().Contain("id=\"loyalty-account-workspace-shell\"");
        detailsSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        detailsSource.Should().Contain("@T.T(\"LoyaltyAccountTitle\")");
        detailsSource.Should().Contain("@Model.UserDisplayName (@Model.UserEmail)");
        detailsSource.Should().Contain("hx-get=\"@Url.Action(\"Redemptions\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        detailsSource.Should().Contain("@T.T(\"Redemptions\")");
        detailsSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\", new { q = Model.UserEmail })\"");
        detailsSource.Should().Contain("@T.T(\"LoyaltyMobileOps\")");
        detailsSource.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { providerReviewOnly = true })\"");
        detailsSource.Should().Contain("@T.T(\"ProviderReview\")");
        detailsSource.Should().Contain("hx-get=\"@Url.Action(\"AdjustPoints\", \"Loyalty\", new { loyaltyAccountId = Model.Id })\"");
        detailsSource.Should().Contain("@T.T(\"LoyaltyAdjustPoints\")");
        detailsSource.Should().Contain("hx-post=\"@Url.Action(\"SuspendAccount\", \"Loyalty\")\"");
        detailsSource.Should().Contain("@T.T(\"Suspend\")");
        detailsSource.Should().Contain("hx-post=\"@Url.Action(\"ActivateAccount\", \"Loyalty\")\"");
        detailsSource.Should().Contain("@T.T(\"Activate\")");
        detailsSource.Should().Contain("name=\"rowVersion\" value=\"@Convert.ToBase64String(Model.RowVersion)\"");
        detailsSource.Should().Contain("hx-get=\"@Url.Action(\"Accounts\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        detailsSource.Should().Contain("@T.T(\"Back\")");
        detailsSource.Should().Contain("string LocalizeAccountStatus(Darwin.Domain.Enums.LoyaltyAccountStatus status) => T.T(status.ToString());");
        detailsSource.Should().Contain("string LocalizeRedemptionStatus(Darwin.Domain.Enums.LoyaltyRedemptionStatus status) => T.T(status.ToString());");
        detailsSource.Should().Contain("string LocalizeScanStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        detailsSource.Should().Contain("string LocalizeScanOutcome(string? outcome) => string.IsNullOrWhiteSpace(outcome) ? \"-\" : T.T(outcome);");
        detailsSource.Should().Contain("string LocalizeScanFailureReason(string? reason) => reason switch");
        detailsSource.Should().Contain("\"Token was consumed concurrently by another request.\" => T.T(\"TokenAlreadyConsumed\")");
        detailsSource.Should().Contain("\"Loyalty account for scan session not found.\" => T.T(\"AccountNotFound\")");
        detailsSource.Should().Contain("\"Loyalty account is not active.\" => T.T(\"AccountNotActive\")");
        detailsSource.Should().Contain("\"Session expired before redemption confirmation.\" => T.T(\"Expired\")");
        detailsSource.Should().Contain("\"Scan session does not contain any selected rewards.\" => T.T(\"NoSelections\")");
        detailsSource.Should().Contain("\"Selected rewards do not require any points.\" => T.T(\"InvalidSelections\")");
        detailsSource.Should().Contain("\"Account does not have enough points at confirmation time.\" => T.T(\"InsufficientPoints\")");
        detailsSource.Should().Contain("@T.T(\"Status\")");
        detailsSource.Should().Contain("@T.T(\"Points\")");
        detailsSource.Should().Contain("@T.T(\"Lifetime\")");
        detailsSource.Should().Contain("@T.T(\"LastAccrual\")");
        detailsSource.Should().Contain("@T.T(\"LoyaltyRecentTransactions\")");
        detailsSource.Should().Contain("@T.T(\"LoyaltyNoTransactionsFound\")");
        detailsSource.Should().Contain("@T.T(\"LoyaltyRecentRedemptions\")");
        detailsSource.Should().Contain("@T.T(\"LoyaltyNoRedemptionsFound\")");
        detailsSource.Should().Contain("@LocalizeAccountStatus(Model.Status)");
        detailsSource.Should().Contain("@LocalizeRedemptionStatus(item.Status)");
        detailsSource.Should().Contain("@LocalizeScanStatus(item.ScanStatus)");
        detailsSource.Should().Contain("@LocalizeScanOutcome(item.ScanOutcome)");
        detailsSource.Should().Contain("LocalizeScanFailureReason(item.ScanFailureReason)");
        detailsSource.Should().Contain("hx-post=\"@Url.Action(\"ConfirmRedemption\", \"Loyalty\")\"");
        detailsSource.Should().Contain("@T.T(\"Confirm\")");
    }


    [Fact]
    public void LoyaltyCampaignsWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "Campaigns.cshtml"));

        source.Should().Contain("id=\"loyalty-campaigns-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"LoyaltyCampaignsTitle\")");
        source.Should().Contain("@T.T(\"LoyaltyCampaignsIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"CreateCampaign\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        source.Should().Contain("@T.T(\"LoyaltyNewCampaign\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.ActiveCount");
        source.Should().Contain("@Model.Summary.ScheduledCount");
        source.Should().Contain("@Model.Summary.DraftCount");
        source.Should().Contain("@Model.Summary.ExpiredCount");
        source.Should().Contain("@Model.Summary.PushEnabledCount");
        source.Should().Contain("@T.T(\"LoyaltyCampaignPlaybooks\")");
        source.Should().Contain("@T.T(\"Playbook\")");
        source.Should().Contain("@T.T(\"WhenItApplies\")");
        source.Should().Contain("@T.T(\"OperatorAction\")");
        source.Should().Contain("@playbook.Title");
        source.Should().Contain("@playbook.ScopeNote");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("hx-get=\"@Url.Action(\"Campaigns\", \"Loyalty\")\"");
        source.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"Filter\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("asp-route-filter=\"Active\"");
        source.Should().Contain("asp-route-filter=\"Scheduled\"");
        source.Should().Contain("asp-route-filter=\"Draft\"");
        source.Should().Contain("asp-route-filter=\"Expired\"");
        source.Should().Contain("asp-route-filter=\"PushEnabled\"");
        source.Should().Contain("@T.T(\"LoyaltyClearQueueFilters\")");
        source.Should().Contain("@T.T(\"Name\")");
        source.Should().Contain("@T.T(\"Title\")");
        source.Should().Contain("@T.T(\"State\")");
        source.Should().Contain("@T.T(\"Channels\")");
        source.Should().Contain("@T.T(\"LoyaltyWindow\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"LoyaltyNoCampaignsFound\")");
        source.Should().Contain("string LocalizeCampaignState(string? state) => string.IsNullOrWhiteSpace(state) ? \"-\" : T.T(state);");
        source.Should().Contain("T.T(\"LoyaltyInAppPlusPush\")");
        source.Should().Contain("T.T(\"LoyaltyInAppOnly\")");
        source.Should().Contain("@LocalizeCampaignState(item.CampaignState)");
        source.Should().Contain("@T.T(\"LoyaltyPushEnabled\")");
        source.Should().Contain("@T.T(\"LoyaltyTo\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"EditCampaign\", \"Loyalty\", new { id = item.Id, businessId = item.BusinessId })\"");
        source.Should().Contain("hx-post=\"@Url.Action(\"SetCampaignActivation\", \"Loyalty\")\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("name=\"rowVersion\" value=\"@Convert.ToBase64String(item.RowVersion)\"");
        source.Should().Contain("name=\"isActive\" value=\"@((!item.IsActive).ToString())\"");
        source.Should().Contain("item.IsActive ? T.T(\"Deactivate\") : T.T(\"Activate\")");
        source.Should().Contain("asp-controller=\"Loyalty\"");
        source.Should().Contain("asp-action=\"Campaigns\"");
        source.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
    }


    [Fact]
    public void LoyaltyCampaignEditorShellAndForm_Should_KeepCreateEditAndStateContractsWired()
    {
        var shellSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_CampaignEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_CampaignForm.cshtml"));

        shellSource.Should().Contain("id=\"loyalty-campaign-editor-shell\"");
        shellSource.Should().Contain("ViewData[\"IsCreate\"] as bool? == true");
        shellSource.Should().Contain("ViewData[\"Title\"] = isCreate ? T.T(\"LoyaltyCreateCampaign\") : T.T(\"LoyaltyEditCampaign\")");
        shellSource.Should().Contain("@(isCreate ? T.T(\"LoyaltyCreateCampaign\") : T.T(\"LoyaltyEditCampaign\"))");
        shellSource.Should().Contain("string LocalizeCampaignState(string? state) => string.IsNullOrWhiteSpace(state) ? \"-\" : T.T(state);");
        shellSource.Should().Contain("@T.T(\"LoyaltyCurrentState\") @LocalizeCampaignState(Model.CampaignState)");
        shellSource.Should().Contain("<partial name=\"_CampaignForm\" model=\"Model\" />");

        formSource.Should().Contain("asp-action=\"@(ViewData[\"IsCreate\"] as bool? == true ? \"CreateCampaign\" : \"EditCampaign\")\"");
        formSource.Should().Contain("hx-post=\"@Url.Action(ViewData[\"IsCreate\"] as bool? == true ? \"CreateCampaign\" : \"EditCampaign\", \"Loyalty\")\"");
        formSource.Should().Contain("@Html.AntiForgeryToken()");
        formSource.Should().Contain("<input asp-for=\"Id\" type=\"hidden\" />");
        formSource.Should().Contain("<input asp-for=\"RowVersion\" type=\"hidden\" />");
        formSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger\"");
        formSource.Should().Contain("asp-for=\"BusinessId\" asp-items=\"Model.BusinessOptions\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"Name\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Title\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Subtitle\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"MediaUrl\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"LandingUrl\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"Channels\" asp-items=\"Model.ChannelItems\" class=\"form-select\"");
        formSource.Should().Contain("asp-for=\"StartsAtUtc\" asp-format=\"{0:yyyy-MM-ddTHH:mm}\" type=\"datetime-local\" class=\"form-control\"");
        formSource.Should().Contain("asp-for=\"EndsAtUtc\" asp-format=\"{0:yyyy-MM-ddTHH:mm}\" type=\"datetime-local\" class=\"form-control\"");
        formSource.Should().Contain("string LocalizeCampaignState(object? state) => state is null ? \"-\" : T.T(state.ToString() ?? string.Empty);");
        formSource.Should().Contain("<input asp-for=\"CampaignState\" type=\"hidden\" />");
        formSource.Should().Contain("<input value=\"@LocalizeCampaignState(Model.CampaignState)\" class=\"form-control\" readonly />");
        formSource.Should().Contain("textarea asp-for=\"Body\" class=\"form-control\" rows=\"4\"></textarea>");
        formSource.Should().Contain("textarea asp-for=\"TargetingJson\" class=\"form-control\" rows=\"4\"></textarea>");
        formSource.Should().Contain("@T.T(\"LoyaltyTargetingJsonHelp\")");
        formSource.Should().Contain("textarea asp-for=\"PayloadJson\" class=\"form-control\" rows=\"4\"></textarea>");
        formSource.Should().Contain("@T.T(\"LoyaltyPayloadJsonHelp\")");
        formSource.Should().Contain("class=\"btn btn-primary\">@(ViewData[\"IsCreate\"] as bool? == true ? T.T(\"Create\") : T.T(\"Save\"))</button>");
        formSource.Should().Contain("hx-get=\"@Url.Action(\"Campaigns\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        formSource.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void LoyaltyScanSessionsAndRedemptionsWorkspaces_Should_KeepOperationalShellQueueGridAndPagerContractsWired()
    {
        var scanSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "ScanSessions.cshtml"));
        var redemptionsSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "Redemptions.cshtml"));

        scanSource.Should().Contain("id=\"loyalty-scan-sessions-workspace-shell\"");
        scanSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        scanSource.Should().Contain("@T.T(\"LoyaltyScanSessionsTitle\")");
        scanSource.Should().Contain("@T.T(\"LoyaltyScanSessionsIntro\")");
        scanSource.Should().Contain("hx-get=\"@Url.Action(\"Redemptions\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        scanSource.Should().Contain("@T.T(\"Redemptions\")");
        scanSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\")\"");
        scanSource.Should().Contain("@T.T(\"LoyaltyMobileOps\")");
        scanSource.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { providerReviewOnly = true })\"");
        scanSource.Should().Contain("@T.T(\"ProviderReview\")");
        scanSource.Should().Contain("hx-get=\"@Url.Action(\"ScanSessions\", \"Loyalty\")\"");
        scanSource.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        scanSource.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        scanSource.Should().Contain("@T.T(\"LoyaltySearchCustomerOrOutcome\")");
        scanSource.Should().Contain("name=\"mode\" asp-items=\"Model.ModeItems\"");
        scanSource.Should().Contain("name=\"status\" asp-items=\"Model.StatusItems\"");
        scanSource.Should().Contain("@T.T(\"Filter\")");
        scanSource.Should().Contain("@T.T(\"Reset\")");
        scanSource.Should().Contain("@Model.Summary.TotalCount");
        scanSource.Should().Contain("@Model.Summary.AccrualCount");
        scanSource.Should().Contain("@Model.Summary.RedemptionCount");
        scanSource.Should().Contain("@Model.Summary.PendingCount");
        scanSource.Should().Contain("@Model.Summary.ExpiredCount");
        scanSource.Should().Contain("@Model.Summary.FailureCount");
        scanSource.Should().Contain("@T.T(\"LoyaltyScanSessionPlaybooks\")");
        scanSource.Should().Contain("@T.T(\"LoyaltyAccrual\")");
        scanSource.Should().Contain("@T.T(\"LoyaltyRedemption\")");
        scanSource.Should().Contain("@T.T(\"Pending\")");
        scanSource.Should().Contain("@T.T(\"Completed\")");
        scanSource.Should().Contain("@T.T(\"Expired\")");
        scanSource.Should().Contain("string LocalizeScanMode(object? mode) => mode is null ? \"-\" : T.T(mode.ToString() ?? string.Empty);");
        scanSource.Should().Contain("string LocalizeScanStatus(Darwin.Domain.Enums.LoyaltyScanStatus status) => T.T(status.ToString());");
        scanSource.Should().Contain("string LocalizeScanOutcome(string? outcome) => string.IsNullOrWhiteSpace(outcome) ? \"-\" : T.T(outcome);");
        scanSource.Should().Contain("string LocalizeScanFailureReason(string? reason) => reason switch");
        scanSource.Should().Contain("\"Session expired before use.\" => T.T(\"Expired\")");
        scanSource.Should().Contain("@T.T(\"LoyaltyClearQueueFilters\")");
        scanSource.Should().Contain("@T.T(\"Customer\")");
        scanSource.Should().Contain("@T.T(\"Mode\")");
        scanSource.Should().Contain("@T.T(\"Outcome\")");
        scanSource.Should().Contain("@T.T(\"Failure\")");
        scanSource.Should().Contain("@T.T(\"LoyaltyNoScanSessionsFound\")");
        scanSource.Should().Contain("@LocalizeScanMode(item.Mode)");
        scanSource.Should().Contain("@LocalizeScanStatus(item.Status)");
        scanSource.Should().Contain("@LocalizeScanOutcome(item.Outcome)");
        scanSource.Should().Contain("@LocalizeScanFailureReason(item.FailureReason)");
        scanSource.Should().Contain("@T.T(\"LoyaltyExp\")");
        scanSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\", new { q = item.CustomerEmail })\"");
        scanSource.Should().Contain("asp-controller=\"Loyalty\"");
        scanSource.Should().Contain("asp-action=\"ScanSessions\"");
        scanSource.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        scanSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        scanSource.Should().Contain("asp-route-mode=\"@Model.ModeFilter\"");
        scanSource.Should().Contain("asp-route-status=\"@Model.StatusFilter\"");

        redemptionsSource.Should().Contain("id=\"loyalty-redemptions-workspace-shell\"");
        redemptionsSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyRedemptionsTitle\")");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyRedemptionsIntro\")");
        redemptionsSource.Should().Contain("hx-get=\"@Url.Action(\"Accounts\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        redemptionsSource.Should().Contain("@T.T(\"Accounts\")");
        redemptionsSource.Should().Contain("hx-get=\"@Url.Action(\"ScanSessions\", \"Loyalty\", new { businessId = Model.BusinessId })\"");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyScanSessionsTitle\")");
        redemptionsSource.Should().Contain("hx-get=\"@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { providerReviewOnly = true })\"");
        redemptionsSource.Should().Contain("@T.T(\"ProviderReview\")");
        redemptionsSource.Should().Contain("hx-get=\"@Url.Action(\"Redemptions\", \"Loyalty\")\"");
        redemptionsSource.Should().Contain("name=\"businessId\" asp-items=\"Model.BusinessOptions\"");
        redemptionsSource.Should().Contain("name=\"q\" value=\"@Model.Query\"");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltySearchMemberRewardOutcome\")");
        redemptionsSource.Should().Contain("name=\"status\" asp-items=\"Model.StatusItems\"");
        redemptionsSource.Should().Contain("@T.T(\"Filter\")");
        redemptionsSource.Should().Contain("@T.T(\"Reset\")");
        redemptionsSource.Should().Contain("@Model.Summary.TotalCount");
        redemptionsSource.Should().Contain("@Model.Summary.PendingCount");
        redemptionsSource.Should().Contain("@Model.Summary.CompletedCount");
        redemptionsSource.Should().Contain("@Model.Summary.CancelledCount");
        redemptionsSource.Should().Contain("@Model.Summary.ScanFailureCount");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyRedemptionPlaybooks\")");
        redemptionsSource.Should().Contain("@T.T(\"Pending\")");
        redemptionsSource.Should().Contain("@T.T(\"Completed\")");
        redemptionsSource.Should().Contain("@T.T(\"Cancelled\")");
        redemptionsSource.Should().Contain("string LocalizeRedemptionStatus(Darwin.Domain.Enums.LoyaltyRedemptionStatus status) => T.T(status.ToString());");
        redemptionsSource.Should().Contain("string LocalizeScanStatus(object? status) => status is null ? \"-\" : T.T(status.ToString() ?? string.Empty);");
        redemptionsSource.Should().Contain("string LocalizeScanOutcome(string? outcome) => string.IsNullOrWhiteSpace(outcome) ? \"-\" : T.T(outcome);");
        redemptionsSource.Should().Contain("string LocalizeScanFailureReason(string? reason) => reason switch");
        redemptionsSource.Should().Contain("@T.T(\"Member\")");
        redemptionsSource.Should().Contain("@T.T(\"Reward\")");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyScan\")");
        redemptionsSource.Should().Contain("@T.T(\"When\")");
        redemptionsSource.Should().Contain("@T.T(\"Note\")");
        redemptionsSource.Should().Contain("@T.T(\"Actions\")");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyNoRedemptionsFound\")");
        redemptionsSource.Should().Contain("@LocalizeRedemptionStatus(item.Status)");
        redemptionsSource.Should().Contain("@LocalizeScanStatus(item.ScanStatus)");
        redemptionsSource.Should().Contain("@LocalizeScanOutcome(item.ScanOutcome)");
        redemptionsSource.Should().Contain("LocalizeScanFailureReason(item.ScanFailureReason)");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyPts\")");
        redemptionsSource.Should().Contain("hx-get=\"@Url.Action(\"AccountDetails\", \"Loyalty\", new { id = item.LoyaltyAccountId })\"");
        redemptionsSource.Should().Contain("@T.T(\"Account\")");
        redemptionsSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"MobileOperations\", new { q = item.ConsumerEmail })\"");
        redemptionsSource.Should().Contain("@T.T(\"LoyaltyMobileOps\")");
        redemptionsSource.Should().Contain("hx-post=\"@Url.Action(\"ConfirmRedemption\", \"Loyalty\")\"");
        redemptionsSource.Should().Contain("@T.T(\"Confirm\")");
        redemptionsSource.Should().Contain("asp-controller=\"Loyalty\"");
        redemptionsSource.Should().Contain("asp-action=\"Redemptions\"");
        redemptionsSource.Should().Contain("asp-route-businessId=\"@Model.BusinessId\"");
        redemptionsSource.Should().Contain("asp-route-q=\"@Model.Query\"");
        redemptionsSource.Should().Contain("asp-route-status=\"@Model.StatusFilter\"");
    }


    [Fact]
    public void LoyaltyAdjustPointsEditorShell_Should_KeepHiddenStateAndApplyContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "_AdjustPointsEditorShell.cshtml"));

        source.Should().Contain("id=\"loyalty-adjust-points-editor-shell\"");
        source.Should().Contain("@T.T(\"LoyaltyAdjustPointsTitle\")");
        source.Should().Contain("@Model.AccountLabel");
        source.Should().Contain("hx-post=\"@Url.Action(\"AdjustPoints\", \"Loyalty\")\"");
        source.Should().Contain("@Html.AntiForgeryToken()");
        source.Should().Contain("input asp-for=\"LoyaltyAccountId\" type=\"hidden\"");
        source.Should().Contain("input asp-for=\"BusinessId\" type=\"hidden\"");
        source.Should().Contain("input asp-for=\"UserId\" type=\"hidden\"");
        source.Should().Contain("input asp-for=\"AccountLabel\" type=\"hidden\"");
        source.Should().Contain("input asp-for=\"RowVersion\" type=\"hidden\"");
        source.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger\"");
        source.Should().Contain("asp-for=\"PointsDelta\" class=\"form-control\"");
        source.Should().Contain("@T.T(\"LoyaltyPointsDeltaHelp\")");
        source.Should().Contain("asp-for=\"Reason\" class=\"form-control\"");
        source.Should().Contain("asp-for=\"Reference\" class=\"form-control\"");
        source.Should().Contain("@T.T(\"LoyaltyApply\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"AccountDetails\", \"Loyalty\", new { id = Model.LoyaltyAccountId })\"");
        source.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void BrandsController_Should_KeepWorkspaceAndEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "BrandsController.cs"));

        source.Should().Contain("public sealed class BrandsController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20,");
        source.Should().Contain("var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;");
        source.Should().Contain("var (items, total) = await _getPage.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);");
        source.Should().Contain("var summary = await _getBrandOpsSummary.HandleAsync(ct);");
        source.Should().Contain("Playbooks = BuildBrandPlaybooks(),");
        source.Should().Contain("UnpublishedCount = summary.UnpublishedCount,");
        source.Should().Contain("MissingSlugCount = summary.MissingSlugCount,");
        source.Should().Contain("MissingLogoCount = summary.MissingLogoCount");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("public IActionResult Create() => RenderBrandEditor(new BrandEditVm");
        source.Should().Contain("Translations = { new BrandTranslationVm { Culture = GetDefaultCulture() } }");
        source.Should().Contain("public async Task<IActionResult> Create(BrandEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("await EnsureTranslationsAsync(vm, ct);");
        source.Should().Contain("var dto = new BrandCreateDto");
        source.Should().Contain("Slug = string.IsNullOrWhiteSpace(vm.Slug) ? null : vm.Slug.Trim(),");
        source.Should().Contain("Translations = vm.Translations.Select(t => new BrandTranslationDto");
        source.Should().Contain("SetSuccessMessage(\"BrandCreated\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getForEdit.HandleAsync(id, ct);");
        source.Should().Contain("SetErrorMessage(\"BrandNotFound\");");
        source.Should().Contain("return RenderBrandEditor(vm, isCreate: false);");
        source.Should().Contain("public async Task<IActionResult> Edit(BrandEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new BrandEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        source.Should().Contain("SetSuccessMessage(\"BrandUpdated\");");
        source.Should().Contain("SetErrorMessage(\"BrandConcurrencyConflict\");");
        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        source.Should().Contain("var dto = new BrandDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };");
        source.Should().Contain("result.Succeeded ? T(\"BrandDeleted\") : (result.Error ?? T(\"BrandDeleteFailed\"))");
        source.Should().Contain("private IActionResult RenderBrandEditor(BrandEditVm vm, bool isCreate)");
        source.Should().Contain("ViewData[\"IsCreate\"] = isCreate;");
        source.Should().Contain("return PartialView(\"~/Views/Brands/_BrandEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(BrandsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Brands/Index.cshtml\", vm);");
        source.Should().Contain("return View(\"Index\", vm);");
    }


    [Fact]
    public void BrandsController_Should_KeepTranslationPlaybookAndHtmxHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "BrandsController.cs"));

        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private string GetDefaultCulture()");
        source.Should().Contain("return _siteSettingCache.GetAsync().GetAwaiter().GetResult().DefaultCulture;");
        source.Should().Contain("private async Task EnsureTranslationsAsync(BrandEditVm vm, CancellationToken ct)");
        source.Should().Contain("var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;");
        source.Should().Contain("if (vm.Translations.Count == 0)");
        source.Should().Contain("vm.Translations.Add(new BrandTranslationVm { Culture = defaultCulture });");
        source.Should().Contain("private OperationalPlaybookVm[] BuildBrandPlaybooks()");
        source.Should().Contain("QueueLabel = T(\"Unpublished\")");
        source.Should().Contain("WhyItMatters = T(\"BrandPlaybookUnpublishedScope\")");
        source.Should().Contain("QueueLabel = T(\"MissingSlug\")");
        source.Should().Contain("WhyItMatters = T(\"BrandPlaybookMissingSlugScope\")");
        source.Should().Contain("QueueLabel = T(\"MissingLogo\")");
        source.Should().Contain("WhyItMatters = T(\"BrandPlaybookMissingLogoScope\")");
    }


    [Fact]
    public void BrandsWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Brands", "Index.cshtml"));

        source.Should().Contain("id=\"brands-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"Brands\")");
        source.Should().Contain("@T.T(\"BrandsWorkspaceIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Brands\")\"");
        source.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        source.Should().Contain("name=\"filter\" value=\"@Model.Filter\"");
        source.Should().Contain("@T.T(\"SearchBrandsPlaceholder\")");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Brands\")\"");
        source.Should().Contain("@T.T(\"CreateBrand\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@T.T(\"BrandsOpsTotalNote\")");
        source.Should().Contain("@Model.Summary.UnpublishedCount");
        source.Should().Contain("@T.T(\"BrandsOpsUnpublishedNote\")");
        source.Should().Contain("@Model.Summary.MissingSlugCount");
        source.Should().Contain("@T.T(\"BrandsOpsMissingSlugNote\")");
        source.Should().Contain("@Model.Summary.MissingLogoCount");
        source.Should().Contain("@T.T(\"BrandsOpsMissingLogoNote\")");
        source.Should().Contain("asp-route-filter=\"unpublished\"");
        source.Should().Contain("asp-route-filter=\"missing-slug\"");
        source.Should().Contain("asp-route-filter=\"missing-logo\"");
        source.Should().Contain("@T.T(\"BrandOperationsPlaybooks\")");
        source.Should().Contain("@playbook.QueueLabel");
        source.Should().Contain("@playbook.WhyItMatters");
        source.Should().Contain("@T.T(\"OperatorAction\"):");
        source.Should().Contain("@T.T(\"Name\")");
        source.Should().Contain("@T.T(\"Slug\")");
        source.Should().Contain("@T.T(\"Published\")");
        source.Should().Contain("@T.T(\"Logo\")");
        source.Should().Contain("@T.T(\"Modified\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"Configured\")");
        source.Should().Contain("@T.T(\"Missing\")");
        source.Should().Contain("@T.T(\"NoBrandsFound\")");
        source.Should().Contain("span class=\"badge bg-success\">@T.T(\"Yes\")</span>");
        source.Should().Contain("span class=\"badge bg-outline-secondary border\">@T.T(\"No\")</span>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Brands\", new { id = b.Id })\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Brands\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(b.RowVersion)\"");
        source.Should().Contain("asp-controller=\"Brands\"");
        source.Should().Contain("asp-route-query=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void AddOnGroupsController_Should_KeepWorkspaceAndEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "AddOnGroupsController.cs"));

        source.Should().Contain("public sealed class AddOnGroupsController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(");
        source.Should().Contain("var (items, total) = await _getPage.HandleAsync(page, pageSize, query, filter, ct);");
        source.Should().Contain("var summary = await _getSummary.HandleAsync(query, ct);");
        source.Should().Contain("FilterItems = BuildFilterItems(filter),");
        source.Should().Contain("Playbooks = BuildPlaybooks(),");
        source.Should().Contain("GlobalCount = summary.GlobalCount,");
        source.Should().Contain("UnattachedCount = summary.UnattachedCount,");
        source.Should().Contain("VariantLinkedCount = summary.VariantLinkedCount");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("public IActionResult Create()");
        source.Should().Contain("var defaultCurrency = _siteSettingCache.GetAsync().GetAwaiter().GetResult().DefaultCurrency;");
        source.Should().Contain("Options = { new AddOnOptionVm { Label = \"Option\", Values = { new AddOnOptionValueVm { Label = \"Value\" } } } }");
        source.Should().Contain("return RenderCreateEditor(new AddOnGroupCreateVm");
        source.Should().Contain("public async Task<IActionResult> Create(AddOnGroupCreateVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new AddOnGroupCreateDto");
        source.Should().Contain("Currency = string.IsNullOrWhiteSpace(vm.Currency)");
        source.Should().Contain("SelectionMode = vm.SelectionMode,");
        source.Should().Contain("Options = vm.Options.Select(o => new AddOnOptionDto");
        source.Should().Contain("Values = o.Values.Select(v => new AddOnOptionValueDto");
        source.Should().Contain("await _create.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"AddOnGroupCreated\");");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getForEdit.HandleAsync(id, ct);");
        source.Should().Contain("SetErrorMessage(\"AddOnGroupNotFound\");");
        source.Should().Contain("return RenderEditEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Edit(AddOnGroupEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new AddOnGroupEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        source.Should().Contain("await _update.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"AddOnGroupUpdated\");");
        source.Should().Contain("ModelState.AddModelError(string.Empty, T(\"AddOnGroupConcurrencyConflict\"));");
        source.Should().Contain("private IActionResult RenderCreateEditor(AddOnGroupCreateVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/AddOnGroups/_AddOnGroupCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Create\", vm);");
        source.Should().Contain("private IActionResult RenderEditEditor(AddOnGroupEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/AddOnGroups/_AddOnGroupEditEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Edit\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(AddOnGroupsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/AddOnGroups/Index.cshtml\", vm);");
        source.Should().Contain("return View(\"Index\", vm);");
    }


    [Fact]
    public void AddOnGroupsController_Should_KeepAttachmentAndHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "AddOnGroupsController.cs"));

        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        source.Should().Contain("var dto = new AddOnGroupDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };");
        source.Should().Contain("var result = await _softDelete.HandleAsync(dto, ct);");
        source.Should().Contain("TempData[\"Warning\"] = result.Error ?? T(\"AddOnGroupDeleteFailed\");");
        source.Should().Contain("SetSuccessMessage(\"AddOnGroupDeleted\");");
        source.Should().Contain("public async Task<IActionResult> AttachToProducts(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        source.Should().Contain("var (items, total) = await _getProductsPage.HandleAsync(page, pageSize, defaultCulture, query, filter: null, ct);");
        source.Should().Contain("var attached = await _getAttachedProducts.HandleAsync(id, ct);");
        source.Should().Contain("return RenderAttachToProducts(vm);");
        source.Should().Contain("public async Task<IActionResult> AttachToProducts(AddOnGroupAttachToProductsVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new AddOnGroupAttachToProductsDto");
        source.Should().Contain("ProductIds = (vm.SelectedProductIds ?? new List<Guid>()).ToArray()");
        source.Should().Contain("var result = await _attachProducts.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"AddOnGroupAttachedToProducts\");");
        source.Should().Contain("public async Task<IActionResult> AttachToCategories(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        source.Should().Contain("var attached = await _getAttachedCategories.HandleAsync(id, ct);");
        source.Should().Contain("return RenderAttachToCategories(vm);");
        source.Should().Contain("await _attachCategories.HandleAsync(");
        source.Should().Contain("SetSuccessMessage(\"AddOnGroupAttachedToCategories\");");
        source.Should().Contain("public async Task<IActionResult> AttachToBrands(Guid id, int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)");
        source.Should().Contain("var attached = await _getAttachedBrands.HandleAsync(id, ct);");
        source.Should().Contain("return RenderAttachToBrands(vm);");
        source.Should().Contain("await _attachBrands.HandleAsync(");
        source.Should().Contain("SetSuccessMessage(\"AddOnGroupAttachedToBrands\");");
        source.Should().Contain("public async Task<IActionResult> AttachToVariants(");
        source.Should().Contain("var (items, total) = await _getVariantsPage.HandleAsync(page, pageSize, q, culture, ct);");
        source.Should().Contain("var attached = await _getAttachedVariants.HandleAsync(id, ct);");
        source.Should().Contain("Display = $\"{v.Sku} - {v.ProductName ?? \"(no name)\"}\"");
        source.Should().Contain("SelectedVariantIds = attached.ToList()");
        source.Should().Contain("return RenderAttachToVariants(vm);");
        source.Should().Contain("public async Task<IActionResult> AttachToVariants(AddOnGroupAttachToVariantsVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new AddOnGroupAttachToVariantsDto");
        source.Should().Contain("VariantIds = (vm.SelectedVariantIds ?? new List<Guid>()).ToArray()");
        source.Should().Contain("var result = await _attachVariants.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"AddOnGroupAttachedToVariants\");");
        source.Should().Contain("private IActionResult RenderAttachToProducts(AddOnGroupAttachToProductsVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/AddOnGroups/AttachToProducts.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderAttachToCategories(AddOnGroupAttachToCategoriesVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/AddOnGroups/AttachToCategories.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderAttachToBrands(AddOnGroupAttachToBrandsVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/AddOnGroups/AttachToBrands.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderAttachToVariants(AddOnGroupAttachToVariantsVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/AddOnGroups/AttachToVariants.cshtml\", vm);");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildFilterItems(AddOnGroupQueueFilter selected)");
        source.Should().Contain("new SelectListItem(T(\"AddOnAllGroups\"), AddOnGroupQueueFilter.All.ToString(), selected == AddOnGroupQueueFilter.All)");
        source.Should().Contain("new SelectListItem(T(\"Inactive\"), AddOnGroupQueueFilter.Inactive.ToString(), selected == AddOnGroupQueueFilter.Inactive)");
        source.Should().Contain("new SelectListItem(T(\"Global\"), AddOnGroupQueueFilter.Global.ToString(), selected == AddOnGroupQueueFilter.Global)");
        source.Should().Contain("new SelectListItem(T(\"Unattached\"), AddOnGroupQueueFilter.Unattached.ToString(), selected == AddOnGroupQueueFilter.Unattached)");
        source.Should().Contain("new SelectListItem(T(\"AddOnVariantProductLinked\"), AddOnGroupQueueFilter.VariantLinked.ToString(), selected == AddOnGroupQueueFilter.VariantLinked)");
        source.Should().Contain("private List<AddOnGroupPlaybookVm> BuildPlaybooks()");
        source.Should().Contain("QueueLabel = T(\"AddOnPlaybookUnattachedTitle\")");
        source.Should().Contain("QueueLabel = T(\"AddOnPlaybookInactiveTitle\")");
        source.Should().Contain("QueueLabel = T(\"AddOnPlaybookGlobalTitle\")");
    }


    [Fact]
    public void ProductsController_Should_KeepWorkspaceAndEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "ProductsController.cs"));

        source.Should().Contain("public sealed class ProductsController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)");
        source.Should().Contain("var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;");
        source.Should().Contain("var (items, total) = await _getProductsPage.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);");
        source.Should().Contain("var summary = await _getProductOpsSummary.HandleAsync(ct);");
        source.Should().Contain("Playbooks = BuildProductPlaybooks()");
        source.Should().Contain("InactiveCount = summary.InactiveCount,");
        source.Should().Contain("HiddenCount = summary.HiddenCount,");
        source.Should().Contain("SingleVariantCount = summary.SingleVariantCount,");
        source.Should().Contain("ScheduledCount = summary.ScheduledCount");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct)");
        source.Should().Contain("await LoadLookupsAsync(ct);");
        source.Should().Contain("var siteSettings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("if (vm.Translations.Count == 0) vm.Translations.Add(new ProductTranslationVm { Culture = defaultCulture });");
        source.Should().Contain("if (vm.Variants.Count == 0) vm.Variants.Add(new ProductVariantCreateVm { Currency = defaultCurrency });");
        source.Should().Contain("return RenderCreateEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Create(ProductCreateVm vm, CancellationToken ct)");
        source.Should().Contain("ModelState.AddModelError(nameof(vm.Translations), T(\"ProductAtLeastOneTranslationRequired\"));");
        source.Should().Contain("ModelState.AddModelError(nameof(vm.Variants), T(\"ProductAtLeastOneVariantRequired\"));");
        source.Should().Contain("await EnsureProductDefaultsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("var dto = new ProductCreateDto");
        source.Should().Contain("Kind = vm.Kind,");
        source.Should().Contain("Translations = vm.Translations.Select(t => new ProductTranslationDto");
        source.Should().Contain("Variants = vm.Variants.Select(v => new ProductVariantCreateDto");
        source.Should().Contain("await _createProduct.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"ProductCreated\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct)");
        source.Should().Contain("var dto = await _getProductForEdit.HandleAsync(id, ct);");
        source.Should().Contain("SetErrorMessage(\"ProductNotFound\");");
        source.Should().Contain("RowVersion = dto.RowVersion,");
        source.Should().Contain("await LoadLookupsAsync(ct);");
        source.Should().Contain("return RenderEditEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Edit(ProductEditVm vm, CancellationToken ct)");
        source.Should().Contain("var dto = new ProductEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        source.Should().Contain("await _updateProduct.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"ProductUpdated\");");
        source.Should().Contain("ModelState.AddModelError(string.Empty, T(\"ProductConcurrencyConflict\"));");
        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)");
        source.Should().Contain("await _softDeleteProduct.HandleAsync(id, ct);");
        source.Should().Contain("SetSuccessMessage(\"ProductDeleted\");");
        source.Should().Contain("SetErrorMessage(\"ProductDeleteFailed\");");
        source.Should().Contain("private IActionResult RenderCreateEditor(ProductCreateVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Products/_ProductCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Create\", vm);");
        source.Should().Contain("private IActionResult RenderEditEditor(ProductEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Products/_ProductEditEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Edit\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(ProductsIndexVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Products/Index.cshtml\", vm);");
        source.Should().Contain("return View(\"Index\", vm);");
    }


    [Fact]
    public void ProductsController_Should_KeepLookupDefaultAndHtmxHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "ProductsController.cs"));

        source.Should().Contain("private async Task LoadLookupsAsync(CancellationToken ct)");
        source.Should().Contain("var lookups = await _getLookups.HandleAsync(defaultCulture, ct);");
        source.Should().Contain("ViewBag.Brands = lookups.Brands;");
        source.Should().Contain("ViewBag.Categories = lookups.Categories;");
        source.Should().Contain("ViewBag.TaxCategories = lookups.TaxCategories;");
        source.Should().Contain("var (_, cultures) = await _getCultures.HandleAsync(ct);");
        source.Should().Contain("ViewBag.Cultures = cultures;");
        source.Should().Contain("ViewBag.Currencies = BuildCurrencyOptions((await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCurrency);");
        source.Should().Contain("private static IReadOnlyList<string> BuildCurrencyOptions(string defaultCurrency)");
        source.Should().Contain("var items = new List<string>();");
        source.Should().Contain("items.Add(defaultCurrency.Trim().ToUpperInvariant());");
        source.Should().Contain("foreach (var currency in new[] { \"USD\", \"GBP\", Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCurrencyDefault })");
        source.Should().Contain("if (!items.Contains(currency, StringComparer.OrdinalIgnoreCase))");
        source.Should().Contain("return items;");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("return new EmptyResult();");
        source.Should().Contain("return RedirectToAction(actionName, routeValues);");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private async Task EnsureProductDefaultsAsync(ProductEditorVm vm, CancellationToken ct)");
        source.Should().Contain("if (vm.Translations.Count == 0)");
        source.Should().Contain("vm.Translations.Add(new ProductTranslationVm { Culture = defaultCulture });");
        source.Should().Contain("if (vm.Variants.Count == 0)");
        source.Should().Contain("vm.Variants.Add(new ProductVariantCreateVm { Currency = siteSettings.DefaultCurrency });");
        source.Should().Contain("private OperationalPlaybookVm[] BuildProductPlaybooks()");
        source.Should().Contain("QueueLabel = T(\"Inactive\")");
        source.Should().Contain("WhyItMatters = T(\"ProductPlaybookInactiveScope\")");
        source.Should().Contain("QueueLabel = T(\"Hidden\")");
        source.Should().Contain("WhyItMatters = T(\"ProductPlaybookHiddenScope\")");
        source.Should().Contain("QueueLabel = T(\"Scheduled\")");
        source.Should().Contain("WhyItMatters = T(\"ProductPlaybookScheduledScope\")");
    }


    [Fact]
    public void CategoriesController_Should_KeepWorkspaceAndEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "CategoriesController.cs"));

        source.Should().Contain("public sealed class CategoriesController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)");
        source.Should().Contain("var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;");
        source.Should().Contain("var (items, total) = await _list.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);");
        source.Should().Contain("var summary = await _getCategoryOpsSummary.HandleAsync(ct);");
        source.Should().Contain("Playbooks = BuildCategoryPlaybooks()");
        source.Should().Contain("InactiveCount = summary.InactiveCount,");
        source.Should().Contain("UnpublishedCount = summary.UnpublishedCount,");
        source.Should().Contain("RootCount = summary.RootCount,");
        source.Should().Contain("ChildCount = summary.ChildCount");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct)");
        source.Should().Contain("await LoadLookupsAsync(ct);");
        source.Should().Contain("if (vm.Translations.Count == 0) vm.Translations.Add(new CategoryTranslationVm { Culture = defaultCulture });");
        source.Should().Contain("return RenderCreateEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Create(CategoryCreateVm vm, CancellationToken ct)");
        source.Should().Contain("ModelState.AddModelError(nameof(vm.Translations), T(\"CategoryAtLeastOneTranslationRequired\"));");
        source.Should().Contain("await EnsureCreateTranslationsAsync(vm, ct);");
        source.Should().Contain("var dto = new CategoryCreateDto");
        source.Should().Contain("ParentId = vm.ParentId,");
        source.Should().Contain("SortOrder = vm.SortOrder,");
        source.Should().Contain("IsActive = vm.IsActive,");
        source.Should().Contain("Translations = vm.Translations.Select(t => new CategoryTranslationDto");
        source.Should().Contain("await _create.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"CategoryCreated\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct)");
        source.Should().Contain("var dto = await _getForEdit.HandleAsync(id, ct);");
        source.Should().Contain("SetErrorMessage(\"CategoryNotFound\");");
        source.Should().Contain("RowVersion = dto.RowVersion,");
        source.Should().Contain("await LoadLookupsAsync(ct);");
        source.Should().Contain("return RenderEditEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Edit(CategoryEditVm vm, CancellationToken ct)");
        source.Should().Contain("var dto = new CategoryEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        source.Should().Contain("await _update.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"CategoryUpdated\");");
        source.Should().Contain("ModelState.AddModelError(string.Empty, T(\"CategoryConcurrencyConflict\"));");
        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)");
        source.Should().Contain("await _softDelete.HandleAsync(id, ct);");
        source.Should().Contain("SetSuccessMessage(\"CategoryDeleted\");");
        source.Should().Contain("SetErrorMessage(\"CategoryDeleteFailed\");");
        source.Should().Contain("private IActionResult RenderCreateEditor(CategoryCreateVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Categories/_CategoryCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Create\", vm);");
        source.Should().Contain("private IActionResult RenderEditEditor(CategoryEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Categories/_CategoryEditEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Edit\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(CategoriesIndexVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Categories/Index.cshtml\", vm);");
        source.Should().Contain("return View(\"Index\", vm);");
    }


    [Fact]
    public void CategoriesController_Should_KeepLookupTranslationAndHtmxHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Catalog", "CategoriesController.cs"));

        source.Should().Contain("private async Task LoadLookupsAsync(CancellationToken ct)");
        source.Should().Contain("var lookups = await _getLookups.HandleAsync(defaultCulture, ct);");
        source.Should().Contain("ViewBag.Categories = lookups.Categories;");
        source.Should().Contain("var (_, cultures) = await _getCultures.HandleAsync(ct);");
        source.Should().Contain("ViewBag.Cultures = cultures;");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("return new EmptyResult();");
        source.Should().Contain("return RedirectToAction(actionName, routeValues);");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private async Task EnsureCreateTranslationsAsync(CategoryCreateVm vm, CancellationToken ct)");
        source.Should().Contain("vm.Translations.Add(new CategoryTranslationVm { Culture = defaultCulture });");
        source.Should().Contain("private async Task EnsureEditTranslationsAsync(CategoryEditVm vm, CancellationToken ct)");
        source.Should().Contain("vm.Translations.Add(new CategoryTranslationVm { Culture = defaultCulture });");
        source.Should().Contain("private OperationalPlaybookVm[] BuildCategoryPlaybooks()");
        source.Should().Contain("QueueLabel = T(\"Inactive\")");
        source.Should().Contain("WhyItMatters = T(\"CategoryPlaybookInactiveScope\")");
        source.Should().Contain("QueueLabel = T(\"Unpublished\")");
        source.Should().Contain("WhyItMatters = T(\"CategoryPlaybookUnpublishedScope\")");
        source.Should().Contain("QueueLabel = T(\"ChildCategories\")");
        source.Should().Contain("WhyItMatters = T(\"CategoryPlaybookChildScope\")");
    }


    [Fact]
    public void CategoriesWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Categories", "Index.cshtml"));

        source.Should().Contain("id=\"categories-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"Categories\")");
        source.Should().Contain("@T.T(\"CategoriesWorkspaceIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Categories\")\"");
        source.Should().Contain("name=\"query\" value=\"@query\"");
        source.Should().Contain("name=\"filter\" value=\"@filter\"");
        source.Should().Contain("@T.T(\"SearchCategoriesPlaceholder\")");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Categories\")\"");
        source.Should().Contain("@T.T(\"CreateCategory\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@T.T(\"CategoriesOpsTotalNote\")");
        source.Should().Contain("@Model.Summary.InactiveCount");
        source.Should().Contain("@T.T(\"CategoriesOpsInactiveNote\")");
        source.Should().Contain("@Model.Summary.UnpublishedCount");
        source.Should().Contain("@T.T(\"CategoriesOpsUnpublishedNote\")");
        source.Should().Contain("@Model.Summary.ChildCount");
        source.Should().Contain("@T.T(\"CategoriesOpsChildNote\")");
        source.Should().Contain("asp-route-filter=\"inactive\"");
        source.Should().Contain("asp-route-filter=\"unpublished\"");
        source.Should().Contain("asp-route-filter=\"root\"");
        source.Should().Contain("asp-route-filter=\"child\"");
        source.Should().Contain("@T.T(\"CategoryOperationsPlaybooks\")");
        source.Should().Contain("@playbook.QueueLabel");
        source.Should().Contain("@playbook.WhyItMatters");
        source.Should().Contain("@T.T(\"OperatorAction\"):");
        source.Should().Contain("@T.T(\"CategoryNameDefaultCulture\")");
        source.Should().Contain("@T.T(\"Active\")");
        source.Should().Contain("@T.T(\"Published\")");
        source.Should().Contain("@T.T(\"SortOrder\")");
        source.Should().Contain("@T.T(\"Modified\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoCategoriesFound\")");
        source.Should().Contain("T.T(\"RootCategory\")");
        source.Should().Contain("T.T(\"ChildCategory\")");
        source.Should().Contain("span class=\"badge bg-success\">@T.T(\"Yes\")</span>");
        source.Should().Contain("span class=\"badge bg-outline-secondary border\">@T.T(\"No\")</span>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Categories\", new { id = c.Id })\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Categories\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(c.RowVersion)\"");
        source.Should().Contain("<pager page=\"@currentPage\"");
        source.Should().Contain("asp-controller=\"Categories\"");
        source.Should().Contain("asp-route-query=\"@query\"");
        source.Should().Contain("asp-route-filter=\"@filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void PagesController_Should_KeepWorkspaceAndEditorContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "CMS", "PagesController.cs"));

        source.Should().Contain("public sealed class PagesController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? query = null, string? filter = null, CancellationToken ct = default)");
        source.Should().Contain("var defaultCulture = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCulture;");
        source.Should().Contain("var (items, total) = await _list.HandleAsync(page, pageSize, defaultCulture, query, filter, ct);");
        source.Should().Contain("var summary = await _getPageOpsSummary.HandleAsync(ct);");
        source.Should().Contain("Playbooks = BuildPagePlaybooks()");
        source.Should().Contain("DraftCount = summary.DraftCount,");
        source.Should().Contain("PublishedCount = summary.PublishedCount,");
        source.Should().Contain("WindowedCount = summary.WindowedCount,");
        source.Should().Contain("LiveWindowCount = summary.LiveWindowCount");
        source.Should().Contain("return RenderIndex(vm);");
        source.Should().Contain("public async Task<IActionResult> Create(CancellationToken ct)");
        source.Should().Contain("await LoadCulturesAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderCreateEditor(new PageCreateVm");
        source.Should().Contain("Translations = new() { new PageTranslationVm { Culture = defaultCulture } }");
        source.Should().Contain("public async Task<IActionResult> Create(PageCreateVm vm, CancellationToken ct)");
        source.Should().Contain("ModelState.AddModelError(nameof(vm.Translations), T(\"PageTranslationRequired\"));");
        source.Should().Contain("await EnsureTranslationsAsync(vm, ct).ConfigureAwait(false);");
        source.Should().Contain("var dto = new PageCreateDto");
        source.Should().Contain("Status = vm.Status,");
        source.Should().Contain("PublishStartUtc = vm.PublishStartUtc,");
        source.Should().Contain("PublishEndUtc = vm.PublishEndUtc,");
        source.Should().Contain("Translations = vm.Translations.Select(t => new PageTranslationDto");
        source.Should().Contain("await _create.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"PageCreated\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Index), new { });");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct)");
        source.Should().Contain("var dto = await _get.HandleAsync(id, ct);");
        source.Should().Contain("SetErrorMessage(\"PageNotFound\");");
        source.Should().Contain("RowVersion = dto.RowVersion,");
        source.Should().Contain("Status = dto.Status,");
        source.Should().Contain("await LoadCulturesAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("return RenderEditEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Edit(PageEditVm vm, CancellationToken ct)");
        source.Should().Contain("var dto = new PageEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion ?? Array.Empty<byte>(),");
        source.Should().Contain("await _update.HandleAsync(dto, ct);");
        source.Should().Contain("SetSuccessMessage(\"PageUpdated\");");
        source.Should().Contain("ModelState.AddModelError(string.Empty, T(\"PageConcurrencyConflict\"));");
        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        source.Should().Contain("await _softDeletePage.HandleAsync(id, rowVersion, ct);");
        source.Should().Contain("SetSuccessMessage(\"PageDeleted\");");
        source.Should().Contain("SetErrorMessage(\"PageDeleteFailed\");");
        source.Should().Contain("private IActionResult RenderIndex(PagesIndexVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Pages/Index.cshtml\", vm);");
        source.Should().Contain("return View(\"Index\", vm);");
        source.Should().Contain("private IActionResult RenderCreateEditor(PageCreateVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Pages/_PageCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Create\", vm);");
        source.Should().Contain("private IActionResult RenderEditEditor(PageEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Pages/_PageEditEditorShell.cshtml\", vm);");
        source.Should().Contain("return View(\"Edit\", vm);");
    }


    [Fact]
    public void PagesController_Should_KeepCultureTranslationAndHtmxHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "CMS", "PagesController.cs"));

        source.Should().Contain("private async Task LoadCulturesAsync(CancellationToken ct)");
        source.Should().Contain("var (_, cultures) = await _getCultures.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("ViewBag.Cultures = cultures;");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("return new EmptyResult();");
        source.Should().Contain("return RedirectToAction(actionName, routeValues);");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private async Task EnsureTranslationsAsync(PageEditorVm vm, CancellationToken ct)");
        source.Should().Contain("vm.Translations.Add(new PageTranslationVm { Culture = defaultCulture });");
        source.Should().Contain("private PagePlaybookVm[] BuildPagePlaybooks()");
        source.Should().Contain("QueueLabel = T(\"Draft\")");
        source.Should().Contain("WhyItMatters = T(\"PagesPlaybookDraftScope\")");
        source.Should().Contain("QueueLabel = T(\"Windowed\")");
        source.Should().Contain("WhyItMatters = T(\"PagesPlaybookWindowedScope\")");
        source.Should().Contain("QueueLabel = T(\"LiveWindow\")");
        source.Should().Contain("WhyItMatters = T(\"PagesPlaybookLiveWindowScope\")");
    }


    [Fact]
    public void MediaController_Should_KeepWorkspaceEditorAndMutationContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Media", "MediaController.cs"));

        source.Should().Contain("public sealed class MediaController : AdminBaseController");
        source.Should().Contain("private static readonly string[] AllowedExtensions = [\".png\", \".jpg\", \".jpeg\", \".webp\", \".gif\"];");
        source.Should().Contain("private const long MaxUploadBytes = 5 * 1024 * 1024;");
        source.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 24, string? query = null, MediaAssetQueueFilter filter = MediaAssetQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var (items, total) = await _getPage.HandleAsync(page, pageSize, query, filter, ct).ConfigureAwait(false);");
        source.Should().Contain("var summary = await _getSummary.HandleAsync(ct).ConfigureAwait(false);");
        source.Should().Contain("Playbooks = BuildPlaybooks(),");
        source.Should().Contain("FilterItems = BuildFilterItems(filter),");
        source.Should().Contain("MissingAltCount = summary.MissingAltCount,");
        source.Should().Contain("MissingTitleCount = summary.MissingTitleCount,");
        source.Should().Contain("EditorAssetCount = summary.EditorAssetCount,");
        source.Should().Contain("LibraryAssetCount = summary.LibraryAssetCount");
        source.Should().Contain("return RenderIndex(vm);");
        source.Should().Contain("public IActionResult Create()");
        source.Should().Contain("return RenderCreateEditor(new MediaAssetCreateVm());");
        source.Should().Contain("public async Task<IActionResult> Create(MediaAssetCreateVm vm, CancellationToken ct = default)");
        source.Should().Contain("ModelState.AddModelError(nameof(vm.File), T(\"MediaUploadFileRequired\"));");
        source.Should().Contain("var validationError = ValidateFile(vm.File);");
        source.Should().Contain("var stored = await SaveUploadAsync(vm.File, ct).ConfigureAwait(false);");
        source.Should().Contain("await _create.HandleAsync(new MediaAssetCreateDto");
        source.Should().Contain("Url = stored.PublicUrl,");
        source.Should().Contain("ContentHash = stored.ContentHash,");
        source.Should().Contain("Role = string.IsNullOrWhiteSpace(vm.Role) ? \"LibraryAsset\" : vm.Role");
        source.Should().Contain("SetSuccessMessage(\"MediaUploaded\");");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getForEdit.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetErrorMessage(\"MediaAssetNotFound\");");
        source.Should().Contain("return RenderEditEditor(new MediaAssetEditVm");
        source.Should().Contain("public async Task<IActionResult> Edit(MediaAssetEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("await _update.HandleAsync(new MediaAssetEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion,");
        source.Should().Contain("SetSuccessMessage(\"MediaUpdated\");");
        source.Should().Contain("SetErrorMessage(\"MediaConcurrencyConflict\");");
        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)");
        source.Should().Contain("await _softDelete.HandleAsync(id, ct).ConfigureAwait(false);");
        source.Should().Contain("SetSuccessMessage(\"MediaDeleted\");");
        source.Should().Contain("public async Task<IActionResult> UploadQuill(IFormFile? file, CancellationToken ct)");
        source.Should().Contain("[IgnoreAntiforgeryToken]");
        source.Should().Contain("return BadRequest(new { error = T(\"MediaUploadFileRequired\") });");
        source.Should().Contain("return Json(new { url = stored.PublicUrl });");
        source.Should().Contain("if (stored is not null && System.IO.File.Exists(stored.PhysicalPath))");
        source.Should().Contain("System.IO.File.Delete(stored.PhysicalPath);");
        source.Should().Contain("private IActionResult RenderCreateEditor(MediaAssetCreateVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Media/_MediaAssetCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderEditEditor(MediaAssetEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Media/_MediaAssetEditEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderIndex(MediaAssetsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Media/Index.cshtml\", vm);");
    }


    [Fact]
    public void MediaController_Should_KeepFilterUploadAndHtmxHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Media", "MediaController.cs"));

        source.Should().Contain("private IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> BuildFilterItems(MediaAssetQueueFilter selectedFilter)");
        source.Should().Contain("new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T(\"MediaAll\"), MediaAssetQueueFilter.All.ToString(), selectedFilter == MediaAssetQueueFilter.All)");
        source.Should().Contain("new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T(\"MediaMissingAlt\"), MediaAssetQueueFilter.MissingAlt.ToString(), selectedFilter == MediaAssetQueueFilter.MissingAlt)");
        source.Should().Contain("new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T(\"MediaEditorAssets\"), MediaAssetQueueFilter.EditorAssets.ToString(), selectedFilter == MediaAssetQueueFilter.EditorAssets)");
        source.Should().Contain("new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T(\"MediaLibraryAssets\"), MediaAssetQueueFilter.LibraryAssets.ToString(), selectedFilter == MediaAssetQueueFilter.LibraryAssets)");
        source.Should().Contain("new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(T(\"MediaMissingTitle\"), MediaAssetQueueFilter.MissingTitle.ToString(), selectedFilter == MediaAssetQueueFilter.MissingTitle)");
        source.Should().Contain("private List<MediaAssetPlaybookVm> BuildPlaybooks()");
        source.Should().Contain("Title = T(\"MediaPlaybookMissingAltTitle\")");
        source.Should().Contain("Title = T(\"MediaPlaybookMissingTitleTitle\")");
        source.Should().Contain("Title = T(\"MediaPlaybookEditorAssetsTitle\")");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("return new EmptyResult();");
        source.Should().Contain("return RedirectToAction(actionName, routeValues);");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private string? ValidateFile(IFormFile file)");
        source.Should().Contain("if (file.Length == 0)");
        source.Should().Contain("return T(\"MediaUploadFileEmpty\");");
        source.Should().Contain("var ext = Path.GetExtension(file.FileName).ToLowerInvariant();");
        source.Should().Contain("if (!AllowedExtensions.Contains(ext))");
        source.Should().Contain("return T(\"MediaUploadInvalidFileType\");");
        source.Should().Contain("if (file.Length > MaxUploadBytes)");
        source.Should().Contain("return T(\"MediaUploadFileTooLarge\");");
        source.Should().Contain("private async Task<StoredUploadResult> SaveUploadAsync(IFormFile file, CancellationToken ct)");
        source.Should().Contain("var uploadsRoot = Path.Combine(GetWebRootPath(), \"uploads\");");
        source.Should().Contain("Directory.CreateDirectory(uploadsRoot);");
        source.Should().Contain("var fileName = $\"{Guid.NewGuid():N}{ext}\";");
        source.Should().Contain("await input.CopyToAsync(buffer, ct).ConfigureAwait(false);");
        source.Should().Contain("await System.IO.File.WriteAllBytesAsync(fullPath, bytes, ct).ConfigureAwait(false);");
        source.Should().Contain("var hash = Convert.ToHexString(SHA256.HashData(bytes));");
        source.Should().Contain("PublicUrl: $\"/uploads/{fileName}\"");
        source.Should().Contain("private string GetWebRootPath()");
        source.Should().Contain("if (!string.IsNullOrWhiteSpace(_env.WebRootPath))");
        source.Should().Contain("return _env.WebRootPath;");
        source.Should().Contain("return Path.Combine(_env.ContentRootPath, \"wwwroot\");");
        source.Should().Contain("private sealed record StoredUploadResult(string PhysicalPath, string PublicUrl, long SizeBytes, string ContentHash);");
    }


    [Fact]
    public void RolesController_Should_KeepWorkspaceEditorAndPermissionsContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "RolesController.cs"));

        source.Should().Contain("[PermissionAuthorize(\"FullAdminAccess\")]");
        source.Should().Contain("public sealed class RolesController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, RoleQueueFilter filter = RoleQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var (items, _) = await _getRole.HandleAsync(1, 500, q, ct);");
        source.Should().Contain("var listVms = items.Select(dto => new RoleListItemVm");
        source.Should().Contain("var filteredItems = ApplyRoleFilter(listVms, filter).ToList();");
        source.Should().Contain("FilterItems = BuildRoleFilterItems(filter),");
        source.Should().Contain("SystemCount = listVms.Count(x => x.IsSystem),");
        source.Should().Contain("CustomCount = listVms.Count(x => !x.IsSystem),");
        source.Should().Contain("DelegatedSupportCount = listVms.Count(IsDelegatedSupportRole)");
        source.Should().Contain("new SelectListItem(\"100\", \"100\", pageSize == 100),");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("public IActionResult Create()");
        source.Should().Contain("return RenderCreateEditor(new RoleCreateVm());");
        source.Should().Contain("public async Task<IActionResult> Create(RoleCreateVm model, CancellationToken ct = default)");
        source.Should().Contain("SetWarningMessage(\"ValidationErrorsRetry\");");
        source.Should().Contain("var dto = new RoleCreateDto");
        source.Should().Contain("Key = model.Key?.Trim() ?? string.Empty,");
        source.Should().Contain("DisplayName = model.DisplayName?.Trim() ?? string.Empty,");
        source.Should().Contain("var result = await _create.HandleAsync(dto, ct);");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"RoleCreateFailed\");");
        source.Should().Contain("SetSuccessMessage(\"RoleCreated\");");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var dto = await _getRoleForEdit.HandleAsync(id, ct);");
        source.Should().Contain("SetWarningMessage(\"RoleNotFound\");");
        source.Should().Contain("return RenderEditEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Edit(RoleEditVm model, CancellationToken ct = default)");
        source.Should().Contain("var dto = new RoleEditDto");
        source.Should().Contain("RowVersion = model.RowVersion,");
        source.Should().Contain("IsSystem = model.IsSystem");
        source.Should().Contain("var result = await _update.HandleAsync(dto, ct);");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"RoleUpdateFailed\");");
        source.Should().Contain("SetSuccessMessage(\"RoleUpdated\");");
        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, CancellationToken ct = default)");
        source.Should().Contain("await _delete.HandleAsync(id, ct);");
        source.Should().Contain("SetSuccessMessage(\"RoleDeleted\");");
        source.Should().Contain("TempData[\"Warning\"] = string.IsNullOrWhiteSpace(ex.Message)");
        source.Should().Contain("SetErrorMessage(\"RoleDeleteFailed\");");
        source.Should().Contain("public async Task<IActionResult> Permissions(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var vm = await BuildRolePermissionsVmAsync(id, null, null, ct);");
        source.Should().Contain("SetErrorMessage(\"RolePermissionsLoadFailed\");");
        source.Should().Contain("return RenderPermissionsEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Permissions(RolePermissionsEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new RolePermissionsUpdateDto");
        source.Should().Contain("PermissionIds = vm.SelectedPermissionIds?.ToList() ?? new List<Guid>()");
        source.Should().Contain("var result = await _updateRolePerms.HandleAsync(dto, ct);");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"RolePermissionsUpdateFailed\");");
        source.Should().Contain("SetSuccessMessage(\"RolePermissionsUpdated\");");
        source.Should().Contain("return RedirectOrHtmx(nameof(Permissions), new { id = vm.RoleId });");
        source.Should().Contain("private IActionResult RenderCreateEditor(RoleCreateVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Roles/_RoleCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(RolesListItemVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Roles/Index.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderEditEditor(RoleEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Roles/_RoleEditEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderPermissionsEditor(RolePermissionsEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Roles/_RolePermissionsEditorShell.cshtml\", vm);");
    }


    [Fact]
    public void RolesController_Should_KeepFilterPermissionBuilderAndHtmxHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "RolesController.cs"));

        source.Should().Contain("private static IEnumerable<RoleListItemVm> ApplyRoleFilter(IEnumerable<RoleListItemVm> items, RoleQueueFilter filter)");
        source.Should().Contain("RoleQueueFilter.System => items.Where(x => x.IsSystem),");
        source.Should().Contain("RoleQueueFilter.Custom => items.Where(x => !x.IsSystem),");
        source.Should().Contain("RoleQueueFilter.DelegatedSupport => items.Where(IsDelegatedSupportRole),");
        source.Should().Contain("private static bool IsDelegatedSupportRole(RoleListItemVm item)");
        source.Should().Contain("return string.Equals(item.Key, \"business-support-admins\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildRoleFilterItems(RoleQueueFilter selected)");
        source.Should().Contain("new(T(\"IdentityFilterAll\"), RoleQueueFilter.All.ToString(), selected == RoleQueueFilter.All),");
        source.Should().Contain("new(T(\"IdentityFilterSystem\"), RoleQueueFilter.System.ToString(), selected == RoleQueueFilter.System),");
        source.Should().Contain("new(T(\"IdentityFilterCustom\"), RoleQueueFilter.Custom.ToString(), selected == RoleQueueFilter.Custom),");
        source.Should().Contain("new(T(\"IdentityFilterDelegatedSupport\"), RoleQueueFilter.DelegatedSupport.ToString(), selected == RoleQueueFilter.DelegatedSupport)");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("return new EmptyResult();");
        source.Should().Contain("return RedirectToAction(actionName, routeValues);");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private async Task<RolePermissionsEditVm?> BuildRolePermissionsVmAsync(");
        source.Should().Contain("var result = await _getRolePerms.HandleAsync(roleId, ct);");
        source.Should().Contain("if (!result.Succeeded || result.Value is null)");
        source.Should().Contain("return null;");
        source.Should().Contain("RoleId = dto.RoleId,");
        source.Should().Contain("RoleDisplayName = dto.RoleDisplayName,");
        source.Should().Contain("RowVersion = rowVersion ?? dto.RowVersion,");
        source.Should().Contain("AllPermissions = dto.AllPermissions.Select(p => new PermissionItemVm");
        source.Should().Contain("SelectedPermissionIds = selectedPermissionIds?.ToList() ?? dto.PermissionIds.ToList()");
    }


    [Fact]
    public void PermissionsController_Should_KeepWorkspaceEditorAndMutationContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "PermissionsController.cs"));

        source.Should().Contain("[Route(\"Admin/[controller]/[action]\")]");
        source.Should().Contain("[PermissionAuthorize(\"FullAdminAccess\")]");
        source.Should().Contain("public sealed class PermissionsController : AdminBaseController");
        source.Should().Contain("public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null, PermissionQueueFilter filter = PermissionQueueFilter.All, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getPage.HandleAsync(1, 500, q, ct);");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"PermissionsLoadFailed\");");
        source.Should().Contain("return RenderIndexWorkspace(new PermissionsListVm());");
        source.Should().Contain("var listItems = pageData.Items");
        source.Should().Contain("var filteredItems = ApplyPermissionFilter(listItems, filter).ToList();");
        source.Should().Contain("FilterItems = BuildPermissionFilterItems(filter),");
        source.Should().Contain("SystemCount = listItems.Count(x => x.IsSystem),");
        source.Should().Contain("CustomCount = listItems.Count(x => !x.IsSystem),");
        source.Should().Contain("DelegatedSupportCount = listItems.Count(IsDelegatedSupportPermission)");
        source.Should().Contain("new SelectListItem(\"100\", \"100\", pageSize == 100),");
        source.Should().Contain("return RenderIndexWorkspace(vm);");
        source.Should().Contain("public IActionResult Create()");
        source.Should().Contain("return RenderCreateEditor(new PermissionCreateVm());");
        source.Should().Contain("public async Task<IActionResult> Create(PermissionCreateVm vm, CancellationToken ct = default)");
        source.Should().Contain("SetWarningMessage(\"ValidationErrorsRetry\");");
        source.Should().Contain("var result = await _create.HandleAsync(vm.Key?.Trim() ?? string.Empty,");
        source.Should().Contain("vm.DisplayName?.Trim() ?? string.Empty,");
        source.Should().Contain("string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),");
        source.Should().Contain("false, ct);");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"PermissionCreateFailed\");");
        source.Should().Contain("SetSuccessMessage(\"PermissionCreated\");");
        source.Should().Contain("public async Task<IActionResult> Edit(Guid id, CancellationToken ct = default)");
        source.Should().Contain("var result = await _getForEdit.HandleAsync(id, ct);");
        source.Should().Contain("TempData[\"Warning\"] = result.Error ?? T(\"PermissionNotFound\");");
        source.Should().Contain("return RenderEditEditor(vm);");
        source.Should().Contain("public async Task<IActionResult> Edit(PermissionEditVm vm, CancellationToken ct = default)");
        source.Should().Contain("var dto = new PermissionEditDto");
        source.Should().Contain("RowVersion = vm.RowVersion,");
        source.Should().Contain("DisplayName = vm.DisplayName?.Trim() ?? string.Empty,");
        source.Should().Contain("var result = await _update.HandleAsync(dto, ct);");
        source.Should().Contain("TempData[\"Error\"] = result.Error ?? T(\"PermissionUpdateFailed\");");
        source.Should().Contain("SetSuccessMessage(\"PermissionUpdated\");");
        source.Should().Contain("public async Task<IActionResult> Delete([FromForm] Guid id, [FromForm] byte[]? rowVersion, CancellationToken ct = default)");
        source.Should().Contain("var dto = new PermissionDeleteDto { Id = id, RowVersion = rowVersion ?? Array.Empty<byte>() };");
        source.Should().Contain("var result = await _softDelete.HandleAsync(dto, ct);");
        source.Should().Contain("TempData[\"Warning\"] = result.Error ?? T(\"PermissionDeleteFailed\");");
        source.Should().Contain("SetSuccessMessage(\"PermissionDeleted\");");
        source.Should().Contain("SetErrorMessage(\"PermissionDeleteFailed\");");
        source.Should().Contain("private IActionResult RenderCreateEditor(PermissionCreateVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Permissions/_PermissionCreateEditorShell.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderIndexWorkspace(PermissionsListVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Permissions/Index.cshtml\", vm);");
        source.Should().Contain("private IActionResult RenderEditEditor(PermissionEditVm vm)");
        source.Should().Contain("return PartialView(\"~/Views/Permissions/_PermissionEditEditorShell.cshtml\", vm);");
    }


    [Fact]
    public void PermissionsController_Should_KeepFilterAndHtmxHelperContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "PermissionsController.cs"));

        source.Should().Contain("private static IEnumerable<PermissionListItemVm> ApplyPermissionFilter(IEnumerable<PermissionListItemVm> items, PermissionQueueFilter filter)");
        source.Should().Contain("PermissionQueueFilter.System => items.Where(x => x.IsSystem),");
        source.Should().Contain("PermissionQueueFilter.Custom => items.Where(x => !x.IsSystem),");
        source.Should().Contain("PermissionQueueFilter.DelegatedSupport => items.Where(IsDelegatedSupportPermission),");
        source.Should().Contain("private static bool IsDelegatedSupportPermission(PermissionListItemVm item)");
        source.Should().Contain("return string.Equals(item.Key, \"ManageBusinessSupport\", StringComparison.OrdinalIgnoreCase);");
        source.Should().Contain("private IEnumerable<SelectListItem> BuildPermissionFilterItems(PermissionQueueFilter selected)");
        source.Should().Contain("new(T(\"IdentityFilterAll\"), PermissionQueueFilter.All.ToString(), selected == PermissionQueueFilter.All),");
        source.Should().Contain("new(T(\"IdentityFilterSystem\"), PermissionQueueFilter.System.ToString(), selected == PermissionQueueFilter.System),");
        source.Should().Contain("new(T(\"IdentityFilterCustom\"), PermissionQueueFilter.Custom.ToString(), selected == PermissionQueueFilter.Custom),");
        source.Should().Contain("new(T(\"IdentityFilterDelegatedSupport\"), PermissionQueueFilter.DelegatedSupport.ToString(), selected == PermissionQueueFilter.DelegatedSupport)");
        source.Should().Contain("private IActionResult RedirectOrHtmx(string actionName, object routeValues)");
        source.Should().Contain("Response.Headers[\"HX-Redirect\"] = Url.Action(actionName, routeValues) ?? string.Empty;");
        source.Should().Contain("return new EmptyResult();");
        source.Should().Contain("return RedirectToAction(actionName, routeValues);");
        source.Should().Contain("private bool IsHtmxRequest()");
        source.Should().Contain("return string.Equals(Request.Headers[\"HX-Request\"], \"true\", StringComparison.OrdinalIgnoreCase);");
    }


    [Fact]
    public void IdentityAdminVisibility_Should_KeepDelegatedSupportAndBusinessAwareSupportRailsWired()
    {
        var usersSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "UsersController.cs"));
        var rolesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "RolesController.cs"));
        var permissionsSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Identity", "PermissionsController.cs"));
        var businessesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var usersIndexSource = ReadWebAdminFile(Path.Combine("Views", "Users", "Index.cshtml"));
        var businessMemberEditorSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        usersSource.Should().Contain("yield return new SelectListItem(T(\"UsersFilterMobileLinked\"), UserQueueFilter.MobileLinked.ToString(), selectedFilter == UserQueueFilter.MobileLinked);");
        usersSource.Should().Contain("OpensMobileOperations = true");
        usersIndexSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Users\", new { filter = \"MobileLinked\" })\"");
        usersIndexSource.Should().Contain("@Model.Summary.MobileLinkedCount");

        rolesSource.Should().Contain("return string.Equals(item.Key, \"business-support-admins\", StringComparison.OrdinalIgnoreCase);");
        permissionsSource.Should().Contain("return string.Equals(item.Key, \"ManageBusinessSupport\", StringComparison.OrdinalIgnoreCase);");

        businessesSource.Should().Contain("[PermissionAuthorize(PermissionKeys.ManageBusinessSupport)]");
        businessesSource.Should().Contain("public async Task<IActionResult> Members(");
        businessesSource.Should().Contain("Playbooks = BuildBusinessMemberPlaybooks(businessId),");
        businessesSource.Should().Contain("public async Task<IActionResult> SendMemberActivationEmail(");
        businessesSource.Should().Contain("public async Task<IActionResult> ConfirmMemberEmail(");
        businessesSource.Should().Contain("public async Task<IActionResult> SendMemberPasswordReset(");
        businessesSource.Should().Contain("public async Task<IActionResult> LockMemberUser(");
        businessesSource.Should().Contain("public async Task<IActionResult> UnlockMemberUser(");

        businessMemberEditorSource.Should().Contain("@T.T(\"BusinessMemberSupportActionsTitle\")");
        businessMemberEditorSource.Should().Contain("hx-post=\"@Url.Action(\"SendMemberActivationEmail\", \"Businesses\")\"");
        businessMemberEditorSource.Should().Contain("hx-post=\"@Url.Action(\"ConfirmMemberEmail\", \"Businesses\")\"");
        businessMemberEditorSource.Should().Contain("hx-post=\"@Url.Action(\"SendMemberPasswordReset\", \"Businesses\")\"");
        businessMemberEditorSource.Should().Contain("hx-post=\"@Url.Action(\"LockMemberUser\", \"Businesses\")\"");
        businessMemberEditorSource.Should().Contain("hx-post=\"@Url.Action(\"UnlockMemberUser\", \"Businesses\")\"");
    }


    [Fact]
    public void PagesWorkspace_Should_KeepShellSummaryFilterAndGridContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Pages", "Index.cshtml"));

        source.Should().Contain("id=\"pages-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"PagesWorkspaceIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Pages\")\"");
        source.Should().Contain("name=\"query\" value=\"@query\"");
        source.Should().Contain("name=\"filter\" value=\"@filter\"");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Pages\")\"");
        source.Should().Contain("@T.T(\"CreatePage\")");
        source.Should().Contain("@T.T(\"Draft\")</div><div class=\"display-6 fw-semibold\">@Model.Summary.DraftCount");
        source.Should().Contain("@T.T(\"Published\")</div><div class=\"display-6 fw-semibold\">@Model.Summary.PublishedCount");
        source.Should().Contain("@T.T(\"Windowed\")</div><div class=\"display-6 fw-semibold\">@Model.Summary.WindowedCount");
        source.Should().Contain("@T.T(\"LiveWindow\")</div><div class=\"display-6 fw-semibold\">@Model.Summary.LiveWindowCount");
        source.Should().Contain("asp-route-filter=\"draft\"");
        source.Should().Contain("asp-route-filter=\"published\"");
        source.Should().Contain("asp-route-filter=\"windowed\"");
        source.Should().Contain("asp-route-filter=\"live-window\"");
        source.Should().Contain("@T.T(\"TotalPages\"): @Model.Summary.TotalCount");
        source.Should().Contain("@T.T(\"ContentOperationsPlaybooks\")");
        source.Should().Contain("@playbook.QueueLabel");
        source.Should().Contain("@playbook.WhyItMatters");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("@T.T(\"PrimaryTitleDe\")");
        source.Should().Contain("@T.T(\"PublishWindowUtc\")");
        source.Should().Contain("@T.T(\"NoPagesFound\")");
        source.Should().Contain("@T.T(\"Live\")");
        source.Should().Contain("@T.T(\"Windowed\")");
        source.Should().Contain("@p.Status");
        source.Should().Contain("@(p.PublishStartUtc?.ToString(\"u\") ?? \"-\") @T.T(\"To\") @(p.PublishEndUtc?.ToString(\"u\") ?? \"-\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Pages\", new { id = p.Id })\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Pages\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(p.RowVersion)\"");
        source.Should().Contain("<pager page=\"@currentPage\"");
        source.Should().Contain("asp-controller=\"Pages\"");
        source.Should().Contain("asp-route-query=\"@query\"");
        source.Should().Contain("asp-route-filter=\"@filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void MediaWorkspace_Should_KeepShellSummaryQueueAndGridContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Media", "Index.cshtml"));

        source.Should().Contain("id=\"media-workspace-shell\"");
        source.Should().Contain("@T.T(\"MediaLibraryTitle\")");
        source.Should().Contain("@T.T(\"MediaLibraryIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Media\")\"");
        source.Should().Contain("@T.T(\"UploadMedia\")");
        source.Should().Contain("asp-route-filter=\"MissingAlt\"");
        source.Should().Contain("asp-route-filter=\"MissingTitle\"");
        source.Should().Contain("asp-route-filter=\"EditorAssets\"");
        source.Should().Contain("asp-route-filter=\"LibraryAssets\"");
        source.Should().Contain("@T.T(\"ClearQueueFilters\")");
        source.Should().Contain("@T.T(\"MediaMissingAlt\")");
        source.Should().Contain("@Model.Summary.MissingAltCount");
        source.Should().Contain("@T.T(\"MediaMissingTitle\")");
        source.Should().Contain("@Model.Summary.MissingTitleCount");
        source.Should().Contain("@T.T(\"MediaEditorAssets\")");
        source.Should().Contain("@Model.Summary.EditorAssetCount");
        source.Should().Contain("@T.T(\"MediaLibraryAssets\")");
        source.Should().Contain("@Model.Summary.LibraryAssetCount");
        source.Should().Contain("@T.T(\"MediaOpsPlaybooksTitle\")");
        source.Should().Contain("@item.Title");
        source.Should().Contain("@item.ScopeNote");
        source.Should().Contain("@item.OperatorAction");
        source.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"MediaSearchPlaceholder\")");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("@T.T(\"MediaNoAssetsFound\")");
        source.Should().Contain("@item.OriginalFileName");
        source.Should().Contain("@T.T(\"MediaEditorAsset\")");
        source.Should().Contain("@T.T(\"MediaLibraryAsset\")");
        source.Should().Contain("@T.T(\"AltLabel\"):");
        source.Should().Contain("@T.T(\"Role\"):");
        source.Should().Contain("@T.T(\"Size\"):");
        source.Should().Contain("@T.T(\"Updated\"):");
        source.Should().Contain("class=\"btn btn-outline-secondary js-copy-media-url\" data-url=\"@item.Url\">@T.T(\"Copy\")</button>");
        source.Should().Contain("@T.T(\"Open\")");
        source.Should().Contain("@T.T(\"MediaSetAlt\")");
        source.Should().Contain("@T.T(\"MediaSetTitle\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Media\", new { id = item.Id })\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Media\")\"");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("asp-controller=\"Media\"");
        source.Should().Contain("asp-route-query=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
        source.Should().Contain("document.querySelectorAll('.js-copy-media-url').forEach(function (button)");
        source.Should().Contain("navigator.clipboard.writeText(url);");
        source.Should().Contain("button.textContent = '@T.T(\"Copied\")';");
        source.Should().Contain("button.textContent = '@T.T(\"Copy\")';");
    }


    [Fact]
    public void MediaEditorShells_Should_KeepCreateAndEditFormContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "Media", "_MediaAssetCreateEditorShell.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "Media", "_MediaAssetEditEditorShell.cshtml"));

        createSource.Should().Contain("id=\"media-create-editor-shell\"");
        createSource.Should().Contain("@T.T(\"UploadMedia\")");
        createSource.Should().Contain("asp-action=\"Create\"");
        createSource.Should().Contain("enctype=\"multipart/form-data\"");
        createSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"Media\")\"");
        createSource.Should().Contain("hx-encoding=\"multipart/form-data\"");
        createSource.Should().Contain("hx-target=\"#media-create-editor-shell\"");
        createSource.Should().Contain("@Html.AntiForgeryToken()");
        createSource.Should().Contain("asp-for=\"File\" class=\"form-control\" accept=\".png,.jpg,.jpeg,.webp,.gif\"");
        createSource.Should().Contain("asp-validation-for=\"File\" class=\"text-danger\"");
        createSource.Should().Contain("@T.T(\"MediaSupportedFormatsHelp\")");
        createSource.Should().Contain("<partial name=\"_MediaAssetUploadForm\" model=\"Model\" />");
        createSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger mt-3\"");
        createSource.Should().Contain("@T.T(\"Upload\")");
        createSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Media\")\"");
        createSource.Should().Contain("@T.T(\"Cancel\")");

        editSource.Should().Contain("id=\"media-edit-editor-shell\"");
        editSource.Should().Contain("@T.T(\"MediaEditTitle\")");
        editSource.Should().Contain("@Model.OriginalFileName");
        editSource.Should().Contain("href=\"@Model.Url\" target=\"_blank\" rel=\"noreferrer\"");
        editSource.Should().Contain("@T.T(\"MediaOpenFile\")");
        editSource.Should().Contain("<img src=\"@Model.Url\" alt=\"@Model.Alt\" class=\"w-100 h-100 object-fit-cover\" />");
        editSource.Should().Contain("@T.T(\"Url\"):");
        editSource.Should().Contain("@T.T(\"Dimensions\"):");
        editSource.Should().Contain("@(Model.Width.HasValue && Model.Height.HasValue ? $\"{Model.Width} x {Model.Height}\" : T.T(\"MediaUnknownValue\"))");
        editSource.Should().Contain("@(Model.ModifiedAtUtc?.ToLocalTime().ToString(\"yyyy-MM-dd HH:mm\") ?? T.T(\"MediaUnknownValue\"))");
        editSource.Should().Contain("asp-action=\"Edit\"");
        editSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"Media\")\"");
        editSource.Should().Contain("hx-target=\"#media-edit-editor-shell\"");
        editSource.Should().Contain("@Html.AntiForgeryToken()");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editSource.Should().Contain("<partial name=\"_MediaAssetUploadForm\" model=\"Model\" />");
        editSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger mt-3\"");
        editSource.Should().Contain("@T.T(\"Save\")");
        editSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Media\")\"");
        editSource.Should().Contain("@T.T(\"Back\")");
    }


    [Fact]
    public void RolesWorkspace_Should_KeepShellSummaryFilterGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Roles", "Index.cshtml"));

        source.Should().Contain("id=\"roles-workspace-shell\"");
        source.Should().Contain("@T.T(\"Roles\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.SystemCount");
        source.Should().Contain("@Model.Summary.CustomCount");
        source.Should().Contain("@Model.Summary.DelegatedSupportCount");
        source.Should().Contain("@T.T(\"RoleOpsPlaybook\")");
        source.Should().Contain("@T.T(\"RoleOpsPlaybookSystem\")");
        source.Should().Contain("@T.T(\"RoleOpsPlaybookDelegatedSupport\")");
        source.Should().Contain("@T.T(\"RoleOpsPlaybookCustom\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Roles\")\"");
        source.Should().Contain("id=\"q\" name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("id=\"pageSize\" name=\"pageSize\" class=\"form-select\" asp-items=\"Model.PageSizeItems\"");
        source.Should().Contain("id=\"filter\" name=\"filter\" class=\"form-select\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"RoleSearchHelp\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Roles\")\"");
        source.Should().Contain("@T.T(\"NewRole\")");
        source.Should().Contain("@T.T(\"DisplayName\")");
        source.Should().Contain("@T.T(\"Description\")");
        source.Should().Contain("@T.T(\"System\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoRolesFound\")");
        source.Should().Contain("string.Equals(r.Key, \"business-support-admins\", StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("@T.T(\"DelegatedSupport\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Roles\", new { id = r.Id })\"");
        source.Should().Contain("hx-get=\"@Url.Action(\"Permissions\", \"Roles\", new { id = r.Id })\"");
        source.Should().Contain("@T.T(\"Permissions\")");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Roles\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(r.RowVersion)\"");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("asp-controller=\"Roles\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void RoleEditorShells_Should_KeepCreateEditAndPermissionsContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "Roles", "_RoleCreateEditorShell.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "Roles", "_RoleEditEditorShell.cshtml"));
        var permissionsSource = ReadWebAdminFile(Path.Combine("Views", "Roles", "_RolePermissionsEditorShell.cshtml"));

        createSource.Should().Contain("id=\"role-editor-shell\"");
        createSource.Should().Contain("@T.T(\"CreateRole\")");
        createSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        createSource.Should().Contain("asp-action=\"Create\"");
        createSource.Should().Contain("class=\"needs-validation\"");
        createSource.Should().Contain("novalidate");
        createSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"Roles\")\"");
        createSource.Should().Contain("@Html.AntiForgeryToken()");
        createSource.Should().Contain("asp-validation-summary=\"ModelOnly\" class=\"text-danger\"");
        createSource.Should().Contain("asp-for=\"Key\" class=\"form-control\"");
        createSource.Should().Contain("@T.T(\"RoleKeyHelp\")");
        createSource.Should().Contain("asp-validation-for=\"Key\" class=\"text-danger\"");
        createSource.Should().Contain("asp-for=\"DisplayName\" class=\"form-control\"");
        createSource.Should().Contain("@T.T(\"AdminDisplayNameHelp\")");
        createSource.Should().Contain("asp-for=\"Description\" rows=\"3\" class=\"form-control\"");
        createSource.Should().Contain("@T.T(\"RoleDescriptionHelp\")");
        createSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Roles\")\"");
        createSource.Should().Contain("@T.T(\"Back\")");
        createSource.Should().Contain("@T.T(\"Save\")");

        editSource.Should().Contain("id=\"role-editor-shell\"");
        editSource.Should().Contain("@T.T(\"EditRole\")");
        editSource.Should().Contain("asp-action=\"Edit\"");
        editSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"Roles\")\"");
        editSource.Should().Contain("@Html.AntiForgeryToken()");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Key\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"IsSystem\" />");
        editSource.Should().Contain("value=\"@Model.Key\" readonly");
        editSource.Should().Contain("@T.T(\"ImmutableKeyHelp\")");
        editSource.Should().Contain("@T.T(\"SystemRole\")");
        editSource.Should().Contain("@T.T(\"System\")");
        editSource.Should().Contain("@T.T(\"No\")");
        editSource.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Roles\")\"");
        editSource.Should().Contain("data-rowversion=\"@Convert.ToBase64String(Model.RowVersion)\"");
        editSource.Should().Contain("data-name=\"@Model.DisplayName\"");
        editSource.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");

        permissionsSource.Should().Contain("id=\"role-permissions-workspace-shell\"");
        permissionsSource.Should().Contain("@T.T(\"PermissionsForRole\") - @Model.RoleDisplayName");
        permissionsSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        permissionsSource.Should().Contain("asp-action=\"Permissions\"");
        permissionsSource.Should().Contain("hx-post=\"@Url.Action(\"Permissions\", \"Roles\")\"");
        permissionsSource.Should().Contain("@Html.AntiForgeryToken()");
        permissionsSource.Should().Contain("<input type=\"hidden\" asp-for=\"RoleId\" />");
        permissionsSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        permissionsSource.Should().Contain("@T.T(\"Permissions\")");
        permissionsSource.Should().Contain("@if (Model.AllPermissions?.Count > 0)");
        permissionsSource.Should().Contain("name=\"SelectedPermissionIds\"");
        permissionsSource.Should().Contain("@p.DisplayName");
        permissionsSource.Should().Contain("@p.Key - @p.Description");
        permissionsSource.Should().Contain("@T.T(\"NoPermissionsFound\")");
        permissionsSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Roles\")\"");
        permissionsSource.Should().Contain("@T.T(\"Back\")");
        permissionsSource.Should().Contain("@T.T(\"Save\")");
    }


    [Fact]
    public void PermissionsWorkspace_Should_KeepShellSummaryFilterGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Permissions", "Index.cshtml"));

        source.Should().Contain("id=\"permissions-workspace-shell\"");
        source.Should().Contain("@T.T(\"Permissions\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.SystemCount");
        source.Should().Contain("@Model.Summary.CustomCount");
        source.Should().Contain("@Model.Summary.DelegatedSupportCount");
        source.Should().Contain("@T.T(\"PermissionOpsPlaybook\")");
        source.Should().Contain("@T.T(\"PermissionOpsPlaybookSystem\")");
        source.Should().Contain("@T.T(\"PermissionOpsPlaybookDelegatedSupport\")");
        source.Should().Contain("@T.T(\"PermissionOpsPlaybookCustom\")");
        source.Should().Contain("@T.T(\"DelegatedBusinessSupport\")");
        source.Should().Contain("@T.T(\"PermissionDelegatedSupportNote\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Permissions\")\"");
        source.Should().Contain("id=\"q\" name=\"q\" value=\"@Model.Query\"");
        source.Should().Contain("id=\"pageSize\" name=\"pageSize\" class=\"form-select\" asp-items=\"Model.PageSizeItems\"");
        source.Should().Contain("id=\"filter\" name=\"filter\" class=\"form-select\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"PermissionSearchHelp\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Permissions\")\"");
        source.Should().Contain("@T.T(\"NewPermission\")");
        source.Should().Contain("@T.T(\"Key\")");
        source.Should().Contain("@T.T(\"DisplayName\")");
        source.Should().Contain("@T.T(\"Description\")");
        source.Should().Contain("@T.T(\"System\")");
        source.Should().Contain("@T.T(\"Actions\")");
        source.Should().Contain("@T.T(\"NoPermissionsFound\")");
        source.Should().Contain("string.Equals(p.Key, \"ManageBusinessSupport\", StringComparison.OrdinalIgnoreCase)");
        source.Should().Contain("@T.T(\"DelegatedSupport\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Permissions\", new { id = p.Id })\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Permissions\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(p.RowVersion)\"");
        source.Should().Contain("<pager page=\"Model.Page\"");
        source.Should().Contain("asp-controller=\"Permissions\"");
        source.Should().Contain("asp-route-q=\"@Model.Query\"");
        source.Should().Contain("asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void AddOnGroupsWorkspace_Should_KeepShellSummaryFilterGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "Index.cshtml"));

        source.Should().Contain("id=\"add-on-groups-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"AddOnGroups\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@Model.Summary.InactiveCount");
        source.Should().Contain("@Model.Summary.GlobalCount");
        source.Should().Contain("@Model.Summary.UnattachedCount");
        source.Should().Contain("@T.T(\"AddOnLinkedGroups\"): @Model.Summary.VariantLinkedCount");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"AddOnGroups\")\"");
        source.Should().Contain("name=\"query\" value=\"@Model.Query\"");
        source.Should().Contain("name=\"filter\" asp-items=\"Model.FilterItems\"");
        source.Should().Contain("@T.T(\"AddOnSearchNameCurrency\")");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"AddOnGroups\")\"");
        source.Should().Contain("@T.T(\"AddOnNewGroup\")");
        source.Should().Contain("@T.T(\"AddOnOperationsPlaybooks\")");
        source.Should().Contain("@playbook.QueueLabel");
        source.Should().Contain("@playbook.WhyItMatters");
        source.Should().Contain("@playbook.OperatorAction");
        source.Should().Contain("@T.T(\"Currency\")");
        source.Should().Contain("@T.T(\"Global\")");
        source.Should().Contain("@T.T(\"Active\")");
        source.Should().Contain("@T.T(\"Options\")");
        source.Should().Contain("@T.T(\"Attachments\")");
        source.Should().Contain("@T.T(\"Modified\")");
        source.Should().Contain("@T.T(\"AddOnNoGroups\")");
        source.Should().Contain("@g.Name");
        source.Should().Contain("@g.Currency");
        source.Should().Contain("@(g.IsActive ? T.T(\"Yes\") : T.T(\"No\"))");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"AddOnGroups\", new { id = g.Id })\"");
        source.Should().Contain("asp-action=\"AttachToVariants\"");
        source.Should().Contain("asp-action=\"AttachToProducts\"");
        source.Should().Contain("asp-action=\"AttachToCategories\"");
        source.Should().Contain("asp-action=\"AttachToBrands\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"AddOnGroups\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(g.RowVersion)\"");
        source.Should().Contain("<pager page=\"Model.Page\" page-size=\"Model.PageSize\" total=\"Model.Total\" asp-controller=\"AddOnGroups\" asp-action=\"Index\" asp-route-query=\"@Model.Query\" asp-route-filter=\"@Model.Filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }


    [Fact]
    public void AddOnGroupEditorAndAttachSurfaces_Should_KeepCreateEditAndAttachProductsContractsWired()
    {
        var createSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_AddOnGroupCreateEditorShell.cshtml"));
        var editSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "_AddOnGroupEditEditorShell.cshtml"));
        var attachProductsSource = ReadWebAdminFile(Path.Combine("Views", "AddOnGroups", "AttachToProducts.cshtml"));

        createSource.Should().Contain("id=\"add-on-group-editor-shell\"");
        createSource.Should().Contain("@T.T(\"AddOnNewGroup\")");
        createSource.Should().Contain("@T.T(\"AddOnCreateIntro\")");
        createSource.Should().Contain("asp-action=\"Create\"");
        createSource.Should().Contain("class=\"needs-validation\"");
        createSource.Should().Contain("novalidate");
        createSource.Should().Contain("hx-post=\"@Url.Action(\"Create\", \"AddOnGroups\")\"");
        createSource.Should().Contain("@Html.AntiForgeryToken()");
        createSource.Should().Contain("<partial name=\"_AddOnGroupForm\" model=\"Model\" />");
        createSource.Should().Contain("const currency = shell.querySelector('#Currency');");
        createSource.Should().Contain("currency.dataset.uppercaseBound = 'true';");
        createSource.Should().Contain("this.value = this.value.toUpperCase();");

        editSource.Should().Contain("id=\"add-on-group-editor-shell\"");
        editSource.Should().Contain("@T.T(\"AddOnEditHeading\") @Model.Name");
        editSource.Should().Contain("@T.T(\"AddOnEditIntro\")");
        editSource.Should().Contain("asp-action=\"Edit\"");
        editSource.Should().Contain("hx-post=\"@Url.Action(\"Edit\", \"AddOnGroups\")\"");
        editSource.Should().Contain("@Html.AntiForgeryToken()");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"Id\" />");
        editSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        editSource.Should().Contain("<partial name=\"_AddOnGroupForm\" model=\"Model\" />");
        editSource.Should().Contain("const currency = shell.querySelector('#Currency');");

        attachProductsSource.Should().Contain("id=\"add-on-group-attach-products-shell\"");
        attachProductsSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        attachProductsSource.Should().Contain("@T.T(\"AddOnAttachProductsTitle\") <small class=\"text-muted\">(@Model.AddOnGroupName)</small>");
        attachProductsSource.Should().Contain("asp-action=\"AttachToProducts\"");
        attachProductsSource.Should().Contain("hx-get=\"@Url.Action(\"AttachToProducts\", \"AddOnGroups\")\"");
        attachProductsSource.Should().Contain("name=\"id\" value=\"@Model.AddOnGroupId\"");
        attachProductsSource.Should().Contain("@T.T(\"AddOnSearchProducts\")");
        attachProductsSource.Should().Contain("@T.T(\"Reset\")");
        attachProductsSource.Should().Contain("@Html.AntiForgeryToken()");
        attachProductsSource.Should().Contain("name=\"AddOnGroupId\" value=\"@Model.AddOnGroupId\"");
        attachProductsSource.Should().Contain("name=\"RowVersion\" value=\"@(Model.RowVersion is null ? \"\" : Convert.ToBase64String(Model.RowVersion))\"");
        attachProductsSource.Should().Contain("name=\"Page\" value=\"@Model.Page\"");
        attachProductsSource.Should().Contain("name=\"PageSize\" value=\"@Model.PageSize\"");
        attachProductsSource.Should().Contain("name=\"Query\" value=\"@Model.Query\"");
        attachProductsSource.Should().Contain("id=\"toggleAll\"");
        attachProductsSource.Should().Contain("class=\"row-check\" name=\"SelectedProductIds\" value=\"@p.Id\"");
        attachProductsSource.Should().Contain("@T.T(\"AddOnNoProducts\")");
        attachProductsSource.Should().Contain("@T.T(\"AddOnSaveAttachments\")");
        attachProductsSource.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"AddOnGroups\")\"");
        attachProductsSource.Should().Contain("@T.T(\"Back\")");
        attachProductsSource.Should().Contain("<pager page=\"Model.Page\" page-size=\"Model.PageSize\" total=\"Model.Total\" asp-controller=\"AddOnGroups\" asp-action=\"AttachToProducts\" asp-route-id=\"@Model.AddOnGroupId\" asp-route-query=\"@Model.Query\"");
        attachProductsSource.Should().Contain("document.getElementById('toggleAll');");
        attachProductsSource.Should().Contain("document.querySelectorAll('.row-check').forEach(cb => cb.checked = toggle.checked);");
    }


    [Fact]
    public void ProductsWorkspace_Should_KeepShellSummaryQueueGridAndPagerContractsWired()
    {
        var source = ReadWebAdminFile(Path.Combine("Views", "Products", "Index.cshtml"));

        source.Should().Contain("id=\"products-workspace-shell\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        source.Should().Contain("@T.T(\"Products\")");
        source.Should().Contain("@T.T(\"ProductsWorkspaceIntro\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Index\", \"Products\")\"");
        source.Should().Contain("name=\"query\" value=\"@query\"");
        source.Should().Contain("name=\"filter\" value=\"@filter\"");
        source.Should().Contain("@T.T(\"SearchProductsPlaceholder\")");
        source.Should().Contain("@T.T(\"Search\")");
        source.Should().Contain("@T.T(\"Reset\")");
        source.Should().Contain("hx-get=\"@Url.Action(\"Create\", \"Products\")\"");
        source.Should().Contain("@T.T(\"CreateProduct\")");
        source.Should().Contain("@Model.Summary.TotalCount");
        source.Should().Contain("@T.T(\"ProductsOpsTotalNote\")");
        source.Should().Contain("@Model.Summary.InactiveCount");
        source.Should().Contain("@T.T(\"ProductsOpsInactiveNote\")");
        source.Should().Contain("@Model.Summary.HiddenCount");
        source.Should().Contain("@T.T(\"ProductsOpsHiddenNote\")");
        source.Should().Contain("@Model.Summary.ScheduledCount");
        source.Should().Contain("@T.T(\"ProductsOpsScheduledNote\")");
        source.Should().Contain("@Model.Summary.SingleVariantCount");
        source.Should().Contain("@T.T(\"ProductsOpsSingleVariantNote\")");
        source.Should().Contain("@T.T(\"ActiveCatalogWindow\")");
        source.Should().Contain("@T.T(\"ProductsOpsVisibilityWindowNote\")");
        source.Should().Contain("@T.T(\"CatalogOperationsPlaybooks\")");
        source.Should().Contain("@playbook.QueueLabel");
        source.Should().Contain("@playbook.WhyItMatters");
        source.Should().Contain("@T.T(\"OperatorAction\"):");
        source.Should().Contain("asp-route-filter=\"inactive\"");
        source.Should().Contain("asp-route-filter=\"hidden\"");
        source.Should().Contain("asp-route-filter=\"single-variant\"");
        source.Should().Contain("asp-route-filter=\"scheduled\"");
        source.Should().Contain("@T.T(\"TotalProductsLabel\"): @Model.Summary.TotalCount");
        source.Should().Contain("@T.T(\"ProductNameDefaultCulture\")");
        source.Should().Contain("@T.T(\"Variants\")");
        source.Should().Contain("@T.T(\"Visible\")");
        source.Should().Contain("@T.T(\"NoProductsFound\")");
        source.Should().Contain("@T.T(\"CatalogWindowLabel\"):");
        source.Should().Contain("@p.VariantCount");
        source.Should().Contain("span class=\"badge bg-success\">@T.T(\"Yes\")</span>");
        source.Should().Contain("span class=\"badge bg-outline-secondary border\">@T.T(\"No\")</span>");
        source.Should().Contain("hx-get=\"@Url.Action(\"Edit\", \"Products\", new { id = p.Id })\"");
        source.Should().Contain("data-action=\"@Url.Action(\"Delete\", \"Products\")\"");
        source.Should().Contain("data-rowversion=\"@Convert.ToBase64String(p.RowVersion)\"");
        source.Should().Contain("<pager page=\"@currentPage\"");
        source.Should().Contain("asp-controller=\"Products\"");
        source.Should().Contain("asp-route-query=\"@query\"");
        source.Should().Contain("asp-route-filter=\"@filter\"");
        source.Should().Contain("<partial name=\"~/Views/Shared/_ConfirmDeleteModal.cshtml\" />");
    }

    [Fact]
    public void ProductEditorShellAndForm_Should_KeepLocalizedProductKindContractsWired()
    {
        var createShellSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ProductCreateEditorShell.cshtml"));
        var editShellSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ProductEditEditorShell.cshtml"));
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Products", "_ProductForm.cshtml"));

        createShellSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateProduct\");");
        createShellSource.Should().Contain("<partial name=\"_ProductForm\" model=\"Model\" />");

        editShellSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditProduct\");");
        editShellSource.Should().Contain("<partial name=\"_ProductForm\" model=\"Model\" />");

        formSource.Should().Contain("var productKindOptions = Html.GetEnumSelectList<ProductKind>()");
        formSource.Should().Contain("Text = T.T(option.Text)");
        formSource.Should().Contain("asp-for=\"Kind\" class=\"form-select\"");
        formSource.Should().Contain("asp-items=\"productKindOptions\"");
    }


    [Fact]
    public void BusinessStaffAccessBadgeWorkspace_Should_KeepConstraintRemediationContractsWired()
    {
        var businessesSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var badgeViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "StaffAccessBadge.cshtml"));

        businessesSource.Should().Contain("public async Task<IActionResult> StaffAccessBadge(Guid id, CancellationToken ct = default)");
        businessesSource.Should().Contain("return RenderStaffAccessBadgeWorkspace(vm);");

        badgeViewSource.Should().Contain("string LocalizeBusinessMemberRole(object? role) => role is null ? \"-\" : T.T(role.ToString() ?? string.Empty);");
        badgeViewSource.Should().Contain("@Model.Business.Name | @Model.UserDisplayName");
        badgeViewSource.Should().Contain("@T.T(\"BusinessStaffAccessBadgeIntro\")");
        badgeViewSource.Should().Contain("@Url.Action(\"EditMember\", \"Businesses\", new { id = Model.MembershipId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Edit\", \"Users\", new { id = Model.UserId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Business.Id })");
        badgeViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        badgeViewSource.Should().Contain("@if (!Model.EmailConfirmed || !Model.IsActive || isLocked)");
        badgeViewSource.Should().Contain("@T.T(\"BusinessStaffAccessBadgeConstraintWarning\")");
        badgeViewSource.Should().Contain("@T.T(\"OpenUserAction\")");
        badgeViewSource.Should().Contain("@T.T(\"EditMemberAction\")");
        badgeViewSource.Should().Contain("@T.T(\"CommonMembers\")");
        badgeViewSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        badgeViewSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        badgeViewSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        badgeViewSource.Should().NotContain("<i class=\"fa-solid fa-user-lock\"></i> @T.T(\"UsersFilterLocked\")");
        badgeViewSource.Should().NotContain("<span class=\"text-danger\"> | @T.T(\"UsersLifecycleLockedBadge\")</span>");
        badgeViewSource.Should().Contain("@T.T(\"MobileOperationsTitle\")");
        badgeViewSource.Should().Contain("@T.T(\"BusinessStaffAccessBadgePayloadNote\")");
        badgeViewSource.Should().Contain("@T.T(\"BusinessStaffAccessBadgeMemberRoleLabel\"): @LocalizeBusinessMemberRole(Model.Role)");
        badgeViewSource.Should().Contain("@LocalizeBusinessMemberRole(Model.Role)");
        badgeViewSource.Should().Contain("@Url.Action(\"EditMember\", \"Businesses\", new { id = Model.MembershipId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Edit\", \"Users\", new { id = Model.UserId })");
        badgeViewSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Business.Id })");
        badgeViewSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
    }


    [Fact]
    public void BusinessSetupPreviewWorkspaces_Should_KeepMemberAndInvitationRemediationContractsWired()
    {
        var businessesSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));
        var membersPreviewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupMembersPreview.cshtml"));
        var invitationsPreviewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SetupInvitationsPreview.cshtml"));

        businessesSource.Should().Contain("@T.T(\"BusinessSetupMembersRequiringAttentionTitle\")");
        businessesSource.Should().Contain("@Url.Action(\"SetupMembersPreview\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        businessesSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        businessesSource.Should().Contain("@Url.Action(\"SetupInvitationsPreview\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        businessesSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        businessesSource.Should().Contain("string MemberWorkspaceLabel() => T.T(\"Members\")");
        businessesSource.Should().Contain("@MemberWorkspaceLabel()");
        businessesSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        businessesSource.Should().Contain("string MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        businessesSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        businessesSource.Should().Contain("@MemberSupportFilterLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        businessesSource.Should().NotContain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation })\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        businessesSource.Should().NotContain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked })\"\r\n                       hx-target=\"#business-setup-shell\"\r\n                       hx-swap=\"outerHTML\"\r\n                       hx-push-url=\"true\">@T.T(\"UsersFilterLocked\")</a>");
        businessesSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        businessesSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        businessesSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        businessesSource.Should().Contain("<i class=\"fa-solid fa-user-group\"></i> @MemberWorkspaceLabel()");
        businessesSource.Should().Contain("hx-push-url=\"true\">@MemberWorkspaceLabel()</a>");
        businessesSource.Should().Contain("<i class=\"fa-solid fa-envelope\"></i> @InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        businessesSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Members\")</a>");
        businessesSource.Should().NotContain("<span>@T.T(\"OpenInvitations\")</span>");
        businessesSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        businessesSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
        businessesSource.Should().NotContain("<i class=\"fa-solid fa-envelope\"></i> @T.T(\"Invitations\")");

        membersPreviewSource.Should().Contain("@Url.Action(\"Edit\", \"Users\", new { id = item.UserId })");
        membersPreviewSource.Should().Contain("@Url.Action(\"EditMember\", \"Businesses\", new { id = item.Id })");
        membersPreviewSource.Should().Contain("flowKey = \"AccountActivation\"");
        membersPreviewSource.Should().Contain("string MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        membersPreviewSource.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        membersPreviewSource.Should().Contain("@MemberAttentionStatusLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        membersPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        membersPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Locked\")</a>");

        invitationsPreviewSource.Should().Contain("flowKey = \"BusinessInvitation\"");
        invitationsPreviewSource.Should().Contain("@T.T(\"OpenFailedInvitationEmails\")");
        invitationsPreviewSource.Should().Contain("filter = \"Pending\"");
        invitationsPreviewSource.Should().Contain("filter = \"Expired\"");
        invitationsPreviewSource.Should().Contain("string LocalizeBusinessInvitationRole(object? role) => role is null ? \"-\" : T.T(role.ToString() ?? string.Empty);");
        invitationsPreviewSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationsPreviewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Pending)");
        invitationsPreviewSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Expired)");
        invitationsPreviewSource.Should().Contain("@LocalizeBusinessInvitationRole(item.Role)");
        invitationsPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Pending\")</a>");
        invitationsPreviewSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Expired\")</a>");
    }


    [Fact]
    public void BusinessMemberAndInvitationWorkspaces_Should_KeepLocalizedRoleDisplayContractsWired()
    {
        var membersSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Members.cshtml"));
        var invitationsSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Invitations.cshtml"));

        membersSource.Should().Contain("string LocalizeBusinessMemberRole(object? role) => role is null ? \"-\" : T.T(role.ToString() ?? string.Empty);");
        membersSource.Should().Contain("@LocalizeBusinessMemberRole(item.Role)");
        invitationsSource.Should().Contain("string LocalizeBusinessInvitationRole(object? role) => role is null ? \"-\" : T.T(role.ToString() ?? string.Empty);");
        invitationsSource.Should().Contain("@LocalizeBusinessInvitationRole(item.Role)");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepSummaryRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"OperationalStatus\")");
        setupShellSource.Should().Contain("operationalStatus = Model.OperationalStatus");
        setupShellSource.Should().Contain("string BusinessLifecycleStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus status) => status switch");
        setupShellSource.Should().Contain("_ => T.T(status.ToString())");
        setupShellSource.Should().Contain("@T.T(\"EditBusiness\")");
        setupShellSource.Should().Contain("string MemberWorkspaceLabel() => T.T(\"Members\")");
        setupShellSource.Should().Contain("@MemberWorkspaceLabel()");
        setupShellSource.Should().Contain("@T.T(\"Locations\")");
        setupShellSource.Should().Contain("@T.T(\"OpenGlobalLocalization\")");
        setupShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void BusinessOperationalStatusViews_Should_KeepResourceBackedStatusFallbacksWired()
    {
        var indexSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "Index.cshtml"));
        var readinessSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "MerchantReadiness.cshtml"));
        var attentionSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_SupportQueueAttentionBusinesses.cshtml"));

        indexSource.Should().Contain("string BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus status) => status switch");
        indexSource.Should().Contain("_ => T.T(status.ToString())");

        readinessSource.Should().Contain("string BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus status) => status switch");
        readinessSource.Should().Contain("_ => T.T(status.ToString())");
        readinessSource.Should().Contain("string SubscriptionStatusLabel(string? status) => string.IsNullOrWhiteSpace(status) ? \"-\" : T.T(status);");
        readinessSource.Should().Contain("SubscriptionStatusLabel(item.SubscriptionStatus)");

        attentionSource.Should().Contain("string BusinessOperationalStatusLabel(Darwin.Domain.Enums.BusinessOperationalStatus status) => status switch");
        attentionSource.Should().Contain("_ => T.T(status.ToString())");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepCommunicationReadinessRemediationContractsWired()
    {
        var controllerSource = ReadWebAdminFile(Path.Combine("Controllers", "Admin", "Businesses", "BusinessesController.cs"));
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessEmailConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessEmailMissingSummary\")");
        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessSmsConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessSmsMissingSummary\")");
        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessWhatsAppConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessWhatsAppMissingSummary\")");
        controllerSource.Should().Contain("? T(\"BusinessCommunicationReadinessAdminRoutingConfiguredSummary\")");
        controllerSource.Should().Contain(": T(\"BusinessCommunicationReadinessAdminRoutingMissingSummary\")");
        setupShellSource.Should().Contain("@T.T(\"CommunicationReadiness\")");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"EmailAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"ChannelAudits\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("fragment = \"site-settings-admin-routing\"");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationProfileTitle\")");
        setupShellSource.Should().Contain("@T.T(\"EmailAudits\")");
        setupShellSource.Should().Contain("@T.T(\"SmsWhatsAppAuditsTitle\")");
        setupShellSource.Should().Contain("@T.T(\"AdminAlerts\")");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepSubscriptionSnapshotRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessSubscriptionSnapshot\")");
        setupShellSource.Should().Contain("@T.T(\"NoActiveSubscriptionSnapshot\")");
        setupShellSource.Should().Contain("string SubscriptionStatusLabel(string? status) => string.IsNullOrWhiteSpace(status) ? \"-\" : T.T(status);");
        setupShellSource.Should().Contain("@SubscriptionStatusLabel(Model.Subscription.Status)");
        setupShellSource.Should().Contain("@Url.Action(\"Subscription\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"SubscriptionInvoices\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Payments\", \"Billing\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSubscriptionInvoicesTitle\")");
        setupShellSource.Should().Contain("@T.T(\"OpenPayments\")");
        setupShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepIncompleteChecklistRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"SetupIncompleteWarning\")");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingOwner)");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingPrimaryLocation)");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingContactEmail)");
        setupShellSource.Should().Contain("@BusinessReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.MissingLegalName)");
        setupShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Edit\", \"Businesses\", new { id = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
    }


    [Fact]
    public void BusinessSetupWorkspace_Should_KeepOwnershipRemediationContractsWired()
    {
        var setupShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessSetupShell.cshtml"));

        setupShellSource.Should().Contain("@T.T(\"BusinessOwnedHere\")");
        setupShellSource.Should().Contain("@T.T(\"GlobalDependencies\")");
        setupShellSource.Should().Contain("@Url.Action(\"Details\", \"BusinessCommunications\", new { businessId = Model.Id })");
        setupShellSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Id })");
        setupShellSource.Should().Contain("fragment = \"site-settings-communications-policy\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-payments\"");
        setupShellSource.Should().Contain("fragment = \"site-settings-tax\"");
        setupShellSource.Should().Contain("@T.T(\"BusinessCommunicationProfileTitle\")");
        setupShellSource.Should().Contain("@T.T(\"CommunicationPolicy\")");
        setupShellSource.Should().Contain("@T.T(\"Payments\")");
        setupShellSource.Should().Contain("@T.T(\"Tax\")");
    }


    [Fact]
    public void BusinessEditorWorkspace_Should_KeepSummaryAndOnboardingRemediationContractsWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOwners\")");
        editorShellSource.Should().Contain("string InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter filter) => filter switch");
        editorShellSource.Should().Contain("string MemberWorkspaceLabel() => T.T(\"Members\")");
        editorShellSource.Should().Contain("@InvitationReadinessLabel(Darwin.Application.Businesses.DTOs.BusinessReadinessQueueFilter.PendingInvites)");
        editorShellSource.Should().NotContain("@T.T(\"BusinessEditorPendingInvites\")");
        editorShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("hx-push-url=\"true\">@MemberWorkspaceLabel()</a>");
        editorShellSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"Members\")</a>");
        editorShellSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOperationalStatus\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorOnboardingChecklist\")");
        editorShellSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.Id })");
        editorShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        editorShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorNextActions\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorNotOnboardingCompleteYet\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void BusinessEditorWorkspace_Should_KeepChecklistSummaryAndIncompleteRemediationWired()
    {
        var editorShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessEditorShell.cshtml"));

        editorShellSource.Should().Contain("@(checklistComplete ? T.T(\"BusinessEditorReadyForGoLiveReview\") : T.T(\"BusinessEditorSetupStillIncomplete\"))");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorChecklistComplete\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorChecklistIncomplete\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorNotOnboardingCompleteYet\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAddAtLeastOneActiveOwner\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorAddPrimaryLocationSentence\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorConfigureContactEmail\")");
        editorShellSource.Should().Contain("@T.T(\"BusinessEditorCompleteLegalBusinessName\")");
    }


    [Fact]
    public void BusinessInvitationForm_Should_KeepCreationGuidanceRemediationContractsWired()
    {
        var invitationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessInvitationForm.cshtml"));

        invitationFormSource.Should().Contain("@T.T(\"BusinessInvitationCreateHelp\")");
        invitationFormSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.BusinessId })");
        invitationFormSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.BusinessId })");
        invitationFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        invitationFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        invitationFormSource.Should().Contain("string InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter filter) => filter switch");
        invitationFormSource.Should().Contain("@InvitationQueueLabel(Darwin.Application.Businesses.DTOs.BusinessInvitationQueueFilter.Open)");
        invitationFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"OpenInvitations\")</a>");
        invitationFormSource.Should().Contain("@T.T(\"Setup\")");
        invitationFormSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        invitationFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessMemberForm_Should_KeepAssignmentGuidanceRemediationContractsWired()
    {
        var memberFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberForm.cshtml"));

        memberFormSource.Should().Contain("@T.T(\"BusinessMemberAssignmentHelp\")");
        memberFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId, filter = Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation })");
        memberFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        memberFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        memberFormSource.Should().Contain("string MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter filter) => filter switch");
        memberFormSource.Should().Contain("@T.T(\"CommonMembers\")");
        memberFormSource.Should().Contain("@MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.PendingActivation)");
        memberFormSource.Should().NotContain("hx-push-url=\"true\">@T.T(\"PendingActivation\")</a>");
        memberFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
        memberFormSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
    }


    [Fact]
    public void BusinessStaffAccessBadgeWorkspace_Should_KeepLockedRemediationShortcutHelperBacked()
    {
        var badgeViewSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "StaffAccessBadge.cshtml"));

        badgeViewSource.Should().Contain("<i class=\"fa-solid fa-user-lock\"></i> @MemberSupportLabel(Darwin.Application.Businesses.DTOs.BusinessMemberSupportFilter.Locked)");
        badgeViewSource.Should().NotContain("<i class=\"fa-solid fa-user-lock\"></i> @T.T(\"UsersFilterLocked\")");
    }


    [Fact]
    public void BusinessLocationForm_Should_KeepFooterRemediationContractsWired()
    {
        var locationFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessLocationForm.cshtml"));

        locationFormSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.BusinessId })");
        locationFormSource.Should().Contain("@Url.Action(\"Setup\", \"Businesses\", new { id = Model.BusinessId })");
        locationFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        locationFormSource.Should().Contain("@T.T(\"Cancel\")");
        locationFormSource.Should().Contain("@T.T(\"Setup\")");
        locationFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessForm_Should_KeepInitialOwnerGuidanceRemediationContractsWired()
    {
        var businessFormSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessForm.cshtml"));

        businessFormSource.Should().Contain("@T.T(\"BusinessFormInitialOwnerHelp\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormActiveHelp\")");
        businessFormSource.Should().Contain("@Url.Action(\"Index\", \"Users\")");
        businessFormSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"Locations\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"Invitations\", \"Businesses\", new { businessId = Model.Id })");
        businessFormSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        businessFormSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        businessFormSource.Should().Contain("@T.T(\"Users\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageMembers\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageLocations\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessFormManageInvitations\")");
        businessFormSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        businessFormSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessMemberEditorShell_Should_KeepOwnerOverrideRemediationContractsWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("@T.T(\"BusinessMemberLastActiveOwnerWarning\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberControlledOwnerOverrideTitle\")");
        memberShellSource.Should().Contain("@Url.Action(\"Members\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberShellSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberShellSource.Should().Contain("@Url.Action(\"SupportQueue\", \"Businesses\")");
        memberShellSource.Should().Contain("@Url.Action(\"MerchantReadiness\", \"Businesses\")");
        memberShellSource.Should().Contain("@T.T(\"CommonMembers\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMembersOwnerOverrideAuditAction\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessSupportQueueTitle\")");
        memberShellSource.Should().Contain("@T.T(\"MerchantReadinessTitle\")");
    }


    [Fact]
    public void BusinessMemberEditorShell_Should_KeepOwnerOverrideRemediationRailsWired()
    {
        var memberShellSource = ReadWebAdminFile(Path.Combine("Views", "Businesses", "_BusinessMemberEditorShell.cshtml"));

        memberShellSource.Should().Contain("@T.T(\"BusinessMemberLastActiveOwnerWarning\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberControlledOwnerOverrideTitle\")");
        memberShellSource.Should().Contain("@T.T(\"BusinessMemberControlledOwnerOverrideNote\")");
        memberShellSource.Should().Contain("@Url.Action(\"OwnerOverrideAudits\", \"Businesses\", new { businessId = Model.BusinessId })");
        memberShellSource.Should().Contain("@T.T(\"BusinessMembersOwnerOverrideAuditAction\")");
        memberShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"BusinessSupportQueueTitle\")</a>");
        memberShellSource.Should().Contain("hx-push-url=\"true\">@T.T(\"MerchantReadinessTitle\")</a>");
    }


    [Fact]
    public void SharedCatalogCmsAndCrmViewModels_Should_KeepWorkspaceAndEditorContractShapesWired()
    {
        var addOnGroupVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Catalog", "AddOnGroupVms.cs"));
        var brandVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Catalog", "BrandVms.cs"));
        var categoryVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Catalog", "CategoryVms.cs"));
        var productIndexVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "Catalog", "ProductIndexVms.cs"));
        var productCreateVmSource = ReadWebAdminFile(Path.Combine("ViewModels", "Catalog", "ProductCreateVm.cs"));
        var productEditVmSource = ReadWebAdminFile(Path.Combine("ViewModels", "Catalog", "ProductEditVm.cs"));
        var pageIndexVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "CMS", "PageIndexVms.cs"));
        var pageVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "CMS", "PageVms.cs"));
        var mediaVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "CMS", "MediaVms.cs"));
        var crmVmsSource = ReadWebAdminFile(Path.Combine("ViewModels", "CRM", "CrmVms.cs"));

        addOnGroupVmsSource.Should().Contain("public sealed class AddOnGroupsListVm");
        addOnGroupVmsSource.Should().Contain("public AddOnGroupQueueFilter Filter { get; set; } = AddOnGroupQueueFilter.All;");
        addOnGroupVmsSource.Should().Contain("public AddOnGroupOpsSummaryVm Summary { get; set; } = new();");
        addOnGroupVmsSource.Should().Contain("public abstract class AddOnGroupEditorVm");
        addOnGroupVmsSource.Should().Contain("[Required, StringLength(3)]");
        addOnGroupVmsSource.Should().Contain("public string Currency { get; set; } = string.Empty;");
        addOnGroupVmsSource.Should().Contain("public List<AddOnOptionVm> Options { get; set; } = new();");
        addOnGroupVmsSource.Should().Contain("public sealed class AddOnGroupAttachToProductsVm");
        addOnGroupVmsSource.Should().Contain("public List<Guid> SelectedProductIds { get; set; } = new();");
        addOnGroupVmsSource.Should().Contain("public sealed class AddOnGroupAttachToVariantsVm");

        brandVmsSource.Should().Contain("public sealed class BrandsListVm");
        brandVmsSource.Should().Contain("public BrandOpsSummaryVm Summary { get; set; } = new();");
        brandVmsSource.Should().Contain("public IReadOnlyList<OperationalPlaybookVm> Playbooks { get; set; } = Array.Empty<OperationalPlaybookVm>();");
        brandVmsSource.Should().Contain("public sealed class BrandTranslationVm");
        brandVmsSource.Should().Contain("public string? DescriptionHtml { get; set; }");
        brandVmsSource.Should().Contain("public sealed class BrandEditVm");
        brandVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");

        categoryVmsSource.Should().Contain("public sealed class CategoriesIndexVm");
        categoryVmsSource.Should().Contain("public CategoryOpsSummaryVm Summary { get; set; } = new();");
        categoryVmsSource.Should().Contain("public sealed class CategoryCreateVm");
        categoryVmsSource.Should().Contain("public bool IsActive { get; set; } = true;");
        categoryVmsSource.Should().Contain("public List<CategoryTranslationVm> Translations { get; set; } = new();");
        categoryVmsSource.Should().Contain("public sealed class CategoryEditVm");
        categoryVmsSource.Should().Contain("public byte[]? RowVersion { get; set; }");

        productIndexVmsSource.Should().Contain("public sealed class ProductsIndexVm");
        productIndexVmsSource.Should().Contain("public ProductOpsSummaryVm Summary { get; set; } = new();");
        productIndexVmsSource.Should().Contain("public IReadOnlyList<OperationalPlaybookVm> Playbooks { get; set; } = new List<OperationalPlaybookVm>();");
        productIndexVmsSource.Should().Contain("public sealed class OperationalPlaybookVm");
        productCreateVmSource.Should().Contain("public abstract class ProductEditorVm");
        productCreateVmSource.Should().Contain("public string Kind { get; set; } = \"Simple\";");
        productCreateVmSource.Should().Contain("public List<ProductTranslationVm> Translations { get; set; } = new();");
        productCreateVmSource.Should().Contain("public List<ProductVariantCreateVm> Variants { get; set; } = new();");
        productCreateVmSource.Should().Contain("public ProductCreateVm()");
        productCreateVmSource.Should().Contain("Variants = new List<ProductVariantCreateVm> { new() };");
        productEditVmSource.Should().Contain("public sealed class ProductEditVm : ProductEditorVm");
        productEditVmSource.Should().Contain("public byte[]? RowVersion { get; set; }");

        pageIndexVmsSource.Should().Contain("public sealed class PagesIndexVm");
        pageIndexVmsSource.Should().Contain("public PageOpsSummaryVm Summary { get; set; } = new();");
        pageIndexVmsSource.Should().Contain("public IReadOnlyList<PagePlaybookVm> Playbooks { get; set; } = new List<PagePlaybookVm>();");
        pageVmsSource.Should().Contain("public abstract class PageEditorVm");
        pageVmsSource.Should().Contain("public PageStatus Status { get; set; } = PageStatus.Draft;");
        pageVmsSource.Should().Contain("public List<PageTranslationVm> Translations { get; set; } = new();");
        pageVmsSource.Should().Contain("public sealed class PageEditVm : PageEditorVm");
        pageVmsSource.Should().Contain("public byte[]? RowVersion { get; set; }");

        mediaVmsSource.Should().Contain("public sealed class MediaAssetsListVm");
        mediaVmsSource.Should().Contain("public int PageSize { get; set; } = 24;");
        mediaVmsSource.Should().Contain("public MediaAssetQueueFilter Filter { get; set; } = MediaAssetQueueFilter.All;");
        mediaVmsSource.Should().Contain("public MediaAssetOpsSummaryVm Summary { get; set; } = new();");
        mediaVmsSource.Should().Contain("public List<MediaAssetPlaybookVm> Playbooks { get; set; } = new();");
        mediaVmsSource.Should().Contain("public abstract class MediaAssetEditorVm");
        mediaVmsSource.Should().Contain("public IFormFile? File { get; set; }");
        mediaVmsSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");

        crmVmsSource.Should().Contain("public sealed class CustomersListVm");
        crmVmsSource.Should().Contain("public string PlatformDefaultCulture { get; set; } = AdminCultureCatalog.DefaultCulture;");
        crmVmsSource.Should().Contain("public sealed class CustomerEditVm");
        crmVmsSource.Should().Contain("public IdentityAddressSummaryVm? DefaultBillingAddress { get; set; }");
        crmVmsSource.Should().Contain("public List<CustomerAddressVm> Addresses { get; set; } = new();");
        crmVmsSource.Should().Contain("public sealed class LeadsListVm");
        crmVmsSource.Should().Contain("public LeadQueueFilter Filter { get; set; } = LeadQueueFilter.All;");
        crmVmsSource.Should().Contain("public sealed class LeadEditVm");
        crmVmsSource.Should().Contain("public LeadStatus Status { get; set; } = LeadStatus.New;");
        crmVmsSource.Should().Contain("public sealed class OpportunitiesListVm");
        crmVmsSource.Should().Contain("public OpportunityQueueFilter Filter { get; set; } = OpportunityQueueFilter.All;");
        crmVmsSource.Should().Contain("public sealed class OpportunityEditVm");
        crmVmsSource.Should().Contain("public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;");
        crmVmsSource.Should().Contain("public List<OpportunityItemVm> Items { get; set; } = new();");
        crmVmsSource.Should().Contain("public sealed class CustomerSegmentsListVm");
        crmVmsSource.Should().Contain("public CustomerSegmentQueueFilter Filter { get; set; } = CustomerSegmentQueueFilter.All;");
        crmVmsSource.Should().Contain("public sealed class InvoicesListVm");
        crmVmsSource.Should().Contain("public TaxPolicySnapshotVm TaxPolicy { get; set; } = new();");
        crmVmsSource.Should().Contain("public sealed class InvoiceEditVm");
        crmVmsSource.Should().Contain("public DateTime DueDateUtc { get; set; } = DateTime.UtcNow.AddDays(14);");
        crmVmsSource.Should().Contain("public InvoiceRefundCreateVm Refund { get; set; } = new();");
    }


    [Fact]
    public void MediaAssetUploadForm_Should_KeepAltTitleAndRoleFieldContractsWired()
    {
        var formSource = ReadWebAdminFile(Path.Combine("Views", "Media", "_MediaAssetUploadForm.cshtml"));

        formSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CMS.MediaAssetEditorVm");
        formSource.Should().Contain("@inject Darwin.WebAdmin.Localization.IAdminTextLocalizer T");
        formSource.Should().Contain("asp-for=\"Alt\"");
        formSource.Should().Contain("@T.T(\"AltText\")");
        formSource.Should().Contain("asp-validation-for=\"Alt\"");
        formSource.Should().Contain("asp-for=\"Title\"");
        formSource.Should().Contain("@T.T(\"Title\")");
        formSource.Should().Contain("asp-validation-for=\"Title\"");
        formSource.Should().Contain("asp-for=\"Role\"");
        formSource.Should().Contain("@T.T(\"Role\")");
        formSource.Should().Contain("placeholder=\"@T.T(\"MediaRolePlaceholder\")\"");
        formSource.Should().Contain("asp-validation-for=\"Role\"");
    }


    [Fact]
    public void MediaWrapperViews_Should_KeepTitleAndShellHandoffContractsWired()
    {
        var createViewSource = ReadWebAdminFile(Path.Combine("Views", "Media", "Create.cshtml"));
        var editViewSource = ReadWebAdminFile(Path.Combine("Views", "Media", "Edit.cshtml"));

        createViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CMS.MediaAssetCreateVm");
        createViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"UploadMedia\");");
        createViewSource.Should().Contain("<partial name=\"_MediaAssetCreateEditorShell\" model=\"Model\" />");

        editViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CMS.MediaAssetEditVm");
        editViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditMedia\");");
        editViewSource.Should().Contain("<partial name=\"_MediaAssetEditEditorShell\" model=\"Model\" />");
    }


    [Fact]
    public void LoyaltyWrapperViews_Should_KeepCreateModeTitleAndShellHandoffContractsWired()
    {
        var createProgramViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "CreateProgram.cshtml"));
        var editProgramViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "EditProgram.cshtml"));
        var createRewardTierViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "CreateRewardTier.cshtml"));
        var editRewardTierViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "EditRewardTier.cshtml"));
        var createAccountViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "CreateAccount.cshtml"));
        var createCampaignViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "CreateCampaign.cshtml"));
        var editCampaignViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "EditCampaign.cshtml"));

        createProgramViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.LoyaltyProgramEditVm");
        createProgramViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createProgramViewSource.Should().Contain("<partial name=\"_ProgramEditorShell\" model=\"Model\" />");

        editProgramViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.LoyaltyProgramEditVm");
        editProgramViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editProgramViewSource.Should().Contain("<partial name=\"_ProgramEditorShell\" model=\"Model\" />");

        createRewardTierViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.LoyaltyRewardTierEditVm");
        createRewardTierViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createRewardTierViewSource.Should().Contain("<partial name=\"_RewardTierEditorShell\" model=\"Model\" />");

        editRewardTierViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.LoyaltyRewardTierEditVm");
        editRewardTierViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editRewardTierViewSource.Should().Contain("<partial name=\"_RewardTierEditorShell\" model=\"Model\" />");

        createAccountViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.CreateLoyaltyAccountVm");
        createAccountViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateLoyaltyAccount\");");
        createAccountViewSource.Should().Contain("<partial name=\"_AccountCreateEditorShell\" model=\"Model\" />");
        createAccountViewSource.Should().Contain("@section Scripts {");
        createAccountViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        createCampaignViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.LoyaltyCampaignEditVm");
        createCampaignViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createCampaignViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateLoyaltyCampaign\");");
        createCampaignViewSource.Should().Contain("<partial name=\"_CampaignEditorShell\" model=\"Model\" />");

        editCampaignViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.LoyaltyCampaignEditVm");
        editCampaignViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editCampaignViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditLoyaltyCampaign\");");
        editCampaignViewSource.Should().Contain("<partial name=\"_CampaignEditorShell\" model=\"Model\" />");
    }


    [Fact]
    public void InventoryWrapperViews_Should_KeepTitleCreateModeShellAndLineTemplateScriptContractsWired()
    {
        var createWarehouseViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "CreateWarehouse.cshtml"));
        var editWarehouseViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "EditWarehouse.cshtml"));
        var createSupplierViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "CreateSupplier.cshtml"));
        var editSupplierViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "EditSupplier.cshtml"));
        var createPurchaseOrderViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "CreatePurchaseOrder.cshtml"));
        var editPurchaseOrderViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "EditPurchaseOrder.cshtml"));
        var createStockLevelViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "CreateStockLevel.cshtml"));
        var editStockLevelViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "EditStockLevel.cshtml"));
        var createStockTransferViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "CreateStockTransfer.cshtml"));
        var editStockTransferViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "EditStockTransfer.cshtml"));

        createWarehouseViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.WarehouseEditVm");
        createWarehouseViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateWarehouse\");");
        createWarehouseViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createWarehouseViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_WarehouseEditorShell.cshtml\" model=\"Model\" />");
        createWarehouseViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        editWarehouseViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.WarehouseEditVm");
        editWarehouseViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditWarehouse\");");
        editWarehouseViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editWarehouseViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_WarehouseEditorShell.cshtml\" model=\"Model\" />");
        editWarehouseViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        createSupplierViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.SupplierEditVm");
        createSupplierViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateSupplier\");");
        createSupplierViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createSupplierViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_SupplierEditorShell.cshtml\" model=\"Model\" />");
        createSupplierViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        editSupplierViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.SupplierEditVm");
        editSupplierViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditSupplier\");");
        editSupplierViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editSupplierViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_SupplierEditorShell.cshtml\" model=\"Model\" />");
        editSupplierViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        createPurchaseOrderViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.PurchaseOrderEditVm");
        createPurchaseOrderViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreatePurchaseOrder\");");
        createPurchaseOrderViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createPurchaseOrderViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_PurchaseOrderEditorShell.cshtml\" model=\"Model\" />");
        createPurchaseOrderViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        createPurchaseOrderViewSource.Should().Contain("event.target.closest('#addPurchaseOrderLine')");
        createPurchaseOrderViewSource.Should().Contain("template.innerHTML.replaceAll('__index__'");

        editPurchaseOrderViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.PurchaseOrderEditVm");
        editPurchaseOrderViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditPurchaseOrder\");");
        editPurchaseOrderViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editPurchaseOrderViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_PurchaseOrderEditorShell.cshtml\" model=\"Model\" />");
        editPurchaseOrderViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        editPurchaseOrderViewSource.Should().Contain("event.target.closest('#addPurchaseOrderLine')");
        editPurchaseOrderViewSource.Should().Contain("button.closest('.line-row')?.remove();");

        createStockLevelViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.StockLevelEditVm");
        createStockLevelViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createStockLevelViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateStockLevel\");");
        createStockLevelViewSource.Should().Contain("<partial name=\"_StockLevelEditorShell\" model=\"Model\" />");
        createStockLevelViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        editStockLevelViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.StockLevelEditVm");
        editStockLevelViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editStockLevelViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditStockLevel\");");
        editStockLevelViewSource.Should().Contain("<partial name=\"_StockLevelEditorShell\" model=\"Model\" />");
        editStockLevelViewSource.Should().Contain("@section Scripts { <partial name=\"_ValidationScriptsPartial\" /> }");

        createStockTransferViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.StockTransferEditVm");
        createStockTransferViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateStockTransfer\");");
        createStockTransferViewSource.Should().Contain("ViewData[\"IsCreate\"] = true;");
        createStockTransferViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_StockTransferEditorShell.cshtml\" model=\"Model\" />");
        createStockTransferViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        createStockTransferViewSource.Should().Contain("event.target.closest('#addTransferLine')");
        createStockTransferViewSource.Should().Contain("template.innerHTML.replaceAll('__index__'");

        editStockTransferViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.StockTransferEditVm");
        editStockTransferViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditStockTransfer\");");
        editStockTransferViewSource.Should().Contain("ViewData[\"IsCreate\"] = false;");
        editStockTransferViewSource.Should().Contain("<partial name=\"~/Views/Inventory/_StockTransferEditorShell.cshtml\" model=\"Model\" />");
        editStockTransferViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        editStockTransferViewSource.Should().Contain("event.target.closest('#addTransferLine')");
        editStockTransferViewSource.Should().Contain("button.closest('.line-row')?.remove();");
    }


    [Fact]
    public void CrmThinWrapperViews_Should_KeepTitleFormActionShellAndValidationHandoffContractsWired()
    {
        var createCustomerViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "CreateCustomer.cshtml"));
        var createLeadViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "CreateLead.cshtml"));
        var createOpportunityViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "CreateOpportunity.cshtml"));
        var createSegmentViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "CreateSegment.cshtml"));
        var editSegmentViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "EditSegment.cshtml"));
        var editInvoiceViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "EditInvoice.cshtml"));

        createCustomerViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.CustomerEditVm");
        createCustomerViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateCustomer\");");
        createCustomerViewSource.Should().Contain("ViewData[\"FormAction\"] = \"CreateCustomer\";");
        createCustomerViewSource.Should().Contain("<partial name=\"~/Views/Crm/_CustomerEditorShell.cshtml\" model=\"Model\" />");
        createCustomerViewSource.Should().Contain("@section Scripts {");
        createCustomerViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        createLeadViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.LeadEditVm");
        createLeadViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateLead\");");
        createLeadViewSource.Should().Contain("<h1 class=\"mb-3\">@T.T(\"CreateLead\")</h1>");
        createLeadViewSource.Should().Contain("<partial name=\"_LeadEditorShell\" model=\"Model\" />");
        createLeadViewSource.Should().Contain("@section Scripts {");
        createLeadViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        createOpportunityViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.OpportunityEditVm");
        createOpportunityViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateOpportunity\");");
        createOpportunityViewSource.Should().Contain("<h1 class=\"mb-3\">@T.T(\"CreateOpportunity\")</h1>");
        createOpportunityViewSource.Should().Contain("<partial name=\"_OpportunityEditorShell\" model=\"Model\" />");
        createOpportunityViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
        createOpportunityViewSource.Should().Contain("document.getElementById('addOpportunityLine')?.addEventListener('click'");
        createOpportunityViewSource.Should().Contain("template.innerHTML.replaceAll('__index__'");

        createSegmentViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.CustomerSegmentEditVm");
        createSegmentViewSource.Should().Contain("ViewData[\"FormAction\"] = \"CreateSegment\";");
        createSegmentViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"CreateSegment\");");
        createSegmentViewSource.Should().Contain("<partial name=\"_SegmentEditorShell\" model=\"Model\" />");
        createSegmentViewSource.Should().Contain("@section Scripts {");
        createSegmentViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editSegmentViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.CustomerSegmentEditVm");
        editSegmentViewSource.Should().Contain("ViewData[\"FormAction\"] = \"EditSegment\";");
        editSegmentViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditSegment\");");
        editSegmentViewSource.Should().Contain("<partial name=\"_SegmentEditorShell\" model=\"Model\" />");
        editSegmentViewSource.Should().Contain("@section Scripts {");
        editSegmentViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editInvoiceViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.InvoiceEditVm");
        editInvoiceViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditInvoice\");");
        editInvoiceViewSource.Should().Contain("<partial name=\"~/Views/Crm/_InvoiceEditorShell.cshtml\" model=\"Model\" />");
        editInvoiceViewSource.Should().Contain("@section Scripts {");
        editInvoiceViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void CrmEditWorkspaceViews_Should_KeepConversionInteractionConsentAndSegmentContractsWired()
    {
        var editCustomerViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "EditCustomer.cshtml"));
        var editLeadViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "EditLead.cshtml"));
        var editOpportunityViewSource = ReadWebAdminFile(Path.Combine("Views", "Crm", "EditOpportunity.cshtml"));

        editCustomerViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.CustomerEditVm");
        editCustomerViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditCustomer\");");
        editCustomerViewSource.Should().Contain("ViewData[\"FormAction\"] = \"EditCustomer\";");
        editCustomerViewSource.Should().Contain("<partial name=\"~/Views/Crm/_CustomerEditorShell.cshtml\" model=\"Model\" />");
        editCustomerViewSource.Should().Contain("hx-post=\"@Url.Action(\"CustomerInteractions\", \"Crm\")\"");
        editCustomerViewSource.Should().Contain("hx-get=\"@Url.Action(\"CustomerInteractions\", \"Crm\", new { customerId = Model.Id })\"");
        editCustomerViewSource.Should().Contain("hx-post=\"@Url.Action(\"CustomerConsents\", \"Crm\")\"");
        editCustomerViewSource.Should().Contain("hx-get=\"@Url.Action(\"CustomerConsents\", \"Crm\", new { customerId = Model.Id })\"");
        editCustomerViewSource.Should().Contain("hx-post=\"@Url.Action(\"CustomerSegmentMemberships\", \"Crm\")\"");
        editCustomerViewSource.Should().Contain("hx-get=\"@Url.Action(\"CustomerSegmentMemberships\", \"Crm\", new { customerId = Model.Id })\"");
        editCustomerViewSource.Should().Contain("var interactionTypeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.InteractionType>()");
        editCustomerViewSource.Should().Contain("var interactionChannelOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.InteractionChannel>()");
        editCustomerViewSource.Should().Contain("var consentTypeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.ConsentType>()");
        editCustomerViewSource.Should().Contain("Text = T.T(option.Text)");
        editCustomerViewSource.Should().Contain("option.Text == nameof(Darwin.Domain.Enums.ConsentType.Sms) ? T.T(\"SMS\") : T.T(option.Text)");
        editCustomerViewSource.Should().Contain("asp-items=\"interactionTypeOptions\"");
        editCustomerViewSource.Should().Contain("asp-items=\"interactionChannelOptions\"");
        editCustomerViewSource.Should().Contain("asp-items=\"consentTypeOptions\"");
        editCustomerViewSource.Should().Contain("name=\"CustomerId\" value=\"@Model.Id\"");
        editCustomerViewSource.Should().Contain("asp-items=\"Model.SegmentOptions\"");
        editCustomerViewSource.Should().Contain("@section Scripts {");
        editCustomerViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editLeadViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.LeadEditVm");
        editLeadViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditLead\");");
        editLeadViewSource.Should().Contain("<h1 class=\"mb-3\">@T.T(\"EditLead\")</h1>");
        editLeadViewSource.Should().Contain("<partial name=\"_LeadEditorShell\" model=\"Model\" />");
        editLeadViewSource.Should().Contain("@T.T(\"LeadConversion\")");
        editLeadViewSource.Should().Contain("@if (Model.CustomerId.HasValue)");
        editLeadViewSource.Should().Contain("asp-action=\"EditCustomer\" asp-route-id=\"@Model.CustomerId.Value\"");
        editLeadViewSource.Should().Contain("asp-action=\"ConvertLead\"");
        editLeadViewSource.Should().Contain("name=\"LeadId\" value=\"@Model.Id\"");
        editLeadViewSource.Should().Contain("name=\"RowVersion\" value=\"@Convert.ToBase64String(Model.RowVersion)\"");
        editLeadViewSource.Should().Contain("name=\"CopyNotesToCustomer\" value=\"true\" checked=\"checked\"");
        editLeadViewSource.Should().Contain("hx-post=\"@Url.Action(\"LeadInteractions\", \"Crm\")\"");
        editLeadViewSource.Should().Contain("hx-get=\"@Url.Action(\"LeadInteractions\", \"Crm\", new { leadId = Model.Id })\"");
        editLeadViewSource.Should().Contain("var interactionTypeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.InteractionType>()");
        editLeadViewSource.Should().Contain("var interactionChannelOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.InteractionChannel>()");
        editLeadViewSource.Should().Contain("Text = T.T(option.Text)");
        editLeadViewSource.Should().Contain("asp-items=\"interactionTypeOptions\"");
        editLeadViewSource.Should().Contain("asp-items=\"interactionChannelOptions\"");
        editLeadViewSource.Should().Contain("@section Scripts {");
        editLeadViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");

        editOpportunityViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.CRM.OpportunityEditVm");
        editOpportunityViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditOpportunity\");");
        editOpportunityViewSource.Should().Contain("<h1 class=\"mb-3\">@T.T(\"EditOpportunity\")</h1>");
        editOpportunityViewSource.Should().Contain("<partial name=\"_OpportunityEditorShell\" model=\"Model\" />");
        editOpportunityViewSource.Should().Contain("hx-post=\"@Url.Action(\"OpportunityInteractions\", \"Crm\")\"");
        editOpportunityViewSource.Should().Contain("hx-get=\"@Url.Action(\"OpportunityInteractions\", \"Crm\", new { opportunityId = Model.Id })\"");
        editOpportunityViewSource.Should().Contain("var interactionTypeOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.InteractionType>()");
        editOpportunityViewSource.Should().Contain("var interactionChannelOptions = Html.GetEnumSelectList<Darwin.Domain.Enums.InteractionChannel>()");
        editOpportunityViewSource.Should().Contain("Text = T.T(option.Text)");
        editOpportunityViewSource.Should().Contain("asp-items=\"interactionTypeOptions\"");
        editOpportunityViewSource.Should().Contain("asp-items=\"interactionChannelOptions\"");
        editOpportunityViewSource.Should().Contain("document.getElementById('addOpportunityLine')?.addEventListener('click'");
        editOpportunityViewSource.Should().Contain("template.innerHTML.replaceAll('__index__'");
        editOpportunityViewSource.Should().Contain("const button = event.target.closest('.remove-line');");
        editOpportunityViewSource.Should().Contain("@section Scripts {");
        editOpportunityViewSource.Should().Contain("<partial name=\"_ValidationScriptsPartial\" />");
    }


    [Fact]
    public void InventoryActionWrapperViews_Should_KeepTitleAndShellHandoffContractsWired()
    {
        var adjustStockViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "AdjustStock.cshtml"));
        var reserveStockViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "ReserveStock.cshtml"));
        var releaseReservationViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "ReleaseReservation.cshtml"));
        var returnReceiptViewSource = ReadWebAdminFile(Path.Combine("Views", "Inventory", "ReturnReceipt.cshtml"));
        var adjustPointsViewSource = ReadWebAdminFile(Path.Combine("Views", "Loyalty", "AdjustPoints.cshtml"));

        adjustStockViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.InventoryAdjustActionVm");
        adjustStockViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"AdjustStock\");");
        adjustStockViewSource.Should().Contain("<partial name=\"_AdjustStockEditorShell\" model=\"Model\" />");

        reserveStockViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.InventoryReserveActionVm");
        reserveStockViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"ReserveStock\");");
        reserveStockViewSource.Should().Contain("<partial name=\"_ReserveStockEditorShell\" model=\"Model\" />");

        releaseReservationViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.InventoryReleaseReservationActionVm");
        releaseReservationViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"ReleaseReservation\");");
        releaseReservationViewSource.Should().Contain("<partial name=\"_ReleaseReservationEditorShell\" model=\"Model\" />");

        returnReceiptViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Inventory.InventoryReturnReceiptActionVm");
        returnReceiptViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"ProcessReturnReceipt\");");
        returnReceiptViewSource.Should().Contain("<partial name=\"_ReturnReceiptEditorShell\" model=\"Model\" />");

        adjustPointsViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Loyalty.AdjustLoyaltyPointsVm");
        adjustPointsViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"AdjustLoyaltyPoints\");");
        adjustPointsViewSource.Should().Contain("<partial name=\"_AdjustPointsEditorShell\" model=\"Model\" />");
    }


    [Fact]
    public void RolePermissionsWorkspace_Should_KeepHeadingFormAndSelectionContractsWired()
    {
        var rolePermissionsViewSource = ReadWebAdminFile(Path.Combine("Views", "Roles", "Permissions.cshtml"));

        rolePermissionsViewSource.Should().Contain("@model Darwin.WebAdmin.ViewModels.Identity.RolePermissionsEditVm");
        rolePermissionsViewSource.Should().Contain("ViewData[\"Title\"] = T.T(\"EditRolePermissions\");");
        rolePermissionsViewSource.Should().Contain("@T.T(\"PermissionsForRole\") - @Model.RoleDisplayName");
        rolePermissionsViewSource.Should().Contain("<partial name=\"~/Views/Shared/_Alerts.cshtml\" />");
        rolePermissionsViewSource.Should().Contain("<form asp-action=\"Permissions\" method=\"post\" class=\"row g-3\">");
        rolePermissionsViewSource.Should().Contain("@Html.AntiForgeryToken()");
        rolePermissionsViewSource.Should().Contain("<input type=\"hidden\" asp-for=\"RoleId\" />");
        rolePermissionsViewSource.Should().Contain("<input type=\"hidden\" asp-for=\"RowVersion\" />");
        rolePermissionsViewSource.Should().Contain("name=\"SelectedPermissionIds\"");
        rolePermissionsViewSource.Should().Contain("@p.Key - @p.Description");
        rolePermissionsViewSource.Should().Contain("@T.T(\"NoPermissionsFound\")");
        rolePermissionsViewSource.Should().Contain("asp-controller=\"Roles\" asp-action=\"Index\"");
        rolePermissionsViewSource.Should().Contain("@T.T(\"Back\")");
        rolePermissionsViewSource.Should().Contain("@T.T(\"Save\")");
    }


    [Fact]
    public void ICurrentUserService_Should_KeepUserIdResolutionContractWired()
    {
        var source = ReadApplicationFile(Path.Combine("Abstractions", "Auth", "ICurrentUserService.cs"));

        source.Should().Contain("public interface ICurrentUserService");
        source.Should().Contain("Provides the identity (UserId) of the current actor executing the use case.");
        source.Should().Contain("Guid GetCurrentUserId();");
    }


    [Fact]
    public void ILoyaltyPresentationService_Should_KeepRewardEnrichmentAndAvailableRewardMethodFloorWired()
    {
        var source = ReadWebApiFile(Path.Combine("Services", "ILoyaltyPresentationService.cs"));

        source.Should().Contain("public interface ILoyaltyPresentationService");
        source.Should().Contain("Presentation-focused helper for loyalty-related UI shaping tasks.");
        source.Should().Contain("Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> EnrichSelectedRewardsAsync(");
        source.Should().Contain("Guid businessId,");
        source.Should().Contain("IReadOnlyCollection<Guid>? selectedTierIds,");
        source.Should().Contain("bool failIfMissing,");
        source.Should().Contain("Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> GetAvailableRewardsForBusinessAsync(");
    }


    [Fact]
    public void LoyaltyPresentationService_Should_KeepCacheNormalizationAndMissingRewardFallbackContractsWired()
    {
        var source = ReadWebApiFile(Path.Combine("Services", "LoyaltyPresentationService.cs"));

        source.Should().Contain("public sealed class LoyaltyPresentationService : ILoyaltyPresentationService");
        source.Should().Contain("private readonly GetAvailableLoyaltyRewardsForBusinessHandler _availableRewardsHandler;");
        source.Should().Contain("private readonly IMemoryCache _cache;");
        source.Should().Contain("private readonly ILogger<LoyaltyPresentationService> _logger;");
        source.Should().Contain("private const int DefaultAvailableRewardsCacheSeconds = 60;");
        source.Should().Contain("public LoyaltyPresentationService(");
        source.Should().Contain("_availableRewardsHandler = availableRewardsHandler ?? throw new ArgumentNullException(nameof(availableRewardsHandler));");
        source.Should().Contain("_cache = cache ?? throw new ArgumentNullException(nameof(cache));");
        source.Should().Contain("_logger = logger ?? throw new ArgumentNullException(nameof(logger));");
        source.Should().Contain("public async Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> EnrichSelectedRewardsAsync(");
        source.Should().Contain("if (businessId == Guid.Empty)");
        source.Should().Contain("return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(\"BusinessId is required.\");");
        source.Should().Contain("if (selectedTierIds is null || selectedTierIds.Count == 0)");
        source.Should().Contain("return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(Array.Empty<LoyaltyRewardSummary>());");
        source.Should().Contain("var orderedDistinct = selectedTierIds");
        source.Should().Contain(".Where(x => x != Guid.Empty)");
        source.Should().Contain(".Distinct()");
        source.Should().Contain(".ToList();");
        source.Should().Contain("if (orderedDistinct.Count == 0)");
        source.Should().Contain("var availableResult = await GetAvailableRewardsForBusinessAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("if (!availableResult.Succeeded || availableResult.Value is null)");
        source.Should().Contain("_logger.LogWarning(\"Failed to load available rewards for business {BusinessId}. failIfMissing={FailIfMissing}\", businessId, failIfMissing);");
        source.Should().Contain("if (failIfMissing)");
        source.Should().Contain("return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(\"Could not load business rewards for enrichment.\");");
        source.Should().Contain("var dict = available.ToDictionary(r => r.LoyaltyRewardTierId);");
        source.Should().Contain("var missing = new List<Guid>();");
        source.Should().Contain("var resultList = new List<LoyaltyRewardSummary>(orderedDistinct.Count);");
        source.Should().Contain("foreach (var id in orderedDistinct)");
        source.Should().Contain("if (dict.TryGetValue(id, out var reward))");
        source.Should().Contain("resultList.Add(reward);");
        source.Should().Contain("missing.Add(id);");
        source.Should().Contain("if (missing.Count > 0 && failIfMissing)");
        source.Should().Contain("_logger.LogWarning(\"Missing {MissingCount} selected reward(s) for business {BusinessId}. MissingExample={MissingExample}\", missing.Count, businessId, missing.FirstOrDefault());");
        source.Should().Contain("return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(\"Some selected rewards are not available for this business.\");");
        source.Should().Contain("return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(resultList);");
        source.Should().Contain("public async Task<Result<IReadOnlyList<LoyaltyRewardSummary>>> GetAvailableRewardsForBusinessAsync(");
        source.Should().Contain("var cacheKey = GetCacheKey(businessId);");
        source.Should().Contain("if (_cache.TryGetValue(cacheKey, out IReadOnlyList<LoyaltyRewardSummary>? cached) && cached is not null)");
        source.Should().Contain("return Result<IReadOnlyList<LoyaltyRewardSummary>>.Ok(cached);");
        source.Should().Contain("var handlerResult = await _availableRewardsHandler.HandleAsync(businessId, ct).ConfigureAwait(false);");
        source.Should().Contain("if (!handlerResult.Succeeded || handlerResult.Value is null)");
        source.Should().Contain("_logger.LogWarning(\"GetAvailableLoyaltyRewardsForBusinessHandler failed for business {BusinessId}\", businessId);");
        source.Should().Contain("return Result<IReadOnlyList<LoyaltyRewardSummary>>.Fail(handlerResult.Error ?? \"Failed to load available rewards.\");");
        source.Should().Contain(".Select(Darwin.WebApi.Mappers.LoyaltyContractsMapper.ToContract)");
        source.Should().Contain(".ToList()");
        source.Should().Contain(".AsReadOnly();");
        source.Should().Contain("var cacheEntryOptions = new MemoryCacheEntryOptions");
        source.Should().Contain("AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(DefaultAvailableRewardsCacheSeconds)");
        source.Should().Contain("_cache.Set(cacheKey, mapped, cacheEntryOptions);");
        source.Should().Contain("private static string GetCacheKey(Guid businessId) => $\"loyalty:availableRewards:{businessId:N}\";");
    }


    [Fact]
    public void MemberOrdersAndInvoicesControllers_Should_KeepHistoryRetryAndDocumentContractsWired()
    {
        var ordersSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberOrdersController.cs"));
        var invoicesSource = ReadWebApiFile(Path.Combine("Controllers", "Member", "MemberInvoicesController.cs"));

        ordersSource.Should().Contain("public sealed class MemberOrdersController : ApiControllerBase");
        ordersSource.Should().Contain("private readonly GetMyOrdersPageHandler _getMyOrdersPageHandler;");
        ordersSource.Should().Contain("private readonly GetMyOrderForViewHandler _getMyOrderForViewHandler;");
        ordersSource.Should().Contain("private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;");
        ordersSource.Should().Contain("private readonly StorefrontCheckoutUrlBuilder _checkoutUrlBuilder;");
        ordersSource.Should().Contain("_getMyOrdersPageHandler = getMyOrdersPageHandler ?? throw new ArgumentNullException(nameof(getMyOrdersPageHandler));");
        ordersSource.Should().Contain("_getMyOrderForViewHandler = getMyOrderForViewHandler ?? throw new ArgumentNullException(nameof(getMyOrderForViewHandler));");
        ordersSource.Should().Contain("_createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));");
        ordersSource.Should().Contain("_checkoutUrlBuilder = checkoutUrlBuilder ?? throw new ArgumentNullException(nameof(checkoutUrlBuilder));");
        ordersSource.Should().Contain("public async Task<IActionResult> GetMyOrdersAsync([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct = default)");
        ordersSource.Should().Contain("var normalizedPage = page.GetValueOrDefault(1);");
        ordersSource.Should().Contain("var normalizedPageSize = pageSize.GetValueOrDefault(20);");
        ordersSource.Should().Contain("var (items, total) = await _getMyOrdersPageHandler");
        ordersSource.Should().Contain("return Ok(new PagedResponse<MemberOrderSummary>");
        ordersSource.Should().Contain("Items = items.Select(MapSummary).ToList(),");
        ordersSource.Should().Contain("public async Task<IActionResult> GetMyOrderAsync(Guid id, CancellationToken ct = default)");
        ordersSource.Should().Contain("if (id == Guid.Empty)");
        ordersSource.Should().Contain("return BadRequestProblem(\"Id must not be empty.\");");
        ordersSource.Should().Contain("var dto = await _getMyOrderForViewHandler.HandleAsync(id, ct).ConfigureAwait(false);");
        ordersSource.Should().Contain("if (dto is null)");
        ordersSource.Should().Contain("return NotFoundProblem(\"Order not found.\");");
        ordersSource.Should().Contain("return Ok(MapDetail(dto));");
        ordersSource.Should().Contain("public async Task<IActionResult> CreatePaymentIntentAsync(Guid id, [FromBody] CreateStorefrontPaymentIntentRequest? request, CancellationToken ct = default)");
        ordersSource.Should().Contain("if (!CanRetryPayment(dto))");
        ordersSource.Should().Contain("return BadRequestProblem(\"Order cannot accept a new payment attempt.\");");
        ordersSource.Should().Contain("var result = await _createStorefrontPaymentIntentHandler.HandleAsync(new CreateStorefrontPaymentIntentDto");
        ordersSource.Should().Contain("OrderNumber = dto.OrderNumber,");
        ordersSource.Should().Contain("Provider = string.IsNullOrWhiteSpace(request?.Provider) ? \"DarwinCheckout\" : request.Provider.Trim()");
        ordersSource.Should().Contain("var returnUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.Id, dto.OrderNumber, cancelled: false);");
        ordersSource.Should().Contain("var cancelUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.Id, dto.OrderNumber, cancelled: true);");
        ordersSource.Should().Contain("var checkoutUrl = _checkoutUrlBuilder.BuildGatewayUrl(result, returnUrl, cancelUrl);");
        ordersSource.Should().Contain("return BadRequestProblem(\"Payment intent could not be created.\", ex.Message);");
        ordersSource.Should().Contain("public async Task<IActionResult> DownloadDocumentAsync(Guid id, CancellationToken ct = default)");
        ordersSource.Should().Contain("var fileName = $\"order-{SanitizeFileToken(dto.OrderNumber)}.txt\";");
        ordersSource.Should().Contain("var bytes = Encoding.UTF8.GetBytes(RenderOrderDocument(dto));");
        ordersSource.Should().Contain("return File(bytes, \"text/plain; charset=utf-8\", fileName);");
        ordersSource.Should().Contain("private static MemberOrderActions BuildActions(MemberOrderDetailDto dto)");
        ordersSource.Should().Contain("CanRetryPayment = canRetryPayment,");
        ordersSource.Should().Contain("PaymentIntentPath = canRetryPayment ? GetPaymentIntentPath(dto.Id) : null,");
        ordersSource.Should().Contain("private static bool CanRetryPayment(MemberOrderDetailDto dto)");
        ordersSource.Should().Contain("dto.Status is not OrderStatus.Cancelled and not OrderStatus.Refunded");
        ordersSource.Should().Contain("dto.Payments.All(payment => payment.Status is not PaymentStatus.Captured and not PaymentStatus.Completed);");
        ordersSource.Should().Contain("private static string GetPaymentIntentPath(Guid id) => $\"/api/v1/member/orders/{id:D}/payment-intent\";");
        ordersSource.Should().Contain("private static string GetConfirmationPath(Guid id) => $\"/api/v1/public/checkout/orders/{id:D}/confirmation\";");
        ordersSource.Should().Contain("private static string GetDocumentPath(Guid id) => $\"/api/v1/member/orders/{id:D}/document\";");
        ordersSource.Should().Contain("private static string SanitizeFileToken(string value)");
        ordersSource.Should().Contain("invalidChars.Contains(ch) ? '-' : ch");
        ordersSource.Should().Contain("private static string RenderOrderDocument(MemberOrderDetailDto dto)");
        ordersSource.Should().Contain("builder.AppendLine(\"Lines:\");");
        ordersSource.Should().Contain("builder.AppendLine(\"Payments:\");");
        ordersSource.Should().Contain("builder.AppendLine(\"Shipments:\");");
        ordersSource.Should().Contain("builder.AppendLine(\"Invoices:\");");

        invoicesSource.Should().Contain("public sealed class MemberInvoicesController : ApiControllerBase");
        invoicesSource.Should().Contain("private readonly GetMyInvoicesPageHandler _getMyInvoicesPageHandler;");
        invoicesSource.Should().Contain("private readonly GetMyInvoiceDetailHandler _getMyInvoiceDetailHandler;");
        invoicesSource.Should().Contain("private readonly CreateStorefrontPaymentIntentHandler _createStorefrontPaymentIntentHandler;");
        invoicesSource.Should().Contain("private readonly StorefrontCheckoutUrlBuilder _checkoutUrlBuilder;");
        invoicesSource.Should().Contain("_getMyInvoicesPageHandler = getMyInvoicesPageHandler ?? throw new ArgumentNullException(nameof(getMyInvoicesPageHandler));");
        invoicesSource.Should().Contain("_getMyInvoiceDetailHandler = getMyInvoiceDetailHandler ?? throw new ArgumentNullException(nameof(getMyInvoiceDetailHandler));");
        invoicesSource.Should().Contain("_createStorefrontPaymentIntentHandler = createStorefrontPaymentIntentHandler ?? throw new ArgumentNullException(nameof(createStorefrontPaymentIntentHandler));");
        invoicesSource.Should().Contain("_checkoutUrlBuilder = checkoutUrlBuilder ?? throw new ArgumentNullException(nameof(checkoutUrlBuilder));");
        invoicesSource.Should().Contain("public async Task<IActionResult> GetMyInvoicesAsync([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct = default)");
        invoicesSource.Should().Contain("var normalizedPageSize = pageSize.GetValueOrDefault(20);");
        invoicesSource.Should().Contain("var (items, total) = await _getMyInvoicesPageHandler");
        invoicesSource.Should().Contain("Items = items.Select(MapSummary).ToList(),");
        invoicesSource.Should().Contain("public async Task<IActionResult> GetMyInvoiceAsync(Guid id, CancellationToken ct = default)");
        invoicesSource.Should().Contain("return NotFoundProblem(\"Invoice not found.\");");
        invoicesSource.Should().Contain("public async Task<IActionResult> CreatePaymentIntentAsync(Guid id, [FromBody] CreateStorefrontPaymentIntentRequest? request, CancellationToken ct = default)");
        invoicesSource.Should().Contain("if (!dto.OrderId.HasValue)");
        invoicesSource.Should().Contain("return BadRequestProblem(\"Invoice is not linked to an order and cannot open a storefront payment flow.\");");
        invoicesSource.Should().Contain("if (!CanRetryPayment(dto))");
        invoicesSource.Should().Contain("return BadRequestProblem(\"Invoice cannot accept a new payment attempt.\");");
        invoicesSource.Should().Contain("OrderId = dto.OrderId.Value,");
        invoicesSource.Should().Contain("var returnUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.OrderId.Value, dto.OrderNumber, cancelled: false);");
        invoicesSource.Should().Contain("var cancelUrl = _checkoutUrlBuilder.BuildFrontOfficeConfirmationUrl(dto.OrderId.Value, dto.OrderNumber, cancelled: true);");
        invoicesSource.Should().Contain("return BadRequestProblem(\"Payment intent could not be created.\", ex.Message);");
        invoicesSource.Should().Contain("public async Task<IActionResult> DownloadDocumentAsync(Guid id, CancellationToken ct = default)");
        invoicesSource.Should().Contain("var fileName = $\"invoice-{SanitizeFileToken(dto.OrderNumber ?? dto.Id.ToString(\"D\"))}.txt\";");
        invoicesSource.Should().Contain("var bytes = Encoding.UTF8.GetBytes(RenderInvoiceDocument(dto));");
        invoicesSource.Should().Contain("private static MemberInvoiceActions BuildActions(MemberInvoiceDetailDto dto)");
        invoicesSource.Should().Contain("PaymentIntentPath = canRetryPayment ? GetPaymentIntentPath(dto.Id) : null,");
        invoicesSource.Should().Contain("OrderPath = dto.OrderId.HasValue ? GetOrderPath(dto.OrderId.Value) : null,");
        invoicesSource.Should().Contain("private static bool CanRetryPayment(MemberInvoiceDetailDto dto)");
        invoicesSource.Should().Contain("dto.Status is not InvoiceStatus.Cancelled &&");
        invoicesSource.Should().Contain("dto.BalanceMinor > 0;");
        invoicesSource.Should().Contain("private static string GetPaymentIntentPath(Guid id) => $\"/api/v1/member/invoices/{id:D}/payment-intent\";");
        invoicesSource.Should().Contain("private static string GetOrderPath(Guid id) => $\"/api/v1/member/orders/{id:D}\";");
        invoicesSource.Should().Contain("private static string GetDocumentPath(Guid id) => $\"/api/v1/member/invoices/{id:D}/document\";");
        invoicesSource.Should().Contain("private static string RenderInvoiceDocument(MemberInvoiceDetailDto dto)");
        invoicesSource.Should().Contain("builder.AppendLine(\"Lines:\");");
    }


    [Fact]
    public void BusinessAccountAndLoyaltyControllers_Should_KeepBusinessContextRewardCampaignAndScanOrchestrationContractsWired()
    {
        var accountSource = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessAccountController.cs"));
        var loyaltySource = ReadWebApiFile(Path.Combine("Controllers", "Business", "BusinessLoyaltyController.cs"));

        accountSource.Should().Contain("public sealed class BusinessAccountController : ApiControllerBase");
        accountSource.Should().Contain("private readonly GetCurrentBusinessAccessStateHandler _getCurrentBusinessAccessStateHandler;");
        accountSource.Should().Contain("_getCurrentBusinessAccessStateHandler = getCurrentBusinessAccessStateHandler ?? throw new ArgumentNullException(nameof(getCurrentBusinessAccessStateHandler));");
        accountSource.Should().Contain("public async Task<IActionResult> GetAccessStateAsync(CancellationToken ct = default)");
        accountSource.Should().Contain("if (!BusinessControllerConventions.TryGetCurrentBusinessId(User, out var businessId))");
        accountSource.Should().Contain("return BadRequestProblem(\"Business context is required.\");");
        accountSource.Should().Contain("if (!BusinessControllerConventions.TryGetCurrentUserId(User, out var userId))");
        accountSource.Should().Contain("return BadRequestProblem(\"User context is required.\");");
        accountSource.Should().Contain("var dto = await _getCurrentBusinessAccessStateHandler.HandleAsync(businessId, userId, ct).ConfigureAwait(false);");
        accountSource.Should().Contain("return NotFoundProblem(\"Business was not found.\");");
        accountSource.Should().Contain("return Ok(new BusinessAccessStateResponse");
        accountSource.Should().Contain("UserId = dto.UserId,");
        accountSource.Should().Contain("HasActiveMembership = dto.HasActiveMembership,");
        accountSource.Should().Contain("IsUserActive = dto.IsUserActive,");
        accountSource.Should().Contain("IsUserEmailConfirmed = dto.IsUserEmailConfirmed,");
        accountSource.Should().Contain("IsUserLockedOut = dto.IsUserLockedOut,");
        accountSource.Should().Contain("IsApprovalPending = dto.IsApprovalPending,");
        accountSource.Should().Contain("IsSuspended = dto.IsSuspended,");
        accountSource.Should().Contain("IsBusinessClientAccessAllowed = dto.IsBusinessClientAccessAllowed,");
        accountSource.Should().Contain("OperationalStatus = dto.OperationalStatus.ToString(),");
        accountSource.Should().Contain("IsOperationsAllowed = dto.IsOperationsAllowed,");
        accountSource.Should().Contain("HasActivationBlockingIssues = dto.HasActivationBlockingIssues,");
        accountSource.Should().Contain("SetupIncompleteItemCount = dto.SetupIncompleteItemCount,");
        accountSource.Should().Contain("PrimaryBlockingCode = dto.PrimaryBlockingCode,");
        accountSource.Should().Contain("BlockingReason = dto.BlockingReason");

        loyaltySource.Should().Contain("public sealed class BusinessLoyaltyController : ApiControllerBase");
        loyaltySource.Should().Contain("private readonly ProcessScanSessionForBusinessHandler _processScanSessionForBusinessHandler;");
        loyaltySource.Should().Contain("private readonly ConfirmAccrualFromSessionHandler _confirmAccrualFromSessionHandler;");
        loyaltySource.Should().Contain("private readonly ConfirmRedemptionFromSessionHandler _confirmRedemptionFromSessionHandler;");
        loyaltySource.Should().Contain("private readonly GetLoyaltyProgramsPageHandler _getLoyaltyProgramsPageHandler;");
        loyaltySource.Should().Contain("private readonly GetLoyaltyRewardTiersPageHandler _getLoyaltyRewardTiersPageHandler;");
        loyaltySource.Should().Contain("private readonly CreateLoyaltyProgramHandler _createLoyaltyProgramHandler;");
        loyaltySource.Should().Contain("private readonly CreateLoyaltyRewardTierHandler _createLoyaltyRewardTierHandler;");
        loyaltySource.Should().Contain("private readonly UpdateLoyaltyRewardTierHandler _updateLoyaltyRewardTierHandler;");
        loyaltySource.Should().Contain("private readonly SoftDeleteLoyaltyRewardTierHandler _softDeleteLoyaltyRewardTierHandler;");
        loyaltySource.Should().Contain("private readonly GetBusinessCampaignsHandler _getBusinessCampaignsHandler;");
        loyaltySource.Should().Contain("private readonly CreateBusinessCampaignHandler _createBusinessCampaignHandler;");
        loyaltySource.Should().Contain("private readonly UpdateBusinessCampaignHandler _updateBusinessCampaignHandler;");
        loyaltySource.Should().Contain("private readonly SetCampaignActivationHandler _setCampaignActivationHandler;");
        loyaltySource.Should().Contain("private readonly ILoyaltyPresentationService _presentationService;");
        loyaltySource.Should().Contain("_processScanSessionForBusinessHandler = processScanSessionForBusinessHandler ?? throw new ArgumentNullException(nameof(processScanSessionForBusinessHandler));");
        loyaltySource.Should().Contain("_presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));");

        loyaltySource.Should().Contain("public async Task<IActionResult> GetBusinessRewardConfigurationAsync(CancellationToken ct = default)");
        loyaltySource.Should().Contain("var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("if (!hasBusinessAccess)");
        loyaltySource.Should().Contain("return errorResult ?? Forbid();");
        loyaltySource.Should().Contain(".HandleAsync(page: 1, pageSize: 1, businessId: businessId, ct: ct)");
        loyaltySource.Should().Contain("RewardTiers = Array.Empty<BusinessRewardTierConfigItem>()");
        loyaltySource.Should().Contain(".HandleAsync(program.Id, page: 1, pageSize: 200, filter: LoyaltyRewardTierQueueFilter.All, ct: ct)");
        loyaltySource.Should().Contain("RewardType = x.RewardType.ToString(),");

        loyaltySource.Should().Contain("public async Task<IActionResult> CreateBusinessRewardTierAsync([FromBody] CreateBusinessRewardTierRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"Request body is required.\");");
        loyaltySource.Should().Contain("if (!TryParseRewardType(request.RewardType, out var rewardType))");
        loyaltySource.Should().Contain("return BadRequestProblem(\"RewardType is invalid. Allowed values: FreeItem, PercentDiscount, AmountDiscount.\");");
        loyaltySource.Should().Contain("var programId = await EnsureBusinessProgramAsync(businessId, createIfMissing: true, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("HandleAsync(new LoyaltyRewardTierCreateDto");
        loyaltySource.Should().Contain("AllowSelfRedemption = request.AllowSelfRedemption,");
        loyaltySource.Should().Contain("MetadataJson = request.MetadataJson");
        loyaltySource.Should().Contain("catch (FluentValidation.ValidationException ex)");
        loyaltySource.Should().Contain("return BadRequestProblem(ex.Message);");

        loyaltySource.Should().Contain("public async Task<IActionResult> UpdateBusinessRewardTierAsync([FromBody] UpdateBusinessRewardTierRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"RewardTierId is required.\");");
        loyaltySource.Should().Contain("var programId = await EnsureBusinessProgramAsync(businessId, createIfMissing: false, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("return BadRequestProblem(\"No loyalty program was found for the current business.\");");
        loyaltySource.Should().Contain("if (!await IsRewardTierOwnedByBusinessAsync(programId, request.RewardTierId, ct).ConfigureAwait(false))");
        loyaltySource.Should().Contain("return Forbid();");
        loyaltySource.Should().Contain("HandleAsync(new LoyaltyRewardTierEditDto");
        loyaltySource.Should().Contain("RowVersion = request.RowVersion");

        loyaltySource.Should().Contain("public async Task<IActionResult> DeleteBusinessRewardTierAsync([FromBody] DeleteBusinessRewardTierRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("HandleAsync(new LoyaltyRewardTierDeleteDto { Id = request.RewardTierId, RowVersion = request.RowVersion }, ct)");
        loyaltySource.Should().Contain("if (!result.Succeeded)");
        loyaltySource.Should().Contain("return ProblemFromResult(result);");
        loyaltySource.Should().Contain("return Ok(new BusinessRewardTierMutationResponse { RewardTierId = request.RewardTierId, Success = true });");

        loyaltySource.Should().Contain("public async Task<IActionResult> ProcessScanSessionForBusinessAsync([FromBody] ProcessScanSessionForBusinessRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"ScanSessionToken is required.\");");
        loyaltySource.Should().Contain(".HandleAsync(request.ScanSessionToken, businessId, ct)");
        loyaltySource.Should().Contain("if (!result.Succeeded || result.Value is null)");
        loyaltySource.Should().Contain("return MapScanFailure(result);");
        loyaltySource.Should().Contain("var tierIds = result.Value.SelectedRewards");
        loyaltySource.Should().Contain(".EnrichSelectedRewardsAsync(businessId, tierIds, failIfMissing: true, ct)");
        loyaltySource.Should().Contain("var allowedActions =");
        loyaltySource.Should().Contain("? LoyaltyScanAllowedActions.CanConfirmAccrual");
        loyaltySource.Should().Contain(": LoyaltyScanAllowedActions.CanConfirmRedemption;");
        loyaltySource.Should().Contain("SelectedRewards = selectedRewards,");

        loyaltySource.Should().Contain("public async Task<IActionResult> ConfirmAccrualAsync([FromBody] ConfirmAccrualRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"ScanSessionToken is too long.\");");
        loyaltySource.Should().Contain("return BadRequestProblem(\"Points must be greater than zero.\");");
        loyaltySource.Should().Contain("HandleAsync(new ConfirmAccrualFromSessionDto");
        loyaltySource.Should().Contain("Points = request.Points,");
        loyaltySource.Should().Contain("Note = request.Note");
        loyaltySource.Should().Contain("NewBalance = result.Value.NewPointsBalance,");

        loyaltySource.Should().Contain("public async Task<IActionResult> ConfirmRedemptionAsync([FromBody] ConfirmRedemptionRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("HandleAsync(new ConfirmRedemptionFromSessionDto");
        loyaltySource.Should().Contain("ScanSessionToken = request.ScanSessionToken");

        loyaltySource.Should().Contain("public async Task<IActionResult> GetBusinessCampaignsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)");
        loyaltySource.Should().Contain("var result = await _getBusinessCampaignsHandler.HandleAsync(businessId, page, pageSize, filter: LoyaltyCampaignQueueFilter.All, ct: ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("return Ok(new GetBusinessCampaignsResponse");
        loyaltySource.Should().Contain("EligibilityRules = x.EligibilityRules.Select(rule => new PromotionEligibilityRule");

        loyaltySource.Should().Contain("public async Task<IActionResult> CreateBusinessCampaignAsync([FromBody] CreateBusinessCampaignRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("var result = await _createBusinessCampaignHandler.HandleAsync(new CreateBusinessCampaignDto");
        loyaltySource.Should().Contain("BusinessId = businessId,");
        loyaltySource.Should().Contain("EligibilityRules = request.EligibilityRules.Select(rule => new Darwin.Application.Loyalty.Campaigns.PromotionEligibilityRuleDto");
        loyaltySource.Should().Contain("return Created($\"/api/v1/business/loyalty/campaigns/{result.Value}\", new BusinessCampaignMutationResponse { CampaignId = result.Value });");

        loyaltySource.Should().Contain("public async Task<IActionResult> UpdateBusinessCampaignAsync(Guid id, [FromBody] UpdateBusinessCampaignRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("if (request is null || request.Id != id)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"Request body is required and route id must match body id.\");");
        loyaltySource.Should().Contain("var result = await _updateBusinessCampaignHandler.HandleAsync(new UpdateBusinessCampaignDto");
        loyaltySource.Should().Contain("return NoContent();");

        loyaltySource.Should().Contain("public async Task<IActionResult> SetBusinessCampaignActivationAsync(Guid id, [FromBody] SetCampaignActivationRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("var result = await _setCampaignActivationHandler.HandleAsync(new SetCampaignActivationDto");
        loyaltySource.Should().Contain("IsActive = request.IsActive,");
        loyaltySource.Should().Contain("RowVersion = request.RowVersion");

        loyaltySource.Should().Contain("private async Task<(bool Success, Guid BusinessId, IActionResult? ErrorResult)> TryGetCurrentBusinessIdAsync(CancellationToken ct)");
        loyaltySource.Should().Contain("!BusinessControllerConventions.TryGetCurrentUserId(User, out var userId)");
        loyaltySource.Should().Contain("var accessState = await _getCurrentBusinessAccessStateHandler.HandleAsync(businessId, userId, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("if (!accessState.IsBusinessClientAccessAllowed || !accessState.IsOperationsAllowed)");
        loyaltySource.Should().Contain("return (false, Guid.Empty, Forbid(accessState.BlockingReason));");
        loyaltySource.Should().Contain("private async Task<Guid> EnsureBusinessProgramAsync(Guid businessId, bool createIfMissing, CancellationToken ct)");
        loyaltySource.Should().Contain("if (!createIfMissing)");
        loyaltySource.Should().Contain("return Guid.Empty;");
        loyaltySource.Should().Contain("Name = \"Default Loyalty Program\",");
        loyaltySource.Should().Contain("AccrualMode = Darwin.Domain.Enums.LoyaltyAccrualMode.PerVisit,");
        loyaltySource.Should().Contain("private async Task<bool> IsRewardTierOwnedByBusinessAsync(Guid programId, Guid rewardTierId, CancellationToken ct)");
        loyaltySource.Should().Contain("return tiers.Items.Any(x => x.Id == rewardTierId);");
        loyaltySource.Should().Contain("private static bool TryParseRewardType(string? rewardType, out DomainLoyaltyRewardType value)");
        loyaltySource.Should().Contain("Enum.TryParse(rewardType, ignoreCase: true, out DomainLoyaltyRewardType parsed)");
        loyaltySource.Should().Contain("private IActionResult MapScanFailure<T>(Result<T> result)");
        loyaltySource.Should().Contain("if (text.Contains(\"expired\") || text.Contains(\"consumed\"))");
        loyaltySource.Should().Contain("return ConflictProblem(msg);");
        loyaltySource.Should().Contain("if (text.Contains(\"not found\"))");
        loyaltySource.Should().Contain("return NotFoundProblem(msg);");
        loyaltySource.Should().Contain("text.Contains(\"belongs to a different business\") ||");
        loyaltySource.Should().Contain("return ProblemFromResult(result);");
        loyaltySource.Should().Contain("private IActionResult ConflictProblem(string detail)");
        loyaltySource.Should().Contain("Title = \"Conflict\",");
        loyaltySource.Should().Contain("Status = StatusCodes.Status409Conflict,");
        loyaltySource.Should().Contain("return StatusCode(StatusCodes.Status409Conflict, problem);");
    }


    [Fact]
    public void ShippingBillingAndMemberLoyaltyControllers_Should_KeepQuoteCheckoutAndTimelineContractsWired()
    {
        var shippingSource = ReadWebApiFile(Path.Combine("Controllers", "Public", "PublicShippingController.cs"));
        var billingSource = ReadWebApiFile(Path.Combine("Controllers", "Billing", "BillingController.cs"));
        var loyaltySource = ReadWebApiFile(Path.Combine("Controllers", "Loyalty", "LoyaltyController.cs"));

        shippingSource.Should().Contain("public sealed class PublicShippingController : ApiControllerBase");
        shippingSource.Should().Contain("private readonly RateShipmentHandler _rateShipmentHandler;");
        shippingSource.Should().Contain("_rateShipmentHandler = rateShipmentHandler ?? throw new ArgumentNullException(nameof(rateShipmentHandler));");
        shippingSource.Should().Contain("public async Task<IActionResult> GetRatesAsync([FromBody] PublicShippingRateRequest? request, CancellationToken ct = default)");
        shippingSource.Should().Contain("return BadRequestProblem(\"Request body is required.\");");
        shippingSource.Should().Contain("var items = await _rateShipmentHandler.HandleAsync(new RateShipmentInputDto");
        shippingSource.Should().Contain("Country = request.Country,");
        shippingSource.Should().Contain("SubtotalNetMinor = request.SubtotalNetMinor,");
        shippingSource.Should().Contain("ShipmentMass = request.ShipmentMass,");
        shippingSource.Should().Contain("Currency = request.Currency");
        shippingSource.Should().Contain("}, request.Currency ?? SiteSettingDto.DefaultCurrencyDefault, ct).ConfigureAwait(false);");
        shippingSource.Should().Contain("return Ok(items.Select(MapOption).ToList());");
        shippingSource.Should().Contain("catch (Exception ex) when (ex is InvalidOperationException || ex is FluentValidation.ValidationException)");
        shippingSource.Should().Contain("return BadRequestProblem(\"Shipping options could not be calculated.\", ex.Message);");
        shippingSource.Should().Contain("private static PublicShippingOption MapOption(ShippingOptionDto dto)");
        shippingSource.Should().Contain("MethodId = dto.MethodId,");
        shippingSource.Should().Contain("Carrier = dto.Carrier,");
        shippingSource.Should().Contain("Service = dto.Service");

        billingSource.Should().Contain("public sealed class BillingController : ApiControllerBase");
        billingSource.Should().Contain("private readonly GetBusinessSubscriptionStatusHandler _getBusinessSubscriptionStatusHandler;");
        billingSource.Should().Contain("private readonly SetCancelAtPeriodEndHandler _setCancelAtPeriodEndHandler;");
        billingSource.Should().Contain("private readonly GetBillingPlansHandler _getBillingPlansHandler;");
        billingSource.Should().Contain("private readonly CreateSubscriptionCheckoutIntentHandler _createSubscriptionCheckoutIntentHandler;");
        billingSource.Should().Contain("private readonly IConfiguration _configuration;");
        billingSource.Should().Contain("_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));");
        billingSource.Should().Contain("public async Task<IActionResult> GetCurrentBusinessSubscriptionAsync(CancellationToken ct = default)");
        billingSource.Should().Contain("var (hasBusinessAccess, businessId, errorResult) = await TryGetCurrentBusinessIdAsync(requireOperationsAllowed: false, ct).ConfigureAwait(false);");
        billingSource.Should().Contain("if (!hasBusinessAccess)");
        billingSource.Should().Contain("var result = await _getBusinessSubscriptionStatusHandler");
        billingSource.Should().Contain("return ProblemFromResult(result, \"Failed to retrieve business subscription status.\");");
        billingSource.Should().Contain("return Ok(MapStatus(result.Value));");
        billingSource.Should().Contain("public async Task<IActionResult> SetCancelAtPeriodEndAsync([FromBody] SetCancelAtPeriodEndRequest request, CancellationToken ct = default)");
        billingSource.Should().Contain("if (request is null)");
        billingSource.Should().Contain("return BadRequestProblem(\"Request body is required.\");");
        billingSource.Should().Contain("HandleAsync(");
        billingSource.Should().Contain("request.SubscriptionId,");
        billingSource.Should().Contain("request.CancelAtPeriodEnd,");
        billingSource.Should().Contain("request.RowVersion,");
        billingSource.Should().Contain("return Ok(new SetCancelAtPeriodEndResponse");
        billingSource.Should().Contain("CancelAtPeriodEnd = result.Value.CancelAtPeriodEnd,");
        billingSource.Should().Contain("public async Task<IActionResult> GetBillingPlansAsync([FromQuery] bool activeOnly = true, CancellationToken ct = default)");
        billingSource.Should().Contain("var (hasBusinessAccess, _, errorResult) = await TryGetCurrentBusinessIdAsync(requireOperationsAllowed: false, ct).ConfigureAwait(false);");
        billingSource.Should().Contain("var dto = await _getBillingPlansHandler");
        billingSource.Should().Contain("var response = new GetBillingPlansResponse");
        billingSource.Should().Contain("Items = dto.Items");
        billingSource.Should().Contain("TrialDays = x.TrialDays,");
        billingSource.Should().Contain("public async Task<IActionResult> CreateSubscriptionCheckoutIntentAsync([FromBody] CreateSubscriptionCheckoutIntentRequest request, CancellationToken ct = default)");
        billingSource.Should().Contain("var validation = await _createSubscriptionCheckoutIntentHandler");
        billingSource.Should().Contain(".ValidateAsync(businessId, request.PlanId, ct)");
        billingSource.Should().Contain("return ProblemFromResult(validation, \"Unable to create checkout intent.\");");
        billingSource.Should().Contain("var checkoutBaseUrl = _configuration[\"Billing:CheckoutBaseUrl\"];");
        billingSource.Should().Contain("if (string.IsNullOrWhiteSpace(checkoutBaseUrl) || !Uri.TryCreate(checkoutBaseUrl, UriKind.Absolute, out var baseUri))");
        billingSource.Should().Contain("return BadRequestProblem(\"Billing checkout endpoint is not configured.\");");
        billingSource.Should().Contain("var queryBuilder = new QueryBuilder");
        billingSource.Should().Contain("{ \"businessId\", businessId.ToString(\"D\") },");
        billingSource.Should().Contain("{ \"planId\", request.PlanId.ToString(\"D\") }");
        billingSource.Should().Contain("var checkoutUrl = new UriBuilder(baseUri)");
        billingSource.Should().Contain("Query = queryBuilder.ToQueryString().Value?.TrimStart('?')");
        billingSource.Should().Contain("CheckoutUrl = checkoutUrl,");
        billingSource.Should().Contain("ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15),");
        billingSource.Should().Contain("Provider = \"Stripe\"");
        billingSource.Should().Contain("private static BusinessSubscriptionStatusResponse MapStatus(BusinessSubscriptionStatusDto dto)");
        billingSource.Should().Contain("CancelAtPeriodEnd = dto.CancelAtPeriodEnd");
        billingSource.Should().Contain("private async Task<(bool Success, Guid BusinessId, IActionResult? ErrorResult)> TryGetCurrentBusinessIdAsync(bool requireOperationsAllowed, CancellationToken ct)");
        billingSource.Should().Contain("!BusinessControllerConventions.TryGetCurrentUserId(User, out var userId)");
        billingSource.Should().Contain("var accessState = await _getCurrentBusinessAccessStateHandler.HandleAsync(businessId, userId, ct).ConfigureAwait(false);");
        billingSource.Should().Contain("if (!accessState.IsBusinessClientAccessAllowed)");
        billingSource.Should().Contain("if (requireOperationsAllowed && !accessState.IsOperationsAllowed)");
        billingSource.Should().Contain("return (false, Guid.Empty, Forbid(accessState.BlockingReason));");

        loyaltySource.Should().Contain("public sealed class LoyaltyController : ApiControllerBase");
        loyaltySource.Should().Contain("private readonly PrepareScanSessionHandler _prepareScanSessionHandler;");
        loyaltySource.Should().Contain("private readonly GetMyLoyaltyTimelinePageHandler _getMyLoyaltyTimelinePageHandler;");
        loyaltySource.Should().Contain("private readonly CreateLoyaltyAccountHandler _createLoyaltyAccountHandler;");
        loyaltySource.Should().Contain("private readonly ILoyaltyPresentationService _presentationService;");
        loyaltySource.Should().Contain("_presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));");
        loyaltySource.Should().Contain("public async Task<IActionResult> PrepareScanSessionAsync(");
        loyaltySource.Should().Contain("return BadRequestProblem(\"Request body is required.\");");
        loyaltySource.Should().Contain("return BadRequestProblem(\"BusinessId is required.\");");
        loyaltySource.Should().Contain("var dto = new PrepareScanSessionDto");
        loyaltySource.Should().Contain("Mode = LoyaltyContractsMapper.ToDomain(request.Mode),");
        loyaltySource.Should().Contain("SelectedRewardTierIds = request.SelectedRewardTierIds?");
        loyaltySource.Should().Contain(".Where(x => x != Guid.Empty)");
        loyaltySource.Should().Contain(".Distinct()");
        loyaltySource.Should().Contain("DeviceId = request.DeviceId");
        loyaltySource.Should().Contain("var result = await _prepareScanSessionHandler.HandleAsync(dto, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("if (result.Value.Mode == Darwin.Domain.Enums.LoyaltyScanMode.Redemption &&");
        loyaltySource.Should().Contain(".EnrichSelectedRewardsAsync(request.BusinessId, result.Value.SelectedRewardTierIds, failIfMissing: true, ct)");
        loyaltySource.Should().Contain("SelectedRewards = selectedRewards");

        loyaltySource.Should().Contain("public async Task<IActionResult> GetMyBusinessesAsync(");
        loyaltySource.Should().Contain("var normalizedPage = page.GetValueOrDefault(1);");
        loyaltySource.Should().Contain("return BadRequestProblem(\"Page must be a positive integer.\");");
        loyaltySource.Should().Contain("var normalizedPageSize = pageSize.GetValueOrDefault(20);");
        loyaltySource.Should().Contain("return BadRequestProblem(\"PageSize must be between 1 and 200.\");");
        loyaltySource.Should().Contain("var request = new MyLoyaltyBusinessListRequestDto");
        loyaltySource.Should().Contain("IncludeInactiveBusinesses = includeInactiveBusinesses.GetValueOrDefault(false)");
        loyaltySource.Should().Contain("var (items, total) = await _getMyLoyaltyBusinessesHandler.HandleAsync(request, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("var safeItems = items ?? new List<MyLoyaltyBusinessListItemDto>();");
        loyaltySource.Should().Contain("Request = new PagedRequest { Page = normalizedPage, PageSize = normalizedPageSize, Search = null }");

        loyaltySource.Should().Contain("public async Task<IActionResult> GetMyPromotionsAsync([FromBody] MyPromotionsRequest? request, CancellationToken ct = default)");
        loyaltySource.Should().Contain("if (request.BusinessId.HasValue && request.BusinessId.Value == Guid.Empty)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"BusinessId must be a non-empty GUID when provided.\");");
        loyaltySource.Should().Contain("HandleAsync(new MyPromotionsDto");
        loyaltySource.Should().Contain("Policy = request.Policy is null");
        loyaltySource.Should().Contain("EnableDeduplication = request.Policy.EnableDeduplication,");
        loyaltySource.Should().Contain("SuppressionWindowMinutes = request.Policy.SuppressionWindowMinutes");
        loyaltySource.Should().Contain("return Ok(new MyPromotionsResponse");
        loyaltySource.Should().Contain("AppliedPolicy = new PromotionFeedPolicy");
        loyaltySource.Should().Contain("Diagnostics = new PromotionFeedDiagnostics");
        loyaltySource.Should().Contain("Items = result.Value.Items");
        loyaltySource.Should().Contain("EligibilityRules = x.EligibilityRules.Select(rule => new PromotionEligibilityRule");

        loyaltySource.Should().Contain("public async Task<IActionResult> TrackPromotionInteractionAsync(");
        loyaltySource.Should().Contain("if (request.BusinessId == Guid.Empty)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"BusinessId is required.\");");
        loyaltySource.Should().Contain("if (string.IsNullOrWhiteSpace(request.Title))");
        loyaltySource.Should().Contain("return BadRequestProblem(\"Title is required.\");");
        loyaltySource.Should().Contain("HandleAsync(new TrackPromotionInteractionDto");
        loyaltySource.Should().Contain("EventType = MapPromotionInteractionEventType(request.EventType),");
        loyaltySource.Should().Contain("return NoContent();");

        loyaltySource.Should().Contain("public async Task<IActionResult> GetMyLoyaltyTimelinePageAsync(");
        loyaltySource.Should().Contain("if (!request.BusinessId.HasValue || request.BusinessId.Value == Guid.Empty)");
        loyaltySource.Should().Contain("return BadRequestProblem(\"BusinessId is required and must be a non-empty GUID.\");");
        loyaltySource.Should().Contain("if ((request.BeforeAtUtc is null) != (request.BeforeId is null))");
        loyaltySource.Should().Contain("return BadRequestProblem(\"Invalid cursor. Both BeforeAtUtc and BeforeId must be provided together.\");");
        loyaltySource.Should().Contain("var dto = new GetMyLoyaltyTimelinePageDto");
        loyaltySource.Should().Contain("BeforeAtUtc = request.BeforeAtUtc,");
        loyaltySource.Should().Contain("BeforeId = request.BeforeId");
        loyaltySource.Should().Contain("var result = await _getMyLoyaltyTimelinePageHandler.HandleAsync(dto, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("Items = (result.Value.Items ?? Array.Empty<LoyaltyTimelineEntryDto>())");
        loyaltySource.Should().Contain("NextBeforeAtUtc = result.Value.NextBeforeAtUtc,");
        loyaltySource.Should().Contain("NextBeforeId = result.Value.NextBeforeId");

        loyaltySource.Should().Contain("public async Task<IActionResult> JoinLoyaltyAsync(");
        loyaltySource.Should().Contain("var result = await _createLoyaltyAccountHandler");
        loyaltySource.Should().Contain(".HandleAsync(businessId, request?.BusinessLocationId, ct)");
        loyaltySource.Should().Contain("return Ok(LoyaltyContractsMapper.ToContract(result.Value));");

        loyaltySource.Should().Contain("public async Task<IActionResult> GetNextRewardAsync([FromRoute] Guid businessId, CancellationToken ct = default)");
        loyaltySource.Should().Contain("var accountResult = await _getMyLoyaltyAccountForBusinessHandler.HandleAsync(businessId, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("var availableResult = await _getAvailableLoyaltyRewardsForBusinessHandler.HandleAsync(businessId, ct).ConfigureAwait(false);");
        loyaltySource.Should().Contain("var candidate = (availableResult.Value ?? Array.Empty<LoyaltyRewardSummaryDto>())");
        loyaltySource.Should().Contain(".Where(r => r.RequiredPoints > account.PointsBalance && r.IsActive && r.IsSelectable)");
        loyaltySource.Should().Contain(".OrderBy(r => r.RequiredPoints)");
        loyaltySource.Should().Contain(".FirstOrDefault();");
        loyaltySource.Should().Contain("return candidate is null");
        loyaltySource.Should().Contain("? NoContent()");
        loyaltySource.Should().Contain(": Ok(LoyaltyContractsMapper.ToContract(candidate));");

        loyaltySource.Should().Contain("private static Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType MapPromotionInteractionEventType(");
        loyaltySource.Should().Contain("PromotionInteractionEventType.Open => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Open,");
        loyaltySource.Should().Contain("PromotionInteractionEventType.Claim => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Claim,");
        loyaltySource.Should().Contain("_ => Darwin.Application.Loyalty.DTOs.PromotionInteractionEventType.Impression");
    }


    [Fact]
    public void WebApiMapperAndLoyaltyRegistrationLayer_Should_KeepContractTranslationAndServiceRegistrationWired()
    {
        var extensionsSource = ReadWebApiFile(Path.Combine("Extensions", "LoyaltyServiceCollectionExtensions.cs"));
        var businessMapperSource = ReadWebApiFile(Path.Combine("Mappers", "BusinessContractsMapper.cs"));
        var loyaltyMapperSource = ReadWebApiFile(Path.Combine("Mappers", "LoyaltyContractsMapper.cs"));

        extensionsSource.Should().Contain("public static class LoyaltyServiceCollectionExtensions");
        extensionsSource.Should().Contain("public static IServiceCollection AddLoyaltyPresentationServices(this IServiceCollection services)");
        extensionsSource.Should().Contain("services.AddScoped<ILoyaltyPresentationService, LoyaltyPresentationService>();");
        extensionsSource.Should().Contain("return services;");

        businessMapperSource.Should().Contain("public static class BusinessContractsMapper");
        businessMapperSource.Should().Contain("public static BusinessSummary ToContract(BusinessDiscoveryListItemDto dto)");
        businessMapperSource.Should().Contain("ArgumentNullException.ThrowIfNull(dto);");
        businessMapperSource.Should().Contain("Name = dto.Name ?? string.Empty,");
        businessMapperSource.Should().Contain("Category = dto.Category.ToString(),");
        businessMapperSource.Should().Contain("Location = dto.Coordinate is null ? null : new GeoCoordinateModel");
        businessMapperSource.Should().Contain("DistanceMeters = dto.DistanceKm.HasValue");
        businessMapperSource.Should().Contain("? (int?)Math.Round(dto.DistanceKm.Value * 1000.0, MidpointRounding.AwayFromZero)");
        businessMapperSource.Should().Contain("Rating = dto.RatingAverage.HasValue ? (double?)dto.RatingAverage.Value : null,");
        businessMapperSource.Should().Contain("RatingCount = dto.RatingCount");
        businessMapperSource.Should().Contain("public static BusinessDetail ToContract(BusinessPublicDetailDto dto)");
        businessMapperSource.Should().Contain("var primaryLocation = dto.Locations?.FirstOrDefault(l => l.IsPrimary) ?? dto.Locations?.FirstOrDefault();");
        businessMapperSource.Should().Contain("Description = null,");
        businessMapperSource.Should().Contain("ImageUrls = BuildImageUrls(dto.PrimaryImageUrl, dto.GalleryImageUrls),");
        businessMapperSource.Should().Contain("City = primaryLocation?.City,");
        businessMapperSource.Should().Contain("Coordinate = primaryLocation?.Coordinate is null ? null : new GeoCoordinateModel");
        businessMapperSource.Should().Contain("PhoneE164 = dto.ContactPhoneE164,");
        businessMapperSource.Should().Contain("DefaultCurrency = string.IsNullOrWhiteSpace(dto.DefaultCurrency) ? SiteSettingDto.DefaultCurrencyDefault : dto.DefaultCurrency,");
        businessMapperSource.Should().Contain("DefaultCulture = string.IsNullOrWhiteSpace(dto.DefaultCulture) ? SiteSettingDto.DefaultCultureDefault : dto.DefaultCulture,");
        businessMapperSource.Should().Contain("Locations = (dto.Locations ?? new List<BusinessPublicLocationDto>())");
        businessMapperSource.Should().Contain("LoyaltyProgramPublic = dto.LoyaltyProgram is null ? null : ToContract(dto.LoyaltyProgram)");
        businessMapperSource.Should().Contain("public static BusinessDetailWithMyAccount ToContract(BusinessPublicDetailWithMyAccountDto dto)");
        businessMapperSource.Should().Contain("ArgumentNullException.ThrowIfNull(dto.Business);");
        businessMapperSource.Should().Contain("MyAccount = dto.MyAccount is null ? null : ToContract(dto.MyAccount)");
        businessMapperSource.Should().Contain("public static BusinessLocation ToContract(BusinessPublicLocationDto dto)");
        businessMapperSource.Should().Contain("BusinessLocationId = dto.Id,");
        businessMapperSource.Should().Contain("OpeningHoursJson = dto.OpeningHoursJson");
        businessMapperSource.Should().Contain("public static LoyaltyProgramPublic ToContract(LoyaltyProgramPublicDto dto)");
        businessMapperSource.Should().Contain("RewardType = t.RewardType.ToString(),");
        businessMapperSource.Should().Contain("AllowSelfRedemption = t.AllowSelfRedemption");
        businessMapperSource.Should().Contain("public static LoyaltyAccountSummary ToContract(LoyaltyAccountSummaryDto dto)");
        businessMapperSource.Should().Contain("LoyaltyAccountId = dto.Id,");
        businessMapperSource.Should().Contain("Status = dto.Status.ToString(),");
        businessMapperSource.Should().Contain("NextRewardTitle = null");
        businessMapperSource.Should().Contain("private static IReadOnlyList<string> BuildImageUrls(string? primaryImageUrl, List<string>? galleryImageUrls)");
        businessMapperSource.Should().Contain("if (!string.IsNullOrWhiteSpace(primaryImageUrl))");
        businessMapperSource.Should().Contain("list.Add(primaryImageUrl.Trim());");
        businessMapperSource.Should().Contain("if (galleryImageUrls is { Count: > 0 })");
        businessMapperSource.Should().Contain("if (!string.IsNullOrWhiteSpace(url))");
        businessMapperSource.Should().Contain("list.Add(url.Trim());");

        loyaltyMapperSource.Should().Contain("public static class LoyaltyContractsMapper");
        loyaltyMapperSource.Should().Contain("public static DomainLoyaltyScanMode ToDomain(ContractLoyaltyScanMode mode) =>");
        loyaltyMapperSource.Should().Contain("ContractLoyaltyScanMode.Accrual => DomainLoyaltyScanMode.Accrual,");
        loyaltyMapperSource.Should().Contain("ContractLoyaltyScanMode.Redemption => DomainLoyaltyScanMode.Redemption,");
        loyaltyMapperSource.Should().Contain("_ => DomainLoyaltyScanMode.Accrual");
        loyaltyMapperSource.Should().Contain("public static ContractLoyaltyScanMode ToContract(DomainLoyaltyScanMode mode) =>");
        loyaltyMapperSource.Should().Contain("DomainLoyaltyScanMode.Accrual => ContractLoyaltyScanMode.Accrual,");
        loyaltyMapperSource.Should().Contain("DomainLoyaltyScanMode.Redemption => ContractLoyaltyScanMode.Redemption,");
        loyaltyMapperSource.Should().Contain("_ => ContractLoyaltyScanMode.Accrual");
        loyaltyMapperSource.Should().Contain("public static LoyaltyAccountSummary ToContract(LoyaltyAccountSummaryDto dto)");
        loyaltyMapperSource.Should().Contain("NextRewardTitle = dto.NextRewardTitle,");
        loyaltyMapperSource.Should().Contain("NextRewardRequiredPoints = dto.NextRewardRequiredPoints,");
        loyaltyMapperSource.Should().Contain("PointsToNextReward = dto.PointsToNextReward,");
        loyaltyMapperSource.Should().Contain("NextRewardProgressPercent = dto.NextRewardProgressPercent");
        loyaltyMapperSource.Should().Contain("public static LoyaltyRewardSummary ToContract(LoyaltyRewardSummaryDto dto)");
        loyaltyMapperSource.Should().Contain("LoyaltyRewardTierId = dto.LoyaltyRewardTierId,");
        loyaltyMapperSource.Should().Contain("IsSelectable = dto.IsSelectable");
        loyaltyMapperSource.Should().Contain("public static BusinessLoyaltyAccountSummary ToContractBusinessAccountSummary(ScanSessionBusinessViewDto dto)");
        loyaltyMapperSource.Should().Contain("PointsBalance = dto.CurrentPointsBalance,");
        loyaltyMapperSource.Should().Contain("CustomerDisplayName = dto.CustomerDisplayName");
        loyaltyMapperSource.Should().Contain("public static PointsTransaction ToContract(LoyaltyPointsTransactionDto dto)");
        loyaltyMapperSource.Should().Contain("OccurredAtUtc = dto.CreatedAtUtc,");
        loyaltyMapperSource.Should().Contain("Type = dto.Type.ToString(),");
        loyaltyMapperSource.Should().Contain("Delta = dto.PointsDelta,");
        loyaltyMapperSource.Should().Contain("public static MyLoyaltyBusinessSummary ToContract(MyLoyaltyBusinessListItemDto dto)");
        loyaltyMapperSource.Should().Contain("Status = dto.AccountStatus.ToString(),");
        loyaltyMapperSource.Should().Contain("public static MyLoyaltyOverviewResponse ToContract(MyLoyaltyOverviewDto dto)");
        loyaltyMapperSource.Should().Contain("Accounts = dto.Accounts.Select(ToContract).ToList()");
        loyaltyMapperSource.Should().Contain("public static MyLoyaltyBusinessDashboard ToContract(MyLoyaltyBusinessDashboardDto dto)");
        loyaltyMapperSource.Should().Contain("NextReward = dto.NextReward is null ? null : ToContract(dto.NextReward),");
        loyaltyMapperSource.Should().Contain("RecentTransactions = dto.RecentTransactions.Select(ToContract).ToList(),");
        loyaltyMapperSource.Should().Contain("PointsExpiringSoon = dto.PointsExpiringSoon,");
        loyaltyMapperSource.Should().Contain("NextPointsExpiryAtUtc = dto.NextPointsExpiryAtUtc");
        loyaltyMapperSource.Should().Contain("public static LoyaltyTimelineEntry ToContract(LoyaltyTimelineEntryDto dto)");
        loyaltyMapperSource.Should().Contain("Darwin.Application.Loyalty.DTOs.LoyaltyTimelineEntryKind.PointsTransaction");
        loyaltyMapperSource.Should().Contain("Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.PointsTransaction,");
        loyaltyMapperSource.Should().Contain("Darwin.Application.Loyalty.DTOs.LoyaltyTimelineEntryKind.RewardRedemption");
        loyaltyMapperSource.Should().Contain("Darwin.Contracts.Loyalty.LoyaltyTimelineEntryKind.RewardRedemption,");
        loyaltyMapperSource.Should().Contain("public static GeoCoordinateDto ToApplication(GeoCoordinateModel model)");
        loyaltyMapperSource.Should().Contain("Latitude = model.Latitude,");
        loyaltyMapperSource.Should().Contain("Longitude = model.Longitude,");
        loyaltyMapperSource.Should().Contain("AltitudeMeters = model.AltitudeMeters");
    }


    [Fact]
    public void PublicBusinessLoyaltyBillingShippingAndMetaContracts_Should_KeepCanonicalShapeAndDefaults()
    {
        var businessDetailSource = ReadContractsFile(Path.Combine("Businesses", "BusinessDetail.cs"));
        var businessListRequestSource = ReadContractsFile(Path.Combine("Businesses", "BusinessListRequest.cs"));
        var businessOnboardingRequestSource = ReadContractsFile(Path.Combine("Businesses", "BusinessOnboardingRequest.cs"));
        var loyaltyPrepareSource = ReadContractsFile(Path.Combine("Loyalty", "PrepareScanSessionRequest.cs"));
        var loyaltyPromotionsSource = ReadContractsFile(Path.Combine("Loyalty", "MyPromotionsResponse.cs"));
        var billingStatusSource = ReadContractsFile(Path.Combine("Billing", "BusinessSubscriptionStatusResponse.cs"));
        var shippingContractsSource = ReadContractsFile(Path.Combine("Shipping", "PublicShippingContracts.cs"));
        var bootstrapSource = ReadContractsFile(Path.Combine("Meta", "AppBootstrapResponse.cs"));

        businessDetailSource.Should().Contain("public sealed class BusinessDetail");
        businessDetailSource.Should().Contain("public Guid Id { get; set; }");
        businessDetailSource.Should().Contain("public string Name { get; set; } = string.Empty;");
        businessDetailSource.Should().Contain("public string Category { get; set; } = string.Empty;");
        businessDetailSource.Should().Contain("public string? ShortDescription { get; set; }");
        businessDetailSource.Should().Contain("public string? Description { get; set; }");
        businessDetailSource.Should().Contain("public string? PrimaryImageUrl { get; set; }");
        businessDetailSource.Should().Contain("public IReadOnlyList<string>? GalleryImageUrls { get; set; }");
        businessDetailSource.Should().Contain("public IReadOnlyList<string>? ImageUrls { get; set; }");
        businessDetailSource.Should().Contain("public GeoCoordinateModel? Coordinate { get; set; }");
        businessDetailSource.Should().Contain("public object? OpeningHours { get; set; }");
        businessDetailSource.Should().Contain("public string DefaultCurrency { get; set; } = ContractDefaults.DefaultCurrency;");
        businessDetailSource.Should().Contain("public string DefaultCulture { get; set; } = ContractDefaults.DefaultLocale;");
        businessDetailSource.Should().Contain("public List<BusinessLocation> Locations { get; set; } = new();");
        businessDetailSource.Should().Contain("public object? LoyaltyProgram { get; set; }");
        businessDetailSource.Should().Contain("public LoyaltyProgramPublic? LoyaltyProgramPublic { get; set; }");

        businessListRequestSource.Should().Contain("public sealed class BusinessListRequest : PagedRequest");
        businessListRequestSource.Should().Contain("public string? Query { get; init; }");
        businessListRequestSource.Should().Contain("public string? CountryCode { get; init; }");
        businessListRequestSource.Should().Contain("public string? AddressQuery { get; init; }");
        businessListRequestSource.Should().Contain("public string? City { get; init; }");
        businessListRequestSource.Should().Contain("public string? CategoryKindKey { get; init; }");
        businessListRequestSource.Should().Contain("public double? MinRating { get; init; }");
        businessListRequestSource.Should().Contain("public bool? HasActiveLoyaltyProgram { get; init; }");
        businessListRequestSource.Should().Contain("public GeoCoordinateModel? Near { get; init; }");
        businessListRequestSource.Should().Contain("public int? RadiusMeters { get; init; }");

        businessOnboardingRequestSource.Should().Contain("public sealed class BusinessOnboardingRequest");
        businessOnboardingRequestSource.Should().Contain("public string Name { get; init; } = string.Empty;");
        businessOnboardingRequestSource.Should().Contain("public string? LegalName { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? TaxId { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? ShortDescription { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? WebsiteUrl { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? ContactEmail { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? ContactPhoneE164 { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? CategoryKindKey { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? DefaultCurrency { get; init; }");
        businessOnboardingRequestSource.Should().Contain("public string? DefaultCulture { get; init; }");

        loyaltyPrepareSource.Should().Contain("public sealed class PrepareScanSessionRequest");
        loyaltyPrepareSource.Should().Contain("public Guid BusinessId { get; init; }");
        loyaltyPrepareSource.Should().Contain("public Guid? BusinessLocationId { get; init; }");
        loyaltyPrepareSource.Should().Contain("public LoyaltyScanMode Mode { get; init; } = LoyaltyScanMode.Accrual;");
        loyaltyPrepareSource.Should().Contain("public IReadOnlyList<Guid> SelectedRewardTierIds { get; init; }");
        loyaltyPrepareSource.Should().Contain("= Array.Empty<Guid>();");
        loyaltyPrepareSource.Should().Contain("public string? DeviceId { get; init; }");

        loyaltyPromotionsSource.Should().Contain("public sealed class MyPromotionsResponse");
        loyaltyPromotionsSource.Should().Contain("public List<PromotionFeedItem> Items { get; init; } = new();");
        loyaltyPromotionsSource.Should().Contain("public PromotionFeedPolicy AppliedPolicy { get; init; } = new();");
        loyaltyPromotionsSource.Should().Contain("public PromotionFeedDiagnostics Diagnostics { get; init; } = new();");

        billingStatusSource.Should().Contain("public sealed class BusinessSubscriptionStatusResponse");
        billingStatusSource.Should().Contain("public bool HasSubscription { get; set; }");
        billingStatusSource.Should().Contain("public Guid SubscriptionId { get; set; }");
        billingStatusSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");
        billingStatusSource.Should().Contain("public string Status { get; set; } = string.Empty;");
        billingStatusSource.Should().Contain("public string Provider { get; set; } = string.Empty;");
        billingStatusSource.Should().Contain("public string PlanCode { get; set; } = string.Empty;");
        billingStatusSource.Should().Contain("public string PlanName { get; set; } = string.Empty;");
        billingStatusSource.Should().Contain("public long UnitPriceMinor { get; set; }");
        billingStatusSource.Should().Contain("public string Currency { get; set; } = ContractDefaults.DefaultCurrency;");
        billingStatusSource.Should().Contain("public bool CancelAtPeriodEnd { get; set; }");

        shippingContractsSource.Should().Contain("public sealed class PublicShippingRateRequest");
        shippingContractsSource.Should().Contain("public string Country { get; set; } = ContractDefaults.DefaultCountryCode;");
        shippingContractsSource.Should().Contain("public long SubtotalNetMinor { get; set; }");
        shippingContractsSource.Should().Contain("public int ShipmentMass { get; set; }");
        shippingContractsSource.Should().Contain("public string? Currency { get; set; }");
        shippingContractsSource.Should().Contain("public sealed class PublicShippingOption");
        shippingContractsSource.Should().Contain("public Guid MethodId { get; set; }");
        shippingContractsSource.Should().Contain("public string Name { get; set; } = string.Empty;");
        shippingContractsSource.Should().Contain("public long PriceMinor { get; set; }");
        shippingContractsSource.Should().Contain("public string Currency { get; set; } = ContractDefaults.DefaultCurrency;");
        shippingContractsSource.Should().Contain("public string Carrier { get; set; } = string.Empty;");
        shippingContractsSource.Should().Contain("public string Service { get; set; } = string.Empty;");

        bootstrapSource.Should().Contain("public sealed class AppBootstrapResponse");
        bootstrapSource.Should().Contain("public string JwtAudience { get; init; } = \"Darwin.PublicApi\";");
        bootstrapSource.Should().Contain("public int QrTokenRefreshSeconds { get; init; } = 60;");
        bootstrapSource.Should().Contain("public int MaxOutboxItems { get; init; } = 100;");
    }


    [Fact]
    public void BusinessInvitationAndBusinessLoyaltyContracts_Should_KeepBusinessFacingShapeDefaultsAndConcurrencyFields()
    {
        var invitationContractsSource = ReadContractsFile(Path.Combine("Businesses", "BusinessInvitationOnboardingContracts.cs"));
        var rewardConfigurationSource = ReadContractsFile(Path.Combine("Loyalty", "BusinessRewardConfigurationResponse.cs"));
        var processScanSource = ReadContractsFile(Path.Combine("Loyalty", "ProcessScanSessionForBusinessResponse.cs"));
        var campaignsResponseSource = ReadContractsFile(Path.Combine("Loyalty", "GetBusinessCampaignsResponse.cs"));
        var createCampaignSource = ReadContractsFile(Path.Combine("Loyalty", "CreateBusinessCampaignRequest.cs"));
        var updateCampaignSource = ReadContractsFile(Path.Combine("Loyalty", "UpdateBusinessCampaignRequest.cs"));

        invitationContractsSource.Should().Contain("public sealed class BusinessInvitationPreviewResponse");
        invitationContractsSource.Should().Contain("public Guid InvitationId { get; init; }");
        invitationContractsSource.Should().Contain("public Guid BusinessId { get; init; }");
        invitationContractsSource.Should().Contain("public string BusinessName { get; init; } = string.Empty;");
        invitationContractsSource.Should().Contain("public string Email { get; init; } = string.Empty;");
        invitationContractsSource.Should().Contain("public string Role { get; init; } = string.Empty;");
        invitationContractsSource.Should().Contain("public string Status { get; init; } = string.Empty;");
        invitationContractsSource.Should().Contain("public DateTime ExpiresAtUtc { get; init; }");
        invitationContractsSource.Should().Contain("public bool HasExistingUser { get; init; }");
        invitationContractsSource.Should().Contain("public sealed class AcceptBusinessInvitationRequest");
        invitationContractsSource.Should().Contain("public string Token { get; init; } = string.Empty;");
        invitationContractsSource.Should().Contain("public string? DeviceId { get; init; }");
        invitationContractsSource.Should().Contain("public string? FirstName { get; init; }");
        invitationContractsSource.Should().Contain("public string? LastName { get; init; }");
        invitationContractsSource.Should().Contain("public string? Password { get; init; }");

        rewardConfigurationSource.Should().Contain("public sealed class BusinessRewardConfigurationResponse");
        rewardConfigurationSource.Should().Contain("public Guid LoyaltyProgramId { get; init; }");
        rewardConfigurationSource.Should().Contain("public string ProgramName { get; init; } = string.Empty;");
        rewardConfigurationSource.Should().Contain("public bool IsProgramActive { get; init; }");
        rewardConfigurationSource.Should().Contain("public IReadOnlyList<BusinessRewardTierConfigItem> RewardTiers { get; init; } = Array.Empty<BusinessRewardTierConfigItem>();");

        processScanSource.Should().Contain("public sealed class ProcessScanSessionForBusinessResponse");
        processScanSource.Should().Contain("public LoyaltyScanMode Mode { get; init; }");
        processScanSource.Should().Contain("public Guid BusinessId { get; init; }");
        processScanSource.Should().Contain("public Guid? BusinessLocationId { get; init; }");
        processScanSource.Should().Contain("public BusinessLoyaltyAccountSummary AccountSummary { get; set; }");
        processScanSource.Should().Contain("= new BusinessLoyaltyAccountSummary();");
        processScanSource.Should().Contain("public string? CustomerDisplayName { get; init; }");
        processScanSource.Should().Contain("public IReadOnlyList<LoyaltyRewardSummary> SelectedRewards { get; init; }");
        processScanSource.Should().Contain("= Array.Empty<LoyaltyRewardSummary>();");
        processScanSource.Should().Contain("public LoyaltyScanAllowedActions AllowedActions { get; init; }");

        campaignsResponseSource.Should().Contain("public sealed class GetBusinessCampaignsResponse");
        campaignsResponseSource.Should().Contain("public List<BusinessCampaignItem> Items { get; init; } = new();");
        campaignsResponseSource.Should().Contain("public int Total { get; init; }");

        createCampaignSource.Should().Contain("public sealed class CreateBusinessCampaignRequest");
        createCampaignSource.Should().Contain("public string Name { get; init; } = string.Empty;");
        createCampaignSource.Should().Contain("public string Title { get; init; } = string.Empty;");
        createCampaignSource.Should().Contain("public string? Subtitle { get; init; }");
        createCampaignSource.Should().Contain("public string? Body { get; init; }");
        createCampaignSource.Should().Contain("public string? MediaUrl { get; init; }");
        createCampaignSource.Should().Contain("public string? LandingUrl { get; init; }");
        createCampaignSource.Should().Contain("public short Channels { get; init; } = 1;");
        createCampaignSource.Should().Contain("public DateTime? StartsAtUtc { get; init; }");
        createCampaignSource.Should().Contain("public DateTime? EndsAtUtc { get; init; }");
        createCampaignSource.Should().Contain("public string TargetingJson { get; init; } = \"{}\";");
        createCampaignSource.Should().Contain("public List<PromotionEligibilityRule> EligibilityRules { get; init; } = new();");
        createCampaignSource.Should().Contain("public string PayloadJson { get; init; } = \"{}\";");

        updateCampaignSource.Should().Contain("public sealed class UpdateBusinessCampaignRequest");
        updateCampaignSource.Should().Contain("public Guid Id { get; init; }");
        updateCampaignSource.Should().Contain("public string Name { get; init; } = string.Empty;");
        updateCampaignSource.Should().Contain("public string Title { get; init; } = string.Empty;");
        updateCampaignSource.Should().Contain("public short Channels { get; init; } = 1;");
        updateCampaignSource.Should().Contain("public string TargetingJson { get; init; } = \"{}\";");
        updateCampaignSource.Should().Contain("public List<PromotionEligibilityRule> EligibilityRules { get; init; } = new();");
        updateCampaignSource.Should().Contain("public string PayloadJson { get; init; } = \"{}\";");
        updateCampaignSource.Should().Contain("public byte[] RowVersion { get; init; } = Array.Empty<byte>();");
    }


    [Fact]
    public void BusinessDiscoveryEngagementAndMemberLoyaltyContracts_Should_KeepCanonicalResponseAndPagedShapeFloors()
    {
        var accessStateSource = ReadContractsFile(Path.Combine("Businesses", "BusinessAccessStateResponse.cs"));
        var detailWithMyAccountSource = ReadContractsFile(Path.Combine("Businesses", "BusinessDetailWithMyAccount.cs"));
        var engagementSummarySource = ReadContractsFile(Path.Combine("Businesses", "BusinessEngagementSummaryResponse.cs"));
        var businessSummarySource = ReadContractsFile(Path.Combine("Businesses", "BusinessSummary.cs"));
        var toggleReactionSource = ReadContractsFile(Path.Combine("Businesses", "ToggleBusinessReactionResponse.cs"));
        var upsertReviewSource = ReadContractsFile(Path.Combine("Businesses", "UpsertBusinessReviewRequest.cs"));
        var loyaltyBusinessesResponseSource = ReadContractsFile(Path.Combine("Loyalty", "MyLoyaltyBusinessesResponse.cs"));
        var loyaltyTimelineResponseSource = ReadContractsFile(Path.Combine("Loyalty", "GetMyLoyaltyTimelinePageResponse.cs"));

        accessStateSource.Should().Contain("public sealed class BusinessAccessStateResponse");
        accessStateSource.Should().Contain("public Guid BusinessId { get; set; }");
        accessStateSource.Should().Contain("public string BusinessName { get; set; } = string.Empty;");
        accessStateSource.Should().Contain("public string OperationalStatus { get; set; } = string.Empty;");
        accessStateSource.Should().Contain("public bool IsActive { get; set; }");
        accessStateSource.Should().Contain("public DateTime? ApprovedAtUtc { get; set; }");
        accessStateSource.Should().Contain("public DateTime? SuspendedAtUtc { get; set; }");
        accessStateSource.Should().Contain("public string? SuspensionReason { get; set; }");
        accessStateSource.Should().Contain("public bool HasActiveOwner { get; set; }");
        accessStateSource.Should().Contain("public bool HasPrimaryLocation { get; set; }");
        accessStateSource.Should().Contain("public bool HasContactEmail { get; set; }");
        accessStateSource.Should().Contain("public bool HasLegalName { get; set; }");
        accessStateSource.Should().Contain("public bool IsApprovalPending { get; set; }");
        accessStateSource.Should().Contain("public bool IsSuspended { get; set; }");
        accessStateSource.Should().Contain("public bool IsOperationsAllowed { get; set; }");
        accessStateSource.Should().Contain("public bool IsSetupComplete { get; set; }");
        accessStateSource.Should().Contain("public bool HasActivationBlockingIssues { get; set; }");
        accessStateSource.Should().Contain("public int SetupIncompleteItemCount { get; set; }");
        accessStateSource.Should().Contain("public string? PrimaryBlockingCode { get; set; }");
        accessStateSource.Should().Contain("public string? BlockingReason { get; set; }");

        detailWithMyAccountSource.Should().Contain("public sealed record BusinessDetailWithMyAccount");
        detailWithMyAccountSource.Should().Contain("public required BusinessDetail Business { get; init; }");
        detailWithMyAccountSource.Should().Contain("public required bool HasAccount { get; init; }");
        detailWithMyAccountSource.Should().Contain("public LoyaltyAccountSummary? MyAccount { get; init; }");

        engagementSummarySource.Should().Contain("public sealed class BusinessEngagementSummaryResponse");
        engagementSummarySource.Should().Contain("public Guid BusinessId { get; init; }");
        engagementSummarySource.Should().Contain("public int LikeCount { get; init; }");
        engagementSummarySource.Should().Contain("public int FavoriteCount { get; init; }");
        engagementSummarySource.Should().Contain("public int RatingCount { get; init; }");
        engagementSummarySource.Should().Contain("public decimal? RatingAverage { get; init; }");
        engagementSummarySource.Should().Contain("public bool IsLikedByMe { get; init; }");
        engagementSummarySource.Should().Contain("public bool IsFavoritedByMe { get; init; }");
        engagementSummarySource.Should().Contain("public BusinessReviewItem? MyReview { get; init; }");
        engagementSummarySource.Should().Contain("public List<BusinessReviewItem> RecentReviews { get; init; } = new();");

        businessSummarySource.Should().Contain("public sealed class BusinessSummary");
        businessSummarySource.Should().Contain("public Guid Id { get; init; }");
        businessSummarySource.Should().Contain("public string Name { get; init; } = default!;");
        businessSummarySource.Should().Contain("public string? ShortDescription { get; init; }");
        businessSummarySource.Should().Contain("public string? LogoUrl { get; init; }");
        businessSummarySource.Should().Contain("public string Category { get; init; } = \"Unknown\";");
        businessSummarySource.Should().Contain("public double? Rating { get; init; }");
        businessSummarySource.Should().Contain("public int? RatingCount { get; init; }");
        businessSummarySource.Should().Contain("public GeoCoordinateModel? Location { get; init; }");
        businessSummarySource.Should().Contain("public string? City { get; init; }");
        businessSummarySource.Should().Contain("public bool? IsOpenNow { get; init; }");
        businessSummarySource.Should().Contain("public bool IsActive { get; init; }");
        businessSummarySource.Should().Contain("public int? DistanceMeters { get; set; }");

        toggleReactionSource.Should().Contain("public sealed class ToggleBusinessReactionResponse");
        toggleReactionSource.Should().Contain("public bool IsActive { get; init; }");
        toggleReactionSource.Should().Contain("public int TotalCount { get; init; }");

        upsertReviewSource.Should().Contain("public sealed class UpsertBusinessReviewRequest");
        upsertReviewSource.Should().Contain("public byte Rating { get; init; }");
        upsertReviewSource.Should().Contain("public string? Comment { get; init; }");

        loyaltyBusinessesResponseSource.Should().Contain("public sealed class MyLoyaltyBusinessesResponse : PagedResponse<MyLoyaltyBusinessSummary>");

        loyaltyTimelineResponseSource.Should().Contain("public sealed class GetMyLoyaltyTimelinePageResponse");
        loyaltyTimelineResponseSource.Should().Contain("public IReadOnlyList<LoyaltyTimelineEntry> Items { get; init; } = Array.Empty<LoyaltyTimelineEntry>();");
        loyaltyTimelineResponseSource.Should().Contain("public DateTime? NextBeforeAtUtc { get; init; }");
        loyaltyTimelineResponseSource.Should().Contain("public Guid? NextBeforeId { get; init; }");
    }


    [Fact]
    public void BillingAndBusinessLoyaltyMutationContracts_Should_KeepCheckoutConcurrencyAndScanConfirmFloors()
    {
        var checkoutIntentRequestSource = ReadContractsFile(Path.Combine("Billing", "CreateSubscriptionCheckoutIntentRequest.cs"));
        var checkoutIntentResponseSource = ReadContractsFile(Path.Combine("Billing", "CreateSubscriptionCheckoutIntentResponse.cs"));
        var billingPlansResponseSource = ReadContractsFile(Path.Combine("Billing", "GetBillingPlansResponse.cs"));
        var cancelAtPeriodEndRequestSource = ReadContractsFile(Path.Combine("Billing", "SetCancelAtPeriodEndRequest.cs"));
        var cancelAtPeriodEndResponseSource = ReadContractsFile(Path.Combine("Billing", "SetCancelAtPeriodEndResponse.cs"));
        var confirmAccrualRequestSource = ReadContractsFile(Path.Combine("Loyalty", "ConfirmAccrualRequest.cs"));
        var confirmAccrualResponseSource = ReadContractsFile(Path.Combine("Loyalty", "ConfirmAccrualResponse.cs"));
        var confirmRedemptionRequestSource = ReadContractsFile(Path.Combine("Loyalty", "ConfirmRedemptionRequest.cs"));
        var confirmRedemptionResponseSource = ReadContractsFile(Path.Combine("Loyalty", "ConfirmRedemptionResponse.cs"));

        checkoutIntentRequestSource.Should().Contain("public sealed class CreateSubscriptionCheckoutIntentRequest");
        checkoutIntentRequestSource.Should().Contain("public Guid PlanId { get; set; }");

        checkoutIntentResponseSource.Should().Contain("public sealed class CreateSubscriptionCheckoutIntentResponse");
        checkoutIntentResponseSource.Should().Contain("public string CheckoutUrl { get; set; } = string.Empty;");
        checkoutIntentResponseSource.Should().Contain("public DateTime ExpiresAtUtc { get; set; }");
        checkoutIntentResponseSource.Should().Contain("public string Provider { get; set; } = \"Stripe\";");

        billingPlansResponseSource.Should().Contain("public sealed class GetBillingPlansResponse");
        billingPlansResponseSource.Should().Contain("public IReadOnlyList<BillingPlanSummary> Items { get; set; } = new List<BillingPlanSummary>();");

        cancelAtPeriodEndRequestSource.Should().Contain("public sealed class SetCancelAtPeriodEndRequest");
        cancelAtPeriodEndRequestSource.Should().Contain("public Guid SubscriptionId { get; set; }");
        cancelAtPeriodEndRequestSource.Should().Contain("public bool CancelAtPeriodEnd { get; set; }");
        cancelAtPeriodEndRequestSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");

        cancelAtPeriodEndResponseSource.Should().Contain("public sealed class SetCancelAtPeriodEndResponse");
        cancelAtPeriodEndResponseSource.Should().Contain("public Guid SubscriptionId { get; set; }");
        cancelAtPeriodEndResponseSource.Should().Contain("public bool CancelAtPeriodEnd { get; set; }");
        cancelAtPeriodEndResponseSource.Should().Contain("public byte[] RowVersion { get; set; } = Array.Empty<byte>();");

        confirmAccrualRequestSource.Should().Contain("public sealed class ConfirmAccrualRequest");
        confirmAccrualRequestSource.Should().Contain("public string ScanSessionToken { get; set; } = string.Empty;");
        confirmAccrualRequestSource.Should().Contain("public int Points { get; init; } = 1;");
        confirmAccrualRequestSource.Should().Contain("public string? Note { get; init; }");

        confirmAccrualResponseSource.Should().Contain("public sealed class ConfirmAccrualResponse");
        confirmAccrualResponseSource.Should().Contain("public bool Success { get; init; }");
        confirmAccrualResponseSource.Should().Contain("public int? NewBalance { get; init; }");
        confirmAccrualResponseSource.Should().Contain("public LoyaltyAccountSummary? UpdatedAccount { get; init; }");
        confirmAccrualResponseSource.Should().Contain("public string? ErrorCode { get; init; }");
        confirmAccrualResponseSource.Should().Contain("public string? ErrorMessage { get; init; }");

        confirmRedemptionRequestSource.Should().Contain("public sealed class ConfirmRedemptionRequest");
        confirmRedemptionRequestSource.Should().Contain("public string ScanSessionToken { get; set; } = string.Empty;");

        confirmRedemptionResponseSource.Should().Contain("public sealed class ConfirmRedemptionResponse");
        confirmRedemptionResponseSource.Should().Contain("public bool Success { get; init; }");
        confirmRedemptionResponseSource.Should().Contain("public int? NewBalance { get; init; }");
        confirmRedemptionResponseSource.Should().Contain("public LoyaltyAccountSummary? UpdatedAccount { get; init; }");
        confirmRedemptionResponseSource.Should().Contain("public string? ErrorCode { get; init; }");
        confirmRedemptionResponseSource.Should().Contain("public string? ErrorMessage { get; init; }");
    }


    [Fact]
    public void LoyaltyEnumAndValueObjectContracts_Should_KeepStableSharedTokensAndTimelineShape()
    {
        var scanActionsSource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyScanAllowedActions.cs"));
        var scanModeSource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyScanMode.cs"));
        var timelineEntrySource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyTimelineEntry.cs"));
        var timelineKindSource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyTimelineEntryKind.cs"));
        var audienceKindSource = ReadContractsFile(Path.Combine("Loyalty", "PromotionAudienceKind.cs"));
        var campaignStateSource = ReadContractsFile(Path.Combine("Loyalty", "PromotionCampaignState.cs"));
        var interactionEventTypeSource = ReadContractsFile(Path.Combine("Loyalty", "PromotionInteractionEventType.cs"));
        var eligibilityRuleSource = ReadContractsFile(Path.Combine("Loyalty", "PromotionEligibilityRule.cs"));

        scanActionsSource.Should().Contain("[Flags]");
        scanActionsSource.Should().Contain("public enum LoyaltyScanAllowedActions");
        scanActionsSource.Should().Contain("None = 0,");
        scanActionsSource.Should().Contain("CanConfirmAccrual = 1 << 0,");
        scanActionsSource.Should().Contain("CanConfirmRedemption = 1 << 1");

        scanModeSource.Should().Contain("public enum LoyaltyScanMode");
        scanModeSource.Should().Contain("Accrual = 0,");
        scanModeSource.Should().Contain("Redemption = 1");

        timelineEntrySource.Should().Contain("public sealed class LoyaltyTimelineEntry");
        timelineEntrySource.Should().Contain("public Guid Id { get; init; }");
        timelineEntrySource.Should().Contain("public LoyaltyTimelineEntryKind Kind { get; init; }");
        timelineEntrySource.Should().Contain("public Guid LoyaltyAccountId { get; init; }");
        timelineEntrySource.Should().Contain("public Guid BusinessId { get; init; }");
        timelineEntrySource.Should().Contain("public DateTime OccurredAtUtc { get; init; }");
        timelineEntrySource.Should().Contain("public int? PointsDelta { get; init; }");
        timelineEntrySource.Should().Contain("public int? PointsSpent { get; init; }");
        timelineEntrySource.Should().Contain("public Guid? RewardTierId { get; init; }");
        timelineEntrySource.Should().Contain("public string? Reference { get; init; }");
        timelineEntrySource.Should().Contain("public string? Note { get; init; }");

        timelineKindSource.Should().Contain("public enum LoyaltyTimelineEntryKind");
        timelineKindSource.Should().Contain("PointsTransaction = 0,");
        timelineKindSource.Should().Contain("RewardRedemption = 1");

        audienceKindSource.Should().Contain("public static class PromotionAudienceKind");
        audienceKindSource.Should().Contain("public const string JoinedMembers = \"JoinedMembers\";");
        audienceKindSource.Should().Contain("public const string TierSegment = \"TierSegment\";");
        audienceKindSource.Should().Contain("public const string PointsThreshold = \"PointsThreshold\";");
        audienceKindSource.Should().Contain("public const string DateWindow = \"DateWindow\";");

        campaignStateSource.Should().Contain("public static class PromotionCampaignState");
        campaignStateSource.Should().Contain("public const string Draft = \"Draft\";");
        campaignStateSource.Should().Contain("public const string Scheduled = \"Scheduled\";");
        campaignStateSource.Should().Contain("public const string Active = \"Active\";");
        campaignStateSource.Should().Contain("public const string Expired = \"Expired\";");

        interactionEventTypeSource.Should().Contain("public enum PromotionInteractionEventType");
        interactionEventTypeSource.Should().Contain("Impression = 1,");
        interactionEventTypeSource.Should().Contain("Open = 2,");
        interactionEventTypeSource.Should().Contain("Claim = 3");

        eligibilityRuleSource.Should().Contain("public sealed class PromotionEligibilityRule");
        eligibilityRuleSource.Should().Contain("public string AudienceKind { get; init; } = PromotionAudienceKind.JoinedMembers;");
        eligibilityRuleSource.Should().Contain("public int? MinPoints { get; init; }");
        eligibilityRuleSource.Should().Contain("public int? MaxPoints { get; init; }");
        eligibilityRuleSource.Should().Contain("public string? TierKey { get; init; }");
        eligibilityRuleSource.Should().Contain("public string? Note { get; init; }");
    }


    [Fact]
    public void BusinessAndLoyaltySummaryItemContracts_Should_KeepListAndDashboardBuildingBlockFloors()
    {
        var businessLocationSource = ReadContractsFile(Path.Combine("Businesses", "BusinessLocation.cs"));
        var businessReviewItemSource = ReadContractsFile(Path.Combine("Businesses", "BusinessReviewItem.cs"));
        var loyaltyAccountSummarySource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyAccountSummary.cs"));
        var loyaltyRewardSummarySource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyRewardSummary.cs"));
        var myLoyaltyBusinessSummarySource = ReadContractsFile(Path.Combine("Loyalty", "MyLoyaltyBusinessSummary.cs"));
        var myLoyaltyOverviewSource = ReadContractsFile(Path.Combine("Loyalty", "MyLoyaltyOverviewResponse.cs"));
        var pointsTransactionSource = ReadContractsFile(Path.Combine("Loyalty", "PointsTransaction.cs"));
        var loyaltyProgramPublicSource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyProgramPublic.cs"));

        businessLocationSource.Should().Contain("public sealed class BusinessLocation");
        businessLocationSource.Should().Contain("public Guid BusinessLocationId { get; init; }");
        businessLocationSource.Should().Contain("public string Name { get; init; } = default!;");
        businessLocationSource.Should().Contain("public string? AddressLine1 { get; init; }");
        businessLocationSource.Should().Contain("public string? AddressLine2 { get; init; }");
        businessLocationSource.Should().Contain("public string? City { get; init; }");
        businessLocationSource.Should().Contain("public string? Region { get; init; }");
        businessLocationSource.Should().Contain("public string? CountryCode { get; init; }");
        businessLocationSource.Should().Contain("public string? PostalCode { get; init; }");
        businessLocationSource.Should().Contain("public GeoCoordinateModel? Coordinate { get; init; }");
        businessLocationSource.Should().Contain("public bool IsPrimary { get; init; }");
        businessLocationSource.Should().Contain("public string? OpeningHoursJson { get; init; }");

        businessReviewItemSource.Should().Contain("public sealed class BusinessReviewItem");
        businessReviewItemSource.Should().Contain("public Guid Id { get; init; }");
        businessReviewItemSource.Should().Contain("public Guid UserId { get; init; }");
        businessReviewItemSource.Should().Contain("public string AuthorName { get; init; } = string.Empty;");
        businessReviewItemSource.Should().Contain("public byte Rating { get; init; }");
        businessReviewItemSource.Should().Contain("public string? Comment { get; init; }");
        businessReviewItemSource.Should().Contain("public DateTime CreatedAtUtc { get; init; }");

        loyaltyAccountSummarySource.Should().Contain("public sealed class LoyaltyAccountSummary");
        loyaltyAccountSummarySource.Should().Contain("public Guid BusinessId { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public string BusinessName { get; init; } = string.Empty;");
        loyaltyAccountSummarySource.Should().Contain("public int PointsBalance { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public Guid LoyaltyAccountId { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public int LifetimePoints { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public string Status { get; init; } = \"Active\";");
        loyaltyAccountSummarySource.Should().Contain("public DateTime? LastAccrualAtUtc { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public string? NextRewardTitle { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public int? NextRewardRequiredPoints { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public int? PointsToNextReward { get; init; }");
        loyaltyAccountSummarySource.Should().Contain("public decimal? NextRewardProgressPercent { get; init; }");

        loyaltyRewardSummarySource.Should().Contain("public sealed class LoyaltyRewardSummary");
        loyaltyRewardSummarySource.Should().Contain("public Guid LoyaltyRewardTierId { get; init; }");
        loyaltyRewardSummarySource.Should().Contain("public Guid BusinessId { get; init; }");
        loyaltyRewardSummarySource.Should().Contain("public string Name { get; init; } = string.Empty;");
        loyaltyRewardSummarySource.Should().Contain("public string? Description { get; init; }");
        loyaltyRewardSummarySource.Should().Contain("public int RequiredPoints { get; init; }");
        loyaltyRewardSummarySource.Should().Contain("public bool IsActive { get; init; }");
        loyaltyRewardSummarySource.Should().Contain("public bool RequiresConfirmation { get; init; }");
        loyaltyRewardSummarySource.Should().Contain("public bool IsSelectable { get; init; }");

        myLoyaltyBusinessSummarySource.Should().Contain("public sealed class MyLoyaltyBusinessSummary");
        myLoyaltyBusinessSummarySource.Should().Contain("public Guid BusinessId { get; init; }");
        myLoyaltyBusinessSummarySource.Should().Contain("public string BusinessName { get; init; } = string.Empty;");
        myLoyaltyBusinessSummarySource.Should().Contain("public string Category { get; init; } = \"Unknown\";");
        myLoyaltyBusinessSummarySource.Should().Contain("public string? City { get; init; }");
        myLoyaltyBusinessSummarySource.Should().Contain("public GeoCoordinateModel? Location { get; init; }");
        myLoyaltyBusinessSummarySource.Should().Contain("public string? PrimaryImageUrl { get; init; }");
        myLoyaltyBusinessSummarySource.Should().Contain("public int PointsBalance { get; init; }");
        myLoyaltyBusinessSummarySource.Should().Contain("public int LifetimePoints { get; init; }");
        myLoyaltyBusinessSummarySource.Should().Contain("public string Status { get; init; } = \"Active\";");
        myLoyaltyBusinessSummarySource.Should().Contain("public DateTime? LastAccrualAtUtc { get; init; }");

        myLoyaltyOverviewSource.Should().Contain("public sealed class MyLoyaltyOverviewResponse");
        myLoyaltyOverviewSource.Should().Contain("public int TotalAccounts { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public int ActiveAccounts { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public int TotalPointsBalance { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public int TotalLifetimePoints { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public DateTime? LastAccrualAtUtc { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public IReadOnlyList<LoyaltyAccountSummary> Accounts { get; set; } = Array.Empty<LoyaltyAccountSummary>();");
        myLoyaltyOverviewSource.Should().Contain("public sealed class MyLoyaltyBusinessDashboard");
        myLoyaltyOverviewSource.Should().Contain("public LoyaltyAccountSummary Account { get; set; } = new();");
        myLoyaltyOverviewSource.Should().Contain("public int AvailableRewardsCount { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public int RedeemableRewardsCount { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public LoyaltyRewardSummary? NextReward { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public IReadOnlyList<PointsTransaction> RecentTransactions { get; set; } = Array.Empty<PointsTransaction>();");
        myLoyaltyOverviewSource.Should().Contain("public int? PointsToNextReward { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public int? NextRewardRequiredPoints { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public decimal? NextRewardProgressPercent { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public bool ExpiryTrackingEnabled { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public int PointsExpiringSoon { get; set; }");
        myLoyaltyOverviewSource.Should().Contain("public DateTime? NextPointsExpiryAtUtc { get; set; }");

        pointsTransactionSource.Should().Contain("public sealed class PointsTransaction");
        pointsTransactionSource.Should().Contain("public DateTime OccurredAtUtc { get; init; }");
        pointsTransactionSource.Should().Contain("public string Type { get; init; } = \"Accrual\";");
        pointsTransactionSource.Should().Contain("public int Delta { get; init; }");
        pointsTransactionSource.Should().Contain("public string? Reference { get; init; }");
        pointsTransactionSource.Should().Contain("public string? Notes { get; init; }");

        loyaltyProgramPublicSource.Should().Contain("public sealed class LoyaltyProgramPublic");
        loyaltyProgramPublicSource.Should().Contain("public Guid Id { get; init; }");
        loyaltyProgramPublicSource.Should().Contain("public Guid BusinessId { get; init; }");
        loyaltyProgramPublicSource.Should().Contain("public string Name { get; init; } = string.Empty;");
        loyaltyProgramPublicSource.Should().Contain("public bool IsActive { get; init; }");
        loyaltyProgramPublicSource.Should().Contain("public IReadOnlyList<LoyaltyRewardTierPublic> RewardTiers { get; init; } = Array.Empty<LoyaltyRewardTierPublic>();");
    }


    [Fact]
    public void BillingAndBusinessLoyaltyItemContracts_Should_KeepPlanCampaignAndRewardSummaryFloors()
    {
        var billingPlanSummarySource = ReadContractsFile(Path.Combine("Billing", "BillingPlanSummary.cs"));
        var businessCampaignItemSource = ReadContractsFile(Path.Combine("Loyalty", "BusinessCampaignItem.cs"));
        var businessCampaignMutationResponseSource = ReadContractsFile(Path.Combine("Loyalty", "BusinessCampaignMutationResponse.cs"));
        var businessAccountSummarySource = ReadContractsFile(Path.Combine("Loyalty", "BusinessLoyaltyAccountSummary.cs"));
        var rewardTierConfigItemSource = ReadContractsFile(Path.Combine("Loyalty", "BusinessRewardTierConfigItem.cs"));
        var rewardTierMutationResponseSource = ReadContractsFile(Path.Combine("Loyalty", "BusinessRewardTierMutationResponse.cs"));
        var rewardTierPublicSource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyRewardTierPublic.cs"));
        var loyaltyProgramSummarySource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyProgramSummary.cs"));

        billingPlanSummarySource.Should().Contain("public sealed class BillingPlanSummary");
        billingPlanSummarySource.Should().Contain("public Guid Id { get; set; }");
        billingPlanSummarySource.Should().Contain("public string Code { get; set; } = string.Empty;");
        billingPlanSummarySource.Should().Contain("public string Name { get; set; } = string.Empty;");
        billingPlanSummarySource.Should().Contain("public string? Description { get; set; }");
        billingPlanSummarySource.Should().Contain("public long PriceMinor { get; set; }");
        billingPlanSummarySource.Should().Contain("public string Currency { get; set; } = ContractDefaults.DefaultCurrency;");
        billingPlanSummarySource.Should().Contain("public string Interval { get; set; } = string.Empty;");
        billingPlanSummarySource.Should().Contain("public int IntervalCount { get; set; }");
        billingPlanSummarySource.Should().Contain("public int? TrialDays { get; set; }");
        billingPlanSummarySource.Should().Contain("public bool IsActive { get; set; }");

        businessCampaignItemSource.Should().Contain("public sealed class BusinessCampaignItem");
        businessCampaignItemSource.Should().Contain("public Guid Id { get; init; }");
        businessCampaignItemSource.Should().Contain("public Guid BusinessId { get; init; }");
        businessCampaignItemSource.Should().Contain("public string Name { get; init; } = string.Empty;");
        businessCampaignItemSource.Should().Contain("public string Title { get; init; } = string.Empty;");
        businessCampaignItemSource.Should().Contain("public string? Subtitle { get; init; }");
        businessCampaignItemSource.Should().Contain("public string? Body { get; init; }");
        businessCampaignItemSource.Should().Contain("public string? MediaUrl { get; init; }");
        businessCampaignItemSource.Should().Contain("public string? LandingUrl { get; init; }");
        businessCampaignItemSource.Should().Contain("public short Channels { get; init; }");
        businessCampaignItemSource.Should().Contain("public DateTime? StartsAtUtc { get; init; }");
        businessCampaignItemSource.Should().Contain("public DateTime? EndsAtUtc { get; init; }");
        businessCampaignItemSource.Should().Contain("public bool IsActive { get; init; }");
        businessCampaignItemSource.Should().Contain("public string CampaignState { get; init; } = PromotionCampaignState.Draft;");
        businessCampaignItemSource.Should().Contain("public string TargetingJson { get; init; } = \"{}\";");
        businessCampaignItemSource.Should().Contain("public List<PromotionEligibilityRule> EligibilityRules { get; init; } = new();");
        businessCampaignItemSource.Should().Contain("public string PayloadJson { get; init; } = \"{}\";");
        businessCampaignItemSource.Should().Contain("public byte[] RowVersion { get; init; } = Array.Empty<byte>();");

        businessCampaignMutationResponseSource.Should().Contain("public sealed class BusinessCampaignMutationResponse");
        businessCampaignMutationResponseSource.Should().Contain("public Guid CampaignId { get; init; }");

        businessAccountSummarySource.Should().Contain("public sealed class BusinessLoyaltyAccountSummary");
        businessAccountSummarySource.Should().Contain("public Guid LoyaltyAccountId { get; set; }");
        businessAccountSummarySource.Should().Contain("public int PointsBalance { get; set; }");
        businessAccountSummarySource.Should().Contain("public string? CustomerDisplayName { get; set; }");

        rewardTierConfigItemSource.Should().Contain("public sealed class BusinessRewardTierConfigItem");
        rewardTierConfigItemSource.Should().Contain("public Guid RewardTierId { get; init; }");
        rewardTierConfigItemSource.Should().Contain("public int PointsRequired { get; init; }");
        rewardTierConfigItemSource.Should().Contain("public string RewardType { get; init; } = string.Empty;");
        rewardTierConfigItemSource.Should().Contain("public decimal? RewardValue { get; init; }");
        rewardTierConfigItemSource.Should().Contain("public string? Description { get; init; }");
        rewardTierConfigItemSource.Should().Contain("public bool AllowSelfRedemption { get; init; }");
        rewardTierConfigItemSource.Should().Contain("public byte[] RowVersion { get; init; } = Array.Empty<byte>();");

        rewardTierMutationResponseSource.Should().Contain("public sealed class BusinessRewardTierMutationResponse");
        rewardTierMutationResponseSource.Should().Contain("public Guid RewardTierId { get; init; }");
        rewardTierMutationResponseSource.Should().Contain("public bool Success { get; init; }");

        rewardTierPublicSource.Should().Contain("public sealed class LoyaltyRewardTierPublic");
        rewardTierPublicSource.Should().Contain("public Guid Id { get; init; }");
        rewardTierPublicSource.Should().Contain("public int PointsRequired { get; init; }");
        rewardTierPublicSource.Should().Contain("public string RewardType { get; init; } = string.Empty;");
        rewardTierPublicSource.Should().Contain("public decimal? RewardValue { get; init; }");
        rewardTierPublicSource.Should().Contain("public string? Description { get; init; }");
        rewardTierPublicSource.Should().Contain("public bool AllowSelfRedemption { get; init; }");

        loyaltyProgramSummarySource.Should().Contain("public sealed class LoyaltyProgramSummary");
        loyaltyProgramSummarySource.Should().Contain("public Guid LoyaltyProgramId { get; init; }");
        loyaltyProgramSummarySource.Should().Contain("public string Name { get; init; } = string.Empty;");
        loyaltyProgramSummarySource.Should().Contain("public string AccrualMode { get; init; } = \"PerVisit\";");
        loyaltyProgramSummarySource.Should().Contain("public IReadOnlyList<LoyaltyRewardTier> RewardTiers { get; init; } = Array.Empty<LoyaltyRewardTier>();");
    }


    [Fact]
    public void RemainingBusinessAndLoyaltyContracts_Should_KeepOnboardingRewardTierAndDiagnosticsFloors()
    {
        var onboardingResponseSource = ReadContractsFile(Path.Combine("Businesses", "BusinessOnboardingResponse.cs"));
        var createRewardTierRequestSource = ReadContractsFile(Path.Combine("Loyalty", "CreateBusinessRewardTierRequest.cs"));
        var deleteRewardTierRequestSource = ReadContractsFile(Path.Combine("Loyalty", "DeleteBusinessRewardTierRequest.cs"));
        var joinLoyaltyRequestSource = ReadContractsFile(Path.Combine("Loyalty", "JoinLoyaltyRequest.cs"));
        var prepareScanResponseSource = ReadContractsFile(Path.Combine("Loyalty", "PrepareScanSessionResponse.cs"));
        var processScanRequestSource = ReadContractsFile(Path.Combine("Loyalty", "ProcessScanSessionForBusinessRequest.cs"));
        var promotionDiagnosticsSource = ReadContractsFile(Path.Combine("Loyalty", "PromotionFeedDiagnostics.cs"));
        var setCampaignActivationSource = ReadContractsFile(Path.Combine("Loyalty", "SetCampaignActivationRequest.cs"));
        var updateRewardTierRequestSource = ReadContractsFile(Path.Combine("Loyalty", "UpdateBusinessRewardTierRequest.cs"));
        var myLoyaltyBusinessesRequestSource = ReadContractsFile(Path.Combine("Loyalty", "MyLoyaltyBusinessesRequest.cs"));
        var loyaltyRewardTierSource = ReadContractsFile(Path.Combine("Loyalty", "LoyaltyRewardTier.cs"));

        onboardingResponseSource.Should().Contain("public sealed class BusinessOnboardingResponse");
        onboardingResponseSource.Should().Contain("public Guid BusinessId { get; init; }");
        onboardingResponseSource.Should().Contain("public Guid BusinessMemberId { get; init; }");

        createRewardTierRequestSource.Should().Contain("public sealed class CreateBusinessRewardTierRequest");
        createRewardTierRequestSource.Should().Contain("public int PointsRequired { get; init; }");
        createRewardTierRequestSource.Should().Contain("public string RewardType { get; init; } = string.Empty;");
        createRewardTierRequestSource.Should().Contain("public decimal? RewardValue { get; init; }");
        createRewardTierRequestSource.Should().Contain("public string? Description { get; init; }");
        createRewardTierRequestSource.Should().Contain("public bool AllowSelfRedemption { get; init; }");
        createRewardTierRequestSource.Should().Contain("public string? MetadataJson { get; init; }");

        deleteRewardTierRequestSource.Should().Contain("public sealed class DeleteBusinessRewardTierRequest");
        deleteRewardTierRequestSource.Should().Contain("public Guid RewardTierId { get; init; }");
        deleteRewardTierRequestSource.Should().Contain("public byte[] RowVersion { get; init; } = Array.Empty<byte>();");

        joinLoyaltyRequestSource.Should().Contain("public sealed class JoinLoyaltyRequest");
        joinLoyaltyRequestSource.Should().Contain("public Guid? BusinessLocationId { get; init; }");

        prepareScanResponseSource.Should().Contain("public sealed class PrepareScanSessionResponse");
        prepareScanResponseSource.Should().Contain("public string ScanSessionToken { get; set; } = string.Empty;");
        prepareScanResponseSource.Should().Contain("public LoyaltyScanMode Mode { get; init; }");
        prepareScanResponseSource.Should().Contain("public DateTime ExpiresAtUtc { get; init; }");
        prepareScanResponseSource.Should().Contain("public int CurrentPointsBalance { get; init; }");
        prepareScanResponseSource.Should().Contain("public IReadOnlyList<LoyaltyRewardSummary> SelectedRewards { get; init; }");
        prepareScanResponseSource.Should().Contain("= Array.Empty<LoyaltyRewardSummary>();");

        processScanRequestSource.Should().Contain("public sealed class ProcessScanSessionForBusinessRequest");
        processScanRequestSource.Should().Contain("public string ScanSessionToken { get; set; } = string.Empty;");

        promotionDiagnosticsSource.Should().Contain("public sealed class PromotionFeedDiagnostics");
        promotionDiagnosticsSource.Should().Contain("public int InitialCandidates { get; init; }");
        promotionDiagnosticsSource.Should().Contain("public int SuppressedByFrequency { get; init; }");
        promotionDiagnosticsSource.Should().Contain("public int Deduplicated { get; init; }");
        promotionDiagnosticsSource.Should().Contain("public int TrimmedByCap { get; init; }");
        promotionDiagnosticsSource.Should().Contain("public int FinalCount { get; init; }");

        setCampaignActivationSource.Should().Contain("public sealed class SetCampaignActivationRequest");
        setCampaignActivationSource.Should().Contain("public Guid Id { get; init; }");
        setCampaignActivationSource.Should().Contain("public bool IsActive { get; init; }");
        setCampaignActivationSource.Should().Contain("public byte[] RowVersion { get; init; } = Array.Empty<byte>();");

        updateRewardTierRequestSource.Should().Contain("public sealed class UpdateBusinessRewardTierRequest");
        updateRewardTierRequestSource.Should().Contain("public Guid RewardTierId { get; init; }");
        updateRewardTierRequestSource.Should().Contain("public int PointsRequired { get; init; }");
        updateRewardTierRequestSource.Should().Contain("public string RewardType { get; init; } = string.Empty;");
        updateRewardTierRequestSource.Should().Contain("public decimal? RewardValue { get; init; }");
        updateRewardTierRequestSource.Should().Contain("public string? Description { get; init; }");
        updateRewardTierRequestSource.Should().Contain("public bool AllowSelfRedemption { get; init; }");
        updateRewardTierRequestSource.Should().Contain("public string? MetadataJson { get; init; }");
        updateRewardTierRequestSource.Should().Contain("public byte[] RowVersion { get; init; } = Array.Empty<byte>();");

        myLoyaltyBusinessesRequestSource.Should().Contain("public sealed class MyLoyaltyBusinessesRequest : PagedRequest");
        myLoyaltyBusinessesRequestSource.Should().Contain("public bool IncludeInactiveBusinesses { get; init; }");

        loyaltyRewardTierSource.Should().Contain("public sealed class LoyaltyRewardTier");
        loyaltyRewardTierSource.Should().Contain("public Guid LoyaltyRewardTierId { get; init; }");
        loyaltyRewardTierSource.Should().Contain("public int Threshold { get; init; }");
        loyaltyRewardTierSource.Should().Contain("public string Title { get; init; } = default!;");
        loyaltyRewardTierSource.Should().Contain("public string? Description { get; init; }");
    }
}

