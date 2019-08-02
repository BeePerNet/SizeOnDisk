using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows;

namespace SizeOnDisk.Utilities
{
    /// <summary>
    /// Interaction logic for ExceptionBox.xaml
    /// </summary>
    [SuppressMessage("Design", "CA1501")]
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

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<En attente>")]
        public static void ShowException(Exception ex)
        {
            ExceptionBox.ShowException(TextExceptionFormatter.GetInnerException(ex).Message, new TextExceptionFormatter(ex).Format());
        }

        public static void ShowException(string startText, Exception ex)
        {
            ExceptionBox.ShowException(startText, new TextExceptionFormatter(ex).Format());
        }

        private static void InternalShowException(Window owner, string textblock, string textbox)
        {
            try
            {
                ExceptionBox window = new ExceptionBox(textblock, textbox)
                {
                    Owner = owner
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                Trace.Write(new TextExceptionFormatter(ex).Format());
            }
        }

        public static void ShowException(string textblock, string textbox)
        {
            if (Application.Current == null)
                InternalShowException(null, textblock, textbox);
            else if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                InternalShowException(Application.Current.MainWindow, textblock, textbox);
            else
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InternalShowException(Application.Current.MainWindow, textblock, textbox);
                }));
        }

    }
}
