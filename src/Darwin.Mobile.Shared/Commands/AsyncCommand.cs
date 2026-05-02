using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;

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
        private int _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous operation to execute.</param>
        /// <param name="canExecute">Optional predicate that indicates whether the command can execute.</param>
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
        public bool IsExecuting => Volatile.Read(ref _isExecuting) == 1;

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            if (IsExecuting)
            {
                return false;
            }

            try
            {
                return _canExecute?.Invoke() ?? true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AsyncCommand CanExecute failed closed: {ex}");
                return false;
            }
        }

        /// <inheritdoc />
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            if (Interlocked.Exchange(ref _isExecuting, 1) == 1)
            {
                return;
            }

            try
            {
                RaiseCanExecuteChanged();

                await _execute();
            }
            catch (Exception ex)
            {
                // Commands are invoked through async-void ICommand.Execute. Any unhandled exception here
                // would crash the app, so unexpected command failures are logged and the command resets safely.
                Debug.WriteLine($"AsyncCommand execution failed: {ex}");
            }
            finally
            {
                Volatile.Write(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Forces WPF/XAML binding engine to reevaluate CanExecute.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            if (MainThread.IsMainThread)
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
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
        private int _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCommand{T}"/> class.
        /// </summary>
        /// <param name="execute">The asynchronous operation to execute.</param>
        /// <param name="canExecute">Optional predicate that indicates whether the command can execute.</param>
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
        public bool IsExecuting => Volatile.Read(ref _isExecuting) == 1;

        /// <inheritdoc />
        public bool CanExecute(object? parameter)
        {
            if (IsExecuting)
            {
                return false;
            }

            try
            {
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
            catch (Exception ex)
            {
                Debug.WriteLine($"AsyncCommand<{typeof(T).Name}> CanExecute failed closed: {ex}");
                return false;
            }
        }

        /// <inheritdoc />
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            if (Interlocked.Exchange(ref _isExecuting, 1) == 1)
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
                RaiseCanExecuteChanged();

                await _execute(typed);
            }
            catch (Exception ex)
            {
                // Commands are invoked through async-void ICommand.Execute. Any unhandled exception here
                // would crash the app, so unexpected command failures are logged and the command resets safely.
                Debug.WriteLine($"AsyncCommand<{typeof(T).Name}> execution failed: {ex}");
            }
            finally
            {
                Volatile.Write(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Forces WPF/XAML binding engine to reevaluate CanExecute.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            if (MainThread.IsMainThread)
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => CanExecuteChanged?.Invoke(this, EventArgs.Empty));
        }
    }
}
