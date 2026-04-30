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

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var profile = await _profileService.GetMeAsync(CancellationToken.None);
            if (profile is null)
            {
                ErrorMessage = AppResources.ProfileLoadFailed;
                return;
            }

            _profileId = profile.Id;
            _rowVersion = profile.RowVersion;

            Email = profile.Email ?? string.Empty;
            FirstName = profile.FirstName ?? string.Empty;
            LastName = profile.LastName ?? string.Empty;
            PhoneE164 = profile.PhoneE164 ?? string.Empty;
            Locale = string.IsNullOrWhiteSpace(profile.Locale) ? ProfileContractDefaults.DefaultLocale : profile.Locale;
            Timezone = string.IsNullOrWhiteSpace(profile.Timezone) ? ProfileContractDefaults.DefaultTimezone : profile.Timezone;
            Currency = string.IsNullOrWhiteSpace(profile.Currency) ? ProfileContractDefaults.DefaultCurrency : profile.Currency;
        }
        catch (Exception ex)
        {
            ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfileLoadFailed);
        }
        finally
        {
            IsBusy = false;
            SaveCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            if (_profileId == Guid.Empty || _rowVersion is null || _rowVersion.Length == 0)
            {
                await RefreshAsync();
            }

            if (_profileId == Guid.Empty || _rowVersion is null || _rowVersion.Length == 0)
            {
                ErrorMessage = AppResources.ProfileNotLoadedYet;
                return;
            }

            if (!ValidateRequiredFields())
            {
                ErrorMessage = AppResources.ProfileRequiredFields;
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
                RowVersion = _rowVersion
            };

            var updateResult = await _profileService.UpdateMeAsync(payload, CancellationToken.None);
            if (!updateResult.Succeeded)
            {
                ErrorMessage = ResolveProfileSaveFailureMessage(updateResult.Error);
                return;
            }

            SuccessMessage = AppResources.ProfileSaveSuccess;

            _loaded = false;
            await RefreshAsync();
            _loaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ProfileSaveFailed);
        }
        finally
        {
            IsBusy = false;
            SaveCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        }
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
