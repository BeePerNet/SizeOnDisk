using SizeOnDisk.Shell;
using System;
using System.IO;
using System.Security;

namespace SizeOnDisk.Shell
{
    /// <summary>
    /// Smaller FileInfo class like System.IO.FileInfo. 
    /// Bypass the PathTooLongException.
    /// </summary>
    [SecurityCritical]
    public class LittleFileInfo
    {
        long? _CompressedSize = null;
        IOHelper.WIN32_FILE_ATTRIBUTE_DATA _data;

        string _Filename;
        string _Path;

        internal LittleFileInfo(string path, string filename)
        {
            _Filename = Filename;
            _Path = path;
            string fullfilename = filename;
            if (string.IsNullOrEmpty(path))
                _Path = filename;
            else
                fullfilename = System.IO.Path.Combine(path, filename);
            fullfilename = string.Concat("\\\\?\\", fullfilename);
            IOHelper.FillAttributeInfo(fullfilename, ref _data);
            if ((this.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
                if ((this.Attributes & FileAttributes.Directory) == 0)
                {
                    _CompressedSize = IOHelper.GetCompressedFileSize(fullfilename);
                }
        }

        internal LittleFileInfo(string path, string filename, IOHelper.WIN32_FILE_ATTRIBUTE_DATA data)
        {
            _Filename = filename;
            _Path = path;
            _data = data;
            if ((this.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
                if ((this.Attributes & FileAttributes.Directory) == 0)
                {
                    string fullfilename = string.Concat("\\\\?\\", System.IO.Path.Combine(path, filename));
                    _CompressedSize = IOHelper.GetCompressedFileSize(fullfilename);
                }
        }

        public bool IsFolder
        {
            get
            {
                return (_data.fileAttributes & (int)FileAttributes.Directory) > 0;
            }
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
                return _CompressedSize;
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

        public string Filename { get => _Filename; }
        public string Path { get => _Path; }
    }
}
