using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using SizeOnDisk.ViewModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WPFByYourCommand;
using WPFByYourCommand.Commands;
using WPFLocalizeExtension.Engine;

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
            this.RunAsAdmin.Visibility = (UserAccessControlHelper.SupportUserAccessControl ? Visibility.Visible : Visibility.Collapsed);
            this.RunAsAdmin.IsEnabled = !UserAccessControlHelper.IsProcessElevated;
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
            WindowOptions options = new WindowOptions(this)
            {
                Owner = this
            };
            options.ShowDialog();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutBox aboutBox = new AboutBox(this);
            aboutBox.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            (this.DataContext as VMRootHierarchy).StopAllAsync();
            Application.Current.Shutdown();
        }
    }
}
