﻿using SizeOnDisk.Shell;
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

            //System.Windows.Media.Imaging.BitmapSource Thumbnail = ShellHelper.GetIcon(@"P:\Sébastien\Mes images\BSG-75.jpg", 96, true);

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.Language))
            {
                LanguagesHelper.ChangeLanguage(new CultureInfo(Properties.Settings.Default.Language));
            }
            else
            {
                LanguagesHelper.ChangeLanguage(CultureInfo.CurrentCulture);
            }
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

        public void UpdateUILanguage()
        {
            //TODO: Test and reactivate elsewhere
            /*if (this.Listing.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(this.Listing.Items).Refresh();
            }*/
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