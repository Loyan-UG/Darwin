using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles the password reset completion flow using email + reset token + new password.
/// This is intentionally explicit so support teams can guide users who copy token-based links manually.
/// </summary>
public sealed class ResetPasswordViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    private string _email = string.Empty;
    private string _token = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;
    private string? _successMessage;

    public ResetPasswordViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        ResetPasswordCommand = new AsyncCommand(ResetPasswordAsync, () => !IsBusy);
    }

    /// <summary>
    /// Command bound to the reset action button.
    /// </summary>
    public AsyncCommand ResetPasswordCommand { get; }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set => SetProperty(ref _confirmNewPassword, value);
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

    /// <summary>
    /// Applies prefilled handoff values from earlier auth-recovery steps without overwriting
    /// fields the user has already changed on the reset page.
    /// </summary>
    public void ApplyPrefill(string? email, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(email))
        {
            Email = email.Trim();
        }

        if (string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(token))
        {
            Token = token.Trim();
        }
    }

    private async Task ResetPasswordAsync()
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
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = AppResources.EmailRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                ErrorMessage = AppResources.ResetPasswordTokenRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = AppResources.PasswordRequired;
                return;
            }

            if (NewPassword.Length < 8)
            {
                ErrorMessage = AppResources.PasswordMinLength;
                return;
            }

            if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
            {
                ErrorMessage = AppResources.PasswordMismatch;
                return;
            }

            var success = await _authService.ResetPasswordAsync(
                Email.Trim(),
                Token.Trim(),
                NewPassword,
                CancellationToken.None);

            if (!success)
            {
                ErrorMessage = AppResources.ResetPasswordFailed;
                return;
            }

            SuccessMessage = AppResources.ResetPasswordSuccess;

            // Security hygiene: clear secrets from memory-backed UI bindings after success.
            Token = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ResetPasswordFailed);
        }
        finally
        {
            IsBusy = false;
            ResetPasswordCommand.RaiseCanExecuteChanged();
        }
    }
}
