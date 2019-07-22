using SizeOnDisk.Shell;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    public class VMFileDetails : ObservableObject, IDisposable
    {
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
                FileType = string.Empty;
            }
            else
            {
                FileType = ShellHelper.GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
            }

            LittleFileInfo fileInfo = new LittleFileInfo(_vmFile.Parent.Path, _vmFile.Name);
            this.CreationTime = fileInfo.CreationTime;
            this.LastAccessTime = fileInfo.LastAccessTime;
            this.LastWriteTime = fileInfo.LastWriteTime;

            this.Icon = ShellHelper.GetIcon(_vmFile.Path, 16);
            this.Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);

            task = new Task(() =>
            {
                Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                this.OnPropertyChanged(nameof(Thumbnail));
            });
            task.Start();

            return fileInfo;
        }

        public string FileType { get; private set; }
        public DateTime? CreationTime { get; private set; }
        public DateTime? LastAccessTime { get; private set; }
        public DateTime? LastWriteTime { get; private set; }
        public BitmapSource Icon { get; private set; } = null;

        //Seems to have problems with VOB
        public BitmapSource Thumbnail { get; private set; } = null;

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
