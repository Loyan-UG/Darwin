using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Handles preview and acceptance of business invitations in the Business app.
/// The phase-1 onboarding flow is token-entry based, but the view model also supports
/// a pre-filled token so future deep-link flows can reuse the same page.
/// </summary>
public sealed class AcceptInvitationViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    private string? _invitationToken;
    private string? _firstName;
    private string? _lastName;
    private string? _password;
    private string? _confirmPassword;
    private BusinessInvitationPreviewResponse? _preview;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptInvitationViewModel"/> class.
    /// </summary>
    public AcceptInvitationViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        PreviewInvitationCommand = new AsyncCommand(PreviewInvitationAsync, () => !IsBusy);
        AcceptInvitationCommand = new AsyncCommand(AcceptInvitationAsync, CanAcceptInvitation);
    }

    /// <summary>
    /// Token provided by the operator or future deep-link navigation.
    /// </summary>
    public string? InvitationToken
    {
        get => _invitationToken;
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            if (SetProperty(ref _invitationToken, normalized))
            {
                ResetPreview();
                RaiseCommandStates();
            }
        }
    }

    /// <summary>
    /// First name required only when the invited email does not map to an existing user.
    /// </summary>
    public string? FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    /// <summary>
    /// Last name required only when the invited email does not map to an existing user.
    /// </summary>
    public string? LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    /// <summary>
    /// Password required only when the invited email does not map to an existing user.
    /// </summary>
    public string? Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>
    /// Confirmation for the password used during new-account onboarding.
    /// </summary>
    public string? ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    /// <summary>
    /// Indicates that preview data is currently available.
    /// </summary>
    public bool HasPreview => _preview is not null;

    /// <summary>
    /// Indicates that the operator must create a new platform account as part of acceptance.
    /// </summary>
    public bool RequiresAccountDetails => _preview is not null && !_preview.HasExistingUser;

    /// <summary>
    /// Business display name from invitation preview.
    /// </summary>
    public string BusinessName => _preview?.BusinessName ?? string.Empty;

    /// <summary>
    /// Invited email address from invitation preview.
    /// </summary>
    public string InvitedEmail => _preview?.Email ?? string.Empty;

    /// <summary>
    /// Invited business role from invitation preview.
    /// </summary>
    public string InvitationRole => _preview?.Role ?? string.Empty;

    /// <summary>
    /// Localized display of the invited business role while preserving the canonical role value.
    /// </summary>
    public string InvitationRoleDisplay => LocalizeInvitationRole(_preview?.Role);

    /// <summary>
    /// Effective invitation status from invitation preview.
    /// </summary>
    public string InvitationStatus => _preview?.Status ?? string.Empty;

    /// <summary>
    /// Localized display of the invitation status while preserving the canonical status value.
    /// </summary>
    public string InvitationStatusDisplay => LocalizeInvitationStatus(_preview?.Status);

    /// <summary>
    /// Localized display of the invitation expiration timestamp.
    /// </summary>
    public string InvitationExpiresAtDisplay => _preview is null
        ? string.Empty
        : _preview.ExpiresAtUtc.ToLocalTime().ToString("g");

    /// <summary>
    /// Context hint shown under the preview summary.
    /// </summary>
    public string InvitationHint => RequiresAccountDetails
        ? AppResources.InvitationNewUserHint
        : AppResources.InvitationExistingUserHint;

    /// <summary>
    /// Loads invitation preview data.
    /// </summary>
    public AsyncCommand PreviewInvitationCommand { get; }

    /// <summary>
    /// Accepts the invitation and signs the operator into the app.
    /// </summary>
    public AsyncCommand AcceptInvitationCommand { get; }

    /// <inheritdoc />
    public override async Task OnAppearingAsync()
    {
        await base.OnAppearingAsync().ConfigureAwait(false);

        if (!HasPreview && !string.IsNullOrWhiteSpace(InvitationToken))
        {
            await PreviewInvitationAsync().ConfigureAwait(false);
        }
    }

    private bool CanAcceptInvitation()
        => !IsBusy &&
           HasPreview &&
           string.Equals(InvitationStatus, "Pending", StringComparison.OrdinalIgnoreCase);

    private async Task PreviewInvitationAsync()
    {
        var token = InvitationToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            ErrorMessage = AppResources.InvitationTokenRequired;
            return;
        }

        IsBusy = true;
        RaiseCommandStates();
        RunOnMain(() => ErrorMessage = null);

        try
        {
            var preview = await _authService
                .GetBusinessInvitationPreviewAsync(token, CancellationToken.None)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
                _preview = preview;
                PublishPreviewState();
                ErrorMessage = null;
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() =>
            {
                ResetPreview();
                ErrorMessage = ResolveInvitationErrorMessage(ex, AppResources.InvitationPreviewFailed);
            });
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    private async Task AcceptInvitationAsync()
    {
        if (!HasPreview)
        {
            ErrorMessage = AppResources.InvitationPreviewFailed;
            return;
        }

        if (RequiresAccountDetails)
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                ErrorMessage = AppResources.InvitationFirstNameRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(LastName))
            {
                ErrorMessage = AppResources.InvitationLastNameRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = AppResources.PasswordRequired;
                return;
            }

            if (Password.Length < 8)
            {
                ErrorMessage = AppResources.PasswordMinLength;
                return;
            }

            if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
            {
                ErrorMessage = AppResources.InvitationPasswordMismatch;
                return;
            }
        }

        IsBusy = true;
        RaiseCommandStates();
        RunOnMain(() => ErrorMessage = null);

        try
        {
            await _authService.AcceptBusinessInvitationAsync(
                    new AcceptBusinessInvitationRequest
                    {
                        Token = InvitationToken?.Trim() ?? string.Empty,
                        FirstName = RequiresAccountDetails ? FirstName?.Trim() : null,
                        LastName = RequiresAccountDetails ? LastName?.Trim() : null,
                        Password = RequiresAccountDetails ? Password : null
                    },
                    deviceId: null,
                    CancellationToken.None)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
                ErrorMessage = null;
                FirstName = string.Empty;
                LastName = string.Empty;
                Password = string.Empty;
                ConfirmPassword = string.Empty;
            });

            await _navigationService.GoToAsync($"//{Routes.Home}").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ResolveInvitationErrorMessage(ex, AppResources.InvitationAcceptFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
        }
    }

    private void ResetPreview()
    {
        _preview = null;
        PublishPreviewState();
    }

    private void PublishPreviewState()
    {
        OnPropertyChanged(nameof(HasPreview));
        OnPropertyChanged(nameof(RequiresAccountDetails));
        OnPropertyChanged(nameof(BusinessName));
        OnPropertyChanged(nameof(InvitedEmail));
        OnPropertyChanged(nameof(InvitationRole));
        OnPropertyChanged(nameof(InvitationRoleDisplay));
        OnPropertyChanged(nameof(InvitationStatus));
        OnPropertyChanged(nameof(InvitationStatusDisplay));
        OnPropertyChanged(nameof(InvitationExpiresAtDisplay));
        OnPropertyChanged(nameof(InvitationHint));
    }

    private void RaiseCommandStates()
    {
        PreviewInvitationCommand.RaiseCanExecuteChanged();
        AcceptInvitationCommand.RaiseCanExecuteChanged();
    }

    private static string ResolveInvitationErrorMessage(Exception ex, string fallbackMessage)
    {
        var raw = ex.Message ?? string.Empty;

        if (raw.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.InvitationNotFoundMessage;
        }

        if (raw.Contains("expired", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.InvitationExpiredMessage;
        }

        if (raw.Contains("revoked", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.InvitationRevokedMessage;
        }

        if (raw.Contains("already been accepted", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.InvitationAlreadyAcceptedMessage;
        }

        if (raw.Contains("disabled", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("locked", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("not available", StringComparison.OrdinalIgnoreCase))
        {
            return AppResources.InvitationAccountUnavailableMessage;
        }

        return fallbackMessage;
    }

    private static string LocalizeInvitationRole(string? role)
        => role switch
        {
            "Owner" => AppResources.InvitationRoleOwner,
            "Manager" => AppResources.InvitationRoleManager,
            "Staff" => AppResources.InvitationRoleStaff,
            _ => AppResources.InvitationRoleUnknown
        };

    private static string LocalizeInvitationStatus(string? status)
        => status switch
        {
            "Pending" => AppResources.InvitationStatusPending,
            "Accepted" => AppResources.InvitationStatusAccepted,
            "Expired" => AppResources.InvitationStatusExpired,
            "Revoked" => AppResources.InvitationStatusRevoked,
            _ => AppResources.InvitationStatusUnknown
        };
}
