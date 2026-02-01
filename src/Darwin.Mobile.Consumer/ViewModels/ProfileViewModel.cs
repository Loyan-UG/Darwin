using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// View model for the consumer profile page. Currently holds placeholder data.
/// Future phases should add properties for user name, email, points history, etc.
/// </summary>
public sealed class ProfileViewModel : BaseViewModel
{
    private string? _displayName;

    public ProfileViewModel()
    {
        // Placeholder initialization
        DisplayName = "User";
    }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string? DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }
}
