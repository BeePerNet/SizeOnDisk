using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SizeOnDisk.Converters;
using SizeOnDisk.UI;
using SizeOnDisk.Utilities;

namespace SizeOnDisk
{
    /// <summary>
    /// Interaction logic for WindowOptions.xaml
    /// </summary>
    public partial class WindowOptions : Window
    {

        public WindowOptions(Window owner)
        {
            InitializeComponent();
            this.Owner = owner;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.LanguageSelector.SelectedValue = CultureInfo.CurrentCulture.Name;
            this.SizeFormatSelector.SelectedValue = ((UISizeFormatType)Properties.Settings.Default.UISizeFormat);
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded && e.AddedItems.Count > 0)
            {
                KeyValuePair<string, string> value = (KeyValuePair<string, string>)e.AddedItems[0];
                Properties.Settings.Default.Language = value.Key;
                LanguageHelper.ChangeLanguage(CultureInfo.GetCultureInfo(value.Key));
                MainWindow window = (MainWindow)this.Owner;
                window.UpdateUILanguage();
            }
        }

        private void SizeFormatSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded && e.AddedItems.Count > 0)
            {
                EnumValue value = e.AddedItems[0] as EnumValue;
                if (value != null)
                {
                    Properties.Settings.Default.UISizeFormat = (byte)(UISizeFormatType)value.Value;
                    MainWindow window = (MainWindow)this.Owner;
                    window.UpdateUILanguage();
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Language = this.LanguageSelector.SelectedValue.ToString();
            Properties.Settings.Default.UISizeFormat = (int)this.SizeFormatSelector.SelectedValue;
            Properties.Settings.Default.Save();
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

    }
}
