/*using System;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace SizeOnDisk.UI
{
    public class RoutedUICommand2 : RoutedUICommand
    {
        private ImageSource _image = null;

        public RoutedUICommand2()
            : base()
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType) :
            this(text, name, ownerType, string.Empty, Key.None, ModifierKeys.None)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, Key key) :
            this(text, name, ownerType, string.Empty, key, ModifierKeys.None)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, Key key, ModifierKeys modifierKeys) :
            this(text, name, ownerType, string.Empty, key, modifierKeys)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, string imageResourceUri) :
            this(text, name, ownerType, imageResourceUri, Key.None, ModifierKeys.None)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, string imageResourceUri, Key key) :
            this(text, name, ownerType, imageResourceUri, key, ModifierKeys.None)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, Uri imageResourceUri) :
            this(text, name, ownerType, imageResourceUri.ToString() ?? string.Empty, Key.None, ModifierKeys.None)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, Uri imageResourceUri, Key key) :
            this(text, name, ownerType, imageResourceUri.ToString() ?? string.Empty, key, ModifierKeys.None)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, Uri imageResourceUri, Key key, ModifierKeys modifierKeys) :
            this(text, name, ownerType, imageResourceUri.ToString() ?? string.Empty, key, modifierKeys)
        {
        }

        public RoutedUICommand2(string text, string name, Type ownerType, string imageResourceUri, Key key, ModifierKeys modifierKeys)
            : base(text, name, ownerType)
        {
            if (!string.IsNullOrWhiteSpace(imageResourceUri))
            {
                _image = new BitmapImage(new Uri(string.Format(CultureInfo.InvariantCulture, "pack://application:,,,{0}", imageResourceUri)));
            }
            if (key != Key.None)
            {
                InputGestures.Add(new KeyGesture(key, modifierKeys));
            }
        }

        public ImageSource Image
        {
            get { return _image; }
            set { _image = value; }
        }



    }
}
*/