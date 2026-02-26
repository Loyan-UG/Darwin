using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Dedicated password change screen logic.
/// Separating this from profile edit avoids mixed responsibilities and keeps user feedback clearer.
/// </summary>
public sealed class ChangePasswordViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;
    private string? _successMessage;

    public ChangePasswordViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        UpdatePasswordCommand = new AsyncCommand(UpdatePasswordAsync, () => !IsBusy && CanSubmit());
    }

    public AsyncCommand UpdatePasswordCommand { get; }

    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            if (SetProperty(ref _currentPassword, value))
            {
                UpdatePasswordCommand.RaiseCanExecuteChanged();
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
                UpdatePasswordCommand.RaiseCanExecuteChanged();
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
                UpdatePasswordCommand.RaiseCanExecuteChanged();
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

    private bool CanSubmit()
    {
        return !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmNewPassword);
    }

    private async Task UpdatePasswordAsync()
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
            if (!CanSubmit())
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
                RunOnMain(() => ErrorMessage = AppResources.PasswordChangeFailed);
                return;
            }

            RunOnMain(() =>
            {
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
                SuccessMessage = AppResources.PasswordChangeSuccess;
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ex.Message);
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                UpdatePasswordCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
