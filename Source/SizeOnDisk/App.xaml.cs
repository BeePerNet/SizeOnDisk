using SizeOnDisk.Properties;
using SizeOnDisk.Utilities;
using System;
using System.Threading;
using System.Windows;

namespace SizeOnDisk
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            /*bool usingDarkTheme = false;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    usingDarkTheme = Convert.ToInt32(key.GetValue("AppsUseLightTheme", 0)) == 0;
                }
            }

            //Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);

            // now set the Green accent and dark theme
            ThemeManager.ChangeAppStyle(Application.Current,
                                        //appStyle.Item2,
                                        ThemeManager.GetAccent("Blue"),
                                        ThemeManager.GetAppTheme(usingDarkTheme ? "BaseDark": "BaseLight"));*/

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            Settings.CheckUpgrade();

            //ThreadPool.GetMinThreads(out int minWork, out int mincompletion);
            //ThreadPool.SetMinThreads(minWork * 4, mincompletion * 4);



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
