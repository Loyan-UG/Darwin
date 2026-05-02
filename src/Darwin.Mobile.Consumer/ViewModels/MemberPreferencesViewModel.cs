using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Provides canonical member preference management backed by the WebApi profile endpoints.
/// </summary>
public sealed class MemberPreferencesViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private CancellationTokenSource? _operationCancellation;
    private bool _isLoaded;
    private byte[] _rowVersion = Array.Empty<byte>();
    private string? _successMessage;
    private bool _marketingConsent;
    private bool _allowEmailMarketing;
    private bool _allowSmsMarketing;
    private bool _allowWhatsAppMarketing;
    private bool _allowPromotionalPushNotifications;
    private bool _allowOptionalAnalyticsTracking;
    private string? _acceptsTermsAtText;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberPreferencesViewModel"/> class.
    /// </summary>
    public MemberPreferencesViewModel(IProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy);
    }

    /// <summary>Gets the refresh command.</summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>Gets the save command.</summary>
    public AsyncCommand SaveCommand { get; }

    /// <summary>Gets or sets the success message.</summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    /// <summary>Gets a value indicating whether a success message is present.</summary>
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    /// <summary>Gets or sets aggregate marketing consent.</summary>
    public bool MarketingConsent
    {
        get => _marketingConsent;
        set
        {
            if (SetProperty(ref _marketingConsent, value) && !value)
            {
                AllowEmailMarketing = false;
                AllowSmsMarketing = false;
                AllowWhatsAppMarketing = false;
                AllowPromotionalPushNotifications = false;
            }
        }
    }

    /// <summary>Gets or sets the email-marketing flag.</summary>
    public bool AllowEmailMarketing { get => _allowEmailMarketing; set => SetProperty(ref _allowEmailMarketing, value); }

    /// <summary>Gets or sets the SMS-marketing flag.</summary>
    public bool AllowSmsMarketing { get => _allowSmsMarketing; set => SetProperty(ref _allowSmsMarketing, value); }

    /// <summary>Gets or sets the WhatsApp-marketing flag.</summary>
    public bool AllowWhatsAppMarketing { get => _allowWhatsAppMarketing; set => SetProperty(ref _allowWhatsAppMarketing, value); }

    /// <summary>Gets or sets the promotional-push flag.</summary>
    public bool AllowPromotionalPushNotifications { get => _allowPromotionalPushNotifications; set => SetProperty(ref _allowPromotionalPushNotifications, value); }

    /// <summary>Gets or sets the optional analytics flag.</summary>
    public bool AllowOptionalAnalyticsTracking { get => _allowOptionalAnalyticsTracking; set => SetProperty(ref _allowOptionalAnalyticsTracking, value); }

    /// <summary>Gets or sets the terms-accepted timestamp display text.</summary>
    public string? AcceptsTermsAtText
    {
        get => _acceptsTermsAtText;
        private set
        {
            if (SetProperty(ref _acceptsTermsAtText, value))
            {
                OnPropertyChanged(nameof(HasAcceptsTermsAt));
            }
        }
    }

    /// <summary>Gets a value indicating whether a terms-accepted timestamp is available.</summary>
    public bool HasAcceptsTermsAt => !string.IsNullOrWhiteSpace(AcceptsTermsAtText);

    /// <inheritdoc />
    public override async Task OnAppearingAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        await RefreshAsync().ConfigureAwait(false);
        _isLoaded = true;
    }

    /// <summary>
    /// Cancels the current preference load/save operation when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        RunOnMain(() =>
        {
            IsBusy = false;
            RefreshCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
        });

        return Task.CompletedTask;
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            await LoadPreferencesSnapshotAsync(operationCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the preferences screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberPreferencesLoadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RefreshCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            });

            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            var cancellationToken = operationCancellation.Token;

            var request = new UpdateMemberPreferencesRequest
            {
                RowVersion = _rowVersion,
                MarketingConsent = MarketingConsent,
                AllowEmailMarketing = MarketingConsent && AllowEmailMarketing,
                AllowSmsMarketing = MarketingConsent && AllowSmsMarketing,
                AllowWhatsAppMarketing = MarketingConsent && AllowWhatsAppMarketing,
                AllowPromotionalPushNotifications = MarketingConsent && AllowPromotionalPushNotifications,
                AllowOptionalAnalyticsTracking = AllowOptionalAnalyticsTracking
            };

            var result = await _profileService.UpdatePreferencesAsync(request, cancellationToken).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.MemberPreferencesSaveFailed);
                return;
            }

            RunOnMain(() => SuccessMessage = AppResources.MemberPreferencesSaved);
            await LoadPreferencesSnapshotAsync(cancellationToken).ConfigureAwait(false);
            _isLoaded = true;
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the preferences screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberPreferencesSaveFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RefreshCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            });

            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Loads preference values and the latest RowVersion without checking the outer busy state.
    /// Save uses this helper after a successful update so the next save sends current optimistic-concurrency metadata.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used for the preferences service call.</param>
    private async Task LoadPreferencesSnapshotAsync(CancellationToken cancellationToken)
    {
        var preferences = await _profileService.GetPreferencesAsync(cancellationToken).ConfigureAwait(false);
        if (preferences is null)
        {
            RunOnMain(() => ErrorMessage = AppResources.MemberPreferencesLoadFailed);
            return;
        }

        RunOnMain(() =>
        {
            _rowVersion = preferences.RowVersion;
            MarketingConsent = preferences.MarketingConsent;
            AllowEmailMarketing = preferences.AllowEmailMarketing;
            AllowSmsMarketing = preferences.AllowSmsMarketing;
            AllowWhatsAppMarketing = preferences.AllowWhatsAppMarketing;
            AllowPromotionalPushNotifications = preferences.AllowPromotionalPushNotifications;
            AllowOptionalAnalyticsTracking = preferences.AllowOptionalAnalyticsTracking;
            AcceptsTermsAtText = preferences.AcceptsTermsAtUtc.HasValue
                ? string.Format(AppResources.MemberPreferencesTermsAcceptedFormat, preferences.AcceptsTermsAtUtc.Value.ToLocalTime())
                : null;
        });
    }

    /// <summary>
    /// Starts a cancellable preference operation and cancels any stale operation that was still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the current preference operation without disposing the token source while service code may still observe it.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var current = Interlocked.Exchange(ref _operationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed preference operation when it still owns the active operation slot.
    /// </summary>
    /// <param name="operationCancellation">Completed operation token source.</param>
    private void EndCurrentOperation(CancellationTokenSource operationCancellation)
    {
        if (ReferenceEquals(_operationCancellation, operationCancellation))
        {
            _operationCancellation = null;
        }

        operationCancellation.Dispose();
    }
}
