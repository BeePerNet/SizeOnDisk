using SizeOnDisk.Converters;
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
                Properties.Settings.Default.Save();
                GlobalizationBehavior.CallUpdate("UISize");
            }
        }

        public string Language
        {
            get => CultureInfo.CurrentCulture.Name;
            set
            {
                Properties.Settings.Default.Language = value;
                Properties.Settings.Default.Save();
                GlobalizationBehavior.ChangeLanguage(value);
            }
        }

    }
}
