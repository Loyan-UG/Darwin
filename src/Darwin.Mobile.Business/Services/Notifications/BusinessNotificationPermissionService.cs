using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Services.Legal;
using Darwin.Mobile.Shared.Services.Permissions;
using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Business.Services.Notifications;

/// <summary>
/// Business-app specific notification permission coordinator.
/// </summary>
public sealed class BusinessNotificationPermissionService : IBusinessNotificationPermissionService
{
    private readonly IPermissionDisclosureService _permissionDisclosureService;

    public BusinessNotificationPermissionService(IPermissionDisclosureService permissionDisclosureService)
    {
        _permissionDisclosureService = permissionDisclosureService ?? throw new ArgumentNullException(nameof(permissionDisclosureService));
    }

    public async Task<Result<bool>> EnsurePermissionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var currentStatus = await GetCurrentStatusAsync(cancellationToken).ConfigureAwait(false);
            if (currentStatus == PermissionStatus.Granted)
            {
                return Result<bool>.Ok(true);
            }

            var shouldProceed = await _permissionDisclosureService.ShowAsync(new PermissionDisclosureRequest
            {
                Title = AppResources.NotificationDisclosureTitle,
                PermissionName = AppResources.NotificationDisclosurePermissionName,
                WhyThisIsNeeded = AppResources.NotificationDisclosurePurpose,
                FeatureRequirementText = AppResources.NotificationDisclosureRequirement,
                ContinueButtonText = AppResources.PermissionDisclosureContinueButton,
                CancelButtonText = AppResources.PermissionDisclosureCancelButton,
                LegalReferenceButtonText = AppResources.PermissionDisclosurePrivacyButton,
                LegalReferenceOpenFailedMessage = AppResources.LegalOpenFailed,
                LegalReferenceKind = LegalLinkKind.PrivacyPolicy
            }, cancellationToken).ConfigureAwait(false);

            if (!shouldProceed)
            {
                return Result<bool>.Ok(false);
            }

#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>().ConfigureAwait(false);
            return Result<bool>.Ok(status == PermissionStatus.Granted);
#else
            await Task.CompletedTask.ConfigureAwait(false);
            return Result<bool>.Ok(false);
#endif
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Notification permission could not be requested: {ex.Message}");
        }
    }

    private static async Task<PermissionStatus> GetCurrentStatusAsync(CancellationToken cancellationToken)
    {
#if ANDROID
        return await Permissions.CheckStatusAsync<Permissions.PostNotifications>().ConfigureAwait(false);
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return PermissionStatus.Denied;
#endif
    }
}
