using Darwin.Mobile.Shared.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Darwin.Mobile.Business
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;

        public App(IAuthService authService)
        {
            InitializeComponent();
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Force a deterministic visual theme for the Business app.
            // We intentionally do not follow device Dark/Light mode for now.
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        /// <summary>
        /// Best-effort token refresh when the app returns from background.
        /// This avoids stale access tokens causing misleading downstream errors.
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            _ = TryRefreshSilentlyAsync();
        }

        private async Task TryRefreshSilentlyAsync()
        {
            try
            {
                await _authService.TryRefreshAsync(CancellationToken.None);
            }
            catch
            {
                // Best effort only. If refresh fails, secured screens will redirect on next guarded action.
            }
        }
    }
}