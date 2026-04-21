using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Orders;
using Darwin.Mobile.Consumer.Resources;
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
        ViewInvoiceCommand = new AsyncCommand<MemberCommerceInvoiceItemViewModel>(LoadInvoiceDetailAsync, CanLoadInvoiceDetail);
        RetryInvoicePaymentCommand = new AsyncCommand(RetryInvoicePaymentAsync, () => !IsBusy && SelectedInvoice?.CanRetryPayment == true);
        CopyInvoiceDocumentCommand = new AsyncCommand(CopyInvoiceDocumentAsync, () => !IsBusy && SelectedInvoice?.HasDocument == true);
    }

    /// <summary>
    /// Gets the recent order summaries.
    /// </summary>
    public ObservableCollection<MemberCommerceOrderItemViewModel> Orders { get; } = new();

    /// <summary>
    /// Gets the recent invoice summaries.
    /// </summary>
    public ObservableCollection<MemberCommerceInvoiceItemViewModel> Invoices { get; } = new();

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

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var ordersTask = _memberCommerceService.GetMyOrdersAsync(1, 10, CancellationToken.None);
            var invoicesTask = _memberCommerceService.GetMyInvoicesAsync(1, 10, CancellationToken.None);
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

            RunOnMain(() =>
            {
                Orders.Clear();
                foreach (var order in ordersResult.Value?.Items ?? Array.Empty<MemberOrderSummary>())
                {
                    Orders.Add(MapOrderSummary(order));
                }

                Invoices.Clear();
                foreach (var invoice in invoicesResult.Value?.Items ?? Array.Empty<MemberInvoiceSummary>())
                {
                    Invoices.Add(MapInvoiceSummary(invoice));
                }

                SelectedOrder = null;
                SelectedInvoice = null;
                OnPropertyChanged(nameof(HasOrders));
                OnPropertyChanged(nameof(HasInvoices));
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCommerceLoadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RefreshCommand.RaiseCanExecuteChanged();
                ViewOrderCommand.RaiseCanExecuteChanged();
                ViewInvoiceCommand.RaiseCanExecuteChanged();
                RetryOrderPaymentCommand.RaiseCanExecuteChanged();
                CopyOrderDocumentCommand.RaiseCanExecuteChanged();
                RetryInvoicePaymentCommand.RaiseCanExecuteChanged();
                CopyInvoiceDocumentCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private bool CanLoadOrderDetail(MemberCommerceOrderItemViewModel? item) => !IsBusy && item is not null;

    private async Task LoadOrderDetailAsync(MemberCommerceOrderItemViewModel? item)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var result = await _memberCommerceService.GetOrderAsync(item.Id, CancellationToken.None).ConfigureAwait(false);
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
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCommerceOrderDetailLoadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                ViewOrderCommand.RaiseCanExecuteChanged();
                RetryOrderPaymentCommand.RaiseCanExecuteChanged();
                CopyOrderDocumentCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private bool CanLoadInvoiceDetail(MemberCommerceInvoiceItemViewModel? item) => !IsBusy && item is not null;

    private async Task LoadInvoiceDetailAsync(MemberCommerceInvoiceItemViewModel? item)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var result = await _memberCommerceService.GetInvoiceAsync(item.Id, CancellationToken.None).ConfigureAwait(false);
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
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCommerceInvoiceDetailLoadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                ViewInvoiceCommand.RaiseCanExecuteChanged();
                RetryInvoicePaymentCommand.RaiseCanExecuteChanged();
                CopyInvoiceDocumentCommand.RaiseCanExecuteChanged();
            });
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
            request => _memberCommerceService.CreateOrderPaymentIntentAsync(SelectedOrder.Id, request, CancellationToken.None),
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
            request => _memberCommerceService.CreateInvoicePaymentIntentAsync(SelectedInvoice.Id, request, CancellationToken.None),
            AppResources.MemberCommercePaymentIntentFailed).ConfigureAwait(false);
    }

    private async Task RetryPaymentAsync(
        string referenceNumber,
        Func<CreateStorefrontPaymentIntentRequest, Task<Darwin.Shared.Results.Result<CreateStorefrontPaymentIntentResponse>>> action,
        string fallbackError)
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var result = await action(new CreateStorefrontPaymentIntentRequest
            {
                Provider = "HostedCheckout"
            }).ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null || string.IsNullOrWhiteSpace(result.Value.CheckoutUrl))
            {
                RunOnMain(() => ErrorMessage = result.Error ?? fallbackError);
                return;
            }

            await Browser.Default.OpenAsync(result.Value.CheckoutUrl, BrowserLaunchMode.SystemPreferred).ConfigureAwait(false);
            RunOnMain(() => SuccessMessage = string.Format(AppResources.MemberCommerceCheckoutLaunchedFormat, referenceNumber));
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, fallbackError));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RetryOrderPaymentCommand.RaiseCanExecuteChanged();
                RetryInvoicePaymentCommand.RaiseCanExecuteChanged();
            });
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
            () => _memberCommerceService.DownloadOrderDocumentAsync(SelectedOrder.Id, CancellationToken.None),
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
            () => _memberCommerceService.DownloadInvoiceDocumentAsync(SelectedInvoice.Id, CancellationToken.None),
            AppResources.MemberCommerceDocumentDownloadFailed).ConfigureAwait(false);
    }

    private async Task CopyDocumentAsync(
        string referenceNumber,
        Func<Task<Darwin.Shared.Results.Result<string>>> action,
        string fallbackError)
    {
        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var result = await action().ConfigureAwait(false);
            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Value))
            {
                RunOnMain(() => ErrorMessage = result.Error ?? fallbackError);
                return;
            }

            await Clipboard.Default.SetTextAsync(result.Value).ConfigureAwait(false);
            RunOnMain(() => SuccessMessage = string.Format(AppResources.MemberCommerceDocumentCopiedFormat, referenceNumber));
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, fallbackError));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                CopyOrderDocumentCommand.RaiseCanExecuteChanged();
                CopyInvoiceDocumentCommand.RaiseCanExecuteChanged();
            });
        }
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
            CanRetryPayment = order.Actions.CanRetryPayment,
            HasDocument = !string.IsNullOrWhiteSpace(order.Actions.DocumentPath)
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
    /// <summary>Gets or sets a value indicating whether retry payment is available.</summary>
    public bool CanRetryPayment { get; set; }
    /// <summary>Gets or sets a value indicating whether a document can be copied.</summary>
    public bool HasDocument { get; set; }
    /// <summary>Gets a value indicating whether a shipping-method label should be displayed.</summary>
    public bool HasShippingMethod => !string.IsNullOrWhiteSpace(ShippingMethodText);
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
