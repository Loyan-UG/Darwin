using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Identity;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Legal;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles customer self-service registration with legal acknowledgements and a confirmation-aware continuation.
/// </summary>
/// <remarks>
/// Legal/compliance rules implemented here:
/// - Account creation is blocked until the user accepts the consumer terms.
/// - Account creation is blocked until the user acknowledges the privacy notice.
/// - Backend-backed privacy and communication preferences are managed after account creation in the profile area.
/// </remarks>
public sealed class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;
    private readonly ILegalLinkService _legalLinkService;

    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _acceptConsumerTerms;
    private bool _acknowledgePrivacyNotice;
    private string? _infoMessage;

    public RegisterViewModel(
        IAuthService authService,
        IAppRootNavigator appRootNavigator,
        ILegalLinkService legalLinkService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _legalLinkService = legalLinkService ?? throw new ArgumentNullException(nameof(legalLinkService));

        RegisterCommand = new AsyncCommand(RegisterAsync, CanRegister);
        OpenTermsCommand = new AsyncCommand(() => OpenLegalLinkAsync(LegalLinkKind.ConsumerTerms), () => !IsBusy);
        OpenPrivacyPolicyCommand = new AsyncCommand(() => OpenLegalLinkAsync(LegalLinkKind.PrivacyPolicy), () => !IsBusy);
    }

    /// <summary>
    /// Gets the registration action command.
    /// </summary>
    public AsyncCommand RegisterCommand { get; }

    /// <summary>
    /// Gets the command that opens the consumer terms page.
    /// </summary>
    public AsyncCommand OpenTermsCommand { get; }

    /// <summary>
    /// Gets the command that opens the privacy notice page.
    /// </summary>
    public AsyncCommand OpenPrivacyPolicyCommand { get; }

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
    /// Gets or sets a non-error informational message shown after successful registration steps.
    /// </summary>
    public string? InfoMessage
    {
        get => _infoMessage;
        private set
        {
            if (SetProperty(ref _infoMessage, value))
            {
                OnPropertyChanged(nameof(HasInfo));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether an informational message is available.
    /// </summary>
    public bool HasInfo => !string.IsNullOrWhiteSpace(_infoMessage);

    /// <summary>
    /// Gets or sets a value indicating whether the consumer terms were explicitly accepted.
    /// </summary>
    public bool AcceptConsumerTerms
    {
        get => _acceptConsumerTerms;
        set
        {
            if (SetProperty(ref _acceptConsumerTerms, value))
            {
                RegisterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the privacy notice was acknowledged.
    /// </summary>
    public bool AcknowledgePrivacyNotice
    {
        get => _acknowledgePrivacyNotice;
        set
        {
            if (SetProperty(ref _acknowledgePrivacyNotice, value))
            {
                RegisterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool CanRegister() => !IsBusy && AcceptConsumerTerms && AcknowledgePrivacyNotice;

    private async Task RegisterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        InfoMessage = null;
        RegisterCommand.RaiseCanExecuteChanged();

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

            if (response.ConfirmationEmailSent)
            {
                InfoMessage = AppResources.RegisterEmailConfirmationSent;
                Password = string.Empty;
                ConfirmPassword = string.Empty;
                return;
            }

            try
            {
                _ = await _authService.LoginAsync(
                    normalizedEmail,
                    rawPassword,
                    deviceId: null,
                    CancellationToken.None);

                await _appRootNavigator.NavigateToAuthenticatedShellAsync();
            }
            catch
            {
                ErrorMessage = AppResources.RegisterAutoLoginFailed;
                Password = string.Empty;
                ConfirmPassword = string.Empty;
            }
        }
        catch (Exception ex)
        {
            InfoMessage = null;
            ErrorMessage = ResolveFriendlyError(ex, AppResources.RegisterFailed);
        }
        finally
        {
            IsBusy = false;
            RegisterCommand.RaiseCanExecuteChanged();
            OpenTermsCommand.RaiseCanExecuteChanged();
            OpenPrivacyPolicyCommand.RaiseCanExecuteChanged();
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

        if (!AcceptConsumerTerms)
        {
            ErrorMessage = AppResources.RegisterTermsAcceptanceRequired;
            return false;
        }

        if (!AcknowledgePrivacyNotice)
        {
            ErrorMessage = AppResources.RegisterPrivacyAcknowledgementRequired;
            return false;
        }

        return true;
    }

    private async Task OpenLegalLinkAsync(LegalLinkKind linkKind)
    {
        var result = await _legalLinkService.OpenAsync(linkKind, CancellationToken.None).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            RunOnMain(() => ErrorMessage = AppResources.LegalOpenFailed);
        }
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

        return ViewModelErrorMapper.ToUserMessage(ex, fallback);
    }
}
