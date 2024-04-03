using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OpenCVVideoRedactor.Commands
{
    public class ApplicationMaxMinCommand : ICommand
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
            var window = App.Current.MainWindow;
            if (window.WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(window);
            }
            else
            {
                SystemCommands.MaximizeWindow(window);
            }
        }
        private ApplicationMaxMinCommand() { }
        static ApplicationMaxMinCommand()
        {
            _instance = new ApplicationMaxMinCommand();
        }
    }
}
