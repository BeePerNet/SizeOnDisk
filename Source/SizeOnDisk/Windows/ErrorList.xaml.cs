using SizeOnDisk.ViewModel;
using System.Windows;
using System.Windows.Input;
using WPFByYourCommand.Commands;
using WPFByYourCommand.Exceptions;

namespace SizeOnDisk.Windows
{
    /// <summary>
    /// Logique d'interaction pour ErrorList.xaml
    /// </summary>
    public partial class ErrorList : Window
    {
        public ErrorList(VMRootFolder folder)
        {
            InitializeComponent();
            DataContext = folder;
        }

        private void Path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                e.Handled = true;
                VMFile.SelectCommand.Execute(null, sender as IInputElement);
            }
        }

        private void Exception_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ExceptionButton_Click(sender, e);
            }
        }

        private void ExceptionButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            VMLog log = CommandViewModel.GetViewModelObject<VMLog>(e.OriginalSource);
            ExceptionBox.ShowException(log.ShortText, log.LongText, log.File.Path, this);
        }
    }
}
