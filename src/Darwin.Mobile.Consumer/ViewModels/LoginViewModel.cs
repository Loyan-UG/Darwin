using CommunityToolkit.Mvvm.Input;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Legal;
using Darwin.Mobile.Shared.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles user login via <see cref="IAuthService"/>.
/// After successful authentication, the app root is switched to the authenticated shell.
/// </summary>
public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;
    private readonly ILegalLinkService _legalLinkService;

    /// <summary>
    /// Raised when the page should reveal the error area (for example: scroll to top).
    /// Keeping this signal in the ViewModel helps us keep the UX consistent across screen sizes
    /// where the software keyboard can hide feedback text after a failed login attempt.
    /// </summary>
    public event Action? ErrorBecameVisibleRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    /// <param name="authService">Service used to authenticate users.</param>
    /// <param name="appRootNavigator">Service that performs window-safe root navigation.</param>
    /// <param name="pushRegistrationCoordinator">Best-effort push registration coordinator.</param>
    /// <param name="legalLinkService">Service used to open configured legal links from the pre-login area.</param>
    public LoginViewModel(
        IAuthService authService,
        IAppRootNavigator appRootNavigator,
        IConsumerPushRegistrationCoordinator pushRegistrationCoordinator,
        ILegalLinkService legalLinkService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));
        _legalLinkService = legalLinkService ?? throw new ArgumentNullException(nameof(legalLinkService));

#if DEBUG
        Email = "cons1@darwin.de";
        Password = "Consumer123!";
#else
        Email = string.Empty;
        Password = string.Empty;
#endif
    }

    /// <summary>
    /// Gets or sets the email entered by the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password entered by the user.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Executes the login process. Uses <see cref="IAuthService.LoginAsync"/> and
    /// swaps the root page to the authenticated shell on success.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = AppResources.EmailRequired;
                ErrorBecameVisibleRequested?.Invoke();
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = AppResources.PasswordRequired;
                ErrorBecameVisibleRequested?.Invoke();
                return;
            }

            _ = await _authService.LoginAsync(
                Email.Trim(),
                Password,
                deviceId: null,
                CancellationToken.None);

            _ = _pushRegistrationCoordinator.TryRegisterCurrentDeviceAsync(CancellationToken.None);

            await _appRootNavigator.NavigateToAuthenticatedShellAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ResolveLoginErrorMessage(ex);
            ErrorBecameVisibleRequested?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenImpressumAsync() => await OpenLegalLinkAsync(LegalLinkKind.Impressum).ConfigureAwait(false);

    [RelayCommand]
    private async Task OpenPrivacyPolicyAsync() => await OpenLegalLinkAsync(LegalLinkKind.PrivacyPolicy).ConfigureAwait(false);

    [RelayCommand]
    private async Task OpenTermsAsync() => await OpenLegalLinkAsync(LegalLinkKind.ConsumerTerms).ConfigureAwait(false);

    private async Task OpenLegalLinkAsync(LegalLinkKind linkKind)
    {
        var result = await _legalLinkService.OpenAsync(linkKind, CancellationToken.None).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            RunOnMain(() => ErrorMessage = AppResources.LegalOpenFailed);
        }
    }

    /// <summary>
    /// Maps technical exceptions to end-user login messages.
    /// </summary>
    private static string ResolveLoginErrorMessage(Exception ex)
    {
        return ViewModelErrorMapper.ToUserMessage(ex, AppResources.InvalidCredentials);
    }
}
