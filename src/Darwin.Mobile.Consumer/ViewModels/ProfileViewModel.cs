using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles profile read/update flows for the consumer app.
/// This view model also exposes a secure password change action for the authenticated user.
/// </summary>
public sealed class ProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;

    private byte[]? _rowVersion;
    private bool _isLoaded;

    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _phoneE164 = string.Empty;
    private string _locale = string.Empty;
    private string _timezone = string.Empty;
    private string _currency = string.Empty;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;

    private string? _successMessage;

    public ProfileViewModel(IProfileService profileService, IAuthService authService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        SaveProfileCommand = new AsyncCommand(SaveProfileAsync, () => !IsBusy && CanSaveProfile());
        ChangePasswordCommand = new AsyncCommand(ChangePasswordAsync, () => !IsBusy && CanChangePassword());
    }

    public AsyncCommand RefreshCommand { get; }

    public AsyncCommand SaveProfileCommand { get; }

    public AsyncCommand ChangePasswordCommand { get; }

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
                SaveProfileCommand.RaiseCanExecuteChanged();
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
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string PhoneE164
    {
        get => _phoneE164;
        set
        {
            if (SetProperty(ref _phoneE164, value))
            {
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Locale
    {
        get => _locale;
        set
        {
            if (SetProperty(ref _locale, value))
            {
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Timezone
    {
        get => _timezone;
        set
        {
            if (SetProperty(ref _timezone, value))
            {
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Currency
    {
        get => _currency;
        set
        {
            if (SetProperty(ref _currency, value))
            {
                SaveProfileCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            if (SetProperty(ref _currentPassword, value))
            {
                ChangePasswordCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                ChangePasswordCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set
        {
            if (SetProperty(ref _confirmNewPassword, value))
            {
                ChangePasswordCommand.RaiseCanExecuteChanged();
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

        IsBusy = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            var profile = await _profileService.GetMeAsync(CancellationToken.None).ConfigureAwait(false);
            if (profile is null)
            {
                ErrorMessage = AppResources.ProfileLoadFailed;
                return;
            }

            RunOnMain(() =>
            {
                Email = profile.Email ?? string.Empty;
                FirstName = profile.FirstName ?? string.Empty;
                LastName = profile.LastName ?? string.Empty;
                PhoneE164 = profile.PhoneE164 ?? string.Empty;
                Locale = profile.Locale ?? string.Empty;
                Timezone = profile.Timezone ?? string.Empty;
                Currency = profile.Currency ?? string.Empty;
                _rowVersion = profile.RowVersion;

                SaveProfileCommand.RaiseCanExecuteChanged();
                ChangePasswordCommand.RaiseCanExecuteChanged();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            SaveProfileCommand.RaiseCanExecuteChanged();
            ChangePasswordCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanSaveProfile()
    {
        return !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName);
    }

    private async Task SaveProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;

        if (!CanSaveProfile())
        {
            ErrorMessage = AppResources.ProfileRequiredNames;
            return;
        }

        IsBusy = true;
        SaveProfileCommand.RaiseCanExecuteChanged();
        ChangePasswordCommand.RaiseCanExecuteChanged();

        try
        {
            var request = new CustomerProfile
            {
                Email = Email,
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                PhoneE164 = Normalize(PhoneE164),
                Locale = Normalize(Locale),
                Timezone = Normalize(Timezone),
                Currency = Normalize(Currency),
                RowVersion = _rowVersion
            };

            var updated = await _profileService.UpdateMeAsync(request, CancellationToken.None).ConfigureAwait(false);
            if (!updated)
            {
                ErrorMessage = AppResources.ProfileSaveFailed;
                return;
            }

            SuccessMessage = AppResources.ProfileSaveSuccess;

            // Reload the profile to refresh row version and server-side normalized values.
            _isLoaded = false;
            await RefreshAsync().ConfigureAwait(false);
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            SaveProfileCommand.RaiseCanExecuteChanged();
            ChangePasswordCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanChangePassword()
    {
        return !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmNewPassword);
    }

    private async Task ChangePasswordAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        SuccessMessage = null;

        if (!CanChangePassword())
        {
            ErrorMessage = AppResources.PasswordRequired;
            return;
        }

        if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
        {
            ErrorMessage = AppResources.PasswordMismatch;
            return;
        }

        if (NewPassword.Length < 8)
        {
            ErrorMessage = AppResources.PasswordMinLength;
            return;
        }

        IsBusy = true;
        SaveProfileCommand.RaiseCanExecuteChanged();
        ChangePasswordCommand.RaiseCanExecuteChanged();

        try
        {
            var changed = await _authService
                .ChangePasswordAsync(CurrentPassword, NewPassword, CancellationToken.None)
                .ConfigureAwait(false);

            if (!changed)
            {
                ErrorMessage = AppResources.PasswordChangeFailed;
                return;
            }

            // Clear sensitive fields immediately after success.
            RunOnMain(() =>
            {
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
            });

            SuccessMessage = AppResources.PasswordChangeSuccess;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            SaveProfileCommand.RaiseCanExecuteChanged();
            ChangePasswordCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
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
}
