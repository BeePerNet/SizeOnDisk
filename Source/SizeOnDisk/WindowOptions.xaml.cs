using SizeOnDisk.ViewModel;
using System.Windows;

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
