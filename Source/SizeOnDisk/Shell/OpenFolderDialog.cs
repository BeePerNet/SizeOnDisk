using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
            {
                return ShowVistaDialog(owner);
            }
            else
            {
                return ShowLegacyDialog(owner);
            }
        }

        private bool ShowVistaDialog(IWin32Window owner)
        {
            var frm = (ShellHelper.IFileDialog)(new ShellHelper.FileOpenDialogRCW());
            FileOpenOptions options;
            frm.GetOptions(out options);
            options |= FileOpenOptions.PickFolders | FileOpenOptions.ForceFilesystem | FileOpenOptions.NoValidate | FileOpenOptions.NoTestFileCreate | FileOpenOptions.DontAddToRecent;
            frm.SetOptions(options);
            if (this.InitialFolder != null)
            {
                ShellHelper.IShellItem directoryShellItem;
                var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                if (ShellHelper.SHCreateItemFromParsingName(this.InitialFolder, IntPtr.Zero, ref riid, out directoryShellItem) == (int)HResult.Ok)
                {
                    frm.SetFolder(directoryShellItem);
                }
            }
            if (this.DefaultFolder != null)
            {
                ShellHelper.IShellItem directoryShellItem;
                var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"); //IShellItem
                if (ShellHelper.SHCreateItemFromParsingName(this.DefaultFolder, IntPtr.Zero, ref riid, out directoryShellItem) == (int)HResult.Ok)
                {
                    frm.SetDefaultFolder(directoryShellItem);
                }
            }

            if (frm.Show(owner.Handle) == (int)HResult.Ok)
            {
                ShellHelper.IShellItem shellItem;
                if (frm.GetResult(out shellItem) == (int)HResult.Ok)
                {
                    IntPtr pszString;
                    if (shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out pszString) == (int)HResult.Ok)
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
                frm.Title = "Select Folder";
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
