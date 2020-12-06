using System;
using System.Windows.Input;

namespace LogGrokCore
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public DelegateCommand(Action execute, Func<bool> canExecute)
            : this(execute)
        {
            _canExecute = _ => canExecute();
        }
        
        public DelegateCommand(Action execute)
        {
            _execute = _ => execute();
        }

        public DelegateCommand(Action<object> execute, Func<object?, bool> canExecute)
            : this(execute)
        {
            _canExecute = canExecute;
        }
        
        public DelegateCommand(Action<object> execute)
        {
            _execute = o =>
            {
                if (o == null) throw new NullReferenceException("Execute: parameter is null.");
                execute(o);
            };
        }

        public static DelegateCommand Create<T>(Action<T> execute, Func<T, bool> canExecute)
        {
            void Execute(object? o)
            {
                if (o is T t)
                    execute(t);
                else
                    throw new InvalidOperationException($"Unexpected parameter: {o}");
            }

            bool CanExecute(object? o)
            {
                if (o is T t) return canExecute(t);
                return false;
            }

            return new DelegateCommand(Execute, CanExecute);
        }

        public static DelegateCommand Create<T>(Action<T> execute)
        {
            void Execute(object? o)
            {
                if (o is T t)
                    execute(t);
                else
                    throw new InvalidOperationException($"Unexpected parameter: {o}");
            }

            return new DelegateCommand(Execute, _ => true);
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

#pragma warning disable CS0169, CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0169
#pragma warning restore CS0067
    }
}