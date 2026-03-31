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
        SendResetLinkCommand = new AsyncCommand(SendResetLinkAsync, () => IsSendReady);
    }

    /// <summary>
    /// Command bound to the "send reset instructions" button.
    /// </summary>
    public AsyncCommand SendResetLinkCommand { get; }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                RaiseReadinessChanged();
                SendResetLinkCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the reset-request form is locally ready to submit.
    /// </summary>
    public bool IsSendReady => !IsBusy && !string.IsNullOrWhiteSpace(Email);

    /// <summary>
    /// Gets a contextual helper message for the reset-request flow.
    /// </summary>
    public string ForgotPasswordReadinessMessage =>
        IsBusy
            ? AppResources.ForgotPasswordReadinessBusy
            : string.IsNullOrWhiteSpace(Email)
                ? AppResources.ForgotPasswordReadinessEmail
                : AppResources.ForgotPasswordReadinessReady;

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
        RaiseReadinessChanged();

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
            RaiseReadinessChanged();
            SendResetLinkCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Raises readiness properties together so helper text and CTA state stay consistent.
    /// </summary>
    private void RaiseReadinessChanged()
    {
        OnPropertyChanged(nameof(IsSendReady));
        OnPropertyChanged(nameof(ForgotPasswordReadinessMessage));
    }

    private static string ResolveFriendlyError(Exception ex)
    {
        return ViewModelErrorMapper.ToUserMessage(ex, AppResources.ForgotPasswordFailed);
    }
}
