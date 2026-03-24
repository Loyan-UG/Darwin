using Darwin.Mobile.Shared.Services.Legal;

namespace Darwin.Mobile.Shared.Services.Permissions;

/// <summary>
/// Describes the content shown before an operating-system permission dialog is requested.
/// </summary>
public sealed class PermissionDisclosureRequest
{
    /// <summary>
    /// Gets or sets the dialog title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission name shown to the user.
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a concise explanation of why the feature requests the permission.
    /// </summary>
    public string WhyThisIsNeeded { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a concise explanation of whether the feature is optional or functionally required.
    /// </summary>
    public string FeatureRequirementText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary action text that continues to the operating-system dialog.
    /// </summary>
    public string ContinueButtonText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cancel action text.
    /// </summary>
    public string CancelButtonText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional button text used to open a referenced legal page.
    /// </summary>
    public string LegalReferenceButtonText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the legal destination that should open when the user asks for more privacy detail.
    /// </summary>
    public LegalLinkKind LegalReferenceKind { get; set; } = LegalLinkKind.PrivacyPolicy;
}
