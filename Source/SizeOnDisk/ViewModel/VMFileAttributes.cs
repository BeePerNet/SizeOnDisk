using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SizeOnDisk.Utilities;
using Microsoft.Win32;

namespace SizeOnDisk.ViewModel
{
    public class VMFileAttributes
    {
        private string _FileType = null;
        private FileAttributes _Attributes;

        private DateTime? _CreationTime;
        private DateTime? _LastAccessTime;
        private DateTime? _LastWriteTime;

        public VMFileAttributes(VMFile vmFile)
        {
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
                GetFriendlyName(System.IO.Path.GetExtension(vmFile.Name));
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
                        fileType = String.Format(Localization.FileTypeUnkown, extension.ToUpper().Replace(".", ""));
                    }
                }

                // Cache the association so we don't traverse the registry again
                associations.Add(extension, fileType);
            }

            return associations[extension];
        }


    }
}
