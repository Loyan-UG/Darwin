using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Darwin.Mobile.Shared.ViewModels
{
    /// <summary>
    /// Base class for all view models in the mobile apps.
    /// Provides INotifyPropertyChanged support and simple
    /// busy/error state tracking.
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
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            protected set => SetProperty(ref _errorMessage, value);
        }

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
    }
}
