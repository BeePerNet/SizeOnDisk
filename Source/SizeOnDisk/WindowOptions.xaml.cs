using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SizeOnDisk.Converters;
using SizeOnDisk.UI;
using SizeOnDisk.Utilities;
using SizeOnDisk.ViewModel;

namespace SizeOnDisk
{
    /// <summary>
    /// Interaction logic for WindowOptions.xaml
    /// </summary>
    public partial class WindowOptions : Window
    {
        VMOptions options;
        public WindowOptions(Window owner)
        {
            InitializeComponent();
            options = (VMOptions)this.DataContext;
            options.Owner = owner;
        }

    }
}
