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
/// Handles profile load/update and password-change flows for the consumer app.
///
/// Threading and UX rules I want to keep explicit:
/// - This ViewModel is UI-bound, therefore all UI-facing state changes must run on the main thread.
/// - Service calls can run asynchronously, but bound properties (IsBusy, ErrorMessage, etc.) are always marshalled via RunOnMain.
/// - Save command stays enabled by design (unless busy) so QA can always trigger validation and see the exact reason.
/// </summary>
public sealed class ProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;

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

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;

    private string? _successMessage;

    public ProfileViewModel(IProfileService profileService, IAuthService authService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);

        // Intentionally keep Save command available whenever we are not busy.
        // Validation is done inside SaveProfileAsync so user always gets actionable feedback.
        SaveProfileCommand = new AsyncCommand(SaveProfileAsync, () => !IsBusy);

        ChangePasswordCommand = new AsyncCommand(ChangePasswordAsync, () => !IsBusy && CanChangePassword());
    }

    public AsyncCommand RefreshCommand { get; }

    public AsyncCommand SaveProfileCommand { get; }

    public AsyncCommand ChangePasswordCommand { get; }

    /// <summary>
    /// Email is displayed as read-only in this screen.
    /// Dedicated email change flows may exist elsewhere.
    /// </summary>
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

        await RefreshAsync();
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
            var refreshed = await TryRefreshTokenIfPossibleAsync();
            if (!refreshed)
            {
                RunOnMain(() => ErrorMessage = AppResources.SessionExpiredReLogin);
                return;
            }

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

                ChangePasswordCommand.RaiseCanExecuteChanged();
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ResolveFriendlyError(ex, AppResources.ProfileLoadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                SaveProfileCommand.RaiseCanExecuteChanged();
                ChangePasswordCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private async Task SaveProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            ErrorMessage = null;
            SuccessMessage = null;
            IsBusy = true;
            SaveProfileCommand.RaiseCanExecuteChanged();
            ChangePasswordCommand.RaiseCanExecuteChanged();
        });

        try
        {
            var refreshed = await TryRefreshTokenIfPossibleAsync();
            if (!refreshed)
            {
                RunOnMain(() => ErrorMessage = AppResources.SessionExpiredReLogin);
                return;
            }

            if (_profileId == Guid.Empty || _rowVersion is null || _rowVersion.Length == 0)
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileNotLoadedYet);
                return;
            }

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

            var updated = await _profileService.UpdateMeAsync(request, CancellationToken.None);
            if (!updated)
            {
                RunOnMain(() => ErrorMessage = AppResources.ProfileSaveFailed);
                return;
            }

            RunOnMain(() => SuccessMessage = AppResources.ProfileSaveSuccess);

            // Reload to get newest RowVersion and normalized values from server.
            _isLoaded = false;
            await RefreshAsync();
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ResolveFriendlyError(ex, AppResources.ProfileSaveFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                SaveProfileCommand.RaiseCanExecuteChanged();
                ChangePasswordCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            });
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

        RunOnMain(() =>
        {
            ErrorMessage = null;
            SuccessMessage = null;
            IsBusy = true;
            SaveProfileCommand.RaiseCanExecuteChanged();
            ChangePasswordCommand.RaiseCanExecuteChanged();
        });

        try
        {
            var refreshed = await TryRefreshTokenIfPossibleAsync();
            if (!refreshed)
            {
                RunOnMain(() => ErrorMessage = AppResources.SessionExpiredReLogin);
                return;
            }

            if (!CanChangePassword())
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordRequired);
                return;
            }

            if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordMismatch);
                return;
            }

            if (NewPassword.Length < 8)
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordMinLength);
                return;
            }

            var changed = await _authService.ChangePasswordAsync(
                CurrentPassword,
                NewPassword,
                CancellationToken.None);

            if (!changed)
            {
                // This should now happen only when API really rejects credentials/policy.
                RunOnMain(() => ErrorMessage = AppResources.PasswordChangeFailed);
                return;
            }

            RunOnMain(() =>
            {
                // Clear sensitive data immediately.
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
                SuccessMessage = AppResources.PasswordChangeSuccess;
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ResolveFriendlyError(ex, AppResources.PasswordChangeFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                SaveProfileCommand.RaiseCanExecuteChanged();
                ChangePasswordCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            });
        }
    }

    /// <summary>
    /// Validates mandatory fields according to server-side expectations.
    /// </summary>
    private bool ValidateProfileFields()
    {
        return !string.IsNullOrWhiteSpace(FirstName) &&
               !string.IsNullOrWhiteSpace(LastName) &&
               !string.IsNullOrWhiteSpace(Locale) &&
               !string.IsNullOrWhiteSpace(Timezone) &&
               !string.IsNullOrWhiteSpace(Currency);
    }

    /// <summary>
    /// Tries to refresh access token before sensitive calls.
    /// If refresh fails, caller should show relogin message.
    /// </summary>
    private async Task<bool> TryRefreshTokenIfPossibleAsync()
    {
        try
        {
            return await _authService.TryRefreshAsync(CancellationToken.None);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Maps technical errors to more useful user-facing errors.
    /// </summary>
    private static string ResolveFriendlyError(Exception ex, string fallback)
    {
        var raw = ex.Message ?? string.Empty;
        if (raw.Contains("401", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.SessionExpiredReLogin;
        }

        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
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
