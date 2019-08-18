using System;
using System.Windows;
using System.Windows.Forms;

namespace SizeOnDisk.Utilities
{
    public sealed class WrapperIWin32Window : IWin32Window
    {
        // Fields
        private readonly IntPtr _handle;

        // Methods
        public WrapperIWin32Window(IntPtr handle)
        {
            _handle = handle;
        }

        // Methods
        public WrapperIWin32Window(Window window)
        {
            _handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        }

        // Properties
        IntPtr IWin32Window.Handle => _handle;
    }

}
