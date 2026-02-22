using System;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Business.Constants;
using Darwin.Mobile.Business.Resources;
using Darwin.Mobile.Business.Services.Identity;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Navigation;
using Darwin.Mobile.Shared.ViewModels;

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

        private bool _loadedOnce;
        private string _businessName = "—";
        private string _businessCategory = "—";
        private string _businessCity = "—";
        private string _operatorEmail = "—";
        private string _businessDescription = string.Empty;

        public HomeViewModel(
            INavigationService navigationService,
            IBusinessIdentityContextService businessIdentityContextService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _businessIdentityContextService = businessIdentityContextService ?? throw new ArgumentNullException(nameof(businessIdentityContextService));

            LoadContextCommand = new AsyncCommand(LoadContextAsync, () => !IsBusy);
            ScanCommand = new AsyncCommand(OpenScannerAsync, () => !IsBusy);
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
        /// Optional short description fetched from business detail endpoint.
        /// </summary>
        public string BusinessDescription
        {
            get => _businessDescription;
            private set => SetProperty(ref _businessDescription, value);
        }

        /// <summary>
        /// Indicates whether a business context has been resolved.
        /// </summary>
        public bool HasBusinessContext => !string.IsNullOrWhiteSpace(BusinessName) && BusinessName != "—";

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
                        BusinessName = "—";
                        BusinessCategory = "—";
                        BusinessCity = "—";
                        OperatorEmail = "—";
                        BusinessDescription = string.Empty;

                        ErrorMessage = result.Error ?? "Unable to resolve current business context.";
                        OnPropertyChanged(nameof(HasBusinessContext));
                    });

                    return;
                }

                var context = result.Value;

                RunOnMain(() =>
                {
                    BusinessName = context.BusinessName;
                    BusinessCategory = string.IsNullOrWhiteSpace(context.Category) ? "—" : context.Category;
                    BusinessCity = string.IsNullOrWhiteSpace(context.City) ? "—" : context.City;
                    OperatorEmail = string.IsNullOrWhiteSpace(context.OperatorEmail) ? "—" : context.OperatorEmail;
                    BusinessDescription = context.ShortDescription ?? string.Empty;

                    ErrorMessage = null;
                    OnPropertyChanged(nameof(HasBusinessContext));
                });
            }
            catch (Exception ex)
            {
                RunOnMain(() =>
                {
                    ErrorMessage = $"Unable to load business information. {ex.Message}";
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
            await _navigationService.GoToAsync($"//{Routes.Scanner}");
        }
    }
}
