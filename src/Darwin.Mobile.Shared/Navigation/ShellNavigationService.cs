using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Navigation
{
    /// <summary>
    /// Default navigation service implementation that delegates to MAUI Shell.
    /// </summary>
    public sealed class ShellNavigationService : INavigationService
    {
        /// <inheritdoc />
        public Task GoToAsync(string route, IDictionary<string, object?>? parameters = null)
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
        }

        /// <inheritdoc />
        public Task GoBackAsync()
        {
            if (Shell.Current is null)
            {
                throw new InvalidOperationException("Shell navigation is not available. Ensure Shell is initialized.");
            }

            return Shell.Current.GoToAsync("..");
        }
    }
}
