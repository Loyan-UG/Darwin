using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Services.Legal;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Shared.Services.Permissions;

/// <summary>
/// Shared disclosure service that presents a modal in-app sheet before sensitive operating-system permission requests.
/// </summary>
public sealed class PermissionDisclosureService : IPermissionDisclosureService
{
    private readonly ILegalLinkService _legalLinkService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionDisclosureService"/> class.
    /// </summary>
    /// <param name="legalLinkService">Service used to open the referenced legal page from the disclosure sheet.</param>
    public PermissionDisclosureService(ILegalLinkService legalLinkService)
    {
        _legalLinkService = legalLinkService ?? throw new ArgumentNullException(nameof(legalLinkService));
    }

    /// <inheritdoc />
    public async Task<bool> ShowAsync(PermissionDisclosureRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();

        while (true)
        {
            var page = ResolveCurrentPage();
            if (page is null)
            {
                return false;
            }

            var dialogPage = new PermissionDisclosureDialogPage(request);
            PermissionDisclosureChoice choice;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await page.Navigation.PushModalAsync(dialogPage).ConfigureAwait(false);
            }).ConfigureAwait(false);

            try
            {
                choice = await dialogPage.WaitForChoiceAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (page.Navigation.ModalStack.Contains(dialogPage))
                    {
                        await page.Navigation.PopModalAsync().ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }

            if (choice == PermissionDisclosureChoice.Continue)
            {
                return true;
            }

            if (choice == PermissionDisclosureChoice.LegalReference)
            {
                var openResult = await _legalLinkService.OpenAsync(request.LegalReferenceKind, cancellationToken).ConfigureAwait(false);
                if (!openResult.Succeeded)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                        await page.DisplayAlertAsync(
                            request.Title,
                            openResult.Error ?? request.LegalReferenceOpenFailedMessage,
                            request.CancelButtonText).ConfigureAwait(false));
                }

                continue;
            }

            return false;
        }
    }

    private static Page? ResolveCurrentPage()
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window?.Page is null)
        {
            return null;
        }

        return ResolveVisiblePage(window.Page);
    }

    private static Page ResolveVisiblePage(Page page)
    {
        return page switch
        {
            NavigationPage navigationPage when navigationPage.CurrentPage is not null => ResolveVisiblePage(navigationPage.CurrentPage),
            TabbedPage tabbedPage when tabbedPage.CurrentPage is not null => ResolveVisiblePage(tabbedPage.CurrentPage),
            FlyoutPage flyoutPage when flyoutPage.Detail is not null => ResolveVisiblePage(flyoutPage.Detail),
            Shell shell when shell.CurrentPage is not null => ResolveVisiblePage(shell.CurrentPage),
            _ => page
        };
    }

    private enum PermissionDisclosureChoice
    {
        Cancel = 0,
        Continue = 1,
        LegalReference = 2
    }

    private sealed class PermissionDisclosureDialogPage : ContentPage
    {
        private readonly TaskCompletionSource<PermissionDisclosureChoice> _choiceSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PermissionDisclosureDialogPage(PermissionDisclosureRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            Title = request.Title;
            Padding = new Thickness(24);
            BackgroundColor = Colors.White;

            var permissionLabel = new Label
            {
                Text = request.PermissionName,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var whyLabel = new Label
            {
                Text = request.WhyThisIsNeeded,
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var requirementLabel = new Label
            {
                Text = request.FeatureRequirementText,
                FontSize = 14,
                FontAttributes = FontAttributes.Italic,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var privacyButton = new Button
            {
                Text = request.LegalReferenceButtonText,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Blue
            };
            privacyButton.Clicked += (_, _) => _choiceSource.TrySetResult(PermissionDisclosureChoice.LegalReference);

            var continueButton = new Button
            {
                Text = request.ContinueButtonText
            };
            continueButton.Clicked += (_, _) => _choiceSource.TrySetResult(PermissionDisclosureChoice.Continue);

            var cancelButton = new Button
            {
                Text = request.CancelButtonText,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.Black
            };
            cancelButton.Clicked += (_, _) => _choiceSource.TrySetResult(PermissionDisclosureChoice.Cancel);

            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Spacing = 16,
                    Children =
                    {
                        permissionLabel,
                        whyLabel,
                        requirementLabel,
                        privacyButton,
                        continueButton,
                        cancelButton
                    }
                }
            };
        }

        protected override bool OnBackButtonPressed()
        {
            _choiceSource.TrySetResult(PermissionDisclosureChoice.Cancel);
            return true;
        }

        public Task<PermissionDisclosureChoice> WaitForChoiceAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _choiceSource.TrySetCanceled(cancellationToken));
            return _choiceSource.Task;
        }
    }
}
