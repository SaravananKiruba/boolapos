using System;
using System.Windows.Input;

namespace Page_Navigation_App.Utilities
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            // Handle the case where parameter is not of type T or is MS.Internal.NamedObject
            if (parameter != null && !(parameter is T) && parameter.GetType().FullName == "MS.Internal.NamedObject")
            {
                return _canExecute == null; // Return default value when parameter is NamedObject
            }
            
            try
            {
                return _canExecute == null || _canExecute((T)(parameter ?? default(T)));
            }
            catch
            {
                // In case of any casting exception, return a safe default value
                return _canExecute == null;
            }
        }

        public void Execute(object parameter)
        {
            // Only execute if parameter is of correct type or can be cast to T
            if ((parameter == null && default(T) == null) || parameter is T)
            {
                _execute((T)parameter);
            }
            else if (parameter == null)
            {
                _execute(default(T));
            }
            // Silently ignore execution with incompatible parameter types
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
