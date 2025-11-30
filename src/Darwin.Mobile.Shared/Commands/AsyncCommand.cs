using System.Windows.Input;

namespace Darwin.Mobile.Shared.Commands
{
    /// <summary>
    /// Represents an asynchronous command that can be bound from XAML and
    /// encapsulates an asynchronous operation without a parameter.
    /// </summary>
    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class.
        /// </summary>
        /// <param name="execute">Asynchronous operation to execute.</param>
        /// <param name="canExecute">Optional predicate controlling whether the command can run.</param>
        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
            {
                return false;
            }

            return _canExecute?.Invoke() ?? true;
        }

        /// <inheritdoc />
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _execute().ConfigureAwait(false);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Notifies bound controls that the executability of this command may have changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }

    /// <summary>
    /// Represents an asynchronous command that can be bound from XAML and
    /// encapsulates an asynchronous operation with a strongly-typed parameter.
    /// </summary>
    /// <typeparam name="T">Type of the command parameter.</typeparam>
    public sealed class AsyncCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">Asynchronous operation to execute.</param>
        /// <param name="canExecute">Optional predicate controlling whether the command can run.</param>
        public AsyncCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
            {
                return false;
            }

            if (_canExecute is null)
            {
                return true;
            }

            return _canExecute((T?)parameter);
        }

        /// <inheritdoc />
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _execute((T?)parameter).ConfigureAwait(false);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Notifies bound controls that the executability of this command may have changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
