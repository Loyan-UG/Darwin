using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles profile load/update flows for the consumer app.
/// </summary>
/// <remarks>
/// Design choice:
/// - This screen is profile-only (no password section) to keep UX focused.
/// - All messages are intended to be shown near the save action, not at page bottom.
/// </remarks>
public sealed class ProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;
    private readonly IConsumerPushTokenProvider _pushTokenProvider;
    private readonly IConsumerNotificationPermissionService _notificationPermissionService;

    private Guid _profileId;
    private byte[]? _rowVersion;
    private bool _isLoaded;

    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _phoneE164 = string.Empty;
    private string _locale = "de-DE";
    private string _timezone = "Europe/Berlin";
    private string _currency = "EUR";

    private string? _successMessage;
    private string _pushRegistrationStatus = AppResources.ProfilePushRegistrationStatusIdle;
    private string? _lastPushSyncAtText;
    private bool _isPushSyncBusy;
    private string _pushPermissionStateText = AppResources.ProfilePushPermissionUnknown;
    private string _pushTokenAvailabilityText = AppResources.ProfilePushTokenAvailabilityUnknown;
    private int _addressCount;
    private string? _defaultBillingAddressSummary;
    private string? _defaultShippingAddressSummary;
    private string? _linkedCustomerDisplayName;
    private string? _linkedCustomerCompanyName;
    private string? _linkedCustomerSegmentsSummary;
    private string? _linkedCustomerLastInteractionText;

    public ProfileViewModel(
        IProfileService profileService,
        IConsumerPushRegistrationCoordinator pushRegistrationCoordinator,
        IConsumerPushTokenProvider pushTokenProvider,
        IConsumerNotificationPermissionService notificationPermissionService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));
        _pushTokenProvider = pushTokenProvider ?? throw new ArgumentNullException(nameof(pushTokenProvider));
        _notificationPermissionService = notificationPermissionService ?? throw new ArgumentNullException(nameof(notificationPermissionService));

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        SaveProfileCommand = new AsyncCommand(SaveProfileAsync, () => !IsBusy);
        SyncPushRegistrationCommand = new AsyncCommand(SyncPushRegistrationAsync, () => !IsPushSyncBusy);
        OpenNotificationSettingsCommand = new AsyncCommand(OpenNotificationSettingsAsync);
    }

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveProfileCommand { get; }
    public AsyncCommand SyncPushRegistrationCommand { get; }
    public AsyncCommand OpenNotificationSettingsCommand { get; }

    public string Email
    {
        get => _email;
        private set => SetProperty(ref _email, value);
    }

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    public string PhoneE164
    {
        get => _phoneE164;
        set => SetProperty(ref _phoneE164, value);
    }

    public string Locale
    {
        get => _locale;
        set => SetProperty(ref _locale, value);
    }

    public string Timezone
    {
        get => _timezone;
        set => SetProperty(ref _timezone, value);
    }

    public string Currency
    {
        get => _currency;
        set => SetProperty(ref _currency, value);
    }

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

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    public string PushRegistrationStatus
    {
        get => _pushRegistrationStatus;
        private set => SetProperty(ref _pushRegistrationStatus, value);
    }

    public string? LastPushSyncAtText
    {
        get => _lastPushSyncAtText;
        private set
        {
            if (SetProperty(ref _lastPushSyncAtText, value))
            {
                OnPropertyChanged(nameof(HasLastPushSyncAt));
            }
        }
    }

    public bool HasLastPushSyncAt => !string.IsNullOrWhiteSpace(LastPushSyncAtText);

    public string PushPermissionStateText
    {
        get => _pushPermissionStateText;
        private set => SetProperty(ref _pushPermissionStateText, value);
    }

    public string PushTokenAvailabilityText
    {
        get => _pushTokenAvailabilityText;
        private set => SetProperty(ref _pushTokenAvailabilityText, value);
    }

    public bool IsPushSyncBusy
    {
        get => _isPushSyncBusy;
        private set
        {
            if (SetProperty(ref _isPushSyncBusy, value))
            {
                SyncPushRegistrationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the number of saved member addresses.
    /// </summary>
    public int AddressCount
    {
        get => _addressCount;
        private set => SetProperty(ref _addressCount, value);
    }

    /// <summary>
    /// Gets a compact default billing address summary.
    /// </summary>
    public string? DefaultBillingAddressSummary
    {
        get => _defaultBillingAddressSummary;
        private set
        {
            if (SetProperty(ref _defaultBillingAddressSummary, value))
            {
                OnPropertyChanged(nameof(HasDefaultBillingAddress));
            }
        }
    }

    /// <summary>
    /// Gets a compact default shipping address summary.
    /// </summary>
    public string? DefaultShippingAddressSummary
    {
        get => _defaultShippingAddressSummary;
        private set
        {
            if (SetProperty(ref _defaultShippingAddressSummary, value))
            {
                OnPropertyChanged(nameof(HasDefaultShippingAddress));
            }
        }
    }

    /// <summary>
    /// Gets the linked CRM customer display name, when one exists.
    /// </summary>
    public string? LinkedCustomerDisplayName
    {
        get => _linkedCustomerDisplayName;
        private set
        {
            if (SetProperty(ref _linkedCustomerDisplayName, value))
            {
                OnPropertyChanged(nameof(HasLinkedCustomerContext));
            }
        }
    }

    /// <summary>
    /// Gets the linked CRM customer company name, when one exists.
    /// </summary>
    public string? LinkedCustomerCompanyName
    {
        get => _linkedCustomerCompanyName;
        private set
        {
            if (SetProperty(ref _linkedCustomerCompanyName, value))
            {
                OnPropertyChanged(nameof(HasLinkedCustomerCompanyName));
            }
        }
    }

    /// <summary>
    /// Gets a compact list of active CRM segment names.
    /// </summary>
    public string? LinkedCustomerSegmentsSummary
    {
        get => _linkedCustomerSegmentsSummary;
        private set
        {
            if (SetProperty(ref _linkedCustomerSegmentsSummary, value))
            {
                OnPropertyChanged(nameof(HasLinkedCustomerSegments));
            }
        }
    }

    /// <summary>
    /// Gets the localized last-interaction summary for the linked CRM customer.
    /// </summary>
    public string? LinkedCustomerLastInteractionText
    {
        get => _linkedCustomerLastInteractionText;
        private set => SetProperty(ref _linkedCustomerLastInteractionText, value);
    }

    public bool HasDefaultBillingAddress => !string.IsNullOrWhiteSpace(DefaultBillingAddressSummary);

    public bool HasDefaultShippingAddress => !string.IsNullOrWhiteSpace(DefaultShippingAddressSummary);

    public bool HasLinkedCustomerContext => !string.IsNullOrWhiteSpace(LinkedCustomerDisplayName);

    public bool HasLinkedCustomerCompanyName => !string.IsNullOrWhiteSpace(LinkedCustomerCompanyName);

    public bool HasLinkedCustomerSegments => !string.IsNullOrWhiteSpace(LinkedCustomerSegmentsSummary);

    public override async Task OnAppearingAsync()
    {
        // Always refresh push runtime diagnostics because permission/token state can change
        // while the app was in background or after returning from system settings.
        await RefreshPushRuntimeStateAsync();

        if (_isLoaded)
        {
            return;
        }

        await RefreshAsync();
        await RefreshPushRuntimeStateAsync();
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
            var profile = await _profileService.GetMeAsync(CancellationToken.None);
            if (profile is null)
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileLoadFailed);
                return;
            }

            RunOnMain(() =>
            {
                _profileId = profile.Id;
                _rowVersion = profile.RowVersion;

                Email = profile.Email ?? string.Empty;
                FirstName = profile.FirstName ?? string.Empty;
                LastName = profile.LastName ?? string.Empty;
                PhoneE164 = profile.PhoneE164 ?? string.Empty;
                Locale = string.IsNullOrWhiteSpace(profile.Locale) ? "de-DE" : profile.Locale;
                Timezone = string.IsNullOrWhiteSpace(profile.Timezone) ? "Europe/Berlin" : profile.Timezone;
                Currency = string.IsNullOrWhiteSpace(profile.Currency) ? "EUR" : profile.Currency;
            });

            await LoadAddressBookSummaryAsync().ConfigureAwait(false);
            await LoadLinkedCustomerContextAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfileLoadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                SaveProfileCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private async Task LoadAddressBookSummaryAsync()
    {
        try
        {
            var addresses = await _profileService.GetAddressesAsync(CancellationToken.None).ConfigureAwait(false);
            var defaultBilling = addresses.FirstOrDefault(x => x.IsDefaultBilling);
            var defaultShipping = addresses.FirstOrDefault(x => x.IsDefaultShipping);

            RunOnMain(() =>
            {
                AddressCount = addresses.Count;
                DefaultBillingAddressSummary = FormatAddressSummary(defaultBilling);
                DefaultShippingAddressSummary = FormatAddressSummary(defaultShipping);
            });
        }
        catch
        {
            RunOnMain(() =>
            {
                AddressCount = 0;
                DefaultBillingAddressSummary = null;
                DefaultShippingAddressSummary = null;
            });
        }
    }

    private async Task LoadLinkedCustomerContextAsync()
    {
        try
        {
            var context = await _profileService.GetLinkedCustomerContextAsync(CancellationToken.None).ConfigureAwait(false);
            if (context is null)
            {
                ClearLinkedCustomerContext();
                return;
            }

            var segmentsSummary = context.Segments.Count == 0
                ? null
                : string.Join(", ", context.Segments.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)));
            var lastInteractionText = context.LastInteractionAtUtc.HasValue
                ? string.Format(AppResources.ProfileCustomerLastInteractionFormat, context.LastInteractionAtUtc.Value.ToLocalTime())
                : AppResources.ProfileCustomerNoInteractions;

            RunOnMain(() =>
            {
                LinkedCustomerDisplayName = context.DisplayName;
                LinkedCustomerCompanyName = context.CompanyName;
                LinkedCustomerSegmentsSummary = segmentsSummary;
                LinkedCustomerLastInteractionText = lastInteractionText;
            });
        }
        catch
        {
            ClearLinkedCustomerContext();
        }
    }

    private void ClearLinkedCustomerContext()
    {
        RunOnMain(() =>
        {
            LinkedCustomerDisplayName = null;
            LinkedCustomerCompanyName = null;
            LinkedCustomerSegmentsSummary = null;
            LinkedCustomerLastInteractionText = null;
        });
    }

    private async Task SaveProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        // If identity metadata is missing, attempt one reload before entering busy-save mode.
        if (_profileId == Guid.Empty || _rowVersion is null || _rowVersion.Length == 0)
        {
            await RefreshAsync();
        }

        if (_profileId == Guid.Empty || _rowVersion is null || _rowVersion.Length == 0)
        {
            RunOnMain(() => ErrorMessage = AppResources.ProfileNotLoadedYet);
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
            if (!ValidateProfileFields())
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileRequiredFields);
                return;
            }

            var request = new CustomerProfile
            {
                Id = _profileId,
                Email = Email,
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                PhoneE164 = Normalize(PhoneE164),
                Locale = Locale.Trim(),
                Timezone = Timezone.Trim(),
                Currency = Currency.Trim().ToUpperInvariant(),
                RowVersion = _rowVersion
            };

            var updateResult = await _profileService.UpdateMeAsync(request, CancellationToken.None);
            if (!updateResult.Succeeded)
            {
                var failureMessage = ResolveProfileSaveFailureMessage(updateResult.Error);
                RunOnMain(() => ErrorMessage = failureMessage);
                return;
            }

            RunOnMain(() => SuccessMessage = AppResources.ProfileSaveSuccess);

            _isLoaded = false;
            await RefreshAsync();
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfileSaveFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                SaveProfileCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private async Task SyncPushRegistrationAsync()
    {
        if (IsPushSyncBusy)
        {
            return;
        }

        RunOnMain(() => IsPushSyncBusy = true);

        try
        {
            var permissionResult = await _notificationPermissionService
                .EnsurePermissionAsync(CancellationToken.None)
                .ConfigureAwait(false);

            if (!permissionResult.Succeeded)
            {
                RunOnMain(() =>
                {
                    PushRegistrationStatus = AppResources.ProfilePushRegistrationStatusFailed;
                    ErrorMessage = AppResources.ProfilePushPermissionRequestFailed;
                });

                return;
            }

            if (!permissionResult.Value)
            {
                RunOnMain(() => PushRegistrationStatus = AppResources.ProfilePushPermissionNotGranted);
                return;
            }

            var result = await _pushRegistrationCoordinator
                .TryRegisterCurrentDeviceAsync(CancellationToken.None)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
                PushRegistrationStatus = result.Succeeded
                    ? AppResources.ProfilePushRegistrationStatusSuccess
                    : AppResources.ProfilePushRegistrationStatusFailed;

                LastPushSyncAtText = string.Format(
                    AppResources.ProfilePushRegistrationLastSyncFormat,
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm"));
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() =>
            {
                PushRegistrationStatus = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfilePushRegistrationStatusFailed);
                LastPushSyncAtText = string.Format(
                    AppResources.ProfilePushRegistrationLastSyncFormat,
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm"));
            });
        }
        finally
        {
            await RefreshPushRuntimeStateAsync();
            RunOnMain(() => IsPushSyncBusy = false);
        }
    }
    private async Task RefreshPushRuntimeStateAsync()
    {
        try
        {
            var runtimeStateResult = await _pushTokenProvider.GetCurrentAsync(CancellationToken.None).ConfigureAwait(false);

            if (!runtimeStateResult.Succeeded || runtimeStateResult.Value is null)
            {
                RunOnMain(() =>
                {
                    PushPermissionStateText = AppResources.ProfilePushPermissionUnknown;
                    PushTokenAvailabilityText = AppResources.ProfilePushTokenAvailabilityUnknown;
                });

                return;
            }

            var runtimeState = runtimeStateResult.Value;
            RunOnMain(() =>
            {
                PushPermissionStateText = runtimeState.NotificationsEnabled
                    ? AppResources.ProfilePushPermissionEnabled
                    : AppResources.ProfilePushPermissionDisabled;

                PushTokenAvailabilityText = string.IsNullOrWhiteSpace(runtimeState.PushToken)
                    ? AppResources.ProfilePushTokenAvailabilityMissing
                    : AppResources.ProfilePushTokenAvailabilityReady;
            });
        }
        catch
        {
            RunOnMain(() =>
            {
                PushPermissionStateText = AppResources.ProfilePushPermissionUnknown;
                PushTokenAvailabilityText = AppResources.ProfilePushTokenAvailabilityUnknown;
            });
        }
    }

    private Task OpenNotificationSettingsAsync()
    {
        RunOnMain(() =>
        {
            try
            {
                AppInfo.ShowSettingsUI();
            }
            catch
            {
                PushRegistrationStatus = AppResources.ProfilePushOpenSettingsFailed;
            }
        });

        return Task.CompletedTask;
    }

    private bool ValidateProfileFields()
    {
        return !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               !string.IsNullOrWhiteSpace(Locale) &&
               !string.IsNullOrWhiteSpace(Timezone) &&
               !string.IsNullOrWhiteSpace(Currency);
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? FormatAddressSummary(MemberAddress? address)
    {
        if (address is null)
        {
            return null;
        }

        var line = string.Join(", ",
            new[] { address.Street1, address.PostalCode, address.City }
                .Where(static value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(line) ? address.FullName : $"{address.FullName} | {line}";
    }

    /// <summary>
    /// Maps profile-update failures to user-facing messages with a dedicated optimistic-concurrency hint.
    /// </summary>
    /// <param name="error">Raw error text returned by shared profile service result.</param>
    /// <returns>Localized message suitable for inline UI feedback.</returns>
    private static string ResolveProfileSaveFailureMessage(string? error)
    {
        if (LooksLikeProfileConcurrencyConflict(error))
        {
            return AppResources.ProfileConcurrencyConflict;
        }

        // Keep non-concurrency failures generic to avoid surfacing raw server/transport details in UI.
        return AppResources.ProfileSaveFailed;
    }

    /// <summary>
    /// Detects common concurrency-conflict markers emitted by WebApi or infrastructure layers.
    /// </summary>
    private static bool LooksLikeProfileConcurrencyConflict(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("concurrency", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("rowversion", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("conflict", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("412", StringComparison.OrdinalIgnoreCase) ||
               error.Contains("409", StringComparison.OrdinalIgnoreCase);
    }
}
