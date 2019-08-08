using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using SizeOnDisk.ViewModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Input;
using WPFByYourCommand.Commands;

namespace SizeOnDisk.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [SuppressMessage("Design", "CA1501")]
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RunAsAdmin.Visibility = (UserAccessControlHelper.SupportUserAccessControl ? Visibility.Visible : Visibility.Collapsed);
            RunAsAdmin.IsEnabled = !UserAccessControlHelper.IsProcessElevated;
        }


        private Legend _Legend;

        private void Selector_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            VMFolder folder = CommandViewModel.GetViewModelObject<VMFolder>(sender);
            if (folder != null && !folder.IsProtected)
            {
                folder.IsTreeSelected = true;
                e.Handled = true;
            }
        }


        private void RunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            ShellHelper.Restart(true);
        }

        private void ButtonLegend_Click(object sender, RoutedEventArgs e)
        {
            if (_Legend == null || !_Legend.IsVisible)
            {
                _Legend = new Legend
                {
                    Owner = this
                };
                _Legend.Show();
            }
            else
            {
                _Legend.Close();
                _Legend = null;
            }
        }

        private void ButtonOptions_Click(object sender, RoutedEventArgs e)
        {
            new WindowOptions(this).Show();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            new AboutBox(this).ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (DataContext as VMRootHierarchy).StopAllAsync();
            Application.Current.Shutdown();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new ErrorList(this, (DataContext as VMRootHierarchy).SelectedRootFolder).Show();
        }
    }
}
