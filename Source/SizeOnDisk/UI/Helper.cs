using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Extensions;
using WPFLocalizeExtension.Providers;

namespace SizeOnDisk.UI
{
    public static class Helper
    {
        /// <summary>
        /// Find a specific parent object type in the visual tree
        /// </summary>
        public static T FindParentControl<T>(DependencyObject outerDepObj) where T : DependencyObject
        {
            while ((outerDepObj = VisualTreeHelper.GetParent(outerDepObj)) != null)
            {
                if (outerDepObj is T)
                    return outerDepObj as T;
            }

            return null;
        }

        public static readonly string AssemblyName = Assembly.GetCallingAssembly().GetName().Name;

        public static string GetLocalizedValue(string key)
        {
            //text.StartsWith("Loc:", true, CultureInfo.CurrentUICulture) ? Helper.GetLocalizedValue<string>(text.Remove(0, 4))
            //bool test = LocalizeDictionary.Instance.ResourceKeyExists(Assembly.GetCallingAssembly().GetName().Name, "Localization", key);
            //LocalizeDictionary.Instance.GetLocalizedObject<string>()
            if (LocalizeDictionary.Instance.ResourceKeyExists(AssemblyName, "Localization", key, CultureInfo.CurrentCulture))
                return LocalizeDictionary.Instance.GetLocalizedObject(AssemblyName, "Localization", key, CultureInfo.CurrentCulture).ToString();
            return key;
        }
    }
}
