using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WPFLocalizeExtension.Extensions;

namespace SizeOnDisk.Shell
{
    //Copyright (c) 2011 Josip Medved <jmedved@jmedved.com>  http://www.jmedved.com

    internal class OpenFolderDialog
    {
        /// <summary>
        /// Gets/sets folder in which dialog will be open.
        /// </summary>
        public string InitialFolder { get; set; }

        /// <summary>
        /// Gets/sets directory in which dialog will be open if there is no recent directory available.
        /// </summary>
        public string DefaultFolder { get; set; }

        /// <summary>
        /// Gets selected folder.
        /// </summary>
        public string Folder { get; private set; }


        internal bool ShowDialog(IWin32Window owner)
        {
            if (Environment.OSVersion.Version.Major >= 6)
                return ShowVistaDialog(owner);
            return ShowLegacyDialog(owner);
        }

        private bool ShowVistaDialog(IWin32Window owner)
        {
            var frm = (ShellHelper.SafeNativeMethods.IFileDialog)(new ShellHelper.SafeNativeMethods.FileOpenDialogRCW());
            frm.GetOptions(out ShellHelper.SafeNativeMethods.FileOpenOptions options);
            options |= ShellHelper.SafeNativeMethods.FileOpenOptions.PickFolders | ShellHelper.SafeNativeMethods.FileOpenOptions.ForceFilesystem | ShellHelper.SafeNativeMethods.FileOpenOptions.NoValidate | ShellHelper.SafeNativeMethods.FileOpenOptions.NoTestFileCreate | ShellHelper.SafeNativeMethods.FileOpenOptions.DontAddToRecent;
            frm.SetOptions(options);
            if (this.InitialFolder != null)
            {
                Guid riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                if (ShellHelper.SafeNativeMethods.SHCreateItemFromParsingName(this.InitialFolder, IntPtr.Zero, ref riid, out ShellHelper.SafeNativeMethods.IShellItem directoryShellItem) == (int)ShellHelper.SafeNativeMethods.HResult.Ok)
                {
                    frm.SetFolder(directoryShellItem);
                }
            }
            if (this.DefaultFolder != null)
            {
                Guid riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                if (ShellHelper.SafeNativeMethods.SHCreateItemFromParsingName(this.DefaultFolder, IntPtr.Zero, ref riid, out ShellHelper.SafeNativeMethods.IShellItem directoryShellItem) == (int)ShellHelper.SafeNativeMethods.HResult.Ok)
                {
                    frm.SetDefaultFolder(directoryShellItem);
                }
            }

            if (frm.Show(owner.Handle) == (int)ShellHelper.SafeNativeMethods.HResult.Ok)
            {
                if (frm.GetResult(out ShellHelper.SafeNativeMethods.IShellItem shellItem) == (int)ShellHelper.SafeNativeMethods.HResult.Ok)
                {
                    if (shellItem.GetDisplayName(ShellHelper.SafeNativeMethods.SIGDN.SIGDN_FILESYSPATH, out IntPtr pszString) == (int)ShellHelper.SafeNativeMethods.HResult.Ok)
                    {
                        if (pszString != IntPtr.Zero)
                        {
                            try
                            {
                                this.Folder = Marshal.PtrToStringAuto(pszString);
                                return true;
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(pszString);
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool ShowLegacyDialog(IWin32Window owner)
        {
            using (var frm = new OpenFileDialog())
            {
                frm.CheckFileExists = false;
                frm.CheckPathExists = true;
                //frm.CreatePrompt = false;
                //frm.Filter = "|" + Guid.Empty.ToString();
                //frm.FileName = "any";
                if (this.InitialFolder != null) { frm.InitialDirectory = this.InitialFolder; }
                //frm.OverwritePrompt = false;
                frm.Title = LocExtension.GetLocalizedValue<string>("ChooseFolder");
                frm.ValidateNames = false;
                if (frm.ShowDialog(owner) == DialogResult.OK)
                {
                    this.Folder = Path.GetDirectoryName(frm.FileName);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }


}
