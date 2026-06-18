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
using Microsoft.Maui.Media;

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
    public sealed class PhoneVerificationChannelOption
    {
        public required string Value { get; init; }

        public required string DisplayName { get; init; }
    }

    private readonly IProfileService _profileService;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;
    private readonly IConsumerPushTokenProvider _pushTokenProvider;
    private readonly IConsumerNotificationPermissionService _notificationPermissionService;
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _operationCancellation;
    private CancellationTokenSource? _pushOperationCancellation;
    private int _pushRuntimeRefreshScheduled;

    private Guid _profileId;
    private byte[]? _rowVersion;
    private bool _isLoaded;

    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string? _profileImageUrl;
    private string _phoneE164 = string.Empty;
    private string _locale = ProfileContractDefaults.DefaultLocale;
    private string _timezone = ProfileContractDefaults.DefaultTimezone;
    private string _currency = ProfileContractDefaults.DefaultCurrency;
    private bool _phoneNumberConfirmed;
    private bool _hasRequestedPhoneVerificationCode;
    private PhoneVerificationChannelOption _selectedPhoneVerificationChannel;
    private int _selectedPhoneVerificationChannelIndex;
    private string _phoneVerificationCode = string.Empty;

    private string? _successMessage;
    private string? _phoneVerificationStatusMessage;
    private string _pushRegistrationStatus = AppResources.ProfilePushRegistrationStatusIdle;
    private string? _lastPushSyncAtText;
    private bool _isPushSyncBusy;
    private string _pushPermissionStateText = AppResources.ProfilePushPermissionUnknown;
    private string _pushTokenAvailabilityText = AppResources.ProfilePushTokenAvailabilityUnknown;
    private bool _isOpeningNotificationSettings;
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
        IConsumerNotificationPermissionService notificationPermissionService,
        TimeProvider timeProvider)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));
        _pushTokenProvider = pushTokenProvider ?? throw new ArgumentNullException(nameof(pushTokenProvider));
        _notificationPermissionService = notificationPermissionService ?? throw new ArgumentNullException(nameof(notificationPermissionService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        PhoneVerificationChannelOptions =
        [
            new PhoneVerificationChannelOption
            {
                Value = "Sms",
                DisplayName = AppResources.ProfilePhoneVerificationSmsOption
            },
            new PhoneVerificationChannelOption
            {
                Value = "WhatsApp",
                DisplayName = AppResources.ProfilePhoneVerificationWhatsAppOption
            }
        ];
        _selectedPhoneVerificationChannel = PhoneVerificationChannelOptions[0];
        PhoneVerificationChannelLabels = PhoneVerificationChannelOptions
            .Select(static option => option.DisplayName)
            .ToArray();

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        SaveProfileCommand = new AsyncCommand(SaveProfileAsync, () => !IsBusy);
        UploadProfileImageCommand = new AsyncCommand(UploadProfileImageAsync, () => !IsBusy);
        RemoveProfileImageCommand = new AsyncCommand(RemoveProfileImageAsync, () => !IsBusy && HasProfileImage);
        RequestPhoneVerificationCommand = new AsyncCommand(RequestPhoneVerificationAsync, CanRunPhoneVerificationAction);
        ConfirmPhoneVerificationCommand = new AsyncCommand(ConfirmPhoneVerificationAsync, CanRunPhoneVerificationAction);
        SyncPushRegistrationCommand = new AsyncCommand(SyncPushRegistrationAsync, () => !IsPushSyncBusy);
        OpenNotificationSettingsCommand = new AsyncCommand(OpenNotificationSettingsAsync, () => !_isOpeningNotificationSettings);
    }

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveProfileCommand { get; }
    public AsyncCommand UploadProfileImageCommand { get; }
    public AsyncCommand RemoveProfileImageCommand { get; }
    public AsyncCommand RequestPhoneVerificationCommand { get; }
    public AsyncCommand ConfirmPhoneVerificationCommand { get; }
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
        set
        {
            if (SetProperty(ref _firstName, value))
            {
                OnPropertyChanged(nameof(ProfileInitials));
            }
        }
    }

    public string LastName
    {
        get => _lastName;
        set
        {
            if (SetProperty(ref _lastName, value))
            {
                OnPropertyChanged(nameof(ProfileInitials));
            }
        }
    }

    public string? ProfileImageUrl
    {
        get => _profileImageUrl;
        private set
        {
            if (SetProperty(ref _profileImageUrl, string.IsNullOrWhiteSpace(value) ? null : value))
            {
                OnPropertyChanged(nameof(HasProfileImage));
                RemoveProfileImageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasProfileImage => !string.IsNullOrWhiteSpace(ProfileImageUrl);

    public string ProfileInitials => BuildInitials(FirstName, LastName, Email);

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

    public bool PhoneNumberConfirmed
    {
        get => _phoneNumberConfirmed;
        private set
        {
            if (SetProperty(ref _phoneNumberConfirmed, value))
            {
                OnPropertyChanged(nameof(PhoneVerificationReadinessText));
                OnPropertyChanged(nameof(ShouldShowPhoneVerificationRequest));
                OnPropertyChanged(nameof(ShouldShowPhoneVerificationCodeEntry));
                RequestPhoneVerificationCommand.RaiseCanExecuteChanged();
                ConfirmPhoneVerificationCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasRequestedPhoneVerificationCode
    {
        get => _hasRequestedPhoneVerificationCode;
        private set
        {
            if (SetProperty(ref _hasRequestedPhoneVerificationCode, value))
            {
                OnPropertyChanged(nameof(ShouldShowPhoneVerificationRequest));
                OnPropertyChanged(nameof(ShouldShowPhoneVerificationCodeEntry));
            }
        }
    }

    public bool ShouldShowPhoneVerificationRequest => !PhoneNumberConfirmed && !HasRequestedPhoneVerificationCode;

    public bool ShouldShowPhoneVerificationCodeEntry => !PhoneNumberConfirmed && HasRequestedPhoneVerificationCode;

    public IReadOnlyList<PhoneVerificationChannelOption> PhoneVerificationChannelOptions { get; }

    /// <summary>
    /// Gets the localized labels used by the phone-verification channel segmented control.
    /// </summary>
    public IReadOnlyList<string> PhoneVerificationChannelLabels { get; }

    public PhoneVerificationChannelOption SelectedPhoneVerificationChannel
    {
        get => _selectedPhoneVerificationChannel;
        set
        {
            if (!SetProperty(ref _selectedPhoneVerificationChannel, value))
            {
                return;
            }

            SyncSelectedPhoneVerificationChannelIndex(value);
        }
    }

    /// <summary>
    /// Gets or sets the selected phone-verification channel index for segmented control binding.
    /// </summary>
    public int SelectedPhoneVerificationChannelIndex
    {
        get => _selectedPhoneVerificationChannelIndex;
        set
        {
            if (!SetProperty(ref _selectedPhoneVerificationChannelIndex, value))
            {
                return;
            }

            if ((uint)value >= (uint)PhoneVerificationChannelOptions.Count)
            {
                return;
            }

            SelectedPhoneVerificationChannel = PhoneVerificationChannelOptions[value];
        }
    }

    public string PhoneVerificationCode
    {
        get => _phoneVerificationCode;
        set
        {
            if (SetProperty(ref _phoneVerificationCode, value))
            {
                ConfirmPhoneVerificationCommand.RaiseCanExecuteChanged();
            }
        }
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

    public string? PhoneVerificationStatusMessage
    {
        get => _phoneVerificationStatusMessage;
        private set
        {
            if (SetProperty(ref _phoneVerificationStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasPhoneVerificationStatus));
            }
        }
    }

    public bool HasPhoneVerificationStatus => !string.IsNullOrWhiteSpace(PhoneVerificationStatusMessage);

    public string PhoneVerificationReadinessText =>
        PhoneNumberConfirmed
            ? AppResources.ProfilePhoneVerificationConfirmed
            : AppResources.ProfilePhoneVerificationPending;

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
        if (_isLoaded)
        {
            SchedulePushRuntimeStateRefresh();
            return;
        }

        await RefreshAsync();
        _isLoaded = true;
        SchedulePushRuntimeStateRefresh();
    }

    /// <summary>
    /// Cancels any in-flight profile or push operation when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        CancelCurrentPushOperation();
        EndProfileBusyState();
        RunOnMain(() => IsPushSyncBusy = false);
        return Task.CompletedTask;
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

        var operationCancellation = BeginCurrentOperation();
        try
        {
            await LoadProfileSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from profile intentionally cancels stale profile loads.
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
                RequestPhoneVerificationCommand.RaiseCanExecuteChanged();
                ConfirmPhoneVerificationCommand.RaiseCanExecuteChanged();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Loads profile identity, row version, address summary, and CRM context without checking the outer busy state.
    /// This is used both by normal refresh and post-save reload so optimistic concurrency metadata is always current.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used by all profile-related service calls.</param>
    private async Task LoadProfileSnapshotAsync(CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetMeAsync(cancellationToken).ConfigureAwait(false);
        if (profile is null)
        {
            RunOnMain(() => ErrorMessage = AppResources.ProfileLoadFailed);
            return;
        }

        RunOnMain(() =>
        {
            _profileId = profile.Id;
            _rowVersion = profile.RowVersion?.ToArray() ?? Array.Empty<byte>();

            Email = profile.Email ?? string.Empty;
            FirstName = profile.FirstName ?? string.Empty;
            LastName = profile.LastName ?? string.Empty;
            ProfileImageUrl = profile.ProfileImageUrl;
            PhoneE164 = profile.PhoneE164 ?? string.Empty;
            PhoneNumberConfirmed = profile.PhoneNumberConfirmed;
            HasRequestedPhoneVerificationCode = false;
            Locale = string.IsNullOrWhiteSpace(profile.Locale) ? ProfileContractDefaults.DefaultLocale : profile.Locale;
            Timezone = string.IsNullOrWhiteSpace(profile.Timezone) ? ProfileContractDefaults.DefaultTimezone : profile.Timezone;
            Currency = string.IsNullOrWhiteSpace(profile.Currency) ? ProfileContractDefaults.DefaultCurrency : profile.Currency;
        });

        await LoadAddressBookSummaryAsync(cancellationToken).ConfigureAwait(false);
        await LoadLinkedCustomerContextAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task LoadAddressBookSummaryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var addresses = await _profileService.GetAddressesAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task LoadLinkedCustomerContextAsync(CancellationToken cancellationToken)
    {
        try
        {
            var context = await _profileService.GetLinkedCustomerContextAsync(cancellationToken).ConfigureAwait(false);
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

        var operationCancellation = BeginCurrentOperation();
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

            var updateResult = await _profileService.UpdateMeAsync(request, operationCancellation.Token);
            if (!updateResult.Succeeded)
            {
                var failureMessage = ResolveProfileSaveFailureMessage(updateResult.Error);
                RunOnMain(() => ErrorMessage = failureMessage);
                return;
            }

            RunOnMain(() => SuccessMessage = AppResources.ProfileSaveSuccess);

            await LoadProfileSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
            _isLoaded = true;
        }
        catch (OperationCanceledException)
        {
            // Navigation away from profile intentionally cancels stale profile saves.
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
                RequestPhoneVerificationCommand.RaiseCanExecuteChanged();
                ConfirmPhoneVerificationCommand.RaiseCanExecuteChanged();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task RequestPhoneVerificationAsync()
    {
        if (!CanRunPhoneVerificationAction())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(PhoneE164))
        {
            RunOnMain(() => PhoneVerificationStatusMessage = AppResources.ProfilePhoneVerificationPhoneRequired);
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            PhoneVerificationStatusMessage = null;
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            var result = await _profileService.RequestPhoneVerificationAsync(
                new RequestPhoneVerificationRequest { Channel = SelectedPhoneVerificationChannel.Value },
                operationCancellation.Token).ConfigureAwait(false);

            RunOnMain(() =>
            {
                PhoneVerificationStatusMessage = result.Succeeded
                    ? string.Format(AppResources.ProfilePhoneVerificationCodeRequested, SelectedPhoneVerificationChannel.DisplayName)
                    : AppResources.ProfilePhoneVerificationRequestFailed;
                HasRequestedPhoneVerificationCode = result.Succeeded;
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from profile intentionally cancels stale phone verification requests.
        }
        catch (Exception ex)
        {
            RunOnMain(() => PhoneVerificationStatusMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfilePhoneVerificationRequestFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                SaveProfileCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
                RequestPhoneVerificationCommand.RaiseCanExecuteChanged();
                ConfirmPhoneVerificationCommand.RaiseCanExecuteChanged();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task UploadProfileImageAsync()
    {
        if (IsBusy)
        {
            return;
        }

        FileResult? picked;
        try
        {
            var pickedPhotos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = AppResources.ProfileAvatarUploadButton
            }).ConfigureAwait(false);
            picked = pickedPhotos.FirstOrDefault();
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfileAvatarUploadFailed));
            return;
        }

        if (picked is null)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            await using var stream = await picked.OpenReadAsync().ConfigureAwait(false);
            var upload = await _profileService.UploadAvatarAsync(
                stream,
                string.IsNullOrWhiteSpace(picked.FileName) ? "avatar.jpg" : picked.FileName,
                string.IsNullOrWhiteSpace(picked.ContentType) ? "image/jpeg" : picked.ContentType,
                operationCancellation.Token).ConfigureAwait(false);

            if (!upload.Succeeded || upload.Value is null || string.IsNullOrWhiteSpace(upload.Value.Url))
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileAvatarUploadFailed);
                return;
            }

            var setResult = await _profileService.SetAvatarAsync(upload.Value.Url, operationCancellation.Token).ConfigureAwait(false);
            if (!setResult.Succeeded)
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileAvatarSaveFailed);
                return;
            }

            RunOnMain(() =>
            {
                ProfileImageUrl = upload.Value.Url;
                SuccessMessage = AppResources.ProfileAvatarSaveSuccess;
            });

            await LoadProfileSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from profile intentionally cancels stale avatar uploads.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfileAvatarUploadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseProfileCommandStates();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task RemoveProfileImageAsync()
    {
        if (IsBusy || !HasProfileImage)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            var result = await _profileService.SetAvatarAsync(null, operationCancellation.Token).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileAvatarRemoveFailed);
                return;
            }

            RunOnMain(() =>
            {
                ProfileImageUrl = null;
                SuccessMessage = AppResources.ProfileAvatarRemoveSuccess;
            });

            await LoadProfileSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from profile intentionally cancels stale avatar updates.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfileAvatarRemoveFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseProfileCommandStates();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task ConfirmPhoneVerificationAsync()
    {
        if (!CanRunPhoneVerificationAction())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(PhoneVerificationCode))
        {
            RunOnMain(() => PhoneVerificationStatusMessage = AppResources.ProfilePhoneVerificationCodeRequired);
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            PhoneVerificationStatusMessage = null;
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            var result = await _profileService.ConfirmPhoneVerificationAsync(
                new ConfirmPhoneVerificationRequest { Code = PhoneVerificationCode.Trim() },
                operationCancellation.Token).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => PhoneVerificationStatusMessage = AppResources.ProfilePhoneVerificationConfirmFailed);
                return;
            }

            RunOnMain(() =>
            {
                PhoneVerificationCode = string.Empty;
                PhoneVerificationStatusMessage = AppResources.ProfilePhoneVerificationConfirmSuccess;
                HasRequestedPhoneVerificationCode = false;
            });

            await LoadProfileSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
            _isLoaded = true;
        }
        catch (OperationCanceledException)
        {
            // Navigation away from profile intentionally cancels stale phone verification confirmations.
        }
        catch (Exception ex)
        {
            RunOnMain(() => PhoneVerificationStatusMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfilePhoneVerificationConfirmFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseProfileCommandStates();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task SyncPushRegistrationAsync()
    {
        if (IsPushSyncBusy)
        {
            return;
        }

        RunOnMain(() => IsPushSyncBusy = true);

        var pushOperationCancellation = BeginCurrentPushOperation();
        try
        {
            var permissionResult = await _notificationPermissionService
                .EnsurePermissionAsync(pushOperationCancellation.Token)
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
                .TryRegisterCurrentDeviceAsync(pushOperationCancellation.Token)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
                PushRegistrationStatus = result.Succeeded
                    ? AppResources.ProfilePushRegistrationStatusSuccess
                    : AppResources.ProfilePushRegistrationStatusFailed;

                LastPushSyncAtText = string.Format(
                    AppResources.ProfilePushRegistrationLastSyncFormat,
                    _timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm"));
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from profile intentionally cancels stale push sync work.
        }
        catch (Exception ex)
        {
            RunOnMain(() =>
            {
                PushRegistrationStatus = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfilePushRegistrationStatusFailed);
                LastPushSyncAtText = string.Format(
                    AppResources.ProfilePushRegistrationLastSyncFormat,
                    _timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm"));
            });
        }
        finally
        {
            if (!pushOperationCancellation.IsCancellationRequested)
            {
                await RefreshPushRuntimeStateAsync(pushOperationCancellation.Token);
            }

            RunOnMain(() => IsPushSyncBusy = false);
            EndCurrentPushOperation(pushOperationCancellation);
        }
    }

    /// <summary>
    /// Refreshes local push runtime diagnostics outside a user operation scope.
    /// This is used by appearance refreshes where the UI should report current device state independently from edits.
    /// </summary>
    private Task RefreshPushRuntimeStateAsync() => RefreshPushRuntimeStateAsync(CancellationToken.None);

    private void SchedulePushRuntimeStateRefresh()
    {
        if (Interlocked.Exchange(ref _pushRuntimeRefreshScheduled, 1) == 1)
        {
            return;
        }

        _ = RefreshPushRuntimeStateDeferredAsync();
    }

    private async Task RefreshPushRuntimeStateDeferredAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await RefreshPushRuntimeStateAsync().ConfigureAwait(false);
        }
        catch
        {
            // Push diagnostics are non-critical and must never affect profile responsiveness.
        }
        finally
        {
            Interlocked.Exchange(ref _pushRuntimeRefreshScheduled, 0);
        }
    }

    private async Task RefreshPushRuntimeStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var runtimeStateResult = await _pushTokenProvider.GetCurrentAsync(cancellationToken).ConfigureAwait(false);

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
        if (_isOpeningNotificationSettings)
        {
            return Task.CompletedTask;
        }

        RunOnMain(() =>
        {
            _isOpeningNotificationSettings = true;
            OpenNotificationSettingsCommand.RaiseCanExecuteChanged();
        });

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
            finally
            {
                _isOpeningNotificationSettings = false;
                OpenNotificationSettingsCommand.RaiseCanExecuteChanged();
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts a cancellable profile operation and cancels any stale profile operation still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active profile operation without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var current = Interlocked.Exchange(ref _operationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed profile operation when it still owns the active operation slot.
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

    /// <summary>
    /// Starts a cancellable push operation and cancels any stale push operation still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentPushOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _pushOperationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active push operation without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentPushOperation()
    {
        var current = Interlocked.Exchange(ref _pushOperationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed push operation when it still owns the active operation slot.
    /// </summary>
    /// <param name="pushOperationCancellation">Completed push operation token source.</param>
    private void EndCurrentPushOperation(CancellationTokenSource pushOperationCancellation)
    {
        if (ReferenceEquals(_pushOperationCancellation, pushOperationCancellation))
        {
            _pushOperationCancellation = null;
        }

        pushOperationCancellation.Dispose();
    }

    /// <summary>
    /// Clears profile busy state and refreshes profile command availability.
    /// </summary>
    private void EndProfileBusyState()
    {
        RunOnMain(() =>
        {
            IsBusy = false;
            RaiseProfileCommandStates();
        });
    }

    private void RaiseProfileCommandStates()
    {
        SaveProfileCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
        UploadProfileImageCommand.RaiseCanExecuteChanged();
        RemoveProfileImageCommand.RaiseCanExecuteChanged();
        RequestPhoneVerificationCommand.RaiseCanExecuteChanged();
        ConfirmPhoneVerificationCommand.RaiseCanExecuteChanged();
    }

    private bool ValidateProfileFields()
    {
        return !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               !string.IsNullOrWhiteSpace(Locale) &&
               !string.IsNullOrWhiteSpace(Timezone) &&
               !string.IsNullOrWhiteSpace(Currency);
    }

    private bool CanRunPhoneVerificationAction()
    {
        return !IsBusy && !PhoneNumberConfirmed;
    }

    /// <summary>
    /// Keeps the segmented control index synchronized when the selected channel changes programmatically.
    /// </summary>
    /// <param name="channel">The selected verification channel.</param>
    private void SyncSelectedPhoneVerificationChannelIndex(PhoneVerificationChannelOption channel)
    {
        var index = -1;
        for (var i = 0; i < PhoneVerificationChannelOptions.Count; i++)
        {
            if (ReferenceEquals(PhoneVerificationChannelOptions[i], channel) ||
                string.Equals(PhoneVerificationChannelOptions[i].Value, channel.Value, StringComparison.Ordinal))
            {
                index = i;
                break;
            }
        }

        if (index >= 0 && _selectedPhoneVerificationChannelIndex != index)
        {
            _selectedPhoneVerificationChannelIndex = index;
            OnPropertyChanged(nameof(SelectedPhoneVerificationChannelIndex));
        }
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string BuildInitials(string? firstName, string? lastName, string? email)
    {
        var first = Normalize(firstName);
        var last = Normalize(lastName);
        if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
        {
            return string.Concat(TakeFirstTextElement(first), TakeFirstTextElement(last)).ToUpperInvariant();
        }

        var fallback = Normalize(first) ?? Normalize(last) ?? Normalize(email)?.Split('@')[0];
        if (string.IsNullOrWhiteSpace(fallback))
        {
            return "US";
        }

        return new string(fallback.Where(char.IsLetterOrDigit).Take(2).ToArray()).ToUpperInvariant();
    }

    private static string TakeFirstTextElement(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(value.Trim());
        return enumerator.MoveNext() ? enumerator.GetTextElement() ?? string.Empty : string.Empty;
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
