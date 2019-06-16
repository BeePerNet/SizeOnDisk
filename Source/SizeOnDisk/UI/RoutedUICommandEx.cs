using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime;
using System.Windows.Input;
using WPFLocalizeExtension.Extensions;

namespace SizeOnDisk.UI
{
    //[TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework, Version=4.0.0.0, PublicKeyToken=31bf3856ad364e35, Custom=null")]
    [TypeConverter("System.Windows.Input.CommandConverter, PresentationFramework")]
    public class RoutedUICommandEx : RoutedUICommand
    {
        // Methods publicRoutedUICommand()    { this._text = string.Empty;    }
        [TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public RoutedUICommandEx(string text, string name, Type ownerType) : this(text, name, ownerType, null) { }

        public RoutedUICommandEx(string text, string name, Type ownerType, InputGestureCollection inputGestures) :
            base(Helper.GetLocalizedValue(text), name, ownerType, inputGestures)
        {
        }
               
    }
}



