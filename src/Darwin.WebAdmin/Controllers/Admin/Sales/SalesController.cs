using Darwin.Application.Sales.Commands;
using Darwin.Application.Sales.DTOs;
using Darwin.Application.Sales.Queries;
using Darwin.Domain.Enums;
using Darwin.WebAdmin.ViewModels.Sales;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Darwin.WebAdmin.Controllers.Admin.Sales
{
    /// <summary>
    /// Sales workspace over current order/invoice projections and internal quote lifecycle.
    /// </summary>
    public sealed class SalesController : AdminBaseController
    {
        private readonly GetSalesOverviewHandler _getOverview;
        private readonly GetSalesOrdersPageHandler _getOrdersPage;
        private readonly GetSalesInvoicesPageHandler _getInvoicesPage;
        private readonly GetSalesOrderDocumentHandler _getOrderDocument;
        private readonly GetSalesInvoiceDocumentHandler _getInvoiceDocument;
        private readonly GetSalesQuotesPageHandler _getQuotesPage;
        private readonly GetSalesQuoteDetailHandler _getQuoteDetail;
        private readonly CreateSalesQuoteHandler _createQuote;
        private readonly UpdateSalesQuoteHandler _updateQuote;
        private readonly SendSalesQuoteHandler _sendQuote;
        private readonly UpdateSalesQuoteLifecycleHandler _updateQuoteLifecycle;
        private readonly ConvertSalesQuoteToOrderHandler _convertQuoteToOrder;
        private readonly GetDeliveryNotesPageHandler _getDeliveryNotesPage;
        private readonly GetDeliveryNoteDetailHandler _getDeliveryNoteDetail;
        private readonly CreateDeliveryNoteFromShipmentHandler _createDeliveryNoteFromShipment;
        private readonly UpdateDeliveryNoteLifecycleHandler _updateDeliveryNoteLifecycle;
        private readonly GetReturnOrdersPageHandler _getReturnOrdersPage;
        private readonly GetReturnOrderDetailHandler _getReturnOrderDetail;
        private readonly CreateReturnOrderHandler _createReturnOrder;
        private readonly UpdateReturnOrderLifecycleHandler _updateReturnOrderLifecycle;
        private readonly GetCreditNotesPageHandler _getCreditNotesPage;
        private readonly GetCreditNoteDetailHandler _getCreditNoteDetail;
        private readonly GetInvoiceLinesForCreditNoteHandler _getInvoiceLinesForCreditNote;
        private readonly GetCreditNoteSourceExportHandler _getCreditNoteSourceExport;
        private readonly CreateCreditNoteHandler _createCreditNote;
        private readonly UpdateCreditNoteLifecycleHandler _updateCreditNoteLifecycle;

        public SalesController(
            GetSalesOverviewHandler getOverview,
            GetSalesOrdersPageHandler getOrdersPage,
            GetSalesInvoicesPageHandler getInvoicesPage,
            GetSalesOrderDocumentHandler getOrderDocument,
            GetSalesInvoiceDocumentHandler getInvoiceDocument,
            GetSalesQuotesPageHandler getQuotesPage,
            GetSalesQuoteDetailHandler getQuoteDetail,
            CreateSalesQuoteHandler createQuote,
            UpdateSalesQuoteHandler updateQuote,
            SendSalesQuoteHandler sendQuote,
            UpdateSalesQuoteLifecycleHandler updateQuoteLifecycle,
            ConvertSalesQuoteToOrderHandler convertQuoteToOrder,
            GetDeliveryNotesPageHandler getDeliveryNotesPage,
            GetDeliveryNoteDetailHandler getDeliveryNoteDetail,
            CreateDeliveryNoteFromShipmentHandler createDeliveryNoteFromShipment,
            UpdateDeliveryNoteLifecycleHandler updateDeliveryNoteLifecycle,
            GetReturnOrdersPageHandler getReturnOrdersPage,
            GetReturnOrderDetailHandler getReturnOrderDetail,
            CreateReturnOrderHandler createReturnOrder,
            UpdateReturnOrderLifecycleHandler updateReturnOrderLifecycle,
            GetCreditNotesPageHandler getCreditNotesPage,
            GetCreditNoteDetailHandler getCreditNoteDetail,
            GetInvoiceLinesForCreditNoteHandler getInvoiceLinesForCreditNote,
            GetCreditNoteSourceExportHandler getCreditNoteSourceExport,
            CreateCreditNoteHandler createCreditNote,
            UpdateCreditNoteLifecycleHandler updateCreditNoteLifecycle)
        {
            _getOverview = getOverview ?? throw new ArgumentNullException(nameof(getOverview));
            _getOrdersPage = getOrdersPage ?? throw new ArgumentNullException(nameof(getOrdersPage));
            _getInvoicesPage = getInvoicesPage ?? throw new ArgumentNullException(nameof(getInvoicesPage));
            _getOrderDocument = getOrderDocument ?? throw new ArgumentNullException(nameof(getOrderDocument));
            _getInvoiceDocument = getInvoiceDocument ?? throw new ArgumentNullException(nameof(getInvoiceDocument));
            _getQuotesPage = getQuotesPage ?? throw new ArgumentNullException(nameof(getQuotesPage));
            _getQuoteDetail = getQuoteDetail ?? throw new ArgumentNullException(nameof(getQuoteDetail));
            _createQuote = createQuote ?? throw new ArgumentNullException(nameof(createQuote));
            _updateQuote = updateQuote ?? throw new ArgumentNullException(nameof(updateQuote));
            _sendQuote = sendQuote ?? throw new ArgumentNullException(nameof(sendQuote));
            _updateQuoteLifecycle = updateQuoteLifecycle ?? throw new ArgumentNullException(nameof(updateQuoteLifecycle));
            _convertQuoteToOrder = convertQuoteToOrder ?? throw new ArgumentNullException(nameof(convertQuoteToOrder));
            _getDeliveryNotesPage = getDeliveryNotesPage ?? throw new ArgumentNullException(nameof(getDeliveryNotesPage));
            _getDeliveryNoteDetail = getDeliveryNoteDetail ?? throw new ArgumentNullException(nameof(getDeliveryNoteDetail));
            _createDeliveryNoteFromShipment = createDeliveryNoteFromShipment ?? throw new ArgumentNullException(nameof(createDeliveryNoteFromShipment));
            _updateDeliveryNoteLifecycle = updateDeliveryNoteLifecycle ?? throw new ArgumentNullException(nameof(updateDeliveryNoteLifecycle));
            _getReturnOrdersPage = getReturnOrdersPage ?? throw new ArgumentNullException(nameof(getReturnOrdersPage));
            _getReturnOrderDetail = getReturnOrderDetail ?? throw new ArgumentNullException(nameof(getReturnOrderDetail));
            _createReturnOrder = createReturnOrder ?? throw new ArgumentNullException(nameof(createReturnOrder));
            _updateReturnOrderLifecycle = updateReturnOrderLifecycle ?? throw new ArgumentNullException(nameof(updateReturnOrderLifecycle));
            _getCreditNotesPage = getCreditNotesPage ?? throw new ArgumentNullException(nameof(getCreditNotesPage));
            _getCreditNoteDetail = getCreditNoteDetail ?? throw new ArgumentNullException(nameof(getCreditNoteDetail));
            _getInvoiceLinesForCreditNote = getInvoiceLinesForCreditNote ?? throw new ArgumentNullException(nameof(getInvoiceLinesForCreditNote));
            _getCreditNoteSourceExport = getCreditNoteSourceExport ?? throw new ArgumentNullException(nameof(getCreditNoteSourceExport));
            _createCreditNote = createCreditNote ?? throw new ArgumentNullException(nameof(createCreditNote));
            _updateCreditNoteLifecycle = updateCreditNoteLifecycle ?? throw new ArgumentNullException(nameof(updateCreditNoteLifecycle));
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var dto = await _getOverview.HandleAsync(ct).ConfigureAwait(false);
            return RenderOverviewWorkspace(new SalesOverviewVm
            {
                OrderCount = dto.OrderCount,
                QuoteCount = dto.QuoteCount,
                QuoteAttentionCount = dto.QuoteAttentionCount,
                ReturnOrderCount = dto.ReturnOrderCount,
                ReturnOrderAttentionCount = dto.ReturnOrderAttentionCount,
                GrossTotalMinor = dto.GrossTotalMinor,
                TaxTotalMinor = dto.TaxTotalMinor,
                InvoiceCount = dto.InvoiceCount,
                OpenInvoiceBalanceMinor = dto.OpenInvoiceBalanceMinor,
                PaymentAttentionCount = dto.PaymentAttentionCount,
                FulfillmentAttentionCount = dto.FulfillmentAttentionCount,
                InvoiceAttentionCount = dto.InvoiceAttentionCount,
                Currency = dto.Currency,
                ChannelBreakdown = dto.ChannelBreakdown.Select(x => new SalesChannelBreakdownVm
                {
                    SalesChannel = x.SalesChannel,
                    OrderCount = x.OrderCount,
                    GrossTotalMinor = x.GrossTotalMinor
                }).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Quotes(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            SalesQuoteDocumentFilter filter = SalesQuoteDocumentFilter.All,
            Guid? businessId = null,
            Guid? customerId = null,
            Guid? opportunityId = null,
            DateTime? validUntilFromUtc = null,
            DateTime? validUntilToUtc = null,
            CancellationToken ct = default)
        {
            businessId = NormalizeGuid(businessId);
            customerId = NormalizeGuid(customerId);
            opportunityId = NormalizeGuid(opportunityId);
            var (items, total) = await _getQuotesPage
                .HandleAsync(page, pageSize, q, filter, businessId, customerId, opportunityId, validUntilFromUtc, validUntilToUtc, ct)
                .ConfigureAwait(false);

            return RenderQuotesWorkspace(new SalesQuotesListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q?.Trim() ?? string.Empty,
                Filter = filter,
                BusinessId = businessId,
                CustomerId = customerId,
                OpportunityId = opportunityId,
                ValidUntilFromUtc = validUntilFromUtc,
                ValidUntilToUtc = validUntilToUtc,
                FilterItems = BuildSalesQuoteFilterItems(filter),
                Items = items
            });
        }

        [HttpGet]
        public IActionResult CreateQuote()
            => RenderQuoteEditor(new SalesQuoteEditorVm { IsCreate = true, Quote = new SalesQuoteEditDto { Currency = "EUR" } });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuote(SalesQuoteEditDto quote, CancellationToken ct = default)
        {
            try
            {
                var id = await _createQuote.HandleAsync(quote, ct).ConfigureAwait(false);
                SetSuccessMessage("SalesQuoteCreated");
                return RedirectOrHtmx(nameof(Quote), new { id });
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException)
            {
                SetErrorMessage(ex.Message);
                return RenderQuoteEditor(new SalesQuoteEditorVm { IsCreate = true, Quote = quote });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditQuote(Guid id, CancellationToken ct = default)
        {
            var detail = await _getQuoteDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (detail is null)
            {
                SetErrorMessage("SalesQuoteNotFound");
                return RedirectOrHtmx(nameof(Quotes), new { });
            }

            return RenderQuoteEditor(new SalesQuoteEditorVm { IsCreate = false, Quote = MapQuoteEdit(detail) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuote(SalesQuoteEditDto quote, CancellationToken ct = default)
        {
            try
            {
                await _updateQuote.HandleAsync(quote, ct).ConfigureAwait(false);
                SetSuccessMessage("SalesQuoteUpdated");
                return RedirectOrHtmx(nameof(Quote), new { id = quote.Id });
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                SetErrorMessage(ex.Message);
                return RenderQuoteEditor(new SalesQuoteEditorVm { IsCreate = false, Quote = quote });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Quote(Guid id, CancellationToken ct = default)
        {
            var detail = await _getQuoteDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (detail is null)
            {
                SetErrorMessage("SalesQuoteNotFound");
                return RedirectOrHtmx(nameof(Quotes), new { });
            }

            return RenderQuoteWorkspace(new SalesQuoteDetailVm
            {
                Document = detail,
                Convert = new SalesQuoteConvertDto { Id = detail.Id, RowVersion = detail.RowVersion },
                CreateOrder = new SalesQuoteCreateOrderDto { Id = detail.Id, RowVersion = detail.RowVersion }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendQuote(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteQuoteLifecycleAsync(dto.Id, () => _sendQuote.HandleAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptQuote(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteQuoteLifecycleAsync(dto.Id, () => _updateQuoteLifecycle.AcceptAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectQuote(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteQuoteLifecycleAsync(dto.Id, () => _updateQuoteLifecycle.RejectAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpireQuote(SalesQuoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteQuoteLifecycleAsync(dto.Id, () => _updateQuoteLifecycle.ExpireAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertQuote(SalesQuoteConvertDto dto, CancellationToken ct = default)
            => await ExecuteQuoteLifecycleAsync(dto.Id, () => _updateQuoteLifecycle.ConvertAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrderFromQuote(SalesQuoteCreateOrderDto dto, CancellationToken ct = default)
        {
            try
            {
                var orderId = await _convertQuoteToOrder.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("SalesQuoteOrderCreated");
                return RedirectOrHtmx(nameof(Order), new { id = orderId });
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                SetErrorMessage(ex.Message);
                return RedirectOrHtmx(nameof(Quote), new { id = dto.Id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Orders(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            SalesOrderDocumentFilter filter = SalesOrderDocumentFilter.All,
            SalesChannel? salesChannel = null,
            DateTime? orderedFromUtc = null,
            DateTime? orderedToUtc = null,
            Guid? businessId = null,
            Guid? customerId = null,
            CancellationToken ct = default)
        {
            businessId = NormalizeGuid(businessId);
            customerId = NormalizeGuid(customerId);
            var (items, total) = await _getOrdersPage
                .HandleAsync(page, pageSize, q, filter, salesChannel, orderedFromUtc, orderedToUtc, businessId, customerId, ct)
                .ConfigureAwait(false);

            return RenderOrdersWorkspace(new SalesOrdersListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q?.Trim() ?? string.Empty,
                Filter = filter,
                SalesChannel = salesChannel,
                OrderedFromUtc = orderedFromUtc,
                OrderedToUtc = orderedToUtc,
                BusinessId = businessId,
                CustomerId = customerId,
                FilterItems = BuildSalesOrderFilterItems(filter),
                SalesChannelItems = BuildSalesChannelItems(salesChannel),
                Items = items.Select(MapOrderListItem).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> DeliveryNotes(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            DeliveryNoteDocumentFilter filter = DeliveryNoteDocumentFilter.All,
            Guid? businessId = null,
            Guid? customerId = null,
            DateTime? issuedFromUtc = null,
            DateTime? issuedToUtc = null,
            CancellationToken ct = default)
        {
            businessId = NormalizeGuid(businessId);
            customerId = NormalizeGuid(customerId);
            var (items, total) = await _getDeliveryNotesPage
                .HandleAsync(page, pageSize, q, filter, businessId, customerId, issuedFromUtc, issuedToUtc, ct)
                .ConfigureAwait(false);

            return RenderDeliveryNotesWorkspace(new DeliveryNotesListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q?.Trim() ?? string.Empty,
                Filter = filter,
                BusinessId = businessId,
                CustomerId = customerId,
                IssuedFromUtc = issuedFromUtc,
                IssuedToUtc = issuedToUtc,
                FilterItems = BuildDeliveryNoteFilterItems(filter),
                Items = items
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDeliveryNoteFromShipment(DeliveryNoteCreateFromShipmentDto dto, CancellationToken ct = default)
        {
            try
            {
                var id = await _createDeliveryNoteFromShipment.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("DeliveryNoteCreated");
                return RedirectOrHtmx(nameof(DeliveryNote), new { id });
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                SetErrorMessage(ex.Message);
                return RedirectOrHtmx(nameof(DeliveryNotes), new { });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DeliveryNote(Guid id, CancellationToken ct = default)
        {
            var detail = await _getDeliveryNoteDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (detail is null)
            {
                SetErrorMessage("DeliveryNoteNotFound");
                return RedirectOrHtmx(nameof(DeliveryNotes), new { });
            }

            return RenderDeliveryNoteWorkspace(new DeliveryNoteDetailVm
            {
                Document = detail,
                Lifecycle = new DeliveryNoteLifecycleDto { Id = detail.Id, RowVersion = detail.RowVersion }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrepareDeliveryNote(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteDeliveryNoteLifecycleAsync(dto.Id, () => _updateDeliveryNoteLifecycle.PrepareAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueDeliveryNote(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteDeliveryNoteLifecycleAsync(dto.Id, () => _updateDeliveryNoteLifecycle.IssueAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDeliveryNoteShipped(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteDeliveryNoteLifecycleAsync(dto.Id, () => _updateDeliveryNoteLifecycle.MarkShippedAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDeliveryNoteDelivered(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteDeliveryNoteLifecycleAsync(dto.Id, () => _updateDeliveryNoteLifecycle.MarkDeliveredAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelDeliveryNote(DeliveryNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteDeliveryNoteLifecycleAsync(dto.Id, () => _updateDeliveryNoteLifecycle.CancelAsync(dto, ct)).ConfigureAwait(false);

        [HttpGet]
        public async Task<IActionResult> ReturnOrders(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            ReturnOrderDocumentFilter filter = ReturnOrderDocumentFilter.All,
            Guid? businessId = null,
            Guid? customerId = null,
            DateTime? createdFromUtc = null,
            DateTime? createdToUtc = null,
            CancellationToken ct = default)
        {
            businessId = NormalizeGuid(businessId);
            customerId = NormalizeGuid(customerId);
            var (items, total) = await _getReturnOrdersPage
                .HandleAsync(page, pageSize, q, filter, businessId, customerId, createdFromUtc, createdToUtc, ct)
                .ConfigureAwait(false);

            return RenderReturnOrdersWorkspace(new ReturnOrdersListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q?.Trim() ?? string.Empty,
                Filter = filter,
                BusinessId = businessId,
                CustomerId = customerId,
                CreatedFromUtc = createdFromUtc,
                CreatedToUtc = createdToUtc,
                FilterItems = BuildReturnOrderFilterItems(filter),
                Items = items,
                Create = new ReturnOrderCreateDto
                {
                    Lines = new List<ReturnOrderCreateLineDto> { new() }
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReturnOrder(ReturnOrderCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                dto.Lines = dto.Lines.Where(x => x.OrderLineId != Guid.Empty && x.RequestedQuantity > 0).ToList();
                var id = await _createReturnOrder.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("ReturnOrderCreated");
                return RedirectOrHtmx(nameof(ReturnOrder), new { id });
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                SetErrorMessage(ex.Message);
                return RedirectOrHtmx(nameof(ReturnOrders), new { });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ReturnOrder(Guid id, CancellationToken ct = default)
        {
            var detail = await _getReturnOrderDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (detail is null)
            {
                SetErrorMessage("ReturnOrderNotFound");
                return RedirectOrHtmx(nameof(ReturnOrders), new { });
            }

            return RenderReturnOrderWorkspace(BuildReturnOrderDetailVm(detail));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReturnOrder(ReturnOrderApproveDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.ApproveAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturnOrder(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.RejectAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QueueReturnShipment(ReturnOrderQueueShipmentDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.QueueReturnShipmentAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveReturnOrder(ReturnOrderReceiveDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.ReceiveAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InspectReturnOrder(ReturnOrderInspectDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.InspectAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReturnOrderRefundReady(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.MarkRefundReadyAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkReturnOrderRefund(ReturnOrderLinkRefundDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.LinkRefundAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseReturnOrder(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.CloseAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReturnOrder(ReturnOrderLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteReturnOrderLifecycleAsync(dto.Id, () => _updateReturnOrderLifecycle.CancelAsync(dto, ct)).ConfigureAwait(false);

        [HttpGet]
        public async Task<IActionResult> CreditNotes(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            CreditNoteDocumentFilter filter = CreditNoteDocumentFilter.All,
            Guid? businessId = null,
            Guid? customerId = null,
            DateTime? issuedFromUtc = null,
            DateTime? issuedToUtc = null,
            CancellationToken ct = default)
        {
            businessId = NormalizeGuid(businessId);
            customerId = NormalizeGuid(customerId);
            var (items, total) = await _getCreditNotesPage
                .HandleAsync(page, pageSize, q, filter, businessId, customerId, issuedFromUtc, issuedToUtc, ct)
                .ConfigureAwait(false);

            return RenderCreditNotesWorkspace(new CreditNotesListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q?.Trim() ?? string.Empty,
                Filter = filter,
                BusinessId = businessId,
                CustomerId = customerId,
                IssuedFromUtc = issuedFromUtc,
                IssuedToUtc = issuedToUtc,
                FilterItems = BuildCreditNoteFilterItems(filter),
                Items = items,
                Create = new CreditNoteCreateDto
                {
                    Lines = new List<CreditNoteCreateLineDto> { new() }
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCreditNote(CreditNoteCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                if (dto.Lines.Count == 1 && dto.Lines[0].InvoiceLineId == Guid.Empty && dto.InvoiceId != Guid.Empty)
                {
                    dto.Lines = await _getInvoiceLinesForCreditNote.HandleAsync(dto.InvoiceId, ct).ConfigureAwait(false);
                }
                else
                {
                    dto.Lines = dto.Lines.Where(x => x.InvoiceLineId != Guid.Empty && x.CreditedQuantity > 0).ToList();
                }

                var id = await _createCreditNote.HandleAsync(dto, ct).ConfigureAwait(false);
                SetSuccessMessage("CreditNoteCreated");
                return RedirectOrHtmx(nameof(CreditNote), new { id });
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                SetErrorMessage(ex.Message);
                return RedirectOrHtmx(nameof(CreditNotes), new { });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreditNote(Guid id, CancellationToken ct = default)
        {
            var detail = await _getCreditNoteDetail.HandleAsync(id, ct).ConfigureAwait(false);
            if (detail is null)
            {
                SetErrorMessage("CreditNoteNotFound");
                return RedirectOrHtmx(nameof(CreditNotes), new { });
            }

            return RenderCreditNoteWorkspace(new CreditNoteDetailVm
            {
                Document = detail,
                Lifecycle = new CreditNoteLifecycleDto { Id = detail.Id, RowVersion = detail.RowVersion }
            });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCreditNoteSourceModel(Guid id, CancellationToken ct = default)
        {
            var export = await _getCreditNoteSourceExport.HandleAsync(id, ct).ConfigureAwait(false);
            if (export is null)
            {
                SetErrorMessage("CreditNoteSourceModelNotAvailable");
                return RedirectOrHtmx(nameof(CreditNote), new { id });
            }

            return File(System.Text.Encoding.UTF8.GetBytes(export.SourceModelJson), export.ContentType, export.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueCreditNote(CreditNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteCreditNoteLifecycleAsync(dto.Id, () => _updateCreditNoteLifecycle.IssueAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VoidCreditNote(CreditNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteCreditNoteLifecycleAsync(dto.Id, () => _updateCreditNoteLifecycle.VoidAsync(dto, ct)).ConfigureAwait(false);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelCreditNote(CreditNoteLifecycleDto dto, CancellationToken ct = default)
            => await ExecuteCreditNoteLifecycleAsync(dto.Id, () => _updateCreditNoteLifecycle.CancelAsync(dto, ct)).ConfigureAwait(false);

        [HttpGet]
        public async Task<IActionResult> Order(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("SalesOrderNotFound");
                return RedirectOrHtmx(nameof(Orders), new { });
            }

            var dto = await _getOrderDocument.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("SalesOrderNotFound");
                return RedirectOrHtmx(nameof(Orders), new { });
            }

            return RenderOrderWorkspace(new SalesOrderDetailVm { Document = dto });
        }

        [HttpGet]
        public async Task<IActionResult> Invoices(
            int page = 1,
            int pageSize = 20,
            string? q = null,
            SalesInvoiceDocumentFilter filter = SalesInvoiceDocumentFilter.All,
            DateTime? dateFromUtc = null,
            DateTime? dateToUtc = null,
            Guid? businessId = null,
            Guid? customerId = null,
            CancellationToken ct = default)
        {
            businessId = NormalizeGuid(businessId);
            customerId = NormalizeGuid(customerId);
            var (items, total) = await _getInvoicesPage
                .HandleAsync(page, pageSize, q, filter, dateFromUtc, dateToUtc, businessId, customerId, ct)
                .ConfigureAwait(false);

            return RenderInvoicesWorkspace(new SalesInvoicesListVm
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Query = q?.Trim() ?? string.Empty,
                Filter = filter,
                DateFromUtc = dateFromUtc,
                DateToUtc = dateToUtc,
                BusinessId = businessId,
                CustomerId = customerId,
                FilterItems = BuildSalesInvoiceFilterItems(filter),
                Items = items.Select(MapInvoiceListItem).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Invoice(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
            {
                SetErrorMessage("SalesInvoiceNotFound");
                return RedirectOrHtmx(nameof(Invoices), new { });
            }

            var dto = await _getInvoiceDocument.HandleAsync(id, ct).ConfigureAwait(false);
            if (dto is null)
            {
                SetErrorMessage("SalesInvoiceNotFound");
                return RedirectOrHtmx(nameof(Invoices), new { });
            }

            return RenderInvoiceWorkspace(new SalesInvoiceDetailVm { Document = dto });
        }

        private IEnumerable<SelectListItem> BuildSalesOrderFilterItems(SalesOrderDocumentFilter selectedFilter)
        {
            foreach (var value in Enum.GetValues<SalesOrderDocumentFilter>())
            {
                yield return new SelectListItem(T($"SalesOrderFilter{value}"), value.ToString(), selectedFilter == value);
            }
        }

        private IEnumerable<SelectListItem> BuildSalesInvoiceFilterItems(SalesInvoiceDocumentFilter selectedFilter)
        {
            foreach (var value in Enum.GetValues<SalesInvoiceDocumentFilter>())
            {
                yield return new SelectListItem(T($"SalesInvoiceFilter{value}"), value.ToString(), selectedFilter == value);
            }
        }

        private IEnumerable<SelectListItem> BuildSalesQuoteFilterItems(SalesQuoteDocumentFilter selectedFilter)
        {
            foreach (var value in Enum.GetValues<SalesQuoteDocumentFilter>())
            {
                yield return new SelectListItem(T($"SalesQuoteFilter{value}"), value.ToString(), selectedFilter == value);
            }
        }

        private IEnumerable<SelectListItem> BuildDeliveryNoteFilterItems(DeliveryNoteDocumentFilter selectedFilter)
        {
            foreach (var value in Enum.GetValues<DeliveryNoteDocumentFilter>())
            {
                yield return new SelectListItem(T($"DeliveryNoteFilter{value}"), value.ToString(), selectedFilter == value);
            }
        }

        private IEnumerable<SelectListItem> BuildReturnOrderFilterItems(ReturnOrderDocumentFilter selectedFilter)
        {
            foreach (var value in Enum.GetValues<ReturnOrderDocumentFilter>())
            {
                yield return new SelectListItem(T($"ReturnOrderFilter{value}"), value.ToString(), selectedFilter == value);
            }
        }

        private IEnumerable<SelectListItem> BuildCreditNoteFilterItems(CreditNoteDocumentFilter selectedFilter)
        {
            foreach (var value in Enum.GetValues<CreditNoteDocumentFilter>())
            {
                yield return new SelectListItem(T($"CreditNoteFilter{value}"), value.ToString(), selectedFilter == value);
            }
        }

        private IEnumerable<SelectListItem> BuildSalesChannelItems(SalesChannel? selectedChannel)
        {
            yield return new SelectListItem(T("SalesChannelAll"), string.Empty, !selectedChannel.HasValue);
            foreach (var value in Enum.GetValues<SalesChannel>())
            {
                yield return new SelectListItem(T($"SalesChannel{value}"), value.ToString(), selectedChannel == value);
            }
        }

        private static SalesOrderListItemVm MapOrderListItem(SalesOrderListItemDto dto)
        {
            return new SalesOrderListItemVm
            {
                Id = dto.Id,
                OrderNumber = dto.OrderNumber,
                BusinessId = dto.BusinessId,
                CustomerId = dto.CustomerId,
                SalesChannel = dto.SalesChannel,
                OrderedAtUtc = dto.OrderedAtUtc,
                Status = dto.Status,
                Currency = dto.Currency,
                GrandTotalGrossMinor = dto.GrandTotalGrossMinor,
                TaxTotalMinor = dto.TaxTotalMinor,
                LineCount = dto.LineCount,
                PaymentCount = dto.PaymentCount,
                FailedPaymentCount = dto.FailedPaymentCount,
                ShipmentCount = dto.ShipmentCount,
                InvoiceCount = dto.InvoiceCount
            };
        }

        private static SalesInvoiceListItemVm MapInvoiceListItem(SalesInvoiceListItemDto dto)
        {
            return new SalesInvoiceListItemVm
            {
                Id = dto.Id,
                InvoiceNumber = dto.InvoiceNumber,
                BusinessId = dto.BusinessId,
                CustomerId = dto.CustomerId,
                OrderId = dto.OrderId,
                PaymentId = dto.PaymentId,
                OrderNumber = dto.OrderNumber,
                Status = dto.Status,
                Currency = dto.Currency,
                TotalNetMinor = dto.TotalNetMinor,
                TotalTaxMinor = dto.TotalTaxMinor,
                TotalGrossMinor = dto.TotalGrossMinor,
                BalanceMinor = dto.BalanceMinor,
                DueDateUtc = dto.DueDateUtc,
                IssuedAtUtc = dto.IssuedAtUtc,
                HasIssuedSnapshot = dto.HasIssuedSnapshot,
                HasArchiveMetadata = dto.HasArchiveMetadata
            };
        }

        private IActionResult RenderOverviewWorkspace(SalesOverviewVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/Index.cshtml", vm);
            }

            return View("Index", vm);
        }

        private IActionResult RenderOrdersWorkspace(SalesOrdersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/Orders.cshtml", vm);
            }

            return View("Orders", vm);
        }

        private IActionResult RenderOrderWorkspace(SalesOrderDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/Order.cshtml", vm);
            }

            return View("Order", vm);
        }

        private IActionResult RenderInvoicesWorkspace(SalesInvoicesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/Invoices.cshtml", vm);
            }

            return View("Invoices", vm);
        }

        private IActionResult RenderQuotesWorkspace(SalesQuotesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/Quotes.cshtml", vm);
            }

            return View("Quotes", vm);
        }

        private IActionResult RenderQuoteWorkspace(SalesQuoteDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/Quote.cshtml", vm);
            }

            return View("Quote", vm);
        }

        private IActionResult RenderQuoteEditor(SalesQuoteEditorVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/QuoteEditor.cshtml", vm);
            }

            return View("QuoteEditor", vm);
        }

        private IActionResult RenderDeliveryNotesWorkspace(DeliveryNotesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/DeliveryNotes.cshtml", vm);
            }

            return View("DeliveryNotes", vm);
        }

        private IActionResult RenderDeliveryNoteWorkspace(DeliveryNoteDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/DeliveryNote.cshtml", vm);
            }

            return View("DeliveryNote", vm);
        }

        private IActionResult RenderReturnOrdersWorkspace(ReturnOrdersListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/ReturnOrders.cshtml", vm);
            }

            return View("ReturnOrders", vm);
        }

        private IActionResult RenderReturnOrderWorkspace(ReturnOrderDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/ReturnOrder.cshtml", vm);
            }

            return View("ReturnOrder", vm);
        }

        private IActionResult RenderCreditNotesWorkspace(CreditNotesListVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/CreditNotes.cshtml", vm);
            }

            return View("CreditNotes", vm);
        }

        private IActionResult RenderCreditNoteWorkspace(CreditNoteDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/CreditNote.cshtml", vm);
            }

            return View("CreditNote", vm);
        }

        private async Task<IActionResult> ExecuteDeliveryNoteLifecycleAsync(Guid id, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                SetSuccessMessage("DeliveryNoteUpdated");
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectOrHtmx(nameof(DeliveryNote), new { id });
        }

        private async Task<IActionResult> ExecuteReturnOrderLifecycleAsync(Guid id, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                SetSuccessMessage("ReturnOrderUpdated");
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectOrHtmx(nameof(ReturnOrder), new { id });
        }

        private async Task<IActionResult> ExecuteCreditNoteLifecycleAsync(Guid id, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                SetSuccessMessage("CreditNoteUpdated");
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectOrHtmx(nameof(CreditNote), new { id });
        }

        private async Task<IActionResult> ExecuteQuoteLifecycleAsync(Guid id, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                SetSuccessMessage("SalesQuoteUpdated");
            }
            catch (Exception ex) when (ex is ValidationException or InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                SetErrorMessage(ex.Message);
            }

            return RedirectOrHtmx(nameof(Quote), new { id });
        }

        private static SalesQuoteEditDto MapQuoteEdit(SalesQuoteDetailDto detail)
        {
            return new SalesQuoteEditDto
            {
                Id = detail.Id,
                BusinessId = detail.BusinessId,
                CustomerId = detail.CustomerId,
                OpportunityId = detail.OpportunityId,
                Title = detail.Title,
                Currency = detail.Currency,
                ValidUntilUtc = detail.ValidUntilUtc,
                OwnerUserId = detail.OwnerUserId,
                PreparedByUserId = detail.PreparedByUserId,
                CustomerSnapshotJson = detail.CustomerSnapshotJson,
                BillingAddressJson = detail.BillingAddressJson,
                ShippingAddressJson = detail.ShippingAddressJson,
                InternalNotes = detail.InternalNotes,
                RowVersion = detail.RowVersion,
                Lines = detail.Lines.Select(x => new SalesQuoteLineEditDto
                {
                    Id = x.Id,
                    ProductVariantId = x.ProductVariantId,
                    Name = x.Name,
                    Sku = x.Sku,
                    Description = x.Description,
                    Quantity = x.Quantity,
                    UnitPriceNetMinor = x.UnitPriceNetMinor,
                    UnitPriceGrossMinor = x.UnitPriceGrossMinor,
                    TaxRate = x.TaxRate,
                    SortOrder = x.SortOrder
                }).ToList()
            };
        }

        private static ReturnOrderDetailVm BuildReturnOrderDetailVm(ReturnOrderDetailDto detail)
        {
            return new ReturnOrderDetailVm
            {
                Document = detail,
                Lifecycle = new ReturnOrderLifecycleDto { Id = detail.Id, RowVersion = detail.RowVersion },
                Approve = new ReturnOrderApproveDto
                {
                    Id = detail.Id,
                    RowVersion = detail.RowVersion,
                    Lines = detail.Lines.Select(x => new ReturnOrderQuantityLineDto { LineId = x.Id, Quantity = x.RequestedQuantity }).ToList()
                },
                QueueShipment = new ReturnOrderQueueShipmentDto { Id = detail.Id, RowVersion = detail.RowVersion, ShipmentId = detail.ShipmentId },
                Receive = new ReturnOrderReceiveDto
                {
                    Id = detail.Id,
                    RowVersion = detail.RowVersion,
                    Lines = detail.Lines.Select(x => new ReturnOrderQuantityLineDto { LineId = x.Id, Quantity = x.ApprovedQuantity }).ToList()
                },
                Inspect = new ReturnOrderInspectDto
                {
                    Id = detail.Id,
                    RowVersion = detail.RowVersion,
                    Lines = detail.Lines.Select(x => new ReturnOrderInspectionLineDto
                    {
                        LineId = x.Id,
                        AcceptedQuantity = x.ReceivedQuantity,
                        RestockQuantity = x.ProductVariantId.HasValue ? x.ReceivedQuantity : 0,
                        RestockWarehouseId = x.RestockWarehouseId
                    }).ToList()
                },
                LinkRefund = new ReturnOrderLinkRefundDto { Id = detail.Id, RowVersion = detail.RowVersion }
            };
        }

        private IActionResult RenderInvoiceWorkspace(SalesInvoiceDetailVm vm)
        {
            if (IsHtmxRequest())
            {
                return PartialView("~/Views/Sales/Invoice.cshtml", vm);
            }

            return View("Invoice", vm);
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

        private static Guid? NormalizeGuid(Guid? value)
        {
            return value.HasValue && value.Value != Guid.Empty ? value : null;
        }
    }
}
