using System;
using System.Windows.Input;

namespace LogGrokCore
{
    public class DelegateCommand : ICommand
    {
        private readonly Action _action;
        private readonly Func<bool>? _canExecute;

        public DelegateCommand(Action action, Func<bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public DelegateCommand(Action action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public event EventHandler CanExecuteChanged;
    }
}