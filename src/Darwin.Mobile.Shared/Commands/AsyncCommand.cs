using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Darwin.Mobile.Shared.Commands
{
    /// <summary>
    /// Simple async command implementation for use with MAUI bindings.
    /// It prevents concurrent executions by default and exposes execution state.
    /// </summary>
    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous operation to execute.</param>
        /// <param name="canExecute">
        /// Optional predicate that indicates whether the command can execute.
        /// </param>
        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Gets a value indicating whether the command is currently executing.
        /// </summary>
        public bool IsExecuting => _isExecuting;

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

                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Forces WPF/XAML binding engine to reevaluate CanExecute.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Generic async command that passes a strongly-typed parameter to the delegate.
    /// </summary>
    /// <typeparam name="T">The parameter type.</typeparam>
    public sealed class AsyncCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous operation to execute.</param>
        /// <param name="canExecute">
        /// Optional predicate that indicates whether the command can execute.
        /// </param>
        public AsyncCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <inheritdoc />
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// Gets a value indicating whether the command is currently executing.
        /// </summary>
        public bool IsExecuting => _isExecuting;

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

            if (parameter is null && typeof(T).IsValueType)
            {
                return _canExecute(default);
            }

            if (parameter is T typed)
            {
                return _canExecute(typed);
            }

            return false;
        }

        /// <inheritdoc />
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            T? typed = default;
            if (parameter is T value)
            {
                typed = value;
            }

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute(typed);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Forces WPF/XAML binding engine to reevaluate CanExecute.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
