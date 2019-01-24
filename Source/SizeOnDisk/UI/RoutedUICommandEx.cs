using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime;
using System.Windows.Input;

namespace SizeOnDisk.UI
{
    //[TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework, Version=4.0.0.0, PublicKeyToken=31bf3856ad364e35, Custom=null")]
    [TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework")]
    public class RoutedUICommandEx : RoutedUICommand
    {
        //private ImageSource _image = null;

        private string _text;

        // Methods publicRoutedUICommand()    { this._text = string.Empty;    }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public RoutedUICommandEx(string text, string name, Type ownerType) : this(text, name, ownerType, null) { }

        public RoutedUICommandEx(string text, string name, Type ownerType, InputGestureCollection inputGestures) :
            base(text, name, ownerType, inputGestures)
        {
            if (text.StartsWith("LocText:", true, CultureInfo.CurrentUICulture))
            {
                this._text = text.Remove(0, 8);
            }
        }

        private string GetText()
        {
            return null;
        }



        // Properties 
        public new string Text
        {
            get
            {
                if (this._text == null)
                {
                    return base.Text;
                }
                else
                {
                    return this.GetText();
                }
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (value.StartsWith("LocText:", true, CultureInfo.CurrentUICulture))
                {
                    this._text = value.Remove(0, 8);
                }
                else
                {
                    base.Text = value;
                }
            }
        }



        /*public RoutedUICommandEx(string text, string name, Type ownerType) :
            this(text, name, ownerType, string.Empty, Key.None, ModifierKeys.None)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, Key key) :
            this(text, name, ownerType, string.Empty, key, ModifierKeys.None)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, Key key, ModifierKeys modifierKeys) :
            this(text, name, ownerType, string.Empty, key, modifierKeys)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, string imageResourceUri) :
            this(text, name, ownerType, imageResourceUri, Key.None, ModifierKeys.None)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, string imageResourceUri, Key key) :
            this(text, name, ownerType, imageResourceUri, key, ModifierKeys.None)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, Uri imageResourceUri) :
            this(text, name, ownerType, imageResourceUri.ToString() ?? string.Empty, Key.None, ModifierKeys.None)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, Uri imageResourceUri, Key key) :
            this(text, name, ownerType, imageResourceUri.ToString() ?? string.Empty, key, ModifierKeys.None)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, Uri imageResourceUri, Key key, ModifierKeys modifierKeys) :
            this(text, name, ownerType, imageResourceUri.ToString() ?? string.Empty, key, modifierKeys)
        {
        }

        public RoutedUICommandEx(string text, string name, Type ownerType, string imageResourceUri, Key key, ModifierKeys modifierKeys)
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
        }*/



    }
}



