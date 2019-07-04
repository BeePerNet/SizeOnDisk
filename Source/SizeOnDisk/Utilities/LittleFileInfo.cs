using SizeOnDisk.Shell;
using System;
using System.IO;
using System.Security;

namespace SizeOnDisk.Utilities
{
    /// <summary>
    /// Smaller FileInfo class like System.IO.FileInfo. 
    /// Bypass the PathTooLongException.
    /// </summary>
    [SecurityCritical]
    public class LittleFileInfo
    {
        IOHelper.WIN32_FILE_ATTRIBUTE_DATA _data;

        string _Filename;

        public LittleFileInfo(string fileName)
        {
            _Filename = string.Concat("\\\\?\\", fileName);
            IOHelper.FillAttributeInfo(fileName, ref _data, false);
        }

        public long Size
        {
            get
            {
                return ((long)_data.fileSizeHigh << 32) + _data.fileSizeLow;
            }
        }

        public long? CompressedSize
        {
            get
            {
                if ((_data.fileAttributes & 2048) == 2048)
                {
                    IOHelper.GetCompressedFileSize(_Filename, ref _data);
                    return ((long)_data.fileSizeHigh << 32) + _data.fileSizeLow;
                }
                return null;
            }
        }

        public FileAttributes Attributes
        {
            get
            {
                return (FileAttributes)_data.fileAttributes;
            }
        }

        public DateTime CreationTime
        {
            get
            {
                return DateTime.FromFileTime(((long)_data.ftCreationTimeHigh << 32) + _data.ftCreationTimeLow);
            }
        }

        public DateTime LastAccessTime
        {
            get
            {
                return DateTime.FromFileTime(((long)_data.ftLastAccessTimeHigh << 32) + _data.ftLastAccessTimeLow);
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return DateTime.FromFileTime(((long)_data.ftLastWriteTimeHigh << 32) + _data.ftLastWriteTimeLow);
            }
        }


    }
}
