using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Settings hub view model.
/// This page intentionally acts as a stable container for account settings and safe navigation actions.
/// </summary>
public sealed class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly IProfileService _profileService;
    private string _profileDisplayName = string.Empty;
    private string _profileEmail = string.Empty;
    private string? _profileImageUrl;

    public SettingsViewModel(INavigationService navigationService, IProfileService profileService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        OpenProfileCommand = new AsyncCommand(OpenProfileAsync, () => !IsBusy);
        OpenChangePasswordCommand = new AsyncCommand(OpenChangePasswordAsync, () => !IsBusy);
        OpenMemberCommerceCommand = new AsyncCommand(OpenMemberCommerceAsync, () => !IsBusy);
        OpenMemberPreferencesCommand = new AsyncCommand(OpenMemberPreferencesAsync, () => !IsBusy);
        OpenLegalHubCommand = new AsyncCommand(OpenLegalHubAsync, () => !IsBusy);
        OpenAccountDeletionCommand = new AsyncCommand(OpenAccountDeletionAsync, () => !IsBusy);
    }

    public AsyncCommand OpenProfileCommand { get; }

    public AsyncCommand OpenChangePasswordCommand { get; }

    public AsyncCommand OpenMemberCommerceCommand { get; }

    public AsyncCommand OpenMemberPreferencesCommand { get; }

    public AsyncCommand OpenLegalHubCommand { get; }

    public AsyncCommand OpenAccountDeletionCommand { get; }

    public string ProfileDisplayName
    {
        get => _profileDisplayName;
        private set
        {
            if (SetProperty(ref _profileDisplayName, value))
            {
                OnPropertyChanged(nameof(ProfileInitials));
            }
        }
    }

    public string ProfileEmail
    {
        get => _profileEmail;
        private set
        {
            if (SetProperty(ref _profileEmail, value))
            {
                OnPropertyChanged(nameof(ProfileInitials));
            }
        }
    }

    public string? ProfileImageUrl
    {
        get => _profileImageUrl;
        private set
        {
            if (SetProperty(ref _profileImageUrl, string.IsNullOrWhiteSpace(value) ? null : value))
            {
                OnPropertyChanged(nameof(HasProfileImage));
            }
        }
    }

    public bool HasProfileImage => !string.IsNullOrWhiteSpace(ProfileImageUrl);

    public string ProfileInitials => BuildInitials(ProfileDisplayName, ProfileEmail);

    public override async Task OnAppearingAsync()
    {
        await RefreshProfileSummaryAsync();
    }

    private async Task OpenProfileAsync()
        => await NavigateAsync(Routes.ProfileEdit);

    private async Task OpenChangePasswordAsync()
        => await NavigateAsync(Routes.ChangePassword);

    private async Task OpenMemberCommerceAsync()
        => await NavigateAsync(Routes.MemberCommerce);

    private async Task OpenMemberPreferencesAsync()
        => await NavigateAsync(Routes.MemberPreferences);

    private async Task OpenLegalHubAsync()
        => await NavigateAsync(Routes.LegalHub);

    private async Task OpenAccountDeletionAsync()
        => await NavigateAsync(Routes.AccountDeletion);

    private async Task NavigateAsync(string route)
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            RaiseCommandStates();
        });

        try
        {
            await _navigationService.GoToAsync(route);
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

    /// <summary>
    /// Updates settings navigation commands after busy-state transitions.
    /// </summary>
    private void RaiseCommandStates()
    {
        OpenProfileCommand.RaiseCanExecuteChanged();
        OpenChangePasswordCommand.RaiseCanExecuteChanged();
        OpenMemberCommerceCommand.RaiseCanExecuteChanged();
        OpenMemberPreferencesCommand.RaiseCanExecuteChanged();
        OpenLegalHubCommand.RaiseCanExecuteChanged();
        OpenAccountDeletionCommand.RaiseCanExecuteChanged();
    }

    private async Task RefreshProfileSummaryAsync()
    {
        try
        {
            var profile = await _profileService.GetMeAsync(CancellationToken.None);
            if (profile is null)
            {
                return;
            }

            RunOnMain(() => ApplyProfileSummary(profile));
        }
        catch
        {
            // Settings navigation must remain usable even when the optional profile summary cannot refresh.
        }
    }

    private void ApplyProfileSummary(CustomerProfile profile)
    {
        ProfileEmail = profile.Email ?? string.Empty;
        ProfileDisplayName = BuildDisplayName(profile.FirstName, profile.LastName, ProfileEmail);
        ProfileImageUrl = profile.ProfileImageUrl;
    }

    private static string BuildDisplayName(string? firstName, string? lastName, string fallbackEmail)
    {
        var fullName = string.Join(
            " ",
            new[] { firstName, lastName }
                .Where(static part => !string.IsNullOrWhiteSpace(part))
                .Select(static part => part!.Trim()));

        return string.IsNullOrWhiteSpace(fullName) ? fallbackEmail : fullName;
    }

    private static string BuildInitials(string displayName, string email)
    {
        var source = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
        var parts = source
            .Split([' ', '.', '@', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static part => part.Length > 0)
            .Take(2)
            .Select(static part => char.ToUpperInvariant(part[0]))
            .ToArray();

        return parts.Length == 0 ? "?" : new string(parts);
    }
}
