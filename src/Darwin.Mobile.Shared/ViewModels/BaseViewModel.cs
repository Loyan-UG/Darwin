using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Darwin.Mobile.Shared.ViewModels
{
    /// <summary>
    /// Base type for all mobile view models providing property change
    /// notification and common UI state such as busy flags and error messages.
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isBusy;
        private string? _title;
        private string? _errorMessage;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the page title bound to the shell or view header.
        /// </summary>
        public string? Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the view is currently
        /// performing a long-running operation. This property is typically
        /// bound to activity indicators or disables action buttons.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        /// <summary>
        /// Gets or sets the last user-facing error message to be displayed
        /// in the UI. The view model should set this when a recoverable
        /// error occurs and clear it once the user has acknowledged the issue.
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Safely sets a backing field and raises the <see cref="PropertyChanged"/>
        /// event if the value has changed.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to the backing field.</param>
        /// <param name="value">New value to assign.</param>
        /// <param name="propertyName">Name of the property (auto-filled by compiler).</param>
        /// <returns><c>true</c> if the value changed; otherwise <c>false</c>.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises a <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is null)
            {
                return;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
