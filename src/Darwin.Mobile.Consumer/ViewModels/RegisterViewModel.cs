using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Identity;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles customer self-service registration flow.
///
/// UX and behavior notes:
/// - This view model is intentionally UI-focused and keeps user-facing feedback deterministic.
/// - Validation happens client-side first to avoid unnecessary API calls and to provide instant feedback.
/// - Server-side validation remains authoritative; client checks are only a fast first gate.
/// - Password fields are cleared after successful registration to avoid keeping sensitive data in memory-bound UI fields.
/// </summary>
public sealed class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _successMessage;

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        RegisterCommand = new AsyncCommand(RegisterAsync, () => !IsBusy);
    }

    /// <summary>
    /// Registration action command.
    /// </summary>
    public AsyncCommand RegisterCommand { get; }

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

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    /// <summary>
    /// User-facing success message after a successful registration attempt.
    /// </summary>
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

    /// <summary>
    /// Convenience flag for XAML visibility binding.
    /// </summary>
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    private async Task RegisterAsync()
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
            if (!ValidateInputs())
            {
                return;
            }

            var request = new RegisterRequest
            {
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Email = Email.Trim(),
                Password = Password
            };

            var response = await _authService.RegisterAsync(request, CancellationToken.None);
            if (response is null)
            {
                ErrorMessage = AppResources.RegisterFailed;
                return;
            }

            SuccessMessage = response.ConfirmationEmailSent
                ? AppResources.RegisterSuccessConfirmationSent
                : AppResources.RegisterSuccess;

            // Keep email for convenience (in case user wants to login immediately),
            // but clear password fields for security hygiene.
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ResolveFriendlyError(ex, AppResources.RegisterFailed);
        }
        finally
        {
            IsBusy = false;
            RegisterCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Performs light client-side validation for better UX and fewer unnecessary API calls.
    /// </summary>
    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            ErrorMessage = AppResources.FirstNameRequired;
            return false;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = AppResources.LastNameRequired;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = AppResources.EmailRequired;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = AppResources.PasswordRequired;
            return false;
        }

        if (Password.Length < 8)
        {
            ErrorMessage = AppResources.PasswordMinLength;
            return false;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = AppResources.PasswordMismatch;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Maps technical exceptions to stable, user-friendly registration messages.
    /// </summary>
    private static string ResolveFriendlyError(Exception ex, string fallback)
    {
        var raw = ex.Message ?? string.Empty;

        // Common conflict patterns for duplicate email.
        if (raw.Contains("409", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RegisterEmailAlreadyUsed;
        }

        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }
}
