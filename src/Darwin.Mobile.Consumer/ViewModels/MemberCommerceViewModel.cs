using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Collections;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Commerce;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Provides a lightweight member commerce history surface for orders and invoices in the consumer app.
/// </summary>
public sealed class MemberCommerceViewModel : BaseViewModel
{
    private readonly IMemberCommerceService _memberCommerceService;
    private BusyOperationScope? _currentOperation;
    private bool _isLoaded;
    private string? _successMessage;
    private MemberCommerceOrderDetailViewModel? _selectedOrder;
    private MemberCommerceInvoiceDetailViewModel? _selectedInvoice;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberCommerceViewModel"/> class.
    /// </summary>
    public MemberCommerceViewModel(IMemberCommerceService memberCommerceService)
    {
        _memberCommerceService = memberCommerceService ?? throw new ArgumentNullException(nameof(memberCommerceService));
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        ViewOrderCommand = new AsyncCommand<MemberCommerceOrderItemViewModel>(LoadOrderDetailAsync, CanLoadOrderDetail);
        RetryOrderPaymentCommand = new AsyncCommand(RetryOrderPaymentAsync, () => !IsBusy && SelectedOrder?.CanRetryPayment == true);
        CopyOrderDocumentCommand = new AsyncCommand(CopyOrderDocumentAsync, () => !IsBusy && SelectedOrder?.HasDocument == true);
        OpenOrderShipmentTrackingCommand = new AsyncCommand<MemberCommerceShipmentSummaryViewModel>(OpenOrderShipmentTrackingAsync, CanOpenOrderShipmentTracking);
        ViewInvoiceCommand = new AsyncCommand<MemberCommerceInvoiceItemViewModel>(LoadInvoiceDetailAsync, CanLoadInvoiceDetail);
        RetryInvoicePaymentCommand = new AsyncCommand(RetryInvoicePaymentAsync, () => !IsBusy && SelectedInvoice?.CanRetryPayment == true);
        CopyInvoiceDocumentCommand = new AsyncCommand(CopyInvoiceDocumentAsync, () => !IsBusy && SelectedInvoice?.HasDocument == true);
    }

    /// <summary>
    /// Gets the recent order summaries.
    /// </summary>
    public RangeObservableCollection<MemberCommerceOrderItemViewModel> Orders { get; } = new();

    /// <summary>
    /// Gets the recent invoice summaries.
    /// </summary>
    public RangeObservableCollection<MemberCommerceInvoiceItemViewModel> Invoices { get; } = new();

    /// <summary>
    /// Gets the refresh command.
    /// </summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the order detail load command.
    /// </summary>
    public AsyncCommand<MemberCommerceOrderItemViewModel> ViewOrderCommand { get; }

    /// <summary>
    /// Gets the order payment retry command.
    /// </summary>
    public AsyncCommand RetryOrderPaymentCommand { get; }

    /// <summary>
    /// Gets the order document copy command.
    /// </summary>
    public AsyncCommand CopyOrderDocumentCommand { get; }

    /// <summary>
    /// Gets the order shipment tracking command.
    /// </summary>
    public AsyncCommand<MemberCommerceShipmentSummaryViewModel> OpenOrderShipmentTrackingCommand { get; }

    /// <summary>
    /// Gets the invoice detail load command.
    /// </summary>
    public AsyncCommand<MemberCommerceInvoiceItemViewModel> ViewInvoiceCommand { get; }

    /// <summary>
    /// Gets the invoice payment retry command.
    /// </summary>
    public AsyncCommand RetryInvoicePaymentCommand { get; }

    /// <summary>
    /// Gets the invoice document copy command.
    /// </summary>
    public AsyncCommand CopyInvoiceDocumentCommand { get; }

    /// <summary>
    /// Gets the latest success message.
    /// </summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a success message is present.
    /// </summary>
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    /// <summary>
    /// Gets the currently selected order detail.
    /// </summary>
    public MemberCommerceOrderDetailViewModel? SelectedOrder
    {
        get => _selectedOrder;
        private set
        {
            if (SetProperty(ref _selectedOrder, value))
            {
                OnPropertyChanged(nameof(HasSelectedOrder));
                RetryOrderPaymentCommand.RaiseCanExecuteChanged();
                CopyOrderDocumentCommand.RaiseCanExecuteChanged();
                OpenOrderShipmentTrackingCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the currently selected invoice detail.
    /// </summary>
    public MemberCommerceInvoiceDetailViewModel? SelectedInvoice
    {
        get => _selectedInvoice;
        private set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                OnPropertyChanged(nameof(HasSelectedInvoice));
                RetryInvoicePaymentCommand.RaiseCanExecuteChanged();
                CopyInvoiceDocumentCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether orders are available.
    /// </summary>
    public bool HasOrders => Orders.Count > 0;

    /// <summary>
    /// Gets a value indicating whether invoices are available.
    /// </summary>
    public bool HasInvoices => Invoices.Count > 0;

    /// <summary>
    /// Gets a value indicating whether an order detail is currently loaded.
    /// </summary>
    public bool HasSelectedOrder => SelectedOrder is not null;

    /// <summary>
    /// Gets a value indicating whether an invoice detail is currently loaded.
    /// </summary>
    public bool HasSelectedInvoice => SelectedInvoice is not null;

    /// <inheritdoc />
    public override async Task OnAppearingAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        await RefreshAsync().ConfigureAwait(false);
        _isLoaded = true;
    }

    /// <summary>
    /// Cancels any in-flight commerce operation when the page is no longer visible.
    /// This keeps payment, document, and detail-load continuations from updating stale UI state after navigation.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        return Task.CompletedTask;
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        using var operation = BeginBusyOperation();
        var cancellationToken = operation.Token;

        try
        {
            var ordersTask = _memberCommerceService.GetMyOrdersAsync(1, 10, cancellationToken);
            var invoicesTask = _memberCommerceService.GetMyInvoicesAsync(1, 10, cancellationToken);
            await Task.WhenAll(ordersTask, invoicesTask).ConfigureAwait(false);

            var ordersResult = await ordersTask.ConfigureAwait(false);
            var invoicesResult = await invoicesTask.ConfigureAwait(false);

            if (!ordersResult.Succeeded || ordersResult.Value is null)
            {
                RunOnMain(() => ErrorMessage = ordersResult.Error ?? AppResources.MemberCommerceLoadFailed);
            }

            if (!invoicesResult.Succeeded || invoicesResult.Value is null)
            {
                RunOnMain(() => ErrorMessage = invoicesResult.Error ?? AppResources.MemberCommerceLoadFailed);
            }

            var orders = (ordersResult.Value?.Items ?? Array.Empty<MemberOrderSummary>())
                .Select(MapOrderSummary)
                .ToList();
            var invoices = (invoicesResult.Value?.Items ?? Array.Empty<MemberInvoiceSummary>())
                .Select(MapInvoiceSummary)
                .ToList();

            RunOnMain(() =>
            {
                // Replace both history lists in batches so refreshes do not trigger one layout pass per item.
                Orders.ReplaceRange(orders);
                Invoices.ReplaceRange(invoices);
                SelectedOrder = null;
                SelectedInvoice = null;
                OnPropertyChanged(nameof(HasOrders));
                OnPropertyChanged(nameof(HasInvoices));
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the commerce screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCommerceLoadFailed));
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    private bool CanLoadOrderDetail(MemberCommerceOrderItemViewModel? item) => !IsBusy && item is not null;

    private async Task LoadOrderDetailAsync(MemberCommerceOrderItemViewModel? item)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        using var operation = BeginBusyOperation();

        try
        {
            var result = await _memberCommerceService.GetOrderAsync(item.Id, operation.Token).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.MemberCommerceOrderDetailLoadFailed);
                return;
            }

            RunOnMain(() =>
            {
                SelectedOrder = MapOrderDetail(result.Value);
                SelectedInvoice = null;
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the commerce screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCommerceOrderDetailLoadFailed));
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    private bool CanLoadInvoiceDetail(MemberCommerceInvoiceItemViewModel? item) => !IsBusy && item is not null;

    private async Task LoadInvoiceDetailAsync(MemberCommerceInvoiceItemViewModel? item)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        using var operation = BeginBusyOperation();

        try
        {
            var result = await _memberCommerceService.GetInvoiceAsync(item.Id, operation.Token).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.MemberCommerceInvoiceDetailLoadFailed);
                return;
            }

            RunOnMain(() =>
            {
                SelectedInvoice = MapInvoiceDetail(result.Value);
                SelectedOrder = null;
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the commerce screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCommerceInvoiceDetailLoadFailed));
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    private async Task RetryOrderPaymentAsync()
    {
        if (SelectedOrder is null || !SelectedOrder.CanRetryPayment || IsBusy)
        {
            return;
        }

        await RetryPaymentAsync(
            SelectedOrder.ReferenceNumber,
            (request, cancellationToken) => _memberCommerceService.CreateOrderPaymentIntentAsync(SelectedOrder.Id, request, cancellationToken),
            AppResources.MemberCommercePaymentIntentFailed).ConfigureAwait(false);
    }

    private async Task RetryInvoicePaymentAsync()
    {
        if (SelectedInvoice is null || !SelectedInvoice.CanRetryPayment || IsBusy)
        {
            return;
        }

        await RetryPaymentAsync(
            SelectedInvoice.ReferenceNumber,
            (request, cancellationToken) => _memberCommerceService.CreateInvoicePaymentIntentAsync(SelectedInvoice.Id, request, cancellationToken),
            AppResources.MemberCommercePaymentIntentFailed).ConfigureAwait(false);
    }

    private async Task RetryPaymentAsync(
        string referenceNumber,
        Func<CreateStorefrontPaymentIntentRequest, CancellationToken, Task<Darwin.Shared.Results.Result<CreateStorefrontPaymentIntentResponse>>> action,
        string fallbackError)
    {
        if (IsBusy)
        {
            return;
        }

        using var operation = BeginBusyOperation();

        try
        {
            var result = await action(new CreateStorefrontPaymentIntentRequest
            {
                Provider = "HostedCheckout"
            }, operation.Token).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null || string.IsNullOrWhiteSpace(result.Value.CheckoutUrl))
            {
                RunOnMain(() => ErrorMessage = result.Error ?? fallbackError);
                return;
            }

            await Browser.Default.OpenAsync(result.Value.CheckoutUrl, BrowserLaunchMode.SystemPreferred).ConfigureAwait(false);
            RunOnMain(() => SuccessMessage = string.Format(AppResources.MemberCommerceCheckoutLaunchedFormat, referenceNumber));
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the commerce screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, fallbackError));
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    private async Task CopyOrderDocumentAsync()
    {
        if (SelectedOrder is null || !SelectedOrder.HasDocument || IsBusy)
        {
            return;
        }

        await CopyDocumentAsync(
            SelectedOrder.ReferenceNumber,
            cancellationToken => _memberCommerceService.DownloadOrderDocumentAsync(SelectedOrder.Id, cancellationToken),
            AppResources.MemberCommerceDocumentDownloadFailed).ConfigureAwait(false);
    }

    private async Task CopyInvoiceDocumentAsync()
    {
        if (SelectedInvoice is null || !SelectedInvoice.HasDocument || IsBusy)
        {
            return;
        }

        await CopyDocumentAsync(
            SelectedInvoice.ReferenceNumber,
            cancellationToken => _memberCommerceService.DownloadInvoiceDocumentAsync(SelectedInvoice.Id, cancellationToken),
            AppResources.MemberCommerceDocumentDownloadFailed).ConfigureAwait(false);
    }

    private async Task CopyDocumentAsync(
        string referenceNumber,
        Func<CancellationToken, Task<Darwin.Shared.Results.Result<string>>> action,
        string fallbackError)
    {
        using var operation = BeginBusyOperation();

        try
        {
            var result = await action(operation.Token).ConfigureAwait(false);
            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value))
            {
                RunOnMain(() => ErrorMessage = result.Error ?? fallbackError);
                return;
            }

            await Clipboard.Default.SetTextAsync(result.Value).ConfigureAwait(false);
            RunOnMain(() => SuccessMessage = string.Format(AppResources.MemberCommerceDocumentCopiedFormat, referenceNumber));
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the commerce screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, fallbackError));
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    private bool CanOpenOrderShipmentTracking(MemberCommerceShipmentSummaryViewModel? shipment) =>
        !IsBusy && shipment?.HasTrackingLink == true;

    private async Task OpenOrderShipmentTrackingAsync(MemberCommerceShipmentSummaryViewModel? shipment)
    {
        if (shipment is null || !shipment.HasTrackingLink || string.IsNullOrWhiteSpace(shipment.TrackingUrl) || IsBusy)
        {
            return;
        }

        using var operation = BeginBusyOperation();

        try
        {
            await Browser.Default.OpenAsync(shipment.TrackingUrl, BrowserLaunchMode.SystemPreferred).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the commerce screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCommerceTrackingOpenFailed));
        }
        finally
        {
            EndBusyOperation(operation);
        }
    }

    /// <summary>
    /// Marks the commerce screen as busy and owns cancellation for one user operation.
    /// </summary>
    private BusyOperationScope BeginBusyOperation()
    {
        var operation = new BusyOperationScope(this);
        var previousOperation = Interlocked.Exchange(ref _currentOperation, operation);
        previousOperation?.Cancel();
        previousOperation?.Dispose();

        RunOnMain(() =>
        {
            ErrorMessage = null;
            SuccessMessage = null;
            IsBusy = true;
            RaiseCommandStates();
        });
        return operation;
    }

    /// <summary>
    /// Clears busy state when the matching commerce operation completes.
    /// </summary>
    /// <param name="operation">Operation scope that owns the current busy state.</param>
    private void EndBusyOperation(BusyOperationScope operation)
    {
        if (operation.IsDisposed)
        {
            return;
        }

        var isCurrentOperation = ReferenceEquals(_currentOperation, operation);
        if (isCurrentOperation)
        {
            _currentOperation = null;
        }

        operation.IsDisposed = true;
        operation.Cancellation.Dispose();

        if (isCurrentOperation)
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    /// <summary>
    /// Cancels the current commerce operation and releases the visible busy state.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var operation = Interlocked.Exchange(ref _currentOperation, null);
        if (operation is null)
        {
            return;
        }

        operation.Cancel();
        RunOnMain(() =>
        {
            IsBusy = false;
            RaiseCommandStates();
        });
    }

    /// <summary>
    /// Owns cancellation and busy-state lifetime for one commerce operation.
    /// </summary>
    private sealed class BusyOperationScope : IDisposable
    {
        private readonly MemberCommerceViewModel _owner;

        public BusyOperationScope(MemberCommerceViewModel owner)
        {
            _owner = owner;
            Cancellation = new CancellationTokenSource();
        }

        public CancellationTokenSource Cancellation { get; }

        public CancellationToken Token => Cancellation.Token;

        public bool IsDisposed { get; set; }

        public void Cancel()
        {
            if (!Cancellation.IsCancellationRequested)
            {
                Cancellation.Cancel();
            }
        }

        public void Dispose()
        {
            _owner.EndBusyOperation(this);
        }
    }

    /// <summary>
    /// Refreshes all commerce commands after busy-state transitions so payment, document, and tracking actions stay single-flight.
    /// </summary>
    private void RaiseCommandStates()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        ViewOrderCommand.RaiseCanExecuteChanged();
        RetryOrderPaymentCommand.RaiseCanExecuteChanged();
        CopyOrderDocumentCommand.RaiseCanExecuteChanged();
        OpenOrderShipmentTrackingCommand.RaiseCanExecuteChanged();
        ViewInvoiceCommand.RaiseCanExecuteChanged();
        RetryInvoicePaymentCommand.RaiseCanExecuteChanged();
        CopyInvoiceDocumentCommand.RaiseCanExecuteChanged();
    }

    private static MemberCommerceOrderItemViewModel MapOrderSummary(MemberOrderSummary order)
    {
        return new MemberCommerceOrderItemViewModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            StatusText = string.Format(AppResources.MemberCommerceOrderStatusFormat, LocalizeOrderStatus(order.Status)),
            CreatedAtText = string.Format(AppResources.MemberCommerceOrderCreatedFormat, order.CreatedAtUtc.ToLocalTime()),
            TotalText = string.Format(AppResources.MemberCommerceOrderTotalFormat, FormatMoney(order.GrandTotalGrossMinor, order.Currency))
        };
    }

    private static MemberCommerceInvoiceItemViewModel MapInvoiceSummary(MemberInvoiceSummary invoice)
    {
        var referenceNumber = string.IsNullOrWhiteSpace(invoice.OrderNumber)
            ? invoice.Id.ToString("N")[..8].ToUpperInvariant()
            : invoice.OrderNumber!;

        return new MemberCommerceInvoiceItemViewModel
        {
            Id = invoice.Id,
            ReferenceNumber = referenceNumber,
            BusinessName = invoice.BusinessName,
            StatusText = string.Format(AppResources.MemberCommerceInvoiceStatusFormat, LocalizeInvoiceStatus(invoice.Status)),
            DueDateText = string.Format(AppResources.MemberCommerceInvoiceDueDateFormat, invoice.DueDateUtc.ToLocalTime()),
            TotalText = string.Format(AppResources.MemberCommerceInvoiceTotalFormat, FormatMoney(invoice.TotalGrossMinor, invoice.Currency)),
            BalanceText = string.Format(AppResources.MemberCommerceInvoiceBalanceFormat, FormatMoney(invoice.BalanceMinor, invoice.Currency))
        };
    }

    private static MemberCommerceOrderDetailViewModel MapOrderDetail(MemberOrderDetail order)
    {
        return new MemberCommerceOrderDetailViewModel
        {
            Id = order.Id,
            ReferenceNumber = order.OrderNumber,
            HeaderTitle = order.OrderNumber,
            StatusText = string.Format(AppResources.MemberCommerceOrderStatusFormat, LocalizeOrderStatus(order.Status)),
            CreatedAtText = string.Format(AppResources.MemberCommerceOrderCreatedFormat, order.CreatedAtUtc.ToLocalTime()),
            TotalText = string.Format(AppResources.MemberCommerceOrderTotalFormat, FormatMoney(order.GrandTotalGrossMinor, order.Currency)),
            ShippingMethodText = string.IsNullOrWhiteSpace(order.ShippingMethodName)
                ? null
                : string.Format(AppResources.MemberCommerceOrderShippingMethodFormat, order.ShippingMethodName),
            PaymentsCountText = string.Format(AppResources.MemberCommerceOrderPaymentsCountFormat, order.Payments.Count),
            InvoicesCountText = string.Format(AppResources.MemberCommerceOrderInvoicesCountFormat, order.Invoices.Count),
            LineSummaries = order.Lines.Select(static line => $"{line.Name} x{line.Quantity}").ToArray(),
            ShipmentSummaries = order.Shipments.Select(MapShipmentSummary).ToArray(),
            CanRetryPayment = order.Actions.CanRetryPayment,
            HasDocument = !string.IsNullOrWhiteSpace(order.Actions.DocumentPath)
        };
    }

    private static MemberCommerceShipmentSummaryViewModel MapShipmentSummary(MemberOrderShipment shipment)
    {
        return new MemberCommerceShipmentSummaryViewModel
        {
            TitleText = string.Format(
                CultureInfo.CurrentCulture,
                AppResources.MemberCommerceShipmentTitleFormat,
                shipment.Carrier,
                shipment.Service),
            StatusText = string.Format(AppResources.MemberCommerceOrderStatusFormat, LocalizeOrderStatus(shipment.Status)),
            TrackingText = string.IsNullOrWhiteSpace(shipment.TrackingNumber)
                ? null
                : string.Format(AppResources.MemberCommerceShipmentTrackingFormat, shipment.TrackingNumber),
            TrackingUrl = string.IsNullOrWhiteSpace(shipment.TrackingUrl) ? null : shipment.TrackingUrl,
            ShippedAtText = shipment.ShippedAtUtc is null
                ? null
                : string.Format(AppResources.MemberCommerceShipmentShippedFormat, shipment.ShippedAtUtc.Value.ToLocalTime()),
            DeliveredAtText = shipment.DeliveredAtUtc is null
                ? null
                : string.Format(AppResources.MemberCommerceShipmentDeliveredFormat, shipment.DeliveredAtUtc.Value.ToLocalTime())
        };
    }

    private static MemberCommerceInvoiceDetailViewModel MapInvoiceDetail(MemberInvoiceDetail invoice)
    {
        var referenceNumber = string.IsNullOrWhiteSpace(invoice.OrderNumber)
            ? invoice.Id.ToString("N")[..8].ToUpperInvariant()
            : invoice.OrderNumber!;

        return new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoice.Id,
            ReferenceNumber = referenceNumber,
            HeaderTitle = referenceNumber,
            StatusText = string.Format(AppResources.MemberCommerceInvoiceStatusFormat, LocalizeInvoiceStatus(invoice.Status)),
            DueDateText = string.Format(AppResources.MemberCommerceInvoiceDueDateFormat, invoice.DueDateUtc.ToLocalTime()),
            TotalText = string.Format(AppResources.MemberCommerceInvoiceTotalFormat, FormatMoney(invoice.TotalGrossMinor, invoice.Currency)),
            BalanceText = string.Format(AppResources.MemberCommerceInvoiceBalanceFormat, FormatMoney(invoice.BalanceMinor, invoice.Currency)),
            PaymentSummaryText = string.Format(AppResources.MemberCommerceInvoicePaymentSummaryFormat, invoice.PaymentSummary),
            LineSummaries = invoice.Lines.Select(static line => $"{line.Description} x{line.Quantity}").ToArray(),
            CanRetryPayment = invoice.Actions.CanRetryPayment,
            HasDocument = !string.IsNullOrWhiteSpace(invoice.Actions.DocumentPath)
        };
    }

    private static string FormatMoney(long amountMinor, string currency)
    {
        var amount = amountMinor / 100m;
        return string.Format(CultureInfo.CurrentCulture, "{0:N2} {1}", amount, currency);
    }

    private static string LocalizeOrderStatus(string? status)
    {
        return status switch
        {
            "Created" => AppResources.MemberCommerceStatusCreated,
            "Confirmed" => AppResources.MemberCommerceStatusConfirmed,
            "Paid" => AppResources.MemberCommerceStatusPaid,
            "PartiallyShipped" => AppResources.MemberCommerceStatusPartiallyShipped,
            "Shipped" => AppResources.MemberCommerceStatusShipped,
            "Delivered" => AppResources.MemberCommerceStatusDelivered,
            "Cancelled" => AppResources.MemberCommerceStatusCancelled,
            "Refunded" => AppResources.MemberCommerceStatusRefunded,
            "PartiallyRefunded" => AppResources.MemberCommerceStatusPartiallyRefunded,
            "Completed" => AppResources.MemberCommerceStatusCompleted,
            _ => AppResources.MemberCommerceStatusUnknown
        };
    }

    private static string LocalizeInvoiceStatus(string? status)
    {
        return status switch
        {
            "Draft" => AppResources.MemberCommerceStatusDraft,
            "Open" => AppResources.MemberCommerceStatusOpen,
            "Paid" => AppResources.MemberCommerceStatusPaid,
            "Cancelled" => AppResources.MemberCommerceStatusCancelled,
            _ => AppResources.MemberCommerceStatusUnknown
        };
    }
}

/// <summary>
/// Read-only order summary model for commerce history screens.
/// </summary>
public sealed class MemberCommerceOrderItemViewModel
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the order number.</summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the status display text.</summary>
    public string StatusText { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp display text.</summary>
    public string CreatedAtText { get; set; } = string.Empty;
    /// <summary>Gets or sets the total display text.</summary>
    public string TotalText { get; set; } = string.Empty;
}

/// <summary>
/// Read-only invoice summary model for commerce history screens.
/// </summary>
public sealed class MemberCommerceInvoiceItemViewModel
{
    /// <summary>Gets or sets the invoice identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the reference number shown in the UI.</summary>
    public string ReferenceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional business name.</summary>
    public string? BusinessName { get; set; }
    /// <summary>Gets or sets the status display text.</summary>
    public string StatusText { get; set; } = string.Empty;
    /// <summary>Gets or sets the due date display text.</summary>
    public string DueDateText { get; set; } = string.Empty;
    /// <summary>Gets or sets the total display text.</summary>
    public string TotalText { get; set; } = string.Empty;
    /// <summary>Gets or sets the balance display text.</summary>
    public string BalanceText { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether the business name should be displayed.</summary>
    public bool HasBusinessName => !string.IsNullOrWhiteSpace(BusinessName);
}

/// <summary>
/// Read-only order detail model for commerce history screens.
/// </summary>
public sealed class MemberCommerceOrderDetailViewModel
{
    /// <summary>Gets or sets the order identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the reference number.</summary>
    public string ReferenceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the header title.</summary>
    public string HeaderTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the status display text.</summary>
    public string StatusText { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation display text.</summary>
    public string CreatedAtText { get; set; } = string.Empty;
    /// <summary>Gets or sets the total display text.</summary>
    public string TotalText { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional shipping-method text.</summary>
    public string? ShippingMethodText { get; set; }
    /// <summary>Gets or sets the payment count display text.</summary>
    public string PaymentsCountText { get; set; } = string.Empty;
    /// <summary>Gets or sets the linked invoice count display text.</summary>
    public string InvoicesCountText { get; set; } = string.Empty;
    /// <summary>Gets or sets the purchased line summaries.</summary>
    public IReadOnlyList<string> LineSummaries { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets the shipment summaries.</summary>
    public IReadOnlyList<MemberCommerceShipmentSummaryViewModel> ShipmentSummaries { get; set; } = Array.Empty<MemberCommerceShipmentSummaryViewModel>();
    /// <summary>Gets or sets a value indicating whether retry payment is available.</summary>
    public bool CanRetryPayment { get; set; }
    /// <summary>Gets or sets a value indicating whether a document can be copied.</summary>
    public bool HasDocument { get; set; }
    /// <summary>Gets a value indicating whether a shipping-method label should be displayed.</summary>
    public bool HasShippingMethod => !string.IsNullOrWhiteSpace(ShippingMethodText);
    /// <summary>Gets a value indicating whether shipment summaries should be displayed.</summary>
    public bool HasShipments => ShipmentSummaries.Count > 0;
}

/// <summary>
/// Read-only shipment summary model for member commerce order detail screens.
/// </summary>
public sealed class MemberCommerceShipmentSummaryViewModel
{
    /// <summary>Gets or sets the shipment title.</summary>
    public string TitleText { get; set; } = string.Empty;
    /// <summary>Gets or sets the shipment status display text.</summary>
    public string StatusText { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional tracking text.</summary>
    public string? TrackingText { get; set; }
    /// <summary>Gets or sets the optional carrier tracking URL.</summary>
    public string? TrackingUrl { get; set; }
    /// <summary>Gets or sets the optional shipped-at display text.</summary>
    public string? ShippedAtText { get; set; }
    /// <summary>Gets or sets the optional delivered-at display text.</summary>
    public string? DeliveredAtText { get; set; }

    /// <summary>Gets a value indicating whether a tracking label should be displayed.</summary>
    public bool HasTrackingText => !string.IsNullOrWhiteSpace(TrackingText);
    /// <summary>Gets a value indicating whether a tracking link is available.</summary>
    public bool HasTrackingLink => !string.IsNullOrWhiteSpace(TrackingUrl);
    /// <summary>Gets a value indicating whether a shipped-at label should be displayed.</summary>
    public bool HasShippedAt => !string.IsNullOrWhiteSpace(ShippedAtText);
    /// <summary>Gets a value indicating whether a delivered-at label should be displayed.</summary>
    public bool HasDeliveredAt => !string.IsNullOrWhiteSpace(DeliveredAtText);
}

/// <summary>
/// Read-only invoice detail model for commerce history screens.
/// </summary>
public sealed class MemberCommerceInvoiceDetailViewModel
{
    /// <summary>Gets or sets the invoice identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the reference number.</summary>
    public string ReferenceNumber { get; set; } = string.Empty;
    /// <summary>Gets or sets the header title.</summary>
    public string HeaderTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the status display text.</summary>
    public string StatusText { get; set; } = string.Empty;
    /// <summary>Gets or sets the due date display text.</summary>
    public string DueDateText { get; set; } = string.Empty;
    /// <summary>Gets or sets the total display text.</summary>
    public string TotalText { get; set; } = string.Empty;
    /// <summary>Gets or sets the balance display text.</summary>
    public string BalanceText { get; set; } = string.Empty;
    /// <summary>Gets or sets the payment-summary display text.</summary>
    public string PaymentSummaryText { get; set; } = string.Empty;
    /// <summary>Gets or sets the line summaries.</summary>
    public IReadOnlyList<string> LineSummaries { get; set; } = Array.Empty<string>();
    /// <summary>Gets or sets a value indicating whether retry payment is available.</summary>
    public bool CanRetryPayment { get; set; }
    /// <summary>Gets or sets a value indicating whether a document can be copied.</summary>
    public bool HasDocument { get; set; }
}
