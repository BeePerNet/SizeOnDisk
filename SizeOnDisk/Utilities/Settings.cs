using SizeOnDisk.Shell;
using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Windows;

namespace SizeOnDisk.Properties
{
    internal sealed partial class Settings
    {
        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<En attente>")]
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
                Settings.Default.Upgrade();
                Settings.Default.ApplicationVersion = appVersion.ToString();
                Settings.Default.Save();

                //Clean the old files
                string fullFilePath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
                string parentPath = Path.GetFullPath(Path.Combine(fullFilePath, @"..\..\.."));
                DeleteFiles(parentPath);
                foreach (string dir in Directory.GetDirectories(parentPath))
                {
                    if (fullFilePath.StartsWith(dir, StringComparison.Ordinal))
                    {
                        DeleteFiles(dir);
                        foreach (string versiondir in Directory.GetDirectories(dir))
                        {
                            if (!fullFilePath.StartsWith(versiondir, StringComparison.Ordinal))
                            {
                                Directory.Delete(versiondir, true);
                            }
                        }
                    }
                    else
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
        }

        public static void DeleteFiles(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
        }
    }
}
