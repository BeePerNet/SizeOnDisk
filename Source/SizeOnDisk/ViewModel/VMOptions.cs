using SizeOnDisk.Converters;
using System.Collections.Generic;
using System.Globalization;
using WPFByYourCommand.Behaviors;

namespace SizeOnDisk.ViewModel
{
    public class VMOptions
    {
        public UISizeFormatType UISizeFormat
        {
            get => Properties.Settings.Default.UISizeFormat;
            set
            {
                Properties.Settings.Default.UISizeFormat = value;
                GlobalizationBehavior.CallUpdate("UISize");
            }
        }

        public string Language
        {
            get => CultureInfo.CurrentCulture.Name;
            set
            {
                Properties.Settings.Default.Language = value;
                GlobalizationBehavior.ChangeLanguage(value);
            }
        }


        public static Dictionary<string, string> Languages => GlobalizationBehavior.Cultures;





    }
}
