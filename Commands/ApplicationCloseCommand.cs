using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenCVVideoRedactor.Commands
{
    public class ApplicationCloseCommand : ICommand
    {
        private static ICommand _instance;
        public static ICommand Instance
        {
            get
            {
                return _instance;
            }
        }
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        public bool CanExecute(object? parameter)
        {
            return App.Current != null && App.Current.MainWindow != null;
        }

        public void Execute(object? parameter)
        {
            App.Current.Shutdown();
        }
        private ApplicationCloseCommand() { }
        static ApplicationCloseCommand()
        {
            _instance = new ApplicationCloseCommand();
        }
    }
}
