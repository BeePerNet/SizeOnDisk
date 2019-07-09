using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace SizeOnDisk.ViewModel
{
    public class VMFileDetails
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
                _FileType = ShellHelper.GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
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
