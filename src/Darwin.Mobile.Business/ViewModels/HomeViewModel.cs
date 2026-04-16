using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.ViewModels;
using Darwin.Contracts.Businesses;

namespace Darwin.Mobile.Business.ViewModels
{
    /// <summary>
    /// View model for the business home page.
    /// 
    /// UX goals:
    /// - Show who is logged in and which business context is active.
    /// - Reduce test/operator confusion when multiple business accounts exist.
    /// - Keep a clear and fast primary action to open scanner flow.
    /// </summary>
    public sealed partial class HomeViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IBusinessIdentityContextService _businessIdentityContextService;
        private readonly IBusinessAuthorizationService _businessAuthorizationService;
        private readonly IBusinessAccessService _businessAccessService;

        private bool _loadedOnce;
        private string _businessName = AppResources.HomeUnavailableValue;
        private string _businessCategory = AppResources.HomeUnavailableValue;
        private string _businessCity = AppResources.HomeUnavailableValue;
        private string _operatorEmail = AppResources.HomeUnavailableValue;
        private string _businessDescription = string.Empty;
        private string _operatorRole = AppResources.HomeUnavailableValue;
        private string _businessOperationalStatusLabel = AppResources.HomeUnavailableValue;
        private string _businessOperationalStatusMessage = string.Empty;
        private string _setupChecklistSummary = string.Empty;
        private bool _isOperationsAllowed;

        public HomeViewModel(
            INavigationService navigationService,
            IBusinessIdentityContextService businessIdentityContextService,
            IBusinessAuthorizationService businessAuthorizationService,
            IBusinessAccessService businessAccessService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _businessIdentityContextService = businessIdentityContextService ?? throw new ArgumentNullException(nameof(businessIdentityContextService));
            _businessAuthorizationService = businessAuthorizationService ?? throw new ArgumentNullException(nameof(businessAuthorizationService));
            _businessAccessService = businessAccessService ?? throw new ArgumentNullException(nameof(businessAccessService));

            LoadContextCommand = new AsyncCommand(LoadContextAsync, () => !IsBusy);
            ScanCommand = new AsyncCommand(OpenScannerAsync, () => !IsBusy && IsOperationsAllowed);
        }

        /// <summary>
        /// Main title shown on top of the page.
        /// </summary>
        public string Greeting => AppResources.HomeTitle;

        /// <summary>
        /// Currently resolved business display name.
        /// </summary>
        public string BusinessName
        {
            get => _businessName;
            private set => SetProperty(ref _businessName, value);
        }

        /// <summary>
        /// Business category badge text (e.g. Bakery, Cafe).
        /// </summary>
        public string BusinessCategory
        {
            get => _businessCategory;
            private set => SetProperty(ref _businessCategory, value);
        }

        /// <summary>
        /// Business city shown for quick context.
        /// </summary>
        public string BusinessCity
        {
            get => _businessCity;
            private set => SetProperty(ref _businessCity, value);
        }

        /// <summary>
        /// Operator email from access token claims.
        /// </summary>
        public string OperatorEmail
        {
            get => _operatorEmail;
            private set => SetProperty(ref _operatorEmail, value);
        }


        /// <summary>
        /// Operator role resolved from token scope claims for UI visibility.
        /// </summary>
        public string OperatorRole
        {
            get => _operatorRole;
            private set => SetProperty(ref _operatorRole, value);
        }

        /// <summary>
        /// Optional short description fetched from business detail endpoint.
        /// </summary>
        public string BusinessDescription
        {
            get => _businessDescription;
            private set => SetProperty(ref _businessDescription, value);
        }

        /// <summary>
        /// Gets the localized operational status label for the current business.
        /// </summary>
        public string BusinessOperationalStatusLabel
        {
            get => _businessOperationalStatusLabel;
            private set => SetProperty(ref _businessOperationalStatusLabel, value);
        }

        /// <summary>
        /// Gets the operator-facing status message for the current business lifecycle state.
        /// </summary>
        public string BusinessOperationalStatusMessage
        {
            get => _businessOperationalStatusMessage;
            private set
            {
                if (SetProperty(ref _businessOperationalStatusMessage, value))
                {
                    OnPropertyChanged(nameof(HasBusinessOperationalStatusMessage));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether live operational workflows are currently allowed.
        /// </summary>
        public bool IsOperationsAllowed
        {
            get => _isOperationsAllowed;
            private set
            {
                if (SetProperty(ref _isOperationsAllowed, value))
                {
                    ScanCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether a business status message should be shown.
        /// </summary>
        public bool HasBusinessOperationalStatusMessage => !string.IsNullOrWhiteSpace(BusinessOperationalStatusMessage);

        /// <summary>
        /// Gets the setup checklist summary used for soft-gated onboarding guidance.
        /// </summary>
        public string SetupChecklistSummary
        {
            get => _setupChecklistSummary;
            private set
            {
                if (SetProperty(ref _setupChecklistSummary, value))
                {
                    OnPropertyChanged(nameof(HasSetupChecklistSummary));
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the setup checklist should be shown.
        /// </summary>
        public bool HasSetupChecklistSummary => !string.IsNullOrWhiteSpace(SetupChecklistSummary);

        /// <summary>
        /// Indicates whether a business context has been resolved.
        /// </summary>
        public bool HasBusinessContext => !string.IsNullOrWhiteSpace(BusinessName) && BusinessName != AppResources.HomeUnavailableValue;

        public AsyncCommand LoadContextCommand { get; }
        public AsyncCommand ScanCommand { get; }

        public override async Task OnAppearingAsync()
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            await LoadContextAsync().ConfigureAwait(false);
        }

        private async Task LoadContextAsync()
        {
            ErrorMessage = null;
            IsBusy = true;
            LoadContextCommand.RaiseCanExecuteChanged();
            ScanCommand.RaiseCanExecuteChanged();

            try
            {
                var result = await _businessIdentityContextService.GetCurrentAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                if (!result.Succeeded || result.Value is null)
                {
                    RunOnMain(() =>
                    {
                        // Keep fallback placeholders visible so UI remains stable.
                        BusinessName = AppResources.HomeUnavailableValue;
                        BusinessCategory = AppResources.HomeUnavailableValue;
                        BusinessCity = AppResources.HomeUnavailableValue;
                        OperatorEmail = AppResources.HomeUnavailableValue;
                        BusinessDescription = string.Empty;
                        OperatorRole = AppResources.HomeUnavailableValue;
                        BusinessOperationalStatusLabel = AppResources.HomeUnavailableValue;
                        BusinessOperationalStatusMessage = string.Empty;
                        SetupChecklistSummary = string.Empty;
                        IsOperationsAllowed = false;

                        ErrorMessage = result.Error ?? AppResources.HomeResolveCurrentBusinessContextFailed;
                        OnPropertyChanged(nameof(HasBusinessContext));
                    });

                    return;
                }

                var context = result.Value;

                RunOnMain(() =>
                {
                    BusinessName = context.BusinessName;
                    BusinessCategory = string.IsNullOrWhiteSpace(context.Category) ? AppResources.HomeUnavailableValue : context.Category;
                    BusinessCity = string.IsNullOrWhiteSpace(context.City) ? AppResources.HomeUnavailableValue : context.City;
                    OperatorEmail = string.IsNullOrWhiteSpace(context.OperatorEmail) ? AppResources.HomeUnavailableValue : context.OperatorEmail;
                    BusinessDescription = context.ShortDescription ?? string.Empty;

                    ErrorMessage = null;
                    OnPropertyChanged(nameof(HasBusinessContext));
                });

                var authSnapshotTask = _businessAuthorizationService.GetSnapshotAsync(CancellationToken.None);
                var accessStateTask = _businessAccessService.GetCurrentAccessStateAsync(CancellationToken.None);
                await Task.WhenAll(authSnapshotTask, accessStateTask).ConfigureAwait(false);

                var authSnapshotResult = await authSnapshotTask.ConfigureAwait(false);
                var accessStateResult = await accessStateTask.ConfigureAwait(false);

                RunOnMain(() =>
                {
                    OperatorRole = authSnapshotResult.Succeeded && authSnapshotResult.Value is not null
                        ? authSnapshotResult.Value.RoleDisplayName
                        : AppResources.HomeUnavailableValue;

                    ApplyAccessState(accessStateResult.Succeeded ? accessStateResult.Value : null);
                });
            }
            catch (Exception)
            {
                RunOnMain(() =>
                {
                    ErrorMessage = AppResources.HomeLoadBusinessInfoFailed;
                    BusinessOperationalStatusLabel = AppResources.HomeUnavailableValue;
                    BusinessOperationalStatusMessage = AppResources.BusinessAccessStateLoadFailed;
                    SetupChecklistSummary = string.Empty;
                    IsOperationsAllowed = false;
                    OnPropertyChanged(nameof(HasBusinessContext));
                });
            }
            finally
            {
                RunOnMain(() =>
                {
                    IsBusy = false;
                    LoadContextCommand.RaiseCanExecuteChanged();
                    ScanCommand.RaiseCanExecuteChanged();
                });
            }
        }

        private async Task OpenScannerAsync()
        {
            if (!IsOperationsAllowed)
            {
                ErrorMessage = BusinessOperationalStatusMessage;
                return;
            }

            await _navigationService.GoToAsync($"//{Routes.Scanner}");
        }

        private void ApplyAccessState(BusinessAccessStateResponse? state)
        {
            if (state is null)
            {
                BusinessOperationalStatusLabel = AppResources.HomeUnavailableValue;
                BusinessOperationalStatusMessage = AppResources.BusinessAccessStateLoadFailed;
                SetupChecklistSummary = string.Empty;
                IsOperationsAllowed = false;
                return;
            }

            BusinessOperationalStatusLabel = BusinessAccessStateUiMapper.GetOperationalStatusLabel(state);
            BusinessOperationalStatusMessage = BusinessAccessStateUiMapper.GetOperationalStatusMessage(state);
            SetupChecklistSummary = (!state.IsOperationsAllowed || !state.IsSetupComplete)
                ? BusinessAccessStateUiMapper.BuildSetupChecklistSummary(state)
                : string.Empty;
            IsOperationsAllowed = state.IsOperationsAllowed;
        }
    }
}
