using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.Navigation
{
    /// <summary>
    /// Abstraction for navigation operations used by view models.
    /// This keeps navigation logic decoupled from MAUI Shell usage and
    /// makes unit testing of view models easier.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to the specified route using the platform's navigation mechanism.
        /// </summary>
        /// <param name="route">Shell route or page identifier.</param>
        /// <param name="parameters">Optional route query parameters.</param>
        /// <returns>A task that completes once navigation has been requested.</returns>
        Task GoToAsync(string route, IDictionary<string, object?>? parameters = null);

        /// <summary>
        /// Navigates back to the previous page in the navigation stack, if any.
        /// </summary>
        /// <returns>A task that completes once navigation has been requested.</returns>
        Task GoBackAsync();
    }
}
