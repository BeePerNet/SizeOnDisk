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

    }
}
