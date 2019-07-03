using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using SizeOnDisk.Utilities;
using SizeOnDisk.ViewModel;
using System.IO;
using SizeOnDisk.Shell;

namespace SizeOnDisk
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Language))
            {
                LanguageHelper.ChangeLanguage(new CultureInfo(Properties.Settings.Default.Language));
            }
            else
            {
                LanguageHelper.ChangeLanguage(CultureInfo.CurrentCulture);
            }
            this.RunAsAdmin.Visibility = (UserAccessControlHelper.SupportUserAccessControl ? Visibility.Visible : Visibility.Collapsed);
            this.RunAsAdmin.IsEnabled = !UserAccessControlHelper.IsProcessElevated;
            _RootHierarchy = this.DataContext as VMRootHierarchy;

            

            //CanExecuteRoutedEventHandler _handler = new CanExecuteRoutedEventHandler(OnCanExecuteRoutedEventHandler);

            //EventManager.RegisterClassHandler(typeof(DataGrid), CommandManager.CanExecuteEvent, _handler);

            //EventManager.RegisterClassHandler(typeof(DataGrid), CommandManager.RemoveCanExecuteHandler, DataGrid.
        }

        /*void OnCanExecuteRoutedEventHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            RoutedCommand routedCommand = (e.Command as RoutedCommand);

            if (routedCommand != null)
            {
                if (routedCommand.Name == "Delete")
                {
                    e.CanExecute = false;
                    e.Handled = false;
                }
            }

        }*/


        private VMRootHierarchy _RootHierarchy;
        private Legend _Legend;

        private void _Datagrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid datagrid = sender as DataGrid;
            if (datagrid.SelectedItem != null && datagrid.SelectedItem is VMFolder)
            {
                VMFolder folder = (datagrid.SelectedItem as VMFolder);
                folder.IsSelected = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// When selection changed, scroll to the first item of the list
        /// </summary>
        private void _TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(this.Listing.ItemsSource);
            IEditableCollectionView collection = view as IEditableCollectionView;

            if (collection != null && collection.IsEditingItem)
            {
                collection.CancelEdit();
            }
            if (view != null)
            {
                view.MoveCurrentToFirst();
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

        public void UpdateUILanguage()
        {
            if (this.Listing.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(this.Listing.Items).Refresh();
            }
            BindingExpression binding;
            foreach (StatusBarItem item in StatusBar.Items.OfType<StatusBarItem>())
            {
                binding = BindingOperations.GetBindingExpression(item, StatusBarItem.ContentProperty);
                if (binding != null)
                {
                    binding.UpdateTarget();
                }
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_RootHierarchy != null)
            {
                _RootHierarchy.StopAsync();
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutBox aboutBox = new AboutBox(this);
            aboutBox.ShowDialog();
        }

        private void CommandBinding_DeleteCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Listing.SelectedItem != null && !(Listing.SelectedItem is VMRootFolder))
            {
                e.CanExecute = true;
            }
            e.Handled = true;
        }

        private void CommandBinding_DeleteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (Listing.SelectedItems != null && Listing.SelectedItems.Count > 0)
            {
                VMFile[] vmfiles = Listing.SelectedItems.OfType<VMFile>().ToArray();
                VMFolder parent = vmfiles.First().Parent;
                string[] files = vmfiles.Select(T => T.Path).ToArray();
                
                if (IOHelper.SafeNativeMethods.MoveToRecycleBin(files))
                {
                    foreach (VMFile vmfile in vmfiles)
                    {
                        if (!File.Exists(vmfile.Path) && !Directory.Exists(vmfile.Path))
                        {
                            parent.RemoveChild(vmfile);
                        }
                    }
                    parent.RefreshCount();
                    parent.RefreshParents();
                }
            }
            e.Handled = true;
        }

        /*private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = Helper.FindParentControl<TreeViewItem>(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }*/
    }
}
