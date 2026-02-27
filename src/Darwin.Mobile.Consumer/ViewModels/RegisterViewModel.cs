using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Identity;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles customer self-service registration with an auto-login continuation.
///
/// UX contract:
/// - Registration should not leave the user stranded on the same screen after success.
/// - After account creation, the app attempts to sign in immediately and enters authenticated shell.
/// - If auto-login fails after successful registration, the user gets a clear non-technical message
///   and can still login manually without losing control of the flow.
/// </summary>
public sealed class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;

    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    public RegisterViewModel(IAuthService authService, IAppRootNavigator appRootNavigator)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));

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

    private async Task RegisterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        // Keep normalized copies to avoid accidental drift between registration and auto-login inputs.
        var normalizedEmail = Email.Trim();
        var rawPassword = Password;

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
                Email = normalizedEmail,
                Password = rawPassword
            };

            var response = await _authService.RegisterAsync(request, CancellationToken.None);
            if (response is null)
            {
                ErrorMessage = AppResources.RegisterFailed;
                return;
            }

            // Registration succeeded. Next step is immediate sign-in to satisfy expected UX.
            // We intentionally do not use ConfigureAwait(false) here because this view model is UI-bound.
            try
            {
                _ = await _authService.LoginAsync(
                    normalizedEmail,
                    rawPassword,
                    deviceId: null,
                    CancellationToken.None);

                // Enter authenticated mode immediately after successful auto-login.
                await _appRootNavigator.NavigateToAuthenticatedShellAsync();
            }
            catch
            {
                // Account was created but auto-login failed.
                // We avoid technical exception details and provide a safe user-facing fallback.
                ErrorMessage = AppResources.RegisterAutoLoginFailed;

                // Clear sensitive password fields for security hygiene.
                Password = string.Empty;
                ConfirmPassword = string.Empty;
            }
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
    /// Performs fast local validation before calling backend.
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
    /// Maps low-level exceptions to user-friendly messages.
    /// </summary>
    private static string ResolveFriendlyError(Exception ex, string fallback)
    {
        var raw = ex.Message ?? string.Empty;

        if (raw.Contains("409", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.RegisterEmailAlreadyUsed;
        }

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

        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }
}
