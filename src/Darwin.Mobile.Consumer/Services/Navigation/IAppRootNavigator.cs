using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace Darwin.Mobile.Consumer.Services.Navigation;

/// <summary>
/// Defines a single place to switch the Consumer app root page in a window-aware way.
///
/// Why this abstraction exists:
/// - ViewModels must not depend on concrete <see cref="App"/> members.
/// - Modern MAUI discourages using <c>Application.MainPage</c> directly.
/// - Root switching must target the active <see cref="Window"/> and run on its dispatcher.
/// </summary>
public interface IAppRootNavigator
{
    /// <summary>
    /// Attaches the primary application window used for root page changes.
    /// This is called by <see cref="App"/> during startup.
    /// </summary>
    /// <param name="window">The created MAUI window.</param>
    void AttachWindow(Window window);

    /// <summary>
    /// Shows the authenticated shell root.
    /// </summary>
    Task NavigateToAuthenticatedShellAsync();

    /// <summary>
    /// Shows the unauthenticated login root.
    /// </summary>
    Task NavigateToLoginAsync();
}
