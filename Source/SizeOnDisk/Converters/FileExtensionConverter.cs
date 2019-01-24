using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using SizeOnDisk.Utilities;
using Microsoft.Win32;
using System.Windows;

namespace SizeOnDisk.Converters
{
    public class FileExtensionConverter : IMultiValueConverter
    {
        private static Dictionary<string, string> associations = new Dictionary<string, string>();

        public static string GetFriendlyName(string extension)
        {
            if (!associations.ContainsKey(extension))
            {
                string fileType = String.Empty;

                using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey("\\" + extension))
                {
                    if (rk != null)
                    {
                        string applicationType = rk.GetValue("", String.Empty).ToString();

                        if (!string.IsNullOrEmpty(applicationType))
                        {
                            using (RegistryKey appTypeKey = Registry.ClassesRoot.OpenSubKey("\\" + applicationType))
                            {
                                fileType = appTypeKey.GetValue("", String.Empty).ToString();
                            }
                        }
                    }

                    // Couldn't find the file type in the registry. Display some default.
                    if (string.IsNullOrEmpty(fileType))
                    {
                        fileType = String.Format(Localization.FileTypeUnkown, extension.ToUpper().Replace(".", ""));
                    }
                }

                // Cache the association so we don't traverse the registry again
                associations.Add(extension, fileType);
            }

            return associations[extension];
        }
        
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            if (value.Length != 2)
                throw new ArgumentOutOfRangeException("value");

            string extension = (string)value[0];
            bool isFile = (value[1] != DependencyProperty.UnsetValue && ((bool)value[1]));

            if (!isFile)
                return Localization.FileTypeFolder;

            if (targetType != typeof(string))
                throw new ArgumentException("targetType must be of type string.");

            return GetFriendlyName(extension);
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
