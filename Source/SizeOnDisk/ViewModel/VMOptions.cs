using SizeOnDisk.Converters;
using SizeOnDisk.Utilities;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

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
                MainWindow window = (MainWindow)this.Owner;
                window.UpdateUILanguage();
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
                LanguageHelper.ChangeLanguage(CultureInfo.GetCultureInfo(value));
                MainWindow window = (MainWindow)this.Owner;
                window.UpdateUILanguage();
            }
        }


        public Dictionary<string, string> Languages
        {
            get
            {
                return LanguageHelper.Cultures;
            }
        }





    }
}
