using Darwin.Application.CRM.Commands;
using Darwin.Application.CRM.DTOs;
using Darwin.Application.CRM.Queries;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
using Darwin.WebAdmin.ViewModels.CRM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Darwin.WebAdmin.Controllers.Admin.CRM
{
    /// <summary>
    /// Admin CRM controller for customers, leads, and opportunities.
    /// </summary>
    public sealed class CrmController : AdminBaseController
    {
        private readonly GetCustomersPageHandler _getCustomersPage;
        private readonly GetCustomerForEditHandler _getCustomerForEdit;
        private readonly CreateCustomerHandler _createCustomer;
        private readonly UpdateCustomerHandler _updateCustomer;
        private readonly GetLeadsPageHandler _getLeadsPage;
        private readonly GetLeadForEditHandler _getLeadForEdit;
        private readonly CreateLeadHandler _createLead;
        private readonly UpdateLeadHandler _updateLead;
        private readonly GetOpportunitiesPageHandler _getOpportunitiesPage;
        private readonly GetOpportunityForEditHandler _getOpportunityForEdit;
        private readonly CreateOpportunityHandler _createOpportunity;
        private readonly UpdateOpportunityHandler _updateOpportunity;
        private readonly AdminReferenceDataService _referenceData;

        public CrmController(
            GetCustomersPageHandler getCustomersPage,
            GetCustomerForEditHandler getCustomerForEdit,
            CreateCustomerHandler createCustomer,
            UpdateCustomerHandler updateCustomer,
            GetLeadsPageHandler getLeadsPage,
            GetLeadForEditHandler getLeadForEdit,
            CreateLeadHandler createLead,
            UpdateLeadHandler updateLead,
            GetOpportunitiesPageHandler getOpportunitiesPage,
            GetOpportunityForEditHandler getOpportunityForEdit,
            CreateOpportunityHandler createOpportunity,
            UpdateOpportunityHandler updateOpportunity,
            AdminReferenceDataService referenceData)
        {
            _getCustomersPage = getCustomersPage;
            _getCustomerForEdit = getCustomerForEdit;
            _createCustomer = createCustomer;
            _updateCustomer = updateCustomer;
            _getLeadsPage = getLeadsPage;
            _getLeadForEdit = getLeadForEdit;
            _createLead = createLead;
            _updateLead = updateLead;
            _getOpportunitiesPage = getOpportunitiesPage;
            _getOpportunityForEdit = getOpportunityForEdit;
            _createOpportunity = createOpportunity;
            _updateOpportunity = updateOpportunity;
            _referenceData = referenceData;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Customers));

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
                return View(vm);
            }

            var dto = new CustomerCreateDto
            {
                UserId = vm.UserId,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                Phone = vm.Phone,
                CompanyName = vm.CompanyName,
                Notes = vm.Notes,
                Addresses = vm.Addresses.Select(MapCustomerAddress).ToList()
            };

            try
            {
                var id = await _createCustomer.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Customer created.";
                return RedirectToAction(nameof(EditCustomer), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
                }).ToList()
            };

            EnsureCustomerAddressRows(vm);
            await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(CustomerEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureCustomerAddressRows(vm);
                await PopulateCustomerOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new CustomerEditDto
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
            };

            try
            {
                await _updateCustomer.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Customer updated.";
                return RedirectToAction(nameof(EditCustomer), new { id = vm.Id });
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
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Leads(int page = 1, int pageSize = 20, string? q = null, CancellationToken ct = default)
        {
            var (items, total) = await _getLeadsPage.HandleAsync(page, pageSize, q, ct).ConfigureAwait(false);
            var vm = new LeadsListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q ?? string.Empty,
                Items = items.Select(x => new LeadListItemVm
                {
                    Id = x.Id,
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLead(LeadEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new LeadCreateDto
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
            };

            try
            {
                var id = await _createLead.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Lead created.";
                return RedirectToAction(nameof(EditLead), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
                CustomerId = dto.CustomerId
            };

            await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLead(LeadEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLeadOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new LeadEditDto
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
            };

            try
            {
                await _updateLead.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Lead updated.";
                return RedirectToAction(nameof(EditLead), new { id = vm.Id });
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
                return View(vm);
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOpportunity(OpportunityEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new OpportunityCreateDto
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
            };

            try
            {
                var id = await _createOpportunity.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Opportunity created.";
                return RedirectToAction(nameof(EditOpportunity), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
                }).ToList()
            };

            EnsureOpportunityLineRows(vm);
            await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOpportunity(OpportunityEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureOpportunityLineRows(vm);
                await PopulateOpportunityOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
            }

            var dto = new OpportunityEditDto
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
            };

            try
            {
                await _updateOpportunity.HandleAsync(dto, ct).ConfigureAwait(false);
                TempData["Success"] = "Opportunity updated.";
                return RedirectToAction(nameof(EditOpportunity), new { id = vm.Id });
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
                return View(vm);
            }
        }

        private async Task PopulateCustomerOptionsAsync(CustomerEditVm vm, CancellationToken ct)
        {
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.UserId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private async Task PopulateLeadOptionsAsync(LeadEditVm vm, CancellationToken ct)
        {
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.AssignedToUserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: true, ct).ConfigureAwait(false);
        }

        private async Task PopulateOpportunityOptionsAsync(OpportunityEditVm vm, CancellationToken ct)
        {
            vm.CustomerOptions = await _referenceData.GetCustomerOptionsAsync(vm.CustomerId, includeEmpty: false, ct).ConfigureAwait(false);
            vm.UserOptions = await _referenceData.GetUserOptionsAsync(vm.AssignedToUserId, includeEmpty: true, ct).ConfigureAwait(false);
            vm.VariantOptions = await _referenceData.GetVariantOptionsAsync(null, ct).ConfigureAwait(false);
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
    }
}
