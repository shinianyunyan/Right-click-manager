using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RightClickManager.Base
{
    public partial class RelayCommand : ICommand
    {
        private readonly Action action;
        private readonly Func<bool>? canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action action) : this(action, null) { }

        public RelayCommand(Action action, Func<bool>? canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (canExecute == null) return true;
            return canExecute.Invoke();
        }

        public void Execute(object? parameter)
        {
            this.Execute();
        }

        public void Execute()
        {
            action.Invoke();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public partial class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> action;
        private readonly Func<T?, bool>? canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<T?> action) : this(action, null) { }

        public RelayCommand(Action<T?> action, Func<T?, bool>? canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (canExecute == null) return true;
            return canExecute.Invoke((T?)parameter);
        }

        public void Execute(object? parameter)
        {
            action.Invoke((T?)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public partial class AsyncRelayCommand : ICommand, INotifyPropertyChanged
    {
        private readonly Func<Task> action;
        private readonly Func<bool>? canExecute;

        private Task? task;

        public event EventHandler? CanExecuteChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public AsyncRelayCommand(Func<Task> action) : this(action, null) { }

        public AsyncRelayCommand(Func<Task> action, Func<bool>? canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public bool IsRunning
        {
            get
            {
                var _task = task;
                return _task != null && !_task.IsCompleted;
            }
        }

        public bool CanExecute(object? parameter)
        {
            if (canExecute == null) return true;
            var _task = task;
            if (_task != null && !_task.IsCompleted) return false;

            return canExecute.Invoke();
        }

        public void Execute(object? parameter)
        {
            _ = ExecuteAsync();
        }

        public Task ExecuteAsync()
        {
            var _task = task;
            if (_task != null && !_task.IsCompleted) return _task;

            return RunAsync();

            async Task RunAsync()
            {
                var _task = task = action.Invoke();
                RaiseCanExecuteChanged();
                try
                {
                    await _task;
                    RaiseCanExecuteChanged();
                }
                finally
                {
                    task = null;
                }
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        }
    }

    public partial class AsyncRelayCommand<T> : ICommand, INotifyPropertyChanged
    {
        private readonly Func<T?, Task> action;
        private readonly Func<T?, bool>? canExecute;

        private Task? task;

        public event EventHandler? CanExecuteChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public AsyncRelayCommand(Func<T?, Task> action) : this(action, null) { }

        public AsyncRelayCommand(Func<T?, Task> action, Func<T?, bool>? canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public bool IsRunning
        {
            get
            {
                var _task = task;
                return _task != null && !_task.IsCompleted;
            }
        }

        public bool CanExecute(object? parameter)
        {
            if (canExecute == null) return true;
            var _task = task;
            if (_task != null && !_task.IsCompleted) return false;

            return canExecute.Invoke((T?)parameter);
        }

        public void Execute(object? parameter)
        {
            _ = ExecuteAsync((T?)parameter);
        }

        public Task ExecuteAsync(T? parameter)
        {
            var _task = task;
            if (_task != null && !_task.IsCompleted)
            {
                Helpers.Logger.Info("Search already running, reusing existing task");
                return _task;
            }

            Helpers.Logger.Info("Search started");
            return RunAsync();

            async Task RunAsync()
            {
                var _task = task = action.Invoke(parameter);
                RaiseCanExecuteChanged();
                try
                {
                    await _task;
                    Helpers.Logger.Info("Search completed");
                    RaiseCanExecuteChanged();
                }
                catch (System.Exception ex)
                {
                    Helpers.Logger.Error("Search crashed", ex.ToString());
                }
                finally
                {
                    task = null;
                }
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        }
    }
}
