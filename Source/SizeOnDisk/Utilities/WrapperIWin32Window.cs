using System;
using System.Security;
using System.Windows;
using System.Windows.Forms;

namespace SizeOnDisk.Utilities
{
    public sealed class WrapperIWin32Window : IWin32Window
    {
        // Fields
        [SecurityCritical]
        private IntPtr _handle;

        // Methods
        [SecurityCritical]
        public WrapperIWin32Window(IntPtr handle)
        {
            this._handle = handle;
        }

        // Methods
        [SecurityCritical]
        public WrapperIWin32Window(Window window)
        {
            this._handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        }

        // Properties
        IntPtr IWin32Window.Handle
        {
            get
            {
                return this._handle;
            }
        }
    }

}
