using SizeOnDisk.Shell;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    public class VMFileDetails : ObservableObject, IDisposable
    {
        private string _FileType;
        private DateTime? _CreationTime;
        private DateTime? _LastAccessTime;
        private DateTime? _LastWriteTime;
        private readonly VMFile _vmFile;

        public VMFileDetails(VMFile vmFile)
        {
            _vmFile = vmFile;
        }

        Task task;

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
            this._CreationTime = fileInfo.CreationTime;
            this._LastAccessTime = fileInfo.LastAccessTime;
            this._LastWriteTime = fileInfo.LastWriteTime;

            this._icon = ShellHelper.GetIcon(_vmFile.Path, 16);
            this._thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);

            task = new Task(() =>
            {
                _thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                this.OnPropertyChanged(nameof(Thumbnail));
            });
            task.Start();

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

        /*BitmapSource _bigicon = null;
        public BitmapSource BigIcon
        {
            get
            {
                return _bigicon;
            }
        }*/

        BitmapSource _thumbnail = null;
        //Seems to have problems with VOB
        public BitmapSource Thumbnail
        {
            get
            {
                return _thumbnail;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                if (this.task != null)
                    this.task.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
