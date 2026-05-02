using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Shared.Navigation
{
    /// <summary>
    /// Default navigation service implementation that delegates to MAUI Shell.
    /// </summary>
    /// <remarks>
    /// View models and shared services can resume on background threads after awaited network/storage work.
    /// This service owns the UI-thread transition so every caller gets safe Shell navigation without duplicating
    /// platform-specific dispatch logic.
    /// </remarks>
    public sealed class ShellNavigationService : INavigationService
    {
        /// <inheritdoc />
        public Task GoToAsync(string route, IDictionary<string, object?>? parameters = null)
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Shell.Current is null)
                {
                    throw new InvalidOperationException("Shell navigation is not available. Ensure Shell is initialized.");
                }

                if (parameters is null || parameters.Count == 0)
                {
                    return Shell.Current.GoToAsync(route);
                }

                return Shell.Current.GoToAsync(route, parameters);
            });
        }

        /// <inheritdoc />
        public Task GoBackAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (Shell.Current is null)
                {
                    throw new InvalidOperationException("Shell navigation is not available. Ensure Shell is initialized.");
                }

                return Shell.Current.GoToAsync("..");
            });
        }
    }
}
