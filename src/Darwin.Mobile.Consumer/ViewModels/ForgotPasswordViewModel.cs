using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles forgot-password requests for consumer users.
///
/// Security and UX notes:
/// - The backend may intentionally return a generic success behavior to prevent user enumeration.
/// - The UI mirrors that policy by showing a generic success message when the request is accepted.
/// - Validation is intentionally lightweight on client-side; server remains the source of truth.
/// </summary>
public sealed class ForgotPasswordViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private string _email = string.Empty;
    private string? _successMessage;

    public ForgotPasswordViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        SendResetLinkCommand = new AsyncCommand(SendResetLinkAsync, () => !IsBusy);
    }

    /// <summary>
    /// Command bound to the "send reset instructions" button.
    /// </summary>
    public AsyncCommand SendResetLinkCommand { get; }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
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

    private async Task SendResetLinkAsync()
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

            var normalizedEmail = Email.Trim();
            var requested = await _authService.RequestPasswordResetAsync(normalizedEmail, CancellationToken.None);

            if (!requested)
            {
                ErrorMessage = AppResources.ForgotPasswordFailed;
                return;
            }

            SuccessMessage = AppResources.ForgotPasswordSuccess;
        }
        catch (Exception ex)
        {
            ErrorMessage = ResolveFriendlyError(ex);
        }
        finally
        {
            IsBusy = false;
            SendResetLinkCommand.RaiseCanExecuteChanged();
        }
    }

    private static string ResolveFriendlyError(Exception ex)
    {
        var raw = ex.Message ?? string.Empty;

        if (raw.Contains("Network error", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("certificate", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("invalid_requesturi", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.ServerUnreachableMessage;
        }

        return AppResources.ForgotPasswordFailed;
    }
}
