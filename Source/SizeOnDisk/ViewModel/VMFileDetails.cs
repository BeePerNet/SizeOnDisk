using Microsoft.Win32;
using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;
using WPFByYourCommand;

namespace SizeOnDisk.ViewModel
{
    public class VMFileDetails : ObservableObject
    {
        private string _FileType;
        private FileAttributes _Attributes;

        private DateTime? _CreationTime;
        private DateTime? _LastAccessTime;
        private DateTime? _LastWriteTime;
        VMFile _vmFile;

        public VMFileDetails(VMFile vmFile)
        {
            _vmFile = vmFile;
        }

        public void Load()
        {
            if (_vmFile is VMFolder)
            {
                _FileType = string.Empty;
            }
            else
            {
                _FileType = GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
            }

            LittleFileInfo fileInfo = new LittleFileInfo(_vmFile.Path);
            this._Attributes = fileInfo.Attributes;
            this._CreationTime = fileInfo.CreationTime;
            this._LastAccessTime = fileInfo.LastAccessTime;
            this._LastWriteTime = fileInfo.LastWriteTime;

            this._icon = ShellHelper.GetIcon(_vmFile.Path, 16);
            this._thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
        }

        public FileAttributes Attributes
        {
            get { return _Attributes; }
        }


        public string FileType
        {
            get
            {
                return _FileType;
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

        private static Dictionary<string, string> associations = new Dictionary<string, string>();

        public static string GetFriendlyName(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return string.Empty;
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
                        fileType = extension.Replace(".", "");
                    }
                }
                if (!associations.ContainsKey(extension))
                    // Cache the association so we don't traverse the registry again
                    associations.Add(extension, fileType);
            }

            return associations[extension];
        }





        BitmapSource _icon = null;
        public BitmapSource Icon
        {
            get
            {
                return _icon;
            }
        }


        BitmapSource _thumbnail = null;
        //Seems to have problems with VOB
        public BitmapSource Thumbnail
        {
            get
            {
                return _thumbnail;
            }
        }


    }
}
