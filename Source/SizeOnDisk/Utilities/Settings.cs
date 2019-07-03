using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows;
using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;

namespace SizeOnDisk.Properties
{
    internal sealed partial class Settings
    {
        public static void CheckUpgrade()
        {
            Assembly a = Assembly.GetExecutingAssembly();
            Version appVersion = a.GetName().Version;
            string configVersion = string.Empty;
            //TODO:Catch the ConfigurationException do thing to continue: delete settings, reload, reset functions, to test...
            try
            {
                configVersion = Settings.Default.ApplicationVersion;
            }
            catch (ConfigurationException ex)
            {
                while (ex != null)
                {
                    if (ex.Filename == null)
                    {
                        ex = ex.InnerException as ConfigurationException;
                    }
                    else
                    {
                        MessageBox.Show("A configuration file error as occured, the application needs to restart to reload the good one.", "Application needs to be restarted", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        File.Delete(ex.Filename);
                        ShellHelper.Restart(false);
                        return;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(configVersion) || Version.Parse(configVersion) < appVersion)
            {
                Settings.Default.ApplicationVersion = appVersion.ToString();
                Settings.Default.Save();
            }
        }
    }
}
