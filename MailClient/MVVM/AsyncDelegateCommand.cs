using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MailClient.MVVM
{
    public class AsyncDelegateCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public event EventHandler CanExecuteChanged;

        public AsyncDelegateCommand(Action<object> execute)
                       : this(execute, null)
        {
        }


        public AsyncDelegateCommand(Action<object> execute,
                       Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter = null)
        {
            if (_canExecute == null)
            {
                return true;
            }

            return _canExecute(parameter);
        }

        public async void Execute(object parameter = null)
        {
            await Task.Run(() => _execute(parameter));
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}
