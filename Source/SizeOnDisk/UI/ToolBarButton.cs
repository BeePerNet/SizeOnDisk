/*using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SizeOnDisk.UI
{
    public class ToolBarButton: Button
    {
        static ToolBarButton()
        {
            ButtonBase.CommandProperty.AddOwner(typeof(ToolBarButton), 
                new FrameworkPropertyMetadata(null, 
                    new PropertyChangedCallback(ToolBarButton.OnCommandChanged)));

            ContentControl.ContentProperty.OverrideMetadata(typeof(ToolBarButton), 
                new FrameworkPropertyMetadata(null, 
                    new CoerceValueCallback(ToolBarButton.CoerceContent)));

            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarButton), new FrameworkPropertyMetadata(ToolBar.ButtonStyleKey));
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ToolBarButton)d).OnCommandChanged();
        }

        private void OnCommandChanged()
        {
            base.CoerceValue(ContentControl.ContentProperty);
        }

        private static object CoerceContent(DependencyObject d, object value)
        {
            ToolBarButton container = (ToolBarButton)d;
            if (value == null)
            {
                RoutedUICommand2 commandEx = container.Command as RoutedUICommand2;
                if (commandEx != null)
                {
                    if (commandEx.Image != null)
                    {
                        AutoDisablingImage img = new AutoDisablingImage();
                        img.Source=commandEx.Image;
                        return img;
                    }
                }
                RoutedUICommand command = container.Command as RoutedUICommand;
                if (command != null)
                {
                    return command.Text;
                }
                return value;
            }
            return value;
        }

 




        
    }
}*/
