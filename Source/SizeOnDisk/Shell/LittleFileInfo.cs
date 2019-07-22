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
        private IOHelper.WIN32_FILE_ATTRIBUTE_DATA _data;

        internal LittleFileInfo(string path, string filename)
        {
            FileName = FileName;
            Path = path;
            string fullfilename = filename;
            if (string.IsNullOrEmpty(path))
                Path = filename;
            else
                fullfilename = System.IO.Path.Combine(path, filename);
            fullfilename = string.Concat("\\\\?\\", fullfilename);
            IOHelper.FillAttributeInfo(fullfilename, ref _data);
            if ((this.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
                if ((this.Attributes & FileAttributes.Directory) == 0)
                    CompressedSize = IOHelper.GetCompressedFileSize(fullfilename);
        }

        internal LittleFileInfo(string path, string filename, IOHelper.WIN32_FILE_ATTRIBUTE_DATA data)
        {
            FileName = filename;
            Path = path;
            _data = data;
            if ((this.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
                if ((this.Attributes & FileAttributes.Directory) == 0)
                    CompressedSize = IOHelper.GetCompressedFileSize(string.Concat("\\\\?\\", System.IO.Path.Combine(path, filename)));
        }

        public bool IsFolder => (_data.fileAttributes & (int)FileAttributes.Directory) > 0;

        public long Size => ((long)_data.fileSizeHigh << 32) + _data.fileSizeLow;

        public long? CompressedSize { get; } = null;

        public FileAttributes Attributes => (FileAttributes)_data.fileAttributes;

        public DateTime CreationTime => DateTime.FromFileTime(((long)_data.ftCreationTimeHigh << 32) + _data.ftCreationTimeLow);

        public DateTime LastAccessTime => DateTime.FromFileTime(((long)_data.ftLastAccessTimeHigh << 32) + _data.ftLastAccessTimeLow);

        public DateTime LastWriteTime => DateTime.FromFileTime(((long)_data.ftLastWriteTimeHigh << 32) + _data.ftLastWriteTimeLow);

        public string FileName { get; }
        public string Path { get; }
    }
}
