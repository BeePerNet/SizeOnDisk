using SizeOnDisk.Properties;
using System;
using System.Windows;
using WPFByYourCommand.Behaviors;
using WPFByYourCommand.Exceptions;

namespace SizeOnDisk
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            Settings.CheckUpgrade();

            GlobalizationBehavior.ChangeLanguage(Settings.Default.Language);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ExceptionBox.ShowException(e.Exception);
            e.Handled = true;
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ExceptionBox.ShowException(e.ExceptionObject as Exception);
        }

    }
}
