using System;
using System.Windows.Input;
using DryIoc;

namespace LogGrokCore
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool>? _canExecute;

        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            _execute = _ => execute();
            _canExecute = _ => canExecute();
        }

        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public static DelegateCommand Create<T>(Action<T> execute, Func<T, bool> canExecute)
        {
            void Execute(object o)
            {
                if (o is T t)
                    execute(t);
                else
                    throw new InvalidOperationException();
            }

            bool CanExecute(object o)
            {
                if (o is T t) return canExecute(t);
                throw new InvalidOperationException();
            }

            return new DelegateCommand(Execute, CanExecute);
        }

        public static DelegateCommand Create<T>(Action<T> execute)
        {
            return Create(execute, _ => true);
        }

        public DelegateCommand(Action execute)
        {
            _execute = _ => execute();
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged;
    }
}