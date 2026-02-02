using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread

namespace Darwin.Mobile.Shared.ViewModels
{
    /// <summary>
    /// Base class for all view models in the mobile apps.
    /// Provides INotifyPropertyChanged support and simple
    /// busy/error state tracking.
    /// 
    /// Important: ViewModels may be updated from background continuations.
    /// Use <see cref="RunOnMain(Action)"/> to marshal UI-bound updates to
    /// the main (UI) thread when needed.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string? _errorMessage;

        /// <summary>
        /// Raised whenever a property value is changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether the view model is currently
        /// performing a background operation. UI can bind to this to show
        /// loading indicators and disable actions.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            protected set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the last error message that should be displayed to the user.
        /// This is a user-facing message and should not leak internal/server details.
        /// Use <see cref="HasError"/> to determine visibility in the view.
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            protected set
            {
                // Use SetProperty to raise PropertyChanged for ErrorMessage.
                // Also raise HasError so the UI can bind IsVisible to HasError.
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Convenience boolean that is true if <see cref="ErrorMessage"/> contains a non-empty value.
        /// Useful for XAML bindings (IsVisible).
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

        /// <summary>
        /// Helper to set a property and raise PropertyChanged when the value changes.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="storage">Reference to the backing field.</param>
        /// <param name="value">New value to set.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns><c>true</c> if the value changed; otherwise <c>false</c>.</returns>
        protected bool SetProperty<T>(
            ref T storage,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises the PropertyChanged event for the given property name.
        /// </summary>
        /// <param name="propertyName">The changed property name.</param>
        protected void OnPropertyChanged(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Called by the view when it becomes visible.
        /// Override in derived view models to load data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task OnAppearingAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called by the view when it is no longer visible.
        /// Override in derived view models to release resources.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual Task OnDisappearingAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes the provided <paramref name="action"/> on the main (UI) thread.
        /// 
        /// Why:
        /// - In mobile apps UI components must be touched only from the platform's UI thread.
        /// - Many asynchronous operations in services use ConfigureAwait(false) and their continuations
        ///   can run on thread-pool threads. If those continuations update UI-bound properties
        ///   (which raise PropertyChanged) we must marshal those updates to the UI thread.
        /// 
        /// Usage:
        /// - Call RunOnMain(() => { /* UI-bound property updates */ });
        /// - The method is safe to call even if already on the UI thread.
        /// 
        /// Implementation detail:
        /// - Uses Microsoft.Maui.ApplicationModel.MainThread, which abstracts platform-specific
        ///   marshalling (Android/iOS/Windows). If MainThread.IsMainThread is true, the action runs immediately.
        /// </summary>
        /// <param name="action">Action to execute on the UI thread.</param>
        protected void RunOnMain(Action action)
        {
            if (action is null) return;

            if (MainThread.IsMainThread)
            {
                action();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(action);
            }
        }
    }
}