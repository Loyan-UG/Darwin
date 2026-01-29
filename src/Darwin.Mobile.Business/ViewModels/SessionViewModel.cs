using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Loyalty;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Mobile.Shared.Navigation;

namespace Darwin.Mobile.Business.ViewModels;

/// <summary>
/// View model for the scan session page in the business app.
/// It loads details about a scanned QR session and exposes commands to confirm accrual or redemption.
/// </summary>
public sealed class SessionViewModel : BaseViewModel
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly INavigationService _navigationService;

    private string _sessionToken = string.Empty;
    private string? _customerName;
    private int _pointsBalance;
    private bool _canConfirmAccrual;
    private bool _canConfirmRedemption;
    private int _pointsToAccrue = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionViewModel"/> class.
    /// </summary>
    public SessionViewModel(ILoyaltyService loyaltyService, INavigationService navigationService)
    {
        _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

        LoadSessionCommand = new AsyncCommand(LoadSessionAsync);
        ConfirmAccrualCommand = new AsyncCommand(ConfirmAccrualAsync, () => CanConfirmAccrual && !IsBusy);
        ConfirmRedemptionCommand = new AsyncCommand(ConfirmRedemptionAsync, () => CanConfirmRedemption && !IsBusy);
    }

    /// <summary>
    /// Gets or sets the session token (passed from the scanner).
    /// </summary>
    public string SessionToken
    {
        get => _sessionToken;
        set => SetProperty(ref _sessionToken, value);
    }

    /// <summary>
    /// Gets or sets the customer display name.
    /// </summary>
    public string? CustomerName
    {
        get => _customerName;
        private set => SetProperty(ref _customerName, value);
    }

    /// <summary>
    /// Gets or sets the current points balance of the account associated with this session.
    /// </summary>
    public int PointsBalance
    {
        get => _pointsBalance;
        private set => SetProperty(ref _pointsBalance, value);
    }

    /// <summary>
    /// Gets a value indicating whether accrual is allowed.
    /// </summary>
    public bool CanConfirmAccrual
    {
        get => _canConfirmAccrual;
        private set
        {
            if (SetProperty(ref _canConfirmAccrual, value))
            {
                ConfirmAccrualCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether redemption is allowed.
    /// </summary>
    public bool CanConfirmRedemption
    {
        get => _canConfirmRedemption;
        private set
        {
            if (SetProperty(ref _canConfirmRedemption, value))
            {
                ConfirmRedemptionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of points that will be added when confirming accrual.
    /// </summary>
    public int PointsToAccrue
    {
        get => _pointsToAccrue;
        set => SetProperty(ref _pointsToAccrue, value);
    }

    /// <summary>
    /// Command that loads session details from the server.
    /// </summary>
    public AsyncCommand LoadSessionCommand { get; }

    /// <summary>
    /// Command that confirms a points accrual for the current session.
    /// </summary>
    public AsyncCommand ConfirmAccrualCommand { get; }

    /// <summary>
    /// Command that confirms a redemption for the current session.
    /// </summary>
    public AsyncCommand ConfirmRedemptionCommand { get; }

    /// <summary>
    /// Loads session details using the current <see cref="SessionToken"/>.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadSessionAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(SessionToken))
        {
            ErrorMessage = "Invalid session token.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _loyaltyService.ProcessScanSessionForBusinessAsync(SessionToken, CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load session details.";
                return;
            }

            var model = result.Value;
            CustomerName = model.CustomerDisplayName;
            PointsBalance = model.AccountSummary.PointsBalance;
            CanConfirmAccrual = model.CanConfirmAccrual;
            CanConfirmRedemption = model.CanConfirmRedemption;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Confirms points accrual for the current session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfirmAccrualAsync()
    {
        ErrorMessage = null;
        if (!CanConfirmAccrual)
        {
            ErrorMessage = "Accrual is not allowed for this session.";
            return;
        }

        if (PointsToAccrue <= 0)
        {
            ErrorMessage = "Points must be greater than zero.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _loyaltyService.ConfirmAccrualAsync(SessionToken, PointsToAccrue, CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to confirm accrual.";
                return;
            }

            // Update points balance and disable further accrual/redemption on this session.
            PointsBalance = result.Value.PointsBalance;
            CanConfirmAccrual = false;
            CanConfirmRedemption = false;

            // TODO: Navigate to a result sheet page in a later step.
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Confirms reward redemption for the current session.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ConfirmRedemptionAsync()
    {
        ErrorMessage = null;
        if (!CanConfirmRedemption)
        {
            ErrorMessage = "Redemption is not allowed for this session.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _loyaltyService.ConfirmRedemptionAsync(SessionToken, CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to confirm redemption.";
                return;
            }

            PointsBalance = result.Value.PointsBalance;
            CanConfirmAccrual = false;
            CanConfirmRedemption = false;

            // TODO: Navigate to a result sheet page in a later step.
        }
        finally
        {
            IsBusy = false;
        }
    }
}
