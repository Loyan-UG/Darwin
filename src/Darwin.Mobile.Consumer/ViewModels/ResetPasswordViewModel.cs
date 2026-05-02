using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Handles the password reset completion flow using email + reset token + new password.
/// This is intentionally explicit so support teams can guide users who copy token-based links manually.
/// </summary>
public sealed class ResetPasswordViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private CancellationTokenSource? _operationCancellation;

    private string _email = string.Empty;
    private string _token = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;
    private string? _successMessage;

    public ResetPasswordViewModel(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        ResetPasswordCommand = new AsyncCommand(ResetPasswordAsync, () => !IsBusy);
    }

    /// <summary>
    /// Command bound to the reset action button.
    /// </summary>
    public AsyncCommand ResetPasswordCommand { get; }

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

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmNewPassword
    {
        get => _confirmNewPassword;
        set => SetProperty(ref _confirmNewPassword, value);
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
    /// Cancels any in-flight password reset completion when the page is no longer visible.
    /// </summary>
    /// <returns>A completed task because cancellation is signaled synchronously.</returns>
    public override Task OnDisappearingAsync()
    {
        CancelCurrentOperation();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies prefilled handoff values from earlier auth-recovery steps without overwriting
    /// fields the user has already changed on the reset page.
    /// </summary>
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

    private async Task ResetPasswordAsync()
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
            ResetPasswordCommand.RaiseCanExecuteChanged();
        });

        var operationCancellation = BeginCurrentOperation();
        try
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                RunOnMain(() => ErrorMessage = AppResources.EmailRequired);
                return;
            }

            if (string.IsNullOrWhiteSpace(Token))
            {
                RunOnMain(() => ErrorMessage = AppResources.ResetPasswordTokenRequired);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordRequired);
                return;
            }

            if (NewPassword.Length < 8)
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordMinLength);
                return;
            }

            if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
            {
                RunOnMain(() => ErrorMessage = AppResources.PasswordMismatch);
                return;
            }

            var success = await _authService.ResetPasswordAsync(
                Email.Trim(),
                Token.Trim(),
                NewPassword,
                operationCancellation.Token);

            if (!success)
            {
                RunOnMain(() => ErrorMessage = AppResources.ResetPasswordFailed);
                return;
            }

            RunOnMain(() =>
            {
                SuccessMessage = AppResources.ResetPasswordSuccess;

                // Security hygiene: clear secrets from memory-backed UI bindings after success.
                Token = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
            });
        }
        catch (OperationCanceledException)
        {
            // Navigation away from the reset completion screen intentionally cancels stale work.
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.ResetPasswordFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                ResetPasswordCommand.RaiseCanExecuteChanged();
            });
            EndCurrentOperation(operationCancellation);
        }
    }

    /// <summary>
    /// Starts a cancellable password reset completion and cancels any stale operation still in-flight.
    /// </summary>
    private CancellationTokenSource BeginCurrentOperation()
    {
        var current = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _operationCancellation, current);
        previous?.Cancel();
        return current;
    }

    /// <summary>
    /// Cancels the active password reset completion without disposing a token source still observed by service code.
    /// </summary>
    private void CancelCurrentOperation()
    {
        var current = Interlocked.Exchange(ref _operationCancellation, null);
        current?.Cancel();
    }

    /// <summary>
    /// Releases a completed password reset completion when it still owns the active operation slot.
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
