using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace SizeOnDisk.Utilities
{
    /// <summary>
    /// Interaction logic for ExceptionBox.xaml
    /// </summary>
    public partial class ExceptionBox : Window
    {
        private ExceptionBox(string text)
        {
            InitializeComponent();

            this.DataContext = text;
        }

        public static void ShowException(Exception ex)
        {
            ExceptionBox.ShowException(new TextExceptionFormatter(ex).Format());
        }

        public static void ShowException(string startText, Exception ex)
        {
            ExceptionBox.ShowException(string.Format(CultureInfo.CurrentCulture, "{0}{1}{2}", startText, Environment.NewLine, new TextExceptionFormatter(ex).Format()));
        }

        private static void ShowException(string text)
        {
            Application.Current.Dispatcher.BeginInvoke((ThreadStart)delegate
            {
                ExceptionBox window = new ExceptionBox(text);
                if (Application.Current.MainWindow.IsLoaded)
                {
                    window.Owner = Application.Current.MainWindow;
                }
                window.ShowDialog();
            });
        }

    }
}
