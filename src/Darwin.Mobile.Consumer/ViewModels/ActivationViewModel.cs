using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles the consumer account activation lifecycle: request a new email or confirm an account with email + token.
/// </summary>
public sealed class ActivationViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private string _email = string.Empty;
    private string _token = string.Empty;
    private string? _successMessage;

    public ActivationViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        RequestActivationEmailCommand = new AsyncCommand(RequestActivationEmailAsync, () => !IsBusy);
        ConfirmEmailCommand = new AsyncCommand(ConfirmEmailAsync, () => !IsBusy);
    }

    public AsyncCommand RequestActivationEmailCommand { get; }

    public AsyncCommand ConfirmEmailCommand { get; }

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

    private async Task RequestActivationEmailAsync()
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

            var requested = await _authService.RequestEmailConfirmationAsync(Email.Trim(), CancellationToken.None);
            if (!requested)
            {
                ErrorMessage = AppResources.ActivationEmailRequestFailed;
                return;
            }

            SuccessMessage = AppResources.ActivationEmailSent;
        }
        catch (Exception ex)
        {
            ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ActivationEmailRequestFailed);
        }
        finally
        {
            IsBusy = false;
            RequestActivationEmailCommand.RaiseCanExecuteChanged();
            ConfirmEmailCommand.RaiseCanExecuteChanged();
        }
    }

    private async Task ConfirmEmailAsync()
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
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Token))
            {
                ErrorMessage = AppResources.ActivationEmailTokenRequired;
                return;
            }

            var confirmed = await _authService.ConfirmEmailAsync(Email.Trim(), Token.Trim(), CancellationToken.None);
            if (!confirmed)
            {
                ErrorMessage = AppResources.ActivationConfirmFailed;
                return;
            }

            SuccessMessage = AppResources.ActivationConfirmSuccess;
            Token = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ActivationConfirmFailed);
        }
        finally
        {
            IsBusy = false;
            RequestActivationEmailCommand.RaiseCanExecuteChanged();
            ConfirmEmailCommand.RaiseCanExecuteChanged();
        }
    }
}
