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
        private class Context
        {
            public string Textblock { get; set; }
            public string Textbox { get; set; }
        }

        private ExceptionBox(string textblock, string textbox)
        {
            InitializeComponent();

            this.DataContext = new Context() { Textblock = textblock, Textbox = textbox };
        }

        public static void ShowException(Exception ex)
        {
            ExceptionBox.ShowException(TextExceptionFormatter.GetInnerException(ex).Message, new TextExceptionFormatter(ex).Format());
        }

        public static void ShowException(string startText, Exception ex)
        {
            ExceptionBox.ShowException(startText, new TextExceptionFormatter(ex).Format());
        }

        public static void ShowException(string textblock, string textbox)
        {
            Application.Current.Dispatcher.BeginInvoke((ThreadStart)delegate
            {
                ExceptionBox window = new ExceptionBox(textblock, textbox);
                if (Application.Current.MainWindow.IsLoaded)
                {
                    window.Owner = Application.Current.MainWindow;
                }
                window.ShowDialog();
            });
        }

    }
}
