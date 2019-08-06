using SizeOnDisk.ViewModel;
using System.Windows;
using System.Windows.Controls;
using WPFByYourCommand.Exceptions;

namespace SizeOnDisk.Windows
{
    /// <summary>
    /// Logique d'interaction pour ErrorList.xaml
    /// </summary>
    public partial class ErrorList : Window
    {
        public ErrorList(Window owner, VMRootFolder folder)
        {
            InitializeComponent();
            this.Owner = owner;
            this.DataContext = folder;
        }

        private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow;
            VMLog log = row.DataContext as VMLog;
            ExceptionBox.ShowException(log.ShortText, log.LongText, this);
        }
    }
}
