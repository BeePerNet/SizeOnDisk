using Microsoft.Win32;
using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SizeOnDisk.ViewModel
{
    public class VMFileAttributes
    {
        private readonly string _FileType;
        private readonly FileAttributes _Attributes;

        private DateTime? _CreationTime;
        private DateTime? _LastAccessTime;
        private DateTime? _LastWriteTime;
        VMFile _vmFile;

        public VMFileAttributes(VMFile vmFile)
        {
            _vmFile = vmFile;
            LittleFileInfo fileInfo = new LittleFileInfo(vmFile.Path);
            this._Attributes = fileInfo.Attributes;
            this._CreationTime = fileInfo.CreationTime;
            this._LastAccessTime = fileInfo.LastAccessTime;
            this._LastWriteTime = fileInfo.LastWriteTime;

            if (vmFile is VMFolder)
            {
                _FileType = string.Empty;
            }
            else
            {
                _FileType = GetFriendlyName(System.IO.Path.GetExtension(vmFile.Name));
            }
        }


        public string FileType
        {
            get
            {
                return _FileType;
            }
        }

        public FileAttributes Attributes
        {
            get
            {
                return _Attributes;
            }
        }


        public DateTime? CreationTime
        {
            get
            {
                return _CreationTime;
            }
        }
        public DateTime? LastAccessTime
        {
            get
            {
                return _LastAccessTime;
            }
        }
        public DateTime? LastWriteTime
        {
            get
            {
                return _LastWriteTime;
            }
        }

        public bool IsHidden
        {
            get
            {
                return _Attributes.HasFlag(FileAttributes.Hidden);
            }
        }

        private static Dictionary<string, string> associations = new Dictionary<string, string>();

        public static string GetFriendlyName(string extension)
        {
            if (!associations.ContainsKey(extension))
            {
                string fileType = String.Empty;

                using (RegistryKey rk = Registry.ClassesRoot.OpenSubKey("\\" + extension))
                {
                    if (rk != null)
                    {
                        string applicationType = rk.GetValue("", String.Empty).ToString();

                        if (!string.IsNullOrEmpty(applicationType))
                        {
                            using (RegistryKey appTypeKey = Registry.ClassesRoot.OpenSubKey("\\" + applicationType))
                            {
                                if (appTypeKey != null)
                                {
                                    fileType = appTypeKey.GetValue("", String.Empty).ToString();
                                }
                            }
                        }
                    }

                    // Couldn't find the file type in the registry. Display some default.
                    if (string.IsNullOrEmpty(fileType))
                    {
                        fileType = String.Format(CultureInfo.CurrentCulture, Localization.FileTypeUnkown, extension.Replace(".", ""));
                    }
                }

                // Cache the association so we don't traverse the registry again
                associations.Add(extension, fileType);
            }

            return associations[extension];
        }


        public BitmapSource Icon
        {
            get
            {
                // Create a native shellitem from our path
                Guid guid = new Guid(ShellHelper.ShellIIDGuid.IShellItem);
                int retCode = ShellHelper.SHCreateItemFromParsingName(this._vmFile.Path, IntPtr.Zero, ref guid, out ShellHelper.IShellItem nativeShellItem);

                if (retCode < 0)
                {
                    throw new Exception("ShellObjectFactoryUnableToCreateItem", Marshal.GetExceptionForHR(retCode));
                }

                ShellHelper.Size nativeSIZE = new ShellHelper.Size();
                nativeSIZE.Width = Convert.ToInt32(16);
                nativeSIZE.Height = Convert.ToInt32(16);

                int hr = ((ShellHelper.IShellItemImageFactory)nativeShellItem).GetImage(nativeSIZE, SIIGBF.IconOnly, out IntPtr hBitmap);
                if (hr != (int)HResult.Ok)
                    throw new ExternalException("HResult Exception", (int)hr);


                // return a System.Media.Imaging.BitmapSource
                // Use interop to create a BitmapSource from hBitmap.
                BitmapSource returnValue = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                // delete HBitmap to avoid memory leaks
                ShellHelper.DeleteObject(hBitmap);

                returnValue.Freeze();

                return returnValue;
            }
        }


    }
}
