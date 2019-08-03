using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WPFByYourCommand;
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
                defaultFileBigIcon = Helper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileBig.png");
            return defaultFileBigIcon;
        }

        private static BitmapImage defaultFileIcon;
        private static BitmapImage GetDefaultFileIcon()
        {
            if (defaultFileBigIcon == null)
                defaultFileBigIcon = Helper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFileSmall.png");
            return defaultFileBigIcon;
        }

        private static BitmapImage defaultFolderBigIcon;
        private static BitmapImage GetDefaultFolderBigIcon()
        {
            if (defaultFolderBigIcon == null)
                defaultFolderBigIcon = Helper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderBig.png");
            return defaultFolderBigIcon;
        }

        private static BitmapImage defaultFolderIcon;
        private static BitmapImage GetDefaultFolderIcon()
        {
            if (defaultFolderIcon == null)
                defaultFolderIcon = Helper.LoadImageResource("pack://application:,,,/SizeOnDisk;component/Icons/UnknownFolderSmall.png");
            return defaultFolderIcon;
        }

        public VMFileDetails(VMFile vmFile)
        {
            _vmFile = vmFile;
        }

        public LittleFileInfo Load()
        {
            LittleFileInfo fileInfo = new LittleFileInfo(_vmFile.Parent.Path, _vmFile.Name);
            this.CreationTime = fileInfo.CreationTime;
            this.LastAccessTime = fileInfo.LastAccessTime;
            this.LastWriteTime = fileInfo.LastWriteTime;

            this.thumbnailInitialized = false;
            this.OnPropertyChanged(nameof(Thumbnail));

            return fileInfo;
        }

        public string FileType
        {
            get
            {
                if (_vmFile.IsFile)
                    return ShellHelper.GetFriendlyName(System.IO.Path.GetExtension(_vmFile.Name));
                else
                    return string.Empty;
            }
        }

        public DateTime CreationTime { get; private set; }
        public DateTime LastAccessTime { get; private set; }
        public DateTime LastWriteTime { get; private set; }
        public BitmapSource Icon
        {
            get
            {
                BitmapSource icon = null;
                try
                {
                    icon = ShellHelper.GetIcon(_vmFile.Path, 16);
                    if (icon == null)
                    {
                        if (_vmFile.IsFile)
                            icon = GetDefaultFileIcon();
                        else
                            icon = GetDefaultFolderIcon();
                    }
                }
                catch (Exception ex)
                {
                    ExceptionBox.ShowException(ex);
                    this._vmFile.LogException(ex);
                }
                return icon;
            }
        }


        private bool thumbnailInitialized = false;
        //Seems to have problems with VOB
        BitmapSource _Thumbnail = null;
        public BitmapSource Thumbnail
        {
            get
            {
                if (!thumbnailInitialized)
                {
                    if (_Thumbnail == null)
                        this._Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96);
                    if (this._Thumbnail == null)
                    {
                        if (_vmFile.IsFile)
                            this._Thumbnail = GetDefaultFileBigIcon();
                        else
                            this._Thumbnail = GetDefaultFolderBigIcon();
                    }
                    else
                    {
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                _Thumbnail = ShellHelper.GetIcon(_vmFile.Path, 96, true);
                                OnPropertyChanged(nameof(Thumbnail));
                            }
                            catch (Exception ex)
                            {
                                ExceptionBox.ShowException(ex);
                                this._vmFile.LogException(ex);
                            }
                        }, TaskCreationOptions.LongRunning);
                    }
                    thumbnailInitialized = true;
                }
                return _Thumbnail;
            }
        }

    }
}
