using Darwin.Contracts.Billing;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Coordinates business subscription self-service actions from mobile settings.
/// 
/// Current scope:
/// - Display subscription status snapshot from server (plan/status/renewal fields).
/// - Let business operators toggle cancel-at-period-end intent with optimistic concurrency.
/// - Validate portal URL policy (absolute + HTTPS + optional host allowlist).
/// - Open/copy billing portal URL for secure provider-managed operations.
/// </summary>
public sealed class SubscriptionViewModel : BaseViewModel
{
    private readonly ApiOptions _apiOptions;
    private readonly ILoyaltyService _loyaltyService;

    private bool _isPortalConfigured;
    private string _portalHint = string.Empty;
    private string _portalUrlText = string.Empty;
    private string _portalConfigurationDetails = string.Empty;

    private bool _hasSubscriptionStatus;
    private string _subscriptionSummaryText = string.Empty;
    private string _subscriptionDatesText = string.Empty;
    private bool _cancelAtPeriodEnd;
    private string _availablePlansText = string.Empty;
    private readonly ObservableCollection<BillingPlanSummary> _planOptions = new();
    private BillingPlanSummary? _selectedPlan;
    private string _selectedPlanSummaryText = string.Empty;

    private Guid _subscriptionId;
    private byte[] _subscriptionRowVersion = Array.Empty<byte>();

    public SubscriptionViewModel(ApiOptions apiOptions, ILoyaltyService loyaltyService)
    {
        _apiOptions = apiOptions ?? throw new ArgumentNullException(nameof(apiOptions));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));

        RefreshSubscriptionStatusCommand = new AsyncCommand(RefreshSubscriptionStatusAsync, () => !IsBusy);
        ToggleCancelAtPeriodEndCommand = new AsyncCommand(ToggleCancelAtPeriodEndAsync, () => !IsBusy && HasSubscriptionStatus);
        StartUpgradeCheckoutCommand = new AsyncCommand(StartUpgradeCheckoutAsync, () => !IsBusy && _selectedPlan is not null);
        OpenBillingPortalCommand = new AsyncCommand(OpenBillingPortalAsync, () => !IsBusy && IsPortalConfigured);
        CopyBillingPortalUrlCommand = new AsyncCommand(CopyBillingPortalUrlAsync, () => !IsBusy && IsPortalConfigured);
    }

    public bool IsPortalConfigured
    {
        get => _isPortalConfigured;
        private set
        {
            if (SetProperty(ref _isPortalConfigured, value))
            {
                OpenBillingPortalCommand.RaiseCanExecuteChanged();
                CopyBillingPortalUrlCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string PortalHint
    {
        get => _portalHint;
        private set => SetProperty(ref _portalHint, value);
    }

    public string PortalUrlText
    {
        get => _portalUrlText;
        private set => SetProperty(ref _portalUrlText, value);
    }

    public string PortalConfigurationDetails
    {
        get => _portalConfigurationDetails;
        private set => SetProperty(ref _portalConfigurationDetails, value);
    }

    public bool HasSubscriptionStatus
    {
        get => _hasSubscriptionStatus;
        private set
        {
            if (SetProperty(ref _hasSubscriptionStatus, value))
            {
                ToggleCancelAtPeriodEndCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string SubscriptionSummaryText
    {
        get => _subscriptionSummaryText;
        private set => SetProperty(ref _subscriptionSummaryText, value);
    }

    public string SubscriptionDatesText
    {
        get => _subscriptionDatesText;
        private set => SetProperty(ref _subscriptionDatesText, value);
    }

    /// <summary>
    /// Gets compact list of available plans for upgrade/checkout decision support.
    /// </summary>
    public string AvailablePlansText
    {
        get => _availablePlansText;
        private set => SetProperty(ref _availablePlansText, value);
    }

    public string SelectedPlanSummaryText
    {
        get => _selectedPlanSummaryText;
        private set => SetProperty(ref _selectedPlanSummaryText, value);
    }

    public IReadOnlyList<BillingPlanSummary> PlanOptions => _planOptions;

    public BillingPlanSummary? SelectedPlan
    {
        get => _selectedPlan;
        set
        {
            if (!SetProperty(ref _selectedPlan, value))
            {
                return;
            }

            UpdateSelectedPlanSummary();
            StartUpgradeCheckoutCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasPlanOptions => _planOptions.Count > 0;

    /// <summary>
    /// Gets current button label for cancel-at-period-end action.
    /// </summary>
    public string ToggleCancelAtPeriodEndButtonText
        => CancelAtPeriodEnd
            ? AppResources.SubscriptionUndoCancelAtPeriodEndButton
            : AppResources.SubscriptionSetCancelAtPeriodEndButton;

    private bool CancelAtPeriodEnd
    {
        get => _cancelAtPeriodEnd;
        set
        {
            if (SetProperty(ref _cancelAtPeriodEnd, value))
            {
                OnPropertyChanged(nameof(ToggleCancelAtPeriodEndButtonText));
            }
        }
    }

    public AsyncCommand RefreshSubscriptionStatusCommand { get; }
    public AsyncCommand ToggleCancelAtPeriodEndCommand { get; }
    public AsyncCommand StartUpgradeCheckoutCommand { get; }
    public AsyncCommand OpenBillingPortalCommand { get; }
    public AsyncCommand CopyBillingPortalUrlCommand { get; }

    public override async Task OnAppearingAsync()
    {
        ApplyPortalValidationState();
        await RefreshSubscriptionStatusAsync().ConfigureAwait(false);
    }

    private async Task RefreshSubscriptionStatusAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });

        try
        {
            var result = await _loyaltyService
                .GetCurrentBusinessSubscriptionStatusAsync(CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                RunOnMain(() =>
                {
                    HasSubscriptionStatus = false;
                    SubscriptionSummaryText = AppResources.SubscriptionStatusUnavailable;
                    SubscriptionDatesText = string.Empty;
                    AvailablePlansText = AppResources.SubscriptionPlansUnavailable;
                    ErrorMessage = result.Error ?? AppResources.SubscriptionStatusUnavailable;
                });
                return;
            }

            RunOnMain(() => ApplySubscriptionStatus(result.Value));
            await LoadBillingPlansAsync().ConfigureAwait(false);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    private async Task ToggleCancelAtPeriodEndAsync()
    {
        if (IsBusy || !HasSubscriptionStatus || _subscriptionId == Guid.Empty || _subscriptionRowVersion.Length == 0)
        {
            return;
        }

        var requestedValue = !CancelAtPeriodEnd;

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });

        try
        {
            var result = await _loyaltyService
                .SetCancelAtPeriodEndAsync(
                    new SetCancelAtPeriodEndRequest
                    {
                        SubscriptionId = _subscriptionId,
                        CancelAtPeriodEnd = requestedValue,
                        RowVersion = _subscriptionRowVersion
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.SubscriptionCancelAtPeriodEndUpdateFailed);
                return;
            }

            RunOnMain(() =>
            {
                CancelAtPeriodEnd = result.Value.CancelAtPeriodEnd;
                _subscriptionRowVersion = result.Value.RowVersion ?? Array.Empty<byte>();
                PortalHint = result.Value.CancelAtPeriodEnd
                    ? AppResources.SubscriptionCancelAtPeriodEndScheduled
                    : AppResources.SubscriptionCancelAtPeriodEndCleared;
            });

            // Reload full snapshot to keep date/status fields in sync with server-side business rules.
            await RefreshSubscriptionStatusAsync().ConfigureAwait(false);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    private async Task LoadBillingPlansAsync()
    {
        var plansResult = await _loyaltyService
            .GetBillingPlansAsync(activeOnly: true, CancellationToken.None)
            .ConfigureAwait(false);

        if (!plansResult.Succeeded || plansResult.Value?.Items is null)
        {
            RunOnMain(() =>
            {
                _planOptions.Clear();
                SelectedPlan = null;
                AvailablePlansText = AppResources.SubscriptionPlansUnavailable;
                UpdateSelectedPlanSummary();
                OnPropertyChanged(nameof(HasPlanOptions));
                StartUpgradeCheckoutCommand.RaiseCanExecuteChanged();
            });
            return;
        }

        var plans = plansResult.Value.Items
            .Where(static x => x.IsActive && x.Id != Guid.Empty)
            .OrderBy(static x => x.PriceMinor)
            .ThenBy(static x => x.Name)
            .ToList();

        RunOnMain(() =>
        {
            _planOptions.Clear();
            foreach (var plan in plans)
            {
                _planOptions.Add(plan);
            }

            SelectedPlan = _planOptions.FirstOrDefault();

            AvailablePlansText = plans.Count == 0
                ? AppResources.SubscriptionPlansUnavailable
                : string.Join(Environment.NewLine, plans.Select(p => $"• {FormatPlanOption(p)}"));

            UpdateSelectedPlanSummary();
            OnPropertyChanged(nameof(HasPlanOptions));
            StartUpgradeCheckoutCommand.RaiseCanExecuteChanged();
        });
    }

    private async Task StartUpgradeCheckoutAsync()
    {
        if (IsBusy || _selectedPlan is null)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });

        try
        {
            var result = await _loyaltyService
                .CreateSubscriptionCheckoutIntentAsync(
                    new CreateSubscriptionCheckoutIntentRequest { PlanId = _selectedPlan.Id },
                    CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null || string.IsNullOrWhiteSpace(result.Value.CheckoutUrl))
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.SubscriptionPlansUnavailable);
                return;
            }

            if (!Uri.TryCreate(result.Value.CheckoutUrl, UriKind.Absolute, out var checkoutUri))
            {
                RunOnMain(() => ErrorMessage = AppResources.SubscriptionCheckoutUrlInvalid);
                return;
            }

            await Browser.OpenAsync(checkoutUri, BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionCheckoutStartFailed);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    private void ApplySubscriptionStatus(BusinessSubscriptionStatusResponse status)
    {
        _subscriptionId = status.SubscriptionId;
        _subscriptionRowVersion = status.RowVersion ?? Array.Empty<byte>();

        if (!status.HasSubscription)
        {
            HasSubscriptionStatus = false;
            CancelAtPeriodEnd = false;
            SubscriptionSummaryText = AppResources.SubscriptionNoActivePlan;
            SubscriptionDatesText = string.Empty;
            return;
        }

        HasSubscriptionStatus = true;
        CancelAtPeriodEnd = status.CancelAtPeriodEnd;

        var planName = string.IsNullOrWhiteSpace(status.PlanName) ? status.PlanCode : status.PlanName;
        if (string.IsNullOrWhiteSpace(planName))
        {
            planName = AppResources.SubscriptionUnknownPlan;
        }

        var provider = string.IsNullOrWhiteSpace(status.Provider) ? AppResources.SubscriptionUnknownProvider : status.Provider;
        var statusName = string.IsNullOrWhiteSpace(status.Status) ? AppResources.SubscriptionUnknownStatus : status.Status;

        SubscriptionSummaryText = string.Format(
            AppResources.SubscriptionStatusSummaryFormat,
            planName,
            statusName,
            provider,
            FormatMoney(status.UnitPriceMinor, status.Currency));

        var periodEnd = status.CurrentPeriodEndUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? AppResources.SubscriptionDateUnknown;
        var trialEnd = status.TrialEndsAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? AppResources.SubscriptionDateUnknown;
        SubscriptionDatesText = string.Format(AppResources.SubscriptionStatusDatesFormat, periodEnd, trialEnd);
    }

    private static string FormatMoney(long minor, string? currency)
    {
        var c = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant();
        var major = minor / 100m;
        return $"{major:0.00} {c}";
    }

    private void ApplyPortalValidationState()
    {
        RunOnMain(() =>
        {
            var portalValidation = ValidatePortalConfiguration();
            IsPortalConfigured = portalValidation.PortalUri is not null;
            PortalUrlText = portalValidation.PortalUri?.AbsoluteUri ?? string.Empty;
            PortalConfigurationDetails = portalValidation.Details;

            PortalHint = IsPortalConfigured
                ? AppResources.SubscriptionPortalReadyHint
                : AppResources.SubscriptionPortalMissingHint;
        });
    }

    private async Task OpenBillingPortalAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var portalUri = ValidatePortalConfiguration().PortalUri;
        if (portalUri is null)
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionPortalMissingHint);
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });

        try
        {
            await Browser.OpenAsync(portalUri, BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionPortalOpenFailed);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    private async Task CopyBillingPortalUrlAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var portalUri = ValidatePortalConfiguration().PortalUri;
        if (portalUri is null)
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionPortalMissingHint);
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });

        try
        {
            await Clipboard.SetTextAsync(portalUri.AbsoluteUri);
            RunOnMain(() => PortalHint = AppResources.SubscriptionPortalCopiedHint);
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionPortalCopyFailed);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    private PortalValidationResult ValidatePortalConfiguration()
    {
        var rawUrl = _apiOptions.BusinessBillingPortalUrl?.Trim();
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return new PortalValidationResult(null, AppResources.SubscriptionPortalValidationMissingUrl);
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var portalUri))
        {
            return new PortalValidationResult(null, AppResources.SubscriptionPortalValidationInvalidUrl);
        }

        if (!string.Equals(portalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return new PortalValidationResult(null, AppResources.SubscriptionPortalValidationRequiresHttps);
        }

        var allowedHosts = _apiOptions.BusinessBillingPortalAllowedHosts?
            .Where(static host => !string.IsNullOrWhiteSpace(host))
            .Select(static host => host.Trim())
            .ToArray();

        if (allowedHosts is { Length: > 0 } &&
            !allowedHosts.Any(host => string.Equals(host, portalUri.Host, StringComparison.OrdinalIgnoreCase)))
        {
            return new PortalValidationResult(
                null,
                string.Format(AppResources.SubscriptionPortalValidationHostNotAllowedFormat, portalUri.Host));
        }

        var details = string.Format(AppResources.SubscriptionPortalValidationReadyFormat, portalUri.Host);
        return new PortalValidationResult(portalUri, details);
    }

    private void RaiseCommandStates()
    {
        RefreshSubscriptionStatusCommand.RaiseCanExecuteChanged();
        ToggleCancelAtPeriodEndCommand.RaiseCanExecuteChanged();
        StartUpgradeCheckoutCommand.RaiseCanExecuteChanged();
        OpenBillingPortalCommand.RaiseCanExecuteChanged();
        CopyBillingPortalUrlCommand.RaiseCanExecuteChanged();
    }

    private sealed record PortalValidationResult(Uri? PortalUri, string Details);

    private static string FormatPlanOption(BillingPlanSummary plan)
    {
        return string.Format(
            AppResources.SubscriptionPlanLineFormat,
            !string.IsNullOrWhiteSpace(plan.Name) ? plan.Name : plan.Code,
            (plan.PriceMinor / 100m).ToString("0.00"),
            string.IsNullOrWhiteSpace(plan.Currency) ? "EUR" : plan.Currency.Trim().ToUpperInvariant(),
            plan.IntervalCount,
            string.IsNullOrWhiteSpace(plan.Interval) ? "period" : plan.Interval);
    }

    private void UpdateSelectedPlanSummary()
    {
        SelectedPlanSummaryText = _selectedPlan is null
            ? AppResources.SubscriptionCheckoutNoPlanSelected
            : string.Format(
                AppResources.SubscriptionCheckoutSelectedPlanFormat,
                !string.IsNullOrWhiteSpace(_selectedPlan.Name) ? _selectedPlan.Name : _selectedPlan.Code,
                FormatMoney(_selectedPlan.PriceMinor, _selectedPlan.Currency));
    }
}
