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
            if (_vmFile.IsFile)
                FileType = ShellHelper.GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
            else
                FileType = string.Empty;

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
        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public BitmapSource Icon { get; private set; } = null;

        //Seems to have problems with VOB
        public BitmapSource Thumbnail { get; private set; } = null;

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            // free native resources
            if (disposed)
                return;

            try
            {
                if (disposing)
                {
                    if (this.task != null)
                        this.task.Dispose();
                }
            }
            finally
            {
                disposed = true;
            }

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
