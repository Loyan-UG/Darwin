using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.CRM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
        private readonly GetOpportunitiesPageHandler _getOpportunitiesPage;
        private readonly GetOpportunityForEditHandler _getOpportunityForEdit;
        private readonly GetOpportunityInteractionsPageHandler _getOpportunityInteractionsPage;
        private readonly CreateOpportunityHandler _createOpportunity;
        private readonly UpdateOpportunityHandler _updateOpportunity;
        private readonly GetCustomerSegmentsPageHandler _getCustomerSegmentsPage;
        private readonly GetCustomerSegmentForEditHandler _getCustomerSegmentForEdit;
        private readonly GetInvoicesPageHandler _getInvoicesPage;
        private readonly GetInvoiceForEditHandler _getInvoiceForEdit;
        private readonly CreateCustomerSegmentHandler _createCustomerSegment;
        private readonly UpdateCustomerSegmentHandler _updateCustomerSegment;
        private readonly UpdateInvoiceHandler _updateInvoice;
        private readonly TransitionInvoiceStatusHandler _transitionInvoiceStatus;
        private readonly CreateInteractionHandler _createInteraction;
        private readonly CreateConsentHandler _createConsent;
        private readonly AssignCustomerSegmentHandler _assignCustomerSegment;
        private readonly RemoveCustomerSegmentMembershipHandler _removeCustomerSegmentMembership;
        private readonly AdminReferenceDataService _referenceData;

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
            GetOpportunitiesPageHandler getOpportunitiesPage,
            GetOpportunityForEditHandler getOpportunityForEdit,
            GetOpportunityInteractionsPageHandler getOpportunityInteractionsPage,
            CreateOpportunityHandler createOpportunity,
            UpdateOpportunityHandler updateOpportunity,
            GetCustomerSegmentsPageHandler getCustomerSegmentsPage,
            GetCustomerSegmentForEditHandler getCustomerSegmentForEdit,
            GetInvoicesPageHandler getInvoicesPage,
            GetInvoiceForEditHandler getInvoiceForEdit,
            CreateCustomerSegmentHandler createCustomerSegment,
            UpdateCustomerSegmentHandler updateCustomerSegment,
            UpdateInvoiceHandler updateInvoice,
            TransitionInvoiceStatusHandler transitionInvoiceStatus,
            CreateInteractionHandler createInteraction,
            CreateConsentHandler createConsent,
            AssignCustomerSegmentHandler assignCustomerSegment,
            RemoveCustomerSegmentMembershipHandler removeCustomerSegmentMembership,
            AdminReferenceDataService referenceData)
        {
            _getCustomersPage = getCustomersPage;
            _getCustomerForEdit = getCustomerForEdit;
            _getCrmSummary = getCrmSummary;
            _getCustomerInteractionsPage = getCustomerInteractionsPage;
            _getCustomerConsentsPage = getCustomerConsentsPage;
            _getCustomerSegmentMemberships = getCustomerSegmentMemberships;
            _createCustomer = createCustomer;
            _updateCustomer = updateCustomer;
            _getLeadsPage = getLeadsPage;
            _getLeadForEdit = getLeadForEdit;
            _getLeadInteractionsPage = getLeadInteractionsPage;
            _createLead = createLead;
            _updateLead = updateLead;
            _convertLeadToCustomer = convertLeadToCustomer;
            _getOpportunitiesPage = getOpportunitiesPage;
            _getOpportunityForEdit = getOpportunityForEdit;
            _getOpportunityInteractionsPage = getOpportunityInteractionsPage;
            _createOpportunity = createOpportunity;
            _updateOpportunity = updateOpportunity;
            _getCustomerSegmentsPage = getCustomerSegmentsPage;
            _getCustomerSegmentForEdit = getCustomerSegmentForEdit;
            _getInvoicesPage = getInvoicesPage;
            _getInvoiceForEdit = getInvoiceForEdit;
            _createCustomerSegment = createCustomerSegment;
            _updateCustomerSegment = updateCustomerSegment;
            _updateInvoice = updateInvoice;
            _transitionInvoiceStatus = transitionInvoiceStatus;
            _createInteraction = createInteraction;
            _createConsent = createConsent;
            _assignCustomerSegment = assignCustomerSegment;
            _removeCustomerSegmentMembership = removeCustomerSegmentMembership;
            _referenceData = referenceData;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            return View("Overview", MapSummary(summary));
        }

        [HttpGet]
        public async Task<IActionResult> Customers(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _getCustomersPage.HandleAsync(page, pageSize, q, ct).ConfigureAwait(false);
            var vm = new CustomersListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Items = items.Select(x => new CustomerListItemVm
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    Phone = x.Phone,
                    CompanyName = x.CompanyName,
                    SegmentCount = x.SegmentCount,
                    OpportunityCount = x.OpportunityCount,
                    CreatedAtUtc = x.CreatedAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCustomer(CancellationToken ct = default)
        {
            var vm = new CustomerEditVm();
            vm.Addresses.Add(new CustomerAddressVm
            {
                Country = "DE",
                IsDefaultBilling = true,
                IsDefaultShipping = true
            });

            await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
            return View(vm);
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
                    Notes = vm.Notes,
                    Addresses = vm.Addresses.Select(MapCustomerAddress).ToList()
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Customer created.";
                return RedirectOrHtmx(nameof(EditCustomer), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCustomerEditor(vm, "CreateCustomer");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditCustomer(Guid id, CancellationToken ct = default)
        {
            var dto = await _getCustomerForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction(nameof(Customers));
            }

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
                Notes = dto.Notes,
                EffectiveFirstName = dto.EffectiveFirstName,
                EffectiveLastName = dto.EffectiveLastName,
                EffectiveEmail = dto.EffectiveEmail,
                EffectivePhone = dto.EffectivePhone,
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
                NewConsent = new ConsentCreateVm { CustomerId = dto.Id, GrantedAtUtc = DateTime.UtcNow },
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
                    Notes = vm.Notes,
                    Addresses = vm.Addresses.Select(MapCustomerAddress).ToList()
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Customer updated.";
                return RedirectOrHtmx(nameof(EditCustomer), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the customer and try again.";
                return RedirectToAction(nameof(EditCustomer), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderCustomerEditor(vm, "EditCustomer");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Invoices(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _getInvoicesPage.HandleAsync(page, pageSize, q, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            return View(new InvoicesListVm
            {
                Summary = MapSummary(summary),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Items = items.Select(x => new InvoiceListItemVm
                {
                    Id = x.Id,
                    BusinessId = x.BusinessId,
                    CustomerId = x.CustomerId,
                    CustomerDisplayName = x.CustomerDisplayName,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    PaymentId = x.PaymentId,
                    PaymentSummary = x.PaymentSummary,
                    Status = x.Status,
                    Currency = x.Currency,
                    TotalGrossMinor = x.TotalGrossMinor,
                    RefundedAmountMinor = x.RefundedAmountMinor,
                    SettledAmountMinor = x.SettledAmountMinor,
                    BalanceMinor = x.BalanceMinor,
                    DueDateUtc = x.DueDateUtc,
                    PaidAtUtc = x.PaidAtUtc,
                    RowVersion = x.RowVersion
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditInvoice(Guid id, CancellationToken ct = default)
        {
            var dto = await _getInvoiceForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(Invoices));
            }

            var vm = new InvoiceEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                BusinessId = dto.BusinessId,
                CustomerId = dto.CustomerId,
                CustomerDisplayName = dto.CustomerDisplayName,
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
                PaidAtUtc = dto.PaidAtUtc
            };

            await PopulateInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderInvoiceEditor(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInvoice(InvoiceEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderInvoiceEditor(vm);
            }

            try
            {
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

                TempData["Success"] = "Invoice updated.";
                return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the invoice and try again.";
                return RedirectToAction(nameof(EditInvoice), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateInvoiceOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderInvoiceEditor(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransitionInvoiceStatus(InvoiceStatusTransitionVm vm, CancellationToken ct = default)
        {
            try
            {
                await _transitionInvoiceStatus.HandleAsync(new InvoiceStatusTransitionDto
                {
                    Id = vm.Id,
                    RowVersion = vm.RowVersion,
                    TargetStatus = vm.TargetStatus,
                    PaidAtUtc = vm.PaidAtUtc
                }, ct).ConfigureAwait(false);

                TempData["Success"] = $"Invoice marked as {vm.TargetStatus}.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the invoice and try again.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectOrHtmx(nameof(EditInvoice), new { id = vm.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Leads(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _getLeadsPage.HandleAsync(page, pageSize, q, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var vm = new LeadsListVm
            {
                Summary = MapSummary(summary),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Items = items.Select(x => new LeadListItemVm
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    FullName = (x.FirstName + " " + x.LastName).Trim(),
                    CompanyName = x.CompanyName,
                    Email = x.Email,
                    Phone = x.Phone,
                    Status = x.Status,
                    InteractionCount = x.InteractionCount,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
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

                TempData["Success"] = "Lead created.";
                return RedirectOrHtmx(nameof(EditLead), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderLeadEditor(vm, nameof(CreateLead));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditLead(Guid id, CancellationToken ct = default)
        {
            var dto = await _getLeadForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Lead not found.";
                return RedirectToAction(nameof(Leads));
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
                CustomerId = dto.CustomerId,
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

                TempData["Success"] = "Lead updated.";
                return RedirectOrHtmx(nameof(EditLead), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the lead and try again.";
                return RedirectToAction(nameof(EditLead), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderLeadEditor(vm, nameof(EditLead));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Opportunities(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _getOpportunitiesPage.HandleAsync(page, pageSize, q, ct).ConfigureAwait(false);
            var vm = new OpportunitiesListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Items = items.Select(x => new OpportunityListItemVm
                {
                    Id = x.Id,
                    CustomerDisplayName = x.CustomerDisplayName,
                    Title = x.Title,
                    EstimatedValueMinor = x.EstimatedValueMinor,
                    Stage = x.Stage,
                    ExpectedCloseDateUtc = x.ExpectedCloseDateUtc,
                    ItemCount = x.ItemCount,
                    InteractionCount = x.InteractionCount,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOpportunity(CancellationToken ct = default)
        {
            var vm = new OpportunityEditVm();
            EnsureOpportunityLineRows(vm);
            await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
            return RenderOpportunityEditor(vm, nameof(CreateOpportunity));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOpportunity(OpportunityEditVm vm, CancellationToken ct = default)
        {
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

                TempData["Success"] = "Opportunity created.";
                return RedirectOrHtmx(nameof(EditOpportunity), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderOpportunityEditor(vm, nameof(CreateOpportunity));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditOpportunity(Guid id, CancellationToken ct = default)
        {
            var dto = await _getOpportunityForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Opportunity not found.";
                return RedirectToAction(nameof(Opportunities));
            }

            var vm = new OpportunityEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                CustomerId = dto.CustomerId,
                Title = dto.Title,
                EstimatedValueMinor = dto.EstimatedValueMinor,
                Stage = dto.Stage,
                ExpectedCloseDateUtc = dto.ExpectedCloseDateUtc,
                AssignedToUserId = dto.AssignedToUserId,
                CustomerDisplayName = dto.CustomerDisplayName,
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

                TempData["Success"] = "Opportunity updated.";
                return RedirectOrHtmx(nameof(EditOpportunity), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the opportunity and try again.";
                return RedirectToAction(nameof(EditOpportunity), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return RenderOpportunityEditor(vm, nameof(EditOpportunity));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Segments(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _getCustomerSegmentsPage.HandleAsync(page, pageSize, q, ct).ConfigureAwait(false);
            var summary = await _getCrmSummary.HandleAsync(ct).ConfigureAwait(false);
            var vm = new CustomerSegmentsListVm
            {
                Summary = MapSummary(summary),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Items = items.Select(x => new CustomerSegmentListItemVm
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    MemberCount = x.MemberCount,
                    RowVersion = x.RowVersion
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertLead(ConvertLeadVm vm, CancellationToken ct = default)
        {
            try
            {
                var customerId = await _convertLeadToCustomer.HandleAsync(new ConvertLeadToCustomerDto
                {
                    LeadId = vm.LeadId,
                    RowVersion = vm.RowVersion,
                    UserId = vm.UserId,
                    CopyNotesToCustomer = vm.CopyNotesToCustomer
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Lead converted to customer.";
                return RedirectToAction(nameof(EditCustomer), new { id = customerId });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the lead and try again.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(EditLead), new { id = vm.LeadId });
        }

        [HttpGet]
        public IActionResult CreateSegment() => View(new CustomerSegmentEditVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSegment(CustomerSegmentEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                var id = await _createCustomerSegment.HandleAsync(new CustomerSegmentEditDto
                {
                    Name = vm.Name,
                    Description = vm.Description
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Segment created.";
                return RedirectToAction(nameof(EditSegment), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditSegment(Guid id, CancellationToken ct = default)
        {
            var dto = await _getCustomerSegmentForEdit.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                TempData["Error"] = "Segment not found.";
                return RedirectToAction(nameof(Segments));
            }

            return View(new CustomerSegmentEditVm
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion,
                Name = dto.Name,
                Description = dto.Description
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSegment(CustomerSegmentEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
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

                TempData["Success"] = "Segment updated.";
                return RedirectToAction(nameof(EditSegment), new { id = vm.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Concurrency conflict. Reload the segment and try again.";
                return RedirectToAction(nameof(EditSegment), new { id = vm.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> CustomerInteractions(Guid customerId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
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
            try
            {
                await _createInteraction.HandleAsync(MapInteraction(vm), ct).ConfigureAwait(false);
                TempData["Success"] = "Interaction added.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return await CustomerInteractions(vm.CustomerId ?? Guid.Empty, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> LeadInteractions(Guid leadId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
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
            try
            {
                await _createInteraction.HandleAsync(MapInteraction(vm), ct).ConfigureAwait(false);
                TempData["Success"] = "Interaction added.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return await LeadInteractions(vm.LeadId ?? Guid.Empty, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> OpportunityInteractions(Guid opportunityId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
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
            try
            {
                await _createInteraction.HandleAsync(MapInteraction(vm), ct).ConfigureAwait(false);
                TempData["Success"] = "Interaction added.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return await OpportunityInteractions(vm.OpportunityId ?? Guid.Empty, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerConsents(Guid customerId, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
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

                TempData["Success"] = "Consent record added.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return await CustomerConsents(vm.CustomerId, ct: ct).ConfigureAwait(false);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerSegmentMemberships(Guid customerId, CancellationToken ct = default)
        {
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
            try
            {
                await _assignCustomerSegment.HandleAsync(new AssignCustomerSegmentDto
                {
                    CustomerId = vm.CustomerId,
                    CustomerSegmentId = vm.CustomerSegmentId
                }, ct).ConfigureAwait(false);

                TempData["Success"] = "Segment assigned.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return await CustomerSegmentMemberships(vm.CustomerId, ct).ConfigureAwait(false);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCustomerSegmentMembership(Guid customerId, Guid membershipId, CancellationToken ct = default)
        {
            try
            {
                await _removeCustomerSegmentMembership.HandleAsync(membershipId, ct).ConfigureAwait(false);
                TempData["Success"] = "Segment removed.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
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

        private async Task PopulateInvoiceOptionsAsync(InvoiceEditVm vm, CancellationToken ct)
        {
            vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
            vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.PaymentOptions = await _referenceData.GetPaymentOptionsAsync(vm.PaymentId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private static void EnsureCustomerAddressRows(CustomerEditVm vm)
        {
            vm.Addresses ??= new List<CustomerAddressVm>();
            if (vm.Addresses.Count == 0)
            {
                vm.Addresses.Add(new CustomerAddressVm
                {
                    Country = "DE",
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

        private static CrmSummaryVm MapSummary(CrmSummaryDto dto)
        {
            return new CrmSummaryVm
            {
                CustomerCount = dto.CustomerCount,
                LeadCount = dto.LeadCount,
                QualifiedLeadCount = dto.QualifiedLeadCount,
                OpenOpportunityCount = dto.OpenOpportunityCount,
                OpenPipelineMinor = dto.OpenPipelineMinor,
                SegmentCount = dto.SegmentCount,
                RecentInteractionCount = dto.RecentInteractionCount
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

        private IActionResult RenderOpportunityEditor(OpportunityEditVm vm, string actionName)
        {
            if (IsHtmxRequest())
            {
                ViewData["FormAction"] = actionName;
                return PartialView("~/Views/Crm/_OpportunityEditorShell.cshtml", vm);
            }

            return actionName == nameof(CreateOpportunity) ? View("CreateOpportunity", vm) : View("EditOpportunity", vm);
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
