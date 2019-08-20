using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SizeOnDisk.Themes
{
    public partial class TextBlock : ResourceDictionary
    {
        public void CallCopyCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (sender is ContextMenu ctx && ctx.PlacementTarget is System.Windows.Controls.TextBlock tb)
            {
                Clipboard.SetText(tb.Text);
            }
            else
            {
                throw new ArgumentNullException(nameof(sender));
            }
        }

    }
}
