using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Services.Permissions;
using Darwin.Shared.Results;
using Microsoft.Maui.ApplicationModel;

namespace Darwin.Mobile.Consumer.Services.Notifications;

/// <summary>
/// Consumer-app specific notification permission coordinator.
/// </summary>
public sealed class ConsumerNotificationPermissionService : IConsumerNotificationPermissionService
{
    private readonly IPermissionDisclosureService _permissionDisclosureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsumerNotificationPermissionService"/> class.
    /// </summary>
    /// <param name="permissionDisclosureService">Reusable disclosure service shown before the operating-system prompt.</param>
    public ConsumerNotificationPermissionService(IPermissionDisclosureService permissionDisclosureService)
    {
        _permissionDisclosureService = permissionDisclosureService ?? throw new ArgumentNullException(nameof(permissionDisclosureService));
    }

    /// <inheritdoc />
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
                LegalReferenceKind = Darwin.Mobile.Shared.Services.Legal.LegalLinkKind.PrivacyPolicy
            }, cancellationToken).ConfigureAwait(false);

            if (!shouldProceed)
            {
                return Result<bool>.Ok(false);
            }

#if ANDROID
            var status = await Permissions.RequestAsync<Permissions.PostNotifications>().ConfigureAwait(false);
            return Result<bool>.Ok(status == PermissionStatus.Granted);
#elif IOS || MACCATALYST
            await ApplePushRuntimeBridge.RequestAuthorizationAndRegisterAsync(cancellationToken).ConfigureAwait(false);
            var granted = await ApplePushRuntimeBridge.AreNotificationsEnabledAsync(cancellationToken).ConfigureAwait(false);
            return Result<bool>.Ok(granted);
#else
            await Task.CompletedTask.ConfigureAwait(false);
            return Result<bool>.Ok(true);
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
#elif IOS || MACCATALYST
        var granted = await ApplePushRuntimeBridge.AreNotificationsEnabledAsync(cancellationToken).ConfigureAwait(false);
        return granted ? PermissionStatus.Granted : PermissionStatus.Denied;
#else
        await Task.CompletedTask.ConfigureAwait(false);
        return PermissionStatus.Granted;
#endif
    }
}
