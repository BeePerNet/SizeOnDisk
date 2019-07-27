using SizeOnDisk.ViewModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace SizeOnDisk
{
    /// <summary>
    /// Interaction logic for WindowOptions.xaml
    /// </summary>
    [SuppressMessage("Design", "CA1501")]
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
