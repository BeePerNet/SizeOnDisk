using SizeOnDisk.Shell;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    public class VMFileDetails : ObservableObject, IDisposable
    {
        private readonly VMFile _vmFile;
        private static BitmapImage defaultFileBigIcon;

        private static BitmapImage GetDefaultFileBigIcon()
        {
            if (defaultFileBigIcon == null)
            {
                BitmapImage bmi = new BitmapImage();
                bmi.BeginInit();
                bmi.UriSource = new Uri("pack://application:,,,/SizeOnDisk;component/Icons/NotFoundIconBig.png");
                bmi.EndInit();

                bmi.Freeze();
                defaultFileBigIcon = bmi;
            }
            return defaultFileBigIcon;
        }

        private static BitmapImage defaultFileIcon;

        private static BitmapImage GetDefaultFileIcon()
        {
            if (defaultFileIcon == null)
            {
                BitmapImage bmi = new BitmapImage();
                bmi.BeginInit();
                bmi.UriSource = new Uri("pack://application:,,,/SizeOnDisk;component/Icons/NotFoundIcon.png");
                bmi.EndInit();

                bmi.Freeze();
                defaultFileIcon = bmi;
            }
            return defaultFileIcon;
        }

        public VMFileDetails(VMFile vmFile)
        {
            _vmFile = vmFile;
        }

        Task task;
        CancellationTokenSource cancellationTokenSource;

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
            if (this.Icon == null)
                this.Icon = GetDefaultFileIcon();

            if (Thumbnail == null)
            {
                this.Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);
                if (this.Thumbnail == null)
                    this.Thumbnail = GetDefaultFileBigIcon();
            }
            if (this.Thumbnail != GetDefaultFileBigIcon())
            {
                cancellationTokenSource = new CancellationTokenSource();
                task = Task.Run(() =>
                {
                    cancellationTokenSource.CancelAfter(10000);
                    Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                    this.OnPropertyChanged(nameof(Thumbnail));
                    task = null;
                }, cancellationTokenSource.Token);//.CancelAfter(10000);
            }
            return fileInfo;
        }

        public string FileType { get; private set; }
        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public BitmapSource Icon { get; private set; } = null;

        //Seems to have problems with VOB
        public BitmapSource Thumbnail { get; private set; } = null;




        bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (cancellationTokenSource != null && cancellationTokenSource.Token.CanBeCanceled)
                {
                    cancellationTokenSource.Cancel(true);
                    cancellationTokenSource.Dispose();
                }
                // dispose managed resources
                if (this.task != null)
                    this.task.Dispose();
            }
            // free native resources
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
