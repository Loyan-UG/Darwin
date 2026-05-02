using Darwin.Contracts.Billing;
using Darwin.Contracts.Common;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Reporting;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Common;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Presents a read-only subscription snapshot for the Business app.
/// 
/// Product policy:
/// - Mobile operators may view the current subscription state only.
/// - Plan changes, payment methods, invoicing, and cancellation management are intentionally
///   moved to the public Loyan website instead of being executed inside the mobile app.
/// - The mobile experience therefore exposes only a status refresh action and a website link.
/// </summary>
public sealed class SubscriptionViewModel : BaseViewModel
{
    private readonly ApiOptions _apiOptions;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IBusinessActivityTracker _activityTracker;
    private CancellationTokenSource? _operationCancellation;

    private bool _hasSubscriptionStatus;
    private string _subscriptionSummaryText = string.Empty;
    private string _subscriptionDatesText = string.Empty;
    private string _subscriptionPolicyNotice = string.Empty;
    private bool _isManagementWebsiteConfigured;
    private string _managementWebsiteHint = string.Empty;
    private string _managementWebsiteUrlText = string.Empty;
    private string _managementWebsiteDetails = string.Empty;

    public SubscriptionViewModel(
        ApiOptions apiOptions,
        ILoyaltyService loyaltyService,
        IBusinessActivityTracker activityTracker)
    {
        _apiOptions = apiOptions ?? throw new ArgumentNullException(nameof(apiOptions));
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _activityTracker = activityTracker ?? throw new ArgumentNullException(nameof(activityTracker));

        RefreshSubscriptionStatusCommand = new AsyncCommand(RefreshSubscriptionStatusAsync, () => !IsBusy);
        OpenManagementWebsiteCommand = new AsyncCommand(OpenManagementWebsiteAsync, () => !IsBusy && IsManagementWebsiteConfigured);
    }

    /// <summary>
    /// Gets a value indicating whether the current business has a subscription snapshot available.
    /// </summary>
    public bool HasSubscriptionStatus
    {
        get => _hasSubscriptionStatus;
        private set => SetProperty(ref _hasSubscriptionStatus, value);
    }

    /// <summary>
    /// Gets the localized one-line summary of the current subscription plan.
    /// </summary>
    public string SubscriptionSummaryText
    {
        get => _subscriptionSummaryText;
        private set => SetProperty(ref _subscriptionSummaryText, value);
    }

    /// <summary>
    /// Gets the localized secondary text containing current period/trial dates.
    /// </summary>
    public string SubscriptionDatesText
    {
        get => _subscriptionDatesText;
        private set => SetProperty(ref _subscriptionDatesText, value);
    }

    /// <summary>
    /// Gets a policy reminder explaining that management actions moved to the website.
    /// </summary>
    public string SubscriptionPolicyNotice
    {
        get => _subscriptionPolicyNotice;
        private set => SetProperty(ref _subscriptionPolicyNotice, value);
    }

    /// <summary>
    /// Gets a value indicating whether the management website URL passed validation.
    /// </summary>
    public bool IsManagementWebsiteConfigured
    {
        get => _isManagementWebsiteConfigured;
        private set
        {
            if (SetProperty(ref _isManagementWebsiteConfigured, value))
            {
                OpenManagementWebsiteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the explanatory text shown above the management website action.
    /// </summary>
    public string ManagementWebsiteHint
    {
        get => _managementWebsiteHint;
        private set => SetProperty(ref _managementWebsiteHint, value);
    }

    /// <summary>
    /// Gets the absolute management website URL rendered in the page.
    /// </summary>
    public string ManagementWebsiteUrlText
    {
        get => _managementWebsiteUrlText;
        private set => SetProperty(ref _managementWebsiteUrlText, value);
    }

    /// <summary>
    /// Gets additional diagnostics about the configured management website host.
    /// </summary>
    public string ManagementWebsiteDetails
    {
        get => _managementWebsiteDetails;
        private set => SetProperty(ref _managementWebsiteDetails, value);
    }

    public AsyncCommand RefreshSubscriptionStatusCommand { get; }

    public AsyncCommand OpenManagementWebsiteCommand { get; }

    public override async Task OnAppearingAsync()
    {
        ApplyManagementWebsiteState();
        await RefreshSubscriptionStatusAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Cancels any in-flight subscription status operation when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        RunOnMain(() =>
        {
            IsBusy = false;
            RaiseCommandStates();
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads the server-backed subscription snapshot so operators can confirm the current
    /// plan, provider, price, and renewal window without leaving the app.
    /// </summary>
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

        var operationCancellation = BeginCurrentOperation();
        try
        {
            var cancellationToken = operationCancellation.Token;
            var result = await _loyaltyService
                .GetCurrentBusinessSubscriptionStatusAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded || result.Value is null)
            {
                await TrackSubscriptionStatusRefreshAsync(succeeded: false, cancellationToken).ConfigureAwait(false);

                RunOnMain(() =>
                {
                    HasSubscriptionStatus = false;
                    SubscriptionSummaryText = AppResources.SubscriptionStatusUnavailable;
                    SubscriptionDatesText = string.Empty;
                    SubscriptionPolicyNotice = AppResources.SubscriptionReadOnlyNotice;
                    ErrorMessage = result.Error ?? AppResources.SubscriptionStatusUnavailable;
                });
                return;
            }

            await TrackSubscriptionStatusRefreshAsync(succeeded: true, cancellationToken).ConfigureAwait(false);
            RunOnMain(() => ApplySubscriptionStatus(result.Value));
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the subscription page intentionally cancels stale refresh work.
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });

            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Opens the public Loyan website where subscription changes and financial operations are handled.
    /// </summary>
    private async Task OpenManagementWebsiteAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var websiteValidation = ValidateManagementWebsiteConfiguration();
        if (websiteValidation.WebsiteUri is null)
        {
            RunOnMain(() => ErrorMessage = websiteValidation.Details);
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
            await Browser.OpenAsync(websiteValidation.WebsiteUri, BrowserLaunchMode.SystemPreferred);
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.SubscriptionManagementWebsiteOpenFailed);
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

    /// <summary>
    /// Applies the server snapshot to the UI while keeping management actions read-only.
    /// </summary>
    private void ApplySubscriptionStatus(BusinessSubscriptionStatusResponse status)
    {
        if (!status.HasSubscription)
        {
            HasSubscriptionStatus = false;
            SubscriptionSummaryText = AppResources.SubscriptionNoActivePlan;
            SubscriptionDatesText = string.Empty;
            SubscriptionPolicyNotice = AppResources.SubscriptionReadOnlyNotice;
            return;
        }

        HasSubscriptionStatus = true;

        var planName = string.IsNullOrWhiteSpace(status.PlanName) ? status.PlanCode : status.PlanName;
        if (string.IsNullOrWhiteSpace(planName))
        {
            planName = AppResources.SubscriptionUnknownPlan;
        }

        var provider = string.IsNullOrWhiteSpace(status.Provider)
            ? AppResources.SubscriptionUnknownProvider
            : status.Provider;
        var statusName = string.IsNullOrWhiteSpace(status.Status)
            ? AppResources.SubscriptionUnknownStatus
            : ResolveSubscriptionStatusDisplayName(status.Status);

        SubscriptionSummaryText = string.Format(
            AppResources.SubscriptionStatusSummaryFormat,
            planName,
            statusName,
            provider,
            FormatMoney(status.UnitPriceMinor, status.Currency));

        var periodEnd = status.CurrentPeriodEndUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            ?? AppResources.SubscriptionDateUnknown;
        var trialEnd = status.TrialEndsAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            ?? AppResources.SubscriptionDateUnknown;

        SubscriptionDatesText = string.Format(
            AppResources.SubscriptionStatusDatesFormat,
            periodEnd,
            trialEnd);

        SubscriptionPolicyNotice = status.CancelAtPeriodEnd
            ? AppResources.SubscriptionReadOnlyCancellationNotice
            : AppResources.SubscriptionReadOnlyNotice;
    }

    private static string ResolveSubscriptionStatusDisplayName(string status)
    {
        return status switch
        {
            "Trialing" => AppResources.SubscriptionStatusTrialing,
            "Active" => AppResources.SubscriptionStatusActive,
            "PastDue" => AppResources.SubscriptionStatusPastDue,
            "Canceled" => AppResources.SubscriptionStatusCanceled,
            "Unpaid" => AppResources.SubscriptionStatusUnpaid,
            "Incomplete" => AppResources.SubscriptionStatusIncomplete,
            "IncompleteExpired" => AppResources.SubscriptionStatusIncompleteExpired,
            "Paused" => AppResources.SubscriptionStatusPaused,
            _ => AppResources.SubscriptionUnknownStatus
        };
    }

    /// <summary>
    /// Evaluates the configured management website URL and prepares the page-level hint text.
    /// </summary>
    private void ApplyManagementWebsiteState()
    {
        RunOnMain(() =>
        {
            var websiteValidation = ValidateManagementWebsiteConfiguration();
            IsManagementWebsiteConfigured = websiteValidation.WebsiteUri is not null;
            ManagementWebsiteUrlText = websiteValidation.WebsiteUri?.AbsoluteUri ?? string.Empty;
            ManagementWebsiteDetails = websiteValidation.Details;
            ManagementWebsiteHint = IsManagementWebsiteConfigured
                ? AppResources.SubscriptionManagementWebsiteHint
                : AppResources.SubscriptionManagementWebsiteMissingHint;
        });
    }

    /// <summary>
    /// Validates the public Loyan website configuration.
    /// The mobile app intentionally accepts only an absolute HTTPS website URL here because
    /// no in-app billing or provider-specific direct portal flow is allowed anymore.
    /// </summary>
    private WebsiteValidationResult ValidateManagementWebsiteConfiguration()
    {
        var rawUrl = _apiOptions.BusinessManagementWebsiteUrl?.Trim();
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return new WebsiteValidationResult(null, AppResources.SubscriptionManagementWebsiteMissingUrl);
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var websiteUri))
        {
            return new WebsiteValidationResult(null, AppResources.SubscriptionManagementWebsiteInvalidUrl);
        }

        if (!string.Equals(websiteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return new WebsiteValidationResult(null, AppResources.SubscriptionManagementWebsiteRequiresHttps);
        }

        return new WebsiteValidationResult(
            websiteUri,
            string.Format(AppResources.SubscriptionManagementWebsiteReadyFormat, websiteUri.Host));
    }

    private void RaiseCommandStates()
    {
        RefreshSubscriptionStatusCommand.RaiseCanExecuteChanged();
        OpenManagementWebsiteCommand.RaiseCanExecuteChanged();
    }

    private async Task TrackSubscriptionStatusRefreshAsync(bool succeeded, CancellationToken cancellationToken)
    {
        try
        {
            await _activityTracker.RecordSubscriptionStatusRefreshAsync(succeeded, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Telemetry must never block operators from viewing the current subscription snapshot.
        }
    }

    private static string FormatMoney(long minor, string? currency)
    {
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? ContractDefaults.DefaultCurrency : currency.Trim().ToUpperInvariant();
        var major = minor / 100m;
        return $"{major:0.00} {normalizedCurrency}";
    }

    private sealed record WebsiteValidationResult(Uri? WebsiteUri, string Details);

    /// <summary>
    /// Starts a cancellable subscription operation and cancels any stale operation still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the current subscription operation without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var current = Interlocked.Exchange(ref _operationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed subscription operation when it still owns the active operation slot.
    /// </summary>
    /// <param name="operationCancellation">Completed operation token source.</param>
    private void EndCurrentOperation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_operationCancellation, operationCancellation))
        {
            _operationCancellation = null;
        }

        operationCancellation.Dispose();
    }
}
