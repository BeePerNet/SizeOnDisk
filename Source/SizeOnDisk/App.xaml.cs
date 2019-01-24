using System;
using System.Windows;
using SizeOnDisk.Properties;
using SizeOnDisk.Utilities;

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
#if DEBUG
            //TextWriterTraceListener tr1 = new TextWriterTraceListener(System.Console.Out);
            //Debug.Listeners.Clear();
            //Debug.Listeners.Add(tr1);
#endif
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
