using Darwin.Application.Billing.Commands;
using Darwin.Application.Billing.DTOs;
using Darwin.Application.Billing.Queries;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.Controllers.Admin;
using Darwin.WebAdmin.Services.Admin;
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
        private readonly GetPaymentForEditHandler _getPaymentForEdit;
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

        public BillingController(
            GetPaymentsPageHandler getPaymentsPage,
            GetPaymentForEditHandler getPaymentForEdit,
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
            AdminReferenceDataService referenceData)
        {
            _getPaymentsPage = getPaymentsPage;
            _getPaymentForEdit = getPaymentForEdit;
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
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Payments));

        [HttpGet]
        public async Task<IActionResult> Payments(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);

            var items = new List<PaymentListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getPaymentsPage.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
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
                    PaidAtUtc = x.PaidAtUtc,
                    RowVersion = x.RowVersion
                }).ToList();
                total = result.Total;
            }

            var vm = new PaymentsListVm
            {
                BusinessId = businessId,
                BusinessOptions = await _referenceData.GetBusinessOptionsAsync(businessId, ct).ConfigureAwait(false),
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePayment(Guid? businessId = null, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var vm = new PaymentEditVm
            {
                BusinessId = businessId ?? Guid.Empty,
                Currency = "EUR",
                Status = PaymentStatus.Pending
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
                PaidAtUtc = dto.PaidAtUtc
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

        [HttpGet]
        public async Task<IActionResult> FinancialAccounts(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<FinancialAccountListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getAccountsPage.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFinancialAccount(FinancialAccountEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
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
                return RedirectToAction(nameof(EditFinancialAccount), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinancialAccount(FinancialAccountEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                vm.BusinessOptions = await _referenceData.GetBusinessOptionsAsync(vm.BusinessId, ct).ConfigureAwait(false);
                return View(vm);
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
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Expenses(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<ExpenseListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getExpensesPage.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExpense(ExpenseEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
                return RedirectToAction(nameof(EditExpense), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExpense(ExpenseEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                await PopulateExpenseOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> JournalEntries(Guid? businessId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            businessId = await _referenceData.ResolveBusinessIdAsync(businessId, ct).ConfigureAwait(false);
            var items = new List<JournalEntryListItemVm>();
            var total = 0;
            if (businessId.HasValue)
            {
                var result = await _getJournalEntriesPage.HandleAsync(businessId.Value, page, pageSize, ct).ConfigureAwait(false);
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJournalEntry(JournalEntryEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
                return RedirectToAction(nameof(EditJournalEntry), new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJournalEntry(JournalEntryEditVm vm, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
            {
                EnsureJournalEntryRows(vm);
                await PopulateJournalEntryOptionsAsync(vm, ct).ConfigureAwait(false);
                return View(vm);
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
                return View(vm);
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
