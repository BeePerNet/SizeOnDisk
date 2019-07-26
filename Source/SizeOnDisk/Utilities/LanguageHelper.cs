using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using WPFLocalizeExtension.Engine;

namespace SizeOnDisk.Utilities
{
    public static class LanguageHelper
    {
        /// <summary>
        /// List of specific cultures.
        /// It seem that xaml cannot parse a static function with parameters.
        /// To delete if howto was found and xaml fixed.
        /// </summary>
        /// <returns>List of specific cultures</returns>
        public static Dictionary<string, string> Cultures
        {
            get
            {
                Dictionary<string, string> result = CultureInfo.GetCultures(CultureTypes.SpecificCultures).OrderBy(T => T.NativeName).ToDictionary(T => T.Name, T => T.NativeName);
                string[] languagelist = Directory.GetDirectories(Path.GetDirectoryName(Assembly.GetAssembly(typeof(LanguageHelper)).Location));
                List<string> installedlanguages = languagelist.Select(T => Path.GetFileName(T)).ToList();
                installedlanguages.Add("en");
                foreach (string language in installedlanguages)
                {
                    foreach (KeyValuePair<string, string> pair in result.Where(T => T.Key.StartsWith(language, StringComparison.OrdinalIgnoreCase)).ToList())
                    {
                        result[pair.Key] = string.Concat(pair.Value, " *");
                    }
                }
                return result;
            }
        }

        public static void ChangeLanguage(CultureInfo cultureInfo)
        {
            LocalizeDictionary.Instance.Culture = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            foreach (Window window in Application.Current.Windows)
            {
                window.Language = XmlLanguage.GetLanguage(cultureInfo.IetfLanguageTag);
            }
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }


    }
}
