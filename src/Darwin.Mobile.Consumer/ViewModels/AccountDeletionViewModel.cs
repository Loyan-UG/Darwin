using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.Services.Navigation;
using Darwin.Mobile.Consumer.Services.Notifications;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Coordinates the in-app consumer account deletion request flow.
/// </summary>
/// <remarks>
/// The underlying server workflow performs deactivation and anonymization, not a physical delete.
/// After a successful request the user is signed out locally and the app is returned to the login root.
/// </remarks>
public sealed class AccountDeletionViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;
    private readonly IAppRootNavigator _appRootNavigator;
    private readonly IConsumerPushRegistrationCoordinator _pushRegistrationCoordinator;
    private bool _confirmIrreversibleDeletion;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountDeletionViewModel"/> class.
    /// </summary>
    /// <param name="profileService">Profile service used to submit the deletion request.</param>
    /// <param name="authService">Authentication service used to clear local session state.</param>
    /// <param name="appRootNavigator">Root navigator used to return to the login flow.</param>
    /// <param name="pushRegistrationCoordinator">Push coordinator whose cached state must be cleared after sign-out.</param>
    public AccountDeletionViewModel(
        IProfileService profileService,
        IAuthService authService,
        IAppRootNavigator appRootNavigator,
        IConsumerPushRegistrationCoordinator pushRegistrationCoordinator)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appRootNavigator = appRootNavigator ?? throw new ArgumentNullException(nameof(appRootNavigator));
        _pushRegistrationCoordinator = pushRegistrationCoordinator ?? throw new ArgumentNullException(nameof(pushRegistrationCoordinator));

        SubmitDeletionRequestCommand = new AsyncCommand(SubmitDeletionRequestAsync, () => !IsBusy);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the user explicitly confirmed the irreversible workflow.
    /// </summary>
    public bool ConfirmIrreversibleDeletion
    {
        get => _confirmIrreversibleDeletion;
        set
        {
            if (SetProperty(ref _confirmIrreversibleDeletion, value))
            {
                SubmitDeletionRequestCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the command that submits the authenticated account deletion request.
    /// </summary>
    public AsyncCommand SubmitDeletionRequestCommand { get; }

    private async Task SubmitDeletionRequestAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!ConfirmIrreversibleDeletion)
        {
            RunOnMain(() => ErrorMessage = AppResources.AccountDeletionConfirmationRequired);
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
        });

        try
        {
            var result = await _profileService
                .RequestAccountDeletionAsync(new RequestAccountDeletionRequest(true), CancellationToken.None)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = AppResources.AccountDeletionRequestFailed);
                return;
            }

            await _authService.LogoutAsync(CancellationToken.None).ConfigureAwait(false);
            _pushRegistrationCoordinator.ResetCachedRegistrationState();
            await _appRootNavigator.NavigateToLoginAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.AccountDeletionRequestFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                SubmitDeletionRequestCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
