using Darwin.Application.Abstractions.Services;
using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.Services.Settings;
using Darwin.WebAdmin.ViewModels.CRM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Darwin.WebAdmin.Controllers.Admin.CRM
{
    /// <summary>
    /// Admin CRM controller for customers, leads, opportunities, segments, and related timeline data.
    /// </summary>
    public sealed class CrmController : AdminBaseController
    {
        private readonly GetCustomersPageHandler _getCustomersPage;
        private readonly GetCustomerForEditHandler _getCustomerForEdit;
        private readonly GetCrmSummaryHandler _getCrmSummary;
        private readonly GetCustomerInteractionsPageHandler _getCustomerInteractionsPage;
        private readonly GetCustomerConsentsPageHandler _getCustomerConsentsPage;
        private readonly GetCustomerSegmentMembershipsHandler _getCustomerSegmentMemberships;
        private readonly CreateCustomerHandler _createCustomer;
        private readonly UpdateCustomerHandler _updateCustomer;
        private readonly GetLeadsPageHandler _getLeadsPage;
        private readonly GetLeadForEditHandler _getLeadForEdit;
        private readonly GetLeadInteractionsPageHandler _getLeadInteractionsPage;
        private readonly CreateLeadHandler _createLead;
        private readonly UpdateLeadHandler _updateLead;
        private readonly ConvertLeadToCustomerHandler _convertLeadToCustomer;
        private readonly UpdateLeadLifecycleHandler _updateLeadLifecycle;
        private readonly GetOpportunitiesPageHandler _getOpportunitiesPage;
        private readonly GetOpportunityForEditHandler _getOpportunityForEdit;
        private readonly GetOpportunityInteractionsPageHandler _getOpportunityInteractionsPage;
        private readonly CreateOpportunityHandler _createOpportunity;
        private readonly UpdateOpportunityHandler _updateOpportunity;
        private readonly UpdateOpportunityLifecycleHandler _updateOpportunityLifecycle;
        private readonly GetCustomerSegmentsPageHandler _getCustomerSegmentsPage;
        private readonly GetCustomerSegmentForEditHandler _getCustomerSegmentForEdit;
        private readonly GetInvoicesPageHandler _getInvoicesPage;
        private readonly GetInvoiceForEditHandler _getInvoiceForEdit;
        private readonly GetInvoiceArchiveSnapshotHandler _getInvoiceArchiveSnapshot;
        private readonly GetInvoiceArchiveDocumentHandler _getInvoiceArchiveDocument;
        private readonly CreateCustomerSegmentHandler _createCustomerSegment;
        private readonly UpdateCustomerSegmentHandler _updateCustomerSegment;
        private readonly UpdateInvoiceHandler _updateInvoice;
        private readonly TransitionInvoiceStatusHandler _transitionInvoiceStatus;
        private readonly CreateInvoiceRefundHandler _createInvoiceRefund;
        private readonly CreateInteractionHandler _createInteraction;
        private readonly CreateConsentHandler _createConsent;
        private readonly AssignCustomerSegmentHandler _assignCustomerSegment;
        private readonly RemoveCustomerSegmentMembershipHandler _removeCustomerSegmentMembership;
        private readonly AdminReferenceDataService _referenceData;
        private readonly ISiteSettingCache _siteSettingCache;
        private readonly IClock _clock;

        public CrmController(
            GetCustomersPageHandler getCustomersPage,
            GetCustomerForEditHandler getCustomerForEdit,
            GetCrmSummaryHandler getCrmSummary,
            GetCustomerInteractionsPageHandler getCustomerInteractionsPage,
            GetCustomerConsentsPageHandler getCustomerConsentsPage,
            GetCustomerSegmentMembershipsHandler getCustomerSegmentMemberships,
            CreateCustomerHandler createCustomer,
            UpdateCustomerHandler updateCustomer,
            GetLeadsPageHandler getLeadsPage,
            GetLeadForEditHandler getLeadForEdit,
            GetLeadInteractionsPageHandler getLeadInteractionsPage,
            CreateLeadHandler createLead,
            UpdateLeadHandler updateLead,
            ConvertLeadToCustomerHandler convertLeadToCustomer,
            UpdateLeadLifecycleHandler updateLeadLifecycle,
            GetOpportunitiesPageHandler getOpportunitiesPage,
            GetOpportunityForEditHandler getOpportunityForEdit,
            GetOpportunityInteractionsPageHandler getOpportunityInteractionsPage,
            CreateOpportunityHandler createOpportunity,
            UpdateOpportunityHandler updateOpportunity,
            UpdateOpportunityLifecycleHandler updateOpportunityLifecycle,
            GetCustomerSegmentsPageHandler getCustomerSegmentsPage,
            GetCustomerSegmentForEditHandler getCustomerSegmentForEdit,
            GetInvoicesPageHandler getInvoicesPage,
            GetInvoiceForEditHandler getInvoiceForEdit,
            GetInvoiceArchiveSnapshotHandler getInvoiceArchiveSnapshot,
            GetInvoiceArchiveDocumentHandler getInvoiceArchiveDocument,
            CreateCustomerSegmentHandler createCustomerSegment,
            UpdateCustomerSegmentHandler updateCustomerSegment,
            UpdateInvoiceHandler updateInvoice,
            TransitionInvoiceStatusHandler transitionInvoiceStatus,
            CreateInvoiceRefundHandler createInvoiceRefund,
            CreateInteractionHandler createInteraction,
            CreateConsentHandler createConsent,
            AssignCustomerSegmentHandler assignCustomerSegment,
            RemoveCustomerSegmentMembershipHandler removeCustomerSegmentMembership,
            AdminReferenceDataService referenceData,
            ISiteSettingCache siteSettingCache,
            IClock clock)
        {
            _getCustomersPage = getCustomersPage ?? throw new ArgumentNullException(nameof(getCustomersPage));
            _getCustomerForEdit = getCustomerForEdit ?? throw new ArgumentNullException(nameof(getCustomerForEdit));
            _getCrmSummary = getCrmSummary ?? throw new ArgumentNullException(nameof(getCrmSummary));
            _getCustomerInteractionsPage = getCustomerInteractionsPage ?? throw new ArgumentNullException(nameof(getCustomerInteractionsPage));
            _getCustomerConsentsPage = getCustomerConsentsPage ?? throw new ArgumentNullException(nameof(getCustomerConsentsPage));
            _getCustomerSegmentMemberships = getCustomerSegmentMemberships ?? throw new ArgumentNullException(nameof(getCustomerSegmentMemberships));
            _createCustomer = createCustomer ?? throw new ArgumentNullException(nameof(createCustomer));
            _updateCustomer = updateCustomer ?? throw new ArgumentNullException(nameof(updateCustomer));
            _getLeadsPage = getLeadsPage ?? throw new ArgumentNullException(nameof(getLeadsPage));
            _getLeadForEdit = getLeadForEdit ?? throw new ArgumentNullException(nameof(getLeadForEdit));
            _getLeadInteractionsPage = getLeadInteractionsPage ?? throw new ArgumentNullException(nameof(getLeadInteractionsPage));
            _createLead = createLead ?? throw new ArgumentNullException(nameof(createLead));
            _updateLead = updateLead ?? throw new ArgumentNullException(nameof(updateLead));
            _convertLeadToCustomer = convertLeadToCustomer ?? throw new ArgumentNullException(nameof(convertLeadToCustomer));
            _updateLeadLifecycle = updateLeadLifecycle ?? throw new ArgumentNullException(nameof(updateLeadLifecycle));
            _getOpportunitiesPage = getOpportunitiesPage ?? throw new ArgumentNullException(nameof(getOpportunitiesPage));
            _getOpportunityForEdit = getOpportunityForEdit ?? throw new ArgumentNullException(nameof(getOpportunityForEdit));
            _getOpportunityInteractionsPage = getOpportunityInteractionsPage ?? throw new ArgumentNullException(nameof(getOpportunityInteractionsPage));
            _createOpportunity = createOpportunity ?? throw new ArgumentNullException(nameof(createOpportunity));
            _updateOpportunity = updateOpportunity ?? throw new ArgumentNullException(nameof(updateOpportunity));
            _updateOpportunityLifecycle = updateOpportunityLifecycle ?? throw new ArgumentNullException(nameof(updateOpportunityLifecycle));
            _getCustomerSegmentsPage = getCustomerSegmentsPage ?? throw new ArgumentNullException(nameof(getCustomerSegmentsPage));
            _getCustomerSegmentForEdit = getCustomerSegmentForEdit ?? throw new ArgumentNullException(nameof(getCustomerSegmentForEdit));
            _getInvoicesPage = getInvoicesPage ?? throw new ArgumentNullException(nameof(getInvoicesPage));
            _getInvoiceForEdit = getInvoiceForEdit ?? throw new ArgumentNullException(nameof(getInvoiceForEdit));
            _getInvoiceArchiveSnapshot = getInvoiceArchiveSnapshot ?? throw new ArgumentNullException(nameof(getInvoiceArchiveSnapshot));
            _getInvoiceArchiveDocument = getInvoiceArchiveDocument ?? throw new ArgumentNullException(nameof(getInvoiceArchiveDocument));
            _createCustomerSegment = createCustomerSegment ?? throw new ArgumentNullException(nameof(createCustomerSegment));
            _updateCustomerSegment = updateCustomerSegment ?? throw new ArgumentNullException(nameof(updateCustomerSegment));
            _updateInvoice = updateInvoice ?? throw new ArgumentNullException(nameof(updateInvoice));
            _transitionInvoiceStatus = transitionInvoiceStatus ?? throw new ArgumentNullException(nameof(transitionInvoiceStatus));
            _createInvoiceRefund = createInvoiceRefund ?? throw new ArgumentNullException(nameof(createInvoiceRefund));
            _createInteraction = createInteraction ?? throw new ArgumentNullException(nameof(createInteraction));
            _createConsent = createConsent ?? throw new ArgumentNullException(nameof(createConsent));
            _assignCustomerSegment = assignCustomerSegment ?? throw new ArgumentNullException(nameof(assignCustomerSegment));
            _removeCustomerSegmentMembership = removeCustomerSegmentMembership ?? throw new ArgumentNullException(nameof(removeCustomerSegmentMembership));
            _referenceData = referenceData ?? throw new ArgumentNullException(nameof(referenceData));
            _siteSettingCache = siteSettingCache ?? throw new ArgumentNullException(nameof(siteSettingCache));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            return RenderOverviewWorkspace(MapSummary(summary, settings.DefaultCurrency));
        }

        [HttpGet]
        public async Task<IActionResult> Customers(int page = 1, int pageSize = 20, string? q = null, CustomerQueueFilter filter = CustomerQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getCustomersPage.HandleAsync(page, pageSize, q, filter, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var customerItems = items.ToList();
            var vm = new CustomersListVm
            {
                Summary = MapSummary(summary, settings.DefaultCurrency),
                OpsSummary = BuildCustomerOpsSummary(customerItems),
                Playbooks = BuildCustomerPlaybooks(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                PlatformDefaultCulture = settings.DefaultCulture,
                FilterItems = BuildCustomerFilterItems(filter),
                Items = customerItems.Select(x => new CustomerListItemVm
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Phone = x.Phone,
                    CompanyName = x.CompanyName,
                    TaxProfileType = x.TaxProfileType,
                    VatId = x.VatId,
                    Locale = x.Locale,
                    UsesPlatformLocaleFallback = x.UsesPlatformLocaleFallback,
                    SegmentCount = x.SegmentCount,
                    OpportunityCount = x.OpportunityCount,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderCustomersWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCustomer(CancellationToken ct = default)
        {
            var vm = new CustomerEditVm();
            vm.Addresses.Add(new CustomerAddressVm
            {
                Country = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault,
                IsDefaultBilling = true,
                IsDefaultShipping = true
            });

            await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderCustomerEditor(vm, nameof(CreateCustomer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustomer(CustomerEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCustomerEditor(vm, "CreateCustomer");
            }

            try
            {
                var id = await _createCustomer.HandleAsync(new CustomerCreateDto
                {
                    UserId = vm.UserId,
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Email = vm.Email,
                    Phone = vm.Phone,
                    CompanyName = vm.CompanyName,
                    TaxProfileType = vm.TaxProfileType,
                    VatId = vm.VatId,
                    Notes = vm.Notes,
                    Addresses = vm.Addresses.Select(MapCustomerAddress).ToList()
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("CustomerCreatedMessage");
                return RedirectOrHtmx(nameof(EditCustomer), new { id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("CustomerCreateFailedMessage", ex);
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCustomerEditor(vm, "CreateCustomer");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditCustomer(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("CustomerNotFoundMessage");
                return RedirectOrHtmx(nameof(Customers), new { });
            }

            var dto = await _getCustomerForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("CustomerNotFoundMessage");
                return RedirectOrHtmx(nameof(Customers), new { });
            }

            var nowUtc = _clock.UtcNow;
            var vm = new CustomerEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                UserId = dto.UserId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                TaxProfileType = dto.TaxProfileType,
                VatId = dto.VatId,
                Notes = dto.Notes,
                EffectiveFirstName = dto.EffectiveFirstName,
                EffectiveLastName = dto.EffectiveLastName,
                EffectiveEmail = dto.EffectiveEmail,
                EffectivePhone = dto.EffectivePhone,
                EffectiveLocale = dto.EffectiveLocale,
                UsesPlatformLocaleFallback = dto.UsesPlatformLocaleFallback,
                SegmentCount = dto.SegmentCount,
                OpportunityCount = dto.OpportunityCount,
                InteractionCount = dto.InteractionCount,
                ConsentCount = dto.ConsentCount,
                DefaultBillingAddress = dto.DefaultBillingAddress is null ? null : new IdentityAddressSummaryVm
                {
                    FullName = dto.DefaultBillingAddress.FullName,
                    Street1 = dto.DefaultBillingAddress.Street1,
                    Street2 = dto.DefaultBillingAddress.Street2,
                    PostalCode = dto.DefaultBillingAddress.PostalCode,
                    City = dto.DefaultBillingAddress.City,
                    State = dto.DefaultBillingAddress.State,
                    CountryCode = dto.DefaultBillingAddress.CountryCode,
                    PhoneE164 = dto.DefaultBillingAddress.PhoneE164
                },
                DefaultShippingAddress = dto.DefaultShippingAddress is null ? null : new IdentityAddressSummaryVm
                {
                    FullName = dto.DefaultShippingAddress.FullName,
                    Street1 = dto.DefaultShippingAddress.Street1,
                    Street2 = dto.DefaultShippingAddress.Street2,
                    PostalCode = dto.DefaultShippingAddress.PostalCode,
                    City = dto.DefaultShippingAddress.City,
                    State = dto.DefaultShippingAddress.State,
                    CountryCode = dto.DefaultShippingAddress.CountryCode,
                    PhoneE164 = dto.DefaultShippingAddress.PhoneE164
                },
                Addresses = dto.Addresses.Select(x => new CustomerAddressVm
                {
                    Id = x.Id,
                    AddressId = x.AddressId,
                    Line1 = x.Line1,
                    Line2 = x.Line2,
                    City = x.City,
                    State = x.State,
                    PostalCode = x.PostalCode,
                    Country = x.Country,
                    IsDefaultBilling = x.IsDefaultBilling,
                    IsDefaultShipping = x.IsDefaultShipping
                }).ToList(),
                NewInteraction = new InteractionCreateVm { CustomerId = dto.Id },
                NewConsent = new ConsentCreateVm { CustomerId = dto.Id, GrantedAtUtc = nowUtc },
                SegmentAssignment = new AssignCustomerSegmentVm { CustomerId = dto.Id }
            };

            EnsureCustomerAddressRows(vm);
            await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderCustomerEditor(vm, "EditCustomer");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(CustomerEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("CustomerNotFoundMessage");
                return RedirectOrHtmx(nameof(Customers), new { });
            }

            if (!ModelState.IsValid)
            {
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCustomerEditor(vm, nameof(EditCustomer));
            }

            try
            {
                await _updateCustomer.HandleAsync(new CustomerEditDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    UserId = vm.UserId,
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    Email = vm.Email,
                    Phone = vm.Phone,
                    CompanyName = vm.CompanyName,
                    TaxProfileType = vm.TaxProfileType,
                    VatId = vm.VatId,
                    Notes = vm.Notes,
                    Addresses = vm.Addresses.Select(MapCustomerAddress).ToList()
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("CustomerUpdatedMessage");
                return RedirectOrHtmx(nameof(EditCustomer), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("CustomerConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditCustomer), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("CustomerUpdateFailedMessage", ex);
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCustomerEditor(vm, "EditCustomer");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Invoices(int page = 1, int pageSize = 20, string? q = null, InvoiceQueueFilter filter = InvoiceQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getInvoicesPage.HandleAsync(page, pageSize, q, filter, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var invoiceItems = items.Select(x => new InvoiceListItemVm
            {
                Id = x.Id,
                BusinessId = x.BusinessId,
                CustomerId = x.CustomerId,
                CustomerDisplayName = x.CustomerDisplayName,
                CustomerTaxProfileType = x.CustomerTaxProfileType,
                CustomerVatId = x.CustomerVatId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                PaymentId = x.PaymentId,
                PaymentSummary = x.PaymentSummary,
                Status = x.Status,
                Currency = x.Currency,
                TotalNetMinor = x.TotalNetMinor,
                TotalTaxMinor = x.TotalTaxMinor,
                TotalGrossMinor = x.TotalGrossMinor,
                RefundedAmountMinor = x.RefundedAmountMinor,
                SettledAmountMinor = x.SettledAmountMinor,
                BalanceMinor = x.BalanceMinor,
                DueDateUtc = x.DueDateUtc,
                PaidAtUtc = x.PaidAtUtc,
                IssuedAtUtc = x.IssuedAtUtc,
                HasIssuedSnapshot = x.HasIssuedSnapshot,
                IssuedSnapshotHashSha256 = x.IssuedSnapshotHashSha256,
                ArchiveGeneratedAtUtc = x.ArchiveGeneratedAtUtc,
                ArchiveRetainUntilUtc = x.ArchiveRetainUntilUtc,
                ArchiveRetentionPolicyVersion = x.ArchiveRetentionPolicyVersion,
                ArchivePurgedAtUtc = x.ArchivePurgedAtUtc,
                ArchivePurgeReason = x.ArchivePurgeReason,
                RowVersion = x.RowVersion
            }).ToList();
            var todayUtc = _clock.UtcNow.Date;

            return RenderInvoicesWorkspace(new InvoicesListVm
            {
                Summary = MapSummary(summary, settings.DefaultCurrency),
                OpsSummary = BuildInvoiceOpsSummary(invoiceItems, todayUtc),
                Playbooks = BuildInvoicePlaybooks(),
                TaxPolicy = MapTaxPolicy(settings),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                Items = invoiceItems
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditInvoice(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("InvoiceNotFoundMessage");
                return RedirectOrHtmx(nameof(Invoices), new { });
            }

            var dto = await _getInvoiceForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("InvoiceNotFoundMessage");
                return RedirectOrHtmx(nameof(Invoices), new { });
            }

            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var vm = new InvoiceEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                TaxPolicy = MapTaxPolicy(settings),
                BusinessId = dto.BusinessId,
                CustomerId = dto.CustomerId,
                CustomerDisplayName = dto.CustomerDisplayName,
                CustomerTaxProfileType = dto.CustomerTaxProfileType,
                CustomerVatId = dto.CustomerVatId,
                OrderId = dto.OrderId,
                OrderNumber = dto.OrderNumber,
                PaymentId = dto.PaymentId,
                PaymentSummary = dto.PaymentSummary,
                Status = dto.Status,
                Currency = dto.Currency,
                TotalNetMinor = dto.TotalNetMinor,
                TotalTaxMinor = dto.TotalTaxMinor,
                TotalGrossMinor = dto.TotalGrossMinor,
                RefundedAmountMinor = dto.RefundedAmountMinor,
                SettledAmountMinor = dto.SettledAmountMinor,
                BalanceMinor = dto.BalanceMinor,
                DueDateUtc = dto.DueDateUtc,
                PaidAtUtc = dto.PaidAtUtc,
                IssuedAtUtc = dto.IssuedAtUtc,
                HasIssuedSnapshot = dto.HasIssuedSnapshot,
                IssuedSnapshotHashSha256 = dto.IssuedSnapshotHashSha256,
                ArchiveGeneratedAtUtc = dto.ArchiveGeneratedAtUtc,
                ArchiveRetainUntilUtc = dto.ArchiveRetainUntilUtc,
                ArchiveRetentionPolicyVersion = dto.ArchiveRetentionPolicyVersion,
                ArchivePurgedAtUtc = dto.ArchivePurgedAtUtc,
                ArchivePurgeReason = dto.ArchivePurgeReason,
                IsFinancialContentLocked = IsInvoiceFinancialContentLocked(dto.Status),
                FinancialEditLockReason = IsInvoiceFinancialContentLocked(dto.Status) ? T("InvoiceFinancialEditLockReason") : string.Empty,
                Refund = new InvoiceRefundCreateVm
                {
                    InvoiceId = dto.Id,
                    RowVersion = dto.RowVersion,
                    AmountMinor = dto.SettledAmountMinor,
                    Currency = dto.Currency,
                    Reason = "Customer refund"
                }
            };

            await PopulateInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderInvoiceEditor(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadInvoiceArchiveSnapshot(Guid id, CancellationToken ct = default)
        {
            var snapshot = await _getInvoiceArchiveSnapshot.HandleAsync(id, ct).ConfigureAwait(false);
            if (snapshot is null)
            {
                SetErrorMessage("InvoiceArchiveUnavailableMessage");
                return RedirectOrHtmx(nameof(EditInvoice), new { id });
            }

            return File(
                Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(snapshot.SnapshotJson)).ToArray(),
                "application/json",
                snapshot.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadInvoiceArchiveDocument(Guid id, CancellationToken ct = default)
        {
            var document = await _getInvoiceArchiveDocument.HandleAsync(id, ct).ConfigureAwait(false);
            if (document is null)
            {
                SetErrorMessage("InvoiceArchiveUnavailableMessage");
                return RedirectOrHtmx(nameof(EditInvoice), new { id });
            }

            return File(
                Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(document.Html)).ToArray(),
                "text/html; charset=utf-8",
                document.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInvoice(InvoiceEditVm vm, CancellationToken ct = default)
        {
            vm.IsFinancialContentLocked = IsInvoiceFinancialContentLocked(vm.Status);
            vm.FinancialEditLockReason = vm.IsFinancialContentLocked ? T("InvoiceFinancialEditLockReason") : string.Empty;

            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("InvoiceNotFoundMessage");
                return RedirectOrHtmx(nameof(Invoices), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderInvoiceEditor(vm);
            }

            try
            {
                var existingInvoice = await _getInvoiceForEdit.HandleAsync(vm.Id, ct).ConfigureAwait(false);
                if (existingInvoice is null)
                {
                    SetErrorMessage("InvoiceNotFoundMessage");
                    return RedirectOrHtmx(nameof(Invoices), new { });
                }

                if (IsInvoiceFinancialContentLocked(existingInvoice.Status))
                {
                    SetErrorMessage("InvoiceFinancialEditLockedMessage");
                    return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.Id });
                }

                await _updateInvoice.HandleAsync(new InvoiceEditDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    BusinessId = vm.BusinessId,
                    CustomerId = vm.CustomerId,
                    OrderId = vm.OrderId,
                    PaymentId = vm.PaymentId,
                    Status = vm.Status,
                    Currency = vm.Currency,
                    TotalNetMinor = vm.TotalNetMinor,
                    TotalTaxMinor = vm.TotalTaxMinor,
                    TotalGrossMinor = vm.TotalGrossMinor,
                    DueDateUtc = vm.DueDateUtc,
                    PaidAtUtc = vm.PaidAtUtc
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("InvoiceUpdatedMessage");
                return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("InvoiceConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("InvoiceUpdateFailedMessage", ex);
                await PopulateInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderInvoiceEditor(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransitionInvoiceStatus(InvoiceStatusTransitionVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("InvoiceStatusUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Invoices), new { });
            }

            try
            {
                await _transitionInvoiceStatus.HandleAsync(new InvoiceStatusTransitionDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    TargetStatus = vm.TargetStatus,
                    PaidAtUtc = vm.PaidAtUtc
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("InvoiceStatusUpdatedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("InvoiceConcurrencyMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("InvoiceStatusUpdateFailedMessage", ex);
            }

            return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundInvoice(InvoiceRefundCreateVm vm, CancellationToken ct = default)
        {
            if (vm.InvoiceId == Guid.Empty)
            {
                SetErrorMessage("InvoiceRefundRecordFailedMessage");
                return RedirectOrHtmx(nameof(Invoices), new { });
            }

            try
            {
                await _createInvoiceRefund.HandleAsync(new InvoiceRefundCreateDto
                {
                    InvoiceId = vm.InvoiceId,
                    RowVersion = vm.RowVersion,
                    AmountMinor = vm.AmountMinor,
                    Currency = vm.Currency,
                    Reason = vm.Reason
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("InvoiceRefundRecordedMessage");
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("InvoiceConcurrencyMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("InvoiceRefundRecordFailedMessage", ex);
            }

            return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.InvoiceId });
        }

        [HttpGet]
        public async Task<IActionResult> Leads(int page = 1, int pageSize = 20, string? q = null, LeadQueueFilter filter = LeadQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getLeadsPage.HandleAsync(page, pageSize, q, filter, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var leadItems = items.ToList();
            var vm = new LeadsListVm
            {
                Summary = MapSummary(summary, settings.DefaultCurrency),
                OpsSummary = BuildLeadOpsSummary(leadItems),
                Playbooks = BuildLeadPlaybooks(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildLeadFilterItems(filter),
                Items = leadItems.Select(x => new LeadListItemVm
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    FullName = (x.FirstName + " " + x.LastName).Trim(),
                    CompanyName = x.CompanyName,
                    Email = x.Email,
                    Phone = x.Phone,
                    Status = x.Status,
                    AssignedToUserId = x.AssignedToUserId,
                    AssignedToUserDisplayName = x.AssignedToUserDisplayName,
                    InteractionCount = x.InteractionCount,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderLeadsWorkspace(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateLead(CancellationToken ct = default)
        {
            var vm = new LeadEditVm();
            await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderLeadEditor(vm, nameof(CreateLead));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLead(LeadEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderLeadEditor(vm, nameof(CreateLead));
            }

            try
            {
                var id = await _createLead.HandleAsync(new LeadCreateDto
                {
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    CompanyName = vm.CompanyName,
                    Email = vm.Email,
                    Phone = vm.Phone,
                    Source = vm.Source,
                    Notes = vm.Notes,
                    Status = vm.Status,
                    AssignedToUserId = vm.AssignedToUserId,
                    CustomerId = vm.CustomerId
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("LeadCreatedMessage");
                return RedirectOrHtmx(nameof(EditLead), new { id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("LeadCreateFailedMessage", ex);
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderLeadEditor(vm, nameof(CreateLead));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditLead(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("LeadNotFoundMessage");
                return RedirectOrHtmx(nameof(Leads), new { });
            }

            var dto = await _getLeadForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("LeadNotFoundMessage");
                return RedirectOrHtmx(nameof(Leads), new { });
            }

            var vm = new LeadEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                CompanyName = dto.CompanyName,
                Email = dto.Email,
                Phone = dto.Phone,
                Source = dto.Source,
                Notes = dto.Notes,
                Status = dto.Status,
                AssignedToUserId = dto.AssignedToUserId,
                AssignedToUserDisplayName = dto.AssignedToUserDisplayName,
                CustomerId = dto.CustomerId,
                CustomerDisplayName = dto.CustomerDisplayName,
                InteractionCount = dto.InteractionCount,
                Conversion = new ConvertLeadVm
                {
                    LeadId = dto.Id,
                    RowVersion = dto.RowVersion,
                    CopyNotesToCustomer = true
                },
                NewInteraction = new InteractionCreateVm { LeadId = dto.Id }
            };

            await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderLeadEditor(vm, nameof(EditLead));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLead(LeadEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("LeadNotFoundMessage");
                return RedirectOrHtmx(nameof(Leads), new { });
            }

            if (!ModelState.IsValid)
            {
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderLeadEditor(vm, nameof(EditLead));
            }

            try
            {
                await _updateLead.HandleAsync(new LeadEditDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    FirstName = vm.FirstName,
                    LastName = vm.LastName,
                    CompanyName = vm.CompanyName,
                    Email = vm.Email,
                    Phone = vm.Phone,
                    Source = vm.Source,
                    Notes = vm.Notes,
                    Status = vm.Status,
                    AssignedToUserId = vm.AssignedToUserId,
                    CustomerId = vm.CustomerId
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("LeadUpdatedMessage");
                return RedirectOrHtmx(nameof(EditLead), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("LeadConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditLead), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("LeadUpdateFailedMessage", ex);
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderLeadEditor(vm, nameof(EditLead));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Opportunities(int page = 1, int pageSize = 20, string? q = null, OpportunityQueueFilter filter = OpportunityQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getOpportunitiesPage.HandleAsync(page, pageSize, q, filter, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var opportunityItems = items.ToList();
            var todayUtc = _clock.UtcNow.Date;
            var vm = new OpportunitiesListVm
            {
                Summary = MapSummary(summary, settings.DefaultCurrency),
                OpsSummary = BuildOpportunityOpsSummary(opportunityItems, todayUtc),
                Playbooks = BuildOpportunityPlaybooks(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildOpportunityFilterItems(filter),
                Items = opportunityItems.Select(x => new OpportunityListItemVm
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    Title = x.Title,
                    Currency = settings.DefaultCurrency,
                    EstimatedValueMinor = x.EstimatedValueMinor,
                    Stage = x.Stage,
                    ExpectedCloseDateUtc = x.ExpectedCloseDateUtc,
                    AssignedToUserId = x.AssignedToUserId,
                    AssignedToUserDisplayName = x.AssignedToUserDisplayName,
                    ItemCount = x.ItemCount,
                    InteractionCount = x.InteractionCount,
                    ModifiedAtUtc = x.ModifiedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderOpportunitiesWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLeadLifecycle(
            Guid id,
            string rowVersion,
            string action,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            LeadQueueFilter filter = LeadQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("LeadLifecycleUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Leads), new { page, pageSize, q, filter });
            }

            var version = DecodeBase64RowVersion(rowVersion);

            var result = await _updateLeadLifecycle
                .HandleAsync(new UpdateLeadLifecycleDto
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
                    "Qualify" => "LeadQualifiedMessage",
                    "Disqualify" => "LeadDisqualifiedMessage",
                    "Reopen" => "LeadReopenedMessage",
                    _ => "LeadLifecycleUpdatedMessage"
                });
            }
            else
            {
                TempData["Error"] = result.Error ?? T("LeadLifecycleUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(Leads), new { page, pageSize, q, filter });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOpportunityLifecycle(
            Guid id,
            string rowVersion,
            string action,
            int page = 1,
            int pageSize = 20,
            string? q = null,
            OpportunityQueueFilter filter = OpportunityQueueFilter.All,
            CancellationToken ct = default)
        {
            if (id == Guid.Empty || string.IsNullOrWhiteSpace(action))
            {
                SetErrorMessage("OpportunityLifecycleUpdateFailedMessage");
                return RedirectOrHtmx(nameof(Opportunities), new { page, pageSize, q, filter });
            }

            var version = DecodeBase64RowVersion(rowVersion);

            var result = await _updateOpportunityLifecycle
                .HandleAsync(new UpdateOpportunityLifecycleDto
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
                    "Advance" => "OpportunityAdvancedMessage",
                    "CloseWon" => "OpportunityClosedWonMessage",
                    "CloseLost" => "OpportunityClosedLostMessage",
                    "Reopen" => "OpportunityReopenedMessage",
                    _ => "OpportunityLifecycleUpdatedMessage"
                });
            }
            else
            {
                TempData["Error"] = result.Error ?? T("OpportunityLifecycleUpdateFailedMessage");
            }

            return RedirectOrHtmx(nameof(Opportunities), new { page, pageSize, q, filter });
        }

        [HttpGet]
        public async Task<IActionResult> CreateOpportunity(Guid? customerId = null, CancellationToken ct = default)
        {
            if (customerId == Guid.Empty)
            {
                customerId = null;
            }

            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var vm = new OpportunityEditVm
            {
                CustomerId = customerId ?? Guid.Empty,
                Currency = settings.DefaultCurrency
            };
            EnsureOpportunityLineRows(vm);
            await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderOpportunityEditor(vm, nameof(CreateOpportunity));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOpportunity(OpportunityEditVm vm, CancellationToken ct = default)
        {
            await EnsureOpportunityCurrencyAsync(vm, ct).ConfigureAwait(false);

            if (!ModelState.IsValid)
            {
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderOpportunityEditor(vm, nameof(CreateOpportunity));
            }

            try
            {
                var id = await _createOpportunity.HandleAsync(new OpportunityCreateDto
                {
                    CustomerId = vm.CustomerId,
                    Title = vm.Title,
                    EstimatedValueMinor = vm.EstimatedValueMinor,
                    Stage = vm.Stage,
                    ExpectedCloseDateUtc = vm.ExpectedCloseDateUtc,
                    AssignedToUserId = vm.AssignedToUserId,
                    Items = vm.Items.Select(x => new OpportunityItemDto
                    {
                        Id = x.Id,
                        ProductVariantId = x.ProductVariantId,
                        Quantity = x.Quantity,
                        UnitPriceMinor = x.UnitPriceMinor
                    }).ToList()
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("OpportunityCreatedMessage");
                return RedirectOrHtmx(nameof(EditOpportunity), new { id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("OpportunityCreateFailedMessage", ex);
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderOpportunityEditor(vm, nameof(CreateOpportunity));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditOpportunity(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("OpportunityNotFoundMessage");
                return RedirectOrHtmx(nameof(Opportunities), new { });
            }

            var dto = await _getOpportunityForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("OpportunityNotFoundMessage");
                return RedirectOrHtmx(nameof(Opportunities), new { });
            }

            var vm = new OpportunityEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                CustomerId = dto.CustomerId,
                Title = dto.Title,
                Currency = (await _siteSettingCache.GetAsync(ct).ConfigureAwait(false)).DefaultCurrency,
                EstimatedValueMinor = dto.EstimatedValueMinor,
                Stage = dto.Stage,
                ExpectedCloseDateUtc = dto.ExpectedCloseDateUtc,
                AssignedToUserId = dto.AssignedToUserId,
                AssignedToUserDisplayName = dto.AssignedToUserDisplayName,
                CustomerDisplayName = dto.CustomerDisplayName,
                InteractionCount = dto.InteractionCount,
                Items = dto.Items.Select(x => new OpportunityItemVm
                {
                    Id = x.Id,
                    ProductVariantId = x.ProductVariantId,
                    Quantity = x.Quantity,
                    UnitPriceMinor = x.UnitPriceMinor
                }).ToList(),
                NewInteraction = new InteractionCreateVm { OpportunityId = dto.Id }
            };

            EnsureOpportunityLineRows(vm);
            await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderOpportunityEditor(vm, nameof(EditOpportunity));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOpportunity(OpportunityEditVm vm, CancellationToken ct = default)
        {
            await EnsureOpportunityCurrencyAsync(vm, ct).ConfigureAwait(false);

            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("OpportunityNotFoundMessage");
                return RedirectOrHtmx(nameof(Opportunities), new { });
            }

            if (!ModelState.IsValid)
            {
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderOpportunityEditor(vm, nameof(EditOpportunity));
            }

            try
            {
                await _updateOpportunity.HandleAsync(new OpportunityEditDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    CustomerId = vm.CustomerId,
                    Title = vm.Title,
                    EstimatedValueMinor = vm.EstimatedValueMinor,
                    Stage = vm.Stage,
                    ExpectedCloseDateUtc = vm.ExpectedCloseDateUtc,
                    AssignedToUserId = vm.AssignedToUserId,
                    Items = vm.Items.Select(x => new OpportunityItemDto
                    {
                        Id = x.Id,
                        ProductVariantId = x.ProductVariantId,
                        Quantity = x.Quantity,
                        UnitPriceMinor = x.UnitPriceMinor
                    }).ToList()
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("OpportunityUpdatedMessage");
                return RedirectOrHtmx(nameof(EditOpportunity), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("OpportunityConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditOpportunity), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("OpportunityUpdateFailedMessage", ex);
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderOpportunityEditor(vm, nameof(EditOpportunity));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Segments(int page = 1, int pageSize = 20, string? q = null, CustomerSegmentQueueFilter filter = CustomerSegmentQueueFilter.All, CancellationToken ct = default)
        {
            var (items, total) = await _getCustomerSegmentsPage.HandleAsync(page, pageSize, q, filter, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            var segmentSummary = await _getCustomerSegmentsPage.GetSummaryAsync(ct).ConfigureAwait(false);
            var vm = new CustomerSegmentsListVm
            {
                Summary = MapSummary(summary, settings.DefaultCurrency),
                SegmentSummary = new CustomerSegmentOpsSummaryVm
                {
                    TotalCount = segmentSummary.TotalCount,
                    EmptyCount = segmentSummary.EmptyCount,
                    InUseCount = segmentSummary.InUseCount,
                    MissingDescriptionCount = segmentSummary.MissingDescriptionCount
                },
                Playbooks = BuildSegmentPlaybooks(),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Filter = filter,
                FilterItems = BuildSegmentFilterItems(filter),
                Items = items.Select(x => new CustomerSegmentListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    MemberCount = x.MemberCount,
                    HasDescription = x.HasDescription,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return RenderSegmentsWorkspace(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertLead(ConvertLeadVm vm, CancellationToken ct = default)
        {
            if (vm.LeadId == Guid.Empty)
            {
                SetErrorMessage("LeadConvertFailedMessage");
                return RedirectOrHtmx(nameof(Leads), new { });
            }

            try
            {
                var customerId = await _convertLeadToCustomer.HandleAsync(new ConvertLeadToCustomerDto
                {
                    LeadId = vm.LeadId,
                    RowVersion = vm.RowVersion,
                    UserId = vm.UserId,
                    CopyNotesToCustomer = vm.CopyNotesToCustomer
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("LeadConvertedMessage");
                return RedirectOrHtmx(nameof(EditCustomer), new { id = customerId });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("LeadConcurrencyMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("LeadConvertFailedMessage", ex);
            }

            return RedirectOrHtmx(nameof(EditLead), new { id = vm.LeadId });
        }

        [HttpGet]
        public IActionResult CreateSegment() => RenderSegmentEditor(new CustomerSegmentEditVm(), nameof(CreateSegment));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSegment(CustomerSegmentEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return RenderSegmentEditor(vm, nameof(CreateSegment));
            }

            try
            {
                var id = await _createCustomerSegment.HandleAsync(new CustomerSegmentEditDto
                {
                    Name = vm.Name,
                    Description = vm.Description
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("SegmentCreatedMessage");
                return RedirectOrHtmx(nameof(EditSegment), new { id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("SegmentCreateFailedMessage", ex);
                return RenderSegmentEditor(vm, nameof(CreateSegment));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSegment(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("SegmentNotFoundMessage");
                return RedirectOrHtmx(nameof(Segments), new { });
            }

            var dto = await _getCustomerSegmentForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("SegmentNotFoundMessage");
                return RedirectOrHtmx(nameof(Segments), new { });
            }

            return RenderSegmentEditor(new CustomerSegmentEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Name = dto.Name,
                Description = dto.Description
            }, nameof(EditSegment));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSegment(CustomerSegmentEditVm vm, CancellationToken ct = default)
        {
            if (vm.Id == Guid.Empty)
            {
                SetErrorMessage("SegmentNotFoundMessage");
                return RedirectOrHtmx(nameof(Segments), new { });
            }

            if (!ModelState.IsValid)
            {
                return RenderSegmentEditor(vm, nameof(EditSegment));
            }

            try
            {
                await _updateCustomerSegment.HandleAsync(new CustomerSegmentEditDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    Name = vm.Name,
                    Description = vm.Description
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("SegmentUpdatedMessage");
                return RedirectOrHtmx(nameof(EditSegment), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                SetErrorMessage("SegmentConcurrencyMessage");
                return RedirectOrHtmx(nameof(EditSegment), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                AddLocalizedModelError("SegmentUpdateFailedMessage", ex);
                return RenderSegmentEditor(vm, nameof(EditSegment));
            }
        }

        [HttpGet]
        public async Task<IActionResult> CustomerInteractions(Guid customerId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            if (customerId == Guid.Empty)
            {
                return BadRequest();
            }

            var (items, total) = await _getCustomerInteractionsPage.HandleAsync(customerId, page, pageSize, ct).ConfigureAwait(false);
            return PartialView("~/Views/Crm/_InteractionsSection.cshtml", new InteractionsPageVm
            {
                Scope = "customer",
                EntityId = customerId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(MapInteraction).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerInteractions(InteractionCreateVm vm, CancellationToken ct = default)
        {
            if (vm.CustomerId is null || vm.CustomerId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                SetErrorMessage("InteractionAddFailedMessage");
                return await CustomerInteractions(vm.CustomerId.Value, ct: ct).ConfigureAwait(false);
            }

            try
            {
                await _createInteraction.HandleAsync(MapInteraction(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("InteractionAddedMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("InteractionAddFailedMessage", ex);
            }

            return await CustomerInteractions(vm.CustomerId ?? Guid.Empty, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> LeadInteractions(Guid leadId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            if (leadId == Guid.Empty)
            {
                return BadRequest();
            }

            var (items, total) = await _getLeadInteractionsPage.HandleAsync(leadId, page, pageSize, ct).ConfigureAwait(false);
            return PartialView("~/Views/Crm/_InteractionsSection.cshtml", new InteractionsPageVm
            {
                Scope = "lead",
                EntityId = leadId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(MapInteraction).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeadInteractions(InteractionCreateVm vm, CancellationToken ct = default)
        {
            if (vm.LeadId is null || vm.LeadId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                SetErrorMessage("InteractionAddFailedMessage");
                return await LeadInteractions(vm.LeadId.Value, ct: ct).ConfigureAwait(false);
            }

            try
            {
                await _createInteraction.HandleAsync(MapInteraction(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("InteractionAddedMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("InteractionAddFailedMessage", ex);
            }

            return await LeadInteractions(vm.LeadId ?? Guid.Empty, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> OpportunityInteractions(Guid opportunityId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            if (opportunityId == Guid.Empty)
            {
                return BadRequest();
            }

            var (items, total) = await _getOpportunityInteractionsPage.HandleAsync(opportunityId, page, pageSize, ct).ConfigureAwait(false);
            return PartialView("~/Views/Crm/_InteractionsSection.cshtml", new InteractionsPageVm
            {
                Scope = "opportunity",
                EntityId = opportunityId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(MapInteraction).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpportunityInteractions(InteractionCreateVm vm, CancellationToken ct = default)
        {
            if (vm.OpportunityId is null || vm.OpportunityId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                SetErrorMessage("InteractionAddFailedMessage");
                return await OpportunityInteractions(vm.OpportunityId.Value, ct: ct).ConfigureAwait(false);
            }

            try
            {
                await _createInteraction.HandleAsync(MapInteraction(vm), ct).ConfigureAwait(false);
                SetSuccessMessage("InteractionAddedMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("InteractionAddFailedMessage", ex);
            }

            return await OpportunityInteractions(vm.OpportunityId ?? Guid.Empty, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerConsents(Guid customerId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            if (customerId == Guid.Empty)
            {
                return BadRequest();
            }

            var (items, total) = await _getCustomerConsentsPage.HandleAsync(customerId, page, pageSize, ct).ConfigureAwait(false);
            return PartialView("~/Views/Crm/_ConsentsSection.cshtml", new ConsentsPageVm
            {
                CustomerId = customerId,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items.Select(x => new ConsentListItemVm
                {
                    Id = x.Id,
                    Type = x.Type,
                    Granted = x.Granted,
                    GrantedAtUtc = x.GrantedAtUtc,
                    RevokedAtUtc = x.RevokedAtUtc
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerConsents(ConsentCreateVm vm, CancellationToken ct = default)
        {
            if (vm.CustomerId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                SetErrorMessage("ConsentAddFailedMessage");
                return await CustomerConsents(vm.CustomerId, ct: ct).ConfigureAwait(false);
            }

            try
            {
                await _createConsent.HandleAsync(new ConsentCreateDto
                {
                    CustomerId = vm.CustomerId,
                    Type = vm.Type,
                    Granted = vm.Granted,
                    GrantedAtUtc = vm.GrantedAtUtc,
                    RevokedAtUtc = vm.RevokedAtUtc
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("ConsentAddedMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("ConsentAddFailedMessage", ex);
            }

            return await CustomerConsents(vm.CustomerId, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerSegmentMemberships(Guid customerId, CancellationToken ct = default)
        {
            if (customerId == Guid.Empty)
            {
                return BadRequest();
            }

            var items = await _getCustomerSegmentMemberships.HandleAsync(customerId, ct).ConfigureAwait(false);
            return PartialView("~/Views/Crm/_CustomerSegmentsSection.cshtml", new CustomerMembershipsVm
            {
                CustomerId = customerId,
                Items = items.Select(x => new CustomerSegmentMembershipVm
                {
                    MembershipId = x.MembershipId,
                    SegmentId = x.SegmentId,
                    Name = x.Name,
                    Description = x.Description
                }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerSegmentMemberships(AssignCustomerSegmentVm vm, CancellationToken ct = default)
        {
            if (vm.CustomerId == Guid.Empty || vm.CustomerSegmentId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                SetErrorMessage("SegmentAssignFailedMessage");
                return await CustomerSegmentMemberships(vm.CustomerId, ct).ConfigureAwait(false);
            }

            try
            {
                await _assignCustomerSegment.HandleAsync(new AssignCustomerSegmentDto
                {
                    CustomerId = vm.CustomerId,
                    CustomerSegmentId = vm.CustomerSegmentId
                }, ct).ConfigureAwait(false);

                SetSuccessMessage("SegmentAssignedMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("SegmentAssignFailedMessage", ex);
            }

            return await CustomerSegmentMemberships(vm.CustomerId, ct).ConfigureAwait(false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCustomerSegmentMembership(Guid customerId, Guid membershipId, CancellationToken ct = default)
        {
            if (customerId == Guid.Empty || membershipId == Guid.Empty)
            {
                return BadRequest();
            }

            try
            {
                await _removeCustomerSegmentMembership.HandleAsync(membershipId, ct).ConfigureAwait(false);
                SetSuccessMessage("SegmentRemovedMessage");
            }
            catch (Exception ex)
            {
                SetLocalizedError("SegmentRemoveFailedMessage", ex);
            }

            return await CustomerSegmentMemberships(customerId, ct).ConfigureAwait(false);
        }

        private async Task PopulateCustomerOptionsAsync(CustomerEditVm vm, CancellationToken ct)
        {
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.NewInteraction.UserOptions = await _referenceData.GetUserOptionsAsync(vm.NewInteraction.UserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.SegmentOptions = await _referenceData.GetCustomerSegmentOptionsAsync(vm.SegmentAssignment.CustomerSegmentId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private async Task PopulateLeadOptionsAsync(LeadEditVm vm, CancellationToken ct)
        {
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.AssignedToUserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.NewInteraction.UserOptions = await _referenceData.GetUserOptionsAsync(vm.NewInteraction.UserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.Conversion.UserOptions = await _referenceData.GetUserOptionsAsync(vm.Conversion.UserId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private async Task PopulateOpportunityOptionsAsync(OpportunityEditVm vm, CancellationToken ct)
        {
            vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: false, ct).ConfigureAwait(false);
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.AssignedToUserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(null, ct).ConfigureAwait(false);
            vm.NewInteraction.UserOptions = await _referenceData.GetUserOptionsAsync(vm.NewInteraction.UserId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private async Task EnsureOpportunityCurrencyAsync(OpportunityEditVm vm, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(vm.Currency))
            {
                return;
            }

            var settings = await _siteSettingCache.GetAsync(ct).ConfigureAwait(false);
            vm.Currency = settings.DefaultCurrency;
            ModelState.Remove(nameof(OpportunityEditVm.Currency));
        }

        private async Task PopulateInvoiceOptionsAsync(InvoiceEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.PaymentOptions = await _referenceData.GetPaymentOptionsAsync(vm.PaymentId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private void AddLocalizedModelError(string fallbackKey, Exception ex)
        {
            ModelState.AddModelError(string.Empty, T(fallbackKey));
        }

        private void SetLocalizedError(string fallbackKey, Exception ex)
        {
            TempData["Error"] = T(fallbackKey);
        }

        private static void EnsureCustomerAddressRows(CustomerEditVm vm)
        {
            vm.Addresses ??= new List<CustomerAddressVm>();
            if (vm.Addresses.Count == 0)
            {
                vm.Addresses.Add(new CustomerAddressVm
                {
                    Country = Darwin.Application.Settings.DTOs.SiteSettingDto.DefaultCountryDefault,
                    IsDefaultBilling = true,
                    IsDefaultShipping = true
                });
            }
        }

        private static void EnsureOpportunityLineRows(OpportunityEditVm vm)
        {
            vm.Items ??= new List<OpportunityItemVm>();
            if (vm.Items.Count == 0)
            {
                vm.Items.Add(new OpportunityItemVm());
            }
        }

        private static CustomerAddressDto MapCustomerAddress(CustomerAddressVm vm)
        {
            return new CustomerAddressDto
            {
                Id = vm.Id,
                AddressId = vm.AddressId,
                Line1 = vm.Line1,
                Line2 = vm.Line2,
                City = vm.City,
                State = vm.State,
                PostalCode = vm.PostalCode,
                Country = vm.Country,
                IsDefaultBilling = vm.IsDefaultBilling,
                IsDefaultShipping = vm.IsDefaultShipping
            };
        }

        private static InteractionCreateDto MapInteraction(InteractionCreateVm vm)
        {
            return new InteractionCreateDto
            {
                CustomerId = vm.CustomerId,
                LeadId = vm.LeadId,
                OpportunityId = vm.OpportunityId,
                Type = vm.Type,
                Channel = vm.Channel,
                Subject = vm.Subject,
                Content = vm.Content,
                UserId = vm.UserId
            };
        }

        private static InteractionListItemVm MapInteraction(InteractionListItemDto dto)
        {
            return new InteractionListItemVm
            {
                Id = dto.Id,
                Type = dto.Type,
                Channel = dto.Channel,
                Subject = dto.Subject,
                Content = dto.Content,
                CreatedAtUtc = dto.CreatedAtUtc
            };
        }

        private static CrmSummaryVm MapSummary(CrmSummaryDto dto, string currency)
        {
            return new CrmSummaryVm
            {
                CustomerCount = dto.CustomerCount,
                LeadCount = dto.LeadCount,
                QualifiedLeadCount = dto.QualifiedLeadCount,
                OpenOpportunityCount = dto.OpenOpportunityCount,
                Currency = string.IsNullOrWhiteSpace(currency) ? string.Empty : currency.Trim().ToUpperInvariant(),
                OpenPipelineMinor = dto.OpenPipelineMinor,
                SegmentCount = dto.SegmentCount,
                RecentInteractionCount = dto.RecentInteractionCount
            };
        }

        private TaxPolicySnapshotVm MapTaxPolicy(Darwin.Application.Settings.DTOs.SiteSettingDto dto)
        {
            var issuerConfigured = !string.IsNullOrWhiteSpace(dto.InvoiceIssuerLegalName);
            var issuerTaxIdConfigured = !string.IsNullOrWhiteSpace(dto.InvoiceIssuerTaxId);
            var issuerAddressConfigured =
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerAddressLine1) &&
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerPostalCode) &&
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerCity) &&
                !string.IsNullOrWhiteSpace(dto.InvoiceIssuerCountry);
            var archiveReady = issuerConfigured && issuerTaxIdConfigured && issuerAddressConfigured;
            var eInvoiceBaselineReady = archiveReady && dto.VatEnabled;
            var structuredExportBaselineReady = archiveReady;

            return new TaxPolicySnapshotVm
            {
                VatEnabled = dto.VatEnabled,
                DefaultVatRatePercent = dto.DefaultVatRatePercent,
                PricesIncludeVat = dto.PricesIncludeVat,
                AllowReverseCharge = dto.AllowReverseCharge,
                IssuerConfigured = issuerConfigured,
                InvoiceIssuerLegalName = dto.InvoiceIssuerLegalName ?? string.Empty,
                InvoiceIssuerCountry = dto.InvoiceIssuerCountry ?? string.Empty,
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

        private static bool IsInvoiceFinancialContentLocked(Darwin.Domain.Enums.InvoiceStatus status)
        {
            return status is not Darwin.Domain.Enums.InvoiceStatus.Draft;
        }

        private IEnumerable<SelectListItem> BuildCustomerFilterItems(CustomerQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllCustomers"), CustomerQueueFilter.All.ToString(), selectedFilter == CustomerQueueFilter.All);
            yield return new SelectListItem(T("LinkedUser"), CustomerQueueFilter.LinkedUser.ToString(), selectedFilter == CustomerQueueFilter.LinkedUser);
            yield return new SelectListItem(T("NeedsSegmentation"), CustomerQueueFilter.NeedsSegmentation.ToString(), selectedFilter == CustomerQueueFilter.NeedsSegmentation);
            yield return new SelectListItem(T("HasOpportunities"), CustomerQueueFilter.HasOpportunities.ToString(), selectedFilter == CustomerQueueFilter.HasOpportunities);
            yield return new SelectListItem(T("B2B"), CustomerQueueFilter.Business.ToString(), selectedFilter == CustomerQueueFilter.Business);
            yield return new SelectListItem(T("B2BMissingVatId"), CustomerQueueFilter.MissingVatId.ToString(), selectedFilter == CustomerQueueFilter.MissingVatId);
            yield return new SelectListItem(T("UsesPlatformLocaleFallback"), CustomerQueueFilter.UsesPlatformLocaleFallback.ToString(), selectedFilter == CustomerQueueFilter.UsesPlatformLocaleFallback);
        }

        private static CustomerOpsSummaryVm BuildCustomerOpsSummary(IReadOnlyCollection<CustomerListItemDto> items)
        {
            return new CustomerOpsSummaryVm
            {
                LinkedUserCount = items.Count(x => x.UserId.HasValue),
                LocaleFallbackCount = items.Count(x => x.UsesPlatformLocaleFallback),
                BusinessCount = items.Count(x => x.TaxProfileType == Darwin.Domain.Enums.CustomerTaxProfileType.Business),
                MissingVatIdCount = items.Count(x => x.TaxProfileType == Darwin.Domain.Enums.CustomerTaxProfileType.Business && string.IsNullOrWhiteSpace(x.VatId)),
                NeedsSegmentationCount = items.Count(x => x.SegmentCount == 0),
                HasOpportunitiesCount = items.Count(x => x.OpportunityCount > 0)
            };
        }

        private static List<CrmPlaybookVm> BuildCustomerPlaybooks()
        {
            return new List<CrmPlaybookVm>
            {
                new()
                {
                    Title = "CrmCustomerLocaleFallbackPlaybookTitle",
                    ScopeNote = "CrmCustomerLocaleFallbackPlaybookScopeNote",
                    OperatorAction = "CrmCustomerLocaleFallbackPlaybookAction"
                },
                new()
                {
                    Title = "CrmCustomerMissingVatPlaybookTitle",
                    ScopeNote = "CrmCustomerMissingVatPlaybookScopeNote",
                    OperatorAction = "CrmCustomerMissingVatPlaybookAction"
                },
                new()
                {
                    Title = "CrmCustomerUnsegmentedPlaybookTitle",
                    ScopeNote = "CrmCustomerUnsegmentedPlaybookScopeNote",
                    OperatorAction = "CrmCustomerUnsegmentedPlaybookAction"
                }
            };
        }

        private static LeadOpsSummaryVm BuildLeadOpsSummary(IReadOnlyCollection<LeadListItemDto> items)
        {
            return new LeadOpsSummaryVm
            {
                QualifiedCount = items.Count(x => x.Status == Darwin.Domain.Enums.LeadStatus.Qualified),
                UnassignedCount = items.Count(x => !x.AssignedToUserId.HasValue),
                UnconvertedCount = items.Count(x => !x.CustomerId.HasValue),
                LinkedCustomerCount = items.Count(x => x.CustomerId.HasValue),
                HighInteractionCount = items.Count(x => x.InteractionCount >= 3)
            };
        }

        private static List<CrmPlaybookVm> BuildLeadPlaybooks()
        {
            return new List<CrmPlaybookVm>
            {
                new()
                {
                    Title = "CrmLeadQualifiedPlaybookTitle",
                    ScopeNote = "CrmLeadQualifiedPlaybookScopeNote",
                    OperatorAction = "CrmLeadQualifiedPlaybookAction"
                },
                new()
                {
                    Title = "CrmLeadUnassignedPlaybookTitle",
                    ScopeNote = "CrmLeadUnassignedPlaybookScopeNote",
                    OperatorAction = "CrmLeadUnassignedPlaybookAction"
                },
                new()
                {
                    Title = "CrmLeadUnconvertedPlaybookTitle",
                    ScopeNote = "CrmLeadUnconvertedPlaybookScopeNote",
                    OperatorAction = "CrmLeadUnconvertedPlaybookAction"
                }
            };
        }

        private static OpportunityOpsSummaryVm BuildOpportunityOpsSummary(IReadOnlyCollection<OpportunityListItemDto> items, DateTime todayUtc)
        {
            var closingSoonThreshold = todayUtc.AddDays(14);
            return new OpportunityOpsSummaryVm
            {
                OpenCount = items.Count(x => x.Stage != Darwin.Domain.Enums.OpportunityStage.ClosedWon && x.Stage != Darwin.Domain.Enums.OpportunityStage.ClosedLost),
                ClosingSoonCount = items.Count(x => x.ExpectedCloseDateUtc.HasValue && x.ExpectedCloseDateUtc.Value.Date <= closingSoonThreshold),
                HighValueCount = items.Count(x => x.EstimatedValueMinor >= 100000),
                UnassignedCount = items.Count(x => !x.AssignedToUserId.HasValue),
                HighInteractionCount = items.Count(x => x.InteractionCount >= 3)
            };
        }

        private static List<CrmPlaybookVm> BuildOpportunityPlaybooks()
        {
            return new List<CrmPlaybookVm>
            {
                new()
                {
                    Title = "CrmOpportunityClosingSoonPlaybookTitle",
                    ScopeNote = "CrmOpportunityClosingSoonPlaybookScopeNote",
                    OperatorAction = "CrmOpportunityClosingSoonPlaybookAction"
                },
                new()
                {
                    Title = "CrmOpportunityUnassignedPlaybookTitle",
                    ScopeNote = "CrmOpportunityUnassignedPlaybookScopeNote",
                    OperatorAction = "CrmOpportunityUnassignedPlaybookAction"
                },
                new()
                {
                    Title = "CrmOpportunityHighInteractionPlaybookTitle",
                    ScopeNote = "CrmOpportunityHighInteractionPlaybookScopeNote",
                    OperatorAction = "CrmOpportunityHighInteractionPlaybookAction"
                }
            };
        }

        private InvoiceOpsSummaryVm BuildInvoiceOpsSummary(List<InvoiceListItemVm> items, DateTime todayUtc)
        {
            return new InvoiceOpsSummaryVm
            {
                DraftCount = items.Count(x => x.Status == Darwin.Domain.Enums.InvoiceStatus.Draft),
                DueSoonCount = items.Count(x => x.BalanceMinor > 0 && x.DueDateUtc.Date >= todayUtc && x.DueDateUtc.Date <= todayUtc.AddDays(7)),
                OverdueCount = items.Count(x => x.BalanceMinor > 0 && x.DueDateUtc.Date < todayUtc),
                MissingVatIdCount = items.Count(x => x.CustomerTaxProfileType == Darwin.Domain.Enums.CustomerTaxProfileType.Business && string.IsNullOrWhiteSpace(x.CustomerVatId)),
                RefundedCount = items.Count(x => x.RefundedAmountMinor > 0)
            };
        }

        private List<CrmPlaybookVm> BuildInvoicePlaybooks()
        {
            return new List<CrmPlaybookVm>
            {
                new()
                {
                    Title = "CrmInvoicesPlaybookDueSoonTitle",
                    ScopeNote = "CrmInvoicesPlaybookDueSoonScope",
                    OperatorAction = "CrmInvoicesPlaybookDueSoonAction"
                },
                new()
                {
                    Title = "CrmInvoicesPlaybookVatGapTitle",
                    ScopeNote = "CrmInvoicesPlaybookVatGapScope",
                    OperatorAction = "CrmInvoicesPlaybookVatGapAction"
                },
                new()
                {
                    Title = "CrmInvoicesPlaybookRefundTitle",
                    ScopeNote = "CrmInvoicesPlaybookRefundScope",
                    OperatorAction = "CrmInvoicesPlaybookRefundAction"
                }
            };
        }

        private IActionResult RenderCustomersWorkspace(CustomersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Crm/Customers.cshtml", vm);
            }

            return View("Customers", vm);
        }

        private IActionResult RenderOverviewWorkspace(CrmSummaryVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Crm/Overview.cshtml", vm);
            }

            return View("Overview", vm);
        }

        private IEnumerable<SelectListItem> BuildLeadFilterItems(LeadQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllLeads"), LeadQueueFilter.All.ToString(), selectedFilter == LeadQueueFilter.All);
            yield return new SelectListItem(T("Qualified"), LeadQueueFilter.Qualified.ToString(), selectedFilter == LeadQueueFilter.Qualified);
            yield return new SelectListItem(T("Unassigned"), LeadQueueFilter.Unassigned.ToString(), selectedFilter == LeadQueueFilter.Unassigned);
            yield return new SelectListItem(T("Unconverted"), LeadQueueFilter.Unconverted.ToString(), selectedFilter == LeadQueueFilter.Unconverted);
        }

        private IEnumerable<SelectListItem> BuildOpportunityFilterItems(OpportunityQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllOpportunities"), OpportunityQueueFilter.All.ToString(), selectedFilter == OpportunityQueueFilter.All);
            yield return new SelectListItem(T("Open"), OpportunityQueueFilter.Open.ToString(), selectedFilter == OpportunityQueueFilter.Open);
            yield return new SelectListItem(T("ClosingSoon"), OpportunityQueueFilter.ClosingSoon.ToString(), selectedFilter == OpportunityQueueFilter.ClosingSoon);
            yield return new SelectListItem(T("HighValue"), OpportunityQueueFilter.HighValue.ToString(), selectedFilter == OpportunityQueueFilter.HighValue);
        }

        private IEnumerable<SelectListItem> BuildSegmentFilterItems(CustomerSegmentQueueFilter selectedFilter)
        {
            yield return new SelectListItem(T("AllSegments"), CustomerSegmentQueueFilter.All.ToString(), selectedFilter == CustomerSegmentQueueFilter.All);
            yield return new SelectListItem(T("Empty"), CustomerSegmentQueueFilter.Empty.ToString(), selectedFilter == CustomerSegmentQueueFilter.Empty);
            yield return new SelectListItem(T("InUse"), CustomerSegmentQueueFilter.InUse.ToString(), selectedFilter == CustomerSegmentQueueFilter.InUse);
            yield return new SelectListItem(T("MissingDescription"), CustomerSegmentQueueFilter.MissingDescription.ToString(), selectedFilter == CustomerSegmentQueueFilter.MissingDescription);
        }

        private List<CrmPlaybookVm> BuildSegmentPlaybooks()
        {
            return new List<CrmPlaybookVm>
            {
                new()
                {
                    Title = T("CrmSegmentsPlaybookEmptyTitle"),
                    ScopeNote = T("CrmSegmentsPlaybookEmptyScope"),
                    OperatorAction = T("CrmSegmentsPlaybookEmptyAction")
                },
                new()
                {
                    Title = T("CrmSegmentsPlaybookMissingDescriptionTitle"),
                    ScopeNote = T("CrmSegmentsPlaybookMissingDescriptionScope"),
                    OperatorAction = T("CrmSegmentsPlaybookMissingDescriptionAction")
                }
            };
        }

        private IActionResult RenderCustomerEditor(CustomerEditVm vm, string actionName)
        {
            if (IsHtmxRequest())
            {
                ViewData["FormAction"] = actionName;
                return PartialView("~/Views/Crm/_CustomerEditorShell.cshtml", vm);
            }

            return actionName == nameof(CreateCustomer) ? View("CreateCustomer", vm) : View("EditCustomer", vm);
        }

        private IActionResult RenderSegmentsWorkspace(CustomerSegmentsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Crm/Segments.cshtml", vm);
            }

            return View("Segments", vm);
        }

        private IActionResult RenderInvoicesWorkspace(InvoicesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Crm/Invoices.cshtml", vm);
            }

            return View("Invoices", vm);
        }

        private IActionResult RenderInvoiceEditor(InvoiceEditVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Crm/_InvoiceEditorShell.cshtml", vm);
            }

            return View("EditInvoice", vm);
        }

        private IActionResult RenderLeadEditor(LeadEditVm vm, string actionName)
        {
            if (IsHtmxRequest())
            {
                ViewData["FormAction"] = actionName;
                return PartialView("~/Views/Crm/_LeadEditorShell.cshtml", vm);
            }

            return actionName == nameof(CreateLead) ? View("CreateLead", vm) : View("EditLead", vm);
        }

        private IActionResult RenderLeadsWorkspace(LeadsListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Crm/Leads.cshtml", vm);
            }

            return View("Leads", vm);
        }

        private IActionResult RenderOpportunityEditor(OpportunityEditVm vm, string actionName)
        {
            if (IsHtmxRequest())
            {
                ViewData["FormAction"] = actionName;
                return PartialView("~/Views/Crm/_OpportunityEditorShell.cshtml", vm);
            }

            return actionName == nameof(CreateOpportunity) ? View("CreateOpportunity", vm) : View("EditOpportunity", vm);
        }

        private IActionResult RenderOpportunitiesWorkspace(OpportunitiesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Crm/Opportunities.cshtml", vm);
            }

            return View("Opportunities", vm);
        }

        private IActionResult RenderSegmentEditor(CustomerSegmentEditVm vm, string actionName)
        {
            if (IsHtmxRequest())
            {
                ViewData["FormAction"] = actionName;
                return PartialView("~/Views/Crm/_SegmentEditorShell.cshtml", vm);
            }

            return actionName == nameof(CreateSegment) ? View("CreateSegment", vm) : View("EditSegment", vm);
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


