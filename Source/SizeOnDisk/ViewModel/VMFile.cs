using SizeOnDisk.Shell;
using SizeOnDisk.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WPFByYourCommand;

namespace SizeOnDisk.ViewModel
{
    public class VMFile : CommandViewModel
    {
        public static readonly CommandEx OpenCommand = new CommandEx("open", "PresentationCore:ExceptionStringTable:OpenText", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control, "PresentationCore:ExceptionStringTable:OpenKeyDisplayString"));
        public static readonly CommandEx EditCommand = new CommandEx("edit", "Edit", typeof(VMFile), new KeyGesture(Key.E, ModifierKeys.Control, "EditKey"));
        public static readonly CommandEx OpenAsCommand = new CommandEx("openas", "OpenAs", typeof(VMFile), new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Alt, "OpenAsKey"));
        public static readonly CommandEx PrintCommand = new CommandEx("print", "PresentationCore:ExceptionStringTable:PrintText", "pack://application:,,,/SizeOnDisk;component/Icons/PrintHS.png", typeof(VMFile), new KeyGesture(Key.P, ModifierKeys.Control, "PresentationCore:ExceptionStringTable:PrintKeyDisplayString"));
        public static readonly CommandEx ExploreCommand = new CommandEx("explore", "Explore", "pack://application:,,,/SizeOnDisk;component/Icons/Folder.png", typeof(VMFile), new KeyGesture(Key.N, ModifierKeys.Control, "ExploreKey"));
        public static readonly CommandEx FindCommand = new CommandEx("find", "PresentationCore:ExceptionStringTable:FindText", "pack://application:,,,/SizeOnDisk;component/Icons/SearchFolderHS.png", typeof(VMFile), new KeyGesture(Key.F, ModifierKeys.Control, "PresentationCore:ExceptionStringTable:FindKeyDisplayString"));
        public static readonly CommandEx DeleteCommand = new CommandEx("delete", "PresentationCore:ExceptionStringTable:DeleteText", "pack://application:,,,/SizeOnDisk;component/Icons/Recycle_Bin_Empty.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.None, "PresentationCore:ExceptionStringTable:DeleteKeyDisplayString"));
        public static readonly CommandEx PermanentDeleteCommand = new CommandEx("permanentdelete", "PermanentDelete", "pack://application:,,,/SizeOnDisk;component/Icons/DeleteHS.png", typeof(VMFile), new KeyGesture(Key.Delete, ModifierKeys.Shift, "PermanentDeleteKey"));
        public static readonly CommandEx PropertiesCommand = new CommandEx("properties", "PresentationCore:ExceptionStringTable:PropertiesText", typeof(VMFile), new KeyGesture(Key.F4, ModifierKeys.None, "PresentationCore:ExceptionStringTable:PropertiesKeyDisplayString"));


        public override void AddCommandModels(CommandBindingCollection bindingCollection)
        {
            bindingCollection.Add(new CommandBinding(OpenCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(OpenAsCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(EditCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(PrintCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(ExploreCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(FindCommand, CallShellCommand, CanCallShellCommand));
            bindingCollection.Add(new CommandBinding(DeleteCommand, CallDeleteCommand, CanCallDeleteCommand));
            bindingCollection.Add(new CommandBinding(PermanentDeleteCommand, CallPermanentDeleteCommand, CanCallDeleteCommand));
            bindingCollection.Add(new CommandBinding(PropertiesCommand, CallShellCommand, CanCallShellCommand));
        }

        public override void AddInputModels(InputBindingCollection bindingCollection)
        {
        }


        #region fields

        private string _Name;

        private long? _DiskSize = null;
        private long? _FileSize = null;
        private long? _FileCount = 1;
        private long? _FolderCount = null;

        private bool _IsProtected = false;

        private bool _isTreeSelected;

        #endregion fields

        #region constructor

        [DesignOnly(true)]
        internal VMFile(VMFolder parent, string name) : this(parent, name, null)
        {

        }

        internal VMFile(VMFolder parent, string name, string path)
        {
            _Name = name;
            Path = path;
            Parent = parent;
        }

        #endregion constructor

        #region properties

        public VMFolder Parent { get; }

        public string Path { get; private set; }

        public string Name
        {
            get { return _Name; }
            set
            {
                this.Rename(value);
            }
        }

        public void Rename(string newName)
        {
            if (this.Name != newName)
            {
                string newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Path), newName);
                if (this is VMFolder)
                {
                    File.Move(this.Path, newPath);
                }
                else
                {
                    Directory.Move(this.Path, newPath);
                }
                _Name = newName;
                Path = newPath;
                this.OnPropertyChanged(nameof(Name));
                this.OnPropertyChanged(nameof(Path));
            }
        }

        public long? FileCount
        {
            get { return _FileCount; }
            protected set { SetProperty(ref _FileCount, value); }
        }

        public long? FolderCount
        {
            get { return _FolderCount; }
            protected set { SetProperty(ref _FolderCount, value); }
        }

        public long? DiskSize
        {
            get { return _DiskSize; }
            protected set { SetProperty(ref _DiskSize, value); }
        }

        public long? FileSize
        {
            get { return _FileSize; }
            protected set { SetProperty(ref _FileSize, value); }
        }

        public bool IsProtected
        {
            get { return _IsProtected; }
            protected set { SetProperty(ref _IsProtected, value); }
        }


        private bool _isSelected = false;
        public virtual bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        public virtual bool IsTreeSelected
        {
            get { return _isTreeSelected; }
            set
            {
                if (value != _isTreeSelected)
                {
                    _isTreeSelected = value;
                    if (_isTreeSelected && this.Parent != null)
                    {
                        this.Parent.IsExpanded = true;
                        this.SelectItem();
                    }
                    this.OnPropertyChanged(nameof(IsTreeSelected));
                }
            }
        }

        #endregion properties

        #region functions

        protected virtual void SelectItem()
        {
            this.Parent.SelectItem();
        }


        public virtual void Refresh(uint clusterSize, ParallelOptions parallelOptions)
        {
            if (parallelOptions != null && parallelOptions.CancellationToken.IsCancellationRequested)
                return;

            LittleFileInfo fileInfo = new Utilities.LittleFileInfo(this.Path);
            this.FileSize = fileInfo.Size;
            this.DiskSize = ((((fileInfo.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed ?
                fileInfo.CompressedSize : this.FileSize)
                + clusterSize - 1) / clusterSize) * clusterSize;
        }

        VMFileAttributes _Attributes;
        public VMFileAttributes Attributes
        {
            get
            {
                if (_Attributes == null)
                    RefreshOnView();
                return _Attributes;
            }
        }

        public void RefreshOnView()
        {
            if (this is VMRootHierarchy)
                return;
            try
            {
                _Attributes = new VMFileAttributes(this);
            }
            catch (UnauthorizedAccessException)
            {
                this.IsProtected = true;
            }
            this.OnPropertyChanged(nameof(Attributes));
            this._thumbnail = null;
            this.OnPropertyChanged(nameof(Thumbnail));
        }

        //For VisualStudio Watch
        public override string ToString()
        {
            return string.Concat(this.GetType().Name, ": ", Path);
        }

        #endregion functions

        #region Commands


        private static void CallDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            file.Parent.DeleteAllSelectedFiles();
        }

        private static void CallPermanentDeleteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            file.Parent.PermanentDeleteAllSelectedFiles();
        }

        private static void CanCallDeleteCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = false;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                return;

            e.CanExecute = !(file is VMRootFolder);
        }

        private static void CanCallShellCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = false;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                return;

            RoutedCommand command = e.Command as RoutedCommand;
            if (command == null)
                return;
            bool isFolder = file is VMFolder;
            if (command == PropertiesCommand
                || (command == ExploreCommand && (!isFolder || !file.IsProtected))
                || (command == FindCommand && isFolder && !file.IsProtected)
                || (command == OpenAsCommand && !isFolder
                || (command == OpenCommand && isFolder && !(file is VMRootHierarchy) && !file.IsProtected)))
            {
                e.CanExecute = true;
                return;
            }

            e.CanExecute = ShellHelper.CanCallShellCommand(file.Path, command.Name);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308")]
        protected virtual void CallShellCommand(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;

            VMFile file = GetViewModelObject<VMFile>(e.OriginalSource);
            if (file == null)
                throw new ArgumentNullException("e", "OriginalSource is not VMFile");

            RoutedCommand command = e.Command as RoutedCommand;
            if (command == null)
                throw new ArgumentNullException("e", "Command is not RoutedCommand");

            if (file is VMFolder && command == OpenCommand)
                ShellHelper.ShellExecute(file.Path, OpenCommand.Name.ToLowerInvariant(), new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
            else
            {
                string path = file.Path;
                if (e.Command == ExploreCommand && !(file is VMFolder))
                    path = file.Parent.Path;
                ShellHelper.ShellExecute(path, command.Name.ToLowerInvariant(), new System.Windows.Interop.WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle);
            }
        }


        #endregion Commands





        BitmapSource _icon = null;
        public BitmapSource Icon
        {
            get
            {
                if (_icon == null)
                {
                    _icon = ShellHelper.GetIcon(this.Path, 16, false);
                }
                return _icon;
            }
        }

        private void RefreshThumbnail()
        {
            if (_thumbnail == null)
            {
                _thumbnail = ShellHelper.GetIcon(this.Path, 96, false);
                Task.Run(() =>
                {
                    BitmapSource tmp = ShellHelper.GetIcon(this.Path, 96, true);
                    if (tmp != null)
                        _thumbnail = tmp;
                    this.OnPropertyChanged(nameof(Thumbnail));
                });
            }
        }

        BitmapSource _thumbnail = null;
        //Seems to have problems with VOB
        public BitmapSource Thumbnail
        {
            get
            {
                if (_thumbnail == null)
                {
                    RefreshThumbnail();
                }
                return _thumbnail;
            }
        }

    }
}
