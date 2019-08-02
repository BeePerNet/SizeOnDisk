using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPFByYourCommand.Observables;

namespace SizeOnDisk.ViewModel
{
    public class VMFileDetails : ObservableObject
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

            this._Thumbnail = null;
            this.OnPropertyChanged(nameof(Thumbnail));

            return fileInfo;
        }

        public string FileType { get; private set; }
        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public BitmapSource Icon
        {
            get
            {
                
                BitmapSource icon = ShellHelper.GetIcon(_vmFile.Path, 16);
                if (icon == null)
                    icon = GetDefaultFileIcon();
                return icon;
            }
        }

        Task task;
        //Seems to have problems with VOB
        BitmapSource _Thumbnail = null;
        public BitmapSource Thumbnail
        {
            get
            {
                if (_Thumbnail == null)
                {
                    this._Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);
                    if (this._Thumbnail == null)
                        this._Thumbnail = GetDefaultFileBigIcon();
                    else
                    {
                        if (task == null)
                        {
                            task = Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    _Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                                    OnPropertyChanged(nameof(Thumbnail));
                                }
                                catch (Exception ex)
                                {
                                    ExceptionBox.ShowException(ex);
                                }
                                task = null;
                            }, TaskCreationOptions.LongRunning);
                        }
                    }
                }

                return _Thumbnail;
            }
        }

    }
}
