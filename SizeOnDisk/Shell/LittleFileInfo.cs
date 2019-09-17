using System;
using System.IO;

namespace SizeOnDisk.Shell
{
    /// <summary>
    /// Smaller FileInfo class like System.IO.FileInfo. 
    /// Bypass the PathTooLongException.
    /// </summary>
    public class LittleFileInfo
    {
        private ShellHelper.WIN32_FILE_ATTRIBUTE_DATA _data;

        internal LittleFileInfo(string path, string filename)
        {
            FileName = filename;
            Path = path;
            FullPath = filename;
            if (string.IsNullOrEmpty(path))
            {
                Path = filename;
            }
            else
            {
                FullPath = System.IO.Path.Combine(path, FullPath);
            }

            string fullfilename = string.Concat("\\\\?\\", FullPath);
            ShellHelper.FillAttributeInfo(fullfilename, ref _data);
            if ((Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
            {
                if ((Attributes & FileAttributes.Directory) == 0)
                {
                    CompressedSize = ShellHelper.GetCompressedFileSize(fullfilename);
                }
            }
        }

        internal LittleFileInfo(string path, string filename, ShellHelper.WIN32_FILE_ATTRIBUTE_DATA data)
        {
            FileName = filename;
            Path = path;
            FullPath = System.IO.Path.Combine(path, filename);
            _data = data;
            if ((Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
            {
                if ((Attributes & FileAttributes.Directory) == 0)
                {
                    CompressedSize = ShellHelper.GetCompressedFileSize(string.Concat("\\\\?\\", System.IO.Path.Combine(path, filename)));
                }
            }
        }

        public bool IsFolder => (_data.fileAttributes & (int)FileAttributes.Directory) > 0;

        public ulong Size => ((ulong)_data.fileSizeHigh << 32) + _data.fileSizeLow;

        public ulong? CompressedSize { get; } = null;

        public FileAttributes Attributes => (FileAttributes)_data.fileAttributes;

        public DateTime CreationTime => DateTime.FromFileTime(((long)_data.ftCreationTimeHigh << 32) + _data.ftCreationTimeLow);

        public DateTime LastAccessTime => DateTime.FromFileTime(((long)_data.ftLastAccessTimeHigh << 32) + _data.ftLastAccessTimeLow);

        public DateTime LastWriteTime => DateTime.FromFileTime(((long)_data.ftLastWriteTimeHigh << 32) + _data.ftLastWriteTimeLow);

        public string FileName { get; }
        public string Path { get; }
        public string FullPath { get; }
    }
}
