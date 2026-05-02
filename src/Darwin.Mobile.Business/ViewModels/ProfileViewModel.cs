using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Handles profile load and update for Business users.
/// 
/// Important behavior:
/// - Uses server-provided Id and RowVersion for optimistic concurrency.
/// - Keeps feedback close to the primary action.
/// - Marshals UI state updates to main thread via base ViewModel helper when needed by callers.
/// </summary>
public sealed class ProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private CancellationTokenSource? _operationCancellation;

    private Guid _profileId;
    private byte[]? _rowVersion;
    private bool _loaded;

    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _phoneE164 = string.Empty;
    private string _locale = ProfileContractDefaults.DefaultLocale;
    private string _timezone = ProfileContractDefaults.DefaultTimezone;
    private string _currency = ProfileContractDefaults.DefaultCurrency;
    private string? _successMessage;

    public ProfileViewModel(IProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy);
    }

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SaveCommand { get; }

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

    public override async Task OnAppearingAsync()
    {
        if (_loaded)
        {
            return;
        }

        await RefreshAsync();
        _loaded = true;
    }

    /// <summary>
    /// Cancels any in-flight profile load/save operation when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        EndBusyState();
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
                SaveCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task SaveAsync()
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
            if (_profileId == Guid.Empty || _rowVersion is null || _rowVersion.Length == 0)
            {
                await LoadProfileSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
            }

            if (_profileId == Guid.Empty || _rowVersion is null || _rowVersion.Length == 0)
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileNotLoadedYet);
                return;
            }

            if (!ValidateRequiredFields())
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileRequiredFields);
                return;
            }

            var payload = new CustomerProfile
            {
                Id = _profileId,
                Email = Email,
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                PhoneE164 = Normalize(PhoneE164),
                Locale = Locale.Trim(),
                Timezone = Timezone.Trim(),
                Currency = Currency.Trim().ToUpperInvariant(),
                RowVersion = _rowVersion.ToArray()
            };

            var updateResult = await _profileService.UpdateMeAsync(payload, operationCancellation.Token);
            if (!updateResult.Succeeded)
            {
                RunOnMain(() => ErrorMessage = ResolveProfileSaveFailureMessage(updateResult.Error));
                return;
            }

            RunOnMain(() => SuccessMessage = AppResources.ProfileSaveSuccess);

            await LoadProfileSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
            _loaded = true;
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
                SaveCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Starts a cancellable profile operation and cancels any stale operation still in-flight.
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
    /// Clears busy state and refreshes profile command availability.
    /// </summary>
    private void EndBusyState()
    {
        RunOnMain(() =>
        {
            IsBusy = false;
            SaveCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        });
    }

    /// <summary>
    /// Loads the current profile and concurrency token without checking the outer busy state.
    /// Save uses this helper after successful update so the next save always sends the latest RowVersion.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used for the profile service call.</param>
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
            PhoneE164 = profile.PhoneE164 ?? string.Empty;
            Locale = string.IsNullOrWhiteSpace(profile.Locale) ? ProfileContractDefaults.DefaultLocale : profile.Locale;
            Timezone = string.IsNullOrWhiteSpace(profile.Timezone) ? ProfileContractDefaults.DefaultTimezone : profile.Timezone;
            Currency = string.IsNullOrWhiteSpace(profile.Currency) ? ProfileContractDefaults.DefaultCurrency : profile.Currency;
        });
    }

    private bool ValidateRequiredFields()
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

    /// <summary>
    /// Maps profile update errors to user-facing messages with explicit concurrency guidance.
    /// </summary>
    private static string ResolveProfileSaveFailureMessage(string? error)
    {
        if (LooksLikeProfileConcurrencyConflict(error))
        {
            return AppResources.ProfileConcurrencyConflict;
        }

        // Keep non-concurrency failures generic to avoid leaking internal/server details.
        return AppResources.ProfileSaveFailed;
    }

    /// <summary>
    /// Detects typical optimistic-concurrency conflict markers from API error payloads.
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
