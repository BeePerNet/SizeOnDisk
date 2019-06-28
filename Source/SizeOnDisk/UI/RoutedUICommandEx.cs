using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SizeOnDisk.UI
{
    public class RoutedUICommandEx: RoutedUICommand
    {
        /*public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
            "Command", typeof(ICommand), typeof(RoutedUICommandEx), new PropertyMetadata(_CommandPropertyChanged));
        private static void _CommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuItem target = d as MenuItem;
            if (target != null)
            {
                IMenuCommand command = (IMenuCommand)e.NewValue;
                if (command != null)
                {
                    target.Command = command;
                    target.Header = command.Header;
                }
                else
                {
                    target.Command = null;
                    target.Header = null;
                }
            }
        }
        public static void SetCommand(MenuItem target, IMenuCommand command)
        {
            target.SetValue(CommandProperty, command);
        }
        public static IMenuCommand GetCommand(MenuItem target)
        {
            return (IMenuCommand)target.GetValue(CommandProperty);
        }*/







    }
}
