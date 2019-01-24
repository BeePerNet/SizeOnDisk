/*using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SizeOnDisk.UI
{
    public class MenuItem2 : MenuItem
    {
        static MenuItem2()
        {
            ButtonBase.CommandProperty.AddOwner(typeof(MenuItem2),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(MenuItem2.OnCommandChanged)));

            MenuItem.IconProperty.OverrideMetadata(typeof(MenuItem2),
                new FrameworkPropertyMetadata(null,
                    new CoerceValueCallback(MenuItem2.CoerceIcon)));

        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MenuItem2)d).OnCommandChanged();
        }

        private void OnCommandChanged()
        {
            base.CoerceValue(MenuItem.IconProperty);
        }

        private static object CoerceIcon(DependencyObject d, object value)
        {
            MenuItem2 container = (MenuItem2)d;
            if (value == null)
            {
                RoutedUICommand2 commandEx = container.Command as RoutedUICommand2;
                if (commandEx != null)
                {
                    if (commandEx.Image != null)
                    {
                        AutoDisablingImage img = new AutoDisablingImage();
                        img.Source = commandEx.Image;
                        return img;
                    }
                }
            }
            return value;
        }

    
    }
}
*/