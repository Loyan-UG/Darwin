using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles the consumer account activation lifecycle: request a new email or confirm an account with email + token.
/// </summary>
public sealed class ActivationViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private CancellationTokenSource? _operationCancellation;
    private string _email = string.Empty;
    private string _token = string.Empty;
    private string? _successMessage;

    public ActivationViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        RequestActivationEmailCommand = new AsyncCommand(RequestActivationEmailAsync, () => !IsBusy);
        ConfirmEmailCommand = new AsyncCommand(ConfirmEmailAsync, () => !IsBusy);
    }

    public AsyncCommand RequestActivationEmailCommand { get; }

    public AsyncCommand ConfirmEmailCommand { get; }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value);
    }

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

    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    /// <summary>
    /// Cancels any in-flight activation operation when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        return Task.CompletedTask;
    }

    public void ApplyPrefill(string? email, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(email))
        {
            Email = email.Trim();
        }

        if (string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(token))
        {
            Token = token.Trim();
        }
    }

    private async Task RequestActivationEmailAsync()
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
            RaiseCommandStates();
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                RunOnMain(() => ErrorMessage = AppResources.EmailRequired);
                return;
            }

            var requested = await _authService.RequestEmailConfirmationAsync(Email.Trim(), operationCancellation.Token);
            if (!requested)
            {
                RunOnMain(() => ErrorMessage = AppResources.ActivationEmailRequestFailed);
                return;
            }

            RunOnMain(() => SuccessMessage = AppResources.ActivationEmailSent);
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the activation screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ActivationEmailRequestFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    private async Task ConfirmEmailAsync()
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
            RaiseCommandStates();
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Token))
            {
                RunOnMain(() => ErrorMessage = AppResources.ActivationEmailTokenRequired);
                return;
            }

            var confirmed = await _authService.ConfirmEmailAsync(Email.Trim(), Token.Trim(), operationCancellation.Token);
            if (!confirmed)
            {
                RunOnMain(() => ErrorMessage = AppResources.ActivationConfirmFailed);
                return;
            }

            RunOnMain(() =>
            {
                SuccessMessage = AppResources.ActivationConfirmSuccess;
                Token = string.Empty;
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the activation screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ActivationConfirmFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCommandStates();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Starts a cancellable activation operation and cancels any stale operation still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active activation operation without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var current = Interlocked.Exchange(ref _operationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed activation operation when it still owns the active operation slot.
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

    /// <summary>
    /// Updates both activation commands whenever the busy state changes so repeated taps cannot submit duplicate requests.
    /// </summary>
    private void RaiseCommandStates()
    {
        RequestActivationEmailCommand.RaiseCanExecuteChanged();
        ConfirmEmailCommand.RaiseCanExecuteChanged();
    }
}
