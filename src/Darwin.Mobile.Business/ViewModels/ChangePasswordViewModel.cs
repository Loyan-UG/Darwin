using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// Handles password update flow for Business users.
/// 
/// This ViewModel intentionally provides detailed validation and clear feedback messages
/// so support teams can diagnose user reports faster.
/// </summary>
public sealed class ChangePasswordViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private CancellationTokenSource? _operationCancellation;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;
    private string? _successMessage;

    public ChangePasswordViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        UpdatePasswordCommand = new AsyncCommand(UpdatePasswordAsync, () => !IsBusy && CanSubmit());
    }

    public AsyncCommand UpdatePasswordCommand { get; }

    public string CurrentPassword
    {
        get => _currentPassword;
        set
        {
            if (SetProperty(ref _currentPassword, value))
            {
                UpdatePasswordCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                UpdatePasswordCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set
        {
            if (SetProperty(ref _confirmNewPassword, value))
            {
                UpdatePasswordCommand.RaiseCanExecuteChanged();
            }
        }
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
    /// Cancels any in-flight password change when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        return Task.CompletedTask;
    }

    private bool CanSubmit()
    {
        return !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmNewPassword);
    }

    private async Task UpdatePasswordAsync()
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
            UpdatePasswordCommand.RaiseCanExecuteChanged();
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            if (!CanSubmit())
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordRequired);
                return;
            }

            if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordMismatch);
                return;
            }

            if (NewPassword.Length < 8)
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordMinLength);
                return;
            }

            var changed = await _authService.ChangePasswordAsync(CurrentPassword, NewPassword, operationCancellation.Token);
            if (!changed)
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordChangeFailed);
                return;
            }

            RunOnMain(() =>
            {
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
                SuccessMessage = AppResources.PasswordChangeSuccess;
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the password screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.PasswordChangeFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                UpdatePasswordCommand.RaiseCanExecuteChanged();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Starts a cancellable password change operation and cancels any stale operation still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active password change without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var current = Interlocked.Exchange(ref _operationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed password change when it still owns the active operation slot.
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
