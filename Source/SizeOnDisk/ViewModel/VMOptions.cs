using SizeOnDisk.Converters;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using WPFByYourCommand.Behaviors;

namespace SizeOnDisk.ViewModel
{
    public class VMOptions
    {
        public Window Owner { get; set; }

        public UISizeFormatType UISizeFormat
        {
            get
            {
                return Properties.Settings.Default.UISizeFormat;
            }
            set
            {
                Properties.Settings.Default.UISizeFormat = value;
                GlobalizationBehavior.CallUpdate("UISize");
            }
        }

        public string Language
        {
            get
            {
                return CultureInfo.CurrentCulture.Name;
            }
            set
            {
                Properties.Settings.Default.Language = value;
                GlobalizationBehavior.ChangeLanguage(value);
            }
        }


        public static Dictionary<string, string> Languages
        {
            get
            {
                return GlobalizationBehavior.Cultures;
            }
        }





    }
}
