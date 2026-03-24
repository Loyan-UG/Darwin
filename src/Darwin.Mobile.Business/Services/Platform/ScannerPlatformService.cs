using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Views;
using Darwin.Mobile.Shared.Integration;
using Darwin.Mobile.Shared.Services.Permissions;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business.Services.Platform;

/// <summary>
/// Platform-specific QR scanner for the Business app.
/// </summary>
/// <remarks>
/// Responsibilities:
/// - Shows a just-in-time privacy disclosure before the operating-system camera permission prompt.
/// - Requests and validates camera permission in a professional way.
/// - Launches the existing modal QrScanPage and awaits its Completed event.
/// - Provides a manual fallback for emulators or when camera access is not possible.
/// </remarks>
public sealed class ScannerPlatformService : IScanner
{
    private readonly IPermissionDisclosureService _permissionDisclosureService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScannerPlatformService"/> class.
    /// </summary>
    /// <param name="permissionDisclosureService">Service used to show a privacy disclosure before requesting camera access.</param>
    public ScannerPlatformService(IPermissionDisclosureService permissionDisclosureService)
    {
        _permissionDisclosureService = permissionDisclosureService ?? throw new ArgumentNullException(nameof(permissionDisclosureService));
    }

    /// <summary>
    /// Initiates a QR scan operation and returns the decoded token string, or null if cancelled/failed.
    /// </summary>
    /// <param name="ct">Cancellation token from caller.</param>
    public async Task<string?> ScanAsync(CancellationToken ct)
    {
        try
        {
            var current = await Permissions.CheckStatusAsync<Permissions.Camera>().ConfigureAwait(false);

            if (current == PermissionStatus.Granted)
            {
                return await LaunchScanPageWithFallbackAsync(ct).ConfigureAwait(false);
            }

            var shouldProceed = await _permissionDisclosureService.ShowAsync(new PermissionDisclosureRequest
            {
                Title = AppResources.CameraDisclosureTitle,
                PermissionName = AppResources.CameraDisclosurePermissionName,
                WhyThisIsNeeded = AppResources.CameraDisclosurePurpose,
                FeatureRequirementText = AppResources.CameraDisclosureRequirement,
                ContinueButtonText = AppResources.PermissionDisclosureContinueButton,
                CancelButtonText = AppResources.PermissionDisclosureCancelButton,
                LegalReferenceButtonText = AppResources.PermissionDisclosurePrivacyButton,
                LegalReferenceOpenFailedMessage = AppResources.LegalOpenFailed,
                LegalReferenceKind = Darwin.Mobile.Shared.Services.Legal.LegalLinkKind.PrivacyPolicy
            }, ct).ConfigureAwait(false);

            if (!shouldProceed)
            {
                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }

            var status = await Permissions.RequestAsync<Permissions.Camera>().ConfigureAwait(false);

            if (status == PermissionStatus.Granted)
            {
                return await LaunchScanPageWithFallbackAsync(ct).ConfigureAwait(false);
            }

            if (status == PermissionStatus.Denied)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var open = await Shell.Current!.DisplayAlertAsync(
                        AppResources.CameraDisclosureDeniedTitle,
                        AppResources.CameraDisclosureDeniedBody,
                        AppResources.CameraDisclosureOpenSettingsButton,
                        AppResources.PermissionDisclosureCancelButton).ConfigureAwait(false);

                    if (open)
                    {
                        AppInfo.ShowSettingsUI();
                    }
                }).ConfigureAwait(false);

                return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
            }

            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return await PromptForManualTokenAsync(ct).ConfigureAwait(false);
        }
    }

    private static async Task<string?> LaunchScanPageWithFallbackAsync(CancellationToken ct)
    {
        var scanPage = new QrScanPage();
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void CompletedHandler(object? sender, string? token) => tcs.TrySetResult(token);

        scanPage.Completed += CompletedHandler;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current!.Navigation.PushModalAsync(scanPage).ConfigureAwait(false);
            }).ConfigureAwait(false);

            string? result;
            using (ct.Register(() => tcs.TrySetCanceled(ct)))
            {
                try
                {
                    result = await tcs.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    result = null;
                }
            }

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        finally
        {
            scanPage.Completed -= CompletedHandler;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Shell.Current?.Navigation.ModalStack?.Count > 0)
                    {
                        await Shell.Current.Navigation.PopModalAsync().ConfigureAwait(false);
                    }
                }
                catch
                {
                }
            }).ConfigureAwait(false);
        }
    }

    private static Task<string?> PromptForManualTokenAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var token = await Shell.Current!.DisplayPromptAsync(
                    title: AppResources.ScannerManualTokenTitle,
                    message: AppResources.ScannerManualTokenMessage,
                    accept: AppResources.ScannerManualTokenAccept,
                    cancel: AppResources.ScannerManualTokenCancel,
                    placeholder: AppResources.ScannerManualTokenPlaceholder,
                    keyboard: Keyboard.Text).ConfigureAwait(false);

                tcs.TrySetResult(token);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        ct.Register(() => tcs.TrySetCanceled(ct));
        return tcs.Task;
    }
}
