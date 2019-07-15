using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.IO;
using System.Threading.Tasks;
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

        public LittleFileInfo Load()
        {
            if (_vmFile is VMFolder)
            {
                _FileType = string.Empty;
            }
            else
            {
                _FileType = ShellHelper.GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
            }

            LittleFileInfo fileInfo = new LittleFileInfo(_vmFile.Parent.Path, _vmFile.Name);
            this._Attributes = fileInfo.Attributes;
            this._CreationTime = fileInfo.CreationTime;
            this._LastAccessTime = fileInfo.LastAccessTime;
            this._LastWriteTime = fileInfo.LastWriteTime;

            this._icon = ShellHelper.GetIcon(_vmFile.Path, 16);
            this._thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);

            new Task(() =>
            {
                _thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                this.OnPropertyChanged(nameof(Thumbnail));
            }).Start();

            return fileInfo;
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
